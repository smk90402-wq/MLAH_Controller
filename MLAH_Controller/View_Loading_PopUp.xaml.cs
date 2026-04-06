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
using System.Windows.Shapes;

namespace MLAH_Controller
{
    ///  <summary>..
    ///  UserControl1.xaml에 대한 상호 작용 논리..
    ///  </summary>..
    public partial class View_Loading_PopUp
    {

        public View_Loading_PopUp()
        {
            InitializeComponent();

            //ConfirmButton.Content = "확인";

            TimeSpan TimerInterval = TimeSpan.FromSeconds(1);
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = TimerInterval;
        }

        public View_Loading_PopUp(double TimeParameter)
        {
            InitializeComponent();

            SetRemainTime = TimeParameter;
            //ConfirmButton.Content = "확인";

            TimeSpan TimerInterval = TimeSpan.FromSeconds(1);
            dispatcherTimer.Tick += dispatcherTimer_Tick_TimeParameter;
            dispatcherTimer.Interval = TimerInterval;
        }

        private void ConfirmEvent(object sender, RoutedEventArgs e)
        {
            base.Hide();
            base.Close();
        }

        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        DateTime startTime = DateTime.Now;


        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan ElapsedTime = DateTime.Now - startTime;
            int RemainTime = 30;
            RemainTime = 30 - ElapsedTime.Seconds;
            //ConfirmButton.Content = string.Format("확인({0}초 후 닫힘)", RemainTime);
            if (ElapsedTime >= TimeSpan.FromSeconds(30))
            {
                ((System.Windows.Threading.DispatcherTimer)sender).Stop();
                base.Hide();

                //Close보다 먼저 Dispose가 와야 GC에서 명시적으로 정리가 가능
                base.Close();
            }
        }

        private void dispatcherTimer_Tick_TimeParameter(object sender, EventArgs e)
        {
            TimeSpan ElapsedTime = DateTime.Now - startTime;
            double RemainTime = SetRemainTime;
            RemainTime = SetRemainTime - ElapsedTime.Seconds;
            //ConfirmButton.Content = string.Format("확인({0:F0}초 후 닫힘)", RemainTime);
            if (ElapsedTime >= TimeSpan.FromSeconds(SetRemainTime))
            {
                ((System.Windows.Threading.DispatcherTimer)sender).Stop();
                base.Hide();

                //Close보다 먼저 Dispose가 와야 GC에서 명시적으로 정리가 가능
                base.Close();

                //GDI 체크하고 안되면 Dispose 투입
            }
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Visibility NewVisibility = e.NewValue as Visibility? ?? Visibility.Visible;

            if (NewVisibility == Visibility.Hidden || NewVisibility == Visibility.Collapsed)
            {
                startTime = DateTime.Now;
                dispatcherTimer.Stop();
            }
            else
            {
                startTime = DateTime.Now;
                dispatcherTimer.Start();
            }
        }

        private double SetRemainTime = 30;

        private void ViewModel_DialogBase_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                base.DragMove();
            }
        }
    }
}

