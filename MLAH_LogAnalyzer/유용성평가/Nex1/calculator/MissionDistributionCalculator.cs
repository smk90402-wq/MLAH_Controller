using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MLAH_LogAnalyzer
{
    public class MissionDistributionResult
    {
        /// <summary>UAV별 임무 일시정지 구간 (Key: "UAV4" 등)</summary>
        public Dictionary<string, List<PauseTimeRange>> MissionPauseTimestamp { get; set; } = new();
        public uint Score { get; set; }
    }

    public static class MissionDistributionCalculator
    {
        // ──────────────────────────────────────────────────────────────────────
        // 공개 API (ScenarioData 오버로드)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>임무 분배 효과도 점수만 빠르게 계산합니다.</summary>
        public static async Task<uint?> getMissionDistributionScore(ScenarioData scenarioData)
        {
            try
            {
                if (scenarioData == null) return null;
                return getMissionDistributionScore(scenarioData.FlightData, scenarioData.MissionDetail);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>임무 분배 효과도 상세 분석 결과를 반환합니다.</summary>
        public static async Task<MissionDistributionResult?> getMissionDistributionData(ScenarioData scenarioData)
        {
            if (scenarioData == null)
            {
                Console.WriteLine("시나리오 데이터를 로드할 수 없어 임무 분배 효과도 분석을 수행할 수 없습니다.");
                return null;
            }
            return getMissionDistributionData(scenarioData.FlightData, scenarioData.MissionDetail);
        }

        // ──────────────────────────────────────────────────────────────────────
        // 공개 API (원시 데이터 오버로드)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// (각 UAV 운용 시간 합계 - 일시정지 시간) / 운용 시간 합계 * 100
        /// </summary>
        public static uint getMissionDistributionScore(List<FlightData> flightDataList, List<MissionDetail> missionDetail)
        {
            if (missionDetail    == null || !missionDetail.Any())    return 100;
            if (flightDataList   == null || !flightDataList.Any())   return 100;

            // UAV 데이터(AircraftID > 3)만 필터링하여 UAV별 운용 시간 합산
            var uavGroups = flightDataList
                .Where(f => f.AircraftID > 3)
                .GroupBy(f => f.AircraftID)
                .ToDictionary(g => g.Key, g => g.OrderBy(f => f.Timestamp).ToList());

            if (!uavGroups.Any()) return 100;

            ulong totalOperationTime = 0;
            foreach (var group in uavGroups.Values)
            {
                var timestamps = group.Select(f => f.Timestamp).Where(t => t > 0).ToList();
                if (timestamps.Count >= 2)
                    totalOperationTime += timestamps.Last() - timestamps.First();
            }

            if (totalOperationTime == 0) return 100;

            // 모든 MissionDetail에서 UAV별 일시정지 시간 합산
            ulong totalPauseTime = 0;
            foreach (var mission in missionDetail)
            {
                if (mission.MissionPauseTimeStamp == null) continue;

                var uavPauseLists = new[]
                {
                    mission.MissionPauseTimeStamp.UAV4,
                    mission.MissionPauseTimeStamp.UAV5,
                    mission.MissionPauseTimeStamp.UAV6,
                };

                foreach (var pauseRanges in uavPauseLists)
                {
                    if (pauseRanges == null) continue;
                    foreach (var range in pauseRanges)
                    {
                        if (range.End > range.Start)
                            totalPauseTime += range.End - range.Start;
                    }
                }
            }

            if (totalPauseTime > totalOperationTime) return 0;

            double efficiency = (double)(totalOperationTime - totalPauseTime) / totalOperationTime * 100;
            return (uint)Math.Max(0, Math.Min(100, Math.Round(efficiency, MidpointRounding.AwayFromZero)));
        }

        public static MissionDistributionResult getMissionDistributionData(List<FlightData> flightDataList, List<MissionDetail> missionDetail)
        {
            Console.WriteLine("\n--- Mission Distribution Analysis ---");

            var result = new MissionDistributionResult();

            if (flightDataList == null || !flightDataList.Any())
            {
                Console.WriteLine("비행 데이터가 없습니다.");
                return result;
            }

            if (missionDetail == null || !missionDetail.Any())
            {
                Console.WriteLine("MissionDetail 데이터가 없습니다.");
                return result;
            }

            // UAV별 일시정지 타임스탬프 수집
            foreach (var mission in missionDetail)
            {
                if (mission.MissionPauseTimeStamp == null) continue;

                var uavPauseData = new[]
                {
                    new { Name = "UAV4", Ranges = mission.MissionPauseTimeStamp.UAV4 },
                    new { Name = "UAV5", Ranges = mission.MissionPauseTimeStamp.UAV5 },
                    new { Name = "UAV6", Ranges = mission.MissionPauseTimeStamp.UAV6 },
                };

                foreach (var uav in uavPauseData)
                {
                    if (uav.Ranges == null) continue;

                    if (!result.MissionPauseTimestamp.ContainsKey(uav.Name))
                        result.MissionPauseTimestamp[uav.Name] = new List<PauseTimeRange>();

                    foreach (var range in uav.Ranges)
                        result.MissionPauseTimestamp[uav.Name].Add(new PauseTimeRange { Start = range.Start, End = range.End });
                }
            }

            result.Score = getMissionDistributionScore(flightDataList, missionDetail);
            return result;
        }
    }
}
