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
    public partial class View_LAH_Analyzer : Window
    {
        #region Singleton
        private static volatile View_LAH_Analyzer _SingletonInstance = null;
        private static readonly object syncRoot = new object();
        public static View_LAH_Analyzer SingletonInstance
        {
            get
            {
                if (_SingletonInstance == null)
                {
                    lock (syncRoot)
                    {
                        if (_SingletonInstance == null)
                        {
                            _SingletonInstance = new View_LAH_Analyzer();
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

        
        

        public View_LAH_Analyzer()
        {
            InitializeComponent();
            //CommonEvent.OnRequestFadeOut += Callback_OnRequestFadeOut;

            //test
            var row1 = new { Column1 = "AnalysisData1.xml", Column2 = "2025-08-09 18:22:13", Column3 = "분석 데이터 모델" };
            var row2 = new { Column1 = "AnalysisData2.dat", Column2 = "2025-08-09 11:12:43" , Column3 = "분석 데이터 모델" };
            var row3 = new { Column1 = "AnalysisData3.json", Column2 = "2025-04-02 11:12:43", Column3 = "분석 데이터 결과" };

            testgrid.Items.Add(row1);
            testgrid.Items.Add(row2);
            testgrid.Items.Add(row3);
        }

    
      
    }
}
