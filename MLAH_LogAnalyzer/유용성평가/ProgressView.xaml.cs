//using DevExpress.ExpressApp.Utils;
using DevExpress.Xpf.Core; // ISplashScreen
using System.Windows.Controls;

namespace MLAH_LogAnalyzer
{
    
    public partial class ProgressView : UserControl, ISplashScreen
    {
        public ProgressView()
        {
            InitializeComponent();
        }

        // 메인 스레드에서 DXSplashScreen.SetState(state)를 호출하면 이 메서드가 실행됨
        public void Progress(object state)
        {
            if (state is ProgressState progressState)
            {
                // UI 업데이트 (이 코드는 Splash 스레드에서 실행됨)
                StatusText.Text = progressState.Message;
                ProgressBar.EditValue = progressState.Percentage;
            }
        }

        public void Progress(double value)
        {
            ProgressBar.EditValue = value;
        }

        // [!] 3. (bool isIndeterminate) - (누락된 메서드)
        // 인터페이스 구현을 위해 추가해야 합니다.
        public void SetProgressState(bool isIndeterminate)
        {
            this.IsIndeterminate = isIndeterminate;
            // ProgressBarEdit는 IsIndeterminate 시각적 상태가 없지만, 
            // 인터페이스 계약을 지키기 위해 속성 설정은 필요합니다.
        }

        // 인터페이스 구현 (DXSplashScreen.Close() 호출 시 실행됨)
        public void CloseSplashScreen()
        {
            this.IsDone = true;
        }

        public bool IsDone { get; private set; }
        public bool IsIndeterminate { get; set; }
    }
}