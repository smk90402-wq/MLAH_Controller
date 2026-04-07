using DevExpress.Xpf.Core;
using DevExpress.Xpf.Map;
using System.Collections.ObjectModel;
using System.Windows.Threading;


namespace MLAH_Controller
{
    public partial class ViewModel_Unit_Map : CommonBase
    {
        public enum EditLayerType
        {
            None,           // 편집 안 함
            InitMission,    // 초기 임무 수정(InitMissionPolygon)
            FlightArea,     // 비행 가능 구역
            ProhibitedArea,  // 비행 금지 구역
            TempInitMission  // 초기 임무 생성(InitMissionPolygon)
        }

        // 2. 현재 편집 중인 레이어 상태 속성
        private EditLayerType _CurrentEditLayer = EditLayerType.None;
        public EditLayerType CurrentEditLayer
        {
            get => _CurrentEditLayer;
            set
            {
                _CurrentEditLayer = value;
                OnPropertyChanged("CurrentEditLayer");
            }
        }

        // (참고) 각 모드로 변경하는 커맨드나 메서드를 연결하여 사용
        public void SetEditMode(EditLayerType mode)
        {
            CurrentEditLayer = mode;
        }

        private ObservableCollectionCore<UnitMapObjectInfo> _ObjectDisplayList = new ObservableCollectionCore<UnitMapObjectInfo>();
        public ObservableCollectionCore<UnitMapObjectInfo> ObjectDisplayList
        {
            get
            {
                return _ObjectDisplayList;
            }
            set
            {
                _ObjectDisplayList = value;
                OnPropertyChanged("ObjectDisplayList");
            }
        }


        private ObservableCollection<MapPolygon> _UnrealLandScapeList = new ObservableCollection<MapPolygon>();
        public ObservableCollection<MapPolygon> UnrealLandScapeList
        {
            get
            {
                return _UnrealLandScapeList;
            }
            set
            {
                _UnrealLandScapeList = value;
                OnPropertyChanged("UnrealLandScapeList");
            }
        }

        private ObservableCollectionCore<MapPolygon> _FocusSquareList = new ObservableCollectionCore<MapPolygon>();
        public ObservableCollectionCore<MapPolygon> FocusSquareList
        {
            get
            {
                return _FocusSquareList;
            }
            set
            {
                _FocusSquareList = value;
                OnPropertyChanged("FocusSquareList");
            }
        }

        private MapPolyline _focusHeadingLine;

        private ObservableCollectionCore<MapPolyline> _FocusHeadingLineList = new ObservableCollectionCore<MapPolyline>();
        public ObservableCollectionCore<MapPolyline> FocusHeadingLineList
        {
            get => _FocusHeadingLineList;
            set
            {
                _FocusHeadingLineList = value;
                OnPropertyChanged("FocusHeadingLineList");
            }
        }


        private ObservableCollection<CustomMapPolygon> _TempINITMissionPolygonList = new ObservableCollection<CustomMapPolygon>();
        public ObservableCollection<CustomMapPolygon> TempINITMissionPolygonList
        {
            get
            {
                return _TempINITMissionPolygonList;
            }
            set
            {
                _TempINITMissionPolygonList = value;
                OnPropertyChanged("TempINITMissionPolygonList");
            }
        }

        private ObservableCollection<CustomMapPolygon> _PreINITMissionPolygonList = new ObservableCollection<CustomMapPolygon>();
        public ObservableCollection<CustomMapPolygon> PreINITMissionPolygonList
        {
            get
            {
                return _PreINITMissionPolygonList;
            }
            set
            {
                _PreINITMissionPolygonList = value;
                OnPropertyChanged("PreINITMissionPolygonList");
            }
        }

        private ObservableCollection<MapLine> _TempINITMissionPolygonLineList = new ObservableCollection<MapLine>();
        public ObservableCollection<MapLine> TempINITMissionPolygonLineList
        {
            get
            {
                return _TempINITMissionPolygonLineList;
            }
            set
            {
                _TempINITMissionPolygonLineList = value;
                OnPropertyChanged("TempINITMissionPolygonLineList");
            }
        }

        private ObservableCollection<CustomMapPolygon> _INITMissionPolygonList = new ObservableCollection<CustomMapPolygon>();
        public ObservableCollection<CustomMapPolygon> INITMissionPolygonList
        {
            get
            {
                return _INITMissionPolygonList;
            }
            set
            {
                _INITMissionPolygonList = value;
                OnPropertyChanged("INITMissionPolygonList");
            }
        }

        private ObservableCollection<CustomMapPolygon> _FlightAreaPolygonList = new ObservableCollection<CustomMapPolygon>();
        public ObservableCollection<CustomMapPolygon> FlightAreaPolygonList
        {
            get
            {
                return _FlightAreaPolygonList;
            }
            set
            {
                _FlightAreaPolygonList = value;
                OnPropertyChanged("FlightAreaPolygonList");
            }
        }

        private bool _isFlightAreaEditMode;
        public bool IsFlightAreaEditMode
        {
            get => _isFlightAreaEditMode;
            set
            {
                _isFlightAreaEditMode = value;
                OnPropertyChanged(nameof(IsFlightAreaEditMode));

                // 편집 모드가 꺼지면(저장 시점), 현재 맵에 그려진 모양을 데이터로 확정 짓는 로직이 필요할 수 있음
                // 수정 모드에서 완료할 때 하면 될듯
            }
        }

        private bool _isINITMissionPolygonEditMode;
        public bool IsINITMissionPolygonEditMode
        {
            get => _isINITMissionPolygonEditMode;
            set
            {
                _isINITMissionPolygonEditMode = value;
                OnPropertyChanged(nameof(IsINITMissionPolygonEditMode));

                // 편집 모드가 꺼지면(저장 시점), 현재 맵에 그려진 모양을 데이터로 확정 짓는 로직이 필요할 수 있음
                // 수정 모드에서 완료할 때 하면 될듯
            }
        }

        private bool _isTempINITMissionPolygonEditMode;
        public bool IsTempINITMissionPolygonEditMode
        {
            get => _isTempINITMissionPolygonEditMode;
            set
            {
                _isTempINITMissionPolygonEditMode = value;
                OnPropertyChanged(nameof(IsTempINITMissionPolygonEditMode));

                // 편집 모드가 꺼지면(저장 시점), 현재 맵에 그려진 모양을 데이터로 확정 짓는 로직이 필요할 수 있음
                // 수정 모드에서 완료할 때 하면 될듯
            }
        }




        private ObservableCollection<CustomMapPolygon> _ProhibitedAreaPolygonList = new ObservableCollection<CustomMapPolygon>();
        public ObservableCollection<CustomMapPolygon> ProhibitedAreaPolygonList
        {
            get
            {
                return _ProhibitedAreaPolygonList;
            }
            set
            {
                _ProhibitedAreaPolygonList = value;
                OnPropertyChanged("ProhibitedAreaPolygonList");
            }
        }

        private bool _isProhibitedAreaEditMode;
        public bool IsProhibitedAreaEditMode
        {
            get => _isProhibitedAreaEditMode;
            set
            {
                _isProhibitedAreaEditMode = value;
                OnPropertyChanged(nameof(IsProhibitedAreaEditMode));

                // 편집 모드가 꺼지면(저장 시점), 현재 맵에 그려진 모양을 데이터로 확정 짓는 로직이 필요할 수 있음
                // 수정 모드에서 완료할 때 하면 될듯
            }
        }

        private ObservableCollection<MapLine> _TempINITMissionLineList = new ObservableCollection<MapLine>();
        public ObservableCollection<MapLine> TempINITMissionLineList
        {
            get
            {
                return _TempINITMissionLineList;
            }
            set
            {
                _TempINITMissionLineList = value;
                OnPropertyChanged("TempINITMissionLineList");
            }
        }

        private ObservableCollection<CustomMapLine> _INITMissionLineList = new ObservableCollection<CustomMapLine>();
        public ObservableCollection<CustomMapLine> INITMissionLineList
        {
            get
            {
                return _INITMissionLineList;
            }
            set
            {
                _INITMissionLineList = value;
                OnPropertyChanged("INITMissionLineList");
            }
        }

        private ObservableCollection<CustomMapPolygon> _INITMissionLinePolygonList = new ObservableCollection<CustomMapPolygon>();
        public ObservableCollection<CustomMapPolygon> INITMissionLinePolygonList
        {
            get
            {
                return _INITMissionLinePolygonList;
            }
            set
            {
                _INITMissionLinePolygonList = value;
                OnPropertyChanged("INITMissionLinePolygonList");
            }
        }

        private ObservableCollection<CustomMapPoint> _INITMissionLineLabelList = new ObservableCollection<CustomMapPoint>();
        public ObservableCollection<CustomMapPoint> INITMissionLineLabelList
        {
            get
            {
                return _INITMissionLineLabelList;
            }
            set
            {
                _INITMissionLineLabelList= value;
                OnPropertyChanged("INITMissionLineLabelList");
            }
        }

        private ObservableCollection<MapPolyline> _LAHWapointList = new ObservableCollection<MapPolyline>();
        public ObservableCollection<MapPolyline> LAHWapointList
        {
            get
            {
                return _LAHWapointList;
            }
            set
            {
                _LAHWapointList = value;
                OnPropertyChanged("LAHWapointList");
            }
        }

        private ObservableCollection<MapPolyline> _lahStaticLineList = new ObservableCollection<MapPolyline>();
        public ObservableCollection<MapPolyline> LAHStaticLineList
        {
            get => _lahStaticLineList;
            set { _lahStaticLineList = value; OnPropertyChanged(nameof(LAHStaticLineList)); }
        }

        private ObservableCollectionCore<MapPolyline> _lahPulseLineList = new ObservableCollectionCore<MapPolyline>();
        public ObservableCollectionCore<MapPolyline> LAHPulseLineList
        {
            get => _lahPulseLineList;
            set { _lahPulseLineList = value; OnPropertyChanged(nameof(LAHPulseLineList)); }
        }

        private ObservableCollection<MapPolyline> _UAVWapointList = new ObservableCollection<MapPolyline>();
        public ObservableCollection<MapPolyline> UAVWapointList
        {
            get
            {
                return _UAVWapointList;
            }
            set
            {
                _UAVWapointList = value;
                OnPropertyChanged("UAVWapointList");
            }
        }

        private ObservableCollection<MapPolyline> _TextTestWapointList = new ObservableCollection<MapPolyline>();
        public ObservableCollection<MapPolyline> TextTestWapointList
        {
            get
            {
                return _TextTestWapointList;
            }
            set
            {
                _TextTestWapointList = value;
                OnPropertyChanged("TextTestWapointList");
            }
        }

        private ObservableCollection<GeoPoint> _TempINITMissionPointList = new ObservableCollection<GeoPoint>();
        public ObservableCollection<GeoPoint> TempINITMissionPointList
        {
            get
            {
                return _TempINITMissionPointList;
            }
            set
            {
                _TempINITMissionPointList = value;
                OnPropertyChanged("TempCompINITMissionPointList");
            }
        }

        private ObservableCollection<CustomMapPoint> _INITMissionPointList = new ObservableCollection<CustomMapPoint>();
        public ObservableCollection<CustomMapPoint> INITMissionPointList
        {
            get
            {
                return _INITMissionPointList;
            }
            set
            {
                _INITMissionPointList = value;
                OnPropertyChanged("INITMissionPointList");
            }
        }

        private ObservableCollection<CustomMapPoint> _TakeOverPointList = new ObservableCollection<CustomMapPoint>();
        public ObservableCollection<CustomMapPoint> TakeOverPointList
        {
            get
            {
                return _TakeOverPointList;
            }
            set
            {
                _TakeOverPointList = value;
                OnPropertyChanged("TakeOverPointList");
            }
        }

        private ObservableCollection<CustomMapPoint> _HandOverPointList = new ObservableCollection<CustomMapPoint>();
        public ObservableCollection<CustomMapPoint> HandOverPointList
        {
            get
            {
                return _HandOverPointList;
            }
            set
            {
                _HandOverPointList = value;
                OnPropertyChanged("HandOverPointList");
            }
        }

        private ObservableCollection<CustomMapPoint> _RTBPointList = new ObservableCollection<CustomMapPoint>();
        public ObservableCollection<CustomMapPoint> RTBPointList
        {
            get
            {
                return _RTBPointList;
            }
            set
            {
                _RTBPointList = value;
                OnPropertyChanged("RTBPointList");
            }
        }





        private ObservableCollection<MapPolyline> _TempUnitDevelopPathPlanList = new ObservableCollection<MapPolyline>();
        public ObservableCollection<MapPolyline> TempUnitDevelopPathPlanList
        {
            get
            {
                return _TempUnitDevelopPathPlanList;
            }
            set
            {
                _TempUnitDevelopPathPlanList = value;
                OnPropertyChanged("TempUnitDevelopPathPlanList");
            }
        }

        private ObservableCollection<MapPolyline> _UnitDevelopPathPlanList = new ObservableCollection<MapPolyline>();
        public ObservableCollection<MapPolyline> UnitDevelopPathPlanList
        {
            get
            {
                return _UnitDevelopPathPlanList;
            }
            set
            {
                _UnitDevelopPathPlanList = value;
                OnPropertyChanged("UnitDevelopPathPlanList");
            }
        }

        private ObservableCollection<CustomMapPoint> _LAHWpMarkerList = new ObservableCollection<CustomMapPoint>();
        public ObservableCollection<CustomMapPoint> LAHWpMarkerList
        {
            get => _LAHWpMarkerList;
            set
            {
                _LAHWpMarkerList = value;
                OnPropertyChanged(nameof(LAHWpMarkerList));
            }
        }

        private ObservableCollection<CustomMapPoint> _UAVWpMarkerList = new ObservableCollection<CustomMapPoint>();
        public ObservableCollection<CustomMapPoint> UAVWpMarkerList
        {
            get => _UAVWpMarkerList;
            set
            {
                _UAVWpMarkerList = value;
                OnPropertyChanged(nameof(UAVWpMarkerList));
            }
        }

        private ObservableCollection<CustomMapPoint> _TextTestWpMarkerList = new ObservableCollection<CustomMapPoint>();
        public ObservableCollection<CustomMapPoint> TextTestWpMarkerList
        {
            get => _TextTestWpMarkerList;
            set
            {
                _TextTestWpMarkerList = value;
                OnPropertyChanged(nameof(TextTestWpMarkerList));
            }
        }

        private double _MapCursorLat = 0;
        public double MapCursorLat
        {
            get
            {
                return _MapCursorLat;
            }
            set
            {
                _MapCursorLat = value;
                OnPropertyChanged("MapCursorLat");
            }
        }

        private double _MapCursorLon = 0;
        public double MapCursorLon
        {
            get
            {
                return _MapCursorLon;
            }
            set
            {
                _MapCursorLon = value;
                OnPropertyChanged("MapCursorLon");
            }
        }

        private int _MapCursorAlt = 0;
        public int MapCursorAlt
        {
            get
            {
                return _MapCursorAlt;
            }
            set
            {
                _MapCursorAlt = value;
                OnPropertyChanged("MapCursorAlt");
            }
        }

        //SRTM 리더 인스턴스 (한 번만 열어두고 계속 쓰기 위함)
        public SrtmReader SrtmReaderInstance { get; private set; }

        public void InitializeSrtm(string srtmPath)
        {
            try
            {
                if (System.IO.File.Exists(srtmPath))
                {
                    SrtmReaderInstance = new SrtmReader(srtmPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SRTM Load Error: " + ex.Message);
            }
        }

        DispatcherTimer _animationTimer;
        private double _currentDashOffset = 0;

        private StrokeStyle _baseStrokeStyle;  // 배경용 (고정)
        private StrokeStyle _pulseStrokeStyle; // 펄스용 (애니메이션)



        private MapPolygon? _focusPolygon;

        //FootPrint롤백
        //private MapPolygon _footprintPolygon;
        //private readonly List<MapLine> _sideLines = new List<MapLine>();
        // UAV 시각 요소를 그룹으로 관리하기 위한 내부 클래스
        private class UavVisualSet
        {
            public MapPolygon FootprintPoly { get; set; }
            public List<MapLine> SideLines { get; set; } = new List<MapLine>();
        }

        // 현재 화면에 그려지고 있는 UAV들의 시각 요소 보관소 (Key: UAV ID)
        private Dictionary<int, UavVisualSet> _activeUavVisuals = new Dictionary<int, UavVisualSet>();
        // 항상 표시할 UAV ID 목록 (4, 5, 6번)
        private readonly HashSet<int> _alwaysShowIds = new HashSet<int> { 4, 5, 6 };

        //Footprint롤백




        private CancellationTokenSource _visualsUpdateCts;

        public ObservableCollection<MapPolygon> FocusSquareItems { get; } = new ObservableCollection<MapPolygon>();

        // UAV 촬영 영역의 '바닥면'을 위한 컬렉션
        public ObservableCollection<MapPolygon> FourCornerItems { get; } = new ObservableCollection<MapPolygon>();

        // UAV 촬영 영역의 '옆면(선)'을 위한 새로운 컬렉션
        public ObservableCollection<MapLine> FootprintSideLines { get; } = new ObservableCollection<MapLine>();

        




        private double _currentZoomLevel = 11.0; // XAML의 초기 ZoomLevel과 맞추거나 기본값 설정
        public double CurrentZoomLevel
        {
            get => _currentZoomLevel;
            set
            {
                if (_currentZoomLevel != value)
                {
                    _currentZoomLevel = value;
                    OnPropertyChanged(nameof(CurrentZoomLevel));
                }
            }
        }

        // ──────────────────────────────────────────────
        //  지도 레이어 필터
        // ──────────────────────────────────────────────

        private bool _FilterEntityID = true;
        public bool FilterEntityID
        {
            get => _FilterEntityID;
            set { _FilterEntityID = value; OnPropertyChanged(nameof(FilterEntityID)); }
        }

        private bool _FilterFlightRefInfo = true;
        public bool FilterFlightRefInfo
        {
            get => _FilterFlightRefInfo;
            set { _FilterFlightRefInfo = value; OnPropertyChanged(nameof(FilterFlightRefInfo)); }
        }

        private bool _FilterLAHPlan = true;
        public bool FilterLAHPlan
        {
            get => _FilterLAHPlan;
            set { _FilterLAHPlan = value; OnPropertyChanged(nameof(FilterLAHPlan)); }
        }

        private bool _FilterUAVPlan = true;
        public bool FilterUAVPlan
        {
            get => _FilterUAVPlan;
            set { _FilterUAVPlan = value; OnPropertyChanged(nameof(FilterUAVPlan)); }
        }
    }
}