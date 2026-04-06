using DevExpress.Xpf.Grid;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace MLAH_LogAnalyzer
{
    public class MessageItem : INotifyPropertyChanged
    {
        public string Name { get; set; }
        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked))); }
        }

        //고아 여부(DB 폴더만 있고 부모 메시지 json은 없을 때)
        public bool HasOrphans { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class LogEntry
    {
        public ulong Timestamp { get; set; }
        public string TimeString { get; set; }
        public string MessageName { get; set; }
        public string From { get; set; } = "Hardcoded_From";
        public string To { get; set; } = "Hardcoded_To";

        //고아 여부(DB 폴더만 있고 부모 메시지 json은 없을 때)
        public bool IsOrphan { get; set; } = false;
        public object OriginalData { get; set; }
    }

    public class MessageNode
    {
        // ✅ 고유 ID와 부모 ID를 위한 속성 추가
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ParentId { get; set; }

        public string Name { get; set; }
        public string Value { get; set; }
        public List<MessageNode> Children { get; set; } = new List<MessageNode>();
    }

    public class TimelineEvent
    {
        public DateTime Timestamp { get; set; }
        public DateTime EndTimestamp { get; set; }
        public string MessageName { get; set; }
        public bool IsBackground { get; set; }

        // RangeControlClient의 ValueDataMember용 (double로 변환 가능해야 함)
        public double One => 1.0;
    }


    public class RowFill
    {
        public string MessageName { get; set; }   // X(회전이라 가로축) = 메시지명
        public DateTime Start { get; set; }   // Y축 시작(전체 로그 시작)
        public DateTime End { get; set; }   // Y축 끝(전체 로그 끝)
    }



    public static class MessageNameMapping
    {
        // ✅ 여기에 ID와 실제 메시지 이름을 계속 추가하면 돼.
        private static readonly Dictionary<string, string> IdToNameMap = new Dictionary<string, string>
    {
        { "0000", "0000 (Response)" },
        { "0101", "0101 (SystemOperationMode)" },
        { "0102", "0102 (ModuleStatus)" },
        { "0103", "0103 (SWStatus)" },
        { "0201", "0201 (InputMissionPlan)" },
        { "0202", "0202 (PriorMissionInfo)" },
        { "0203", "0203 (FlightReferenceInfo)" },
        { "0301", "0301 (MissionPlan)" },
        { "0302", "0302 (IndividualMissionPlan)" },
        { "0303", "0303 (UAVFlightPlan)" },
        { "0304", "0304 (LAHFlightPlan)" },
        { "0305", "0305 (ReplanStatus)" },
        { "0401", "0401 (LAHStatus)" },
        { "0402", "0402 (SituationAwarenessInfo)" },
        { "0501", "0501 (MissionStateInfo)" },
        { "0502", "0502 (EndMissionRequest)" },
        { "0503", "0503 (InformEndMission)" },
        { "0601", "0601 (UnderlyingAction)" },
        { "0602", "0602 (UAVControl)" },
        { "0701", "0701 (MissionPlanOptionInfo)" },
        { "0702", "0702 (MissionProgress)" },
        { "0801", "0801 (ReplanCommand)" },
        { "0802", "0802 (MandatoryCommand)" },
        { "0803", "0803 (StartNextMissionCommand)" },
        { "0804", "0804 (MissionRestartCommand)" },
        { "0805", "0805 (EndMissionCommand)" },
        { "0806", "0806 (EndSWCommand)" },
        { "0901", "0901 (RequestOptionInfo)" },
        { "0902", "0902 (ReplanRequest)" },
        { "0903", "0903 (RequestRenewMission)" },
        { "0904", "0904 (RequestBehaviorTree)" },
        
        //AB SBC#1 (종합) -> SBC#2 (단위1)
        { "51200", "(51200) ACSStatus" },
        { "51201", "(51201) BootCommand" },
        { "51202", "(51202) SystemEvent" },
        { "51203", "(51203) RequestMessage" },
        { "51220", "(51220) LAHStatesList" },
        { "51221", "(51221) UAVStatesList" },
        { "51223", "(51223) MalfunctionList" },
        
        //BA SBC#2 (단위1) -> SBC#1 (종합)
        { "52100", "(52100) SBC2Status" },
        { "52101", "(52101) RequestMessage" },
        { "52110", "(52110) TargetInformation" },

        //BC SBC#2 (단위1) -> SBC#3 (단위2)
        { "52310", "(52310) RegionOfInterest" },
        { "52311", "(52311) TargetInformation" },

        //AC SBC#1 (종합) -> SBC#3 (단위2)
        { "51300", "(51300) ACSStatus" },
        { "51301", "(51301) BootCommand" },
        { "51302", "(51302) SystemEvent" },
        { "51303", "(51303) RequestMessage" },
        { "51310", "(51310) InputMissionPackage" },
        { "51311", "(51311) MissionReferencePackage" },
        { "51320", "(51320) LAHStatesList" },
        { "51321", "(51321) UAVStatesList" },
        { "51323", "(51323) MalfunctionList" },
        { "51330", "(51330) ExecutionCommand" },
        { "51331", "(51331) PilotDecision" },
        { "51332", "(51332) MandatoryCommand" },
        { "51333", "(51333) PriorMissionCommand" },

        //CA SBC#3 (단위2) -> SBC#1 (종합)
        { "53100", "(53100) SBC3Status" },
        { "53103", "(53103) RequestMessage" },
        { "53110", "(53110) ReplanningStatus" },
        { "53111", "(53111) LAHMissionPlan" },
        { "53112", "(53112) UAVMissionPlan" },
        { "53113", "(53113) MissionPlanOptionInfo" },
        { "53114", "(53114) MissionUpdatewithoutPilotDecision" },
        { "53115", "(53115) MissionResults" },
        { "53120", "(53120) MUM-T Control Command" },
        { "53130", "(53130) MissionProgress" },
    };

        public static string GetName(string id)
        {
            // 만약 맵에 이름이 있으면 그 이름을, 없으면 원래 ID를 반환
            return IdToNameMap.TryGetValue(id, out var name) ? name : id;
        }

        // 데이터 모델 (StartTime, EndTime 포함)
        //public class TimelineSegment
        //{
        //    public string TaskName { get; set; }
        //    public DateTime StartTime { get; set; }
        //    public DateTime EndTime { get; set; }
        //    public string Color { get; set; }
        //    public string State { get; set; }
        //}

        public class TimelineSegment
        {
            public string TaskName { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public string Color { get; set; }  // "LimeGreen", "Crimson" 등

            //public System.Windows.Media.Brush Color { get; set; }
            public string State { get; set; }  // "자동이륙", "표적추적비행 (시간 초과)" 등
            public bool IsHatched { get; set; } // 빗금 여부 (true/false)

            public uint UavID { get; set; }
            public object Tag { get; set; } // 범용성을 위해

            public string MetricDetails { get; set; }
        }

        public static readonly Dictionary<string, string> FourDigitIdToDestinationsMap = new Dictionary<string, string>
        {
            //A : DSC
            //B : IDM
            //C : MSM
            //D : MMR
            //E : UCC
            //F : MOB
            //G : CSP
            { "0000", "DSC, IDM, MSM, MMR, UCC, MOB, CSP" },
            { "0101", "DSC, IDM, MSM, MMR, UCC, MOB" },
            { "0102", "IDM" },
            { "0103", "DSC" },
            { "0201", "IDM, MSM, MMR, UCC, MOB" },
            { "0202", "IDM, MSM, MMR, UCC, MOB" },
            { "0203", "IDM, MSM, MMR, UCC, MOB" },
            { "0301", "DSC, IDM, MSM, UCC, MOB" },
            { "0302", "DSC, IDM, MSM, UCC, MOB" },
            { "0303", "DSC, IDM, MSM, UCC, MOB" },
            { "0304", "DSC, IDM, MSM, UCC, MOB" },
            { "0305", "DSC, IDM" },
            { "0401", "IDM, MSM, MMR, UCC, MOB" },
            { "0402", "IDM, MSM, MMR, UCC, MOB" },
            { "0501", "DSC, IDM, MMR, UCC, MOB" },
            { "0502", "DSC, IDM" },
            { "0503", "DSC, IDM" },
            { "0504", "DSC, IDM" },
            { "0601", "IDM, MSM" },
            { "0602", "DSC, IDM" },
            { "0701", "DSC, IDM" },
            { "0702", "IDM, MSM" },
            { "0801", "IDM, MSM" },
            { "0802", "IDM, MSM" },
            { "0803", "IDM, MSM" },
            { "0804", "IDM, MSM, MMR, UCC, MOB" },
            { "0805", "IDM, MSM, UCC" },
            { "0806", "IDM, MSM, MMR, UCC, MOB, CSP" },
            { "0901", "IDM ,MOB" },
            { "0902", "IDM, MMR" },
            { "0903", "IDM, MSM" },
            { "0904", "IDM, CSP" },
        };



    }

    // 상세 데이터 연결을 위한 설정 클래스
    public class DetailMapInfo
    {
        public string FolderName { get; set; }  // 예: "InputMissionPlan"
        public string KeyField { get; set; }    // 예: "inputMissionPackageID"
    }


}
