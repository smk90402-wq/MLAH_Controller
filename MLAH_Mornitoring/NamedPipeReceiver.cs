using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Pipes;
// [확인 후 삭제] 미사용 using
//using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
// [확인 후 삭제] 미사용 using
//using DevExpress.PivotGrid.SliceQueryDataSource;
using Google.Protobuf;
using MLAHInterop;
using REALTIMEVISUAL.Native.FederateInterface;
using Application = System.Windows.Application; // .proto 파일에서 생성된 네임스페이스




namespace MLAH_Mornitoring // UI 앱의 네임스페이스
{
    // gRPC 앱에서 보낸 데이터 패킷을 받을 클래스 (보내는 쪽과 동일해야 함)
    public class PipeDataPacket
    {
        public string MessageTypeName { get; set; }
        public ContextInfo Context { get; set; } // ContextInfo 클래스도 UI 프로젝트에 있어야 합니다.
        public byte[] ProtoData { get; set; }
    }

    public class NamedPipeReceiver
    {
        private const string PipeName = "MLAHMonitoringPipe"; // 파이프 이름 (gRPC 앱과 동일해야 함)

        // 백그라운드 스레드에서 파이프 서버를 계속 실행
        public void Start()
        {
            Task.Run(() => ListenLoop());
        }

        private async Task ListenLoop()
        {
            // 이 루프는 서버 스트림 자체에 심각한 오류가 발생했을 때
            // 서버를 재생성하기 위해 존재합니다.
            while (true)
            {
                // 1. 클라이언트와의 한 번의 완전한 세션을 위해 파이프 서버를 생성합니다.
                using (var pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                {
                    try
                    {
                        // 2. 클라이언트의 연결을 기다립니다.
                        await pipeServer.WaitForConnectionAsync();
                        //System.Diagnostics.Debug.WriteLine("Pipe Client Connected. Start listening for messages...");

                        // 3. [핵심 수정] 연결이 유지되는 동안 계속해서 메시지를 읽습니다.
                        while (pipeServer.IsConnected)
                        {
                            // ProcessPipeData는 한 메시지를 읽고 처리합니다.
                            // 클라이언트 연결이 끊기면 이 메서드에서 IOException이 발생합니다.
                            await ProcessPipeData(pipeServer);
                        }
                    }
                    catch (IOException)
                    {
                        // 클라이언트가 정상적으로 또는 비정상적으로 연결을 끊었을 때 발생하는 예상된 예외입니다.
                        // using 블록이 끝나면서 pipeServer 리소스가 정리되고, 외부 while 루프가 새 연결을 위해 새 서버를 생성합니다.
                        System.Diagnostics.Debug.WriteLine("Pipe Client Disconnected. Waiting for new connection...");
                    }
                    catch (Exception )
                    {
                        // 예상치 못한 다른 오류 처리
                        //System.Diagnostics.Debug.WriteLine($"Pipe Listen Error: {ex.Message}");
                        await Task.Delay(1000); // 오류가 너무 빠르게 반복되는 것을 방지
                    }
                } // 여기서 pipeServer.Dispose()가 호출되어 세션이 완전히 정리됩니다.
            }
        }


        private async Task ProcessPipeData(NamedPipeServerStream pipeServer)
        {
            // 1. 데이터 길이(4바이트)를 먼저 읽음
            byte[] lengthBuffer = new byte[4];

            // [수정] ReadAsync의 반환값(읽은 바이트 수)을 확인하여 정상적인 연결 종료를 감지합니다.
            int bytesRead = await pipeServer.ReadAsync(lengthBuffer, 0, 4);
            if (bytesRead < 4)
            {
                // 0 바이트를 읽었거나 4바이트보다 적게 읽었다면 클라이언트가 연결을 닫은 것입니다.
                throw new IOException("Client disconnected while reading data length.");
            }

            int dataLength = BitConverter.ToInt32(lengthBuffer, 0);
            if (dataLength <= 0)
            {
                throw new InvalidDataException("Received invalid data length.");
            }

            // 2. 실제 데이터 읽기
            byte[] dataBuffer = new byte[dataLength];
            int totalBytesRead = 0;
            while (totalBytesRead < dataLength)
            {
                bytesRead = await pipeServer.ReadAsync(dataBuffer, totalBytesRead, dataLength - totalBytesRead);
                if (bytesRead == 0)
                {
                    // 데이터를 읽는 도중 연결이 끊겼습니다.
                    throw new IOException("Client disconnected while reading data content.");
                }
                totalBytesRead += bytesRead;
            }

            string jsonString = Encoding.UTF8.GetString(dataBuffer);

            // 3. JSON 문자열을 데이터 패킷 객체로 역직렬화 (이하 로직은 기존과 동일)
            var packet = JsonSerializer.Deserialize<PipeDataPacket>(jsonString);
            if (packet == null) return;

            // ... (기존 역직렬화 및 UI 업데이트 로직) ...
            IMessage message = DeserializeProto(packet.MessageTypeName, packet.ProtoData);
            if (message == null) return;

            var parsedNodes = ParseMessageToNodes(message);
            packet.Context.FieldNodes = parsedNodes;

            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                Model_Mornitoring_PopUp.SingletonInstance.EnqueueMessage(new Tuple<IMessage, ContextInfo>(message, packet.Context));
                //System.Diagnostics.Debug.WriteLine($"Pipe Received and Parsed: {packet.MessageTypeName}");
            });
        }

        // 메시지 타입 이름으로 적절한 Protobuf 파서를 찾아 역직렬화하는 메서드
        private IMessage DeserializeProto(string typeName, byte[] data)
        {
            // .proto에 정의된 모든 메시지 타입을 여기에 등록해야 합니다.
            // 더 나은 방법: 리플렉션을 사용해 동적으로 파서를 찾는 방법도 있습니다.
            switch (typeName)
            {
                case "MLAHInterop.ScenarioRequest":
                    return ScenarioRequest.Parser.ParseFrom(data);
                case "MLAHInterop.ScenarioResponse":
                    return ScenarioResponse.Parser.ParseFrom(data);
                case "MLAHInterop.ScenarioMissionRequest":
                    return ScenarioMissionRequest.Parser.ParseFrom(data);
                case "MLAHInterop.InitEpisodeResponse":
                    return InitEpisodeResponse.Parser.ParseFrom(data);
                case "MLAHInterop.SWBit":
                    return SWBit.Parser.ParseFrom(data);
                case "MLAHInterop.DisconnectRequest":
                    return DisconnectRequest.Parser.ParseFrom(data);
                case "MLAHInterop.ObservationRequest":
                    return ObservationRequest.Parser.ParseFrom(data);
                case "REALTIMEVISUAL.Native.FederateInterface.JoinFederationExecutionReq":
                    return JoinFederationExecutionReq.Parser.ParseFrom(data);
                case "MLAHInterop.UpdateEntity":
                    return UpdateEntity.Parser.ParseFrom(data);
                case "MLAHInterop.ScenarioMissionResponse":
                    return ScenarioMissionResponse.Parser.ParseFrom(data);
                case "MLAHInterop.EntityWapointDoneResponse":
                    return EntityWapointDoneResponse.Parser.ParseFrom(data);
                case "MLAHInterop.ChangeEnvironmentResponse":
                    return ChangeEnvironmentResponse.Parser.ParseFrom(data);
                case "MLAHInterop.ChangeSensorResponse":
                    return ChangeSensorResponse.Parser.ParseFrom(data);
                case "MLAHInterop.ChangeFuelResponse":
                    return ChangeFuelResponse.Parser.ParseFrom(data);
                case "MLAHInterop.ChangeHealthResponse":
                    return ChangeHealthResponse.Parser.ParseFrom(data);
                case "MLAHInterop.ChangeDataLinkResponse":
                    return ChangeDataLinkResponse.Parser.ParseFrom(data);
                case "MLAHInterop.ChangeFuelStatusResponse":
                    return ChangeFuelStatusResponse.Parser.ParseFrom(data);
                case "MLAHInterop.ChangeHealthStatusResponse":
                    return ChangeHealthStatusResponse.Parser.ParseFrom(data);

                case "REALTIMEVISUAL.Native.FederateInterface.ResignFederationExecutionReq":
                    return ResignFederationExecutionReq.Parser.ParseFrom(data);
                // ... (gRPC로 주고받는 모든 메시지 타입 추가) ...
                default:
                    return null;
            }
        }

        public ObservableCollection<MessageNode> ParseMessageToNodes(IMessage message)
        {
            if (message == null)
            {
                return new ObservableCollection<MessageNode>();
            }

            var rootNodes = new ObservableCollection<MessageNode>();
            // 스택 아이템: (처리할 메시지, 부모 노드 컬렉션, 현재 경로에서 방문한 메시지 Set)
            var stack = new Stack<Tuple<IMessage, ObservableCollection<MessageNode>, HashSet<IMessage>>>();

            // 시작점: 최상위 메시지, 루트 노드, 그리고 새로운 방문 기록용 HashSet 추가
            stack.Push(Tuple.Create(message, rootNodes, new HashSet<IMessage>()));

            while (stack.Count > 0)
            {
                var (currentMessage, parentNodes, visited) = stack.Pop();

                // ★★★ 핵심: 현재 메시지를 방문 목록에 추가 ★★★
                if (!visited.Add(currentMessage))
                {
                    // 이미 이 경로에서 처리한 메시지라면 순환이므로 건너뛴다.
                    continue;
                }

                var descriptor = currentMessage.Descriptor;

                foreach (var field in descriptor.Fields.InDeclarationOrder())
                {
                    var fieldValue = field.Accessor.GetValue(currentMessage);
                    var node = new MessageNode { Name = field.Name };

                    if (fieldValue is IMessage nestedMessage)
                    {
                        node.Value = $"({nestedMessage.Descriptor.Name})";
                        parentNodes.Add(node);

                        // ★★★ 핵심: 자식 노드로 내려갈 때 현재까지의 방문 기록을 복사해서 넘겨준다. ★★★
                        stack.Push(Tuple.Create(nestedMessage, node.Children, new HashSet<IMessage>(visited)));
                    }
                    else if (fieldValue is IList list)
                    {
                        node.Value = $"[Count: {list.Count}]";
                        parentNodes.Add(node);

                        for (int i = 0; i < list.Count; i++)
                        {
                            var item = list[i];
                            var childNode = new MessageNode { Name = $"[{i}]" };

                            if (item is IMessage listItemMessage)
                            {
                                childNode.Value = $"({listItemMessage.Descriptor.Name})";
                                node.Children.Add(childNode);

                                // 리스트 아이템에 대해서도 방문 기록을 복사해서 넘겨준다.
                                stack.Push(Tuple.Create(listItemMessage, childNode.Children, new HashSet<IMessage>(visited)));
                            }
                            else
                            {
                                childNode.Value = item?.ToString() ?? "null";
                                node.Children.Add(childNode);
                            }
                        }
                    }
                    else
                    {
                        node.Value = fieldValue?.ToString() ?? "null";
                        parentNodes.Add(node);
                    }
                }
            }

            return rootNodes;
        }
    }
}