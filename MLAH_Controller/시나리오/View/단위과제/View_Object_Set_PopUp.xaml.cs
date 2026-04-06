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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MLAH_Controller
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class View_Object_Set_PopUp : Window
    {

        #region Singleton
        private static readonly Lazy<View_Object_Set_PopUp> _lazyInstance =
               new Lazy<View_Object_Set_PopUp>(() => new View_Object_Set_PopUp());
        public static View_Object_Set_PopUp SingletonInstance => _lazyInstance.Value;

        
        #endregion Singleton

        public View_Object_Set_PopUp()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 마우스 왼쪽 버튼이 눌렸는지 확인
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // 윈도우 드래그 시작
                this.DragMove();
            }
        }
    }
}
