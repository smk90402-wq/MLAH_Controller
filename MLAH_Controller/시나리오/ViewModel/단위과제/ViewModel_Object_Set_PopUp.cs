//using System.Data.Entity.Core.Metadata.Edm;
using System.Windows.Threading;
using MLAHInterop;
using REALTIMEVISUAL.Native.FederateInterface;
using System.IO;


namespace MLAH_Controller
{
    public partial class ViewModel_Object_Set_PopUp
    {
        #region Singleton
        static ViewModel_Object_Set_PopUp _ViewModel_Object_Set_PopUp = null;
        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static ViewModel_Object_Set_PopUp SingletonInstance
        {
            get
            {
                if (_ViewModel_Object_Set_PopUp == null)
                {
                    _ViewModel_Object_Set_PopUp = new ViewModel_Object_Set_PopUp();
                }
                return _ViewModel_Object_Set_PopUp;
            }
        }

        #endregion Singleton

        #region 생성자 & 콜백
        public ViewModel_Object_Set_PopUp()
        {
            ApplyCommand = new RelayCommand(ApplyCommandAction);
            CloseCommand = new RelayCommand(CloseCommandAction);
            RefreshCommand = new RelayCommand(RefreshCommandAction);

            LAHSend1Command = new RelayCommand(LAHSend1CommandAction);
            LAHSend2Command = new RelayCommand(LAHSend2CommandAction);
            LAHSend3Command = new RelayCommand(LAHSend3CommandAction);

            UAVSend1Command = new RelayCommand(UAVSend1CommandAction);
            UAVSend2Command = new RelayCommand(UAVSend2CommandAction);
            UAVSend3Command = new RelayCommand(UAVSend3CommandAction);



            Drone1Enable = false;
            Drone1Opacity = 0.4;
            Drone2Enable = false;
            Drone2Opacity = 0.4;
            Drone3Enable = false;
            Drone3Opacity = 0.4;

            Heli1Enable = false;
            Heli1Opacity = 0.4;
            Heli2Enable = false;
            Heli2Opacity = 0.4;
            Heli3Enable = false;
            Heli3Opacity = 0.4;

            // DispatcherTimer 객체 생성
            timer = new DispatcherTimer();

            // 타이머 간격을 1초로 설정
            timer.Interval = TimeSpan.FromSeconds(1);

            // Tick 이벤트에 이벤트 핸들러 연결
            timer.Tick += Timer_Tick;

            // 타이머 시작
            timer.Start();

        }
        #endregion 생성자 & 콜백

        private DispatcherTimer timer;
        private void Timer_Tick(object sender, EventArgs e)
        {
            //Deadlock 방지용 복사
            var TempScenarioObjects = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList;

            if(TempScenarioObjects.Count > 0)
            {
                foreach (var Item in TempScenarioObjects)
                {
                    switch (Item.ID)
                    {
                        case 1:
                            {
                                Heli1Enable = true;
                                Heli1Opacity = 1;
                            }
                            break;

                        case 2:
                            {
                                Heli2Enable = true;
                                Heli2Opacity = 1;
                            }
                            break;

                        case 3:
                            {
                                Heli3Enable = true;
                                Heli3Opacity = 1;
                            }
                            break;

                        case 4:
                            {
                                Drone1Enable = true;
                                Drone1Opacity = 1;
                            }
                            break;

                        case 5:
                            {
                                Drone2Enable = true;
                                Drone2Opacity = 1;
                            }
                            break;

                        case 6:
                            {
                                Drone3Enable = true;
                                Drone3Opacity = 1;
                            }
                            break;

                        default:
                            {
                                //Drone1Enable = false;
                                //Drone1Opacity = 0.4;
                                //Drone2Enable = false;
                                //Drone2Opacity = 0.4;
                                //Drone3Enable = false;
                                //Drone3Opacity = 0.4;

                                //Heli1Enable = false;
                                //Heli1Opacity = 0.4;
                                //Heli2Enable = false;
                                //Heli2Opacity = 0.4;
                                //Heli3Enable = false;
                                //Heli3Opacity = 0.4;
                            }
                            break;
                    }

                }
            }
            /// <summary>
            /// 표적 번호 -  0 : 지휘기  / 1 : 편대기  / 2 : 편대기  / 3 : 무인기 1  / 4 : 무인기 2  / 5 : 무인기 3
            /// </summary>
            
        }

        public RelayCommand LAHSend1Command { get; set; }

        public async void LAHSend1CommandAction(object param)
        {
            double input_health = 0;
            if(LAH1Hit == 0)
            {
                input_health = 100;
            }
            else
            {
                input_health = 50;
            }

            if(LAH1Crash == 1)
            {
                input_health = 0;
            }
            
            var hit_message = new ChangeHealthResponse
            {
                Id = 1,
                Health = input_health,
            };
            var hit_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(hit_message);
            var hit_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeHealthResponse",
                    Parameter = hit_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(hit_notification);



            var health_status_message = new ChangeHealthStatusResponse
            {
                Id = 1,
                Healthstatus = LAH1HealthStatus,
            };
            var health_status_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(health_status_message);
            var health_status_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeHealthStatusResponse",
                    Parameter = health_status_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(health_status_notification);

            //연료
            var fuel_message = new ChangeFuelStatusResponse
            {
                Id = 1,
                Fuelstatus = LAH1FuelWarning,
            };
            var fuel_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(fuel_message);
            var fuel_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeFuelStatusResponse",
                    Parameter = fuel_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(fuel_notification);

            /// 0 - all, 1 - 4, 2 - 5, 3 - 6, 4 - (4,5), 5 - (4,6), 6 - (5,6)
            int input_link = 0;

            if(LAH1Link1 == 1 && LAH1Link2 == 1 && LAH1Link3 == 1)
            {
                input_link = 7;
            }
            else if(LAH1Link1 == 1 && LAH1Link2 == 0 && LAH1Link3 == 0)
            {
                input_link = 1;
            }
            else if (LAH1Link1 == 0 && LAH1Link2 == 1 && LAH1Link3 == 0)
            {
                input_link = 2;
            }
            else if (LAH1Link1 == 0 && LAH1Link2 == 0 && LAH1Link3 == 1)
            {
                input_link = 3;
            }
            else if (LAH1Link1 == 1 && LAH1Link2 == 1 && LAH1Link3 == 0)
            {
                input_link = 4;
            }
            else if (LAH1Link1 == 1 && LAH1Link2 == 0 && LAH1Link3 == 1)
            {
                input_link = 5;
            }
            else if (LAH1Link1 == 0 && LAH1Link2 == 1 && LAH1Link3 == 1)
            {
                input_link = 6;
            }

            //데이터링크
            var link_message = new ChangeDataLinkResponse
            {
                Id = 1,
                Link = input_link,
            };
            var link_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(link_message);
            var link_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeDataLinkResponse",
                    Parameter = link_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(link_notification);

        }

        public RelayCommand LAHSend2Command { get; set; }

        public async void LAHSend2CommandAction(object param)
        {
            double input_health = 0;
            if (LAH2Hit == 0)
            {
                input_health = 100;
            }
            else
            {
                input_health = 50;
            }

            if (LAH2Crash == 1)
            {
                input_health = 0;
            }
            //피격
            var hit_message = new ChangeHealthResponse
            {
                Id = 2,
                Health = input_health,
            };
            var hit_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(hit_message);
            var hit_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeHealthResponse",
                    Parameter = hit_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(hit_notification);

         
            var health_status_message = new ChangeHealthStatusResponse
            {
                Id = 2,
                Healthstatus = LAH2HealthStatus,
            };
            var health_status_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(health_status_message);
            var health_status_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeHealthStatusResponse",
                    Parameter = health_status_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(health_status_notification);

            //연료
            var fuel_message = new ChangeFuelStatusResponse
            {
                Id = 2,
                Fuelstatus = LAH2FuelWarning,
            };
            var fuel_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(fuel_message);
            var fuel_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeFuelStatusResponse",
                    Parameter = fuel_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(fuel_notification);

            /// 0 - all, 1 - 4, 2 - 5, 3 - 6, 4 - (4,5), 5 - (4,6), 6 - (5,6)
            int input_link = 0;

            if (LAH2Link1 == 1 && LAH2Link2 == 1 && LAH2Link3 == 1)
            {
                input_link = 7;
            }
            else if (LAH2Link1 == 1 && LAH2Link2 == 0 && LAH2Link3 == 0)
            {
                input_link = 1;
            }
            else if (LAH2Link1 == 0 && LAH2Link2 == 1 && LAH2Link3 == 0)
            {
                input_link = 2;
            }
            else if (LAH2Link1 == 0 && LAH2Link2 == 0 && LAH2Link3 == 1)
            {
                input_link = 3;
            }
            else if (LAH2Link1 == 1 && LAH2Link2 == 1 && LAH2Link3 == 0)
            {
                input_link = 4;
            }
            else if (LAH2Link1 == 1 && LAH2Link2 == 0 && LAH2Link3 == 1)
            {
                input_link = 5;
            }
            else if (LAH2Link1 == 0 && LAH2Link2 == 1 && LAH2Link3 == 1)
            {
                input_link = 6;
            }

            //데이터링크
            var link_message = new ChangeDataLinkResponse
            {
                Id = 2,
                Link = input_link,
            };
            var link_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(link_message);
            var link_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeDataLinkResponse",
                    Parameter = link_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(link_notification);

        }

        public RelayCommand LAHSend3Command { get; set; }

        public async void LAHSend3CommandAction(object param)
        {
            double input_health = 0;
            if (LAH3Hit == 0)
            {
                input_health = 100;
            }
            else
            {
                input_health = 50;
            }

            if (LAH3Crash == 1)
            {
                input_health = 0;
            }
            //피격
            var hit_message = new ChangeHealthResponse
            {
                Id = 3,
                Health = input_health,
            };
            var hit_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(hit_message);
            var hit_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeHealthResponse",
                    Parameter = hit_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(hit_notification);

            var health_status_message = new ChangeHealthStatusResponse
            {
                Id = 3,
                Healthstatus = LAH3HealthStatus,
            };
            var health_status_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(health_status_message);
            var health_status_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeHealthStatusResponse",
                    Parameter = health_status_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(health_status_notification);

            //연료
            var fuel_message = new ChangeFuelStatusResponse
            {
                Id = 3,
                Fuelstatus = LAH3FuelWarning,
            };
            var fuel_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(fuel_message);
            var fuel_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeFuelStatusResponse",
                    Parameter = fuel_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(fuel_notification);

            /// 0 - all, 1 - 4, 2 - 5, 3 - 6, 4 - (4,5), 5 - (4,6), 6 - (5,6)
            int input_link = 0;

            if (LAH3Link1 == 1 && LAH3Link2 == 1 && LAH3Link3 == 1)
            {
                input_link = 7;
            }
            else if (LAH3Link1 == 1 && LAH3Link2 == 0 && LAH3Link3 == 0)
            {
                input_link = 1;
            }
            else if (LAH3Link1 == 0 && LAH3Link2 == 1 && LAH3Link3 == 0)
            {
                input_link = 2;
            }
            else if (LAH3Link1 == 0 && LAH3Link2 == 0 && LAH3Link3 == 1)
            {
                input_link = 3;
            }
            else if (LAH3Link1 == 1 && LAH3Link2 == 1 && LAH3Link3 == 0)
            {
                input_link = 4;
            }
            else if (LAH3Link1 == 1 && LAH3Link2 == 0 && LAH3Link3 == 1)
            {
                input_link = 5;
            }
            else if (LAH3Link1 == 0 && LAH3Link2 == 1 && LAH3Link3 == 1)
            {
                input_link = 6;
            }

            //데이터링크
            var link_message = new ChangeDataLinkResponse
            {
                Id = 3,
                Link = input_link,
            };
            var link_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(link_message);
            var link_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeDataLinkResponse",
                    Parameter = link_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(link_notification);

        }

        public RelayCommand UAVSend1Command { get; set; }

        public async void UAVSend1CommandAction(object param)
        {

            //무인기 활성화/비활성화
            var hit_message = new ChangeHealthStatusResponse
            {
                Id = 4,
                Healthstatus = UAV1Health
            };
            var hit_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(hit_message);
            var hit_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeHealthStatusResponse",
                    Parameter = hit_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(hit_notification);

            //임무장비 활성화/비활성화
            var sensor_message = new ChangeSensorResponse
            {
                Id = 4,
                Sensor = UAV1Sensor,
            };
            var sensor_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(sensor_message);
            var sensor_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeSensorResponse",
                    Parameter = sensor_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(sensor_notification);

            

            //연료 정상/경고/위험
            var fuel_message = new ChangeFuelStatusResponse
            {
                Id = 4,
                Fuelstatus = UAV1Fuel,
            };
            var fuel_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(fuel_message);
            var fuel_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeFuelStatusResponse",
                    Parameter = fuel_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(fuel_notification);

        }

        public RelayCommand UAVSend2Command { get; set; }

        public async void UAVSend2CommandAction(object param)
        {

            //무인기 활성화/비활성화
            var hit_message = new ChangeHealthStatusResponse
            {
                Id = 5,
                Healthstatus = UAV2Health
            };
            var hit_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(hit_message);
            var hit_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeHealthStatusResponse",
                    Parameter = hit_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(hit_notification);

            //임무장비 활성화/비활성화
            var sensor_message = new ChangeSensorResponse
            {
                Id = 5,
                Sensor = UAV2Sensor,
            };
            var sensor_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(sensor_message);
            var sensor_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeSensorResponse",
                    Parameter = sensor_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(sensor_notification);



            //연료 정상/경고/위험
            var fuel_message = new ChangeFuelStatusResponse
            {
                Id = 5,
                Fuelstatus = UAV2Fuel,
            };
            var fuel_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(fuel_message);
            var fuel_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeFuelStatusResponse",
                    Parameter = fuel_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(fuel_notification);

        }

        public RelayCommand UAVSend3Command { get; set; }

        public async void UAVSend3CommandAction(object param)
        {

            //무인기 활성화/비활성화
            var hit_message = new ChangeHealthStatusResponse
            {
                Id = 6,
                Healthstatus = UAV3Health
            };
            var hit_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(hit_message);
            var hit_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeHealthStatusResponse",
                    Parameter = hit_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(hit_notification);

            //임무장비 활성화/비활성화
            var sensor_message = new ChangeSensorResponse
            {
                Id = 6,
                Sensor = UAV3Sensor,
            };
            var sensor_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(sensor_message);
            var sensor_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeSensorResponse",
                    Parameter = sensor_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(sensor_notification);



            //연료 정상/경고/위험
            var fuel_message = new ChangeFuelStatusResponse
            {
                Id = 6,
                Fuelstatus = UAV3Fuel,
            };
            var fuel_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(fuel_message);
            var fuel_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeFuelStatusResponse",
                    Parameter = fuel_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(fuel_notification);

        }

        public RelayCommand ApplyCommand { get; set; }

        public async void ApplyCommandAction(object param)
        {
            //피격
            var hit_message = new ChangeHealthResponse
            {
             Id= 1,
             Health = 50,
            };
            var hit_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(hit_message);
            var hit_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeHealthResponse",
                    Parameter = hit_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(hit_notification);

            //연료
            var fuel_message = new ChangeFuelResponse
            {
                Id = 1,
                Fuel = 50,
            };
            var fuel_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(fuel_message);
            var fuel_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeFuelResponse",
                    Parameter = fuel_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(fuel_notification);

            //데이터링크
            var link_message = new ChangeDataLinkResponse
            {
                Id = 1,
                Link = 6,
            };
            var link_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(link_message);
            var link_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeDataLinkResponse",
                    Parameter = link_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(link_notification);

            //센서
            var sensor_message = new ChangeSensorResponse
            {
                Id = 1,
                Sensor = 1,
            };
            var sensor_anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(sensor_message);
            var sensor_notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ChangeSensorResponse",
                    Parameter = sensor_anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(sensor_notification);

        }

        public RelayCommand RefreshCommand { get; set; }

        public void RefreshCommandAction(object param)
        {
            //Deadlock 방지용 복사
            var TempScenarioObjects = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList;
            if(TempScenarioObjects.Count > 0)
            {
                foreach (var Item in TempScenarioObjects)
                {
                    switch (Item.ID)
                    {
                        case 4:
                            {
                                Drone1Enable = true;
                                Drone1Opacity = 1;
                                if (Item.Health == 1)
                                {
                                    Drone1Status = 1;
                                }
                                else
                                {
                                    Drone1Status = 2;
                                    //if (Item.AbnormalReason == 1)
                                    //{
                                    //    Drone1Reason = 1;
                                    //}
                                    //else if (Item.AbnormalReason == 2)
                                    //{
                                    //    Drone1Reason = 2;
                                    //}
                                    //else if (Item.AbnormalReason == 3)
                                    //{
                                    //    Drone1Reason = 3;
                                    //}
                                    //else
                                    //{
                                    //    Drone1Reason = 0;
                                    //}
                                }
                            }
                            break;

                        case 5:
                            {
                                Drone2Enable = true;
                                Drone2Opacity = 1;
                                if (Item.Health == 1)
                                {
                                    Drone2Status = 1;
                                }
                                else
                                {
                                    Drone2Status = 2;
                                    //if (Item.AbnormalReason == 1)
                                    //{
                                    //    Drone2Reason = 1;
                                    //}
                                    //else if (Item.AbnormalReason == 2)
                                    //{
                                    //    Drone2Reason = 2;
                                    //}
                                    //else if (Item.AbnormalReason == 3)
                                    //{
                                    //    Drone2Reason = 3;
                                    //}
                                    //else
                                    //{
                                    //    Drone2Reason = 0;
                                    //}
                                }
                            }
                            break;

                        case 6:
                            {
                                Drone3Enable = true;
                                Drone3Opacity = 1;
                                if (Item.Health == 1)
                                {
                                    Drone3Status = 1;
                                }
                                else
                                {
                                    Drone3Status = 2;
                                    //if (Item.AbnormalReason == 1)
                                    //{
                                    //    Drone3Reason = 1;
                                    //}
                                    //else if (Item.AbnormalReason == 2)
                                    //{
                                    //    Drone3Reason = 2;
                                    //}
                                    //else if (Item.AbnormalReason == 3)
                                    //{
                                    //    Drone3Reason = 3;
                                    //}
                                    //else
                                    //{
                                    //    Drone3Reason = 0;
                                    //}
                                }
                            }
                            break;

                        case 1:
                            {
                                Heli1Enable = true;
                                Heli1Opacity = 1;
                                if (Item.Health == 1)
                                {
                                    Heli1Status = 1;
                                }
                                else
                                {
                                    Heli1Status = 2;
                                    //if (Item.AbnormalReason == 1)
                                    //{
                                    //    Heli1Reason = 1;
                                    //}
                                    //else if (Item.AbnormalReason == 2)
                                    //{
                                    //    Heli1Reason = 2;
                                    //}
                                    //else if (Item.AbnormalReason == 3)
                                    //{
                                    //    Heli1Reason = 3;
                                    //}
                                    //else
                                    //{
                                    //    Heli1Reason = 0;
                                    //}
                                }
                            }
                            break;

                        case 2:
                            {
                                Heli2Enable = true;
                                Heli2Opacity = 1;
                                if (Item.Health == 1)
                                {
                                    Heli2Status = 1;
                                }
                                else
                                {
                                    Heli2Status = 2;
                                    //if (Item.AbnormalReason == 1)
                                    //{
                                    //    Heli2Reason = 1;
                                    //}
                                    //else if (Item.AbnormalReason == 2)
                                    //{
                                    //    Heli2Reason = 2;
                                    //}
                                    //else if (Item.AbnormalReason == 3)
                                    //{
                                    //    Heli2Reason = 3;
                                    //}
                                    //else
                                    //{
                                    //    Heli2Reason = 0;
                                    //}
                                }
                            }
                            break;

                        case 3:
                            {
                                Heli3Enable = true;
                                Heli3Opacity = 1;
                                if (Item.Health == 1)
                                {
                                    Heli3Status = 1;
                                }
                                else
                                {
                                    Heli3Status = 2;
                                    //if (Item.AbnormalReason == 1)
                                    //{
                                    //    Heli3Reason = 1;
                                    //}
                                    //else if (Item.AbnormalReason == 2)
                                    //{
                                    //    Heli3Reason = 2;
                                    //}
                                    //else if (Item.AbnormalReason == 3)
                                    //{
                                    //    Heli3Reason = 3;
                                    //}
                                    //else
                                    //{
                                    //    Heli3Reason = 0;
                                    //}
                                }
                            }
                            break;


                        default:
                            {
                                //Drone1Enable = false;
                                //Drone1Opacity = 0.4;
                                //Drone2Enable = false;
                                //Drone2Opacity = 0.4;
                                //Drone3Enable = false;
                                //Drone3Opacity = 0.4;

                                //Heli1Enable = false;
                                //Heli1Opacity = 0.4;
                                //Heli2Enable = false;
                                //Heli2Opacity = 0.4;
                                //Heli3Enable = false;
                                //Heli3Opacity = 0.4;
                            }
                            break;
                    }

                }
            }
            

        }



        public RelayCommand CloseCommand { get; set; }

        public void CloseCommandAction(object param)
        {
            //var fadeOutAnimation = new DoubleAnimation
            //{
            //    From = 1.0,
            //    To = 0.0,
            //    Duration = new System.Windows.Duration(TimeSpan.FromSeconds(0.5))
            //};
            //fadeOutAnimation.Completed += (s, a) =>
            //{
            //    View_Object_Set_PopUp.SingletonInstance.Hide();
            //};

            //View_Object_Set_PopUp.SingletonInstance.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
            View_Object_Set_PopUp.SingletonInstance.Hide();
        }


    }


}
