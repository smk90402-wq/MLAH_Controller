
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
using DevExpress.Xpf.CodeView.Margins;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Map;
using DevExpress.XtraPrinting.Native;
using Windows.Devices.Geolocation;
using Windows.Storage.Provider;
using static DevExpress.Charts.Designer.Native.BarThicknessEditViewModel;
using static DevExpress.Utils.Drawing.Helpers.NativeMethods;

namespace MLAH_LogAnalyzer
{
    public class ViewModel_Unit_Map : CommonBase
    {
        private static readonly Lazy<ViewModel_Unit_Map> _lazy =
        new Lazy<ViewModel_Unit_Map>(() => new ViewModel_Unit_Map());

        public static ViewModel_Unit_Map SingletonInstance => _lazy.Value;

        #region 생성자 & 콜백
        public ViewModel_Unit_Map()
        {
            // ✅ 디자이너에서 이벤트 구독을 막는 보호 코드를 추가합니다.
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return;
            }

            FocusSquareItems = new ObservableCollection<MapPolygon>();
            FourCornerItems = new ObservableCollection<MapPolygon>();
        }

        #endregion 생성자 & 콜백

        private ObservableCollection<MapPolyline> _evaluationTracks = new ObservableCollection<MapPolyline>();
        public ObservableCollection<MapPolyline> EvaluationTracks { get => _evaluationTracks; set { _evaluationTracks = value; OnPropertyChanged(nameof(EvaluationTracks)); } }

        private ObservableCollection<UnitMapObjectInfo> _evaluationUavPositions = new ObservableCollection<UnitMapObjectInfo>();
        public ObservableCollection<UnitMapObjectInfo> EvaluationUavPositions { get => _evaluationUavPositions; set { _evaluationUavPositions = value; OnPropertyChanged(nameof(EvaluationUavPositions)); } }

        private ObservableCollection<MapPolygon> _evaluationUavFootprints = new ObservableCollection<MapPolygon>();
        public ObservableCollection<MapPolygon> EvaluationUavFootprints { get => _evaluationUavFootprints; set { _evaluationUavFootprints = value; OnPropertyChanged(nameof(EvaluationUavFootprints)); } }

        private ObservableCollection<UnitMapObjectInfo> _evaluationTargets = new ObservableCollection<UnitMapObjectInfo>();
        public ObservableCollection<UnitMapObjectInfo> EvaluationTargets { get => _evaluationTargets; set { _evaluationTargets = value; OnPropertyChanged(nameof(EvaluationTargets)); } }

        private ObservableCollection<MapPolygon> _coveragePolygons = new ObservableCollection<MapPolygon>();
        public ObservableCollection<MapPolygon> CoveragePolygons { get => _coveragePolygons; set { _coveragePolygons = value; OnPropertyChanged(nameof(CoveragePolygons)); } }

        //표적 표시 여부 (체크박스 바인딩)
        private bool _isTargetVisible = true;
        public bool IsTargetVisible
        {
            get => _isTargetVisible;
            set { _isTargetVisible = value; OnPropertyChanged(nameof(IsTargetVisible)); }
        }

        /// <summary>
        /// [신규] 특정 시점(timestamp)의 표적 위치를 업데이트합니다.
        /// </summary>
        // 표적 아이콘 캐시 (Clear/Add 반복 방지)
        private Dictionary<uint, UnitMapObjectInfo> _cachedTargetIcons = new();
        private Dictionary<int, UnitMapObjectInfo> _cachedUavIcons = new();
        private Dictionary<int, MapPolygon> _cachedUavFootprints = new();

        public void UpdateTargetsAt(ulong timestamp, List<RealTargetData> targetDataList)
        {
            if (targetDataList == null || !targetDataList.Any()) return;

            // UI 외부: 데이터 검색
            List<Target> currentTargets = SafetyLevelCalculator.FindClosestTargetEntry(timestamp, targetDataList);
            if (currentTargets == null) return;

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                var currentIds = new HashSet<uint>(currentTargets.Select(t => t.ID));

                // 사라진 표적 제거
                var removedIds = _cachedTargetIcons.Keys.Where(id => !currentIds.Contains(id)).ToList();
                foreach (var id in removedIds)
                {
                    if (_cachedTargetIcons.TryGetValue(id, out var icon))
                    {
                        EvaluationTargets.Remove(icon);
                        _cachedTargetIcons.Remove(id);
                    }
                }

                // 표적 위치 갱신 또는 신규 생성
                foreach (var target in currentTargets)
                {
                    if (_cachedTargetIcons.TryGetValue(target.ID, out var existing))
                    {
                        existing.Location = new GeoPoint(target.Latitude, target.Longitude);
                    }
                    else
                    {
                        var dummyTarget = new UnitObjectInfo
                        {
                            ID = (int)target.ID,
                            Type = 4,
                            Status = 1,
                            LOC = new CoordinateInfo { Latitude = (float)target.Latitude, Longitude = (float)target.Longitude },
                            velocity = new Velocity { Heading = 0 }
                        };
                        var mapObj = ConvertToObjectInfo(dummyTarget);
                        mapObj.TypeString = target.Type;
                        mapObj.PlatformString = target.Subtype;
                        _cachedTargetIcons[target.ID] = mapObj;
                        EvaluationTargets.Add(mapObj);
                    }
                }
            });
        }

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
        //private MapPolygon _footprintPolygon;
        //private readonly List<MapLine> _sideLines = new List<MapLine>();

        private CancellationTokenSource _visualsUpdateCts;

        public ObservableCollection<MapPolygon> FocusSquareItems { get; } = new ObservableCollection<MapPolygon>();

        // UAV 촬영 영역의 '바닥면'을 위한 컬렉션
        public ObservableCollection<MapPolygon> FourCornerItems { get; } = new ObservableCollection<MapPolygon>();

        // UAV 촬영 영역의 '옆면(선)'을 위한 새로운 컬렉션
        public ObservableCollection<MapLine> FootprintSideLines { get; } = new ObservableCollection<MapLine>();




        public async Task BuildMapCoveragePolygonsAsync(AnalysisResult analysisResult, ScenarioData scenarioData)
        {
            if (analysisResult == null || scenarioData == null) return;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                CoveragePolygons.Clear();
                EvaluationTracks.Clear();
            });

            // 1. [백그라운드 스레드] NTS (NetTopologySuite)를 이용한 무거운 폴리곤 연산
            var geomData = await Task.Run(() =>
            {
                var planned = CoverageCalculator.CreateMissionSegmentPolygons(scenarioData.MissionDetail);
                var totalArea = CoverageCalculator.CalculateTotalCoveredAreaOptimized(scenarioData.FlightData);
                var filmed = new Dictionary<uint, NetTopologySuite.Geometries.Geometry>();

                foreach (var kvp in planned)
                {
                    if (kvp.Value != null && !kvp.Value.IsEmpty)
                    {
                        // 실제 촬영 영역 교차 계산
                        var intersection = kvp.Value.Intersection(totalArea);
                        // 맵 스크롤 렉 방지를 위한 단순화 (Tolerance 0.0001)
                        filmed[kvp.Key] = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(intersection, 0.0001);
                    }
                }
                return new { Planned = planned, Filmed = filmed };
            });

            // 2. [UI 스레드] 연산된 Geometry를 UI 객체(MapPolygon)로 변환하고 통째로 교체
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                GeoPoint? firstPointToCenterOn = null;
                var newCoveragePolygons = new List<MapPolygon>();
                var newTracks = new List<MapPolyline>();

                // [A] 폴리곤 생성 및 리스트 추가
                foreach (var kvp in geomData.Planned)
                {
                    uint segmentId = kvp.Key;

                    // 1) 계획 영역 (기본 회색 파선)
                    var planPolys = ConvertGeometryToMapPolygons(kvp.Value, ref firstPointToCenterOn,
                        Brushes.Transparent, Brushes.Transparent, new StrokeStyle(), $"Mission_{segmentId}");
                    newCoveragePolygons.AddRange(planPolys);

                    // 2) 실제 촬영 영역 (반투명 빨강 면)
                    if (geomData.Filmed.TryGetValue(segmentId, out var filmedGeom) && !filmedGeom.IsEmpty)
                    {
                        var footprintFill = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
                        footprintFill.Freeze();

                        var filmedPolys = ConvertGeometryToMapPolygons(filmedGeom, ref firstPointToCenterOn,
                            footprintFill, Brushes.Transparent, null, $"Filmed_{segmentId}");
                        newCoveragePolygons.AddRange(filmedPolys);
                    }
                }

                // [B] UAV 항적 생성
                if (analysisResult.UavInfos != null)
                {
                    var uavColors = new Dictionary<int, Brush> { { 4, Brushes.Magenta }, { 5, Brushes.Cyan }, { 6, Brushes.LimeGreen } };

                    foreach (var uavInfo in analysisResult.UavInfos.Where(u => u.UAVID >= 4 && u.UAVID <= 6))
                    {
                        if (uavInfo.Snapshots.Any())
                        {
                            var trackPolyline = new MapPolyline
                            {
                                Stroke = uavColors.TryGetValue(uavInfo.UAVID, out var color) ? color : Brushes.Magenta,
                                StrokeStyle = new StrokeStyle { Thickness = 2 },
                                IsHitTestVisible = false
                            };

                            // 항적 샘플링: 매 3번째 포인트만 취하여 렌더링 부하 감소 (5Hz→~1.7Hz)
                            var validSnapshots = uavInfo.Snapshots.Where(s => s.Position != null).ToList();
                            for (int i = 0; i < validSnapshots.Count; i++)
                            {
                                if (i % 3 == 0 || i == validSnapshots.Count - 1) // 첫/끝/매 3번째
                                    trackPolyline.Points.Add(new GeoPoint(validSnapshots[i].Position.Latitude, validSnapshots[i].Position.Longitude));
                            }

                            if (trackPolyline.Points.Any())
                            {
                                newTracks.Add(trackPolyline);
                            }
                        }
                    }
                }

                // [C] ★ 렌더링 렉을 방지하기 위한 컬렉션 일괄 교체 (단 1번의 렌더링 알림)
                CoveragePolygons = new ObservableCollection<MapPolygon>(newCoveragePolygons);
                EvaluationTracks = new ObservableCollection<MapPolyline>(newTracks);

                // [D] 맵 중앙 포커싱
                if (firstPointToCenterOn != null)
                {
                    CenterPoint = new GeoPoint(firstPointToCenterOn.GetY(), firstPointToCenterOn.GetX());
                    CurrentZoomLevel = 15;
                }

                // 초기 색상 설정
                HighlightSegment(null);
            });
        }

        private List<MapPolygon> ConvertGeometryToMapPolygons(NetTopologySuite.Geometries.Geometry geometry, ref GeoPoint? firstPointRef, Brush fill, Brush stroke, StrokeStyle strokeStyle, string tag)
        {
            var mapPolygons = new List<MapPolygon>();

            if (geometry == null || geometry.IsEmpty) return mapPolygons;

            for (int i = 0; i < geometry.NumGeometries; i++)
            {
                NetTopologySuite.Geometries.Geometry geomN = geometry.GetGeometryN(i);
                if (geomN is NetTopologySuite.Geometries.Polygon polygon)
                {
                    // 지나치게 작은 면적 무시 (최적화)
                    if (polygon.Area < 1e-8) continue;

                    // 1. 외부 링(Exterior Ring) 변환
                    MapPolygon exteriorPolygon = ConvertNtsRingToMapPolygon(polygon.ExteriorRing, ref firstPointRef, fill, stroke, strokeStyle, tag);
                    if (exteriorPolygon != null)
                    {
                        mapPolygons.Add(exteriorPolygon);
                    }

                    // 2. 내부 링(Interior Rings = 구멍) 변환
                    for (int j = 0; j < polygon.NumInteriorRings; j++)
                    {
                        var hole = polygon.GetInteriorRingN(j);
                        if (hole.Length < 0.001) continue;

                        var holeStyle = new StrokeStyle { Thickness = 1, DashArray = new DoubleCollection { 4, 2 } };

                        MapPolygon holePolygon = ConvertNtsRingToMapPolygon(hole, ref firstPointRef, Brushes.Transparent, Brushes.Gray, holeStyle, tag);
                        if (holePolygon != null)
                        {
                            mapPolygons.Add(holePolygon);
                        }
                    }
                }
            }

            return mapPolygons;
        }

        #region ★ [성능 최적화] Frozen Brush 캐시 - HighlightSegment에서 매번 new 하지 않도록

        private static Brush CreateFrozenBrush(string hex, double opacity)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)) { Opacity = opacity };
            b.Freeze(); // Freeze하면 크로스 스레드 접근 가능 + WPF 렌더링 성능 향상
            return b;
        }

        private static readonly List<(Brush PlanFill, Brush FilmedFill)> _cachedColorPalette = new List<(Brush, Brush)>
        {
            (CreateFrozenBrush("#FF5B9BD5", 0.4), CreateFrozenBrush("#FF5B9BD5", 0.8)),
            (CreateFrozenBrush("#FFC00000", 0.4), CreateFrozenBrush("#FFC00000", 0.8)),
            (CreateFrozenBrush("#FF92D050", 0.4), CreateFrozenBrush("#FF92D050", 0.8)),
            (CreateFrozenBrush("#FFFF8C00", 0.4), CreateFrozenBrush("#FFFF8C00", 0.8)),
            (CreateFrozenBrush("#FF9370DB", 0.4), CreateFrozenBrush("#FF9370DB", 0.8)),
        };

        private static readonly Brush _frozenGrayPlanBrush = CreateFrozenBrush("#FF808080", 0.4);
        private static readonly Brush _frozenGrayFilmedBrush = CreateFrozenBrush("#FF808080", 0.8);
        private static readonly StrokeStyle _cachedDashStroke = new StrokeStyle { Thickness = 1, DashArray = new DoubleCollection { 4, 2 } };
        private static readonly StrokeStyle _cachedSolidStroke = new StrokeStyle { Thickness = 1 };

        private static readonly Brush _frozenFootprintFill;

        static ViewModel_Unit_Map()
        {
            var fpBrush = new SolidColorBrush(Color.FromArgb(102, 255, 255, 0));
            fpBrush.Freeze();
            _frozenFootprintFill = fpBrush;
        }

        #endregion

        // [신규 2] 그리드를 클릭할 때 "색상만" 순식간에 바꿔주는 함수
        public void HighlightSegment(uint? selectedSegmentId)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                foreach (var poly in CoveragePolygons)
                {
                    if (poly.Tag == null) continue;

                    string[] tags = poly.Tag.ToString().Split('_');
                    if (tags.Length < 2) continue;

                    string type = tags[0]; // "Mission" or "Filmed"
                    uint polySegId = uint.Parse(tags[1]);

                    bool isSelected = (!selectedSegmentId.HasValue || selectedSegmentId.Value == polySegId);

                    if (isSelected)
                    {
                        int colorIdx = (int)(polySegId % _cachedColorPalette.Count);
                        poly.Fill = type == "Mission" ? _cachedColorPalette[colorIdx].PlanFill : _cachedColorPalette[colorIdx].FilmedFill;
                        poly.Stroke = type == "Mission" ? Brushes.White : Brushes.Transparent;
                        poly.StrokeStyle = type == "Mission" ? _cachedDashStroke : _cachedSolidStroke;
                        poly.ZIndex = 100;
                    }
                    else
                    {
                        poly.Fill = type == "Mission" ? _frozenGrayPlanBrush : _frozenGrayFilmedBrush;
                        poly.Stroke = Brushes.Transparent;
                        poly.ZIndex = 100;
                    }
                }
            });
        }

        /// <summary>
        /// [신규] 트랙바 스냅용: 특정 시점의 UAV 위치와 풋프린트를 표시
        /// [2026-03-24] 변경 전: 매번 Clear() + Add() → 맵 레이어 전체 삭제/재생성으로 깜빡임 + 성능 저하
        /// [2026-03-24] 변경 후: UpdateSpecificUavSnapshot으로 위임하여 기존 객체 위치만 업데이트 (Clear/Add 없음)
        /// </summary>
        public void ShowUavSnapshot(UavSnapshot snapshot, int uavId)
        {
            // 기존 코드:
            // Application.Current.Dispatcher.BeginInvoke(() =>
            // {
            //     EvaluationUavPositions.Clear();
            //     EvaluationUavFootprints.Clear();
            //     ... new UnitObjectInfo + Add ...
            //     ... new MapPolygon + Add ...
            // });

            // 객체 재사용 방식의 UpdateSpecificUavSnapshot으로 통합
            UpdateSpecificUavSnapshot(snapshot, uavId);
        }

        /// <summary>
        /// [신규] 시나리오 선택 변경 시 이전 항적/스냅샷을 모두 지우기
        /// </summary>
        public void ClearEvaluationData()
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                EvaluationTracks.Clear();
                EvaluationUavPositions.Clear();
                EvaluationUavFootprints.Clear();
                EvaluationTargets.Clear();
                _cachedTargetIcons.Clear();
                _cachedUavIcons.Clear();
                _cachedUavFootprints.Clear();
            });
        }


        private MapPolygon ConvertNtsRingToMapPolygon(NetTopologySuite.Geometries.LineString ring, ref GeoPoint? firstPointRef,Brush fill, Brush stroke, StrokeStyle strokeStyle, string tag) 
        {
            if (ring == null || ring.IsEmpty || ring.Coordinates.Length < 3) return null;

            var mapPolygon = new MapPolygon();

            bool isFirstPointOfRing = true;
            foreach (var coord in ring.Coordinates)
            {
                var geoPoint = new GeoPoint(coord.Y, coord.X); // Lat, Lon
                mapPolygon.Points.Add(geoPoint);

                if (firstPointRef == null && isFirstPointOfRing)
                {
                    firstPointRef = new GeoPoint(coord.Y, coord.X);
                    isFirstPointOfRing = false;
                }
            }

            // --- [!!!] 스타일 직접 설정 (isMissing 분기 제거) ---
            mapPolygon.Fill = fill;
            mapPolygon.Stroke = stroke;
            mapPolygon.StrokeStyle = strokeStyle;

            mapPolygon.Tag = tag;
            

            return mapPolygon;
        }

        private double _currentZoomLevel = 11.0;
        public double CurrentZoomLevel
        {
            get => _currentZoomLevel;
            set
            {
                if (Math.Abs(_currentZoomLevel - value) < 0.01) return;
                _currentZoomLevel = value;
                // 줌 애니메이션 중 프레임마다 PropertyChanged가 발생하면 성능 저하.
                // 정수 단위 변화 시에만 알림하여 연쇄 바인딩 최소화.
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
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                FocusSquareItems.Clear();
                FourCornerItems.Clear();
                FootprintSideLines.Clear();
                // [확인 후 삭제] 미사용 필드 참조 - 선언부 주석처리됨
                //_focusPolygon = null;
                //_footprintPolygon = null;
                //_sideLines.Clear();
            });
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




        // [2026-03-24] UAV별 풋프린트 Brush를 Frozen 캐시로 미리 생성 (신규 생성 시 매번 new 방지)
        private static readonly Dictionary<int, Brush> _uavStrokeCache = new Dictionary<int, Brush>
        {
            { 4, Brushes.Magenta }, { 5, Brushes.Cyan }, { 6, Brushes.LimeGreen }
        };
        private static readonly StrokeStyle _cachedThickStroke = new StrokeStyle { Thickness = 2 };

        /// <summary>
        /// [핵심] 특정 UAV(ID)의 위치와 풋프린트만 개별적으로 갱신합니다.
        /// (다른 UAV의 아이콘은 건드리지 않습니다)
        /// [2026-03-24] Brush Freeze 적용 - 신규 생성 시 매번 new SolidColorBrush 대신 캐시된 Frozen Brush 사용
        /// </summary>
        public void UpdateSpecificUavSnapshot(UavSnapshot snapshot, int uavId)
        {
            if (snapshot == null) return;

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // 1. 아이콘 업데이트 (Dictionary 캐시로 O(1) 검색)
                _cachedUavIcons.TryGetValue(uavId, out var existingIcon);

                if (snapshot.Position != null)
                {
                    if (existingIcon != null)
                    {
                        existingIcon.Location = new GeoPoint(snapshot.Position.Latitude, snapshot.Position.Longitude);
                    }
                    else
                    {
                        var dummyInfo = new UnitObjectInfo
                        {
                            ID = uavId,
                            Type = 1,
                            Status = 1,
                            LOC = new CoordinateInfo
                            {
                                Latitude = (float)snapshot.Position.Latitude,
                                Longitude = (float)snapshot.Position.Longitude
                            },
                            velocity = new Velocity { Heading = 0 }
                        };
                        var mapSymbol = ConvertToObjectInfo(dummyInfo);
                        _cachedUavIcons[uavId] = mapSymbol;
                        EvaluationUavPositions.Add(mapSymbol);
                    }
                }
                else if (existingIcon != null)
                {
                    EvaluationUavPositions.Remove(existingIcon);
                    _cachedUavIcons.Remove(uavId);
                }

                // 2. 풋프린트 업데이트 (Dictionary 캐시로 O(1) 검색)
                _cachedUavFootprints.TryGetValue(uavId, out var existingFootprint);

                if (snapshot.Footprint?.CameraTopLeft != null)
                {
                    var newPoints = new List<GeoPoint>
                    {
                        new GeoPoint(snapshot.Footprint.CameraTopLeft.Latitude, snapshot.Footprint.CameraTopLeft.Longitude),
                        new GeoPoint(snapshot.Footprint.CameraTopRight.Latitude, snapshot.Footprint.CameraTopRight.Longitude),
                        new GeoPoint(snapshot.Footprint.CameraBottomRight.Latitude, snapshot.Footprint.CameraBottomRight.Longitude),
                        new GeoPoint(snapshot.Footprint.CameraBottomLeft.Latitude, snapshot.Footprint.CameraBottomLeft.Longitude)
                    };

                    if (existingFootprint != null)
                    {
                        // 4개 포인트 고정이므로 인덱스 직접 교체 (Clear+Add 대신)
                        if (existingFootprint.Points.Count == 4)
                        {
                            for (int i = 0; i < 4; i++)
                                existingFootprint.Points[i] = newPoints[i];
                        }
                        else
                        {
                            existingFootprint.Points.Clear();
                            foreach (var pt in newPoints) existingFootprint.Points.Add(pt);
                        }
                    }
                    else
                    {
                        var footprintPolygon = new MapPolygon
                        {
                            Fill = _frozenFootprintFill,
                            Stroke = _uavStrokeCache.TryGetValue(uavId, out var sc) ? sc : Brushes.Yellow,
                            StrokeStyle = _cachedThickStroke,
                            Tag = uavId
                        };

                        foreach (var pt in newPoints) footprintPolygon.Points.Add(pt);
                        _cachedUavFootprints[uavId] = footprintPolygon;
                        EvaluationUavFootprints.Add(footprintPolygon);
                    }
                }
                else if (existingFootprint != null)
                {
                    EvaluationUavFootprints.Remove(existingFootprint);
                    _cachedUavFootprints.Remove(uavId);
                }
            });
        }

    }
}

