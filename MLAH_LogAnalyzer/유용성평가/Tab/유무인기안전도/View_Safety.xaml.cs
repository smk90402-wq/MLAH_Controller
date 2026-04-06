// 필수 using 문들
using DevExpress.Map.Kml.Model;
using DevExpress.Mvvm;
using DevExpress.Xpf.Charts;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Map;
using Microsoft.WindowsAPICodePack.Dialogs;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
using static MLAH_LogAnalyzer.View_ScoreCoverage;
// using System.Windows.Media; // C# 코드에서 직접 색상을 사용하지 않으므로 불필요

namespace MLAH_LogAnalyzer
{
    public partial class View_Safety : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion

        private static readonly DateTime Epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private bool _isBulkUpdating = false;

        /// <summary>
        /// 안전도 그리드 데이터 소스
        /// </summary>
        private ObservableCollection<SafetyDetailItem> _SelectedScenarioSafetyGridData = new ObservableCollection<SafetyDetailItem>();
        public ObservableCollection<SafetyDetailItem> SelectedScenarioSafetyGridData
        {
            get => _SelectedScenarioSafetyGridData;
            set { _SelectedScenarioSafetyGridData = value; OnPropertyChanged(nameof(SelectedScenarioSafetyGridData)); }
        }

        private ScoreScenarioSummary _selectedScenarioSummary;
        public ScoreScenarioSummary SelectedScenarioSummary
        {
            get => _selectedScenarioSummary;
            set
            {
                _selectedScenarioSummary = value;
                OnPropertyChanged(nameof(SelectedScenarioSummary));

                //UpdateDisplayPanels();
                UpdateDisplayPanelsAsync();
                UpdateScore();
            }
        }

        internal class SafetyPanelData
        {
            public ScenarioData ScenarioData { get; set; }
            public SafetyResult SafetyResult { get; set; }
            public AnalysisResult AnalysisResult { get; set; } // 트랙바 계산용
        }

        /// <summary>
        /// 차트(시간별 관측 달성도) 데이터 소스
        /// </summary>
        private ObservableCollection<SafetyDataOutput> _SelectedScenarioSafetyChartData = new ObservableCollection<SafetyDataOutput>();
        public ObservableCollection<SafetyDataOutput> SelectedScenarioSafetyChartData
        {
            get => _SelectedScenarioSafetyChartData;
            set { _SelectedScenarioSafetyChartData = value; OnPropertyChanged(nameof(SelectedScenarioSafetyChartData)); }
        }


        private string _currentSnapshotTimestampFormatted = "--:--:---";
        public string CurrentSnapshotTimestampFormatted
        {
            get => _currentSnapshotTimestampFormatted;
            set
            {
                _currentSnapshotTimestampFormatted = value;
                OnPropertyChanged(nameof(CurrentSnapshotTimestampFormatted));
            }
        }

        // [2026-03-24] 슬라이더 디바운스용 CancellationTokenSource (빠른 드래그 시 중간 프레임 스킵하여 렌더링 부하 감소)
        private CancellationTokenSource _sliderDebounceCts;

        private int _currentTimelineIndex;
        public int CurrentTimelineIndex
        {
            get => _currentTimelineIndex;
            set
            {
                if (_currentTimelineIndex != value)
                {
                    _currentTimelineIndex = value;
                    OnPropertyChanged(nameof(CurrentTimelineIndex));

                    UpdateMapIconsToTimelineIndex(value);
                    UpdateTimestampLabel(value);
                    UpdateChartCrosshair(value);
                }
            }
        }


        private uint _SelectedScenarioScore = 0;
        public uint SelectedScenarioScore
        {
            get => _SelectedScenarioScore;
            set { _SelectedScenarioScore = value; OnPropertyChanged(nameof(SelectedScenarioScore)); }
        }

        private async void UpdateScore()
        {
            var scenarioToLoad = _selectedScenarioSummary;
            if (scenarioToLoad != null)
                SelectedScenarioScore = scenarioToLoad.SafetyScore;
        }

        private List<ulong> _currentScenarioTimestamps = new List<ulong>();
        private static readonly uint[] AIRCRAFT_FOR_TIMELINE = new uint[] { 1, 2, 3 };

        private ScenarioData _cachedScenarioData;

        // 안전도 분석 결과를 캐싱
        private SafetyResult _cachedSafetyResult;

        public View_Safety()
        {
            InitializeComponent();
            this.DataContext = this;

        }



        #region 트랙바 헬퍼

        private ulong GetTimestampFromIndex(int index)
        {
            // [!!!] 수정: uav4Info 대신 통합 타임스탬프 리스트(_currentScenarioTimestamps) 사용
            if (_currentScenarioTimestamps == null || index < 0 || index >= _currentScenarioTimestamps.Count)
                return 0;

            return _currentScenarioTimestamps[index];
        }

        private void UpdateTimestampLabel(int index)
        {
            try
            {
                ulong currentTime = GetTimestampFromIndex(index);
                if (currentTime == 0)
                {
                    CurrentSnapshotTimestampFormatted = "--:--:---";
                    return;
                }

                // 1. ulong -> DateTime으로 변환
                DateTime originalTimestamp = Epoch.AddMilliseconds(currentTime).ToLocalTime();
                DateTime displayTimestamp;

                // 2. 2055년 보정 로직 적용
                if (originalTimestamp.Year == 2055)
                {
                    displayTimestamp = new DateTime(2025,
                                originalTimestamp.Month,
                                originalTimestamp.Day,
                                originalTimestamp.Hour,
                                originalTimestamp.Minute,
                                originalTimestamp.Second,
                                originalTimestamp.Millisecond,
                                originalTimestamp.Kind);
                }
                else
                {
                    displayTimestamp = originalTimestamp;
                }

                // 3. '실제 시각' 포맷으로 레이블 업데이트
                // (차트 X축과 동일한 포맷을 사용하거나 원하는 포맷으로 변경)
                //CurrentSnapshotTimestampFormatted = displayTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                CurrentSnapshotTimestampFormatted = displayTimestamp.ToString("HH:mm:ss.fff");
                // (만약 '경과 시간'이 필요하면 이전 로직 사용)
                // ulong startTime = _currentScenarioTimestamps.First();
                // TimeSpan duration = TimeSpan.FromMilliseconds(currentTime - startTime);
                // CurrentSnapshotTimestampFormatted = duration.ToString(@"hh\:mm\:ss\.fff");
            }
            catch (Exception) { CurrentSnapshotTimestampFormatted = "Error"; }
        }

        private void UpdateMapIconsToTimelineIndex(int index)
        {
            // ✅ [수정] 캐시 데이터 확인
            if (_cachedScenarioData == null)
            {
                ViewModel_Unit_Map_Safety.SingletonInstance.EvaluationUavPositions.Clear();
                return;
            }

            ulong timestamp = GetTimestampFromIndex(index);
            if (timestamp == 0)
            {
                ViewModel_Unit_Map_Safety.SingletonInstance.EvaluationUavPositions.Clear();
                return;
            }

            // ❌ [삭제] 반복 로드 제거
            // if (_selectedScenarioSummary == null) return;
            // string fullPath = _selectedScenarioSummary.OriginalItem.FullPath;
            // string baseDirectory = Path.GetDirectoryName(fullPath);
            // ScenarioData scenarioData = Utils.LoadScenarioData(baseDirectory, _selectedScenarioSummary.ScenarioNumber);

            // 캐시된 데이터 사용
            ViewModel_Unit_Map_Safety.SingletonInstance.ShowAircraftPositionsAt(timestamp, _cachedScenarioData, _cachedSafetyResult);
        }
        

        /// <summary>
                /// 트랙바 인덱스를 차트 크로스헤어 위치로 변환
                /// </summary>
        private void UpdateChartCrosshair(int index)
        {
            if (ScoreChart == null || !ScoreChart.IsLoaded || !ScoreChart.IsVisible) return;

            // 1. ulong 타임스탬프 가져오기
            ulong timestamp = GetTimestampFromIndex(index);
            if (timestamp == 0)
            {
                // [!!!] 수정: ChartControl.HideCrosshair() 사용 [!!!]
                //ScoreChart.HideCrosshair(); // 인덱스 유효하지 않으면 크로스헤어 숨김
                return;
            }

            // 2. ulong -> DateTime 변환 (2055년 보정 포함)
            DateTime originalTimestamp = Epoch.AddMilliseconds(timestamp).ToLocalTime();
            DateTime displayTimestamp;

            if (originalTimestamp.Year == 2055)
            {
                displayTimestamp = new DateTime(2025,
                       originalTimestamp.Month,
                       originalTimestamp.Day,
                       originalTimestamp.Hour,
                       originalTimestamp.Minute,
                       originalTimestamp.Second,
                       originalTimestamp.Millisecond,
                       originalTimestamp.Kind);
            }
            else
            {
                displayTimestamp = originalTimestamp;
            }

            // 3. [!!!] 수정: ShowCrosshair(argument, value) 오버로드 사용 [!!!]
            var diagram = ScoreChart.Diagram as XYDiagram2D;
            if (diagram != null)
            {
                // SnapMode가 'NearestArgument'로 설정되어 있으므로, 
                // Y축 값(value)으로 'null'을 전달하여 X축(Argument)에만 스냅하도록 합니다.
                diagram.ShowCrosshair(displayTimestamp, null);
            }
        }
        #endregion


        private async void UpdateDisplayPanelsAsync()
        {
            // 1. 로드할 대상 가져오기
            var scenarioToLoad = _selectedScenarioSummary;

            if (scenarioToLoad == null)
            {
                return;
            }

            // 2. 스플래시 스크린 뷰모델 정의 (참고 코드 기반)
            var splashViewModel = new DXSplashScreenViewModel()
            {
                Title = $"{scenarioToLoad.ScenarioName} Loading...",
                Status = "유용성 평가 분석 데이터 동기화 중...",
                IsIndeterminate = true,
            };

            // 3. 스플래시 스크린을 띄울 부모 창 찾기
            var parentWindow = Window.GetWindow(this);
            if (parentWindow == null)
            {
                // (비상시) 부모 창을 찾을 수 없으면 그냥 동기식으로 실행 (UI 멈춤)
                if (scenarioToLoad != null)
                {
                    // ✅ [수정] 경로 추출 및 전달
                    string fullPath = scenarioToLoad.OriginalItem.FullPath;
                    string baseDirectory = Path.GetDirectoryName(fullPath);

                    //var data = await Utils.LoadScenarioData(baseDirectory, scenarioToLoad.ScenarioNumber);
                    var data = await Utils.LoadScenarioDataByPath(fullPath);
                    var result = scenarioToLoad.OriginalItem.SafetyAnalysisResult;
                    await UpdateSafetyPanelsAsync(data, result);
                }
                else
                {
                    await UpdateSafetyPanelsAsync(null, null); // Clear UI
                }
                return;
            }

            // 4. 스플래시 스크린 매니저 생성
            var manager = SplashScreenManager.CreateWaitIndicator(splashViewModel);

            SafetyPanelData loadedData = null;

            try
            {
                // 5. 스플래시 스크린 표시
                manager.Show(parentWindow, WindowStartupLocation.CenterOwner, true, InputBlockMode.Window);

                if (scenarioToLoad != null)
                {
                    //splashViewModel.Status = $"Scenario {scenarioToLoad.ScenarioNumber} 데이터 로드 중...";

                    string fullPath = scenarioToLoad.OriginalItem.FullPath;
                    string baseDirectory = Path.GetDirectoryName(fullPath);

                    //loadedData = await Task.Run(() =>
                    //{
                    //    // ✅ [수정] 경로(baseDirectory)를 함께 전달
                    //    var data = Utils.LoadScenarioData(baseDirectory, scenarioToLoad.ScenarioNumber);
                    //    var result = scenarioToLoad.OriginalItem.SafetyAnalysisResult;

                    //    return new SafetyPanelData { ScenarioData = data, SafetyResult = result };
                    //});
                    loadedData = await Task.Run(async () => // 1. async 추가
                    {
                        // 2. await 추가
                        //var data = await Utils.LoadScenarioData(baseDirectory, scenarioToLoad.ScenarioNumber);
                        var data = await Utils.LoadScenarioDataByPath(fullPath);

                        var analysis = scenarioToLoad.OriginalItem.SafetyAnalysisResult;
                        return new SafetyPanelData { SafetyResult = analysis, ScenarioData = data };
                    });
                }

                // 7. [!!!] 빠른 작업(UI 업데이트)은 다시 UI 스레드에서 실행 [!!!]
                //splashViewModel.Status = "UI 업데이트 중...";
                if (loadedData != null)
                {
                    await UpdateSafetyPanelsAsync(loadedData.ScenarioData, loadedData.SafetyResult);
                }
                else
                {
                    await UpdateSafetyPanelsAsync(null, null); // 선택 해제 시 UI 클리어
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(parentWindow, $"데이터 로드 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                await UpdateSafetyPanelsAsync(null, null); // 오류 시 UI 클리어
            }
            finally
            {
                // WPF 렌더링 큐 비운 후 스플래시 닫기
                await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                await Task.Delay(800);
                manager.Close();
            }
        }

        /// <summary>
        /// [!!!] 신규: 안전도 관련 그리드와 맵 시각화를 업데이트합니다. [!!!]
        /// </summary>
        private async Task UpdateSafetyPanelsAsync(ScenarioData scenarioData, SafetyResult safetyResult)
        {
            // 1. 모든 데이터 클리어
            SelectedScenarioSafetyChartData = new ObservableCollection<SafetyDataOutput>();
            SelectedScenarioSafetyGridData = new ObservableCollection<SafetyDetailItem>();
            ViewModel_Unit_Map_Safety.SingletonInstance.ClearSafetyVisuals(); // 맵 클리어
            _currentScenarioTimestamps.Clear();

            _cachedScenarioData = scenarioData;
            _cachedSafetyResult = safetyResult;

            // 2. 데이터가 없으면 트랙바/맵 아이콘도 초기화하고 종료
            if (scenarioData == null)
            {
                this.CurrentTimelineIndex = 0;
                if (UavTrackBar != null)
                {
                    UavTrackBar.Minimum = 0;
                    UavTrackBar.Maximum = 0;
                }

                this.CurrentTimelineIndex = 0;
                UpdateMapIconsToTimelineIndex(0);
                UpdateTimestampLabel(0);
                UpdateChartCrosshair(0);

                ApplyChartRange(); // 빈 차트라도 축 범위는 적용
                return;
            }

            // 시나리오 데이터뿐만 아니라, 계산된 '안전 결과(safetyResult)'도 함께 넘기기 - LOS 때문
            ViewModel_Unit_Map_Safety.SingletonInstance.BuildFlightDataIndex(scenarioData);
            await ViewModel_Unit_Map_Safety.SingletonInstance.UpdateSafetyVisualsAsync(scenarioData, safetyResult);

            var aircraftToChart = new uint[] { 1, 2, 3 };

            // 기체별 비행 데이터 그룹화 (차트/트랙바에서 공통 사용)
            var allFlightDataByAircraft = scenarioData.FlightData
        .Where(fd => aircraftToChart.Contains(fd.AircraftID) && fd.FlightDataLog != null)
        .OrderBy(fd => fd.Timestamp)
        .GroupBy(fd => fd.AircraftID);

            // 4. [!!!] safetyResult가 있을 때만 차트와 그리드 계산 [!!!]
            if (safetyResult != null && safetyResult.ThreatenedTimestamps != null)
            {
                // 4-1. [차트] 데이터 채우기

                // 빠른 조회를 위해 기체별 위협 타임스탬프 Set 생성
                var dangerousTimestampSets = new Dictionary<string, HashSet<ulong>>();
                foreach (var key in safetyResult.ThreatenedTimestamps.Keys)
                {
                    dangerousTimestampSets[key] = new HashSet<ulong>(safetyResult.ThreatenedTimestamps[key]);
                }

                var chartList = new List<SafetyDataOutput>();

                // 기체별로 루프를 돌며 누적 점수 계산
                foreach (var aircraftGroup in allFlightDataByAircraft)
                {
                    string key = (aircraftGroup.Key <= 3) ? $"LAH{aircraftGroup.Key}" : $"UAV{aircraftGroup.Key}";
                    HashSet<ulong> dangerousSet = null;
                    dangerousTimestampSets.TryGetValue(key, out dangerousSet);

                    int totalTimestampsSoFar = 0;
                    int threatTimestampsSoFar = 0;

                    // 해당 기체의 모든 비행 기록을 순회
                    foreach (var flightEntry in aircraftGroup)
                    {
                        totalTimestampsSoFar++;
                        bool isDangerous = (dangerousSet != null && dangerousSet.Contains(flightEntry.Timestamp));

                        if (isDangerous)
                        {
                            threatTimestampsSoFar++;
                        }

                        double cumulativeSafetyScore = 100.0 * (double)(totalTimestampsSoFar - threatTimestampsSoFar) / totalTimestampsSoFar;

                        // 2055년 보정 로직
                        DateTime originalTimestamp = Epoch.AddMilliseconds(flightEntry.Timestamp).ToLocalTime();
                        DateTime displayTimestamp;
                        if (originalTimestamp.Year == 2055)
                        {
                            displayTimestamp = new DateTime(2025,
                                          originalTimestamp.Month,
                               originalTimestamp.Day,
                               originalTimestamp.Hour,
                               originalTimestamp.Minute,
                               originalTimestamp.Second,
                               originalTimestamp.Millisecond,
                               originalTimestamp.Kind);
                        }
                        else
                        {
                            displayTimestamp = originalTimestamp;
                        }

                        chartList.Add(new SafetyDataOutput
                        {
                            Timestamp = displayTimestamp,
                            AircraftID = flightEntry.AircraftID,
                            Score = cumulativeSafetyScore
                        });
                    }
                }

                SelectedScenarioSafetyChartData = new ObservableCollection<SafetyDataOutput>(chartList);

                // 4-2. [그리드] 데이터 채우기 ('횟수' 기준)
                var gridList = new List<SafetyDetailItem>();
                foreach (var id in AIRCRAFT_FOR_TIMELINE) // (LAH 1-3, UAV 4-6)
                {
                    //string key = (id <= 3) ? $"LAH{id}" : $"UAV{id}";
                    string key = $"LAH{id}";

                    int totalCount = scenarioData.FlightData.Count(fd => fd.AircraftID == id && fd.FlightDataLog != null);
                    int threatCount = 0;
                    if (safetyResult.ThreatenedTimestamps.TryGetValue(key, out var tsList))
                    {
                        threatCount = tsList.Count;
                    }

                    int safeCount = totalCount - threatCount;

                    double safetyPercentage = (totalCount > 0) ? ((double)safeCount / totalCount * 100.0) : 0;

                    if (totalCount > 0)
                    {
                        gridList.Add(new SafetyDetailItem
                        {
                            ID = key,
                            // [수정] 'ExposurePercentage' 속성에 "안전도"를 할당
                            ExposurePercentage = safetyPercentage,
                            ThreatTimestampCount = threatCount,
                            TotalTimestampCount = totalCount
                        });
                    }
                }
                SelectedScenarioSafetyGridData = new ObservableCollection<SafetyDetailItem>(gridList);
            } // [if (safetyResult != null)] 끝

            // 5. [트랙바] 설정 (scenarioData만 필요)
            _currentScenarioTimestamps = allFlightDataByAircraft
           .SelectMany(g => g) // 모든 그룹의 항목을 하나로 합침
                      .Select(fd => fd.Timestamp)
           .Distinct()
           .OrderBy(ts => ts) // 정렬
                      .ToList();

            if (_currentScenarioTimestamps.Any())
            {
                int snapshotCount = _currentScenarioTimestamps.Count;
                UavTrackBar.Minimum = 0;
                UavTrackBar.Maximum = (snapshotCount > 0) ? snapshotCount - 1 : 0;
                UavTrackBar.TickFrequency = 1;

                this.CurrentTimelineIndex = 0;
                UpdateMapIconsToTimelineIndex(0); // 맵 아이콘/원 표시
                UpdateTimestampLabel(0);       // 레이블 표시
                UpdateChartCrosshair(0);       // 차트 크로스헤어 표시
            }
            else
            {
                UavTrackBar.Minimum = 0;
                UavTrackBar.Maximum = 0;

                this.CurrentTimelineIndex = 0;
                UpdateMapIconsToTimelineIndex(0);
                UpdateTimestampLabel(0);
                UpdateChartCrosshair(0);
            }

            // 6. 차트 축 범위 적용
            //ApplyChartRange(isCumulative: true);
            ApplyChartRange();
        }

        private void ApplyChartRange()
        {
            // [방어 1] 차트가 아직 로드되지 않았으면 건너뜀
            if (ScoreChart == null || !ScoreChart.IsLoaded) return;

            if (ScoreChart.Diagram is not XYDiagram2D diagram || axisX == null) return;

            // [방어 2] 업데이트 중 렌더링 충돌 방지를 위한 잠금
            ScoreChart.BeginInit();
            try
            {
                DateTime minTime;
                DateTime maxTime;

                if (!SelectedScenarioSafetyChartData.Any())
                {
                    minTime = DateTime.Now;
                    maxTime = minTime.AddSeconds(1);
                }
                else
                {
                    minTime = SelectedScenarioSafetyChartData.Min(dp => dp.Timestamp);
                    maxTime = SelectedScenarioSafetyChartData.Max(dp => dp.Timestamp);
                    if (maxTime <= minTime) maxTime = minTime.AddSeconds(1);
                }

                // X축 범위
                if (axisX.WholeRange != null)
                    axisX.WholeRange.SetMinMaxValues(minTime, maxTime);
                if (axisX.VisualRange != null)
                    axisX.VisualRange.SetMinMaxValues(minTime, maxTime);

                // Y축 범위 (-5 ~ 105점)
                if (diagram.ActualAxisY != null)
                {
                    double minScore = -5;
                    double maxScore = 105;

                    if (diagram.ActualAxisY.WholeRange != null)
                        diagram.ActualAxisY.WholeRange.SetMinMaxValues(minScore, maxScore);
                    if (diagram.ActualAxisY.VisualRange != null)
                        diagram.ActualAxisY.VisualRange.SetMinMaxValues(minScore, maxScore);
                }
            }
            finally
            {
                // [방어 2] 렌더링 잠금 해제
                ScoreChart.EndInit();
            }
        }

        private void ScoreChart_BoundDataChanged(object sender, RoutedEventArgs e)
        {
            var chart = sender as ChartControl;
            if (chart == null || chart.Diagram == null)
                return;

            foreach (var series in chart.Diagram.Series)
            {
                if (series is LineSeries2D lineSeries)
                {
                    if (uint.TryParse(lineSeries.DisplayName, out uint aircraftId))
                    {
                        // UAV 4(파랑), 5(빨강), 6(연두)
                        SolidColorBrush brush = Brushes.Gray; // 기본값
                        //switch (uavId)
                        //{
                        //    case 4: brush = Brushes.Blue; break; // UAV 4
                        //    case 5: brush = Brushes.Red; break; // UAV 5
                        //    case 6: brush = Brushes.LimeGreen; break; // UAV 6
                        //}

                        switch (aircraftId)
                        {
                            case 1: brush = Brushes.Blue; break;       // LAH 1 (지휘기)
                            case 2: brush = Brushes.CornflowerBlue; break; // LAH 2
                            case 3: brush = Brushes.DeepSkyBlue; break;    // LAH 3
                                                                           // 4, 5, 6 케이스 제거
                        }

                        //if (lineSeries.LineStyle == null) lineSeries.LineStyle = new LineStyle();
                        //lineSeries.LineStyle.Brush = brush;

                        //if (lineSeries.MarkerStyle == null) lineSeries.MarkerStyle = new MarkerStyle();
                        //lineSeries.MarkerStyle.Stroke = brush;
                        //lineSeries.MarkerStyle.Brush = Brushes.White;
                    }
                }
            }
        }

        // ---------------------------------------------------------
        // 차트 클릭 시 트랙바/지도 동기화 로직
        // ---------------------------------------------------------
        private void Chart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var chart = sender as ChartControl;
            if (chart == null || !chart.IsLoaded) return;

            // 1. 클릭한 위치의 정보(HitInfo)를 계산
            System.Windows.Point position = e.GetPosition(chart);
            ChartHitInfo hitInfo = chart.CalcHitInfo(position);

            // 2. 시리즈 포인트(점) 위를 클릭했거나, 다이어그램 내부를 클릭했을 때 처리
            // (정확히 점을 안 찍어도 해당 X축 위치로 이동하려면 InDiagram 조건도 포함 - 일단 진행
            if (hitInfo != null && (hitInfo.SeriesPoint != null || hitInfo.InDiagram))
            {
                DateTime clickedTime;

                // A. 점을 정확히 클릭한 경우
                if (hitInfo.SeriesPoint != null)
                {
                    clickedTime = hitInfo.SeriesPoint.DateTimeArgument;
                }
                // B. 점이 아닌 빈 공간(Diagram)을 클릭한 경우 (X축 좌표 역산)
                else
                {
                    var diagram = chart.Diagram as XYDiagram2D;
                    if (diagram == null) return;

                    // 화면 좌표를 다이어그램 좌표로 변환
                    var coords = diagram.PointToDiagram(position);
                    if (coords == null || coords.DateTimeArgument == DateTime.MinValue) return;

                    clickedTime = coords.DateTimeArgument;
                }

                // 3. 클릭한 시간과 가장 가까운 타임스탬프 인덱스 찾기
                int bestIndex = FindClosestTimestampIndex(clickedTime);

                // 4. 인덱스 업데이트 -> 트랙바, 지도, 차트 크로스헤어 자동 동기화됨
                if (bestIndex >= 0 && bestIndex < _currentScenarioTimestamps.Count)
                {
                    CurrentTimelineIndex = bestIndex;
                }
            }
        }

        /// <summary>
        /// DateTime을 받아서 _timestamps 리스트 중 가장 가까운 인덱스를 반환
        /// </summary>
        private int FindClosestTimestampIndex(DateTime targetDateTime)
        {
            if (_currentScenarioTimestamps == null || _currentScenarioTimestamps.Count == 0) return -1;

            // 1. DateTime -> ulong Timestamp 변환 (기존 Epoch 로직 역산)
            // Epoch는 UTC 기준이므로, LocalTime으로 들어온 targetDateTime을 UTC로 바꿔서 계산해야 함
            ulong targetTs = (ulong)(targetDateTime.ToUniversalTime() - Epoch).TotalMilliseconds;

            // 2. 가장 차이가 적은 인덱스 탐색
            int bestIndex = -1;
            ulong minDiff = ulong.MaxValue;

            for (int i = 0; i < _currentScenarioTimestamps.Count; i++)
            {
                // ulong이라 음수가 나올 수 없으므로 절대값 처리 주의 (큰 수 - 작은 수)
                ulong currentTs = _currentScenarioTimestamps[i];
                ulong diff = (currentTs > targetTs) ? (currentTs - targetTs) : (targetTs - currentTs);

                if (diff < minDiff)
                {
                    minDiff = diff;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }
    }
}
