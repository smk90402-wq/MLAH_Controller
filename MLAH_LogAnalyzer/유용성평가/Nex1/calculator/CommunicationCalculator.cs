using BitMiracle.LibTiff.Classic;
using MLAH_LogAnalyzer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MLAH_LogAnalyzer
{
    public class CommunicationDataOutput
    {
        public DateTime Timestamp { get; set; }
        public uint AircraftID { get; set; } // UAV 4, 5, 6 식별자
        public int Status { get; set; }
    }

    public class CommunicationResult
    {
        public List<CommunicationDataOutput> CommunicationDatas { get; set; } = new List<CommunicationDataOutput>();
        public Dictionary<string, List<ulong>> LOSFalseTimestamps { get; set; } = new Dictionary<string, List<ulong>>();
        public uint Score { get; set; }
    }

    /// <summary>
    /// 통신가용도 계산기
    /// - 헬기(지휘기) -> 무인기 간 통신 LOS 판정
    /// - RealLAHData의 loss_uav4/5/6 필드 기반 (1=LOS, 0=no LOS)
    /// - 점수 = (LOS 성공 타임스탬프 / 전체 타임스탬프) x 100
    /// - 헬기 LOS와 UAV 타임스탬프가 정확히 일치하지 않으므로 최근접 매칭 사용
    /// </summary>
    public static class CommunicationCalculator
    {
        private static readonly DateTime Epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // 최근접 매칭 허용 오차 (ms). 이 범위 밖이면 매칭하지 않음.
        private const long MaxTimestampDiffMs = 1000;

        /// <summary>
        /// 헬기 LOS 데이터에서 주어진 타임스탬프에 가장 가까운 항목을 이진 탐색으로 찾음
        /// </summary>
        private static FlightData? FindClosestLahEntry(ulong timestamp, List<FlightData> lahSorted, List<ulong> lahTimestamps)
        {
            int idx = lahTimestamps.BinarySearch(timestamp);
            if (idx >= 0) return lahSorted[idx]; // 정확 일치

            idx = ~idx;
            if (idx == 0)
            {
                long diff = Math.Abs((long)timestamp - (long)lahTimestamps[0]);
                return diff <= MaxTimestampDiffMs ? lahSorted[0] : null;
            }
            if (idx == lahTimestamps.Count)
            {
                long diff = Math.Abs((long)timestamp - (long)lahTimestamps[^1]);
                return diff <= MaxTimestampDiffMs ? lahSorted[^1] : null;
            }

            long diffBefore = Math.Abs((long)timestamp - (long)lahTimestamps[idx - 1]);
            long diffAfter = Math.Abs((long)timestamp - (long)lahTimestamps[idx]);
            long minDiff = Math.Min(diffBefore, diffAfter);

            if (minDiff > MaxTimestampDiffMs) return null;

            return diffBefore <= diffAfter ? lahSorted[idx - 1] : lahSorted[idx];
        }

        /// <summary>
        /// 통신가용도 점수 계산 (ScenarioData 래퍼)
        /// </summary>
        public static uint? getCommunicationScore(ScenarioData scenarioData, uint commandAircraftID = 1)
        {
            try
            {
                if (scenarioData == null)
                    return null;

                return getCommunicationScore(scenarioData.FlightData, commandAircraftID);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 통신가용도 상세 분석 (ScenarioData 래퍼)
        /// </summary>
        public static CommunicationResult? getCommunicationData(ScenarioData scenarioData, uint commandAircraftID = 1)
        {
            if (scenarioData == null)
            {
                Console.WriteLine($"시나리오 데이터를 로드할 수 없어 통신 가용성 분석을 수행할 수 없습니다.");
                return null;
            }

            return getCommunicationData(scenarioData.FlightData, commandAircraftID);
        }

        /// <summary>
        /// 통신가용도 점수 계산 (RealLAHData LOS 필드 기반, 최근접 매칭)
        /// </summary>
        public static uint getCommunicationScore(List<FlightData> flightDataList, uint commandAircraftID = 1)
        {
            // 헬기 LOS 데이터 (타임스탬프 정렬)
            var lahSorted = flightDataList
                .Where(f => f.AircraftID == commandAircraftID && f.LosUav4.HasValue)
                .GroupBy(f => f.Timestamp)
                .Select(g => g.First())
                .OrderBy(f => f.Timestamp)
                .ToList();

            if (!lahSorted.Any()) return 0;

            var lahTimestamps = lahSorted.Select(f => f.Timestamp).ToList();

            int totalTimestamps = 0;
            int totalConnected = 0;

            var uavLosSelectors = new Dictionary<uint, Func<FlightData, int?>>
            {
                { 4, f => f.LosUav4 },
                { 5, f => f.LosUav5 },
                { 6, f => f.LosUav6 }
            };

            foreach (var (uavId, losSelector) in uavLosSelectors)
            {
                var uavEntries = flightDataList
                    .Where(f => f.AircraftID == uavId && f.Timestamp > 0)
                    .OrderBy(f => f.Timestamp)
                    .ToList();

                foreach (var uavEntry in uavEntries)
                {
                    var closest = FindClosestLahEntry(uavEntry.Timestamp, lahSorted, lahTimestamps);
                    if (closest == null) continue;

                    int? los = losSelector(closest);
                    if (!los.HasValue) continue;

                    totalTimestamps++;
                    if (los.Value == 1) totalConnected++;
                }
            }

            return totalTimestamps > 0 ? (uint)Math.Round((double)totalConnected / totalTimestamps * 100, MidpointRounding.AwayFromZero) : 0;
        }

        /// <summary>
        /// 통신가용도 상세 분석 (타임라인, LOS false 타임스탬프 등, 최근접 매칭)
        /// </summary>
        public static CommunicationResult getCommunicationData(List<FlightData> flightDataList, uint commandAircraftID = 1)
        {
            var result = new CommunicationResult();

            if (!flightDataList.Any())
            {
                System.Diagnostics.Debug.WriteLine("[통신가용도] FlightData 비어있음");
                return result;
            }

            // 헬기 LOS 데이터 (타임스탬프 정렬, 이진 탐색용)
            var lahSorted = flightDataList
                .Where(f => f.AircraftID == commandAircraftID && f.LosUav4.HasValue)
                .GroupBy(f => f.Timestamp)
                .Select(g => g.First())
                .OrderBy(f => f.Timestamp)
                .ToList();

            if (!lahSorted.Any())
            {
                System.Diagnostics.Debug.WriteLine("[통신가용도] LOS 데이터 없음");
                return result;
            }

            var lahTimestamps = lahSorted.Select(f => f.Timestamp).ToList();
            System.Diagnostics.Debug.WriteLine($"[통신가용도] 헬기 LOS: {lahSorted.Count}개, 범위=[{lahTimestamps.First()} ~ {lahTimestamps.Last()}]");

            var uavLosSelectors = new Dictionary<uint, Func<FlightData, int?>>
            {
                { 4, f => f.LosUav4 },
                { 5, f => f.LosUav5 },
                { 6, f => f.LosUav6 }
            };

            int totalTimestamps = 0;
            int totalConnected = 0;

            foreach (var (uavId, losSelector) in uavLosSelectors)
            {
                string uavKey = $"UAV{uavId}";
                result.LOSFalseTimestamps[uavKey] = new List<ulong>();

                var uavEntries = flightDataList
                    .Where(f => f.AircraftID == uavId && f.Timestamp > 0)
                    .OrderBy(f => f.Timestamp)
                    .ToList();

                if (!uavEntries.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[통신가용도] UAV{uavId} 데이터 없음");
                    continue;
                }

                System.Diagnostics.Debug.WriteLine($"[통신가용도] UAV{uavId}: {uavEntries.Count}개, 범위=[{uavEntries.First().Timestamp} ~ {uavEntries.Last().Timestamp}]");

                int uavConnected = 0;
                int uavMatched = 0;

                foreach (var uavEntry in uavEntries)
                {
                    var closest = FindClosestLahEntry(uavEntry.Timestamp, lahSorted, lahTimestamps);
                    if (closest == null) continue;

                    int? los = losSelector(closest);
                    if (!los.HasValue) continue;

                    uavMatched++;
                    bool hasLos = los.Value == 1;

                    if (!hasLos)
                        result.LOSFalseTimestamps[uavKey].Add(uavEntry.Timestamp);
                    else
                        uavConnected++;

                    result.CommunicationDatas.Add(new CommunicationDataOutput
                    {
                        Timestamp = Epoch.AddMilliseconds(uavEntry.Timestamp),
                        AircraftID = uavId,
                        Status = hasLos ? 1 : 0
                    });
                }

                totalTimestamps += uavMatched;
                totalConnected += uavConnected;

                System.Diagnostics.Debug.WriteLine($"[통신가용도] 지휘기↔UAV{uavId}: 매칭={uavMatched}, 연결={uavConnected}, 실패={uavMatched - uavConnected}");
            }

            result.Score = totalTimestamps > 0 ? (uint)Math.Round((double)totalConnected / totalTimestamps * 100, MidpointRounding.AwayFromZero) : 0;
            System.Diagnostics.Debug.WriteLine($"[통신가용도] 최종: 연결={totalConnected}, 전체={totalTimestamps}, Score={result.Score}%");

            return result;
        }

        // SRTM LOS 관련 (안전도, 성능분석 등 외부에서 참조)

        private static Dictionary<string, float[,]> _elevationCache = new Dictionary<string, float[,]>();
        private static Dictionary<string, (double minLat, double maxLat, double minLon, double maxLon, int rows, int cols)> _geoInfoCache = new Dictionary<string, (double, double, double, double, int, int)>();

        public static void ClearCaches()
        {
            _elevationCache.Clear();
            _geoInfoCache.Clear();
        }

        public static async Task<bool> CheckLineOfSight(float lat1, float lon1, float alt1, float lat2, float lon2, float alt2, string srtmFilePath, int numSamples = 1000)
        {
            return await Task.Run(() => CheckLineOfSightSync(lat1, lon1, alt1, lat2, lon2, alt2, srtmFilePath, numSamples));
        }

        private const double EarthRadiusKm = 6371.0;
        private static double ToRadians(double angle) => Math.PI * angle / 180.0;

        private static double FastCalculateDistance(float lat1, float lon1, float lat2, float lon2)
        {
            double x = ToRadians(lon2 - lon1) * Math.Cos(ToRadians((lat1 + lat2) / 2));
            double y = ToRadians(lat2 - lat1);
            return Math.Sqrt(x * x + y * y) * EarthRadiusKm;
        }

        private static bool CheckLineOfSightSync(float lat1, float lon1, float alt1, float lat2, float lon2, float alt2, string srtmFilePath, int numSamples)
        {
            try
            {
                if (!LoadSRTMData(srtmFilePath))
                {
                    Console.WriteLine($"SRTM 파일을 로드할 수 없습니다: {srtmFilePath}");
                    return false;
                }

                double totalDistance = FastCalculateDistance(lat1, lon1, lat2, lon2);
                int optimalSamples = (int)Math.Max(10, totalDistance / 30.0);

                for (int i = 0; i < optimalSamples; i++)
                {
                    float fraction = optimalSamples > 1 ? (float)i / (optimalSamples - 1) : 0f;
                    float currentLat = lat1 + fraction * (lat2 - lat1);
                    float currentLon = lon1 + fraction * (lon2 - lon1);
                    float lineAltitude = alt1 + fraction * (alt2 - alt1);

                    float? terrainElevation = GetElevation(currentLat, currentLon, srtmFilePath);

                    if (!terrainElevation.HasValue)
                        return false;

                    if (terrainElevation.Value > lineAltitude)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LOS 계산 중 오류 발생: {ex.Message}");
                return false;
            }
        }

        public static float? GetElevation(float latitude, float longitude, string srtmFilePath)
        {
            try
            {
                if (!_elevationCache.ContainsKey(srtmFilePath) || !_geoInfoCache.ContainsKey(srtmFilePath))
                    return null;

                var elevationData = _elevationCache[srtmFilePath];
                var geoInfo = _geoInfoCache[srtmFilePath];

                double latRange = geoInfo.maxLat - geoInfo.minLat;
                double lonRange = geoInfo.maxLon - geoInfo.minLon;

                double latRatio = (geoInfo.maxLat - (double)latitude) / latRange;
                double lonRatio = ((double)longitude - geoInfo.minLon) / lonRange;

                int row = (int)(latRatio * (geoInfo.rows - 1));
                int col = (int)(lonRatio * (geoInfo.cols - 1));

                if (row < 0 || row >= geoInfo.rows || col < 0 || col >= geoInfo.cols)
                    return null;

                return elevationData[row, col];
            }
            catch
            {
                return null;
            }
        }

        private static bool LoadSRTMData(string srtmFilePath)
        {
            if (_elevationCache.ContainsKey(srtmFilePath))
                return true;

            try
            {
                if (!File.Exists(srtmFilePath))
                {
                    Console.WriteLine($"SRTM 파일이 존재하지 않습니다: {srtmFilePath}");
                    return false;
                }

                var originalOut = Console.Out;
                var originalErr = Console.Error;
                try
                {
                    Console.SetOut(TextWriter.Null);
                    Console.SetError(TextWriter.Null);

                    using (Tiff tiff = Tiff.Open(srtmFilePath, "r"))
                    {
                        if (tiff == null) return false;

                        int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                        int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

                        double minLon = 125.0, maxLat = 40.0, maxLon = 130.0, minLat = 35.0;

                        var elevationData = new float[height, width];

                        for (int row = 0; row < height; row++)
                        {
                            byte[] scanline = new byte[tiff.ScanlineSize()];
                            tiff.ReadScanline(scanline, row);

                            for (int col = 0; col < width; col++)
                            {
                                if (col * 2 + 1 < scanline.Length)
                                {
                                    short elevation = (short)(scanline[col * 2] | (scanline[col * 2 + 1] << 8));
                                    if (elevation == -32768) elevation = 0;
                                    elevationData[row, col] = elevation;
                                }
                            }
                        }

                        _elevationCache[srtmFilePath] = elevationData;
                        _geoInfoCache[srtmFilePath] = (minLat, maxLat, minLon, maxLon, height, width);

                        return true;
                    }
                }
                finally
                {
                    Console.SetOut(originalOut);
                    Console.SetError(originalErr);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SRTM 데이터 로드 중 오류 발생: {ex.Message}");
                return false;
            }
        }
    }
}
