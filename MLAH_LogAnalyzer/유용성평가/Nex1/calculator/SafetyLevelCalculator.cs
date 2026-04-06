using MLAH_LogAnalyzer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MLAH_LogAnalyzer
{
    /// <summary>
    /// JSON 역직렬화 시 Target의 Status 값을 int로 변환하는 컨버터.
    /// 숫자("3"), 문자열("STATUS_DEATH") 등 다양한 형태를 지원합니다.
    /// </summary>
    public class TargetStatusConverter : JsonConverter
    {
        private static readonly Dictionary<string, int> StatusMap = new()
        {
            ["STATUS_DEATH"]  = 3,
            ["STATUS_DETECT"] = 2,
            ["STATUS_RECOG"]  = 1,
            ["STATUS_WAIT"]   = 0,
        };

        public override bool CanConvert(Type objectType) =>
            objectType == typeof(int) || objectType == typeof(int?);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)    return 0;
            if (reader.TokenType == JsonToken.Integer) return Convert.ToInt32(reader.Value);

            if (reader.TokenType == JsonToken.String)
            {
                string s = reader.Value.ToString();
                if (int.TryParse(s, out int n)) return n;
                return StatusMap.TryGetValue(s, out int v) ? v : 0;
            }

            return 0;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            writer.WriteValue(value);
    }

    public class SafetyResult
    {
        /// <summary>기체별 위협 노출 타임스탬프 (Key: "LAH1" / "UAV4" 등)</summary>
        public Dictionary<string, List<ulong>> ThreatenedTimestamps { get; set; } = new();
        /// <summary>안전한 시간 / 전체 비행 시간 (%)</summary>
        public uint Score { get; set; }
    }

    public static class SafetyLevelCalculator
    {
        // ──────────────────────────────────────────────────────────────────────
        // 공개 API (ScenarioData 오버로드)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>위협 노출 분석 결과를 반환합니다.</summary>
        public static SafetyResult? getSafetyData(string baseDirectory, ScenarioData scenarioData)
        {
            if (scenarioData == null)
            {
                Console.WriteLine("시나리오 데이터를 로드할 수 없어 위협 노출 분석을 수행할 수 없습니다.");
                return null;
            }

            return getSafetyData(scenarioData.FlightData, scenarioData.RealTargetData);
        }

        /// <summary>안전성 점수만 빠르게 계산합니다.</summary>
        public static uint? getSafetyScore(string baseDirectory, ScenarioData scenarioData)
        {
            try
            {
                if (scenarioData == null) return null;
                return getSafetyScore(scenarioData.FlightData, scenarioData.RealTargetData);
            }
            catch
            {
                return null;
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // 공개 API (원시 데이터 오버로드)
        // ──────────────────────────────────────────────────────────────────────

        public static uint getSafetyScore(List<FlightData> flightDataList, List<RealTargetData> realTargetDataList)
        {
            var sortedTargets     = realTargetDataList.OrderBy(x => x.Timestamp).ToList();
            var mannedFlightData  = flightDataList.Where(e => e.AircraftID < 4).ToList();

            int total = 0;
            int safe  = 0;

            foreach (var entry in mannedFlightData)
            {
                if (entry.Timestamp == 0 || entry.FlightDataLog == null) continue;

                var targets = FindClosestTargetEntry(entry.Timestamp, sortedTargets);
                total++;

                if (!targets.Any()) { safe++; continue; }

                bool exposed = IsExposedToThreat(entry.AircraftID, targets);
                if (!exposed) safe++;
            }

            return total > 0 ? (uint)Math.Round((double)safe / total * 100, MidpointRounding.AwayFromZero) : 100;
        }

        public static SafetyResult getSafetyData(List<FlightData> flightDataList, List<RealTargetData> realTargetDataList)
        {
            Console.WriteLine("\n--- Threat Exposure Analysis ---");

            var result = new SafetyResult();

            if (flightDataList == null || !flightDataList.Any())
            {
                Console.WriteLine("위협 노출 분석을 위한 비행 데이터가 없습니다.");
                return result;
            }

            // 표적이 없으면 100점
            if (realTargetDataList == null || !realTargetDataList.Any())
            {
                Console.WriteLine("위협 노출 분석을 위한 목표물 데이터가 없습니다. 안전으로 간주.");

                foreach (var group in flightDataList.GroupBy(f => f.AircraftID))
                {
                    string key = group.Key <= 3 ? $"LAH{group.Key}" : $"UAV{group.Key}";
                    result.ThreatenedTimestamps[key] = new List<ulong>();
                }

                int validTotal = flightDataList.Count(fd => fd.FlightDataLog != null);
                result.Score = validTotal > 0 ? 100u : 0u;
                return result;
            }

            var sortedTargets = realTargetDataList.OrderBy(x => x.Timestamp).ToList();
            var flightDataById = flightDataList.GroupBy(f => f.AircraftID).ToDictionary(g => g.Key, g => g.ToList());

            int totalTs = 0;
            int safeTimes  = 0;

            foreach (var (aircraftId, flights) in flightDataById)
            {
                if (aircraftId > 3) continue; // 유인기만 분석

                string key = $"LAH{aircraftId}";
                result.ThreatenedTimestamps[key] = new List<ulong>();

                foreach (var entry in flights.OrderBy(f => f.Timestamp))
                {
                    ulong ts = (ulong)entry.Timestamp;
                    if (ts == 0 || entry.FlightDataLog == null) continue;

                    var targets = FindClosestTargetEntry(ts, sortedTargets);
                    totalTs++;

                    if (!targets.Any()) { safeTimes++; continue; }

                    bool exposed = IsExposedToThreat(aircraftId, targets);
                    if (exposed)
                        result.ThreatenedTimestamps[key].Add(ts);
                    else
                        safeTimes++;
                }
            }

            result.Score = totalTs > 0 ? (uint)Math.Round((double)safeTimes / totalTs * 100, MidpointRounding.AwayFromZero) : 0;
            return result;
        }

        // ──────────────────────────────────────────────────────────────────────
        // 내부 헬퍼
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 표적의 LAHxLOS 필드로 위협 노출 판정.
        /// 표적이 해당 헬기를 볼 수 있으면(LOS=true) 위협 노출.
        /// </summary>
        private static bool IsExposedToThreat(uint aircraftId, List<Target> targets)
        {
            foreach (var target in targets)
            {
                bool hasLos = aircraftId switch
                {
                    1 => target.LAH1LOS,
                    2 => target.LAH2LOS,
                    3 => target.LAH3LOS,
                    _ => false
                };

                if (hasLos) return true;
            }
            return false;
        }

        public static List<Target> FindClosestTargetEntry(ulong flightTimestamp, List<RealTargetData> sortedTargetDataList)
        {
            if (!sortedTargetDataList.Any()) return new List<Target>();

            // 100억 미만이면 초(sec) 단위로 간주 → ms 변환
            var targetTimestamps = sortedTargetDataList
                .Select(e => { ulong ts = (ulong)e.Timestamp; return ts < 10_000_000_000 ? ts * 1000 : ts; })
                .ToList();

            int idx = targetTimestamps.BinarySearch(flightTimestamp);
            if (idx < 0) idx = ~idx;

            if (idx == 0)                          return sortedTargetDataList[0].TargetList ?? new List<Target>();
            if (idx == sortedTargetDataList.Count) return sortedTargetDataList[^1].TargetList ?? new List<Target>();

            long diffBefore = Math.Abs((long)flightTimestamp - (long)targetTimestamps[idx - 1]);
            long diffAfter  = Math.Abs((long)flightTimestamp - (long)targetTimestamps[idx]);

            return (diffBefore <= diffAfter
                ? sortedTargetDataList[idx - 1]
                : sortedTargetDataList[idx]).TargetList ?? new List<Target>();
        }
    }
}
