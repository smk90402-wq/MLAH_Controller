using MLAH_Controller;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MLAH_Controller
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class View_MainView : UserControl
    {
        #region Singleton
        private static volatile View_MainView _SingletonInstance = null;
        private static readonly object syncRoot = new object();
        public static View_MainView SingletonInstance
        {
            get
            {
                if (_SingletonInstance == null)
                {
                    lock (syncRoot)
                    {
                        if (_SingletonInstance == null)
                        {
                            _SingletonInstance = new View_MainView();
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

        public View_MainView()
        {
            InitializeComponent();
            LayoutRoot.DataContext = ViewModel_MainView.SingletonInstance;
            //this.DataContext = ViewModel_MainView.SingletonInstance;
            //this.DataContext = new ViewModel_MainView();
            // 그 다음에 DataContext를 코드에서 직접 연결합니다.
            //this.DataContext = ViewModel_MainView.SingletonInstance;

            //CommonEvent.OnRequestFadeOut += Callback_OnRequestFadeOut;
        }

        private void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // 팝업 생성 및 표시 로직 (위의 OnUnitHealthDropped 메서드 내용과 동일)
            var warningPopup = new View_AlertPopUp(30);
            //warningPopup.Description.Text = $"{unit.Name}(ID:{unit.ID}) 피격";
            warningPopup.Description.Text = $"경보창 테스트";
            //warningPopup.Reason.Text = $"현재 체력: {unit.Health:F0} / 100";
            warningPopup.Show();
        }

        //private void Callback_OnRequestFadeOut(ViewName viewName)
        //{
        //    //생성자 추가
        //    //CommonEvent.OnRequestFadeOut += ViewModel_OnRequestFadeOut;
        //    var fadeOutAnimation = new DoubleAnimation
        //    {
        //        From = 1.0,
        //        To = 0.0,
        //        Duration = new Duration(TimeSpan.FromSeconds(0.5)),
        //        EasingFunction = new QuadraticEase()
        //    };
        //    fadeOutAnimation.Completed += (s, a) =>
        //    {
        //        this.Hide();
        //        CommonUtil.ShowFadeWindow(viewName);
        //    };
        //    this.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
        //}

    }
}
