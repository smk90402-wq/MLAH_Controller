using System.Windows;
using System.Windows.Input;

namespace MLAH_Controller
{
    /// <summary>
    /// View_PopUp_Dialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class View_PopUp_Dialog : Window
    {
        public View_PopUp_Dialog()
        {
            InitializeComponent();
        }

        private void ViewModel_DialogBase_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 상단 여백 등 빈 공간 클릭 시 드래그 가능하게
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void YesEvent(object sender, RoutedEventArgs e)
        {
            // ShowDialog()로 열었을 경우 DialogResult 값을 설정하면 창이 자동으로 닫힙니다.
            this.DialogResult = true;
        }

        private void NoEvent(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        // ★ 신규 추가된 단축키 연동 로직
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Enter 키 누르면 '예'
            if (e.Key == Key.Enter)
            {
                YesEvent(YesButton, null);
                e.Handled = true;
            }
            // ESC 키 누르면 '아니오'
            else if (e.Key == Key.Escape)
            {
                NoEvent(NoButton, null);
                e.Handled = true;
            }
        }
    }
}