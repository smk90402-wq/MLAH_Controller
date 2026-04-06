
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Animation;
using System.Windows.Controls;
//using GMap.NET;
using System.Security.Policy;
using MLAH_Controller;
using DevExpress.Pdf.ContentGeneration;
using DevExpress.Map;
using Windows.Devices.Geolocation;
using DevExpress.Xpf.Map;
using System.Globalization;
using System.Windows.Data;
using DevExpress.XtraTreeList;
using static DevExpress.Charts.Designer.Native.BarThicknessEditViewModel;
using DevExpress.Mvvm.Native;
using System.Windows.Input;



namespace MLAH_Controller
{

    
    public class ViewModel_UC_Unit_LAHMissionPlan : CommonBase
    {
        #region Singleton
        static ViewModel_UC_Unit_LAHMissionPlan _ViewModel_UC_Unit_LAHMissionPlan = null;
        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static ViewModel_UC_Unit_LAHMissionPlan SingletonInstance
        {
            get
            {
                if (_ViewModel_UC_Unit_LAHMissionPlan == null)
                {
                    _ViewModel_UC_Unit_LAHMissionPlan = new ViewModel_UC_Unit_LAHMissionPlan();
                }
                return _ViewModel_UC_Unit_LAHMissionPlan;
            }
        }

        #endregion Singleton

        #region 생성자 & 콜백
        public ViewModel_UC_Unit_LAHMissionPlan()
        {
            LAHMissionPlanItemSource.CollectionChanged += OnMissionPlansChanged;
            Button1Command = new RelayCommand(Button1CommandAction);
            Button2Command = new RelayCommand(Button2CommandAction);
            Button3Command = new RelayCommand(Button3CommandAction);
        }

        // 투명도 제어용 프로퍼티 (1.0 = 불투명, 0.0 = 투명)
        private double _ControlStatusOpacity1 = 1.0;
        public double ControlStatusOpacity1
        {
            get => _ControlStatusOpacity1;
            set { _ControlStatusOpacity1 = value; OnPropertyChanged("ControlStatusOpacity1"); }
        }

        private double _ControlStatusOpacity2 = 1.0;
        public double ControlStatusOpacity2
        {
            get => _ControlStatusOpacity2;
            set { _ControlStatusOpacity2 = value; OnPropertyChanged("ControlStatusOpacity2"); }
        }

        private double _ControlStatusOpacity3 = 1.0;
        public double ControlStatusOpacity3
        {
            get => _ControlStatusOpacity3;
            set { _ControlStatusOpacity3 = value; OnPropertyChanged("ControlStatusOpacity3"); }
        }

        private void OnMissionPlansChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ViewModel_Unit_Map.SingletonInstance.UpdateAllLAHWaypoints(LAHMissionPlanItemSource);
        }

        #endregion 생성자 & 콜백

        private ObservableCollection<LAHMissionPlan> _LAHMissionPlanItemSource = new ObservableCollection<LAHMissionPlan>();
        public ObservableCollection<LAHMissionPlan> LAHMissionPlanItemSource
        {
            get
            {
                return _LAHMissionPlanItemSource;
            }
            set
            {
                _LAHMissionPlanItemSource = value;
                OnPropertyChanged("LAHMissionPlanItemSource");
            }
        }

        private int _LAHMissionPlanSelectedIndex = -1;
        public int LAHMissionPlanSelectedIndex
        {
            get
            {
                return _LAHMissionPlanSelectedIndex;
            }
            set
            {
                _LAHMissionPlanSelectedIndex = value;
                OnPropertyChanged("LAHMissionPlanSelectedIndex");
            }
        }

        private LAHMissionPlan _LAHMissionPlanSelectedItem = new LAHMissionPlan();
        public LAHMissionPlan LAHMissionPlanSelectedItem
        {
            get
            {
                return _LAHMissionPlanSelectedItem;
            }
            set
            {
                _LAHMissionPlanSelectedItem = value;
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
                OnPropertyChanged("LAHMissionPlanSelectedItem");
            }
        }

        private ObservableCollection<MissionSegmentLAH> _MissionSegmentItemSource = new ObservableCollection<MissionSegmentLAH>();
        public ObservableCollection<MissionSegmentLAH> MissionSegmentItemSource
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

        private MissionSegmentLAH _MissionSegmentSelectedItem = new MissionSegmentLAH();
        public MissionSegmentLAH MissionSegmentSelectedItem
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

        private ObservableCollection<IndividualMissionLAH> _IndividualMissionPlanItemSource = new ObservableCollection<IndividualMissionLAH>();
        public ObservableCollection<IndividualMissionLAH> IndividualMissionPlanItemSource
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

        private IndividualMissionLAH _IndividualMissionPlanSelectedItem = new IndividualMissionLAH();
        public IndividualMissionLAH IndividualMissionPlanSelectedItem
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
                    WayPointLAHItemSource.Clear();
                    foreach (var input in value.WaypointList)
                    {
                        WayPointLAHItemSource.Add(input);
                    }
                    var index = WayPointLAHItemSource.Count();
                    WayPointLAHSelectedIndex = index - 1;
                }
                OnPropertyChanged("IndividualMissionPlanSelectedItem");
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


        private ObservableCollection<WaypointLAH> _WayPointLAHItemSource = new ObservableCollection<WaypointLAH>();
        public ObservableCollection<WaypointLAH> WayPointLAHItemSource
        {
            get
            {
                return _WayPointLAHItemSource;
            }
            set
            {
                _WayPointLAHItemSource = value;
                OnPropertyChanged("WayPointLAHItemSource");
            }
        }

        private int _WayPointLAHSelectedIndex = -1;
        public int WayPointLAHSelectedIndex
        {
            get
            {
                return _WayPointLAHSelectedIndex;
            }
            set
            {
                _WayPointLAHSelectedIndex = value;
                OnPropertyChanged("WayPointLAHSelectedIndex");
            }
        }

        private WaypointLAH _WayPointLAHSelectedItem = new WaypointLAH();
        public WaypointLAH WayPointLAHSelectedItem
        {
            get
            {
                return _WayPointLAHSelectedItem;
            }
            set
            {
                _WayPointLAHSelectedItem = value;
                if (value != null)
                {
                    //WaypointLAH_ID
                    WayPointLAH_LAT = value.Coordinate.Latitude;
                    WayPointLAH_LON = value.Coordinate.Longitude;
                    WayPointLAH_ALT = value.Coordinate.Altitude;
                    WayPointLAH_Speed = value.Speed;
                    WayPointLAH_ETA = value.ETA;
                    WayPointLAH_Next = value.NextWaypointID;
                    WayPointLAH_Hovering = value.Hovering;
                    WayPointLAH_AttackTargetID = value.Attack.TargetID;
                    WayPointLAH_AttackType = value.Attack.WeaponType;
                }
                OnPropertyChanged("WayPointLAHSelectedItem");
            }
        }

        private float _WayPointLAH_LAT = 0;
        public float WayPointLAH_LAT
        {
            get
            {
                return _WayPointLAH_LAT;
            }
            set
            {
                _WayPointLAH_LAT = value;
                OnPropertyChanged("WayPointLAH_LAT");
            }
        }

        private float _WayPointLAH_LON = 0;
        public float WayPointLAH_LON
        {
            get
            {
                return _WayPointLAH_LON;
            }
            set
            {
                _WayPointLAH_LON = value;
                OnPropertyChanged("WayPointLAH_LON");
            }
        }

        private int _WayPointLAH_ALT = 0;
        public int WayPointLAH_ALT
        {
            get
            {
                return _WayPointLAH_ALT;
            }
            set
            {
                _WayPointLAH_ALT = value;
                OnPropertyChanged("WayPointLAH_ALT");
            }
        }

        private float _WayPointLAH_Speed = 0;
        public float WayPointLAH_Speed
        {
            get
            {
                return _WayPointLAH_Speed;
            }
            set
            {
                _WayPointLAH_Speed = value;
                OnPropertyChanged("WayPointLAH_Speed");
            }
        }

        private int _WayPointLAH_ETA = 0;
        public int WayPointLAH_ETA
        {
            get
            {
                return _WayPointLAH_ETA;
            }
            set
            {
                _WayPointLAH_ETA = value;
                OnPropertyChanged("WayPointLAH_ETA");
            }
        }

        private uint _WayPointLAH_Next = 0;
        public uint WayPointLAH_Next
        {
            get
            {
                return _WayPointLAH_Next;
            }
            set
            {
                _WayPointLAH_Next = value;
                OnPropertyChanged("WayPointLAH_Next");
            }
        }

        private uint _WayPointLAH_Hovering = 0;
        public uint WayPointLAH_Hovering
        {
            get
            {
                return _WayPointLAH_Hovering;
            }
            set
            {
                _WayPointLAH_Hovering = value;
                OnPropertyChanged("WayPointLAH_Hovering");
            }
        }

        private uint _WayPointLAH_AttackTargetID = 0;
        public uint WayPointLAH_AttackTargetID
        {
            get
            {
                return _WayPointLAH_AttackTargetID;
            }
            set
            {
                _WayPointLAH_AttackTargetID = value;
                OnPropertyChanged("WayPointLAH_AttackTargetID");
            }
        }

        private uint _WayPointLAH_AttackType = 0;
        public uint WayPointLAH_AttackType
        {
            get
            {
                return _WayPointLAH_AttackType;
            }
            set
            {
                _WayPointLAH_AttackType = value;
                OnPropertyChanged("WayPointLAH_AttackType");
            }
        }

        #region 헬기 임무 통제

        //"System","Pilot"
        private string _ControlDecisionType1 = "-";
        public string ControlDecisionType1
        {
            get
            {
                return _ControlDecisionType1;
            }
            set
            {
                _ControlDecisionType1 = value;
                OnPropertyChanged("ControlDecisionType1");
            }
        }

        private int _ControlIndividualID1 = 0;
        public int ControlIndividualID1
        {
            get
            {
                return _ControlIndividualID1;
            }
            set
            {
                _ControlIndividualID1 = value;
                OnPropertyChanged("ControlIndividualID1");

                //값 변경 시 지도 펄스 갱신 호출
                //ViewModel_Unit_Map.SingletonInstance.RefreshActivePulseLines();
                ViewModel_Unit_Map.SingletonInstance.UpdateAllLAHWaypoints(LAHMissionPlanItemSource);
            }
        }

        //"-", "O", "X"
        private string _ControlMissionDone1 = "-";
        public string ControlMissionDone1
        {
            get
            {
                return _ControlMissionDone1;
            }
            set
            {
                _ControlMissionDone1 = value;
                OnPropertyChanged("ControlMissionDone1");
            }
        }

        private int _ControlNextID1 = 0;
        public int ControlNextID1
        {
            get
            {
                return _ControlNextID1;
            }
            set
            {
                _ControlNextID1 = value;
                OnPropertyChanged("ControlNextID1");
            }
        }

        //"System","Pilot"
        private string _ControlDecisionType2 = "-";
        public string ControlDecisionType2
        {
            get
            {
                return _ControlDecisionType2;
            }
            set
            {
                _ControlDecisionType2 = value;
                OnPropertyChanged("ControlDecisionType2");
            }
        }

        private int _ControlIndividualID2 = 0;
        public int ControlIndividualID2
        {
            get
            {
                return _ControlIndividualID2;
            }
            set
            {
                _ControlIndividualID2 = value;
                OnPropertyChanged("ControlIndividualID2");

                //값 변경 시 지도 펄스 갱신 호출
                //ViewModel_Unit_Map.SingletonInstance.RefreshActivePulseLines();
                ViewModel_Unit_Map.SingletonInstance.UpdateAllLAHWaypoints(LAHMissionPlanItemSource);
            }
        }

        //"-", "O", "X"
        private string _ControlMissionDone2 = "-";
        public string ControlMissionDone2
        {
            get
            {
                return _ControlMissionDone2;
            }
            set
            {
                _ControlMissionDone2 = value;
                OnPropertyChanged("ControlMissionDone2");
            }
        }

        private int _ControlNextID2 = 0;
        public int ControlNextID2
        {
            get
            {
                return _ControlNextID2;
            }
            set
            {
                _ControlNextID2 = value;
                OnPropertyChanged("ControlNextID2");
            }
        }

        //"System","Pilot"
        private string _ControlDecisionType3 = "-";
        public string ControlDecisionType3
        {
            get
            {
                return _ControlDecisionType3;
            }
            set
            {
                _ControlDecisionType3 = value;
                OnPropertyChanged("ControlDecisionType3");
            }
        }

        private int _ControlIndividualID3 = 0;
        public int ControlIndividualID3
        {
            get
            {
                return _ControlIndividualID3;
            }
            set
            {
                _ControlIndividualID3 = value;
                OnPropertyChanged("ControlIndividualID3");

                //값 변경 시 지도 펄스 갱신 호출
                //ViewModel_Unit_Map.SingletonInstance.RefreshActivePulseLines();
                ViewModel_Unit_Map.SingletonInstance.UpdateAllLAHWaypoints(LAHMissionPlanItemSource);
            }
        }

        //"-", "O", "X"
        private string _ControlMissionDone3 = "-";
        public string ControlMissionDone3
        {
            get
            {
                return _ControlMissionDone3;
            }
            set
            {
                _ControlMissionDone3 = value;
                OnPropertyChanged("ControlMissionDone3");
            }
        }

        private int _ControlNextID3 = 0;
        public int ControlNextID3
        {
            get
            {
                return _ControlNextID3;
            }
            set
            {
                _ControlNextID3 = value;
                OnPropertyChanged("ControlNextID3");
            }
        }

        private bool _IsAutoExecutionMode = true;
        public bool IsAutoExecutionMode
        {
            get => _IsAutoExecutionMode;
            set
            {
                _IsAutoExecutionMode = value;
                OnPropertyChanged("IsAutoExecutionMode");
            }
        }

        //임무 시작
        public RelayCommand Button1Command { get; set; }

        public void Button1CommandAction(object param)
        {
            _ = Model_ScenarioSequenceManager.SingletonInstance.ExecuteCurrentMission(1);
        }

        public RelayCommand Button2Command { get; set; }

        public void Button2CommandAction(object param)
        {
            _ = Model_ScenarioSequenceManager.SingletonInstance.ExecuteCurrentMission(2);
        }

        public RelayCommand Button3Command { get; set; }

        public void Button3CommandAction(object param)
        {
            _ = Model_ScenarioSequenceManager.SingletonInstance.ExecuteCurrentMission(3);
        }

        #endregion 헬기 임무 통제

    }

}
