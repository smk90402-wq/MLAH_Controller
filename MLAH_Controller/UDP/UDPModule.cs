using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Protocol;
using MLAH_Controller;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using static MLAH_Controller.CommonUtil;

namespace MLAH_Controller
{
    public class UDPModule : IDisposable
    {
        // 수신용 UdpClient (예: 포트 9000)
        //private UdpClient _receiver;

        #region Singleton
        //private static Model_MainView _Model_MainView = null;
        private static readonly Lazy<UDPModule> _lazyInstance = new Lazy<UDPModule>(() => new UDPModule(), true);

        /// <summary>.
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>.
        public static UDPModule SingletonInstance
        {
            get { return _lazyInstance.Value; }
        }

        #endregion Singleton

        private UdpClient _receiver;

        //private UdpClient _receiver_Display;

        private UdpClient _sender;

        // <포트 번호, UdpClient 객체> 형태로 수신기들을 관리
        private Dictionary<int, UdpClient> _receivers = new Dictionary<int, UdpClient>();

        // 패킷을 담아둘 고성능 비동기 큐 (채널)
        private Channel<UdpReceiveResult> _packetChannel;

        private readonly CancellationTokenSource _cts;
        private readonly object _receiverLock = new object();

        private readonly CancellationTokenSource _cts_Display;
        private readonly object _receiverLock_Display = new object();

        // [추가] 0xAA 0x55 패킷 조립을 위한 상태 관리 클래스
        private class LegacyAssembler
        {
            // 순서가 뒤섞여 들어와도 인덱스별로 저장하기 위한 딕셔너리
            public Dictionary<int, byte[]> Buffer = new Dictionary<int, byte[]>();

            // End 패킷이 들어왔을 때의 마지막 인덱스 번호 (아직 안 들어왔으면 -1)
            public int MaxIndex = -1;

            // 타임아웃 처리를 위한 마지막 수신 시간
            public DateTime LastReceivedTime = DateTime.Now;

            public void Clear()
            {
                Buffer.Clear();
                MaxIndex = -1;
                LastReceivedTime = DateTime.Now;
            }
        }

        // [추가] UDPModule 클래스 멤버 변수로 선언
        private LegacyAssembler _legacyAssembler = new LegacyAssembler();
        private readonly object _legacyLock = new object();

        /// <summary>
        /// UDP 수신기가 성공적으로 시작되었는지 여부
        /// </summary>
        public bool IsListening { get; private set; } = false;

        public UDPModule()
        {
            _cts = new CancellationTokenSource();
            _sender = new UdpClient();
        }

        private void InitializeChannelAndConsumers()
        {
            var channelOptions = new BoundedChannelOptions(1000) 
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleWriter = true,
                SingleReader = true,
            };
            _packetChannel = Channel.CreateBounded<UdpReceiveResult>(channelOptions);

            // Consumer는 여러 개를 실행해도 모든 Channel의 데이터를 처리함
            Task.Run(() => ConsumerLoopAsync(_cts.Token));
            //Task.Run(() => ConsumerLoopAsync(_cts.Token));
        }

        /// <summary>
        /// (버전 1) IP 주소와 포트를 지정하여 수신기를 초기화하는 핵심 메서드
        /// </summary>
        public bool InitializeReceiver(string ipAddress, int port)
        {
            if (_receivers.ContainsKey(port)) return true;

            try
            {
                if (_packetChannel == null)
                {
                    InitializeChannelAndConsumers();
                }

                // 매개변수로 받은 ipAddress를 파싱하여 사용
                var bindIp = IPAddress.Parse(ipAddress);
                var newReceiver = new UdpClient(new IPEndPoint(bindIp, port));
                newReceiver.Client.ReceiveBufferSize = int.MaxValue;
                _receivers.Add(port, newReceiver);

                Task.Run(() => ProducerLoopAsync(newReceiver, _cts.Token));

                Debug.WriteLine($"UDP 수신기가 {ipAddress}:{port} 포트에서 성공적으로 시작되었습니다.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UDP 수신기 ({ipAddress}:{port}) 초기화 실패: {ex.Message}");
                return false;
            }
        }

        // 어떤 UdpClient를 사용할지 receiver 매개변수로 받음
        private async Task ProducerLoopAsync(UdpClient receiver, CancellationToken token)
        {
            var writer = _packetChannel.Writer;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // 전달받은 receiver를 사용해 데이터 수신
                    UdpReceiveResult result = await receiver.ReceiveAsync();
                    await writer.WriteAsync(result, token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { /* ... */ }
            }
        }

        private async Task ProducerLoopAsync(CancellationToken token)
        {
            // 채널의 Writer를 가져옴
            var writer = _packetChannel.Writer;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult result = await _receiver.ReceiveAsync();
                    // 채널에 쓰기를 시도. 채널이 꽉 찼으면 대기.
                    await writer.WriteAsync(result, token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"UDP 수신 오류: {ex.Message}");
                }
            }
        }


        /// <summary>
        /// 송신측 수정이 불가능한 0xAA 0x55 헤더 패킷 처리
        /// </summary>
        /// <returns>패킷을 처리했으면 true, 아니면 false</returns>
        private bool local_OnReceive(byte[] data)
        {
            // 1. 최소 길이 및 매직 넘버 체크 (헤더 8바이트)
            if (data.Length < 8) return false;
            if (data[0] != 0xAA || data[1] != 0x55) return false;

            // 2. 헤더 파싱
            bool isStart = data[2] == 1;
            bool isEnd = data[3] == 1;
            int index = BitConverter.ToInt32(data, 4);

            // 3. Payload 분리
            int payloadLen = data.Length - 8;
            if (payloadLen < 0) return false; // 데이터가 깨진 경우 방어

            byte[] payload = new byte[payloadLen];
            Array.Copy(data, 8, payload, 0, payloadLen);

            // 4. 조립 로직 (Thread-Safe)
            //lock (_legacyLock)
            {
                // (1) 시작 패킷이면 무조건 버퍼 초기화 (새로운 메시지의 시작으로 간주)
                // 주의: 송신측이 고유 ID를 안 보내므로, 중간에 새 메시지가 끼어들면 
                // 기존 메시지는 버려지고 새 메시지로 덮어씌워집니다 (최선의 방어책).
                if (isStart)
                {
                    _legacyAssembler.Clear();
                }

                // (2) 데이터 저장 (이미 받은 인덱스면 덮어씌움)
                _legacyAssembler.Buffer[index] = payload;
                _legacyAssembler.LastReceivedTime = DateTime.Now;

                // (3) 끝 패킷이면 MaxIndex 설정
                if (isEnd)
                {
                    _legacyAssembler.MaxIndex = index;
                }

                // (4) 조립 완성 조건 검사
                // 조건: End 패킷을 받았고(MaxIndex != -1) && 저장된 조각 개수가 (MaxIndex + 1)개 인가?
                if (_legacyAssembler.MaxIndex != -1 &&
                    _legacyAssembler.Buffer.Count == (_legacyAssembler.MaxIndex + 1))
                {
                    try
                    {
                        // 누락된 인덱스가 없는지 한 번 더 체크 (완전 무결성 검증)
                        using (MemoryStream ms = new MemoryStream())
                        {
                            for (int i = 0; i <= _legacyAssembler.MaxIndex; i++)
                            {
                                if (_legacyAssembler.Buffer.TryGetValue(i, out byte[] chunk))
                                {
                                    ms.Write(chunk, 0, chunk.Length);
                                }
                                else
                                {
                                    // 이론상 여기 올 수 없지만, 만약 발생하면 조립 실패 처리
                                    Debug.WriteLine($"[Legacy] 조각 누락됨. Index: {i}");
                                    return true;
                                }
                            }

                            // (5) 완성된 바이트 배열 생성
                            byte[] completeData = ms.ToArray();

                            // (6) 버퍼 비우기 (다음 메시지 준비)
                            _legacyAssembler.Clear();

                            // (7) 완성된 데이터를 JSON 처리 로직으로 넘김
                            //ProcessCompleteJsonMessage(completeData);
                            _ = Task.Run(() => ProcessCompleteJsonMessage(completeData));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Legacy] 조립 실패: {ex.Message}");
                        // 에러가 나더라도 버퍼는 비워주는 것이 안전함
                        _legacyAssembler.Clear();
                    }
                }
            }

            return true; // 0xAA 0x55 패킷이었으므로 true 반환
        }

        private async Task ProcessCompleteJsonMessage(byte[] completeData)
        {
            try
            {
                string jsonString = Encoding.UTF8.GetString(completeData);

                // JSON 파싱 시도
                dynamic obj = JsonConvert.DeserializeObject(jsonString);
                if (obj == null) return;

                uint id = (uint)obj.MessageID;

                // --- 기존의 ID 분기 로직 ---
                if (id == 53111)
                {
                    LAHMissionPlan plan = JsonConvert.DeserializeObject<LAHMissionPlan>(jsonString);
                    CommonEvent.OnLAHMissionPlanReceived?.Invoke(plan);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        //var pop = new View_PopUp(10);
                        //pop.Description.Text = "메시지 수신";
                        //pop.Reason.Text = $"유인기 재계획 수신 : {plan.AircraftID}";
                        //pop.Show();
                        ViewModel_ScenarioView.SingletonInstance.AddLog($"유인기 재계획 수신 : {plan.AircraftID}", 4);
                    });
                }
                else if (id == 53112)
                {
                    UAVMissionPlan plan = JsonConvert.DeserializeObject<UAVMissionPlan>(jsonString);
                    CommonEvent.OnUAVMissionPlanReceived?.Invoke(plan);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        //var pop = new View_PopUp(10);
                        //pop.Description.Text = "메시지 수신";
                        //pop.Reason.Text = $"무인기 재계획 수신 : {plan.AircraftID}";
                        //pop.Show();
                        ViewModel_ScenarioView.SingletonInstance.AddLog($"무인기 재계획 수신 : {plan.AircraftID}", 4);
                    });
                }
                else if (id == 51310)
                {
                    InputMissionPackageJson pack = JsonConvert.DeserializeObject<InputMissionPackageJson>(jsonString);
                    ViewModel_ScenarioView.SingletonInstance.ScenarioViewClearFromMessage();
                    ViewModel_ScenarioView.SingletonInstance.ScenarioViewInit(pack);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        //var pop = new View_PopUp(10);
                        //pop.Description.Text = "메시지 수신";
                        //pop.Reason.Text = $"협업기저임무 재수행/변경";
                        //pop.Show();
                        ViewModel_ScenarioView.SingletonInstance.AddLog($"협업기저임무 재수행/변경", 1);
                    });
                }
                // ... 필요한 다른 ID들 추가 ...
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Legacy] JSON 파싱 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 소비자: 채널(큐)에서 패킷을 꺼내서 파싱하고 처리합니다.
        /// </summary>
        private async Task ConsumerLoopAsync(CancellationToken token)
        {
            // 채널의 Reader를 가져옴
            var reader = _packetChannel.Reader;

            try
            {
                // 채널에서 읽을 데이터가 있는 동안 계속 루프
                await foreach (var result in reader.ReadAllAsync(token))
                {
                    // 이 안에서 기존의 처리 로직을 그대로 수행
                    byte[] buffer = result.Buffer;

                    // =========================================================
                    // [수정] 0xAA 0x55 패킷인지 먼저 확인하고 처리
                    // =========================================================
                    if (local_OnReceive(buffer))
                    {
                        continue; // 여기서 처리 끝, 다음 패킷 대기 (아래 로직 실행 X)
                    }

                    //대용량롤백
                    //if (buffer.Length < 4) continue;

                    //대용량롤백
                    //uint headerID = BinaryPrimitives.ReadUInt32BigEndian(buffer);

                    //대용량롤백
                    // =========================================================
                    // [CASE 1] 분할 패킷 (ID: 99999) -> 조각 모음 로직으로 이동
                    // =========================================================
                    //if (headerID == ID_SPLIT_PACKET)
                    //{
                    //    HandleChunkPacket(buffer);
                    //    continue; // 여기서 처리 끝, 다음 패킷 대기
                    //}

                    var rawPacket = new PipeDataPacketUdpRaw
                    {
                        SenderIp = result.RemoteEndPoint.Address.ToString(),
                        SenderPort = result.RemoteEndPoint.Port,
                        UdpData = result.Buffer
                    };

                    //_ = NamedPipeSenderUDP.Instance.SendDataAsync(rawPacket);
                    NamedPipeSenderUDP.Instance.SendData(rawPacket);
                    

                    // (기존 if/else 로직 시작)
                    if (buffer.Length >= 1 && buffer[0] == '{')
                    {
                        string jsonString = Encoding.UTF8.GetString(buffer);
                        //dynamic obj = JsonConvert.DeserializeObject(jsonString);
                        //uint id = (uint)obj.MessageID;

                        // 1. JObject로 가볍게 구조만 읽어서 ID만 빼옵니다.
                        JObject jObj = JObject.Parse(jsonString);
                        uint id = jObj["MessageID"]?.Value<uint>() ?? 0;


                        if (id == 53111)
                        {
                            //LAHMissionPlan deserializedObject = JsonConvert.DeserializeObject<LAHMissionPlan>(jsonString);
                            LAHMissionPlan deserializedObject = jObj.ToObject<LAHMissionPlan>();
                            CommonEvent.OnLAHMissionPlanReceived?.Invoke(deserializedObject);


                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                //var pop_error = new View_PopUp(10);
                                //pop_error.Description.Text = "메시지 수신";
                                //pop_error.Reason.Text = $"유인기 재계획 수신 : {deserializedObject.AircraftID}";
                                //pop_error.Show();
                                ViewModel_ScenarioView.SingletonInstance.AddLog($"유인기 재계획 수신 : {deserializedObject.AircraftID}", 4);
                            });

                        }
                        else if (id == 53112)
                        {
                            UAVMissionPlan deserializedObject = JsonConvert.DeserializeObject<UAVMissionPlan>(jsonString);
                            CommonEvent.OnUAVMissionPlanReceived?.Invoke(deserializedObject);

                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                //var pop_error = new View_PopUp(10);
                                //pop_error.Description.Text = "메시지 수신";
                                //pop_error.Reason.Text = $"무인기 재계획 수신 : {deserializedObject.AircraftID}";
                                //pop_error.Show();
                                ViewModel_ScenarioView.SingletonInstance.AddLog($"무인기 재계획 수신 : {deserializedObject.AircraftID}", 4);
                            });

                        }
                        else if (id == 53113)
                        {
                            //MissionPlanOptionInfo deserializedObject = JsonConvert.DeserializeObject<MissionPlanOptionInfo>(jsonString);
                            //CommonEvent.OnMissionPlanOptionInfoReceived?.Invoke(deserializedObject);
                        }

                        else if (id == 51310)
                        {
                            InputMissionPackageJson deserializedObject = JsonConvert.DeserializeObject<InputMissionPackageJson>(jsonString);
                            ViewModel_ScenarioView.SingletonInstance.ScenarioViewClearFromMessage();
                            ViewModel_ScenarioView.SingletonInstance.ScenarioViewInit(deserializedObject);

                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                //var pop_error = new View_PopUp(10);
                                //pop_error.Description.Text = "메시지 수신";
                                //pop_error.Reason.Text = $"협업기저임무 재수행/변경";
                                //pop_error.Show();
                                ViewModel_ScenarioView.SingletonInstance.AddLog($"협업기저임무 재수행/변경", 1);
                            });
                        }

                        //else if (id == 51311)
                        //{
                        //    MissionReferencePackage deserializedObject = JsonConvert.DeserializeObject<MissionReferencePackage>(jsonString);
                        //    ViewModel_ScenarioView.SingletonInstance.ScenarioViewClearFromMessage();
                        //    ViewModel_ScenarioView.SingletonInstance.ScenarioViewInit(deserializedObject);

                        //    await Application.Current.Dispatcher.InvokeAsync(() =>
                        //    {
                        //        var pop_error = new View_PopUp(10);
                        //        pop_error.Description.Text = "메시지 수신";
                        //        pop_error.Reason.Text = $"협업기저임무 변경";
                        //        pop_error.Show();
                        //    });
                        //}
                    }
                    else
                    {
                        if (buffer.Length < 4) continue;

                        // 1. 기존 방식대로 맨 앞 4바이트를 ID로 가정하고 읽어봄
                        uint messageID_Legacy = BinaryPrimitives.ReadUInt32BigEndian(buffer);

                        // 2. 신규 방식(헤더 16바이트)인지 확인하기 위해 오프셋 12의 2바이트 ID도 읽어봄
                        ushort messageID_New = 0;
                        if (buffer.Length >= 14) // 헤더(16) 중 ID(2)는 오프셋 12에 위치
                        {
                            messageID_New = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(12, 2));
                        }

                        // =====================================================================
                        // [분기 로직] 어떤 ID를 믿을 것인가?
                        // =====================================================================

                        // CASE A: 신규 포맷의 ID가 53135인 경우 (우선 처리)
                        if (messageID_New == 53115 && ViewModel_ScenarioView.SingletonInstance.IsSimPlaying)
                        {
                            try
                            {
                                var update = ParseMissionResult(buffer); // 아까 만든 함수
                                CommonEvent.OnMissionResultDataReceived?.Invoke(update);

                                Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    //var pop_error = new View_PopUp(10);
                                    //pop_error.Description.Text = "모의 자동화 결과 수신";
                                    //pop_error.Reason.Text = $"결과: {update.RecommendText} ({update.SystemRecommend})";
                                    //pop_error.Show();
                                    ViewModel_ScenarioView.SingletonInstance.AddLog($"모의 자동화 결과 수신 : {update.RecommendText}", 3);
                                });
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[Error] MissionResult(53115) Parsing Fail: {ex.Message}");
                            }
                            continue; // 처리 했으니 다음 패킷으로
                        }

                        // CASE B: 기존 로직 (messageID_Legacy 사용)

                        // SW 상태 정보(ID: 1)가 아닌 다른 모든 메시지는 모의 시작 상태일 때만 처리
                        if (messageID_Legacy != 1 && !ViewModel_ScenarioView.SingletonInstance.IsSimPlaying)
                        {
                            continue;
                        }

                        switch (messageID_Legacy)
                        {
                            //case 1: // SW 상태 정보 (항상 처리)
                            //    try
                            //    {
                            //        ViewModel_MainView.SingletonInstance.Callback_SwStatus(result);
                            //    }
                            //    catch (Exception ex) { /* 예외 로깅 */ }
                            //    break;

                            case 4: // 센서 제어 명령 (모의 중에만 처리)
                                try
                                {
                                    SensorControlCommand command = ParseSensorControlCommand(buffer);
                                    Model_ScenarioSequenceManager.SingletonInstance.ProcessReceivedSensorCommand(command);
                                }
                                catch (Exception ex) { /* 예외 로깅 */ }
                                break;

                            case 7: // UAV 고장 상태 (모의 중에만 처리)
                                try
                                {
                                    //ViewModel_ScenarioView.SingletonInstance.Callback_UavMalFunctionState(result);
                                }
                                catch (Exception ex) { /* 예외 로깅 */ }
                                break;

                            case 9: // 기타 명령 (모의 중에만 처리)
                                Model_ScenarioSequenceManager.SingletonInstance.Callback_71303(result);
                                break;

                            case 51331: // 조종사 의사결정 (PilotDecision)
                                try
                                {
                                    var decision = ParsePilotDecision(buffer);
                                    CommonEvent.OnPilotDecisionReceived?.Invoke(decision);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error parsing PilotDecision: {ex.Message}");
                                }
                                break;

                            case 53113: // 의사결정옵션정보 (MissionPlanOptionInfo)
                                try
                                {
                                    // 위에서 만든 파싱 함수를 호출합니다.
                                    var optionInfo = Model_ScenarioSequenceManager.SingletonInstance.ParseMissionPlanOptionInfo(buffer);

                                    // 파싱된 객체를 이벤트로 다른 모듈에 전달합니다.
                                    CommonEvent.OnMissionPlanOptionInfoReceived?.Invoke(optionInfo);

                                    ViewModel_ScenarioView.SingletonInstance.AddLog($"의사결정옵션정보 수신", 3);
                                }
                                catch (Exception ex)
                                {
                                    // 예외 로깅 
                                    System.Diagnostics.Debug.WriteLine($"Error parsing MissionPlanOptionInfo: {ex.Message}");
                                }
                                break;

                            case 53114: // 조종사 의사결정 없는 임무 갱신
                                try
                                {
                                    var update = ParseMissionUpdateWithoutDecision(buffer);
                                    CommonEvent.OnMissionUpdateWithoutDecisionReceived?.Invoke(update);
                                    Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    //var pop_error = new View_PopUp(10);
                                    //pop_error.Description.Text = "메시지 수신";
                                    //pop_error.Reason.Text = $"임무 인가 - System";
                                    //pop_error.Show();
                                    ViewModel_ScenarioView.SingletonInstance.AddLog($"임무 인가 - System", 3);
                                });
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error parsing MissionUpdatewithoutPilotDecision: {ex.Message}");
                                }
                                break;

                            default:
                                // 처리하지 않는 MessageID
                                break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("소비자 루프가 정상적으로 중단되었습니다.");
            }
        }

        public async Task SendUDPMessageAsync(byte[] data, string targetIp, int targetPort)
        {
            try
            {
                // 1. UDP 메시지를 먼저 전송합니다.
                await _sender.SendAsync(data, data.Length, targetIp, targetPort);

                // 2. 전송이 성공하면, 모니터링을 위해 파이프로 보낼 패킷을 만듭니다.
                // ★★★ 핵심: _sender 소켓에서 자신의 로컬 IP와 포트 정보를 가져옵니다. ★★★
                var localEndPoint = (IPEndPoint)_sender.Client.LocalEndPoint;

                var rawPacket = new PipeDataPacketUdpRaw
                {
                    // SenderIp/Port에 로컬 엔드포인트 정보를 할당합니다.
                    SenderIp = localEndPoint.Address.ToString(),
                    SenderPort = localEndPoint.Port,
                    UdpData = data // 실제로 보낸 데이터(data 파라미터)를 담습니다.
                };


                NamedPipeSenderUDP.Instance.SendData(rawPacket);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UDP 송신 오류: {ex.Message}");
            }
        }

        // UDP만 전송, NamedPipe 전달 없음 (다중 IP 전송 시 중복 모니터링 방지)
        public async Task SendUDPOnlyAsync(byte[] data, string targetIp, int targetPort)
        {
            try
            {
                await _sender.SendAsync(data, data.Length, targetIp, targetPort);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UDP 송신 오류: {ex.Message}");
            }
        }




        public void Dispose()
        {
            // 채널을 닫고 모든 스레드를 정상적으로 종료
            _packetChannel.Writer.Complete();
            _cts.Cancel();

            _receiver?.Close();
            //_receiver_Display?.Close();
            _sender?.Close();

            _cts.Dispose();
        }



        private SensorControlCommand ParseSensorControlCommand(byte[] buffer)
        {
            const int expectedLength = 84;
            if (buffer.Length < expectedLength)
            {
                throw new ArgumentException($"수신된 버퍼의 길이가 짧습니다. 예상: {expectedLength}, 실제: {buffer.Length}");
            }

            // 원본 버퍼에서 직접 메모리를 읽는 Span을 생성합니다. (메모리 복사 없음)
            ReadOnlySpan<byte> span = buffer;

            var command = new SensorControlCommand();

            // System.Buffers.Binary.BinaryPrimitives를 사용하여 Big-Endian 값을 직접 읽습니다.
            // 이 방식은 중간에 새로운 배열을 생성하지 않아 GC 부담이 없습니다.

            int offset = 0;

            command.MessageID = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
            offset += 4;

            command.UavID = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
            offset += 4;

            command.SensorType = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
            offset += 4;

            command.HorizontalFov = BinaryPrimitives.ReadSingleBigEndian(span.Slice(offset));
            offset += 4;

            command.VerticalFov = BinaryPrimitives.ReadSingleBigEndian(span.Slice(offset));
            offset += 4;

            command.GimbalRoll = BinaryPrimitives.ReadSingleBigEndian(span.Slice(offset));
            offset += 4;

            command.GimbalPitch = BinaryPrimitives.ReadSingleBigEndian(span.Slice(offset));
            offset += 4;

            command.SensorLat = BinaryPrimitives.ReadDoubleBigEndian(span.Slice(offset));
            offset += 8;

            command.SensorLon = BinaryPrimitives.ReadDoubleBigEndian(span.Slice(offset));
            offset += 8;

            command.SensorAlt = BinaryPrimitives.ReadDoubleBigEndian(span.Slice(offset));
            offset += 8;

            command.Roll = BinaryPrimitives.ReadDoubleBigEndian(span.Slice(offset));
            offset += 8;

            command.Pitch = BinaryPrimitives.ReadDoubleBigEndian(span.Slice(offset));
            offset += 8;

            command.Heading = BinaryPrimitives.ReadDoubleBigEndian(span.Slice(offset));
            offset += 8;

            command.Speed = BinaryPrimitives.ReadDoubleBigEndian(span.Slice(offset));
            offset += 8; 

            command.Fuel = BinaryPrimitives.ReadSingleBigEndian(span.Slice(offset));
            // 마지막 필드는 offset 증가 필요 없음

            return command;
        }

        private PilotDecision ParsePilotDecision(byte[] buffer)
        {
            var decision = new PilotDecision();
            ReadOnlySpan<byte> span = buffer;
            int offset = 0;

            decision.MessageID = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
            offset += 4;

            offset += 1; // PresenceVector
            offset += 5; // Timestamp

            decision.Ignore = span[offset] != 0;
            offset += 1;

            decision.EditOptionsIDConverter = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
            offset += 4;

            return decision;
        }

        private MissionUpdatewithoutPilotDecision ParseMissionUpdateWithoutDecision(byte[] buffer)
        {
            var update = new MissionUpdatewithoutPilotDecision();
            ReadOnlySpan<byte> span = buffer;
            int offset = 0;

            update.MessageID = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
            offset += 4;

            offset += 1; // PresenceVector
            offset += 5; // Timestamp

            //update.MissionPlanID = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));

            // UAV Mission Plan ID 리스트 파싱
            update.UAVMissionPlanIDListN = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
            offset += 4;
            for (int j = 0; j < update.UAVMissionPlanIDListN; j++)
            {
                update.UAVMissionPlanIDList.Add(BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset)));
                offset += 4;
            }

            // LAH Mission Plan ID 리스트 파싱
            update.LAHMissionPlanIDListN = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset));
            offset += 4;
            for (int k = 0; k < update.LAHMissionPlanIDListN; k++)
            {
                update.LAHMissionPlanIDList.Add(BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset)));
                offset += 4;
            }

            return update;
        }

        private MissionResultData ParseMissionResult(byte[] buffer)
        {
            // [검증] 최소 패킷 길이 확인
            // Header(16) + Presence(1) + Time(5) + Recommend(4) + Checksum(2) = 28 bytes
            if (buffer.Length < 28)
            {
                throw new ArgumentException($"패킷 길이가 너무 짧습니다. (수신: {buffer.Length} bytes, 필요: 28 bytes)");
            }

            var data = new MissionResultData();
            int offset = 0;

            // 1. Split Info (2 bytes)
            data.SplitInfo = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset, 2));
            offset += 2;

            // 2. Length (2 bytes)
            data.DataLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset, 2));
            offset += 2;

            // 3. Source ID (4 bytes)
            data.SourceID = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset, 4));
            offset += 4;

            // 4. Dest ID (4 bytes)
            data.DestID = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset, 4));
            offset += 4;

            // 5. Message ID (2 bytes) - 여기서 53135 확인 가능
            //53115로 수정 53135로 송신 하기로했으나 잘 안되서 일단 53115로
            data.MessageID = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset, 2));
            offset += 2;

            // 6. Properties (2 bytes)
            data.Properties = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset, 2));
            offset += 2;

            // --- Data Field 시작 ---

            // 7. Presence Vector (1 byte)
            data.PresenceVector = buffer[offset];
            offset += 1;

            // 8. Timestamp (5 bytes) - ★ 까다로운 부분
            // 파이썬: int(time_diff...).to_bytes(5, 'big')
            // C#에선 5바이트 정수형이 없으므로 long(8byte)으로 변환
            // Big Endian이므로 앞에서부터 읽어서 쉬프트 연산
            long timestamp = 0;
            timestamp |= (long)buffer[offset + 0] << 32;
            timestamp |= (long)buffer[offset + 1] << 24;
            timestamp |= (long)buffer[offset + 2] << 16;
            timestamp |= (long)buffer[offset + 3] << 8;
            timestamp |= (long)buffer[offset + 4];
            data.Timestamp = timestamp;
            offset += 5;

            // 9. SystemRecommend (4 bytes)
            data.SystemRecommend = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset, 4));
            offset += 4;

            // 10. Checksum (2 bytes) - 필요시 검증 로직 추가
            // ushort checksum = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset, 2));

            return data;
        }
    

        // 압축 헬퍼 메서드
        public byte[] Compress(byte[] data)
        {
            using (var outputStream = new MemoryStream())
            {
                // GZipStream으로 outputStream을 감싸고 압축 모드로 설정
                using (var gZipStream = new GZipStream(outputStream, CompressionMode.Compress))
                {
                    // 원본 데이터를 GZipStream에 씁니다.
                    gZipStream.Write(data, 0, data.Length);
                }
                // 압축된 데이터가 담긴 MemoryStream을 byte[]로 반환
                return outputStream.ToArray();
            }
        }

        public async Task SendMissionPlanAsyncTest(LAHMissionPlan plan, string targetIp, int targetPort)
        {
            // 1. 객체를 JSON 문자열로 직렬화
            string jsonString = JsonConvert.SerializeObject(plan);
            byte[] originalBytes = Encoding.UTF8.GetBytes(jsonString);

            // 2. JSON 바이트를 Gzip으로 압축
            byte[] compressedBytes = Compress(originalBytes);

            Debug.WriteLine($"원본 JSON: {originalBytes.Length} bytes -> 압축: {compressedBytes.Length} bytes");

            // 3. UDPModule로 압축된 데이터 전송
            await UDPModule.SingletonInstance.SendUDPMessageAsync(compressedBytes, targetIp, targetPort);
        }

        public byte[] Decompress(byte[] data)
        {
            using (var inputStream = new MemoryStream(data))
            using (var gZipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                // GZipStream(압축 해제 모드)에서 데이터를 읽어 outputStream에 복사
                gZipStream.CopyTo(outputStream);
                return outputStream.ToArray();
            }
        }

        // UDPModule.ConsumerLoopAsync 수정 (일부)
        private async Task ConsumerLoopAsyncTest(CancellationToken token)
        {
            var reader = _packetChannel.Reader;

            try
            {
                await foreach (var result in reader.ReadAllAsync(token))
                {
                    byte[] buffer = result.Buffer;
                    if (buffer.Length < 4) continue; // 최소 길이 체크

                    // ... (NamedPipeSenderUDP 로직은 그대로 둠) ...

                    // 1. Gzip 압축 데이터인지 확인 (매직 넘버 0x1F 0x8B)
                    if (buffer.Length > 2 && buffer[0] == 0x1F && buffer[1] == 0x8B)
                    {
                        try
                        {
                            // 1-1. Gzip 압축 해제
                            byte[] decompressedBytes = Decompress(buffer);
                            string jsonString = Encoding.UTF8.GetString(decompressedBytes);
                            dynamic obj = JsonConvert.DeserializeObject(jsonString);
                            uint id = (uint)obj.MessageID;

                            // 1-2. 기존 JSON 처리 로직 (ID 53111, 53112, 51310 등)
                            if (id == 53111)
                            {
                                LAHMissionPlan deserializedObject = JsonConvert.DeserializeObject<LAHMissionPlan>(jsonString);
                                CommonEvent.OnLAHMissionPlanReceived?.Invoke(deserializedObject);
                                // ... 팝업 로직 ...
                            }
                            else if (id == 53112)
                            {
                                // ... UAVMissionPlan 처리 ...
                            }
                            else if (id == 51310)
                            {
                                // ... InputMissionPackageJson 처리 ...
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[오류] Gzip 압축 해제 또는 JSON 파싱 실패: {ex.Message}");
                        }
                    }
                    // 2. (호환용) 기존 텍스트 JSON인지 확인 (이 로직은 제거해도 됨)
                    else if (buffer[0] == '{')
                    {
                        Debug.WriteLine("[경고] 압축되지 않은 JSON 수신. (호환 모드)");
                        // ... 기존 JSON 처리 로직 ...
                    }
                    // 3. 바이너리 메시지 처리 (기존 로직)
                    else
                    {
                        if (buffer.Length < 4) continue;
                        uint messageID = BinaryPrimitives.ReadUInt32BigEndian(buffer);

                        // ... (기존 switch-case 로직: 1, 4, 7, 9, 51331 등) ...
                    }
                }
            }
            catch (OperationCanceledException) { /* ... */ }
        }


        //#region 대용량 송신

        //// 1. 분할 패킷 식별용 ID (택배 상자 ID)
        //private const uint ID_SPLIT_PACKET = 99999;
        //private const int MAX_CHUNK_SIZE = 1400; // MTU(1500) 고려한 안전 크기
        //private const int HEADER_SIZE = 16;      // 헤더 크기 (4+4+4+2+2)

        //// 2. 재조립 중인 데이터를 보관할 버퍼 클래스
        //private class ReassemblyBuffer
        //{
        //    public byte[] FullData;         // 전체 데이터를 담을 그릇
        //    public bool[] ReceivedChunks;   // 각 조각 수신 여부 체크
        //    public int ReceivedCount;       // 현재까지 받은 조각 수
        //    public int TotalChunks;         // 전체 조각 수
        //    public DateTime LastReceivedTime; // 타임아웃 처리용
        //}

        //// 3. 진행 중인 재조립 작업 목록 (Key: TransferID) - 동기화 필요
        //private Dictionary<uint, ReassemblyBuffer> _reassemblyMap = new Dictionary<uint, ReassemblyBuffer>();
        //private readonly object _reassemblyLock = new object();

        ///// <summary>
        ///// 대용량 데이터를 분할하여 전송합니다. (Wrapper ID: 99999 사용)
        ///// </summary>
        //public async Task SendLargeDataSplitAsync<T>(T dataObj, string targetIp, int targetPort)
        //{
        //    // 1. 직렬화 & 압축
        //    string jsonString = JsonConvert.SerializeObject(dataObj);
        //    byte[] compressedData = Compress(Encoding.UTF8.GetBytes(jsonString));

        //    int totalLength = compressedData.Length;
        //    int totalChunks = (totalLength + MAX_CHUNK_SIZE - 1) / MAX_CHUNK_SIZE;

        //    // 이번 전송의 고유 ID (Random)
        //    uint transferId = (uint)new Random().Next(1, int.MaxValue);
        //    //uint transferId = 99999;

        //    Debug.WriteLine($"[분할 송신 시작] TransferID:{transferId}, 전체크기:{totalLength} bytes, 조각수:{totalChunks}");

        //    // 2. 조각내서 전송
        //    for (ushort i = 0; i < totalChunks; i++)
        //    {
        //        int offset = i * MAX_CHUNK_SIZE;
        //        int currentChunkSize = Math.Min(MAX_CHUNK_SIZE, totalLength - offset);

        //        // 보낼 패킷 버퍼 생성 (헤더 + 데이터)
        //        byte[] packet = new byte[HEADER_SIZE + currentChunkSize];
        //        WriteChunkHeader(packet, transferId, (uint)totalLength, (ushort)totalChunks, i);

         

        //        // [데이터 복사]
        //        Array.Copy(compressedData, offset, packet, HEADER_SIZE, currentChunkSize);

        //        // 3. 전송 (기존 메서드 활용)
        //        await SendUDPMessageAsync(packet, targetIp, targetPort);

        //        // [중요] 패킷 유실 방지를 위한 미세 딜레이 (네트워크 부하 조절)
        //        // 10~20개마다 1ms 정도 쉬어주면 공유기 버퍼 오버플로우를 막는 데 도움됨
        //        if (i % 20 == 0) await Task.Delay(1);
        //    }

        //    Debug.WriteLine($"[분할 송신 완료] TransferID:{transferId}");
        //}

        //private void WriteChunkHeader(byte[] packet, uint transferId, uint totalLength, ushort totalChunks, ushort chunkIndex)
        //{
        //    // 일반 메서드에서는 Span 선언 가능
        //    Span<byte> span = packet;

        //    // 헤더 작성 (Big Endian)
        //    BinaryPrimitives.WriteUInt32BigEndian(span.Slice(0), ID_SPLIT_PACKET); // 99999
        //    BinaryPrimitives.WriteUInt32BigEndian(span.Slice(4), transferId);
        //    BinaryPrimitives.WriteUInt32BigEndian(span.Slice(8), totalLength);
        //    BinaryPrimitives.WriteUInt16BigEndian(span.Slice(12), totalChunks);
        //    BinaryPrimitives.WriteUInt16BigEndian(span.Slice(14), chunkIndex);
        //}

        //private void HandleChunkPacket(byte[] buffer)
        //{
        //    if (buffer.Length < HEADER_SIZE) return;

        //    ReadOnlySpan<byte> span = buffer;

        //    // 1. 헤더 파싱 (이미 ID는 확인했으니 건너뛰고)
        //    // Offset 4: TransferID
        //    uint transferId = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(4));
        //    // Offset 8: TotalSize
        //    uint totalSize = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(8));
        //    // Offset 12: TotalChunks
        //    ushort totalChunks = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(12));
        //    // Offset 14: ChunkIndex
        //    ushort chunkIndex = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(14));

        //    int payloadSize = buffer.Length - HEADER_SIZE;

        //    lock (_reassemblyLock)
        //    {
        //        // 2. 버퍼가 없으면 새로 생성 (처음 도착한 조각)
        //        if (!_reassemblyMap.ContainsKey(transferId))
        //        {
        //            _reassemblyMap[transferId] = new ReassemblyBuffer
        //            {
        //                FullData = new byte[totalSize],
        //                ReceivedChunks = new bool[totalChunks],
        //                ReceivedCount = 0,
        //                TotalChunks = totalChunks,
        //                LastReceivedTime = DateTime.Now
        //            };
        //        }

        //        var bufferObj = _reassemblyMap[transferId];
        //        bufferObj.LastReceivedTime = DateTime.Now;

        //        // 3. 중복 수신 체크 및 데이터 복사
        //        if (chunkIndex < totalChunks && !bufferObj.ReceivedChunks[chunkIndex])
        //        {
        //            int dataOffset = chunkIndex * MAX_CHUNK_SIZE;

        //            // 배열 범위 체크
        //            if (dataOffset + payloadSize <= bufferObj.FullData.Length)
        //            {
        //                Array.Copy(buffer, HEADER_SIZE, bufferObj.FullData, dataOffset, payloadSize);
        //                bufferObj.ReceivedChunks[chunkIndex] = true;
        //                bufferObj.ReceivedCount++;
        //            }
        //        }

        //        // 4. 다 모았는지 확인
        //        if (bufferObj.ReceivedCount == bufferObj.TotalChunks)
        //        {
        //            Debug.WriteLine($"[재조립 완료] TransferID:{transferId}, 크기:{bufferObj.FullData.Length}");

        //            // 다 모은 데이터 처리 함수 호출
        //            ProcessCompleteData(bufferObj.FullData);

        //            // 메모리 정리
        //            _reassemblyMap.Remove(transferId);
        //        }
        //    }
        //}

        //private void ProcessCompleteData(byte[] completeData)
        //{
        //    try
        //    {
        //        // 1. 압축 해제 (모든 대용량 패킷은 압축되었다고 가정)
        //        byte[] decompressedBytes = Decompress(completeData);
        //        string jsonString = Encoding.UTF8.GetString(decompressedBytes);

        //        // 2. JObject로 살짝 열어서 진짜 MessageID 확인
        //        var jObj = JObject.Parse(jsonString);
        //        if (jObj.TryGetValue("MessageID", out var idToken))
        //        {
        //            uint realID = idToken.Value<uint>();

        //            Debug.WriteLine($"[대용량 패킷 파싱 성공] Real MessageID: {realID}");

        //            if (realID == 53111) // LAHMissionPlan
        //            {
        //                var plan = JsonConvert.DeserializeObject<LAHMissionPlan>(jsonString);
        //                CommonEvent.OnLAHMissionPlanReceived?.Invoke(plan);

        //                // UI 팝업 호출 (필요 시)
        //                Application.Current.Dispatcher.InvokeAsync(() => {
        //                    var pop = new View_PopUp(10);
        //                    pop.Description.Text = "대용량 메시지 수신";
        //                    pop.Reason.Text = $"LAH 재계획 (ID:{plan.AircraftID})";
        //                    pop.Show();
        //                });
        //            }
        //            else if (realID == 53112) // UAVMissionPlan
        //            {
        //                var plan = JsonConvert.DeserializeObject<UAVMissionPlan>(jsonString);
        //                CommonEvent.OnUAVMissionPlanReceived?.Invoke(plan);
        //            }
        //            // 51310 등 다른 대용량 메시지도 여기서 추가 처리
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"[재조립 데이터 파싱 실패] {ex.Message}");
        //    }
        //}

        //#endregion 대용량 송신

    }


}



