
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
    public struct MissionDisSegmentData
    {
        public List<GeoPoint> Points; // CoordPointCollection 대신 List 사용 (스레드 안전)
        public bool IsPaused;
    }
    public class ViewModel_Unit_Map_Distribution : CommonBase
    {
        private static readonly Lazy<ViewModel_Unit_Map_Distribution> _lazy =
        new Lazy<ViewModel_Unit_Map_Distribution>(() => new ViewModel_Unit_Map_Distribution());

        public static ViewModel_Unit_Map_Distribution SingletonInstance => _lazy.Value;

        #region 생성자 & 콜백
        public ViewModel_Unit_Map_Distribution()
        {
            // 생성자에서 브러시/스타일 초기화 및 Freeze() 호출
            _runTrackBrush = new SolidColorBrush(Colors.Blue) { Opacity = 0.8 };
            _runTrackBrush.Freeze(); // 변경 불가능하게 만들어 스레드 안전성 확보

            _pauseTrackBrush = new SolidColorBrush(Colors.Gray) { Opacity = 0.6 };
            _pauseTrackBrush.Freeze();

            _runStrokeStyle = new StrokeStyle { Thickness = 3 };
            // StrokeStyle은 Freeze가 안될 수도 있습니다(DevExpress 버전에 따라). 
            // 하지만 아래 로직 수정으로 UI 스레드에서만 쓰게 하면 문제 없습니다.

            _pauseStrokeStyle = new StrokeStyle { Thickness = 3, DashArray = new DoubleCollection { 2, 2 } };
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

        /// <summary>
                /// 임무 수행/중지 항적 (파란 실선 / 회색 점선)
                /// </summary>
        // AircraftID별 FlightData 인덱스 (타임스탬프 오름차순 정렬, BinarySearch용)
        private Dictionary<uint, List<FlightData>> _flightDataIndex;

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

        private FlightData FindFlightDataAtOrBefore(uint aircraftId, ulong timestamp)
        {
            if (_flightDataIndex == null || !_flightDataIndex.TryGetValue(aircraftId, out var list) || list.Count == 0)
                return null;

            int lo = 0, hi = list.Count - 1;
            int bestIdx = -1;
            while (lo <= hi)
            {
                int mid = lo + (hi - lo) / 2;
                if (list[mid].Timestamp <= timestamp) { bestIdx = mid; lo = mid + 1; }
                else { hi = mid - 1; }
            }
            return bestIdx >= 0 ? list[bestIdx] : null;
        }

        private ObservableCollection<MapPolyline> _missionTracks = new ObservableCollection<MapPolyline>();
        public ObservableCollection<MapPolyline> MissionTracks
        {
            get => _missionTracks;
            set { _missionTracks = value; OnPropertyChanged(nameof(MissionTracks)); }
        }

        /// <summary>
        /// 평가용: 트랙바가 선택한 시점의 UAV 위치 (심볼)
        /// (XAML의 EvaluationUavPositionLayer에 바인딩됨)
        /// </summary>
        public ObservableCollection<UnitMapObjectInfo> EvaluationUavPositions { get; set; } = new ObservableCollection<UnitMapObjectInfo>();

        // --- 브러시 및 스타일 정의 ---
        private readonly SolidColorBrush _runTrackBrush = new SolidColorBrush(Colors.Blue) { Opacity = 0.8 };
        private readonly SolidColorBrush _pauseTrackBrush = new SolidColorBrush(Colors.Gray) { Opacity = 0.6 };

        // [!!!] 점선 스타일 (DashArray) [!!!]
        private readonly StrokeStyle _pauseStrokeStyle = new StrokeStyle { Thickness = 3, DashArray = new DoubleCollection { 2, 2 } };
        private readonly StrokeStyle _runStrokeStyle = new StrokeStyle { Thickness = 3 };

   

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

        public void ClearMissionVisuals()
        {
            _flightDataIndex = null; // 시나리오 변경 시 인덱스 재빌드
            _cachedAircraftIcons.Clear();
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                MissionTracks.Clear();
                EvaluationUavPositions.Clear();
            });
        }

        public async void UpdateMissionVisuals(ScenarioData scenarioData, MissionDistributionResult missionResult)
        {
            if (scenarioData == null) { ClearMissionVisuals(); return; }

            // 1. 백그라운드 스레드: 데이터 가공 (List<GeoPoint> 까지만 사용)
            var resultData = await Task.Run(() =>
            {
                var calculatedSegments = new List<MissionDisSegmentData>();
                GeoPoint? firstPointToCenterOn = null;
                var aircraftToDraw = new uint[] { 4, 5, 6 };
                var pauseRangesDict = new Dictionary<uint, List<PauseTimeRange>>();

                if (missionResult != null && missionResult.MissionPauseTimestamp != null)
                {
                    if (missionResult.MissionPauseTimestamp.TryGetValue("UAV4", out var r4)) pauseRangesDict[4] = r4;
                    if (missionResult.MissionPauseTimestamp.TryGetValue("UAV5", out var r5)) pauseRangesDict[5] = r5;
                    if (missionResult.MissionPauseTimestamp.TryGetValue("UAV6", out var r6)) pauseRangesDict[6] = r6;
                }

                var allFlightData = scenarioData.FlightData
                    .Where(fd => aircraftToDraw.Contains(fd.AircraftID) && fd.FlightDataLog != null)
                    .OrderBy(fd => fd.Timestamp)
                    .GroupBy(fd => fd.AircraftID);

                foreach (var aircraftGroup in allFlightData)
                {
                    uint uavId = aircraftGroup.Key;
                    pauseRangesDict.TryGetValue(uavId, out var pauseRanges);
                    var flightPathPoints = aircraftGroup.ToList();
                    if (flightPathPoints.Count == 0) continue;

                    if (firstPointToCenterOn == null)
                    {
                        firstPointToCenterOn = new GeoPoint(flightPathPoints[0].FlightDataLog.Latitude, flightPathPoints[0].FlightDataLog.Longitude);
                    }

                    var currentPoints = new List<GeoPoint>();
                    var firstPt = flightPathPoints[0];
                    currentPoints.Add(new GeoPoint(firstPt.FlightDataLog.Latitude, firstPt.FlightDataLog.Longitude));
                    bool? currentIsPaused = IsPaused(firstPt.Timestamp, pauseRanges);

                    for (int i = 0; i < flightPathPoints.Count - 1; i++)
                    {
                        var p1 = flightPathPoints[i];
                        var p2 = flightPathPoints[i + 1];
                        bool nextIsPaused = IsPaused(p1.Timestamp, pauseRanges);

                        if (nextIsPaused != currentIsPaused)
                        {
                            if (currentPoints.Count > 1)
                            {
                                calculatedSegments.Add(new MissionDisSegmentData { Points = new List<GeoPoint>(currentPoints), IsPaused = currentIsPaused.Value });
                            }
                            if (currentPoints.Count > 0)
                            {
                                var lastPoint = currentPoints.Last();
                                currentPoints.Clear();
                                currentPoints.Add(lastPoint);
                            }
                            currentIsPaused = nextIsPaused;
                        }
                        currentPoints.Add(new GeoPoint(p2.FlightDataLog.Latitude, p2.FlightDataLog.Longitude));
                    }

                    if (currentPoints.Count > 1 && currentIsPaused.HasValue)
                    {
                        calculatedSegments.Add(new MissionDisSegmentData { Points = new List<GeoPoint>(currentPoints), IsPaused = currentIsPaused.Value });
                    }
                }
                return new { Segments = calculatedSegments, Center = firstPointToCenterOn };
            });

            // 2. UI 스레드: CreatePolyline 호출 및 컬렉션 교체
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                var newTracks = new List<MapPolyline>();

                foreach (var segmentData in resultData.Segments)
                {
                    var coordCollection = new CoordPointCollection();
                    // 항적 샘플링: 매 3번째 포인트만 취하여 렌더링 부하 감소
                    var pts = segmentData.Points;
                    if (pts.Count <= 6)
                    {
                        foreach (var pt in pts) coordCollection.Add(pt);
                    }
                    else
                    {
                        for (int i = 0; i < pts.Count; i++)
                        {
                            if (i % 3 == 0 || i == pts.Count - 1)
                                coordCollection.Add(pts[i]);
                        }
                    }

                    // 여기서 생성해야 에러가 안 납니다!
                    newTracks.Add(CreatePolyline(coordCollection, segmentData.IsPaused));
                }

                MissionTracks = new ObservableCollection<MapPolyline>(newTracks);

                if (resultData.Center != null)
                {
                    CenterPoint = new GeoPoint(resultData.Center.GetY(), resultData.Center.GetX());
                    CurrentZoomLevel = 15;
                }
            });
        }

        private bool IsPaused(ulong timestamp, List<PauseTimeRange> ranges)
        {
            if (ranges == null) return false;
            foreach (var range in ranges)
            {
                if (timestamp >= range.Start && timestamp <= range.End) return true;
            }
            return false;
        }

        private MapPolyline CreatePolyline(CoordPointCollection points, bool isPaused)
        {
            return new MapPolyline
            {
                Points = points,
                Stroke = isPaused ? _pauseTrackBrush : _runTrackBrush,
                StrokeStyle = isPaused ? _pauseStrokeStyle : _runStrokeStyle
            };
        }


        /// <summary>
        /// 트랙바 이동 시 4개 아이콘 위치 업데이트
        /// </summary>
        public void ShowAircraftPositionsAt(ulong timestamp, ScenarioData scenarioData)
        {
            if (scenarioData == null) return;

            // 인덱스가 없으면 최초 1회 빌드
            if (_flightDataIndex == null)
                BuildFlightDataIndex(scenarioData);

            // UI 스레드 외부: BinarySearch로 데이터 검색 O(logN)
            var aircraftIdsToShow = new uint[] { 4, 5, 6 };
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
                            var dummyInput = new UnitObjectInfo
                            {
                                ID = (int)id,
                                Type = 1,
                                Status = 1,
                                LOC = new CoordinateInfo { Latitude = loc.Latitude, Longitude = loc.Longitude },
                                velocity = new Velocity { Heading = 0 }
                            };
                            var icon = ConvertToObjectInfo(dummyInput);
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

