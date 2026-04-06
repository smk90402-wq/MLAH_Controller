using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Threading;
//using DevExpress.Images;

namespace MLAH_LogAnalyzer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static readonly string CrashLogPath = Path.Combine(
            AppContext.BaseDirectory, "crash_log.txt");

        public App()
        {
            // GPU 하드웨어 가속 강제 활성화
            RenderOptions.ProcessRenderMode = RenderMode.Default;

            // GPU 렌더링 티어 확인
            int tier = RenderCapability.Tier >> 16;
            System.Diagnostics.Debug.WriteLine($"[렌더링] GPU Tier: {tier} (2=full HW acceleration)");

            //ImageResourceRegistrator.Register();

            // UI 스레드 미처리 예외
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // 비-UI 스레드 미처리 예외
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Task 내 미관찰 예외
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash("DispatcherUnhandledException", e.Exception);
            MessageBox.Show(
                $"예기치 않은 오류가 발생했습니다.\n\n{e.Exception.Message}\n\n상세 로그: {CrashLogPath}",
                "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // 앱 종료 방지
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            LogCrash("UnhandledException", ex);
            MessageBox.Show(
                $"치명적 오류가 발생했습니다.\n\n{ex?.Message}\n\n상세 로그: {CrashLogPath}",
                "치명적 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogCrash("UnobservedTaskException", e.Exception);
            e.SetObserved(); // 앱 종료 방지
        }

        private static void LogCrash(string source, Exception? ex)
        {
            try
            {
                var log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\n" +
                          $"Message: {ex?.Message}\n" +
                          $"StackTrace:\n{ex?.ToString()}\n" +
                          $"{"".PadRight(80, '-')}\n";
                File.AppendAllText(CrashLogPath, log);
            }
            catch { /* 로그 기록 실패 시 무시 */ }
        }
    }

}
