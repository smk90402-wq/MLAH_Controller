using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
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
//using DevExpress.Xpf.Grid;

namespace MLAH_Mornitoring_UDP
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class View_Mornitoring_PopUp : Window
    {

        #region Singleton

       // private static readonly Lazy<View_Mornitoring_PopUp> _lazyInstance =
       //new Lazy<View_Mornitoring_PopUp>(() => new View_Mornitoring_PopUp());
       // public static View_Mornitoring_PopUp SingletonInstance => _lazyInstance.Value;

        //private static volatile View_Mornitoring_PopUp _SingletonInstance = null;
        //private static readonly object syncRoot = new object();
        //public static View_Mornitoring_PopUp SingletonInstance
        //{
        //    get
        //    {
        //        if (_SingletonInstance == null)
        //        {
        //            lock (syncRoot)
        //            {
        //                if (_SingletonInstance == null)
        //                {
        //                    _SingletonInstance = new View_Mornitoring_PopUp();
        //                }
        //            }
        //        }
        //        return _SingletonInstance;
        //    }
        //    set
        //    {
        //        _SingletonInstance = value;
        //    }
        //}
        #endregion Singleton

        public View_Mornitoring_PopUp()
        {
            InitializeComponent();
            this.DataContext = ViewModel_Mornitoring_PopUp.SingletonInstance;
        }

        // ViewModel에서 접근할 수 있도록 public 속성 추가
        //public GridControl MessageGridControl => this.FindName("MessageGridControl") as GridControl;

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 마우스 왼쪽 버튼이 눌렸는지 확인
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // 윈도우 드래그 시작
                this.DragMove();
            }
        }
      
        private async void View_Mornitoring_PopUp_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= View_Mornitoring_PopUp_Loaded;
            await SignalReadyToParent();
        }

        private async Task SignalReadyToParent()
        {
            string readyPipeName = "UDPMornitoringAppReadyPipe";
            try
            {
                using (var pipeClient = new NamedPipeClientStream(".", readyPipeName, PipeDirection.Out, PipeOptions.Asynchronous))
                {
                    await pipeClient.ConnectAsync(5000); // 5초 내 연결 시도
                    if (pipeClient.IsConnected)
                    {
                        using (var writer = new StreamWriter(pipeClient, Encoding.UTF8))
                        {
                            await writer.WriteAsync("READY");
                            await writer.FlushAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"Could not send READY signal: {ex.Message}");
            }
        }
    }
}
