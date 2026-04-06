//using DevExpress.ExpressApp.Utils;
using DevExpress.Xpf.Core; // ISplashScreen
using System.Windows;
using System.Windows.Controls;

namespace MLAH_LogAnalyzer
{
    
    public partial class SimpleProgressWindow : ThemedWindow
    {
        public SimpleProgressWindow()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 다른 스레드에서 이 메서드를 호출하여 UI를 안전하게 업데이트
        /// </summary>
        public void UpdateProgress(int percentage, string message)
        {
            // 이 Window가 생성된 스레드가 아닌 다른 스레드(예: 메인 스레드)에서 호출될 경우
            if (!this.Dispatcher.CheckAccess())
            {
                // UI 스레드의 큐에 이 작업을 넣습니다.
                this.Dispatcher.Invoke(() => UpdateProgress(percentage, message));
                return;
            }

            // UI 스레드에서 UI 컨트롤에 접근
            progressBar.Value = percentage;
            statusText.Text = message;
        }

    }
}