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
    public partial class View_Complexity : Window
    {

        #region Singleton
        private static volatile View_Complexity _SingletonInstance = null;
        private static readonly object syncRoot = new object();
        public static View_Complexity SingletonInstance
        {
            get
            {
                if (_SingletonInstance == null)
                {
                    lock (syncRoot)
                    {
                        if (_SingletonInstance == null)
                        {
                            _SingletonInstance = new View_Complexity();
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

        public View_Complexity()
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
