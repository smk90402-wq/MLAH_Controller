using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json.Serialization;

namespace MLAH_LogAnalyzer
{
    public class ScenarioData
    {
        public required List<FlightData> FlightData { get; set; }
        public required List<MissionDetail> MissionDetail { get; set; }

        [JsonProperty("RealTargetData")]
        public List<RealTargetData> RealTargetData { get; set; }

        //���� �ҽ� ������ - �м� ������ �������� ��� ����
        public string SourceLogPath { get; set; }
        public string TargetLogPath { get; set; }
    }
    
    public class FlightData : CommonBase
    {
        public uint AircraftID { get; set; }
        public ulong Timestamp { get; set; }
        public uint MissionPlanID { get; set; }
        public uint MissionSegmentID { get; set; }

        public int FlightMode { get; set; }
        public int PayloadMode { get; set; }

        private FlightDataLog _FlightDataLog = new FlightDataLog();
        public FlightDataLog FlightDataLog
        {
            get { return _FlightDataLog; }
            set
            {
                _FlightDataLog = value;
                OnPropertyChanged(nameof(FlightDataLog));
            }
        }

        private CameraDataLog _CameraDataLog = new CameraDataLog();
        public CameraDataLog CameraDataLog
        {
            get { return _CameraDataLog; }
            set
            {
                _CameraDataLog = value;
                OnPropertyChanged(nameof(CameraDataLog));
            }
        }

        // 헬기→무인기 LOS 정보 (RealLAHData 기반, 1=LOS, 0=no LOS, null=데이터 없음)
        public int? LosUav4 { get; set; }
        public int? LosUav5 { get; set; }
        public int? LosUav6 { get; set; }
    }


    public class RealTargetData
    {
        public ulong Timestamp { get; set; }

        // JSON�� "TargetData"�� C#�� TargetList �Ӽ��� ����
        [JsonPropertyName("TargetData")]
        public List<Target> TargetList { get; set; }
    }

    public class MissionDetail
    {
        public uint MissionSegmentID { get; set; }
        public MissionPauseTimeStamp? MissionPauseTimeStamp { get; set; }
        //public required LineList LineList { get; set; }
        public required List<LineList> LineList { get; set; }
        public required List<AreaList> AreaList { get; set; }
    }

    public class Coordinate
    {
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public float Altitude { get; set; }
    }

    public class FlightDataLog
    {
        public FlightDataLog()
        {
            Latitude = 0;
            Longitude = 0;
            Altitude = 0;
        }
            
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public float Altitude { get; set; }
    }

    public class CameraPoint
    {
        public  float Latitude { get; set; }
        public  float Longitude { get; set; }
    }

    public class CameraDataLog : CommonBase
    {
        private CameraPoint _CameraTopLeft = new CameraPoint();
        public  CameraPoint CameraTopLeft
        {
            get
            { 
                return _CameraTopLeft;
            }
            set
            {
                _CameraTopLeft = value;
                OnPropertyChanged(nameof(CameraTopLeft));
            }
        }

        private CameraPoint _CameraTopRight = new CameraPoint();
        public CameraPoint CameraTopRight
        {
            get
            {
                return _CameraTopRight;
            }
            set
            {
                _CameraTopRight = value;
                OnPropertyChanged(nameof(CameraTopRight));
            }
        }

        private CameraPoint _CameraBottomLeft = new CameraPoint();
        public CameraPoint CameraBottomLeft
        {
            get
            {
                return _CameraBottomLeft;
            }
            set
            {
                _CameraBottomLeft = value;
                OnPropertyChanged(nameof(CameraBottomLeft));
            }
        }

        private CameraPoint _CameraBottomRight = new CameraPoint();
        public CameraPoint CameraBottomRight
        {
            get
            {
                return _CameraBottomRight;
            }
            set
            {
                _CameraBottomRight = value;
                OnPropertyChanged(nameof(CameraBottomRight));
            }
        }

        private CameraPoint _CameraCenterPoint = new CameraPoint();
        public CameraPoint CameraCenterPoint
        {
            get
            {
                return _CameraCenterPoint;
            }
            set
            {
                _CameraCenterPoint = value;
                OnPropertyChanged(nameof(CameraCenterPoint));
            }
        }
    }


    public class Target : CommonBase
    {
        public string Type { get; set; } = string.Empty;
        public string Subtype { get; set; } = string.Empty;
        public uint ID { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public float Altitude { get; set; }

        private uint _Status = 0;
        public uint Status
        {
            get
            {
                return _Status;
            }
            set
            {
                _Status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public bool IsEnemy { get; set; }
        public bool LAH1LOS { get; set; }
        public bool LAH2LOS { get; set; }
        public bool LAH3LOS { get; set; }

        // 부대1(아군) 표적추적 대상 표적 ID (0이면 미지정)
        public uint Unit1TargetID { get; set; }

    }


    public class PauseTimeRange
    {
        public ulong Start { get; set; }
        public ulong End { get; set; }
    }

    public class MissionPauseTimeStamp
    {
        public List<PauseTimeRange>? UAV4 { get; set; } 
        public List<PauseTimeRange>? UAV5 { get; set; } 
        public List<PauseTimeRange>? UAV6 { get; set; } 
    }

    public class LineList
    {
        public uint Width { get; set; }
        public required List<Coordinate> CoordinateList { get; set; }
    }

    public class AreaList
    {
        public bool IsHole { get; set; }
        public required List<Coordinate> CoordinateList { get; set; }
    }

    /// <summary>
    /// �ó����� ���� ����
    /// </summary>
    public class ScenarioFolderInfo
    {
        public DirectoryInfo Directory { get; set; }
        public DateTime DateTime { get; set; }

        public ScenarioFolderInfo(DirectoryInfo directory, DateTime dateTime)
        {
            Directory = directory;
            DateTime = dateTime;
        }
    }

    
}
