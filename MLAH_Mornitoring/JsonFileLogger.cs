using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf; // Protobuf JSON 변환용

namespace MLAH_Mornitoring
{
    public class JsonFileLogger
    {
        // Singleton
        private static readonly Lazy<JsonFileLogger> _lazy = new Lazy<JsonFileLogger>(() => new JsonFileLogger());
        public static JsonFileLogger Instance => _lazy.Value;

        // I/O 부하 분리를 위한 BlockingCollection (생산자-소비자 패턴)
        // 큐 크기를 제한하여(예: 10000개) 메모리 폭주 방지
        private BlockingCollection<LogEntry> _logQueue = new BlockingCollection<LogEntry>(10000);

        private string _baseDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
    "Unit2EnvLogs"
);
        private const long MAX_FILE_SIZE = 3 * 1024 * 1024; // 3MB

        private JsonFileLogger()
        {
            // 백그라운드 기록 스레드 시작
            Task.Factory.StartNew(ProcessLogQueue, TaskCreationOptions.LongRunning);
        }

        // 외부에서 호출하는 메서드 (Non-blocking)
        public void EnqueueLog(IMessage message, string messageName)
        {
            if (message == null) return;

            // 큐에 넣기만 하고 즉시 리턴 (I/O 대기 없음)
            _logQueue.TryAdd(new LogEntry
            {
                Timestamp = DateTime.Now,
                MessageName = messageName,
                ProtoMessage = message
            });
        }

        // 실제 파일 쓰기를 담당하는 백그라운드 작업
        private void ProcessLogQueue()
        {
            Directory.CreateDirectory(_baseDirectory);
            string currentFilePath = GetNewFilePath();

            // Protobuf를 JSON으로 바꾸는 포매터
            var formatter = new Google.Protobuf.JsonFormatter(Google.Protobuf.JsonFormatter.Settings.Default);

            foreach (var entry in _logQueue.GetConsumingEnumerable())
            {
                try
                {
                    // 1. 파일 크기 체크 및 로테이션
                    var fileInfo = new FileInfo(currentFilePath);
                    if (fileInfo.Exists && fileInfo.Length >= MAX_FILE_SIZE)
                    {
                        currentFilePath = GetNewFilePath();
                    }

                    // 2. JSON 데이터 생성 (NDJSON: 줄바꿈으로 구분된 JSON 권장)
                    // 직접 문자열을 조합하여 오버헤드 최소화
                    var jsonBuilder = new StringBuilder();
                    jsonBuilder.Append("{");
                    jsonBuilder.Append($"\"time\": \"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\",");
                    jsonBuilder.Append($"\"type\": \"{entry.MessageName}\",");
                    jsonBuilder.Append($"\"data\": {formatter.Format(entry.ProtoMessage)}"); // Protobuf -> JSON 변환
                    jsonBuilder.Append("}");
                    jsonBuilder.AppendLine(); // 한 줄 띄우기

                    // 3. 파일 쓰기 (Append 모드)
                    File.AppendAllText(currentFilePath, jsonBuilder.ToString(), Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    // 로깅 실패가 앱을 죽이지 않도록 예외 처리
                    System.Diagnostics.Debug.WriteLine($"Log Error: {ex.Message}");
                }
            }
        }

        private string GetNewFilePath()
        {
            // 파일명 예시: Log_20251215_143001_555.json
            return Path.Combine(_baseDirectory, $"Log_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json");
        }

        // 큐에 들어갈 데이터 구조체
        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string MessageName { get; set; }
            public IMessage ProtoMessage { get; set; }
        }
    }
}