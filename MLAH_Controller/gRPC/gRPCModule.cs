using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;// For logging
using System.Windows;
using DevExpress.DataProcessing;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes; // For Empty message handling
using Grpc.Core;
using MLAHInterop;
using System.Threading;
using REALTIMEVISUAL.Native.FederateInterface;
using Notification = REALTIMEVISUAL.Native.FederateInterface.Notification;



namespace MLAH_Controller
{
    public class gRPCModule
    {
        public gRPCModule()
        {
            ServiceImplementation = new FederateServiceImpl();

            var serverOptions = new List<ChannelOption>
            {
                new ChannelOption("grpc.keepalive_time_ms", 5000),              // 5초 간격으로 ping
                new ChannelOption("grpc.keepalive_timeout_ms", 1000000),             // 1000000ms 이내 응답 없으면 타임아웃
                new ChannelOption("grpc.keepalive_permit_without_calls", 1)         // 호출이 없더라도 ping 수행
            };

            server = new Server(serverOptions)
            {
                Services = { FederateService.BindService(ServiceImplementation) },
                Ports = { new ServerPort(CommonUtil.IPConfig.GrpcServerIP, 50000, ServerCredentials.Insecure) },
            };


        }


        public async Task ShutdownGrpcAsync()
        {
            if (server is { } s)
            {
                Debug.WriteLine("서버 종료 절차 시작...");
                try
                {
                    // 3초의 타임아웃을 설정합니다.
                    var shutdownTask = s.ShutdownAsync();

                    // 3초 안에 정상 종료되면 true, 아니면 false를 반환합니다.
                    if (await Task.WhenAny(shutdownTask, Task.Delay(2000)) == shutdownTask)
                    {
                        await shutdownTask; // 혹시 모를 예외를 확인하기 위해 await
                        Debug.WriteLine("gRPC 서버가 정상적으로 종료되었습니다.");
                    }
                    else
                    {
                        // 5초가 지나도 종료되지 않으면 강제 종료를 시도합니다.
                        Debug.WriteLine("정상 종료 시간 초과. 강제 종료를 시도합니다.");
                        await s.KillAsync();
                        Debug.WriteLine("gRPC 서버를 강제 종료했습니다.");
                    }
                }
                catch (Exception ex)
                {
                    // 종료 중 발생할 수 있는 예외를 로깅합니다.
                    Debug.WriteLine($"서버 종료 중 예외 발생: {ex.Message}");
                }
                finally
                {
                    server = null; // 중복 호출 방지
                }
            }
        }

        //test
        //public int obser_check = 0;
        private Server server;
        public FederateServiceImpl ServiceImplementation { get; private set; }

        private static gRPCModule _SingletonInstance = null; // 인스턴스를 보관할 private 필드
        private static readonly object padlock = new object();

        // 서버가 정상적으로 실행 중인지 상태를 저장하는 프로퍼티
        public bool IsServerRunning { get; private set; } = false;

        public static gRPCModule SingletonInstance
        {
            get
            {
                lock (padlock)
                {
                    if (_SingletonInstance == null)
                    {
                        _SingletonInstance = new gRPCModule(); // 여기에서 인스턴스 생성
                    }
                    return _SingletonInstance;
                }
            }
        }
        public void StartServer()
        {
            // 이미 실행 중이거나 서버 객체가 없으면 시작하지 않음
            if (IsServerRunning || server == null) return;

            try
            {
                server.Start();
                IsServerRunning = true; // 성공 시 상태 변경
                Debug.WriteLine($"gRPC 서버가 포트 {server.Ports.First().Port}에서 시작되었습니다.");
            }
            catch (System.IO.IOException ex)
            {
                // 포트 바인딩 실패 등 IO 예외 발생 시
                IsServerRunning = false;
                Debug.WriteLine($"gRPC 서버 시작 실패: {ex.Message}");
                // 여기서 사용자에게 알림(MessageBox 등)을 띄우는 로직을 추가할 수 있습니다.
            }
            catch (Exception ex)
            {
                // 그 외 예외
                IsServerRunning = false;
                Debug.WriteLine($"알 수 없는 오류로 gRPC 서버 시작에 실패했습니다: {ex.Message}");
            }
        }

        public int ExtractIdFromMessage(IMessage message)
        {
            var messageDescriptor = message.Descriptor;
            foreach (var field in messageDescriptor.Fields.InDeclarationOrder())
            {
                if (field.FieldType == Google.Protobuf.Reflection.FieldType.Message && field.IsRepeated)
                {
                    // 반복 필드에 대한 처리
                    var repeatedField = (IEnumerable)field.Accessor.GetValue(message);
                    foreach (IMessage subMessage in repeatedField)
                    {
                        int id = ExtractIdFromMessage(subMessage);
                        if (id != 0) return id;  // 배열 내에서 유효한 id를 찾으면 반환
                    }
                }

                else if (field.Name == "id" && field.FieldType == Google.Protobuf.Reflection.FieldType.Int32)
                {
                    var idValue = field.Accessor.GetValue(message);
                    if (int.TryParse(idValue.ToString(), out int id))
                    {
                        return id;  // 단일 id 필드를 찾았으면 반환
                    }
                }
            }
            return 0; // 'id' 필드가 없는 경우
        }


    }

    public class MessageProcessor
    {
        private static MessageProcessor _instance;
        private static readonly object _lock = new object();

        public event CommonEvent.MessageReceivedHandler OnStringMessageReceived;
        public event CommonEvent.MessageReceivedHandler OnIntegerMessageReceived;
        public event CommonEvent.MessageReceivedHandler OnBattleFieldMessageReceived;
        //public event CommonEvent.MessageReceivedHandler OnScenarioRequest; 
        public event CommonEvent.MessageReceivedHandler OnObservationHelicopterInfo;
        public event CommonEvent.MessageReceivedHandler OnObservationUAVInfo;
        public event CommonEvent.MessageReceivedHandler OnObservationRequest;
        public event CommonEvent.MessageReceivedHandler OnDisconnectRequest;
        public event CommonEvent.MessageReceivedHandler OnInitEpisodeResponse;
        public event CommonEvent.MessageReceivedHandler OnSWBit;
        public event CommonEvent.AsyncMessageReceivedHandler OnScenarioRequestAsync;
        public event CommonEvent.AsyncMessageReceivedHandler OnScenarioMissionRequestAsync;


        // 싱글톤 인스턴스 접근자
        public static MessageProcessor Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new MessageProcessor();
                    }
                    return _instance;
                }
            }
        }

        // 생성자를 private로 설정하여 외부에서 인스턴스 생성을 막음
        private MessageProcessor() { }

        public void ProcessMessage(string typeName, IMessage messageInstance, ContextInfo contextInfo)
        {
            switch (typeName)
            {
                //case "MLAHInterop.BattleFieldSitSimSW_TO_Controller":
                //    OnBattleFieldMessageReceived?.Invoke(messageInstance, contextInfo);
                //    break;
                case "MLAHInterop.ScenarioRequest":
                    OnScenarioRequestAsync?.Invoke(messageInstance, contextInfo);
                    break;
                case "MLAHInterop.ScenarioMissionRequest":
                    OnScenarioMissionRequestAsync?.Invoke(messageInstance, contextInfo);
                    break;
                //case "MLAHInterop.ObservationRequest":
                //    {
                //        ViewModel_ScenarioView.SingletonInstance.Callback_Observation(messageInstance);
                //    }
                //    break;
                case "MLAHInterop.DisconnectRequest":
                    OnDisconnectRequest?.Invoke(messageInstance, contextInfo);
                    break;
                //case "MLAHInterop.ObservationHelicopterInfo":
                //    OnObservationHelicopterInfo?.Invoke(messageInstance, contextInfo);
                //    break;
                //case "MLAHInterop.ObservationUAVInfo":
                //    OnObservationUAVInfo?.Invoke(messageInstance, contextInfo);
                //    break;
                case "MLAHInterop.InitEpisodeResponse":
                    OnInitEpisodeResponse?.Invoke(messageInstance, contextInfo);
                    break;
                case "MLAHInterop.SWBit":
                    OnSWBit?.Invoke(messageInstance, contextInfo);
                    break;
                case "MLAHInterop.EntityWapointDoneResponse": // 네임스페이스 확인 필요
                    var doneMsg = messageInstance as EntityWapointDoneResponse;
                    if (doneMsg != null)
                    {
                        // 매니저에게 완료 알림 전달
                        Model_ScenarioSequenceManager.SingletonInstance.ProcessWaypointDone(doneMsg.Id, doneMsg.Missionid);
                    }
                    break;
                default:
                    //System.Diagnostics.Debug.WriteLine($"gRPC Module : No handler for this type - {typeName}");
                    break;
            }
        }
    }

    /// <summary>
    /// 클라이언트 세션 정보를 통합 관리하는 클래스입니다.
    /// 세션과 관련된 모든 리소스를 한 곳에서 관리하여 안정성을 높입니다.
    /// </summary>
    public class ClientSession
    {
        public string Peer { get; }
        public IServerStreamWriter<Notification> ResponseStream { get; }
        public CancellationTokenSource Cts { get; }

        public SemaphoreSlim WriteSemaphore { get; } = new SemaphoreSlim(1, 1);

        public ClientSession(string peer, IServerStreamWriter<Notification> responseStream, CancellationTokenSource cts)
        {
            Peer = peer;
            ResponseStream = responseStream;
            Cts = cts;
        }

        public void CancelAndDispose()
        {
            if (!Cts.IsCancellationRequested)
            {
                Cts.Cancel();
            }
            Cts.Dispose();
        }


    }


    public class FederateServiceImpl : FederateService.FederateServiceBase
    {
        // Peer(IP:Port)를 키로 사용하여 클라이언트 세션을 관리하는 단일 ConcurrentDictionary
        private readonly ConcurrentDictionary<string, ClientSession> _sessions = new ConcurrentDictionary<string, ClientSession>();
        //private readonly TypeRegistry _registry = TypeRegistry.FromMessages(MLAHInterop.ScenarioResponse.Descriptor);

        private readonly ConcurrentDictionary<string, string> _peerToFederateIdMap = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 현재 활성화된 클라이언트 세션의 개수를 반환합니다.
        /// </summary>
        public int ActiveSessionCount => _sessions.Count;

        private readonly TypeRegistry registry = TypeRegistry.FromMessages(MLAHInterop.ScenarioResponse.Descriptor);

        public FederateServiceImpl()
        {

        }

        /// <summary>
        /// 지정된 Peer의 세션을 안전하고 완벽하게 정리하는 중앙 집중식 메서드입니다.
        /// </summary>
        private async Task CleanupSessionAsync(string federateId)
        {
            if (_sessions.TryRemove(federateId, out var sessionToCleanup))
            {
                Debug.WriteLine($"[Cleanup] 세션 정리 시작. FederateId: {federateId}, Peer: {sessionToCleanup.Peer}");
                _peerToFederateIdMap.TryRemove(sessionToCleanup.Peer, out _);
                //sessionToCleanup.CancelAndDispose();

                // 클라이언트에게 종료 신호 전송 시도
                //try
                //{
                //    await sessionToCleanup.ResponseStream.WriteAsync(new Notification
                //    {
                //        NotifType = enumNotifType.NotifMessageReceived,
                //        MessageReceived = new MessageReceived { Name = "ForceDisconnect" }
                //    });
                //}
                //catch { /* 전송 실패는 이미 연결이 끊긴 상태이므로 무시 */ }

                sessionToCleanup.CancelAndDispose();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    //var pop_error = new View_PopUp(10, 400);
                    //pop_error.Description.Text = "클라이언트 연결 종료";
                    //pop_error.Reason.Text = $"클라이언트({sessionToCleanup.Peer})와의 연결이 종료되었습니다.";
                    //pop_error.Show();
                    ViewModel_ScenarioView.SingletonInstance.AddLog($"클라이언트({sessionToCleanup.Peer})와의 연결이 종료되었습니다.",3);
                });
                CommonEvent.OnClientSessionCleanedUp?.Invoke(sessionToCleanup.Peer);

                Debug.WriteLine($"[Cleanup] 세션 정리 완료. FederateId: {federateId}");
            }
        }


        // 클라이언트가 연결될 때 스트림 저장
        public override async Task JoinFederationExecution(JoinFederationExecutionReq request, IServerStreamWriter<Notification> responseStream, ServerCallContext context)
        {
            string peer = context.Peer;
            string federateId = request.FederateId;
            Debug.WriteLine($"[Join] 새로운 연결 시도 from: {peer}, FederateId: {federateId}");

            if (string.IsNullOrEmpty(federateId))
            {
                // FederateId가 없는 비정상적인 요청은 거부
                Debug.WriteLine($"[Join] 거부: FederateId가 비어있습니다. Peer: {peer}");
                return;
            }

            // --- 1. 기존 세션 정리 (개선된 방식) ---
            // 동일한 클라이언트가 비정상 종료 후 재접속했을 경우를 대비해, 기존에 남아있을 수 있는 세션을 먼저 정리합니다.
            if (_sessions.ContainsKey(federateId))
            {
                Debug.WriteLine($"[Join] 이전 좀비 세션 정리. FederateId: {federateId}");
                await CleanupSessionAsync(federateId);
            }

            // --- 2. 접속 정보 파싱 및 모니터링 (사용자 로직 유지 및 통합) ---
            // 기존 코드와 동일하게 접속자 정보를 파싱하고 모니터링 UI에 표시합니다.
            string clientIp = "";
            string clientPort = "";
            if (!string.IsNullOrEmpty(peer))
            {
                // peer 문자열(예: "ipv4:127.0.0.1:50000")에서 IP와 Port를 파싱합니다.
                int firstColonIndex = peer.IndexOf(':');
                if (firstColonIndex >= 0 && firstColonIndex < peer.Length - 1)
                {
                    string remainder = peer.Substring(firstColonIndex + 1);
                    if (remainder.StartsWith("[")) // IPv6 처리
                    {
                        int closingBracket = remainder.IndexOf(']');
                        if (closingBracket > 0)
                        {
                            clientIp = remainder.Substring(1, closingBracket - 1);
                            int colonAfterBracket = remainder.IndexOf(':', closingBracket);
                            if (colonAfterBracket > 0 && colonAfterBracket < remainder.Length - 1)
                            {
                                clientPort = remainder.Substring(colonAfterBracket + 1);
                            }
                        }
                    }
                    else // IPv4 처리
                    {
                        var parts = remainder.Split(':');
                        if (parts.Length >= 2)
                        {
                            clientIp = parts[0];
                            clientPort = parts[1];
                        }
                        else if (parts.Length == 1)
                        {
                            clientIp = parts[0];
                        }
                    }
                }
            }

            //var contextinfo = new ContextInfo
            //{
            //    IP = clientIp,
            //    Port = clientPort,
            //    Protocol = "gRPC",
            //    ReceivedTime = DateTime.Now.ToLongTimeString(),
            //    MessageName = "JoinFederationExecutionReq"
            //};

            var contextinfo = new ContextInfo
            {
                IP = clientIp,
                Port = clientPort,
                Protocol = "gRPC",
                ReceivedTime = DateTime.Now.ToLongTimeString(),
                MessageName = "JoinFederationExecutionReq"
                // OriginalMessage와 FieldNodes는 나중에 ViewModel/Model에서 채우므로 여기서 설정 X
            };



            var packet = new PipeDataPacket
            {
                MessageTypeName = JoinFederationExecutionReq.Descriptor.FullName,
                Context = contextinfo,
                ProtoData = request.ToByteArray()
            };
            NamedPipeSender.Instance.SendData(packet);

            //_ = NamedPipeSender.Instance.SendDataAsync(request, contextinfo);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                //var pop_error = new View_PopUp(10, 400);
                //pop_error.Description.Text = "클라이언트 연결";
                //pop_error.Reason.Text = $"클라이언트({clientIp}:{clientPort})가 접속하였습니다.";
                //pop_error.Show();
                // 메시지 수신 시 로그 남기기
                ViewModel_ScenarioView.SingletonInstance.AddLog($"클라이언트({clientIp}:{clientPort})가 접속하였습니다.",3);
            });

            // --- 3. 첫 번째 초기 응답 전송 (사용자 로직 유지) ---
            // 클라이언트에게 Join 요청이 성공적으로 수신되었다는 1차 응답을 보냅니다.
            var notification = new Notification
            {
                NotifType = enumNotifType.NotifFeRsp,
                FeRsp = new FERsp { Result = 0 }
            };
            await responseStream.WriteAsync(notification);

            // --- 4. 새 세션 생성 및 등록 (개선된 방식) ---
            // 안정적인 세션 관리를 위해 ClientSession 객체를 생성하고 단일 딕셔너리에 등록합니다.
            var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
            var newSession = new ClientSession(peer, responseStream, cts);

            if (_sessions.TryAdd(federateId, newSession))
            {
                _peerToFederateIdMap[peer] = federateId;
                Debug.WriteLine($"[Join] 세션 등록 성공. FederateId: {federateId}");
                await responseStream.WriteAsync(new Notification { NotifType = enumNotifType.NotifFeRsp, FeRsp = new FERsp { Result = 0 } });

                // --- 5. 두 번째 초기 응답 전송 (사용자 로직 유지 및 개선) ---
                // 세션이 완전히 준비된 후, InitEpisodeResponse 메시지를 '해당 클라이언트에게만' 보냅니다.
                var initResponse = new InitEpisodeResponse { Message = "Response" };
                var anyMessage = Google.Protobuf.WellKnownTypes.Any.Pack(initResponse);
                var messageNotification = new Notification
                {
                    NotifType = enumNotifType.NotifMessageReceived,
                    MessageReceived = new MessageReceived
                    {
                        Name = "InitEpisodeResponse",
                        Parameter = anyMessage,
                    }
                };
                // SendServerMessage는 모든 클라이언트에게 브로드캐스트하므로, 여기서는 해당 세션의 스트림에 직접 쓰는 것이 올바릅니다.
                await newSession.ResponseStream.WriteAsync(messageNotification);
            }
            else
            {
                newSession.CancelAndDispose();
                return;
            }


            // SendServerMessage를 호출하여 모니터링 UI에도 기록되도록 합니다. (선택적)
            // 이 경우, SendServerMessage는 모든 클라이언트에게 메시지를 보내므로 주의가 필요합니다.
            // await SendServerMessage(messageNotification);

            // --- 6. 세션 유지 및 종료 처리 (개선된 방식) ---
            try
            {
                // 연결이 유지되는 동안 무한정 대기합니다.
                await Task.Delay(Timeout.Infinite, newSession.Cts.Token);
            }
            catch (TaskCanceledException)
            {
                // CancellationToken에 의해 정상적으로 취소된 경우 (의도된 동작)
                Debug.WriteLine($"[Join] 스트림이 취소되었습니다. FederateId: {federateId}");
            }
            catch (Exception ex)
            {
                // 예기치 못한 다른 예외 처리
                Debug.WriteLine($"[Join] 스트림 대기 중 예외 발생. FederateId: {federateId}, Error: {ex.Message}");
            }
            finally
            {
                // ★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★
                // 연결이 정상적으로 종료되든, 강제로 끊기든, Keepalive 타임아웃이 되든
                // 이 블록은 '반드시' 실행
                Debug.WriteLine($"[Join] 스트림 종료. 세션 정리 시작. FederateId: {federateId}");
                await CleanupSessionAsync(federateId);
            }
        }


        /// <summary>
        /// 클라이언트의 정상적인 연동 종료 요청을 처리합니다.
        /// </summary>
        public override async Task<FERsp> ResignFederationExecution(ResignFederationExecutionReq request, ServerCallContext context)
        {
            if (_peerToFederateIdMap.TryGetValue(context.Peer, out var federateId))
            {
                Debug.WriteLine($"[Resign] 정상 종료 요청 수신. FederateId: {federateId}");
                //await CleanupSessionAsync(federateId);
            }
            else
            {
                Debug.WriteLine($"[Resign] 경고: 알 수 없는 클라이언트({context.Peer})의 종료 요청.");
            }
            return new FERsp { Result = 0 };
        }

        public override async Task<FERsp> RegisterObjectInstance(RegisterObjectInstanceReq request, ServerCallContext context)
        {
            string clientId = context.Peer;  // 또는 request에서 클라이언트 ID 추출
                                             // 클라이언트 스트림 제거

            return new FERsp { Result = 0 };
        }

        public override async Task<FERsp> DeleteObjectInstance(DeleteObjectInstanceReq request, ServerCallContext context)
        {
            //string clientId = context.Peer;  // 또는 request에서 클라이언트 ID 추출
            // 클라이언트 스트림 제거

            return new FERsp { Result = 0 };
        }

        // 서버에서 클라이언트로 메시지 보내기
        /// <summary>
        /// 서버에서 모든 활성 클라이언트에게 메시지를 브로드캐스트합니다.
        /// </summary>
        public async Task SendServerMessage(Notification message)
        {
            // 1. [모니터링] 보내려는 메시지를 먼저 파싱하여 UI에 표시합니다.
            if (message.NotifType == enumNotifType.NotifMessageReceived)
            {
                try
                {
                    string typeName = message.MessageReceived.Parameter.TypeUrl.Replace("type.googleapis.com/", "");
                    var messageType = registry.Find(typeName);
                    if (messageType != null)
                    {
                        IMessage messageInstance = messageType.Parser.ParseFrom(message.MessageReceived.Parameter.Value);
                        var contextinfo = new ContextInfo
                        {
                            IP = "192.168.20.200",
                            Port = "50000",
                            Protocol = "gRPC(Send)",
                            ReceivedTime = DateTime.Now.ToLongTimeString(),
                            MessageName = typeName.Replace("MLAHInterop.", ""),
                            //fieldValues = DisplayMessageDetails(messageInstance),
                            ID = gRPCModule.SingletonInstance.ExtractIdFromMessage(messageInstance)
                        };

                        var packet = new PipeDataPacket
                        {
                            MessageTypeName = messageInstance.Descriptor.FullName,
                            Context = contextinfo,
                            ProtoData = messageInstance.ToByteArray()
                        };
                        NamedPipeSender.Instance.SendData(packet);
                        //_ = NamedPipeSender.Instance.SendDataAsync(messageInstance, contextinfo);

                    }
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine($"[SendServerMessage] 송신 메시지 파싱 중 예외(모니터링용): {ex.Message}");
                }
            }

            // 2. 현재 세션 목록을 한 번만 가져옴
            var currentSessions = _sessions.Values.ToList();
            if (currentSessions.Count == 0) return;

            // 3. 각 세션에 대한 전송 작업을 담을 리스트 생성
            var sendTasks = new List<Task>();
            var disconnectedFederateIds = new ConcurrentBag<string>(); // 여러 스레드에서 접근하므로 ConcurrentBag 사용

            foreach (var session in currentSessions)
            {
                // 각 클라이언트에 대한 전송 작업을 비동기 람다로 생성하여 리스트에 추가
                sendTasks.Add(Task.Run(async () =>
                {
                    // FederateId는 클로저를 통해 캡처
                    var federateId = _peerToFederateIdMap.FirstOrDefault(x => x.Value == session.Peer).Key;

                    await session.WriteSemaphore.WaitAsync();
                    try
                    {
                        if (session.Cts.IsCancellationRequested)
                        {
                            disconnectedFederateIds.Add(federateId);
                            return;
                        }
                        await session.ResponseStream.WriteAsync(message);
                    }
                    catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable || ex.StatusCode == StatusCode.Cancelled)
                    {
                        Debug.WriteLine($"[SendServerMessage] 클라이언트({session.Peer}) 연결 종료 감지. 정리 목록에 추가. FederateId: {federateId}");
                        disconnectedFederateIds.Add(federateId);
                    }
                    catch (Exception ex)
                    {
                        if (ex is InvalidOperationException && ex.Message.Contains("completed"))
                        {
                            //Debug.WriteLine($"[SendServerMessage] 이미 닫힌 스트림에 쓰기 시도 감지. 정리 목록에 추가. FederateId: {federateId}");
                            disconnectedFederateIds.Add(federateId);
                        }
                        else
                        {
                            //Debug.WriteLine($"[SendServerMessage] CRITICAL: 메시지 전송 중 예외. Peer: {session.Peer}, Error: {ex}");
                        }
                    }
                    finally
                    {
                        session.WriteSemaphore.Release();
                    }
                }));
            }

            // 4. 모든 전송 작업이 완료될 때까지 기다림 (병렬 실행)
            await Task.WhenAll(sendTasks);

            // 5. 전송에 실패한 클라이언트 세션을 정리
            if (!disconnectedFederateIds.IsEmpty)
            {
                foreach (var federateId in disconnectedFederateIds)
                {
                    await CleanupSessionAsync(federateId);
                }
            }
        }

        public async Task ForwardServerMessage(Notification message, string senderPeer)
        {
            // 1. 현재 세션 목록에서 메시지를 보낸 클라이언트를 '제외'한 목록을 만듭니다.
            var targetSessions = _sessions.Values.Where(s => s.Peer != senderPeer).ToList();
            if (targetSessions.Count == 0) return;

            // 2. 각 대상 세션에 대한 전송 작업을 담을 리스트 생성
            var sendTasks = new List<Task>();
            var disconnectedFederateIds = new ConcurrentBag<string>();

            foreach (var session in targetSessions)
            {
                // 3. 병렬로 메시지를 전송합니다. (SendServerMessage와 동일한 로직)
                sendTasks.Add(Task.Run(async () =>
                {
                    var federateId = _peerToFederateIdMap.FirstOrDefault(x => x.Value == session.Peer).Key;

                    await session.WriteSemaphore.WaitAsync();
                    try
                    {
                        if (session.Cts.IsCancellationRequested)
                        {
                            disconnectedFederateIds.Add(federateId);
                            return;
                        }
                        await session.ResponseStream.WriteAsync(message);
                    }
                    catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable || ex.StatusCode == StatusCode.Cancelled)
                    {
                        disconnectedFederateIds.Add(federateId);
                    }
                    catch (Exception ex)
                    {
                        if (ex is InvalidOperationException && ex.Message.Contains("completed"))
                        {
                            disconnectedFederateIds.Add(federateId);
                        }
                        else
                        {
                            //Debug.WriteLine($"[ForwardServerMessage] CRITICAL: 메시지 전송 중 예외. Peer: {session.Peer}, Error: {ex}");
                        }
                    }
                    finally
                    {
                        session.WriteSemaphore.Release();
                    }
                }));
            }

            // 4. 모든 전송 작업이 완료될 때까지 기다립니다.
            await Task.WhenAll(sendTasks);

            // 5. 전송에 실패한 클라이언트 세션을 정리합니다.
            if (!disconnectedFederateIds.IsEmpty)
            {
                foreach (var federateId in disconnectedFederateIds)
                {
                    await CleanupSessionAsync(federateId);
                }
            }
        }

        /// <summary>
        /// SendMessage의 실제 비동기 처리 로직입니다.
        /// </summary>
        private async Task SendMessageInternalAsync(SendMessageReq request, ServerCallContext context)
        {
            string peer = context.Peer;

            // 유효하지 않은 세션일 경우, 예외를 던지는 대신 로그만 남기고 즉시 반환합니다.
            if (!_peerToFederateIdMap.TryGetValue(peer, out var federateId) || !_sessions.ContainsKey(federateId))
            {
                // Debug.WriteLine($"[SendMessage] 경고: 유효하지 않은 세션({peer})의 메시지를 무시합니다.");
                return; // ★★★ 핵심: 여기서 조용히 종료
            }

            string typeName = request.Parameter.TypeUrl.Replace("type.googleapis.com/", "");
            if (typeName.EndsWith("DisconnectRequest"))
            {
                Debug.WriteLine($"[SendMessage] 클라이언트({federateId})가 종료 의사를 밝혔습니다.");
                //_ = CleanupSessionAsync(federateId);
                return;
            }

            // 3. 일반 메시지 처리 및 모니터링
            try
            {
                var messageType = registry.Find(typeName);
                if (messageType != null)
                {
                    IMessage messageInstance = messageType.Parser.ParseFrom(request.Parameter.Value);
                    var contextinfo = new ContextInfo();

                    //string peer = context.Peer;
                    string clientIp = "N/A";
                    string clientPort = "N/A";

                    if (!string.IsNullOrEmpty(peer) && peer.Contains(":"))
                    {
                        // "JoinFederationExecution"에서 사용했던 파싱 로직과 동일하게 처리
                        string addressPart = peer.Substring(peer.IndexOf(':') + 1);
                        int lastColon = addressPart.LastIndexOf(':');
                        if (lastColon > 0)
                        {
                            clientIp = addressPart.Substring(0, lastColon);
                            // IPv6 주소의 대괄호 제거
                            if (clientIp.StartsWith("[") && clientIp.EndsWith("]"))
                            {
                                clientIp = clientIp.Substring(1, clientIp.Length - 2);
                            }
                            clientPort = addressPart.Substring(lastColon + 1);
                        }
                    }

                    contextinfo.IP = clientIp;
                    contextinfo.Port = clientPort;


                    contextinfo.Protocol = "gRPC";
                    contextinfo.ReceivedTime = DateTime.Now.ToLongTimeString();

                    string typeNametoSave = request.Parameter.TypeUrl.Replace("type.googleapis.com/MLAHInterop.", "");
                    //contextinfo.fieldValues = DisplayMessageDetails(messageInstance);

                    //StackOverFlow 테스트
                    //부하
                    //contextinfo.FieldNodes = Model_Mornitoring_PopUp.SingletonInstance.ParseMessageToNodes(messageInstance);

                    contextinfo.MessageName = typeNametoSave;
                    contextinfo.ID = gRPCModule.SingletonInstance.ExtractIdFromMessage(messageInstance);

                    // UI 스레드에서 안전하게 모니터링 데이터 소스에 추가합니다.
                    //await Application.Current.Dispatcher.InvokeAsync(() =>
                    //{
                    //    ViewModel_Mornitoring_PopUp.SingletonInstance.MornitoringDataSource.Add(contextinfo);
                    //});


                    // [모니터링 로직 끝]

                    //ObservationRequest 특별 처리
                    if (typeName == "MLAHInterop.ObservationRequest")
                    {
                        // 1. 고빈도 메시지는 중앙 처리 큐로 보냅니다.
                        //Model_ScenarioSequenceManager.SingletonInstance.EnqueueMessage(messageInstance);
                        await Model_ScenarioSequenceManager.SingletonInstance.ProcessObservationRequest(messageInstance as ObservationRequest);
                        //Model_Mornitoring_PopUp.SingletonInstance.EnqueueMessage(new Tuple<IMessage, ContextInfo>(messageInstance, contextinfo));
                    }
                    else if (typeName == "MLAHInterop.UpdateEntity")
                    {
                        // UpdateEntity 메시지를 받으면, Forwarding 로직을 호출합니다.
                        var notification = new Notification
                        {
                            NotifType = enumNotifType.NotifMessageReceived,
                            MessageReceived = new MessageReceived
                            {
                                Name = "UpdateEntity", // proto에 정의된 메시지 이름
                                Parameter = Any.Pack(messageInstance)
                            }
                        };
                        // Fire-and-Forget 방식으로 Forwarding 실행
                        _ = ForwardServerMessage(notification, peer);
                    }
                    else
                    {
                        // 2. 그 외 모든 메시지는 기존의 유연한 MessageProcessor로 보냅니다.
                        MessageProcessor.Instance.ProcessMessage(typeName, messageInstance, contextinfo);
                        //Model_Mornitoring_PopUp.SingletonInstance.EnqueueMessage(new Tuple<IMessage, ContextInfo>(messageInstance, contextinfo));
                    }
                    //StackOverFlow 테스트

                    //부하
                    //Model_Mornitoring_PopUp.SingletonInstance.EnqueueMessage(new Tuple<IMessage, ContextInfo>(messageInstance, contextinfo));
                    //
                    //
                    //NamedPipeSender.Instance.SendData(messageInstance, contextinfo);

                    var packet = new PipeDataPacket
                    {
                        MessageTypeName = messageInstance.Descriptor.FullName,
                        Context = contextinfo,
                        ProtoData = messageInstance.ToByteArray()
                    };
                    NamedPipeSender.Instance.SendData(packet);
                    //_ = NamedPipeSender.Instance.SendDataAsync(messageInstance, contextinfo);
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"[SendMessage] 메시지 처리 중 예외 발생: {ex.Message}");
            }
        }




        /// <summary>
        /// 클라이언트가 보낸 메시지를 수신하는 단일 진입점입니다.
        /// 즉시 클라이언트에 응답하고, 실제 처리는 백그라운드에서 수행합니다.
        /// </summary>
        public override Task<Empty> SendMessage(SendMessageReq request, ServerCallContext context)
        {
            // 실제 로직은 아래의 InternalAsync 메서드에 위임하고, 즉시 클라이언트에 응답합니다.
            _ = SendMessageInternalAsync(request, context);
            return Task.FromResult(new Empty());
        }
    }
}
