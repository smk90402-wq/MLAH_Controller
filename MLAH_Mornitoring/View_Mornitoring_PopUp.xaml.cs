using DevExpress.Xpf.Grid;
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

namespace MLAH_Mornitoring
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

            // 1. [핵심] DataContext를 싱글톤 인스턴스로 강제 연결
            var vm = ViewModel_Mornitoring_PopUp.SingletonInstance;
            this.DataContext = vm;

            treeListView.TreeDerivationMode = TreeDerivationMode.ChildNodesSelector;
            treeListView.ChildNodesSelector = new MessageNodeChildNodesSelector();

            // 2. [핵심] 이벤트 구독을 생성자에서 즉시 수행 (Loaded 보다 확실함)
            vm.PropertyChanged += ViewModel_PropertyChanged;
            vm.OnBatchUpdateStart += () => MessageGridControl.BeginDataUpdate();
            vm.OnBatchUpdateEnd += () => MessageGridControl.EndDataUpdate();
            //this.DataContext = new ViewModel_Mornitoring_PopUp();

            
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
            //var vm = ViewModel_Mornitoring_PopUp.SingletonInstance;

            //// 이벤트 연결
            //vm.OnBatchUpdateStart += () => MessageGridControl.BeginDataUpdate();
            //vm.OnBatchUpdateEnd += () => MessageGridControl.EndDataUpdate();
            //ViewModel_Mornitoring_PopUp.SingletonInstance.PropertyChanged += ViewModel_PropertyChanged;
            //this.Loaded -= View_Mornitoring_PopUp_Loaded;
            await SignalReadyToParent();
        }

        private async Task SignalReadyToParent()
        {
            string readyPipeName = "MornitoringAppReadyPipe";
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

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // DetailNodes가 갱신되었거나, OpenTree 옵션이 변경된 경우
            if (e.PropertyName == nameof(ViewModel_Mornitoring_PopUp.DetailNodes) ||
                e.PropertyName == nameof(ViewModel_Mornitoring_PopUp.IsOpenTreeChecked))
            {
                var vm = ViewModel_Mornitoring_PopUp.SingletonInstance;

                // UI 스레드에서 안전하게 실행
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // 데이터 갱신 알림 (트리가 멍때리는 것 방지)
                    treeListView.DataControl.RefreshData();

                    if (vm.IsOpenTreeChecked)
                    {
                        treeListView.ExpandAllNodes();
                    }
                    else
                    {
                        treeListView.CollapseAllNodes();
                        // (옵션) 루트 노드만 펼치기
                        if (treeListView.Nodes.Count > 0)
                            treeListView.ExpandNode(treeListView.Nodes[0].RowHandle);
                    }
                }));
            }
        }
    }
}
