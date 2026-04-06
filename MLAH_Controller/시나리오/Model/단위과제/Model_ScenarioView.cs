using MLAH_Controller;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace MLAH_Controller
{
    public class Model_ScenarioView : CommonBase
    {
        //협업기저임무
        public string[] IndividualMissionTextRef = {"-","광역영역수색", "협역영역수색", "편대통로정찰(Leader)", "편대통로정찰(Follower)", "표적추적", "이동", "전술기동비행", "표적공격", "대기비행",
            "도심영역수색", "엄호", "광역영역경계", "협역영역경계"};

        public string[] BaseActionTextRef = {"-","계획실행모드 촬영", "좌표지향모드 촬영", "구역탐색모드 촬영", "구간탐색모드 촬영", "자동주사모드 촬영", "기체고정모드 촬영", "자동추적모드 촬영", "경로 이동 비행", "표적 추적 비행", "편대비행(Follower)",
            "점항법 비행","RTB 비행", "통제권이양지 이동", "미사일 공격", "로켓 공격", "기관포 공격"};

        public void clear()
        {
            // 기본값으로 설정
            ScenarioName = "";  // 문자열 프로퍼티의 기본값은 빈 문자열
            StartTime = DateTime.MinValue;  // DateTime 프로퍼티의 기본값 설정
            EndTime = DateTime.MinValue;

            // 컬렉션 비우기
            ScenarioObjects.Clear();
            AbnormalZones.Clear();
            coBaseMissions.Clear();

            // 복합 객체의 경우, 새 인스턴스로 초기화하거나 기본값 설정
            battlefieldEnv = new BattlefieldEnv();  // 새 인스턴스 할당으로 초기화

            // 메서드 내에서 PropertyChanged 이벤트를 발생시키는 것은 각 세터에서 처리
        }

        private string _ScenarioName;
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

        private DateTime _StartTime;
        public DateTime StartTime
        {
            get
            {
                return _StartTime;
            }
            set
            {
                _StartTime = value;
                OnPropertyChanged("StartTime");
            }
        }

        private DateTime _EndTime;
        public DateTime EndTime
        {
            get
            {
                return _EndTime;
            }
            set
            {
                _EndTime = value;
                OnPropertyChanged("EndTime");
            }
        }

        private ObservableCollection<Scenario_Object> _ScenarioObjects = new ObservableCollection<Scenario_Object>();
        public ObservableCollection<Scenario_Object> ScenarioObjects
        {
            get
            {
                return _ScenarioObjects;
            }
            set
            {
                _ScenarioObjects = value;
                OnPropertyChanged("ScenarioObjects");
            }
        }

        private List<Scenario_AbnormalZone> _AbnormalZones = new List<Scenario_AbnormalZone>();
        public List<Scenario_AbnormalZone> AbnormalZones
        {
            get
            {
                return _AbnormalZones;
            }
            set
            {
                _AbnormalZones = value;
                OnPropertyChanged("AbnormalZones");
            }
        }

        private BattlefieldEnv _battlefieldEnv = new BattlefieldEnv();
        public BattlefieldEnv battlefieldEnv
        {
            get
            {
                return _battlefieldEnv;
            }
            set
            {
                _battlefieldEnv = value;
                OnPropertyChanged("battlefieldEnv");
            }
        }

        //이거도 추후에 정리해야할 듯, 유인기/무인기 객체정보 안에 있어야 함
        private List<CoBaseMission> _coBaseMissions = new List<CoBaseMission>();
        public List<CoBaseMission> coBaseMissions
        {
            get
            {
                return _coBaseMissions;
            }
            set
            {
                _coBaseMissions = value;
                OnPropertyChanged("coBaseMissions");
            }


        }

        //이 Model에 선언되는 클래스는 UI 기능과 연계를 위한 것
        //json, gprc 관련 인터페이스 및 구조체는 새로 정의 필요할수도 있음 - converter 활용
        //객체 공통
        //메세지 송신시에는 인터페이스 다르게 해야할 수도? - 이름이나 지휘기 여부 필요성 생각해보기 - 유인기/무인기/표적으로 구조체 나눌거냐 / 객체 구조체로 통일해서 필요한거만 쓸거냐


      
    }

    public class Scenario_Object : CommonBase
    {

        private int _ObjectNum = 0;
        /// <summary>
        /// 표적 번호 -  0 : 지휘기  / 1 : 편대기  / 2 : 편대기  / 3 : 무인기 1  / 4 : 무인기 2  / 5 : 무인기 3
        /// </summary>
        public int ObjectNum
        {
            get
            {
                return _ObjectNum;
            }
            set
            {
                _ObjectNum = value;
                switch (value)
                {
                    case 0:
                        {
                            ObjectName = "유인기(0)";
                        }
                        break;
                    case 1:
                        {
                            ObjectName = "유인기(1)";
                        }
                        break;
                    case 2:
                        {
                            ObjectName = "유인기(2)";
                        }
                        break;
                    case 3:
                        {
                            ObjectName = "무인기(3)";
                        }
                        break;
                    case 4:
                        {
                            ObjectName = "무인기(4)";
                        }
                        break;
                    case 5:
                        {
                            ObjectName = "무인기(5)";
                        }
                        break;
                    default:
                        {
                            ObjectName = string.Format(ObjectName + "({0})", value);
                        }
                        break;

                }
                OnPropertyChanged("ObjectNum");
            }
        }

        private int _ObjectType;
        public int ObjectType
        {
            get
            {
                return _ObjectType;
            }
            set
            {
                _ObjectType = value;
                OnPropertyChanged("ObjectType");
            }   
        }

        private bool _ObjectStatus = false;
        public bool ObjectStatus
        {
            get
            {
                return _ObjectStatus;
            }
            set
            {
                _ObjectStatus = value;
                if (value == true)
                {
                    ObjectStatusText = "정상";
                    StatusColor = Application.Current.Resources["MLAH_COLOR_Value_Brush"] as System.Windows.Media.Brush;
                }
                else
                {
                    ObjectStatusText = "비정상";
                    StatusColor = new SolidColorBrush(Colors.Red);
                }
                OnPropertyChanged("ObjectStatus");
            }
        }

        private string _ObjectStatusText = "비정상";
        public string ObjectStatusText
        {
            get
            {
                return _ObjectStatusText;
            }
            set
            {
                _ObjectStatusText = value;
                OnPropertyChanged("ObjectStatusText");
            }
        }

        private int _AbnormalReason = 0;
        public int AbnormalReason
        {
            get
            {
                return _AbnormalReason;
            }
            set
            {
                _AbnormalReason = value;
                if (value == 1)
                {
                    AbnormalReasonText = "센서 고장";
                }
                else if (value == 2)
                {
                    AbnormalReasonText = "장비 고장";
                }
                else if (value == 3)
                {
                    AbnormalReasonText = "피격";
                }
                else
                {
                    AbnormalReasonText = "-";
                }
                OnPropertyChanged("AbnormalReason");
            }
        }

        private string _AbnormalReasonText = "-";
        public string AbnormalReasonText
        {
            get
            {
                return _AbnormalReasonText;
            }
            set
            {
                _AbnormalReasonText = value;
                OnPropertyChanged("AbnormalReasonText");
            }
        }

        private System.Windows.Media.Brush _StatusColor = new SolidColorBrush(Colors.Red);
        public System.Windows.Media.Brush StatusColor
        {

            get
            {
                return _StatusColor;
            }
            set
            {
                _StatusColor = value;
                OnPropertyChanged("StatusColor");
            }
        }

        private string _ObjectName;
        public string ObjectName
        {
            get
            {
                return _ObjectName;
            }
            set
            {
                _ObjectName = value;
                OnPropertyChanged("ObjectName");
            }
        }

        private double _ObjectLAT;
        public double ObjectLAT
        {
            get
            {
                return _ObjectLAT;
            }
            set
            {
                _ObjectLAT = value;
                OnPropertyChanged("ObjectLAT");
            }
        }

        private double _ObjectLON;
        public double ObjectLON
        {
            get
            {
                return _ObjectLON;
            }
            set
            {
                _ObjectLON = value;
                OnPropertyChanged("ObjectLON");
            }
        }

        private double _ObjectALT;
        public double ObjectALT
        {
            get
            {
                return _ObjectALT;
            }
            set
            {
                _ObjectALT = value;
                OnPropertyChanged("ObjectALT");
            }
        }

        private double _ObjectRoll;
        public double ObjectRoll
        {
            get
            {
                return _ObjectRoll;
            }
            set
            {
                _ObjectRoll = value;
                OnPropertyChanged("ObjectRoll");
            }
        }

        private double _ObjectPitch;
        public double ObjectPitch
        {
            get
            {
                return _ObjectPitch;
            }
            set
            {
                _ObjectPitch = value;
                OnPropertyChanged("ObjectPitch");
            }
        }

        private double _ObjectYaw;
        public double ObjectYaw
        {
            get
            {
                return _ObjectYaw;
            }
            set
            {
                _ObjectYaw = value;
                OnPropertyChanged("ObjectYaw");
            }
        }

        private double _ObjectSpeedX;
        public double ObjectSpeedX
        {
            get
            {
                return _ObjectSpeedX;
            }
            set
            {
                _ObjectSpeedX = value;
                OnPropertyChanged("ObjectSpeedX");
            }
        }

        private double _ObjectSpeedY;
        public double ObjectSpeedY
        {
            get
            {
                return _ObjectSpeedY;
            }
            set
            {
                _ObjectSpeedY = value;
                OnPropertyChanged("ObjectSpeedY");
            }
        }

        private double _ObjectSpeedZ;
        public double ObjectSpeedZ
        {
            get
            {
                return _ObjectSpeedZ;
            }
            set
            {
                _ObjectSpeedZ = value;
                OnPropertyChanged("ObjectSpeedZ");
            }
        }

        private double _ObjectVelocityRoll;
        public double ObjectVelocityRoll
        {
            get
            {
                return _ObjectVelocityRoll;
            }
            set
            {
                _ObjectVelocityRoll = value;
                OnPropertyChanged("ObjectVelocityRoll");
            }
        }

        private double _ObjectVelocityPitch;
        public double ObjectVelocityPitch
        {
            get
            {
                return _ObjectVelocityPitch;
            }
            set
            {
                _ObjectVelocityPitch = value;
                OnPropertyChanged("ObjectVelocityPitch");
            }
        }

        private double _ObjectVelocityYaw;
        public double ObjectVelocityYaw
        {
            get
            {
                return _ObjectVelocityYaw;
            }
            set
            {
                _ObjectVelocityYaw = value;
                OnPropertyChanged("ObjectVelocityYaw");
            }
        }

        private double _ObjectHeading;
        public double ObjectHeading
        {
            get
            {
                return _ObjectHeading;
            }
            set
            {
                _ObjectHeading = value;
                OnPropertyChanged("ObjectHeading");
            }
        }



        private bool _IsLeader;
        public bool IsLeader
        {
            get
            {
                return _IsLeader;
            }
            set
            {
                _IsLeader = value;
                if (value == true)
                {
                    IsLeaderText = "Y";
                }
                else
                {
                    IsLeaderText = "N";
                }
                OnPropertyChanged("IsLeader");
            }
        }

        private string _IsLeaderText = "N";
        public string IsLeaderText
        {
            get
            {
                return _IsLeaderText;
            }
            set
            {
                _IsLeaderText = value;
                OnPropertyChanged("IsLeaderText");
            }
        }

        private double _ObjectFuel;
        public double ObjectFuel
        {
            get
            {
                return _ObjectFuel;
            }
            set
            {
                _ObjectFuel = value;
                OnPropertyChanged("ObjectFuel");
            }
        }

        private double _ObjectFuelConsumption;
        public double ObjectFuelConsumption
        {
            get
            {
                return _ObjectFuelConsumption;
            }
            set
            {
                _ObjectFuelConsumption = value;
                OnPropertyChanged("ObjectFuelConsumption");
            }
        }

        private int _DroneLinkNumber;
        public int DroneLinkNumber
        {
            get
            {
                return _DroneLinkNumber;
            }
            set
            {
                _DroneLinkNumber = value;
                OnPropertyChanged("DroneLinkNumber");
            }
        }

        private int _TargetMode;
        public int TargetMode
        {
            get
            {
                return _TargetMode;
            }
            set
            {
                _TargetMode = value;
                if (value == 1)
                {
                    TargetModeText = "고정";
                }
                else
                {
                    TargetModeText = "이동";
                }
                OnPropertyChanged("TargetMode");
            }
        }

        private string _TargetModeText;
        public string TargetModeText
        {
            get
            {
                return _TargetModeText;
            }
            set
            {
                _TargetModeText = value;
                OnPropertyChanged("TargetModeText");
            }
        }

        private int _TargetType;
        public int TargetType
        {
            get
            {
                return _TargetType;
            }
            set
            {
                _TargetType = value;
                if (TargetIdentify == 1)
                {
                    if(_TargetType==1)
                    {
                        TargetTypeText = "유인기동헬기";
                    }
                    else if (_TargetType == 2)
                    {
                        TargetTypeText = "장갑차";
                    }
                    else if (_TargetType == 3)
                    {
                        TargetTypeText = "탱크";
                    }
                    else if (_TargetType == 4)
                    {
                        TargetTypeText = "자주포";
                    }
                    else if (_TargetType == 5)
                    {
                        TargetTypeText = "트럭";
                    }
                    else if (_TargetType == 6)
                    {
                        TargetTypeText = "작전차량";
                    }
                    else if (_TargetType == 7)
                    {
                        TargetTypeText = "군인";
                    }
                }
                else if(TargetIdentify == 2)
                {
                    if (_TargetType == 1)
                    {
                        TargetTypeText = "장갑차";
                    }
                    else if (_TargetType == 2)
                    {
                        TargetTypeText = "탱크";
                    }
                    else if (_TargetType == 3)
                    {
                        TargetTypeText = "방사포";
                    }
                    else if (_TargetType == 4)
                    {
                        TargetTypeText = "곡사포";
                    }
                    else if (_TargetType == 5)
                    {
                        TargetTypeText = "고정고사포";
                    }
                    else if (_TargetType == 6)
                    {
                        TargetTypeText = "특작군인";
                    }
                }
                OnPropertyChanged("TargetType");
            }
        }

        private string _TargetTypeText;
        public string TargetTypeText
        {
            get
            {
                return _TargetTypeText;
            }
            set
            {
                _TargetTypeText = value;
                OnPropertyChanged("TargetTypeText");
            }
        }

        private int _TargetPlatform;
        public int TargetPlatform
        {
            get
            {
                return _TargetPlatform;
            }
            set
            {
                _TargetPlatform = value;
                if (TargetIdentify == 1)
                {
                    if (TargetType == 1)
                    {
                        if(_TargetPlatform == 1)
                        {
                            TargetPlatformText = "KUH";
                        }
                    }
                    else if (TargetType == 2)
                    {
                        if (_TargetPlatform == 1)
                        {
                            TargetPlatformText = "K200";
                        }
                        else if(_TargetPlatform == 2)
                        {
                            TargetPlatformText = "K221";
                        }
                        else if (_TargetPlatform == 3)
                        {
                            TargetPlatformText = "K806";
                        }
                        else if (_TargetPlatform == 4)
                        {
                            TargetPlatformText = "K808";
                        }
                    }
                    else if (TargetType == 3)
                    {
                        if (_TargetPlatform == 1)
                        {
                            TargetPlatformText = "K1A1";
                        }
                        else if (_TargetPlatform == 2)
                        {
                            TargetPlatformText = "K2";
                        }
                    }
                    else if (TargetType == 4)
                    {
                        if (_TargetPlatform == 1)
                        {
                            TargetPlatformText = "K55A1";
                        }
                        else if (_TargetPlatform == 2)
                        {
                            TargetPlatformText = "K9";
                        }
                    }
                    else if (TargetType == 5)
                    {
                        if (_TargetPlatform == 1)
                        {
                            TargetPlatformText = "K311";
                        }
                        else if (_TargetPlatform == 2)
                        {
                            TargetPlatformText = "K511";
                        }
                    }
                    else if (TargetType == 6)
                    {
                        if (_TargetPlatform == 1)
                        {
                            TargetPlatformText = "K131";
                        }
                    }
                    else if (TargetType == 7)
                    {
                        if (_TargetPlatform == 1)
                        {
                            TargetPlatformText = "군인(공중강습)";
                        }
                    }
                }
                else if (TargetIdentify == 2)
                {
                    if (TargetType == 1)
                    {
                        if (_TargetPlatform == 1)
                        {
                            TargetPlatformText = "M2010";
                        }
                    }
                    else if (TargetType == 2)
                    {
                        if (_TargetPlatform == 1)
                        {
                            TargetPlatformText = "T-55";
                        }
                        else if (_TargetPlatform == 2)
                        {
                            TargetPlatformText = "T-72";
                        }
                        else if (_TargetPlatform == 3)
                        {
                            TargetPlatformText = "천마호";
                        }
                        else if (_TargetPlatform == 4)
                        {
                            TargetPlatformText = "폭풍호";
                        }
                    }
                    else if (TargetType == 3)
                    {
                        if (_TargetPlatform == 1)
                        {
                            TargetPlatformText = "M1992 (122mm)";
                        }
                    }
                    else if (TargetType == 4)
                    {
                        if (_TargetPlatform == 1)
                        {
                            TargetPlatformText = "M1938 (122mm)";
                        }
                    }
                    else if (TargetType == 5)
                    {
                        if (_TargetPlatform == 1)
                        {
                            TargetPlatformText = "ZPU-4";
                        }
                        else if (_TargetPlatform == 2)
                        {
                            TargetPlatformText = "ZPU-23";
                        }
                        else if (_TargetPlatform == 3)
                        {
                            TargetPlatformText = "KS-12";
                        }
                        else if (_TargetPlatform == 4)
                        {
                            TargetPlatformText = "KS-19";
                        }
                        else if (_TargetPlatform == 5)
                        {
                            TargetPlatformText = "SA-3";
                        }
                    }
                    else if (TargetType == 6)
                    {
                        if (_TargetPlatform == 1)
                        {
                            TargetPlatformText = "특작군인 (SA-16)";
                        }
                    }
                }
                OnPropertyChanged("TargetPlatform");
            }
        }

        private string _TargetPlatformText;
        public string TargetPlatformText
        {
            get
            {
                return _TargetPlatformText;
            }
            set
            {
                _TargetPlatformText = value;
                OnPropertyChanged("TargetPlatformText");
            }
        }

        private int _TargetIdentify = 1;
        public int TargetIdentify
        {
            get
            {
                return _TargetIdentify;
            }
            set
            {
                _TargetIdentify = value;
                if (value == 1)
                {
                    TargetIdentifyText = "아군";
                }
                else
                {
                    TargetIdentifyText = "적군";
                }
                OnPropertyChanged("TargetIdentify");
            }
        }

        private string _TargetIdentifyText;
        public string TargetIdentifyText
        {
            get
            {
                return _TargetIdentifyText;
            }
            set
            {
                _TargetIdentifyText = value;
                OnPropertyChanged("TargetIdentifyText");
            }
        }

        //private int _SelectedHelicopterWeapon;
        //public int SelectedHelicopterWeapon
        //{
        //    get
        //    {
        //        return _SelectedHelicopterWeapon;
        //    }
        //    set
        //    {
        //        _SelectedHelicopterWeapon = value;
        //        if (value == 1)
        //        {
        //            SelectedHelicopterWeaponText = "기관포";
        //        }
        //        else if (value == 2)
        //        {
        //            SelectedHelicopterWeaponText = "유도탄";
        //        }
        //        else if (value == 3)
        //        {
        //            SelectedHelicopterWeaponText = "공대지미사일";
        //        }
        //        else
        //        {
        //            SelectedHelicopterWeaponText = "No Statement";
        //        }
        //        OnPropertyChanged("SelectedHelicopterWeapon");
        //    }
        //}

        private string _SelectedHelicopterWeaponText = "No Statement";
        public string SelectedHelicopterWeaponText
        {
            get
            {
                return _SelectedHelicopterWeaponText;
            }
            set
            {
                _SelectedHelicopterWeaponText = value;
                OnPropertyChanged("SelectedHelicopterWeaponText");
            }
        }

        private double _MissileDamage = 0;
        public double MissileDamage
        {
            get
            {
                return _MissileDamage;
            }
            set
            {
                _MissileDamage = value;
                OnPropertyChanged("MissileDamage");
            }
        }

        private ushort _MissileRound = 0;
        public ushort MissileRound
        {
            get
            {
                return _MissileRound;
            }
            set
            {
                _MissileRound = value;
                OnPropertyChanged("MissileRound");
            }
        }

        private double _RocketDamage = 0;
        public double RocketDamage
        {
            get
            {
                return _RocketDamage;
            }
            set
            {
                _RocketDamage = value;
                OnPropertyChanged("RocketDamage");
            }
        }

        private ushort _RocketRound = 0;
        public ushort RocketRound
        {
            get
            {
                return _RocketRound;
            }
            set
            {
                _RocketRound = value;
                OnPropertyChanged("RocketRound");
            }
        }

        private List<CoBaseMission> _coBaseMissions = new List<CoBaseMission>();
        public List<CoBaseMission> coBaseMissions
        {
            get
            {
                return _coBaseMissions;
            }
            set
            {
                _coBaseMissions = value;
                OnPropertyChanged("coBaseMissions");
            }
        }

        private List<IndividualMission> _IndividualMissions = new List<IndividualMission>();
        public List<IndividualMission> IndividualMissions
        {
            get
            {
                return _IndividualMissions;
            }
            set
            {
                _IndividualMissions = value;
                OnPropertyChanged("IndividualMissions");
            }
        }

        private List<TargetMovePlanWayPoint> _TargetMovePlan = new List<TargetMovePlanWayPoint>();
        public List<TargetMovePlanWayPoint> TargetMovePlan
        {
            get
            {
                return _TargetMovePlan;
            }
            set
            {
                _TargetMovePlan = value;
                OnPropertyChanged("TargetMovePlan");
            }
        }

        public int CurrentIndividualMission = 0;

    }

    public class BattlefieldEnv : CommonBase
    {
        private int _BattlefieldArea;
        public int BattlefieldArea
        {
            get
            {
                return _BattlefieldArea;
            }
            set
            {
                _BattlefieldArea = value;
                OnPropertyChanged("BattlefieldArea");
            }
        }

        private DateTime _BattlefieldASetTime;
        public DateTime BattlefieldASetTime
        {
            get
            {
                return _BattlefieldASetTime;
            }
            set
            {
                _BattlefieldASetTime = value;
                OnPropertyChanged("BattlefieldASetTime");
            }
        }

        private int _BattlefieldSeason;
        public int BattlefieldSeason
        {
            get
            {
                return _BattlefieldSeason;
            }
            set
            {
                _BattlefieldSeason = value;
                OnPropertyChanged("BattlefieldSeason");
            }
        }

        private int _BattlefieldWeather;
        public int BattlefieldWeather
        {
            get
            {
                return _BattlefieldWeather;
            }
            set
            {
                _BattlefieldWeather = value;
                OnPropertyChanged("BattlefieldWeather");
            }
        }

        private int _BattlefieldCloud;
        public int BattlefieldCloud
        {
            get
            {
                return _BattlefieldCloud;
            }
            set
            {
                _BattlefieldCloud = value;
                OnPropertyChanged("BattlefieldCloud");
            }
        }
    }

    public class Scenario_AbnormalZone : CommonBase
    {
        private int _AbnormalZoneType;
        public int AbnormalZoneType
        {
            get
            {
                return _AbnormalZoneType;
            }
            set
            {
                _AbnormalZoneType = value;
                switch (value)
                {
                    case 0:
                        {
                            AbnormalZoneTypeText = "전파 방해";
                        }
                        break;

                    case 1:
                        {
                            AbnormalZoneTypeText = "위험 구역";
                        }
                        break;

                    case 2:
                        {
                            AbnormalZoneTypeText = "No Statement";
                        }
                        break;

                    default:
                        break;
                }
                OnPropertyChanged("AbnormalZoneType");
            }
        }

        private string _AbnormalZoneTypeText;
        public string AbnormalZoneTypeText
        {
            get
            {
                return _AbnormalZoneTypeText;
            }
            set
            {
                _AbnormalZoneTypeText = value;
                OnPropertyChanged("AbnormalZoneTypeText");
            }
        }

        private double _AbnormalZoneStartLat;
        public double AbnormalZoneStartLat
        {
            get
            {
                return _AbnormalZoneStartLat;
            }
            set
            {
                _AbnormalZoneStartLat = value;
                OnPropertyChanged("AbnormalZoneStartLat");
            }
        }

        private double _AbnormalZoneStartLon;
        public double AbnormalZoneStartLon
        {
            get
            {
                return _AbnormalZoneStartLon;
            }
            set
            {
                _AbnormalZoneStartLon = value;
                OnPropertyChanged("AbnormalZoneStartLon");
            }
        }

        private double _AbnormalZoneEndLat;
        public double AbnormalZoneEndLat
        {
            get
            {
                return _AbnormalZoneEndLat;
            }
            set
            {
                _AbnormalZoneEndLat = value;
                OnPropertyChanged("AbnormalZoneEndLat");
            }
        }

        private double _AbnormalZoneEndLon;
        public double AbnormalZoneEndLon
        {
            get
            {
                return _AbnormalZoneEndLon;
            }
            set
            {
                _AbnormalZoneEndLon = value;
                OnPropertyChanged("AbnormalZoneEndLon");
            }
        }

        private double _AbnormalZoneRectArea;
        public double AbnormalZoneRectArea
        {
            get
            {
                return _AbnormalZoneRectArea;
            }
            set
            {
                _AbnormalZoneRectArea = value;
                OnPropertyChanged("AbnormalZoneRectArea");
            }
        }
    }

    public class CoBaseMission : CommonBase
    {
        private int _MissionIndex;
        public int MissionIndex
        {
            get
            {
                return _MissionIndex;
            }
            set
            {
                _MissionIndex = value;
                OnPropertyChanged("MissionIndex");
            }
        }

        //MissionType에 따라 활용하는 필드가 다름 / ex-협업기동이면 시작점, 끝점 // 경계나 수색이면 지역 // 엄호면????
        private int _MissionType;
        public int MissionType
        {
            get
            {
                return _MissionType;
            }
            set
            {
                _MissionType = value;
                if (value == 1)
                {
                    MissionTypeText = "협업기동";
                }
                else if (value == 2)
                {
                    MissionTypeText = "협업수색공격";
                }
                else if (value == 3)
                {
                    MissionTypeText = "협업경계";
                }
                else if (value == 4)
                {
                    MissionTypeText = "협업공중부대엄호";
                }
                else if (value == 5)
                {
                    MissionTypeText = "협업지상부대엄호";
                }
                else if (value == 6)
                {
                    MissionTypeText = "협업도심수색공격";
                }
                else
                {

                }
                OnPropertyChanged("MissionType");
            }
        }

        private string _MissionTypeText;
        public string MissionTypeText
        {
            get
            {
                return _MissionTypeText;
            }
            set
            {
                _MissionTypeText = value;
                OnPropertyChanged("MissionTypeText");
            }
        }

        private double _StartLat;
        public double StartLat
        {
            get
            {
                return _StartLat;
            }
            set
            {
                _StartLat = value;
                OnPropertyChanged("StartLat");
            }
        }

        private double _StartLon;
        public double StartLon
        {
            get
            {
                return _StartLon;
            }
            set
            {
                _StartLon = value;
                OnPropertyChanged("StartLon");
            }
        }

        private double _StartAlt;
        public double StartAlt
        {
            get
            {
                return _StartAlt;
            }
            set
            {
                _StartAlt = value;
                OnPropertyChanged("StartAlt");
            }
        }

        private double _EndLat;
        public double EndLat
        {
            get
            {
                return _EndLat;
            }
            set
            {
                _EndLat = value;
                OnPropertyChanged("EndLat");
            }
        }

        private double _EndLon;
        public double EndLon
        {
            get
            {
                return _EndLon;
            }
            set
            {
                _EndLon = value;
                OnPropertyChanged("EndLon");
            }
        }

        private double _EndAlt;
        public double EndAlt
        {
            get
            {
                return _EndAlt;
            }
            set
            {
                _EndAlt = value;
                OnPropertyChanged("EndAlt");
            }
        }

    }

    public class IndividualMission : CommonBase
    {
        private int _MissionIndex;
        public int MissionIndex
        {
            get
            {
                return _MissionIndex;
            }
            set
            {
                _MissionIndex = value;
                OnPropertyChanged("MissionIndex");
            }
        }

        //MissionType에 따라 활용하는 필드가 다름 / ex-협업기동이면 시작점, 끝점 // 경계나 수색이면 지역 // 엄호면????
        private int _MissionType;
        public int MissionType
        {
            get
            {
                return _MissionType;
            }
            set
            {
                _MissionType = value;
                MissionTypeText = new Model_ScenarioView().IndividualMissionTextRef[value];
                OnPropertyChanged("MissionType");
            }
        }

        private int _BaseCoMission;
        public int BaseCoMission
        {
            get
            {
                return _BaseCoMission;
            }
            set
            {
                _BaseCoMission = value;
                OnPropertyChanged("BaseCoMission");
            }
        }

        private string _MissionTypeText;
        public string MissionTypeText
        {
            get
            {
                return _MissionTypeText;
            }
            set
            {
                _MissionTypeText = value;
                OnPropertyChanged("MissionTypeText");
            }
        }

        //표적추적 / 표적공격
        //Scenario_Object TargetInfo;

        //영역 수색

        //웨이포인트
        private List<Waypoint> _MissionWaypoints = new List<Waypoint>();
        public List<Waypoint> MissionWaypoints
        {
            get
            {
                return _MissionWaypoints;
            }
            set
            {
                _MissionWaypoints = value;
                OnPropertyChanged("MissionWaypoints");
            }
        }

        public int CurrentWaypoint = 0;
    }
    public class Waypoint : CommonBase
    {
        private int _WaypointIndex;
        public int WaypointIndex
        {
            get
            {
                return _WaypointIndex;
            }
            set
            {
                _WaypointIndex = value;
                OnPropertyChanged("WaypointIndex");
            }
        }

        private double _WaypointLat = 0;
        public double WaypointLat
        {
            get
            {
                return _WaypointLat;
            }
            set
            {
                _WaypointLat = value;
                OnPropertyChanged("WaypointLat");
            }
        }

        private double _WaypointLon = 0;
        public double WaypointLon
        {
            get
            {
                return _WaypointLon;
            }
            set
            {
                _WaypointLon = value;
                OnPropertyChanged("WaypointLon");
            }
        }

        private double _WaypointAlt = 0;
        public double WaypointAlt
        {
            get
            {
                return _WaypointAlt;
            }
            set
            {
                _WaypointAlt = value;
                OnPropertyChanged("WaypointAlt");
            }
        }

        

        private double _WaypointMoveAlt = 0;
        public double WaypointMoveAlt
        {
            get
            {
                return _WaypointMoveAlt;
            }
            set
            {
                _WaypointMoveAlt = value;
                OnPropertyChanged("WaypointMoveAlt");
            }
        }

        private double _WaypointMoveSpeed = 0;
        public double WaypointMoveSpeed
        {
            get
            {
                return _WaypointMoveSpeed;
            }
            set
            {
                _WaypointMoveSpeed = value;
                OnPropertyChanged("WaypointMoveSpeed");
            }
        }

        private BaseBehavior _baseBehavior = new BaseBehavior();
        public BaseBehavior baseBehavior
        {
            get
            {
                return _baseBehavior;
            }
            set
            {
                _baseBehavior = value;
                OnPropertyChanged("baseBehaviors");
            }
        }
    }

    public class TargetMovePlanWayPoint : CommonBase
    {
        private int _WaypointIndex;
        public int WaypointIndex
        {
            get
            {
                return _WaypointIndex;
            }
            set
            {
                _WaypointIndex = value;
                OnPropertyChanged("WaypointIndex");
            }
        }

        private double _WaypointLat;
        public double WaypointLat
        {
            get
            {
                return _WaypointLat;
            }
            set
            {
                _WaypointLat = value;
                OnPropertyChanged("WaypointLat");
            }
        }

        private double _WaypointLon;
        public double WaypointLon
        {
            get
            {
                return _WaypointLon;
            }
            set
            {
                _WaypointLon = value;
                OnPropertyChanged("WaypointLon");
            }
        }

        private double _WaypointAlt;
        public double WaypointAlt
        {
            get
            {
                return _WaypointAlt;
            }
            set
            {
                _WaypointAlt = value;
                OnPropertyChanged("WaypointAlt");
            }
        }

        private double _WaypointMoveAlt = 0;
        public double WaypointMoveAlt
        {
            get
            {
                return _WaypointMoveAlt;
            }
            set
            {
                _WaypointMoveAlt = value;
                OnPropertyChanged("WaypointMoveAlt");
            }
        }

        private double _WaypointMoveSpeed = 0;
        public double WaypointMoveSpeed
        {
            get
            {
                return _WaypointMoveSpeed;
            }
            set
            {
                _WaypointMoveSpeed = value;
                OnPropertyChanged("WaypointMoveSpeed");
            }
        }
    }

    public class WaypointPath : CommonBase
    {
        private int _WaypointPathIndex;
        public int WaypointPathIndex
        {
            get
            {
                return _WaypointPathIndex;
            }
            set
            {
                _WaypointPathIndex = value;
                OnPropertyChanged("WaypointPathIndex");
            }
        }

        private double _WaypointPathStartLat;
        public double WaypointPathStartLat
        {
            get
            {
                return _WaypointPathStartLat;
            }
            set
            {
                _WaypointPathStartLat = value;
                OnPropertyChanged("WaypointPathStartLat");
            }
        }

        private double _WaypointPathStartLon;
        public double WaypointPathStartLon
        {
            get
            {
                return _WaypointPathStartLon;
            }
            set
            {
                _WaypointPathStartLon = value;
                OnPropertyChanged("WaypointPathStartLon");
            }
        }

        private double _WaypointPathStartAlt;
        public double WaypointPathStartAlt
        {
            get
            {
                return _WaypointPathStartAlt;
            }
            set
            {
                _WaypointPathStartAlt = value;
                OnPropertyChanged("WaypointPathStartAlt");
            }
        }

        //private double _WaypointPathEndLat;
        //public double WaypointPathEndLat
        //{
        //    get
        //    {
        //        return _WaypointPathEndLat;
        //    }
        //    set
        //    {
        //        _WaypointPathEndLat = value;
        //        OnPropertyChanged("WaypointPathEndLat");
        //    }
        //}

        //private double _WaypointPathEndLon;
        //public double WaypointPathEndLon
        //{
        //    get
        //    {
        //        return _WaypointPathEndLon;
        //    }
        //    set
        //    {
        //        _WaypointPathEndLon = value;
        //        OnPropertyChanged("WaypointPathEndLon");
        //    }
        //}

        //private double _WaypointPathEndAlt;
        //public double WaypointPathEndAlt
        //{
        //    get
        //    {
        //        return _WaypointPathEndAlt;
        //    }
        //    set
        //    {
        //        _WaypointPathEndAlt = value;
        //        OnPropertyChanged("WaypointPathEndAlt");
        //    }
        //}

        private double _WaypointPathMoveAlt = 0;
        public double WaypointPathMoveAlt
        {
            get
            {
                return _WaypointPathMoveAlt;
            }
            set
            {
                _WaypointPathMoveAlt = value;
                OnPropertyChanged("WaypointPathMoveAlt");
            }
        }

        private double _WaypointPathMoveSpeed = 0;
        public double WaypointPathMoveSpeed
        {
            get
            {
                return _WaypointPathMoveSpeed;
            }
            set
            {
                _WaypointPathMoveSpeed = value;
                OnPropertyChanged("WaypointPathMoveSpeed");
            }
        }

        private BaseBehavior _baseBehavior = new BaseBehavior();
        public BaseBehavior baseBehavior
        {
            get
            {
                return _baseBehavior;
            }
            set
            {
                _baseBehavior = value;
                OnPropertyChanged("baseBehaviors");
            }
        }



    }

    public class BaseBehavior : CommonBase
    {
        private int _BehaviorType = 0;
        public int BehaviorType
        {
            get
            {
                return _BehaviorType;
            }
            set
            {
                _BehaviorType = value;
                BehaviorTypeStr = new Model_ScenarioView().BaseActionTextRef[value];
                OnPropertyChanged("BehaviorType");
            }
        }

        private string _BehaviorTypeStr = "-";
        public string BehaviorTypeStr
        {
            get
            {
                return _BehaviorTypeStr;
            }
            set
            {
                _BehaviorTypeStr = value;
                OnPropertyChanged("BehaviorTypeStr");
            }
        }

        private bool _IsCapture = false;
        public bool IsCapture
        {
            get
            {
                return _IsCapture;
            }
            set
            {
                _IsCapture = value;
                OnPropertyChanged("IsCapture");
            }
        }

        private double _dverticalFOV = 0;
        public double dverticalFOV
        {
            get
            {
                return _dverticalFOV;
            }
            set
            {
                _dverticalFOV = value;
                OnPropertyChanged("dverticalFOV");
            }
        }

        private double _dhorizontalFOV = 0;
        public double dhorizontalFOV
        {
            get
            {
                return _dhorizontalFOV;
            }
            set
            {
                _dhorizontalFOV = value;
                OnPropertyChanged("dhorizontalFOV");
            }
        }

        private double _CaptureAreaStartLat = 0;
        public double CaptureAreaStartLat
        {
            get
            {
                return _CaptureAreaStartLat;
            }
            set
            {
                _CaptureAreaStartLat = value;
                OnPropertyChanged("CaptureAreaStartLat");
            }
        }

        private double _CaptureAreaStartLon = 0;
        public double CaptureAreaStartLon
        {
            get
            {
                return _CaptureAreaStartLon;
            }
            set
            {
                _CaptureAreaStartLon = value;
                OnPropertyChanged("CaptureAreaStartLon");
            }
        }

        private double _CaptureAreaStartAlt = 0;
        public double CaptureAreaStartAlt
        {
            get
            {
                return _CaptureAreaStartAlt;
            }
            set
            {
                _CaptureAreaStartAlt = value;
                OnPropertyChanged("CaptureAreaStartAlt");
            }
        }

        private double _CaptureAreaEndLat = 0;
        public double CaptureAreaEndLat
        {
            get
            {
                return _CaptureAreaEndLat;
            }
            set
            {
                _CaptureAreaEndLat = value;
                OnPropertyChanged("CaptureAreaEndLat");
            }
        }

        private double _CaptureAreaEndLon = 0;
        public double CaptureAreaEndLon
        {
            get
            {
                return _CaptureAreaEndLon;
            }
            set
            {
                _CaptureAreaEndLon = value;
                OnPropertyChanged("CaptureAreaEndLon");
            }
        }

        private double _CaptureAreaEndAlt = 0;
        public double CaptureAreaEndAlt
        {
            get
            {
                return _CaptureAreaEndAlt;
            }
            set
            {
                _CaptureAreaEndAlt = value;
                OnPropertyChanged("CaptureAreaEndAlt");
            }
        }

        private uint _AttackTargetNum = 0;
        public uint AttackTargetNum
        {
            get
            {
                return _AttackTargetNum;
            }
            set
            {
                _AttackTargetNum = value;
                OnPropertyChanged("AttackTargetNum");
            }
        }


    }
}
