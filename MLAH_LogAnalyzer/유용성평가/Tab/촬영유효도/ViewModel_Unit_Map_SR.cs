
using DevExpress.Map;
using DevExpress.Map.Kml.Model;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.CodeView.Margins;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Map;
using DevExpress.XtraPrinting.Native;
using DevExpress.XtraSpreadsheet.Model;
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
using Windows.Devices.Geolocation;
using Windows.Storage.Provider;
using static DevExpress.Charts.Designer.Native.BarThicknessEditViewModel;
using static DevExpress.Utils.Drawing.Helpers.NativeMethods;

namespace MLAH_LogAnalyzer
{
    public class ViewModel_Unit_Map_SR : CommonBase
    {
        private static readonly Lazy<ViewModel_Unit_Map_SR> _lazy =
        new Lazy<ViewModel_Unit_Map_SR>(() => new ViewModel_Unit_Map_SR());

        public static ViewModel_Unit_Map_SR SingletonInstance => _lazy.Value;

        // [2026-03-24] 변경 전: _validFootprintFill = 초록(0,255,0), _highQualityRegionFill = 초록(0,255,0), _trackBrush = Blue Opacity 0.8
        // [2026-03-24] 변경 후: 유효영역 초록→파란으로 가시성 개선, 항적 Blue→Black + 투명도 낮춤(0.4)으로 배경 간섭 감소
        private readonly SolidColorBrush _validFootprintFill;   // 파란 (유효) — 변경: 초록→파란
        private readonly SolidColorBrush _invalidFootprintFill; // 빨강 (무효)
        private readonly SolidColorBrush _lowQualityRegionFill; // 빨강 (실패 지역)
        private readonly SolidColorBrush _missionAreaFill;      // [추가] 임무 영역 채우기 색상
        private readonly SolidColorBrush _trackBrush;           // 항적 — 변경: Blue→Black, 투명도 낮춤
        private readonly StrokeStyle _solidStyle;
        private readonly SolidColorBrush _missionLineBrush;
        private readonly SolidColorBrush _highQualityRegionFill; // 변경: 초록→파란

        #region 생성자 & 콜백
        public ViewModel_Unit_Map_SR()
        {
            // ✅ 디자이너에서 이벤트 구독을 막는 보호 코드를 추가합니다.
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return;
            }

            FocusSquareItems = new ObservableCollection<MapPolygon>();
            FourCornerItems = new ObservableCollection<MapPolygon>();

            // [2026-03-24] 변경 전: _validFootprintFill = ARGB(100, 0, 255, 0) (초록)
            // [2026-03-24] 변경 후: ARGB(100, 30, 120, 255) (파란) — 빨간 영역과 대비 향상
            _validFootprintFill = new SolidColorBrush(Color.FromArgb(100, 30, 120, 255)); _validFootprintFill.Freeze();
            _invalidFootprintFill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0)); _invalidFootprintFill.Freeze();
            _lowQualityRegionFill = new SolidColorBrush(Color.FromArgb(80, 255, 0, 0)); _lowQualityRegionFill.Freeze();
            _missionAreaFill = new SolidColorBrush(Color.FromArgb(50, 255, 255, 0)); _missionAreaFill.Freeze();
            // [2026-03-24] 변경 전: Colors.Blue, Opacity = 0.8 → 변경 후: Colors.Black, Opacity = 0.4 — 항적이 촬영영역을 가리지 않도록 투명도 낮춤
            _trackBrush = new SolidColorBrush(Colors.Black) { Opacity = 0.4 }; _trackBrush.Freeze();
            _solidStyle = new StrokeStyle { Thickness = 2 };
            _missionLineBrush = new SolidColorBrush(Colors.Yellow) { Opacity = 0.8 }; _missionLineBrush.Freeze();
            // [2026-03-24] 변경 전: ARGB(80, 0, 255, 0) (초록) → 변경 후: ARGB(80, 30, 120, 255) (파란) — 빨간 영역과 대비 향상
            _highQualityRegionFill = new SolidColorBrush(Color.FromArgb(80, 30, 120, 255)); _highQualityRegionFill.Freeze();
        }

        #endregion 생성자 & 콜백

  
        // [확인 후 삭제] 미사용 필드 - StartSelectedObjectVisualsLoop가 빈 메서드라 실제 할당 없음
        //private MapPolygon? _focusPolygon;
        //private MapPolygon _footprintPolygon;
        //private readonly List<MapLine> _sideLines = new List<MapLine>();

        //private CancellationTokenSource _focusUpdateCts;
        private CancellationTokenSource _visualsUpdateCts;

        public ObservableCollection<MapPolygon> FocusSquareItems { get; } = new ObservableCollection<MapPolygon>();

        // UAV 촬영 영역의 '바닥면'을 위한 컬렉션
        public ObservableCollection<MapPolygon> FourCornerItems { get; } = new ObservableCollection<MapPolygon>();

        // UAV 촬영 영역의 '옆면(선)'을 위한 새로운 컬렉션
        public ObservableCollection<MapLine> FootprintSideLines { get; } = new ObservableCollection<MapLine>();


        private ObservableCollection<MapPolyline> _missionTracks = new ObservableCollection<MapPolyline>();
        public ObservableCollection<MapPolyline> MissionTracks { get => _missionTracks; set { _missionTracks = value; OnPropertyChanged(nameof(MissionTracks)); } }

        private ObservableCollection<MapPolygon> _lowQualityRegions = new ObservableCollection<MapPolygon>();
        public ObservableCollection<MapPolygon> LowQualityRegions { get => _lowQualityRegions; set { _lowQualityRegions = value; OnPropertyChanged(nameof(LowQualityRegions)); } }

        private ObservableCollection<MapPolygon> _highQualityRegions = new ObservableCollection<MapPolygon>();
        public ObservableCollection<MapPolygon> HighQualityRegions { get => _highQualityRegions; set { _highQualityRegions = value; OnPropertyChanged(nameof(HighQualityRegions)); } }

        private ObservableCollection<MapPolygon> _missionAreas = new ObservableCollection<MapPolygon>();
        public ObservableCollection<MapPolygon> MissionAreas { get => _missionAreas; set { _missionAreas = value; OnPropertyChanged(nameof(MissionAreas)); } }

        private ObservableCollection<MapPolyline> _missionDetailLines = new ObservableCollection<MapPolyline>();
        public ObservableCollection<MapPolyline> MissionDetailLines { get => _missionDetailLines; set { _missionDetailLines = value; OnPropertyChanged(nameof(MissionDetailLines)); } }

        //public ObservableCollection<MapPolyline> MissionDetailLines { get; set; } = new ObservableCollection<MapPolyline>();

        // AircraftID별 FlightData 인덱스 (타임스탬프 오름차순 정렬, BinarySearch용)
        private Dictionary<uint, List<FlightData>> _flightDataIndex;
        // UAV 아이콘/풋프린트 캐시 (매 프레임 Remove/Add 방지)
        private Dictionary<int, UnitMapObjectInfo> _cachedUavIcons = new();
        private Dictionary<int, MapPolygon> _cachedUavFootprints = new();

        public void BuildFlightDataIndex(ScenarioData scenarioData)
        {
            _flightDataIndex = new Dictionary<uint, List<FlightData>>();
            _cachedUavIcons.Clear();
            _cachedUavFootprints.Clear();
            if (scenarioData?.FlightData == null) return;

            foreach (var group in scenarioData.FlightData
                .Where(fd => fd.FlightDataLog != null && fd.Timestamp > 0)
                .GroupBy(fd => fd.AircraftID))
            {
                _flightDataIndex[group.Key] = group.OrderBy(fd => fd.Timestamp).ToList();
            }
        }

        private FlightData FindFlightDataExact(uint aircraftId, ulong timestamp)
        {
            if (_flightDataIndex == null || !_flightDataIndex.TryGetValue(aircraftId, out var list) || list.Count == 0)
                return null;

            int lo = 0, hi = list.Count - 1;
            while (lo <= hi)
            {
                int mid = lo + (hi - lo) / 2;
                if (list[mid].Timestamp == timestamp) return list[mid];
                else if (list[mid].Timestamp < timestamp) lo = mid + 1;
                else hi = mid - 1;
            }
            return null;
        }

        // UAV별 개별 컬렉션 (트랙바가 3개이므로 각각 독립적으로 움직임)
        public ObservableCollection<UnitMapObjectInfo> UavPositions { get; set; } = new ObservableCollection<UnitMapObjectInfo>();
        public ObservableCollection<MapPolygon> UavFootprints { get; set; } = new ObservableCollection<MapPolygon>();

        //표적 아이콘 컬렉션
        public ObservableCollection<UnitMapObjectInfo> EvaluationTargets { get; set; } = new ObservableCollection<UnitMapObjectInfo>();
        private Dictionary<uint, UnitMapObjectInfo> _cachedTargetIcons = new();

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
        public void UpdateTargetsAt(ulong timestamp, List<RealTargetData> targetDataList)
        {
            // 데이터가 없으면 리턴
            if (targetDataList == null || !targetDataList.Any()) return;

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

        // Backing Field (초기값 설정)
        private GeoPoint _centerPoint = new GeoPoint(38.12, 127.31);

        public GeoPoint CenterPoint
        {
            get => _centerPoint;
            set
            {
                // 값이 변경되었을 때만 알림 (선택 사항이지만 성능상 권장)
                if (_centerPoint != value)
                {
                    _centerPoint = value;
                    OnPropertyChanged(nameof(CenterPoint)); // UI에 변경 알림
                }
            }
        }

        // Backing Field (초기값 설정)
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

        public void ClearMissionVisuals()
        {
            _cachedTargetIcons.Clear();
            _cachedUavIcons.Clear();
            _cachedUavFootprints.Clear();
            _flightDataIndex = null; // 시나리오 변경 시 인덱스 재빌드
            Application.Current.Dispatcher.BeginInvoke(() => {
                MissionTracks.Clear();
                LowQualityRegions.Clear();
                HighQualityRegions.Clear();
                UavPositions.Clear();
                UavFootprints.Clear();
                MissionAreas.Clear();
                EvaluationTargets.Clear();
                //MissionDetailLines.Clear();
            });
        }




        // 1. [초기화] 항적 & 저품질 영역(실패 지역) 그리기
        public void UpdateMissionVisuals(ScenarioData scenario, SpatialResolutionResult result)
        {
            if (scenario == null) { ClearMissionVisuals(); return; }

            Task.Run(() =>
            {
                var tracks = new List<List<GeoPoint>>();
                var failRegions = new List<List<GeoPoint>>();
                var successRegions = new List<List<GeoPoint>>();
                var missionAreaPolygons = new List<List<GeoPoint>>();
                var lineData = new List<List<GeoPoint>>();

                bool isCenterFound = false;
                GeoPoint tempCenter = new GeoPoint(38.12, 127.31);

                // A. 항적 추출
                foreach (var group in scenario.FlightData.Where(f => f.AircraftID >= 4 && f.AircraftID <= 6 && f.FlightDataLog != null).GroupBy(f => f.AircraftID))
                {
                    var allPts = group.OrderBy(f => f.Timestamp).Select(f => new GeoPoint(f.FlightDataLog.Latitude, f.FlightDataLog.Longitude)).ToList();
                    if (allPts.Count > 1)
                    {
                        if (!isCenterFound)
                        {
                            tempCenter = allPts[0];
                            isCenterFound = true;
                        }
                        // 항적 샘플링: 매 3번째 포인트만 취하여 렌더링 부하 감소
                        var sampled = new List<GeoPoint>();
                        for (int i = 0; i < allPts.Count; i++)
                        {
                            if (i % 3 == 0 || i == allPts.Count - 1)
                                sampled.Add(allPts[i]);
                        }
                        tracks.Add(sampled);
                    }
                }

                // B. 저품질 영역 (LowQualityRegions)
                // [고품질/저품질 구역 시각화] 이 블록은 항상 활성 상태로 유지.
                // SRCalculator.cs에서 데이터를 채워주면 자동으로 지도에 표시됨.
                // ON/OFF는 SRCalculator.cs의 "ON/OFF 스위치" 주석 참고.
                if (result != null && result.LowQualityRegions != null)
                {
                    foreach (var region in result.LowQualityRegions.Values)
                    {
                        var pts = region.Select(c => new GeoPoint(c.Latitude, c.Longitude)).ToList();
                        if (pts.Count >= 3) failRegions.Add(pts);
                    }
                }

                // C. 고품질 영역 (HighQualityRegions)
                // [고품질/저품질 구역 시각화] 위 B 블록과 동일 — ON/OFF는 SRCalculator.cs에서 제어.
                if (result != null && result.HighQualityRegions != null)
                {
                    foreach (var region in result.HighQualityRegions.Values)
                    {
                        var pts = region.Select(c => new GeoPoint(c.Latitude, c.Longitude)).ToList();
                        if (pts.Count >= 3) successRegions.Add(pts);
                    }
                }

                // D. 임무 영역 데이터 추출
                if (scenario.MissionDetail != null)
                {
                    foreach (var m in scenario.MissionDetail)
                    {
                        // 1) AreaList
                        if (m.AreaList != null)
                        {
                            foreach (var a in m.AreaList)
                            {
                                var pts = a.CoordinateList.Select(c => new GeoPoint(c.Latitude, c.Longitude)).ToList();
                                if (pts.Any()) missionAreaPolygons.Add(pts);
                            }
                        }

                        // 2) LineList
                        if (m.LineList != null)
                        {
                            foreach (var l in m.LineList)
                            {
                                if (l.CoordinateList == null || l.CoordinateList.Count < 2) continue;

                                var linePoints = l.CoordinateList.Select(c => new GeoPoint(c.Latitude, c.Longitude)).ToList();
                                double halfWidth = l.Width / 2.0;

                                var segmentPolygons = GeneratePolygonsFromLine(linePoints, halfWidth);
                                missionAreaPolygons.AddRange(segmentPolygons);
                            }
                        }
                    }
                }

                
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    // 1. 임시 리스트에 MapPolyline/MapPolygon 생성
                    var newTracks = new List<MapPolyline>();
                    foreach (var p in tracks) newTracks.Add(CreatePolyline(p, _trackBrush));

                    DataTemplate titleTemplate = XamlHelper.GetTemplate("<Grid> <TextBlock Text=\"{Binding Path=Text}\" Foreground=\"Black\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"/> </Grid>");

                    var newFailRegions = new List<MapPolygon>();
                    foreach (var p in failRegions) newFailRegions.Add(CreatePolygon(p, _lowQualityRegionFill, Brushes.Red, new ShapeTitleOptions() { Pattern = "", VisibilityMode = VisibilityMode.Visible, Template = titleTemplate }));

                    var newSuccessRegions = new List<MapPolygon>();
                    // [2026-03-24] 변경 전: Brushes.LimeGreen (초록 테두리) → 변경 후: Brushes.DodgerBlue (파란 테두리) — 유효영역 색상 통일
                    foreach (var p in successRegions) newSuccessRegions.Add(CreatePolygon(p, _highQualityRegionFill, Brushes.DodgerBlue, new ShapeTitleOptions() { Pattern = "", VisibilityMode = VisibilityMode.Visible, Template = titleTemplate }));

                    var newMissionAreas = new List<MapPolygon>();
                    foreach (var p in missionAreaPolygons) newMissionAreas.Add(CreatePolygon(p, _missionAreaFill, Brushes.Yellow));

                    var newLineData = new List<MapPolyline>();
                    foreach (var pts in lineData) newLineData.Add(CreatePolyline(pts, _missionLineBrush));

                    // 2. 기존 컬렉션을 통째로 교체 (렌더링 폭발 방지, 탭 전환 즉시 완료)
                    MissionTracks = new ObservableCollection<MapPolyline>(newTracks);
                    LowQualityRegions = new ObservableCollection<MapPolygon>(newFailRegions);
                    HighQualityRegions = new ObservableCollection<MapPolygon>(newSuccessRegions);
                    MissionAreas = new ObservableCollection<MapPolygon>(newMissionAreas);
                    MissionDetailLines = new ObservableCollection<MapPolyline>(newLineData);

                    if (isCenterFound)
                    {
                        CenterPoint = tempCenter;
                        CurrentZoomLevel = 14;
                    }
                });
            });
        }

        private List<List<GeoPoint>> GeneratePolygonsFromLine(List<GeoPoint> linePoints, double halfWidthMeters)
        {
            var polygons = new List<List<GeoPoint>>();

            for (int i = 0; i < linePoints.Count - 1; i++)
            {
                GeoPoint A = linePoints[i];
                GeoPoint B = linePoints[i + 1];

                var (nx, ny) = UnitPerp(A, B);

                GeoPoint A1 = Offset(A, nx, ny, halfWidthMeters);
                GeoPoint A2 = Offset(A, -nx, -ny, halfWidthMeters);
                GeoPoint B1 = Offset(B, nx, ny, halfWidthMeters);
                GeoPoint B2 = Offset(B, -nx, -ny, halfWidthMeters);

                polygons.Add(new List<GeoPoint> { A1, B1, B2, A2 });
            }
            return polygons;
        }

        private (double x, double y) UnitPerp(GeoPoint A, GeoPoint B)
        {
            double mPerDegLat = 111000.0;
            double mPerDegLon = Math.Cos(((A.Latitude + B.Latitude) / 2) * Math.PI / 180.0) * 111000.0;
            double vx = (B.Longitude - A.Longitude) * mPerDegLon;
            double vy = (B.Latitude - A.Latitude) * mPerDegLat;
            double len = Math.Sqrt(vx * vx + vy * vy);
            if (len < 1e-6) return (0, 0);
            return (-vy / len, vx / len);
        }

        private GeoPoint Offset(GeoPoint src, double ux, double uy, double dist)
        {
            double latDeg = (uy * dist) / 111000.0;
            double lonDeg = (ux * dist) / (Math.Cos(src.Latitude * Math.PI / 180.0) * 111000.0);
            return new GeoPoint(src.Latitude + latDeg, src.Longitude + lonDeg);
        }

        /// <summary>
        /// 트랙바 스냅용 메서드
        /// </summary>
        public void ShowUavSnapshot(ulong timestamp, int uavId, ScenarioData data, SpatialResolutionResult result)
        {
            if (data == null) return;

            // 인덱스가 없으면 최초 1회 빌드
            if (_flightDataIndex == null)
                BuildFlightDataIndex(data);

            // UI 스레드 외부: BinarySearch로 O(logN) 검색
            var flight = FindFlightDataExact((uint)uavId, timestamp);

            Application.Current.Dispatcher.BeginInvoke(() => {
                // 1. 아이콘 갱신 (캐시 기반, Remove/Add 방지)
                _cachedUavIcons.TryGetValue(uavId, out var existingIcon);

                if (flight != null && flight.FlightDataLog != null)
                {
                    if (existingIcon != null)
                    {
                        existingIcon.Location = new GeoPoint(flight.FlightDataLog.Latitude, flight.FlightDataLog.Longitude);
                    }
                    else
                    {
                        var info = new UnitMapObjectInfo
                        {
                            ID = (uint)uavId,
                            Type = 1,
                            Status = 1,
                            Location = new GeoPoint(flight.FlightDataLog.Latitude, flight.FlightDataLog.Longitude)
                        };
                        info.imagesource = (ImageSource)Application.Current.Resources["UAV_TopView"];
                        _cachedUavIcons[uavId] = info;
                        UavPositions.Add(info);
                    }

                    // 2. Footprint 갱신 (캐시 기반)
                    _cachedUavFootprints.TryGetValue(uavId, out var existingFoot);

                    if (flight.CameraDataLog != null && result != null)
                    {
                        List<SRTimestampData> srList = uavId == 4 ? result.SRData.UAV4 :
                                                       uavId == 5 ? result.SRData.UAV5 :
                                                       result.SRData.UAV6;

                        var srData = srList.FirstOrDefault(x => x.Timestamp == timestamp);

                        if (srData == null)
                        {
                            // 임무구역 밖: 풋프린트 제거
                            if (existingFoot != null)
                            {
                                UavFootprints.Remove(existingFoot);
                                _cachedUavFootprints.Remove(uavId);
                            }
                            return;
                        }

                        float currentGSD = srData.SpatialResolution;
                        Brush fill = currentGSD <= result.SRThreshold ? _validFootprintFill : _invalidFootprintFill;
                        Brush stroke = currentGSD <= result.SRThreshold ? Brushes.DodgerBlue : Brushes.Red;

                        var cam = flight.CameraDataLog;
                        var newPoints = new[]
                        {
                            new GeoPoint(cam.CameraTopLeft.Latitude, cam.CameraTopLeft.Longitude),
                            new GeoPoint(cam.CameraTopRight.Latitude, cam.CameraTopRight.Longitude),
                            new GeoPoint(cam.CameraBottomRight.Latitude, cam.CameraBottomRight.Longitude),
                            new GeoPoint(cam.CameraBottomLeft.Latitude, cam.CameraBottomLeft.Longitude)
                        };

                        if (existingFoot != null)
                        {
                            existingFoot.Fill = fill;
                            existingFoot.Stroke = stroke;
                            if (existingFoot.Points.Count == 4)
                            {
                                for (int i = 0; i < 4; i++)
                                    existingFoot.Points[i] = newPoints[i];
                            }
                            else
                            {
                                existingFoot.Points.Clear();
                                foreach (var pt in newPoints) existingFoot.Points.Add(pt);
                            }
                        }
                        else
                        {
                            var poly = new MapPolygon
                            {
                                Fill = fill,
                                Stroke = stroke,
                                StrokeStyle = _solidStyle,
                                Tag = uavId
                            };
                            foreach (var pt in newPoints) poly.Points.Add(pt);
                            _cachedUavFootprints[uavId] = poly;
                            UavFootprints.Add(poly);
                        }
                    }
                    else if (existingFoot != null)
                    {
                        UavFootprints.Remove(existingFoot);
                        _cachedUavFootprints.Remove(uavId);
                    }
                }
                else
                {
                    // 비행 데이터 없으면 아이콘/풋프린트 제거
                    if (existingIcon != null) { UavPositions.Remove(existingIcon); _cachedUavIcons.Remove(uavId); }
                    if (_cachedUavFootprints.TryGetValue(uavId, out var foot)) { UavFootprints.Remove(foot); _cachedUavFootprints.Remove(uavId); }
                }
            });
        }

        // 헬퍼 메서드 (CreatePolygon에 Stroke도 추가)
        private MapPolyline CreatePolyline(List<GeoPoint> pts, Brush b)
        {
            if (pts == null || !pts.Any()) return null;
            var c = new CoordPointCollection(); foreach (var p in pts) c.Add(p);
            return new MapPolyline { Points = c, Stroke = b, StrokeStyle = _solidStyle };
        }

        // [수정] CreatePolygon 메서드에 Fill과 Stroke 인자 추가
        private MapPolygon CreatePolygon(List<GeoPoint> pts, Brush fill, Brush stroke, ShapeTitleOptions title = null)
        {
            if (pts == null || !pts.Any()) return null;
            var c = new CoordPointCollection(); foreach (var p in pts) c.Add(p);


            return new MapPolygon { Points = c, Fill = fill, Stroke = stroke, TitleOptions = title };
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


        // [확인 후 삭제] 미사용 메서드 - 빈 콜백, 이벤트 구독도 없음
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
  




    }
}

