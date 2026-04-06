// 필수 using 문들
using DevExpress.Map.Kml.Model;
using DevExpress.Mvvm;
using DevExpress.Xpf.Charts;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;
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
using static MLAH_LogAnalyzer.View_Safety;
// using System.Windows.Media; // C# 코드에서 직접 색상을 사용하지 않으므로 불필요

namespace MLAH_LogAnalyzer
{
    internal class MissionDistPanelData
    {
        public ScenarioData ScenarioData { get; set; }
        public MissionDistributionResult MissionResult { get; set; }
    }

    public partial class View_Distribution : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion

        private static readonly DateTime Epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private ObservableCollection<MissionDistChartItem> _SelectedScenarioMissionChartData = new ObservableCollection<MissionDistChartItem>();
        public ObservableCollection<MissionDistChartItem> SelectedScenarioMissionChartData
        {
            get => _SelectedScenarioMissionChartData;
            set { _SelectedScenarioMissionChartData = value; OnPropertyChanged(nameof(SelectedScenarioMissionChartData)); }
        }

        private ObservableCollection<MissionDistDetailItem> _SelectedScenarioMissionGridData = new ObservableCollection<MissionDistDetailItem>();
        public ObservableCollection<MissionDistDetailItem> SelectedScenarioMissionGridData
        {
            get => _SelectedScenarioMissionGridData;
            set { _SelectedScenarioMissionGridData = value; OnPropertyChanged(nameof(SelectedScenarioMissionGridData)); }
        }


        private ScoreScenarioSummary _selectedScenarioSummary;
        public ScoreScenarioSummary SelectedScenarioSummary
        {
            get => _selectedScenarioSummary;
            set
            {
                if (_selectedScenarioSummary == value) return;
                _selectedScenarioSummary = value;
                OnPropertyChanged(nameof(SelectedScenarioSummary));
                UpdateDisplayPanelsAsync();
                UpdateScore();
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
                SelectedScenarioScore = scenarioToLoad.MissionDistScore;
        }

        // (트랙바 관련 속성)
        private string _currentSnapshotTimestampFormatted = "--:--:---";
        public string CurrentSnapshotTimestampFormatted
        {
            get => _currentSnapshotTimestampFormatted;
            set { _currentSnapshotTimestampFormatted = value; OnPropertyChanged(nameof(CurrentSnapshotTimestampFormatted)); }
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
                    UpdateTimestampLabel(value);
                    UpdateChartCrosshair(value);
                    UpdateMapIconsToTimelineIndex(value);
                }
            }
        }

        private List<ulong> _currentScenarioTimestamps = new List<ulong>();
        private static readonly uint[] AIRCRAFT_FOR_TIMELINE = new uint[] { 4, 5, 6 }; // 임무 분배는 UAV 4,5,6만 대상

        private ScenarioData _cachedScenarioData;

        public View_Distribution()
        {
            InitializeComponent();
            this.DataContext = this;

        }

        #region 트랙바 헬퍼

        private ulong GetTimestampFromIndex(int index)
        {
            if (_currentScenarioTimestamps == null || index < 0 || index >= _currentScenarioTimestamps.Count)
                return 0;
            return _currentScenarioTimestamps[index];
        }

        private void UpdateTimestampLabel(int index)
        {
            try
            {
                // [수정] 인덱스 유효성 체크
                if (_currentScenarioTimestamps == null || index < 0 || index >= _currentScenarioTimestamps.Count)
                {
                    CurrentSnapshotTimestampFormatted = "--:--:---";
                    return;
                }

                // 값 가져오기 (0이어도 정상 처리)
                ulong currentTime = _currentScenarioTimestamps[index];

                DateTime originalTimestamp = Epoch.AddMilliseconds(currentTime).ToLocalTime();
                DateTime displayTimestamp;

                if (originalTimestamp.Year == 2055)
                {
                    displayTimestamp = new DateTime(2025, originalTimestamp.Month, originalTimestamp.Day, originalTimestamp.Hour, originalTimestamp.Minute, originalTimestamp.Second, originalTimestamp.Millisecond, originalTimestamp.Kind);
                }
                else
                {
                    displayTimestamp = originalTimestamp;
                }
                CurrentSnapshotTimestampFormatted = displayTimestamp.ToString("HH:mm:ss.fff");
            }
            catch (Exception) { CurrentSnapshotTimestampFormatted = "Error"; }
        }

        private void UpdateChartCrosshair(int index)
        {
            if (MissionChart == null || !MissionChart.IsLoaded || !MissionChart.IsVisible) return;

            var diagram = MissionChart.Diagram as XYDiagram2D;
            if (diagram == null) return;

            ulong timestamp = GetTimestampFromIndex(index);
            if (timestamp == 0)
            {
                //MissionChart.HideCrosshair();
                return;
            }

            DateTime originalTimestamp = Epoch.AddMilliseconds(timestamp).ToLocalTime();
            DateTime displayTimestamp;
            if (originalTimestamp.Year == 2055)
            {
                displayTimestamp = new DateTime(2025, originalTimestamp.Month, originalTimestamp.Day, originalTimestamp.Hour, originalTimestamp.Minute, originalTimestamp.Second, originalTimestamp.Millisecond, originalTimestamp.Kind);
            }
            else
            {
                displayTimestamp = originalTimestamp;
            }
            diagram.ShowCrosshair(displayTimestamp, null);
        }

        private void UpdateMapIconsToTimelineIndex(int index)
        {
            // ✅ [수정] 캐시된 데이터가 없거나 인덱스가 유효하지 않으면 리턴
            if (_cachedScenarioData == null) return;

            if (_currentScenarioTimestamps == null || index < 0 || index >= _currentScenarioTimestamps.Count)
            {
                ViewModel_Unit_Map_Distribution.SingletonInstance.EvaluationUavPositions.Clear();
                return;
            }

            ulong timestamp = _currentScenarioTimestamps[index];

            // ❌ [삭제] 매번 파일 로드하던 코드 제거
            // string fullPath = _selectedScenarioSummary.OriginalItem.FullPath;
            // string baseDirectory = Path.GetDirectoryName(fullPath);
            // ScenarioData scenarioData = Utils.LoadScenarioData(baseDirectory, _selectedScenarioSummary.ScenarioNumber);

            // ✅ [수정] 캐시된 데이터(_cachedScenarioData) 사용
            ViewModel_Unit_Map_Distribution.SingletonInstance.ShowAircraftPositionsAt(timestamp, _cachedScenarioData);
        }
        #endregion


        private async void UpdateDisplayPanelsAsync()
        {
            var item = _selectedScenarioSummary;

            if (item == null)
            {
                return;
            }

            var splashViewModel = new DXSplashScreenViewModel()
            {
                Title = $"{item.ScenarioName} Loading...",
                Status = "유용성 평가 분석 데이터 동기화 중...",
                IsIndeterminate = true,
            };

            var parent = Window.GetWindow(this);
            if (parent == null) { UpdateDisplayPanels(null); return; }

            var manager = SplashScreenManager.CreateWaitIndicator(splashViewModel);
            MissionDistPanelData loaded = null;

            try
            {
                manager.Show(parent, WindowStartupLocation.CenterOwner, true, InputBlockMode.Window);

                if (item != null && item.OriginalItem != null)
                {
                    //splashViewModel.Status = $"Scenario {item.ScenarioNumber} 시각화 데이터 준비 중...";
                    var preCalculatedResult = item.OriginalItem.MissionDistResult;

                    // ✅ [수정] 경로 추출
                    string fullPath = item.OriginalItem.FullPath;
                    string baseDirectory = Path.GetDirectoryName(fullPath);

                    //loaded = await Task.Run(() => {
                    //    // ✅ [수정] 경로(baseDirectory)를 함께 전달
                    //    var data = Utils.LoadScenarioData(baseDirectory, item.ScenarioNumber);

                    //    return new MissionDistPanelData
                    //    {
                    //        ScenarioData = data,
                    //        MissionResult = preCalculatedResult
                    //    };
                    //});
                    loaded = await Task.Run(async () => // 1. async 추가
                    {
                        // 2. await 추가
                        //var data = await Utils.LoadScenarioData(baseDirectory, item.ScenarioNumber);
                        var data = await Utils.LoadScenarioDataByPath(fullPath);

                        return new MissionDistPanelData { MissionResult = preCalculatedResult, ScenarioData = data };
                    });
                }

                //splashViewModel.Status = "UI 업데이트 중...";
                UpdateDisplayPanels(loaded);
            }
            catch (Exception ex)
            {
                MessageBox.Show(parent, $"데이터 로드 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateDisplayPanels(null);
            }
            finally
            {
                await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                await Task.Delay(800);
                manager.Close();
            }
        }

        private void UpdateDisplayPanels(MissionDistPanelData loadedData)
        {
            // 1. 모든 데이터 클리어
            SelectedScenarioMissionChartData = new ObservableCollection<MissionDistChartItem>();
            SelectedScenarioMissionGridData = new ObservableCollection<MissionDistDetailItem>();
            _currentScenarioTimestamps.Clear();
            ViewModel_Unit_Map_Distribution.SingletonInstance.ClearMissionVisuals();

            // 2. 데이터가 없으면 트랙바 초기화하고 종료
            if (loadedData == null || loadedData.ScenarioData == null || loadedData.MissionResult == null)
            {
                _cachedScenarioData = null; // 초기화
                this.CurrentTimelineIndex = 0;
                if (MissionTrackBar != null)
                {
                    MissionTrackBar.Minimum = 0;
                    MissionTrackBar.Maximum = 0;
                }
                this.CurrentTimelineIndex = 0;
                UpdateTimestampLabel(0);
                UpdateChartCrosshair(0);
                ApplyChartRange();
                return;
            }

            _cachedScenarioData = loadedData.ScenarioData;
            //var scenarioData = loadedData.ScenarioData;
            var missionResult = loadedData.MissionResult;

            ViewModel_Unit_Map_Distribution.SingletonInstance.BuildFlightDataIndex(_cachedScenarioData);
            ViewModel_Unit_Map_Distribution.SingletonInstance.UpdateMissionVisuals(_cachedScenarioData, missionResult);

            // 3. [차트] 데이터 채우기 (1 = 수행, 0 = 중지)

            // 빠른 조회를 위해 UAV별 '중지 시간 범위' 딕셔너리 생성
            var pauseRangesDict = new Dictionary<uint, List<PauseTimeRange>>();
            if (missionResult.MissionPauseTimestamp != null)
            {
                if (missionResult.MissionPauseTimestamp.TryGetValue("UAV4", out var ranges4)) pauseRangesDict[4] = ranges4;
                if (missionResult.MissionPauseTimestamp.TryGetValue("UAV5", out var ranges5)) pauseRangesDict[5] = ranges5;
                if (missionResult.MissionPauseTimestamp.TryGetValue("UAV6", out var ranges6)) pauseRangesDict[6] = ranges6;
            }

            var uavFlightDataByAircraft = _cachedScenarioData.FlightData
              .Where(fd => AIRCRAFT_FOR_TIMELINE.Contains(fd.AircraftID) && fd.FlightDataLog != null)
              .OrderBy(fd => fd.Timestamp)
              .GroupBy(fd => fd.AircraftID);

            var chartList = new List<MissionDistChartItem>();
            foreach (var aircraftGroup in uavFlightDataByAircraft)
            {
                uint uavId = aircraftGroup.Key;
                List<PauseTimeRange> pauseRanges = null;
                pauseRangesDict.TryGetValue(uavId, out pauseRanges);

                foreach (var flightEntry in aircraftGroup)
                {
                    bool isPaused = false;
                    if (pauseRanges != null)
                    {
                        // 현재 타임스탬프가 중지 범위 중 하나라도 포함되는지 확인
                        foreach (var range in pauseRanges)
                        {
                            if (flightEntry.Timestamp >= range.Start && flightEntry.Timestamp <= range.End)
                            {
                                isPaused = true;
                                break;
                            }
                        }
                    }

                    // (2055년 보정 로직)
                    DateTime originalTimestamp = Epoch.AddMilliseconds(flightEntry.Timestamp).ToLocalTime();
                    DateTime displayTimestamp;
                    if (originalTimestamp.Year == 2055)
                    {
                        displayTimestamp = new DateTime(2025, originalTimestamp.Month, originalTimestamp.Day, originalTimestamp.Hour, originalTimestamp.Minute, originalTimestamp.Second, originalTimestamp.Millisecond, originalTimestamp.Kind);
                    }
                    else
                    {
                        displayTimestamp = originalTimestamp;
                    }

                    chartList.Add(new MissionDistChartItem
                    {
                        Timestamp = displayTimestamp,
                        AircraftID = uavId,
                        Status = isPaused ? 0 : 1 // 0 = 중지, 1 = 수행
                    });
                }
            }

            SelectedScenarioMissionChartData = new ObservableCollection<MissionDistChartItem>(chartList);

            // 4. [그리드] 데이터 채우기 (시간(초) 기준)
            var gridList = new List<MissionDistDetailItem>();
            foreach (var id in AIRCRAFT_FOR_TIMELINE) // (UAV 4,5,6)
            {
                string key = $"UAV{id}";

                // 총 운용시간 계산 (첫 타임스탬프 ~ 마지막 타임스탬프)
                var timestamps = _cachedScenarioData.FlightData
          .Where(fd => fd.AircraftID == id && fd.Timestamp > 0)
          .Select(fd => fd.Timestamp)
          .OrderBy(ts => ts)
          .ToList();

                ulong totalOperationMs = 0;
                if (timestamps.Count >= 2)
                {
                    totalOperationMs = timestamps.Last() - timestamps.First();
                }

                // 총 중지시간 계산
                ulong totalPauseMs = 0;
                if (pauseRangesDict.TryGetValue(id, out var pauses))
                {
                    foreach (var range in pauses)
                    {
                        if (range.End > range.Start)
                            totalPauseMs += (range.End - range.Start);
                    }
                }

                // 점수 계산
                double score = 100.0;
                if (totalOperationMs > 0)
                {
                    if (totalPauseMs >= totalOperationMs)
                        score = 0;
                    else
                        score = 100.0 * (double)(totalOperationMs - totalPauseMs) / totalOperationMs;
                }

                if (totalOperationMs > 0 || totalPauseMs > 0) // 둘 중 하나라도 데이터가 있으면 표시
                {
                    gridList.Add(new MissionDistDetailItem
                    {
                        AircraftID = key,
                        TotalOperationSeconds = totalOperationMs / 1000.0,
                        TotalPauseSeconds = totalPauseMs / 1000.0,
                        Score = score
                    });
                }
            }

            SelectedScenarioMissionGridData = new ObservableCollection<MissionDistDetailItem>(gridList);

            // 5. [트랙바] 설정 (UAV 4,5,6 데이터 기준)
            _currentScenarioTimestamps = uavFlightDataByAircraft
           .SelectMany(g => g)
           .Select(fd => fd.Timestamp)
           .Distinct()
           .OrderBy(ts => ts)
           .ToList();

            if (_currentScenarioTimestamps.Any())
            {
                int snapshotCount = _currentScenarioTimestamps.Count;
                MissionTrackBar.Minimum = 0;
                MissionTrackBar.Maximum = (snapshotCount > 0) ? snapshotCount - 1 : 0;
                MissionTrackBar.TickFrequency = 1;

                this.CurrentTimelineIndex = 0;

                UpdateMapIconsToTimelineIndex(0);
                UpdateTimestampLabel(0);
                UpdateChartCrosshair(0);
            }
            else
            {
                MissionTrackBar.Minimum = 0;
                MissionTrackBar.Maximum = 0;

                this.CurrentTimelineIndex = 0;
                UpdateMapIconsToTimelineIndex(0);
                UpdateTimestampLabel(0);
                UpdateChartCrosshair(0);
            }

            // 6. 차트 축 범위 적용
            ApplyChartRange();
        }
        private void ApplyChartRange()
        {
            // [방어 1] 차트가 아직 로드되지 않았으면 건너뜀
            if (MissionChart == null || !MissionChart.IsLoaded) return;

            if (MissionChart.Diagram is not XYDiagram2D diagram) return;
            var axisX = diagram.ActualAxisX;
            if (axisX == null) return;

            // [방어 2] 업데이트 중 렌더링 충돌 방지를 위한 잠금
            MissionChart.BeginInit();
            try
            {
                DateTime minTime;
                DateTime maxTime;

                if (!SelectedScenarioMissionChartData.Any())
                {
                    minTime = DateTime.Now;
                    maxTime = minTime.AddSeconds(1);
                }
                else
                {
                    minTime = SelectedScenarioMissionChartData.Min(dp => dp.Timestamp);
                    maxTime = SelectedScenarioMissionChartData.Max(dp => dp.Timestamp);
                    if (maxTime <= minTime) maxTime = minTime.AddSeconds(1);
                }

                // X축 (시간) 설정
                if (axisX.WholeRange != null)
                    axisX.WholeRange.SetMinMaxValues(minTime, maxTime);
                if (axisX.VisualRange != null)
                    axisX.VisualRange.SetMinMaxValues(minTime, maxTime);

                // Y축 설정
                if (diagram.ActualAxisY != null)
                {
                    if (diagram.ActualAxisY.WholeRange != null)
                        diagram.ActualAxisY.WholeRange.SetMinMaxValues(-0.5, 1.5);
                    if (diagram.ActualAxisY.VisualRange != null)
                        diagram.ActualAxisY.VisualRange.SetMinMaxValues(-1.5, 1.5);

                    diagram.ActualAxisY.CustomLabels.Clear();
                    diagram.ActualAxisY.CustomLabels.Add(new CustomAxisLabel(0, "Fail"));
                    diagram.ActualAxisY.CustomLabels.Add(new CustomAxisLabel(1, "Success"));
                }
            }
            finally
            {
                // [방어 2] 렌더링 잠금 해제
                MissionChart.EndInit();
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
                    if (uint.TryParse(lineSeries.DisplayName, out uint uavId))
                    {
                        // UAV 4(파랑), 5(빨강), 6(연두)
                        SolidColorBrush brush = Brushes.Gray; // 기본값
                        switch (uavId)
                        {
                            case 4: brush = Brushes.Blue; break; // UAV 4
                            case 5: brush = Brushes.Red; break; // UAV 5
                            case 6: brush = Brushes.LimeGreen; break; // UAV 6
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

        private void Chart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var chart = sender as ChartControl;
            if (chart == null || !chart.IsLoaded) return;

            // 1. 클릭한 위치의 정보(HitInfo)를 계산
            System.Windows.Point position = e.GetPosition(chart);
            ChartHitInfo hitInfo = chart.CalcHitInfo(position);

            // 2. 시리즈 포인트(점) 위를 클릭했거나, 다이어그램 내부를 클릭했을 때 처리
            // (정확히 점을 안 찍어도 해당 X축 위치로 이동하려면 InDiagram 조건도 포함하면 좋습니다)
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
