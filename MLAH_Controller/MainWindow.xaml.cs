using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Grpc.Core;
using Grpc.Net.Client;
using MLAH_Controller;
using REALTIMEVISUAL.Native.FederateInterface;
using static MLAH_Controller.CommonUtil;

namespace MLAH_Controller
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //this.Visibility = Visibility.Hidden; // 생성자에서 숨김 처리
            //this.Loaded += MainWindow_Loaded;

            // Loaded 대신 SourceInitialized 이벤트를 구독합니다.
            //this.SourceInitialized += MainWindow_SourceInitialized;

            // 클라이언트의 IP 주소와 포트 번호
            //var server = new UDPServer("127.0.0.1", 11000);
            //server.SendMessage(1,"notepad.exe");  // 예를 들어, 명령 1을 전송
            //server.Close();
            string strINIFilePath = AppDomain.CurrentDomain.BaseDirectory + "Config.ini";
            int.TryParse((CommonUtil.Readini_Click("Platform", "Platform", strINIFilePath)), out Controller_Platform);


            //this.Opacity = 0;
            //this.Loaded += (s, e) => { this.Opacity = 1; };

            //_demLoader = new DemLoader(@"38714.img");
            //string DEMFilePath = AppDomain.CurrentDomain.BaseDirectory + "38714.img";
            //_demLoader = new DemLoader(DEMFilePath);
            //string strINIFilePath = AppDomain.CurrentDomain.BaseDirectory + "Config.ini";
            //var alt = _demLoader.GetAltitude(38.13831,127.29790);

            if (Controller_Platform == 1)
            {
                Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate
                {
                    //var scenarioView = new View_ScenarioView();
                    //InitializeViews();
                    View_AbnormalZone_PopUp.SingletonInstance.Hide();
                    //View_Mornitoring_PopUp.SingletonInstance.Hide();
                    //View_Mornitoring_UDP_PopUp.SingletonInstance.Hide();
                    View_Config_PopUp.SingletonInstance.Hide();
                    View_ScenarioObject_PopUp.SingletonInstance.Hide();
                    //View_ScenarioView.SingletonInstance.Show();
                    //View_ScenarioView.SingletonInstance.Hide();
                    View_Complexity.SingletonInstance.Hide();
                    //View_MainView.SingletonInstance.Show();
                    //View_MainView.SingletonInstance.Topmost = true;
                    //View_MainView.SingletonInstance.Activate();
                    //View_MainView.SingletonInstance.Topmost = false;
                    //View_LAH_Analyzer.SingletonInstance.Show();
                    //View_Initial_Scenario_Info_PopUp.SingletonInstance.Show();
                }));

                var grpcModule = gRPCModule.SingletonInstance;
                grpcModule.StartServer();

                var udpmodule = UDPModule.SingletonInstance;
                //var udpModule = new UDPModule(receiverPort: 50001, senderPort: 50002);
                //var udpModule = new UDPModule();
            }
            //else if (Controller_Platform == 2)
            //{
            //    Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate
            //    {

            //        View_Config_PopUp.SingletonInstance.Hide();
            //        //View_Comp_ScenarioMainView.SingletonInstance.Show();
            //        //View_Comp_ScenarioMainView.SingletonInstance.Hide();
            //        View_Complexity.SingletonInstance.Hide();
            //        View_MainView.SingletonInstance.Show();
            //        View_MainView.SingletonInstance.Topmost = true;
            //        View_MainView.SingletonInstance.Activate();
            //        View_MainView.SingletonInstance.Topmost = false;
            //        //View_LAH_Analyzer.SingletonInstance.Show();
            //        //View_Initial_Scenario_Info_PopUp.SingletonInstance.Show();
            //    }));
            //}

            Model_ScenarioSequenceManager model_ScenarioSequenceManager = Model_ScenarioSequenceManager.SingletonInstance;


        }

        // 이벤트 핸들러 이름 변경
        private void MainWindow_SourceInitialized(object sender, System.EventArgs e)
        {
            // 기존의 사전 로딩 로직은 그대로 호출합니다.
            //_ = PreloadHeavyViewsAsync();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //_ = PreloadHeavyViewsAsync();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 무거운 View들을 비동기적으로, UI 스레드가 한가할 때 사전 로딩합니다.
        /// </summary>
        //private async Task PreloadHeavyViewsAsync()
        //{
        //    // Dispatcher를 통해 UI 스레드에서 작업을 실행하되, 우선순위를 낮춰 UI 멈춤을 방지합니다.
        //    // ApplicationIdle: 애플리케이션이 유휴 상태일 때 작업을 처리합니다.
        //    await Application.Current.Dispatcher.InvokeAsync(() =>
        //    {
        //        try
        //        {
        //            Debug.WriteLine("ScenarioView 사전 로딩을 시작합니다...");

        //            // 1. 싱글톤 인스턴스를 가져옵니다. 이 시점에 View가 처음 생성되고 InitializeComponent()가 호출됩니다.
        //            var scenarioView = View_ScenarioView.SingletonInstance;

        //            // 2. 사용자가 인지하지 못하도록 창을 준비시킵니다.
        //            scenarioView.ShowInTaskbar = false; // 작업 표시줄에 나타나지 않게 설정
        //            scenarioView.Opacity = 0.0;         // 완전히 투명하게 설정

        //            // 3. 창을 잠깐 보여줬다가 바로 숨깁니다.
        //            //    이 과정에서 WPF의 Measure/Arrange/Render 패스가 실행되어 렌더링이 강제됩니다.
        //            scenarioView.Show();
        //            scenarioView.Hide();

        //            // 4. 나중에 실제로 보여줄 때를 대비해 속성을 원래대로 복원합니다.
        //            scenarioView.Opacity = 1.0;
        //            scenarioView.ShowInTaskbar = true;

        //            Debug.WriteLine("ScenarioView 사전 로딩 완료.");
        //        }
        //        catch (Exception ex)
        //        {
        //            // 사전 로딩 중 발생할 수 있는 예외를 처리합니다.
        //            Debug.WriteLine($"오류: ScenarioView 사전 로딩 실패 - {ex.Message}");
        //        }

        //    }, DispatcherPriority.ApplicationIdle); // 가장 낮은 우선순위 중 하나
        //}

        /// <summary>
        /// 단위/종합 구분자 / 1:단위 2:종합
        /// </summary>
        public int Controller_Platform = 0;
        //private DemLoader _demLoader;


    }
}
