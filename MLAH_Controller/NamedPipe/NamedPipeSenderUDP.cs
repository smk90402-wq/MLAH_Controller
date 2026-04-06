using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using Google.Protobuf;

namespace MLAH_Controller
{


    public class PipeDataPacketUdpRaw
    {
        public string SenderIp { get; set; }
        public int SenderPort { get; set; }
        public byte[] UdpData { get; set; } // UDP로 받은 원본 byte 배열
    }

    public class NamedPipeSenderUDP : IDisposable
    {
        private static readonly Lazy<NamedPipeSenderUDP> _instance = new Lazy<NamedPipeSenderUDP>(() => new NamedPipeSenderUDP());
        public static NamedPipeSenderUDP Instance => _instance.Value;

        private const string PipeName = "MLAHMonitoringPipeUDP";
        private NamedPipeClientStream _pipeClient;
        private readonly object _lock = new object();
        private volatile bool _isConnected = false; // 여러 스레드에서 접근하므로 volatile 키워드 사용

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Task _connectionManagerTask;

        private readonly Channel<PipeDataPacketUdpRaw> _packetChannel;

        private NamedPipeSenderUDP()
        {
            _packetChannel = Channel.CreateUnbounded<PipeDataPacketUdpRaw>();

            // 생성자에서 백그라운드 연결 관리 작업을 시작합니다.
            _connectionManagerTask = Task.Run(() => ManageConnectionAsync(_cancellationTokenSource.Token));
            Task.Run(() => PipeSendingConsumerLoopAsync(_cancellationTokenSource.Token));
        }

        private async Task PipeSendingConsumerLoopAsync(CancellationToken token)
        {
            // 채널에서 읽을 데이터가 있는 동안 계속 루프
            await foreach (var packet in _packetChannel.Reader.ReadAllAsync(token))
            {
                if (!_isConnected) continue;

                lock (_lock)
                {
                    if (!_isConnected || _pipeClient == null || !_pipeClient.IsConnected)
                    {
                        _isConnected = false;
                        continue; // 다음 루프로 넘어감
                    }
                    try
                    {
                        string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(packet);
                        byte[] buffer = Encoding.UTF8.GetBytes(jsonString);
                        _pipeClient.Write(BitConverter.GetBytes(buffer.Length), 0, 4);
                        _pipeClient.Write(buffer, 0, buffer.Length);
                    }
                    catch (IOException) { _isConnected = false; }
                    catch (Exception ex) 
                    { 
                        //Debug.WriteLine($"UDP Pipe send error: {ex.Message}"); 
                    }
                }
            }
        }

        // 연결을 관리하는 독립적인 백그라운드 작업
        private async Task ManageConnectionAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!_isConnected)
                {
                    try
                    {
                        // 기존 클라이언트가 있다면 정리
                        _pipeClient?.Dispose();
                        _pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out, PipeOptions.Asynchronous);

                        //System.Diagnostics.Debug.WriteLine("Attempting to connect to pipe server...");
                        //await _pipeClient.ConnectAsync(1000, token); // 비동기로 연결 시도
                        await _pipeClient.ConnectAsync(token);
                        _isConnected = true;
                        //System.Diagnostics.Debug.WriteLine("Pipe connected successfully.");
                    }
                    catch (OperationCanceledException)
                    {
                        // Dispose() 등으로 토큰이 취소되었을 때 자연스럽게 루프 종료
                        break;
                    }
          
                    catch (Exception ex)
                    {
                        // 타임아웃 외의 진짜 에러(권한 문제 등)만 잡아서 확인
                        Debug.WriteLine($"[NamedPipe Error] {ex.Message}");

                        _isConnected = false;
                        // 에러 발생 시 너무 빠른 재시도 방지용 딜레이
                        await Task.Delay(1000, token);
                    }
                }
                else // 이미 연결된 상태라면
                {
                    // 잠시 대기하여 무한 루프가 CPU를 과도하게 점유하는 것을 방지
                    await Task.Delay(100, token);
                }
            }
        }

     
        public void SendData(PipeDataPacketUdpRaw packet)
        {
            // 채널의 Writer에 데이터를 쓰려고 시도합니다. 
            // 큐가 꽉 찰 일이 거의 없으므로 TryWrite를 사용해 매우 빠르게 작업을 완료합니다.
            _packetChannel.Writer.TryWrite(packet);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel(); // 백그라운드 작업 취소
            try
            {
                // 작업이 정상적으로 또는 '취소'되어 종료될 때까지 대기합니다.
                _connectionManagerTask?.Wait(2000);
            }
            catch (AggregateException ae)
            {
                // AggregateException 내부의 예외 중 TaskCanceledException만 골라서 정상 처리된 것으로 간주합니다.
                ae.Handle(e => e is TaskCanceledException);
            }
            catch (TaskCanceledException)
            {
                // 작업 취소는 정상적인 종료 과정이므로 이 예외는 무시합니다.
            }
            _pipeClient?.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}