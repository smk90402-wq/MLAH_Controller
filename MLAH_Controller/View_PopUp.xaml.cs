using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MLAH_Controller
{
    /// <summary>
    /// View_PopUp.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class View_PopUp : Window
    {
        private DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private DateTime startTime;
        private double countdownSeconds = 30; // 기본 카운트다운 시간

        // 기본 생성자: 30초 카운트다운
        public View_PopUp()
        {
            InitializeComponent();
            InitializeTimer();
        }

        // 시간을 파라미터로 받는 생성자
        public View_PopUp(double seconds)
        {
            InitializeComponent();
            this.countdownSeconds = seconds;
            InitializeTimer();
        }

        public View_PopUp(double seconds,int width)
        {
            InitializeComponent();
            this.countdownSeconds = seconds;
            this.Width = width;
            InitializeTimer();
        }

        //기본 200 width //기본 100 height
        public View_PopUp(double seconds, int width, int height)
        {
            InitializeComponent();
            this.countdownSeconds = seconds;
            this.Width = width;
            this.Height = height;
            InitializeTimer();
        }

        // 타이머 초기화 로직 (중복 제거)
        private void InitializeTimer()
        {
            ConfirmButton.Content = "확인";
            dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            dispatcherTimer.Tick += DispatcherTimer_Tick;
        }

        // 타이머 Tick 이벤트 핸들러 (통합)
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            UpdateTimerText(); // UI 업데이트

            TimeSpan elapsedTime = DateTime.Now - startTime;
            if (elapsedTime.TotalSeconds >= countdownSeconds)
            {
                dispatcherTimer.Stop();
                this.Close(); // Hide()는 Close()에 의해 내부적으로 처리되므로 생략 가능
            }
        }

        // 남은 시간을 계산하고 버튼 텍스트를 업데이트하는 메소드 (신규)
        private void UpdateTimerText()
        {
            TimeSpan elapsedTime = DateTime.Now - startTime;
            // TotalSeconds를 사용해야 60초 이상일 때도 정확히 계산됩니다.
            // Math.Ceiling을 사용해 남은 시간을 올림 처리하여 사용자 경험을 개선합니다 (예: 4.1초 남아도 5초로 표시).
            double remainingSeconds = countdownSeconds - elapsedTime.TotalSeconds;

            if (remainingSeconds > 0)
            {
                ConfirmButton.Content = string.Format("확인({0}초 후 닫힘)", Math.Ceiling(remainingSeconds));
            }
            else
            {
                ConfirmButton.Content = "확인(0초 후 닫힘)";
            }
        }

        // 창의 Visibility가 변경될 때 호출
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue) // 창이 보여질 때 (true)
            {
                startTime = DateTime.Now;

                // ★★★ 핵심: 타이머 시작과 동시에 UI를 즉시 업데이트
                UpdateTimerText();

                dispatcherTimer.Start();
            }
            else // 창이 숨겨질 때
            {
                dispatcherTimer.Stop();
            }
        }

        // 확인 버튼 클릭 이벤트
        private void ConfirmEvent(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // 창 드래그 이동
        private void ViewModel_DialogBase_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}