// 필수 using 문들
using DevExpress.Xpf.Charts;
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
    public partial class View_Performance_Analyze : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion

        public ObservableCollection<TimelineSegment> TimelineSegments { get; set; } = new();
        private static readonly DateTime Epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private bool _isBulkUpdating = false;

        public ObservableCollection<ScenarioItem> AllScenarioItems { get; set; } = new();
        public ObservableCollection<ScenarioSummary> CheckedScenariosForDisplay { get; set; } = new();
        public ObservableCollection<ReplanSummary> SelectedScenarioReplanData { get; set; } = new();

        public ObservableCollection<UavSuccessMetrics> SelectedScenarioUavMetrics { get; set; } = new();

        private ScenarioSummary _selectedScenarioSummary;
        public ScenarioSummary SelectedScenarioSummary
        {
            get => _selectedScenarioSummary;
            set { _selectedScenarioSummary = value; OnPropertyChanged(nameof(SelectedScenarioSummary)); }
        }

        public ObservableCollection<TimelineSegment> TimelineSegmentsUAV1 { get; set; } = new();
        public ObservableCollection<TimelineSegment> TimelineSegmentsUAV2 { get; set; } = new();
        public ObservableCollection<TimelineSegment> TimelineSegmentsUAV3 { get; set; } = new();

        // ✅ [추가] 응답 마커용 컬렉션 (UAV 별로)
        public ObservableCollection<TimelineSegment> ResponseMarkersUAV1 { get; set; } = new();
        public ObservableCollection<TimelineSegment> ResponseMarkersUAV2 { get; set; } = new();
        public ObservableCollection<TimelineSegment> ResponseMarkersUAV3 { get; set; } = new();


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

        private string _SelectedScenarioName = "";
        public string SelectedScenarioName
        {
            get => _SelectedScenarioName;
            set { _SelectedScenarioName = value; OnPropertyChanged(nameof(SelectedScenarioName)); }
        }

        public View_Performance_Analyze()
        {
            InitializeComponent();
            this.DataContext = this;

            LogStartTime = 60; // 2000-01-01 00:00:00.000 (UTC)
            LogEndTime = 3600 * 1000; // 2000-01-01 00:01:00.000 (UTC)

            GenerateCustomLegend();
        }
        private uint _overallSuccessRate = 0;
        public uint OverallSuccessRate
        {
            get => _overallSuccessRate;
            set { _overallSuccessRate = value; OnPropertyChanged(nameof(OverallSuccessRate)); }
        }

        private uint _selectedScenarioSuccessRate = 0;
        public uint SelectedScenarioSuccessRate
        {
            get => _selectedScenarioSuccessRate;
            set { _selectedScenarioSuccessRate = value; OnPropertyChanged(nameof(SelectedScenarioSuccessRate)); }
        }
        private void UpdateSuccessRates()
        {
            // 전체 성공률 계산 (체크된 시나리오들의 평균)
            var checkedItems = AllScenarioItems.Where(item => item.IsChecked).ToList();
            if (checkedItems.Any())
            {
                // 각 시나리오의 SuccessRateData에서 "Score" 값을 가져옴 (없으면 0)
                double totalScore = checkedItems.Sum(item => item.SuccessRateData?["Score"]?.Value<uint>() ?? 0);
                OverallSuccessRate = (uint)Math.Round(totalScore / checkedItems.Count);
            }
            else
            {
                OverallSuccessRate = 0; // 체크된 항목 없으면 0
            }

            // 선택된 시나리오 성공률 업데이트
            if (ScenarioListBox.SelectedItem != null)
            {
                string selectedName = ScenarioListBox.SelectedItem.ToString();
                SelectedScenarioName = selectedName;
                var selectedItem = AllScenarioItems.FirstOrDefault(s => s.ScenarioName == selectedName);
                if (selectedItem != null)
                {
                    SelectedScenarioSuccessRate = selectedItem.SuccessRateData?["Score"]?.Value<uint>() ?? 0;
                }
                else
                {
                    SelectedScenarioSuccessRate = 0; // 선택된 항목 정보 없으면 0
                }
            }
            else
            {
                SelectedScenarioSuccessRate = 0; // 선택된 항목 없으면 0
            }
        }

        private static readonly Dictionary<int, string> FlightModeMap = new()
        {
            { 1, "자동이륙" },
            { 2, "자동착륙" },
            { 5, "RTB" },
            { 6, "편대비행" },
            { 7, "경로이동비행" },
            { 8, "점항법비행" },
            { 9, "표적추적비행" }
        };

        private static readonly Dictionary<int, string> PayloadModeMap = new()
        {
            { 1, "좌표지향모드" },
            { 2, "구간탐색/구역감시모드" },
            { 3, "자동추적모드" },
            { 4, "기체고정모드" },
            { 5, "자동주사모드" }
        };

        //private static readonly Dictionary<string, Brush> StateBrushMap = new();
        //private static readonly Dictionary<string, string> StateColorMap = new()
        //{
        //    // 비행 모드 색상
        //    { "자동이륙", "LimeGreen" },
        //    { "자동착륙", "LightSkyBlue" },
        //    { "RTB", "OrangeRed" },
        //    { "편대비행", "Gold" },
        //    { "경로이동비행", "CornflowerBlue" },
        //    { "점항법비행", "Orange" },
        //    { "표적추적비행", "Crimson" },

        //    // 임무장비 모드 색상
        //    { "좌표지향모드", "Orchid" },
        //    { "구간탐색/구역감시모드", "Turquoise" },
        //    { "자동추적모드", "MediumPurple" },
        //    { "기체고정모드", "DeepSkyBlue" },
        //    { "자동주사모드", "SlateBlue" },

        //    // 기타
        //    { "미운용", "Gray" },
        //    { "시간 초과", "Black" },
        //    { "응답 없음", "Black" }
        //};

        private static readonly Dictionary<string, string> StateColorMap = new()
{
    // ★ 임무별 색상 (성공 시)
    { "표적추적", "Crimson" },
    { "영역수색", "DodgerBlue" },
    { "영역경계", "Orange" },
    { "좌표점정찰", "MediumPurple" },
    { "통로정찰", "Teal" },
    { "이동", "Gray" },

    // ★ 실패 (공통)
    { "임무 실패", "Black" }
};

        private void GenerateCustomLegend()
        {
            // 차트의 자동 생성된 범례 항목은 숨깁니다.
            var firstSeries = diagram.Series.FirstOrDefault();
            if (firstSeries != null)
            {
                firstSeries.ShowInLegend = false;
            }

            // 기존 사용자 정의 범례 항목을 비웁니다.
            TimelineChart.Legend.CustomItems.Clear();
            var converter = new BrushConverter();

            // StateColorMap의 모든 항목을 순회하며 범례 아이템을 만듭니다.
            foreach (var stateColorPair in StateColorMap)
            {
                var legendItem = new CustomLegendItem
                {
                    Text = stateColorPair.Key, // 범례 텍스트 (예: "자동이륙")
                                               // 색상 이름을 실제 Brush 객체로 변환하여 마커 색상으로 지정
                    MarkerBrush = (Brush)converter.ConvertFromString(stateColorPair.Value)
                };
                TimelineChart.Legend.CustomItems.Add(legendItem);
            }
        }


        private async void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // 1. 현재 로그인한 사용자의 바탕화면 경로 가져오기
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // 2. 바탕화면 아래의 '분석데이터' 폴더 경로 조합
            string targetPath = Path.Combine(desktopPath, "분석데이터", "raw");

            if (!Directory.Exists(targetPath))
            {
                targetPath = desktopPath;
            }

            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                InitialDirectory = targetPath
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                await LoadScenariosAsync(dialog.FileName);
            }
        }

        private async Task LoadScenariosAsync(string parentDirectoryPath)
        {
            AllScenarioItems.Clear();
            CheckedScenariosForDisplay.Clear();
            SelectedScenarioReplanData.Clear();
            TimelineSegmentsUAV1.Clear();
            TimelineSegmentsUAV2.Clear();
            TimelineSegmentsUAV3.Clear();

            ulong globalMinTs = ulong.MaxValue;
            ulong globalMaxTs = ulong.MinValue;
            bool hasAnyData = false;

            SimpleProgressWindow progressWindow = null;

            try
            {
                // 1. 진행률 창 생성 및 표시
                progressWindow = new SimpleProgressWindow();
                progressWindow.UpdateProgress(0, "Searching scenarios...");
                progressWindow.Show();

                var subDirectories = Directory.GetDirectories(parentDirectoryPath);
                int totalScenarios = subDirectories.Length;
                int processedCount = 0;

                if (totalScenarios == 0)
                {
                    progressWindow.Close();
                    MessageBox.Show("해당 경로에 시나리오 폴더가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                foreach (var dirPath in subDirectories)
                {
                    // (진행률 업데이트)
                    processedCount++;
                    int percentage = (int)((double)processedCount / totalScenarios * 100);
                    string currentDirName = Path.GetFileName(dirPath);

                    // UI 스레드에서 진행률 업데이트
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        progressWindow.UpdateProgress(percentage, $"Analyzing {currentDirName} ({processedCount}/{totalScenarios})...");
                    });

                    //데이터 로드
                    //ScenarioData? scenarioData = await Utils.LoadScenarioData(dirPath, 0);

                    // 1. FlightData (0401) 로드
                    var flightDataList = await Task.Run(() => MissionSuccessCalculator.LoadFlightData(dirPath));

                    // 2. IndividualMission (0302) 로드 (★ 사용자분 코드가 들어간 부분)
                    var individualMissions = await Task.Run(() => MissionSuccessCalculator.LoadIndividualMissions(dirPath));

                    // 3. TargetData (RealTarget) 로드
                    var targetDataList = await Task.Run(() => MissionSuccessCalculator.LoadRealTargetData(dirPath));

                    // 4. SRTM 경로 (기본 경로 설정)
                    string srtmFilePath = Path.Combine(AppContext.BaseDirectory, "srtm_62_05.tif");

                    var scenarioItem = new ScenarioItem
                    {
                        ScenarioName = currentDirName,
                        FullPath = dirPath,
                        IsChecked = false
                    };

                    // 5. 계산기 호출 (로드한 데이터 전달)
                    scenarioItem.Complexity = await Task.Run(() => CalculateComplexity(dirPath));
                    scenarioItem.AverageReplanTime = await Task.Run(() => CalculateReplanTime(dirPath));

                    scenarioItem.AnalysisResult = await MissionSuccessCalculator.CalculateIndividualMissionSuccessAsync(
                    dirPath,
                    flightDataList,
                    individualMissions,
                    targetDataList,
                    srtmFilePath
                     );


                    // 3. 타임스탬프 스캔 (차트 범위 설정을 위함)
                    if (flightDataList.Any())
                    {
                        var min = flightDataList.Min(f => f.Timestamp);
                        var max = flightDataList.Max(f => f.Timestamp);
                        if (min < globalMinTs) globalMinTs = min;
                        if (max > globalMaxTs) globalMaxTs = max;
                        hasAnyData = true;
                    }

                    // 이벤트 핸들러 연결 및 추가
                    scenarioItem.PropertyChanged += ScenarioItem_PropertyChanged;
                    AllScenarioItems.Add(scenarioItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"시나리오 로드 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 작업 완료 후 반드시 닫기
                progressWindow?.Close();
            }

            // [수정] 찾은 최소/최대 타임스탬프를 UI에 반영
            if (hasAnyData)
            {
                LogStartTime = globalMinTs;
                LogEndTime = globalMaxTs;
            }
            else
            {
                LogStartTime = 0;
                LogEndTime = 0;
            }

            // UI 갱신 알림
            OnPropertyChanged(nameof(LogStartTimeFormatted));
            OnPropertyChanged(nameof(LogEndTimeFormatted));

            UpdateSuccessRates();
        }

   

        private static JArray LoadMergedJsonData(string scenarioDir, string messageId, string nodeName = "SBC3")
        {
            JArray mergedArray = new JArray();
            string targetDir = MissionSuccessCalculator.FindTargetDirectory(scenarioDir, messageId);

            if (string.IsNullOrEmpty(targetDir))
            {
                return mergedArray; // 폴더가 없으면 빈 배열 반환
            }

            // 파일명 순으로 정렬하여 읽기
            var files = Directory.GetFiles(targetDir, "*.json").OrderBy(f => f).ToArray();

            foreach (var file in files)
            {
                try
                {
                    string content = File.ReadAllText(file);
                    var jsonArray = JArray.Parse(content);
                    foreach (var item in jsonArray)
                    {
                        mergedArray.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error reading {file}: {ex.Message}");
                }
            }
            return mergedArray;

            //if (!Directory.Exists(targetDir))
            //{
            //    // 혹시 SBC3가 없는 구조일 수도 있으니 예비로 바로 아래도 검색 (필요시)
            //    // targetDir = Path.Combine(scenarioDir, messageId);
            //    // if (!Directory.Exists(targetDir)) 
            //    return mergedArray;
            //}

            //// 파일명 순으로 정렬하여 읽어야 순서 보장 가능 (예: 0401.json, 0401_1.json)
            //var files = Directory.GetFiles(targetDir, "*.json").OrderBy(f => f).ToArray();

            //foreach (var file in files)
            //{
            //    try
            //    {
            //        string content = File.ReadAllText(file);
            //        // 파일 내용이 JArray라고 가정
            //        var jsonArray = JArray.Parse(content);
            //        foreach (var item in jsonArray)
            //        {
            //            mergedArray.Add(item);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Debug.WriteLine($"Error reading {file}: {ex.Message}");
            //    }
            //}
            //return mergedArray;
        }

        private void ScenarioItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isBulkUpdating || e.PropertyName != nameof(ScenarioItem.IsChecked)) return;
            UpdateComparisonLists();

            var item = sender as ScenarioItem;
            if (item == null) return;

            if (item.IsChecked)
            {
                // 우측 상단 그리드에 추가
                if (!CheckedScenariosForDisplay.Any(s => s.ScenarioName == item.ScenarioName))
                {
                    CheckedScenariosForDisplay.Add(new ScenarioSummary
                    {
                        ScenarioName = item.ScenarioName,
                        ComplexityScore = item.Complexity.ComplexityScore,
                        AircraftCount = item.Complexity.AircraftCount,
                        CollaborativeMissions = item.Complexity.CollaborativeMissionCount,
                        IndividualMissions = item.Complexity.IndividualMissionCount,
                    });
                }
                // 중앙 리스트박스에 추가
                if (!ScenarioListBox.Items.Contains(item.ScenarioName))
                {
                    ScenarioListBox.Items.Add(item.ScenarioName);
                }
                // ✅ [추가] 재계획 시간 그리드에 추가
                if (!SelectedScenarioReplanData.Any(r => r.ScenarioName == item.ScenarioName))
                {
                    SelectedScenarioReplanData.Add(new ReplanSummary
                    {
                        ScenarioName = item.ScenarioName,
                        //AverageReplanTime = item.AverageReplanTime >= 0 ? $"{item.AverageReplanTime:F2} 초" : "N/A"
                        // 기존: 초 단위 (F2) -> 변경: 1000을 곱하여 밀리초(ms) 단위로 변환 (F0: 소수점 없음)
                        //AverageReplanTime = item.AverageReplanTime >= 0? $"{item.AverageReplanTime * 1000:F2} ms": "N/A"
                        AverageReplanTime = item.AverageReplanTime >= 0 ? $"{item.AverageReplanTime:F2} ms" : "N/A"
                    });
                }
            }
            else
            {
                // 우측 상단 그리드에서 제거
                var summaryToRemove = CheckedScenariosForDisplay.FirstOrDefault(s => s.ScenarioName == item.ScenarioName);
                if (summaryToRemove != null)
                {
                    CheckedScenariosForDisplay.Remove(summaryToRemove);
                }
                // 중앙 리스트박스에서 제거
                ScenarioListBox.Items.Remove(item.ScenarioName);

                // ✅ [추가] 재계획 시간 그리드에서 제거
                var replanToRemove = SelectedScenarioReplanData.FirstOrDefault(r => r.ScenarioName == item.ScenarioName);
                if (replanToRemove != null)
                {
                    SelectedScenarioReplanData.Remove(replanToRemove);
                }
            }
        }

        private void ScenarioListBox_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ScenarioListBox.SelectedItem == null) return;

            string selectedName = ScenarioListBox.SelectedItem.ToString();
            var selectedScenarioItem = AllScenarioItems.FirstOrDefault(s => s.ScenarioName == selectedName);

            if (selectedScenarioItem != null)
            {
                UpdateDetailsForScenario(selectedScenarioItem);
                UpdateSuccessRates();
            }
            else
            {
                // 선택된 항목이 없으면 선택된 시나리오 성공률 0으로 설정 
                SelectedScenarioSuccessRate = 0;
            }
        }

        private void UpdateDetailsForScenario(ScenarioItem selectedItem)
        {
            // 1. 컬렉션 초기화
            TimelineSegmentsUAV1.Clear();
            TimelineSegmentsUAV2.Clear();
            TimelineSegmentsUAV3.Clear();
            SelectedScenarioUavMetrics.Clear();

            // 응답 마커는 이제 안 쓰므로 비워둠 (필요하다면 삭제)
            ResponseMarkersUAV1.Clear();
            ResponseMarkersUAV2.Clear();
            ResponseMarkersUAV3.Clear();

            if (selectedItem?.AnalysisResult == null) return;

            // 2. 타임라인 세그먼트 뿌리기 
            var allSegments = selectedItem.AnalysisResult.TimelineSegments;

            // 2. 타임라인 세그먼트 분배 (UAV ID 기준)
            foreach (var segment in allSegments)
            {
                // ★ [수정] UavID 속성을 사용하여 정확히 분배
                switch (segment.UavID)
                {
                    case 4: TimelineSegmentsUAV1.Add(segment); break; // UAV 4 -> 첫 번째 패널
                    case 5: TimelineSegmentsUAV2.Add(segment); break; // UAV 5 -> 두 번째 패널
                    case 6: TimelineSegmentsUAV3.Add(segment); break; // UAV 6 -> 세 번째 패널
                }
            }

            // 3. 우측 하단 메트릭 업데이트 (기존 로직 유지)
            var uavMetricsData = selectedItem.AnalysisResult.SummaryJson?["UAVMetrics"] as JObject;
            if (uavMetricsData != null)
            {
                foreach (var prop in uavMetricsData.Properties().OrderBy(p => p.Name))
                {
                    SelectedScenarioUavMetrics.Add(new UavSuccessMetrics
                    {
                        UavName = prop.Name.Replace("UAV_", "무인기 "),
                        TotalRequests = prop.Value["TotalRequests"]?.Value<uint>() ?? 0,
                        MatchedCount = prop.Value["MatchedCount"]?.Value<uint>() ?? 0
                    });
                }
            }
        }

        private void ProcessTaskTimeline(
            List<(int DisplayIndex, string TaskName, DateTime Start, DateTime End, string Color, string State)> events,
            ObservableCollection<TimelineSegment> targetCollection,
            ObservableCollection<TimelineSegment> markerCollection, // 추가된 인자
            DateTime globalMaxTime)
        {
            if (!events.Any()) return;

            TimelineSegment currentSegment = null;

            // 마커의 두께(시간 길이) 설정 (예: 200ms -> 앞뒤로 100ms씩)
            // 차트 줌 레벨에 따라 다르게 보이겠지만, 고정된 시간 크기를 줍니다.
            TimeSpan markerHalfWidth = TimeSpan.FromMilliseconds(400);

            foreach (var ev in events)
            {
                // 이전 세그먼트가 있다면, 그 끝나는 지점이 곧 "응답 시점"입니다.
                if (currentSegment != null)
                {
                    currentSegment.EndTime = ev.Start;

                    // ✅ [핵심] 응답 지점에 "검은색 띠" 마커 추가
                    // 응답 시점(ev.Start)을 중심으로 앞뒤로 살짝 벌려서 바를 만듭니다.
                    markerCollection.Add(new TimelineSegment
                    {
                        TaskName = currentSegment.TaskName, // 같은 행에 그려지도록
                        StartTime = ev.Start.Subtract(markerHalfWidth),
                        EndTime = ev.Start.Add(markerHalfWidth),
                        //Color = "Black", // 또는 아주 진한 회색 (#333333)
                        Color = "Red", 
                        State = "응답 시점" // 툴팁용 텍스트
                    });
                }

                currentSegment = new TimelineSegment
                {
                    TaskName = ev.TaskName,
                    StartTime = ev.Start,
                    EndTime = ev.End,
                    Color = ev.Color,
                    State = ev.State
                };
                targetCollection.Add(currentSegment);
            }

            // 마지막 세그먼트 처리
            if (currentSegment != null && currentSegment.EndTime < globalMaxTime)
            {
                currentSegment.EndTime = globalMaxTime;
                // 마지막은 응답이 없으므로 마커를 찍지 않거나, 필요하다면 시나리오 끝 시간에 찍음
            }
        }

        #region Calculation Logic (Adapted from user's code)

        private static float CalculateReplanTime(string scenarioDir)
        {
            string missionPlanDir = MissionSuccessCalculator.FindTargetDirectory(scenarioDir, "MissionPlan");

            if (string.IsNullOrEmpty(missionPlanDir)) return -1.0f;

            float planTimeSum = 0.0f;
            int replanCount = 0;

            foreach (var filepath in Directory.GetFiles(missionPlanDir, "*.json"))
            {
                try
                {
                    JObject missionPlanData = JObject.Parse(File.ReadAllText(filepath));
                    var planningTimeToken = missionPlanData["planningTime"];
                    if (planningTimeToken != null && (planningTimeToken.Type == JTokenType.Float || planningTimeToken.Type == JTokenType.Integer))
                    {
                        planTimeSum += planningTimeToken.Value<float>();
                    }
                    replanCount++;
                }
                catch { }
            }
            return (replanCount > 0) ? (planTimeSum / replanCount) : -1.0f;
        }

        private static ComplexityResult CalculateComplexity(string scenarioDir)
        {
            // 폴더 찾기
            string inputMissionPlanDir = MissionSuccessCalculator.FindTargetDirectory(scenarioDir, "InputMissionPlan");
            string individualMissionPlanDir = MissionSuccessCalculator.FindTargetDirectory(scenarioDir, "IndividualMissionPlan");

            var result = new ComplexityResult();

            try
            {
                // 0401 메시지 폴더 찾아서 병합 로드
                JArray aircraftData = LoadMergedJsonData(scenarioDir, "0401");

                //if (aircraftData.Count > 0 && aircraftData.Last?["agentStateList"] is JArray latestAgentStateList)
                //{
                //    int uavCount = latestAgentStateList.Count(agent => agent["isUnmanned"]?.Value<bool>() == true);
                //    result.AircraftCount = (uint)uavCount;
                //}

                // [수정] 마지막 메시지의 agentStateList에서 '전체 기체 수' 카운트
                if (aircraftData.Count > 0 && aircraftData.Last?["agentStateList"] is JArray latestAgentStateList)
                {
                    // 1. 무인기 수 (UAV)
                    int uavCount = latestAgentStateList.Count(agent => agent["isUnmanned"]?.Value<bool>() == true);

                    // 2. 유인기 수 (Manned Aircraft / Helicopter) - isUnmanned가 false이거나 없는 경우
                    int mannedCount = latestAgentStateList.Count(agent => agent["isUnmanned"]?.Value<bool>() != true);

                    // 3. 전체 기체 수 합산
                    result.AircraftCount = (uint)(uavCount + mannedCount);

                    // (디버깅용 로그: 필요시 주석 해제)
                    // Debug.WriteLine($"Scenario: {Path.GetFileName(scenarioDir)}, Total: {result.AircraftCount} (UAV: {uavCount}, Heli: {mannedCount})");
                }

                result.IndividualMissionCount = (uint)CountMissionFiles(individualMissionPlanDir, "individualMissionList");
                result.CollaborativeMissionCount = (uint)CountMissionFiles(inputMissionPlanDir, "inputMissionList");
                result.ComplexityScore = result.AircraftCount * (result.CollaborativeMissionCount + result.IndividualMissionCount);
            }
            catch { }

            return result;
        }

        private static int CountMissionFiles(string dir, string key)
        {
            int total = 0;
            if (!Directory.Exists(dir)) return 0;

            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    var json = JObject.Parse(File.ReadAllText(file));
                    if (json[key] is JArray list)
                    {
                        total += list.Count;
                    }
                }
                catch { /* ignore parsing errors */ }
            }
            return total;
        }

        private static JObject CalculateMissionSuccess(string scenarioDir)
        {
            var idToDisplayMap = new Dictionary<int, int>
    {
        { 4, 1 }, { 5, 2 }, { 6, 3 }
    };

            try
            {
                JArray baselineActions = LoadMergedJsonData(scenarioDir, "0601");
                JArray controlCommands = LoadMergedJsonData(scenarioDir, "0602");

                if (baselineActions.Count == 0 || controlCommands.Count == 0)
                {
                    return new JObject { ["Score"] = 0, ["ActionTimestamp"] = new JObject(), ["UAVMetrics"] = new JObject() };
                }

                var groupedControlCommands = controlCommands
                    .GroupBy(c => c.Value<int>("aircraftID"))
                    .ToDictionary(g => g.Key, g => g.OrderBy(c => c.Value<long>("timestamp")).ToList());

                int totalRequests = 0;
                int matchedCount = 0;

                Dictionary<int, uint> uavTotalRequests = new Dictionary<int, uint>();
                Dictionary<int, uint> uavMatchedCounts = new Dictionary<int, uint>();
                JObject actionTimestampResult = new JObject();

                foreach (var baseline in baselineActions)
                {
                    // [안전] JSON 필드 null 체크
                    int? baseAircraftIdNullable = baseline.Value<int?>("aircraftID");
                    if (baseAircraftIdNullable == null) continue;
                    int baseAircraftId = baseAircraftIdNullable.Value;

                    if (!idToDisplayMap.TryGetValue(baseAircraftId, out int displayUavId))
                        continue;

                    totalRequests++;
                    ulong? baseTimestampNullable = baseline.Value<ulong?>("timestamp");
                    if (baseTimestampNullable == null) continue;
                    ulong baseTimestamp = baseTimestampNullable.Value;

                    // [수정 1] 0601.json은 구조가 평평하므로 바로 접근해야 합니다.
                    // 기존: baseline["flightModeCommand"]?["flightMode"]... (잘못됨)
                    int? baseFlightMode = (int?)baseline.SelectToken("flightModeCommand.flightMode") ?? (int?)baseline["flightMode"];
                    int? baseFilmingMode = (int?)baseline.SelectToken("filmingModeCommand.operationMode") ?? (int?)baseline["filmingMode"];

                    if (baseFlightMode == null && baseFilmingMode == null) continue;

                    // 만약 0601 데이터에 filmingMode가 아니라 filmingModeCommand로 들어올 가능성도 있다면
                    // SelectToken으로 양쪽 다 체크하는 것이 안전합니다. (현재 데이터 기준으로는 위 코드가 맞음)

                    if (!uavTotalRequests.ContainsKey(displayUavId)) uavTotalRequests[displayUavId] = 0;
                    uavTotalRequests[displayUavId]++;

                    string uavKey = $"UAV_{displayUavId}";
                    if (actionTimestampResult[uavKey] == null) actionTimestampResult[uavKey] = new JObject();
                    var uavData = (JObject)actionTimestampResult[uavKey];

                    JToken bestResponse = null;
                    if (groupedControlCommands.TryGetValue(baseAircraftId, out var relevantControlCommands))
                    {
                        // [수정 2] 타임스탬프 검색 조건 완화
                        // 데이터상 0602가 0601보다 2~10ms 먼저 찍히는 경우가 있으므로, 
                        // "기준 시간 - 1000ms" ~ "기준 시간 + 1000ms" 사이의 데이터를 찾도록 변경합니다.
                        long minTime = (long)baseTimestamp - 1000;

                        bestResponse = relevantControlCommands.FirstOrDefault(control =>
                            (long)control.Value<ulong>("timestamp") >= minTime &&
                            (int?)control.SelectToken("flightModeCommand.flightMode") == baseFlightMode &&
                            (int?)control.SelectToken("filmingModeCommand.operationMode") == baseFilmingMode);
                    }

                    if (bestResponse != null)
                    {
                        ulong controlTimestamp = bestResponse.Value<ulong>("timestamp");

                        // [수정 3] 시간 차이 계산 시 ulong 오버플로우 방지를 위해 long으로 변환 후 절대값 비교
                        long diff = Math.Abs((long)controlTimestamp - (long)baseTimestamp);

                        if (diff <= 1000) // 1초 이내 매칭 성공
                        {
                            matchedCount++;
                            if (!uavMatchedCounts.ContainsKey(displayUavId)) uavMatchedCounts[displayUavId] = 0;
                            uavMatchedCounts[displayUavId]++;

                            if (baseFlightMode.HasValue)
                            {
                                string flightModeKey = $"FlightMode_{baseFlightMode.Value}";
                                if (uavData[flightModeKey] == null) uavData[flightModeKey] = new JObject { ["StartTimestamp"] = new JArray(), ["EndTimestamp"] = new JArray() };
                                ((JArray)uavData[flightModeKey]["StartTimestamp"]).Add(baseTimestamp);
                                ((JArray)uavData[flightModeKey]["EndTimestamp"]).Add(controlTimestamp);
                            }
                            if (baseFilmingMode.HasValue)
                            {
                                string filmingModeKey = $"FilmingMode_{baseFilmingMode.Value}";
                                if (uavData[filmingModeKey] == null) uavData[filmingModeKey] = new JObject { ["StartTimestamp"] = new JArray(), ["EndTimestamp"] = new JArray() };
                                ((JArray)uavData[filmingModeKey]["StartTimestamp"]).Add(baseTimestamp);
                                ((JArray)uavData[filmingModeKey]["EndTimestamp"]).Add(controlTimestamp);
                            }
                        }
                        else // 시간 초과 (Late)
                        {
                            if (baseFlightMode.HasValue)
                            {
                                string lateFlightModeKey = $"LateFlightMode_{baseFlightMode.Value}";
                                if (uavData[lateFlightModeKey] == null) uavData[lateFlightModeKey] = new JObject { ["StartTimestamp"] = new JArray(), ["EndTimestamp"] = new JArray() };
                                ((JArray)uavData[lateFlightModeKey]["StartTimestamp"]).Add(baseTimestamp);
                                ((JArray)uavData[lateFlightModeKey]["EndTimestamp"]).Add(controlTimestamp);
                            }
                            if (baseFilmingMode.HasValue)
                            {
                                string lateFilmingModeKey = $"LateFilmingMode_{baseFilmingMode.Value}";
                                if (uavData[lateFilmingModeKey] == null) uavData[lateFilmingModeKey] = new JObject { ["StartTimestamp"] = new JArray(), ["EndTimestamp"] = new JArray() };
                                ((JArray)uavData[lateFilmingModeKey]["StartTimestamp"]).Add(baseTimestamp);
                                ((JArray)uavData[lateFilmingModeKey]["EndTimestamp"]).Add(controlTimestamp);
                            }
                        }
                    }
                    else // 응답 없음 (NoResp)
                    {
                        ulong failedEndTime = baseTimestamp + 1000;
                        if (baseFlightMode.HasValue)
                        {
                            string noRespFlightModeKey = $"NoRespFlightMode_{baseFlightMode.Value}";
                            if (uavData[noRespFlightModeKey] == null) uavData[noRespFlightModeKey] = new JObject { ["StartTimestamp"] = new JArray(), ["EndTimestamp"] = new JArray() };
                            ((JArray)uavData[noRespFlightModeKey]["StartTimestamp"]).Add(baseTimestamp);
                            ((JArray)uavData[noRespFlightModeKey]["EndTimestamp"]).Add(failedEndTime);
                        }
                        if (baseFilmingMode.HasValue)
                        {
                            string noRespFilmingModeKey = $"NoRespFilmingMode_{baseFilmingMode.Value}";
                            if (uavData[noRespFilmingModeKey] == null) uavData[noRespFilmingModeKey] = new JObject { ["StartTimestamp"] = new JArray(), ["EndTimestamp"] = new JArray() };
                            ((JArray)uavData[noRespFilmingModeKey]["StartTimestamp"]).Add(baseTimestamp);
                            ((JArray)uavData[noRespFilmingModeKey]["EndTimestamp"]).Add(failedEndTime);
                        }
                    }
                }

                double successRate = (totalRequests == 0) ? 0.0 : (double)matchedCount / totalRequests * 100.0;
                JObject finalResult = new JObject { ["Score"] = (uint)Math.Round(successRate), ["ActionTimestamp"] = actionTimestampResult, ["UAVMetrics"] = new JObject() };
                foreach (var uavId in uavTotalRequests.Keys)
                {
                    ((JObject)finalResult["UAVMetrics"])[$"UAV_{uavId}"] = new JObject { ["TotalRequests"] = uavTotalRequests[uavId], ["MatchedCount"] = uavMatchedCounts.ContainsKey(uavId) ? uavMatchedCounts[uavId] : 0U };
                }
                return finalResult;
            }
            catch (Exception ex)
            {
                return new JObject { ["Error"] = ex.Message };
            }
        }

        #endregion

        private void Button_PreviewMouseDown_1(object sender, MouseButtonEventArgs e)
        {
            _isBulkUpdating = true;
            foreach (var item in AllScenarioItems) item.IsChecked = true;
            _isBulkUpdating = false;
            // 수동으로 한 번에 업데이트
            UpdateComparisonLists();
        }

        private void Button_PreviewMouseDown_2(object sender, MouseButtonEventArgs e)
        {
            _isBulkUpdating = true;
            foreach (var item in AllScenarioItems) item.IsChecked = false;
            _isBulkUpdating = false;
            // 수동으로 한 번에 업데이트
            UpdateComparisonLists();
        }

        private void UpdateComparisonLists()
        {
            CheckedScenariosForDisplay.Clear();
            ScenarioListBox.Items.Clear();
            SelectedScenarioReplanData.Clear(); // ✅ [추가] 재계획 데이터 클리어

            foreach (var item in AllScenarioItems.Where(i => i.IsChecked))
            {
                CheckedScenariosForDisplay.Add(new ScenarioSummary
                {
                    ScenarioName = item.ScenarioName,
                    ComplexityScore = item.Complexity.ComplexityScore,
                    AircraftCount = item.Complexity.AircraftCount,
                    CollaborativeMissions = item.Complexity.CollaborativeMissionCount,
                    IndividualMissions = item.Complexity.IndividualMissionCount,
                });
                ScenarioListBox.Items.Add(item.ScenarioName);

                // ✅ [추가] 재계획 데이터 추가
                SelectedScenarioReplanData.Add(new ReplanSummary
                {
                    ScenarioName = item.ScenarioName,
                    //AverageReplanTime = item.AverageReplanTime >= 0 ? $"{item.AverageReplanTime:F2} 초" : "N/A"
                    // 기존: 초 단위 (F2) -> 변경: 1000을 곱하여 밀리초(ms) 단위로 변환 (F0: 소수점 없음)
                    //AverageReplanTime = item.AverageReplanTime >= 0 ? $"{item.AverageReplanTime * 1000:F2} ms" : "N/A"
                    AverageReplanTime = item.AverageReplanTime >= 0 ? $"{item.AverageReplanTime:F2} ms" : "N/A"
                });
            }
            UpdateSuccessRates();
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

        // 바 밖에서는 크로스헤어 차단
        private void Chart_QueryChartCursor(object sender, QueryCursorEventArgs e)
        {
            var chart = (ChartControl)sender;
            var position = Mouse.GetPosition(chart);
            var hitInfo = chart.CalcHitInfo(position);

            if (hitInfo == null || !hitInfo.InSeries)
            {
                e.Handled = true;
                e.Cursor = Cursors.Arrow;
            }
        }

        // 접합부 스냅 필터: 마우스가 실제로 바 위에 있을 때만 크로스헤어 레이블 표시
        private void TimelineChart_CustomDrawCrosshair(object sender, CustomDrawCrosshairEventArgs e)
        {
            var chart = (ChartControl)sender;
            var mousePos = Mouse.GetPosition(chart);
            var hitInfo = chart.CalcHitInfo(mousePos);

            // 마우스가 바 위에 없으면 모든 크로스헤어 레이블 숨김
            if (hitInfo == null || !hitInfo.InSeries)
            {
                foreach (CrosshairElementGroup group in e.CrosshairElementGroups)
                {
                    foreach (CrosshairElement element in group.CrosshairElements)
                    {
                        element.Visible = false;
                    }
                    if (group.HeaderElement != null)
                        group.HeaderElement.Visible = false;
                }
                return;
            }

            // 바 위에 있으면 기본 동작 (레이블 표시)
        }
    }
}