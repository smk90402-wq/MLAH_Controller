using DevExpress.Mvvm;
using DevExpress.Xpf.Charts;
using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static MLAH_LogAnalyzer.View_Safety;

namespace MLAH_LogAnalyzer
{
    // 내부 DTO
    internal class TargetPanelData
    {
        public ScenarioData ScenarioData { get; set; }
        public TargetAnalysisResult Result { get; set; }
    }

    public partial class View_Target : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private static readonly DateTime Epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // 바인딩 소스
        private ObservableCollection<TargetChartItem> _chartData = new ObservableCollection<TargetChartItem>();
        public ObservableCollection<TargetChartItem> SelectedScenarioTargetChartData { get => _chartData; set { _chartData = value; OnPropertyChanged(nameof(SelectedScenarioTargetChartData)); } }

        private ObservableCollection<TargetGridItem> _gridData = new ObservableCollection<TargetGridItem>();
        public ObservableCollection<TargetGridItem> SelectedScenarioTargetGridData { get => _gridData; set { _gridData = value; OnPropertyChanged(nameof(SelectedScenarioTargetGridData)); } }

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
                SelectedScenarioScore = scenarioToLoad.MissionSuccessScore;
        }

        private int _totalTargetCount;
        public int TotalTargetCount { get => _totalTargetCount; set { _totalTargetCount = value; OnPropertyChanged(nameof(TotalTargetCount)); } }

        // 트랙바
        private string _timeFmt = "--:--:---";
        public string CurrentSnapshotTimestampFormatted { get => _timeFmt; set { _timeFmt = value; OnPropertyChanged(nameof(CurrentSnapshotTimestampFormatted)); } }

        // [2026-03-24] 슬라이더 디바운스용 CancellationTokenSource (빠른 드래그 시 중간 프레임 스킵하여 렌더링 부하 감소)
        private CancellationTokenSource _sliderDebounceCts;

        private int _idx;
        public int CurrentTimelineIndex
        {
            get => _idx;
            set
            {
                if (_idx != value)
                {
                    _idx = value;
                    OnPropertyChanged(nameof(CurrentTimelineIndex));
                    UpdateTimestampLabel(value);
                    UpdateChartCrosshair(value);
                    UpdateMapIcons(value);
                }
            }
        }
        private List<ulong> _timestamps = new List<ulong>();
        private TargetAnalysisResult _cachedResult; // 트랙바 이동 시 사용

        private ScenarioData _cachedScenarioData;

        public View_Target()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private async void UpdateDisplayPanelsAsync()
        {
            var item = _selectedScenarioSummary;

            if (item == null)
            {
                return;
            }
            var splash = new DXSplashScreenViewModel()
            {
                Title = $"{item.ScenarioName} Loading...",
                Status = "유용성 평가 분석 데이터 동기화 중...",
                IsIndeterminate = true,
            };
            var parent = Window.GetWindow(this);
            if (parent == null) { UpdatePanels(null); return; }

            var manager = SplashScreenManager.CreateWaitIndicator(splash);
            TargetPanelData loaded = null;

            try
            {
                manager.Show(parent, WindowStartupLocation.CenterOwner, true, InputBlockMode.Window);

                if (item != null && item.OriginalItem != null)
                {
                    //splash.Status = $"Scenario {item.ScenarioNumber} 시각화 데이터 준비 중...";

                    // 이미 Main에서 계산된 결과를 가져옴
                    var preCalculatedResult = item.OriginalItem.TargetAnalysisResult;

                    // ✅ [수정] 경로 추출
                    string fullPath = item.OriginalItem.FullPath;
                    string baseDirectory = Path.GetDirectoryName(fullPath);

                    //loaded = await Task.Run(() => {
                    //    // ✅ [수정] 경로(baseDirectory)를 전달하여 로드
                    //    var data = Utils.LoadScenarioData(baseDirectory, item.ScenarioNumber);

                    //    return new TargetPanelData
                    //    {
                    //        ScenarioData = data,
                    //        Result = preCalculatedResult
                    //    };
                    //});
                    loaded = await Task.Run(async () => // 1. async 추가
                    {
                        // 2. await 추가
                        //var data = await Utils.LoadScenarioData(baseDirectory, item.ScenarioNumber);
                        var data = await Utils.LoadScenarioDataByPath(fullPath);

                        return new TargetPanelData { Result = preCalculatedResult, ScenarioData = data };
                    });
                }

                UpdatePanels(loaded);
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    // 3. 사람의 눈이 인식할 수 있도록 0.5초 정도 안전 딜레이 추가 (필요에 따라 조절)
                    await Task.Delay(800);

                    if (manager.State == SplashScreenState.Shown || manager.State == SplashScreenState.Showing)
                    {
                        manager.Close();
                    }
                }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                UpdatePanels(null);

                // 에러 시 즉시 종료 (마찬가지로 상태 확인 후 닫기)
                if (manager.State != SplashScreenState.Closed && manager.State != SplashScreenState.Closing)
                {
                    manager.Close();
                }
            }
            //finally { await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            //finally { await Task.Delay(300);
            //finally { manager.Close(); }
        }

        private void UpdatePanels(TargetPanelData data)
        {
            SelectedScenarioTargetChartData = new ObservableCollection<TargetChartItem>();
            SelectedScenarioTargetGridData = new ObservableCollection<TargetGridItem>();
            ViewModel_Unit_Map_Target.SingletonInstance.ClearMissionVisuals();
            _timestamps.Clear();

            if (data?.ScenarioData == null || data?.Result == null)
            {
                _cachedScenarioData = null;
                CurrentTimelineIndex = 0;
                TargetTrackBar.Maximum = 0;
                return;
            }

            _cachedScenarioData = data.ScenarioData;
            _cachedResult = data.Result;
            //var scenario = data.ScenarioData;
            var result = data.Result;

            // 1. 맵 업데이트
            ViewModel_Unit_Map_Target.SingletonInstance.BuildFlightDataIndex(_cachedScenarioData);
            ViewModel_Unit_Map_Target.SingletonInstance.UpdateMissionVisuals(_cachedScenarioData, result);

            // 2. 그리드 업데이트
            // grid cleared above
            if (result.GridDataList != null)
            {
                var gridItems = result.GridDataList.ToList();
                    SelectedScenarioTargetGridData = new ObservableCollection<TargetGridItem>(gridItems);
            }

            TotalTargetCount = result.TotalTargetCount;

            // 3. 차트 업데이트 [수정된 부분]
            var chartList = new List<TargetChartItem>();
            // TargetStateHistory는 Dictionary<int, Dictionary<long, int>> 구조입니다.
            foreach (var kvp in result.TargetStateHistory)
            {
                // Key가 uint이므로 int로 캐스팅 (TargetChartItem이 int ID를 쓴다고 가정)
                int targetId = (int)kvp.Key;
                var innerDict = kvp.Value;

                foreach (var pt in innerDict) // pt: Key=Timestamp(ulong), Value=State(int)
                {
                    chartList.Add(new TargetChartItem
                    {
                        Timestamp = Epoch.AddMilliseconds(pt.Key).ToLocalTime(),
                        TargetID = targetId,
                        State = pt.Value
                    });
                }
            }

            SelectedScenarioTargetChartData = new ObservableCollection<TargetChartItem>(chartList);

            // 4. 트랙바 설정
            _timestamps = _cachedScenarioData.FlightData.Select(f => f.Timestamp).Distinct().OrderBy(t => t).ToList();
            if (_timestamps.Any())
            {
                TargetTrackBar.Maximum = _timestamps.Count - 1;
                CurrentTimelineIndex = 0;
                UpdateMapIcons(0); 
                UpdateTimestampLabel(0); 
                UpdateChartCrosshair(0);
            }
            else
            {
                TargetTrackBar.Maximum = 0;
            }

            SetupChartAxis();
        }

        private void SetupChartAxis()
        {
            // [방어 1] 차트가 아직 로드되지 않았다면 축 설정을 건너뜀
            if (!TargetChart.IsLoaded) return;
            if (TargetChart.Diagram is not XYDiagram2D diagram) return;

            // [방어 2] 차트 렌더링 잠금
            TargetChart.BeginInit();
            try
            {
                // 1. Y축 설정 (0:미탐지 ~ 3:파괴)
                if (diagram.ActualAxisY != null)
                {
                    if (diagram.ActualAxisY.WholeRange != null)
                        diagram.ActualAxisY.WholeRange.SetMinMaxValues(-0.5, 3.5);
                    if (diagram.ActualAxisY.VisualRange != null)
                        diagram.ActualAxisY.VisualRange.SetMinMaxValues(-0.5, 3.5);

                    diagram.ActualAxisY.CustomLabels.Clear();
                    diagram.ActualAxisY.CustomLabels.Add(new CustomAxisLabel(0, "미탐지"));
                    diagram.ActualAxisY.CustomLabels.Add(new CustomAxisLabel(1, "탐지"));
                    diagram.ActualAxisY.CustomLabels.Add(new CustomAxisLabel(2, "식별"));
                    diagram.ActualAxisY.CustomLabels.Add(new CustomAxisLabel(3, "파괴"));
                }

                // 2. X축 설정 (전체 시간 범위 고정)
                if (diagram.ActualAxisX != null && _timestamps.Any())
                {
                    DateTime minTime = Epoch.AddMilliseconds(_timestamps.First()).ToLocalTime();
                    DateTime maxTime = Epoch.AddMilliseconds(_timestamps.Last()).ToLocalTime();

                    if (minTime == maxTime) maxTime = minTime.AddSeconds(1);

                    if (diagram.ActualAxisX.WholeRange != null)
                        diagram.ActualAxisX.WholeRange.SetMinMaxValues(minTime, maxTime);

                    if (diagram.ActualAxisX.VisualRange != null)
                        diagram.ActualAxisX.VisualRange.SetMinMaxValues(minTime, maxTime);
                }
            }
            finally
            {
                // [방어 2] 차트 렌더링 잠금 해제
                TargetChart.EndInit();
            }
        }

        private void UpdateMapIcons(int index)
        {
            // ✅ [수정] 캐시된 데이터가 없으면 리턴 (안전장치)
            if (_cachedScenarioData == null) return;

            if (_timestamps.Count <= index) return;

            // ❌ [삭제] 매번 파일 로드하던 코드 제거
            // var data = Utils.LoadScenarioData(_selectedScenarioSummary.ScenarioNumber);

            // ✅ [수정] 캐시된 데이터(_cachedScenarioData) 사용
            ViewModel_Unit_Map_Target.SingletonInstance.ShowPositionsAt(_timestamps[index], _cachedScenarioData, _cachedResult);
        }
        private void UpdateTimestampLabel(int index)
        {
            if (_timestamps.Count <= index) return;
            CurrentSnapshotTimestampFormatted = Epoch.AddMilliseconds(_timestamps[index]).ToLocalTime().ToString("HH:mm:ss.fff");
        }
        private void UpdateChartCrosshair(int index)
        {
            if (_timestamps.Count <= index) return;

            // [방어] 차트가 렌더링되었고 "화면에 보일 때만" 크로스헤어 표시
            if (TargetChart.IsLoaded && TargetChart.IsVisible && TargetChart.Diagram is XYDiagram2D d)
            {
                d.ShowCrosshair(Epoch.AddMilliseconds(_timestamps[index]).ToLocalTime(), null);
            }
        }

        private void ScoreChart_CustomDrawCrosshair(object sender, CustomDrawCrosshairEventArgs e)
        {
            // 현재 이벤트가 발생한 차트 확인
            var chart = sender as ChartControl;

            foreach (var element in e.CrosshairElements)
            {
                if (element.SeriesPoint != null)
                {
                    // 1. 현재 값(Value) 가져오기
                    double val = element.SeriesPoint.Value;

                    string statusText = val switch
                    {
                        //3 => "파괴",
                        //2 => "탐지",
                        //1 => "식별",
                        //_ => "미식별"
                        3 => "파괴 (100pt)",
                        2 => "식별 (70pt)",
                        1 => "탐지 (50pt)",
                        _ => "미식별 (0pt)"
                    };


                    //string argu = element.SeriesPoint.Argument;
                    //CrosshairLabelPattern = "Target {S} : {V}"

                    //string SDM = chart.Diagram.SeriesDataMember;
                    string seriesName = element.Series.DisplayName;


                    // 3. 라벨 텍스트 덮어쓰기
                    // 형식: "UAV 1: Success"
                    element.LabelElement.Text = $"{seriesName}: {statusText}";
                }
            }
        }

        // ---------------------------------------------------------
        // 차트 클릭 시 트랙바/지도 동기화 로직
        // ---------------------------------------------------------
        private void Chart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var chart = sender as ChartControl;

            // [방어] 차트가 없거나 아직 로드되지 않았다면 무시
            if (chart == null || !chart.IsLoaded) return;

            // 1. 클릭한 위치의 정보(HitInfo)를 계산
            Point position = e.GetPosition(chart);
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
                if (bestIndex >= 0 && bestIndex < _timestamps.Count)
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
            if (_timestamps == null || _timestamps.Count == 0) return -1;

            // 1. DateTime -> ulong Timestamp 변환 (기존 Epoch 로직 역산)
            // Epoch는 UTC 기준이므로, LocalTime으로 들어온 targetDateTime을 UTC로 바꿔서 계산해야 함
            ulong targetTs = (ulong)(targetDateTime.ToUniversalTime() - Epoch).TotalMilliseconds;

            // 2. 가장 차이가 적은 인덱스 탐색
            int bestIndex = -1;
            ulong minDiff = ulong.MaxValue;

            for (int i = 0; i < _timestamps.Count; i++)
            {
                // ulong이라 음수가 나올 수 없으므로 절대값 처리 주의 (큰 수 - 작은 수)
                ulong currentTs = _timestamps[i];
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