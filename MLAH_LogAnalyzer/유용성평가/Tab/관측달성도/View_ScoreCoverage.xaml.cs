// 필수 using 문들
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



    public partial class View_ScoreCoverage : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion

        internal class CoveragePanelData
        {
            public AnalysisResult AnalysisResult { get; set; }
            public ScenarioData ScenarioData { get; set; }
        }
        private static readonly DateTime Epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private List<ulong> _timestampsUAV4 = new List<ulong>();
        private List<ulong> _timestampsUAV5 = new List<ulong>();
        private List<ulong> _timestampsUAV6 = new List<ulong>();

        #region TrackBar Properties (UAV 4, 5, 6)

        // ========== UAV 4 ==========
        private bool _uav4TrackBarEnabled;
        public bool UAV4TrackBarEnabled
        { get => _uav4TrackBarEnabled; set { _uav4TrackBarEnabled = value; OnPropertyChanged(nameof(UAV4TrackBarEnabled)); } }

        private int _maxIndexUAV4;
        public int MaxIndexUAV4
        { get => _maxIndexUAV4; set { _maxIndexUAV4 = value; OnPropertyChanged(nameof(MaxIndexUAV4)); } }

        private int _currentIndexUAV4;
        public int CurrentIndexUAV4
        {
            get => _currentIndexUAV4;
            set
            {
                if (_currentIndexUAV4 != value)
                {
                    _currentIndexUAV4 = value;
                    OnPropertyChanged(nameof(CurrentIndexUAV4));
                    UpdateUAVState(4, _timestampsUAV4, value, (timeStr) => CurrentSnapshotTimestampFormatted = timeStr);
                }
            }
        }

        // ========== UAV 5 ==========
        private bool _uav5TrackBarEnabled;
        public bool UAV5TrackBarEnabled
        { get => _uav5TrackBarEnabled; set { _uav5TrackBarEnabled = value; OnPropertyChanged(nameof(UAV5TrackBarEnabled)); } }

        private int _maxIndexUAV5;
        public int MaxIndexUAV5
        { get => _maxIndexUAV5; set { _maxIndexUAV5 = value; OnPropertyChanged(nameof(MaxIndexUAV5)); } }

        private int _currentIndexUAV5;
        public int CurrentIndexUAV5
        {
            get => _currentIndexUAV5;
            set
            {
                if (_currentIndexUAV5 != value)
                {
                    _currentIndexUAV5 = value;
                    OnPropertyChanged(nameof(CurrentIndexUAV5));
                    UpdateUAVState(5, _timestampsUAV5, value, (timeStr) => CurrentSnapshotTimestampFormatted1 = timeStr);
                }
            }
        }

        // ========== UAV 6 ==========
        private bool _uav6TrackBarEnabled;
        public bool UAV6TrackBarEnabled
        { get => _uav6TrackBarEnabled; set { _uav6TrackBarEnabled = value; OnPropertyChanged(nameof(UAV6TrackBarEnabled)); } }

        private int _maxIndexUAV6;
        public int MaxIndexUAV6
        { get => _maxIndexUAV6; set { _maxIndexUAV6 = value; OnPropertyChanged(nameof(MaxIndexUAV6)); } }

        private int _currentIndexUAV6;
        public int CurrentIndexUAV6
        {
            get => _currentIndexUAV6;
            set
            {
                if (_currentIndexUAV6 != value)
                {
                    _currentIndexUAV6 = value;
                    OnPropertyChanged(nameof(CurrentIndexUAV6));
                    UpdateUAVState(6, _timestampsUAV6, value, (timeStr) => CurrentSnapshotTimestampFormatted2 = timeStr);
                }
            }
        }

        #endregion

        // 타임스탬프 레이블 (화면에 시간 표시용)
        private string _currentSnapshotTimestampFormatted = "--:--:---";
        public string CurrentSnapshotTimestampFormatted { get => _currentSnapshotTimestampFormatted; set { _currentSnapshotTimestampFormatted = value; OnPropertyChanged(nameof(CurrentSnapshotTimestampFormatted)); } }

        private string _currentSnapshotTimestampFormatted1 = "--:--:---";
        public string CurrentSnapshotTimestampFormatted1 { get => _currentSnapshotTimestampFormatted1; set { _currentSnapshotTimestampFormatted1 = value; OnPropertyChanged(nameof(CurrentSnapshotTimestampFormatted1)); } }

        private string _currentSnapshotTimestampFormatted2 = "--:--:---";
        public string CurrentSnapshotTimestampFormatted2 { get => _currentSnapshotTimestampFormatted2; set { _currentSnapshotTimestampFormatted2 = value; OnPropertyChanged(nameof(CurrentSnapshotTimestampFormatted2)); } }

        private uint _SelectedScenarioScore = 0;
        public uint SelectedScenarioScore
        {
            get => _SelectedScenarioScore;
            set { _SelectedScenarioScore = value; OnPropertyChanged(nameof(SelectedScenarioScore)); }
        }

        /// <summary>
        /// 차트(시간별 관측 달성도) 데이터 소스
        /// </summary>
        //public ObservableCollection<CoverageChartDataPoint> SelectedScenarioChartData { get; set; } = new();

        private ObservableCollection<CoverageChartDataPoint> _SelectedScenarioChartData = new ObservableCollection<CoverageChartDataPoint>();
        /// <summary>
        /// '시나리오 선택' 그리드에서 선택된 항목
        /// </summary>
        public ObservableCollection<CoverageChartDataPoint> SelectedScenarioChartData
        {
            get => _SelectedScenarioChartData;
            set
            {
                _SelectedScenarioChartData = value;
                OnPropertyChanged(nameof(SelectedScenarioChartData));
            }
        }

        /// <summary>
        /// 하단 그리드(촬영 면적) 데이터 소스
        /// </summary>
        private ObservableCollection<CoverageDetailItem> _SelectedScenarioCoverageData = new ObservableCollection<CoverageDetailItem>();
        public ObservableCollection<CoverageDetailItem> SelectedScenarioCoverageData
        {
            get => _SelectedScenarioCoverageData;
            set { _SelectedScenarioCoverageData = value; OnPropertyChanged(nameof(SelectedScenarioCoverageData)); }
        }

        private ScoreScenarioSummary _selectedScenarioSummary;
        /// <summary>
        /// '시나리오 선택' 그리드에서 선택된 항목
        /// </summary>
        public ScoreScenarioSummary SelectedScenarioSummary
        {
            get => _selectedScenarioSummary;
            set
            {
                _selectedScenarioSummary = value;
                OnPropertyChanged(nameof(SelectedScenarioSummary));

                // [핵심] 선택이 변경되면 이 뷰가 직접 데이터 갱신을 트리거합니다.
                //UpdateCoverageDisplays();
                UpdateCoverageDisplaysAsync();
                UpdateCoverageScore();

            }
        }

        public View_ScoreCoverage()
        {
            InitializeComponent();
            this.DataContext = this;

        }

        private bool _isMissionAreaVisible = true;
        public bool IsMissionAreaVisible
        {
            get => _isMissionAreaVisible;
            set
            {
                if (_isMissionAreaVisible != value)
                {
                    _isMissionAreaVisible = value;
                    OnPropertyChanged(nameof(IsMissionAreaVisible));
                    TogglePolygonVisibility("Mission", value);
                }
            }
        }

        private bool _isFilmedAreaVisible = true;
        public bool IsFilmedAreaVisible
        {
            get => _isFilmedAreaVisible;
            set
            {
                if (_isFilmedAreaVisible != value)
                {
                    _isFilmedAreaVisible = value;
                    OnPropertyChanged(nameof(IsFilmedAreaVisible));
                    TogglePolygonVisibility("Filmed", value);
                }
            }
        }

        private void TogglePolygonVisibility(string tagPrefix, bool isVisible)
        {
            // UI 업데이트이므로 Dispatcher 사용
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                var mapInstance = ViewModel_Unit_Map.SingletonInstance;
                if (mapInstance == null) return;

                foreach (var polygon in mapInstance.CoveragePolygons)
                {
                    if (polygon.Tag != null && polygon.Tag.ToString().StartsWith(tagPrefix))
                    {
                        polygon.Visible = isVisible;
                    }
                }
            });
        }

        private async void UpdateCoverageDisplaysAsync()
        {
            // 1. 로드할 대상 가져오기
            var scenarioToLoad = _selectedScenarioSummary;
            if (scenarioToLoad == null) return;

            var parentWindow = Window.GetWindow(this);
            if (parentWindow == null) { UpdateCoverageDisplays(null); return; }

            // 2. 스플래시 스크린 뷰모델 정의
            var splashViewModel = new DXSplashScreenViewModel()
            {
                Title = $"{scenarioToLoad.ScenarioName} Loading...",
                Status = "유용성 평가 분석 데이터 동기화 중...",
                IsIndeterminate = true,
            };

            // 4. 스플래시 스크린 매니저 생성
            var manager = SplashScreenManager.CreateWaitIndicator(splashViewModel);

            CoveragePanelData loadedData = null;

            try
            {
                // 5. 스플래시 스크린 표시
                manager.Show(parentWindow, WindowStartupLocation.CenterOwner, true, InputBlockMode.Window);

                if (scenarioToLoad != null)
                {
                    //splashViewModel.Status = $"Scenario {scenarioToLoad.ScenarioNumber} 데이터 로드 중...";

                    string fullPath = scenarioToLoad.OriginalItem.FullPath;
                    string baseDirectory = Path.GetDirectoryName(fullPath);

             
                    loadedData = await Task.Run(async () => // 1. async 추가
                    {
                        // 2. await 추가
                        //var data = await Utils.LoadScenarioData(baseDirectory, scenarioToLoad.ScenarioNumber);
                        var data = await Utils.LoadScenarioDataByPath(fullPath);

                        var analysis = scenarioToLoad.OriginalItem.CoverageAnalysisResult;
                        return new CoveragePanelData { AnalysisResult = analysis, ScenarioData = data };
                    });
                }

                UpdateCoverageDisplays(loadedData);
            }
            catch (Exception ex)
            {
                MessageBox.Show(parentWindow, $"데이터 로드 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateCoverageDisplays(null); // 오류 시 UI 클리어
            }
            finally
            {
                this.UpdateLayout();
                await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                await Task.Delay(300);
                manager.Close();
            }
        }
        private async void UpdateCoverageScore()
        {
            var scenarioToLoad = _selectedScenarioSummary;
            if (scenarioToLoad != null)
                SelectedScenarioScore = scenarioToLoad.CoverageScore;
        }

        private AnalysisResult _currentAnalysisResult;
        private ScenarioData _currentScenarioData;

        private async void UpdateCoverageDisplays(CoveragePanelData loadedData)
        {
            // 1. 초기화 (일괄 교체 방식 - 빈 컬렉션으로 교체)
            SelectedScenarioChartData = new ObservableCollection<CoverageChartDataPoint>();
            SelectedScenarioCoverageData = new ObservableCollection<CoverageDetailItem>();
            ViewModel_Unit_Map.SingletonInstance.ClearEvaluationData();
            ResetUAVControls();

            AnalysisResult analysisResult = loadedData?.AnalysisResult;
            ScenarioData scenarioData = loadedData?.ScenarioData;

            if (loadedData != null)
            {
                _currentAnalysisResult = loadedData.AnalysisResult;
                _currentScenarioData = loadedData.ScenarioData;
            }
            else
            {
                _currentAnalysisResult = null;
                _currentScenarioData = null;
            }

            if (analysisResult != null && scenarioData != null)
            {
                // 2. 차트 데이터 채우기 (일괄 교체 - 렌더링 1회)
                var chartList = analysisResult.CoverageDatas.Select(dataPoint =>
                {
                    DateTime originalTimestamp = Epoch.AddMilliseconds(dataPoint.Timestamp).ToLocalTime();
                    DateTime displayTimestamp = (originalTimestamp.Year == 2055)
                        ? new DateTime(2025, originalTimestamp.Month, originalTimestamp.Day, originalTimestamp.Hour, originalTimestamp.Minute, originalTimestamp.Second, originalTimestamp.Millisecond, originalTimestamp.Kind)
                        : originalTimestamp;

                    return new CoverageChartDataPoint
                    {
                        Timestamp = displayTimestamp,
                        MissionSegmentID = dataPoint.MissionSegmentID,
                        Coverage = dataPoint.Coverage
                    };
                }).ToList();

                SelectedScenarioChartData = new ObservableCollection<CoverageChartDataPoint>(chartList);

                // 3. 그리드 데이터 채우기 (일괄 교체 - 렌더링 1회)
                if (analysisResult.MissionSegmentDatas != null)
                {
                    var gridList = analysisResult.MissionSegmentDatas.Select(kvp => new CoverageDetailItem
                    {
                        MissionSegmentID = kvp.Key,
                        Coverage = kvp.Value.Coverage,
                        FilmedArea = kvp.Value.FilmedArea,
                        RequiredArea = kvp.Value.RequiredArea
                    }).ToList();

                    SelectedScenarioCoverageData = new ObservableCollection<CoverageDetailItem>(gridList);
                }

                // 4. [핵심] 각 UAV별 독립 트랙바 설정
                if (analysisResult.UavInfos != null)
                {
                    // UAV 4 설정
                    SetupUAVTrackbar(4, analysisResult, ref _timestampsUAV4,
                        (max) => MaxIndexUAV4 = max,
                        (idx) => CurrentIndexUAV4 = idx,
                        (enabled) => UAV4TrackBarEnabled = enabled);

                    // UAV 5 설정
                    SetupUAVTrackbar(5, analysisResult, ref _timestampsUAV5,
                        (max) => MaxIndexUAV5 = max,
                        (idx) => CurrentIndexUAV5 = idx,
                        (enabled) => UAV5TrackBarEnabled = enabled);

                    // UAV 6 설정
                    SetupUAVTrackbar(6, analysisResult, ref _timestampsUAV6,
                        (max) => MaxIndexUAV6 = max,
                        (idx) => CurrentIndexUAV6 = idx,
                        (enabled) => UAV6TrackBarEnabled = enabled);

                    if (UAV4TrackBarEnabled) UpdateUAVState(4, _timestampsUAV4, 0, (t) => CurrentSnapshotTimestampFormatted = t);
                    if (UAV5TrackBarEnabled) UpdateUAVState(5, _timestampsUAV5, 0, (t) => CurrentSnapshotTimestampFormatted1 = t);
                    if (UAV6TrackBarEnabled) UpdateUAVState(6, _timestampsUAV6, 0, (t) => CurrentSnapshotTimestampFormatted2 = t);
                }
            }

            if (analysisResult != null && scenarioData != null)
            {
                // 1. 맵에 폴리곤 렌더링
                await ViewModel_Unit_Map.SingletonInstance.BuildMapCoveragePolygonsAsync(analysisResult, scenarioData);

                // 2. 그려지자마자 현재 체크박스 상태를 읽어서 숨길 건 숨김
                TogglePolygonVisibility("Mission", IsMissionAreaVisible);
                TogglePolygonVisibility("Filmed", IsFilmedAreaVisible);
            }

            ApplyChartRange();
        }

        private void AreaGrid_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            if (e.NewItem is CoverageDetailItem selectedItem)
            {
                // 새로 만든 색상 변경 함수만 호출 (0.01초 만에 즉시 바뀜)
                ViewModel_Unit_Map.SingletonInstance.HighlightSegment(selectedItem.MissionSegmentID);
            }
            else
            {
                // 선택 해제 시 전체 선택 상태(또는 전부 회색)로 복귀
                ViewModel_Unit_Map.SingletonInstance.HighlightSegment(null);
            }
        }

        private void SetupUAVTrackbar(int uavId, AnalysisResult result, ref List<ulong> timestamps,
            Action<int> setMax, Action<int> setCurrent, Action<bool> setEnabled)
        {
            var uavInfo = result.UavInfos.FirstOrDefault(u => u.UAVID == uavId);

            if (uavInfo != null && uavInfo.Snapshots.Any())
            {
                timestamps = uavInfo.Snapshots.Select(s => s.Timestamp).OrderBy(t => t).ToList();
                setMax(timestamps.Count - 1);
                setEnabled(true); // 활성화
                setCurrent(0);    // 0번으로 이동 (지도에 표시됨)
            }
            else
            {
                timestamps = new List<ulong>();
                setCurrent(0);
                setMax(0);
                setEnabled(false); // 비활성화
            }
        }

        private void UpdateUAVState(int uavId, List<ulong> timestamps, int index, Action<string> updateLabel)
        {
            if (timestamps == null || !timestamps.Any() || index < 0 || index >= timestamps.Count)
            {
                updateLabel("--:--:---");
                return;
            }

            ulong currentTimestamp = timestamps[index];

            // [!!!] 수정: Duration 대신 절대 시간(HH:mm:ss.fff) 표시 [!!!]
            // (차트와 동일한 2055년 -> 2025년 보정 로직 적용)
            DateTime originalTimestamp = Epoch.AddMilliseconds(currentTimestamp).ToLocalTime();
            DateTime displayTimestamp;

            if (originalTimestamp.Year == 2055)
            {
                displayTimestamp = new DateTime(2025, originalTimestamp.Month, originalTimestamp.Day,
                                                originalTimestamp.Hour, originalTimestamp.Minute, originalTimestamp.Second,
                                                originalTimestamp.Millisecond, originalTimestamp.Kind);
            }
            else
            {
                displayTimestamp = originalTimestamp;
            }

            // 레이블 업데이트 (시:분:초.밀리초)
            updateLabel(displayTimestamp.ToString("HH:mm:ss.fff"));

            // 2. 맵 업데이트 (해당 UAV만 갱신)
            var analysisResult = _selectedScenarioSummary?.OriginalItem?.CoverageAnalysisResult;
            var uavInfo = analysisResult?.UavInfos.FirstOrDefault(u => u.UAVID == uavId);
            //if (uavInfo != null)
            //{
            //    // 정확한 타임스탬프의 스냅샷 찾기
            //    var snapshot = uavInfo.Snapshots.FirstOrDefault(s => s.Timestamp == currentTimestamp);
            //    if (snapshot != null)
            //    {
            //        ViewModel_Unit_Map.SingletonInstance.UpdateSpecificUavSnapshot(snapshot, uavId);
            //    }
            //}

            if (uavInfo != null)
            {

                if (index < uavInfo.Snapshots.Count)
                {
                    var snapshot = uavInfo.Snapshots[index]; 

                    // 혹시 모르니 타임스탬프가 맞는지 이중 체크 
                    if (snapshot.Timestamp == currentTimestamp)
                    {
                        ViewModel_Unit_Map.SingletonInstance.UpdateSpecificUavSnapshot(snapshot, uavId);
                    }
                }
            }

            // 현재 시나리오 데이터가 있고, 리얼 타겟 데이터가 존재하는지 확인
            if (_currentScenarioData != null && _currentScenarioData.RealTargetData != null)
            {
                // 3개의 트랙바 중 현재 움직이고 있는 트랙바의 시간(currentTimestamp)을 기준으로 표적 위치 갱신
                ViewModel_Unit_Map.SingletonInstance.UpdateTargetsAt(currentTimestamp, _currentScenarioData.RealTargetData);
            }

            // 3. 차트 크로스헤어 동기화
            // (이미 위에서 계산한 displayTimestamp를 사용하면 정확도가 보장됨)
            //UpdateChartCrosshairByDateTime(displayTimestamp);
            UpdateChartCrosshairByTimestamp(currentTimestamp);
        }

      

        private void UpdateChartCrosshairByTimestamp(ulong timestamp)
        {
            if (timestamp == 0) return;

            // 비동기로 실행하여 슬라이더 드래그 시 UI 차단 방지
            Dispatcher.BeginInvoke(() =>
            {
                if (ScoreChart == null || !ScoreChart.IsLoaded || !ScoreChart.IsVisible) return;
                // 1. 시간 변환 (기존 로직 동일)
                DateTime originalTimestamp = Epoch.AddMilliseconds(timestamp).ToLocalTime();
                DateTime displayTimestamp = (originalTimestamp.Year == 2055)
                    ? new DateTime(2025, originalTimestamp.Month, originalTimestamp.Day,
                        originalTimestamp.Hour, originalTimestamp.Minute, originalTimestamp.Second,
                        originalTimestamp.Millisecond, originalTimestamp.Kind)
                    : originalTimestamp;

                var diagram = ScoreChart.Diagram as XYDiagram2D;
                if (diagram != null)
                {
                    // 2. [중요] 해당 시간이 현재 눈에 보이는 범위(VisualRange) 안에 있는지 확인하고 이동
                    if (diagram.ActualAxisX.VisualRange != null)
                    {
                        DateTime minVis = (DateTime)diagram.ActualAxisX.VisualRange.MinValue;
                        DateTime maxVis = (DateTime)diagram.ActualAxisX.VisualRange.MaxValue;

                        // 화면 밖이면 중심으로 이동
                        if (displayTimestamp < minVis || displayTimestamp > maxVis)
                        {
                            TimeSpan rangeSpan = maxVis - minVis;
                            DateTime newMin = displayTimestamp.AddMilliseconds(-rangeSpan.TotalMilliseconds / 2);
                            DateTime newMax = displayTimestamp.AddMilliseconds(rangeSpan.TotalMilliseconds / 2);
                            diagram.ActualAxisX.VisualRange.SetMinMaxValues(newMin, newMax);
                        }
                    }

                    // 3. Crosshair 표시
                    // X축이 시간이므로 첫 번째 인자에 시간, 두 번째 인자(Y값)는 null 대신 
                    // Y축의 중간값이나 0을 주어서라도 선이 나오게 시도해볼 수 있습니다.
                    // 하지만 보통 (Argument, null) 패턴이 맞으므로, 스레드 문제일 가능성이 큽니다.

                    diagram.ShowCrosshair(displayTimestamp, null);

                    // 만약 위 코드로 안 되면 아래처럼 특정 Series를 지정해서 호출해보세요.
                    // diagram.ShowCrosshair(displayTimestamp, null, diagram.Series[0]); 
                }
            });
        }


        private void ResetUAVControls()
        {
            CurrentIndexUAV4 = 0; MaxIndexUAV4 = 0; UAV4TrackBarEnabled = false; CurrentSnapshotTimestampFormatted = "--:--:---";
            CurrentIndexUAV5 = 0; MaxIndexUAV5 = 0; UAV5TrackBarEnabled = false; CurrentSnapshotTimestampFormatted1 = "--:--:---";
            CurrentIndexUAV6 = 0; MaxIndexUAV6 = 0; UAV6TrackBarEnabled = false; CurrentSnapshotTimestampFormatted2 = "--:--:---";
        }


        private void ApplyChartRange()
        {
            // [방어 1] 차트가 아직 로드되지 않았으면 건너뜀 (XAML의 차트 이름이 ScoreChart인 경우)
            if (ScoreChart == null || !ScoreChart.IsLoaded) return;
            if (ScoreChart.Diagram is not XYDiagram2D diagram || axisX == null) return;

            // [방어 2] 업데이트 중 렌더링 충돌 방지를 위한 잠금
            ScoreChart.BeginInit();
            try
            {
                DateTime minTime;
                DateTime maxTime;

                if (!SelectedScenarioChartData.Any())
                {
                    minTime = DateTime.Now;
                    maxTime = minTime.AddSeconds(1);
                }
                else
                {
                    minTime = SelectedScenarioChartData.Min(dp => dp.Timestamp);
                    maxTime = SelectedScenarioChartData.Max(dp => dp.Timestamp);
                    if (maxTime <= minTime) maxTime = minTime.AddSeconds(1);
                }

                if (axisX.WholeRange != null)
                    axisX.WholeRange.SetMinMaxValues(minTime, maxTime);
                if (axisX.VisualRange != null)
                    axisX.VisualRange.SetMinMaxValues(minTime, maxTime);

                // Y축 (Coverage) 범위 설정
                if (diagram.ActualAxisY != null)
                {
                    double maxY = 100.0;

                    if (SelectedScenarioChartData.Any())
                    {
                        double dataMax = SelectedScenarioChartData.Max(dp => dp.Coverage);
                        maxY = dataMax > 0 ? dataMax * 1.1 : 100.0;
                        if (maxY > 110) maxY = 110;
                    }

                    if (diagram.ActualAxisY.WholeRange != null)
                        diagram.ActualAxisY.WholeRange.SetMinMaxValues(0, maxY);
                    if (diagram.ActualAxisY.VisualRange != null)
                        diagram.ActualAxisY.VisualRange.SetMinMaxValues(0, maxY);
                }
            }
            finally
            {
                // [방어 2] 렌더링 잠금 해제
                ScoreChart.EndInit();
            }
        }

        private void Chart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var chart = sender as ChartControl;
            if (chart == null || !chart.IsLoaded) return;

            // 1. 클릭한 위치의 정보(HitInfo)를 계산
            System.Windows.Point position = e.GetPosition(chart);
            ChartHitInfo hitInfo = chart.CalcHitInfo(position);

            if (hitInfo != null && (hitInfo.SeriesPoint != null || hitInfo.InDiagram))
            {
                DateTime clickedTime;

                // A. 점을 정확히 클릭한 경우
                if (hitInfo.SeriesPoint != null)
                {
                    clickedTime = hitInfo.SeriesPoint.DateTimeArgument;
                }
                // B. 점이 아닌 빈 공간(Diagram)을 클릭한 경우
                else
                {
                    var diagram = chart.Diagram as XYDiagram2D;
                    if (diagram == null) return;

                    var coords = diagram.PointToDiagram(position);
                    if (coords == null || coords.DateTimeArgument == DateTime.MinValue) return;

                    clickedTime = coords.DateTimeArgument;
                }

                // 3. [핵심 수정] 각 UAV별로 가장 가까운 인덱스를 찾아 개별 업데이트
                // _timestamps 단일 변수 대신 _ts4, _ts5, _ts6를 각각 사용
                SyncTrackbarToTime(4, _timestampsUAV4, clickedTime);
                SyncTrackbarToTime(5, _timestampsUAV5, clickedTime);
                SyncTrackbarToTime(6, _timestampsUAV6, clickedTime);
            }
        }

        // [신규] 특정 UAV의 트랙바를 지정된 시간에 맞춤
        private void SyncTrackbarToTime(int uavId, List<ulong> timestamps, DateTime targetTime)
        {
            // 해당 UAV 데이터가 없거나 비활성화 상태면 패스
            if (timestamps == null || timestamps.Count == 0) return;

            int bestIndex = FindClosestTimestampIndex(timestamps, targetTime);

            if (bestIndex >= 0)
            {
                switch (uavId)
                {
                    case 4: CurrentIndexUAV4 = bestIndex; break;
                    case 5: CurrentIndexUAV5 = bestIndex; break;
                    case 6: CurrentIndexUAV6 = bestIndex; break;
                }
            }
        }

        // [수정] 인자로 타임스탬프 리스트를 받도록 변경
        private int FindClosestTimestampIndex(List<ulong> timestampList, DateTime targetDateTime)
        {
            if (timestampList == null || timestampList.Count == 0) return -1;

            // DateTime -> ulong 변환 (UTC 기준)
            ulong targetTs = (ulong)(targetDateTime.ToUniversalTime() - Epoch).TotalMilliseconds;

            int bestIndex = -1;
            ulong minDiff = ulong.MaxValue;

            for (int i = 0; i < timestampList.Count; i++)
            {
                ulong currentTs = timestampList[i];
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
