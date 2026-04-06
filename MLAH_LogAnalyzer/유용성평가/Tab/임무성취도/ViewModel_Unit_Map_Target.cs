using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using DevExpress.Xpf.Map;

namespace MLAH_LogAnalyzer
{
    public class ViewModel_Unit_Map_Target : CommonBase // Base 클래스 유지
    {
        private static readonly Lazy<ViewModel_Unit_Map_Target> _lazy =
            new Lazy<ViewModel_Unit_Map_Target>(() => new ViewModel_Unit_Map_Target());
        public static ViewModel_Unit_Map_Target SingletonInstance => _lazy.Value;

        // 리소스
        private readonly SolidColorBrush _runTrackBrush;
        private readonly SolidColorBrush _targetTrackBrush;
        private readonly StrokeStyle _solidStyle;
        private readonly SolidColorBrush _missionLineBrush;

        public ViewModel_Unit_Map_Target()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) return;

            _runTrackBrush = new SolidColorBrush(Colors.Blue) { Opacity = 0.8 }; _runTrackBrush.Freeze();
            _targetTrackBrush = new SolidColorBrush(Colors.Red) { Opacity = 0.6 }; _targetTrackBrush.Freeze();
            _solidStyle = new StrokeStyle { Thickness = 2 };
            _missionLineBrush = new SolidColorBrush(Colors.Cyan) { Opacity = 0.8 }; _missionLineBrush.Freeze(); // 임무 선 색상

            // (필요시 이벤트 구독 추가)
        }

        // 컬렉션
        private ObservableCollection<MapPolyline> _missionTracks = new ObservableCollection<MapPolyline>();
        public ObservableCollection<MapPolyline> MissionTracks { get => _missionTracks; set { _missionTracks = value; OnPropertyChanged(nameof(MissionTracks)); } }

        private ObservableCollection<MapPolyline> _targetTracks = new ObservableCollection<MapPolyline>();
        public ObservableCollection<MapPolyline> TargetTracks { get => _targetTracks; set { _targetTracks = value; OnPropertyChanged(nameof(TargetTracks)); } }

        private ObservableCollection<MapPolygon> _missionAreas = new ObservableCollection<MapPolygon>();
        public ObservableCollection<MapPolygon> MissionAreas { get => _missionAreas; set { _missionAreas = value; OnPropertyChanged(nameof(MissionAreas)); } }

        private ObservableCollection<MapPolyline> _missionDetailLines = new ObservableCollection<MapPolyline>();
        public ObservableCollection<MapPolyline> MissionDetailLines { get => _missionDetailLines; set { _missionDetailLines = value; OnPropertyChanged(nameof(MissionDetailLines)); } }

        public ObservableCollection<UnitMapObjectInfo> EvaluationUavPositions { get; set; } = new ObservableCollection<UnitMapObjectInfo>();
        public ObservableCollection<UnitMapObjectInfo> EvaluationTargetPositions { get; set; } = new ObservableCollection<UnitMapObjectInfo>();

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

        private GeoPoint _centerPoint = new GeoPoint(38.128774, 127.318005);
        public GeoPoint CenterPoint { get => _centerPoint; set { _centerPoint = value; OnPropertyChanged(nameof(CenterPoint)); } }
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

        // FlightData BinarySearch 인덱스 (AircraftID → 타임스탬프순 리스트)
        private Dictionary<uint, List<FlightData>> _flightDataIndex;
        // RealTargetData BinarySearch 인덱스 (타임스탬프순 리스트)
        private List<RealTargetData> _targetDataIndex;
        // 아이콘 캐시: FirstOrDefault 제거
        private Dictionary<uint, UnitMapObjectInfo> _cachedAircraftIcons = new();
        private Dictionary<uint, UnitMapObjectInfo> _cachedTargetIcons = new();

        public void BuildFlightDataIndex(ScenarioData scenarioData)
        {
            _flightDataIndex = new Dictionary<uint, List<FlightData>>();
            _targetDataIndex = null;
            _cachedAircraftIcons.Clear();
            _cachedTargetIcons.Clear();

            if (scenarioData?.FlightData == null) return;

            foreach (var group in scenarioData.FlightData
                .Where(fd => fd.FlightDataLog != null && fd.Timestamp > 0)
                .GroupBy(fd => fd.AircraftID))
            {
                _flightDataIndex[group.Key] = group.OrderBy(fd => fd.Timestamp).ToList();
            }

            if (scenarioData.RealTargetData != null)
            {
                _targetDataIndex = scenarioData.RealTargetData
                    .Where(r => r.Timestamp > 0)
                    .OrderBy(r => r.Timestamp).ToList();
            }
        }

        private FlightData FindFlightDataAtOrBefore(uint aircraftId, ulong timestamp)
        {
            if (_flightDataIndex == null || !_flightDataIndex.TryGetValue(aircraftId, out var list) || list.Count == 0)
                return null;
            int lo = 0, hi = list.Count - 1, bestIdx = -1;
            while (lo <= hi)
            {
                int mid = lo + (hi - lo) / 2;
                if (list[mid].Timestamp <= timestamp) { bestIdx = mid; lo = mid + 1; }
                else hi = mid - 1;
            }
            return bestIdx >= 0 ? list[bestIdx] : null;
        }

        private RealTargetData FindTargetDataAtOrBefore(ulong timestamp)
        {
            if (_targetDataIndex == null || _targetDataIndex.Count == 0) return null;
            int lo = 0, hi = _targetDataIndex.Count - 1, bestIdx = -1;
            while (lo <= hi)
            {
                int mid = lo + (hi - lo) / 2;
                if (_targetDataIndex[mid].Timestamp <= timestamp) { bestIdx = mid; lo = mid + 1; }
                else hi = mid - 1;
            }
            return bestIdx >= 0 ? _targetDataIndex[bestIdx] : null;
        }

        public void ClearMissionVisuals()
        {
            _flightDataIndex = null;
            _targetDataIndex = null;
            _cachedAircraftIcons.Clear();
            _cachedTargetIcons.Clear();
            Application.Current.Dispatcher.BeginInvoke(() => {
                MissionTracks.Clear(); TargetTracks.Clear(); MissionAreas.Clear();
                EvaluationUavPositions.Clear(); EvaluationTargetPositions.Clear();
            });
        }

        public async void UpdateMissionVisuals(ScenarioData scenarioData, TargetAnalysisResult targetResult)
        {
            if (scenarioData == null) { ClearMissionVisuals(); return; }

            // 1. [백그라운드] 좌표(GeoPoint)만 가공
            var resultData = await Task.Run(() =>
            {
                var uavPaths = new List<List<GeoPoint>>();
                var tgtPaths = new List<List<GeoPoint>>();
                var areas = new List<List<GeoPoint>>();
                var lineData = new List<List<GeoPoint>>();
                GeoPoint? center = null;

                var uavData = scenarioData.FlightData.Where(fd => fd.FlightDataLog != null).OrderBy(fd => fd.Timestamp).GroupBy(fd => fd.AircraftID);
                foreach (var group in uavData)
                {
                    var allPoints = group.Select(fd => new GeoPoint(fd.FlightDataLog.Latitude, fd.FlightDataLog.Longitude)).ToList();
                    if (allPoints.Count > 1)
                    {
                        if (center == null) center = allPoints[0];
                        // 항적 샘플링: 매 3번째 포인트만 취하여 렌더링 부하 감소
                        var sampled = new List<GeoPoint>();
                        for (int i = 0; i < allPoints.Count; i++)
                        {
                            if (i % 3 == 0 || i == allPoints.Count - 1)
                                sampled.Add(allPoints[i]);
                        }
                        uavPaths.Add(sampled);
                    }
                }

                if (targetResult != null)
                {
                    foreach (var kvp in targetResult.TargetPaths)
                    {
                        var points = kvp.Value.Select(t => new GeoPoint(t.Latitude, t.Longitude)).ToList();
                        if (points.Count > 1) tgtPaths.Add(points);
                    }
                }

                if (scenarioData.MissionDetail != null)
                {
                    foreach (var m in scenarioData.MissionDetail)
                    {
                        if (m.AreaList != null)
                        {
                            foreach (var a in m.AreaList)
                            {
                                var points = a.CoordinateList.Select(c => new GeoPoint(c.Latitude, c.Longitude)).ToList();
                                areas.Add(points);
                            }
                        }
                        if (m.LineList != null)
                        {
                            foreach (var l in m.LineList)
                            {
                                var points = l.CoordinateList.Select(c => new GeoPoint(c.Latitude, c.Longitude)).ToList();
                                lineData.Add(points);
                            }
                        }
                    }
                }

                return new { Uav = uavPaths, Tgt = tgtPaths, Area = areas, Line = lineData, Center = center };
            });

            // 2. [UI 스레드] Map 객체 생성
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // Select 안에 들어간 CreatePolyline/CreatePolygon이 UI 스레드에서 실행됨 (안전)
                var newMissionTracks = resultData.Uav.Select(pts => CreatePolyline(pts, _runTrackBrush)).Where(x => x != null).ToList();
                MissionTracks = new ObservableCollection<MapPolyline>(newMissionTracks);

                var newTargetTracks = resultData.Tgt.Select(pts => CreatePolyline(pts, _targetTrackBrush)).Where(x => x != null).ToList();
                TargetTracks = new ObservableCollection<MapPolyline>(newTargetTracks);

                var newMissionAreas = resultData.Area.Select(pts => CreatePolygon(pts)).Where(x => x != null).ToList();
                MissionAreas = new ObservableCollection<MapPolygon>(newMissionAreas);

                var newDetailLines = resultData.Line.Select(pts => CreatePolyline(pts, _missionLineBrush)).Where(x => x != null).ToList();
                MissionDetailLines = new ObservableCollection<MapPolyline>(newDetailLines);

                if (resultData.Center != null)
                {
                    CenterPoint = new GeoPoint(resultData.Center.GetY(), resultData.Center.GetX());
                    CurrentZoomLevel = 14;
                }
            });
        }

        // 2. [트랙바] 특정 시간대의 유닛/표적 위치 아이콘 업데이트
        public void ShowPositionsAt(ulong timestamp, ScenarioData scenarioData, TargetAnalysisResult targetResult)
        {
            if (scenarioData == null) return;

            // 인덱스 최초 빌드
            if (_flightDataIndex == null)
                BuildFlightDataIndex(scenarioData);

            // --- 1. 백그라운드: BinarySearch로 데이터 검색 O(logN) ---
            var aircraftIds = new uint[] { 1, 2, 3, 4, 5, 6 };
            var positions = new Dictionary<uint, FlightDataLog>();
            foreach (var id in aircraftIds)
            {
                var fd = FindFlightDataAtOrBefore(id, timestamp);
                if (fd != null) positions[id] = fd.FlightDataLog;
            }

            var currentSnapshot = FindTargetDataAtOrBefore(timestamp);

            // --- 2. UI 스레드: 아이콘 위치만 갱신 ---
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // UAV 위치 업데이트 (Dictionary 캐시로 O(1) 조회)
                foreach (var id in aircraftIds)
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
                            var icon = CreateObj(id, id <= 3 ? 3 : 1, loc.Latitude, loc.Longitude, 0, "UAV", null, 1);
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

                // 표적 위치 및 상태 업데이트
                var activeTargetIds = new HashSet<uint>();

                if (currentSnapshot?.TargetList != null)
                {
                    foreach (var target in currentSnapshot.TargetList)
                    {
                        activeTargetIds.Add(target.ID);
                        int statusVal = (int)target.Status;
                        string statusStr = statusVal switch
                        {
                            3 => "파괴됨",
                            2 => "탐지됨",
                            1 => "식별됨",
                            _ => "미식별"
                        };

                        if (_cachedTargetIcons.TryGetValue(target.ID, out var existingTarget))
                        {
                            existingTarget.Location = new GeoPoint(target.Latitude, target.Longitude);
                            existingTarget.Status = (uint)statusVal;
                            existingTarget.StatusString = statusStr;
                        }
                        else
                        {
                            var icon = CreateObj(target.ID, 5, target.Latitude, target.Longitude, 0, statusStr, target.Subtype, statusVal);
                            _cachedTargetIcons[target.ID] = icon;
                            EvaluationTargetPositions.Add(icon);
                        }
                    }
                }

                // 사라진 표적 제거
                var removedIds = _cachedTargetIcons.Keys.Where(id => !activeTargetIds.Contains(id)).ToList();
                foreach (var id in removedIds)
                {
                    if (_cachedTargetIcons.TryGetValue(id, out var icon))
                    {
                        EvaluationTargetPositions.Remove(icon);
                        _cachedTargetIcons.Remove(id);
                    }
                }
            });
        }

        private MapPolyline CreatePolyline(List<GeoPoint> pts, Brush brush)
        {
            var coll = new CoordPointCollection(); foreach (var p in pts) coll.Add(p);
            return new MapPolyline { Points = coll, Stroke = brush, StrokeStyle = _solidStyle };
        }
        private MapPolygon CreatePolygon(List<GeoPoint> pts)
        {
            var coll = new CoordPointCollection(); foreach (var p in pts) coll.Add(p);
            var fill = new SolidColorBrush(Colors.Yellow) { Opacity = 0.2 }; fill.Freeze();
            return new MapPolygon { Points = coll, Fill = fill, Stroke = Brushes.Orange };
        }
        private UnitMapObjectInfo CreateObj(uint id, int originalType, double lat, double lon, double h, string label, string subType, int status)
        {
            // 1. 아이콘 타입 결정 로직
            int finalType = originalType;

            // UAV가 아닌 경우에만 SubType 체크
            if (label != "UAV")
            {
                finalType = 5; // 기본값: Hostile_Tank

                if (!string.IsNullOrEmpty(subType))
                {
                    string upperSub = subType.ToUpper(); // 대소문자 무시

                    if (upperSub.Contains("ZPU"))
                    {
                        finalType = 8; // 고정고사포
                    }
                    else if (upperSub.Contains("HOSTILITY") || upperSub.Contains("SA"))
                    {
                        finalType = 9; // 특작군인 (SA-16 등)
                    }
                    else if (upperSub.Contains("PANZER") || upperSub.Contains("M2010"))
                    {
                        finalType = 4;
                    }
                }
            }

            // 2. DTO 생성 (Status 포함 -> XAML 트리거용)
            var info = new UnitMapObjectInfo
            {
                ID = id,
                Type = finalType,
                Status = (uint)status, // 여기서 0, 1, 2, 3 값이 들어가야 View의 DataTrigger가 작동함
                Location = new GeoPoint(lat, lon),
                Heading = h,
                PlatformString = label // "파괴됨", "UAV" 등의 텍스트
            };

            // 3. 리소스 키 매핑 (이미지 소스)
            string resKey = "Hostile_Tank"; // 기본 리소스

            if (finalType == 1) resKey = "UAV_TopView";
            else if (finalType == 3) resKey = "LAH";
            else if (finalType == 4) resKey = "Hostile_Panzer";
            else if (finalType == 8) resKey = "Hostile_AAG";
            else if (finalType == 9) resKey = "Hostile_AAG"; // 특작군인 (SA-16)

            // 리소스 안전하게 로드
            if (Application.Current.Resources.Contains(resKey))
            {
                info.imagesource = (ImageSource)Application.Current.Resources[resKey];
            }

            return info;
        }
    }
}