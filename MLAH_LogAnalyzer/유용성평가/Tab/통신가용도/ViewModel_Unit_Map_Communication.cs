
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using DevExpress.Map;
using DevExpress.Map.Kml.Model;
using DevExpress.Xpf.Bars;
// [확인 후 삭제] 미사용 using
//using DevExpress.Xpf.CodeView.Margins;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Map;
// [확인 후 삭제] 미사용 using
//using DevExpress.XtraPrinting.Native;
// [확인 후 삭제] 미사용 using
//using Windows.Devices.Geolocation;
// [확인 후 삭제] 미사용 using
//using Windows.Storage.Provider;
// [확인 후 삭제] 미사용 using
//using static DevExpress.Charts.Designer.Native.BarThicknessEditViewModel;
// [확인 후 삭제] 미사용 using
//using static DevExpress.Utils.Drawing.Helpers.NativeMethods;

namespace MLAH_LogAnalyzer
{
    public class ViewModel_Unit_Map_Communication : CommonBase
    {
        private static readonly Lazy<ViewModel_Unit_Map_Communication> _lazy =
        new Lazy<ViewModel_Unit_Map_Communication>(() => new ViewModel_Unit_Map_Communication());

        public static ViewModel_Unit_Map_Communication SingletonInstance => _lazy.Value;

        #region 생성자 & 콜백
        public ViewModel_Unit_Map_Communication()
        {
            // ✅ 디자이너에서 이벤트 구독을 막는 보호 코드를 추가합니다.
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return;
            }

            // [확인 후 삭제] 미사용 콜백 구독 - Callback_OnDevelopPathPlanAdd 주석처리됨
            //CommonEvent.OnDevelopPathPlanAdd += Callback_OnDevelopPathPlanAdd;

            CommonEvent.OnINITMissionPolygonAdd += Callback_OnINITMissionPolygonAdd;
            CommonEvent.OnINITMissionPolyLineAdd += Callback_OnINITMissionPolyLineAdd;

            CommonEvent.OnINITMissionPointAdd += Callback_OnINITMissionPointAdd;


            FocusSquareItems = new ObservableCollection<MapPolygon>();
        }

        #endregion 생성자 & 콜백

        // Frozen 브러시 (렌더링 최적화)
        private static readonly SolidColorBrush _footprintFill;
        private static readonly SolidColorBrush _footprintStroke;
        static ViewModel_Unit_Map_Communication()
        {
            _footprintFill = new SolidColorBrush(Color.FromArgb(102, 255, 255, 0));
            _footprintFill.Freeze();
            _footprintStroke = new SolidColorBrush(Colors.Yellow);
            _footprintStroke.Freeze();
        }

        // AircraftID별 FlightData 인덱스 (타임스탬프 오름차순 정렬, BinarySearch용)
        private Dictionary<uint, List<FlightData>> _flightDataIndex;

        /// <summary>
        /// 시나리오 로드 시 1회 호출. 이후 슬라이더 이동 시 O(logN)으로 검색.
        /// </summary>
        public void BuildFlightDataIndex(ScenarioData scenarioData)
        {
            _flightDataIndex = new Dictionary<uint, List<FlightData>>();
            if (scenarioData?.FlightData == null) return;

            foreach (var group in scenarioData.FlightData
                .Where(fd => fd.FlightDataLog != null && fd.Timestamp > 0)
                .GroupBy(fd => fd.AircraftID))
            {
                _flightDataIndex[group.Key] = group.OrderBy(fd => fd.Timestamp).ToList();
            }
        }

        /// <summary>
        /// BinarySearch로 timestamp 이하의 가장 가까운 FlightData를 O(logN)으로 검색
        /// </summary>
        private FlightData FindFlightDataAtOrBefore(uint aircraftId, ulong timestamp)
        {
            if (_flightDataIndex == null || !_flightDataIndex.TryGetValue(aircraftId, out var list) || list.Count == 0)
                return null;

            int lo = 0, hi = list.Count - 1;
            int bestIdx = -1;
            while (lo <= hi)
            {
                int mid = lo + (hi - lo) / 2;
                if (list[mid].Timestamp <= timestamp)
                {
                    bestIdx = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return bestIdx >= 0 ? list[bestIdx] : null;
        }

        private class CommTrackDto
        {
            public List<GeoPoint> Points { get; set; } = new List<GeoPoint>();
            public bool IsFail { get; set; }
            public uint UavId { get; set; }
        }

        /// <summary>
        /// 평가용: 시나리오의 전체 UAV 항적 (Polyline)
        /// (XAML의 EvaluationTrackLayer에 바인딩됨)
        /// </summary>
        private ObservableCollection<MapPolyline> _evaluationTracks = new ObservableCollection<MapPolyline>();
        public ObservableCollection<MapPolyline> EvaluationTracks { get => _evaluationTracks; set { _evaluationTracks = value; OnPropertyChanged(nameof(EvaluationTracks)); } }

        /// <summary>
        /// 평가용: 트랙바가 선택한 시점의 UAV 위치 (심볼)
        /// (XAML의 EvaluationUavPositionLayer에 바인딩됨)
        /// </summary>
        public ObservableCollection<UnitMapObjectInfo> EvaluationUavPositions { get; set; } = new ObservableCollection<UnitMapObjectInfo>();

        /// <summary>
        /// 평가용: 트랙바가 선택한 시점의 UAV 풋프린트 (Polygon)
        /// (XAML의 EvaluationFootprintLayer에 바인딩됨)
        /// </summary>

        private GeoPoint _centerPoint = new GeoPoint(38.128774, 127.318005); // 초기값 설정
        public GeoPoint CenterPoint
        {
            get => _centerPoint;
            set
            {
                _centerPoint = value;
                OnPropertyChanged(nameof(CenterPoint)); // OnPropertyChanged 호출 확인
            }
        }
        // [확인 후 삭제] 미사용 필드 - StartSelectedObjectVisualsLoop가 빈 메서드라 실제 할당 없음
        //private MapPolygon? _focusPolygon;
        //private readonly List<MapLine> _sideLines = new List<MapLine>();

        //private CancellationTokenSource _focusUpdateCts;
        private CancellationTokenSource _visualsUpdateCts;

        public ObservableCollection<MapPolygon> FocusSquareItems { get; } = new ObservableCollection<MapPolygon>();

        // 아이콘 캐시: ID → UnitMapObjectInfo (FirstOrDefault 제거)
        private Dictionary<uint, UnitMapObjectInfo> _cachedAircraftIcons = new();

        /// <summary>
        /// (XAML의 EvaluationCommFalseLayer에 바인딩됨)
        /// </summary>
        //public ObservableCollection<MapPolyline> EvaluationLosFalseTracks { get; set; } = new ObservableCollection<MapPolyline>();

        private ObservableCollection<MapPolyline> _EvaluationLosFalseTracks = new ObservableCollection<MapPolyline>();
        public ObservableCollection<MapPolyline> EvaluationLosFalseTracks
        {
            get => _EvaluationLosFalseTracks;
            set
            {
                if (_EvaluationLosFalseTracks != value)
                {
                    _EvaluationLosFalseTracks = value;
                    OnPropertyChanged(nameof(EvaluationLosFalseTracks));
                }
            }
        }

        /// <summary>
        /// [신규] 트랙바 스냅용: 특정 시점의 UAV 위치와 풋프린트를 표시
        /// </summary>
        public void ShowUavSnapshot(UavSnapshot snapshot, int uavId)
        {
            if (snapshot == null) return;

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // 1. 이전 스냅샷(심볼, 풋프린트) 제거
                EvaluationUavPositions.Clear();

                // 2. 현재 UAV 위치 심볼 그리기
                if (snapshot.Position != null)
                {
                    // XAML의 ElementTemplate(UnitMapObjectInfo)에 바인딩하기 위해
                    // 임시 UnitObjectInfo 객체를 생성 (JSON 데이터가 없으므로 Heading 등은 0)
                    var dummyInfo = new UnitObjectInfo
                    {
                        ID = uavId,
                        Type = 1, // 1 = UAV
                        Status = 1, // 1 = Normal
                        LOC = new CoordinateInfo
                        {
                            Latitude = (float)snapshot.Position.Latitude,
                            Longitude = (float)snapshot.Position.Longitude
                        },
                        velocity = new Velocity { Heading = 0 } // Heading 데이터가 없으므로 0
                    };

                    // ConvertToObjectInfo를 사용해 올바른 이미지 소스를 가진 UnitMapObjectInfo 생성
                    var mapSymbol = ConvertToObjectInfo(dummyInfo);
                    EvaluationUavPositions.Add(mapSymbol);
                }

                // 3. 현재 풋프린트 폴리곤 그리기
                if (snapshot.Footprint != null && snapshot.Footprint.CameraTopLeft != null)
                {
                    var footprintPolygon = new MapPolygon
                    {
                        Fill = _footprintFill,
                        Stroke = _footprintStroke,

                        // XAML의 StrokeStyle
                        StrokeStyle = new StrokeStyle { Thickness = 1 }
                    };

                }
            });
        }

        public async void UpdateCommunicationTracks(ScenarioData scenarioData, CommunicationResult commResult)
        {
            ClearEvaluationData();
            if (scenarioData == null || scenarioData.FlightData == null) return;

            // 1. [백그라운드 스레드] 수학 계산 및 순수 좌표(List<GeoPoint>)만 수집
            var resultData = await Task.Run(() =>
            {
                var dtos = new List<CommTrackDto>();
                GeoPoint? centerPoint = null;

                var aircraftIdsToTrack = new List<uint> { 1, 4, 5, 6 };
                var allTracks = scenarioData.FlightData
                    .Where(fd => aircraftIdsToTrack.Contains(fd.AircraftID) && fd.FlightDataLog != null)
                    .OrderBy(fd => fd.Timestamp)
                    .GroupBy(fd => fd.AircraftID);

                foreach (var trackGroup in allTracks)
                {
                    uint currentAircraftId = trackGroup.Key;
                    HashSet<ulong> falseTimestamps = null;

                    if (commResult != null && currentAircraftId != 1)
                    {
                        string uavKey = $"UAV{currentAircraftId}";
                        if (commResult.LOSFalseTimestamps.ContainsKey(uavKey))
                        {
                            falseTimestamps = new HashSet<ulong>(commResult.LOSFalseTimestamps[uavKey]);
                        }
                    }

                    var trackPoints = trackGroup
                        .Select(fd => new {
                            Point = new GeoPoint(fd.FlightDataLog.Latitude, fd.FlightDataLog.Longitude),
                            Timestamp = fd.Timestamp
                        }).ToList();

                    if (centerPoint == null && trackPoints.Any())
                    {
                        centerPoint = trackPoints.First().Point;
                    }

                    var currentPoints = new List<GeoPoint>();
                    bool wasLastSegmentFail = false;

                    for (int i = 0; i < trackPoints.Count - 1; i++)
                    {
                        var p1 = trackPoints[i];
                        var p2 = trackPoints[i + 1];

                        bool isCurrentSegmentFail = (falseTimestamps != null && falseTimestamps.Contains(p1.Timestamp));

                        if (isCurrentSegmentFail)
                        {
                            // 실패 구간 DTO 저장
                            dtos.Add(new CommTrackDto { Points = new List<GeoPoint> { p1.Point, p2.Point }, IsFail = true, UavId = currentAircraftId });

                            if (!wasLastSegmentFail && currentPoints.Any())
                            {
                                dtos.Add(new CommTrackDto { Points = currentPoints, IsFail = false, UavId = currentAircraftId });
                            }
                            currentPoints = new List<GeoPoint>();
                            wasLastSegmentFail = true;
                        }
                        else
                        {
                            if (wasLastSegmentFail || !currentPoints.Any())
                            {
                                currentPoints.Add(p1.Point);
                            }
                            currentPoints.Add(p2.Point);
                            wasLastSegmentFail = false;
                        }
                    }

                    if (currentPoints.Any())
                    {
                        dtos.Add(new CommTrackDto { Points = currentPoints, IsFail = false, UavId = currentAircraftId });
                    }
                }

                return new { Dtos = dtos, Center = centerPoint };
            });

            // 2. [UI 스레드] 수집된 좌표를 바탕으로 MapPolyline(UI 객체) 생성 및 적용
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                var newTracks = new List<MapPolyline>();
                var newFalseTracks = new List<MapPolyline>();

                foreach (var dto in resultData.Dtos)
                {
                    var coll = new CoordPointCollection();
                    // 실패 구간(2포인트)은 그대로, 성공 구간은 매 3번째 포인트만 취하여 렌더링 부하 감소
                    if (dto.IsFail || dto.Points.Count <= 6)
                    {
                        foreach (var p in dto.Points) coll.Add(p);
                    }
                    else
                    {
                        for (int i = 0; i < dto.Points.Count; i++)
                        {
                            if (i % 3 == 0 || i == dto.Points.Count - 1)
                                coll.Add(dto.Points[i]);
                        }
                    }

                    var polyline = new MapPolyline { Points = coll };

                    if (dto.IsFail)
                    {
                        polyline.Stroke = Brushes.Red;
                        polyline.StrokeStyle = new StrokeStyle { Thickness = 3 };
                        newFalseTracks.Add(polyline);
                    }
                    else
                    {
                        polyline.Stroke = (dto.UavId == 1) ? Brushes.Cyan : Brushes.Blue;
                        polyline.StrokeStyle = new StrokeStyle { Thickness = 2 };
                        newTracks.Add(polyline);
                    }
                }

                EvaluationTracks = new ObservableCollection<MapPolyline>(newTracks);
                EvaluationLosFalseTracks = new ObservableCollection<MapPolyline>(newFalseTracks);

                if (resultData.Center != null)
                {
                    CenterPoint = new GeoPoint(resultData.Center.GetY(), resultData.Center.GetX());
                    CurrentZoomLevel = 15;
                }
            });
        }

        public void ShowAircraftPositionsAt(ulong timestamp, ScenarioData scenarioData)
        {
            if (scenarioData == null) return;

            // 인덱스가 없으면 최초 1회 빌드
            if (_flightDataIndex == null)
                BuildFlightDataIndex(scenarioData);

            // UI 스레드 외부: BinarySearch로 데이터 검색 O(logN)
            var aircraftIdsToShow = new uint[] { 1, 4, 5, 6 };
            var positions = new Dictionary<uint, FlightDataLog>();

            foreach (var id in aircraftIdsToShow)
            {
                var lastData = FindFlightDataAtOrBefore(id, timestamp);
                if (lastData != null)
                    positions[id] = lastData.FlightDataLog;
            }

            // UI 스레드: 아이콘 위치만 갱신 (Dictionary 캐시로 O(1) 조회)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                foreach (var id in aircraftIdsToShow)
                {
                    _cachedAircraftIcons.TryGetValue(id, out var existingIcon);

                    if (positions.TryGetValue(id, out var loc))
                    {
                        if (existingIcon != null)
                        {
                            existingIcon.Location = new GeoPoint(loc.Latitude, loc.Longitude);
                        }
                        else
                        {
                            var dummyInfo = new UnitObjectInfo
                            {
                                ID = (int)id,
                                Type = (id == 1) ? (short)3 : (short)1,
                                Status = 1,
                                LOC = new CoordinateInfo { Latitude = loc.Latitude, Longitude = loc.Longitude },
                                velocity = new Velocity { Heading = 0 }
                            };
                            var icon = ConvertToObjectInfo(dummyInfo);
                            _cachedAircraftIcons[id] = icon;
                            EvaluationUavPositions.Add(icon);
                        }
                    }
                    else if (existingIcon != null)
                    {
                        EvaluationUavPositions.Remove(existingIcon);
                        _cachedAircraftIcons.Remove(id);
                    }
                }
            });
        }

        /// <summary>
        /// [신규] 시나리오 선택 변경 시 이전 항적/스냅샷을 모두 지우기
        /// </summary>
        public void ClearEvaluationData()
        {
            _flightDataIndex = null; // 시나리오 변경 시 인덱스 재빌드
            _cachedAircraftIcons.Clear();
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                EvaluationTracks.Clear();
                EvaluationUavPositions.Clear();
                EvaluationLosFalseTracks.Clear();
            });
        }

        


        private double _currentZoomLevel = 11.0;
        public double CurrentZoomLevel
        {
            get => _currentZoomLevel;
            set
            {
                if (Math.Abs(_currentZoomLevel - value) < 0.01) return;
                _currentZoomLevel = value;
                OnPropertyChanged(nameof(CurrentZoomLevel));
            }
        }


        /// <summary>
        /// 선택된 객체에 대한 시각적 효과(포커스 사각형, UAV 촬영 영역)를 업데이트하는 루프를 시작합니다.
        /// </summary>
        public void StartSelectedObjectVisualsLoop()
        {
            _visualsUpdateCts?.Cancel();
            _visualsUpdateCts = new CancellationTokenSource();
            var token = _visualsUpdateCts.Token;

            //Task.Run(async () =>
            //{
            //    while (!token.IsCancellationRequested)
            //    {
            //        try
            //        {
            //            //await Task.Delay(100, token); // 약 10 FPS로 업데이트
            //            await Task.Delay(41, token); // 약 10 FPS로 업데이트
            //            var selected = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject;

            //            // Dispatcher.InvokeAsync 호출 시 CancellationToken 인수를 제거했습니다.
            //            _ = Application.Current.Dispatcher.InvokeAsync(() =>
            //            {
            //                // --- 1. 선택 해제 시 모든 시각적 요소 제거 ---
            //                if (selected == null || selected.ID == 0)
            //                {
            //                    if (_focusPolygon != null)
            //                    {
            //                        FocusSquareItems.Clear();
            //                        _focusPolygon = null;
            //                    }
            //                    if (_footprintPolygon != null)
            //                    {
            //                        FourCornerItems.Clear();
            //                        FootprintSideLines.Clear();
            //                        _footprintPolygon = null;
            //                        _sideLines.Clear();
            //                    }
            //                    return;
            //                }

            //                // --- 2. 포커스 사각형 업데이트 (모든 객체 공통) ---
            //                UpdateFocusSquare(selected);

            //                // --- 3. UAV 촬영 영역 업데이트 (Type == 1일 때만) ---
            //                UpdateUavFootprint(selected);

            //            });
            //        }
            //        catch (TaskCanceledException) { break; }
            //        catch (Exception) { /* 종료 시 예외 무시 */ }
            //    }
            //}, token);
        }

        /// <summary>
        /// 시각적 효과 업데이트 루프를 중지합니다.
        /// </summary>
        public void StopSelectedObjectVisualsLoop()
        {
            _visualsUpdateCts?.Cancel();
            _visualsUpdateCts?.Dispose();
            _visualsUpdateCts = null;

            // 루프 중지 시 모든 시각적 요소 정리
            Application.Current.Dispatcher.BeginInvoke(() => {
                FocusSquareItems.Clear();
                // [확인 후 삭제] 미사용 필드 참조 - 선언부 주석처리됨
                //_focusPolygon = null;
                //_sideLines.Clear();
            });
        }

        /// <summary>
        /// 포커스 사각형을 생성하거나 업데이트합니다.
        /// </summary>
        //private void UpdateFocusSquare(UnitObjectInfo selected)
        //{
        //    if (_focusPolygon == null)
        //    {
        //        _focusPolygon = new MapPolygon
        //        {
        //            Fill = Brushes.Transparent,
        //            StrokeStyle = new StrokeStyle { Thickness = 2 }
        //        };
        //        FocusSquareItems.Add(_focusPolygon);
        //    }

        //    const double baseZoomLevel = 12.0;
        //    const double baseHalfMeter = 1000.0;
        //    double zoomFactor = Math.Pow(2, baseZoomLevel - this.CurrentZoomLevel);
        //    double halfMeter = baseHalfMeter * zoomFactor;

        //    double centerLat = selected.LOC.Latitude;
        //    double centerLon = selected.LOC.Longitude;
        //    double earthRadius = 6378137.0;
        //    double dLat = (halfMeter / earthRadius) * (180.0 / Math.PI);
        //    double dLon = (halfMeter / (earthRadius * Math.Cos(centerLat * Math.PI / 180.0))) * (180.0 / Math.PI);

        //    var newCorners = new CoordPointCollection
        //    {
        //        new GeoPoint(centerLat + dLat, centerLon - dLon),
        //        new GeoPoint(centerLat + dLat, centerLon + dLon),
        //        new GeoPoint(centerLat - dLat, centerLon + dLon),
        //        new GeoPoint(centerLat - dLat, centerLon - dLon)
        //    };

        //    var strokeBrush = selected.Identification switch
        //    {
        //        1 => Brushes.Blue,
        //        2 => Brushes.Red,
        //        _ => Brushes.Purple
        //    };

        //    _focusPolygon.Stroke = strokeBrush;
        //    _focusPolygon.Points = newCorners;
        //}

        
       



        public void MapClear()
        {
            ObjectDisplayList.Clear();
            FocusSquareList.Clear();

            INITMissionPointList.Clear();
            INITMissionLineList.Clear();
            INITMissionPolygonList.Clear();

        }



        // 아이콘 사이즈 (픽셀)
        private double _iconSize = 60; // 초기 임의값
        public double IconSize
        {
            get => _iconSize;
            set
            {
                if (_iconSize != value)
                {
                    _iconSize = value;
                    //View_Unit_Map.SingletonInstance.UpdateFocusSquare();
                    OnPropertyChanged(nameof(IconSize));
                }
            }
        }


        // [확인 후 삭제] 미사용 메서드 - 빈 콜백 (내부 코드 전부 주석처리됨)
        //private void Callback_OnDevelopPathPlanAdd(int PathID, List<CoordPoint> PathPointList)
        //{
        //}

    

        public UnitMapObjectInfo ConvertToObjectInfo(UnitObjectInfo input)
        {
            // 1) 새 ObjectInfo 생성
            var info = new UnitMapObjectInfo();

            // 2) 초기 설정 (한번만 복사)
            info.ID = (uint)input.ID;
            info.Location = new GeoPoint(input.LOC.Latitude, input.LOC.Longitude);
            info.Heading = input.velocity.Heading;
            info.Type = input.Type;
            info.Status = input.Status;

            IValueConverter converter = (IValueConverter)Application.Current.Resources["ObjectTypeToStringConverter"];
            info.TypeString = converter.Convert(input.Type, typeof(string), null, CultureInfo.CurrentCulture) as string;

            if (input.Type == 1)
            {
                info.PlatformString = "UAV";
            }
            else if (input.Type == 3)
            {
                info.PlatformString = "LAH";
            }
            else
            {
                IMultiValueConverter multiConverter = (IMultiValueConverter)Application.Current.Resources["ObjectPlatformMultiValueConverter"];
                // 컨버터에 전달할 값 배열: 
                // values[0] : 플랫폼 타입, values[1] : 객체 유형 (컨버터의 주석 참고)
                object[] values = new object[] { input.PlatformType, input.Type };

                // 멀티 컨버터의 Convert 메서드 호출
                string convertedString = multiConverter.Convert(values, typeof(string), null, CultureInfo.CurrentCulture) as string;

                // 결과를 원하는 변수에 할당
                info.PlatformString = convertedString;
            }

            if (input.Type == 3) // 유인기(LAH)인 경우
            {
                if (input.Status == 2) // 비정상 상태가 최우선
                {
                    info.imagesource = (ImageSource)Application.Current.Resources["LAH_RED"];
                }
                else if (input.ID == 1) // 정상 상태의 지휘기
                {
                    info.imagesource = (ImageSource)Application.Current.Resources["LAH_BLUE"];
                }
                else // 정상 상태의 편대기
                {
                    info.imagesource = (ImageSource)Application.Current.Resources["LAH"];
                }
            }
            else // 유인기가 아닌 다른 모든 객체
            {
                info.imagesource = Imagesource_Allocater(input.Type);
            }

            return info;
        }

        public ImageSource Imagesource_Allocater(int type)
        {
            //var temp_imagesource = (ImageSource)Application.Current.Resources["Tank"];
            var temp_imagesource = (ImageSource)Application.Current.Resources["Neutral"];
            switch (type)
            {
                case 1:
                    {
                        temp_imagesource = (ImageSource)Application.Current.Resources["UAV_TopView"];
                    }
                    break;
                case 2:
                    {
                        //temp_imagesource = (ImageSource)Application.Current.Resources["UAV"];
                    }
                    break;
                case 3:
                    {
                        temp_imagesource = (ImageSource)Application.Current.Resources["LAH"];
                    }
                    break;
                case 4:
                    {
                        temp_imagesource = (ImageSource)Application.Current.Resources["Hostile_Panzer"];
                    }
                    break;
                case 5:
                    {
                        temp_imagesource = (ImageSource)Application.Current.Resources["Hostile_Tank"];
                    }
                    break;
                case 8:
                    {
                        temp_imagesource = (ImageSource)Application.Current.Resources["Hostile_AAG"];
                    }
                    break;
                default:
                    {

                    }
                    break;

            }
            return temp_imagesource;
        }


        private ObservableCollection<UnitMapObjectInfo> _ObjectDisplayList = new ObservableCollection<UnitMapObjectInfo>();
        public ObservableCollection<UnitMapObjectInfo> ObjectDisplayList
        {
            get
            {
                return _ObjectDisplayList;
            }
            set
            {
                _ObjectDisplayList = value;
                OnPropertyChanged("ObjectDisplayList");
            }
        }



        //public ObservableCollection<UnitMapObjectInfo> ObjectDisplayList { get; } = new();

        // ▼▼▼ 지도 객체를 빠르게 찾기 위한 조회용 딕셔너리 추가 ▼▼▼
        //private readonly Dictionary<uint, UnitMapObjectInfo> _mapObjectDictionary = new();

        //// 객체가 추가될 때 딕셔너리에도 추가
        //public void AddMapObject(UnitMapObjectInfo mapObject)
        //{
        //    if (!_mapObjectDictionary.ContainsKey(mapObject.ID))
        //    {
        //        ObjectDisplayList.Add(mapObject);
        //        _mapObjectDictionary.Add(mapObject.ID, mapObject);
        //    }
        //}

        //// 객체가 제거될 때 딕셔너리에서도 제거
        //public void RemoveMapObject(uint id)
        //{
        //    if (_mapObjectDictionary.TryGetValue(id, out var mapObject))
        //    {
        //        ObjectDisplayList.Remove(mapObject);
        //        _mapObjectDictionary.Remove(id);
        //    }
        //}

        //// 객체를 O(1) 속도로 조회
        //public UnitMapObjectInfo GetMapObject(uint id)
        //{
        //    _mapObjectDictionary.TryGetValue(id, out var mapObject);
        //    return mapObject;
        //}



        private ObservableCollection<MapPolygon> _FocusSquareList = new ObservableCollection<MapPolygon>();
        public ObservableCollection<MapPolygon> FocusSquareList
        {
            get
            {
                return _FocusSquareList;
            }
            set
            {
                _FocusSquareList = value;
                OnPropertyChanged("FocusSquareList");
            }
        }


 

        private ObservableCollection<CustomMapPolygon> _INITMissionPolygonList = new ObservableCollection<CustomMapPolygon>();
        public ObservableCollection<CustomMapPolygon> INITMissionPolygonList
        {
            get
            {
                return _INITMissionPolygonList;
            }
            set
            {
                _INITMissionPolygonList = value;
                OnPropertyChanged("INITMissionPolygonList");
            }
        }

        private ObservableCollection<CustomMapPolygon> _FlightAreaPolygonList = new ObservableCollection<CustomMapPolygon>();
        public ObservableCollection<CustomMapPolygon> FlightAreaPolygonList
        {
            get
            {
                return _FlightAreaPolygonList;
            }
            set
            {
                _FlightAreaPolygonList = value;
                OnPropertyChanged("FlightAreaPolygonList");
            }
        }

        private ObservableCollection<CustomMapPolygon> _ProhibitedAreaPolygonList = new ObservableCollection<CustomMapPolygon>();
        public ObservableCollection<CustomMapPolygon> ProhibitedAreaPolygonList
        {
            get
            {
                return _ProhibitedAreaPolygonList;
            }
            set
            {
                _ProhibitedAreaPolygonList = value;
                OnPropertyChanged("ProhibitedAreaPolygonList");
            }
        }

   

        private ObservableCollection<CustomMapLine> _INITMissionLineList = new ObservableCollection<CustomMapLine>();
        public ObservableCollection<CustomMapLine> INITMissionLineList
        {
            get
            {
                return _INITMissionLineList;
            }
            set
            {
                _INITMissionLineList = value;
                OnPropertyChanged("INITMissionLineList");
            }
        }

        private ObservableCollection<CustomMapPolygon> _INITMissionLinePolygonList = new ObservableCollection<CustomMapPolygon>();
        public ObservableCollection<CustomMapPolygon> INITMissionLinePolygonList
        {
            get
            {
                return _INITMissionLinePolygonList;
            }
            set
            {
                _INITMissionLinePolygonList = value;
                OnPropertyChanged("INITMissionLinePolygonList");
            }
        }


    

        private ObservableCollection<CustomMapPoint> _INITMissionPointList = new ObservableCollection<CustomMapPoint>();
        public ObservableCollection<CustomMapPoint> INITMissionPointList
        {
            get
            {
                return _INITMissionPointList;
            }
            set
            {
                _INITMissionPointList = value;
                OnPropertyChanged("INITMissionPointList");
            }
        }

 


        private double _MapCursorLat = 0;
        public double MapCursorLat
        {
            get
            {
                return _MapCursorLat;
            }
            set
            {
                _MapCursorLat = value;
                OnPropertyChanged("MapCursorLat");
            }
        }

        private double _MapCursorLon = 0;
        public double MapCursorLon
        {
            get
            {
                return _MapCursorLon;
            }
            set
            {
                _MapCursorLon = value;
                OnPropertyChanged("MapCursorLon");
            }
        }
        


        private void Callback_OnINITMissionPointAdd(CustomMapPoint InputMapPoint)
        {
            //var item = new CustomMapPoint();
            //item.MissionID = OverlayID;
            //item.Latitude = Lat;
            //item.Longitude = Lon;
            //item.TagString = item.MissionID.ToString();
            INITMissionPointList.Add(InputMapPoint);
        }




        private void Callback_OnINITMissionPolygonAdd(List<CustomMapPolygon> PolygonList)
        {
         
            foreach (var polygon in PolygonList)
            {
          
                INITMissionPolygonList.Add(polygon);
            }

            // --- 지금부터는 모든 그리기가 끝난 후의 정리 단계 ---


            // 3. View(지도 컨트롤 자체)의 그리기 관련 상태 변수들을 모두 초기 상태로 되돌린다.
            var mapView = View_Unit_Map.SingletonInstance;
            if (mapView != null)
            {
                mapView._linePoints.Clear();
                mapView._previewSegments.Clear();
                mapView._previewRects.Clear();
                mapView._state = View_Unit_Map.DrawState.None;
                mapView._ghostSegment = null;
            }
        }

        private void Callback_OnINITMissionPolyLineAdd(CustomMapLine InputMapLine)
        {
            //// 전달받은 선 객체를 영구 보관할 '선' 리스트에 추가한다.
            //INITMissionLineList.Add(InputMapLine);

            //// !!! 중요 !!!
            //// 여기서는 임시 객체를 절대 지우지 않는다.
            //// 바로 뒤이어 다각형 콜백이 호출될 것이므로, 모든 정리는 그곳에서 한번에 처리한다.
            ///
               // --- 방어 코드 추가 ---
            if (InputMapLine == null)
            {
                // 1. 객체 자체가 null인 경우
                System.Diagnostics.Debug.WriteLine("오류: 추가하려는 지도 선 객체가 null입니다!");
                return;
            }
            if (InputMapLine.Points == null)
            {
                // 2. 객체의 Points 속성이 null인 경우
                System.Diagnostics.Debug.WriteLine($"오류: MissionId {InputMapLine.MissionId} 객체의 Points 속성이 null입니다!");
                return;
            }
            // --------------------

            INITMissionLineList.Add(InputMapLine);
        }






    }
}

