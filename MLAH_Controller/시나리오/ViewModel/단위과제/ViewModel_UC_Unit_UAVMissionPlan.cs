
using System.Collections.ObjectModel;
using System.Windows;
using DevExpress.Xpf.Map;
using MLAH_Controller;
using System.Windows.Input;


namespace MLAH_Controller
{

    
    public class ViewModel_UC_Unit_UAVMissionPlan : CommonBase
    {
        #region Singleton
        static ViewModel_UC_Unit_UAVMissionPlan _ViewModel_UC_Unit_UAVMissionPlan = null;
        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static ViewModel_UC_Unit_UAVMissionPlan SingletonInstance
        {
            get
            {
                if (_ViewModel_UC_Unit_UAVMissionPlan == null)
                {
                    _ViewModel_UC_Unit_UAVMissionPlan = new ViewModel_UC_Unit_UAVMissionPlan();
                }
                return _ViewModel_UC_Unit_UAVMissionPlan;
            }
        }

        #endregion Singleton

        #region 생성자 & 콜백
        public ViewModel_UC_Unit_UAVMissionPlan()
        {
            //LoadDummyDataCommand = new RelayCommand(p => LoadDummyData());
            UAVMissionPlanItemSource.CollectionChanged += OnMissionPlansChanged;
        }


        #endregion 생성자 & 콜백

        private void OnMissionPlansChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // UAV 임무 계획 목록에 변경이 생기면 지도 전체를 다시 그립니다.
            ViewModel_Unit_Map.SingletonInstance.UpdateAllUAVWaypoints(UAVMissionPlanItemSource);
        }

        // [확인 후 삭제] 미사용 필드 - LoadDummyData 주석처리로 인해 미참조
        //private int _currentTestCase = 0;
        public ICommand LoadDummyDataCommand { get; set; }

        private ObservableCollection<UAVMissionPlan> _UAVMissionPlanItemSource = new ObservableCollection<UAVMissionPlan>();
        public ObservableCollection<UAVMissionPlan> UAVMissionPlanItemSource
        {
            get
            {
                return _UAVMissionPlanItemSource;
            }
            set
            {
                _UAVMissionPlanItemSource = value;
                OnPropertyChanged("UAVMissionPlanItemSource");
            }
        }

        private int _UAVMissionPlanSelectedIndex = -1;
        public int UAVMissionPlanSelectedIndex
        {
            get
            {
                return _UAVMissionPlanSelectedIndex;
            }
            set
            {
                _UAVMissionPlanSelectedIndex = value;
                OnPropertyChanged("UAVMissionPlanSelectedIndex");
            }
        }

        private UAVMissionPlan _UAVMissionPlanSelectedItem = new UAVMissionPlan();
        public UAVMissionPlan UAVMissionPlanSelectedItem
        {
            get
            {
                return _UAVMissionPlanSelectedItem;
            }
            set
            {
                _UAVMissionPlanSelectedItem = value;
                if(value != null)
                {
                    MissionSegmentItemSource.Clear();
                    foreach (var input in value.MissionSegemntList)
                    {
                        MissionSegmentItemSource.Add(input);
                    }
                    var index = MissionSegmentItemSource.Count();
                    MissionSegmentSelectedIndex = index - 1;


                }
                OnPropertyChanged("UAVMissionPlanSelectedItem");
            }
        }

        private ObservableCollection<MissionSegmentUAV> _MissionSegmentItemSource = new ObservableCollection<MissionSegmentUAV>();
        public ObservableCollection<MissionSegmentUAV> MissionSegmentItemSource
        {
            get
            {
                return _MissionSegmentItemSource;
            }
            set
            {
                _MissionSegmentItemSource = value;
                OnPropertyChanged("MissionSegmentItemSource");
            }
        }

        private int _MissionSegmentSelectedIndex = -1;
        public int MissionSegmentSelectedIndex
        {
            get
            {
                return _MissionSegmentSelectedIndex;
            }
            set
            {
                _MissionSegmentSelectedIndex = value;
                OnPropertyChanged("MissionSegmentSelectedIndex");
            }
        }

        private MissionSegmentUAV _MissionSegmentSelectedItem = new MissionSegmentUAV();
        public MissionSegmentUAV MissionSegmentSelectedItem
        {
            get
            {
                return _MissionSegmentSelectedItem;
            }
            set
            {
                _MissionSegmentSelectedItem = value;
                if (value != null)
                {
                    IndividualMissionPlanItemSource.Clear();
                    foreach (var input in value.IndividualMissionList)
                    {
                        IndividualMissionPlanItemSource.Add(input);
                    }
                    var index = IndividualMissionPlanItemSource.Count();
                    IndividualMissionPlanSelectedIndex = index - 1;
                }
                OnPropertyChanged("MissionSegmentSelectedItem");
            }
        }

        private ObservableCollection<IndividualMissionUAV> _IndividualMissionPlanItemSource = new ObservableCollection<IndividualMissionUAV>();
        public ObservableCollection<IndividualMissionUAV> IndividualMissionPlanItemSource
        {
            get
            {
                return _IndividualMissionPlanItemSource;
            }
            set
            {
                _IndividualMissionPlanItemSource = value;
                OnPropertyChanged("IndividualMissionPlanItemSource");
            }
        }

        private int _IndividualMissionPlanSelectedIndex = -1;
        public int IndividualMissionPlanSelectedIndex
        {
            get
            {
                return _IndividualMissionPlanSelectedIndex;
            }
            set
            {
                _IndividualMissionPlanSelectedIndex = value;
                OnPropertyChanged("IndividualMissionPlanSelectedIndex");
            }
        }

        private IndividualMissionUAV _IndividualMissionPlanSelectedItem = new IndividualMissionUAV();
        public IndividualMissionUAV IndividualMissionPlanSelectedItem
        {
            get
            {
                return _IndividualMissionPlanSelectedItem;
            }
            set
            {
                _IndividualMissionPlanSelectedItem = value;
                if (value != null)
                {
                    WayPointUAVItemSource.Clear();

                    //경로비행
                    if(value.FlightType == 1)
                    {
                        FlightTypeWayPointVisibility = Visibility.Visible;
                        FlightTypeFormationVisibility = Visibility.Collapsed;

                        foreach (var input in value.WaypointList)
                        {
                            WayPointUAVItemSource.Add(input);
                        }
                        //if (WayPointUAVItemSource.Count > 2)
                        //{
                        //    ViewModel_Unit_Map.SingletonInstance.ClearUAVWayPoint();
                        //    var inputPoints = new MapPolyline();
                        //    foreach (var input in value.WaypointList)
                        //    {
                        //        var inputPoint = new GeoPoint(input.Coordinate.Latitude, input.Coordinate.Longitude);
                        //        inputPoints.Points.Add(inputPoint);
                        //    }
                        //    ViewModel_Unit_Map.SingletonInstance.UAVWapointList.Add(inputPoints);
                        //}
                        var index = WayPointUAVItemSource.Count();
                        WayPointUAVSelectedIndex = index - 1;
                    }
                    //편대비행
                    else if (value.FlightType == 2)
                    {
                        FlightTypeWayPointVisibility = Visibility.Collapsed;
                        FlightTypeFormationVisibility = Visibility.Visible;

                        //Filming_FOV = value.FormationInfo.FilmingProperty.FieldOfView;
                        //Filming_SensorType = value.FormationInfo.FilmingProperty.SensorType;
                        //Filming_OperationMode = value.FormationInfo.FilmingProperty.OperationMode;
                        //Filming_CoordinateListN = value.FormationInfo.FilmingProperty.CoordinateList.Count;
                        //Filming_CoordinateList.Clear();
                        //foreach (var input in value.FormationInfo.FilmingProperty.CoordinateList)
                        //{
                        //    Filming_CoordinateList.Add(input);
                        //}
                        //Filming_SearchSpeed = value.FormationInfo.FilmingProperty.SearchSpeed;
                        //Filming_TargetID = value.FormationInfo.FilmingProperty.TargetID;
                        //Filming_SensorYaw = value.FormationInfo.FilmingProperty.SensorYaw;
                        //Filming_SensorPitch = value.FormationInfo.FilmingProperty.SensorPitch;
                        //Filming_YawAngularSpeed_LeftLimit = value.FormationInfo.FilmingProperty.SensorYawAngularSpeed.LeftLimit;
                        //Filming_YawAngularSpeed_RightLimit = value.FormationInfo.FilmingProperty.SensorYawAngularSpeed.RightLimit;
                        //Filming_YawAngularSpeed_AngularRate = value.FormationInfo.FilmingProperty.SensorYawAngularSpeed.AngularRate;
                    }
                    
                }
                OnPropertyChanged("IndividualMissionPlanSelectedItem");
            }
        }

        private ObservableCollection<WaypointUAV> _WayPointUAVItemSource = new ObservableCollection<WaypointUAV>();
        public ObservableCollection<WaypointUAV> WayPointUAVItemSource
        {
            get
            {
                return _WayPointUAVItemSource;
            }
            set
            {
                _WayPointUAVItemSource = value;
                OnPropertyChanged("WayPointUAVItemSource");
            }
        }

        private int _WayPointUAVSelectedIndex = -1;
        public int WayPointUAVSelectedIndex
        {
            get
            {
                return _WayPointUAVSelectedIndex;
            }
            set
            {
                _WayPointUAVSelectedIndex = value;
                OnPropertyChanged("WayPointUAVSelectedIndex");
            }
        }

        private WaypointUAV _WayPointUAVSelectedItem = new WaypointUAV();
        public WaypointUAV WayPointUAVSelectedItem
        {
            get
            {
                return _WayPointUAVSelectedItem;
            }
            set
            {
                _WayPointUAVSelectedItem = value;
                if (value != null)
                {
                    // value.Coordinate가 null일 수 있으므로 ?. 와 ?? 연산자로 안전하게 접근
                    WayPointUAV_LAT = value.Coordinate?.Latitude ?? 0;
                    WayPointUAV_LON = value.Coordinate?.Longitude ?? 0;
                    WayPointUAV_ALT = value.Coordinate?.Altitude ?? 0;

                    WayPointUAV_Speed = value.Speed;
                    WayPointUAV_ETA = value.ETA;
                    WayPointUAV_Next = value.NextWaypointID;
                    WayPointUAV_PassType = value.WaypointPassType;

                    // value.LoiterProperty가 null이면 NullReferenceException 발생! -> ?. 로 해결
                    WayPointUAV_Radius = value.LoiterProperty?.Radius ?? 0;
                    WayPointUAV_Direction = value.LoiterProperty?.Direction ?? 1;
                    WayPointUAV_Time = value.LoiterProperty?.Time ?? 0;
                    WayPointUAV_LoiterSpeed = value.LoiterProperty?.Speed ?? 0;

                    // value.FilmingProperty가 null이면 NullReferenceException 발생! -> ?. 로 해결
                    Filming_FOV = value.FilmingProperty?.FieldOfView ?? 0;
                    Filming_SensorType = value.FilmingProperty?.SensorType ?? 1;
                    Filming_OperationMode = value.FilmingProperty?.OperationMode ?? 1;
                    Filming_CoordinateListN = value.FilmingProperty?.CoordinateList?.Count ?? 0;

                    Filming_CoordinateList.Clear();
                    if (value.FilmingProperty?.CoordinateList != null)
                    {
                        foreach (var input in value.FilmingProperty.CoordinateList)
                        {
                            Filming_CoordinateList.Add(input);
                        }
                    }

                    Filming_SearchSpeed = value.FilmingProperty?.SearchSpeed ?? 0;
                    Filming_TargetID = value.FilmingProperty?.TargetID ?? 0;
                    Filming_SensorYaw = value.FilmingProperty?.SensorYaw ?? 0;
                    Filming_SensorPitch = value.FilmingProperty?.SensorPitch ?? 0;

                    // 중첩된 null 체크
                    Filming_YawAngularSpeed_LeftLimit = value.FilmingProperty?.SensorYawAngularSpeed?.LeftLimit ?? 0;
                    Filming_YawAngularSpeed_RightLimit = value.FilmingProperty?.SensorYawAngularSpeed?.RightLimit ?? 0;
                    Filming_YawAngularSpeed_AngularRate = value.FilmingProperty?.SensorYawAngularSpeed?.AngularRate ?? 0;
                }
                OnPropertyChanged("WayPointUAVSelectedItem");
            }
        }

        private float _WayPointUAV_LAT = 0;
        public float WayPointUAV_LAT
        {
            get
            {
                return _WayPointUAV_LAT;
            }
            set
            {
                _WayPointUAV_LAT = value;
                OnPropertyChanged("WayPointUAV_LAT");
            }
        }

        private float _WayPointUAV_LON = 0;
        public float WayPointUAV_LON
        {
            get
            {
                return _WayPointUAV_LON;
            }
            set
            {
                _WayPointUAV_LON = value;
                OnPropertyChanged("WayPointUAV_LON");
            }
        }

        private int _WayPointUAV_ALT = 0;
        public int WayPointUAV_ALT
        {
            get
            {
                return _WayPointUAV_ALT;
            }
            set
            {
                _WayPointUAV_ALT = value;
                OnPropertyChanged("WayPointUAV_ALT");
            }
        }

        private float _WayPointUAV_Speed = 0;
        public float WayPointUAV_Speed
        {
            get
            {
                return _WayPointUAV_Speed;
            }
            set
            {
                _WayPointUAV_Speed = value;
                OnPropertyChanged("WayPointUAV_Speed");
            }
        }

        private uint _WayPointUAV_ETA = 0;
        public uint WayPointUAV_ETA
        {
            get
            {
                return _WayPointUAV_ETA;
            }
            set
            {
                _WayPointUAV_ETA = value;
                OnPropertyChanged("WayPointUAV_ETA");
            }
        }

        private uint _WayPointUAV_Next = 0;
        public uint WayPointUAV_Next
        {
            get
            {
                return _WayPointUAV_Next;
            }
            set
            {
                _WayPointUAV_Next = value;
                OnPropertyChanged("WayPointUAV_Next");
            }
        }

        private int _WayPointUAV_Radius = 0;
        public int WayPointUAV_Radius
        {
            get
            {
                return _WayPointUAV_Radius;
            }
            set
            {
                _WayPointUAV_Radius = value;
                OnPropertyChanged("WayPointUAV_Radius");
            }
        }

        private uint _WayPointUAV_Direction = 1;
        public uint WayPointUAV_Direction
        {
            get
            {
                return _WayPointUAV_Direction;
            }
            set
            {
                _WayPointUAV_Direction = value;
                OnPropertyChanged("WayPointUAV_Direction");
            }
        }

        private int _WayPointUAV_Time = 0;
        public int WayPointUAV_Time
        {
            get
            {
                return _WayPointUAV_Time;
            }
            set
            {
                _WayPointUAV_Time = value;
                OnPropertyChanged("WayPointUAV_Time");
            }
        }

        private float _WayPointUAV_LoiterSpeed = 0;
        public float WayPointUAV_LoiterSpeed
        {
            get
            {
                return _WayPointUAV_LoiterSpeed;
            }
            set
            {
                _WayPointUAV_LoiterSpeed = value;
                OnPropertyChanged("WayPointUAV_LoiterSpeed");
            }
        }

        private int _WayPointUAV_Hovering = 0;
        public int WayPointUAV_Hovering
        {
            get
            {
                return _WayPointUAV_Hovering;
            }
            set
            {
                _WayPointUAV_Hovering = value;
                OnPropertyChanged("WayPointUAV_Hovering");
            }
        }

        private uint _WayPointUAV_AttackTargetID = 0;
        public uint WayPointUAV_AttackTargetID
        {
            get
            {
                return _WayPointUAV_AttackTargetID;
            }
            set
            {
                _WayPointUAV_AttackTargetID = value;
                OnPropertyChanged("WayPointUAV_AttackTargetID");
            }
        }

        private uint _WayPointUAV_PassType = 0;
        public uint WayPointUAV_PassType
        {
            get
            {
                return _WayPointUAV_PassType;
            }
            set
            {
                _WayPointUAV_PassType = value;
                OnPropertyChanged("WayPointUAV_PassType");
            }
        }

        private Visibility _FlightTypeWayPointVisibility = Visibility.Visible;
        public Visibility FlightTypeWayPointVisibility
        {
            get
            {
                return _FlightTypeWayPointVisibility;
            }
            set
            {
                _FlightTypeWayPointVisibility = value;
                OnPropertyChanged("FlightTypeWayPointVisibility");
            }
        }

        private Visibility _FlightTypeFormationVisibility = Visibility.Collapsed;
        public Visibility FlightTypeFormationVisibility
        {
            get
            {
                return _FlightTypeFormationVisibility;
            }
            set
            {
                _FlightTypeFormationVisibility = value;
                OnPropertyChanged("FlightTypeVisibility");
            }
        }



        private float _Filming_FOV = 0;
        public float Filming_FOV
        {
            get
            {
                return _Filming_FOV;
            }
            set
            {
                _Filming_FOV = value;
                OnPropertyChanged("Filming_FOV");
            }
        }

        private uint _Filming_SensorType = 1;
        public uint Filming_SensorType
        {
            get
            {
                return _Filming_SensorType;
            }
            set
            {
                _Filming_SensorType = value;
                OnPropertyChanged("Filming_SensorType");
            }
        }

        private int _Filming_OperationMode = 1;
        public int Filming_OperationMode
        {
            get
            {
                return _Filming_OperationMode;
            }
            set
            {
                _Filming_OperationMode = value;
                OnPropertyChanged("Filming_OperationMode");
            }
        }

        private int _Filming_CoordinateListN = 1;
        public int Filming_CoordinateListN
        {
            get
            {
                return _Filming_CoordinateListN;
            }
            set
            {
                _Filming_CoordinateListN = value;
                OnPropertyChanged("Filming_CoordinateListN");
            }
        }

        private ObservableCollection<CoordinateList> _Filming_CoordinateList = new ObservableCollection<CoordinateList>();
        public ObservableCollection<CoordinateList> Filming_CoordinateList
        {
            get
            {
                return _Filming_CoordinateList;
            }
            set
            {
                _Filming_CoordinateList = value;
                OnPropertyChanged("Filming_CoordinateList");
            }
        }

        private float _Filming_SearchSpeed = 0;
        public float Filming_SearchSpeed
        {
            get
            {
                return _Filming_SearchSpeed;
            }
            set
            {
                _Filming_SearchSpeed = value;
                OnPropertyChanged("Filming_SearchSpeed");
            }
        }

        private uint _Filming_TargetID = 0;
        public uint Filming_TargetID
        {
            get
            {
                return _Filming_TargetID;
            }
            set
            {
                _Filming_TargetID = value;
                OnPropertyChanged("Filming_TargetID");
            }
        }

        private float _Filming_SensorYaw = 0;
        public float Filming_SensorYaw
        {
            get
            {
                return _Filming_SensorYaw;
            }
            set
            {
                _Filming_SensorYaw = value;
                OnPropertyChanged("Filming_SensorYaw");
            }
        }

        private float _Filming_SensorPitch = 0;
        public float Filming_SensorPitch
        {
            get
            {
                return _Filming_SensorPitch;
            }
            set
            {
                _Filming_SensorPitch = value;
                OnPropertyChanged("Filming_SensorPitch");
            }
        }

        private float _Filming_YawAngularSpeed_LeftLimit = 0;
        public float Filming_YawAngularSpeed_LeftLimit
        {
            get
            {
                return _Filming_YawAngularSpeed_LeftLimit;
            }
            set
            {
                _Filming_YawAngularSpeed_LeftLimit = value;
                OnPropertyChanged("_Filming_YawAngularSpeed_LeftLimit");
            }
        }

        private float _Filming_YawAngularSpeed_RightLimit = 0;
        public float Filming_YawAngularSpeed_RightLimit
        {
            get
            {
                return _Filming_YawAngularSpeed_RightLimit;
            }
            set
            {
                _Filming_YawAngularSpeed_RightLimit = value;
                OnPropertyChanged("_Filming_YawAngularSpeed_RightLimit");
            }
        }

        private float _Filming_YawAngularSpeed_AngularRate = 0;
        public float Filming_YawAngularSpeed_AngularRate
        {
            get
            {
                return _Filming_YawAngularSpeed_AngularRate;
            }
            set
            {
                _Filming_YawAngularSpeed_AngularRate = value;
                OnPropertyChanged("_Filming_YawAngularSpeed_AngularRate");
            }
        }

        private uint _LeaderAircraftID = 4;
        public uint LeaderAircraftID
        {
            get
            {
                return _LeaderAircraftID;
            }
            set
            {
                _LeaderAircraftID = value;
                OnPropertyChanged("LeaderAircraftID");
            }
        }

        private int _FormationX = 0;
        public int FormationX
        {
            get
            {
                return _FormationX;
            }
            set
            {
                _FormationX = value;
                OnPropertyChanged("FormationX");
            }
        }

        private int _FormationY = 0;
        public int FormationY
        {
            get
            {
                return _FormationY;
            }
            set
            {
                _FormationY = value;
                OnPropertyChanged("FormationY");
            }
        }

        private int _FormationZ = 0;
        public int FormationZ
        {
            get
            {
                return _FormationZ;
            }
            set
            {
                _FormationZ = value;
                OnPropertyChanged("FormationZ");
            }
        }

        

    }

}
