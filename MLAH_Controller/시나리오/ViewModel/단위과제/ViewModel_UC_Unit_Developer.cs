
using DevExpress.CodeParser.Diagnostics;
using DevExpress.Map;
using DevExpress.Office.Utils;
using DevExpress.Pdf.ContentGeneration;
using DevExpress.Xpf.Map;
using DevExpress.XtraSpreadsheet.DocumentFormats.Xlsb;
using Google.Protobuf.WellKnownTypes;
using MLAH_Controller;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
//using GMap.NET;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static DevExpress.Charts.Designer.Native.BarThicknessEditViewModel;


namespace MLAH_Controller
{
    public class ViewModel_UC_Unit_Developer : CommonBase
    {
        #region Singleton
        static ViewModel_UC_Unit_Developer _ViewModel_UC_Unit_Developer = null;
        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static ViewModel_UC_Unit_Developer SingletonInstance
        {
            get
            {
                if (_ViewModel_UC_Unit_Developer == null)
                {
                    _ViewModel_UC_Unit_Developer = new ViewModel_UC_Unit_Developer();
                }
                return _ViewModel_UC_Unit_Developer;
            }
        }

        #endregion Singleton

        #region 생성자 & 콜백
        public ViewModel_UC_Unit_Developer()
        {
            CommonEvent.OnDevelopPathPlanSet += Callback_OnDevelopPathPlanSet;
            Button1Command = new RelayCommand(Button1CommandAction);
            Button2Command = new RelayCommand(_ => { }); // Developer 뷰에서는 미사용
            Button3Command = new RelayCommand(Button3CommandAction);
            DetailSaveCommand = new RelayCommand(DetailSaveCommandAction);
            TotalDetailSaveCommand = new RelayCommand(TotalDetailSaveCommandAction);
            SendCommand = new RelayCommand(SendCommandAction);
            SendUnrealCommand = new RelayCommand(SendUnrealCommandAction);
            TextTestCommand = new RelayCommand(TextTestCommandAction);
        }

        #endregion 생성자 & 콜백

        private enum MenuButtonState { None, Creating, Editing}
        private MenuButtonState _state = MenuButtonState.None;

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

        private short _ID_Control;
        public short ID_Control
        {
            get
            {
                return _ID_Control;
            }
            set
            {
                _ID_Control = value;
                OnPropertyChanged("ID_Control");
            }
        }

        private string _Name_Control;
        public string Name_Control
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

        private bool _PathEnable = false;
        public bool PathEnable
        {
            get
            {
                return _PathEnable;
            }
            set
            {
                _PathEnable = value;
                OnPropertyChanged("PathEnable");
            }
        }


        private bool _PathCheckEnable = false;
        public bool PathCheckEnable
        {
            get
            {
                return _PathCheckEnable;
            }
            set
            {
                _PathCheckEnable = value;
                OnPropertyChanged("PathCheckEnable");
            }
        }

        private uint _LOCInfo_ID = 0;
        public uint LOCInfo_ID
        {
            get
            {
                return _LOCInfo_ID;
            }
            set
            {
                _LOCInfo_ID = value;
                OnPropertyChanged("LOCInfo_ID");
            }
        }

        private double _LOCInfo_LAT = 0;
        public double LOCInfo_LAT
        {
            get
            {
                return _LOCInfo_LAT;
            }
            set
            {
                _LOCInfo_LAT = value;
                OnPropertyChanged("LOCInfo_LAT");
            }
        }

        private double _LOCInfo_LON = 0;
        public double LOCInfo_LON
        {
            get
            {
                return _LOCInfo_LON;
            }
            set
            {
                _LOCInfo_LON = value;
                OnPropertyChanged("LOCInfo_LON");
            }
        }

        private double _LOCInfo_ALT = 0;
        public double LOCInfo_ALT
        {
            get
            {
                return _LOCInfo_ALT;
            }
            set
            {
                _LOCInfo_ALT = value;
                OnPropertyChanged("LOCInfo_ALT");
            }
        }

        private double _LOCInfo_Speed = 0;
        public double LOCInfo_Speed
        {
            get
            {
                return _LOCInfo_Speed;
            }
            set
            {
                _LOCInfo_Speed = value;
                OnPropertyChanged("LOCInfo_Speed");
            }
        }

        private bool _DevelopPathChecked = false;
        public bool DevelopPathChecked
        {
            get
            {
                return _DevelopPathChecked;
            }
            set
            {
                _DevelopPathChecked = value;
                if(value == true)
                {
                    MovePlans.Clear();
                    ViewModel_Unit_Map.SingletonInstance.TempPathPlanClear();
                }
                OnPropertyChanged("DevelopPathChecked");
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
                OnPropertyChanged("MissionTypeIndex");
            }
        }

        private int _PassTypeIndex = 0;
        public int PassTypeIndex
        {
            get
            {
                return _PassTypeIndex;
            }
            set
            {
                _PassTypeIndex = value;
                OnPropertyChanged("PassTypeIndex");
            }
        }

        private int _WaitTypeIndex = 1;
        public int WaitTypeIndex
        {
            get
            {
                return _WaitTypeIndex;
            }
            set
            {
                _WaitTypeIndex = value;
                OnPropertyChanged("WaitTypeIndex");
            }
        }

        private int _TargetID = 0;
        public int TargetID
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

        private int _AttackCount = 2;
        public int AttackCount
        {
            get
            {
                return _AttackCount;
            }
            set
            {
                _AttackCount = value;
                OnPropertyChanged("AttackCount");
            }
        }


        private int _PathPlanListIndex = -1;
        public int PathPlanListIndex
        {
            get
            {
                return _PathPlanListIndex;
            }
            set
            {
                _PathPlanListIndex = value;
                OnPropertyChanged("PathPlanListIndex");
            }
        }

        private int _MovePlanListIndex = -1;
        public int MovePlanListIndex
        {
            get
            {
                return _MovePlanListIndex;
            }
            set
            {
                _MovePlanListIndex = value;
                OnPropertyChanged("MovePlanListIndex");
            }
        }

        #region 카메라 액션

        private int _CameraModeIndex = 4;
        public int CameraModeIndex
        {
            get
            {
                return _CameraModeIndex;
            }
            set
            {
                _CameraModeIndex = value;
                switch(value)
                {
                    case 0:
                        {
                            FixedPointVisible = Visibility.Visible;
                            SweepVisible = Visibility.Hidden;
                            FixedUAVVisible = Visibility.Hidden;
                            ChasingTargetVisible = Visibility.Hidden;
                        }
                        break;
                    case 1:
                        {
                            FixedPointVisible = Visibility.Hidden;
                            SweepVisible = Visibility.Visible;
                            FixedUAVVisible = Visibility.Hidden;
                            ChasingTargetVisible = Visibility.Hidden;
                        }
                        break;
                    case 2:
                        {
                            FixedPointVisible = Visibility.Hidden;
                            SweepVisible = Visibility.Hidden;
                            FixedUAVVisible = Visibility.Visible;
                            ChasingTargetVisible = Visibility.Hidden;
                        }
                        break;
                    case 3:
                        {
                            FixedPointVisible = Visibility.Hidden;
                            SweepVisible = Visibility.Hidden;
                            FixedUAVVisible = Visibility.Hidden;
                            ChasingTargetVisible = Visibility.Visible;
                        }
                        break;
                    case 4:
                        {
                            FixedPointVisible = Visibility.Hidden;
                            SweepVisible = Visibility.Hidden;
                            FixedUAVVisible = Visibility.Hidden;
                            ChasingTargetVisible = Visibility.Hidden;
                        }
                        break;
                    default:
                        break;
                }
                OnPropertyChanged("CameraModeIndex");
            }
        }

        private Visibility _FixedPointVisible = Visibility.Hidden;
        public Visibility FixedPointVisible
        {
            get
            {
                return _FixedPointVisible;
            }
            set
            {
                _FixedPointVisible = value;
                OnPropertyChanged("FixedPointVisible");
            }
        }

        private Visibility _SweepVisible = Visibility.Hidden;
        public Visibility SweepVisible
        {
            get
            {
                return _SweepVisible;
            }
            set
            {
                _SweepVisible = value;
                OnPropertyChanged("SweepVisible");
            }
        }

        private Visibility _FixedUAVVisible = Visibility.Visible;
        public Visibility FixedUAVVisible
        {
            get
            {
                return _FixedUAVVisible;
            }
            set
            {
                _FixedUAVVisible = value;
                OnPropertyChanged("FixedUAVVisible");
            }
        }

        private Visibility _ChasingTargetVisible = Visibility.Hidden;
        public Visibility ChasingTargetVisible
        {
            get
            {
                return _ChasingTargetVisible;
            }
            set
            {
                _ChasingTargetVisible = value;
                OnPropertyChanged("ChasingTargetVisible");
            }
        }

        private double _CameraFOV = 0;
        public double CameraFOV
        {
            get
            {
                return _CameraFOV;
            }
            set
            {
                _CameraFOV = value;
                OnPropertyChanged("CameraFOV");
            }
        }

        private double _FixedPointLat = 0;
        public double FixedPointLat
        {
            get
            {
                return _FixedPointLat;
            }
            set
            {
                _FixedPointLat = value;
                OnPropertyChanged("FixedPointLat");
            }
        }

        private double _FixedPointLon = 0;
        public double FixedPointLon
        {
            get
            {
                return _FixedPointLon;
            }
            set
            {
                _FixedPointLon = value;
                OnPropertyChanged("FixedPointLon");
            }
        }

        private double _SweepPoint1Lat = 0;
        public double SweepPoint1Lat
        {
            get
            {
                return _SweepPoint1Lat;
            }
            set
            {
                _SweepPoint1Lat = value;
                OnPropertyChanged("SweepPoint1Lat");
            }
        }

        private double _SweepPoint1Lon = 0;
        public double SweepPoint1Lon
        {
            get
            {
                return _SweepPoint1Lon;
            }
            set
            {
                _SweepPoint1Lon = value;
                OnPropertyChanged("SweepPoint1Lon");
            }
        }

        private double _SweepPoint2Lat = 0;
        public double SweepPoint2Lat
        {
            get
            {
                return _SweepPoint2Lat;
            }
            set
            {
                _SweepPoint2Lat = value;
                OnPropertyChanged("SweepPoint2Lat");
            }
        }

        private double _SweepPoint2Lon = 0;
        public double SweepPoint2Lon
        {
            get
            {
                return _SweepPoint2Lon;
            }
            set
            {
                _SweepPoint2Lon = value;
                OnPropertyChanged("SweepPoint2Lon");
            }
        }

        private double _SweepTime = 0;
        public double SweepTime
        {
            get
            {
                return _SweepTime;
            }
            set
            {
                _SweepTime = value;
                OnPropertyChanged("SweepTime");
            }
        }

        private double _FixedUAVRoll = 0;
        public double FixedUAVRoll
        {
            get
            {
                return _FixedUAVRoll;
            }
            set
            {
                _FixedUAVRoll = value;
                OnPropertyChanged("FixedUAVRoll");
            }
        }

        private double _FixedUAVPitch = 0;
        public double FixedUAVPitch
        {
            get
            {
                return _FixedUAVPitch;
            }
            set
            {
                _FixedUAVPitch = value;
                OnPropertyChanged("FixedUAVPitch");
            }
        }

        private double _FixedUAVYaw = 0;
        public double FixedUAVYaw
        {
            get
            {
                return _FixedUAVYaw;
            }
            set
            {
                _FixedUAVYaw = value;
                OnPropertyChanged("FixedUAVYaw");
            }
        }

        private double _ChasingTargetNum = 0;
        public double ChasingTargetNum
        {
            get
            {
                return _ChasingTargetNum;
            }
            set
            {
                _ChasingTargetNum = value;
                OnPropertyChanged("ChasingTargetNum");
            }
        }






        #endregion 카메라 액션


        private Unit_LAH_MovePlans _SelectedMovePlanListItem = new Unit_LAH_MovePlans();
        public Unit_LAH_MovePlans SelectedMovePlanListItem
        {
            get
            {
                return _SelectedMovePlanListItem;
            }
            set
            {
                _SelectedMovePlanListItem = value;
                if (value != null)
                {
                    if(_state == MenuButtonState.None)
                    {
                        Button3Enable = true;
                    }
                    MovePlans.Clear();
                    foreach (var item in value.unit_LAH_MovePlans)
                    {
                        LOCInfo_ID = value.UnitID;

                        var Lat = item.LAT;
                        var Lon = item.LON;
                        var Alt = item.ALT;

                        var item2 = new Unit_LAH_MovePlan();
                        item2.LAT = Lat;
                        item2.LON = Lon;
                        item2.ALT = Alt;

                        item2.Speed = item.Speed;
                        item2.Mission = item.Mission;
                        item2.PassType = item.PassType;
                        item2.WaitTime = item.WaitTime;
                        item2.TargetID = item.TargetID;
                        item2.AttackCount = item.AttackCount;

                        item2.CameraFOV = item.CameraFOV;
                        switch (item.CameraMode)
                        {
                            case 0:
                                {
                                    item2.CameraMode = item.CameraMode;
                                    item2.FixedPointLat = item.FixedPointLat;
                                    item2.FixedPointLon = item.FixedPointLon;
                                }
                                break;
                            case 1:
                                {
                                    item2.CameraMode = item.CameraMode;
                                    item2.SweepPoint1Lat = item.SweepPoint1Lat;
                                    item2.SweepPoint1Lon = item.SweepPoint1Lon;
                                    item2.SweepPoint2Lat = item.SweepPoint2Lat;
                                    item2.SweepPoint2Lon = item.SweepPoint2Lon;
                                    item2.SweepTime = item.SweepTime;
                                }
                                break;
                            case 2:
                                {
                                    item2.CameraMode = item.CameraMode;
                                   item2.FixedUAVRoll = item.FixedUAVRoll;
                                   item2.FixedUAVPitch = item.FixedUAVPitch;
                                    item2.FixedUAVYaw = item.FixedUAVYaw;
                                }
                                break;
                            case 3:
                                {
                                    item2.CameraMode = item.CameraMode;
                                    item2.ChasingTargetNum = item.ChasingTargetNum;
                                }
                                break;
                            case -1:
                                {
                                    item2.CameraMode = item.CameraMode;
                                }
                                break;

                        }

                        //item2.Mission
                        MovePlans.Add(item2);
                    }
                    var MapDrawList = new List<CoordPoint>();
                    foreach (var inputInnterItem in MovePlans)
                    {
                        var InputItem = new Unit_LAH_MovePlan();
                        InputItem.LAT = inputInnterItem.LAT;
                        InputItem.LON = inputInnterItem.LON;
                        InputItem.ALT = inputInnterItem.ALT;

                        GeoPoint Point = new GeoPoint(InputItem.LAT, InputItem.LON);
                        //InputPathPlan.unit_LAH_MovePlans.Add(InputItem);
                        MapDrawList.Add(Point);
                    }
                    if (MapDrawList.Count > 2)
                    {
                        ViewModel_Unit_Map.SingletonInstance.UnitDevelopPathPlanList.Clear();

                        // StrokeStyle 인스턴스 생성 및 속성 설정
                        var myStrokeStyle = new StrokeStyle()
                        {
                            Thickness = 5, // 선 두께를 2로 설정
                            StartLineCap = PenLineCap.Round, // 선 시작 부분을 라운드 처리
                            EndLineCap = PenLineCap.Round,   // 선 끝 부분을 라운드 처리
                            LineJoin = PenLineJoin.Round,     // 선 연결부도 라운드 처리 (필요한 경우)
                                                              //DashArray = DoubleCollection.
                            DashCap = PenLineCap.Round,
                            DashOffset = 5,
                        };

                        var inputPoints = new MapPolyline
                        {
                            Stroke = Brushes.IndianRed,
                            Fill = Brushes.Transparent,
                            StrokeStyle = myStrokeStyle,

                        };

                        //var inputPoints = new MapPolyline();
                        foreach (var input in MapDrawList)
                        {
                            var inputPoint = new GeoPoint(input.GetY(), input.GetX());
                            inputPoints.Points.Add(inputPoint);
                        }
                        ViewModel_Unit_Map.SingletonInstance.UnitDevelopPathPlanList.Add(inputPoints);
                        ViewModel_Unit_Map.SingletonInstance.TempUnitDevelopPathPlanList.Clear();
                    }

                }
                OnPropertyChanged("SelectedMovePlanListItem");
            }
        }

        private Unit_LAH_MovePlan _SelectedPathPlan = new Unit_LAH_MovePlan();
        public Unit_LAH_MovePlan SelectedPathPlan
        {
            get
            {
                return _SelectedPathPlan;
            }
            set
            {
                _SelectedPathPlan = value;
                if(value !=null)
                {
                    //LOCInfo_ID = (uint)value.LAHID;
                    LOCInfo_LAT = value.LAT;
                    LOCInfo_LON = value.LON;
                    LOCInfo_ALT = value.ALT;
                    LOCInfo_Speed = value.Speed;
                    MissionTypeIndex = value.Mission;
                    PassTypeIndex = value.PassType;
                    WaitTypeIndex = value.WaitTime;
                    TargetID = value.TargetID;
                    AttackCount = value.AttackCount;
                    LOCInfo_Speed = value.Speed;

                    CameraFOV = value.CameraFOV;
                    switch (value.CameraMode)
                    {
                        case 0:
                            {
                                CameraModeIndex = value.CameraMode;
                                FixedPointLat = value.FixedPointLat;
                                FixedPointLon = value.FixedPointLon;
                            }
                            break;
                        case 1:
                            {
                                CameraModeIndex = value.CameraMode;
                                SweepPoint1Lat = value.SweepPoint1Lat;
                                SweepPoint1Lon = value.SweepPoint1Lon;
                                SweepPoint2Lat = value.SweepPoint2Lat;
                                SweepPoint2Lon = value.SweepPoint2Lon;
                                SweepTime = value.SweepTime;
                            }
                            break;
                        case 2:
                            {
                                CameraModeIndex = value.CameraMode;
                                FixedUAVRoll = value.FixedUAVRoll;
                                FixedUAVPitch = value.FixedUAVPitch;
                                FixedUAVYaw = value.FixedUAVYaw;
                            }
                            break;
                        case 3:
                            {
                                CameraModeIndex = value.CameraMode;
                                ChasingTargetNum = value.ChasingTargetNum;
                            }
                            break;
                        case -1:
                            {
                                CameraModeIndex = 4;
                            }
                            break;

                    }

                }
                OnPropertyChanged("SelectedPathPlan");
            }
        }

        private ObservableCollection<Unit_LAH_MovePlans> _MovePlanList = new ObservableCollection<Unit_LAH_MovePlans>();
        public ObservableCollection<Unit_LAH_MovePlans> MovePlanList
        {
            get
            {
                return _MovePlanList;
            }
            set
            {
                _MovePlanList = value;
                OnPropertyChanged("MovePlanList");
            }
        }

        private ObservableCollection<Unit_LAH_MovePlan> _MovePlans = new ObservableCollection<Unit_LAH_MovePlan>();
        public ObservableCollection<Unit_LAH_MovePlan> MovePlans
        {
            get
            {
                return _MovePlans;
            }
            set
            {
                _MovePlans = value;
                OnPropertyChanged("MovePlans");
            }
        }





        private void Callback_OnDevelopPathPlanSet(List<CoordPoint> PathPlanList)
        {
            foreach (var loc in PathPlanList) 
            {
                var Lat = loc.GetY();
                var Lon = loc.GetX();
                var item = new Unit_LAH_MovePlan();

                item.LAT = Lat;
                item.LON = Lon;
                item.ALT = 0;
                MovePlans.Add(item);
            }
            //마지막에 추가한 이동경로 구역 선택
            //var TempIndex = PathPlanList.Count();
            //PathPlanListIndex = TempIndex - 1;
        }

        private bool _EditEnable = false;
        public bool EditEnable
        {
            get { return _EditEnable; }
            set { _EditEnable = value; OnPropertyChanged("EditEnable"); }
        }

        public RelayCommand Button1Command { get; set; }
        public RelayCommand Button2Command { get; set; }

        public void Button1CommandAction(object param)
        {
            if (_state == MenuButtonState.None)
            {
                PathEnable = true;
                PathCheckEnable = true;
                Button2Enable = false;
                Button3Enable = true;
                Button1Text = "저장";
                Button3Text = "취소";
                _state = MenuButtonState.Creating;
            }
            else if(_state == MenuButtonState.Creating)
            {
                PathEnable = false;
                PathCheckEnable = false;
                Button2Enable = false;
                Button3Enable = false;
                Button1Text = "생성";
                Button3Text = "삭제";
                _state = MenuButtonState.None;

                var InputPathPlan = new Unit_LAH_MovePlans();
                var MapDrawList = new List<CoordPoint>();
                foreach (var inputInnterItem in MovePlans)
                {
                    var InputItem = new Unit_LAH_MovePlan();

                    InputItem.TargetID = inputInnterItem.TargetID;

                    InputItem.LAT = inputInnterItem.LAT;
                    InputItem.LON = inputInnterItem.LON;
                    InputItem.ALT = inputInnterItem.ALT;
                    InputItem.Mission = inputInnterItem.Mission;
                    InputItem.PassType = inputInnterItem.PassType;
                    InputItem.WaitTime = inputInnterItem.WaitTime;
                    
                    InputItem.AttackCount = inputInnterItem.AttackCount;
                    InputItem.Speed = inputInnterItem.Speed;

                    //InputItem.CameraMode = inputInnterItem.CameraMode;
                    InputItem.CameraFOV = inputInnterItem.CameraFOV;
                    switch (inputInnterItem.CameraMode)
                    {
                        case 0:
                            {
                                InputItem.CameraMode = inputInnterItem.CameraMode;
                                InputItem.FixedPointLat = inputInnterItem.FixedPointLat;
                                InputItem.FixedPointLon = inputInnterItem.FixedPointLon;
                            }
                            break;
                        case 1:
                            {
                                InputItem.CameraMode = inputInnterItem.CameraMode;
                                InputItem.SweepPoint1Lat = inputInnterItem.SweepPoint1Lat;
                                InputItem.SweepPoint1Lon = inputInnterItem.SweepPoint1Lon;
                                InputItem.SweepPoint2Lat = inputInnterItem.SweepPoint2Lat;
                                InputItem.SweepPoint2Lon = inputInnterItem.SweepPoint2Lon;
                                InputItem.SweepTime = inputInnterItem.SweepTime;
                            }
                            break;
                        case 2:
                            {
                                InputItem.CameraMode = inputInnterItem.CameraMode;
                                InputItem.FixedUAVRoll = inputInnterItem.FixedUAVRoll;
                                InputItem.FixedUAVPitch = inputInnterItem.FixedUAVPitch;
                                InputItem.FixedUAVYaw = inputInnterItem.FixedUAVYaw;
                            }
                            break;
                        case 3:
                            {
                                InputItem.CameraMode = inputInnterItem.CameraMode;
                                InputItem.ChasingTargetNum = inputInnterItem.ChasingTargetNum;
                            }
                            break;
                        case -1:
                            {
                                InputItem.CameraMode = inputInnterItem.CameraMode;
                            }
                            break;

                    }

                    GeoPoint Point = new GeoPoint(InputItem.LAT, InputItem.LON);
                    InputPathPlan.unit_LAH_MovePlans.Add(InputItem);
                    MapDrawList.Add(Point);
                }
                InputPathPlan.UnitID = LOCInfo_ID;
                ViewModel_ScenarioView.SingletonInstance.model_Unit_Develop.unit_LAH_MovePlansList.Add(InputPathPlan);
                MovePlanList.Add(InputPathPlan);


                //foreach (var input in value.WaypointList)
                //{
                //    WayPointUAVItemSource.Add(input);
                //}
                if (MapDrawList.Count > 2)
                {
                    ViewModel_Unit_Map.SingletonInstance.UnitDevelopPathPlanList.Clear();
                    //var inputPoints = new MapPolyline();

                    // StrokeStyle 인스턴스 생성 및 속성 설정
                    var myStrokeStyle = new StrokeStyle()
                    {
                        Thickness = 5, // 선 두께를 2로 설정
                        StartLineCap = PenLineCap.Round, // 선 시작 부분을 라운드 처리
                        EndLineCap = PenLineCap.Round,   // 선 끝 부분을 라운드 처리
                        LineJoin = PenLineJoin.Round,     // 선 연결부도 라운드 처리 (필요한 경우)
                        //DashArray = DoubleCollection.
                        DashCap = PenLineCap.Round,
                        DashOffset = 5,                  
                    };

                    var inputPoints = new MapPolyline
                    {
                        Stroke = Brushes.IndianRed,
                        Fill = Brushes.Transparent,
                        StrokeStyle = myStrokeStyle,

                    };

                    foreach (var input in MapDrawList)
                    {
                        var inputPoint = new GeoPoint(input.GetY(), input.GetX());
                        inputPoints.Points.Add(inputPoint);
                    }
                    ViewModel_Unit_Map.SingletonInstance.UnitDevelopPathPlanList.Add(inputPoints);
                    ViewModel_Unit_Map.SingletonInstance.TempUnitDevelopPathPlanList.Clear();
                    ViewModel_Unit_Map.SingletonInstance.TempUnitDevelopPathPlanList.Clear();
                }
                //MovePlans.Clear();
                

                //임시 이동경로 지우기
                //0은 나중에 순번체크
                //CommonEvent.OnDevelopPathPlanAdd?.Invoke(0, MapDrawList);


                //마지막에 추가한 이동경로 구역 선택
                //var TempIndex = PathPlanList.Count();
                //PathPlanListIndex = TempIndex - 1;
            }
        }

        //삭제
        public RelayCommand Button3Command { get; set; }

        public void Button3CommandAction(object param)
        {
            //삭제
            if (_state == MenuButtonState.None)
            {
                //PathEnable = true;
                //PathCheckEnable = true;
                //Button2Enable = false;
                //Button3Enable = true;
                //Button1Text = "저장";
                //Button3Text = "취소";
                //_state = MenuButtonState.Creating;
                ViewModel_ScenarioView.SingletonInstance.model_Unit_Develop.unit_LAH_MovePlansList.Remove(SelectedMovePlanListItem);
                MovePlanList.Remove(SelectedMovePlanListItem);
                MovePlans.Clear();
                ViewModel_Unit_Map.SingletonInstance.TempUnitDevelopPathPlanList.Clear();
            }

            //취소
            //else if (_state == MenuButtonState.Creating)
            //{
            //    PathEnable = false;
            //    PathCheckEnable = false;
            //    //Button2Enable = false;
            //    Button3Enable = false;
            //    Button1Text = "생성";
            //    Button3Text = "삭제";
            //    _state = MenuButtonState.None;

            //    var InputPathPlan = new Unit_LAH_MovePlans();
            //    var MapDrawList = new List<CoordPoint>();
            //    foreach (var inputInnterItem in MovePlans)
            //    {
            //        var InputItem = new Unit_LAH_MovePlan();

            //        InputItem.TargetID = inputInnterItem.TargetID;

            //        InputItem.LAT = inputInnterItem.LAT;
            //        InputItem.LON = inputInnterItem.LON;
            //        InputItem.ALT = inputInnterItem.ALT;
            //        InputItem.Mission = inputInnterItem.Mission;
            //        InputItem.PassType = inputInnterItem.PassType;
            //        InputItem.WaitTime = inputInnterItem.WaitTime;

            //        InputItem.AttackCount = inputInnterItem.AttackCount;
            //        InputItem.Speed = inputInnterItem.Speed;

            //        //InputItem.CameraMode = inputInnterItem.CameraMode;
            //        InputItem.CameraFOV = inputInnterItem.CameraFOV;
            //        switch (inputInnterItem.CameraMode)
            //        {
            //            case 0:
            //                {
            //                    InputItem.CameraMode = inputInnterItem.CameraMode;
            //                    InputItem.FixedPointLat = inputInnterItem.FixedPointLat;
            //                    InputItem.FixedPointLon = inputInnterItem.FixedPointLon;
            //                }
            //                break;
            //            case 1:
            //                {
            //                    InputItem.CameraMode = inputInnterItem.CameraMode;
            //                    InputItem.SweepPoint1Lat = inputInnterItem.SweepPoint1Lat;
            //                    InputItem.SweepPoint1Lon = inputInnterItem.SweepPoint1Lon;
            //                    InputItem.SweepPoint2Lat = inputInnterItem.SweepPoint2Lat;
            //                    InputItem.SweepPoint2Lon = inputInnterItem.SweepPoint2Lon;
            //                    InputItem.SweepTime = inputInnterItem.SweepTime;
            //                }
            //                break;
            //            case 2:
            //                {
            //                    InputItem.CameraMode = inputInnterItem.CameraMode;
            //                    InputItem.FixedUAVRoll = inputInnterItem.FixedUAVRoll;
            //                    InputItem.FixedUAVPitch = inputInnterItem.FixedUAVPitch;
            //                    InputItem.FixedUAVYaw = inputInnterItem.FixedUAVYaw;
            //                }
            //                break;
            //            case 3:
            //                {
            //                    InputItem.CameraMode = inputInnterItem.CameraMode;
            //                    InputItem.ChasingTargetNum = inputInnterItem.ChasingTargetNum;
            //                }
            //                break;
            //            case -1:
            //                {
            //                    InputItem.CameraMode = inputInnterItem.CameraMode;
            //                }
            //                break;

            //        }

            //        GeoPoint Point = new GeoPoint(InputItem.LAT, InputItem.LON);
            //        InputPathPlan.unit_LAH_MovePlans.Add(InputItem);
            //        MapDrawList.Add(Point);
            //    }
            //    InputPathPlan.UnitID = LOCInfo_ID;
            //    ViewModel_ScenarioView.SingletonInstance.model_Unit_Develop.unit_LAH_MovePlansList = InputPathPlan;
            //    MovePlanList.Add(InputPathPlan);


            //    //foreach (var input in value.WaypointList)
            //    //{
            //    //    WayPointUAVItemSource.Add(input);
            //    //}
            //    if (MapDrawList.Count > 2)
            //    {
            //        ViewModel_Unit_Map.SingletonInstance.UnitDevelopPathPlanList.Clear();
            //        //var inputPoints = new MapPolyline();

            //        // StrokeStyle 인스턴스 생성 및 속성 설정
            //        var myStrokeStyle = new StrokeStyle()
            //        {
            //            Thickness = 5, // 선 두께를 2로 설정
            //            StartLineCap = PenLineCap.Round, // 선 시작 부분을 라운드 처리
            //            EndLineCap = PenLineCap.Round,   // 선 끝 부분을 라운드 처리
            //            LineJoin = PenLineJoin.Round,     // 선 연결부도 라운드 처리 (필요한 경우)
            //            //DashArray = DoubleCollection.
            //            DashCap = PenLineCap.Round,
            //            DashOffset = 5,
            //        };

            //        var inputPoints = new MapPolyline
            //        {
            //            Stroke = Brushes.IndianRed,
            //            Fill = Brushes.Transparent,
            //            StrokeStyle = myStrokeStyle,

            //        };

            //        foreach (var input in MapDrawList)
            //        {
            //            var inputPoint = new GeoPoint(input.GetY(), input.GetX());
            //            inputPoints.Points.Add(inputPoint);
            //        }
            //        ViewModel_Unit_Map.SingletonInstance.UnitDevelopPathPlanList.Add(inputPoints);
            //        ViewModel_Unit_Map.SingletonInstance.TempUnitDevelopPathPlanList.Clear();
            //    }
            //    //MovePlans.Clear();


            //    //임시 이동경로 지우기
            //    //0은 나중에 순번체크
            //    //CommonEvent.OnDevelopPathPlanAdd?.Invoke(0, MapDrawList);


            //    //마지막에 추가한 이동경로 구역 선택
            //    //var TempIndex = PathPlanList.Count();
            //    //PathPlanListIndex = TempIndex - 1;
            //}
        }

        public RelayCommand DetailSaveCommand { get; set; }

        public void DetailSaveCommandAction(object param)
        {
            //if (MovePlanList.Count > 0)
            //{
            //    MovePlanList.
            //    MovePlanList[PathPlanListIndex].LAHID = LOCInfo_ID;
            //    MovePlanList[PathPlanListIndex].ALT = LOCInfo_ALT;
            //    MovePlanList[PathPlanListIndex].Speed = LOCInfo_Speed;
            //    MovePlanList[PathPlanListIndex].Mission = MissionTypeIndex;
            //    MovePlanList[PathPlanListIndex].PassType = PassTypeIndex;
            //    MovePlanList[PathPlanListIndex].WaitTime = WaitTypeIndex;
            //    MovePlanList[PathPlanListIndex].TargetID = TargetID;
            //    MovePlanList[PathPlanListIndex].AttackCount = AttackCount;
            //}
            if (MovePlans.Count > 0)
            {
                //MovePlans[PathPlanListIndex].LAHID = LOCInfo_ID;
                //MovePlans[PathPlanListIndex].LAT = LOCInfo_LAT;
                //MovePlans[PathPlanListIndex].LON = LOCInfo_LON;

                //PathPlanListIndex가 -1인지 체크해야할듯
                if(PathPlanListIndex == -1)
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var pop_error = new View_PopUp(10);
                        pop_error.Description.Text = "포인트 저장 실패";
                        pop_error.Reason.Text = "Row를 선택하세요.";
                        pop_error.Show();
                    });
                }
                else
                {
                    MovePlans[PathPlanListIndex].ALT = LOCInfo_ALT;
                    MovePlans[PathPlanListIndex].Speed = LOCInfo_Speed;
                    MovePlans[PathPlanListIndex].Mission = MissionTypeIndex;
                    MovePlans[PathPlanListIndex].PassType = PassTypeIndex;
                    MovePlans[PathPlanListIndex].WaitTime = WaitTypeIndex;
                    MovePlans[PathPlanListIndex].TargetID = TargetID;
                    MovePlans[PathPlanListIndex].AttackCount = AttackCount;

                    MovePlans[PathPlanListIndex].CameraFOV = CameraFOV;
                    switch (CameraModeIndex)
                    {
                        case 0:
                            {
                                MovePlans[PathPlanListIndex].CameraMode = CameraModeIndex;
                                MovePlans[PathPlanListIndex].FixedPointLat = FixedPointLat;
                                MovePlans[PathPlanListIndex].FixedPointLon = FixedPointLon;
                            }
                            break;
                        case 1:
                            {
                                MovePlans[PathPlanListIndex].CameraMode = CameraModeIndex;
                                MovePlans[PathPlanListIndex].SweepPoint1Lat = SweepPoint1Lat;
                                MovePlans[PathPlanListIndex].SweepPoint1Lon = SweepPoint1Lon;
                                MovePlans[PathPlanListIndex].SweepPoint2Lat = SweepPoint2Lat;
                                MovePlans[PathPlanListIndex].SweepPoint2Lon = SweepPoint2Lon;
                                MovePlans[PathPlanListIndex].SweepTime = SweepTime;
                            }
                            break;
                        case 2:
                            {
                                MovePlans[PathPlanListIndex].CameraMode = CameraModeIndex;
                                MovePlans[PathPlanListIndex].FixedUAVRoll = FixedUAVRoll;
                                MovePlans[PathPlanListIndex].FixedUAVPitch = FixedUAVPitch;
                                MovePlans[PathPlanListIndex].FixedUAVYaw = FixedUAVYaw;
                            }
                            break;
                        case 3:
                            {
                                MovePlans[PathPlanListIndex].CameraMode = CameraModeIndex;
                                MovePlans[PathPlanListIndex].ChasingTargetNum = ChasingTargetNum;
                            }
                            break;
                        case 4:
                            {
                                MovePlans[PathPlanListIndex].CameraMode = -1;
                            }
                            break;

                    }
                }

                
            }
        }

        public RelayCommand TotalDetailSaveCommand { get; set; }

        public void TotalDetailSaveCommandAction(object param)
        {
            if (MovePlans.Count > 0)
            {
                for(int i=0; i< MovePlans.Count; i++)
                {
                    //MovePlanList[i].LAHID = LOCInfo_ID;
                    MovePlans[i].ALT = LOCInfo_ALT;
                    MovePlans[i].Speed = LOCInfo_Speed;
                    MovePlans[i].Mission = MissionTypeIndex;
                    MovePlans[i].PassType = PassTypeIndex;
                    MovePlans[i].WaitTime = WaitTypeIndex;
                    MovePlans[i].TargetID = TargetID;
                    MovePlans[i].AttackCount = AttackCount;

                    MovePlans[i].CameraFOV = CameraFOV;
                    switch(CameraModeIndex)
                    {
                        case 0:
                            {
                                MovePlans[i].CameraMode = CameraModeIndex;
                                MovePlans[i].FixedPointLat = FixedPointLat;
                                MovePlans[i].FixedPointLon = FixedPointLon;
                            }
                            break;
                        case 1:
                            {
                                MovePlans[i].CameraMode = CameraModeIndex;
                                MovePlans[i].SweepPoint1Lat = SweepPoint1Lat;
                                MovePlans[i].SweepPoint1Lon = SweepPoint1Lon;
                                MovePlans[i].SweepPoint2Lat = SweepPoint2Lat;
                                MovePlans[i].SweepPoint2Lon = SweepPoint2Lon;
                                MovePlans[i].SweepTime = SweepTime;
                            }
                            break;
                        case 2:
                            {
                                MovePlans[i].CameraMode = CameraModeIndex;
                                MovePlans[i].FixedUAVRoll = FixedUAVRoll;
                                MovePlans[i].FixedUAVPitch = FixedUAVPitch;
                                MovePlans[i].FixedUAVYaw = FixedUAVYaw;
                            }
                            break;
                        case 3:
                            {
                                MovePlans[i].CameraMode = CameraModeIndex;
                                MovePlans[i].ChasingTargetNum = ChasingTargetNum;
                            }
                            break;
                        case 4:
                            {
                                MovePlans[i].CameraMode = -1;
                            }
                            break;

                    }
           
                  
                        
                }
            }
        }

        public RelayCommand TextTestCommand { get; set; }

        public void TextTestCommandAction(object param)
        {
            // 1. 실행 파일 기준 ScenarioFiles 폴더 경로 (기존과 동일)
            string scenarioFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ScenarioFiles");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            // 2. OpenFileDialog 생성 및 .txt 필터 설정
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = scenarioFolder,
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*", // 필터 변경
                Multiselect = false
            };

            // 3. 파일 탐색창 표시
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                string selectedFile = openFileDialog.FileName;

                // 4. 반환할 CustomMapPoint 컬렉션 생성
                var loadedPoints = new ObservableCollection<CustomMapPoint>();

                try
                {
                    // 5. 텍스트 파일의 모든 라인을 읽어옵니다.
                    string[] lines = File.ReadAllLines(selectedFile);

                    foreach (string line in lines)
                    {
                        // 5-1. 비어있는 줄은 건너뜁니다.
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        // 5-2. 콤마(,)로 데이터를 분리합니다.
                        string[] parts = line.Split(',');

                        // 5-3. 위도, 경도가 모두 있는지 확인합니다. (고도는 무시)
                        if (parts.Length >= 2)
                        {
                            // 5-4. 문자열을 double로 파싱합니다.
                            // (CultureInfo.InvariantCulture 사용으로 소수점(.) 파싱 오류 방지)
                            if (double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double latitude) &&
                                double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double longitude))
                            {
                                // 5-5. (0, 0) 좌표는 종종 헤더거나 유효하지 않은 값이므로 제외합니다. (예시 데이터 기준)
                                if (latitude == 0.0 && longitude == 0.0)
                                {
                                    continue;
                                }

                                // 5-6. CustomMapPoint 객체를 생성하고 컬렉션에 추가합니다.
                                loadedPoints.Add(new CustomMapPoint
                                {
                                    Latitude = latitude,
                                    Longitude = longitude
                                });
                            }
                            else
                            {
                                // 파싱 실패 시 디버그 로그 출력 (선택 사항)
                                System.Diagnostics.Debug.WriteLine($"[LoadTextTrack] 파싱 실패: {line}");
                            }
                        }
                    }

                    // 6. 모든 포인트를 읽은 후, 기존 함수를 호출하여 지도에 그립니다.
                    ViewModel_Unit_Map.SingletonInstance.UpdateTextTestWaypoints(loadedPoints);

                    // 필요 시 다른 UI 상태 변경 (예: SceneBorderVisibility = Visibility.Hidden;)
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("텍스트 경로 파일을 로드하는 중 오류가 발생했습니다: " + ex.Message);
                }
            }
        }

        public RelayCommand SendCommand { get; set; }

        public void SendCommandAction(object param)
        {
            Model_ScenarioSequenceManager.SingletonInstance.SendScenarioMission();
        }

        public RelayCommand SendUnrealCommand { get; set; }

        public void SendUnrealCommandAction(object param)
        {
            ViewModel_ScenarioView.SingletonInstance.SceneStatus = "모의 준비 중";
            ViewModel_ScenarioView.SingletonInstance.IsSimPlaying = true;
            ViewModel_ScenarioView.SingletonInstance.IsSimPlayingRev = false;
            ViewModel_ScenarioView.SingletonInstance.SimPlayButtonEnable = false;
            ViewModel_UC_Unit_MissionPackage.SingletonInstance.SimPlayButtonEnable = false;
            ViewModel_ScenarioView.SingletonInstance.EditButtonEnable = false;
            ViewModel_ScenarioView.SingletonInstance.DeleteButtonEnable = false;
            Model_ScenarioSequenceManager.SingletonInstance.IsClickedMake = true;
            ViewModel_ScenarioView.SingletonInstance.IsObervationStart = true;
            ViewModel_ScenarioView.SingletonInstance.prev_model_UnitScenario = JsonConvert.SerializeObject(ViewModel_ScenarioView.SingletonInstance.model_UnitScenario);



            Model_ScenarioSequenceManager.SingletonInstance.ObjectMake();

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var pop_error = new View_PopUp(5);
                pop_error.Description.Text = "메시지 송신";
                pop_error.Reason.Text = "언리얼 객체 생성";
                pop_error.Show();
            });

            ViewModel_ScenarioView.SingletonInstance._uiUpdateTimer.Start();
            //ViewModel_ScenarioView.SingletonInstance._uiUpdateTimer2.Start();
        }


    }

}
