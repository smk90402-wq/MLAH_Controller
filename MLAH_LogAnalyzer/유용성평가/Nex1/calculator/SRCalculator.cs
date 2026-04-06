using MLAH_LogAnalyzer;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MLAH_LogAnalyzer
{
    public class SRTimestampData
    {
        public ulong Timestamp { get; set; }
        public float SpatialResolution { get; set; }
    }

    public class SpatialResolutionResult
    {
        public SRData SRData { get; set; } = new SRData();
        public LowQualityRegion LowQualityRegions { get; set; } = new LowQualityRegion();
        // 타입 재사용 (Dictionary<string, List<CoordinateOutput>>)
        public LowQualityRegion HighQualityRegions { get; set; } = new LowQualityRegion();
        public float SRThreshold { get; set; }
        public ulong ValidTime { get; set; }
        public uint Score { get; set; }
    }

    public class SRData
    {
        public List<SRTimestampData> UAV4 { get; set; } = new List<SRTimestampData>();
        public List<SRTimestampData> UAV5 { get; set; } = new List<SRTimestampData>();
        public List<SRTimestampData> UAV6 { get; set; } = new List<SRTimestampData>();
    }

    /// <summary>
    /// AreaN 형태로 동적 속성을 지원하는 Dictionary 기반 지역 컨테이너
    /// 예: this["Area1"] = coordinates1, this["Area2"] = coordinates2
    /// </summary>
    public class LowQualityRegion : Dictionary<string, List<CoordinateOutput>>
    {
    }


    public static class SRCalculator
    {
        // 카메라 해상도 상수
        private const int FULL_HD_WIDTH  = 1920;
        private const int FULL_HD_HEIGHT = 1080;
        private const int HD_WIDTH       = 1280;
        private const int HD_HEIGHT      = 720;

        private const double EarthRadiusMeters = 6371000.0;

        // ──────────────────────────────────────────────────────────────────────
        // 공개 API (ScenarioData 오버로드)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>공간해상도 점수만 빠르게 계산합니다.</summary>
        public static async Task<uint?> getSRScore(ScenarioData scenarioData, float srThreshold = 7.895f, int camMode = 1)
        {
            try
            {
                if (scenarioData == null) return null;
                return getSRScore(scenarioData.FlightData, scenarioData.MissionDetail, srThreshold, camMode);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>공간해상도 분석(상세 데이터 포함)을 수행합니다.</summary>
        public static async Task<SpatialResolutionResult?> getSRData(ScenarioData scenarioData, float srThreshold = 7.895f, int camMode = 1)
        {
            if (scenarioData == null)
            {
                Console.WriteLine("시나리오 데이터를 로드할 수 없어 공간해상도 분석을 수행할 수 없습니다.");
                return null;
            }
            return getSRData(scenarioData.FlightData, scenarioData.MissionDetail, srThreshold, camMode);
        }

        // ──────────────────────────────────────────────────────────────────────
        // 공개 API (원시 데이터 오버로드)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 비행 데이터 목록으로부터 공간해상도 점수(0~100)를 계산합니다.
        /// 유효(고품질) 촬영 시간 / 전체 촬영 시간 * 100
        /// </summary>
        public static uint getSRScore(List<FlightData> flightDataList, List<MissionDetail> missionDetailList, float srThreshold = 10.0f, int camMode = 1)
        {
            if (!flightDataList.Any()) return 0;

            // UAV(ID > 3)이면서 카메라 데이터가 있는 항목만 시간순 정렬
            var uavData = flightDataList
                .Where(f => f.AircraftID > 3 && f.CameraDataLog != null)
                .OrderBy(f => f.Timestamp)
                .ToList();

            if (!uavData.Any()) return 0;

            ulong startTs = uavData.First().Timestamp;
            ulong endTs   = uavData.Last().Timestamp;
            double totalDurationSeconds = (endTs - startTs) / 1000.0;

            if (totalDurationSeconds <= 0) return 0;

            var missionPolygon = CreateMissionPolygon(missionDetailList, new GeometryFactory());

            double validDurationSeconds = 0;
            ulong  lastValidTs = 0;

            int totalItems     = uavData.Count;
            int processedItems = 0;

            foreach (var flightEntry in uavData)
            {
                ReportProgress(ref processedItems, totalItems, "SR Score");

                var cameraLog = flightEntry.CameraDataLog;
                if (cameraLog == null) continue;

                try
                {
                    if (!HasValidCorners(cameraLog)) continue;
                    if (!IsFootprintInMissionArea(cameraLog, missionPolygon, new GeometryFactory())) continue;

                    double widthMeters  = CalculateDistanceMeters(cameraLog.CameraTopLeft.Latitude,  cameraLog.CameraTopLeft.Longitude,
                                                                   cameraLog.CameraTopRight.Latitude, cameraLog.CameraTopRight.Longitude);
                    double heightMeters = CalculateDistanceMeters(cameraLog.CameraTopLeft.Latitude,    cameraLog.CameraTopLeft.Longitude,
                                                                   cameraLog.CameraBottomLeft.Latitude, cameraLog.CameraBottomLeft.Longitude);

                    if (widthMeters <= 0 || heightMeters <= 0) continue;

                    double avgGsd = CalculateGsdCmPerPixel(widthMeters, heightMeters, camMode);

                    if (avgGsd <= srThreshold)
                    {
                        ulong currentTs = flightEntry.Timestamp;
                        // 이전 유효 데이터와 2초 이내 연속인 경우 시간 누적
                        if (lastValidTs > 0 && (currentTs - lastValidTs) <= 2000)
                            validDurationSeconds += (currentTs - lastValidTs) / 1000.0;
                        lastValidTs = currentTs;
                    }
                    else
                    {
                        lastValidTs = 0; // 품질 미달 → 연속성 끊김
                    }
                }
                catch
                {
                    continue;
                }
            }

            double score = (validDurationSeconds / totalDurationSeconds) * 100.0;
            return (uint)Math.Max(0, Math.Min(100, Math.Round(score, MidpointRounding.AwayFromZero)));
        }

        /// <summary>
        /// 비행 데이터 목록으로부터 공간해상도 상세 분석 결과를 반환합니다.
        /// </summary>
        public static SpatialResolutionResult getSRData(List<FlightData> flightDataList, List<MissionDetail> missionDetailList, float srThreshold = 7.895f, int camMode = 1)
        {
            var result = new SpatialResolutionResult { SRThreshold = srThreshold };

            if (!flightDataList.Any())
            {
                Console.WriteLine("공간 해상도 분석을 위한 비행 데이터가 유효하지 않거나 비어 있습니다. 분석을 건너뜁니다.");
                return result;
            }

            var uavSRData             = new Dictionary<uint, List<SRTimestampData>>();
            var highQualityTimestamps = new List<ulong>();
            // ★ [최적화] 루프 중 고품질 폴리곤을 미리 수집 → CalculateRegionsFromMission 재순회 제거
            var collectedHQPolygons   = new List<Polygon>();
            int totalValidTimestamps  = 0;
            int highQualityCount      = 0;

            // ★ [최적화] GeometryFactory를 루프 밖에서 단 한번만 생성
            var gf = new GeometryFactory();
            var missionPolygon = CreateMissionPolygon(missionDetailList, gf);

            int totalItems     = flightDataList.Count;
            int processedItems = 0;

            foreach (var flightEntry in flightDataList)
            {
                ReportProgress(ref processedItems, totalItems, "SR Data");

                uint  aircraftId = flightEntry.AircraftID;
                ulong timestamp  = (ulong)flightEntry.Timestamp;

                // 카메라 데이터가 없는 무인기(ID 1~3)는 건너뜀
                if (aircraftId <= 3) continue;

                var cameraLog = flightEntry.CameraDataLog;
                if (cameraLog == null) continue;

                try
                {
                    if (!HasValidCorners(cameraLog)) continue;
                    if (!IsFootprintInMissionArea(cameraLog, missionPolygon, gf)) continue;

                    double widthMeters  = CalculateDistanceMeters(cameraLog.CameraTopLeft.Latitude,  cameraLog.CameraTopLeft.Longitude,
                                                                   cameraLog.CameraTopRight.Latitude, cameraLog.CameraTopRight.Longitude);
                    double heightMeters = CalculateDistanceMeters(cameraLog.CameraTopLeft.Latitude,    cameraLog.CameraTopLeft.Longitude,
                                                                   cameraLog.CameraBottomLeft.Latitude, cameraLog.CameraBottomLeft.Longitude);

                    if (widthMeters <= 0 || heightMeters <= 0) continue;

                    double avgGsd = CalculateGsdCmPerPixel(widthMeters, heightMeters, camMode);

                    // UAV별 SR 데이터 저장
                    if (!uavSRData.ContainsKey(aircraftId))
                        uavSRData[aircraftId] = new List<SRTimestampData>();

                    uavSRData[aircraftId].Add(new SRTimestampData
                    {
                        Timestamp         = timestamp,
                        SpatialResolution = (float)avgGsd
                    });

                    totalValidTimestamps++;

                    if (avgGsd <= srThreshold)
                    {
                        highQualityCount++;
                        highQualityTimestamps.Add(timestamp);

                        // ★ [최적화] 고품질 폴리곤을 지금 바로 수집 (나중에 재순회 불필요)
                        try
                        {
                            var coords = new NetTopologySuite.Geometries.Coordinate[]
                            {
                                new(cameraLog.CameraTopLeft.Longitude,    cameraLog.CameraTopLeft.Latitude),
                                new(cameraLog.CameraTopRight.Longitude,   cameraLog.CameraTopRight.Latitude),
                                new(cameraLog.CameraBottomRight.Longitude,cameraLog.CameraBottomRight.Latitude),
                                new(cameraLog.CameraBottomLeft.Longitude, cameraLog.CameraBottomLeft.Latitude),
                                new(cameraLog.CameraTopLeft.Longitude,    cameraLog.CameraTopLeft.Latitude),
                            };
                            if (coords.Take(4).Distinct().Count() >= 3)
                                collectedHQPolygons.Add(gf.CreatePolygon(new LinearRing(coords)));
                        }
                        catch { /* 폴리곤 생성 실패는 무시 */ }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"발자국 코너 데이터 처리 중 오류 (Timestamp: {timestamp}, AircraftID: {aircraftId}): {e.Message}");
                }
            }

            // UAV별 SR 데이터 할당
            if (uavSRData.TryGetValue(4, out var uav4)) result.SRData.UAV4 = uav4;
            if (uavSRData.TryGetValue(5, out var uav5)) result.SRData.UAV5 = uav5;
            if (uavSRData.TryGetValue(6, out var uav6)) result.SRData.UAV6 = uav6;

            // Score: 고품질 비율 (%)
            result.Score = totalValidTimestamps > 0
                ? (uint)((double)highQualityCount / totalValidTimestamps * 100)
                : 0;

            if (result.Score == 0 && totalValidTimestamps == 0)
                Console.WriteLine("유효한 촬영 데이터가 없습니다.");

            // ValidTime: 고품질 촬영 타임스탬프 범위
            if (highQualityTimestamps.Count > 1)
            {
                var sorted = highQualityTimestamps.Distinct().OrderBy(t => t).ToList();
                result.ValidTime = sorted.Last() - sorted.First();
            }

            // ============================================================================
            // [고품질/저품질 구역 시각화 ON/OFF 스위치]
            //
            // 현재 상태: OFF (빈 객체 할당 → 지도에 아무것도 안 그려짐)
            //
            // ★ 켜는 방법 (3단계):
            //   1) [여기] 아래 "OFF 블록"을 주석처리하고, "ON 블록"의 주석을 해제
            //   2) [ViewModel_Unit_Map_SR.cs] UpdateMissionVisuals() 안의
            //      "B. 저품질 영역"과 "C. 고품질 영역" 블록은 이미 활성화되어 있으므로 추가 작업 없음
            //   3) [View_Unit_Map_SR.xaml] LowQualityRegions, HighQualityRegions 레이어도
            //      이미 활성화되어 있으므로 추가 작업 없음
            //
            // ★ 끄는 방법: "ON 블록"을 주석처리하고, "OFF 블록"의 주석을 해제 (현재 상태로 복원)
            //
            // 참고: CalculateRegionsFromMission()은 NTS 폴리곤 연산(Union, Difference)을
            //       수행하므로 임무 구역이 많으면 수 초 소요될 수 있음
            // ============================================================================

            // --- ON 블록 (활성화하려면 이 줄의 주석을 해제) ---
            //var (lowQuality, highQuality) = CalculateRegionsFromMission(missionDetailList, missionPolygon, collectedHQPolygons, gf);

            // --- OFF 블록 (현재 활성 — 비활성화하려면 이 3줄을 주석처리) ---
            var lowQuality = new LowQualityRegion();
            var highQuality = new LowQualityRegion();

            result.LowQualityRegions  = lowQuality;
            result.HighQualityRegions = highQuality;

            return result;
        }

        /// <summary>
        /// 특정 UAV의 개별 임무 구역에 대한 공간해상도 점수를 계산합니다.
        /// </summary>
        public static uint getIndividualMissionSRScore(IndividualMission individualMission, List<FlightData> flightDataList, uint uavID, float srThreshold = 7.895f, int camMode = 1)
        {
            var missionCoordinates = ExtractMissionCoordinates(individualMission);
            if (missionCoordinates == null) return 0;

            if (missionCoordinates.Any() && missionCoordinates.First() != missionCoordinates.Last())
                missionCoordinates.Add(missionCoordinates.First());

            var missionPolygon = new GeometryFactory().CreatePolygon(new LinearRing(missionCoordinates.ToArray()));

            var uavFlightData = flightDataList
                .Where(f => f.AircraftID == uavID && f.CameraDataLog != null)
                .OrderBy(f => f.Timestamp)
                .ToList();

            int totalValidTimestamps  = 0;
            int highQualityTimestamps = 0;
            int totalItems     = uavFlightData.Count;
            int processedItems = 0;

            foreach (var flightEntry in uavFlightData)
            {
                ReportProgress(ref processedItems, totalItems, $"SR Individual Mission UAV{uavID}");

                var cameraLog = flightEntry.CameraDataLog;
                if (cameraLog == null) continue;
                if (!IsFootprintInMissionArea(cameraLog, missionPolygon, new GeometryFactory())) continue;

                totalValidTimestamps++;

                if (!HasValidCorners(cameraLog)) continue;

                double widthMeters  = CalculateDistanceMeters(cameraLog.CameraTopLeft.Latitude,  cameraLog.CameraTopLeft.Longitude,
                                                               cameraLog.CameraTopRight.Latitude, cameraLog.CameraTopRight.Longitude);
                double heightMeters = CalculateDistanceMeters(cameraLog.CameraTopLeft.Latitude,    cameraLog.CameraTopLeft.Longitude,
                                                               cameraLog.CameraBottomLeft.Latitude, cameraLog.CameraBottomLeft.Longitude);

                if (widthMeters > 0 && heightMeters > 0)
                {
                    double avgGsd = CalculateGsdCmPerPixel(widthMeters, heightMeters, camMode);
                    if (avgGsd <= srThreshold) highQualityTimestamps++;
                }
            }

            return totalValidTimestamps > 0
                ? (uint)((double)highQualityTimestamps / totalValidTimestamps * 100)
                : 0;
        }

        // ──────────────────────────────────────────────────────────────────────
        // 내부 헬퍼
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// GSD(지상 샘플 거리)를 cm/pixel 단위로 계산합니다.
        /// </summary>
        internal static double CalculateGsdCmPerPixel(double widthMeters, double heightMeters, int camMode)
        {
            double gsdX, gsdY;
            if (camMode == 1) // Full HD
            {
                gsdX = widthMeters  / FULL_HD_WIDTH;
                gsdY = heightMeters / FULL_HD_HEIGHT;
            }
            else // HD
            {
                gsdX = widthMeters  / HD_WIDTH;
                gsdY = heightMeters / HD_HEIGHT;
            }
            return ((gsdX + gsdY) / 2.0) * 100.0; // m/px → cm/px
        }

        /// <summary>
        /// 카메라 발자국의 4개 코너 좌표가 모두 유효한지 확인합니다.
        /// </summary>
        internal static bool HasValidCorners(CameraDataLog cameraLog)
        {
            return cameraLog.CameraTopLeft     != null
                && cameraLog.CameraTopRight    != null
                && cameraLog.CameraBottomLeft  != null
                && cameraLog.CameraBottomRight != null;
        }

        /// <summary>
        /// 10% 단위로 진행률을 콘솔에 출력합니다.
        /// processedItems는 호출 시 자동으로 1 증가합니다.
        /// </summary>
        private static void ReportProgress(ref int processedItems, int totalItems, string label)
        {
            processedItems++;
            if (totalItems <= 0) return;
            int pct = (int)((processedItems / (double)totalItems) * 100);
            if (pct % 10 == 0)
                Console.WriteLine($"[{label}] Progress: {pct}%");
        }

        /// <summary>
        /// IndividualMission에서 임무 좌표 목록을 추출합니다.
        /// lineList 우선, 없으면 areaList 사용.
        /// </summary>
        private static List<NetTopologySuite.Geometries.Coordinate>? ExtractMissionCoordinates(IndividualMission mission)
        {
            if (mission?.individualMissionInfo == null) return null;

            var info = mission.individualMissionInfo;

            if (info.lineList?.Count > 0)
            {
                return info.lineList
                    .SelectMany(line => line.coordinateList)
                    .Select(c => new NetTopologySuite.Geometries.Coordinate(c.longitude, c.latitude))
                    .ToList();
            }

            if (info.areaList?.Count > 0)
            {
                return info.areaList
                    .SelectMany(area => area.coordinateList)
                    .Select(c => new NetTopologySuite.Geometries.Coordinate(c.longitude, c.latitude))
                    .ToList();
            }

            return null;
        }

        /// <summary>
        /// 두 위경도 지점 간의 거리를 미터 단위로 계산합니다. (피타고라스 근사)
        /// </summary>
        internal static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            double latRad = (lat1 + lat2) / 2.0 * Math.PI / 180.0;
            double dLat   = (lat2 - lat1) * Math.PI / 180.0;
            double dLon   = (lon2 - lon1) * Math.PI / 180.0;
            double x = dLon * Math.Cos(latRad);
            double y = dLat;
            return Math.Sqrt(x * x + y * y) * EarthRadiusMeters;
        }

        /// <summary>
        /// 카메라 발자국이 임무 구역 폴리곤과 교차하는지 확인합니다.
        /// Envelope 사전 검사로 연산량을 최소화합니다.
        /// </summary>
        private static bool IsFootprintInMissionArea(CameraDataLog cameraLog, Geometry missionPolygon, GeometryFactory geometryFactory)
        {
            // 1단계: Bounding Box 빠른 검사
            double minLon = Math.Min(cameraLog.CameraTopLeft.Longitude,  cameraLog.CameraBottomLeft.Longitude);
            double maxLon = Math.Max(cameraLog.CameraTopRight.Longitude, cameraLog.CameraBottomRight.Longitude);
            double minLat = Math.Min(cameraLog.CameraBottomLeft.Latitude,  cameraLog.CameraBottomRight.Latitude);
            double maxLat = Math.Max(cameraLog.CameraTopLeft.Latitude,   cameraLog.CameraTopRight.Latitude);

            if (!missionPolygon.EnvelopeInternal.Intersects(new Envelope(minLon, maxLon, minLat, maxLat)))
                return false;

            // 2단계: 정밀 폴리곤 교차 검사
            var footprintCoords = new NetTopologySuite.Geometries.Coordinate[]
            {
                new(cameraLog.CameraTopLeft.Longitude,    cameraLog.CameraTopLeft.Latitude),
                new(cameraLog.CameraTopRight.Longitude,   cameraLog.CameraTopRight.Latitude),
                new(cameraLog.CameraBottomRight.Longitude,cameraLog.CameraBottomRight.Latitude),
                new(cameraLog.CameraBottomLeft.Longitude, cameraLog.CameraBottomLeft.Latitude),
                new(cameraLog.CameraTopLeft.Longitude,    cameraLog.CameraTopLeft.Latitude),
            };

            try
            {
                return missionPolygon.Intersects(geometryFactory.CreatePolygon(new LinearRing(footprintCoords)));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 임무 구역 폴리곤을 생성합니다. AreaList(면형)와 LineList(선형) 모두 처리합니다.
        /// </summary>
        private static Geometry CreateMissionPolygon(List<MissionDetail> missionDetailList, GeometryFactory geometryFactory)
        {
            var missionPolygonsList = new List<Polygon>();

            try
            {
                foreach (var missionSegment in missionDetailList)
                {
                    // 면형 구역 처리
                    if (missionSegment.AreaList != null)
                    {
                        foreach (var area in missionSegment.AreaList)
                        {
                            if (area.CoordinateList != null && area.CoordinateList.Count >= 3)
                            {
                                var coords = area.CoordinateList
                                    .Select(c => new NetTopologySuite.Geometries.Coordinate(c.Longitude, c.Latitude))
                                    .ToList();

                                if (coords.First() != coords.Last())
                                    coords.Add(coords.First());

                                try { missionPolygonsList.Add(geometryFactory.CreatePolygon(new LinearRing(coords.ToArray()))); }
                                catch (Exception e) { Console.WriteLine($"임무 구역 폴리곤 생성 중 오류: {e.Message}"); }
                            }
                        }
                    }

                    // 선형 구역 처리
                    if (missionSegment.LineList != null)
                    {
                        foreach (var line in missionSegment.LineList)
                        {
                            if (line.CoordinateList != null && line.CoordinateList.Count >= 3)
                            {
                                var coords = line.CoordinateList
                                    .Select(c => new NetTopologySuite.Geometries.Coordinate(c.Longitude, c.Latitude))
                                    .ToList();

                                if (coords.First() != coords.Last())
                                    coords.Add(coords.First());

                                try { missionPolygonsList.Add(geometryFactory.CreatePolygon(new LinearRing(coords.ToArray()))); }
                                catch (Exception e) { Console.WriteLine($"임무 구역 폴리곤 생성 중 오류: {e.Message}"); }
                            }
                        }
                    }
                }

                if (!missionPolygonsList.Any())
                {
                    Console.WriteLine("임무 구역 폴리곤을 찾을 수 없습니다.");
                    return geometryFactory.CreatePolygon();
                }

                // Buffer(0)으로 정규화하여 빌드 구성 무관하게 동일한 토폴로지 보장
                var normalized = missionPolygonsList.Select(g => g.Buffer(0)).Where(g => !g.IsEmpty).ToArray();
                if (!normalized.Any()) return geometryFactory.CreatePolygon();
                var collection = geometryFactory.CreateGeometryCollection(normalized);
                return collection.Union();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"미션 폴리곤 생성 중 오류: {ex.Message}");
                return geometryFactory.CreatePolygon();
            }
        }

        /// <summary>
        /// 임무 구역을 기준으로 고품질/저품질 촬영 지역 폴리곤을 반환합니다.
        /// </summary>
        /// <summary>
        /// getSRData에서 이미 수집한 고품질 폴리곤을 받아서 지역 계산을 수행합니다.
        /// flightDataList 재순회 없이 O(N log N) CascadedUnion으로 병합합니다.
        /// </summary>
        private static (LowQualityRegion lowQuality, LowQualityRegion highQuality) CalculateRegionsFromMission(
            List<MissionDetail> missionDetailList,
            Geometry missionPolygon,
            List<Polygon> preCollectedHighQualityFootprints,
            GeometryFactory gf)
        {
            var lowQualityRegions  = new LowQualityRegion();
            var highQualityRegions = new LowQualityRegion();

            try
            {
                if (missionPolygon == null || missionPolygon.IsEmpty)
                {
                    Console.WriteLine("임무 구역 폴리곤을 찾을 수 없습니다.");
                    return (lowQualityRegions, highQualityRegions);
                }

                // ★ [최적화] 순차 Union O(N²) → CascadedUnion O(N log N)
                Geometry highQualityCoverage = gf.CreatePolygon();
                if (preCollectedHighQualityFootprints.Any())
                {
                    // GeometryCollection.Union() = NetTopologySuite의 CascadedUnion 내부 사용
                    highQualityCoverage = gf
                        .CreateGeometryCollection(preCollectedHighQualityFootprints.Cast<Geometry>().ToArray())
                        .Union();
                }

                // 임무 구역 ∩ 고품질 영역
                Geometry validHighQuality = missionPolygon.Intersection(highQualityCoverage);
                for (int i = 0; i < validHighQuality.NumGeometries; i++)
                {
                    if (validHighQuality.GetGeometryN(i) is Polygon hqPoly && hqPoly.Area > 0)
                    {
                        highQualityRegions[$"HQ_Area{i + 1}"] = hqPoly.ExteriorRing.Coordinates
                            .Select(c => new CoordinateOutput { Latitude = (float)c.Y, Longitude = (float)c.X })
                            .ToList();
                    }
                }

                // 임무 구역 − 고품질 영역 = 저품질 영역
                Geometry lowQualityArea = missionPolygon.Difference(highQualityCoverage);
                for (int i = 0; i < lowQualityArea.NumGeometries; i++)
                {
                    if (lowQualityArea.GetGeometryN(i) is Polygon lqPoly && lqPoly.Area > 0)
                    {
                        lowQualityRegions[$"Area{i + 1}"] = lqPoly.ExteriorRing.Coordinates
                            .Select(c => new CoordinateOutput { Latitude = (float)c.Y, Longitude = (float)c.X })
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"저품질 영역 계산 중 오류: {ex.Message}");
            }

            return (lowQualityRegions, highQualityRegions);
        }
    }
}
