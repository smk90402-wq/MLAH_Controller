using DevExpress.Mvvm.DataAnnotations;
using MLAH_Controller;
using MLAHInterop;
using System.Collections.ObjectModel;
using System.ServiceModel.Channels;
using Windows.Services.Maps;

namespace MLAH_Controller
{
    public partial class Model_UnitScenario : CommonBase
    {
        //[System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public readonly object UnitListLock = new object();
        /// <summary>
        /// 시나리오 모델의 모든 데이터를 기본값으로 초기화합니다.
        /// </summary>
        public void Clear()
        {
            // 1. 값 타입 프로퍼티 초기화
            ScenarioName = string.Empty;
            ScenarioTerrain = string.Empty;

            // 2. 메인 컬렉션 클리어
            UnitObjectList.Clear();

            // 3. 하위 객체들의 Clear() 메소드 연쇄 호출
            InitScenario?.Clear();
            missionPlanManageList?.Clear();
        }

        //public readonly object UnitListLock = new object();

        private string _ScenarioName;
        /// <summary>
        /// 시나리오 이름
        /// </summary>
        public string ScenarioName
        {
            get
            {
                return _ScenarioName;
            }
            set
            {
                _ScenarioName = value;
                OnPropertyChanged("ScenarioName");
            }
        }

        private string _ScenarioDesc;
        /// <summary>
        /// 시나리오 설명
        /// </summary>
        public string ScenarioDesc
        {
            get
            {
                return _ScenarioDesc;
            }
            set
            {
                _ScenarioDesc = value;
                OnPropertyChanged("ScenarioDesc");
            }
        }

        private string _ScenarioTerrain;
        /// <summary>
        /// 시나리오 지형
        /// </summary>
        public string ScenarioTerrain
        {
            get
            {
                return _ScenarioTerrain;
            }
            set
            {
                _ScenarioTerrain = value;
                OnPropertyChanged("ScenarioTerrain");
            }
        }

        private ObservableCollection<UnitObjectInfo> _UnitObjectList = new ObservableCollection<UnitObjectInfo>();
        /// <summary>
        /// 유인공격헬기 리스트
        /// </summary>
        public ObservableCollection<UnitObjectInfo> UnitObjectList
        {
            get
            {
                return _UnitObjectList;
            }
            set
            {
                _UnitObjectList = value;
                OnPropertyChanged("UnitObjectList");
            }
        }

        private InitScenario _InitScenario = new InitScenario();
        /// <summary>
        /// 초기임무정보 리스트
        /// </summary>
        public InitScenario InitScenario
        {
            get
            {
                return _InitScenario;
            }
            set
            {
                _InitScenario = value;
                OnPropertyChanged("InitScenario");
            }
        }

        private MissionPlanManage _missionPlanManageList = new MissionPlanManage();
        public MissionPlanManage missionPlanManageList
        {
            get
            {
                return _missionPlanManageList;
            }
            set
            {
                _missionPlanManageList = value;
                OnPropertyChanged("missionPlanManageList");
            }
        }

    }

    public partial class MissionPlanManage : CommonBase
    {
        public void Clear()
        {
            LAHMissionPlans.Clear();
            UAVMissionPlans.Clear();
            //missionPlanOptionInfo?.Clear();
            // MovePlanModel은 내부 구조를 알 수 없으나, 필요 시 Clear()를 만들어 호출
            // MovePlanModel?.Clear(); 
            if (MovePlanModel == null) MovePlanModel = new Model_Unit_Develop();
        }
        private ObservableCollection<LAHMissionPlan> _LAHMissionPlans = new ObservableCollection<LAHMissionPlan>();
        public ObservableCollection<LAHMissionPlan> LAHMissionPlans
        {
            get
            {
                return _LAHMissionPlans;
            }
            set
            {
                _LAHMissionPlans = value;
                OnPropertyChanged("LAHMissionPlans");
            }
        }

        private ObservableCollection<UAVMissionPlan> _UAVMissionPlans = new ObservableCollection<UAVMissionPlan>();
        public ObservableCollection<UAVMissionPlan> UAVMissionPlans
        {
            get
            {
                return _UAVMissionPlans;
            }
            set
            {
                _UAVMissionPlans = value;
                OnPropertyChanged("UAVMissionPlans");
            }
        }

        private MissionPlanOptionInfo _missionPlanOptionInfo = new MissionPlanOptionInfo();
        public MissionPlanOptionInfo missionPlanOptionInfo
        {
            get
            {
                return _missionPlanOptionInfo;
            }
            set
            {
                _missionPlanOptionInfo = value;
                OnPropertyChanged("missionPlanOptionInfo");
            }
        }

        private Model_Unit_Develop _MovePlanModel = new Model_Unit_Develop();
        public Model_Unit_Develop MovePlanModel
        {
            get
            {
                return _MovePlanModel;
            }
            set
            {
                _MovePlanModel = value;
                OnPropertyChanged("MovePlanModel");
            }
        }
    }



    public partial class UnitObjectInfo : CommonBase
    {
        public void Clear()
        {
            ID = 0;
            Name = string.Empty;
            Type = 0;
            PlatformType = 0;
            Health = 100;
            Status = 1;
            PayLoadHealth = 1;
            FuelWarning = 1;
            entityAbnormalCause?.Clear();
            Identification = 0;
            IsLeader = 0;
            LOC?.Clear();
            velocity?.Clear();
            weapons?.Clear();
            Fuel = 15f;
            FuelConsumption = 0.00000323f;
            HorizontalFov = 0f;
            VerticalFov = 0f;
            DiagonalFov = 0f;
            SensorCenterLat = 0f;
            SensorCenterLon = 0f;
            SensorCenterAlt = 0;
            SlantRange = 0;
            FootPrintLeftTopLat = 0f;
            FootPrintLeftTopLon = 0f;
            FootPrintLeftTopAlt = 0;
            FootPrintRightTopLat = 0f;
            FootPrintRightTopLon = 0f;
            FootPrintRightTopAlt = 0;
            FootPrintRightBottomLat = 0f;
            FootPrintRightBottomLon = 0f;
            FootPrintRightBottomAlt = 0;
            FootPrintLeftBottomLat = 0f;
            FootPrintLeftBottomLon = 0f;
            FootPrintLeftBottomAlt = 0;
            DetectRange = 1500;
            AttackRange = 1000;
            AttackAccuracy = 50;
            AttackDelay = 0.1;
            AttackDamage = 1;
            DetectPixel = 20;
            RecogPixel = 10;
            AttackFlag = 1;
            Unit1TargetID = 0;
        }

        private int _ID;
        /// <summary>
        /// ID
        /// </summary>
        public int ID
        {
            get
            {
                return _ID;
            }
            set
            {
                _ID = value;
                OnPropertyChanged("ID");
            }
        }

        private string _Name;
        /// <summary>
        /// 이름
        /// </summary>
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                OnPropertyChanged("Name");
            }
        }

        private short _Type;
        /// <summary>
        /// 객체유형 - 1: UAV 2:KUH 3:LAH(Light Armed Helicopter) 4:장갑차 5:탱크 6:방사포
        /// 7:곡사포 8:고정고사포 9:특작군인 10:자주포 11:트럭 12:작전차량 13:군인
        /// </summary>
        public short Type
        {
            get
            {
                return _Type;
            }
            set
            {
                _Type = value;
                OnPropertyChanged("Type");
            }
        }

        private short _PlatformType = 0;
        /// <summary>
        /// 기종
        /// </summary>
        public short PlatformType
        {
            get
            {
                return _PlatformType;
            }
            set
            {
                _PlatformType = value;
                OnPropertyChanged("PlatformType");
            }
        }

        /// <summary>
        /// 상태
        /// 체력
        /// </summary>
        private double _Health = 100;
        public double Health
        {
            get
            {
                return _Health;
            }
            set
            {
                _Health = value;
                OnPropertyChanged("Health");
            }
        }

        /// <summary>
        /// 상태
        /// 0: 알수없음,1: 정상,2: 비정상
        /// </summary>
        private uint _Status = 1;
        public uint Status
        {
            get
            {
                return _Status;
            }
            set
            {
                _Status = value;
                OnPropertyChanged("Status");
            }
        }

        /// <summary>
        /// 상태
        /// 0: 알수없음,1: 정상,2: 비정상
        /// </summary>
        private uint _PayLoadHealth = 1;
        public uint PayLoadHealth
        {
            get
            {
                return _PayLoadHealth;
            }
            set
            {
                _PayLoadHealth = value;
                OnPropertyChanged("PayLoadHealth");
            }
        }

        /// <summary>
        /// 상태
        /// 0: 알수없음,1: 정상,2: 비정상
        /// </summary>
        private uint _FuelWarning = 1;
        public uint FuelWarning
        {
            get
            {
                return _FuelWarning;
            }
            set
            {
                _FuelWarning = value;
                OnPropertyChanged("FuelWarning");
            }
        }

        /// <summary>
        /// 상태
        /// 0: 알수없음,1: 정상,2: 비정상
        /// </summary>
        private EntityAbnormalCause _entityAbnormalCause = new EntityAbnormalCause();
        public EntityAbnormalCause entityAbnormalCause
        {
            get
            {
                return _entityAbnormalCause;
            }
            set
            {
                _entityAbnormalCause = value;
                OnPropertyChanged("entityAbnormalCause");
            }
        }

        /// <summary>
        /// 피아식별 유형
        /// 1 : 아군 / 2 : 적군
        /// </summary>
        private short _Identification;
        public short Identification
        {
            get
            {
                return _Identification;
            }
            set
            {
                _Identification = value;
                OnPropertyChanged("Identification");
            }
        }


        private short _IsLeader;
        /// <summary>
        /// 지휘 권한 - ID 1번이 지휘기라고 가정
        /// </summary>
        public short IsLeader
        {
            get
            {
                return _IsLeader;
            }
            set
            {
                _IsLeader = value;
                OnPropertyChanged("IsLeader");
            }
        }

        private CoordinateInfo _LOC = new CoordinateInfo();
        /// <summary>
        /// 위치 - 초기위치
        /// </summary>
        public CoordinateInfo LOC
        {
            get
            {
                return _LOC;
            }
            set
            {
                _LOC = value;
                OnPropertyChanged("LOC");
            }
        }

        private Velocity _velocity = new Velocity();
        /// <summary>
        /// 위치 - 초기위치
        /// </summary>
        public Velocity velocity
        {
            get
            {
                return _velocity;
            }
            set
            {
                _velocity = value;
                OnPropertyChanged("velocity");
            }
        }

        private Weapons _weapons = new Weapons();
        /// <summary>
        /// 무장 정보
        /// </summary>
        public Weapons weapons
        {
            get
            {
                return _weapons;
            }
            set
            {
                _weapons = value;
                OnPropertyChanged("weapons");
            }
        }

        private float _Fuel;
        /// <summary>
        /// 연료량(L?)
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

        private float _FuelConsumption;
        /// <summary>
        /// 연료량(L?)
        /// </summary>
        public float FuelConsumption
        {
            get
            {
                return _FuelConsumption;
            }
            set
            {
                _FuelConsumption = value;
                OnPropertyChanged("FuelConsumption");
            }
        }

        public float HorizontalFov;
        public float VerticalFov;
        public float DiagonalFov;
        public float SensorCenterLat;
        public float SensorCenterLon;
        public int SensorCenterAlt;
        public int SlantRange;
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

        private double _DetectRange = 1500;
        public double DetectRange
        {

            get { return _DetectRange; }
            set
            {
                _DetectRange = value;
                OnPropertyChanged("DetectRange");
            }
        }

        private double _AttackRange = 1000;
        public double AttackRange
        {

            get { return _AttackRange; }
            set
            {
                _AttackRange = value;
                OnPropertyChanged("AttackRange");
            }
        }

        private double _AttackAccuracy = 50;
        public double AttackAccuracy
        {

            get { return _AttackAccuracy; }
            set
            {
                _AttackAccuracy = value;
                OnPropertyChanged("AttackAccuracy");
            }
        }

        private double _AttackDelay = 0.1;
        public double AttackDelay
        {

            get { return _AttackDelay; }
            set
            {
                _AttackDelay = value;
                OnPropertyChanged("AttackDelay");
            }
        }

        private double _AttackDamage = 1;
        public double AttackDamage
        {

            get { return _AttackDamage; }
            set
            {
                _AttackDamage = value;
                OnPropertyChanged("AttackDamage");
            }
        }

        private int _UAVTestMode = 0;
        public int UAVTestMode
        {

            get { return _UAVTestMode; }
            set
            {
                _UAVTestMode = value;
                OnPropertyChanged("UAVTestMode");
            }
        }

        private double _DetectPixel = 20;
        public double DetectPixel
        {

            get { return _DetectPixel; }
            set
            {
                _DetectPixel = value;
                OnPropertyChanged("DetectPixel");
            }
        }

        private double _RecogPixel = 10;
        public double RecogPixel
        {

            get { return _RecogPixel; }
            set
            {
                _RecogPixel = value;
                OnPropertyChanged("RecogPixel");
            }
        }

        private int _AttackFlag = 1;
        public int AttackFlag
        {

            get { return _AttackFlag; }
            set
            {
                _AttackFlag = value;
                OnPropertyChanged("AttackFlag");
            }
        }

        private int _Unit1TargetID = 0;
        public int Unit1TargetID
        {
            get { return _Unit1TargetID; }
            set
            {
                _Unit1TargetID = value;
                OnPropertyChanged("Unit1TargetID");
            }
        }

        //enum enumEntityStatus
        //{
        //    STATUS_FLIGHT = 0;
        //    STATUS_WAIT = 1;
        //    STATUS_ATTACK = 2;
        //    STATUS_DEATH = 3;
        //}

        private enumEntityStatus _LAHStatus = enumEntityStatus.StatusWait;
        public enumEntityStatus LAHStatus
        {

            get { return _LAHStatus; }
            set
            {
                _LAHStatus = value;
                OnPropertyChanged("LAHStatus");
            }
        }

        //private ObservableCollection<CoordinateInfo> _PathPlanList = new ObservableCollection<CoordinateInfo>();
        ///// <summary>
        ///// 이동경로 리스트
        ///// </summary>
        //public ObservableCollection<CoordinateInfo> PathPlanList
        //{
        //    get
        //    {
        //        return _PathPlanList;
        //    }
        //    set
        //    {
        //        _PathPlanList = value;
        //        OnPropertyChanged("PathPlanList");
        //    }
        //}

        // 피격/추락 상황에 대해 이미 팝업을 띄웠는지 체크하는 플래그
        public bool IsAbnormalPopupShown { get; set; } = false;
    }

    public partial class Velocity : CommonBase
    {
        public void Clear()
        {
            Speed = 0f;
            Heading = 0f;
        }

        private float _Speed;
        /// <summary>
        /// 속도 - Km/h
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

        private float _Heading;
        /// <summary>
        /// 헤딩
        /// </summary>
        public float Heading
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
    }

    public partial class Weapons : CommonBase
    {
        public void Clear()
        {
            Type1 = 0;
            Type2 = 0;
            Type3 = 0;
        }

        private uint _Type1;
        /// <summary>
        /// 무장타입1 - 기관총?(ea)
        /// </summary>
        public uint Type1
        {
            get
            {
                return _Type1;
            }
            set
            {
                _Type1 = value;
                OnPropertyChanged("Type1");
            }
        }

        private uint _Type2;
        /// <summary>
        /// 무장타입2 - 로켓?(ea)
        /// </summary>
        public uint Type2
        {
            get
            {
                return _Type2;
            }
            set
            {
                _Type2 = value;
                OnPropertyChanged("Type2");
            }
        }

        private uint _Type3;
        /// <summary>
        /// 무장타입3 - 미사일?(ea)
        /// </summary>
        public uint Type3
        {
            get
            {
                return _Type3;
            }
            set
            {
                _Type3 = value;
                OnPropertyChanged("Type3");
            }
        }
    }

    public partial class EntityAbnormalCause : CommonBase
    {
        public void Clear()
        {
            Hit = 0;
            Loss1 = 0;
            Loss2 = 0;
            Loss3 = 0;
            FuelWarning = 0;
            FuelDanger = 0;
            FuelZero = 0;
            Crash = 0;
            //Sensor = 0;
        }

        private int _Hit;
        public int Hit
        {
            get
            {
                return _Hit;
            }
            set
            {
                _Hit = value;
                OnPropertyChanged("Hit");
            }
        }

        private int _Loss1;
        public int Loss1
        {
            get
            {
                return _Loss1;
            }
            set
            {
                _Loss1 = value;
                OnPropertyChanged("Loss1");
            }
        }

        private int _Loss2;
        public int Loss2
        {
            get
            {
                return _Loss2;
            }
            set
            {
                _Loss2 = value;
                OnPropertyChanged("Loss2");
            }
        }

        private int _Loss3;
        public int Loss3
        {
            get
            {
                return _Loss3;
            }
            set
            {
                _Loss3 = value;
                OnPropertyChanged("Loss3");
            }
        }

        private int _FuelWarning;
        public int FuelWarning
        {
            get
            {
                return _FuelWarning;
            }
            set
            {
                _FuelWarning = value;
                OnPropertyChanged("FuelWarning");
            }
        }

        private int _FuelDanger;
        public int FuelDanger
        {
            get
            {
                return _FuelDanger;
            }
            set
            {
                _FuelDanger = value;
                OnPropertyChanged("FuelDanger");
            }
        }

        private int _FuelZero;
        public int FuelZero
        {
            get
            {
                return _FuelZero;
            }
            set
            {
                _FuelZero = value;
                OnPropertyChanged("FuelZero");
            }
        }

        private int _Crash;
        public int Crash
        {
            get
            {
                return _Crash;
            }
            set
            {
                _Crash = value;
                OnPropertyChanged("Crash");
            }
        }

        //private int _Sensor;
        //public int Sensor
        //{
        //    get
        //    {
        //        return _Sensor;
        //    }
        //    set
        //    {
        //        _Sensor = value;
        //        OnPropertyChanged("Sensor");
        //    }
        //}
    }

}
