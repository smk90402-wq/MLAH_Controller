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
    public partial class View_SR : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion

        internal class SRPanelData
        {
            public SpatialResolutionResult Result { get; set; }
            public ScenarioData Scenario { get; set; }
        }
        private static readonly DateTime Epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Data Sources
        private ObservableCollection<SRChartItem> _selectedScenarioSRChartData = new ObservableCollection<SRChartItem>();
        public ObservableCollection<SRChartItem> SelectedScenarioSRChartData { get => _selectedScenarioSRChartData; set { _selectedScenarioSRChartData = value; OnPropertyChanged(nameof(SelectedScenarioSRChartData)); } }
        private ObservableCollection<SRGridItem> _selectedScenarioSRGridData = new ObservableCollection<SRGridItem>();
        public ObservableCollection<SRGridItem> SelectedScenarioSRGridData { get => _selectedScenarioSRGridData; set { _selectedScenarioSRGridData = value; OnPropertyChanged(nameof(SelectedScenarioSRGridData)); } }

        // Threshold Binding
        private float _srThreshold;
        public float SRThreshold { get => _srThreshold; set { _srThreshold = value; OnPropertyChanged(nameof(SRThreshold)); } }

        // ---------------- UAV 4 ----------------
        private bool _uav4Enabled;
        public bool UAV4TrackBarEnabled
        {
            get => _uav4Enabled;
            set { _uav4Enabled = value; OnPropertyChanged(nameof(UAV4TrackBarEnabled)); }
        }

        private int _max4;
        public int MaxIndexUAV4
        {
            get => _max4;
            set { _max4 = value; OnPropertyChanged(nameof(MaxIndexUAV4)); }
        }

        private int _curr4;
        public int CurrentIndexUAV4
        {
            get => _curr4;
            set
            {
                if (_curr4 != value)
                {
                    _curr4 = value;
                    OnPropertyChanged(nameof(CurrentIndexUAV4));
                    UpdateUAV(4, value, s => CurrentSnapshotTimestampFormatted = s);
                }
            }
        }

        // ---------------- UAV 5 ----------------
        private bool _uav5Enabled;
        public bool UAV5TrackBarEnabled
        {
            get => _uav5Enabled;
            set { _uav5Enabled = value; OnPropertyChanged(nameof(UAV5TrackBarEnabled)); }
        }

        private int _max5;
        public int MaxIndexUAV5
        {
            get => _max5;
            set { _max5 = value; OnPropertyChanged(nameof(MaxIndexUAV5)); }
        }

        private int _curr5;
        public int CurrentIndexUAV5
        {
            get => _curr5;
            set
            {
                if (_curr5 != value)
                {
                    _curr5 = value;
                    OnPropertyChanged(nameof(CurrentIndexUAV5));
                    UpdateUAV(5, value, s => CurrentSnapshotTimestampFormatted1 = s);
                }
            }
        }

        // ---------------- UAV 6 ----------------
        private bool _uav6Enabled;
        public bool UAV6TrackBarEnabled
        {
            get => _uav6Enabled;
            set { _uav6Enabled = value; OnPropertyChanged(nameof(UAV6TrackBarEnabled)); }
        }

        private int _max6;
        public int MaxIndexUAV6
        {
            get => _max6;
            set { _max6 = value; OnPropertyChanged(nameof(MaxIndexUAV6)); }
        }

        private int _curr6;
        public int CurrentIndexUAV6
        {
            get => _curr6;
            set
            {
                if (_curr6 != value)
                {
                    _curr6 = value;
                    OnPropertyChanged(nameof(CurrentIndexUAV6));
                    UpdateUAV(6, value, s => CurrentSnapshotTimestampFormatted2 = s);
                }
            }
        }

        private string _time1, _time2, _time3;
        public string CurrentSnapshotTimestampFormatted { get => _time1; set { _time1 = value; OnPropertyChanged(nameof(CurrentSnapshotTimestampFormatted)); } }
        public string CurrentSnapshotTimestampFormatted1 { get => _time2; set { _time2 = value; OnPropertyChanged(nameof(CurrentSnapshotTimestampFormatted1)); } }
        public string CurrentSnapshotTimestampFormatted2 { get => _time3; set { _time3 = value; OnPropertyChanged(nameof(CurrentSnapshotTimestampFormatted2)); } }

        private List<ulong> _ts4 = new(), _ts5 = new(), _ts6 = new();
        private SpatialResolutionResult _cachedResult;
        private ScenarioData _cachedScenario;

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
                SelectedScenarioScore = scenarioToLoad.SpatialResScore;
        }
        public View_SR()
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
            SRPanelData loaded = null;

            try
            {
                manager.Show(parent, WindowStartupLocation.CenterOwner, true, InputBlockMode.Window);
                if (item != null && item.OriginalItem != null)
                {
                    //splash.Status = $"Scenario {item.ScenarioNumber} 준비 중...";

                    // ✅ [수정] 경로 추출
                    string fullPath = item.OriginalItem.FullPath;
                    string baseDirectory = Path.GetDirectoryName(fullPath);

                    var result = item.OriginalItem.SpatialResResult;

                    //loaded = await Task.Run(() => {
                    //    // ✅ [수정] 경로 전달
                    //    var data = Utils.LoadScenarioData(baseDirectory, item.ScenarioNumber);
                    //    return new SRPanelData { Scenario = data, Result = result };
                    //});
                    loaded = await Task.Run(async () => // 1. async 추가
                    {
                        // 2. await 추가
                        //var data = await Utils.LoadScenarioData(baseDirectory, item.ScenarioNumber);
                        var data = await Utils.LoadScenarioDataByPath(fullPath);

                        return new SRPanelData { Result = result, Scenario = data };
                    });
                }
                UpdatePanels(loaded);

                // 2. [핵심 수정] UI 렌더링이 완료될 때까지 기다린 후 닫기
                // DispatcherPriority.ApplicationIdle: 화면 그리기가 다 끝난 뒤에 실행됨
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
        private void UpdatePanels(SRPanelData data)
        {
            SelectedScenarioSRChartData = new ObservableCollection<SRChartItem>();
            SelectedScenarioSRGridData = new ObservableCollection<SRGridItem>();
            ViewModel_Unit_Map_SR.SingletonInstance.ClearMissionVisuals();
            ResetTrackbars();

            if (data?.Scenario == null || data?.Result == null) return;

            _cachedResult = data.Result;
            _cachedScenario = data.Scenario;
            SRThreshold = _cachedResult.SRThreshold;

            // 1. 맵 업데이트 (항적 등 표시)
            ViewModel_Unit_Map_SR.SingletonInstance.UpdateMissionVisuals(_cachedScenario, _cachedResult);

            // 2. 그리드 & 차트 업데이트 (통계 데이터는 여전히 분석 결과인 SRData 사용)
            ProcessUAVData(4, _cachedResult.SRData.UAV4, "UAV4");
            ProcessUAVData(5, _cachedResult.SRData.UAV5, "UAV5");
            ProcessUAVData(6, _cachedResult.SRData.UAV6, "UAV6");

            // -----------------------------------------------------------------------
            // [수정 핵심] 3. 트랙바 설정 (SRData가 아닌 전체 FlightData에서 타임스탬프 추출)
            // -----------------------------------------------------------------------

            // 전체 비행 데이터에서 각 UAV별 타임스탬프만 추출 (항적 전체)
            var timestamps4 = GetFullFlightTimestamps(4);
            var timestamps5 = GetFullFlightTimestamps(5);
            var timestamps6 = GetFullFlightTimestamps(6);

            // SetupTrackbar에 전체 타임스탬프 리스트 전달
            SetupTrackbar(4, timestamps4, ref _ts4,
                (v) => MaxIndexUAV4 = v,
                (v) => CurrentIndexUAV4 = v,
                (v) => UAV4TrackBarEnabled = v,
                (s) => CurrentSnapshotTimestampFormatted = s);

            SetupTrackbar(5, timestamps5, ref _ts5,
                (v) => MaxIndexUAV5 = v,
                (v) => CurrentIndexUAV5 = v,
                (v) => UAV5TrackBarEnabled = v,
                (s) => CurrentSnapshotTimestampFormatted1 = s);

            SetupTrackbar(6, timestamps6, ref _ts6,
                (v) => MaxIndexUAV6 = v,
                (v) => CurrentIndexUAV6 = v,
                (v) => UAV6TrackBarEnabled = v,
                (s) => CurrentSnapshotTimestampFormatted2 = s);

            ApplyChartRange();
        }

        // [추가] 전체 비행 타임스탬프 추출 헬퍼 메서드
        private List<ulong> GetFullFlightTimestamps(int uavId)
        {
            if (_cachedScenario?.FlightData == null) return new List<ulong>();

            return _cachedScenario.FlightData
                .Where(f => f.AircraftID == uavId)
                .OrderBy(f => f.Timestamp)
                .Select(f => f.Timestamp)
                .ToList();
        }

        private void ProcessUAVData(int id, List<SRTimestampData> list, string name)
        {
            if (list == null || !list.Any()) return;

            // A. 차트 데이터
            foreach (var item in list)
            {
                SelectedScenarioSRChartData.Add(new SRChartItem
                {
                    Timestamp = Epoch.AddMilliseconds(item.Timestamp).ToLocalTime(),
                    AircraftID = name,
                    GSD = item.SpatialResolution
                });
            }

            //// B. 그리드 데이터 (통계)
            //int validCount = list.Count(x => x.SpatialResolution <= _cachedResult.SRThreshold);
            //int totalCount = list.Count;

            //// 시간 계산 (단순 Count * 간격(1s)으로 가정하거나 TimeStamp 차이로 계산)
            //// 여기서는 정확하게 Timestamp 차이 합산 방식 또는 간단히 샘플 수로 추정
            //double totalTime = totalCount; // (데이터가 1초 간격이라 가정)
            //double validTime = validCount;

            //// 실제 Timestamp 기반 정밀 계산이 필요하면 아래 로직 사용:
            //if (totalCount > 1) totalTime = (list.Last().Timestamp - list.First().Timestamp) / 1000.0;
            //// ValidTime은 연속된 구간 합산 필요 (생략, 단순 개수 비례로 처리)
            ///
            // B. 그리드 데이터 (정확한 시간 계산)

            // 1. 유효한 데이터만 필터링 (Threshold 이하)
            var validItems = list.Where(x => x.SpatialResolution <= _cachedResult.SRThreshold).OrderBy(x => x.Timestamp).ToList();

            // 2. 전체 시간 계산 (마지막 - 처음) / 1000.0
            // 데이터가 1개뿐이면 0초가 되므로 최소 단위(예: 0.2초 또는 1초)를 보장해줄지 결정 필요
            // 여기서는 단순 차이값 사용
            double totalTime = 0;
            if (list.Count > 1)
            {
                ulong minTs = list.Min(x => x.Timestamp);
                ulong maxTs = list.Max(x => x.Timestamp);
                totalTime = (maxTs - minTs) / 1000.0;
            }

            // 3. 유효 시간(ValidTime) 계산 - 연속된 구간 합산
            // (단순 개수 비례가 아니라, 실제 시간 간격으로 계산해야 정확함)
            double validTime = 0;
            if (validItems.Count > 1)
            {
                // 방법 1: 단순 범위 (중간에 끊겨도 포함해버리는 문제 있음)
                // validTime = (validItems.Last().Timestamp - validItems.First().Timestamp) / 1000.0;

                // 방법 2: 연속성 체크 (추천) - 1초(1000ms) 이상 끊기면 별도 구간으로 취급
                for (int i = 0; i < validItems.Count - 1; i++)
                {
                    ulong diff = validItems[i + 1].Timestamp - validItems[i].Timestamp;
                    // 데이터 간격이 2초 이내면 연속된 것으로 간주하고 합산 (기준은 데이터 주기에 따라 조절)
                    if (diff <= 2000)
                    {
                        validTime += (diff / 1000.0);
                    }
                }
            }

            // [방어 코드] 유효 시간이 전체 시간보다 클 수 없음
            if (validTime > totalTime) validTime = totalTime;

            //double score = totalCount > 0 ? (double)validCount / totalCount * 100.0 : 0;
            double score = (totalTime > 0) ? (validTime / totalTime * 100.0) : 0;

            SelectedScenarioSRGridData.Add(new SRGridItem
            {
                AircraftID = name,
                Score = score,
                ValidTime = validTime,
                TotalTime = totalTime
            });
        }

        // [수정] 인자 타입 변경: List<SRTimestampData> -> List<ulong>
        private void SetupTrackbar(int id, List<ulong> allTimestamps, ref List<ulong> tsList,
             Action<int> setMax, Action<int> setCurrent, Action<bool> setEnable,
             Action<string> setLabel)
        {
            // 이미 정제된 타임스탬프 리스트를 받으므로 바로 할당
            if (allTimestamps != null && allTimestamps.Any())
            {
                tsList = allTimestamps; // 전체 비행 시간
                setMax(tsList.Count - 1);
                setEnable(true);

                UpdateUAV(id, 0, setLabel);
                setCurrent(0);
            }
            else
            {
                tsList.Clear();
                //Max를 0으로 만들기 전에 반드시 현재 값을 0으로 초기화
                setCurrent(0); 
                setMax(0);
                setEnable(false);
                if (setLabel != null) setLabel("--:--:---");
            }
        }

        private void UpdateUAV(int uavId, int index, Action<string> setLabel)
        {
            List<ulong> ts = uavId == 4 ? _ts4 : uavId == 5 ? _ts5 : _ts6;
            if (index < 0 || index >= ts.Count) return;

            ulong timestamp = ts[index];
            var time = Epoch.AddMilliseconds(timestamp).ToLocalTime();

            if (setLabel != null) setLabel(time.ToString("HH:mm:ss.fff"));

            if (_cachedScenario != null && _cachedScenario.RealTargetData != null)
            {
                // ViewModel_Unit_Map_SR에 UpdateTargetsAt 메서드가 있다고 가정
                // 만약 ViewModel_Unit_Map을 써야 한다면 그대로 두세요.
                ViewModel_Unit_Map_SR.SingletonInstance.UpdateTargetsAt(timestamp, _cachedScenario.RealTargetData);

                // 기존 ShowUavSnapshot 호출 (위치 조정 가능)
                ViewModel_Unit_Map_SR.SingletonInstance.ShowUavSnapshot(timestamp, uavId, _cachedScenario, _cachedResult);
            }
            else
            {
                // 시나리오 데이터가 없을 때도 UAV 스냅샷은 보여줘야 한다면 여기서 호출
                ViewModel_Unit_Map_SR.SingletonInstance.ShowUavSnapshot(timestamp, uavId, _cachedScenario, _cachedResult);
            }

            // 차트 크로스헤어
            if (SRChart.IsLoaded && SRChart.IsVisible && SRChart.Diagram is XYDiagram2D d)
            {
                d.ShowCrosshair(time, null);
            }
        }

        private void ResetTrackbars()
        {
            UAV4TrackBarEnabled = UAV5TrackBarEnabled = UAV6TrackBarEnabled = false;
            CurrentSnapshotTimestampFormatted = CurrentSnapshotTimestampFormatted1 = CurrentSnapshotTimestampFormatted2 = "--:--:---";
        }

        private void ApplyChartRange()
        {
            if (SRChart.Diagram is not XYDiagram2D d || d.ActualAxisX == null || d.ActualAxisY == null)
                return;

            if (!SelectedScenarioSRChartData.Any())
                return;

            // [핵심 1] 렌더링 도중 충돌 방지를 위해 차트 업데이트 잠금
            SRChart.BeginInit();
            try
            {
                var minTime = SelectedScenarioSRChartData.Min(x => x.Timestamp);
                var maxTime = SelectedScenarioSRChartData.Max(x => x.Timestamp);
                if (maxTime <= minTime) maxTime = minTime.AddSeconds(1);

                // [핵심 2] new Range() 강제 할당 제거, 기존 Range가 있을 때만 값 세팅
                if (d.ActualAxisX.WholeRange != null)
                    d.ActualAxisX.WholeRange.SetMinMaxValues(minTime, maxTime);
                if (d.ActualAxisX.VisualRange != null)
                    d.ActualAxisX.VisualRange.SetMinMaxValues(minTime, maxTime);

                var axisY = d.ActualAxisY;
                axisY.ScaleBreaks.Clear();

                float maxGSD = SelectedScenarioSRChartData.Max(x => x.GSD);
                float threshold = SRThreshold;

                double breakStart = threshold * 1.5;
                double breakEnd = maxGSD * 0.9;

                if (maxGSD > (threshold * 3) && breakEnd > breakStart)
                {
                    axisY.ScaleBreaks.Add(new ScaleBreak { Edge1 = breakStart, Edge2 = breakEnd });
                }

                if (axisY.WholeRange != null)
                    axisY.WholeRange.SetMinMaxValues(null, null);
                if (axisY.VisualRange != null)
                    axisY.VisualRange.SetMinMaxValues(null, null);
            }
            finally
            {
                // 업데이트 잠금 해제
                SRChart.EndInit();
            }
        }


        // ---------------------------------------------------------
        // [수정] 차트 클릭 시 트랙바/지도 동기화 로직
        // ---------------------------------------------------------
        private void Chart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var chart = sender as ChartControl;
            if (chart == null) return;

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
                SyncTrackbarToTime(4, _ts4, clickedTime);
                SyncTrackbarToTime(5, _ts5, clickedTime);
                SyncTrackbarToTime(6, _ts6, clickedTime);
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
