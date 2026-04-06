using DevExpress.Xpf.Map;
using System.Collections.ObjectModel;

namespace MLAH_Controller
{

    public partial class ViewModel_UC_Unit_MissionPackage : CommonBase
    {

        // [신규] 현재 활성화된 모드를 식별하기 위한 내부 Enum (필요시)
        private enum ActiveMode { None, TakeOver, HandOver, RTB, FlightArea, ProhibitedArea }

        public enum MissionEditState { None, Creating, Editing }

        // [수정/취소용 백업 변수]
        private float _backupLat;
        private float _backupLon;
        private int _backupAlt;
        private int _backupIdIndex; // (필요시) ID도 백업

        private List<AreaLatLonInfo> _flightAreaBackup = new List<AreaLatLonInfo>();
        // 지도 드래그 시 -> 그리드(InnerSource) 실시간 업데이트

        private List<AreaLatLonInfo> _prohibitedAreaBackup = new List<AreaLatLonInfo>();
        // 지도 드래그 시 -> 그리드(InnerSource) 실시간 업데이트

        private MissionEditState _TakeOverState = MissionEditState.None;
        public MissionEditState TakeOverState
        {
            get => _TakeOverState;
            set
            {
                _TakeOverState = value;
                OnPropertyChanged("TakeOverState");
                UpdateInfoPanelState();
                UpdateTakeOverButtonState();
            }
        }

        private MissionEditState _HandOverState = MissionEditState.None;
        public MissionEditState HandOverState
        {
            get => _HandOverState;
            set
            {
                _HandOverState = value;
                OnPropertyChanged("HandOverState");
                UpdateInfoPanelState();
                UpdateHandOverButtonState();
            }
        }

        private MissionEditState _RTBState = MissionEditState.None;
        public MissionEditState RTBState
        {
            get => _RTBState;
            set
            {
                _RTBState = value;
                OnPropertyChanged("RTBState");
                UpdateInfoPanelState();
                UpdateHandOverButtonState();
            }
        }

        private MissionEditState _FlightAreaState = MissionEditState.None;
        public MissionEditState FlightAreaState
        {
            get => _FlightAreaState;
            set
            {
                _FlightAreaState = value;
                OnPropertyChanged("FlightAreaState");
                UpdateInfoPanelState();

                //상태가 바뀌었으니 버튼 상태 갱신
                UpdateFlightAreaButtonState();
            }
        }

        private MissionEditState _ProhibitedAreaState = MissionEditState.None;
        public MissionEditState ProhibitedAreaState
        {
            get => _ProhibitedAreaState;
            set
            {
                _ProhibitedAreaState = value;
                OnPropertyChanged("ProhibitedAreaState");
                UpdateInfoPanelState();
                UpdateProhibitedAreaButtonState();
            }
        }

        private string _TakeOverButton1Text = "생성";
        public string TakeOverButton1Text
        {
            get
            {
                return _TakeOverButton1Text;
            }
            set
            {
                _TakeOverButton1Text = value;
                OnPropertyChanged("TakeOverButton1Text");
            }
        }

        private string _TakeOverButton2Text = "수정";
        public string TakeOverButton2Text
        {
            get
            {
                return _TakeOverButton2Text;
            }
            set
            {
                _TakeOverButton2Text = value;
                OnPropertyChanged("TakeOverButton2Text");
            }
        }

        private string _TakeOverButton3Text = "삭제";
        public string TakeOverButton3Text
        {
            get
            {
                return _TakeOverButton3Text;
            }
            set
            {
                _TakeOverButton3Text = value;
                OnPropertyChanged("TakeOverButton3Text");
            }
        }

        private bool _TakeOverButton1Enable = true;
        public bool TakeOverButton1Enable
        {
            get
            {
                return _TakeOverButton1Enable;
            }
            set
            {
                _TakeOverButton1Enable = value;
                OnPropertyChanged("TakeOverButton1Enable");
            }
        }

        private bool _TakeOverButton2Enable = false;
        public bool TakeOverButton2Enable
        {
            get
            {
                return _TakeOverButton2Enable;
            }
            set
            {
                _TakeOverButton2Enable = value;
                OnPropertyChanged("TakeOverButton2Enable");
            }
        }

        private bool _TakeOverButton3Enable = false;
        public bool TakeOverButton3Enable
        {
            get
            {
                return _TakeOverButton3Enable;
            }
            set
            {
                _TakeOverButton3Enable = value;
                OnPropertyChanged("TakeOverButton3Enable");
            }
        }

        private int _TakeOverUAVIDIndex = 0;
        public int TakeOverUAVIDIndex
        {
            get
            {
                return _TakeOverUAVIDIndex;
            }
            set
            {
                _TakeOverUAVIDIndex = value;
                OnPropertyChanged("TakeOverUAVIDIndex");
            }
        }

        private float _TakeOverLAT = 0;
        public float TakeOverLAT
        {
            get
            {
                return _TakeOverLAT;
            }
            set
            {
                _TakeOverLAT = value;
                OnPropertyChanged("TakeOverLAT");
            }
        }

        private float _TakeOverLON = 0;
        public float TakeOverLON
        {
            get
            {
                return _TakeOverLON;
            }
            set
            {
                _TakeOverLON = value;
                OnPropertyChanged("TakeOverLON");
            }
        }

        private int _TakeOverALT = 1000;
        public int TakeOverALT
        {
            get
            {
                return _TakeOverALT;
            }
            set
            {
                _TakeOverALT = value;
                OnPropertyChanged("TakeOverALT");
            }
        }

        private bool _TakeOverEditEnable = false;
        public bool TakeOverEditEnable
        {
            get
            {
                return _TakeOverEditEnable;
            }
            set
            {
                _TakeOverEditEnable = value;
                OnPropertyChanged("TakeOverEditEnable");
            }
        }

        private bool _TakeOverChecked = false;
        public bool TakeOverChecked
        {
            get
            {
                return _TakeOverChecked;
            }
            set
            {
                _TakeOverChecked = value;
                if (value == true)
                {
                    ViewModel_Unit_Map.SingletonInstance.ClearTempTakeoverPoint();
                }
                OnPropertyChanged("TakeOverChecked");
            }
        }

        private bool _TakeOverCheckEditEnable = false;
        public bool TakeOverCheckEditEnable
        {
            get
            {
                return _TakeOverCheckEditEnable;
            }
            set
            {
                _TakeOverCheckEditEnable = value;
                OnPropertyChanged("TakeOverCheckEditEnable");
            }
        }

        private ObservableCollection<TakeOverHandOverInfo> _TakeOverItemSource = new ObservableCollection<TakeOverHandOverInfo>();
        public ObservableCollection<TakeOverHandOverInfo> TakeOverItemSource
        {
            get
            {
                return _TakeOverItemSource;
            }
            set
            {
                _TakeOverItemSource = value;
                OnPropertyChanged("TakeOverItemSource");
            }
        }

        private int _TakeOverSelectedIndex = -1;
        public int TakeOverSelectedIndex
        {
            get
            {
                return _TakeOverSelectedIndex;
            }
            set
            {
                _TakeOverSelectedIndex = value;
                OnPropertyChanged("TakeOverSelectedIndex");
            }
        }

        private TakeOverHandOverInfo _TakeOverSelectedItem = new TakeOverHandOverInfo();
        public TakeOverHandOverInfo TakeOverSelectedItem
        {
            get
            {
                return _TakeOverSelectedItem;
            }
            set
            {
                _TakeOverSelectedItem = value;
                if (value != null)
                {
                    TakeOverUAVIDIndex = (int)value.AircraftID - 4;
                    if (value.CoordinateList != null)
                    {
                        TakeOverLAT = value.CoordinateList.Latitude;
                        TakeOverLON = value.CoordinateList.Longitude;
                        TakeOverALT = value.CoordinateList.Altitude;
                    }

                }
                OnPropertyChanged("TakeOverSelectedItem");
                UpdateTakeOverButtonState();
            }
        }

        private bool _SimPlayButtonEnable = true;
        /// <summary>
        /// 모의 여부 Flag - 모의 시작 누르면 false
        /// </summary>
        public bool SimPlayButtonEnable
        {
            get
            {
                return _SimPlayButtonEnable;
            }
            set
            {
                _SimPlayButtonEnable = value;
                OnPropertyChanged("SimPlayButtonEnable");

                UpdateTakeOverButtonState();
                UpdateHandOverButtonState();
                UpdateRTBButtonState();

                UpdateFlightAreaButtonState();
                UpdateProhibitedAreaButtonState();


            }
        }

        #region HandOver 통제권 반납 프로퍼티

        private string _HandOverButton1Text = "생성";
        public string HandOverButton1Text
        {
            get
            {
                return _HandOverButton1Text;
            }
            set
            {
                _HandOverButton1Text = value;
                OnPropertyChanged("HandOverButton1Text");
            }
        }

        private string _HandOverButton2Text = "수정";
        public string HandOverButton2Text
        {
            get
            {
                return _HandOverButton2Text;
            }
            set
            {
                _HandOverButton2Text = value;
                OnPropertyChanged("HandOverButton2Text");
            }
        }

        private string _HandOverButton3Text = "삭제";
        public string HandOverButton3Text
        {
            get
            {
                return _HandOverButton3Text;
            }
            set
            {
                _HandOverButton3Text = value;
                OnPropertyChanged("HandOverButton3Text");
            }
        }

        private bool _HandOverButton1Enable = true;
        public bool HandOverButton1Enable
        {
            get
            {
                return _HandOverButton1Enable;
            }
            set
            {
                _HandOverButton1Enable = value;
                OnPropertyChanged("HandOverButton1Enable");
            }
        }

        private bool _HandOverButton2Enable = false;
        public bool HandOverButton2Enable
        {
            get
            {
                return _HandOverButton2Enable;
            }
            set
            {
                _HandOverButton2Enable = value;
                OnPropertyChanged("HandOverButton2Enable");
            }
        }

        private bool _HandOverButton3Enable = false;
        public bool HandOverButton3Enable
        {
            get
            {
                return _HandOverButton3Enable;
            }
            set
            {
                _HandOverButton3Enable = value;
                OnPropertyChanged("HandOverButton3Enable");
            }
        }

        private int _HandOverUAVIDIndex = 0;
        public int HandOverUAVIDIndex
        {
            get
            {
                return _HandOverUAVIDIndex;
            }
            set
            {
                _HandOverUAVIDIndex = value;
                OnPropertyChanged("HandOverUAVIDIndex");
            }
        }

        private float _HandOverLAT = 0;
        public float HandOverLAT
        {
            get
            {
                return _HandOverLAT;
            }
            set
            {
                _HandOverLAT = value;
                OnPropertyChanged("HandOverLAT");
            }
        }

        private float _HandOverLON = 0;
        public float HandOverLON
        {
            get
            {
                return _HandOverLON;
            }
            set
            {
                _HandOverLON = value;
                OnPropertyChanged("HandOverLON");
            }
        }

        private int _HandOverALT = 1000;
        public int HandOverALT
        {
            get
            {
                return _HandOverALT;
            }
            set
            {
                _HandOverALT = value;
                OnPropertyChanged("HandOverALT");
            }
        }

        private bool _HandOverEditEnable = false;
        public bool HandOverEditEnable
        {
            get
            {
                return _HandOverEditEnable;
            }
            set
            {
                _HandOverEditEnable = value;
                OnPropertyChanged("HandOverEditEnable");
            }
        }

        private bool _HandOverChecked = false;
        public bool HandOverChecked
        {
            get
            {
                return _HandOverChecked;
            }
            set
            {
                _HandOverChecked = value;
                if (value == true)
                {
                    ViewModel_Unit_Map.SingletonInstance.ClearTempHandoverPoint();
                }
                OnPropertyChanged("HandOverChecked");
            }
        }

        private bool _HandOverCheckEditEnable = false;
        public bool HandOverCheckEditEnable
        {
            get
            {
                return _HandOverCheckEditEnable;
            }
            set
            {
                _HandOverCheckEditEnable = value;
                OnPropertyChanged("HandOverCheckEditEnable");
            }
        }

        private ObservableCollection<TakeOverHandOverInfo> _HandOverItemSource = new ObservableCollection<TakeOverHandOverInfo>();
        public ObservableCollection<TakeOverHandOverInfo> HandOverItemSource
        {
            get
            {
                return _HandOverItemSource;
            }
            set
            {
                _HandOverItemSource = value;
                OnPropertyChanged("HandOverItemSource");
            }
        }

        private int _HandOverSelectedIndex = -1;
        public int HandOverSelectedIndex
        {
            get
            {
                return _HandOverSelectedIndex;
            }
            set
            {
                _HandOverSelectedIndex = value;
                OnPropertyChanged("HandOverSelectedIndex");
            }
        }

        private TakeOverHandOverInfo _HandOverSelectedItem = new TakeOverHandOverInfo();
        public TakeOverHandOverInfo HandOverSelectedItem
        {
            get
            {
                return _HandOverSelectedItem;
            }
            set
            {
                _HandOverSelectedItem = value;
                if (value != null)
                {
                    HandOverUAVIDIndex = (int)value.AircraftID - 4;
                    if (value.CoordinateList != null)
                    {
                        HandOverLAT = value.CoordinateList.Latitude;
                        HandOverLON = value.CoordinateList.Longitude;
                        HandOverALT = value.CoordinateList.Altitude;
                    }

                }
                OnPropertyChanged("HandOverSelectedItem");
                UpdateHandOverButtonState();
            }
        }

        #endregion HandOver 통제권 획득 프로퍼티

        #region RTB 프로퍼티

        private string _RTBButton1Text = "생성";
        public string RTBButton1Text
        {
            get
            {
                return _RTBButton1Text;
            }
            set
            {
                _RTBButton1Text = value;
                OnPropertyChanged("RTBButton1Text");
            }
        }

        private string _RTBButton2Text = "수정";
        public string RTBButton2Text
        {
            get
            {
                return _RTBButton2Text;
            }
            set
            {
                _RTBButton2Text = value;
                OnPropertyChanged("RTBButton2Text");
            }
        }

        private string _RTBButton3Text = "삭제";
        public string RTBButton3Text
        {
            get
            {
                return _RTBButton3Text;
            }
            set
            {
                _RTBButton3Text = value;
                OnPropertyChanged("RTBButton3Text");
            }
        }

        private bool _RTBButton1Enable = true;
        public bool RTBButton1Enable
        {
            get
            {
                return _RTBButton1Enable;
            }
            set
            {
                _RTBButton1Enable = value;
                OnPropertyChanged("RTBButton1Enable");
            }
        }

        private bool _RTBButton2Enable = false;
        public bool RTBButton2Enable
        {
            get
            {
                return _RTBButton2Enable;
            }
            set
            {
                _RTBButton2Enable = value;
                OnPropertyChanged("RTBButton2Enable");
            }
        }

        private bool _RTBButton3Enable = false;
        public bool RTBButton3Enable
        {
            get
            {
                return _RTBButton3Enable;
            }
            set
            {
                _RTBButton3Enable = value;
                OnPropertyChanged("RTBButton3Enable");
            }
        }

        private int _RTBUAVIDIndex = 0;
        public int RTBUAVIDIndex
        {
            get
            {
                return _RTBUAVIDIndex;
            }
            set
            {
                _RTBUAVIDIndex = value;
                OnPropertyChanged("RTBUAVIDIndex");
            }
        }

        private float _RTBLAT = 0;
        public float RTBLAT
        {
            get
            {
                return _RTBLAT;
            }
            set
            {
                _RTBLAT = value;
                OnPropertyChanged("RTBLAT");
            }
        }

        private float _RTBLON = 0;
        public float RTBLON
        {
            get
            {
                return _RTBLON;
            }
            set
            {
                _RTBLON = value;
                OnPropertyChanged("RTBLON");
            }
        }

        private int _RTBALT = 1000;
        public int RTBALT
        {
            get
            {
                return _RTBALT;
            }
            set
            {
                _RTBALT = value;
                OnPropertyChanged("RTBALT");
            }
        }

        private bool _RTBEditEnable = false;
        public bool RTBEditEnable
        {
            get
            {
                return _RTBEditEnable;
            }
            set
            {
                _RTBEditEnable = value;
                OnPropertyChanged("RTBEditEnable");
            }
        }

        private bool _RTBChecked = false;
        public bool RTBChecked
        {
            get
            {
                return _RTBChecked;
            }
            set
            {
                _RTBChecked = value;
                if (value == true)
                {
                    //Clear 따로 만들어야하는지 체크
                    ViewModel_Unit_Map.SingletonInstance.ClearTempTakeoverPoint();
                }
                OnPropertyChanged("RTBChecked");
            }
        }

        private bool _RTBCheckEditEnable = false;
        public bool RTBCheckEditEnable
        {
            get
            {
                return _RTBCheckEditEnable;
            }
            set
            {
                _RTBCheckEditEnable = value;
                OnPropertyChanged("RTBCheckEditEnable");
            }
        }

        private int _RTBSelectedIndex = -1;
        public int RTBSelectedIndex
        {
            get
            {
                return _RTBSelectedIndex;
            }
            set
            {
                _RTBSelectedIndex = value;
                OnPropertyChanged("RTBSelectedIndex");
            }
        }

        // ID 발급기
        private int _rtbIdCounter = 0;



        // [수정] 리스트 타입을 RTBCoordinateInfo -> RTB_UI_Item으로 변경
        private ObservableCollection<RTB_UI_Item> _RTBItemSource = new ObservableCollection<RTB_UI_Item>();
        public ObservableCollection<RTB_UI_Item> RTBItemSource
        {
            get => _RTBItemSource;
            set { _RTBItemSource = value; OnPropertyChanged("RTBItemSource"); }
        }

        // [수정] SelectedItem 타입을 Wrapper로 변경 & 선택 시 텍스트박스 업데이트 로직 추가
        private RTB_UI_Item _RTBSelectedItem;
        public RTB_UI_Item RTBSelectedItem
        {
            get => _RTBSelectedItem;
            set
            {
                _RTBSelectedItem = value;
                if (value != null)
                {
                    // 그리드 선택 시 텍스트박스에 값 표시
                    RTBLAT = value.Latitude;
                    RTBLON = value.Longitude;
                    RTBALT = value.Altitude;
                }
                OnPropertyChanged("RTBSelectedItem");
                UpdateRTBButtonState();
            }
        }

        #endregion RTB 통제권 획득 프로퍼티

        #region 비행금지구역 프로퍼티

        private string _ProhibitedAreaButton1Text = "생성";
        public string ProhibitedAreaButton1Text
        {
            get
            {
                return _ProhibitedAreaButton1Text;
            }
            set
            {
                _ProhibitedAreaButton1Text = value;
                OnPropertyChanged("ProhibitedAreaButton1Text");
            }
        }

        private string _ProhibitedAreaButton2Text = "수정";
        public string ProhibitedAreaButton2Text
        {
            get
            {
                return _ProhibitedAreaButton2Text;
            }
            set
            {
                _ProhibitedAreaButton2Text = value;
                OnPropertyChanged("ProhibitedAreaButton2Text");
            }
        }

        private string _ProhibitedAreaButton3Text = "삭제";
        public string ProhibitedAreaButton3Text
        {
            get
            {
                return _ProhibitedAreaButton3Text;
            }
            set
            {
                _ProhibitedAreaButton3Text = value;
                OnPropertyChanged("ProhibitedAreaButton3Text");
            }
        }

        private bool _ProhibitedAreaButton1Enable = true;
        public bool ProhibitedAreaButton1Enable
        {
            get
            {
                return _ProhibitedAreaButton1Enable;
            }
            set
            {
                _ProhibitedAreaButton1Enable = value;
                OnPropertyChanged("ProhibitedAreaButton1Enable");
            }
        }

        private bool _ProhibitedAreaButton2Enable = false;
        public bool ProhibitedAreaButton2Enable
        {
            get
            {
                return _ProhibitedAreaButton2Enable;
            }
            set
            {
                _ProhibitedAreaButton2Enable = value;
                OnPropertyChanged("ProhibitedAreaButton2Enable");
            }
        }

        private bool _ProhibitedAreaButton3Enable = false;
        public bool ProhibitedAreaButton3Enable
        {
            get
            {
                return _ProhibitedAreaButton3Enable;
            }
            set
            {
                _ProhibitedAreaButton3Enable = value;
                OnPropertyChanged("ProhibitedAreaButton3Enable");
            }
        }

        private float _ProhibitedAreaLAT = 0;
        public float ProhibitedAreaLAT
        {
            get
            {
                return _ProhibitedAreaLAT;
            }
            set
            {
                _ProhibitedAreaLAT = value;
                OnPropertyChanged("ProhibitedAreaLAT");
            }
        }

        private float _ProhibitedAreaLON = 0;
        public float ProhibitedAreaLON
        {
            get
            {
                return _ProhibitedAreaLON;
            }
            set
            {
                _ProhibitedAreaLON = value;
                OnPropertyChanged("ProhibitedAreaLON");
            }
        }

        private int _ProhibitedAreaLowerALT = 200;
        public int ProhibitedAreaLowerALT
        {
            get
            {
                return _ProhibitedAreaLowerALT;
            }
            set
            {
                _ProhibitedAreaLowerALT = value;
                OnPropertyChanged("ProhibitedAreaLowerALT");
            }
        }

        private int _ProhibitedAreaUpperALT = 1500;
        public int ProhibitedAreaUpperALT
        {
            get
            {
                return _ProhibitedAreaUpperALT;
            }
            set
            {
                _ProhibitedAreaUpperALT = value;
                OnPropertyChanged("ProhibitedAreaUpperALT");
            }
        }

        private bool _ProhibitedAreaEditEnable = false;
        public bool ProhibitedAreaEditEnable
        {
            get
            {
                return _ProhibitedAreaEditEnable;
            }
            set
            {
                _ProhibitedAreaEditEnable = value;
                OnPropertyChanged("ProhibitedAreaEditEnable");
            }
        }

        private bool _ProhibitedAreaChecked = false;
        public bool ProhibitedAreaChecked
        {
            get
            {
                return _ProhibitedAreaChecked;
            }
            set
            {
                _ProhibitedAreaChecked = value;
                OnPropertyChanged("ProhibitedAreaChecked");
                UpdateInfoPanelState();
            }
        }

        private bool _ProhibitedAreaCheckEditEnable = false;
        public bool ProhibitedAreaCheckEditEnable
        {
            get
            {
                return _ProhibitedAreaCheckEditEnable;
            }
            set
            {
                _ProhibitedAreaCheckEditEnable = value;
                OnPropertyChanged("ProhibitedAreaCheckEditEnable");
            }
        }

        private ObservableCollection<AreaLatLonInfo> _ProhibitedAreaInnterItemSource = new ObservableCollection<AreaLatLonInfo>();
        public ObservableCollection<AreaLatLonInfo> ProhibitedAreaInnterItemSource
        {
            get
            {
                return _ProhibitedAreaInnterItemSource;
            }
            set
            {
                _ProhibitedAreaInnterItemSource = value;
                OnPropertyChanged("ProhibitedAreaInnterItemSource");
            }
        }

        private int _ProhibitedAreaSelectedIndex = -1;
        public int ProhibitedAreaSelectedIndex
        {
            get
            {
                return _ProhibitedAreaSelectedIndex;
            }
            set
            {
                _ProhibitedAreaSelectedIndex = value;
                OnPropertyChanged("ProhibitedAreaSelectedIndex");
            }
        }

        private int _ProhibitedAreaInnerSelectedIndex = -1;
        public int ProhibitedAreaInnerSelectedIndex
        {
            get
            {
                return _ProhibitedAreaInnerSelectedIndex;
            }
            set
            {
                _ProhibitedAreaInnerSelectedIndex = value;
                OnPropertyChanged("ProhibitedAreaInnerSelectedIndex");
            }
        }

        private AreaLatLonInfo _ProhibitedAreaInnerSelectedItem = new AreaLatLonInfo();
        public AreaLatLonInfo ProhibitedAreaInnerSelectedItem
        {
            get
            {
                return _ProhibitedAreaInnerSelectedItem;
            }
            set
            {
                _ProhibitedAreaInnerSelectedItem = value;
                if (value != null)
                {
                    ProhibitedAreaLAT = value.Latitude;
                    ProhibitedAreaLON = value.Longitude;
                }
                OnPropertyChanged("ProhibitedAreaInnerSelectedItem");
            }
        }

        // [신규] ID 발급기
        private int _prohibitedAreaIdCounter = 0;

        // [수정] Wrapper 리스트 사용
        private ObservableCollection<ProhibitedAreaWrapper> _ProhibitedAreaItemSource = new ObservableCollection<ProhibitedAreaWrapper>();
        public ObservableCollection<ProhibitedAreaWrapper> ProhibitedAreaItemSource
        {
            get => _ProhibitedAreaItemSource;
            set
            {
                _ProhibitedAreaItemSource = value;
                OnPropertyChanged("ProhibitedAreaItemSource");
            }
        }

        // 선택된 항목 Wrapper
        private ProhibitedAreaWrapper _ProhibitedAreaSelectedItem;
        public ProhibitedAreaWrapper ProhibitedAreaSelectedItem
        {
            get => _ProhibitedAreaSelectedItem;
            set
            {
                _ProhibitedAreaSelectedItem = value;
                OnPropertyChanged("ProhibitedAreaSelectedItem");

                //선택 항목이 바뀌었으니 버튼 상태 갱신
                UpdateProhibitedAreaButtonState();
            }
        }

        #endregion ProhibitedArea 프로퍼티

        #region 비행가능구역 프로퍼티


        private string _FlightAreaButton1Text = "생성";
        public string FlightAreaButton1Text
        {
            get
            {
                return _FlightAreaButton1Text;
            }
            set
            {
                _FlightAreaButton1Text = value;
                OnPropertyChanged("FlightAreaButton1Text");
            }
        }

        private string _FlightAreaButton2Text = "수정";
        public string FlightAreaButton2Text
        {
            get
            {
                return _FlightAreaButton2Text;
            }
            set
            {
                _FlightAreaButton2Text = value;
                OnPropertyChanged("FlightAreaButton2Text");
            }
        }

        private string _FlightAreaButton3Text = "삭제";
        public string FlightAreaButton3Text
        {
            get
            {
                return _FlightAreaButton3Text;
            }
            set
            {
                _FlightAreaButton3Text = value;
                OnPropertyChanged("FlightAreaButton3Text");
            }
        }

        private bool _FlightAreaButton1Enable = true;
        public bool FlightAreaButton1Enable
        {
            get
            {
                return _FlightAreaButton1Enable;
            }
            set
            {
                _FlightAreaButton1Enable = value;
                OnPropertyChanged("FlightAreaButton1Enable");
            }
        }

        private bool _FlightAreaButton2Enable = false;
        public bool FlightAreaButton2Enable
        {
            get
            {
                return _FlightAreaButton2Enable;
            }
            set
            {
                _FlightAreaButton2Enable = value;
                OnPropertyChanged("FlightAreaButton2Enable");
            }
        }

        private bool _FlightAreaButton3Enable = false;
        public bool FlightAreaButton3Enable
        {
            get
            {
                return _FlightAreaButton3Enable;
            }
            set
            {
                _FlightAreaButton3Enable = value;
                OnPropertyChanged("FlightAreaButton3Enable");
            }
        }

        private float _FlightAreaLAT = 0;
        public float FlightAreaLAT
        {
            get
            {
                return _FlightAreaLAT;
            }
            set
            {
                _FlightAreaLAT = value;
                OnPropertyChanged("FlightAreaLAT");
            }
        }

        private float _FlightAreaLON = 0;
        public float FlightAreaLON
        {
            get
            {
                return _FlightAreaLON;
            }
            set
            {
                _FlightAreaLON = value;
                OnPropertyChanged("FlightAreaLON");
            }
        }

        private int _FlightAreaLowerALT = 200;
        public int FlightAreaLowerALT
        {
            get
            {
                return _FlightAreaLowerALT;
            }
            set
            {
                _FlightAreaLowerALT = value;
                OnPropertyChanged("FlightAreaLowerALT");
            }
        }

        private int _FlightAreaUpperALT = 1500;
        public int FlightAreaUpperALT
        {
            get
            {
                return _FlightAreaUpperALT;
            }
            set
            {
                _FlightAreaUpperALT = value;
                OnPropertyChanged("FlightAreaUpperALT");
            }
        }

        private bool _FlightAreaEditEnable = false;
        public bool FlightAreaEditEnable
        {
            get
            {
                return _FlightAreaEditEnable;
            }
            set
            {
                _FlightAreaEditEnable = value;
                OnPropertyChanged("FlightAreaEditEnable");
            }
        }

        private bool _FlightAreaChecked = false;
        public bool FlightAreaChecked
        {
            get
            {
                return _FlightAreaChecked;
            }
            set
            {
                _FlightAreaChecked = value;
                OnPropertyChanged("FlightAreaChecked");
                UpdateInfoPanelState();
            }
        }

        private bool _FlightAreaCheckEditEnable = false;
        public bool FlightAreaCheckEditEnable
        {
            get
            {
                return _FlightAreaCheckEditEnable;
            }
            set
            {
                _FlightAreaCheckEditEnable = value;
                OnPropertyChanged("FlightAreaCheckEditEnable");
            }
        }


        private ObservableCollection<AreaLatLonInfo> _FlightAreaInnterItemSource = new ObservableCollection<AreaLatLonInfo>();
        public ObservableCollection<AreaLatLonInfo> FlightAreaInnterItemSource
        {
            get
            {
                return _FlightAreaInnterItemSource;
            }
            set
            {
                _FlightAreaInnterItemSource = value;
                OnPropertyChanged("FlightAreaInnterItemSource");
            }
        }

        private int _FlightAreaSelectedIndex = -1;
        public int FlightAreaSelectedIndex
        {
            get
            {
                return _FlightAreaSelectedIndex;
            }
            set
            {
                _FlightAreaSelectedIndex = value;
                OnPropertyChanged("FlightAreaSelectedIndex");
            }
        }

        private int _FlightAreaInnerSelectedIndex = -1;
        public int FlightAreaInnerSelectedIndex
        {
            get
            {
                return _FlightAreaInnerSelectedIndex;
            }
            set
            {
                _FlightAreaInnerSelectedIndex = value;
                OnPropertyChanged("FlightAreaInnerSelectedIndex");
            }
        }

        private AreaLatLonInfo _FlightAreaInnerSelectedItem = new AreaLatLonInfo();
        public AreaLatLonInfo FlightAreaInnerSelectedItem
        {
            get
            {
                return _FlightAreaInnerSelectedItem;
            }
            set
            {
                _FlightAreaInnerSelectedItem = value;
                if (value != null)
                {
                    FlightAreaLAT = value.Latitude;
                    FlightAreaLON = value.Longitude;
                }
                OnPropertyChanged("FlightAreaInnerSelectedItem");
            }
        }

        // [신규] ID 발급기
        private int _flightAreaIdCounter = 0;

        // [수정] Wrapper 리스트 사용
        private ObservableCollection<FlightAreaWrapper> _FlightAreaItemSource = new ObservableCollection<FlightAreaWrapper>();
        public ObservableCollection<FlightAreaWrapper> FlightAreaItemSource
        {
            get => _FlightAreaItemSource;
            set
            {
                _FlightAreaItemSource = value;
                OnPropertyChanged("FlightAreaItemSource");
            }
        }

        // 선택된 항목 Wrapper
        private FlightAreaWrapper _FlightAreaSelectedItem;
        public FlightAreaWrapper FlightAreaSelectedItem
        {
            get => _FlightAreaSelectedItem;
            set
            {
                _FlightAreaSelectedItem = value;
                OnPropertyChanged("FlightAreaSelectedItem");

                //선택 항목이 바뀌었으니 버튼 상태 갱신
                UpdateFlightAreaButtonState();
            }
        }


        #endregion FlightArea 프로퍼티
    }

}
