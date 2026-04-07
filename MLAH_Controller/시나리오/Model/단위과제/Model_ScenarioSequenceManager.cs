using DevExpress.DataProcessing;
using DevExpress.Utils.Extensions;
using DevExpress.Xpf.Map;
using Google.Protobuf;
using Microsoft.AspNetCore.Http;
using MLAH_Controller;
using MLAHInterop;
using Newtonsoft.Json;
using REALTIMEVISUAL.Native.FederateInterface;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.ApplicationModel.Background;
using Windows.Storage.Provider;
using Windows.UI.ViewManagement;
using static DevExpress.Charts.Designer.Native.BarThicknessEditViewModel;
using static MLAH_Controller.CommonEvent;


namespace MLAH_Controller
{
    public class Model_ScenarioSequenceManager
    {
        #region Singleton
        private static Model_ScenarioSequenceManager _Model_ScenarioSequenceManager = null;


        private static readonly Lazy<Model_ScenarioSequenceManager> _lazyInstance = new Lazy<Model_ScenarioSequenceManager>(() => new Model_ScenarioSequenceManager(), true);

        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static Model_ScenarioSequenceManager SingletonInstance
        {
            get { return _lazyInstance.Value; }
        }

        #endregion Singleton

        private Model_ScenarioSequenceManager()
        {
            OnLAHMissionPlanReceived += Callback_OnLAHMissionPlanReceived;
            OnUAVMissionPlanReceived += Callback_OnUAVMissionPlanReceived;

            // 2. 30Hz에 맞춰 약 33ms 주기로 백그라운드에서 큐를 처리하는 타이머 설정
            //_processingTimer = new Timer(ProcessQueue, null, Timeout.Infinite, Timeout.Infinite);
            //_backgroundTimer = new Timer(ProcessBackgroundQueue, null, Timeout.Infinite, Timeout.Infinite);

            CommonEvent.OnMissionPlanOptionInfoReceived += Callback_OnMissionPlanOptionInfoReceived;
            CommonEvent.OnPilotDecisionReceived += Callback_OnPilotDecisionReceived;
            CommonEvent.OnMissionUpdateWithoutDecisionReceived += Callback_OnMissionUpdateWithoutDecisionReceived;
        }

        //observation 몰려들어오는거 막기
        private DateTime _lastProcessedTime = DateTime.MinValue;
        private readonly TimeSpan _processInterval = TimeSpan.FromMilliseconds(30); // 약 30Hz 제한

        private bool _isNewImplicitUpdateSequence = true;

        private ConcurrentDictionary<uint, HelicopterExecutionState> _heliStates = new ConcurrentDictionary<uint, HelicopterExecutionState>();

        //수신된 임무계획들을 임시 보관하는 스레드 안전한 저장소 (Key: MissionPlanID)
        private readonly ConcurrentDictionary<uint, LAHMissionPlan> _lahMissionPlanCache = new ConcurrentDictionary<uint, LAHMissionPlan>();
        private readonly ConcurrentDictionary<uint, UAVMissionPlan> _uavMissionPlanCache = new ConcurrentDictionary<uint, UAVMissionPlan>();

        // 수신된 의사결정 옵션들을 보관하는 리스트와 동기화를 위한 lock 객체
        private readonly List<OptionList> _receivedOptions = new List<OptionList>();
        private readonly object _optionsLock = new object();

        //private readonly ConcurrentQueue<IMessage> _messageProcessingQueue = new ConcurrentQueue<IMessage>();
        //private readonly Timer _processingTimer;

        //private readonly object _observationLock = new object();


        // 백그라운드 처리를 위한 큐 (gRPC 전송 등)
        //private readonly ConcurrentQueue<SensorControlCommand> _backgroundProcessingQueue = new ConcurrentQueue<SensorControlCommand>();

        // UI 갱신을 위한 최신 데이터 저장소 (UAV ID, Command 객체)
        private readonly ConcurrentDictionary<uint, SensorControlCommand> _latestCommandsForUI = new ConcurrentDictionary<uint, SensorControlCommand>();

        //private readonly Timer _backgroundTimer;

        // 마지막으로 처리된 ObservationRequest를 저장할 변수
        private ObservationRequest _lastProcessedObservation;
        //private readonly object _lock = new object();
        public void ClearLastObservation()
        {
            _lastProcessedObservation = null;
            // 필요하다면 다른 캐시들도 여기서 초기화
        }

        private DateTime _lastLahStatesSentTime = DateTime.MinValue;

        public ObservationRequest GetLastObservation()
        {
            //lock (_observationLock)
                return _lastProcessedObservation;
            //}
        }
        public void ProcessReceivedSensorCommand(SensorControlCommand command)
        {
            // 1. UI 갱신용 데이터 저장 (매우 빠름)
            _latestCommandsForUI[command.UavID] = command;

            // 2. gRPC 전송 로직을 직접 await로 호출 (통제 가능한 비동기)
            //await SendUAVStatusResponse(command);
            Task.Run(() => SendUAVStatusResponse(command));
        }

        public async Task ProcessObservationRequest(ObservationRequest message)
        {
            if (!ViewModel_ScenarioView.SingletonInstance.IsSimPlaying)
            {
                return;
            }

            if (message == null) return;

            _lastProcessedObservation = message;

            if ((DateTime.Now - _lastProcessedTime) < _processInterval)
            {
                return; // 버려! (가장 최신 데이터는 어차피 UI 타이머가 15fps로 가져감)
            }

            _lastProcessedTime = DateTime.Now;

            //// UDP 송신 (백그라운드에서 바로 처리)
            //string json = JsonConvert.SerializeObject(message);
            //byte[] sendBytes = Encoding.UTF8.GetBytes(json);
            ////상황인지모의 송신
            //await UDPModule.SingletonInstance.SendUDPMessageAsync(sendBytes, "192.168.20.201", 55555);

            // UDP 송신 (Fire-and-Forget)
            string json = JsonConvert.SerializeObject(message);
            byte[] sendBytes = Encoding.UTF8.GetBytes(json);
            _ = Task.Run(() =>
            {
                try
                {
                    UDPModule.SingletonInstance.SendUDPMessageAsync(sendBytes, CommonUtil.IPConfig.UdpSendIP, 55555);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Error] UDP Send Failed: {ex.Message}");
                }
            });

            //await Lah_States_Send(message);

            // [개선 2] 다른 UDP 송신 작업들도 모두 동일하게 처리
            _ = Task.Run(() => Lah_States_Send(message));

            // SensorInfo는 observation 주기로 바로 전송
            _ = Task.Run(() => UAVSensorInfoSend(message));

            DateTime now = DateTime.Now;
            if ((now - _lastLahStatesSentTime).TotalSeconds >= 5.0)
            {
                _ = Task.Run(() => LAHMalFunctionState_Send(message));
                _ = Task.Run(() => UAVMalFunctionSend(message));
                _lastLahStatesSentTime = now;
            }
        }


        // UAV별 SensorInfo 전송 (observation 주기)
        public async Task UAVSensorInfoSend(ObservationRequest message)
        {
            if (message?.Uavs == null) return;

            foreach (var item in message.Uavs)
            {
                SensorInfo input = new SensorInfo
                {
                    MessageID = 5,
                    UavID = (uint)item.Id,
                    HorizontalFov = (float)item.CameraHfov,
                    VerticalFov = (float)item.CameraVfov,
                    DiagonalFov = (float)item.CameraDfov,
                    SensorCenterLat = (float)item.CameraGoalPosition[0].Latitude,
                    SensorCenterLon = (float)item.CameraGoalPosition[0].Longitude,
                    SensorCenterAlt = (int)item.CameraGoalPosition[0].Altitude,
                    SlantRange = (int)item.CameraHypoenuse,
                    FootPrintCenterLat = (float)item.CameraGoalPosition[0].Latitude,
                    FootPrintCenterLon = (float)item.CameraGoalPosition[0].Longitude,
                    FootPrintCenterAlt = (int)item.CameraGoalPosition[0].Altitude,
                    FootPrintLeftTopLat = (float)item.CameraGoalPosition[1].Latitude,
                    FootPrintLeftTopLon = (float)item.CameraGoalPosition[1].Longitude,
                    FootPrintLeftTopAlt = (int)item.CameraGoalPosition[1].Altitude,
                    FootPrintRightTopLat = (float)item.CameraGoalPosition[2].Latitude,
                    FootPrintRightTopLon = (float)item.CameraGoalPosition[2].Longitude,
                    FootPrintRightTopAlt = (int)item.CameraGoalPosition[2].Altitude,
                    FootPrintLeftBottomLat = (float)item.CameraGoalPosition[4].Latitude,
                    FootPrintLeftBottomLon = (float)item.CameraGoalPosition[4].Longitude,
                    FootPrintLeftBottomAlt = (int)item.CameraGoalPosition[4].Altitude,
                    FootPrintRightBottomLat = (float)item.CameraGoalPosition[3].Latitude,
                    FootPrintRightBottomLon = (float)item.CameraGoalPosition[3].Longitude,
                    FootPrintRightBottomAlt = (int)item.CameraGoalPosition[3].Altitude
                };

                await SensorInfo_Send(input);
            }
        }

        // UAV별 MalFunctionCommand 전송 (5초 주기)
        public async Task UAVMalFunctionSend(ObservationRequest message)
        {
            if (message?.Uavs == null) return;

            foreach (var item in message.Uavs)
            {
                UAVMalFunctionCommand inputMalfunction = new UAVMalFunctionCommand
                {
                    MessageID = 8,
                    UavID = (uint)item.Id,
                    Health = (uint)item.HealthStatus,
                    PayloadHealth = (uint)item.SensorStatus,
                    FuelWarning = (uint)item.FuelStatus,
                };

                await UAVMalFunction_Send(inputMalfunction);
            }
        }

        // ViewModel이 UI 갱신을 위해 최신 데이터들을 가져가는 메서드
        public ConcurrentDictionary<uint, SensorControlCommand> GetLatestSensorCommands()
        {
            return _latestCommandsForUI;
        }
        // 백그라운드 타이머가 실행하는 작업 (gRPC 메시지 전송 등)
        //private void ProcessBackgroundQueue(object state)
        //{
        //    while (_backgroundProcessingQueue.TryDequeue(out var command))
        //    {
        //        SendUAVStatusResponse(command);
        //    }
        //}

        // gRPC 메시지를 전송하는 메서드 (깊은 복사 활용)
        private async Task SendUAVStatusResponse(SensorControlCommand command)
        {
                var pos_Input = new LLAPosition
                {
                    Latitude = command.SensorLat,
                    Longitude = command.SensorLon,
                    Altitude = command.SensorAlt
                };

                var sensorRot = new Orientation
                {
                    Psi = 0,
                    Theta = command.GimbalPitch,
                    Phi = command.GimbalRoll
                };

                var UAV_Rot = new Orientation
                {
                    Psi = (float)command.Roll,
                    Theta = (float)command.Pitch,
                    Phi = (float)command.Heading
                };

                var message = new UAVStatusResponse
                {
                    Message = "UAVStatusResponse",
                    Id = (int)command.UavID,
                    UAVLocation = pos_Input,
                    SensorType = (int)command.SensorType,
                    Fov = (double)command.HorizontalFov,
                    SensorAttitude = sensorRot,
                    UAVRotation = UAV_Rot,
                };

                var anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(message);
                var notification = new Notification
                {
                    NotifType = enumNotifType.NotifMessageReceived,
                    MessageReceived = new REALTIMEVISUAL.Native.FederateInterface.MessageReceived
                    {
                        Name = "UAVStatusResponse",
                        Parameter = anyMessage,
                    }
                };
                await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(notification);

            var message1 = new ChangeFuelResponse
            {
                Id = (int)command.UavID,
                Fuel = (double)command.Fuel
            };

            var anyMessage1 = Google.Protobuf.WellKnownTypes.Any.Pack(message1);
            var notification1 = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new REALTIMEVISUAL.Native.FederateInterface.MessageReceived
                {
                    Name = "ChangeFuelResponse",
                    Parameter = anyMessage1,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(notification1);
        }
       

        public async Task Lah_States_Send(ObservationRequest message)
        {
            var originalHelicopters = message.Helicopters
                    .Where(x => x.Id == 1 || x.Id == 2 || x.Id == 3)
                    .ToList();

            
            int count = originalHelicopters.Count;
            if (count == 0) return; // 처리할 데이터가 없으면 종료


            var inputMessage = new Lah_States
            {
                MessageID = 6,
                StatesN = (uint)count,
                States = count > 0 ? new States[count] : null
                
            };

            // 현재 시간 가져오기
            DateTime now = DateTime.Now;

            for (int i = 0; i < count; i++)
            {
                var heli = originalHelicopters[i];

                // 5바이트 시간 생성 로직: 시(1) + 분(1) + 초(1) + 밀리초(2)
                // BigEndianBinaryWriter를 사용하시므로 밀리초도 Big Endian(상위바이트 먼저)으로 쪼개기
                ushort ms = (ushort)now.Millisecond;
                byte[] timeBytes = new byte[5];
                timeBytes[0] = (byte)now.Hour;
                timeBytes[1] = (byte)now.Minute;
                timeBytes[2] = (byte)now.Second;
                timeBytes[3] = (byte)(ms >> 8);   // 밀리초 상위 8비트
                timeBytes[4] = (byte)(ms & 0xFF); // 밀리초 하위 8비트

                inputMessage.States[i] = new States
                {
                    AircraftID = (uint)heli.Id,
                    CoordinateList = new CoordinateList
                    {
                        Latitude = (float)heli.Location.Latitude,
                        Longitude = (float)heli.Location.Longitude,
                        Altitude = (int)heli.Location.Altitude
                    },

                    Velocity = new Velocity
                    {
                        Heading = heli.Rotation.Phi,
                        Speed = (float)((Math.Sqrt(Math.Pow(heli.Velocity.U, 2) +Math.Pow(heli.Velocity.V, 2) +Math.Pow(heli.Velocity.W, 2)))/100),
                    },
                    Fuel = (float)heli.Fuel,
                    Weapons = new Weapons
                    {
                        Type1 = (uint)heli.MinigunRound,
                        Type2 = (uint)heli.HydraRound,
                        Type3 = (uint)heli.AtgmRound
                    },
                    //현재 시간을 5바이트 배열로 할당
                    LastSignalTime = timeBytes
                };
            }


            byte[] bytesToSend;
            using (var ms = new MemoryStream())
            {
                using (var bw = new CommonUtil.BigEndianBinaryWriter(ms))
                {
                    bw.Write(inputMessage.MessageID);
                    bw.Write(inputMessage.PresenceVector);
                    bw.Write(inputMessage.TimeStamp);
                    bw.Write(inputMessage.StatesN);

                    if (inputMessage.States != null)
                    {
                        foreach (var state in inputMessage.States)
                        {
                            bw.Write(state.AircraftID);
                            bw.Write(state.CoordinateList.Latitude);
                            bw.Write(state.CoordinateList.Longitude);
                            bw.Write(state.CoordinateList.Altitude);
                            bw.Write(state.Velocity.Speed);
                            bw.Write(state.Velocity.Heading);
                            bw.Write(state.Fuel);
                            bw.Write(state.Weapons.Type1);
                            bw.Write(state.Weapons.Type2);
                            bw.Write(state.Weapons.Type3);
                            bw.Write(state.LastSignalTime);
                            //bw.Write(state.Health);
                            //bw.Write(state.DatalinkStatus.IsConnectedToUAV1);
                            //bw.Write(state.DatalinkStatus.IsConnectedToUAV2);
                            //bw.Write(state.DatalinkStatus.IsConnectedToUAV3);
                        }
                    }
                    bw.Flush();

                }
                bytesToSend = ms.ToArray();
            }
            //UDPModule.SingletonInstance.SendUDPMessageAsync(bytesToSend, "192.168.20.100", 50004);
            //UDPModule.SingletonInstance.SendUDPMessageAsync(bytesToSend, "192.168.20.100", 50002);

            _ = Task.Run(() =>
            {
                try
                {
                    UDPModule.SingletonInstance.SendUDPMessageAsync(bytesToSend, "192.168.20.100", 50004);
                }
                catch (Exception ex)
                {
                    // 여기에 예외 로깅 로직 추가
                    //Debug.WriteLine($"[Error] UDP Send Failed: {ex.Message}");
                }
            });
            //await UDPModule.SingletonInstance.SendUDPMessageAsync(bytesToSend, "192.168.20.100", 50004);

            _ = Task.Run(() =>
            {
                try
                {
                    UDPModule.SingletonInstance.SendUDPMessageAsync(bytesToSend, "192.168.20.100", 50002);
                }
                catch (Exception ex)
                {
                    // 여기에 예외 로깅 로직 추가
                    //Debug.WriteLine($"[Error] UDP Send Failed: {ex.Message}");
                }
            });
            //await UDPModule.SingletonInstance.SendUDPMessageAsync(bytesToSend, "192.168.20.100", 50002);
        }



        /// <summary>
        /// 유인기의 이상 상태 정보(LAHMalFunctionState)를 생성하여 빅 엔디안으로 송신합니다.
        /// </summary>
        public async Task LAHMalFunctionState_Send(ObservationRequest observation)
        {
            var helicopters = observation.Helicopters;
            int count = helicopters.Count;

            // 전송할 메시지 객체를 생성합니다.
            var inputMessage = new LAHMalFunctionState
            {
                MessageID = 14,
                LAHN = (uint)count,
                LAH = count > 0 ? new LAH[count] : null
            };

            // 각 유인기(헬기)의 상태 정보를 채웁니다.
            for (int i = 0; i < count; i++)
            {
                var heli = helicopters[i];

                var cause = heli.Abnormalcause;
                //var Status = (cause.Hit == 0 &&
                //               cause.Loss1 == 0 &&
                //               cause.Loss2 == 0 &&
                //               cause.Loss3 == 0 &&
                //               cause.Fuelwarning == 0 &&
                //               cause.Fueldenger == 0 &&
                //               cause.Fuelzero == 0 &&
                //               cause.Crash == 0 &&
                //               cause.Sensor == 0) ? (uint)1 : (uint)2;

                //var Status = heli.Health == 100 ? (uint)1 : (uint)2;

                inputMessage.LAH[i] = new LAH
                {
                    AircraftID = (uint)heli.Id,
                    Health = (uint)heli.HealthStatus,
                    DatalinkStatus = new DatalinkStatus
                    {
                        IsConnectedToUAV1 = heli.Abnormalcause.Loss1 != 1,
                        IsConnectedToUAV2 = heli.Abnormalcause.Loss2 != 1,
                        IsConnectedToUAV3 = heli.Abnormalcause.Loss3 != 1
                    }
                };
            }

            byte[] bytesToSend;
            // MemoryStream을 사용하여 객체를 바이트 배열로 직렬화합니다.
            using (var ms = new MemoryStream())
            {
                // 표준 BinaryWriter 대신, 빅 엔디안을 지원하는 BigEndianBinaryWriter를 사용합니다.
                using (var bw = new CommonUtil.BigEndianBinaryWriter(ms))
                {
                    bw.Write(inputMessage.MessageID);
                    bw.Write(inputMessage.LAHN);

                    if (inputMessage.LAH != null)
                    {
                        foreach (var state in inputMessage.LAH)
                        {
                            bw.Write(state.AircraftID);
                            bw.Write(state.Health);
                            bw.Write(state.DatalinkStatus.IsConnectedToUAV1);
                            bw.Write(state.DatalinkStatus.IsConnectedToUAV2);
                            bw.Write(state.DatalinkStatus.IsConnectedToUAV3);
                        }
                    }
                    bw.Flush();
                }
                bytesToSend = ms.ToArray();
            }

            // 최종적으로 생성된 빅 엔디안 바이트 배열을 UDP로 전송합니다.
            //await UDPModule.SingletonInstance.SendUDPMessageAsync(bytesToSend, "192.168.20.100", 50002);
            _ = Task.Run(() =>
            {
                try
                {
                    UDPModule.SingletonInstance.SendUDPMessageAsync(bytesToSend, "192.168.20.100", 50002);
                }
                catch (Exception ex)
                {
                    // 여기에 예외 로깅 로직 추가
                    //Debug.WriteLine($"[Error] UDP Send Failed: {ex.Message}");
                }
            });
        }




        public async Task SensorInfo_Send(SensorInfo Input)
        {
            var data = SerializeSensorInfo(Input);
            // 첫 번째 IP만 pipe 전달 (모니터링용), 나머지는 UDP만
            await UDPModule.SingletonInstance.SendUDPMessageAsync(data, "192.168.20.101", 50002);
            _ = UDPModule.SingletonInstance.SendUDPOnlyAsync(data, "192.168.20.102", 50002);
            _ = UDPModule.SingletonInstance.SendUDPOnlyAsync(data, "192.168.20.103", 50002);
        }

        public async Task UAVMalFunction_Send(UAVMalFunctionCommand Input)
        {
            using (var ms = new MemoryStream())
            using (var bw = new CommonUtil.BigEndianBinaryWriter(ms))
            {
                bw.Write(Input.MessageID);
                bw.Write(Input.UavID);
                bw.Write(Input.Health);
                bw.Write(Input.PayloadHealth);
                bw.Write(Input.FuelWarning);
                bw.Flush();

                var data = ms.ToArray();
                // 첫 번째 IP만 pipe 전달 (모니터링용), 나머지는 UDP만
                await UDPModule.SingletonInstance.SendUDPMessageAsync(data, "192.168.20.101", 50002);
                _ = UDPModule.SingletonInstance.SendUDPOnlyAsync(data, "192.168.20.102", 50002);
                _ = UDPModule.SingletonInstance.SendUDPOnlyAsync(data, "192.168.20.103", 50002);
            }
        }

        //public async Task LAHMalFunctionState_Send(LAHMalFunctionState Input)
        //{
        //    var data = SerializeLAHMalFunctionState(Input);
        //    //UDPModule.SingletonInstance.SendUDPMessageAsync(data, "192.168.20.101", 50002);
        //    //UDPModule.SingletonInstance.SendUDPMessageAsync(data, "192.168.20.102", 50002);
        //    //UDPModule.SingletonInstance.SendUDPMessageAsync(data, "192.168.20.103", 50002);
        //    await UDPModule.SingletonInstance.SendUDPMessageAsync(data, "192.168.20.100", 50002);
        //}

        //public static byte[] SerializeSensorInfo(SensorInfo input)
        //{
        //    using (var ms = new MemoryStream())
        //    using (var bw = new BinaryWriter(ms))
        //    {
        //        bw.Write(input.MessageID);
        //        bw.Write(input.UavID);
        //        bw.Write(input.HorizontalFov);
        //        bw.Write(input.VerticalFov);
        //        bw.Write(input.DiagonalFov);

        //        bw.Write(input.SensorCenterLat);
        //        bw.Write(input.SensorCenterLon);
        //        bw.Write(input.SensorCenterAlt);

        //        bw.Write(input.SlantRange);

        //        bw.Write(input.FootPrintLeftTopLat);
        //        bw.Write(input.FootPrintLeftTopLon);
        //        bw.Write(input.FootPrintLeftTopAlt);

        //        bw.Write(input.FootPrintLeftBottomLat);
        //        bw.Write(input.FootPrintLeftBottomLon);
        //        bw.Write(input.FootPrintLeftBottomAlt);

        //        bw.Write(input.FootPrintRightBottomLat);
        //        bw.Write(input.FootPrintRightBottomLon);
        //        bw.Write(input.FootPrintRightBottomAlt);

        //        bw.Flush();
        //        return ms.ToArray();
        //    }
        //}

        /// <summary>
        /// SensorInfo 객체를 빅 엔디안 바이트 배열로 직렬화합니다.
        /// </summary>
        /// <param name="input">직렬화할 SensorInfo 객체</param>
        /// <returns>빅 엔디안으로 변환된 바이트 배열</returns>
        public static byte[] SerializeSensorInfo(SensorInfo input)
        {
            using (var ms = new MemoryStream())
            // 표준 BinaryWriter 대신 BigEndianBinaryWriter를 사용하도록 변경
            using (var bw = new CommonUtil.BigEndianBinaryWriter(ms))
            {
                bw.Write(input.MessageID);
                bw.Write(input.UavID);
                bw.Write(input.HorizontalFov);
                bw.Write(input.VerticalFov);
                bw.Write(input.DiagonalFov);

                bw.Write(input.SensorCenterLat);
                bw.Write(input.SensorCenterLon);
                bw.Write(input.SensorCenterAlt);

                bw.Write(input.SlantRange);

                bw.Write(input.FootPrintCenterLat);
                bw.Write(input.FootPrintCenterLon);
                bw.Write(input.FootPrintCenterAlt);

                bw.Write(input.FootPrintLeftTopLat);
                bw.Write(input.FootPrintLeftTopLon);
                bw.Write(input.FootPrintLeftTopAlt);

                bw.Write(input.FootPrintRightTopLat);
                bw.Write(input.FootPrintRightTopLon);
                bw.Write(input.FootPrintRightTopAlt);

                bw.Write(input.FootPrintRightBottomLat);
                bw.Write(input.FootPrintRightBottomLon);
                bw.Write(input.FootPrintRightBottomAlt);

                bw.Write(input.FootPrintLeftBottomLat);
                bw.Write(input.FootPrintLeftBottomLon);
                bw.Write(input.FootPrintLeftBottomAlt);

                bw.Flush();
                return ms.ToArray();
            }
        }

        public static byte[] SerializeLAHMalFunctionState(LAHMalFunctionState input)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(input.MessageID);
                //bw.Write(input.PresenceVector);
                //bw.Write(input.TimeStamp);
                bw.Write(input.LAHN);

                if (input.LAH != null)
                {
                    foreach (var state in input.LAH)
                    {
                        bw.Write(state.AircraftID);
                        bw.Write(state.Health);
                        bw.Write(state.DatalinkStatus.IsConnectedToUAV1);
                        bw.Write(state.DatalinkStatus.IsConnectedToUAV2);
                        bw.Write(state.DatalinkStatus.IsConnectedToUAV3);
                    }
                }

                bw.Flush();
                //byte[] bytesToSend = ms.ToArray();
                return ms.ToArray();
            }
        }

        public bool IsClickedMake = false;

        //임시
        public bool IsUAVMakeFinsh = false;
        //public bool IsObervationStart = false;

        //public int ObjectReqCount = 0;
        //private static readonly object _objectReqCountLock = new object();


        private async Task Callback_OnScenarioRequest(IMessage messageInstance, ContextInfo contextInfo)
        {
            //var message = messageInstance as ScenarioRequest;
            //if (message != null)
            //{
            //    lock (_objectReqCountLock)
            //    {
            //        Model_ScenarioSequenceManager.SingletonInstance.ObjectReqCount++;
            //    }
            //    //await Model_ScenarioSequenceManager.SingletonInstance.ObjectMake();
            //}
        }
        public async Task ObjectMake()
        {
            if (Model_ScenarioSequenceManager.SingletonInstance.IsClickedMake == true)
            {
                //int total_object_count = ViewModel_ScenarioView.SingletonInstance.model_ScenarioView.ScenarioObjects.Count();
                int total_object_count = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList.Count();
                int makecount = 0;
                if (total_object_count != 0)
                {
                    if (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList.Count > 0)
                    {
                        foreach (var item in ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList)
                        {
                            //if(ObjectReqCount-1 <= total_object_count)
                            //{
                            bool IsRemainObject = true;
                            //if (ObjectReqCount != total_object_count)
                            //{
                            //    IsRemainObject = true;
                            //}
                            if (makecount == total_object_count - 1)
                            {
                                IsRemainObject = false;
                            }

                            //if (ObjectReqCount != 0)
                            {
                                LLAPosition tempLOC = new LLAPosition();
                                //tempLOC.Latitude = ViewModel_ScenarioView.SingletonInstance.model_ScenarioView.ScenarioObjects[ObjectReqCount - 1].ObjectLAT;
                                tempLOC.Latitude = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].LOC.Latitude;
                                tempLOC.Longitude = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].LOC.Longitude;
                                tempLOC.Altitude = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].LOC.Altitude;

                                string typestring = "";
                                string identify_check = "";
                                var Inputstatus = enumInputStatus.StatusMission;
                                string temp_subtype = "";
                                //double input_fuel = 100;
                                double input_fuel = 15;
                                double input_consumption = 0;
                                double input_detectPixel = 0;
                                double input_recogPixel = 0;
                                int input_attackflag = 1;
                                if (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].Type == 3 && ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].IsLeader == 1)
                                {
                                    //typestring = "Helicopter";
                                    typestring = "LAH";
                                    identify_check = "Blue";
                                    Inputstatus = enumInputStatus.StatusMission;
                                    temp_subtype = "Commander";
                                    //temp_subtype = "Squadron";
                                    input_fuel = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].Fuel;
                                    input_consumption = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].FuelConsumption;
                                    input_attackflag = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].AttackFlag;
                                }
                                else if (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].Type == 3 && ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].IsLeader != 1)
                                {
                                    //typestring = "Helicopter";
                                    typestring = "LAH";
                                    identify_check = "Blue";
                                    Inputstatus = enumInputStatus.StatusMission;
                                    //temp_subtype = "Commander";
                                    temp_subtype = "Squadron";
                                    input_fuel = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].Fuel;
                                    input_consumption = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].FuelConsumption;
                                    input_attackflag = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].AttackFlag;
                                }
                                else if (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].Type == 1)
                                {
                                    typestring = "UAV";
                                    //typestring = "LAH";
                                    identify_check = "Blue";

                                    if (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].UAVTestMode == 0)
                                    {
                                        temp_subtype = "UAV_VF";
                                        Inputstatus = enumInputStatus.StatusVf;
                                    }
                                    else
                                    {
                                        temp_subtype = "UAV";
                                        Inputstatus = enumInputStatus.StatusMission;
                                    }

                                    input_fuel = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].Fuel;
                                    input_consumption = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].FuelConsumption;

                                    input_detectPixel = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].DetectPixel;
                                    input_recogPixel = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].RecogPixel;

                                }
                                else
                                {
                                    typestring = "Target";
                                    //typestring = "LAH";
                                    if (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].Identification == 1)
                                    {
                                        identify_check = "Blue";
                                    }
                                    else
                                    {
                                        identify_check = "Red";
                                    }
                                    Inputstatus = enumInputStatus.StatusMission;
                                    temp_subtype = "Target";
                                    //표적 서브타입
                                    //T55
                                    //M2010
                                    ///M1938
                                    ///M1992
                                    ///ZPU4
                                    //Hostility
                                    //K511
                                    //Surion
                                    //Friendly
                                    ///K1
                                    ///K2
                                    ///K511

                                    switch (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].Type)
                                    {

                                        case 2:
                                            {
                                                switch (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].PlatformType)
                                                {
                                                    case 1:
                                                        {
                                                            temp_subtype = "Surion";
                                                        }
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                            break;
                                        case 4:
                                            {
                                                switch (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].PlatformType)
                                                {
                                                    case 5:
                                                        {
                                                            temp_subtype = "M2010";
                                                        }
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                            break;
                                        case 5:
                                            {
                                                switch (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].PlatformType)
                                                {
                                                    case 1:
                                                        {
                                                            //temp_subtype = "K1";
                                                        }
                                                        break;

                                                    case 2:
                                                        {
                                                            //temp_subtype = "K2";
                                                        }
                                                        break;

                                                    case 3:
                                                        {
                                                            temp_subtype = "T55";
                                                        }
                                                        break;


                                                    default:
                                                        break;
                                                }
                                            }
                                            break;

                                        case 6:
                                            {
                                                switch (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].PlatformType)
                                                {
                                                    case 1:
                                                        {
                                                            temp_subtype = "M1992";
                                                        }
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                            break;

                                        case 7:
                                            {
                                                switch (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].PlatformType)
                                                {
                                                    case 1:
                                                        {
                                                            temp_subtype = "M1938";
                                                        }
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                            break;

                                        case 8:
                                            {
                                                switch (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].PlatformType)
                                                {
                                                    case 1:
                                                        {
                                                            temp_subtype = "ZPU4";
                                                        }
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                            break;

                                        case 9:
                                            {
                                                switch (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].PlatformType)
                                                {
                                                    case 1:
                                                        {
                                                            temp_subtype = "Hostility";
                                                        }
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                            break;

                                        case 11:
                                            {
                                                switch (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].PlatformType)
                                                {
                                                    case 1:
                                                        {
                                                            //temp_subtype = "K511";
                                                        }
                                                        break;
                                                    case 2:
                                                        {
                                                            temp_subtype = "K511";
                                                        }
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                            break;

                                        case 13:
                                            {
                                                switch (ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].PlatformType)
                                                {
                                                    case 1:
                                                        {
                                                            temp_subtype = "Friendly";
                                                        }
                                                        break;
                                                    case 2:
                                                        {
                                                            //temp_subtype = "Friendly";
                                                        }
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                            break;





                                        default:
                                            {

                                            }
                                            break;


                                    }


                                }


                                MLAHInterop.Orientation temprot = new MLAHInterop.Orientation();
                                temprot.Psi = 0;
                                temprot.Theta = 0;
                                temprot.Phi = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].velocity.Heading - 90;

                                MLAHInterop.Velocity tempvel = new MLAHInterop.Velocity();
                                tempvel.V = 0;
                                tempvel.U = 0;
                                tempvel.W = 0;




                                var message = new ScenarioResponse
                                {
                                    Type = typestring,
                                    Subtype = temp_subtype,
                                    Id = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].ID,
                                    Identify = identify_check,
                                    Location = tempLOC,
                                    NextScenario = IsRemainObject,
                                    CameraType = "EO",
                                    //health 기입해야겠다
                                    Health = 100,
                                    Fuel = input_fuel,
                                    //Fuel = 100,
                                    FuelComsumption = input_consumption,
                                    Rotation = temprot,
                                    Velocity = tempvel,
                                    //enumInputStatus - 조종할 때 control
                                    Inputstatus = Inputstatus,
                                    MinigunDamage = 100,
                                    MinigunRound = (int)ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].weapons.Type1,
                                    HydraDirectDamage = 100,
                                    HydraExplosionDamage = 20,
                                    HydraDirectAccuracy = 50,
                                    HydraExplosionAccuracy = 50,
                                    HydraRound = (int)ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].weapons.Type2,
                                    AtgmDirectDamage = 100,
                                    AtgmExplosionDamage = 20,
                                    AtgmDirectAccuracy = 50,
                                    AtgmExplosionAccuracy = 50,
                                    AtgmRound = (int)ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].weapons.Type3,

                                    TargetAttackDelay = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].AttackDelay,
                                    TargetDamage = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].AttackDamage,
                                    TargetAttackRange = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].AttackRange,
                                    TargetDetectRange = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].DetectRange,
                                    TargetAttackAccuracy = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList[makecount].AttackAccuracy,

                                    DetectionPixel = input_detectPixel,
                                    RecognitionPixel = input_recogPixel,

                                    //자동공격 hardcoding
                                    //AutoAttackFlag = 2,
                                    AutoAttackFlag = input_attackflag,

                                };
                                var anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(message);
                                var notification = new Notification
                                {
                                    NotifType = enumNotifType.NotifMessageReceived,
                                    MessageReceived = new MessageReceived
                                    {
                                        Name = "ScenarioResponse",
                                        Parameter = anyMessage,
                                    }
                                };
                                //await Task.Delay(100);
                                await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(notification);
                                makecount++;
                                if (IsRemainObject == false)
                                {
                                    ViewModel_ScenarioView.SingletonInstance.SceneStatus = "모의 시작";
                                }
                            }

                            //}
                        }
                    }
                }
                else
                {
                    var pop_error = new View_PopUp(10);
                    pop_error.Description.Text = "언리얼 객체 생성불가";
                    pop_error.Reason.Text = "시나리오 객체 개수 : 0";
                    pop_error.Show();
                }


            }
        }
        //public async Task Callback_OnScenarioMissionRequest(IMessage messageInstance, ContextInfo contextInfo)
        //{
        //    var message = messageInstance as ScenarioMissionRequest;
        //    if (message != null)
        //    {
        //        if(message.Id == 4)
        //        {
        //            IsUAVMakeFinsh = true;
        //        }


        //    }
        //}


        public async Task SendScenarioMission()
        {
            //var message = messageInstance as ScenarioMissionRequest;
            //if (message != null)
            {
                foreach(var OnePlan in ViewModel_ScenarioView.SingletonInstance.model_Unit_Develop.unit_LAH_MovePlansList)
                {
                    var sendmessage = ViewModel_ScenarioView.SingletonInstance.model_Unit_Develop.Convert_LAHPlan_To_GrpcLAH(OnePlan.unit_LAH_MovePlans);
                    foreach (var item in sendmessage.MissionList)
                    {
                        item.Id = (int)OnePlan.UnitID;
                    }
                    var anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(sendmessage);
                    var notification = new Notification
                    {
                        NotifType = enumNotifType.NotifMessageReceived,
                        MessageReceived = new MessageReceived
                        {
                            Name = "ScenarioMissionResponse",
                            Parameter = anyMessage,
                        }
                    };
                    //await Task.Delay(500);
                    await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(notification);
                }
                //var sendmessage = ViewModel_ScenarioView.SingletonInstance.model_Unit_Develop.Convert_LAHPlan_To_GrpcLAH(ViewModel_ScenarioView.SingletonInstance.model_Unit_Develop.unit_LAH_MovePlansList.unit_LAH_MovePlans);
                //var sendmessage = new ScenarioMissionResponse();
                
            }
        }



        public async Task InitUnreal()
        {
            var message = new InitEpisodeResponse
            {
                Message = "KMY BABO"
            };
            var anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(message);
            var notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "InitEpisodeResponse",
                    Parameter = anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(notification);
        }


        public void Callback_OnLAHMissionPlanReceived(LAHMissionPlan InputPlan)
        {
            _lahMissionPlanCache[InputPlan.MissionPlanID] = InputPlan;
            //System.Diagnostics.Debug.WriteLine($"[재계획 수신] LAH Plan ID: {InputPlan.MissionPlanID} 캐시 저장됨. (인가 대기중)");
        }

        // UI 갱신 헬퍼 (DecisionType 파라미터 추가)
        private void UpdateControlUI(uint aircraftId, string decisionType = "System")
        {
            if (!_heliStates.TryGetValue(aircraftId, out var state)) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var vm = ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance;
                var currentMission = state.GetCurrentMission();
                var nextMission = state.GetNextMission();

                int curId = currentMission != null ? (int)currentMission.IndividualMissionID : 0;
                string done = state.IsAllFinished ? "O" : "X";
                int nextId = nextMission != null ? (int)nextMission.IndividualMissionID : 0;

                if (aircraftId == 1)
                {
                    vm.ControlDecisionType1 = decisionType; // Pilot or System
                    vm.ControlIndividualID1 = curId;
                    vm.ControlMissionDone1 = done;
                    vm.ControlNextID1 = nextId;
                }
                else if (aircraftId == 2)
                {
                    vm.ControlDecisionType2 = decisionType;
                    vm.ControlIndividualID2 = curId;
                    vm.ControlMissionDone2 = done;
                    vm.ControlNextID2 = nextId;
                }
                else if (aircraftId == 3)
                {
                    vm.ControlDecisionType3 = decisionType;
                    vm.ControlIndividualID3 = curId;
                    vm.ControlMissionDone3 = done;
                    vm.ControlNextID3 = nextId;
                }
            });
        }

       

        public async Task ExecuteCurrentMission(uint aircraftId)
        {
            // 현재 상태가 없거나, 이미 다 끝났으면 무시
            if (!_heliStates.TryGetValue(aircraftId, out var state) || state.IsAllFinished) return;

            var mission = state.GetCurrentMission();
            if (mission == null) return;

            // 1. gRPC 메시지 생성
            var grpcMessage = new ScenarioMissionResponse
            {
                Message = "ScenarioMissionResponse"
            };

            // 2. 현재 개별임무의 웨이포인트들만 변환하여 추가
            if (mission.WaypointList != null)
            {
                foreach (var wp in mission.WaypointList)
                {
                    if (wp.Coordinate == null) continue;

                    // 공격 명령이 있으면 fly-over(0), 없으면 fly-by(1)
                    bool hasAttack = wp.Attack != null && wp.Attack.TargetID != 0;

                    // 이동 명령
                    var moveMission = new ScenarioMission
                    {
                        Type = "LAH",
                        Id = (int)aircraftId,
                        MissionType = enumMissionType.MissionMovetogps,
                        Mission = new Vector4
                        {
                            X = wp.Coordinate.Latitude,
                            Y = wp.Coordinate.Longitude,
                            Z = wp.Coordinate.Altitude,
                            W = wp.Speed
                        },
                        Waypointpasstype = hasAttack ? 0 : 1,
                        Onwaittime = (int)wp.Hovering,
                        MissionID = (int)mission.IndividualMissionID,
                        WaypointID = (int)wp.WaypoinID
                    };
                    grpcMessage.MissionList.Add(moveMission);

                    // 공격 명령 (있을 경우 이동 직후 추가)
                    if (wp.Attack != null && wp.Attack.TargetID != 0)
                    {
                        var attackMission = new ScenarioMission
                        {
                            Type = "LAH",
                            Id = (int)aircraftId,
                            // MissionType은 아래에서 설정
                            Mission = new Vector4 { X = wp.Attack.TargetID, Y = 0, Z = 0, W = 0 },
                            Onwaittime = 0,
                            MissionID = (int)mission.IndividualMissionID, // 동일 ID
                            WaypointID = (int)wp.WaypoinID
                        };

                        switch (wp.Attack.WeaponType)
                        {
                            case 1: attackMission.MissionType = enumMissionType.MissionAttacktoMinigun; break;
                            case 2: attackMission.MissionType = enumMissionType.MissionAttacktoHydra; break;
                            case 3: attackMission.MissionType = enumMissionType.MissionAttacktoAtgm; break;
                            default: attackMission.MissionType = enumMissionType.MissionAttacktoMinigun; break;
                        }
                        grpcMessage.MissionList.Add(attackMission);
                    }
                }
            }

            // 3. 전송
            var anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(grpcMessage);
            var notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived { Name = "ScenarioMissionResponse", Parameter = anyMessage }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(notification);

            //System.Diagnostics.Debug.WriteLine($"[명령 전송] 헬기 {aircraftId}, 개별임무 {mission.IndividualMissionID} 전송 완료.");
        }

        public void ProcessWaypointDone(int aircraftId, int missionId)
        {
            if (!_heliStates.TryGetValue((uint)aircraftId, out var state)) return;

            var currentMission = state.GetCurrentMission();

            // ID 검증: 현재 수행 중이어야 할 미션 ID와 수신된 ID가 일치하는지 확인
            if (currentMission != null && currentMission.IndividualMissionID == missionId)
            {
                //System.Diagnostics.Debug.WriteLine($"[임무 완료] 헬기 {aircraftId}, 임무 {missionId} 완료. 애니메이션 시작.");

                // 1. [즉시] 내부 상태(인덱스)는 다음 단계로 전진시킵니다. (로직 무결성 유지)
                AdvanceToNextStep(state);

                // 2. [비동기] UI 애니메이션 시퀀스 시작 (Fire-and-Forget)
                _ = AnimateMissionCompletion((uint)aircraftId, state);
            }
            else
            {
                //System.Diagnostics.Debug.WriteLine($"[완료 무시] 헬기 {aircraftId} - ID 불일치 (재계획됨/이미완료)");
            }
        }



        private async Task AnimateMissionCompletion(uint aircraftId, HelicopterExecutionState nextState)
        {
            var newCurrentMission = nextState.GetCurrentMission();
            var newNextMission = nextState.GetNextMission();

            int newCurrentId = newCurrentMission != null ? (int)newCurrentMission.IndividualMissionID : 0;
            int newNextId = newNextMission != null ? (int)newNextMission.IndividualMissionID : 0;

            var vm = ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance;

            int viewTime = 2000;  // 완료 'O'를 쳐다보는 시간 (2초)
            int fadeTime = 800;   // 글자가 투명해지는 애니메이션 시간 (0.8초)
            int blankTime = 200;  // 데이터 교체 중 잠깐 멈춤 (0.2초)

            // -------------------------------------------------------
            // [Step 1] 완료 'O' 표시 (파란색으로 뜰 것임)
            // -------------------------------------------------------
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (aircraftId == 1) { vm.ControlMissionDone1 = "O"; vm.ControlStatusOpacity1 = 1.0; }
                else if (aircraftId == 2) { vm.ControlMissionDone2 = "O"; vm.ControlStatusOpacity2 = 1.0; }
                else if (aircraftId == 3) { vm.ControlMissionDone3 = "O"; vm.ControlStatusOpacity3 = 1.0; }
            });

            await Task.Delay(viewTime); 

            // -------------------------------------------------------
            // [Step 2] 페이드 아웃 (투명하게 사라짐)
            // -------------------------------------------------------
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (aircraftId == 1) vm.ControlStatusOpacity1 = 0.0; // 싹 사라짐
                else if (aircraftId == 2) vm.ControlStatusOpacity2 = 0.0;
                else if (aircraftId == 3) vm.ControlStatusOpacity3 = 0.0;
            });

            await Task.Delay(fadeTime); 

            // -------------------------------------------------------
            // [Step 3] 데이터 갱신 (안 보이는 상태에서 값만 바꿈)
            // -------------------------------------------------------
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 1. 현재 임무 ID 갱신
                if (aircraftId == 1) vm.ControlIndividualID1 = newCurrentId;
                else if (aircraftId == 2) vm.ControlIndividualID2 = newCurrentId;
                else if (aircraftId == 3) vm.ControlIndividualID3 = newCurrentId;

                // 2. 완료 여부 'X'로 리셋
                string nextDoneStatus = nextState.IsAllFinished ? "O" : "X";
                if (aircraftId == 1) vm.ControlMissionDone1 = nextDoneStatus;
                else if (aircraftId == 2) vm.ControlMissionDone2 = nextDoneStatus;
                else if (aircraftId == 3) vm.ControlMissionDone3 = nextDoneStatus;

                // 3. 다음 임무 ID 갱신
                if (aircraftId == 1) vm.ControlNextID1 = newNextId;
                else if (aircraftId == 2) vm.ControlNextID2 = newNextId;
                else if (aircraftId == 3) vm.ControlNextID3 = newNextId;
            });

            await Task.Delay(blankTime);

            // -------------------------------------------------------
            // [Step 4] 페이드 인 (새로운 값으로 스르륵 나타남)
            // -------------------------------------------------------
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (aircraftId == 1) vm.ControlStatusOpacity1 = 1.0; // 짠 하고 나타남
                else if (aircraftId == 2) vm.ControlStatusOpacity2 = 1.0;
                else if (aircraftId == 3) vm.ControlStatusOpacity3 = 1.0;
            });

            // 1. 자동 모드가 켜져 있고
            // 2. 모든 임무가 끝난 상태가 아니라면
            if (ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance.IsAutoExecutionMode && !nextState.IsAllFinished)
            {
                //System.Diagnostics.Debug.WriteLine($"[자동 수행] 헬기 {aircraftId} - 이전 임무 완료, 다음 임무 자동 시작.");

                // UI 애니메이션이 끝난 후 자연스럽게 이어지도록 실행
                await ExecuteCurrentMission(aircraftId);
            }
        }

        private void AdvanceToNextStep(HelicopterExecutionState state)
        {
            var segment = state.Plan.MissionSegemntList[state.CurrentSegmentIndex];

            // 1. 현재 세그먼트에 다음 개별임무가 있는지 확인
            if (state.CurrentIndividualIndex + 1 < segment.IndividualMissionList.Count)
            {
                state.CurrentIndividualIndex++;
            }
            // 2. 없다면 다음 세그먼트로 이동
            else if (state.CurrentSegmentIndex + 1 < state.Plan.MissionSegemntList.Count)
            {
                state.CurrentSegmentIndex++;
                state.CurrentIndividualIndex = 0; // 새 세그먼트의 첫 임무부터
            }
            // 3. 더 이상 임무가 없다면 완료 처리
            else
            {
                state.IsAllFinished = true;
            }
        }

        public void Callback_OnUAVMissionPlanReceived(UAVMissionPlan InputPlan)
        {
            //// ▼▼▼▼▼▼▼▼▼▼ 추가 ▼▼▼▼▼▼▼▼▼▼
            //Application.Current.Dispatcher.InvokeAsync(() =>
            //{
            //    var oldPlan = ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance
            //        .UAVMissionPlanItemSource
            //        .FirstOrDefault(x => x.AircraftID == InputPlan.AircraftID);

            //    if (oldPlan != null)
            //    {
            //        ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance
            //            .UAVMissionPlanItemSource
            //            .Remove(oldPlan);
            //    }

            //    ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance
            //        .UAVMissionPlanItemSource
            //        .Add(InputPlan);

            //    var index = ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance.UAVMissionPlanItemSource.Count();
            //    ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance.UAVMissionPlanSelectedIndex = index - 1;
            //});
            //// ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
            _uavMissionPlanCache[InputPlan.MissionPlanID] = InputPlan;
            //System.Diagnostics.Debug.WriteLine($"[재계획 수신] UAV Plan ID: {InputPlan.MissionPlanID} 캐시 저장됨.");
        }

        private void Callback_OnMissionPlanOptionInfoReceived(MissionPlanOptionInfo optionInfo)
        {
            lock (_optionsLock)
            {
                _receivedOptions.Clear();
                _receivedOptions.AddRange(optionInfo.OptionList);
            }
            //System.Diagnostics.Debug.WriteLine($"[옵션 수신] {optionInfo.OptionList.Count}개의 재계획 옵션 수신 완료.");

            // TODO: 여기서 UI에 옵션 목록을 표시하라는 이벤트를 발생시킬 수 있음
            // CommonEvent.OnOptionsAvailable?.Invoke(_receivedOptions);
        }

        private void ApplyApprovedLahPlan(LAHMissionPlan plan, string decisionSource)
        {
            // 1. 상태(State) 새로 생성 -> 무조건 인덱스 0으로 초기화 (기존 진행상황 폐기)
            var newState = new HelicopterExecutionState
            {
                Plan = plan,
                CurrentSegmentIndex = 0,
                CurrentIndividualIndex = 0,
                IsAllFinished = false
            };

            // 2. 딕셔너리에 덮어씌우기 (State Reset)
            _heliStates[plan.AircraftID] = newState;

            // 3. UI 리스트(Grid)에 계획 추가
            Application.Current.Dispatcher.Invoke(() =>
            {
                ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance.LAHMissionPlanItemSource.Add(plan);
            });

            // 4. 통제 패널 UI 갱신 (인가 소스, 현재 ID, 다음 ID 등)
            UpdateControlUI(plan.AircraftID, decisionSource);

            // 5. Unreal(RTV)에 전체 시나리오 정보 전송 (시각화 동기화용)
            // 주의: 임무 명령(Move/Attack)을 보내는 게 아니라, "이런 계획이 있다"는 정보만 보냄
            //_ = Task.Run(() => SendScenarioMissionToUnreal(plan));

            //System.Diagnostics.Debug.WriteLine($"[계획 적용] 헬기 {plan.AircraftID} - 소스: {decisionSource}, 상태 초기화 완료.");

            if (ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance.IsAutoExecutionMode)
            {
                // UI 갱신 등 타이밍 이슈 방지를 위해 약간의 텀을 주거나 바로 실행
                // Task.Run으로 감싸서 비동기 실행 (Fire-and-Forget)
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500); // (선택사항) 사용자가 "변경되었다"는 걸 인지할 0.5초 텀
                    await ExecuteCurrentMission(plan.AircraftID);
                    //System.Diagnostics.Debug.WriteLine($"[자동 수행] 헬기 {plan.AircraftID} - 계획 인가 후 자동 시작.");
                });
            }
        }

        private async void Callback_OnPilotDecisionReceived(PilotDecision decision)
        {
            // 로그: 조종사 인가 수신 알림
            ViewModel_ScenarioView.SingletonInstance.AddLog($"[인가 수신-Manual] 조종사 결정 ID: {decision.EditOptionsIDConverter}", 1);

            OptionList selectedOption;
            lock (_optionsLock)
            {
                selectedOption = _receivedOptions.FirstOrDefault(opt => opt.OptionID == decision.EditOptionsIDConverter);
            }

            if (selectedOption != null)
            {
                // UI 초기화 (전체 클리어)
                ClearAllPlanUI();

                // LAH 계획 적용 (Retry 로직 적용)
                foreach (var lahPlanId in selectedOption.LAHMissionPlanIDList)
                {
                    LAHMissionPlan plan = null;
                    int retryCount = 0;
                    int maxRetry = 30; // 💡 [수정] 3초 (100ms * 30번) 대기

                    // 최대 3초 대기하며 캐시 확인
                    while (retryCount < maxRetry)
                    {
                        if (_lahMissionPlanCache.TryGetValue(lahPlanId, out plan))
                        {
                            break;
                        }
                        retryCount++;
                        await Task.Delay(100); // 100ms 대기 후 재시도
                    }

                    if (plan != null)
                    {
                        ApplyApprovedLahPlan(plan, "Pilot"); // 공통 메서드 호출
                    }
                    else
                    {
                        // 💡 [수정] 3초 대기 실패 시 캐시 상태와 함께 강력한 에러 로그 출력
                        string cachedIds = string.Join(", ", _lahMissionPlanCache.Keys);
                        string errorMsg = $"[패킷 유실 의심] 조종사 인가된 LAH Plan ID {lahPlanId}가 3초 대기 후에도 수신되지 않았습니다. (현재 캐시: {cachedIds})";
                        ViewModel_ScenarioView.SingletonInstance.AddLog(errorMsg, 4);
                    }
                }

                // UAV 계획 적용 (Retry 로직 적용)
                foreach (var uavPlanId in selectedOption.UAVMissionPlanIDList)
                {
                    UAVMissionPlan uavPlan = null;
                    int retryCount = 0;
                    int maxRetry = 30; // 💡 [수정] 3초 대기

                    while (retryCount < maxRetry)
                    {
                        if (_uavMissionPlanCache.TryGetValue(uavPlanId, out uavPlan))
                        {
                            break;
                        }
                        retryCount++;
                        await Task.Delay(100);
                    }

                    if (uavPlan != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance.UAVMissionPlanItemSource.Add(uavPlan);
                        });
                    }
                    else
                    {
                        // 💡 [수정] 캐시 상태 포함 에러 로깅
                        string cachedIds = string.Join(", ", _uavMissionPlanCache.Keys);
                        string errorMsg = $"[패킷 유실 의심] 조종사 인가된 UAV Plan ID {uavPlanId}가 3초 대기 후에도 수신되지 않았습니다. (현재 캐시: {cachedIds})";
                        ViewModel_ScenarioView.SingletonInstance.AddLog(errorMsg, 4);
                    }
                }
            }
            else
            {
                ViewModel_ScenarioView.SingletonInstance.AddLog($"[경고] 수신된 옵션 목록에서 ID {decision.EditOptionsIDConverter}를 찾을 수 없습니다.", 2);
            }
        }

        private async void Callback_OnMissionUpdateWithoutDecisionReceived(MissionUpdatewithoutPilotDecision update)
        {
            ViewModel_ScenarioView.SingletonInstance.AddLog($"[인가 수신-System] 자동 갱신 메시지(53114) 수신", 1);

            if (update != null)
            {
                // UI 초기화 (전체 클리어)
                ClearAllPlanUI();

                // LAH 계획 적용 (Retry 로직 적용)
                foreach (var lahPlanId in update.LAHMissionPlanIDList)
                {
                    LAHMissionPlan plan = null;
                    int retryCount = 0;
                    int maxRetry = 30; // 💡 [수정] 3초 대기

                    while (retryCount < maxRetry)
                    {
                        if (_lahMissionPlanCache.TryGetValue(lahPlanId, out plan))
                        {
                            break;
                        }
                        retryCount++;
                        await Task.Delay(100);
                    }

                    if (plan != null)
                    {
                        ApplyApprovedLahPlan(plan, "System");
                    }
                    else
                    {
                        // 💡 [수정] 캐시 상태 포함 에러 로깅
                        string cachedIds = string.Join(", ", _lahMissionPlanCache.Keys);
                        string errorMsg = $"[패킷 유실 의심] 자동갱신 LAH Plan ID {lahPlanId}가 3초 대기 후에도 수신되지 않았습니다. (현재 캐시: {cachedIds})";
                        ViewModel_ScenarioView.SingletonInstance.AddLog(errorMsg, 4);
                    }
                }

                // UAV 계획 적용 (Retry 로직 적용)
                foreach (var uavPlanId in update.UAVMissionPlanIDList)
                {
                    UAVMissionPlan uavPlan = null;
                    int retryCount = 0;
                    int maxRetry = 30; // 💡 [수정] 3초 대기

                    while (retryCount < maxRetry)
                    {
                        if (_uavMissionPlanCache.TryGetValue(uavPlanId, out uavPlan))
                        {
                            break;
                        }
                        retryCount++;
                        await Task.Delay(100);
                    }

                    if (uavPlan != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance.UAVMissionPlanItemSource.Add(uavPlan);
                        });

                        // UAV Test Mode 확인 후 언리얼 전송 로직 (기존 유지)
                        var targetUnit = ViewModel_ScenarioView.SingletonInstance.model_UnitScenario.UnitObjectList
                                            .FirstOrDefault(x => x.ID == uavPlan.AircraftID);

                        if (targetUnit != null && targetUnit.UAVTestMode == 1)
                        {
                            if (uavPlan.AircraftID == 4 || uavPlan.AircraftID == 5)
                            {
                                ViewModel_ScenarioView.SingletonInstance.AddLog($"[UAV 전송] ID {uavPlan.AircraftID} 임무를 언리얼로 전송합니다.", 3);
                                _ = SendScenarioMissionToUnreal(uavPlan);
                            }
                        }
                    }
                    else
                    {
                        // 💡 [수정] 캐시 상태 포함 에러 로깅
                        string cachedIds = string.Join(", ", _uavMissionPlanCache.Keys);
                        string errorMsg = $"[패킷 유실 의심] 자동갱신 UAV Plan ID {uavPlanId}가 3초 대기 후에도 수신되지 않았습니다. (현재 캐시: {cachedIds})";
                        ViewModel_ScenarioView.SingletonInstance.AddLog(errorMsg, 4);
                    }
                }
            }
        }

        // UI 클리어 헬퍼
        private void ClearAllPlanUI()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance.LAHMissionPlanItemSource.Clear();
                ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance.MissionSegmentItemSource.Clear();
                ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance.IndividualMissionPlanItemSource.Clear();
                ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance.WayPointLAHItemSource.Clear();

                ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance.UAVMissionPlanItemSource.Clear();
                ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance.MissionSegmentItemSource.Clear();
                ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance.IndividualMissionPlanItemSource.Clear();
                ViewModel_UC_Unit_UAVMissionPlan.SingletonInstance.WayPointUAVItemSource.Clear();
            });
        }

        public void ClearReplanCacheAndOptions()
        {
            _lahMissionPlanCache.Clear();
            _uavMissionPlanCache.Clear();
            lock (_optionsLock)
            {
                _receivedOptions.Clear();
            }
            _isNewImplicitUpdateSequence = true; // 상태 플래그도 리셋

            //System.Diagnostics.Debug.WriteLine("[캐시 초기화] 모든 재계획 캐시와 옵션이 삭제되었습니다.");
        }

        public async Task SendScenarioMissionToUnreal(LAHMissionPlan InputPlan)
        {
            var sendmessage = Convert_LAHMissionPlan_To_GrpcLAH(InputPlan);
            var anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(sendmessage);
            var notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ScenarioMissionResponse",
                    Parameter = anyMessage,
                }
            };
            //await Task.Delay(500);
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(notification);
        }

        //롤백용으로 유지
        //public ScenarioMissionResponse Convert_LAHMissionPlan_To_GrpcLAH(LAHMissionPlan Input)
        //{
        //    var grpc_lah = new ScenarioMissionResponse();
        //    grpc_lah.Message = "ScenarioMissionResponse";

        //    if (Input.MissionSegemntList == null) return grpc_lah;

        //    // [수정] 모든 '협업기저임무'를 순회하도록 foreach 루프 추가
        //    foreach (var segment in Input.MissionSegemntList)
        //    {
        //        if (segment.IndividualMissionList == null) continue;

        //        // [수정] 모든 '개별임무'를 순회하도록 foreach 루프 추가
        //        foreach (var individualMission in segment.IndividualMissionList)
        //        {
        //            if (individualMission.WaypointList == null) continue;

        //            // [유지] 모든 '경로점'을 순회하는 기존 로직
        //            foreach (var item in individualMission.WaypointList)
        //            {
        //                var grpc_lah_item = new ScenarioMission();
        //                grpc_lah_item.Type = "LAH";
        //                grpc_lah_item.Id = (int)Input.AircraftID;

        //                grpc_lah_item.MissionType = enumMissionType.MissionMovetogps;
        //                grpc_lah_item.Mission = new Vector4
        //                {
        //                    X = item.Coordinate.Latitude,
        //                    Y = item.Coordinate.Longitude,
        //                    Z = item.Coordinate.Altitude,
        //                    W = item.Speed
        //                };
        //                // gRPC 메시지 타입에 맞게 데이터 할당
        //                if (item.Attack != null)
        //                {
        //                    if(item.Attack.TargetID != 0)
        //                    {
        //                        grpc_lah_item.MissionType = enumMissionType.MissionAttacktoHydra;
        //                        grpc_lah_item.Mission = new Vector4
        //                        {
        //                            X = item.Attack.TargetID,
        //                            Y = 0,
        //                            Z = 0,
        //                            W = 0
        //                        };
        //                    }
        //                }

        //                grpc_lah_item.Onwaittime = (int)item.Hovering; // 제자리 비행 시간

        //                // TODO: 필요 시 Waypointpasstype, 공격 정보 등 추가 변환 로직 구현

        //                grpc_lah.MissionList.Add(grpc_lah_item);
        //            }
        //        }
        //    }

        //    return grpc_lah;
        //}
        public ScenarioMissionResponse Convert_LAHMissionPlan_To_GrpcLAH(LAHMissionPlan Input)
        {
            var grpc_lah = new ScenarioMissionResponse();
            grpc_lah.Message = "ScenarioMissionResponse";

            if (Input.MissionSegemntList == null) return grpc_lah;

            foreach (var segment in Input.MissionSegemntList)
            {
                if (segment.IndividualMissionList == null) continue;

                foreach (var individualMission in segment.IndividualMissionList)
                {
                    if (individualMission.WaypointList == null) continue;

                    foreach (var item in individualMission.WaypointList)
                    {
                        // ---------------------------------------------------------
                        // 1. [이동 명령] 먼저 해당 좌표로 이동하는 명령을 생성합니다.
                        // ---------------------------------------------------------
                        var moveMission = new ScenarioMission();
                        moveMission.Type = "LAH";
                        moveMission.Id = (int)Input.AircraftID;

                        // 기본은 무조건 GPS 이동
                        moveMission.MissionType = enumMissionType.MissionMovetogps;

                        // 이동 좌표 및 속도 설정
                        moveMission.Mission = new Vector4
                        {
                            X = item.Coordinate.Latitude,
                            Y = item.Coordinate.Longitude,
                            Z = item.Coordinate.Altitude,
                            W = item.Speed
                        };

                        // 공격 명령이 있으면 fly-over(0), 없으면 fly-by(1)
                        bool hasAttack = item.Attack != null && item.Attack.TargetID != 0;
                        moveMission.Waypointpasstype = hasAttack ? 0 : 1;

                        // 제자리 비행 시간은 '이동' 명령의 속성으로 넣습니다 (도착 후 대기)
                        moveMission.Onwaittime = (int)item.Hovering;

                        // 리스트에 이동 명령 추가
                        grpc_lah.MissionList.Add(moveMission);


                        // ---------------------------------------------------------
                        // 2. [공격 명령] 공격 타겟이 존재할 경우, 이동 명령 직후에 공격 명령을 추가합니다.
                        // ---------------------------------------------------------
                        if (hasAttack)
                        {
                            var attackMission = new ScenarioMission();
                            attackMission.Type = "LAH";
                            attackMission.Id = (int)Input.AircraftID;

                            // 공격 타입 설정 (예: Hydra)
                            attackMission.MissionType = enumMissionType.MissionAttacktoHydra;

                            // 공격 명령의 파라미터 설정 (X에 TargetID)
                            attackMission.Mission = new Vector4
                            {
                                X = item.Attack.TargetID,
                                Y = 0,
                                Z = 0,
                                W = 0
                            };

                            // 공격 명령에는 별도의 대기 시간이 필요 없다면 0 혹은 필요한 값 설정
                            attackMission.Onwaittime = 0;

                            // 리스트에 공격 명령 추가 (이동 명령 바로 뒤에 붙음)
                            grpc_lah.MissionList.Add(attackMission);
                        }
                    }
                }
            }

            return grpc_lah;
        }

        // -----------------------------------------------------------------------
        // [신규] UAV용 전송 메서드 (오버로딩)
        // -----------------------------------------------------------------------
        public async Task SendScenarioMissionToUnreal(UAVMissionPlan InputPlan)
        {
            // 1. UAV 전용 변환 함수 호출
            var sendmessage = Convert_UAVMissionPlan_To_Grpc(InputPlan);

            // 2. 패킹 및 전송 (기존과 동일)
            var anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(sendmessage);
            var notification = new Notification
            {
                NotifType = enumNotifType.NotifMessageReceived,
                MessageReceived = new MessageReceived
                {
                    Name = "ScenarioMissionResponse",
                    Parameter = anyMessage,
                }
            };
            await gRPCModule.SingletonInstance.ServiceImplementation.SendServerMessage(notification);

            //System.Diagnostics.Debug.WriteLine($"[UAV 전송] ID {InputPlan.AircraftID} 임무 언리얼로 전송 완료.");
        }

        // -----------------------------------------------------------------------
        // [신규] UAV 계획 -> gRPC 메시지 변환 (카메라 로직 포함)
        // -----------------------------------------------------------------------
        public ScenarioMissionResponse Convert_UAVMissionPlan_To_Grpc(UAVMissionPlan Input)
        {
            var grpc_msg = new ScenarioMissionResponse();
            grpc_msg.Message = "ScenarioMissionResponse";

            if (Input.MissionSegemntList == null) return grpc_msg;

            // ★ 표적 위치 (이미지 기준)
            double targetLat = 38.12352;
            double targetLon = 127.30367;

            // ★ 표적 추적용 ID
            //int targetID = 7;
            int targetID = 8;

            // ★ 임무 지역 내 웨이포인트 순서 카운터
            int missionWpIndex = 0;

            foreach (var segment in Input.MissionSegemntList)
            {
                if (segment.IndividualMissionList == null) continue;

                foreach (var individualMission in segment.IndividualMissionList)
                {
                    if (individualMission.WaypointList == null) continue;

                    foreach (var item in individualMission.WaypointList)
                    {
                        var missionItem = new ScenarioMission();
                        missionItem.Type = "UAV";
                        missionItem.Id = (int)Input.AircraftID;
                        missionItem.MissionID = (int)individualMission.IndividualMissionID;
                        missionItem.WaypointID = (int)item.WaypoinID;

                        // 1. 이동 명령
                        missionItem.MissionType = enumMissionType.MissionMovetogps;
                        missionItem.Mission = new Vector4
                        {
                            X = item.Coordinate.Latitude,
                            Y = item.Coordinate.Longitude,
                            Z = item.Coordinate.Altitude,
                            W = item.Speed
                        };

                        // 2. 카메라 액션 설정
                        missionItem.Cameramission = new CameraAction();

                        // ★ 임무 지역 Segment ID 체크 (70000000 대역)
                        if (segment.MissionSegmentID >= 70000000 && segment.MissionSegmentID < 71000000)
                        {
                            // 순서(Index)에 따른 모드 제어
                            if (missionWpIndex == 0)
                            {
                                // [Phase 1] 첫 번째 점까지 이동: 기체 고정
                                // 요청하신 snippet에 따라 Mode 2, X(Pitch?) = -50 적용
                                missionItem.Cameramission.Mode = 2; // 기체 고정 (또는 Angle 고정)
                                missionItem.Cameramission.Fov = 5;
                                missionItem.Cameramission.X = -80; // Pitch Down (요청 값 유지)
                            }
                            else if (missionWpIndex == 1)
                            {
                                // [Phase 2] 두 번째 점까지 이동: 스위핑 (표적 탐색)
                                missionItem.Cameramission.Mode = 1; // Sweep
                                missionItem.Cameramission.Sweeptime = 1.0;
                                missionItem.Cameramission.Fov = 10;

                                // 표적 중심 대각선 스위핑 박스
                                missionItem.Cameramission.X = targetLat + 0.002; // P1 Lat
                                missionItem.Cameramission.Y = targetLon - 0.002; // P1 Lon
                                missionItem.Cameramission.Z = targetLat - 0.002; // P2 Lat
                                missionItem.Cameramission.W = targetLon + 0.002; // P2 Lon
                            }
                            else
                            {
                                // [Phase 3] 두 번째 점 도착(세 번째 점 이동) 이후: 표적 추적
                                // (missionWpIndex >= 2)
                                missionItem.Cameramission.Mode = 3; // Tracking
                                missionItem.Cameramission.X = targetID; // Target ID (7)
                                missionItem.Cameramission.Fov = 10;
                            }

                            // 인덱스 증가
                            missionWpIndex++;
                        }
                        else
                        {
                            // 그 외 지역 (복귀 등): 기본 정면 고정
                            missionItem.Cameramission.Mode = 2; // Default Fixed
                            missionItem.Cameramission.Fov = 15;
                            missionItem.Cameramission.Y = -50;
                        }

                        grpc_msg.MissionList.Add(missionItem);
                    }
                }
            }

            return grpc_msg;
        }

        public async  void Callback_71303(UdpReceiveResult result)
        {
            // OperatingCommand의 총 길이 (바이트 수)
            const int expectedLength = 8;
            if (result.Buffer.Length < expectedLength)
            {
                throw new ArgumentException("수신된 버퍼의 길이가 OperatingCommand에 부족합니다.");
            }

            OperatingCommand command = new OperatingCommand();
            //int offset = 0;

            // 순서대로 각 필드 파싱 (리틀 엔디언을 가정)
            //command.MessageID = BitConverter.ToUInt32(result.Buffer, offset);
            //offset += 4;

            //command.Command = BitConverter.ToUInt32(result.Buffer, offset);
            //offset += 4;

            command.MessageID = CommonUtil.ReadUInt32BigEndian(result.Buffer, 0);
            //offset += 4;

            command.Command = CommonUtil.ReadUInt32BigEndian(result.Buffer, 4);


            //if (command.Command == 1)
            //{
            //    var confirmedLahPlans = ViewModel_UC_Unit_LAHMissionPlan.SingletonInstance.LAHMissionPlanItemSource.ToList();

            //    foreach (var plan in confirmedLahPlans)
            //    {
            //        // 각 확정된 계획을 Unreal에 전송합니다.
            //        await SendScenarioMissionToUnreal(plan);
            //    }
            //}
            //else if (command.Command == 2)
            //{

            //}
        }

        private void Callback_Observation_messageQueue_Available()
        {

        }

        public byte[] SerializeSBC2Status(SBC2Status status)
        {
            // MemoryStream과 BigEndianBinaryWriter를 사용하여 직렬화
            using (var ms = new MemoryStream())
            using (var bw = new CommonUtil.BigEndianBinaryWriter(ms)) // 기존에 기억한 CommonUtil의 BigEndianBinaryWriter 사용
            {
                bw.Write(status.MessageID);         // uint (4 bytes)
                bw.Write(status.PresenceVector);    // byte (1 byte)
                bw.Write(status.Timestamp);         // byte[5] (5 bytes)
                bw.Write(status.Status);            // uint (4 bytes)
                bw.Write(status.Mode);              // uint (4 bytes)
                bw.Flush();
                return ms.ToArray();
            }
        }

        public async Task StartSendingSBC2StatusAsync(string targetIp, int targetPort, CancellationToken token)
        {
            // 전송할 SBC2Status 메시지 객체를 미리 생성
            var statusMessage = new SBC2Status
            {
                // MessageID는 클래스에 정의된 52100으로 자동 설정됩니다.
                Timestamp = new byte[5], // null이 아니도록 초기화
                Status = 1,              // 요청대로 '정상' 상태인 1로 고정
                Mode = 1                 // 요청대로 1로 고정
            };

            // CancellationToken이 취소를 요청할 때까지 무한 반복합니다.
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // 1. 메시지 객체를 빅 엔디안 바이트 배열로 직렬화합니다.
                    byte[] data = SerializeSBC2Status(statusMessage);

                    // 2. UDPModule을 사용해 메시지를 비동기적으로 전송합니다.
                    await UDPModule.SingletonInstance.SendUDPMessageAsync(data, targetIp, targetPort);

                    // 3. 1초(1000ms) 동안 대기합니다.
                    await Task.Delay(1000, token);
                }
                catch (TaskCanceledException)
                {
                    // Task.Delay 중에 취소 요청이 들어오면 루프를 정상적으로 빠져나갑니다.
                    break;
                }
                catch (Exception ex)
                {
                    // 전송 중 다른 오류가 발생하면 콘솔에 출력하고, 루프는 계속됩니다.
                    //System.Diagnostics.Debug.WriteLine($"SBC2Status 전송 오류: {ex.Message}");
                    // 오류 발생 시에도 1초 대기 후 재시도
                    await Task.Delay(1000, token);
                }
            }
        }

        public MissionPlanOptionInfo ParseMissionPlanOptionInfo(byte[] buffer)
        {
            var optionInfo = new MissionPlanOptionInfo();
            ReadOnlySpan<byte> span = buffer;
            int offset = 0;

            // 기본 메시지 정보 파싱
            optionInfo.MessageID = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
            offset += 4;

            // PresenceVector (1 byte) - 클래스 기본값이 있지만, 스트림에서 읽어서 이동
            offset += 1;

            // Timestamp (5 bytes) - 클래스 기본값이 있지만, 스트림에서 읽어서 이동
            offset += 5;

            // AutoExecution (bool, 1 byte로 처리)
            optionInfo.AutoExecution = span[offset] != 0;
            offset += 1;

            // OptionList의 개수 (N)
            optionInfo.OptionListN = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
            offset += 4;

            // OptionListN 만큼 반복하여 각 OptionList 파싱
            for (int i = 0; i < optionInfo.OptionListN; i++)
            {
                var option = new OptionList();

                option.OptionID = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
                offset += 4;

                option.Recommend = span[offset] != 0;
                offset += 1;

                option.OptionName = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
                offset += 4;

                option.SurvivalRate = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset));
                offset += 4;

                option.TimeContraction = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset));
                offset += 4;

                option.RecogEffectiveness = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset));
                offset += 4;

                option.FuelWarning = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset));
                offset += 4;

                option.Distance = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
                offset += 4;

                option.Target = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
                offset += 4;

                // UAV Mission Plan ID 리스트 파싱
                option.UAVMissionPlanIDListN = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
                offset += 4;
                for (int j = 0; j < option.UAVMissionPlanIDListN; j++)
                {
                    option.UAVMissionPlanIDList.Add(BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset)));
                    offset += 4;
                }

                // LAH Mission Plan ID 리스트 파싱
                option.LAHMissionPlanIDListN = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
                offset += 4;
                for (int k = 0; k < option.LAHMissionPlanIDListN; k++)
                {
                    option.LAHMissionPlanIDList.Add(BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset)));
                    offset += 4;
                }

                optionInfo.OptionList.Add(option);
            }

            return optionInfo;
        }

        /// <summary>
        /// 시나리오 데이터를 빅 엔디안 바이너리로 직렬화하는 기능을 제공합니다.
        /// </summary>
        public static class ScenarioSerializer
        {
            /// <summary>
            /// InitScenario 객체를 빅 엔디안 바이트 배열로 직렬화합니다.
            /// </summary>
            /// <param name="scenario">직렬화할 InitScenario 객체</param>
            /// <returns>빅 엔디안으로 변환된 바이트 배열</returns>
            public static byte[] SerializeInitScenario(InitScenario scenario)
            {
                // MemoryStream과 BigEndianBinaryWriter를 사용하여 바이트 단위로 씁니다.
                using (var ms = new MemoryStream())
                using (var bw = new CommonUtil.BigEndianBinaryWriter(ms))
                {
                    // ─── InitScenario ─────────────────────────────────────────
                    bw.Write(scenario.MessageID);

                    // ─── InputMissionPackage 직렬화 ───────────────────────────
                    var inputPackage = scenario.InputMissionPackage;
                    bw.Write(inputPackage.PresenceVector);
                    bw.Write(inputPackage.Timestamp); // 5-byte array
                    bw.Write(inputPackage.InputMissionPackageID);
                    bw.Write(inputPackage.DateAndNight);

                    // InputMissionList의 개수를 먼저 씁니다.
                    bw.Write(inputPackage.InputMissionListN);
                    if (inputPackage.InputMissionList != null)
                    {
                        foreach (var mission in inputPackage.InputMissionList)
                        {
                            bw.Write(mission.InputMissionID);
                            bw.Write(mission.SequenceNumber);
                            bw.Write(mission.InputMissionType);
                            bw.Write(mission.RegionType);
                            bw.Write(mission.IsDone);
                            bw.Write(mission.ShapeType);

                            // ShapeType에 따라 분기하여 직렬화합니다.
                            switch (mission.ShapeType)
                            {
                                case 1: // 점
                                    WriteCoordinateInfo(bw, mission.Coordinate);
                                    break;
                                case 2: // 선
                                    WritePolyLineInfo(bw, mission.PolyLine);
                                    break;
                                case 3: // 면
                                    WritePolyGon(bw, mission.Polygons);
                                    break;
                            }
                        }
                    }

                    // ─── MissionReferencePackage 직렬화 ───────────────────────
                    var refPackage = scenario.MissionReferencePackage;
                    bw.Write(refPackage.PresenceVector);
                    bw.Write(refPackage.Timestamp); // 5-byte array

                    // TakeOverInfoList 직렬화
                    bw.Write(refPackage.TakeOverInfoListN);
                    if (refPackage.TakeOverInfoList != null)
                    {
                        foreach (var info in refPackage.TakeOverInfoList)
                        {
                            bw.Write(info.AircraftID);
                            WriteCoordinateInfo(bw, info.CoordinateList);
                        }
                    }

                    // HandOverInfoList 직렬화
                    bw.Write(refPackage.HandOverInfoListN);
                    if (refPackage.HandOverInfoList != null)
                    {
                        foreach (var info in refPackage.HandOverInfoList)
                        {
                            bw.Write(info.AircraftID);
                            WriteCoordinateInfo(bw, info.CoordinateList);
                        }
                    }

                    // RTBCoordinateList 직렬화
                    bw.Write(refPackage.RTBCoordinateListN);
                    if (refPackage.RTBCoordinateList != null)
                    {
                        foreach (var info in refPackage.RTBCoordinateList)
                        {
                            bw.Write(info.Latitude);
                            bw.Write(info.Longitude);
                            bw.Write(info.Altitude);
                        }
                    }

                    // FlightAreaList 직렬화
                    bw.Write(refPackage.FlightAreaListN);
                    if (refPackage.FlightAreaList != null)
                    {
                        foreach (var area in refPackage.FlightAreaList)
                        {
                            bw.Write(area.AreaLatLonListN);
                            foreach (var latlon in area.AreaLatLonList)
                            {
                                bw.Write(latlon.Latitude);
                                bw.Write(latlon.Longitude);
                            }
                            bw.Write(area.AltitudeLimits.LowerLimit);
                            bw.Write(area.AltitudeLimits.UpperLimit);
                        }
                    }

                    // ProhibitedAreaList 직렬화
                    bw.Write(refPackage.ProhibitedAreaListN);
                    if (refPackage.ProhibitedAreaList != null)
                    {
                        foreach (var area in refPackage.ProhibitedAreaList)
                        {
                            bw.Write(area.AreaLatLonListN);
                            foreach (var latlon in area.AreaLatLonList)
                            {
                                bw.Write(latlon.Latitude);
                                bw.Write(latlon.Longitude);
                            }
                            bw.Write(area.AltitudeLimits.LowerLimit);
                            bw.Write(area.AltitudeLimits.UpperLimit);
                        }
                    }

                    bw.Flush();
                    return ms.ToArray();
                }
            }

            // --- Helper methods for serialization ---
            private static void WriteCoordinateInfo(CommonUtil.BigEndianBinaryWriter bw, CoordinateInfo coord)
            {
                bw.Write(coord.Latitude);
                bw.Write(coord.Longitude);
                bw.Write(coord.Altitude);
            }

            private static void WritePolyLineInfo(CommonUtil.BigEndianBinaryWriter bw, PolyLineInfo polyline)
            {
                bw.Write(polyline.Width);
                bw.Write(polyline.CoordinateListN);
                if (polyline.CoordinateList != null)
                {
                    foreach (var coord in polyline.CoordinateList)
                    {
                        WriteCoordinateInfo(bw, coord);
                    }
                }
            }

            private static void WritePolyGon(CommonUtil.BigEndianBinaryWriter bw, PolyGon polygon)
            {
                bw.Write(polygon.AreaListN);
                if (polygon.AreaList != null)
                {
                    foreach (var area in polygon.AreaList)
                    {
                        bw.Write(area.IsHole);
                        bw.Write(area.CoordinateListN);
                        if (area.CoordinateList != null)
                        {
                            foreach (var coord in area.CoordinateList)
                            {
                                WriteCoordinateInfo(bw, coord);
                            }
                        }
                    }
                }
            }


        }

    }

    public class HelicopterExecutionState
    {
        public LAHMissionPlan Plan { get; set; }
        public int CurrentSegmentIndex { get; set; } = 0;
        public int CurrentIndividualIndex { get; set; } = 0;

        // 모든 임무가 끝났는지 여부
        public bool IsAllFinished { get; set; } = false;

        // 현재 수행 중(또는 대기 중)인 개별 임무 객체 반환
        public IndividualMissionLAH GetCurrentMission()
        {
            if (IsAllFinished || Plan == null || Plan.MissionSegemntList == null || Plan.MissionSegemntList.Count == 0)
                return null;

            if (CurrentSegmentIndex >= Plan.MissionSegemntList.Count)
                return null;

            var segment = Plan.MissionSegemntList[CurrentSegmentIndex];
            if (segment.IndividualMissionList == null || CurrentIndividualIndex >= segment.IndividualMissionList.Count)
                return null;

            return segment.IndividualMissionList[CurrentIndividualIndex];
        }

        // 다음 수행할 개별 임무 객체 반환 (UI 표시용)
        public IndividualMissionLAH GetNextMission()
        {
            if (IsAllFinished || Plan == null) return null;

            var segment = Plan.MissionSegemntList[CurrentSegmentIndex];

            // 1. 현재 세그먼트에 다음 미션이 있는 경우
            if (segment.IndividualMissionList != null && CurrentIndividualIndex + 1 < segment.IndividualMissionList.Count)
            {
                return segment.IndividualMissionList[CurrentIndividualIndex + 1];
            }

            // 2. 다음 세그먼트로 넘어가서 첫 번째 미션을 찾는 경우
            if (CurrentSegmentIndex + 1 < Plan.MissionSegemntList.Count)
            {
                var nextSegment = Plan.MissionSegemntList[CurrentSegmentIndex + 1];
                if (nextSegment.IndividualMissionList != null && nextSegment.IndividualMissionList.Count > 0)
                {
                    return nextSegment.IndividualMissionList[0];
                }
            }

            return null; // 다음 임무 없음 (현재가 마지막)
        }
    }

}



