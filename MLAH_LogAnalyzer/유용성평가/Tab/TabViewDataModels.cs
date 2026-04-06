using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;

namespace MLAH_LogAnalyzer
{
    public class ProgressState
    {
        public int Percentage { get; set; }
        public string Message { get; set; }
    }

    public class ScoreScenarioItem : CommonBase
    {
     

        public ulong StartTime { get; set; } 
        public ulong EndTime { get; set; }   

        public string ScenarioName { get; set; }
        public int ScenarioNumber { get; set; } // Scenario1.json -> 1
        public string FullPath { get; set; } // logs_dummy/Scenario1.json

        // 분석 결과 저장
        public AnalysisResult CoverageAnalysisResult { get; set; }
        public CommunicationResult CommunicationAnalysisResult { get; set; }
        public SafetyResult SafetyAnalysisResult { get; set; }
        public MissionDistributionResult MissionDistResult { get; set; }
         public SpatialResolutionResult SpatialResResult { get; set; } // (가상)
        public TargetAnalysisResult TargetAnalysisResult { get; set; }
        public uint CommScore { get; set; }
        public uint SafetyScore { get; set; }
        public uint MissionDistScore { get; set; }
        public uint MissionSuccessScore { get; set; }
        public uint SpatialResScore { get; set; } // (가상: 촬영 유효도)
        public uint CoverageScore { get; set; }   // (관측 달성도)

        // 분석 완료 여부
        private bool _isAnalyzed = false;
        public bool IsAnalyzed
        {
            get => _isAnalyzed;
            set
            {
                if (_isAnalyzed != value)
                {
                    _isAnalyzed = value;
                    OnPropertyChanged(nameof(IsAnalyzed));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(IsCheckboxEnabled)); // 분석 상태에 따라 체크박스 활성/비활성
                }
            }
        }

        // UI 바인딩용 텍스트 색상 (미분석: Red, 분석됨: Blue)
        public Brush StatusColor => IsAnalyzed ? Brushes.DodgerBlue : Brushes.Red;

        // 체크박스 활성화 여부 (분석된 것만 체크 가능)
        public bool IsCheckboxEnabled => IsAnalyzed;

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                // [수정] 분석되지 않은 항목은 체크 불가하도록 강제
                if (!IsAnalyzed && value == true)
                    return;

                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));
                }
            }
        }

    }

    // ScenarioSummary: 오른쪽 '시나리오 선택' 그리드에 표시될 요약 데이터
    public class ScoreScenarioSummary
    {
        public int ScenarioNumber { get; set; }
        public string ScenarioName { get; set; }

        // --- 6대 항목 점수 ---
        public uint CommScore { get; set; }
        public uint SafetyScore { get; set; }
        public uint MissionDistScore { get; set; }
        public uint MissionSuccessScore { get; set; }
        public uint SpatialResScore { get; set; }
        public uint CoverageScore { get; set; }

        public ulong StartTime { get; set; } 
        public ulong EndTime { get; set; }   



        // 원본 ScenarioItem 참조 (선택 시 상세 데이터 조회를 위해)
        public ScoreScenarioItem OriginalItem { get; set; }
    }

    // Coverage 그리드('촬영 면적')에 표시될 데이터 항목
    // (CoverageCalculator.AnalysisResult의 데이터를 변환)

    public class CoverageChartDataPoint
    {
        public DateTime Timestamp { get; set; }
        //public uint AircraftID { get; set; }
        public uint MissionSegmentID { get; set; }
        public float Coverage { get; set; }
    }
    public class CoverageDetailItem
    {
        //public uint UAVID { get; set; }
        public uint MissionSegmentID { get; set; }
        public float Coverage { get; set; } // 해당 UAV의 고유 기여도 (%)

        //public float CoveragePlane { get; set; } // 해당 UAV의 고유 기여도 (%)
        //public float TotalCoverage { get; set; } // 전체 요구 면적 (km²)

        public float FilmedArea { get; set; }      // [수정] 촬영된 면적 (km²)
        public float RequiredArea { get; set; }    // [수정] 요구 면적 (km²)
    }

    public class SafetyDetailItem
    {
        public string ID { get; set; } // e.g., "LAH1", "UAV4"
        public double ExposurePercentage { get; set; } // 위협 노출도 (%)
        public int ThreatTimestampCount { get; set; } // 위협 노출 시간 (ts)
        public int TotalTimestampCount { get; set; } // 총 비행 시간 (ts)
    }

    public class SafetyDataOutput
    {
        public DateTime Timestamp { get; set; }
        public uint AircraftID { get; set; } 
        
        public double Score { get; set; }
    }

    //임무분배효울도 차트
    public class MissionDistChartItem
    {
        public DateTime Timestamp { get; set; }
        public uint AircraftID { get; set; } // "UAV4", "UAV5", "UAV6"
        public int Status { get; set; }      // 1 (수행), 0 (중지)
    }

    //임무분배효울도 데이터그리드
    public class MissionDistDetailItem
    {
        public string AircraftID { get; set; }          // 예: "UAV4"
        public double TotalOperationSeconds { get; set; } // 총 운용시간 (초)
        public double TotalPauseSeconds { get; set; }     // 임무 중지 시간 (초)
        public double Score { get; set; }                 // 운용 효율성 (%)
    }

    //public class TargetGridItem
    //{
    //    public double AchievementRate { get; set; } // 성취도 (%)
    //    public int IdentifiedCount { get; set; }
    //    public int DetectedCount { get; set; }
    //    public int DestroyedCount { get; set; }
    //    public int TotalCount { get; set; }

    //    // 그리드 표시용 속성
    //    public string StatusCounts => $"식별:{IdentifiedCount} / 탐지:{DetectedCount} / 파괴:{DestroyedCount}";
    //}

    public class TargetGridItem
    {
        public uint TargetID { get; set; }

        // 최종 상태값 (0:미탐지, 1:식별, 2:탐지, 3:파괴)
        public int FinalState { get; set; }

        // 그리드 표시용 (읽기 전용 속성) -> XAML에서 바로 바인딩 가능
        // O/X 문자열로 반환하거나, View에서 BoolToVis 컨버터 사용 가능
        // 여기서는 직관적으로 string "O"/"X" 반환
        public string IsIdentified => FinalState >= 1 ? "O" : "X";
        public string IsDetected => FinalState >= 2 ? "O" : "X";
        public string IsDestroyed => FinalState >= 3 ? "O" : "X";
    }

    public class TargetChartItem
    {
        public DateTime Timestamp { get; set; }
        public int TargetID { get; set; }
        public int State { get; set; } // 0:미탐지, 1:식별, 2:탐지, 3:파괴
    }

    public class SRChartItem
    {
        public DateTime Timestamp { get; set; }
        public string AircraftID { get; set; }
        public float GSD { get; set; }
    }

    public class SRGridItem
    {
        public string AircraftID { get; set; }
        public double Score { get; set; }
        public double ValidTime { get; set; } // 초
        public double TotalTime { get; set; } // 초
    }
}
