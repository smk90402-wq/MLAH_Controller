using System.Windows;
using System.Windows.Media.Animation;
using DevExpress.Mvvm;
using DevExpress.Office.Internal;
using DevExpress.Xpf.Core;
using System.Threading.Tasks;
using System.Windows.Threading;



namespace MLAH_Controller
{
    public class ViewModel_Config_PopUp : CommonBase
    {
        #region Singleton
        static ViewModel_Config_PopUp _ViewModel_Config_PopUp = null;
        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static ViewModel_Config_PopUp SingletonInstance
        {
            get
            {
                if (_ViewModel_Config_PopUp == null)
                {
                    _ViewModel_Config_PopUp = new ViewModel_Config_PopUp();
                }
                return _ViewModel_Config_PopUp;
            }
        }

        #endregion Singleton

        #region 생성자 & 콜백
        public ViewModel_Config_PopUp()
        {
            CancelCommand = new RelayCommand(CancelCommandAction);
            CloseCommand = new RelayCommand(CloseCommandAction);
            EditCommand = new RelayCommand(EditCommandAction);

            BattlefieldExecCommand = new RelayCommand(BattlefieldExecCommandAction);
            BattlefieldExecCommand1 = new RelayCommand(BattlefieldExecCommandAction1);
            BattlefieldQuitCommand = new RelayCommand(BattlefieldQuitCommandAction);

            BattlefieldDebugExecCommand = new RelayCommand(BattlefieldDebugExecCommandAction);
            BattlefieldDebugQuitCommand = new RelayCommand(BattlefieldDebugQuitCommandAction);

            CompTaskExecCommand = new RelayCommand(CompTaskExecCommandAction);
            CompTaskQuitCommand = new RelayCommand(CompTaskQuitCommandAction);

            DroneExecCommand = new RelayCommand(DroneExecCommandAction);
            DroneQuitCommand = new RelayCommand(DroneQuitCommandAction);

            UAV2ExecCommand = new RelayCommand(UAV2ExecCommandAction);
            UAV2QuitCommand = new RelayCommand(UAV2QuitCommandAction);

            UAV3ExecCommand = new RelayCommand(UAV3ExecCommandAction);
            UAV3QuitCommand = new RelayCommand(UAV3QuitCommandAction);

            DisplaySimExecCommand = new RelayCommand(DisplaySimExecCommandAction);
            DisplaySimQuitCommand = new RelayCommand(DisplaySimQuitCommandAction);

            ControlOperSimExecCommand = new RelayCommand(ControlOperSimExecCommandAction);
            ControlOperSimQuitCommand = new RelayCommand(ControlOperSimQuitCommandAction);

            UAV1SimShutDownCommand = new RelayCommand(UAV1SimShutDownCommandAction);
            

            //strINIFilePath = strINIFilePath + "Config.ini";
            //SetDataFromINI();



        }

        #endregion 생성자 & 콜백
        private string strINIFilePath = AppDomain.CurrentDomain.BaseDirectory + "Config.ini";

        PsExecModule ExecModule = new PsExecModule();

        //취소용 임시 저장 변수
        private String TempBattlefieldSituationIP;
        private String TempBattlefieldSituationUser;
        private String TempBattlefieldSituationPw;
        private String TempBattlefieldSituationExecPath;
        private String TempCompTaskSituationIP;
        private String TempCompTaskSituationUser;
        private String TempCompTaskSituationPw;
        private String TempCompTaskSituationExecPath;
        private String TempDroneSituationIP;
        private String TempDroneSituationUser;
        private String TempDroneSituationPw;
        private String TempDroneSituationExecPath;


        private string _BattleSim1IP = "0.0.0.0";
        public string BattleSim1IP
        {
            get
            {
                return _BattleSim1IP;
            }
            set
            {
                _BattleSim1IP = value;
                OnPropertyChanged("BattleSim1IP");
            }
        }

        private string _BattleSim2IP = "0.0.0.0";
        public string BattleSim2IP
        {
            get
            {
                return _BattleSim2IP;
            }
            set
            {
                _BattleSim2IP = value;
                OnPropertyChanged("BattleSim2IP");
            }
        }

        private string _BattleSim3IP = "0.0.0.0";
        public string BattleSim3IP
        {
            get
            {
                return _BattleSim3IP;
            }
            set
            {
                _BattleSim3IP = value;
                OnPropertyChanged("BattleSim3IP");
            }
        }

        private string _ControlOperSimUser = "";
        public string ControlOperSimUser
        {
            get
            {
                return _ControlOperSimUser;
            }
            set
            {
                _ControlOperSimUser = value;
                OnPropertyChanged("ControlOperSimUser");
            }
        }

        private string _ControlOperSimPassword = "";
        public string ControlOperSimPassword
        {
            get
            {
                return _ControlOperSimPassword;
            }
            set
            {
                _ControlOperSimPassword = value;
                OnPropertyChanged("ControlOperSimPassword");
            }
        }

        private string _ControlOperSimExecPath = "";
        public string ControlOperSimExecPath
        {
            get
            {
                return _ControlOperSimExecPath;
            }
            set
            {
                _ControlOperSimExecPath = value;
                OnPropertyChanged("ControlOperSimExecPath");
            }
        }

        private string _ControlOperSimIP = "192.168.20.100";
        public string ControlOperSimIP
        {
            get
            {
                return _ControlOperSimIP;
            }
            set
            {
                _ControlOperSimIP = value;
                OnPropertyChanged("ControlOperSimIP");
            }
        }

        private string _DisplaySimIP = "0.0.0.0";
        public string DisplaySimIP
        {
            get
            {
                return _DisplaySimIP;
            }
            set
            {
                _DisplaySimIP = value;
                OnPropertyChanged("DisplaySimIP");
            }
        }

        private string _SCSimIP = "192.168.20.201";
        public string SCSimIP
        {
            get
            {
                return _SCSimIP;
            }
            set
            {
                _SCSimIP = value;
                OnPropertyChanged("SCSimIP");
            }
        }

        private string _BattlefieldSituationUser = "";
        public string BattlefieldSituationUser
        {
            get
            {
                return _BattlefieldSituationUser;
            }
            set
            {
                _BattlefieldSituationUser = value;
                OnPropertyChanged("BattlefieldSituationUser");
            }
        }

        private string _BattlefieldSituationPw;
        public string BattlefieldSituationPw
        {
            get
            {
                return _BattlefieldSituationPw;
            }
            set
            {
                _BattlefieldSituationPw = value;
                OnPropertyChanged("BattlefieldSituationPw");
            }
        }

        private string _BattlefieldSituationExecPath;
        public string BattlefieldSituationExecPath
        {
            get
            {
                return _BattlefieldSituationExecPath;
            }
            set
            {
                _BattlefieldSituationExecPath = value;
                OnPropertyChanged("BattlefieldSituationExecPath");
            }
        }

        private string _BattlefieldSituationExecPath1;
        public string BattlefieldSituationExecPath1
        {
            get
            {
                return _BattlefieldSituationExecPath1;
            }
            set
            {
                _BattlefieldSituationExecPath1 = value;
                OnPropertyChanged("BattlefieldSituationExecPath1");
            }
        }

        private string _BattlefieldSituationDebugExecPath;
        public string BattlefieldSituationDebugExecPath
        {
            get
            {
                return _BattlefieldSituationDebugExecPath;
            }
            set
            {
                _BattlefieldSituationDebugExecPath = value;
                OnPropertyChanged("BattlefieldSituationEBattlefieldSituationDebugExecPathxecPath");
            }
        }

        private string _CompTaskSituationIP = "0.0.0.0";
        public string CompTaskSituationIP
        {
            get
            {
                return _CompTaskSituationIP;
            }
            set
            {
                _CompTaskSituationIP = value;
                OnPropertyChanged("CompTaskSituationIP");
            }
        }

        private string _CompTaskSituationUser;
        public string CompTaskSituationUser
        {
            get
            {
                return _CompTaskSituationUser;
            }
            set
            {
                _CompTaskSituationUser = value;
                OnPropertyChanged("CompTaskSituationUser");
            }
        }

        private string _CompTaskSituationPw;
        public string CompTaskSituationPw
        {
            get
            {
                return _CompTaskSituationPw;
            }
            set
            {
                _CompTaskSituationPw = value;
                OnPropertyChanged("CompTaskSituationPw");
            }
        }

        private string _CompTaskSituationExecPath;
        public string CompTaskSituationExecPath
        {
            get
            {
                return _CompTaskSituationExecPath;
            }
            set
            {
                _CompTaskSituationExecPath = value;
                OnPropertyChanged("CompTaskSituationExecPath");
            }
        }

        private string _UAVSim1IP = "0.0.0.0";
        public string UAVSim1IP
        {
            get
            {
                return _UAVSim1IP;
            }
            set
            {
                _UAVSim1IP = value;
                OnPropertyChanged("UAVSim1IP");
            }
        }

        private string _UAVSim2IP = "0.0.0.0";
        public string UAVSim2IP
        {
            get
            {
                return _UAVSim2IP;
            }
            set
            {
                _UAVSim2IP = value;
                OnPropertyChanged("UAVSim2IP");
            }
        }

        private string _UAVSim3IP = "0.0.0.0";
        public string UAVSim3IP
        {
            get
            {
                return _UAVSim3IP;
            }
            set
            {
                _UAVSim3IP = value;
                OnPropertyChanged("UAVSim3IP");
            }
        }

        private string _DroneSituationIP = "0.0.0.0";
        public string DroneSituationIP
        {
            get
            {
                return _DroneSituationIP;
            }
            set
            {
                _DroneSituationIP = value;
                OnPropertyChanged("DroneSituationIP");
            }
        }

        private string _DroneSituationUser;
        public string DroneSituationUser
        {
            get
            {
                return _DroneSituationUser;
            }
            set
            {
                _DroneSituationUser = value;
                OnPropertyChanged("DroneSituationUser");
            }
        }

        private string _DroneSituationPw;
        public string DroneSituationPw
        {
            get
            {
                return _DroneSituationPw;
            }
            set
            {
                _DroneSituationPw = value;
                OnPropertyChanged("DroneSituationPw");
            }
        }

        private string _DroneSituationExecPath;
        public string DroneSituationExecPath
        {
            get
            {
                return _DroneSituationExecPath;
            }
            set
            {
                _DroneSituationExecPath = value;
                OnPropertyChanged("DroneSituationExecPath");
            }
        }

        private string _DisplaySimUser;
        public string DisplaySimUser
        {
            get
            {
                return _DisplaySimUser;
            }
            set
            {
                _DisplaySimUser = value;
                OnPropertyChanged("DisplaySimUser");
            }
        }

        private string _DisplaySimPw;
        public string DisplaySimPw
        {
            get
            {
                return _DisplaySimPw;
            }
            set
            {
                _DisplaySimPw = value;
                OnPropertyChanged("DisplaySimPw");
            }
        }

        private string _DisplaySimExecPath;
        public string DisplaySimExecPath
        {
            get
            {
                return _DisplaySimExecPath;
            }
            set
            {
                _DisplaySimExecPath = value;
                OnPropertyChanged("DisplaySimExecPath");
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

        private bool _CancelButtonEnable = false;
        public bool CancelButtonEnable
        {
            get
            {
                return _CancelButtonEnable;
            }
            set
            {
                _CancelButtonEnable = value;
                OnPropertyChanged("CancelButtonEnable");
            }
        }

        private string _EditButtonText = "수정";
        public string EditButtonText
        {
            get
            {
                return _EditButtonText;
            }
            set
            {
                _EditButtonText = value;
                OnPropertyChanged("EditButtonText");
            }
        }



        public RelayCommand EditCommand { get; set; }

        public void EditCommandAction(object param)
        {
            //수정 클릭
            if (EditEnable == false)
            {
                EditEnable = true;
                CancelButtonEnable = true;
                EditButtonText = "저장";

                MakeTempBeforeEdit();
            }
            //저장 클릭
            else
            {
                EditEnable = false;
                CancelButtonEnable = false;
                EditButtonText = "수정";

                //Write
                SaveDataToINI();
            }
        }

        public RelayCommand CancelCommand { get; set; }

        public void CancelCommandAction(object param)
        {
            ApplyTempBeforeEdit();
            EditEnable = false;
            CancelButtonEnable = false;
            EditButtonText = "수정";
        }

        public RelayCommand BattlefieldExecCommand { get; set; }

       

        public async void BattlefieldExecCommandAction(object param)
        {
            var splashViewModel = new DXSplashScreenViewModel()
            {
                Status = "원격 프로세스 실행 준비 중...",
                Title = "전장상황 모의 SW 실행",
                IsIndeterminate = true,
            };
            var manager = SplashScreenManager.CreateWaitIndicator(splashViewModel);

            try
            {
                manager.Show(0, 0, View_Config_PopUp.SingletonInstance, WindowStartupLocation.CenterOwner, true, InputBlockMode.None);

                await Task.Run(async () =>
                {
                    // 1. 기존 프로세스가 있는지 확인하고 종료합니다.
                    splashViewModel.Status = "기존 프로세스 확인 및 종료 중...";
                    await ExecModule.QuitCommandUnrealAsync(BattleSim1IP, BattlefieldSituationUser, BattlefieldSituationPw, BattlefieldSituationExecPath);

                    // 약간의 지연을 주어 프로세스가 완전히 종료될 시간을 확보합니다.
                    await Task.Delay(1000);

                    // 2. 새로운 프로세스를 실행합니다.
                    splashViewModel.Status = "원격 PC에 실행 명령 전송 중...";
                    ExecModule.ExecuteCommand(BattleSim1IP, BattlefieldSituationUser, BattlefieldSituationPw, BattlefieldSituationExecPath);
                });
            }
            finally
            {
                manager.Close();
            }
        }

        public RelayCommand BattlefieldExecCommand1 { get; set; }



        public async void BattlefieldExecCommandAction1(object param)
        {
            var splashViewModel = new DXSplashScreenViewModel()
            {
                Status = "원격 프로세스 실행 준비 중...",
                Title = "전장정보상황인지 모의 SW 실행",
                IsIndeterminate = true,
            };
            var manager = SplashScreenManager.CreateWaitIndicator(splashViewModel);

            try
            {
                manager.Show(0, 0, View_Config_PopUp.SingletonInstance, WindowStartupLocation.CenterOwner, true, InputBlockMode.None);

                await Task.Run(async () =>
                {
                    // 1. 기존 프로세스가 있는지 확인하고 종료합니다.
                    //splashViewModel.Status = "기존 프로세스 확인 및 종료 중...";
                    //await ExecModule.QuitCommandUnrealAsync(BattleSim1IP, BattlefieldSituationUser, BattlefieldSituationPw, BattlefieldSituationExecPath);

                    // 약간의 지연을 주어 프로세스가 완전히 종료될 시간을 확보합니다.
                    //await Task.Delay(1000);

                    // 2. 새로운 프로세스를 실행합니다.
                    splashViewModel.Status = "원격 PC에 실행 명령 전송 중...";
                    ExecModule.ExecuteCommand(BattleSim1IP, BattlefieldSituationUser, BattlefieldSituationPw, BattlefieldSituationExecPath1);
                });
            }
            finally
            {
                manager.Close();
            }
        }

        public RelayCommand BattlefieldDebugExecCommand { get; set; }

        public async void BattlefieldDebugExecCommandAction(object param)
        {
            var splashViewModel = new DXSplashScreenViewModel()
            {
                Status = "원격 프로세스 실행 준비 중...",
                Title = "전장상황 모의 SW 실행 (디버그)",
                IsIndeterminate = true,
            };
            var manager = SplashScreenManager.CreateWaitIndicator(splashViewModel);

            try
            {
                manager.Show(0, 0, View_Config_PopUp.SingletonInstance, WindowStartupLocation.CenterOwner, true, InputBlockMode.None);

                await Task.Run(async () =>
                {
                    // 1. 기존 프로세스가 있는지 확인하고 종료합니다.
                    splashViewModel.Status = "기존 프로세스 확인 및 종료 중...";
                    await ExecModule.QuitCommandUnrealAsync(BattleSim1IP, BattlefieldSituationUser, BattlefieldSituationPw, BattlefieldSituationDebugExecPath);

                    await Task.Delay(1000);

                    // 2. 새로운 프로세스를 실행합니다.
                    splashViewModel.Status = "원격 PC에 실행 명령 전송 중...";
                    ExecModule.ExecuteCommand(BattleSim1IP, BattlefieldSituationUser, BattlefieldSituationPw, BattlefieldSituationDebugExecPath);
                });
            }
            finally
            {
                manager.Close();
            }
        }

        public RelayCommand BattlefieldQuitCommand { get; set; }

        public async void BattlefieldQuitCommandAction(object param)
        {
            var splashViewModel = new DXSplashScreenViewModel()
            {
                Status = "원격 PC에 종료 명령을 전송하는 중...",
                Title = "전장상황 모의 SW 종료",
                IsIndeterminate = true,
            };
            var manager = SplashScreenManager.CreateWaitIndicator(splashViewModel);

            try
            {
                manager.Show(0, 0, View_Config_PopUp.SingletonInstance, WindowStartupLocation.CenterOwner, true, InputBlockMode.None);

                await Task.Run(async () =>
                {
                    await ExecModule.QuitCommandUnrealAsync(BattleSim1IP, BattlefieldSituationUser, BattlefieldSituationPw, BattlefieldSituationExecPath);
                });
            }
            finally
            {
                manager.Close();
            }
        }

        public RelayCommand BattlefieldDebugQuitCommand { get; set; }

        public async void BattlefieldDebugQuitCommandAction(object param)
        {
            var splashViewModel = new DXSplashScreenViewModel()
            {
                Status = "원격 PC에 종료 명령을 전송하는 중...",
                Title = "전장상황 모의 SW 종료",
                IsIndeterminate = true,
            };
            var manager = SplashScreenManager.CreateWaitIndicator(splashViewModel);

            try
            {
                manager.Show(0, 0, View_Config_PopUp.SingletonInstance, WindowStartupLocation.CenterOwner, true, InputBlockMode.None);

                await Task.Run(async () =>
                {
                    await ExecModule.QuitCommand(BattleSim1IP, BattlefieldSituationUser, BattlefieldSituationPw, BattlefieldSituationExecPath);
                });
            }
            finally
            {
                manager.Close();
            }
        }

        public RelayCommand CompTaskExecCommand { get; set; }

        public void CompTaskExecCommandAction(object param)
        {
            ExecModule.ExecuteCommand(CompTaskSituationIP, CompTaskSituationUser, CompTaskSituationPw, CompTaskSituationExecPath);
        }

        public RelayCommand CompTaskQuitCommand { get; set; }

        public void CompTaskQuitCommandAction(object param)
        {
            ExecModule.QuitCommand(CompTaskSituationIP, CompTaskSituationUser, CompTaskSituationPw, CompTaskSituationExecPath);
        }

        public RelayCommand DroneExecCommand { get; set; }

        public async void DroneExecCommandAction(object param)
        {
            //ExecModule.ExecuteCommand(UAVSim1IP, DroneSituationUser, DroneSituationPw, DroneSituationExecPath);

            var splashViewModel = new DXSplashScreenViewModel()
            {
                Status = "원격 프로세스 실행 준비 중...",
                Title = "무인기 1 모의 SW 실행",
                IsIndeterminate = true,
            };
            var manager = SplashScreenManager.CreateWaitIndicator(splashViewModel);

            try
            {
                manager.Show(0, 0, View_Config_PopUp.SingletonInstance, WindowStartupLocation.CenterOwner, true, InputBlockMode.None);

                await Task.Run(async () =>
                {
                    // 1. 기존 프로세스가 있는지 확인하고 종료합니다.
                    splashViewModel.Status = "기존 프로세스 확인 및 종료 중...";
                    await ExecModule.QuitCommand(UAVSim1IP, DroneSituationUser, DroneSituationPw, DroneSituationExecPath);

                    // 약간의 지연을 주어 프로세스가 완전히 종료될 시간을 확보합니다.
                    await Task.Delay(1000);

                    // 2. 새로운 프로세스를 실행합니다.
                    splashViewModel.Status = "원격 PC에 실행 명령 전송 중...";
                    //ExecModule.ExecuteCommand(BattleSim1IP, BattlefieldSituationUser, BattlefieldSituationPw, BattlefieldSituationExecPath);
                    ExecModule.ExecuteCommand(UAVSim1IP, DroneSituationUser, DroneSituationPw, DroneSituationExecPath);
                });
            }
            finally
            {
                manager.Close();
            }

        }

        public RelayCommand DroneQuitCommand { get; set; }

        public void DroneQuitCommandAction(object param)
        {
            ExecModule.QuitCommand(UAVSim1IP, DroneSituationUser, DroneSituationPw, DroneSituationExecPath);
        }

        public RelayCommand UAV2ExecCommand { get; set; }

        public void UAV2ExecCommandAction(object param)
        {
            ExecModule.ExecuteCommand(UAVSim2IP, DroneSituationUser, DroneSituationPw, DroneSituationExecPath);
            //RunRemoteApplication(DroneSituationIP, DroneSituationExecPath, DroneSituationUser, DroneSituationPw);
            //UAV1SimWaiter.Content = "무인기 모의기 1 실행 중";
            //UAV1SimWaiter.DeferedVisibility = true;
        }

        public RelayCommand UAV2QuitCommand { get; set; }

        public void UAV2QuitCommandAction(object param)
        {
            ExecModule.QuitCommand(UAVSim2IP, DroneSituationUser, DroneSituationPw, DroneSituationExecPath);
        }

        public RelayCommand UAV3ExecCommand { get; set; }

        public void UAV3ExecCommandAction(object param)
        {
            ExecModule.ExecuteCommand(UAVSim3IP, DroneSituationUser, DroneSituationPw, DroneSituationExecPath);
            //RunRemoteApplication(DroneSituationIP, DroneSituationExecPath, DroneSituationUser, DroneSituationPw);
            //UAV1SimWaiter.Content = "무인기 모의기 1 실행 중";
            //UAV1SimWaiter.DeferedVisibility = true;
        }

        public RelayCommand UAV3QuitCommand { get; set; }

        public void UAV3QuitCommandAction(object param)
        {
            ExecModule.QuitCommand(UAVSim3IP, DroneSituationUser, DroneSituationPw, DroneSituationExecPath);
        }

        public RelayCommand DisplaySimExecCommand { get; set; }

        public void DisplaySimExecCommandAction(object param)
        {
            ExecModule.ExecuteCommand(DisplaySimIP, DisplaySimUser, DisplaySimPw, DisplaySimExecPath);
        }

        public RelayCommand DisplaySimQuitCommand { get; set; }

        public void DisplaySimQuitCommandAction(object param)
        {
            ExecModule.QuitCommand(DisplaySimIP, DisplaySimUser, DisplaySimPw, DisplaySimExecPath);
        }

        public RelayCommand ControlOperSimExecCommand { get; set; }

        public void ControlOperSimExecCommandAction(object param)
        {
            ExecModule.ExecuteCommand(ControlOperSimIP, ControlOperSimUser, ControlOperSimPassword, ControlOperSimExecPath);
        }

        public RelayCommand ControlOperSimQuitCommand { get; set; }

        public void ControlOperSimQuitCommandAction(object param)
        {
            ExecModule.QuitCommand(ControlOperSimIP, ControlOperSimUser, ControlOperSimPassword, ControlOperSimExecPath);
        }



        //public RelayCommand DroneQuitCommand { get; set; }

        //public void DroneQuitCommandAction(object param)
        //{
        //    ExecModule.QuitCommand(UAVSim1IP, DroneSituationUser, DroneSituationPw, DroneSituationExecPath);
        //}

        public RelayCommand UAV1SimShutDownCommand { get; set; }

        public void UAV1SimShutDownCommandAction(object param)
        {
            ExecModule.ShutdownCommand(UAVSim1IP, DroneSituationUser, DroneSituationPw);
        }

        public RelayCommand CloseCommand { get; set; }

        public void CloseCommandAction(object param)
        {
            //var fadeOutAnimation = new DoubleAnimation
            //{
            //    From = 1.0,
            //    To = 0.0,
            //    Duration = new System.Windows.Duration(TimeSpan.FromSeconds(0.5))
            //};
            //fadeOutAnimation.Completed += (s, a) =>
            //{
            //    View_Config_PopUp.SingletonInstance.Hide();
            //};

            //View_Config_PopUp.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
            View_Config_PopUp.SingletonInstance.Hide();
        }

        private void MakeTempBeforeEdit()
        {
            TempBattlefieldSituationIP = BattleSim1IP;
            TempBattlefieldSituationUser = BattlefieldSituationUser;
            TempBattlefieldSituationPw = BattlefieldSituationPw;
            TempBattlefieldSituationExecPath = BattlefieldSituationExecPath;
            TempCompTaskSituationIP = CompTaskSituationIP;
            TempCompTaskSituationUser = CompTaskSituationUser;
            TempCompTaskSituationPw = CompTaskSituationPw;
            TempCompTaskSituationExecPath = CompTaskSituationExecPath;
            TempDroneSituationIP = DroneSituationIP;
            TempDroneSituationUser = DroneSituationUser;
            TempDroneSituationPw = DroneSituationPw;
            TempDroneSituationExecPath = DroneSituationExecPath;
        }

        private void ApplyTempBeforeEdit()
        {
            BattleSim1IP = TempBattlefieldSituationIP;
            BattlefieldSituationUser = TempBattlefieldSituationUser;
            BattlefieldSituationPw = TempBattlefieldSituationPw;
            BattlefieldSituationExecPath = TempBattlefieldSituationExecPath;
            CompTaskSituationIP = TempCompTaskSituationIP;
            CompTaskSituationUser = TempCompTaskSituationUser;
            CompTaskSituationPw = TempCompTaskSituationPw;
            CompTaskSituationExecPath = TempCompTaskSituationExecPath;
            DroneSituationIP = TempDroneSituationIP;
            DroneSituationUser = TempDroneSituationUser;
            DroneSituationPw = TempDroneSituationPw;
            DroneSituationExecPath = TempDroneSituationExecPath;
        }

        

        public async Task SetDataFromINIAsync()
        {
            await Task.Run(async () =>
            {

                string LocalBattleSim1IP = CommonUtil.Readini_Click("BattleSim1SW", "IP", strINIFilePath);
                string LocalBattlefieldSituationUser = CommonUtil.Readini_Click("BattleSim1SW", "UserName", strINIFilePath);
                string LocalBattlefieldSituationPw = CommonUtil.Readini_Click("BattleSim1SW", "Password", strINIFilePath);
                string LocalBattlefieldSituationExecPath = CommonUtil.Readini_Click("BattleSim1SW", "ExecPath", strINIFilePath);
                string LocalBattlefieldSituationExecPath1 = CommonUtil.Readini_Click("BattleSim1SW", "ExecPath1", strINIFilePath);

                string LocalBattleSim2IP = CommonUtil.Readini_Click("BattleSim2SW", "IP", strINIFilePath);
                string LocalBattleSim3IP = CommonUtil.Readini_Click("BattleSim3SW", "IP", strINIFilePath);
                
                string LocalCompTaskSituationIP = CommonUtil.Readini_Click("ComprehensiveTaskSW", "IP", strINIFilePath);
                string LocalCompTaskSituationUser = CommonUtil.Readini_Click("ComprehensiveTaskSW", "UserName", strINIFilePath);
                string LocalCompTaskSituationPw = CommonUtil.Readini_Click("ComprehensiveTaskSW", "Password", strINIFilePath);
                string LocalCompTaskSituationExecPath = CommonUtil.Readini_Click("ComprehensiveTaskSW", "ExecPath", strINIFilePath);
                
                string LocalUAVSim1IP = CommonUtil.Readini_Click("UAVSim1", "IP", strINIFilePath);
                
                string LocalDroneSituationUser = CommonUtil.Readini_Click("UAVSim1", "UserName", strINIFilePath);
                string LocalDroneSituationPw = CommonUtil.Readini_Click("UAVSim1", "Password", strINIFilePath);
                string LocalDroneSituationExecPath = CommonUtil.Readini_Click("UAVSim1", "ExecPath", strINIFilePath);
                
                string LocalUAVSim2IP = CommonUtil.Readini_Click("UAVSim2", "IP", strINIFilePath);
                string LocalUAVSim3IP = CommonUtil.Readini_Click("UAVSim3", "IP", strINIFilePath);
                
                string LocalDisplaySimIP = CommonUtil.Readini_Click("DisplaySim", "IP", strINIFilePath);
                string LocalDisplaySimUser = CommonUtil.Readini_Click("DisplaySim", "UserName", strINIFilePath);
                string LocalDisplaySimPw = CommonUtil.Readini_Click("DisplaySim", "Password", strINIFilePath);
                string LocalDisplaySimExecPath = CommonUtil.Readini_Click("DisplaySim", "ExecPath", strINIFilePath);
                
                string LocalControlOperSimIP = CommonUtil.Readini_Click("ControlOperSim", "IP", strINIFilePath);
                string LocalControlOperSimUser = CommonUtil.Readini_Click("ControlOperSim", "UserName", strINIFilePath);
                string LocalControlOperSimPassword = CommonUtil.Readini_Click("ControlOperSim", "Password", strINIFilePath);
                string LocalControlOperSimExecPath = CommonUtil.Readini_Click("ControlOperSim", "ExecPath", strINIFilePath);

                string LocalBattlefieldSituationDebugExecPath = CommonUtil.Readini_Click("BattleSimDebugSW", "ExecPath", strINIFilePath);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    BattleSim1IP = LocalBattleSim1IP;
                    BattlefieldSituationUser = LocalBattlefieldSituationUser;
                    BattlefieldSituationPw = LocalBattlefieldSituationPw;
                    BattlefieldSituationExecPath = LocalBattlefieldSituationExecPath;
                    BattlefieldSituationExecPath1 = LocalBattlefieldSituationExecPath1;

                    BattleSim2IP = LocalBattleSim2IP;
                    BattleSim3IP = LocalBattleSim3IP;

                    CompTaskSituationIP = LocalCompTaskSituationIP;
                    CompTaskSituationUser = LocalCompTaskSituationUser;
                    CompTaskSituationPw = LocalCompTaskSituationPw;
                    CompTaskSituationExecPath = LocalCompTaskSituationExecPath;

                    UAVSim1IP = LocalUAVSim1IP;

                    DroneSituationUser = LocalDroneSituationUser;
                    DroneSituationPw = LocalDroneSituationPw;
                    DroneSituationExecPath = LocalDroneSituationExecPath;

                    UAVSim2IP = LocalUAVSim2IP;
                    UAVSim3IP = LocalUAVSim3IP;

                    DisplaySimIP = LocalDisplaySimIP;
                    DisplaySimUser = LocalDisplaySimUser;
                    DisplaySimPw = LocalDisplaySimPw;
                    DisplaySimExecPath = LocalDisplaySimExecPath;

                    ControlOperSimIP = LocalControlOperSimIP;
                    ControlOperSimUser = LocalControlOperSimUser;
                    ControlOperSimPassword = LocalControlOperSimPassword;
                    ControlOperSimExecPath = LocalControlOperSimExecPath;

                    BattlefieldSituationDebugExecPath = LocalBattlefieldSituationDebugExecPath;
                });

                //BattleSim1IP = CommonUtil.Readini_Click("BattleSim1SW", "IP", strINIFilePath);
                //BattlefieldSituationUser = CommonUtil.Readini_Click("BattleSim1SW", "UserName", strINIFilePath);
                //BattlefieldSituationPw = CommonUtil.Readini_Click("BattleSim1SW", "Password", strINIFilePath);
                //BattlefieldSituationExecPath = CommonUtil.Readini_Click("BattleSim1SW", "ExecPath", strINIFilePath);

                //BattleSim2IP = CommonUtil.Readini_Click("BattleSim2SW", "IP", strINIFilePath);
                //BattleSim3IP = CommonUtil.Readini_Click("BattleSim3SW", "IP", strINIFilePath);

                //CompTaskSituationIP = CommonUtil.Readini_Click("ComprehensiveTaskSW", "IP", strINIFilePath);
                //CompTaskSituationUser = CommonUtil.Readini_Click("ComprehensiveTaskSW", "UserName", strINIFilePath);
                //CompTaskSituationPw = CommonUtil.Readini_Click("ComprehensiveTaskSW", "Password", strINIFilePath);
                //CompTaskSituationExecPath = CommonUtil.Readini_Click("ComprehensiveTaskSW", "ExecPath", strINIFilePath);

                //UAVSim1IP = CommonUtil.Readini_Click("UAVSim1", "IP", strINIFilePath);

                //DroneSituationUser = CommonUtil.Readini_Click("UAVSim1", "UserName", strINIFilePath);
                //DroneSituationPw = CommonUtil.Readini_Click("UAVSim1", "Password", strINIFilePath);
                //DroneSituationExecPath = CommonUtil.Readini_Click("UAVSim1", "ExecPath", strINIFilePath);

                //UAVSim2IP = CommonUtil.Readini_Click("UAVSim2", "IP", strINIFilePath);
                //UAVSim3IP = CommonUtil.Readini_Click("UAVSim3", "IP", strINIFilePath);

                //DisplaySimIP = CommonUtil.Readini_Click("DisplaySim", "IP", strINIFilePath);
                //DisplaySimUser = CommonUtil.Readini_Click("DisplaySim", "UserName", strINIFilePath);
                //DisplaySimPw = CommonUtil.Readini_Click("DisplaySim", "Password", strINIFilePath);
                //DisplaySimExecPath = CommonUtil.Readini_Click("DisplaySim", "ExecPath", strINIFilePath);

                //ControlOperSimIP = CommonUtil.Readini_Click("ControlOperSim", "IP", strINIFilePath);
                //ControlOperSimUser = CommonUtil.Readini_Click("ControlOperSim", "UserName", strINIFilePath);
                //ControlOperSimPassword = CommonUtil.Readini_Click("ControlOperSim", "Password", strINIFilePath);
                //ControlOperSimExecPath = CommonUtil.Readini_Click("ControlOperSim", "ExecPath", strINIFilePath);

                //var warmUpTasks = new List<Task>();

                //if (!string.IsNullOrEmpty(BattleSim1IP))
                //    warmUpTasks.Add(ExecModule.WarmUpConnectionAsync(BattleSim1IP, BattlefieldSituationUser, BattlefieldSituationPw));

                //if (!string.IsNullOrEmpty(CompTaskSituationIP))
                //    warmUpTasks.Add(ExecModule.WarmUpConnectionAsync(CompTaskSituationIP, CompTaskSituationUser, CompTaskSituationPw));

                //if (!string.IsNullOrEmpty(UAVSim1IP))
                //    warmUpTasks.Add(ExecModule.WarmUpConnectionAsync(UAVSim1IP, DroneSituationUser, DroneSituationPw));

                //if (!string.IsNullOrEmpty(UAVSim2IP))
                //    warmUpTasks.Add(ExecModule.WarmUpConnectionAsync(UAVSim2IP, DroneSituationUser, DroneSituationPw));

                //if (!string.IsNullOrEmpty(UAVSim3IP))
                //    warmUpTasks.Add(ExecModule.WarmUpConnectionAsync(UAVSim3IP, DroneSituationUser, DroneSituationPw));

                //if (!string.IsNullOrEmpty(DisplaySimIP))
                //    warmUpTasks.Add(ExecModule.WarmUpConnectionAsync(DisplaySimIP, DisplaySimUser, DisplaySimPw));

                //await Task.WhenAll(warmUpTasks);
            });
        }
    

        private void SaveDataToINI()
        {
            // INI 파일에 데이터 쓰기
            CommonUtil.WriteINI("BattlefieldSituationSimSW", "IP", BattleSim1IP, strINIFilePath);
            CommonUtil.WriteINI("BattlefieldSituationSimSW", "UserName", BattlefieldSituationUser, strINIFilePath);
            CommonUtil.WriteINI("BattlefieldSituationSimSW", "Password", BattlefieldSituationPw, strINIFilePath);
            CommonUtil.WriteINI("BattlefieldSituationSimSW", "ExecPath", BattlefieldSituationExecPath, strINIFilePath);

            CommonUtil.WriteINI("ComprehensiveTaskSW", "IP", CompTaskSituationIP, strINIFilePath);
            CommonUtil.WriteINI("ComprehensiveTaskSW", "UserName", CompTaskSituationUser, strINIFilePath);
            CommonUtil.WriteINI("ComprehensiveTaskSW", "Password", CompTaskSituationPw, strINIFilePath);
            CommonUtil.WriteINI("ComprehensiveTaskSW", "ExecPath", CompTaskSituationExecPath, strINIFilePath);

            CommonUtil.WriteINI("DroneSW", "IP", DroneSituationIP, strINIFilePath);
            CommonUtil.WriteINI("DroneSW", "UserName", DroneSituationUser, strINIFilePath);
            CommonUtil.WriteINI("DroneSW", "Password", DroneSituationPw, strINIFilePath);
            CommonUtil.WriteINI("DroneSW", "ExecPath", DroneSituationExecPath, strINIFilePath);
        }



      
    }


}
