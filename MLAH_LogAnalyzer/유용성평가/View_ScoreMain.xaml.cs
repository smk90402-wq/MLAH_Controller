// 필수 using 문들
using DevExpress.Xpf.Charts;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Map;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using static MLAH_LogAnalyzer.MessageNameMapping;
// using System.Windows.Media; // C# 코드에서 직접 색상을 사용하지 않으므로 불필요

namespace MLAH_LogAnalyzer
{


    public partial class View_ScoreMain : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion

        private static readonly DateTime Epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private bool _isBulkUpdating = false;

        public ObservableCollection<ScoreScenarioItem> AllScenarioItems { get; set; } = new();
        public ObservableCollection<ScoreScenarioSummary> CheckedScenariosForDisplay { get; set; } = new();

        private string _SelectedScenarioName = "";
        public string SelectedScenarioName
        {
            get => _SelectedScenarioName;
            set 
            {
                _SelectedScenarioName = value;
                OnPropertyChanged(nameof(SelectedScenarioName)); 
            }
        }

        private ulong _logStartTime;
        public ulong LogStartTime
        {
            get => _logStartTime;
            set { _logStartTime = value; OnPropertyChanged(nameof(LogStartTime)); OnPropertyChanged(nameof(LogStartTimeFormatted)); }
        }

        private ulong _logEndTime;
        public ulong LogEndTime
        {
            get => _logEndTime;
            set { _logEndTime = value; OnPropertyChanged(nameof(LogEndTime)); OnPropertyChanged(nameof(LogEndTimeFormatted)); }
        }

        public DateTime ConvertedTimeFormatted => _ConvertedTime == 0 ? DateTime.Now.Date : Epoch.AddMilliseconds(_logStartTime).ToLocalTime();

        private string _lnputConvertedTime;
        public string lnputConvertedTime
        {
            get => _lnputConvertedTime;
            set { _lnputConvertedTime = value; OnPropertyChanged(nameof(lnputConvertedTime)); }
        }

        private ulong _ConvertedTime;
        public ulong ConvertedTime
        {
            get => _ConvertedTime;
            set { _ConvertedTime = value; OnPropertyChanged(nameof(ConvertedTime)); OnPropertyChanged(nameof(ConvertedTimeFormatted)); }
        }

        public DateTime LogStartTimeFormatted => _logStartTime == 0 ? DateTime.Now.Date : Epoch.AddMilliseconds(_logStartTime).ToLocalTime();
        public DateTime LogEndTimeFormatted => _logEndTime == 0 ? DateTime.Now.Date.AddSeconds(1) : Epoch.AddMilliseconds(_logEndTime).ToLocalTime();

        public View_ScoreMain()
        {
            InitializeComponent();
            this.DataContext = this;

            LogStartTime = 60; // 2000-01-01 00:00:00.000 (UTC)
            LogEndTime = 3600 * 1000; // 2000-01-01 00:01:00.000 (UTC)

        }

        private ScoreScenarioSummary _selectedScenarioSummary;
        public ScoreScenarioSummary SelectedScenarioSummary
        {
            get => _selectedScenarioSummary;
            set
            {
                _selectedScenarioSummary = value;
                OnPropertyChanged(nameof(SelectedScenarioSummary));

                if (_selectedScenarioSummary != null)
                {
                    SelectedScenarioName = _selectedScenarioSummary.ScenarioName;
                    LogStartTime = _selectedScenarioSummary.StartTime;
                    LogEndTime = _selectedScenarioSummary.EndTime;
                }
                else
                {
                    // 선택 해제되면 0으로 초기화
                    SelectedScenarioName = "";
                    LogStartTime = 0;
                    LogEndTime = 0;
                }
            }
        }

        //private async void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
        //    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        //    {
        //        try
        //        {
        //            await LoadScenariosAsync(dialog.FileName);
        //        }
        //        catch (Exception ex)
        //        {
        //            // 4. 'loadingTask'에서 예외가 발생하면 여기서 처리
        //            //    (WhenAll은 예외 발생 시 즉시 중단됨)
        //            MessageBox.Show($"Failed to load scenarios: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        //            // 참고: 예외가 발생하면 fakeProgressTask도 중단되므로
        //            // ShowFakeProgressAsync의 finally 블록이 실행되어 스플래시 스크린이 닫힘
        //        }
        //    }
        //}

        private async void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string targetPath = System.IO.Path.Combine(desktopPath, "분석데이터", "structured");

            if (!System.IO.Directory.Exists(targetPath))
            {
                targetPath = desktopPath; // 폴더가 없으면 그냥 바탕화면으로 지정
            }

            // CommonOpenFileDialog 대신 WPF 기본 OpenFileDialog 사용
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "시나리오 폴더 선택 (안에 있는 파일 확인 가능)",
                InitialDirectory = targetPath,
                ValidateNames = false,       // 파일 이름 유효성 검사 해제
                CheckFileExists = false,     // 실제 파일이 존재하지 않아도 선택 가능하게 설정 (핵심)
                CheckPathExists = true,
                FileName = "이 폴더 선택",      // 다이얼로그 파일명 입력창에 기본으로 뜰 텍스트
                Filter = "모든 파일 (*.*)|*.*" // 안에 있는 파일이 보이도록 필터 설정
            };

            // ShowDialog의 반환값은 bool? 타입입니다.
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // 사용자가 '이 폴더 선택' 상태로 열기를 누르거나 특정 파일을 클릭한 경우,
                    // 해당 경로에서 '폴더(디렉토리) 경로'만 깔끔하게 빼냅니다.
                    string selectedFolderPath = System.IO.Path.GetDirectoryName(dialog.FileName);

                    Utils.ClearScenarioCache();
                    CommunicationCalculator.ClearCaches();

                    await LoadScenariosAsync(selectedFolderPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load scenarios: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task LoadScenariosAsync(string parentDirectoryPath)
        {
            AllScenarioItems.Clear();
            CheckedScenariosForDisplay.Clear();

            var jsonFiles = Directory.GetFiles(parentDirectoryPath, "*.json").ToList();
            if (jsonFiles.Count == 0) return;

            int dummyNumber = 1;
            foreach (string filePath in jsonFiles)
            {
                var scenarioItem = new ScoreScenarioItem
                {
                    ScenarioName = Path.GetFileNameWithoutExtension(filePath), // 파일명 그대로 표시
                    ScenarioNumber = dummyNumber++,
                    FullPath = filePath, // ★ 절대 경로 저장
                    IsChecked = false,
                    IsAnalyzed = false, // 기본값: 미분석 (빨간색)

                    // 초기값은 0으로 세팅
                    StartTime = 0,
                    EndTime = 0,
                    MissionSuccessScore = 0,
                    CoverageScore = 0,
                    CommScore = 0,
                    SafetyScore = 0,
                    MissionDistScore = 0,
                    SpatialResScore = 0
                };
                scenarioItem.PropertyChanged += ScenarioItem_PropertyChanged;
                AllScenarioItems.Add(scenarioItem);
            }

            //List<int> scenarioNumbers;
            //SimpleProgressWindow progressWindow = null;

            //try
            //{
            //    //progressWindow = new SimpleProgressWindow();
            //    //progressWindow.UpdateProgress(0, "Searching for scenarios...");
            //    //progressWindow.Show();

            //    // 1. 시나리오 번호 목록 가져오기 (이미 수정됨)
            //    //scenarioNumbers = Utils.GetAvailableScenarios(parentDirectoryPath);

            //    if (scenarioNumbers.Count == 0)
            //    {
            //        MessageBox.Show("No scenarios found in the selected directory.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            //        return;
            //    }

            //    // 껍데기 아이템 생성 (데이터 로딩 X)
            //    foreach (int scenarioNum in scenarioNumbers)
            //    {
            //        var scenarioItem = new ScoreScenarioItem
            //        {
            //            ScenarioName = $"Scenario {scenarioNum}",
            //            ScenarioNumber = scenarioNum,
            //            FullPath = Path.Combine(parentDirectoryPath, $"Scenario{scenarioNum}.json"),
            //            IsChecked = false,
            //            IsAnalyzed = false, // 기본값: 미분석 (빨간색)

            //            // 초기값은 0으로 세팅
            //            StartTime = 0,
            //            EndTime = 0,
            //            MissionSuccessScore = 0,
            //            CoverageScore = 0,
            //            CommScore = 0,
            //            SafetyScore = 0,
            //            MissionDistScore = 0,
            //            SpatialResScore = 0
            //        };

            //        // 이벤트 구독
            //        scenarioItem.PropertyChanged += ScenarioItem_PropertyChanged;

            //        // UI 리스트에 추가
            //        AllScenarioItems.Add(scenarioItem);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    //throw;
            //    MessageBox.Show($"Error searching scenarios: {ex.Message}");
            //}
        }

        private (ulong Start, ulong End) GetScenarioTimeRange(ScenarioData data)
        {
            ulong minTs = ulong.MaxValue;
            ulong maxTs = ulong.MinValue;
            bool hasData = false;

            // 1. FlightData (비행 로그) 확인
            if (data.FlightData != null && data.FlightData.Any())
            {
                // 0인 값은 제외 (유효하지 않은 데이터일 수 있음)
                var validTimes = data.FlightData.Where(f => f.Timestamp > 0).Select(f => (ulong)f.Timestamp);
                if (validTimes.Any())
                {
                    ulong fMin = validTimes.Min();
                    ulong fMax = validTimes.Max();
                    if (fMin < minTs) minTs = fMin;
                    if (fMax > maxTs) maxTs = fMax;
                    hasData = true;
                }
            }

            // 2. RealTargetData (표적 로그) 확인
            if (data.RealTargetData != null && data.RealTargetData.Any())
            {
                var validTimes = data.RealTargetData.Where(t => t.Timestamp > 0).Select(t => (ulong)t.Timestamp);
                if (validTimes.Any())
                {
                    ulong tMin = validTimes.Min();
                    ulong tMax = validTimes.Max();
                    if (tMin < minTs) minTs = tMin;
                    if (tMax > maxTs) maxTs = tMax;
                    hasData = true;
                }
            }

            // 3. MissionDetail (일시정지 시간 등) 확인
            if (data.MissionDetail != null)
            {
                foreach (var mission in data.MissionDetail)
                {
                    if (mission.MissionPauseTimeStamp != null)
                    {
                        // UAV 4, 5, 6의 일시정지 시간 범위 확인
                        var allPauseRanges = new List<PauseTimeRange>();
                        if (mission.MissionPauseTimeStamp.UAV4 != null) allPauseRanges.AddRange(mission.MissionPauseTimeStamp.UAV4);
                        if (mission.MissionPauseTimeStamp.UAV5 != null) allPauseRanges.AddRange(mission.MissionPauseTimeStamp.UAV5);
                        if (mission.MissionPauseTimeStamp.UAV6 != null) allPauseRanges.AddRange(mission.MissionPauseTimeStamp.UAV6);

                        foreach (var range in allPauseRanges)
                        {
                            if (range.Start > 0)
                            {
                                if (range.Start < minTs) minTs = range.Start;
                                hasData = true;
                            }
                            if (range.End > 0)
                            {
                                if (range.End > maxTs) maxTs = range.End;
                                hasData = true;
                            }
                        }
                    }
                }
            }

            // 데이터가 하나도 없으면 0 반환
            if (!hasData) return (0, 0);

            return (minTs, maxTs);
        }

        /// <summary>
        /// [신규] FlightData에서 UAV 4, 5, 6의 정보를 추출하여 AnalysisResult에 채우기
        /// </summary>
        private void PopulateUavInfo(AnalysisResult result, List<FlightData> flightDataList)
        {
            if (result == null || flightDataList == null) return;

            // 1. UAV 4, 5, 6 데이터 필터링, 타임스탬프 순 정렬, ID별 그룹화
            var uavDataGroups = flightDataList
                .Where(fd => fd.AircraftID >= 1 && fd.AircraftID <= 6)
                .OrderBy(fd => fd.Timestamp)
                .GroupBy(fd => fd.AircraftID);

            foreach (var group in uavDataGroups)
            {
                // 2. UAV별 EvaluationUAVInfo 생성 (제안된 새 클래스 사용)
                var uavInfo = new EvaluationUAVInfo
                {
                    UAVID = (int)group.Key
                };

                // 3. 타임스탬프별 UavSnapshot 생성
                foreach (var flightEntry in group)
                {
                    var snapshot = new UavSnapshot
                    {
                        Timestamp = flightEntry.Timestamp
                    };

                    // 4. 위치 정보 (항적)
                    // (FlightDataLogList가 null이 아니고, 항목이 하나 이상 있으면)
                    if (flightEntry.FlightDataLog!= null)
                    {
                        // 첫 번째 위치 데이터를 이 스냅샷의 Position으로 사용
                        // (JSON 파싱 시 사용한 'Coordinate' 클래스)
                        snapshot.Position.Latitude = flightEntry.FlightDataLog.Latitude;
                        snapshot.Position.Longitude = flightEntry.FlightDataLog.Longitude;
                        snapshot.Position.Altitude = flightEntry.FlightDataLog.Altitude;
                    }

                    // 5. 카메라 촬영 영역 (Footprint)
                    // (CameraDataLogList가 null이 아니고, 항목이 하나 이상 있으면)
                    if (flightEntry.CameraDataLog != null)
                    {
                        // 첫 번째 카메라 데이터를 이 스냅샷의 Footprint로 사용
                        // (JSON 파싱 시 사용한 'CameraDataLog' 클래스)
                        snapshot.Footprint = flightEntry.CameraDataLog;
                    }

                    uavInfo.Snapshots.Add(snapshot);
                }

                result.UavInfos.Add(uavInfo);
            }
        }


        private void UpdateCheckedScenarios(ScoreScenarioItem changedItem)
        {
            if (changedItem.IsChecked)
            {
                // 이미 목록에 있는지 확인
                if (!CheckedScenariosForDisplay.Any(s => s.ScenarioNumber == changedItem.ScenarioNumber))
                {
                    // 분석 결과가 있는지 확인
                    uint coverageScore = changedItem.CoverageAnalysisResult?.Score ?? 0;
                    // uint commScore = changedItem.CommunicationAnalysisResult?.Score ?? 0;
                    // uint safetyScore = changedItem.SafetyAnalysisResult?.Score ?? 0;

                    CheckedScenariosForDisplay.Add(new ScoreScenarioSummary
                    {
                        ScenarioNumber = changedItem.ScenarioNumber,
                        ScenarioName = changedItem.ScenarioName,

                        // [!!!] 6대 점수 모두 전달 [!!!]
                        CommScore = changedItem.CommScore,
                        SafetyScore = changedItem.SafetyScore,
                        MissionDistScore = changedItem.MissionDistScore,
                        MissionSuccessScore = changedItem.MissionSuccessScore,
                        SpatialResScore = changedItem.SpatialResScore,
                        CoverageScore = changedItem.CoverageScore,
                        StartTime = changedItem.StartTime,
                        EndTime = changedItem.EndTime,

                        OriginalItem = changedItem // 원본 참조 연결
                    });
                }
            }
            else
            {
                // 체크 해제 시 목록에서 제거
                var itemToRemove = CheckedScenariosForDisplay.FirstOrDefault(s => s.ScenarioNumber == changedItem.ScenarioNumber);
                if (itemToRemove != null)
                {
                    CheckedScenariosForDisplay.Remove(itemToRemove);

                    // [!!!] 3. 만약 체크 해제된 항목이 현재 선택된 항목이었다면,
                    // [!!!]    SelectedScenarioSummary를 null로 초기화합니다.
                    if (SelectedScenarioSummary == itemToRemove)
                    {
                        SelectedScenarioSummary = null;
                    }
                }
            }
        }

        private void ScenarioItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScoreScenarioItem.IsChecked) && !_isBulkUpdating)
            {
                var item = sender as ScoreScenarioItem;
                if (item == null) return;

                UpdateCheckedScenarios(item);
            }
        }



        private void Button_PreviewMouseDown_1(object sender, MouseButtonEventArgs e)
        {
            _isBulkUpdating = true;
            foreach (var item in AllScenarioItems) item.IsChecked = true;
            _isBulkUpdating = false;

            // 수동으로 한 번에 업데이트
            foreach (var item in AllScenarioItems) UpdateCheckedScenarios(item);
        }

        private void Button_PreviewMouseDown_2(object sender, MouseButtonEventArgs e)
        {
            _isBulkUpdating = true;
            foreach (var item in AllScenarioItems) item.IsChecked = false;
            _isBulkUpdating = false;

            // 수동으로 한 번에 업데이트
            CheckedScenariosForDisplay.Clear();
            SelectedScenarioSummary = null;
        }


      

        private void Button_PreviewMouseDown_3(object sender, MouseButtonEventArgs e)
        {
            // 1. 텍스트박스의 입력값을 ulong 타입으로 변환 시도
            if (ulong.TryParse(lnputConvertedTime, out ulong timestamp))
            {
                // 2. 변환에 성공하면 ConvertedTime 속성에 값을 할당
                //    -> 이 값이 바뀌면 OnPropertyChanged가 호출되어 화면의 TextBlock이 자동으로 갱신됨
                ConvertedTime = timestamp;
            }
            else
            {
                // 3. 변환에 실패하면 (숫자가 아니거나 너무 큰 경우) 에러 메시지 표시
                MessageBox.Show("올바른 Timestamp 숫자 값을 입력하세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //private void Chart_QueryChartCursor(object sender, QueryCursorEventArgs e)
        //{
        //    var chart = (ChartControl)sender;

        //    // [수정] e.Position 대신 Mouse.GetPosition을 사용하여 현재 마우스 위치를 가져옵니다.
        //    var position = Mouse.GetPosition(chart);
        //    var hitInfo = chart.CalcHitInfo(position); // 가져온 position 변수를 사용합니다.

        //    // 마우스가 데이터 막대(Series) 위에 있지 않은 경우,
        //    if (hitInfo == null || !hitInfo.InSeries)
        //    {
        //        // 차트의 기본 크로스헤어 동작을 중지하고,
        //        e.Handled = true;
        //        // 일반 마우스 커서(화살표)를 표시
        //        e.Cursor = Cursors.Arrow;
        //    }
        //    // 마우스가 데이터 막대 위에 있는 경우에는 아무것도 하지 않아
        //    // 차트의 기본 크로스헤어 동작이 실행됩니다.
        //}

        
        // 리스트뷰에서 선택된 항목들(selectedItems)을 인자로 받기
        private async void Button_Analyze_Click(object sender, RoutedEventArgs e)
        {
            // ListView의 SelectedItems를 가져옴
            var selectedItems = ScenarioListView.SelectedItems.Cast<ScoreScenarioItem>().ToList();

            if (selectedItems.Count == 0)
            {
                MessageBox.Show("분석할 시나리오를 선택해주세요.", "알림");
                return;
            }

            await RunAnalysisAsync(selectedItems);
        }


        private async Task RunAnalysisAsync(List<ScoreScenarioItem> targets)
        {
            // 이미 분석된 것은 제외 (건너뛰기)
            var itemsToAnalyze = targets.Where(x => !x.IsAnalyzed).ToList();

            if (itemsToAnalyze.Count == 0)
            {
                // 선택한 것들이 이미 다 분석된 상태라면 종료
                return;
            }

            SimpleProgressWindow progressWindow = null;

            try
            {
                progressWindow = new SimpleProgressWindow();
                progressWindow.UpdateProgress(0, "Initializing Analysis...");
                progressWindow.Show();

                string parentDirectoryPath = Path.GetDirectoryName(itemsToAnalyze[0].FullPath); // 경로 추출
                int totalScenarios = itemsToAnalyze.Count;

                for (int i = 0; i < totalScenarios; i++)
                {
                    var item = itemsToAnalyze[i];

                    // 1. 현재 시나리오가 차지하는 기본 퍼센트와 비중 계산
                    int basePercentage = (i * 100) / totalScenarios;
                    int scenarioWeight = 100 / totalScenarios;

                    // 2. 한 시나리오 내의 세부 단계 수 (로드 1개 + 분석기 7개 = 총 8단계)
                    int totalStepsPerScenario = 8;
                    int completedSteps = 0;

                    // 3. 세부 진행률을 UI에 업데이트하는 로컬 함수
                    void ReportSubProgress(string taskName)
                    {
                        // 멀티 스레드 환경에서 안전하게 카운트 증가
                        int step = System.Threading.Interlocked.Increment(ref completedSteps);
                        int currentPct = basePercentage + (step * scenarioWeight / totalStepsPerScenario);

                        // [핵심] DispatcherPriority.Background를 주면 UI 렌더링을 억지로라도 수행하고 넘어갑니다.
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (progressWindow != null)
                            {
                                // 문자열 앞부분에 퍼센트 [%]를 직접 명시해 줍니다!
                                progressWindow.UpdateProgress(currentPct, $"[{currentPct}%] {item.ScenarioName} - {taskName}...");
                            }
                        }, System.Windows.Threading.DispatcherPriority.Background);
                    }

                    // ★ 4. Task 완료 시 진행률을 올리고, 알맹이 데이터를 그대로 반환하는 헬퍼 함수
                    async Task<T> AttachProgress<T>(Task<T> task, string taskName)
                    {
                        var res = await task;
                        ReportSubProgress(taskName);
                        return res;
                    }

                    try
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                            progressWindow.UpdateProgress(basePercentage, $"[{i + 1}/{totalScenarios}] {item.ScenarioName} - Data Loading...")
                        );

                        // ★ Step 1. 시나리오 데이터 로드
                        ScenarioData scenarioData = await Utils.LoadScenarioDataByPath(item.FullPath);
                        if (scenarioData == null || scenarioData.FlightData == null) continue;

                        // 디버그: 로드된 파일 경로 및 LOS 데이터 확인
                        int losCount = scenarioData.FlightData.Count(f => f.LosUav4.HasValue);
                        int helicopterCount = scenarioData.FlightData.Count(f => f.AircraftID <= 3);
                        System.Diagnostics.Debug.WriteLine($"[로드 확인] {item.FullPath}");
                        System.Diagnostics.Debug.WriteLine($"[로드 확인] 전체={scenarioData.FlightData.Count}, 헬기={helicopterCount}, LOS보유={losCount}");

                        ReportSubProgress("Data Loaded");

                        // ★ Step 2. 시간 범위 계산
                        var (startTs, endTs) = GetScenarioTimeRange(scenarioData);
                        item.StartTime = startTs;
                        item.EndTime = endTs;

                        // ★ Step 3. 병렬 분석 작업 정의 
                        // 동기 함수들은 Task.Run으로 감싸서 백그라운드로 보내고 AttachProgress를 붙입니다.
                        var targetTask = AttachProgress(Task.Run(() => TargetAchievementCalculator.Analyze(scenarioData.RealTargetData)), "Target Analysis");
                        var coverageTask = AttachProgress(Task.Run(() => CoverageCalculator.getCoverage(scenarioData.FlightData, scenarioData.MissionDetail)), "Coverage Analysis");
                        var commTask = AttachProgress(Task.Run(() => CommunicationCalculator.getCommunicationData(scenarioData.FlightData)), "Communication Analysis");
                        var safetyTask = AttachProgress(Task.Run(() => SafetyLevelCalculator.getSafetyData(parentDirectoryPath, scenarioData)), "Safety Analysis");
                        var missionDistTask = AttachProgress(Task.Run(() => MissionDistributionCalculator.getMissionDistributionData(scenarioData.FlightData, scenarioData.MissionDetail)), "Distribution Analysis");

                        // SRCalculator.getSRData(List, List) 는 동기 함수이므로 Task.Run 적용
                        var spatialResDataTask = AttachProgress(Task.Run(() => SRCalculator.getSRData(scenarioData.FlightData, scenarioData.MissionDetail)), "SR Data");

                        // [핵심] SRCalculator.getSRScore(ScenarioData)는 이미 비동기(Task)이므로 Task.Run 없이 바로 넘깁니다.
                        var spatialResTask = AttachProgress(Task.Run(async () => await SRCalculator.getSRScore(scenarioData)), "SR Score");

                        // 7개의 분석이 모두 끝날 때까지 병렬 대기
                        await Task.WhenAll(targetTask, coverageTask, commTask, safetyTask, missionDistTask, spatialResTask, spatialResDataTask);

                        // ★ Step 4. 결과 대입
                        item.TargetAnalysisResult = targetTask.Result;
                        item.MissionSuccessScore = (uint)Math.Round(item.TargetAnalysisResult?.AchievementRate ?? 0);

                        item.CoverageAnalysisResult = coverageTask.Result ?? new AnalysisResult();
                        item.CoverageScore = item.CoverageAnalysisResult.Score;
                        PopulateUavInfo(item.CoverageAnalysisResult, scenarioData.FlightData);

                        item.CommunicationAnalysisResult = commTask.Result;
                        item.CommScore = item.CommunicationAnalysisResult?.Score ?? 0;

                        item.SafetyAnalysisResult = safetyTask.Result ?? new SafetyResult { Score = 0 };
                        item.SafetyScore = item.SafetyAnalysisResult.Score;

                        item.MissionDistResult = missionDistTask.Result;
                        item.MissionDistScore = item.MissionDistResult?.Score ?? 0;

                        item.SpatialResResult = spatialResDataTask.Result;
                        item.SpatialResScore = spatialResTask.Result ?? 0u;

                        // 상태 변경 -> UI 갱신 (파란색/초록색 변경 등)
                        item.IsAnalyzed = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error analyzing {item.ScenarioName}: {ex.Message}");
                    }
                }

                // 모든 분석이 끝나면 100% 채워줌
                progressWindow.UpdateProgress(100, "Analysis Completed!");
                await Task.Delay(300); // 100%를 사용자가 잠깐 볼 수 있게 대기
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Analysis Process Error: {ex.Message}");
            }
            finally
            {
                progressWindow?.Close();
            }
        }
    }
}