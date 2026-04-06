
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


namespace MLAH_Controller
{
    public partial class ViewModel_ScenarioView : CommonBase
    {

        private bool _isSimpleTestMode = false;

        //이미 수행한 시나리오 저장용
        private HashSet<string> _playedScenarios = new HashSet<string>();

        // 작업 취소 토큰 (중단 버튼 클릭 시 즉시 멈추기)
        private CancellationTokenSource _cts;

        #region [제어 대상 선택 체크박스 프로퍼티]

        private bool _isChecked_Recog = true;
        public bool IsChecked_Recog { get => _isChecked_Recog; set { _isChecked_Recog = value; OnPropertyChanged("IsChecked_Recog"); } }

        private bool _isChecked_Battle1 = true;
        public bool IsChecked_Battle1 { get => _isChecked_Battle1; set { _isChecked_Battle1 = value; OnPropertyChanged("IsChecked_Battle1"); } }

        private bool _isChecked_Battle2 = true;
        public bool IsChecked_Battle2 { get => _isChecked_Battle2; set { _isChecked_Battle2 = value; OnPropertyChanged("IsChecked_Battle2"); } }

        private bool _isChecked_Battle3 = true;
        public bool IsChecked_Battle3 { get => _isChecked_Battle3; set { _isChecked_Battle3 = value; OnPropertyChanged("IsChecked_Battle3"); } }

        private bool _isChecked_MissionCtrl = true;
        public bool IsChecked_MissionCtrl { get => _isChecked_MissionCtrl; set { _isChecked_MissionCtrl = value; OnPropertyChanged("IsChecked_MissionCtrl"); } }

        private bool _isChecked_Display = true;
        public bool IsChecked_Display { get => _isChecked_Display; set { _isChecked_Display = value; OnPropertyChanged("IsChecked_Display"); } }

        private bool _isChecked_Icd = true;
        public bool IsChecked_Icd { get => _isChecked_Icd; set { _isChecked_Icd = value; OnPropertyChanged("IsChecked_Icd"); } }

        private bool _isChecked_Uav1 = true;
        public bool IsChecked_Uav1 { get => _isChecked_Uav1; set { _isChecked_Uav1 = value; OnPropertyChanged("IsChecked_Uav1"); } }

        private bool _isChecked_Uav2 = true;
        public bool IsChecked_Uav2 { get => _isChecked_Uav2; set { _isChecked_Uav2 = value; OnPropertyChanged("IsChecked_Uav2"); } }

        private bool _isChecked_Uav3 = true;
        public bool IsChecked_Uav3 { get => _isChecked_Uav3; set { _isChecked_Uav3 = value; OnPropertyChanged("IsChecked_Uav3"); } }

        #endregion

        #region [모의 자동화 프로퍼티]

        private bool _IsAutomationRunning = false;
        public bool IsAutomationRunning
        {
            get => _IsAutomationRunning;
            set { _IsAutomationRunning = value; OnPropertyChanged("IsAutomationRunning"); }
        }

        private bool _IsForceStart = false;
        public bool IsForceStart
        {
            get => _IsForceStart;
            set { _IsForceStart = value; OnPropertyChanged("IsForceStart"); }
        }

        private string _AutomationStatusText = "대기 중";
        public string AutomationStatusText
        {
            get => _AutomationStatusText;
            set { _AutomationStatusText = value; OnPropertyChanged("AutomationStatusText"); }
        }

        private Brush _AutomationStatusBrush = Brushes.Gray;
        public Brush AutomationStatusBrush
        {
            get => _AutomationStatusBrush;
            set { _AutomationStatusBrush = value; OnPropertyChanged("AutomationStatusBrush"); }
        }

        private Queue<string> _scenarioQueue = new Queue<string>();
        private bool _isWaitingForMissionResult = false;

        private Dictionary<int, int> _hwStatusMap = new Dictionary<int, int>();
        private Dictionary<int, int> _swStatusMap = new Dictionary<int, int>();

        // HW 타입별 마지막 수신 시간을 기록 (Ping 체크용)
        private Dictionary<int, DateTime> _lastHwHeartbeat = new Dictionary<int, DateTime>();
        private DispatcherTimer _pingCheckTimer;

        private void Callback_OnHwStatusReceived(int hwType, int status)
        {
            // HW 상태가 들어오면, 이 장비의 마지막 생존 시간 갱신
            _lastHwHeartbeat[hwType] = DateTime.Now;

            // 해당 HW 장비(PC)에서 돌아가는 SW 타입 번호들을 가져옴
            List<int> relatedSwTypes = GetSwTypesByHwType(hwType);

            foreach (int swType in relatedSwTypes)
            {
                _hwStatusMap[swType] = status;
                UpdateCombinedStatus(swType);
            }
        }

        private void Callback_OnSwStatusReceived(int swType, int status)
        {
            _swStatusMap[swType] = status;
            UpdateCombinedStatus(swType);
        }

        // 📌 핵심: 수신된 HW 장비 번호(1~9)를 담당하는 SW 번호로 매핑
        private List<int> GetSwTypesByHwType(int hwType)
        {
            var list = new List<int>();
            switch (hwType)
            {
                case 1: // 전장1 장비 -> (전장1, 상황인지)
                    list.Add(1); list.Add(11); break;
                case 2: // 전장2 장비
                    list.Add(2); break;
                case 3: // 전장3 장비
                    list.Add(3); break;
                case 4: // 임무통제 세트 장비
                    list.Add(4); list.Add(41); list.Add(42); break;
                case 5: // 무인기 1
                    list.Add(5); break;
                case 6: // 무인기 2
                    list.Add(6); break;
                case 7: // 무인기 3
                    list.Add(7); break;
                case 8: // RTV 1 장비
                    list.Add(1); list.Add(11); break;

                case 9: // RTV 2 장비 (말씀하신 타입 9번!)
                        // INI를 보면 RTV2는 사실상 거의 모든 테스트를 수행합니다.
                        // 일단 말씀하신 전장1, 인지는 필수로 넣습니다.
                    list.Add(1); list.Add(11);

                    // 만약 RTV2에서 무인기나 ICD 등도 띄우신다면 아래 주석을 풀고 매핑해주세요!
                    // list.Add(4); list.Add(41); list.Add(42); list.Add(5);
                    break;
            }
            return list;
        }

        // ==========================================
        // [4] Ping(타임아웃) 체크 로직
        // ==========================================
        private void PingCheckTimer_Tick(object sender, EventArgs e)
        {
            // 1~9번 HW를 싹 다 검사
            for (int hwType = 1; hwType <= 9; hwType++)
            {
                if (_lastHwHeartbeat.ContainsKey(hwType))
                {
                    // 마지막 수신으로부터 3초 이상 지났으면 끊김(0) 처리
                    if ((DateTime.Now - _lastHwHeartbeat[hwType]).TotalSeconds > 3)
                    {
                        List<int> relatedSwTypes = GetSwTypesByHwType(hwType);
                        foreach (int swType in relatedSwTypes)
                        {
                            // 끊겼으므로 HW, SW 모두 0으로 리셋
                            _hwStatusMap[swType] = 0;
                            _swStatusMap[swType] = 0;
                            UpdateCombinedStatus(swType);
                        }
                    }
                }
            }
        }

        // ==========================================
        // [5] UI (색상) 갱신 로직
        // ==========================================
        private void UpdateCombinedStatus(int swType)
        {
            int hw = _hwStatusMap.ContainsKey(swType) ? _hwStatusMap[swType] : 0;
            int sw = _swStatusMap.ContainsKey(swType) ? _swStatusMap[swType] : 0;

            // 계산: 둘 다 0이면 빨강(0), HW만 연결되면 노랑(1), 둘 다 연결되면 초록(2)
            int combinedStatus = 0;
            if (hw == 1)
            {
                combinedStatus = (sw == 1) ? 2 : 1;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                switch (swType)
                {
                    case 1: BattleSim1Status = combinedStatus; break;
                    case 11: RecogSimStatus = combinedStatus; break;
                    case 2: BattleSim2Status = combinedStatus; break;
                    case 3: BattleSim3Status = combinedStatus; break;
                    case 4: MissionCtrlStatus = combinedStatus; break;
                    case 41: DisplaySimStatus = combinedStatus; break;
                    case 42: IcdModuleStatus = combinedStatus; break;
                    case 5: UavSim1Status = combinedStatus; break;
                    case 6: UavSim2Status = combinedStatus; break;
                    case 7: UavSim3Status = combinedStatus; break;
                }
            });
        }

        #endregion

        #region [모의 자동화 커맨드]

        public RelayCommand StartAutomationCommand { get; set; }
        public RelayCommand StopAutomationCommand { get; set; }
        public RelayCommand StartSimpleAutomationCommand { get; set; }
        public RelayCommand TurnOnAllAgentsCommand { get; set; }
        public RelayCommand TurnOffAllAgentsCommand { get; set; }

        #region [Agent 실시간 상태 프로퍼티] (XAML 바인딩용)

        private int _battleSim1Status;
        public int BattleSim1Status { get => _battleSim1Status; set { _battleSim1Status = value; OnPropertyChanged("BattleSim1Status"); } }

        private int _recogSimStatus;
        public int RecogSimStatus { get => _recogSimStatus; set { _recogSimStatus = value; OnPropertyChanged("RecogSimStatus"); } }

        private int _battleSim2Status;
        public int BattleSim2Status { get => _battleSim2Status; set { _battleSim2Status = value; OnPropertyChanged("BattleSim2Status"); } }

        private int _battleSim3Status;
        public int BattleSim3Status { get => _battleSim3Status; set { _battleSim3Status = value; OnPropertyChanged("BattleSim3Status"); } }

        private int _missionCtrlStatus;
        public int MissionCtrlStatus { get => _missionCtrlStatus; set { _missionCtrlStatus = value; OnPropertyChanged("MissionCtrlStatus"); } }

        private int _displaySimStatus;
        public int DisplaySimStatus { get => _displaySimStatus; set { _displaySimStatus = value; OnPropertyChanged("DisplaySimStatus"); } }

        private int _icdModuleStatus;
        public int IcdModuleStatus { get => _icdModuleStatus; set { _icdModuleStatus = value; OnPropertyChanged("IcdModuleStatus"); } }

        private int _uavSim1Status;
        public int UavSim1Status { get => _uavSim1Status; set { _uavSim1Status = value; OnPropertyChanged("UavSim1Status"); } }

        private int _uavSim2Status;
        public int UavSim2Status { get => _uavSim2Status; set { _uavSim2Status = value; OnPropertyChanged("UavSim2Status"); } }

        private int _uavSim3Status;
        public int UavSim3Status { get => _uavSim3Status; set { _uavSim3Status = value; OnPropertyChanged("UavSim3Status"); } }

        #endregion



        public void StartAutomationAction(object param)
        {
            ViewModel_ScenarioView.SingletonInstance.SceneBorderVisibility = Visibility.Collapsed;
            _isSimpleTestMode = false;

            // ★ 기존 토큰이 있다면 폐기하고 새로 생성
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            RunAutomationLogic();
        }

        public void StartSimpleAutomationAction(object param)
        {
            ViewModel_ScenarioView.SingletonInstance.SceneBorderVisibility = Visibility.Collapsed;
            // 테스트 모드 플래그 설정
            _isSimpleTestMode = true;

            // ★ 기존 토큰이 있다면 폐기하고 새로 생성
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            // 공통 실행 로직 호출
            RunAutomationLogic();
        }

        private void RunAutomationLogic()
        {
            if (IsAutomationRunning) return;

            // 1. 시나리오 파일 목록 로드 (오름차순)
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "자동생성시나리오");
            if (!Directory.Exists(folderPath))
            {
                System.Windows.MessageBox.Show($"경로를 찾을 수 없습니다: {folderPath}");
                return;
            }

            // 1. 전체 파일 목록 가져오기 (오름차순)
            var allFiles = Directory.GetFiles(folderPath, "*.json").OrderBy(f => f).ToList();

            // 2. 큐 초기화 및 필터링 (이미 수행한 것은 제외)
            _scenarioQueue.Clear();
            foreach (var file in allFiles)
            {
                // 이미 수행한 목록에 없으면 큐에 추가
                if (!_playedScenarios.Contains(file))
                {
                    _scenarioQueue.Enqueue(file);
                }
            }

            // 3. 만약 다 했는데 또 시작 누르면? -> 초기화하고 다시 할지 물어보거나, 완료 메시지
            if (_scenarioQueue.Count == 0)
            {
                if (allFiles.Count > 0)
                {
                    // 모든 시나리오가 완료된 상태라면 기록 초기화 후 다시 큐잉 (선택사항)
                    if (MessageBox.Show("모든 시나리오가 완료되었습니다. 처음부터 다시 하시겠습니까?", "확인", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        _playedScenarios.Clear();
                        foreach (var file in allFiles) _scenarioQueue.Enqueue(file);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("실행할 시나리오 파일이 없습니다.");
                    return;
                }
            }

            IsAutomationRunning = true;
            _isWaitingForMissionResult = false;

            // ★ 취소 토큰 생성
            _cts = new CancellationTokenSource();

            // 자동화 루프 시작 (비동기)
            Task.Run(() => AutomationLoop(_cts.Token));
        }
        public void StopAutomationAction(object param)
        {
            if (!IsAutomationRunning) return;

            // 1. 플래그 끄기
            IsAutomationRunning = false;

            // 2. ★ 중요: 취소 토큰 발동! (대기 중인 Task들을 즉시 깨움)
            _cts?.Cancel();

            // 3. 상태 메시지 업데이트
            SetAutoStatus("중단 요청됨...", Brushes.OrangeRed);
        }

        #endregion

        #region [모의 자동화 핵심 로직]

        private async Task AutomationLoop(CancellationToken token)
        {
            try
            {
                // ★ [수정 1] while 조건에서 IsAutomationRunning 제거
                // 이유: IsAutomationRunning이 false가 되어도, 반드시 내부 로직을 통해 
                // 예외를 던져서 catch 블록(Cleanup)을 타게 만들기 위함입니다.
                while (_scenarioQueue.Count > 0)
                {
                    // ★ [수정 2] 취소/중단 체크를 최우선으로 수행
                    // 토큰이 취소되었거나(Cancel), 플래그가 꺼졌으면(Stop 버튼) -> 강제 종료 처리
                    if (token.IsCancellationRequested || !IsAutomationRunning)
                    {
                        throw new OperationCanceledException();
                    }

                    string currentFile = _scenarioQueue.Peek();
                    string displayName = GetShortFileName(currentFile);

                    SetAutoStatus($"준비: {displayName}", Brushes.Yellow);

                    // 1. SW 실행 및 Ready 체크
                    bool isReady = await PrepareAgentsAsync(token);

                    if (!isReady)
                    {
                        SetAutoStatus("Ready 실패 (중단)", Brushes.Red);
                        // 실패 시에도 깔끔하게 끄고 나가기 위해 예외 처리 하거나 Cleanup 호출
                        await CleanupAndRestartAgentsAsync();
                        SetAutoStatus("대기 중", Brushes.Gray);
                        IsAutomationRunning = false;
                        return;
                    }

                    // 2. 시나리오 로드
                    currentFile = _scenarioQueue.Dequeue();
                    _playedScenarios.Add(currentFile); // 완료 목록 추가

                    await LoadScenarioAsync(currentFile);

                    // 3. 모의 시작
                    //SetAutoStatus($"진행: {displayName}", Brushes.LimeGreen);
                    SetAutoStatus($"모의 진행 중", Brushes.LimeGreen);
                    await ScenarioPlayCommandAction(null);

                    // 4. 결과 대기 (53135 수신 대기)
                    _isWaitingForMissionResult = true;
                    await WaitForMissionCompletion(token);

                    // 5. 모의 종료 및 정리 (정상 진행 시 Cleanup)
                    // ★ 여기서도 중단 여부 체크 (대기 중에 Stop 눌렀을 수 있음)
                    if (token.IsCancellationRequested || !IsAutomationRunning)
                    {
                        throw new OperationCanceledException();
                    }

                    await CleanupAndRestartAgentsAsync();
                }

                if (IsAutomationRunning)
                {
                    SetAutoStatus("완료", Brushes.Cyan);
                    IsAutomationRunning = false;
                }
            }
            catch (OperationCanceledException)
            {
                // 중단 버튼 누르면 무조건 이쪽으로 옴
                SetAutoStatus("사용자 중단", Brushes.Red);

                // 여기서 SW 종료 명령(Kill)을 확실하게 보냄
                await CleanupAndRestartAgentsAsync();

                // 최종 상태 복귀
                SetAutoStatus("대기 중", Brushes.Gray);
            }
            catch (Exception ex)
            {
                SetAutoStatus($"오류: {ex.Message}", Brushes.Red);
                // 오류 나서 멈출 때도 끄는 게 안전함
                await CleanupAndRestartAgentsAsync();
                SetAutoStatus("대기 중", Brushes.Gray);
            }
        }

        private string GetShortFileName(string fullPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(fullPath);
            // "RandomScenario" 또는 "Scenario" 같은 접두어 제거
            fileName = fileName.Replace("RandomScenario", "").Replace("Scenario", "");
            // 앞뒤 언더바, 공백 제거
            return fileName.Trim('_', ' ');
        }

        // 미션 완료 시그널 대기용
        private TaskCompletionSource<bool> _missionCompleteTcs;

        private async Task WaitForMissionCompletion(CancellationToken token)
        {
            _missionCompleteTcs = new TaskCompletionSource<bool>();

            // 토큰이 취소되면 TCS도 취소 처리 (대기 해제)
            using (token.Register(() => _missionCompleteTcs.TrySetCanceled()))
            {
                if (_isSimpleTestMode)
                {
                    // 테스트 모드: 30초 대기
                    await Task.WhenAny(_missionCompleteTcs.Task, Task.Delay(30000, token));
                }
                else
                {
                    // 일반 모드: 1시간 대기
                    await Task.WhenAny(_missionCompleteTcs.Task, Task.Delay(3600000, token));
                }
            }
        }

        // ★ ID: 53135 수신 시 호출되는 콜백 (생성자에서 구독 필수)
        private void Callback_OnMissionResultReceived(MissionResultData data)
        {
            if (data.SystemRecommend != 3)
            {
                return;
            }

            if (IsAutomationRunning && _isWaitingForMissionResult)
            {
                _isWaitingForMissionResult = false;

                // 모의 종료 버튼 동작 수행
                Application.Current.Dispatcher.Invoke(() => ScenarioStopCommandAction(null));

                // 대기 중인 Task 해제 -> 루프 계속 진행
                _missionCompleteTcs?.TrySetResult(true);
            }
        }

        // SW 실행 및 Ready 상태 체크
        private async Task<bool> PrepareAgentsAsync(CancellationToken token)
        {
            SetAutoStatus("SW 구동 중...", Brushes.Orange);
            await ControlAllAgents(true);

            if (IsForceStart) return true;

            // 웜업 (60초) - 취소 토큰 적용
            int warmUpSeconds = 60;
            for (int i = warmUpSeconds; i > 0; i--)
            {
                token.ThrowIfCancellationRequested(); // 취소 체크
                SetAutoStatus($"초기화... ({i}s)", Brushes.DarkOrange);
                try { await Task.Delay(1000, token); } catch { return false; }
            }

            // Ready 체크 (5초)
            int timeout = 5;
            while (timeout > 0 && IsAutomationRunning)
            {
                token.ThrowIfCancellationRequested();

                if (CheckAllAgentsReady())
                {
                    SetAutoStatus("Ready", Brushes.DeepSkyBlue);
                    return true;
                }

                SetAutoStatus($"Ready 대기... ({timeout}s)", Brushes.Orange);
                try { await Task.Delay(1000, token); } catch { return false; }
                timeout--;
            }

            return false;
        }

        // SW 종료 및 재시작 (Reset)
        private async Task CleanupAndRestartAgentsAsync()
        {
            // UI 스레드 충돌 방지 위해 try-catch
            try
            {
                SetAutoStatus("SW 종료 중...", Brushes.OrangeRed);

                // 모의 정지 명령 (UI용)
                Application.Current.Dispatcher.Invoke(() => ScenarioStopCommandAction(null));

                // 1. 모든 SW 강제 종료 (Kill)
                await ControlAllAgents(false);

                // 2. 완전히 꺼질 때까지 대기
                await Task.Delay(3000);
            }
            catch { }
        }

        // 모든 Agent에게 명령 전송 헬퍼
        private async Task ControlAllAgents(bool isStart)
        {
            // 대상 Agent IP 및 Port 목록 (Config나 상수로 관리 권장)
            var targets = new List<(string Ip, int Port, int Type)>
            {
                ("192.168.20.201", 49341, 1), // 전장1
                ("192.168.20.201", 49341, 11), // 상황인지
                //("192.168.20.202", 49342, 2), // 전장2
                //("192.168.20.203", 49343, 3), // 전장3
                ("192.168.20.100", 49344, 4), // 임무통제
                ("192.168.20.100", 49344, 41), // 시현모의
                ("192.168.20.100", 49344, 42), // ICD
                ("192.168.20.101", 49345, 5), // 무인기1
                ("192.168.20.102", 49346, 6), // 무인기2
                ("192.168.20.103", 49347, 7), // 무인기3

                       //RTV1 테스트
                ("192.168.10.75", 49348, 1), // 전장1
                ("192.168.10.75", 49348, 11), // 상황인지
                //("192.168.10.75", 49349, 42), // ICDModule
                //("192.168.10.75", 49349, 5), // 무인기1

                //RTV2 테스트
                ("192.168.30.78", 49349, 1), // 전장1
                ("192.168.30.78", 49349, 11), // 상황인지
                //("192.168.10.73", 49349, 42), // ICDModule
                //("192.168.10.73", 49349, 5), // 무인기1
                
            };

            foreach (var t in targets)
            {
                // 💡 핵심: 해당 타입(Type)의 체크박스가 True인지 확인
                bool shouldSend = false;
                switch (t.Type)
                {
                    case 1: shouldSend = IsChecked_Battle1; break;
                    case 11: shouldSend = IsChecked_Recog; break;
                    case 2: shouldSend = IsChecked_Battle2; break;
                    case 3: shouldSend = IsChecked_Battle3; break;
                    case 4: shouldSend = IsChecked_MissionCtrl; break;
                    case 41: shouldSend = IsChecked_Display; break;
                    case 42: shouldSend = IsChecked_Icd; break;
                    case 5: shouldSend = IsChecked_Uav1; break;
                    case 6: shouldSend = IsChecked_Uav2; break;
                    case 7: shouldSend = IsChecked_Uav3; break;
                }

                // 체크되어 있을 때만 명령 전송
                if (shouldSend)
                {
                    await BitAgentManager.Instance.SendSwControlAsync(t.Ip, t.Port, t.Type, isStart);
                    await Task.Delay(50); // 약간의 텀
                }
            }
        }


        // 모든 Agent가 Ready(2) 상태인지 확인
        private bool CheckAllAgentsReady()
        {
            // Force Start 체크 시에는 상태 무시하고 바로 True
            if (IsForceStart) return true;

            if (_isSimpleTestMode)
            {
                // 완벽한 Ready(2: HW/SW 둘 다 연결)인지 확인
                if (_battleSim1Status != 2) return false; // Type 1

                // 나머지 장비는 무시하고 통과
                return true;
            }

            // 하나라도 완벽한 Ready(2)가 아니면 False 반환
            if (_battleSim1Status != 2) return false;
            if (_missionCtrlStatus != 2) return false;
            if (_uavSim1Status != 2) return false;
            if (_uavSim2Status != 2) return false;
            if (_uavSim3Status != 2) return false;
            if (_recogSimStatus != 2) return false;
            if (_displaySimStatus != 2) return false;
            if (_icdModuleStatus != 2) return false;

            // 모두 통과 시 True
            return true;
        }

        // UI 업데이트 헬퍼
        private void SetAutoStatus(string text, Brush color)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AutomationStatusText = text;
                AutomationStatusBrush = color;
            });
        }

        // 파일 로드 헬퍼 (기존 ScenarioOpenCommand 로직 재사용)
        private async Task LoadScenarioAsync(string filePath)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    string jsonContent = File.ReadAllText(filePath);
                    ScenarioViewClear();
                    model_UnitScenario = Newtonsoft.Json.JsonConvert.DeserializeObject<Model_UnitScenario>(jsonContent);
                    SceneFileName = Path.GetFileName(filePath); // 이름만 표시
                    SceneFileDescription = model_UnitScenario.ScenarioDesc;

                    // 콤보박스 등 UI 초기화
                    PackageTypeComboIndex = (int)model_UnitScenario.InitScenario.InputMissionPackage.MissionType;
                    SensorTypeComboIndex = (int)model_UnitScenario.InitScenario.InputMissionPackage.DateAndNight;

                    ScenarioViewInit(); // 맵 그리기 등 수행
                }
                catch { /* 로깅 */ }
            });
        }

        #endregion
    }

}



