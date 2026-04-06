using System.ComponentModel;
using System.Windows;
using DevExpress.Spreadsheet;
using DevExpress.Xpf.Core;

namespace MLAH_Controller
{
    public partial class ShellView : DXWindow
    {
        public ShellView()
        {
            InitializeComponent();
            //this.DataContext = new ShellViewModel();
            this.Loaded += ShellView_Loaded;
            this.Closing += ShellView_Closing;
        }

        /// <summary>
        /// ShellView가 화면에 완전히 로드된 후 딱 한 번 호출됩니다.
        /// </summary>
        private void ShellView_Loaded(object sender, RoutedEventArgs e)
        {
            //UDPModule.SingletonInstance.StartListening();
            // 이 시점이야말로 모든 것이 준비된, 백그라운드 작업을 시작하기에 가장 안전한 시점입니다.
            //ViewModel_Unit_Map.SingletonInstance.StartFocusSquareLoop();
            //ViewModel_Unit_Map.SingletonInstance.StartFourCornerLoop();
            ViewModel_Unit_Map.SingletonInstance.StartSelectedObjectVisualsLoop();

            //절차서롤백
            //ViewModel_MainView.SingletonInstance.Initialize();

            //Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate
            //{
                //ViewModel_Unit_Map.SingletonInstance.StartFocusSquareLoop();
                //ViewModel_MainView.SingletonInstance.Initialize();
            //}));

        }

        private async void ShellView_Closing(object sender, CancelEventArgs e)
        {
            // 애플리케이션이 닫힐 때 ViewModel_Unit_Map의 백그라운드 루프를 명시적으로 중단시킵니다.
            //ViewModel_Unit_Map.SingletonInstance.StopFocusSquareLoop();
            //Application.Current.Shutdown();

            // 기본 종료 이벤트를 일단 취소시켜, 우리가 직접 종료 절차를 제어할 시간을 확보합니다.
            e.Cancel = true;

            // 중복 실행을 막기 위해 버튼 등을 비활성화할 수 있습니다.
            this.IsEnabled = false;

            // 중앙 집중화된 애플리케이션 종료 메서드를 호출합니다.
            if (Application.Current is App app)
            {
                await app.ShutdownApplicationAsync();
            }
        }

        
    }
}