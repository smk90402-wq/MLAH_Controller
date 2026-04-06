using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MLAH_LogAnalyzer
{
    // ---------------------------------------------------------
    // [1] 분석 결과 컨테이너
    // ---------------------------------------------------------
    public class TargetAnalysisResult
    {
        // [1] 상세 데이터 (GridControl 바인딩용 - 표적별 O/X)
        public List<TargetGridItem> GridDataList { get; set; } = new List<TargetGridItem>();

        // [2] 요약 데이터 (Main 점수 표시용 - 기존 속성 복구)
        public double AchievementRate { get; set; } // 성취도 (%)
        public int TotalTargetCount { get; set; }
        public int DestroyedCount { get; set; }
        public int DetectedCount { get; set; }
        public int IdentifiedCount { get; set; }

        // [3] 차트 및 맵 데이터
        public List<TargetChartItem> ChartData { get; set; } = new List<TargetChartItem>();
        public Dictionary<uint, List<Target>> TargetPaths { get; set; } = new Dictionary<uint, List<Target>>();
        public Dictionary<uint, Dictionary<ulong, int>> TargetStateHistory { get; set; } = new Dictionary<uint, Dictionary<ulong, int>>();
    }

    // ---------------------------------------------------------
    // [2] 계산기 로직
    // ---------------------------------------------------------
    public static class TargetAchievementCalculator
    {
        // Timestamp(ulong)을 DateTime으로 변환하기 위한 기준 시간
        private static readonly DateTime Epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// RealTargetData 리스트를 받아 분석 결과(TargetAnalysisResult)를 반환
        /// </summary>
        public static TargetAnalysisResult Analyze(List<RealTargetData> logs)
        {
            var result = new TargetAnalysisResult();

            if (logs == null || !logs.Any())
            {
                return result;
            }

            try
            {
                // 타겟별 최종 상태를 저장하기 위한 딕셔너리 (Key: ID, Value: Max State)
                var finalStates = new Dictionary<uint, int>();
                var uniqueTargetIds = new HashSet<uint>();

                // B. 데이터 순회 및 상태 분석
                foreach (var log in logs)
                {
                    // TargetList가 없으면 건너뜀
                    if (log.TargetList == null) continue;

                    foreach (var target in log.TargetList)
                    {
                        uniqueTargetIds.Add(target.ID);

                        // 1. 맵 경로 데이터 적재
                        if (!result.TargetPaths.ContainsKey(target.ID))
                        {
                            result.TargetPaths[target.ID] = new List<Target>();
                        }
                        // Target 객체 자체를 리스트에 추가 (위/경/고도 정보 포함됨)
                        result.TargetPaths[target.ID].Add(target);

                        // 2. 상태 결정 (Status 속성 하나로 판단)
                        int state = GetTargetState(target);

                        // 3. 이력(History) 저장
                        if (!result.TargetStateHistory.ContainsKey(target.ID))
                        {
                            result.TargetStateHistory[target.ID] = new Dictionary<ulong, int>();
                        }

                        // 같은 시간대에 중복 데이터가 있을 수 있으므로 체크
                        if (!result.TargetStateHistory[target.ID].ContainsKey(log.Timestamp))
                        {
                            result.TargetStateHistory[target.ID][log.Timestamp] = state;
                        }
                        else
                        {
                            // 이미 있다면 더 높은 상태값(우선순위가 높은 것)으로 갱신
                            if (state > result.TargetStateHistory[target.ID][log.Timestamp])
                                result.TargetStateHistory[target.ID][log.Timestamp] = state;
                        }

                        // 4. 최종 상태 갱신 (통계용, 가장 높은 상태값을 유지)
                        if (!finalStates.ContainsKey(target.ID)) finalStates[target.ID] = 0;
                        if (state > finalStates[target.ID]) finalStates[target.ID] = state;

                        // 5. 차트 아이템 생성
                        // 차트 X축은 DateTime을 사용하므로 ulong Timestamp 변환
                        DateTime chartTime = Epoch.AddMilliseconds(log.Timestamp).ToLocalTime();

                        result.ChartData.Add(new TargetChartItem
                        {
                            Timestamp = chartTime,
                            TargetID = (int)target.ID, // ChartItem이 int를 쓴다면 캐스팅
                            State = state
                        });
                    }
                }

                // C. 그리드 데이터 생성 (표적별 최종 상태)
                result.TotalTargetCount = uniqueTargetIds.Count;
                result.GridDataList = new List<TargetGridItem>();

                //foreach (var id in uniqueTargetIds)
                //{
                //    // 해당 표적의 최종 상태 (Dictionary에 저장된 값)
                //    int finalState = finalStates.ContainsKey(id) ? finalStates[id] : 0;

                //    result.GridDataList.Add(new TargetGridItem
                //    {
                //        TargetID = id,
                //        FinalState = finalState
                //    });
                //}
                double totalScoreSum = 0; // 점수 총합 변수

                foreach (var id in uniqueTargetIds)
                {
                    int finalState = finalStates.ContainsKey(id) ? finalStates[id] : 0;

                    // 점수 산정 로직 적용
                    double targetScore = 0;
                    switch (finalState)
                    {
                        case 3: targetScore = 100; break; // 파괴
                        case 2: targetScore = 70; break; // 식별 (기존 Detect)
                        case 1: targetScore = 50; break; // 탐지 (기존 Recognized)
                        default: targetScore = 0; break; // 미식별
                    }
                    totalScoreSum += targetScore;

                    result.GridDataList.Add(new TargetGridItem
                    {
                        TargetID = id,
                        FinalState = finalState
                    });
                }

                // ID순 정렬
                result.GridDataList = result.GridDataList.OrderBy(x => x.TargetID).ToList();

                // D. [복구됨] 전체 요약 점수 계산 (Main에서 사용할 Score)
                result.TotalTargetCount = result.GridDataList.Count;

                // 각 상태별 개수 집계 (FinalState 기준)
                result.DestroyedCount = result.GridDataList.Count(x => x.FinalState == 3);
                result.DetectedCount = result.GridDataList.Count(x => x.FinalState == 2);
                result.IdentifiedCount = result.GridDataList.Count(x => x.FinalState == 1);

                // 종합 점수 계산 공식 적용
                // 공식: (타겟별 점수 평균) - (유인기 파괴 페널티)

                // 현재 데이터에는 유인기 파괴 정보가 없으므로 0으로 가정
                int destroyedMannedAircraftCount = 0;

                // [수정] 페널티 로직 구체화
                // 유인기(LAH)가 총 3대라고 가정할 때, 1대당 1/3(약 33.3점) 감점
                // 1/3 * 파괴된 수 -> (100.0 / 3.0) * 파괴된 수
                double penaltyPerUnit = 100.0 / 3.0;
                double totalPenalty = penaltyPerUnit * destroyedMannedAircraftCount;

                if (result.TotalTargetCount > 0)
                {
                    double averageScore = totalScoreSum / result.TotalTargetCount; // 획득 점수 (0~100)

                    // 획득 점수에서 페널티 차감 (0점 미만일 경우 0점으로 처리)
                    result.AchievementRate = Math.Max(0, averageScore - totalPenalty);
                }
                else
                {
                    result.AchievementRate = 0.0;
                }

                return result;


                //int successSum = result.DestroyedCount + result.DetectedCount + result.IdentifiedCount;

                //result.AchievementRate = result.TotalTargetCount > 0
                //    ? (double)successSum / result.TotalTargetCount * 100.0
                //    : 0.0;

                //return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Target Analysis Error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Target 모델의 Status 값을 기반으로 분석용 상태값(int) 반환
        /// </summary>
        private static int GetTargetState(Target target)
        {
            // 모델의 Status(uint)를 그대로 int로 변환하여 반환
            // (필요 시 여기서 1, 2, 3 값에 대한 매핑 로직 추가 가능)

            // 예시: 
            // 3: Destroyed (파괴)
            // 2: Detected/Tracking (탐지)
            // 1: Identified (식별)
            // 0: None

            return (int)target.Status;
        }
    }
}