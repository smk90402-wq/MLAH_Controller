
using DevExpress.Map;
using DevExpress.Map.Kml.Model;
using DevExpress.Mvvm.Native;
using DevExpress.Pdf.ContentGeneration;
using DevExpress.Xpf.CodeView;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Layout.Core;
using DevExpress.Xpf.Map;
using DevExpress.XtraRichEdit.Model;
using MLAH_Controller;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
//using GMap.NET;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Windows.Devices.Geolocation;
using static DevExpress.Charts.Designer.Native.BarThicknessEditViewModel;
using static MLAH_Controller.CommonUtil;



namespace MLAH_Controller
{


    public partial class ViewModel_UC_Unit_INITMissionInfo : CommonBase
    {

        private bool _isPointSelected = true;
        private bool _isLineSelected = false;
        private bool _isAreaSelected = false;

        public bool IsPointSelected
        {
            get => _isPointSelected; // 자기 자신 반환
            set
            {
                if (value && !_isPointSelected)
                {
                    _isPointSelected = true;  // Point 켜기
                    _isLineSelected = false;  // 나머지 끄기
                    _isAreaSelected = false;
                    OnPropertyChanged(nameof(IsPointSelected));
                    OnPropertyChanged(nameof(IsLineSelected));
                    OnPropertyChanged(nameof(IsAreaSelected));

                    ShapeTypeIndex = 1; // 동기화 (Point)
                }
            }
        }

        public bool IsLineSelected
        {
            get => _isLineSelected; // 자기 자신 반환
            set
            {
                if (value && !_isLineSelected)
                {
                    _isPointSelected = false;
                    _isLineSelected = true;   // Line 켜기
                    _isAreaSelected = false;
                    OnPropertyChanged(nameof(IsPointSelected));
                    OnPropertyChanged(nameof(IsLineSelected));
                    OnPropertyChanged(nameof(IsAreaSelected));

                    ShapeTypeIndex = 2; // 동기화 (Line)
                }
            }
        }

        public bool IsAreaSelected
        {
            get => _isAreaSelected; // 자기 자신 반환
            set
            {
                if (value && !_isAreaSelected)
                {
                    _isPointSelected = false;
                    _isLineSelected = false;
                    _isAreaSelected = true;   // Area 켜기
                    OnPropertyChanged(nameof(IsPointSelected));
                    OnPropertyChanged(nameof(IsLineSelected));
                    OnPropertyChanged(nameof(IsAreaSelected));

                    ShapeTypeIndex = 3; // 동기화 (Area)
                }
            }
        }
        private enum MenuButtonState { None, Creating, Editing }
        private MenuButtonState _state = MenuButtonState.None;

        public enum MissionEditState { None, Creating, Editing }
        //private enum PolygonMenuButtonState { None, Creating, Editing }
        private MissionEditState _PolygonState = MissionEditState.None;
        public MissionEditState PolygonState
        {
            get => _PolygonState;
            set
            {
                _PolygonState = value;
                OnPropertyChanged("PolygonState");
                UpdatePolygonControlState();     // 버튼 상태 갱신
                UpdateMapInfoPanel();           // 지도 패널(단축키 E) 갱신
            }
        }

        #region [신규] 맵 오버레이 정보 패널 속성

        private bool _IsInfoPanelVisible = false;
        public bool IsInfoPanelVisible
        {
            get => _IsInfoPanelVisible;
            set { _IsInfoPanelVisible = value; OnPropertyChanged("IsInfoPanelVisible"); }
        }

        private string _CurrentModeTitle = "";
        public string CurrentModeTitle
        {
            get => _CurrentModeTitle;
            set { _CurrentModeTitle = value; OnPropertyChanged("CurrentModeTitle"); }
        }

        private string _CurrentShortcutKey = "";
        public string CurrentShortcutKey
        {
            get => _CurrentShortcutKey;
            set { _CurrentShortcutKey = value; OnPropertyChanged("CurrentShortcutKey"); }
        }

        #endregion

        private string _Button1Text = "생성";
        public string Button1Text
        {
            get
            {
                return _Button1Text;
            }
            set
            {
                _Button1Text = value;
                OnPropertyChanged("Button1Text");
            }
        }

        private string _Button2Text = "수정";
        public string Button2Text
        {
            get
            {
                return _Button2Text;
            }
            set
            {
                _Button2Text = value;
                OnPropertyChanged("Button2Text");
            }
        }

        private string _Button3Text = "삭제";
        public string Button3Text
        {
            get
            {
                return _Button3Text;
            }
            set
            {
                _Button3Text = value;
                OnPropertyChanged("Button3Text");
            }
        }

        private string _PolygonButton1Text = "생성";
        public string PolygonButton1Text
        {
            get
            {
                return _PolygonButton1Text;
            }
            set
            {
                _PolygonButton1Text = value;
                OnPropertyChanged("PolygonButton1Text");
            }
        }

        private string _PolygonButton2Text = "수정";
        public string PolygonButton2Text
        {
            get
            {
                return _PolygonButton2Text;
            }
            set
            {
                _PolygonButton2Text = value;
                OnPropertyChanged("PolygonButton2Text");
            }
        }

        private string _PolygonButton3Text = "삭제";
        public string PolygonButton3Text
        {
            get
            {
                return _PolygonButton3Text;
            }
            set
            {
                _PolygonButton3Text = value;
                OnPropertyChanged("PolygonButton3Text");
            }
        }

        private bool _Button1Enable = true;
        public bool Button1Enable
        {
            get
            {
                return _Button1Enable;
            }
            set
            {
                _Button1Enable = value;
                OnPropertyChanged("Button1Enable");
            }
        }

        private bool _Button2Enable = false;
        public bool Button2Enable
        {
            get
            {
                return _Button2Enable;
            }
            set
            {
                _Button2Enable = value;
                OnPropertyChanged("Button2Enable");
            }
        }

        private bool _Button3Enable = false;
        public bool Button3Enable
        {
            get
            {
                return _Button3Enable;
            }
            set
            {
                _Button3Enable = value;
                OnPropertyChanged("Button3Enable");
            }
        }

        private bool _PolygonButton2Enable = false;
        public bool PolygonButton2Enable
        {
            get
            {
                return _PolygonButton2Enable;
            }
            set
            {
                _PolygonButton2Enable = value;
                OnPropertyChanged("PolygonButton2Enable");
            }
        }

        private bool _PolygonButton3Enable = false;
        public bool PolygonButton3Enable
        {
            get
            {
                return _PolygonButton3Enable;
            }
            set
            {
                _PolygonButton3Enable = value;
                OnPropertyChanged("PolygonButton3Enable");
            }
        }

        private double _PolygonLatControl;
        public double PolygonLatControl
        {
            get
            {
                return _PolygonLatControl;
            }
            set
            {
                _PolygonLatControl = value;
                OnPropertyChanged("PolygonLatControl");
            }
        }

        private double _PolygonLonControl;
        public double PolygonLonControl
        {
            get
            {
                return _PolygonLonControl;
            }
            set
            {
                _PolygonLonControl = value;
                OnPropertyChanged("PolygonLonControl");
            }
        }

        private int _PolygonAltControl;
        public int PolygonAltControl
        {
            get
            {
                return _PolygonAltControl;
            }
            set
            {
                _PolygonAltControl = value;
                OnPropertyChanged("PolygonAltControl");
            }
        }

        private double _LineLatControl;
        public double LineLatControl
        {
            get
            {
                return _LineLatControl;
            }
            set
            {
                _LineLatControl = value;
                OnPropertyChanged("LineLatControl");
            }
        }

        private double _LineLonControl;
        public double LineLonControl
        {
            get
            {
                return _LineLonControl;
            }
            set
            {
                _LineLonControl = value;
                OnPropertyChanged("LineLonControl");
            }
        }

        private int _LineAltControl = 900;
        public int LineAltControl
        {
            get
            {
                return _LineAltControl;
            }
            set
            {
                _LineAltControl = value;
                OnPropertyChanged("LineAltControl");
            }
        }


        private int _MissionTypeIndex = 0;
        public int MissionTypeIndex
        {
            get
            {
                return _MissionTypeIndex;
            }
            set
            {
                _MissionTypeIndex = value;
                //이동 엄호
                if (value == 1 || value == 4 || value == 5)
                {
                    ShapeTypeIndex = 2;
                }
                //경계 수색
                else if (value == 2 || value == 3 || value == 6)
                {
                    ShapeTypeIndex = 3;
                }
                OnPropertyChanged("MissionTypeIndex");
            }
        }

        private uint _RegionTypeIndex = 0;
        public uint RegionTypeIndex
        {
            get
            {
                return _RegionTypeIndex;
            }
            set
            {
                _RegionTypeIndex = value;
                OnPropertyChanged("RegionTypeIndex");
            }
        }


        private uint _Name_Control = 70000000;
        public uint Name_Control
        {
            get
            {
                return _Name_Control;
            }
            set
            {
                _Name_Control = value;
                OnPropertyChanged("Name_Control");
            }
        }


        private uint _Order_Control = 0;
        public uint Order_Control
        {
            get
            {
                return _Order_Control;
            }
            set
            {
                _Order_Control = value;
                OnPropertyChanged("Order_Control");
            }
        }



        private bool _PointTypeChecked = false;
        public bool PointTypeChecked
        {
            get
            {
                return _PointTypeChecked;
            }
            set
            {
                _PointTypeChecked = value;
                OnPropertyChanged("PointTypeChecked");
            }
        }



        private bool _LinearTypeChecked = false;
        public bool LinearTypeChecked
        {
            get
            {
                return _LinearTypeChecked;
            }
            set
            {
                _LinearTypeChecked = value;
                if (value == true)
                {
                    //LinearLOCList.Clear();
                    //ViewModel_Unit_Map.SingletonInstance.LineListsClear();
                    //ViewModel_Unit_Map.SingletonInstance.ClearLinearPreview();
                }
                OnPropertyChanged("LinearTypeChecked");
            }
        }



        private bool _PolygonTypeChecked = false;
        public bool PolygonTypeChecked
        {
            get
            {
                return _PolygonTypeChecked;
            }
            set
            {
                _PolygonTypeChecked = value;
                //if (value == true)
                //{
                //    PolygonLOCList.Clear();
                //    ViewModel_Unit_Map.SingletonInstance.PolygonListsClear();
                //}
                OnPropertyChanged("PolygonTypeChecked");
                ViewModel_Unit_Map.SingletonInstance.IsINITMissionPolygonEditMode = false; // 그리기 중엔 편집모드 끔
            }
        }

        private bool _EditEnable = false;
        public bool EditEnable
        {
            get
            {
                return _EditEnable;
            }
            set
            {
                _EditEnable = value;
                OnPropertyChanged("EditEnable");
            }
        }

        private bool _PolygonEditEnable = false;
        public bool PolygonEditEnable
        {
            get
            {
                return _PolygonEditEnable;
            }
            set
            {
                _PolygonEditEnable = value;
                OnPropertyChanged("PolygonEditEnable");
            }
        }

        private double _PointLat_Control = 0;
        public double PointLat_Control
        {
            get
            {
                return _PointLat_Control;
            }
            set
            {
                _PointLat_Control = value;
                OnPropertyChanged("PointLat_Control");
            }
        }

        private double _PointLon_Control = 0;
        public double PointLon_Control
        {
            get
            {
                return _PointLon_Control;
            }
            set
            {
                _PointLon_Control = value;
                OnPropertyChanged("PointLon_Control");
            }
        }

        private double _PointAlt_Control = 0;
        public double PointAlt_Control
        {
            get
            {
                return _PointAlt_Control;
            }
            set
            {
                _PointAlt_Control = value;
                OnPropertyChanged("PointAlt_Control");
            }
        }

        private float _LAHLowerLimitControl = 0;
        public float LAHLowerLimitControl
        {
            get
            {
                return _LAHLowerLimitControl;
            }
            set
            {
                _LAHLowerLimitControl = value;
                OnPropertyChanged("LAHLowerLimitControl");
            }
        }

        private float _LAHUpperLimitControl = 0;
        public float LAHUpperLimitControl
        {
            get
            {
                return _LAHUpperLimitControl;
            }
            set
            {
                _LAHUpperLimitControl = value;
                OnPropertyChanged("LAHUpperLimitControl");
            }
        }

        private float _UAVLowerLimitControl = 0;
        public float UAVLowerLimitControl
        {
            get
            {
                return _UAVLowerLimitControl;
            }
            set
            {
                UAVLowerLimitControl = value;
                OnPropertyChanged("UAVLowerLimitControl");
            }
        }

        private float _UAVUpperLimitControl = 0;
        public float UAVUpperLimitControl
        {
            get
            {
                return _UAVUpperLimitControl;
            }
            set
            {
                _UAVUpperLimitControl = value;
                OnPropertyChanged("UAVUpperLimitControl");
            }
        }

        private int _IsHoleIndex = 1;
        public int IsHoleIndex
        {
            get
            {
                return _IsHoleIndex;
            }
            set
            {
                _IsHoleIndex = value;
                OnPropertyChanged("IsHoleIndex");
            }
        }

        private int _WidthControl = 0;
        public int WidthControl
        {
            get
            {
                return _WidthControl;
            }
            set
            {
                _WidthControl = value;
                OnPropertyChanged("WidthControl");
            }
        }

        private int _SelectedinputMissionListIndex = -1;
        public int SelectedinputMissionListIndex
        {
            get
            {
                return _SelectedinputMissionListIndex;
            }
            set
            {
                _SelectedinputMissionListIndex = value;
                OnPropertyChanged("SelectedinputMissionListIndex");
            }
        }

        private int _SelectedPolygonIndex = -1;
        public int SelectedPolygonIndex
        {
            get
            {
                return _SelectedPolygonIndex;
            }
            set
            {
                _SelectedPolygonIndex = value;
                OnPropertyChanged("SelectedPolygonIndex");
            }
        }

        private int _IsDoneIndex = 1;
        public int IsDoneIndex
        {
            get
            {
                return _IsDoneIndex;
            }
            set
            {
                _IsDoneIndex = value;
                OnPropertyChanged("IsDoneIndex");
            }
        }

        private uint _ShapeTypeIndex = 1;
        public uint ShapeTypeIndex
        {
            get => _ShapeTypeIndex;
            set
            {
                // ★ 핵심: 값이 똑같으면 무시해서 핑퐁(무한루프) 방지
                if (_ShapeTypeIndex == value) return;

                _ShapeTypeIndex = value;

                // 1. 형상 타입에 따라 라디오 버튼 화면(Grid) 전환
                if (value == 1) IsPointSelected = true;
                else if (value == 2) IsLineSelected = true;
                else if (value == 3) IsAreaSelected = true;

                // 2. 형상이 바뀌면 기존 편집 모드를 강제 종료하고 UI 상태를 초기화
                if (PolygonState != MissionEditState.None)
                {
                    PolygonButton3CommandAction(null); // 취소 로직 호출
                }

                PolygonState = MissionEditState.None;
                UpdatePolygonControlState();
                OnPropertyChanged("ShapeTypeIndex");

                // 3. 상태가 싹 정리되었으니, 최종적으로 맵 패널을 갱신!
                UpdateMapInfoPanel();
            }
        }
        private InputMission _SelectedinputMissionItem = new InputMission();
        public InputMission SelectedinputMissionItem
        {
            get
            {
                return _SelectedinputMissionItem;
            }
            set
            {
                _SelectedinputMissionItem = value;
                if (value != null)
                {
                    try { System.IO.File.AppendAllText("debug_mission.txt", $"[DEBUG] SelectedItem Changed: ID={value.InputMissionID}, Seq={value.SequenceNumber}\n"); } catch { }
                    Name_Control = value.InputMissionID;
                    Order_Control = value.SequenceNumber;
                    IsDoneIndex = value.IsDone == false ? 1 : 0;
                    MissionTypeIndex = (int)value.InputMissionType;
                    RegionTypeIndex = value.RegionType;
                    //IsHoleIndex = value.MissionDetail.isHole == false ? 1 : 0;
                    //WidthControl = value.MissionDetail.Width;


                    //점은 그냥 모두 전시로 하고, 선택된 거는 위경고도만 보여주는 걸로
                    if (value.ShapeType == 1)
                    {
                        ShapeTypeIndex = 1;
                        //IsPointSelected = true;
                        if (value.Coordinate != null)
                        {
                            PointLat_Control = value.Coordinate.Latitude;
                            PointLon_Control = value.Coordinate.Longitude;
                            PointAlt_Control = value.Coordinate.Altitude;
                        }

                    }


                    else if (value.ShapeType == 2)
                    {
                        ShapeTypeIndex = 2;
                        if (value.PolyLine != null)
                        {
                            WidthControl = (int)value.PolyLine.Width;
                            IsLineSelected = true;
                            LinearLOCList.Clear();
                            if (value.PolyLine.CoordinateList != null)
                            {
                                foreach (var item in value.PolyLine.CoordinateList)
                                {
                                    var Input = new CoordinateInfo();
                                    Input.Latitude = item.Latitude;
                                    Input.Longitude = item.Longitude;
                                    Input.Altitude = item.Altitude;
                                    LinearLOCList.Add(Input);
                                }
                            }

                        }

                    }

                    //협업기저임무중 폴리곤 선택되면 보유 중인 폴리곤 개수만큼 맵에 전시
                    else if (value.ShapeType == 3)
                    {
                        ShapeTypeIndex = 3;
                        // ★ 선택 시 기존 폴리곤 데이터 로드 (Master-Detail용)
                        LoadExistingPolygons(value);

                        //AreaList.Clear();
                        //PolygonLOCList.Clear();
                        //if (value.Polygons != null)
                        //{
                        //    if (value.Polygons.AreaList != null)
                        //    {

                        //        foreach (var item in value.Polygons.AreaList)
                        //        {
                        //            IsHoleIndex = item.IsHole == false ? 1 : 0;
                        //            IsAreaSelected = true;
                        //            PolygonLOCList.Clear();
                        //            if (item.CoordinateList != null)
                        //            {
                        //                foreach (var LocItem in item.CoordinateList)
                        //                {
                        //                    var Input = new CoordinateInfo();
                        //                    Input.Latitude = LocItem.Latitude;
                        //                    Input.Longitude = LocItem.Longitude;
                        //                    Input.Altitude = LocItem.Altitude;
                        //                    PolygonLOCList.Add(Input);
                        //                }
                        //            }
                        //            AreaList.Add(item);
                        //        }
                        //    }
                        //}
                    }
                }
                OnPropertyChanged("SelectedinputMissionItem");
                UpdateMainControlState();
            }
        }

        private CoordinateInfo _SelectedPolygonItem = new CoordinateInfo();
        public CoordinateInfo SelectedPolygonItem
        {
            get
            {
                return _SelectedPolygonItem;
            }
            set
            {
                _SelectedPolygonItem = value;
                if (value != null)
                {
                    PolygonLatControl = value.Latitude;
                    PolygonLonControl = value.Longitude;
                    PolygonAltControl = value.Altitude;

                }
                OnPropertyChanged("SelectedINITMissionItem");
            }
        }

        private AreaInfo _SelectedPolygon = new AreaInfo();
        public AreaInfo SelectedPolygon
        {
            get
            {
                return _SelectedPolygon;
            }
            set
            {
                _SelectedPolygon = value;
                if (value != null)
                {
                    PolygonLOCList.Clear();
                    IsHoleIndex = value.IsHole == false ? 1 : 0;
                    if (AreaList[SelectedPolygonIndex].CoordinateList != null)
                    {
                        foreach (var item in AreaList[SelectedPolygonIndex].CoordinateList)
                        {
                            var InputItem = new CoordinateInfo();
                            InputItem.Latitude = item.Latitude;
                            InputItem.Longitude = item.Longitude;
                            InputItem.Altitude = item.Altitude;
                            PolygonLOCList.Add(InputItem);
                        }
                    }
                }
                OnPropertyChanged("SelectedPolygon");
            }
        }

        private CoordinateInfo _SelectedLineItem = new CoordinateInfo();
        public CoordinateInfo SelectedLineItem
        {
            get
            {
                return _SelectedLineItem;
            }
            set
            {
                _SelectedLineItem = value;
                if (value != null)
                {
                    LineLatControl = value.Latitude;
                    LineLonControl = value.Longitude;
                    LineAltControl = value.Altitude;

                }
                OnPropertyChanged("SelectedLineItem");
            }
        }

        private ObservableCollection<InputMission> _inputMissionList = new ObservableCollection<InputMission>();
        public ObservableCollection<InputMission> inputMissionList
        {
            get
            {
                return _inputMissionList;
            }
            set
            {
                _inputMissionList = value;
                OnPropertyChanged("inputMissionList");
            }
        }

        //private ObservableCollection<CoordinateList> _MissionDetailPointList = new ObservableCollection<CoordinateList>();
        //public ObservableCollection<CoordinateList> MissionDetailPointList
        //{
        //    get
        //    {
        //        return _MissionDetailPointList;
        //    }
        //    set
        //    {
        //        _MissionDetailPointList = value;
        //        OnPropertyChanged("MissionDetailPointList");
        //    }
        //}

        private ObservableCollection<CoordinateInfo> _LinearLOCList = new ObservableCollection<CoordinateInfo>();
        public ObservableCollection<CoordinateInfo> LinearLOCList
        {
            get
            {
                return _LinearLOCList;
            }
            set
            {
                _LinearLOCList = value;
                OnPropertyChanged("LinearLOCList");
            }
        }

        ////최종 다각형의 꼭짓점을 저장할 리스트
        //public List<GeoPoint> FinalCorridorVertices { get; set; } = new();

        // 최종 '사각형 목록'을 저장할 속성
        public List<List<GeoPoint>> FinalSegmentRectangles { get; set; }

        private ObservableCollection<AreaInfo> _AreaList = new ObservableCollection<AreaInfo>();
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


        private ObservableCollection<CoordinateInfo> _PolygonLOCList = new ObservableCollection<CoordinateInfo>();
        public ObservableCollection<CoordinateInfo> PolygonLOCList
        {
            get
            {
                return _PolygonLOCList;
            }
            set
            {
                _PolygonLOCList = value;
                OnPropertyChanged("PolygonLOCList");
            }
        }

        private void Callback_OnINITMissionPointSet(double lat, double lon)
        {
            PointLat_Control = lat;
            PointLon_Control = lon;

            var srtmReader = ViewModel_Unit_Map.SingletonInstance.SrtmReaderInstance;
            if (srtmReader != null)
            {
                short elevation = srtmReader.GetElevation(lat, lon);
                PointAlt_Control = (elevation == -32768) ? 0 : elevation;
            }
            else
            {
                PointAlt_Control = 0;
            }

            PointTypeChecked = false;
        }

        //취소 버튼용 미리 생성
        /// 선/사각형 미리보기만 싹 지움(상태는 그대로)
        //private void ClearLinearPreview()
        //{
        //    ViewModel_Unit_Map.SingletonInstance.TempINITMissionLineList.Clear();
        //    ViewModel_Unit_Map.SingletonInstance.TempINITMissionPolygonList.Clear();
        //    _previewSegments.Clear();
        //    _previewRects.Clear();
        //    _ghostSegment = null;
        //}

        // [UI 전용] 초기임무 다각형(AreaInfo)을 감싸는 포장지
        public class InitMissionAreaWrapper : CommonBase
        {
            public InitMissionAreaWrapper(AreaInfo model, int uiId)
            {
                this.Model = model;
                this.UI_ID = uiId;
            }

            // 1. 지도 연동용 고유 ID
            public int UI_ID { get; }

            // 2. 실제 데이터 모델
            public AreaInfo Model { get; }

            // 3. 구멍(Hole) 여부 (Grid에서 수정 가능하도록 바인딩)
            public bool IsHole
            {
                get => Model.IsHole;
                set
                {
                    Model.IsHole = value;
                    OnPropertyChanged("IsHole");
                }
            }

            // 4. GridControl Detail(상세) 행 바인딩용 속성
            public ObservableCollection<CoordinateInfo> Points => Model.CoordinateList;
        }

        // [3] ID 카운터 & 컬렉션
        private int _areaIdCounter = 0;

        // Master-Detail용 Wrapper 리스트
        private ObservableCollection<InitMissionAreaWrapper> _AreaWrapperList = new ObservableCollection<InitMissionAreaWrapper>();
        public ObservableCollection<InitMissionAreaWrapper> AreaWrapperList
        {
            get => _AreaWrapperList;
            set { _AreaWrapperList = value; OnPropertyChanged("AreaWrapperList"); }
        }

        // 선택된 다각형 (Wrapper)
        private InitMissionAreaWrapper _SelectedAreaWrapper;
        public InitMissionAreaWrapper SelectedAreaWrapper
        {
            get => _SelectedAreaWrapper;
            set
            {
                _SelectedAreaWrapper = value;
                OnPropertyChanged("SelectedAreaWrapper");
                UpdatePolygonControlState();
            }
        }

        // [4] 임시 데이터 (생성 중일 때 사용)
        // 기존 AreaList 대신, 생성 중인 점들을 임시 보관할 리스트
        private ObservableCollection<CoordinateInfo> _TempPolygonPoints = new ObservableCollection<CoordinateInfo>();
        public ObservableCollection<CoordinateInfo> TempPolygonPoints
        {
            get => _TempPolygonPoints;
            set { _TempPolygonPoints = value; OnPropertyChanged("TempPolygonPoints"); }
        }

        // [5] 버튼 텍스트 & 활성 상태

        private bool _PolygonButton1Enable = true;
        public bool PolygonButton1Enable { get => _PolygonButton1Enable; set { _PolygonButton1Enable = value; OnPropertyChanged("PolygonButton1Enable"); } }

        // [백업용]
        private List<CoordinateInfo> _backupPoints = new List<CoordinateInfo>();

        // 지도 그리기 토글
        //private bool _PolygonMapChecked = false;
        //public bool PolygonMapChecked
        //{
        //    get => _PolygonMapChecked;
        //    set
        //    {
        //        _PolygonMapChecked = value;
        //        OnPropertyChanged("PolygonMapChecked");
        //        // 지도 그리기 모드 연동은 View_Unit_Map에서 이 프로퍼티를 감시하거나, 
        //        // 여기서 MapVM 상태를 변경해도 됩니다. (기존 방식 유지)
        //        ViewModel_Unit_Map.SingletonInstance.IsINITMissionPolygonEditMode = false; // 그리기 중엔 편집모드 끔
        //    }
        //}

        private bool _IsShapeTypeEditable = false;
        public bool IsShapeTypeEditable
        {
            get { return _IsShapeTypeEditable; }
            set
            {
                _IsShapeTypeEditable = value;
                OnPropertyChanged("IsShapeTypeEditable");
            }
        }
    }

}
