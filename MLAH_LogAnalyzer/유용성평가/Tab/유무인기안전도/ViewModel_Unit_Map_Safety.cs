
using DevExpress.Xpf.Map;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Media;
using System.Threading.Tasks;
// [확인 후 삭제] 미사용 using
//using static DevExpress.Utils.Drawing.Helpers.NativeMethods;

namespace MLAH_LogAnalyzer
{
    public class ViewModel_Unit_Map_Safety : CommonBase
    {
        private static readonly Lazy<ViewModel_Unit_Map_Safety> _lazy =
        new Lazy<ViewModel_Unit_Map_Safety>(() => new ViewModel_Unit_Map_Safety());

        public static ViewModel_Unit_Map_Safety SingletonInstance => _lazy.Value;

        #region 생성자 & 콜백
        public ViewModel_Unit_Map_Safety()
        {
            // ✅ 디자이너에서 이벤트 구독을 막는 보호 코드를 추가합니다.
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return;
            }

            CommonEvent.OnINITMissionPolygonAdd += Callback_OnINITMissionPolygonAdd;
            CommonEvent.OnINITMissionPolyLineAdd += Callback_OnINITMissionPolyLineAdd;

            CommonEvent.OnINITMissionPointAdd += Callback_OnINITMissionPointAdd;


            FocusSquareItems = new ObservableCollection<MapPolygon>();
        }

        #endregion 생성자 & 콜백

        /// <summary>
        /// 평가용: 시나리오의 전체 UAV 항적 (Polyline)
        /// (XAML의 EvaluationTrackLayer에 바인딩됨)
        /// </summary>
        //public ObservableCollection<MapPolyline> EvaluationTracks { get; set; } = new ObservableCollection<MapPolyline>();

        /// <summary>
        /// 평가용: 트랙바가 선택한 시점의 UAV 위치 (심볼)
        /// (XAML의 EvaluationUavPositionLayer에 바인딩됨)
        /// </summary>
        public ObservableCollection<UnitMapObjectInfo> EvaluationUavPositions { get; set; } = new ObservableCollection<UnitMapObjectInfo>();

        /// <summary>
        /// 평가용: 트랙바가 선택한 시점의 UAV 풋프린트 (Polygon)
        /// (XAML의 EvaluationFootprintLayer에 바인딩됨)
        /// </summary>

        /// <summary>
                /// 표적 주변 1km 반경 (회색 원)
                /// (XAML의 ThreatCircleLayer에 바인딩됨)
                /// </summary>
        public ObservableCollection<MapEllipse> ThreatCircles { get; set; } = new ObservableCollection<MapEllipse>();

        /// <summary>
        /// 안전/위험 구간이 표시된 항적 (회색/빨강 선)
        /// (XAML의 SafetyTrackLayer에 바인딩됨)
        /// </summary>
        private ObservableCollection<MapPolyline> _safetyTracks = new ObservableCollection<MapPolyline>();
        public ObservableCollection<MapPolyline> SafetyTracks
        {
            get => _safetyTracks;
            set { _safetyTracks = value; OnPropertyChanged(nameof(SafetyTracks)); }
        }

        private struct SafetyTrackDto
        {
            public List<GeoPoint> Points;
            public bool IsDangerous;
        }

        private static SolidColorBrush FrozenBrush(Color c, double opacity)
        {
            var b = new SolidColorBrush(c) { Opacity = opacity };
            b.Freeze();
            return b;
        }

        //안전도 분석에 사용할 브러시 (Freeze하여 렌더링 최적화)
        private readonly SolidColorBrush _threatCircleFill = FrozenBrush(Colors.Gray, 0.4);
        private readonly SolidColorBrush _threatCircleStroke = FrozenBrush(Colors.Gray, 0.6);
        private readonly SolidColorBrush _safeTrackBrush = FrozenBrush(Colors.Blue, 0.8);
        private readonly SolidColorBrush _dangerTrackBrush = FrozenBrush(Colors.Red, 0.9);
        private readonly SolidColorBrush _dangerCircleFill = FrozenBrush(Colors.Red, 0.20);
        private readonly SolidColorBrush _dangerCircleStroke = FrozenBrush(Colors.Red, 0.5);

        public MapCoordinateSystem MapCoordinateSystem { get; set; }

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
        // [확인 후 삭제] 미사용 필드 - 주석처리된 코드에서만 참조됨
        //private MapPolygon? _focusPolygon;
        //private readonly List<MapLine> _sideLines = new List<MapLine>();

        //private CancellationTokenSource _focusUpdateCts;
        private CancellationTokenSource _visualsUpdateCts;

        public ObservableCollection<MapPolygon> FocusSquareItems { get; } = new ObservableCollection<MapPolygon>();

        // 슬라이더 성능 최적화: FlightData 타임스탬프 검색용 인덱스
        // key: AircraftID, value: 타임스탬프 오름차순 정렬된 FlightData 리스트
        private Dictionary<uint, List<FlightData>> _flightDataIndex;
        // 아이콘 캐시: ID → UnitMapObjectInfo (FirstOrDefault 제거)
        private Dictionary<uint, UnitMapObjectInfo> _cachedAircraftIcons = new();
        // 캐시된 표적 아이콘 (ID → MapSymbol), 매 틱마다 재생성하지 않고 위치만 갱신
        private Dictionary<uint, UnitMapObjectInfo> _cachedTargetIcons = new();
        // 캐시된 위협원 (표적ID → MapEllipse), 매 틱마다 재생성하지 않고 색상/위치만 갱신
        private Dictionary<uint, MapEllipse> _cachedThreatEllipses = new();
        // 표적별 최대 위협 반경 기록 (한번 잡힌 크기는 줄어들지 않음)
        private Dictionary<uint, double> _maxThreatRadius = new();

        /// <summary>
        /// FlightData를 AircraftID별 타임스탬프 순으로 인덱싱
        /// 시나리오 로드 시 1회 호출, 이후 슬라이더 이동 시 BinarySearch로 O(logN) 검색
        /// </summary>
        public void BuildFlightDataIndex(ScenarioData scenarioData)
        {
            _flightDataIndex = new Dictionary<uint, List<FlightData>>();
            _cachedAircraftIcons.Clear();
            _cachedTargetIcons.Clear();
            _cachedThreatEllipses.Clear();
            _maxThreatRadius.Clear();

            if (scenarioData == null) return;

            foreach (var group in scenarioData.FlightData
                .Where(fd => fd.FlightDataLog != null && fd.Timestamp > 0)
                .GroupBy(fd => fd.AircraftID))
            {
                _flightDataIndex[group.Key] = group.OrderBy(fd => fd.Timestamp).ToList();
            }
        }

        /// <summary>
        /// 인덱스에서 timestamp 이하의 가장 가까운 FlightData를 BinarySearch로 검색
        /// </summary>
        private FlightData FindFlightDataAtOrBefore(uint aircraftId, ulong timestamp)
        {
            if (_flightDataIndex == null || !_flightDataIndex.TryGetValue(aircraftId, out var list) || list.Count == 0)
                return null;

            // BinarySearch: 정확히 일치하거나, 직전 항목 반환
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

        #region 거리 계산 헬퍼
        private const double EarthRadiusKm = 6371.0;
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c;
        }
        private double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }
        #endregion



        /// <summary>
        /// [!!!] 신규: 트랙바 이동 시 4개 아이콘 위치 업데이트 [!!!]
        /// </summary>
        /// <summary>
        /// 슬라이더 이동 시 기체/표적/위협원 위치 갱신
        /// - 기체: BinarySearch로 O(logN) 검색, 기존 아이콘 재사용
        /// - 표적: 캐시된 아이콘/위협원 위치+색상만 갱신 (삭제-재생성 X)
        /// </summary>
        public void ShowAircraftPositionsAt(ulong timestamp, ScenarioData scenarioData, SafetyResult safetyResult)
        {
            if (scenarioData == null || this.MapCoordinateSystem == null) return;

            // --- 1. UI 스레드 외부: 데이터 검색 (CPU 작업) ---
            var aircraftIdsToShow = new uint[] { 1, 2, 3 };
            var currentAircraftPositions = new Dictionary<uint, GeoPoint>();
            var aircraftData = new Dictionary<uint, FlightDataLog>();

            foreach (var id in aircraftIdsToShow)
            {
                var lastData = FindFlightDataAtOrBefore(id, timestamp);
                if (lastData != null)
                {
                    var loc = lastData.FlightDataLog;
                    currentAircraftPositions[id] = new GeoPoint(loc.Latitude, loc.Longitude);
                    aircraftData[id] = loc;
                }
            }

            List<Target> currentTargets = (scenarioData.RealTargetData != null && scenarioData.RealTargetData.Any())
                ? FindClosestTargetEntry(timestamp, scenarioData.RealTargetData)
                : null;

            // 표적별 위험 상태 계산 (UI 스레드 외부)
            // LOS가 잡힌 헬기 중 최대 거리를 저장 (0이면 LOS 미탐지)
            // 대공능력이 있는 표적만 위협원으로 계산 (ZPU, SA, KS 등)
            var targetDangerMap = new Dictionary<uint, double>();
            var antiAirTargetIds = new HashSet<uint>();
            if (currentTargets != null)
            {
                foreach (var target in currentTargets)
                {
                    if (!IsAntiAirTarget(target))
                        continue;

                    antiAirTargetIds.Add(target.ID);

                    double maxLosDistance = 0;
                    var targetLoc = new GeoPoint(target.Latitude, target.Longitude);

                    foreach (var kvp in currentAircraftPositions)
                    {
                        bool hasLOS = kvp.Key switch
                        {
                            1 => target.LAH1LOS,
                            2 => target.LAH2LOS,
                            3 => target.LAH3LOS,
                            _ => false
                        };
                        if (hasLOS)
                        {
                            double distance = CalculateDistance(targetLoc.Latitude, targetLoc.Longitude, kvp.Value.Latitude, kvp.Value.Longitude);
                            if (distance > maxLosDistance)
                                maxLosDistance = distance;
                        }
                    }
                    targetDangerMap[target.ID] = maxLosDistance;
                }
            }

            // --- 2. UI 스레드: 맵 객체 갱신 (최소한의 변경만) ---
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // 기체 아이콘 갱신 (Dictionary 캐시로 O(1) 조회)
                foreach (var id in aircraftIdsToShow)
                {
                    _cachedAircraftIcons.TryGetValue(id, out var existingIcon);

                    if (aircraftData.TryGetValue(id, out var loc))
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
                                Type = (id <= 3) ? (short)3 : (short)1,
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

                // 표적 아이콘 + 위협원 갱신 (캐시 기반 재사용)
                if (currentTargets == null || !currentTargets.Any())
                {
                    // 표적 없으면 캐시된 것들 제거
                    if (_cachedTargetIcons.Any())
                    {
                        foreach (var icon in _cachedTargetIcons.Values)
                            EvaluationUavPositions.Remove(icon);
                        _cachedAircraftIcons.Clear();
            _cachedTargetIcons.Clear();
                    }
                    if (_cachedThreatEllipses.Any())
                    {
                        ThreatCircles.Clear();
                        _cachedThreatEllipses.Clear();
                    }
                    return;
                }

                var currentTargetIds = new HashSet<uint>(currentTargets.Select(t => t.ID));

                // 사라진 표적 제거
                var removedIds = _cachedTargetIcons.Keys.Where(id => !currentTargetIds.Contains(id)).ToList();
                foreach (var id in removedIds)
                {
                    if (_cachedTargetIcons.TryGetValue(id, out var icon))
                    {
                        EvaluationUavPositions.Remove(icon);
                        _cachedTargetIcons.Remove(id);
                    }
                    if (_cachedThreatEllipses.TryGetValue(id, out var ell))
                    {
                        ThreatCircles.Remove(ell);
                        _cachedThreatEllipses.Remove(id);
                    }
                }

                foreach (var target in currentTargets)
                {
                    var targetLoc = new GeoPoint(target.Latitude, target.Longitude);

                    // 표적 아이콘: 캐시에 있으면 위치만 갱신, 없으면 생성
                    if (_cachedTargetIcons.TryGetValue(target.ID, out var existingTargetIcon))
                    {
                        existingTargetIcon.Location = targetLoc;
                    }
                    else
                    {
                        // Subtype에서 아이콘 타입과 플랫폼 타입 결정
                        var resolved = ResolveTargetType(target.Subtype);
                        short iconType = resolved.iconType;
                        short platformType = resolved.platformType;

                        var targetDummyInfo = new UnitObjectInfo
                        {
                            ID = (int)target.ID,
                            Type = iconType,
                            PlatformType = platformType,
                            Status = 1,
                            LOC = new CoordinateInfo { Latitude = (float)target.Latitude, Longitude = (float)target.Longitude },
                            velocity = new Velocity { Heading = 0 }
                        };
                        var newIcon = ConvertToObjectInfo(targetDummyInfo);
                        _cachedTargetIcons[target.ID] = newIcon;
                        EvaluationUavPositions.Add(newIcon);
                    }

                    // 대공능력 표적만 위협원 표시
                    if (!antiAirTargetIds.Contains(target.ID))
                    {
                        // 대공능력 없는 표적의 기존 원 제거
                        if (_cachedThreatEllipses.TryGetValue(target.ID, out var oldEll))
                        {
                            ThreatCircles.Remove(oldEll);
                            _cachedThreatEllipses.Remove(target.ID);
                        }
                        continue;
                    }

                    double currentLosDistance = targetDangerMap.GetValueOrDefault(target.ID, 0);
                    bool isDangerous = currentLosDistance > 0;

                    // 최대 반경 갱신 (한번 잡힌 크기는 줄어들지 않음)
                    double prevMax = _maxThreatRadius.GetValueOrDefault(target.ID, 1.0);
                    if (currentLosDistance > prevMax)
                        _maxThreatRadius[target.ID] = currentLosDistance;
                    double threatRadiusKm = _maxThreatRadius.GetValueOrDefault(target.ID, 1.0);
                    double threatDiameterKm = threatRadiusKm * 2.0;

                    // 현재 LOS 있으면 빨간색, 전부 벗어나면 회색 (크기 유지)
                    var fillBrush = isDangerous ? _dangerCircleFill : _threatCircleFill;
                    var strokeBrush = isDangerous ? _dangerCircleStroke : _threatCircleStroke;

                    if (_cachedThreatEllipses.TryGetValue(target.ID, out var existingEllipse))
                    {
                        // 크기가 바뀌었으면 재생성, 색상만 바뀌면 색상만 교체
                        double existingW = existingEllipse.Width;
                        bool sizeChanged = Math.Abs(existingW - threatDiameterKm) > 0.001;

                        if (!sizeChanged)
                        {
                            existingEllipse.Fill = fillBrush;
                            existingEllipse.Stroke = strokeBrush;
                        }
                        else
                        {
                            ThreatCircles.Remove(existingEllipse);
                            _cachedThreatEllipses.Remove(target.ID);

                            var ellipse = MapEllipse.CreateByCenter(
                                this.MapCoordinateSystem, targetLoc,
                                threatDiameterKm, threatDiameterKm);
                            ellipse.Fill = fillBrush;
                            ellipse.Stroke = strokeBrush;
                            ellipse.StrokeStyle = new StrokeStyle { Thickness = 1 };
                            _cachedThreatEllipses[target.ID] = ellipse;
                            ThreatCircles.Add(ellipse);
                        }
                    }
                    else
                    {
                        var ellipse = MapEllipse.CreateByCenter(
                            this.MapCoordinateSystem, targetLoc,
                            threatDiameterKm, threatDiameterKm);
                        ellipse.Fill = fillBrush;
                        ellipse.Stroke = strokeBrush;
                        ellipse.StrokeStyle = new StrokeStyle { Thickness = 1 };
                        _cachedThreatEllipses[target.ID] = ellipse;
                        ThreatCircles.Add(ellipse);
                    }
                }
            });
        }

        /// <summary>
                /// [!!!] 신규: ShowAircraftPositionsAt에서 사용할 타임스탬프 근접 로직 [!!!]
                /// (SafetyLevelCalculator에서 복제)
                /// </summary>
        private static List<Target> FindClosestTargetEntry(ulong flightTimestamp, List<RealTargetData> sortedTargetDataList)
        {
            if (!sortedTargetDataList.Any())
                return new List<Target>();

            var targetTimestamps = sortedTargetDataList.Select(e => (ulong)e.Timestamp).ToList();

            int idx = targetTimestamps.BinarySearch(flightTimestamp);

            if (idx < 0) idx = ~idx;

            if (idx == 0)
            {
                return sortedTargetDataList[0].TargetList ?? new List<Target>();
            }
            if (idx == sortedTargetDataList.Count) return sortedTargetDataList[sortedTargetDataList.Count - 1].TargetList ?? new List<Target>();

            ulong timeBefore = targetTimestamps[idx - 1];
            ulong timeAfter = targetTimestamps[idx];

            if (Math.Abs((long)flightTimestamp - (long)timeBefore) <= Math.Abs((long)flightTimestamp - (long)timeAfter))
            {
                return sortedTargetDataList[idx - 1].TargetList ?? new List<Target>();
            }
            else
            {
                return sortedTargetDataList[idx].TargetList ?? new List<Target>();
            }
        }

        /// <summary>
        /// 대공능력이 있는 표적인지 판별 (고정고사포: ZPU, SA, KS 계열)
        /// </summary>
        private static bool IsAntiAirTarget(Target target)
        {
            if (string.IsNullOrEmpty(target.Subtype))
                return false;

            string upper = target.Subtype.ToUpperInvariant();
            return upper.Contains("ZPU") || upper.Contains("SA") || upper.Contains("KS") || upper.Contains("HOSTILITY");
        }

        /// <summary>
        /// 언리얼 로그의 SubType에서 Controller 기준 Type/PlatformType 매핑
        /// </summary>
        private static (short iconType, short platformType) ResolveTargetType(string subtype)
        {
            if (string.IsNullOrEmpty(subtype)) return (5, 0); // 기본: 탱크

            string upper = subtype.ToUpperInvariant();

            // 고정고사포 (Type 8)
            if (upper.Contains("ZPU-4") || upper == "ZPU4") return (8, 1);
            if (upper.Contains("ZPU-23") || upper == "ZPU23") return (8, 2);
            if (upper.Contains("KS-12") || upper == "KS12") return (8, 3);
            if (upper.Contains("KS-19") || upper == "KS19") return (8, 4);
            if (upper.Contains("SA-3") || upper == "SA3") return (8, 5);
            if (upper.Contains("ZPU")) return (8, 1); // ZPU 계열 기본

            // 특작군인 (Type 9) - Hostility = SA-16, 아이콘은 Hostile_AAG 공유
            if (upper.Contains("HOSTILITY") || upper.Contains("SA-16") || upper == "SA16") return (9, 1);
            if (upper.Contains("SA")) return (9, 1); // SA 계열 기본 = 특작군인

            // 탱크 (Type 5)
            if (upper.Contains("T-55") || upper == "T55") return (5, 3);
            if (upper.Contains("T-72") || upper == "T72") return (5, 4);

            // 장갑차 (Type 4)
            if (upper.Contains("M2010")) return (4, 5);
            if (upper.Contains("PANZER")) return (4, 0);

            return (5, 0);
        }

        /// <summary>
        /// 안전도 시각화 요소(원, 항적)를 모두 지우기
        /// </summary>
        public void ClearSafetyVisuals()
        {
            _cachedAircraftIcons.Clear();
            _cachedTargetIcons.Clear();
            _cachedThreatEllipses.Clear();
            _maxThreatRadius.Clear();
            _flightDataIndex = null;

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                ThreatCircles.Clear();
                SafetyTracks.Clear();
                // EvaluationUavPositions는 공용이므로 지우지 않음
            });
        }

        /// <summary>
        /// 안전도 시각화(원, 항적)를 업데이트하는 핵심 메서드 [!!!]
        /// </summary>
        //public void UpdateSafetyVisuals(ScenarioData scenarioData)
        //{
        //    // 맵 좌표계가 준비되지 않았으면 종료
        //    if (this.MapCoordinateSystem == null)
        //    {
        //        return;
        //    }

        //    // 시나리오 데이터가 없으면 (선택 해제)
        //    if (scenarioData == null)
        //    {
        //        ClearSafetyVisuals(); // 기존 항적/원 지우기
        //        return;
        //    }

        //    // [핵심] 맵 객체 조작은 반드시 UI 스레드에서 수행
        //    Application.Current.Dispatcher.BeginInvoke(() =>
        //    {
        //        // 1. 기존 항적 모두 삭제
        //        SafetyTracks.Clear();
        //        // (참고: ThreatCircles는 트랙바 이동 시 ShowAircraftPositionsAt에서 관리됨)

        //        // 맵 중심점을 이동시킬 첫 번째 좌표를 저장할 변수
        //        GeoPoint? firstPointToCenterOn = null;

        //        // --- 1. 정적 항적 그리기 ---
        //        var aircraftToDraw = new uint[] { 1, 2, 3 };

        //        // 모든 기체의 비행 데이터를 가져와 ID별로 그룹화
        //        var allFlightData = scenarioData.FlightData
        //            .Where(fd => aircraftToDraw.Contains(fd.AircraftID) && fd.FlightDataLog != null)
        //            .OrderBy(fd => fd.Timestamp) // 시간순 정렬 (중요)
        //            .GroupBy(fd => fd.AircraftID);

        //        // [루프 시작] 기체 그룹별로 (총 6번 반복)
        //        foreach (var aircraftGroup in allFlightData)
        //        {
        //            // 이 기체의 모든 비행 기록 (이미 시간순으로 정렬됨)
        //            var flightPathPoints = aircraftGroup.ToList();

        //            // 데이터가 없으면 이 기체는 통과
        //            if (!flightPathPoints.Any()) continue;

        //            // ───────────────────────────────────────────
        //            // [!!!] 여기가 "첫 번째 점 찾는 로직"입니다. [!!!]
        //            // ───────────────────────────────────────────
        //            // firstPointToCenterOn 변수가 아직 null (비어 있음)이고,
        //            // 이번 기체 그룹(flightPathPoints)에 데이터가 1개라도 있다면
        //            if (firstPointToCenterOn == null)
        //            {
        //                // 이 기체의 첫 번째 비행 기록(First())의 위치(FlightDataLog)를 가져옴
        //                var firstLog = flightPathPoints.First().FlightDataLog;
        //                if (firstLog != null)
        //                {
        //                    // 이 좌표를 "맵 중심점 후보"로 저장
        //                    firstPointToCenterOn = new GeoPoint(firstLog.Latitude, firstLog.Longitude);
        //                }
        //            }
        //            // ───────────────────────────────────────────


        //            // [--- 최적화된 항적 생성 로직 ---]

        //            // [수정 1] 기체당 *단 하나*의 Polyline 객체 생성
        //            var aircraftTrack = new MapPolyline
        //            {
        //                Stroke = _safeTrackBrush, // ViewModel에 정의된 브러시 (예: 파란색)
        //                StrokeStyle = new StrokeStyle { Thickness = 3 },
        //                IsHitTestVisible = false // [추가 최적화] 마우스 이벤트 무시
        //            };

        //            // [수정 2] 기체당 *단 하나*의 좌표 컬렉션 생성
        //            var allPoints = new CoordPointCollection();

        //            // [수정 3] 이 기체의 모든 비행 기록을 순회하며 좌표만 추가
        //            foreach (var flightEntry in flightPathPoints)
        //            {
        //                // FlightDataLog가 null이 아닌 경우에만 좌표 추가
        //                if (flightEntry.FlightDataLog != null)
        //                {
        //                    allPoints.Add(new GeoPoint(
        //                        flightEntry.FlightDataLog.Latitude,
        //                        flightEntry.FlightDataLog.Longitude
        //                    ));
        //                }
        //            }

        //            // [수정 4] 루프가 끝난 후 좌표 컬렉션을 한 번에 할당
        //            aircraftTrack.Points = allPoints;

        //            // [수정 5] 완성된 '긴' 항적 객체를 맵에 한 번만 추가 (총 6번 호출)
        //            SafetyTracks.Add(aircraftTrack);

        //        } // [루프 끝] (foreach aircraftGroup)

        //        // --- 2. 맵 중심점 업데이트 ---
        //        // 위 루프에서 맵 중심점 후보(firstPointToCenterOn)를 찾았다면
        //        if (firstPointToCenterOn != null)
        //        {
        //            // 맵 뷰모델의 CenterPoint와 CurrentZoomLevel 속성을 업데이트
        //            // (XAML의 MapControl이 이 속성들에 바인딩되어 있으므로 맵이 자동으로 이동/확대됨)
        //            CenterPoint = new GeoPoint(firstPointToCenterOn.GetY(), firstPointToCenterOn.GetX());
        //            CurrentZoomLevel = 15; // 줌 레벨 15로 설정
        //        }
        //    }); // [Dispatcher.Invoke 끝]
        //}
        /// <summary>
        /// 안전도 시각화(원, 항적)를 업데이트하는 핵심 메서드
        /// SafetyResult를 받아서 위험 구간을 빨간색으로 표시합니다.
        /// </summary>
        //public void UpdateSafetyVisuals(ScenarioData scenarioData, SafetyResult safetyResult)
        //{
        //    if (this.MapCoordinateSystem == null) return;

        //    if (scenarioData == null)
        //    {
        //        ClearSafetyVisuals();
        //        return;
        //    }

        //    Application.Current.Dispatcher.BeginInvoke(() =>
        //    {
        //        SafetyTracks.Clear();
        //        GeoPoint? firstPointToCenterOn = null;

        //        var aircraftToDraw = new uint[] { 1, 2, 3 }; // LAH만 표시

        //        // 1. 빠른 조회를 위해 위협 타임스탬프를 HashSet으로 변환
        //        var threatMap = new Dictionary<uint, HashSet<ulong>>();
        //        if (safetyResult != null && safetyResult.ThreatenedTimestamps != null)
        //        {
        //            foreach (var kvp in safetyResult.ThreatenedTimestamps)
        //            {
        //                // key는 "LAH1", "UAV4" 형식이므로 숫자만 추출
        //                string idStr = System.Text.RegularExpressions.Regex.Match(kvp.Key, @"\d+").Value;
        //                if (uint.TryParse(idStr, out uint uid))
        //                {
        //                    threatMap[uid] = new HashSet<ulong>(kvp.Value);
        //                }
        //            }
        //        }

        //        // 2. 기체별 항적 그리기
        //        var allFlightData = scenarioData.FlightData
        //            .Where(fd => aircraftToDraw.Contains(fd.AircraftID) && fd.FlightDataLog != null)
        //            .OrderBy(fd => fd.Timestamp)
        //            .GroupBy(fd => fd.AircraftID);

        //        foreach (var aircraftGroup in allFlightData)
        //        {
        //            uint aircraftId = aircraftGroup.Key;
        //            var flightPathPoints = aircraftGroup.ToList();
        //            if (!flightPathPoints.Any()) continue;

        //            // 맵 중심점 설정 (첫 기체의 첫 위치)
        //            if (firstPointToCenterOn == null)
        //            {
        //                var firstLog = flightPathPoints.First().FlightDataLog;
        //                firstPointToCenterOn = new GeoPoint(firstLog.Latitude, firstLog.Longitude);
        //            }

        //            // 해당 기체의 위협 시간 목록 가져오기
        //            HashSet<ulong> currentThreats = threatMap.ContainsKey(aircraftId) ? threatMap[aircraftId] : new HashSet<ulong>();

        //            // [핵심] 상태(안전/위험)가 바뀔 때마다 선을 끊어서 그림
        //            // 최적화를 위해 점 하나하나를 Polyline으로 만들지 않고, 상태가 같은 구간을 묶음.

        //            var segmentPoints = new CoordPointCollection();
        //            bool isSegmentDangerous = false; // 현재 세그먼트의 상태

        //            // 첫 번째 점 처리
        //            var firstPt = flightPathPoints[0];
        //            bool firstStatus = currentThreats.Contains(firstPt.Timestamp);

        //            segmentPoints.Add(new GeoPoint(firstPt.FlightDataLog.Latitude, firstPt.FlightDataLog.Longitude));
        //            isSegmentDangerous = firstStatus;

        //            for (int i = 1; i < flightPathPoints.Count; i++)
        //            {
        //                var currData = flightPathPoints[i];
        //                bool currStatus = currentThreats.Contains(currData.Timestamp);
        //                var currPoint = new GeoPoint(currData.FlightDataLog.Latitude, currData.FlightDataLog.Longitude);

        //                // 상태가 바뀌었으면 이전 세그먼트 마무리하고 새 세그먼트 시작
        //                if (currStatus != isSegmentDangerous)
        //                {
        //                    // 1. 이전 세그먼트 그리기
        //                    // (연결성을 위해 현재 점까지 포함시켜야 끊어지지 않음)
        //                    segmentPoints.Add(currPoint);
        //                    AddPolyline(segmentPoints, isSegmentDangerous);

        //                    // 2. 새로운 세그먼트 시작
        //                    segmentPoints = new CoordPointCollection();
        //                    segmentPoints.Add(currPoint); // 연결점부터 시작
        //                    isSegmentDangerous = currStatus;
        //                }
        //                else
        //                {
        //                    // 상태가 같으면 점만 추가
        //                    segmentPoints.Add(currPoint);
        //                }
        //            }

        //            // 마지막 남은 세그먼트 그리기
        //            if (segmentPoints.Count > 1)
        //            {
        //                AddPolyline(segmentPoints, isSegmentDangerous);
        //            }
        //        }

        //        // 3. 맵 중심 이동
        //        if (firstPointToCenterOn != null)
        //        {
        //            CenterPoint = new GeoPoint(firstPointToCenterOn.GetY(), firstPointToCenterOn.GetX());
        //            CurrentZoomLevel = 14;
        //        }
        //    });
        //}

        public async Task UpdateSafetyVisualsAsync(ScenarioData scenarioData, SafetyResult safetyResult)
        {
            if (this.MapCoordinateSystem == null) return;
            if (scenarioData == null) { ClearSafetyVisuals(); return; }

            // 슬라이더 검색용 인덱스 구축 (1회)
            BuildFlightDataIndex(scenarioData);

            // ★ 1. 백그라운드: 무거운 계산 및 순수 좌표(GeoPoint)만 수집
            var resultData = await Task.Run(() =>
            {
                var dtos = new List<SafetyTrackDto>();
                GeoPoint? firstPointToCenterOn = null;
                var aircraftToDraw = new uint[] { 1, 2, 3 }; // LAH만 표시

                var threatMap = new Dictionary<uint, HashSet<ulong>>();
                if (safetyResult != null && safetyResult.ThreatenedTimestamps != null)
                {
                    foreach (var kvp in safetyResult.ThreatenedTimestamps)
                    {
                        string idStr = System.Text.RegularExpressions.Regex.Match(kvp.Key, @"\d+").Value;
                        if (uint.TryParse(idStr, out uint uid))
                        {
                            threatMap[uid] = new HashSet<ulong>(kvp.Value);
                        }
                    }
                }

                var allFlightData = scenarioData.FlightData
                    .Where(fd => aircraftToDraw.Contains(fd.AircraftID) && fd.FlightDataLog != null)
                    .OrderBy(fd => fd.Timestamp)
                    .GroupBy(fd => fd.AircraftID);

                foreach (var aircraftGroup in allFlightData)
                {
                    uint aircraftId = aircraftGroup.Key;
                    var flightPathPoints = aircraftGroup.ToList();
                    if (!flightPathPoints.Any()) continue;

                    if (firstPointToCenterOn == null)
                    {
                        var firstLog = flightPathPoints.First().FlightDataLog;
                        firstPointToCenterOn = new GeoPoint(firstLog.Latitude, firstLog.Longitude);
                    }

                    HashSet<ulong> currentThreats = threatMap.ContainsKey(aircraftId) ? threatMap[aircraftId] : new HashSet<ulong>();

                    var segmentPoints = new List<GeoPoint>();
                    var firstPt = flightPathPoints[0];
                    bool isSegmentDangerous = currentThreats.Contains(firstPt.Timestamp);

                    segmentPoints.Add(new GeoPoint(firstPt.FlightDataLog.Latitude, firstPt.FlightDataLog.Longitude));

                    for (int i = 1; i < flightPathPoints.Count; i++)
                    {
                        var currData = flightPathPoints[i];
                        bool currStatus = currentThreats.Contains(currData.Timestamp);
                        var currPoint = new GeoPoint(currData.FlightDataLog.Latitude, currData.FlightDataLog.Longitude);

                        if (currStatus != isSegmentDangerous)
                        {
                            segmentPoints.Add(currPoint);
                            // UI 객체 생성 대신 DTO에 데이터만 담아서 넘김
                            dtos.Add(new SafetyTrackDto { Points = new List<GeoPoint>(segmentPoints), IsDangerous = isSegmentDangerous });

                            segmentPoints.Clear();
                            segmentPoints.Add(currPoint);
                            isSegmentDangerous = currStatus;
                        }
                        else
                        {
                            segmentPoints.Add(currPoint);
                        }
                    }

                    if (segmentPoints.Count > 1)
                    {
                        dtos.Add(new SafetyTrackDto { Points = new List<GeoPoint>(segmentPoints), IsDangerous = isSegmentDangerous });
                    }
                }

                return new { Dtos = dtos, Center = firstPointToCenterOn };
            });

            // ★ 2. UI 스레드: DTO를 MapPolyline으로 변환하고 통째로 갈아끼우기 (안전하고 빠름)
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                var newTracks = new List<MapPolyline>();

                foreach (var dto in resultData.Dtos)
                {
                    var coll = new CoordPointCollection();
                    // 항적 샘플링: 매 3번째 포인트만 취하여 렌더링 부하 감소
                    if (dto.Points.Count <= 6)
                    {
                        foreach (var pt in dto.Points) coll.Add(pt);
                    }
                    else
                    {
                        for (int i = 0; i < dto.Points.Count; i++)
                        {
                            if (i % 3 == 0 || i == dto.Points.Count - 1)
                                coll.Add(dto.Points[i]);
                        }
                    }

                    newTracks.Add(new MapPolyline
                    {
                        Points = coll,
                        Stroke = dto.IsDangerous ? _dangerTrackBrush : _safeTrackBrush,
                        StrokeStyle = new StrokeStyle { Thickness = dto.IsDangerous ? 4 : 3 },
                        IsHitTestVisible = false
                    });
                }

                // 단 1번의 렌더링 갱신
                SafetyTracks = new ObservableCollection<MapPolyline>(newTracks);

                if (resultData.Center != null)
                {
                    CenterPoint = new GeoPoint(resultData.Center.GetY(), resultData.Center.GetX());
                    CurrentZoomLevel = 14;
                }
            });
        }

        // (헬퍼 함수 추가: List 안에서 객체만 만들어 반환)
        private MapPolyline CreateSafetyPolyline(CoordPointCollection points, bool isDangerous)
        {
            return new MapPolyline
            {
                Points = points,
                Stroke = isDangerous ? _dangerTrackBrush : _safeTrackBrush,
                StrokeStyle = new StrokeStyle { Thickness = isDangerous ? 4 : 3 },
                IsHitTestVisible = false
            };
        }

        // [헬퍼] 폴리라인 생성 및 추가
        private void AddPolyline(CoordPointCollection points, bool isDangerous)
        {
            var polyline = new MapPolyline
            {
                Points = points,
                Stroke = isDangerous ? _dangerTrackBrush : _safeTrackBrush,
                StrokeStyle = new StrokeStyle { Thickness = isDangerous ? 4 : 3 }, // 위험 구간은 좀 더 굵게
                IsHitTestVisible = false
            };
            SafetyTracks.Add(polyline);
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

            Application.Current.Dispatcher.BeginInvoke(() =>
                  {
                      ClearSafetyVisuals();
                      EvaluationUavPositions.Clear(); // 공용 트랙바 아이콘
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
                case 8: // 고정고사포
                    {
                        temp_imagesource = (ImageSource)Application.Current.Resources["Hostile_AAG"];
                    }
                    break;
                case 9: // 특작군인 (SA-16 등 대공능력 보유)
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

        private int _MapCursorAlt = 0;
        public int MapCursorAlt
        {
            get
            {
                return _MapCursorAlt;
            }
            set
            {
                _MapCursorAlt = value;
                OnPropertyChanged("MapCursorAlt");
            }
        }

        //SRTM 리더 인스턴스 (한 번만 열어두고 계속 쓰기 위함)
        public SrtmReader SrtmReaderInstance { get; private set; }

        public void InitializeSrtm(string srtmPath)
        {
            try
            {
                if (System.IO.File.Exists(srtmPath))
                {
                    SrtmReaderInstance = new SrtmReader(srtmPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SRTM Load Error: " + ex.Message);
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

