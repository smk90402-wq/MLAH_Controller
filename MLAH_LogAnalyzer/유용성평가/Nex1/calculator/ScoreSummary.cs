using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MLAH_LogAnalyzer
{
    /// <summary>시나리오별 분석 점수 요약</summary>
    public class ScenarioScores
    {
        public uint  ScenarioNumber           { get; set; }
        public uint? CoverageScore            { get; set; }
        public uint? CommunicationScore       { get; set; }
        public uint? SafetyScore              { get; set; }
        public uint? MissionDistributionScore { get; set; }
        public uint? SpatialResolutionScore   { get; set; }
        public uint? OverallAverage           { get; set; }
        public bool  HasErrors                { get; set; }
    }

    public static class ScoreSummary
    {
        // 이 클래스는 ScenarioScores 모델의 네임스페이스 홈입니다.
        // 전체 점수 집계 로직은 Program.cs 또는 호출 측에서 직접 구현합니다.
    }
}