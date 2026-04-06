using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using DevExpress.Xpf.Core;


namespace MLAH_Controller
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class View_ScenarioView : UserControl
    {
        // 1. Lazy<T>를 사용하여 스레드에 안전하고 지연된 초기화를 보장합니다.
        private static readonly Lazy<View_ScenarioView> lazyInstance =
            new Lazy<View_ScenarioView>(() => new View_ScenarioView());

        // 2. 외부에서 인스턴스를 가져갈 유일한 통로입니다.
        public static View_ScenarioView SingletonInstance => lazyInstance.Value;

        // 3. 생성자를 'private'으로 변경하여 외부에서 'new' 키워드로 생성하는 것을 원천 차단합니다.
        public View_ScenarioView()
        {
            InitializeComponent();
            LayoutRoot.DataContext = ViewModel_ScenarioView.SingletonInstance;
            //this.KeyDown += MainWindow_KeyDown;
            //this.Opacity = 0;
            //this.Loaded += (s, e) => { this.Opacity = 1; };
        }



        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // 현재 눌린 키가 올바른 순서의 키인지 확인합니다.
            if (e.Key == _keySequence[_currentIndex])
            {
                // 올바른 키가 눌렸으면, 다음 키로 인덱스를 이동합니다.
                _currentIndex++;

                // 모든 키가 올바른 순서대로 눌렸는지 확인합니다.
                if (_currentIndex >= _keySequence.Count)
                {
                    // 여기서 대상 버튼의 Visibility를 Visible로 변경합니다.
                    // 예: targetButton.Visibility = Visibility.Visible;

                    // 키 입력 순서를 리셋합니다.
                    _currentIndex = 0;
               
                    
                }
            }
            else
            {
                // 잘못된 키가 눌렸으면, 인덱스를 리셋합니다.
                _currentIndex = 0;
            }
        }

        private readonly List<Key> _keySequence = new List<Key> { Key.F7, Key.F8,Key.F8, Key.F7 };
        private int _currentIndex = 0;
 
    }
}
