
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System;
using MLAH_Controller;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.IO.Packaging;
using DevExpress.Xpf.Map;
using DevExpress.CodeParser;
using DevExpress.Xpf.Editors;



namespace MLAH_Controller
{
    public class ViewModel_ScenarioObject_PopUp : CommonBase
    {
        #region Singleton
        static ViewModel_ScenarioObject_PopUp _ViewModel_ScenarioObject_PopUp = null;
        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static ViewModel_ScenarioObject_PopUp SingletonInstance
        {
            get
            {
                if (_ViewModel_ScenarioObject_PopUp == null)
                {
                    _ViewModel_ScenarioObject_PopUp = new ViewModel_ScenarioObject_PopUp();
                }
                return _ViewModel_ScenarioObject_PopUp;
            }
        }

        #endregion Singleton

        #region 생성자 & 콜백
        public ViewModel_ScenarioObject_PopUp()
        {
            ConfirmCommand = new RelayCommand(ConfirmCommandAction);
            CancelCommand = new RelayCommand(CancelCommandAction);

            CommonEvent.OnMapPOSSelect += Callback_OnMapPOSSelect;

            ObjectTargetTypeComboboxItems = new ObservableCollection<string> { "유형 없음","유인기동헬기", "장갑차", "탱크", "자주포", "트럭", "작전차량", "군인" };
            ObjectTargetPlatformComboboxItems = new ObservableCollection<string> { "분류 없음" };
            UpdateTypeComboboxItems();
        }

        #endregion 생성자 & 콜백

        #region Property 모음

        public VisibilityMode PopupMode = VisibilityMode.Add;

        private int _ObjectNumForEdit = 0;
        public int ObjectNumForEdit
        {

            get { return _ObjectNumForEdit; }
            set
            {
                _ObjectNumForEdit = value;
                OnPropertyChanged("ObjectNumForEdit");
            }
        }

        private string _PopUpModeText = "객체 생성";
        public string PopUpModeText
        {

            get { return _PopUpModeText; }
            set
            {
                _PopUpModeText = value;
                OnPropertyChanged("PopUpModeText");
            }
        }

        private Visibility _ObjectNumberVisibility = Visibility.Visible;
        public Visibility ObjectNumberVisibility
        {

            get { return _ObjectNumberVisibility; }
            set
            {
                _ObjectNumberVisibility = value;
                OnPropertyChanged("ObjectNumberVisibility");
            }
        }

        private bool _ObjectTypeComboBoxEnable = true;
        public bool ObjectTypeComboBoxEnable
        {

            get { return _ObjectTypeComboBoxEnable; }
            set
            {
                _ObjectTypeComboBoxEnable = value;
                OnPropertyChanged("ObjectTypeComboBoxEnable");
            }
        }

        private bool _POSSelectChecked = false;
        public bool POSSelectChecked
        {

            get { return _POSSelectChecked; }
            set
            {
                _POSSelectChecked = value;
                OnPropertyChanged("POSSelectChecked");

                if (value)
                {
                    IsInfoPanelVisible = true;
                    CurrentModeTitle = (PopUpModeText == "객체 생성") ? "신규 객체 위치 설정" : "객체 위치 수정";
                    CurrentShortcutKey = "A";
                }
                else
                {
                    IsInfoPanelVisible = false;
                }
            }
        }

        private Visibility _HelicopterVisible = Visibility.Visible;
        public Visibility HelicopterVisible
        {

            get
            {
                return _HelicopterVisible;
            }
            set
            {
                _HelicopterVisible = value;
                OnPropertyChanged("HelicopterVisible");
            }
        }

        private Visibility _DroneVisible = Visibility.Hidden;
        public Visibility DroneVisible
        {

            get
            {
                return _DroneVisible;
            }
            set
            {
                _DroneVisible = value;
                OnPropertyChanged("DroneVisible");
            }
        }

        private Visibility _TargetVisible = Visibility.Hidden;
        public Visibility TargetVisible
        {

            get
            {
                return _TargetVisible;
            }
            set
            {
                _TargetVisible = value;
                OnPropertyChanged("TargetVisible");
            }
        }

        private int _ObjectTypeIndex = 1;
        /// <summary>
        /// 객체 유형 -  1 : 유인기  / 2 : 무인기  / 3 : 아/적군 객체
        /// </summary>
        public int ObjectTypeIndex
        {

            get { return _ObjectTypeIndex; }
            set
            {
                _ObjectTypeIndex = value;
                if (value == 1)
                {
                    HelicopterVisible = Visibility.Visible;
                    DroneVisible = Visibility.Hidden;
                    TargetVisible = Visibility.Hidden;
                }
                else if (value == 2)
                {
                    HelicopterVisible = Visibility.Hidden;
                    DroneVisible = Visibility.Visible;
                    TargetVisible = Visibility.Hidden;
                }
                else
                {
                    HelicopterVisible = Visibility.Hidden;
                    DroneVisible = Visibility.Hidden;
                    TargetVisible = Visibility.Visible;
                }
                OnPropertyChanged("ObjectTypeIndex");
            }
        }

        private int _IsLeaderIndex = 2;
        public int IsLeaderIndex
        {

            get { return _IsLeaderIndex; }
            set
            {
                _IsLeaderIndex = value;
                OnPropertyChanged("IsLeaderIndex");
            }
        }

        private int _AttackFlagIndex = 1;
        public int AttackFlagIndex
        {

            get { return _AttackFlagIndex; }
            set
            {
                _AttackFlagIndex = value;
                OnPropertyChanged("AttackFlagIndex");
            }
        }

        private int _ObjectModeIndex = 1;
        public int ObjectModeIndex
        {

            get { return _ObjectModeIndex; }
            set
            {
                _ObjectModeIndex = value;
                OnPropertyChanged("ObjectModeIndex");
            }
        }



        private int _ObjectTargetIdentifyIndex = 1;
        public int ObjectTargetIdentifyIndex
        {

            get { return _ObjectTargetIdentifyIndex; }
            set
            {
                _ObjectTargetIdentifyIndex = value;
                OnPropertyChanged("ObjectTargetIdentifyIndex");
                //피아 식별에따른 유형 기종 나중에 체크
                //UpdateTypeComboboxItems();
            }
        }

        private int _ObjectTargetTypeIndex = 0;
        public int ObjectTargetTypeIndex
        {

            get { return _ObjectTargetTypeIndex; }
            set
            {
                _ObjectTargetTypeIndex = value;
                OnPropertyChanged("ObjectTargetTypeIndex");
                UpdatePlatformComboboxItems();
            }
        }

        private int _ObjectTargetPlatfformIndex = 0;
        public int ObjectTargetPlatfformIndex
        {

            get { return _ObjectTargetPlatfformIndex; }
            set
            {
                _ObjectTargetPlatfformIndex = value;

                if(PopupMode == VisibilityMode.Add)
                {
                    //ZPU-4
                    if(ObjectTargetTypeIndex == 8 && value ==1)
                    {
                        DetectRange = 1500;
                        AttackRange = 1000;
                        AttackAccuracy = 50;
                        AttackDelay = 0.1;
                        AttackDamage = 1;
                    }
                    //SA-16
                    else if (ObjectTargetTypeIndex == 9 && value == 1)
                    {
                        DetectRange = 1500;
                        AttackRange = 1000;
                        AttackAccuracy = 100;
                        AttackDelay = 5;
                        AttackDamage = 100;
                    }
                    else
                    {
                        //DetectRange = 0;
                        //AttackRange = 0;
                        //AttackAccuracy = 0;
                        //AttackDelay = 0;
                        //AttackDamage = 0;
                    }
                }
                    OnPropertyChanged("ObjectTargetPlatfformIndex");
            }
        }

        private void UpdateTypeComboboxItems()
        {
            ObjectTargetTypeComboboxItems.Clear();
            ObjectTargetTypeComboboxItems.Add("유형 없음");
            ObjectTargetTypeComboboxItems.Add("수직이착륙무인기_");
            ObjectTargetTypeComboboxItems.Add("유인기동헬기");
            ObjectTargetTypeComboboxItems.Add("유인공격헬기_");
            ObjectTargetTypeComboboxItems.Add("장갑차");
            ObjectTargetTypeComboboxItems.Add("탱크");
            ObjectTargetTypeComboboxItems.Add("방사포");
            ObjectTargetTypeComboboxItems.Add("곡사포");
            ObjectTargetTypeComboboxItems.Add("고정고사포");
            ObjectTargetTypeComboboxItems.Add("특작군인");
            ObjectTargetTypeComboboxItems.Add("자주포_");
            ObjectTargetTypeComboboxItems.Add("트럭_");
            ObjectTargetTypeComboboxItems.Add("작전차량_");
            ObjectTargetTypeComboboxItems.Add("군인_");
        }

        //생성가능 아/적군객체 서브타입 2025.02.20
        //T55
        //M2010
        ///M1938
        ///M1992
        ///ZPU4
        ///K1
        ///K2
        ///K511

        private void UpdatePlatformComboboxItems()
        {
            ObjectTargetPlatformComboboxItems.Clear();
            if (ObjectTargetTypeIndex == 1)
            {
                ObjectTargetPlatformComboboxItems.Add("분류 없음");
                ObjectTargetPlatformComboboxItems.Add("UAV_");
                ObjectTargetPlatfformIndex = 0;
            }
            else if (ObjectTargetTypeIndex == 2)
            {
                ObjectTargetPlatformComboboxItems.Add("분류 없음");
                ObjectTargetPlatformComboboxItems.Add("KUH");
                //ObjectTargetPlatformComboboxItems.Add("K200");
                //ObjectTargetPlatformComboboxItems.Add("K221");
                //ObjectTargetPlatformComboboxItems.Add("K806");
                //ObjectTargetPlatformComboboxItems.Add("K808");
                if(PopupMode == VisibilityMode.Add)
                {
                    ObjectTargetPlatfformIndex = 1;
                }
                    
            }
            else if (ObjectTargetTypeIndex == 3)
            {
                ObjectTargetPlatformComboboxItems.Add("분류 없음");
                ObjectTargetPlatformComboboxItems.Add("LAH_");
                //ObjectTargetPlatformComboboxItems.Add("K1A1");
                //ObjectTargetPlatformComboboxItems.Add("K2");
                ObjectTargetPlatfformIndex = 0;
            }
            else if (ObjectTargetTypeIndex == 4)
            {
                ObjectTargetPlatformComboboxItems.Add("분류 없음");
                ObjectTargetPlatformComboboxItems.Add("K200_");
                ObjectTargetPlatformComboboxItems.Add("K221_");
                ObjectTargetPlatformComboboxItems.Add("K806_");
                ObjectTargetPlatformComboboxItems.Add("K808_");
                ObjectTargetPlatformComboboxItems.Add("M2010");
                //ObjectTargetPlatformComboboxItems.Add("K55A1");
                //ObjectTargetPlatformComboboxItems.Add("K9");
                if (PopupMode == VisibilityMode.Add)
                {
                    ObjectTargetPlatfformIndex = 5;
                }
            }
            else if (ObjectTargetTypeIndex == 5)
            {
                ObjectTargetPlatformComboboxItems.Add("분류 없음");
                //ObjectTargetPlatformComboboxItems.Add("K311");
                //ObjectTargetPlatformComboboxItems.Add("K511");
                ObjectTargetPlatformComboboxItems.Add("K1A1_");
                ObjectTargetPlatformComboboxItems.Add("K2_");
                ObjectTargetPlatformComboboxItems.Add("T-55");
                ObjectTargetPlatformComboboxItems.Add("T-72_");
                ObjectTargetPlatformComboboxItems.Add("천마호_");
                ObjectTargetPlatformComboboxItems.Add("폭풍호_");
                if (PopupMode == VisibilityMode.Add)
                {
                    ObjectTargetPlatfformIndex = 3;
                }
            }
            else if (ObjectTargetTypeIndex == 6)
            {
                ObjectTargetPlatformComboboxItems.Add("분류 없음");
                ObjectTargetPlatformComboboxItems.Add("M1992 (122mm)");
                //ObjectTargetPlatformComboboxItems.Add("K131");
                if (PopupMode == VisibilityMode.Add)
                {
                    ObjectTargetPlatfformIndex = 1;
                }
            }
            else if (ObjectTargetTypeIndex == 7)
            {
                ObjectTargetPlatformComboboxItems.Add("분류 없음");
                ObjectTargetPlatformComboboxItems.Add("M1938 (122mm)");
                //ObjectTargetPlatformComboboxItems.Add("군인(공중강습)");
                if (PopupMode == VisibilityMode.Add)
                {
                    ObjectTargetPlatfformIndex = 1;
                }
            }

            else if (ObjectTargetTypeIndex == 8)
            {
                ObjectTargetPlatformComboboxItems.Add("분류 없음");
                ObjectTargetPlatformComboboxItems.Add("ZPU-4");
                ObjectTargetPlatformComboboxItems.Add("ZPU-23_");
                ObjectTargetPlatformComboboxItems.Add("KS-12_");
                ObjectTargetPlatformComboboxItems.Add("KS-19_");
                ObjectTargetPlatformComboboxItems.Add("SA-3_");
                if (PopupMode == VisibilityMode.Add)
                {
                    ObjectTargetPlatfformIndex = 1;
                }
            }

            else if (ObjectTargetTypeIndex == 9)
            {
                ObjectTargetPlatformComboboxItems.Add("분류 없음");
                ObjectTargetPlatformComboboxItems.Add("특작군인 (SA-16)");
                //ObjectTargetPlatformComboboxItems.Add("군인(공중강습)");
                if (PopupMode == VisibilityMode.Add)
                {
                    ObjectTargetPlatfformIndex = 1;
                }
            }

            else if (ObjectTargetTypeIndex == 10)
            {
                ObjectTargetPlatformComboboxItems.Add("분류 없음");
                ObjectTargetPlatformComboboxItems.Add("K55A1_");
                ObjectTargetPlatformComboboxItems.Add("K9_");
                ObjectTargetPlatfformIndex = 0;
            }

            else if (ObjectTargetTypeIndex == 11)
            {
                ObjectTargetPlatformComboboxItems.Add("분류 없음");
                ObjectTargetPlatformComboboxItems.Add("K311_");
                ObjectTargetPlatformComboboxItems.Add("K511_");
                ObjectTargetPlatfformIndex = 0;
            }

            else if (ObjectTargetTypeIndex == 12)
            {
                ObjectTargetPlatformComboboxItems.Add("분류 없음");
                ObjectTargetPlatformComboboxItems.Add("K131_");
                ObjectTargetPlatfformIndex = 0;
            }

            else if (ObjectTargetTypeIndex == 13)
            {
                ObjectTargetPlatformComboboxItems.Add("분류 없음");
                ObjectTargetPlatformComboboxItems.Add("군인(공중강습)_");
                ObjectTargetPlatfformIndex = 0;
            }


        }


        public ObservableCollection<string> ObjectTargetTypeComboboxItems { get; set; }
        public ObservableCollection<string> ObjectTargetPlatformComboboxItems { get; set; }

        private int _DroneLinkNumber = 0;
        public int DroneLinkNumber
        {

            get { return _DroneLinkNumber; }
            set
            {
                _DroneLinkNumber = value;
                OnPropertyChanged("DroneLinkNumber");
            }
        }

        private double _ObjectLat = 0;
        public double ObjectLat
        {

            get
            {
                return _ObjectLat;
            }
            set
            {
                _ObjectLat = value;
                OnPropertyChanged("ObjectLat");
            }
        }

        private double _ObjectLon;
        public double ObjectLon
        {

            get { return _ObjectLon; }
            set
            {
                _ObjectLon = value;
                OnPropertyChanged("ObjectLon");
            }
        }

        private int _ObjectAlt = 1000;
        public int ObjectAlt
        {

            get { return _ObjectAlt; }
            set
            {
                _ObjectAlt = value;
                OnPropertyChanged("ObjectAlt");
            }
        }

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

        private double _ObjectRoll = 0;
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

        private double _ObjectPitch = 0;
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

        private double _ObjectYaw = 0;
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

        private double _ObjectSpeedX = 0;
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

        private double _ObjectSpeedY = 0;
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

        private double _ObjectSpeedZ = 0;
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

        private double _ObjectVelocityRoll = 0;
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

        private double _ObjectVelocityPitch = 0;
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


        private double _ObjectVelocityYaw = 0;
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

        private double _ObjectHeading = 0;
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

        private double _ObjectFuel = 15;
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

        private double _RecogPixel = 10;
        public double RecogPixel
        {
            get
            {
                return _RecogPixel;
            }
            set
            {
                _RecogPixel = value;
                OnPropertyChanged("RecogPixel");
            }
        }

        private double _DetectPixel = 20;
        public double DetectPixel
        {
            get
            {
                return _DetectPixel;
            }
            set
            {
                _DetectPixel = value;
                OnPropertyChanged("DetectPixel");
            }
        }

        private double _ObjectFuelConsumption = 0.0000323;
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

        private ushort _MissileRound = 2;
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

        private ushort _RocketRound = 14;
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

        private ushort _MiniGunRound = 100;
        public ushort MiniGunRound
        {
            get
            {
                return _MiniGunRound;
            }
            set
            {
                _MiniGunRound = value;
                OnPropertyChanged("MiniGunRound");
            }
        }

        private int _IsAbnormalIndex = 1;
        public int IsAbnormalIndex
        {
            get
            {
                return _IsAbnormalIndex;
            }
            set
            {
                _IsAbnormalIndex = value;
                if (value == 1)
                {
                    AbnormalReasonEnable = false;
                    AbnormalReasonIndex = 0;
                }
                else if (value == 2)
                {
                    AbnormalReasonEnable = true;
                }
                else
                {
                    AbnormalReasonEnable = false;
                    AbnormalReasonIndex = 0;
                }
                OnPropertyChanged("IsAbnormalIndex");
            }
        }

        private int _HelicopterWeaponIndex = 0;
        public int HelicopterWeaponIndex
        {
            get
            {
                return _HelicopterWeaponIndex;
            }
            set
            {
                _HelicopterWeaponIndex = value;
                OnPropertyChanged("HelicopterWeaponIndex");
            }
        }

        private int _AbnormalReasonIndex = 0;
        public int AbnormalReasonIndex
        {
            get
            {
                return _AbnormalReasonIndex;
            }
            set
            {
                _AbnormalReasonIndex = value;
                OnPropertyChanged("AbnormalReasonIndex");
            }
        }

        private int _UAVTestMode = 0;
        public int UAVTestMode
        {
            get
            {
                return _UAVTestMode;
            }
            set
            {
                _UAVTestMode = value;
                OnPropertyChanged("UAVTestMode");
            }
        }

        private bool _AbnormalReasonEnable = false;
        public bool AbnormalReasonEnable
        {
            get
            {
                return _AbnormalReasonEnable;
            }
            set
            {
                _AbnormalReasonEnable = value;
                OnPropertyChanged("AbnormalReasonEnable");
            }
        }



        private string _ConfirmButtonText = "생성";
        public string ConfirmButtonText
        {
            get
            {
                return _ConfirmButtonText;
            }
            set
            {
                _ConfirmButtonText = value;
                OnPropertyChanged("ConfirmButtonText");
            }
        }

        private bool _IsLeaderEnable = true;
        public bool IsLeaderEnable
        {
            get
            {
                return _IsLeaderEnable;
            }
            set
            {
                _IsLeaderEnable = value;
                OnPropertyChanged("IsLeaderEnable");
            }
        }

        private bool _ObjectTargetTypeEnable = true;
        public bool ObjectTargetTypeEnable
        {
            get
            {
                return _ObjectTargetTypeEnable;
            }
            set
            {
                _ObjectTargetTypeEnable = value;
                OnPropertyChanged("ObjectTargetTypeEnable");
            }
        }

        private bool _ObjectTargetPlatformEnable = true;
        public bool ObjectTargetPlatformEnable
        {
            get
            {
                return _ObjectTargetPlatformEnable;
            }
            set
            {
                _ObjectTargetPlatformEnable = value;
                OnPropertyChanged("ObjectTargetPlatformEnable");
            }
        }

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


        #endregion Property 모음

        #region Method 모음

        public RelayCommand ConfirmCommand { get; set; }

        public void ConfirmCommandAction(object param)
        {
            if (PopupMode == VisibilityMode.Add)
            {
                var HelicopterCounts = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList.Count(x => x.Type == 3);
                var DroneCounts = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList.Count(x => x.Type == 1);
                var HelicopterLeaderCounts = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList.Count(x => x.Type == 3 && x.ID == 1);
                //var DroneLeaderCounts = ViewModel_ScenarioView.SingletonInstance.TempScenarioObjects.Count(x => x.ObjectType == 2 && x.IsLeader == true);
                if (HelicopterCounts >= 3 && ObjectTypeIndex == 1)
                {
                    var pop_error = new View_PopUp(10);
                    pop_error.Description.Text = "유인기 대수 제한";
                    pop_error.Reason.Text = "현재 유인기가 3대 이상입니다.";
                    pop_error.Show();
                }
                else if (DroneCounts >= 3 && ObjectTypeIndex == 2)
                {
                    var pop_error = new View_PopUp(10);
                    pop_error.Description.Text = "무인기 대수 제한";
                    pop_error.Reason.Text = "현재 무인기가 3대 이상입니다.";
                    pop_error.Show();
                }
                else
                {
                    UnitObjectInfo Temp = new UnitObjectInfo();
                    //유인기
                    if (ObjectTypeIndex == 1)
                    {
                            Temp.ID = ObjectNumAllocator(1);
                            if (Temp.ID == 1)
                            {
                                Temp.IsLeader = 1;
                            }
                            else
                            {
                                Temp.IsLeader = 2;
                            }
                            Temp.Type = 3;
                            //Temp.Health = (uint)IsAbnormalIndex;
                            Temp.Health = 100;
                            Temp.LOC.Latitude = (float)ObjectLat;
                            Temp.LOC.Longitude = (float)ObjectLon;
                            Temp.LOC.Altitude = (int)ObjectAlt;
                            Temp.velocity.Speed = (float)ObjectSpeedX;
                            Temp.velocity.Heading = (float)ObjectHeading;
                            Temp.Fuel = (float)ObjectFuel;
                            Temp.FuelConsumption = (float)ObjectFuelConsumption;
                        

                            Temp.weapons.Type1 = MiniGunRound;
                            //Temp.MissileDamage = MissileDamage;
                            Temp.weapons.Type2 = RocketRound;
                            //Temp.RocketDamage = RocketDamage;
                            Temp.weapons.Type3 = MissileRound;
                            //Temp.AbnormalReason = AbnormalReasonIndex;

                            Temp.Identification = 1;

                            Temp.AttackFlag = AttackFlagIndex;

                            ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList.Add(Temp);
                            //마지막에 추가한 객체 선택
                            var TempIndex = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList.Count();

                            //이렇게 하지말고 UnitObjectList 바뀔 때 정보화면도 업데이트 되도록
                            ViewModel_ScenarioView.SingletonInstance.ListSelectedIndex = TempIndex - 1;
                            ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[TempIndex - 1];

                            var mapItem = ViewModel_Unit_Map.SingletonInstance.ConvertToObjectInfo(Temp);
                            ViewModel_Unit_Map.SingletonInstance.ObjectDisplayList.Add(mapItem);
                            //ViewModel_Unit_Map.SingletonInstance.AddMapObject(mapItem);

                    }
                    //무인기
                    else if (ObjectTypeIndex == 2)
                    {
                        Temp.ID = ObjectNumAllocator(2);
                        Temp.Type = 1;
                        //Temp.Health = (uint)IsAbnormalIndex;
                        //Temp.PlatformType = (short)1;
                        Temp.Health = 100;
                        Temp.LOC.Latitude = (float)ObjectLat;
                        Temp.LOC.Longitude = (float)ObjectLon;
                        Temp.LOC.Altitude = (int)ObjectAlt;
                        Temp.velocity.Speed = (float)ObjectSpeedX;
                        Temp.velocity.Heading = (float)ObjectHeading;
                        //Temp.Fuel = (float)ObjectFuel;
                        Temp.Fuel = (float)100;
                        //Temp.FuelConsumption = (float)ObjectFuelConsumption;
                        Temp.FuelConsumption = (float)0;
                        Temp.DetectPixel = DetectPixel;
                        Temp.RecogPixel = RecogPixel;

                        Temp.Identification = 1;

                        

                        Temp.UAVTestMode = UAVTestMode;
                        //System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        //{
                        ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList.Add(Temp);
                        //마지막에 추가한 객체 선택
                        var TempIndex = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList.Count();

                        //이렇게 하지말고 UnitObjectList 바뀔 때 정보화면도 업데이트 되도록
                        //ViewModel_ScenarioView.SingletonInstance.ListSelectedIndex = TempIndex - 1;
                        //ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject = ViewModel_ScenarioView.SingletonInstance.model_ScenarioView.ScenarioObjects[TempIndex - 1];
                        var mapItem = ViewModel_Unit_Map.SingletonInstance.ConvertToObjectInfo(Temp);
                        ViewModel_Unit_Map.SingletonInstance.ObjectDisplayList.Add(mapItem);
                        //ViewModel_Unit_Map.SingletonInstance.AddMapObject(mapItem);
                        //});
                    }
                    //아/적군 객체
                    else if (ObjectTypeIndex == 3)
                    {
                        Temp.ID = ObjectNumAllocator(3);
                        Temp.Type = (short)ObjectTargetTypeIndex;
                        Temp.PlatformType = (short)ObjectTargetPlatfformIndex;
                        Temp.Identification = (short)ObjectTargetIdentifyIndex;
                        //Temp.Health = (uint)IsAbnormalIndex;
                        Temp.LOC.Latitude = (float)ObjectLat;
                        Temp.LOC.Longitude = (float)ObjectLon;
                        Temp.LOC.Altitude = (int)ObjectAlt;
                        //Temp.velocity.Speed = (float)ObjectSpeedX;
                        //Temp.velocity.Heading = (float)ObjectHeading;
                        //Temp.Fuel = (float)ObjectFuel;
                        //Temp.PathPlanList = 

                        Temp.DetectRange = DetectRange;
                        Temp.AttackRange = AttackRange;
                        Temp.AttackAccuracy = AttackAccuracy;
                        Temp.AttackDelay = AttackDelay;
                        Temp.AttackDamage = AttackDamage;

                        //System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        //{
                        ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList.Add(Temp);
                        //마지막에 추가한 객체 선택
                        var TempIndex = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList.Count();

                        //이렇게 하지말고 UnitObjectList 바뀔 때 정보화면도 업데이트 되도록
                        //ViewModel_ScenarioView.SingletonInstance.ListSelectedIndex = TempIndex - 1;
                        //ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject = ViewModel_ScenarioView.SingletonInstance.model_ScenarioView.ScenarioObjects[TempIndex - 1];
                        //});
                        var mapItem = ViewModel_Unit_Map.SingletonInstance.ConvertToObjectInfo(Temp);
                        ViewModel_Unit_Map.SingletonInstance.ObjectDisplayList.Add(mapItem);
                        //ViewModel_Unit_Map.SingletonInstance.AddMapObject(mapItem);
                    }

                }
            }

          
            else if (PopupMode == VisibilityMode.Edit)
            {
                // --- 1. 데이터 모델 객체 찾기 ---
                var unitToUpdate = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList
                    .FirstOrDefault(x => x.ID == ObjectNumForEdit);
                if (unitToUpdate == null) return;

                // --- 2. 데이터 모델 속성 업데이트 ---
                if (ObjectTypeIndex == 1) // 유인기
                {
                    unitToUpdate.LOC.Latitude = (float)ObjectLat;
                    unitToUpdate.LOC.Longitude = (float)ObjectLon;
                    unitToUpdate.LOC.Altitude = (int)ObjectAlt;
                    unitToUpdate.velocity.Heading = (float)ObjectHeading;
                    unitToUpdate.Fuel = (float)ObjectFuel;
                    unitToUpdate.FuelConsumption = (float)ObjectFuelConsumption;
                    unitToUpdate.weapons.Type1 = MiniGunRound;
                    unitToUpdate.weapons.Type2 = RocketRound;
                    unitToUpdate.weapons.Type3 = MissileRound;
                    unitToUpdate.AttackFlag = AttackFlagIndex;
                }
                else if (ObjectTypeIndex == 2) // 무인기
                {
                    unitToUpdate.LOC.Latitude = (float)ObjectLat;
                    unitToUpdate.LOC.Longitude = (float)ObjectLon;
                    unitToUpdate.LOC.Altitude = (int)ObjectAlt;
                    unitToUpdate.velocity.Heading = (float)ObjectHeading;
                    unitToUpdate.Fuel = (float)ObjectFuel;
                    unitToUpdate.FuelConsumption = (float)ObjectFuelConsumption;
                    unitToUpdate.UAVTestMode = UAVTestMode;
                    unitToUpdate.DetectPixel = DetectPixel;
                    unitToUpdate.RecogPixel = RecogPixel;
                }
                else // 아/적군 객체
                {
                    unitToUpdate.LOC.Latitude = (float)ObjectLat;
                    unitToUpdate.LOC.Longitude = (float)ObjectLon;
                    unitToUpdate.LOC.Altitude = (int)ObjectAlt;
                    unitToUpdate.DetectRange = DetectRange;
                    unitToUpdate.AttackRange = AttackRange;
                    unitToUpdate.AttackAccuracy = AttackAccuracy;
                    unitToUpdate.AttackDelay = AttackDelay;
                    unitToUpdate.AttackDamage = AttackDamage;
                }

                // --- 3. 지도에 표시된 객체 찾아서 업데이트 ---
                var mapObjectToUpdate = ViewModel_Unit_Map.SingletonInstance.ObjectDisplayList.FirstOrDefault(x => x.ID == ObjectNumForEdit);

                if (mapObjectToUpdate != null)
                {
                    // 위치와 방향 속성을 업데이트하여 지도에 즉시 반영
                    mapObjectToUpdate.Location = new GeoPoint(ObjectLat, ObjectLon);
                    mapObjectToUpdate.Heading = ObjectHeading;
                }
                // =====================================================================

                // --- 4. 시나리오 뷰의 상세 정보창 새로고침 ---
                // 선택된 객체를 강제로 다시 선택하게 하여 상세 정보 UI를 갱신
                var scenarioVM = ViewModel_ScenarioView.SingletonInstance;
                int selectedIndex = scenarioVM.ListSelectedIndex;
                scenarioVM.SelectedScenarioObject = null; // 선택 해제
                scenarioVM.SelectedScenarioObject = scenarioVM.model_UnitScenario.UnitObjectList[selectedIndex]; // 다시 선택
            }
            else
            {

            }
            //ObjectTypeComboBoxEnable = true;
            //페이드 닫기
            //var fadeOutAnimation = new DoubleAnimation
            //{
            //    From = 1.0,
            //    To = 0.0,
            //    Duration = new System.Windows.Duration(TimeSpan.FromSeconds(0.5))
            //};
            //fadeOutAnimation.Completed += (s, a) =>
            //{
            //    View_ScenarioObject_PopUp.SingletonInstance.Hide();
            //};
            //View_ScenarioObject_PopUp.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);

            POSSelectChecked = false;
            IsInfoPanelVisible = false;
            View_ScenarioObject_PopUp.SingletonInstance.Hide();
        }

        public RelayCommand CancelCommand { get; set; }

        public void CancelCommandAction(object param)
        {
            POSSelectChecked = false;
            IsInfoPanelVisible = false;
            View_ScenarioObject_PopUp.SingletonInstance.Hide();
        }


        private void Callback_OnMapPOSSelect(double lat, double lon, double alt)
        {
            if (POSSelectChecked)
            {
                POSSelectChecked = false;
                ObjectLat = lat;
                ObjectLon = lon;
                //ObjectAlt = CommonUtil.GetElevationFromCoords(lat, lon);
                //ObjectAlt = alt;
            }
        }

        public void ModeVisibilitySetter(VisibilityMode Mode)
        {
            switch (Mode)
            {
                case VisibilityMode.Add:
                    {
                        PopUpModeText = "객체 생성";
                        ObjectNumberVisibility = Visibility.Collapsed;
                        ConfirmButtonText = "생성";
                        ObjectTypeComboBoxEnable = true;
                        ObjectTargetTypeEnable = true;
                        ObjectTargetPlatformEnable = true;
                        //IsLeaderEnable = true;

                        //초기화
                        //ObjectTypeIndex = 1;
                        //IsAbnormalIndex = 1;
                        //ObjectLat = 0;
                        //ObjectLon = 0;
                        //ObjectAlt = 0;
                        //ObjectRoll = 0;
                        //ObjectPitch = 0;
                        //ObjectYaw = 0;
                        //ObjectSpeedX = 0;
                        //ObjectSpeedY = 0;
                        //ObjectSpeedZ = 0;
                        //ObjectVelocityRoll = 0;
                        //ObjectVelocityPitch = 0;
                        //ObjectVelocityYaw = 0;
                        //ObjectHeading = 0;
                        //IsLeaderIndex = 2;
                        //ObjectFuel = 0;
                        //ObjectFuelConsumption = 0;
                        //MissileDamage = 0;
                        //MissileRound = 0;
                        //RocketDamage = 0;
                        //RocketRound = 0;
                        //HelicopterWeaponIndex = 0;
                        //AbnormalReasonIndex = 0;

                    }
                    break;
                case VisibilityMode.Edit:
                    {
                        PopUpModeText = "객체 수정";
                        ObjectNumberVisibility = Visibility.Visible;
                        ConfirmButtonText = "수정";
                        ObjectTypeComboBoxEnable = false;
                        //IsLeaderEnable = false;
                        ObjectTargetTypeEnable = false;
                        ObjectTargetPlatformEnable = false;

                        ObjectNumForEdit = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ID;
                        if (ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.Type == 1)
                        {
                            ObjectTypeIndex = 2;
                        }
                        else if (ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.Type == 3)
                        {
                            ObjectTypeIndex = 1;
                        }
                        else
                        {
                            ObjectTypeIndex = 3;
                        }

                        //ObjectTypeIndex = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.Type;

                        //Type == 3 >> LAH
                        if (ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.Type == 3)
                        {
                            ObjectLat = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.LOC.Latitude;
                            ObjectLon = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.LOC.Longitude;
                            ObjectAlt = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.LOC.Altitude;
                            //ObjectRoll = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectRoll;
                            //ObjectPitch = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectPitch;
                            //ObjectYaw = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectYaw;
                            //ObjectSpeedX = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectSpeedX;
                            //ObjectSpeedY = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectSpeedY;
                            //ObjectSpeedZ = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectSpeedZ;
                            //ObjectVelocityRoll = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectVelocityRoll;
                            //ObjectVelocityPitch = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectVelocityPitch;
                            //ObjectVelocityYaw = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectVelocityYaw;
                            ObjectHeading = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.velocity.Heading;
                            //IsLeaderIndex = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.IsLeader == true ? 1 : 2;
                            //IsAbnormalIndex = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectStatus == true ? 1 : 2;
                            ObjectFuel = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.Fuel;
                            ObjectFuelConsumption = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.FuelConsumption;
                            //HelicopterWeaponIndex = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.SelectedHelicopterWeapon;
                            //MissileDamage = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.MissileDamage;
                            MiniGunRound = (ushort)ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.weapons.Type1;
                            RocketRound = (ushort)ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.weapons.Type2;
                            MissileRound = (ushort)ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.weapons.Type3;
                            //AbnormalReasonIndex = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.AbnormalReason;
                            AttackFlagIndex = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.AttackFlag;
                        }
                        else if (ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.Type == 1)
                        {
                            ObjectLat = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.LOC.Latitude;
                            ObjectLon = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.LOC.Longitude;
                            ObjectAlt = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.LOC.Altitude;
                            //ObjectRoll = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectRoll;
                            //ObjectPitch = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectPitch;
                            //ObjectYaw = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectYaw;
                            //ObjectSpeedX = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectSpeedX;
                            //ObjectSpeedY = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectSpeedY;
                            //ObjectSpeedZ = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectSpeedZ;
                            //ObjectVelocityRoll = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectVelocityRoll;
                            //ObjectVelocityPitch = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectVelocityPitch;
                            //ObjectVelocityYaw = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectVelocityYaw;
                            ObjectHeading = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.velocity.Heading;
                            //DroneLinkNumber = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.DroneLinkNumber;
                            //IsAbnormalIndex = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.ObjectStatus == true ? 1 : 2;
                            ObjectFuel = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.Fuel;
                            ObjectFuelConsumption = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.FuelConsumption;
                            //AbnormalReasonIndex = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.AbnormalReason;
                            DetectPixel = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.DetectPixel;
                            RecogPixel = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.RecogPixel;
                            UAVTestMode = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.UAVTestMode;
                        }
                        else
                        {
                            ObjectLat = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.LOC.Latitude;
                            ObjectLon = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.LOC.Longitude;
                            ObjectAlt = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.LOC.Altitude;
                            //ObjectModeIndex = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.TargetMode;
                            ObjectTargetIdentifyIndex = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.Identification;
                            ObjectTargetTypeIndex = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.Type;
                            ObjectTargetPlatfformIndex = ViewModel_ScenarioView.SingletonInstance.SelectedScenarioObject.PlatformType;

                        }

                    }
                    break;

                default:
                    { }
                    break;

            }
        }

        /// <summary>
        /// 표적 번호 -  0 : 지휘기  / 1 : 편대기  / 2 : 편대기  / 3 : 무인기 1  / 4 : 무인기 2  / 5 : 무인기 3
        /// </summary>
        private int ObjectNumAllocator(int object_Type)
        {
            IEnumerable<int> validNumbers;
            IEnumerable<int> existingNumbers;

            switch (object_Type)
            {
                /// <summary>
                // 유인기: 1, 2, 3만 할당
                /// </summary>
                case 1:
                    validNumbers = new List<int> { 1, 2, 3 };
                    existingNumbers = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList
                        .Where(x => x.Type == 3 && validNumbers.Contains(x.ID))
                        .Select(x => x.ID)
                        .OrderBy(x => x);
                    break;

                /// <summary>
                // 무인기: 4, 5, 6만 할당
                /// </summary>
                case 2:
                    validNumbers = new List<int> { 4, 5, 6 };
                    existingNumbers = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList
                        .Where(x => x.Type == 1 && validNumbers.Contains(x.ID))
                        .Select(x => x.ID)
                        .OrderBy(x => x);
                    break;

                // 아/적군 객체: 7부터 할당
                case 3:
                    int startNumber = 7;
                    validNumbers = Enumerable.Range(startNumber, int.MaxValue - startNumber + 1);
                    existingNumbers = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList
                        .Where(x => x.ID >= startNumber)
                        .Select(x => x.ID)
                        .OrderBy(x => x);
                    break;

                default:
                    return 0;
            }

            // 사용 가능한 번호 중에서 사용되지 않은 가장 작은 번호 찾기
            int result = validNumbers.Except(existingNumbers).FirstOrDefault();

            return result;
        }

        #endregion Method 모음



    }


}
