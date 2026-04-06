
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using DevExpress.Xpf.Map;
using DevExpress.Xpf.Spreadsheet.Forms;
using DevExpress.XtraSpreadsheet.Model;
using MLAHInterop;
using Newtonsoft.Json;
using static DevExpress.Charts.Designer.Native.BarThicknessEditViewModel;
using Path = System.IO.Path;
using System.Collections.ObjectModel;

namespace MLAH_Controller
{
    public partial class ViewModel_ScenarioView : CommonBase
    {
        public class LogEntry
        {
            public string Time { get; set; }
            public string Message { get; set; }
            public Brush Color { get; set; } // 메시지 중요도에 따른 색상
        }

        // 1. 로그 리스트 (View와 바인딩됨)
        private ObservableCollection<LogEntry> _EventLogs = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> EventLogs
        {
            get { return _EventLogs; }
            set { _EventLogs = value; OnPropertyChanged("EventLogs"); }
        }

        // 2. 패널 펼침/접힘 상태
        private bool _IsLogPanelExpanded = true;
        public bool IsLogPanelExpanded
        {
            get { return _IsLogPanelExpanded; }
            set { _IsLogPanelExpanded = value; OnPropertyChanged("IsLogPanelExpanded"); }
        }

        // 3. 패널 토글 커맨드
        public RelayCommand ToggleLogPanelCommand { get; set; }

        // 생성자 내부에 추가할 내용:
        // ToggleLogPanelCommand = new RelayCommand(p => IsLogPanelExpanded = !IsLogPanelExpanded);

        // 4. 로그 추가 메서드 (외부에서 호출)
        // type: 0=Info(White), 1=Warning(Yellow), 2=Error/Hit(Red)
        public void AddLog(string message, int type = 0)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var newLog = new LogEntry
                {
                    //Time = DateTime.Now.ToString("[HB] HH:mm:ss"), // HB는 Heartbeat 시간 또는 현재시간
                    Time = DateTime.Now.ToString("HH:mm:ss"), // HB는 Heartbeat 시간 또는 현재시간
                    Message = message
                };

                switch (type)
                {
                    case 1: newLog.Color = Brushes.Yellow; break;
                    case 2: newLog.Color = Brushes.Red; break;
                    case 3: newLog.Color = Brushes.Cyan; break; // 시스템 알림 등
                    case 4: newLog.Color = Brushes.Lime; break;
                    default: newLog.Color = Brushes.WhiteSmoke; break;
                }

                // 리스트 맨 앞에 추가 (최신순)
                EventLogs.Insert(0, newLog);

                // 메모리 관리를 위해 100개 넘어가면 삭제
                if (EventLogs.Count > 100)
                {
                    EventLogs.RemoveAt(EventLogs.Count - 1);
                }
            });
        }

        // 5. 로그 클리어 커맨드 (필요시 사용)
        public RelayCommand ClearLogCommand { get; set; }
        // 생성자에: ClearLogCommand = new RelayCommand(p => EventLogs.Clear());


    }

}





