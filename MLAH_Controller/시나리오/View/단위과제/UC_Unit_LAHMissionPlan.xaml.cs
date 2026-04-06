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
    public partial class UC_Unit_LAHMissionPlan : UserControl
    {

        #region Singleton
        private static volatile UC_Unit_LAHMissionPlan _SingletonInstance = null;
        private static readonly object syncRoot = new object();
        public static UC_Unit_LAHMissionPlan SingletonInstance
        {
            get
            {
                if (_SingletonInstance == null)
                {
                    lock (syncRoot)
                    {
                        if (_SingletonInstance == null)
                        {
                            _SingletonInstance = new UC_Unit_LAHMissionPlan();
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

        public UC_Unit_LAHMissionPlan()
        {
            InitializeComponent();
        }

        
    }
}
