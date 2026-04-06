
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
//using static GMap.NET.Entity.OpenStreetMapGraphHopperRouteEntity;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using DevExpress.Dialogs.Core.View;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors.Helpers;
using DevExpress.Xpf.Scheduling.VisualData;
using Google.Protobuf;
using MLAH_Controller;
using MLAHInterop;
using REALTIMEVISUAL.Native.FederateInterface;
using static MLAH_Controller.CommonEvent;


namespace MLAH_Controller
{
    public partial class ViewModel_MainView : CommonBase
    {
        private static readonly Lazy<ViewModel_MainView> _lazy = new Lazy<ViewModel_MainView>(() => new ViewModel_MainView());

        public static ViewModel_MainView SingletonInstance => _lazy.Value;

        #region 생성자 & 콜백
        public ViewModel_MainView()
        {
            //_dialogService = ServiceProvider.DialogService;
            ConfigCommand = new RelayCommand(ConfigCommandAction);
            MornitoringCommand = new RelayCommand(MornitoringCommandAction);
            UDPMornitoringCommand = new RelayCommand(UDPMornitoringCommandAction);
            QuitCommand = new RelayCommand(QuitCommandAction);
            ScenarioButtonCommand = new RelayCommand(ScenarioButtonCommandAction);

            //MessageProcessor.Instance.OnCTRL_BFSS_212_CTRLER_Equip_Status += Callback_OnCTRL_BFSS_212_CTRLER_Equip_Status;
            //MessageProcessor.Instance.OnSWBit += Callback_OnSWBit;

            //절차서 임시
            //SetAllStatusToNormal();
            BitAgentManager.Instance.OnHwStatusReceived += Callback_OnHwStatusReceived;
            BitAgentManager.Instance.OnSwStatusReceived += Callback_OnSwStatusReceived;
        }

        private void Callback_OnHwStatusReceived(int type, int status)
        {
            // UI 스레드에서 프로퍼티를 안전하게 업데이트
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TargetType targetType = (TargetType)type;

                switch (targetType)
                {
                    case TargetType.BattleSim1: BattleSimHwStatus1 = status; break;
                    case TargetType.BattleSim2: BattleSimHwStatus2 = status; break;
                    case TargetType.BattleSim3: BattleSimHwStatus3 = status; break;

                    case TargetType.UAVSim1: UAVSimHwStatus1 = status; break;
                    case TargetType.UAVSim2: UAVSimHwStatus2 = status; break;
                    case TargetType.UAVSim3: UAVSimHwStatus3 = status; break;

                    case TargetType.MissionControl: ControlOperSimHwStatus = status; break;
                    case TargetType.DisplaySim: DisplaySimHwStatus = status; break;
                    case TargetType.SituationAwareness: SCSimHwStatus = status; break;

                    // 💡 [테스트용 추가] RTVTest2 수신 시 전장환경 모의 1(BattleSim1) LED로 확인
                    case TargetType.RTVTest2:
                        BattleSimHwStatus1 = status;
                        SCSimHwStatus = status;
                        //System.Diagnostics.Debug.WriteLine($"[TEST] RTVTest2 HW 상태 수신: {status}");
                        break;
                }
            });
        }

        // =================================================================
        // 💡 [신규] 에이전트 SW 상태 수신 콜백 (기존 Callback_SwStatus 대체)
        // =================================================================
        private void Callback_OnSwStatusReceived(int type, int status)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TargetType targetType = (TargetType)type;

                switch (targetType)
                {
                    case TargetType.BattleSim1: BattleSimSwStatus1 = status; break;
                    case TargetType.BattleSim2: BattleSimSwStatus2 = status; break;
                    case TargetType.BattleSim3: BattleSimSwStatus3 = status; break;

                    case TargetType.UAVSim1: UAVSimSwStatus1 = status; break;
                    case TargetType.UAVSim2: UAVSimSwStatus2 = status; break;
                    case TargetType.UAVSim3: UAVSimSwStatus3 = status; break;

                    case TargetType.MissionControl: ControlOperSimSwStatus = status; break;
                    case TargetType.DisplaySim: DisplaySimSwStatus = status; break;
                    case TargetType.SituationAwareness: SCSimSwStatus = status; break;

                    // 💡 [테스트용 추가] RTVTest2 수신 시 전장환경 모의 1(BattleSim1) LED로 확인
                    case TargetType.RTVTest2:
                        BattleSimSwStatus1 = status;
                        SCSimSwStatus = status;
                        //System.Diagnostics.Debug.WriteLine($"[TEST] RTVTest2 SW 상태 수신: {status}");
                        break;
                }
            });
        }

        #endregion 생성자 & 콜백


        public RelayCommand ScenarioButtonCommand { get; set; }

        public void ScenarioButtonCommandAction(object param)
        {
            ShellViewModel.Instance.GoToScenarioViewCommand.Execute(null);
        }

        public RelayCommand ConfigCommand { get; set; }

        public void ConfigCommandAction(object param)
        {
            ////CommonEvent.OnRequestFadeOut?.Invoke(ViewName.ConfigPopup);
            //CommonUtil.ShowFadeWindow(ViewName.ConfigPopup);
            //View_Config_PopUp.SingletonInstance.Topmost = false;
            //View_Config_PopUp.SingletonInstance.Topmost = true;

            // 기존 방식: CommonUtil.ShowFadeWindow(ViewName.ConfigPopup);

            // 새로운 방식: 팝업 인스턴스를 가져와서 헬퍼 메서드에 넘겨줍니다.
            var configPopup = View_Config_PopUp.SingletonInstance; // 각 팝업 윈도우의 싱글톤 인스턴스
            //CommonUtil.ShowPopup(configPopup);
            configPopup.Show();
            configPopup.Topmost = true;
        }

        public RelayCommand UDPMornitoringCommand { get; set; }

        public async void UDPMornitoringCommandAction(object param)
        {
            var splashViewModel = new DXSplashScreenViewModel()
            {
                Status = "모니터링 앱 시작 중...",
                Title = "실시간 UDP 모니터링",
                IsIndeterminate = true
            };
            var manager = SplashScreenManager.CreateWaitIndicator(splashViewModel);

            // 스플래시 스크린을 먼저 표시합니다.
            // ▼▼▼▼▼▼▼▼▼▼ InputBlockMode.Window로 수정 ▼▼▼▼▼▼▼▼▼▼
            manager.Show(0, 0, Application.Current.MainWindow, WindowStartupLocation.CenterOwner, true, InputBlockMode.Window);

            // Task.Run을 사용하여 모든 작업을 백그라운드에서 처리합니다.
            bool isSuccess = await Task.Run(async () =>
            {
                string readyPipeName = "UDPMornitoringAppReadyPipe"; // UDP용 파이프 이름
                try
                {
                    Task<bool> listenerTask = WaitForReadySignalAsync(readyPipeName);

                    Process monitoringProcess = StartUDPMornitoringApp(); // UDP 모니터링 앱 실행
                    if (monitoringProcess == null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var popError = new View_PopUp(5)
                            {
                                Description = { Text = "실행 오류" },
                                Reason = { Text = "모니터링 프로그램을 찾을 수 없습니다." }
                            };
                            popError.Show();
                        });
                        return false;
                    }

                    return await listenerTask; // true (성공) 또는 false (타임아웃) 반환
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var popError = new View_PopUp(5)
                        {
                            Description = { Text = "알 수 없는 오류" },
                            Reason = { Text = ex.Message }
                        };
                        popError.Show();
                    });
                    return false;
                }
            });

            // 백그라운드 작업이 모두 끝나면 스플래시 스크린을 닫습니다.
            manager.Close();

            // 타임아웃 등으로 성공하지 못했을 경우에만 팝업을 표시합니다.
            if (!isSuccess)
            {
                var popTimeout = new View_PopUp(5)
                {
                    Description = { Text = "실행 시간 초과" },
                    Reason = { Text = "프로그램이 응답하지 않습니다." }
                };
                popTimeout.Show();
            }
        }

        public RelayCommand MornitoringCommand { get; set; }

        public async void MornitoringCommandAction(object param)
        {
            var splashViewModel = new DXSplashScreenViewModel()
            {
                Status = "모니터링 앱 시작 중...",
                Title = "실시간 gRPC 모니터링",
                IsIndeterminate = true
            };
            var manager = SplashScreenManager.CreateWaitIndicator(splashViewModel);

            // ▼▼▼▼▼▼▼▼▼▼ InputBlockMode.Window로 수정 ▼▼▼▼▼▼▼▼▼▼
            manager.Show(0, 0, Application.Current.MainWindow, WindowStartupLocation.CenterOwner, true, InputBlockMode.Window);

            bool isSuccess = await Task.Run(async () =>
            {
                string readyPipeName = "MornitoringAppReadyPipe"; // gRPC용 파이프 이름
                try
                {
                    Task<bool> listenerTask = WaitForReadySignalAsync(readyPipeName);

                    Process monitoringProcess = StartMornitoringApp(); // gRPC 모니터링 앱 실행
                    if (monitoringProcess == null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var popError = new View_PopUp(5)
                            {
                                Description = { Text = "실행 오류" },
                                Reason = { Text = "모니터링 프로그램을 찾을 수 없습니다." }
                            };
                            popError.Show();
                        });
                        return false;
                    }

                    return await listenerTask;
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var popError = new View_PopUp(5)
                        {
                            Description = { Text = "알 수 없는 오류" },
                            Reason = { Text = ex.Message }
                        };
                        popError.Show();
                    });
                    return false;
                }
            });

            manager.Close();

            if (!isSuccess)
            {
                var popTimeout = new View_PopUp(5)
                {
                    Description = { Text = "실행 시간 초과" },
                    Reason = { Text = "프로그램이 응답하지 않습니다." }
                };
                popTimeout.Show();
            }
        }

        private Process StartMornitoringApp()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string monitoringAppPath = Path.Combine(baseDir,"MLAH_Mornitoring.exe");
            return File.Exists(monitoringAppPath) ? Process.Start(monitoringAppPath) : null;
        }

        private Process StartUDPMornitoringApp()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string monitoringAppPath = Path.Combine(baseDir,"MLAH_Mornitoring_UDP.exe");
            return File.Exists(monitoringAppPath) ? Process.Start(monitoringAppPath) : null;
        }

        private async Task<bool> WaitForReadySignalAsync(string pipeName)
        {
            try
            {
                using (var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    await pipeServer.WaitForConnectionAsync(cts.Token);
                    using (var reader = new StreamReader(pipeServer, Encoding.UTF8))
                    {
                        string message = await reader.ReadToEndAsync();
                        return message == "READY";
                    }
                }
            }
            catch { return false; }
        }

        public RelayCommand QuitCommand { get; set; }

        public void QuitCommandAction(object param)
        {
            //Environment.Exit(0);
            //Application.Current.Shutdown();
            // 직접 종료하지 않고, 메인 윈도우를 닫으라는 요청만 보냅니다.
            // 그러면 위의 ShellView_Closing 이벤트가 자연스럽게 호출됩니다.
            Application.Current.MainWindow?.Close();
        }


    }
}
