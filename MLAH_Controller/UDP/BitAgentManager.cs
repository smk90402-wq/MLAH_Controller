using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Buffers.Binary;
using System.Windows;


namespace MLAH_Controller
{
    public class BitAgentManager
    {
        // =============================================================
        // Singleton
        // =============================================================
        private static readonly Lazy<BitAgentManager> _lazy = new Lazy<BitAgentManager>(() => new BitAgentManager());
        public static BitAgentManager Instance => _lazy.Value;

        // =============================================================
        // Events (ViewModel이 구독할 이벤트)
        // =============================================================
        // HW 상태 수신 시 발생 (Type, Status)
        public event Action<int, int> OnHwStatusReceived;

        // SW 상태 수신 시 발생 (Type, Status)
        public event Action<int, int> OnSwStatusReceived;

        // =============================================================
        // UDP Objects
        // =============================================================
        private UdpClient _udpListener;
        private UdpClient _udpSender;
        private bool _isRunning = false;
        private const int LISTEN_PORT = 49350; // Controller가 듣는 포트 (Config.ini와 일치해야 함)

        public BitAgentManager()
        {
            _udpSender = new UdpClient(); // 송신용은 포트 바인딩 불필요
        }

        public void StartListener()
        {
            if (_isRunning) return;

            try
            {
                // 수신용 포트 바인딩
                _udpListener = new UdpClient(LISTEN_PORT);
                _isRunning = true;

                // 비동기 수신 루프 시작
                Task.Run(ReceiveLoop);

                System.Diagnostics.Debug.WriteLine($"[BitManager] Listening on {LISTEN_PORT}...");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"BitAgent 수신 포트({LISTEN_PORT}) 바인딩 실패:\n{ex.Message}");
            }
        }

        public void StopListener()
        {
            _isRunning = false;
            _udpListener?.Close();
            _udpListener?.Dispose();
        }

        // =============================================================
        // 1. 수신 루프 (Receive & Parse)
        // =============================================================
        private async Task ReceiveLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var result = await _udpListener.ReceiveAsync();
                    byte[] buffer = result.Buffer;

                    if (buffer.Length < 4) continue;

                    // ★ Little-Endian으로 ID 파싱 (BitAgent와 동일)
                    int msgID = BinaryPrimitives.ReadInt32LittleEndian(buffer);

                    switch (msgID)
                    {
                        case (int)MsgID.HW_STATUS: // ID: 2
                            ProcessHwStatus(buffer);
                            break;

                        case (int)MsgID.SW_STATUS: // ID: 1
                            ProcessSwStatus(buffer);
                            break;

                            // Agent가 보내는 PONG 등을 처리하려면 여기에 추가
                    }
                }
                catch (ObjectDisposedException) { break; } // 종료 시 발생
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BitManager Recv Error] {ex.Message}");
                }
            }
        }

        private void ProcessHwStatus(byte[] data)
        {
            if (data.Length < 12) return;
            // [Header 4] [Type 4] [Status 4]
            int type = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(4));
            int status = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(8));

            // UI 스레드로 이벤트 발생 (선택 사항: ViewModel에서 처리해도 됨)
            OnHwStatusReceived?.Invoke(type, status);
        }

        private void ProcessSwStatus(byte[] data)
        {
            if (data.Length < 12) return;
            int type = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(4));
            int status = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(8));

            OnSwStatusReceived?.Invoke(type, status);
        }

        // =============================================================
        // 2. 송신 기능 (Send Command)
        // =============================================================

        /// <summary>
        /// 특정 Agent에게 SW 실행/종료 명령 전송
        /// </summary>
        /// <param name="targetIp">Agent IP</param>
        /// <param name="targetPort">Agent Port</param>
        /// <param name="controlType">제어할 대상 Type (예: 11-상황인지)</param>
        /// <param name="isStart">true:실행, false:종료</param>
        public async Task SendSwControlAsync(string targetIp, int targetPort, int controlType, bool isStart)
        {
            try
            {
                var packet = new BitAgent_SWControl
                {
                    MessageID = (int)MsgID.SW_CONTROL, // 3
                    ControlType = controlType,
                    Command = isStart ? 1 : 0
                };

                // 구조체를 바이트 배열로 직렬화 (직접 구현 필요 or 아래 헬퍼 사용)
                byte[] data = PacketToBytes(packet);

                await _udpSender.SendAsync(data, data.Length, targetIp, targetPort);
                //System.Diagnostics.Debug.WriteLine($"[BitManager] Sent CMD to {targetIp}:{targetPort} (Type:{controlType}, Cmd:{packet.Command})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BitManager Send Error] {ex.Message}");
            }
        }

        // BitAgent_SWControl 클래스에 ToBytes()가 없다면 여기서 임시로 구현
        // (MessageProtocol.cs에 ToBytes()를 추가하는 게 가장 좋음)
        private byte[] PacketToBytes(BitAgent_SWControl packet)
        {
            byte[] bytes = new byte[12];
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0), packet.MessageID);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), packet.ControlType);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), packet.Command);
            return bytes;
        }
    }
}