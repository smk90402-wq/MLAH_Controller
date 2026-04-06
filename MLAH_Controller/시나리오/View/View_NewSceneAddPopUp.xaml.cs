using System.Windows;
using System.Windows.Input;

namespace MLAH_Controller
{
    public partial class View_NewSceneAddPopUp : Window
    {
        public View_NewSceneAddPopUp()
        {
            InitializeComponent();

            // 팝업이 뜨자마자 파일명 TextBox에 키보드 포커스를 줍니다.
            // 사용자가 마우스 클릭 없이 바로 타이핑을 시작할 수 있습니다.
            this.Loaded += (s, e) => FileNameBox.Focus();
        }

        private void ConfirmEvent(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            // 뷰모델의 SceneFileName에 입력값 갱신
            ViewModel_ScenarioView.SingletonInstance.SceneFileName = FileNameBox.Text;
        }

        // ★ 신규 추가된 취소 버튼 동작
        private void CancelEvent(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ViewModel_DialogBase_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 상단 여백이나 빈 공간 클릭 시 드래그 가능하게
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Enter 키 누르면 확인(저장) 처리
            if (e.Key == Key.Enter)
            {
                ConfirmEvent(ConfirmButton, e);
                e.Handled = true;
            }
            // ★ ESC 키 누르면 창 닫기(취소) 처리
            else if (e.Key == Key.Escape)
            {
                CancelEvent(CancelButton, e);
                e.Handled = true;
            }
        }
    }
}