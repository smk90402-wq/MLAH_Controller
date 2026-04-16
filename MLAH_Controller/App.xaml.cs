using Microsoft.Extensions.DependencyInjection;
using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;

namespace MLAH_Controller
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }


        private bool _isShuttingDown = false;
        private readonly object _shutdownLock = new object();
        public static bool IsShuttingDown { get; private set; } = false;
        private CancellationTokenSource _appCts = new CancellationTokenSource();

        // 관리할 언리얼 프로세스 이름 (확장자 제외)
        private const string UE_PROCESS_NAME = "MLAHProject";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // [안전 장치] 시작 시에도 혹시 모를 좀비 프로세스 정리
            ForceKillUEProcess();
            ForceKillMonitoringProcesses();

            //var serviceCollection = new ServiceCollection();
            //ConfigureServices(serviceCollection);
            //ServiceProvider = serviceCollection.BuildServiceProvider();

            //var path = PathManager.GetOrSetLibraryPath();

            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                // 애플리케이션이 어떤 이유로든 종료될 때 이 코드가 실행됩니다.
                // GetAwaiter().GetResult()는 이 종료 이벤트 핸들러 내에서 비동기 작업을 동기적으로 기다리는 표준적인 방법입니다.

                // Visual Studio 디버깅 중지 시 이 이벤트가 호출됩니다.
                // 여기서 await를 쓰거나 .GetResult()로 대기하면 프리징 걸릴 확률이 매우 높습니다.
                // 따라서 네트워크 정리보다 외부 프로세스 사살(Kill)을 최우선으로 합니다.

                ForceKillUEProcess(); // 언리얼 프로세스 즉시 종료
                ForceKillMonitoringProcesses();

                ViewModel_ScenarioView.SingletonInstance.IsSimPlaying = false;
                try
                {
                    // gRPC 종료 시도 (단, 너무 오래 기다리지 않도록 타임아웃 짧게 설정)
                    // 디버깅 중일 때는 이 부분이 데드락을 유발하므로 건너뛰거나 try-catch로 감쌉니다.
                    if (!Debugger.IsAttached)
                    {
                        // 배포 버전에서는 정상적인 종료 시도
                        gRPCModule.SingletonInstance.ShutdownGrpcAsync().Wait(1000);
                    }
                }
                catch
                {
                    // 종료 중 오류 무시 
                }
            };

            var scenarioManager = Model_ScenarioSequenceManager.SingletonInstance;
            var mainViewModel = ViewModel_MainView.SingletonInstance;
            var scenarioViewModel = ViewModel_ScenarioView.SingletonInstance;

            string strINIFilePath = CommonUtil.ExecutableDirectory + "Config.ini";

            // Config.ini의 IPSet에 따라 IP 설정 로드 (gRPC/UDP 생성 전에 호출)
            CommonUtil.IPConfig.Load();

            var grpcModule = gRPCModule.SingletonInstance;
            grpcModule.StartServer();

            var udpModule = UDPModule.SingletonInstance;

            // 임무통제운용모의시현모의쪽 수신
            udpModule.InitializeReceiver(CommonUtil.IPConfig.UdpRecvIP, 50001);

            // 무인기 모의쪽 수신
            udpModule.InitializeReceiver(CommonUtil.IPConfig.UdpRecvIP, 50003);
            BitAgentManager.Instance.StartListener();

            var shellView = new ShellView
            {
                DataContext = new ShellViewModel()
                //DataContext = ServiceProvider.GetRequiredService<ShellViewModel>()
            };
            shellView.Show();


            //ViewModel_Config_PopUp.SingletonInstance.SetDataFromINI();
            // 비동기 메서드를 UI 차단 없이 호출("fire-and-forget")
            _ = ViewModel_Config_PopUp.SingletonInstance.SetDataFromINIAsync();

            //AppDomain.CurrentDomain.ProcessExit += async (_, __) => await gRPCModule.SingletonInstance.ShutdownGrpcAsync();
            //DispatcherUnhandledException += async (_, __) => await gRPCModule.SingletonInstance.ShutdownGrpcAsync();

            //Model_ScenarioSequenceManager.SingletonInstance.StartProcessing();

            CommonEvent.OnClientSessionCleanedUp += HandleClientSessionCleanedUp;

            string sbc2_targetIp = "192.168.20.100"; // 예시 IP
            int sbc2_targetPort = 50004;             // 예시 포트

            // ViewModel_MainView의 인스턴스를 통해 메서드 호출 (메서드를 해당 클래스에 넣었을 경우)
            _ = Model_ScenarioSequenceManager.SingletonInstance.StartSendingSBC2StatusAsync(sbc2_targetIp, sbc2_targetPort, _appCts.Token);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ViewModel_ScenarioView.SingletonInstance.IsSimPlaying = false;

            // 1. 먼저 언리얼 프로세스를 정리합니다.
            ForceKillUEProcess();
            ForceKillMonitoringProcesses();

            // 2. 리소스 정리
            try
            {
                // 정상 종료 시에는 비동기 대기를 해도 괜찮습니다.
                gRPCModule.SingletonInstance.ShutdownGrpcAsync().GetAwaiter().GetResult();
            }
            catch { }

            _appCts.Cancel();
            BitAgentManager.Instance.StopListener();
            base.OnExit(e);
        }

        public async Task ShutdownApplicationAsync()
        {
            lock (_shutdownLock)
            {
                if (_isShuttingDown) return;
                _isShuttingDown = true;

                IsShuttingDown = true; // 전역 플래그 설정
            }

            System.Diagnostics.Debug.WriteLine("[App] 애플리케이션 종료 시작...");

            // 먼저 언리얼 프로세스부터 죽여서 Fatal Error 방지
            ForceKillUEProcess();
            ForceKillMonitoringProcesses();

            // 백그라운드 작업들 먼저 중지
            //ViewModel_Unit_Map.SingletonInstance?.StopFocusSquareLoop();
            //ViewModel_Unit_Map.SingletonInstance?.StopFourCornerLoop();
            ViewModel_Unit_Map.SingletonInstance?.StopSelectedObjectVisualsLoop();
            //Model_ScenarioSequenceManager.SingletonInstance?.StopProcessing();

            // gRPC 서버 종료 (타임아웃 포함)
            await gRPCModule.SingletonInstance.ShutdownGrpcAsync();

            int count = gRPCModule.SingletonInstance.ServiceImplementation.ActiveSessionCount;
            System.Diagnostics.Debug.WriteLine($"현재 활성 세션 수: {count}");

            UDPModule.SingletonInstance.Dispose();
            NamedPipeSender.Instance.Dispose();
            NamedPipeSenderUDP.Instance.Dispose();

            // 모든 정리가 끝난 후, 실제 애플리케이션 종료
            System.Diagnostics.Debug.WriteLine("[App] 모든 리소스 정리 완료. 애플리케이션을 종료합니다.");
            Application.Current.Shutdown();
        }

        private void HandleClientSessionCleanedUp(string clientPeer)
        {
            // 이벤트 핸들러는 UI 스레드에서 실행되도록 보장하는 것이 안전합니다.
            //Dispatcher.Invoke(() =>
            //{
            //    var pop_error = new View_PopUp(10);
            //    pop_error.Description.Text = "클라이언트 연결 종료";
            //    pop_error.Reason.Text = $"클라이언트({clientPeer})와의 연결이 종료되었습니다.";
            //    pop_error.Show();
            //});
            System.Diagnostics.Debug.WriteLine($"Client 연결 종료");
        }

        /// <summary>
        /// [핵심] 언리얼 프로세스를 강제로 찾아서 종료하는 메서드
        /// </summary>
        private void ForceKillUEProcess()
        {
            try
            {
                // 이름으로 프로세스 찾기 (확장자 .exe 제외)
                Process[] processes = Process.GetProcessesByName(UE_PROCESS_NAME);

                if (processes.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[App] {UE_PROCESS_NAME} 프로세스 {processes.Length}개 발견. 강제 종료 시도...");
                    foreach (Process p in processes)
                    {
                        try
                        {
                            if (!p.HasExited)
                            {
                                p.Kill(); // 강제 종료
                                p.WaitForExit(1000); // 최대 1초 대기
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[App] 프로세스 종료 실패: {ex.Message}");
                        }
                        finally
                        {
                            p.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] 프로세스 검색 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 모니터링 프로세스들(gRPC, UDP)을 강제로 찾아 종료하는 메서드
        /// </summary>
        private void ForceKillMonitoringProcesses()
        {
            string[] targetApps = { "MLAH_Mornitoring", "MLAH_Mornitoring_UDP" };

            foreach (var appName in targetApps)
            {
                try
                {
                    Process[] processes = Process.GetProcessesByName(appName);
                    if (processes.Length > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[App] {appName} 프로세스 {processes.Length}개 발견. 정리합니다.");
                        foreach (Process p in processes)
                        {
                            try
                            {
                                if (!p.HasExited)
                                {
                                    p.Kill(); // 강제 종료
                                    // p.WaitForExit(500); // 필요 시 대기 (보통 Kill 호출만으로 충분)
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[App] {appName} 종료 실패: {ex.Message}");
                            }
                            finally
                            {
                                p.Dispose();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[App] {appName} 검색 오류: {ex.Message}");
                }
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // =====================================================================
            // ======================= 리팩토링 핵심 변경 사항 =======================
            // =====================================================================

            // 1. 메시지 버스를 싱글턴으로 등록합니다. (아래 '다' 항목에서 구현)
            services.AddSingleton<IMessageBus, MessageBus>();

            // 2. 모든 ViewModel을 '싱글턴' 라이프타임으로 등록합니다.
            // 이렇게 하면 앱 전체에서 각 ViewModel의 인스턴스가 단 하나만 생성됩니다.
            services.AddSingleton<ShellViewModel>();
            //services.AddSingleton<ViewModel_MainView>();
            //services.AddSingleton<ViewModel_ScenarioView>();
            //services.AddSingleton<ViewModel_Unit_Map>();
            //services.AddSingleton<ViewModel_UC_Unit_INITMissionInfo>();
            //services.AddSingleton<ViewModel_ScenarioObject_PopUp>();
            //// ... 다른 모든 ViewModel들도 여기에 등록 ...

            //// 3. gRPC, UDP 같은 서비스들도 싱글턴으로 등록합니다.
            //services.AddSingleton<gRPCModule>();
            //services.AddSingleton<UDPModule>();
        }

    }



}
