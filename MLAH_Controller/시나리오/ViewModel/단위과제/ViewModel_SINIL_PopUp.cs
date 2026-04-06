
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System;
using MLAH_Controller;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Animation;
using System.Windows.Controls;
//using GMap.NET;
using System.Security.Policy;
using Google.Protobuf;
using MLAHInterop;



namespace MLAH_Controller
{
    public class ViewModel_SINIL_PopUp : CommonBase
    {
        #region Singleton
        static ViewModel_SINIL_PopUp _ViewModel_SINIL_PopUp = null;
        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static ViewModel_SINIL_PopUp SingletonInstance
        {
            get
            {
                if (_ViewModel_SINIL_PopUp == null)
                {
                    _ViewModel_SINIL_PopUp = new ViewModel_SINIL_PopUp();
                }
                return _ViewModel_SINIL_PopUp;
            }
        }

        #endregion Singleton

        #region 생성자 & 콜백
        public ViewModel_SINIL_PopUp()
        {
            MUMTPlayCommand = new AsyncRelayCommand(MUMTPlayCommandAction);
            MUMTStopCommand = new RelayCommand(MUMTStopCommandAction);
            CloseCommand = new RelayCommand(CloseCommandAction);
        }

        #endregion 생성자 & 콜백

        public bool IsMUMTPlay = false;

        private Visibility _ObjectNumberVisibility = Visibility.Visible;
        public Visibility ObjectNumberVisibility
        {

            get { return _ObjectNumberVisibility; }
            set
            {
                _ObjectNumberVisibility = value;
                OnPropertyChanged("ObjectNumberVisibility");
            }
        }

        private bool _ObjectTypeComboBoxEnable = true;
        public bool ObjectTypeComboBoxEnable
        {

            get { return _ObjectTypeComboBoxEnable; }
            set
            {
                _ObjectTypeComboBoxEnable = value;
                OnPropertyChanged("ObjectTypeComboBoxEnable");
            }
        }

        public AsyncRelayCommand MUMTPlayCommand { get; set; }
        /// <summary>
        /// 협업기저임무 시작 버튼
        /// </summary>
        public async Task MUMTPlayCommandAction(object param)
        {
            IsMUMTPlay = true;

            var pop_error = new View_PopUp(5);
            pop_error.Description.Text = "명령 수신";
            pop_error.Reason.Text = "협업기저임무 수행 시작";
            pop_error.Show();

            foreach (var item in ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList)
            {
                string name = "";
                if(item.Type == 3)
                {
                    name = "Helicopter";
                }
                else if (item.Type == 1)
                {
                    name = "UAV";
                }
                else
                {
                    name = "Target";
                }

                ScenarioMissionRequest temp = new ScenarioMissionRequest
                {
                    MissionID = 0,
                    WaypointID = 0,
                    //Name = name,
                    Id = item.ID,
                    Type = name,
                };
                ContextInfo tempcontext = new ContextInfo();
                //await Model_ScenarioSequenceManager.SingletonInstance.Callback_OnScenarioMissionRequest(temp, tempcontext);
            }
            
        }

        public RelayCommand MUMTStopCommand { get; set; }
        /// <summary>
        /// 협업기저임무 중지 버튼
        /// </summary>
        public void MUMTStopCommandAction(object param)
        {
            IsMUMTPlay = false;
        }


        public RelayCommand CloseCommand { get; set; }
        /// <summary>
        /// 닫기 버튼
        /// </summary>
        public void CloseCommandAction(object param)
        {
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new System.Windows.Duration(TimeSpan.FromSeconds(0.5))
            };
            fadeOutAnimation.Completed += (s, a) =>
            {
                View_SINIL_PopUp.SingletonInstance.Hide();
            };

            View_SINIL_PopUp.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
        }


       



    }


}
