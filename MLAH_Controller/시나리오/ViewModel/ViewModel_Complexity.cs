
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System;
using MLAH_Controller;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Linq;


namespace MLAH_Controller
{
    public class ViewModel_Complexity : CommonBase
    {
        #region Singleton
        static ViewModel_Complexity _ViewModel_Complexity = null;
        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static ViewModel_Complexity SingletonInstance
        {
            get
            {
                if (_ViewModel_Complexity == null)
                {
                    _ViewModel_Complexity = new ViewModel_Complexity();
                }
                return _ViewModel_Complexity;
            }
        }

        #endregion Singleton

        #region 생성자 & 콜백
        public ViewModel_Complexity()
        {

            TimeSpan TimerInterval = TimeSpan.FromSeconds(1);
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = TimerInterval;
            //dispatcherTimer.Start();

        }

        #endregion 생성자 & 콜백

        private int _HelicopterCounts = 0;
        public int HelicopterCounts
        {
            get
            {
                return _HelicopterCounts;
            }
            set
            {
                _HelicopterCounts= value;
                OnPropertyChanged("HelicopterCounts");
            }
        }

        private int _UAVCounts = 0;
        public int UAVCounts
        {
            get
            {
                return _UAVCounts;
            }
            set
            {
                _UAVCounts = value;
                OnPropertyChanged("UAVCounts");
            }
        }

        private int _CoBaseMissionCounts = 0;
        public int CoBaseMissionCounts
        {
            get
            {
                return _CoBaseMissionCounts;
            }
            set
            {
                _CoBaseMissionCounts = value;
                OnPropertyChanged("CoBaseMissionCounts");
            }
        }

        private int _IndividualMissionCounts = 0;
        public int IndividualMissionCounts
        {
            get
            {
                return _IndividualMissionCounts;
            }
            set
            {
                _IndividualMissionCounts = value;
                OnPropertyChanged("IndividualMissionCounts");
            }
        }

        private int _ComplexityNum = 0;
        public int ComplexityNum
        {
            get
            {
                return _ComplexityNum;
            }
            set
            {
                _ComplexityNum = value;
                OnPropertyChanged("ComplexityNum");
            }
        }
        

        private System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            int HeliCounts = 0;
            int UAVCount = 0;
            int CBMCounts = 0;
            int IMCounts = 0;
            //var Copy_ScenarioData = ViewModel_ScenarioView.SingletonInstance.TempSceneModel;
            var Copy_ScenarioObjectData = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList;
            if(Copy_ScenarioObjectData != null)
            {
                foreach (var SceneObject in Copy_ScenarioObjectData)
                {
                    //if (SceneObject.Type == 1)
                    //{
                    //    HeliCounts++;
                    //}
                    //if (SceneObject.ObjectType == 2)
                    //{
                    //    UAVCount++;
                    //}
                    //if (SceneObject.IsLeader == true)
                    //{
                    //    CBMCounts = SceneObject.coBaseMissions.Count();
                    //}
                    //IMCounts += SceneObject.IndividualMissions.Count();
                }
                //HelicopterCounts = HeliCounts;
                //UAVCounts = UAVCount;
                //CoBaseMissionCounts = CBMCounts;
                //IndividualMissionCounts = IMCounts;
                //var Comp = (HelicopterCounts + UAVCounts) * (CoBaseMissionCounts + IndividualMissionCounts);
                //ViewModel_ScenarioView.SingletonInstance.ScenarioComplexity = Comp;
                //ComplexityNum = Comp;
            }
         
        }

    }


}
