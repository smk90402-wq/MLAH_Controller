using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Text.Json.Serialization;
//using Newtonsoft.Json;
using System.Collections.ObjectModel;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Editors.RangeControl;
//using DevExpress.Xpf.Reports.UserDesigner.Editors;
//using static MLAH_Controller.CommonEvent;

namespace MLAH_Mornitoring_UDP
{
    public partial class InitScenario : CommonBase
    {
        public void Clear()
        {
            MessageID = 2;
            InputMissionPackage?.Clear();
            MissionReferencePackage?.Clear();
        }
        /// <summary>
        /// Presence Vector (고정값 0x00)
        /// </summary>
        //public byte PresenceVector { get; } = 0x00;

        ///// <summary>
        ///// 5바이트짜리 Timestamp (2000.01.01 00:00:00부터 milliseconds)
        ///// </summary>
        //public byte[] Timestamp { get; set; } = new byte[5];


        private uint _MessageID = 2;
        /// <summary>
        /// 메시지 ID
        /// </summary>
        public uint MessageID
        {
            get
            {
                return _MessageID;
            }
            set
            {
                _MessageID = value;
                OnPropertyChanged("MessageID");
            }
        }

   

        private InputMissionPackage _InputMissionPackage = new InputMissionPackage();
        /// <summary>
        /// 협업 기저 관련 시나리오 데이터
        /// </summary>
        public InputMissionPackage InputMissionPackage
        {
            get
            {
                return _InputMissionPackage;
            }
            set
            {
                _InputMissionPackage = value;
                OnPropertyChanged("InputMissionPackage");
            }
        }


        private MissionReferencePackage _MissionReferencePackage = new MissionReferencePackage();
        /// <summary>
        /// 협업 기저 관련 시나리오 데이터
        /// </summary>
        public MissionReferencePackage MissionReferencePackage
        {
            get
            {
                return _MissionReferencePackage;
            }
            set
            {
                _MissionReferencePackage = value;
                OnPropertyChanged("MissionReferencePackage");
            }
        }


    }

    /// <summary>
    /// 임무 정보
    /// </summary>
    public partial class InputMissionPackage : CommonBase
    {
        public void Clear()
        {
            InputMissionPackageID = 100;
            MissionType = 0;
            DateAndNight = 0;
            AircraftIDsN = 0;
            AircraftIDs.Clear();
            //AircraftIDList.Clear();
            InputMissionListN = 0;
            InputMissionList.Clear();
        }

        /// <summary>
        /// Presence Vector (고정값 0x00)
        /// </summary>
        public byte PresenceVector { get; } = 0x00;

        ///// <summary>
        ///// 5바이트짜리 Timestamp (2000.01.01 00:00:00부터 milliseconds)
        ///// </summary>
        public byte[] Timestamp { get; set; } = new byte[5];

        private uint _InputMissionPackageID = 100;
        ///// <summary>
        ///// 협업기저임무 패키지 ID
        ///// </summary>
        public uint InputMissionPackageID
        {
            get
            {
                return _InputMissionPackageID;
            }
            set
            {
                _InputMissionPackageID = value;
                OnPropertyChanged("InputMissionPackageID");
            }
        }

        private uint _MissionType = 0;
        ///// <summary>
        ///// 주간야간EOIR
        ///// </summary>
        public uint MissionType
        {
            get
            {
                return _MissionType;
            }
            set
            {
                _MissionType = value;
                OnPropertyChanged("MissionType");
            }
        }

        private uint _DateAndNight = 1;
        ///// <summary>
        ///// 주간야간EOIR
        ///// </summary>
        public uint DateAndNight
        {
            get
            {
                return _DateAndNight;
            }
            set
            {
                _DateAndNight = value;
                OnPropertyChanged("DateAndNight");
            }
        }

        private uint _AircraftIDsN;
        /// <summary>
        /// 유무인기 대수
        /// </summary>
        public uint AircraftIDsN
        {
            get
            {
                return _AircraftIDsN;
            }
            set
            {
                _AircraftIDsN = value;
                //OnPropertyChanged("AircraftIDsN");
            }
        }

        //private AircraftIDList _AircraftIDList = new AircraftIDList();
        ///// <summary>
        /////협업기저임무 개수
        ///// </summary>
        //public AircraftIDList AircraftIDList
        //{
        //    get
        //    {
        //        return _AircraftIDList;
        //    }
        //    set
        //    {
        //        _AircraftIDList = value;
        //        //OnPropertyChanged("AircraftIDList");
        //    }
        //}

        private ObservableCollection<uint> _AircraftIDs = new ObservableCollection<uint>();
        /// <summary>
        ///협업기저임무 개수
        /// </summary>
        public ObservableCollection<uint> AircraftIDs
        {
            get
            {
                return _AircraftIDs;
            }
            set
            {
                _AircraftIDs = value;
                //OnPropertyChanged("AircraftIDs");
            }
        }


        private uint _InputMissionListN;
        /// <summary>
        ///협업기저임무 개수
        /// </summary>
        public uint InputMissionListN
        {
            get
            {
                return _InputMissionListN;
            }
            set
            {
                _InputMissionListN = value;
                OnPropertyChanged("InputMissionListN");
            }
        }

        private ObservableCollection<InputMission> _InputMissionList = new ObservableCollection<InputMission>();
        /// <summary>
        /// 협업기저임무
        /// </summary>
        public ObservableCollection<InputMission> InputMissionList
        {
            get
            {
                return _InputMissionList;
            }
            set
            {
                _InputMissionList = value;
                OnPropertyChanged("InputMissionList");
            }
        }
    }

    public partial class AircraftIDs : CommonBase
    {
        public void Clear()
        {
            AircraftID = 0;
        }

        private uint _AircraftID = 0;
        /// <summary>
        /// ?
        /// </summary>
        public uint AircraftID
        {
            get
            {
                return _AircraftID;
            }
            set
            {
                _AircraftID = value;
                //OnPropertyChanged("AircraftID");
            }
        }
    }

    /// <summary>
    /// ?
    /// </summary>
    public partial class AircraftIDList : CommonBase
    {
        public void Clear()
        {
            AircraftIDs.Clear();
        }

        /// <summary>
        /// ?
        /// </summary>

        private ObservableCollection<AircraftIDs> _AircraftIDs = new ObservableCollection<AircraftIDs>();
        /// <summary>
        /// ?
        /// </summary>
        public ObservableCollection<AircraftIDs> AircraftIDs
        {
            get
            {
                return _AircraftIDs;
            }
            set
            {
                _AircraftIDs = value;
                //OnPropertyChanged("AircraftIDs");
            }
        }
      
    }

    /// <summary>
    /// 좌표
    /// </summary>
    public partial class CoordinateInfo : CommonBase
    {
        public void Clear()
        {
            Latitude = 0f;
            Longitude = 0f;
            Altitude = 700; // 기본값 유지
        }
        private float _Latitude;
        /// <summary>
        /// 좌표의 위도 (deg) (필요 시 여러 좌표)
        /// </summary>
        public float Latitude
        {
            get
            {
                return _Latitude;
            }
            set
            {
                _Latitude = value;
                OnPropertyChanged("Latitude");
            }
        }

        private float _Longitude;
        /// <summary>
        /// 좌표의 경도 (deg) (필요 시 여러 좌표)
        /// </summary>
        public float Longitude
        {
            get
            {
                return _Longitude;
            }
            set
            {
                _Longitude = value;
                OnPropertyChanged("Longitude");
            }
        }

        private int _Altitude = 700;
        /// <summary>
        /// 좌표의 고도 (m) (필요 시 여러 좌표)
        /// </summary>
        public int Altitude
        {
            get
            {
                return _Altitude;
            }
            set
            {
                _Altitude = value;
                OnPropertyChanged("Altitude");
            }
        }
    }

    /// <summary>
    /// 구간
    /// </summary>
    public partial class PolyLineInfo : CommonBase
    {
        public void Clear()
        {
            Width = 0;
            CoordinateListN = 0;
            CoordinateList.Clear();
        }
        private uint _Width;
        /// <summary>
        /// 구간의 폭 (m) (필요 시 여러 좌표)
        /// </summary>
        public uint Width
        {
            get
            {
                return _Width;
            }
            set
            {
                _Width = value;
                OnPropertyChanged("Width");
            }
        }

        private uint _CoordinateListN;
        /// <summary>
        /// 선을 이루는 점의 개수
        /// </summary>
        public uint CoordinateListN
        {
            get
            {
                return _CoordinateListN;
            }
            set
            {
                _CoordinateListN = value;
                OnPropertyChanged("CoordinateListN");
            }
        }

        private ObservableCollection<CoordinateInfo> _CoordinateList = new ObservableCollection<CoordinateInfo>();
        /// <summary>
        /// 구간에 속한 좌표 리스트
        /// </summary>
        public ObservableCollection<CoordinateInfo> CoordinateList
        {
            get
            {
                return _CoordinateList;
            }
            set
            {
                _CoordinateList = value;
                OnPropertyChanged("CoordinateList");
            }
        }
    }

    /// <summary>
    /// 폴리곤
    /// </summary>
    public partial class PolyGon : CommonBase
    {
        public void Clear()
        {
            AreaListN = 0;
            AreaList.Clear();
        }
        private uint _AreaListN;
        /// <summary>
        /// 폴리곤 개수
        /// </summary>
        public uint AreaListN
        {
            get
            {
                return _AreaListN;
            }
            set
            {
                _AreaListN = value;
                OnPropertyChanged("AreaListN");
            }
        }

        private ObservableCollection<AreaInfo> _AreaList = new ObservableCollection<AreaInfo>();
        /// <summary>
        /// 구간에 속한 좌표 리스트
        /// </summary>
        public ObservableCollection<AreaInfo> AreaList
        {
            get
            {
                return _AreaList;
            }
            set
            {
                _AreaList = value;
                OnPropertyChanged("AreaList");
            }
        }
    }

    /// <summary>
    /// 영역
    /// </summary>
    public partial class AreaInfo : CommonBase
    {
        public void Clear()
        {
            IsHole = false;
            CoordinateListN = 0;
            CoordinateList.Clear();
        }
        private bool _IsHole;
        /// <summary>
        /// 영역제외 여부
        /// </summary>
        public bool IsHole
        {
            get
            {
                return _IsHole;
            }
            set
            {
                _IsHole = value;
                OnPropertyChanged("IsHole");
            }
        }

        private uint _CoordinateListN;
        /// <summary>
        /// 점의 개수
        /// </summary>
        public uint CoordinateListN
        {
            get
            {
                return _CoordinateListN;
            }
            set
            {
                _CoordinateListN = value;
                OnPropertyChanged("CoordinateListN");
            }
        }

        private ObservableCollection<CoordinateInfo> _CoordinateList = new ObservableCollection<CoordinateInfo>();
        /// <summary>
        /// 구간에 속한 좌표 리스트
        /// </summary>
        public ObservableCollection<CoordinateInfo> CoordinateList
        {
            get
            {
                return _CoordinateList;
            }
            set
            {
                _CoordinateList = value;
                OnPropertyChanged("CoordinateList");
            }
        }
    }

    


    public partial class InputMission : CommonBase
    {
        public void Clear()
        {
            InputMissionID = 0;
            SequenceNumber = 0;
            InputMissionType = 0;
            RegionType = 0;
            IsDone = false;
            ShapeType = 0;
            Coordinate?.Clear();
            PolyLine?.Clear();
            Polygons?.Clear();
            IsDisplay = true;
        }
        private uint _InputMissionID;
        /// <summary>
        /// 협업기저임무 ID
        /// </summary>
        public uint InputMissionID
        {
            get
            {
                return _InputMissionID;
            }
            set
            {
                _InputMissionID = value;
                OnPropertyChanged("InputMissionID");
            }
        }

        private uint _SequenceNumber;
        /// <summary>
        /// 임무지역 순서(0부터 시작)
        /// </summary>
        public uint SequenceNumber
        {
            get
            {
                return _SequenceNumber;
            }
            set
            {
                _SequenceNumber = value;
                OnPropertyChanged("SequenceNumber");
            }
        }

        private uint _InputMissionType;
        /// <summary>
        /// 협업기저임무 유형
        /// 0:협업기동임무
        /// 1:협업수색공격 임무
        /// 2:협업경계임무
        /// 3:협업공중부대엄호임무
        /// 4:협업지상부대엄호임무
        /// 5:협업도심수색공격임무
        /// </summary>
        public uint InputMissionType
        {
            get
            {
                return _InputMissionType;
            }
            set
            {
                _InputMissionType = value;
                OnPropertyChanged("InputMissionType");
            }
        }

        private uint _RegionType;
        /// <summary>
        /// 협업기저임무 유형
        /// 0:협업기동임무
        /// 1:협업수색공격 임무
        /// 2:협업경계임무
        /// 3:협업공중부대엄호임무
        /// 4:협업지상부대엄호임무
        /// 5:협업도심수색공격임무
        /// </summary>
        public uint RegionType
        {
            get
            {
                return _RegionType;
            }
            set
            {
                _RegionType = value;
                OnPropertyChanged("RegionType");
            }
        }

        private bool _IsDone;
        /// <summary>
        /// 수행완료 여부
        /// </summary>
        public bool IsDone
        {
            get
            {
                return _IsDone;
            }
            set
            {
                _IsDone = value;
                OnPropertyChanged("IsDone");
            }
        }

        private uint _ShapeType;
        /// <summary>
        /// 1 : 점 / 2 : 선 / 3 : 면
        /// </summary>
        public uint ShapeType
        {
            get
            {
                return _ShapeType;
            }
            set
            {
                _ShapeType = value;
                OnPropertyChanged("ShapeType");
            }
        }

        //private MissionDetail _MissionDetail = new MissionDetail();
        ///// <summary>
        ///// 협업기저임무 세부정보
        ///// </summary>
        //public MissionDetail MissionDetail
        //{
        //    get
        //    {
        //        return _MissionDetail;
        //    }
        //    set
        //    {
        //        _MissionDetail = value;
        //        OnPropertyChanged("MissionDetail");
        //    }
        //}
        private CoordinateInfo _Coordinate = new CoordinateInfo();
        /// <summary>
        /// 점 1개 (ShapeType : 1)
        /// </summary>
        public CoordinateInfo Coordinate
        {
            get
            {
                return _Coordinate;
            }
            set
            {
                _Coordinate = value;
                OnPropertyChanged("Coordinate");
            }
        }

        private PolyLineInfo _PolyLine = new PolyLineInfo();
        /// <summary>
        /// 선 1개 (ShapeType : 2)
        /// </summary>
        public PolyLineInfo PolyLine
        {
            get
            {
                return _PolyLine;
            }
            set
            {
                _PolyLine = value;
                OnPropertyChanged("PolyLine");
            }
        }

        private PolyGon _Polygons = new PolyGon();
        /// <summary>
        /// 다각형 N개 (ShapeType : 3)
        /// </summary>
        public PolyGon Polygons
        {
            get
            {
                return _Polygons;
            }
            set
            {
                _Polygons = value;
                OnPropertyChanged("Polygons");
            }
        }


        private bool _IsDisplay = true;
        /// <summary>
        /// JSON으로 보내지 않을 임시 변수
        /// </summary>
        //[System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public bool IsDisplay
        {
            get => _IsDisplay;
            set
            {
                _IsDisplay = value;
                //OnINITMissionDisplayChanged?.Invoke(value);
                OnPropertyChanged("IsDisplay");
            }
        }
    }

    /// <summary>
    /// 무인기 통제권 획득 위치 (=처음 위치)
    /// </summary>
    public partial class TakeOverHandOverInfo : CommonBase
    {
        public void Clear()
        {
            AircraftID = 0;
            CoordinateList.Clear();
        }

        private uint _AircraftID;
        /// <summary>
        /// 참조 대상 무인기 식별자(ID)
        /// 3 :무인기#1
        /// 4: 무인기#2
        /// 5: 무인기#3
        /// </summary>
        public uint AircraftID
        {
            get
            {
                return _AircraftID;
            }
            set
            {
                _AircraftID = value;
                OnPropertyChanged("AircraftID");
            }
        }

        private CoordinateInfo _CoordinateList = new CoordinateInfo();
        /// <summary>
        /// 무인기 통제권 획득 위치 (=처음 위치)
        /// </summary>
        public CoordinateInfo CoordinateList
        {
            get
            {
                return _CoordinateList;
            }
            set
            {
                _CoordinateList = value;
                OnPropertyChanged("CoordinateList");
            }
        }
    }



    /// <summary>
    /// 무인기 귀환 위치
    /// </summary>
    public partial class RTBCoordinateInfo : CommonBase
    {
        public void Clear()
        {
            Latitude = 0f;
            Longitude = 0f;
            Altitude = 0;
        }
        private float _Latitude;
        /// <summary>
        /// 무인기 귀환 위치 위도 (deg) (필요 시 여러 좌표)
        /// </summary>
        public float Latitude
        {
            get
            {
                return _Latitude;
            }
            set
            {
                _Latitude = value;
                OnPropertyChanged("Latitude");
            }
        }

        private float _Longitude;
        /// <summary>
        /// 무인기 귀환 위치 경도 (deg) (필요 시 여러 좌표)
        /// </summary>
        public float Longitude
        {
            get
            {
                return _Longitude;
            }
            set
            {
                _Longitude = value;
                OnPropertyChanged("Longitude");
            }
        }

        private int _Altitude;
        /// <summary>
        /// 무인기 귀환 위치 고도 (m) (필요 시 여러 좌표)
        /// </summary>
        public int Altitude
        {
            get
            {
                return _Altitude;
            }
            set
            {
                _Altitude = value;
                OnPropertyChanged("Altitude");
            }
        }
    }

    /// <summary>
    /// 비행가능구역 구성 위경도
    /// </summary>
    public partial class AreaLatLonInfo : CommonBase
    {
        public void Clear()
        {
            Latitude = 0f;
            Longitude = 0f;
        }
        private float _Latitude;
        /// <summary>
        /// 비행가능구역 위도 (deg) (필요 시 여러 좌표)
        /// </summary>
        public float Latitude
        {
            get
            {
                return _Latitude;
            }
            set
            {
                _Latitude = value;
                OnPropertyChanged("Latitude");
            }
        }

        private float _Longitude;
        /// <summary>
        /// 비행가능구역 경도 (deg) (필요 시 여러 좌표)
        /// </summary>
        public float Longitude
        {
            get
            {
                return _Longitude;
            }
            set
            {
                _Longitude = value;
                OnPropertyChanged("Longitude");
            }
        }
    }

    /// <summary>
    /// 비행가능구역 상하한 고도
    /// </summary>
    public partial class AltitudeLimitsInfo : CommonBase
    {
        public void Clear()
        {
            LowerLimit = 0;
            UpperLimit = 0;
        }
        private int _LowerLimit;
        /// <summary>
        /// 비행가능구역 하한 고도 (m)
        /// </summary>
        public int LowerLimit
        {
            get
            {
                return _LowerLimit;
            }
            set
            {
                _LowerLimit = value;
                OnPropertyChanged("LowerLimit");
            }
        }

        private int _UpperLimit;
        /// <summary>
        /// 비행가능구역 상한 고도 (m)
        /// </summary>
        public int UpperLimit
        {
            get
            {
                return _UpperLimit;
            }
            set
            {
                _UpperLimit = value;
                OnPropertyChanged("UpperLimit");
            }
        }
    }




    /// <summary>
    /// 비행가능구역
    /// </summary>
    public partial class FlightAreaInfo : CommonBase
    {
        public void Clear()
        {
            AreaLatLonListN = 0;
            AreaLatLonList.Clear();
            AltitudeLimits?.Clear();
        }
        //private string _FlightAreaID;
        ///// <summary>
        ///// 비행가능구역 식별자 ID
        ///// </summary>
        //public string FlightAreaID
        //{
        //    get
        //    {
        //        return _FlightAreaID;
        //    }
        //    set
        //    {
        //        _FlightAreaID = value;
        //        OnPropertyChanged("FlightAreaID");
        //    }
        //}

        public uint AreaLatLonListN;

        private ObservableCollection<AreaLatLonInfo> _AreaLatLonList = new ObservableCollection<AreaLatLonInfo>();
        /// <summary>
        /// 비행가능구역 구성 위경도
        /// </summary>
        public ObservableCollection<AreaLatLonInfo> AreaLatLonList
        {
            get
            {
                return _AreaLatLonList;
            }
            set
            {
                _AreaLatLonList = value;
                OnPropertyChanged("AreaLatLonList");
            }
        }

        private AltitudeLimitsInfo _AltitudeLimits = new AltitudeLimitsInfo();
        /// <summary>
        /// 비행가능구역 상하한 고도
        /// </summary>
        public AltitudeLimitsInfo AltitudeLimits
        {
            get
            {
                return _AltitudeLimits;
            }
            set
            {
                _AltitudeLimits = value;
                OnPropertyChanged("AltitudeLimits");
            }
        }
    }

    /// <summary>
    /// 비행금지구역
    /// </summary>
    public partial class ProhibitedArea : CommonBase
    {
        public void Clear()
        {
            AreaLatLonListN = 0;
            AreaLatLonList.Clear();
            AltitudeLimits?.Clear();
        }
        //private string _ProhibitedAreaID;
        ///// <summary>
        ///// 비행금지구역 식별자 ID
        ///// </summary>
        //public string ProhibitedAreaID
        //{
        //    get
        //    {
        //        return _ProhibitedAreaID;
        //    }
        //    set
        //    {
        //        _ProhibitedAreaID = value;
        //        OnPropertyChanged("ProhibitedAreaID");
        //    }
        //}

        public uint AreaLatLonListN;

        private ObservableCollection<AreaLatLonInfo> _AreaLatLonList = new ObservableCollection<AreaLatLonInfo>();
        /// <summary>
        /// 비행금지구역 구성 위경도
        /// </summary>
        public ObservableCollection<AreaLatLonInfo> AreaLatLonList
        {
            get
            {
                return _AreaLatLonList;
            }
            set
            {
                _AreaLatLonList = value;
                OnPropertyChanged("AreaLatLonList");
            }
        }

        private AltitudeLimitsInfo _AltitudeLimits = new AltitudeLimitsInfo();
        /// <summary>
        /// 비행금지구역 상하한 고도
        /// </summary>
        public AltitudeLimitsInfo AltitudeLimits
        {
            get
            {
                return _AltitudeLimits;
            }
            set
            {
                _AltitudeLimits = value;
                OnPropertyChanged("AltitudeLimits");
            }
        }
    }

    /// <summary>
    /// 비행참조정보 패키지
    /// </summary>
    public partial class MissionReferencePackage : CommonBase
    {
        public void Clear()
        {
            TakeOverInfoListN = 0;
            TakeOverInfoList.Clear();
            HandOverInfoListN = 0;
            HandOverInfoList.Clear();
            RTBCoordinateListN = 0;
            RTBCoordinateList.Clear();
            FlightAreaListN = 0;
            FlightAreaList.Clear();
            ProhibitedAreaListN = 0;
            ProhibitedAreaList.Clear();
        }
        //private string _MissionReferencePackageID;
        ///// <summary>
        ///// 비행참조정보 패키지 ID
        ///// </summary>
        //public string MissionReferencePackageID
        //{
        //    get
        //    {
        //        return _MissionReferencePackageID;
        //    }
        //    set
        //    {
        //        _MissionReferencePackageID = value;
        //        OnPropertyChanged("MissionReferencePackageID");
        //    }
        //}

        /// <summary>
        /// Presence Vector (고정값 0x00)
        /// </summary>
        public byte PresenceVector { get; } = 0x00;

        /// <summary>
        /// 5바이트짜리 Timestamp (2000.01.01 00:00:00부터 milliseconds)
        /// </summary>
        public byte[] Timestamp { get; set; } = new byte[5];

        //private long _TimeStamp;
        ///// <summary>
        ///// 메시지 생성 시간 (2000.01.01 00:00:00부터 miliseconds)
        ///// </summary>
        //public long TimeStamp
        //{
        //    get
        //    {
        //        return _TimeStamp;
        //    }
        //    set
        //    {
        //        _TimeStamp = value;
        //        OnPropertyChanged("TimeStamp");
        //    }
        //}

        private uint _TakeOverInfoListN;
        /// <summary>
        /// 무인기 통제권 획득 위치 개수 - 무인기 대수랑 일치 예상
        /// </summary>
        public uint TakeOverInfoListN
        {
            get
            {
                return _TakeOverInfoListN;
            }
            set
            {
                _TakeOverInfoListN = value;
                OnPropertyChanged("TakeOverInfoListN");
            }
        }

        private ObservableCollection<TakeOverHandOverInfo> _TakeOverInfoList = new ObservableCollection<TakeOverHandOverInfo>();
        /// <summary>
        /// 무인기 통제권 획득 위치 정보 - 무인기 대수랑 일치 예상
        /// </summary>
        public ObservableCollection<TakeOverHandOverInfo> TakeOverInfoList
        {
            get
            {
                return _TakeOverInfoList;
            }
            set
            {
                _TakeOverInfoList = value;
                OnPropertyChanged("TakeOverInfoList");
            }
        }

        private uint _HandOverInfoListN;
        /// <summary>
        /// 무인기 통제권 인계 위치 개수 - 무인기 대수랑 일치 예상
        /// </summary>
        public uint HandOverInfoListN
        {
            get
            {
                return _HandOverInfoListN;
            }
            set
            {
                _HandOverInfoListN = value;
                OnPropertyChanged("HandOverInfoListN");
            }
        }

        private ObservableCollection<TakeOverHandOverInfo> _HandOverInfoList = new ObservableCollection<TakeOverHandOverInfo>();
        /// <summary>
        /// 무인기 통제권 인계 위치 정보 - 무인기 대수랑 일치 예상
        /// </summary>
        public ObservableCollection<TakeOverHandOverInfo> HandOverInfoList
        {
            get
            {
                return _HandOverInfoList;
            }
            set
            {
                _HandOverInfoList = value;
                OnPropertyChanged("HandOverInfoList");
            }
        }

        private uint _RTBCoordinateListN;
        /// <summary>
        /// 무인기 귀환 위치 개수 - 무인기 대수랑 일치 예상
        /// </summary>
        public uint RTBCoordinateListN
        {
            get
            {
                return _RTBCoordinateListN;
            }
            set
            {
                _RTBCoordinateListN = value;
                OnPropertyChanged("RTBCoordinateListN");
            }
        }

        private ObservableCollection<RTBCoordinateInfo> _RTBCoordinateList = new ObservableCollection<RTBCoordinateInfo>();
        /// <summary>
        /// 무인기 귀환 위치 정보 - 무인기 대수랑 일치 예상
        /// </summary>
        public ObservableCollection<RTBCoordinateInfo> RTBCoordinateList
        {
            get
            {
                return _RTBCoordinateList;
            }
            set
            {
                _RTBCoordinateList = value;
                OnPropertyChanged("RTBCoordinateList");
            }
        }

        private uint _FlightAreaListN;
        /// <summary>
        /// 비행가능구역 개수
        /// </summary>
        public uint FlightAreaListN
        {
            get
            {
                return _FlightAreaListN;
            }
            set
            {
                _FlightAreaListN = value;
                OnPropertyChanged("FlightAreaListN");
            }
        }

        private ObservableCollection<FlightAreaInfo> _FlightAreaList = new ObservableCollection<FlightAreaInfo>();
        /// <summary>
        /// 무인기 귀환 위치 정보
        /// </summary>
        public ObservableCollection<FlightAreaInfo> FlightAreaList
        {
            get
            {
                return _FlightAreaList;
            }
            set
            {
                _FlightAreaList = value;
                OnPropertyChanged("FlightAreaList");
            }
        }

        private uint _ProhibitedAreaListN;
        /// <summary>
        /// 비행금지구역 개수
        /// </summary>
        public uint ProhibitedAreaListN
        {
            get
            {
                return _ProhibitedAreaListN;
            }
            set
            {
                _ProhibitedAreaListN = value;
                OnPropertyChanged("ProhibitedAreaListN");
            }
        }

        private ObservableCollection<ProhibitedArea> _ProhibitedAreaList = new ObservableCollection<ProhibitedArea>();
        /// <summary>
        /// 무인기 귀환 위치 정보
        /// </summary>
        public ObservableCollection<ProhibitedArea> ProhibitedAreaList
        {
            get
            {
                return _ProhibitedAreaList;
            }
            set
            {
                _ProhibitedAreaList = value;
                OnPropertyChanged("ProhibitedAreaList");
            }
        }



    }

    public partial class MUMTMissionPackage : CommonBase
    {
        private string _InputMissionPackageID;
        /// <summary>
        /// 협업기저임무 패키지 ID
        /// </summary>
        public string InputMissionPackageID
        {
            get
            {
                return _InputMissionPackageID;
            }
            set
            {
                _InputMissionPackageID = value;
                OnPropertyChanged("InputMissionPackageID");
            }
        }

        private uint _InputMissionPackageType;
        /// <summary>
        /// 0:대기갑항공타격작전
        /// 1:지상작전부대 기동여건 보장 작전
        /// 2:공중강습작전부대 엄호 작전
        /// 3:항공지원작전-중요시설 방호
        /// 4:도시지역 작전
        /// </summary>
        public uint InputMissionPackageType
        {
            get
            {
                return _InputMissionPackageType;
            }
            set
            {
                _InputMissionPackageType = value;
                OnPropertyChanged("InputMissionPackageType");
            }
        }

        private DateTime _InputTimeStamp;
        /// <summary>
        /// 입력된 시점
        /// </summary>
        public DateTime InputTimeStamp
        {
            get
            {
                return _InputTimeStamp;
            }
            set
            {
                _InputTimeStamp = value;
                OnPropertyChanged("InputTimeStamp");
            }
        }
    }

    /// <summary>
    /// SW 상태정보
    /// </summary>
    public partial class SWStatus : CommonBase
    {
        private uint _MessageID;
        /// <summary>
        /// 메세지 ID = 1
        /// </summary>
        public uint MessageID
        {
            get
            {
                return _MessageID;
            }
            set
            {
                _MessageID = value;
                OnPropertyChanged("MessageID");
            }
        }

        private uint _DeviceID;
        /// <summary>
        /// 100:시험운용통제 SW
        /// 200 ~ 400:전장상황모의 SW
        /// 500: 전장정보 상황인지 모의 SW
        /// 600 ~ 800: 무인기 모의 SW
        /// 900 : 임무통제 및 운용모의
        /// 1000 : 시현화면 모의 SW
        /// </summary>
        public uint DeviceID
        {
            get
            {
                return _DeviceID;
            }
            set
            {
                _DeviceID = value;
                OnPropertyChanged("DeviceID");
            }
        }

        private uint _State;
        /// <summary>
        /// 상태 : 1 고정(정상)
        /// </summary>
        public uint State
        {
            get
            {
                return _State;
            }
            set
            {
                _State = value;
                OnPropertyChanged("State");
            }
        }
    }

    /// <summary>
    /// 센서 제어 명령
    /// </summary>
    public partial class SensorControlCommand : CommonBase
    {
        private uint _MessageID;
        /// <summary>
        /// 메세지 ID = 4
        /// </summary>
        public uint MessageID
        {
            get
            {
                return _MessageID;
            }
            set
            {
                _MessageID = value;
                OnPropertyChanged("MessageID");
            }
        }

        private uint _UavID;
        /// <summary>
        /// 4 : 무인기 1
        /// 5 : 무인기 2
        /// 6 : 무인기 3
        /// </summary>
        public uint UavID
        {
            get
            {
                return _UavID;
            }
            set
            {
                _UavID = value;
                OnPropertyChanged("UavID");
            }
        }

        private uint _SensorType;
        /// <summary>
        /// 센서 유형 / 0 : OFF / 1 : EO / 2 : IR
        /// </summary>
        public uint SensorType
        {
            get
            {
                return _SensorType;
            }
            set
            {
                _SensorType = value;
                OnPropertyChanged("SensorType");
            }
        }

        private float _HorizontalFov;
        /// <summary>
        /// 수평 FOV
        /// </summary>
        public float HorizontalFov
        {
            get
            {
                return _HorizontalFov;
            }
            set
            {
                _HorizontalFov = value;
                OnPropertyChanged("HorizontalFov");
            }
        }

        private float _VerticalFov;
        /// <summary>
        /// 수직 FOV
        /// </summary>
        public float VerticalFov
        {
            get
            {
                return _VerticalFov;
            }
            set
            {
                _VerticalFov = value;
                OnPropertyChanged("VerticalFov");
            }
        }

        private float _GimbalRoll;
        /// <summary>
        /// Gimbal Roll
        /// </summary>
        public float GimbalRoll
        {
            get
            {
                return _GimbalRoll;
            }
            set
            {
                _GimbalRoll = value;
                OnPropertyChanged("GimbalRoll");
            }
        }

        private float _GimbalPitch;
        /// <summary>
        /// Gimbal Pitch
        /// </summary>
        public float GimbalPitch
        {
            get
            {
                return _GimbalPitch;
            }
            set
            {
                _GimbalPitch = value;
                OnPropertyChanged("GimbalPitch");
            }
        }

        private double _SensorLat;
        /// <summary>
        /// 센서위치 위도 == 무인기 위치 위도
        /// </summary>
        public double SensorLat
        {
            get
            {
                return _SensorLat;
            }
            set
            {
                _SensorLat = value;
                OnPropertyChanged("SensorLat");
            }
        }

        private double _SensorLon;
        /// <summary>
        /// 센서위치 경도 == 무인기 위치 경도
        /// </summary>
        public double SensorLon
        {
            get
            {
                return _SensorLon;
            }
            set
            {
                _SensorLon = value;
                OnPropertyChanged("SensorLon");
            }
        }

        private double _SensorAlt;
        /// <summary>
        /// 센서위치 고도 == 무인기 위치 고도
        /// </summary>
        public double SensorAlt
        {
            get
            {
                return _SensorAlt;
            }
            set
            {
                _SensorAlt = value;
                OnPropertyChanged("SensorAlt");
            }
        }

        private double _Roll;
        /// <summary>
        /// 무인기 Roll
        /// </summary>
        public double Roll
        {
            get
            {
                return _Roll;
            }
            set
            {
                _Roll = value;
                OnPropertyChanged("Roll");
            }
        }

        private double _Pitch;
        /// <summary>
        /// 무인기 Pitch
        /// </summary>
        public double Pitch
        {
            get
            {
                return _Pitch;
            }
            set
            {
                _Pitch = value;
                OnPropertyChanged("Pitch");
            }
        }

        private double _Heading;
        /// <summary>
        /// 무인기 Heading
        /// </summary>
        public double Heading
        {
            get
            {
                return _Heading;
            }
            set
            {
                _Heading = value;
                OnPropertyChanged("Heading");
            }
        }

        private double _Speed;
        /// <summary>
        /// 무인기 Speed
        /// </summary>
        public double Speed
        {
            get
            {
                return _Speed;
            }
            set
            {
                _Speed = value;
                OnPropertyChanged("Speed");
            }
        }

        private float _Fuel;
        /// <summary>
        /// 무인기 연료
        /// </summary>
        public float Fuel
        {
            get
            {
                return _Fuel;
            }
            set
            {
                _Fuel = value;
                OnPropertyChanged("Fuel");
            }
        }


    }



    public partial class CoordinateList
    {
        public float Latitude;
        public float Longitude;
        public int Altitude;
    }







    public partial class UAVMalFunctionCommand
    {
        public uint MessageID;
        public uint UavID;
        public uint Health;
        public uint PayloadHealth;
        public uint FuelWarning;
    }

    public partial class UAVMalFunctionState
    {
        public uint MessageID;
        public uint UavID;
        public uint Health;
        public uint PayloadHealth;
        public uint FuelWarning;
    }

    public partial class DatalinkStatus
    {
        public bool IsConnectedToUAV1;
        public bool IsConnectedToUAV2;
        public bool IsConnectedToUAV3;
    }

    public partial class States
    {
        public uint AircraftID;
        public CoordinateList CoordinateList;
        public Velocity Velocity;
        public float Fuel;
        public Weapons Weapons;
        //public uint Health;
        //public DatalinkStatus DatalinkStatus;
        public byte[] LastSignalTime = new byte[5];

    }

    public partial class Lah_States
    {
        public uint MessageID;
        public byte PresenceVector;
        public byte[] TimeStamp = new byte[5];
        public uint StatesN;
        public States[] States;
    }

    public partial class SensorInfo
    {
        public uint MessageID;
        public uint UavID;
        public float HorizontalFov;
        public float VerticalFov;
        public float DiagonalFov;
        public float SensorCenterLat;
        public float SensorCenterLon;
        public int SensorCenterAlt;
        public int SlantRange;
        public float FootPrintCenterLat;
        public float FootPrintCenterLon;
        public int FootPrintCenterAlt;
        public float FootPrintLeftTopLat;
        public float FootPrintLeftTopLon;
        public int FootPrintLeftTopAlt;
        public float FootPrintRightTopLat;
        public float FootPrintRightTopLon;
        public int FootPrintRightTopAlt;
        public float FootPrintRightBottomLat;
        public float FootPrintRightBottomLon;
        public int FootPrintRightBottomAlt;
        public float FootPrintLeftBottomLat;
        public float FootPrintLeftBottomLon;
        public int FootPrintLeftBottomAlt;

    }

    public partial class LAH
    {
        public uint AircraftID;
        public uint Health;
        public DatalinkStatus DatalinkStatus;
    }

    public partial class LAHMalFunctionState
    {
        public uint MessageID;
        //public byte PresenceVector;
        //public byte[] TimeStamp = new byte[5];
        public uint LAHN;
        public LAH[] LAH;
    }





    /// <summary>
    /// LAHMissionPlan 메시지를 담는 최상위 클래스
    /// </summary>
    public partial class LAHMissionPlan : CommonBase
    {
        public void Clear()
        {
            MessageID = 0;
            MissionPlanID = 0;
            AircraftID = 0;
            MissionSegemntN = 0;
            MissionSegemntList.Clear();
        }
        public uint MessageID;

        /// <summary>
        /// Presence Vector (고정값 0x00)
        /// </summary>
        public byte PresenceVector { get; set; } = 0x00;

        /// <summary>
        /// 5바이트짜리 Timestamp (2000.01.01 00:00:00부터 milliseconds)
        /// </summary>
        public byte[] Timestamp { get; set; } = new byte[5];

        /// <summary>
        /// 유인기 임무계획 식별자 (500,000,000 ~ 599,999,999)
        /// </summary>
        private uint _MissionPlanID = 0;
        public uint MissionPlanID
        {
            get
            {
                return _MissionPlanID;
            }
            set
            {
                _MissionPlanID = value;
                OnPropertyChanged("MissionPlanID");
            }
        }

        private uint _AircraftID = 0;
        /// <summary>
        /// 대상 유인기
        /// 1: 지휘기, 2: 편대기1, 3: 편대기2
        /// </summary>
        public uint AircraftID
        {
            get
            {
                return _AircraftID;
            }
            set
            {
                _AircraftID = value;
                OnPropertyChanged("AircraftID");
            }
        }


        private uint _MissionSegemntN = 0;
        /// <summary>
        /// 협업기저임무(MissionSegment)의 수
        /// </summary>
        public uint MissionSegemntN
        {
            get
            {
                return _MissionSegemntN;
            }
            set
            {
                _MissionSegemntN = value;
                OnPropertyChanged("MissionSegemntN");
            }
        }



        private ObservableCollection<MissionSegmentLAH> _MissionSegemntList = new ObservableCollection<MissionSegmentLAH>();
        /// <summary>
        /// 실제 협업기저임무 목록
        /// </summary>
        public ObservableCollection<MissionSegmentLAH> MissionSegemntList
        {
            get
            {
                return _MissionSegemntList;
            }
            set
            {
                _MissionSegemntList = value;
                OnPropertyChanged("MissionSegemntList");
            }
        }
    }

    /// <summary>
    /// 협업기저임무(MissionSegment)를 표현하는 클래스
    /// </summary>
    public partial class MissionSegmentLAH : CommonBase
    {
        public void Clear()
        {
            MissionSegmentID = 0;
            IsDone = false;
            MissionSegmentType = 0;
            IndividualMissionListN = 0;
            IndividualMissionList.Clear();
        }
        private uint _MissionSegmentID = 0;
        /// <summary>
        /// 협업기저임무 식별자 (750,000,000 ~ 799,999,999)
        /// </summary>
        public uint MissionSegmentID
        {
            get
            {
                return _MissionSegmentID;
            }
            set
            {
                _MissionSegmentID = value;
                OnPropertyChanged("MissionSegmentID");
            }
        }

        //private uint _RelatedInputMissionID = 0;
        ///// <summary>
        ///// 임무구간(초기임무정보 기반) 식별자 (700,000,000 ~ 749,999,999)
        ///// </summary>
        //public uint RelatedInputMissionID
        //{
        //    get
        //    {
        //        return _RelatedInputMissionID;
        //    }
        //    set
        //    {
        //        _RelatedInputMissionID = value;
        //        OnPropertyChanged("RelatedInputMissionID");
        //    }
        //}


        private bool _IsDone = false;
        ///// <summary>
        ///// 해당 임무지역이 수행완료 되었는지 여부
        ///// </summary>
        public bool IsDone
        {
            get
            {
                return _IsDone;
            }
            set
            {
                _IsDone = value;
                OnPropertyChanged("IsDone");
            }
        }

        private uint _MissionSegmentType = 0;
        /// <summary>
        /// 협업기저임무 타입
        /// 1: 협업기동임무, 2: 협업수색공격임무, 3: 협업경계임무, 4: 협업공중부대엄호임무, 
        /// 5: 협업지상부대엄호임무, 6: 협업도심수색공격임무
        /// </summary>
        public uint MissionSegmentType
        {
            get
            {
                return _MissionSegmentType;
            }
            set
            {
                _MissionSegmentType = value;
                OnPropertyChanged("MissionSegmentType");
            }
        }

        private uint _IndividualMissionListN = 0;
        /// <summary>
        /// 해당 협업기저임무 구간에서 수행될 개별임무(IndividualMission)의 수
        /// </summary>
        public uint IndividualMissionListN
        {
            get
            {
                return _IndividualMissionListN;
            }
            set
            {
                _IndividualMissionListN = value;
                OnPropertyChanged("IndividualMissionListN");
            }
        }

        private ObservableCollection<IndividualMissionLAH> _IndividualMissionList = new ObservableCollection<IndividualMissionLAH>();
        /// <summary>
        /// 실제 개별임무 목록
        /// </summary>
        public ObservableCollection<IndividualMissionLAH> IndividualMissionList
        {
            get
            {
                return _IndividualMissionList;
            }
            set
            {
                _IndividualMissionList = value;
                OnPropertyChanged("IndividualMissionList");
            }
        }
    }

    /// <summary>
    /// 개별임무(IndividualMission)를 표현하는 클래스
    /// </summary>
    public partial class IndividualMissionLAH : CommonBase
    {
        public void Clear()
        {
            IndividualMissionID = 0;
            IsDone = false;
            WaypointListN = 0;
            WaypointList.Clear();
        }
        private uint _IndividualMissionID = 0;
        /// <summary>
        /// 개별임무 식별자 (300,000,000 ~ 399,999,999)
        /// </summary>
        public uint IndividualMissionID
        {
            get
            {
                return _IndividualMissionID;
            }
            set
            {
                _IndividualMissionID = value;
                OnPropertyChanged("IndividualMissionID");
            }
        }

        private bool _IsDone = false;
        /// <summary>
        /// 해당 개별임무가 수행완료 되었는지 여부
        /// </summary>
        public bool IsDone
        {
            get
            {
                return _IsDone;
            }
            set
            {
                _IsDone = value;
                OnPropertyChanged("IsDone");
            }
        }

        /// <summary>
        /// 경로점(Waypoint)의 수
        /// </summary>
        private uint _WaypointListN = 0;
        public uint WaypointListN
        {
            get
            {
                return _WaypointListN;
            }
            set
            {
                _WaypointListN = value;
                OnPropertyChanged("WaypointListN");
            }

        }

        private ObservableCollection<WaypointLAH> _WaypointList = new ObservableCollection<WaypointLAH>();
        /// <summary>
        /// 실제 경로점 목록
        /// </summary>
        public ObservableCollection<WaypointLAH> WaypointList
        {
            get
            {
                return _WaypointList;
            }
            set
            {
                _WaypointList = value;
                OnPropertyChanged("WaypointList");
            }
        }
    }

    /// <summary>
    /// 경로점(Waypoint)을 표현하는 클래스
    /// </summary>
    public partial class WaypointLAH : CommonBase
    {
        public void Clear()
        {
            WaypointID = 0;
            Coordinate?.Clear();
            Speed = 0f;
            ETA = 0;
            NextWaypointID = 200000000; // 기본값 유지
            Hovering = 0;
            Attack?.Clear();
        }
        /// <summary>
        /// 경로점 식별자 (200,000,000 ~ 299,999,999)
        /// </summary>
        private uint _WaypointID = 0;
        public uint WaypointID
        {
            get
            {
                return _WaypointID;
            }
            set
            {
                _WaypointID = value;
                OnPropertyChanged("WaypointID");
            }
        }

        private Coordinate _Coordinate = new Coordinate();
        public Coordinate Coordinate
        {
            get
            {
                return _Coordinate;
            }
            set
            {
                _Coordinate = value;
                OnPropertyChanged("Coordinate");
            }
        }


        private float _Speed = 0;
        /// <summary>
        /// 경로점 목표 속도 (mps)
        /// </summary>
        public float Speed
        {
            get
            {
                return _Speed;
            }
            set
            {
                _Speed = value;
                OnPropertyChanged("Speed");
            }
        }

        private int _ETA = 0;
        /// <summary>
        /// 해당 개별임무를 구성하는 경로점에 대한 도착 예측 누적 시간 (sec)
        /// </summary>
        public int ETA
        {
            get
            {
                return _ETA;
            }
            set
            {
                _ETA = value;
                OnPropertyChanged("ETA");
            }
        }

        private uint _NextWaypointID = 200000000;
        /// <summary>
        /// 다음 경로점 식별자 (200,000,000 ~ 299,999,999)
        /// </summary>
        public uint NextWaypointID
        {
            get
            {
                return _NextWaypointID;
            }
            set
            {
                _NextWaypointID = value;
                OnPropertyChanged("NextWaypointID");
            }
        }

        private uint _Hovering = 0;
        /// <summary>
        /// 경로점 도착 후 제자리비행 시간 (sec)
        /// </summary>
        public uint Hovering
        {
            get
            {
                return _Hovering;
            }
            set
            {
                _Hovering = value;
                OnPropertyChanged("Hovering");
            }
        }

        private Attack _Attack = new Attack();
        /// <summary>
        /// 공격 관련 정보
        /// </summary>
        public Attack Attack
        {
            get
            {
                return _Attack;
            }
            set
            {
                _Attack = value;
                OnPropertyChanged("Attack");
            }
        }
    }

    /// <summary>
    /// 공격(Attack) 정보를 표현하는 클래스
    /// </summary>
    public partial class Attack : CommonBase
    {
        public void Clear()
        {
            TargetID = 1000000000; // 기본값 유지
            WeaponType = 0;
        }
        private uint _TargetID = 1000000000;
        /// <summary>
        /// 공격 대상 표적 식별자 (1,000,000,000 ~ 1,099,999,999), 0이면 공격 없음
        /// </summary>
        public uint TargetID
        {
            get
            {
                return _TargetID;
            }
            set
            {
                _TargetID = value;
                OnPropertyChanged("TargetID");
            }
        }

        private uint _WeaponType = 0;
        /// <summary>
        /// 표적 공격 시 사용할 무장종류
        /// 0: 공격 없음, 1: Type1, 2: Type2, 3: Type3
        /// </summary>
        public uint WeaponType
        {
            get
            {
                return _WeaponType;
            }
            set
            {
                _WeaponType = value;
                OnPropertyChanged("WeaponType");
            }
        }
    }

    /// <summary>
    /// UAVMissionPlan (메시지 ID: 53112)
    /// 무인기의 임무계획이 변경되었을 경우 해당 정보를 전달하기 위한 메시지 구조
    /// </summary>
    public partial class UAVMissionPlan : CommonBase
    {
        public void Clear()
        {
            MessageID = 0;
            MissionPlanID = 500000000; // 기본값 유지
            AircraftID = 4; // 기본값 유지
            MissionSegemntN = 0;
            MissionSegemntList.Clear();
        }
        public uint MessageID;

        /// <summary>
        /// Presence Vector (고정값 0x00)
        /// </summary>
        public byte PresenceVector { get; set; } = 0x00;

        /// <summary>
        /// 5바이트짜리 Timestamp (2000.01.01 00:00:00부터 milliseconds)
        /// </summary>
        public byte[] Timestamp { get; set; } = new byte[5];

        private uint _MissionPlanID = 500000000;
        /// <summary>
        /// 무인기 임무계획 식별자 (500,000,000 ~ 599,999,999)
        /// </summary>
        public uint MissionPlanID
        {
            get
            {
                return _MissionPlanID;
            }
            set
            {
                _MissionPlanID = value;
                OnPropertyChanged("MissionPlanID");
            }
        }

        private uint _AircraftID = 4;
        /// <summary>
        /// 대상 무인기
        /// 4: 무인기1, 5: 무인기2, 6: 무인기3
        /// </summary>
        public uint AircraftID
        {
            get
            {
                return _AircraftID;
            }
            set
            {
                _AircraftID = value;
                OnPropertyChanged("AircraftID");
            }
        }

        private uint _MissionSegemntN = 0;
        /// <summary>
        /// 협업기저임무(MissionSegment)의 수
        /// </summary>
        public uint MissionSegemntN
        {
            get
            {
                return _MissionSegemntN;
            }
            set
            {
                _MissionSegemntN = value;
                OnPropertyChanged("MissionSegemntN");
            }
        }

        private ObservableCollection<MissionSegmentUAV> _MissionSegemntList = new ObservableCollection<MissionSegmentUAV>();
        /// <summary>
        /// 실제 협업기저임무(MissionSegment) 목록
        /// </summary>
        public ObservableCollection<MissionSegmentUAV> MissionSegemntList
        {
            get
            {
                return _MissionSegemntList;
            }
            set
            {
                _MissionSegemntList = value;
                OnPropertyChanged("MissionSegemntList");
            }
        }
    }

    /// <summary>
    /// 협업기저임무(MissionSegment)를 표현하는 클래스
    /// </summary>
    public partial class MissionSegmentUAV : CommonBase
    {
        public void Clear()
        {
            MissionSegmentID = 750000000; // 기본값 유지
            IsDone = false;
            MissionSegmentType = 0;
            IndividualMissionListN = 0;
            IndividualMissionList.Clear();
        }
        private uint _MissionSegmentID = 750000000;
        /// <summary>
        /// 협업기저임무 식별자 (750,000,000 ~ 799,999,999)
        /// </summary>
        public uint MissionSegmentID
        {
            get
            {
                return _MissionSegmentID;
            }
            set
            {
                _MissionSegmentID = value;
                OnPropertyChanged("MissionSegmentID");
            }
        }

        /// <summary>
        /// 임무구간(초기임무정보 기반) 식별자 (700,000,000 ~ 749,999,999)
        /// </summary>
        //public uint RelatedInputMissionID { get; set; }




        private bool _IsDone = false;
        ///// <summary>
        ///// 해당 임무지역이 수행완료 되었는지 여부
        ///// </summary>
        public bool IsDone
        {
            get
            {
                return _IsDone;
            }
            set
            {
                _IsDone = value;
                OnPropertyChanged("IsDone");
            }
        }

        private uint _MissionSegmentType = 0;
        /// <summary>
        /// 협업기저임무의 타입
        /// 1: 협업기동임무,
        /// 2: 협업수색공격임무,
        /// 3: 협업경계임무,
        /// 4: 협업공중부대엄호임무,
        /// 5: 협업지상부대엄호임무,
        /// 6: 협업도심수색공격임무
        /// </summary>
        public uint MissionSegmentType
        {
            get
            {
                return _MissionSegmentType;
            }
            set
            {
                _MissionSegmentType = value;
                OnPropertyChanged("MissionSegmentType");
            }
        }

        private uint _IndividualMissionListN = 0;
        /// <summary>
        /// 해당 협업기저임무 구간에서 수행될 개별임무의 수
        /// </summary>
        public uint IndividualMissionListN
        {
            get
            {
                return _IndividualMissionListN;
            }
            set
            {
                _IndividualMissionListN = value;
                OnPropertyChanged("IndividualMissionListN");
            }
        }

        private ObservableCollection<IndividualMissionUAV> _IndividualMissionList = new ObservableCollection<IndividualMissionUAV>();
        /// <summary>
        /// 실제 개별임무 목록
        /// </summary>
        public ObservableCollection<IndividualMissionUAV> IndividualMissionList
        {
            get
            {
                return _IndividualMissionList;
            }
            set
            {
                _IndividualMissionList = value;
                OnPropertyChanged("IndividualMissionList");
            }
        }
    }

    public partial class Coordinate : CommonBase
    {
        public void Clear()
        {
            Latitude = 0f;
            Longitude = 0f;
            Altitude = 0;
        }
        private float _Latitude = 0f;
        /// <summary>
        /// 위도
        /// </summary>
        public float Latitude
        {
            get { return _Latitude; }
            set
            {
                _Latitude = value;
                OnPropertyChanged("Latitude");
            }
        }

        private float _Longitude = 0f;
        /// <summary>
        /// 경도
        /// </summary>
        public float Longitude
        {
            get { return _Longitude; }
            set
            {
                _Longitude = value;
                OnPropertyChanged("Longitude");
            }
        }

        private int _Altitude = 0;
        /// <summary>
        /// 고도
        /// </summary>
        public int Altitude
        {
            get { return _Altitude; }
            set
            {
                _Altitude = value;
                OnPropertyChanged("Altitude");
            }
        }

    }

    public partial class AllocatedArea : CommonBase
    {
        public void Clear()
        {
            CoordinateListN = 0;
            CoordinateList.Clear();
        }
        private uint _CoordinateListN = 0;
        /// <summary>
        /// 해당 좌표 목록의 개수
        /// </summary>
        public uint CoordinateListN
        {
            get { return _CoordinateListN; }
            set
            {
                _CoordinateListN = value;
                OnPropertyChanged("CoordinateListN");
            }
        }

        private ObservableCollection<CoordinateList> _CoordinateList = new ObservableCollection<CoordinateList>();
        /// <summary>
        /// 좌표 목록
        /// </summary>
        public ObservableCollection<CoordinateList> CoordinateList
        {
            get { return _CoordinateList; }
            set
            {
                _CoordinateList = value;
                OnPropertyChanged("CoordinateList");
            }
        }
    }

    /// <summary>
    /// 개별임무(IndividualMission)를 표현하는 클래스
    /// </summary>
    public partial class IndividualMissionUAV : CommonBase
    {
        public void Clear()
        {
            IndividualMissionID = 0;
            IsDone = false;
            AllocatedArea?.Clear();
            FlightType = 0;
            WaypointListN = 0;
            WaypointList.Clear();
            FormationInfo?.Clear();
        }
        private uint _IndividualMissionID = 0;
        /// <summary>
        /// 개별임무 식별자 (범위 미지정, Uint)
        /// </summary>
        public uint IndividualMissionID
        {
            get { return _IndividualMissionID; }
            set
            {
                _IndividualMissionID = value;
                OnPropertyChanged("IndividualMissionID");
            }
        }

        private bool _IsDone = false;
        /// <summary>
        /// 해당 개별임무 수행완료 여부
        /// </summary>
        public bool IsDone
        {
            get { return _IsDone; }
            set
            {
                _IsDone = value;
                OnPropertyChanged("IsDone");
            }
        }

        private AllocatedArea _AllocatedArea = new AllocatedArea();
        /// <summary>
        /// 할당영역(AllocatedArea)
        /// </summary>
        public AllocatedArea AllocatedArea
        {
            get { return _AllocatedArea; }
            set
            {
                _AllocatedArea = value;
                OnPropertyChanged("AllocatedArea");
            }
        }

        private uint _FlightType = 0;
        /// <summary>
        /// 비행타입
        /// 1: 경로비행(Planar Flight), 2: 편대비행(Formation Flight)
        /// </summary>
        public uint FlightType
        {
            get { return _FlightType; }
            set
            {
                _FlightType = value;
                OnPropertyChanged("FlightType");
            }
        }

        #region FlightType = 1 일 경우 (경로비행 정보)
        private uint _WaypointListN = 0;
        /// <summary>
        /// 경로점(Waypoint)의 수 (FlightType=1 일 때만 유효)
        /// </summary>
        public uint WaypointListN
        {
            get { return _WaypointListN; }
            set
            {
                _WaypointListN = value;
                OnPropertyChanged("WaypointListN");
            }
        }

        private ObservableCollection<WaypointUAV> _WaypointList = new ObservableCollection<WaypointUAV>();
        /// <summary>
        /// 실제 경로점 목록 (FlightType=1 일 때만 유효)
        /// </summary>
        public ObservableCollection<WaypointUAV> WaypointList
        {
            get { return _WaypointList; }
            set
            {
                _WaypointList = value;
                OnPropertyChanged("WaypointList");
            }
        }
        #endregion

        #region FlightType = 2 일 경우 (편대비행 정보)
        private FormationInfo _FormationInfo = new FormationInfo();
        /// <summary>
        /// 편대비행 관련 정보 (FlightType=2 일 때만 유효)
        /// </summary>
        public FormationInfo FormationInfo
        {
            get { return _FormationInfo; }
            set
            {
                _FormationInfo = value;
                OnPropertyChanged("FormationInfo");
            }
        }
        #endregion
    }

    /// <summary>
    /// 경로점(Waypoint)을 표현하는 클래스 (FlightType=1)
    /// </summary>
    public partial class WaypointUAV : CommonBase
    {
        public void Clear()
        {
            WaypointID = 0;
            Coordinate?.Clear();
            Speed = 0f;
            ETA = 0;
            ECF = 0f;
            NextWaypointID = 0;
            WaypointPassType = 0;
            LoiterProperty = null; // 객체는 null로 초기화
            FilmingProperty = null; // 객체는 null로 초기화
        }
        private uint _WaypointID = 0;
        /// <summary>
        /// 경로점 식별자 (200,000,000 ~ 299,999,999)
        /// </summary>
        public uint WaypointID
        {
            get { return _WaypointID; }
            set
            {
                _WaypointID = value;
                OnPropertyChanged("WaypointID");
            }
        }

        private Coordinate _Coordinate = new Coordinate();
        /// <summary>
        /// 경로점 좌표
        /// </summary>
        public Coordinate Coordinate
        {
            get { return _Coordinate; }
            set
            {
                _Coordinate = value;
                OnPropertyChanged("Coordinate");
            }
        }

        private float _Speed = 0f;
        /// <summary>
        /// 경로점 목표 속도 (mps)
        /// </summary>
        public float Speed
        {
            get { return _Speed; }
            set
            {
                _Speed = value;
                OnPropertyChanged("Speed");
            }
        }

        private uint _ETA = 0;
        /// <summary>
        /// 해당 Waypoint 도착까지의 누적 예측 시간 (sec)
        /// </summary>
        public uint ETA
        {
            get { return _ETA; }
            set
            {
                _ETA = value;
                OnPropertyChanged("ETA");
            }
        }

        private float _ECF = 0f;
        /// <summary>
        /// 전 경로점에서 현재 경로점까지 예측 연료 소모량 (liter)
        /// </summary>
        public float ECF
        {
            get { return _ECF; }
            set
            {
                _ECF = value;
                OnPropertyChanged("ECF");
            }
        }

        private uint _NextWaypointID = 0;
        /// <summary>
        /// 다음 경로점 식별자 (200,000,000 ~ 299,999,999)
        /// </summary>
        public uint NextWaypointID
        {
            get { return _NextWaypointID; }
            set
            {
                _NextWaypointID = value;
                OnPropertyChanged("NextWaypointID");
            }
        }

        private uint _WaypointPassType = 0;
        /// <summary>
        /// 경로점 통과 방식
        /// 1: Fly-by, 2: Fly-over
        /// </summary>
        public uint WaypointPassType
        {
            get { return _WaypointPassType; }
            set
            {
                _WaypointPassType = value;
                OnPropertyChanged("WaypointPassType");
            }
        }

        private LoiterProperty _LoiterProperty = null;
        /// <summary>
        /// 선회(Loiter) 정보 (없을 경우 null 처리)
        /// </summary>
        public LoiterProperty LoiterProperty
        {
            get { return _LoiterProperty; }
            set
            {
                _LoiterProperty = value;
                OnPropertyChanged("LoiterProperty");
            }
        }

        private bool _FilmingPlaned = false;
        /// <summary>
        /// </summary>
        public bool FilmingPlaned
        {
            get { return _FilmingPlaned; }
            set
            {
                _FilmingPlaned = value;
                OnPropertyChanged("FilmingPlaned");
            }
        }

        private FilmingProperty _FilmingProperty = null;
        /// <summary>
        /// 촬영 운용 정보 (없을 경우 null 처리)
        /// </summary>
        public FilmingProperty FilmingProperty
        {
            get { return _FilmingProperty; }
            set
            {
                _FilmingProperty = value;
                OnPropertyChanged("FilmingProperty");
            }
        }
    }

    /// <summary>
    /// Loiter(선회) 관련 속성
    /// </summary>
    public partial class LoiterProperty : CommonBase
    {
        public void Clear()
        {
            Radius = 0;
            Direction = 0;
            Time = 0;
            Speed = 0f;
        }
        private int _Radius = 0;
        /// <summary>
        /// 선회 반경 (m)
        /// </summary>
        public int Radius
        {
            get { return _Radius; }
            set
            {
                _Radius = value;
                OnPropertyChanged("Radius");
            }
        }

        private uint _Direction = 0;
        /// <summary>
        /// 선회 방향 (1: 시계방향(CW), 2: 반시계방향(CCW))
        /// </summary>
        public uint Direction
        {
            get { return _Direction; }
            set
            {
                _Direction = value;
                OnPropertyChanged("Direction");
            }
        }

        private int _Time = 0;
        /// <summary>
        /// Loiter 유지시간 (sec)
        /// </summary>
        public int Time
        {
            get { return _Time; }
            set
            {
                _Time = value;
                OnPropertyChanged("Time");
            }
        }

        private float _Speed = 0f;
        /// <summary>
        /// 선회 속도 (mps)
        /// </summary>
        public float Speed
        {
            get { return _Speed; }
            set
            {
                _Speed = value;
                OnPropertyChanged("Speed");
            }
        }
    }

    /// <summary>
    /// 촬영(Filming) 운용 정보를 표현하는 클래스
    /// </summary>
    public partial class FilmingProperty : CommonBase
    {
        public void Clear()
        {
            FieldOfView = 0f;
            SensorType = 0;
            OperationMode = 0;
            CoordinateListN = 0;
            CoordinateList.Clear();
            SearchSpeed = 0f;
            TargetID = 0;
            SensorYaw = 0f;
            SensorPitch = 0f;
            SensorYawAngularSpeed?.Clear();
        }
        private float _FieldOfView = 0f;
        /// <summary>
        /// 카메라 화각 (deg)
        /// </summary>
        public float FieldOfView
        {
            get { return _FieldOfView; }
            set
            {
                _FieldOfView = value;
                OnPropertyChanged("FieldOfView");
            }
        }

        private uint _SensorType = 0;
        /// <summary>
        /// 카메라 센서 타입 (1: EO, 2: IR)
        /// </summary>
        public uint SensorType
        {
            get { return _SensorType; }
            set
            {
                _SensorType = value;
                OnPropertyChanged("SensorType");
            }
        }

        private int _OperationMode = 0;
        /// <summary>
        /// 촬영 운용 모드
        /// 1: 좌표지향모드,
        /// 2: 구간탐색/구역감시모드,
        /// 3: 자동추적모드,
        /// 4: 기체고정모드,
        /// 5: 자동주사모드
        /// </summary>
        public int OperationMode
        {
            get { return _OperationMode; }
            set
            {
                _OperationMode = value;
                OnPropertyChanged("OperationMode");
            }
        }

        #region OperationMode가 1 또는 2일 경우 사용
        private int _CoordinateListN = 0;
        /// <summary>
        /// 좌표의 수 (N=1: 좌표지향모드, N>1: 구간탐색/LineSearch)
        /// </summary>
        public int CoordinateListN
        {
            get { return _CoordinateListN; }
            set
            {
                _CoordinateListN = value;
                OnPropertyChanged("CoordinateListN");
            }
        }

        private ObservableCollection<CoordinateList> _CoordinateList = new ObservableCollection<CoordinateList>();
        /// <summary>
        /// 좌표 리스트 (OperationMode 1,2일 때 사용)
        /// </summary>
        public ObservableCollection<CoordinateList> CoordinateList
        {
            get { return _CoordinateList; }
            set
            {
                _CoordinateList = value;
                OnPropertyChanged("CoordinateList");
            }
        }

        private float _SearchSpeed = 0f;
        /// <summary>
        /// 지형 탐색 속도 (mps) - OperationMode=2(구간탐색)일 때만 사용
        /// </summary>
        public float SearchSpeed
        {
            get { return _SearchSpeed; }
            set
            {
                _SearchSpeed = value;
                OnPropertyChanged("SearchSpeed");
            }
        }
        #endregion

        #region OperationMode=3 (자동추적모드)일 경우 사용
        private uint _TargetID = 0;
        /// <summary>
        /// 자동추적 대상 표적 식별자 (1,000,000,000 ~ 1,099,999,999)
        /// </summary>
        public uint TargetID
        {
            get { return _TargetID; }
            set
            {
                _TargetID = value;
                OnPropertyChanged("TargetID");
            }
        }
        #endregion

        #region OperationMode=4 (기체고정모드)일 경우 사용
        private float _SensorYaw = 0f;
        /// <summary>
        /// 기체 고정각도 (Yaw, deg)
        /// </summary>
        public float SensorYaw
        {
            get { return _SensorYaw; }
            set
            {
                _SensorYaw = value;
                OnPropertyChanged("SensorYaw");
            }
        }
        #endregion

        #region OperationMode=4 or 5 (기체고정 / 자동주사)에서 사용
        private float _SensorPitch = 0f;
        /// <summary>
        /// 기체 고정각도 (Pitch, deg)
        /// </summary>
        public float SensorPitch
        {
            get { return _SensorPitch; }
            set
            {
                _SensorPitch = value;
                OnPropertyChanged("SensorPitch");
            }
        }
        #endregion

        #region OperationMode=5 (자동주사모드)일 경우 사용
        private SensorYawAngularSpeed _SensorYawAngularSpeed = new SensorYawAngularSpeed();
        /// <summary>
        /// 자동주사모드 시 Yaw 각도 하한, 상한, 변화율
        /// </summary>
        public SensorYawAngularSpeed SensorYawAngularSpeed
        {
            get { return _SensorYawAngularSpeed; }
            set
            {
                _SensorYawAngularSpeed = value;
                OnPropertyChanged("SensorYawAngularSpeed");
            }
        }
        #endregion
    }

    /// <summary>
    /// 자동주사모드 시 Yaw 각도와 변화율 정보를 담는 클래스
    /// </summary>
    public partial class SensorYawAngularSpeed : CommonBase
    {
        public void Clear()
        {
            LeftLimit = 0f;
            RightLimit = 0f;
            AngularRate = 0f;
        }
        private float _LeftLimit = 0f;
        /// <summary>
        /// 자동주사모드용 Yaw 각도 하한
        /// </summary>
        public float LeftLimit
        {
            get { return _LeftLimit; }
            set
            {
                _LeftLimit = value;
                OnPropertyChanged("LeftLimit");
            }
        }

        private float _RightLimit = 0f;
        /// <summary>
        /// 자동주사모드용 Yaw 각도 상한
        /// </summary>
        public float RightLimit
        {
            get { return _RightLimit; }
            set
            {
                _RightLimit = value;
                OnPropertyChanged("RightLimit");
            }
        }

        private float _AngularRate = 0f;
        /// <summary>
        /// Yaw 변화율 (deg/sec)
        /// </summary>
        public float AngularRate
        {
            get { return _AngularRate; }
            set
            {
                _AngularRate = value;
                OnPropertyChanged("AngularRate");
            }
        }
    }

    /// <summary>
    /// 편대비행(Formation Flight) 관련 정보 (FlightType=2)
    /// </summary>
    public partial class FormationInfo : CommonBase
    {
        public void Clear()
        {
            LeaderAircraftID = 0;
            Formation?.Clear();
            //FilmingProperty = null; // 객체는 null로 초기화
        }
        private uint _LeaderAircraftID = 0;
        /// <summary>
        /// 편대 리더 무인기
        /// 4: 무인기1, 5: 무인기2, 6: 무인기3
        /// </summary>
        public uint LeaderAircraftID
        {
            get { return _LeaderAircraftID; }
            set
            {
                _LeaderAircraftID = value;
                OnPropertyChanged("LeaderAircraftID");
            }
        }

        private Formation _Formation = new Formation();
        /// <summary>
        /// 편대비행 시 3축 이격거리
        /// </summary>
        public Formation Formation
        {
            get { return _Formation; }
            set
            {
                _Formation = value;
                OnPropertyChanged("Formation");
            }
        }

        //private FilmingProperty _FilmingProperty = null;
        ///// <summary>
        ///// 촬영 운용 정보 (필요 시 사용)
        ///// </summary>
        //public FilmingProperty FilmingProperty
        //{
        //    get { return _FilmingProperty; }
        //    set
        //    {
        //        _FilmingProperty = value;
        //        OnPropertyChanged("FilmingProperty");
        //    }
        //}
    }

    /// <summary>
    /// 편대비행 시 상대 이격거리 (dX, dY, dZ)
    /// </summary>
    public partial class Formation : CommonBase
    {
        public void Clear()
        {
            dX = 0;
            dY = 0;
            dZ = 0;
        }
        private int _dX = 0;
        /// <summary>
        /// 편대 비행 X축 이격 거리 (리더 기체 기준 오른쪽 방향 +)
        /// </summary>
        public int dX
        {
            get { return _dX; }
            set
            {
                _dX = value;
                OnPropertyChanged("dX");
            }
        }

        private int _dY = 0;
        /// <summary>
        /// 편대 비행 Y축 이격 거리 (리더 기체 기준 기수 방향 +)
        /// </summary>
        public int dY
        {
            get { return _dY; }
            set
            {
                _dY = value;
                OnPropertyChanged("dY");
            }
        }

        private int _dZ = 0;
        /// <summary>
        /// 편대 비행 고도축(Z) 이격 거리 (리더 기체 기준 위쪽 +)
        /// </summary>
        public int dZ
        {
            get { return _dZ; }
            set
            {
                _dZ = value;
                OnPropertyChanged("dZ");
            }
        }
    }

    public partial class MissionPlanOptionInfo : CommonBase
    {
        public void Clear()
        {
            MessageID = 53113; // 기본값 유지
            AutoExecution = false;
            OptionListN = 0;
            OptionList.Clear();
        }

        private uint _MessageID = 53113;
        /// <summary>
        /// 메시지 ID: 53113
        /// 의사결정옵션정보 전달을 위한 메시지 구조
        /// </summary>
        public uint MessageID
        {
            get { return _MessageID; }
            set
            {
                _MessageID = value;
                OnPropertyChanged("MessageID");
            }
        }

        private readonly byte _PresenceVector = 0x00;
        /// <summary>
        /// Presence Vector (고정값 0x00)
        /// </summary>
        public byte PresenceVector
        {
            get { return _PresenceVector; }
        }

        private byte[] _Timestamp = new byte[5];
        /// <summary>
        /// 5바이트짜리 Timestamp (2000.01.01 00:00:00부터 milliseconds)
        /// </summary>
        public byte[] Timestamp
        {
            get { return _Timestamp; }
            set
            {
                _Timestamp = value;
                OnPropertyChanged("Timestamp");
            }
        }

        private bool _AutoExecution = false;
        /// <summary>
        /// True: 설정된 시간이 흐르면 시스템 추천으로 자동 인가
        /// False: 자동인가 없음, 조종사의 의사결정 필수
        /// </summary>
        public bool AutoExecution
        {
            get { return _AutoExecution; }
            set
            {
                _AutoExecution = value;
                OnPropertyChanged("AutoExecution");
            }
        }

        private uint _OptionListN = 0;
        /// <summary>
        /// 의사결정 옵션정보 수
        /// </summary>
        public uint OptionListN
        {
            get { return _OptionListN; }
            set
            {
                _OptionListN = value;
                OnPropertyChanged("OptionListN");
            }
        }

        private ObservableCollection<OptionList> _OptionList = new ObservableCollection<OptionList>();
        /// <summary>
        /// 실제 의사결정 옵션 목록
        /// </summary>
        public ObservableCollection<OptionList> OptionList
        {
            get { return _OptionList; }
            set
            {
                _OptionList = value;
                OnPropertyChanged("OptionList");
            }
        }
    }

    public partial class OptionList : CommonBase
    {
        public void Clear()
        {
            OptionID = 0;
            Recommand = false;
            OptionName = 0;
            SurvivalRate = 0;
            TimeContraction = 0;
            RecogEffectiveness = 0;
            FuelWarning = 0;
            Distance = 0;
            Target = 0;
            UAVMissionPlanIDListN = 0;
            UAVMissionPlanIDList.Clear();
            LAHMissionPlanIDListN = 0;
            LAHMissionPlanIDList.Clear();
        }

        private uint _OptionID = 0;
        /// <summary>
        /// 옵션 식별자
        /// </summary>
        public uint OptionID
        {
            get { return _OptionID; }
            set
            {
                _OptionID = value;
                OnPropertyChanged("OptionID");
            }
        }

        private bool _Recommand = false;
        /// <summary>
        /// 해당 옵션이 추천인지 여부
        /// </summary>
        public bool Recommand
        {
            get { return _Recommand; }
            set
            {
                _Recommand = value;
                OnPropertyChanged("Recommand");
            }
        }

        private uint _OptionName = 0;
        /// <summary>
        /// 옵션 타입
        /// 1: 시스템 추천
        /// 2: 공격특화
        /// 3: 공격배제
        /// 4: 정찰특화
        /// 5: 최소시간
        /// </summary>
        public uint OptionName
        {
            get { return _OptionName; }
            set
            {
                _OptionName = value;
                OnPropertyChanged("OptionName");
            }
        }

        private int _SurvivalRate = 0;
        /// <summary>
        /// 기존 대비 생존확률
        /// -1 : 생존확률 감소
        ///  0 : 변화없음
        ///  1 : 생존확률 증가
        /// </summary>
        public int SurvivalRate
        {
            get { return _SurvivalRate; }
            set
            {
                _SurvivalRate = value;
                OnPropertyChanged("SurvivalRate");
            }
        }

        private int _TimeContraction = 0;
        /// <summary>
        /// 기존 대비 임무소요시간 감소량
        /// -1 : 시간증가
        ///  0 : 변화없음
        ///  1 : 시간감소효과
        /// </summary>
        public int TimeContraction
        {
            get { return _TimeContraction; }
            set
            {
                _TimeContraction = value;
                OnPropertyChanged("TimeContraction");
            }
        }

        private int _RecogEffectiveness = 0;
        /// <summary>
        /// 기존 대비 촬영 효과
        /// -1 : 촬영효과 감소
        ///  0 : 변화없음
        ///  1 : 촬영효과 증가
        /// </summary>
        public int RecogEffectiveness
        {
            get { return _RecogEffectiveness; }
            set
            {
                _RecogEffectiveness = value;
                OnPropertyChanged("RecogEffectiveness");
            }
        }

        private int _FuelWarning = 0;
        /// <summary>
        /// 해당 임무계획이 연료의 부족이 존재할 가능성이 있는지 알림
        /// -1 : 연료주의
        ///  0 : 연료양호
        /// </summary>
        public int FuelWarning
        {
            get { return _FuelWarning; }
            set
            {
                _FuelWarning = value;
                OnPropertyChanged("FuelWarning");
            }
        }

        private uint _Distance = 0;
        /// <summary>
        /// 지휘기 잔여 이동거리 (m)
        /// </summary>
        public uint Distance
        {
            get { return _Distance; }
            set
            {
                _Distance = value;
                OnPropertyChanged("Distance");
            }
        }

        private uint _Target = 0;
        /// <summary>
        /// 표적 타격 계획 수
        /// </summary>
        public uint Target
        {
            get { return _Target; }
            set
            {
                _Target = value;
                OnPropertyChanged("Target");
            }
        }

        private uint _UAVMissionPlanIDListN = 0;
        /// <summary>
        /// 무인기 임무계획 구성 수
        /// </summary>
        public uint UAVMissionPlanIDListN
        {
            get { return _UAVMissionPlanIDListN; }
            set
            {
                _UAVMissionPlanIDListN = value;
                OnPropertyChanged("UAVMissionPlanIDListN");
            }
        }

        private ObservableCollection<uint> _UAVMissionPlanIDList = new ObservableCollection<uint>();
        /// <summary>
        /// 무인기 임무계획 식별자 목록
        /// </summary>
        public ObservableCollection<uint> UAVMissionPlanIDList
        {
            get { return _UAVMissionPlanIDList; }
            set
            {
                _UAVMissionPlanIDList = value;
                OnPropertyChanged("UAVMissionPlanIDList");
            }
        }

        private uint _LAHMissionPlanIDListN = 0;
        /// <summary>
        /// 유인기 임무계획 구성 수
        /// </summary>
        public uint LAHMissionPlanIDListN
        {
            get { return _LAHMissionPlanIDListN; }
            set
            {
                _LAHMissionPlanIDListN = value;
                OnPropertyChanged("LAHMissionPlanIDListN");
            }
        }

        private ObservableCollection<uint> _LAHMissionPlanIDList = new ObservableCollection<uint>();
        /// <summary>
        /// 유인기 임무계획 식별자 목록
        /// </summary>
        public ObservableCollection<uint> LAHMissionPlanIDList
        {
            get { return _LAHMissionPlanIDList; }
            set
            {
                _LAHMissionPlanIDList = value;
                OnPropertyChanged("LAHMissionPlanIDList");
            }
        }
    }

    public partial class OperatingCommand : CommonBase
    {
        private uint _MessageID = 0;
        /// <summary>
        /// 메시지 ID
        /// </summary>
        public uint MessageID
        {
            get { return _MessageID; }
            set
            {
                _MessageID = value;
                OnPropertyChanged("MessageID");
            }
        }

        private uint _Command = 0;
        /// <summary>
        /// 0: Not Used / 1 : 임무 시작 / 2 : 임무 종료
        /// </summary>
        public uint Command
        {
            get { return _Command; }
            set
            {
                _Command = value;
                OnPropertyChanged("Command");
            }
        }
    }

    public class PilotDecision
    {
        public uint MessageID = 51331;
        public byte PresenceVector { get; } = 0x00;
        public byte[] Timestamp { get; set; } = new byte[5];
        public bool Ignore;
        public uint EditOptionsIDConverter;
    }

    public class MissionUpdatewithoutPilotDecision : CommonBase
    {
        public uint MessageID = 53114;
        public byte PresenceVector { get; } = 0x00;
        public byte[] Timestamp { get; set; } = new byte[5];

        private uint _UAVMissionPlanIDListN = 0;
        public uint UAVMissionPlanIDListN
        {
            get { return _UAVMissionPlanIDListN; }
            set
            {
                _UAVMissionPlanIDListN = value;
                OnPropertyChanged("UAVMissionPlanIDListN");
            }
        }

        private ObservableCollection<uint> _UAVMissionPlanIDList = new ObservableCollection<uint>();
        public ObservableCollection<uint> UAVMissionPlanIDList
        {
            get { return _UAVMissionPlanIDList; }
            set
            {
                _UAVMissionPlanIDList = value;
                OnPropertyChanged("UAVMissionPlanIDList");
            }
        }

        private uint _LAHMissionPlanIDListN = 0;
        public uint LAHMissionPlanIDListN
        {
            get { return _LAHMissionPlanIDListN; }
            set
            {
                _LAHMissionPlanIDListN = value;
                OnPropertyChanged("LAHMissionPlanIDListN");
            }
        }

        private ObservableCollection<uint> _LAHMissionPlanIDList = new ObservableCollection<uint>();
        public ObservableCollection<uint> LAHMissionPlanIDList
        {
            get { return _LAHMissionPlanIDList; }
            set
            {
                _LAHMissionPlanIDList = value;
                OnPropertyChanged("LAHMissionPlanIDList");
            }
        }
    }

    public class MissionResultData
    {
        public ushort SplitInfo { get; set; }
        public ushort DataLength { get; set; }
        public uint SourceID { get; set; }
        public uint DestID { get; set; }
        public ushort MessageID { get; set; }
        public ushort Properties { get; set; }
        public byte PresenceVector { get; set; }
        public long Timestamp { get; set; }
        public uint SystemRecommend { get; set; }

        public string RecommendText
        {
            get
            {
                switch (SystemRecommend)
                {
                    case 1: return "다음 협업기저임무 추천";
                    case 2: return "현재 협업기저임무 재수행 추천";
                    case 3: return "모든 협업기저임무 완료";
                    default: return "알 수 없음";
                }
            }
        }
    }

}
