using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DevExpress.Xpf.Core;

namespace MLAH_Controller
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class View_ScenarioObject_PopUp : ThemedWindow
    {

        #region Singleton
        private static volatile View_ScenarioObject_PopUp _SingletonInstance = null;
        private static readonly object syncRoot = new object();
        public static View_ScenarioObject_PopUp SingletonInstance
        {
            get
            {
                if (_SingletonInstance == null)
                {
                    lock (syncRoot)
                    {
                        if (_SingletonInstance == null)
                        {
                            _SingletonInstance = new View_ScenarioObject_PopUp();
                        }
                    }
                }
                return _SingletonInstance;
            }
            set
            {
                _SingletonInstance = value;
            }
        }
        #endregion Singleton

        public View_ScenarioObject_PopUp()
        {
            InitializeComponent();

            // Singleton으로 Show/Hide만 반복하므로, 가시성 변경 이벤트를 사용
            this.IsVisibleChanged += View_ScenarioObject_PopUp_IsVisibleChanged;
        }

        private void View_ScenarioObject_PopUp_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // 창이 보여질 때 (Show)
            if ((bool)e.NewValue == true)
            {
                // 중복 등록 방지를 위해 먼저 제거 후 등록
                InputManager.Current.PreProcessInput -= OnPreProcessInput;
                InputManager.Current.PreProcessInput += OnPreProcessInput;
            }
            // 창이 숨겨질 때 (Hide)
            else
            {
                InputManager.Current.PreProcessInput -= OnPreProcessInput;
            }
        }

        // 앱 전체의 입력 이벤트를 가로채는 핸들러
        private void OnPreProcessInput(object sender, NotifyInputEventArgs e)
        {
            // 1. 키보드 입력 이벤트인지 확인
            if (e.StagingItem.Input is KeyEventArgs args)
            {
                // 2. 키 다운(누름) 이벤트이고, 'A' 키인지 확인
                if (e.StagingItem.Input.RoutedEvent == Keyboard.KeyDownEvent && args.Key == Key.A)
                {
                    // 3. 입력 필드(TextBox 등)에 포커스가 없을 때만 동작하도록 방어 (선택 사항)
                    // 만약 TextBox 입력 중에도 'A'를 눌러 토글하고 싶다면 이 블록을 제거하세요.
                    if (Keyboard.FocusedElement is System.Windows.Controls.TextBox)
                    {
                        return;
                    }

                    // 4. ViewModel의 토글 상태 변경
                    var vm = ViewModel_ScenarioObject_PopUp.SingletonInstance;

                    // 체크 상태 반전 (Toggle)
                    vm.POSSelectChecked = !vm.POSSelectChecked;

                    // (옵션) 'a' 키가 다른 곳(메인화면 등)에 영향을 주지 않게 하려면 이벤트를 여기서 끝냄
                    // args.Handled = true; 
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 마우스 왼쪽 버튼이 눌렸는지 확인
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // 윈도우 드래그 시작
                this.DragMove();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var vm = DataContext as ViewModel_ScenarioObject_PopUp;
            if (vm == null)
                return;

            if (e.Key == Key.Enter)
            {
                if (vm.ConfirmCommand.CanExecute(this))
                    vm.ConfirmCommand.Execute(this);

                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                if (vm.CancelCommand.CanExecute(this))
                    vm.CancelCommand.Execute(this);

                e.Handled = true;
            }
        }
    }
}
