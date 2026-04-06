using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MLAH_Mornitoring_UDP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            _pipeReceiver = new NamedPipeReceiver();
            _pipeReceiver.Start();

            var Monitor = new View_Mornitoring_PopUp
            {
                DataContext = new ViewModel_Mornitoring_PopUp()
            };

            //모니터링 창이 닫힐 때 앱 전체를 종료시키는 이벤트 연결
            Monitor.Closed += (s, e) =>
            {
                // 파이프 정리 등 필요한 종료 로직이 있다면 여기서 호출
                Application.Current.Shutdown();
                System.Environment.Exit(0); // 확실하게 프로세스 사살
            };

            Monitor.Show();

            this.Hide();
        }
        private NamedPipeReceiver _pipeReceiver;
    }
}