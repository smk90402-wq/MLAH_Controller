
using MLAH_Controller;
using MLAHInterop;
using REALTIMEVISUAL.Native.FederateInterface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;


namespace MLAH_Controller
{
    public class ViewModel_BattlefieldEnv_PopUp : CommonBase
    {
        #region Singleton
        static ViewModel_BattlefieldEnv_PopUp _ViewModel_BattlefieldEnv_PopUp = null;
        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static ViewModel_BattlefieldEnv_PopUp SingletonInstance
        {
            get
            {
                if (_ViewModel_BattlefieldEnv_PopUp == null)
                {
                    _ViewModel_BattlefieldEnv_PopUp = new ViewModel_BattlefieldEnv_PopUp();
                }
                return _ViewModel_BattlefieldEnv_PopUp;
            }
        }

        #endregion Singleton

        #region 생성자 & 콜백
        public ViewModel_BattlefieldEnv_PopUp()
        {
            ApplyCommand = new RelayCommand(ApplyCommandAction);
            CloseCommand = new RelayCommand(CloseCommandAction);

            BattlefieldSetTimeHour = BattlefieldSetTime.Hour;
            BattlefieldSetTimeMin = BattlefieldSetTime.Minute;
            BattlefieldSetTimeSec = BattlefieldSetTime.Second;

        }

        #endregion 생성자 & 콜백


        private int _BattlefieldArea = 1;
        public int BattlefieldArea
        {
            get
            {
                return _BattlefieldArea;
            }
            set
            {
                _BattlefieldArea = value;
                OnPropertyChanged("BattlefieldArea");
            }
        }

        private DateTime _BattlefieldSetTime = DateTime.Now;
        public DateTime BattlefieldSetTime
        {
            get
            {
                return _BattlefieldSetTime;
            }
            set
            {
                _BattlefieldSetTime = value;
                OnPropertyChanged("BattlefieldSetTime");
            }
        }

        private int _BattlefieldSetTimeHour = 0;
        public int BattlefieldSetTimeHour
        {
            get
            {
                return _BattlefieldSetTimeHour;
            }
            set
            {
                _BattlefieldSetTimeHour = value;
                OnPropertyChanged("BattlefieldSetTimeHour");
            }
        }

        private int _BattlefieldSetTimeMin = 0;
        public int BattlefieldSetTimeMin
        {
            get
            {
                return _BattlefieldSetTimeMin;
            }
            set
            {
                _BattlefieldSetTimeMin = value;
                OnPropertyChanged("BattlefieldSetTimeMin");
            }
        }

        private int _BattlefieldSetTimeSec = 0;
        public int BattlefieldSetTimeSec
        {
            get
            {
                return _BattlefieldSetTimeSec;
            }
            set
            {
                _BattlefieldSetTimeSec = value;
                OnPropertyChanged("BattlefieldSetTimeSec");
            }
        }

        private int _BattlefieldSeason = 1;
        public int BattlefieldSeason
        {
            get
            {
                return _BattlefieldSeason;
            }
            set
            {
                _BattlefieldSeason = value;
                OnPropertyChanged("BattlefieldSeason");
            }
        }

        private int _BattlefieldWeather = 1;
        public int BattlefieldWeather
        {
            get
            {
                return _BattlefieldWeather;
            }
            set
            {
                _BattlefieldWeather = value;
                OnPropertyChanged("BattlefieldWeather");
            }
        }

        private int _BattlefieldCloud = 1;
        public int BattlefieldCloud
        {
            get
            {
                return _BattlefieldCloud;
            }
            set
            {
                _BattlefieldCloud = value;
                OnPropertyChanged("BattlefieldCloud");
            }
        }



        public RelayCommand ApplyCommand { get; set; }

        public void ApplyCommandAction(object param)
        {
            //BattlefieldSetTime = new DateTime(BattlefieldSetTime.Year, BattlefieldSetTime.Month, BattlefieldSetTime.Day, BattlefieldSetTimeHour, BattlefieldSetTimeMin, BattlefieldSetTimeSec);

            ////ViewModel_ScenarioView.SingletonInstance.model_ScenarioView.battlefieldEnv.BattlefieldArea = BattlefieldArea;
            ////ViewModel_ScenarioView.SingletonInstance.model_ScenarioView.battlefieldEnv.BattlefieldASetTime = BattlefieldSetTime;
            ////ViewModel_ScenarioView.SingletonInstance.model_ScenarioView.battlefieldEnv.BattlefieldSeason = BattlefieldSeason;
            ////ViewModel_ScenarioView.SingletonInstance.model_ScenarioView.battlefieldEnv.BattlefieldWeather = BattlefieldWeather;
            ////ViewModel_ScenarioView.SingletonInstance.model_ScenarioView.battlefieldEnv.BattlefieldCloud = BattlefieldCloud;

            //if(BattlefieldSetTimeHour > 17)
            //{
            //    CommonEvent.OnBattlefieldEnvSetNight?.Invoke(true);
            //}
            //else
            //{
            //    CommonEvent.OnBattlefieldEnvSetNight?.Invoke(false);
            //}

            ////위경도 누리꿈 마곡 대전 이벤트 
            //switch (BattlefieldArea)
            //{
            //    case 1:
            //        {
            //            //마곡
            //            CommonEvent.OnBattlefieldEnvSetLocation?.Invoke(37.5722208, 126.8377607);
            //        }
            //        break;
            //    case 2:
            //        {
            //            //상암
            //            CommonEvent.OnBattlefieldEnvSetLocation?.Invoke(37.5796548, 126.8900812);
            //        }
            //        break;
            //    case 3:
            //        {
            //            //대전
            //            CommonEvent.OnBattlefieldEnvSetLocation?.Invoke(36.406017, 127.303039);
            //        }
            //        break;

            //    default:
            //        break;
            //피격

            //message ChangeEnvironmentResponse
            //{
            //    double time = 1;        // -1, 0 ~ 2400
            //    int32 weather_mode = 2; // -1, 0 ~ 10
            //    /*
            //    0 clear_skies
            //    1 cloudy
            //    2 foggy
            //    3 overcast
            //    4 partly_cloudy
            //    5 rain
            //    6 rain_light
            //    7 rain_thunderstorm
            //    8 sand_dust_calm
            //    9 sand_dust_storm
            //    10 snow
            //    11 snow_blizzard
            //    12 snow_light
            //    */
            //    int32 season_mode = 3;  // -1, 0 ~ 3
            //    int32 level = 4;        // 0 ~ 3
            //    /*
            //    0 none
            //    1 Jipo_Level
            //    2 Hongik_Level
            //    3 Inje_Level
            //    */
            //}
            //BattlefieldSetTime = new DateTime(BattlefieldSetTime.Year, BattlefieldSetTime.Month, BattlefieldSetTime.Day, BattlefieldSetTimeHour, BattlefieldSetTimeMin, BattlefieldSetTimeSec);
            double BattlefieldSetTime = (BattlefieldSetTimeHour * 100.0)
                          + BattlefieldSetTimeMin
                          + (BattlefieldSetTimeSec / 60.0);

            switch (BattlefieldArea)
            {
                case 1:
                    {
                        //마곡
                        //CommonEvent.OnBattlefieldEnvSetLocation?.Invoke(37.5722208, 126.8377607);
                    }
                    break;
                case 2:
                    {
                        //상암
                        //CommonEvent.OnBattlefieldEnvSetLocation?.Invoke(37.5796548, 126.8900812);
                    }
                    break;
                case 3:
                    {
                        //대전
                        //CommonEvent.OnBattlefieldEnvSetLocation?.Invoke(36.406017, 127.303039);
                    }
                    break;

                default:
                    break;
            }
            // (기존 주석 처리된 CommonEvent 대신 직접 Map VM 호출)
            ViewModel_Unit_Map.SingletonInstance.MoveMapToBattlefield(BattlefieldArea);

            var env_message = new ChangeEnvironmentResponse
            {
                Time = BattlefieldSetTime,
                WeatherMode = BattlefieldWeather,
                SeasonMode = -1,
                Level = BattlefieldArea,
            };
            var env_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(env_message);
            var env_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeEnvironmentResponse",
                    Parameter = env_anyMessage,
                }
            };
            //await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(env_notification);
            gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(env_notification);
        }



        public RelayCommand CloseCommand { get; set; }

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
                View_BattlefieldEnv_PopUp.SingletonInstance.Hide();
            };

            View_BattlefieldEnv_PopUp.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
        }


    }


}
