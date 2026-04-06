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
using static DevExpress.Xpo.Helpers.AssociatedCollectionCriteriaHelper;
using static MLAH_LogAnalyzer.MessageNameMapping;
using static MLAH_LogAnalyzer.View_SR;
// using System.Windows.Media; // C# 코드에서 직접 색상을 사용하지 않으므로 불필요

namespace MLAH_LogAnalyzer
{
    internal class CommunicationPanelData
    {
        public ScenarioData ScenarioData { get; set; }
        public CommunicationResult CommResult { get; set; }
    }
    public class CommunicationDetailItem
    {
        public uint UAVID { get; set; }
        public float Coverage { get; set; } // 통신 가용도 (%)
        public int LosFailCount { get; set; } // 연결 실패 횟수
        public int TotalConnectCount { get; set; } // 총 연결 횟수 (시도 횟수)
    }

    public partial class View_Communication : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion

        private static readonly DateTime Epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private ObservableCollection<CommunicationDetailItem> _SelectedScenarioGridData = new ObservableCollection<CommunicationDetailItem>();
        public ObservableCollection<CommunicationDetailItem> SelectedScenarioGridData
        {
            get => _SelectedScenarioGridData;
            set
            {
                _SelectedScenarioGridData = value;
                OnPropertyChanged(nameof(SelectedScenarioGridData));
            }
        }

        private ScoreScenarioSummary _selectedScenarioSummary;
        /// <summary>
        /// '시나리오 선택' 그리드에서 선택된 항목
        /// (이 속성은 View_ScoreMain의 DataContext에 바인딩됩니다)
        /// </summary>
        public ScoreScenarioSummary SelectedScenarioSummary
        {
            get => _selectedScenarioSummary;
            set
            {
                _selectedScenarioSummary = value;
                OnPropertyChanged(nameof(SelectedScenarioSummary));

                // [!!!] 2. 핵심: 선택 변경 시 데이터 갱신 메서드 호출 [!!!]
                //UpdateCommunicationDisplays();            }
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
                SelectedScenarioScore = scenarioToLoad.CommScore;
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

   

        private ScenarioData _cachedScenarioData;


        private ObservableCollection<CommunicationDataOutput> _chartDataUAV1 = new();
        public ObservableCollection<CommunicationDataOutput> ChartDataUAV1 { get => _chartDataUAV1; set { _chartDataUAV1 = value; OnPropertyChanged(nameof(ChartDataUAV1)); } }

        private ObservableCollection<CommunicationDataOutput> _chartDataUAV2 = new();
        public ObservableCollection<CommunicationDataOutput> ChartDataUAV2 { get => _chartDataUAV2; set { _chartDataUAV2 = value; OnPropertyChanged(nameof(ChartDataUAV2)); } }

        private ObservableCollection<CommunicationDataOutput> _chartDataUAV3 = new();
        public ObservableCollection<CommunicationDataOutput> ChartDataUAV3 { get => _chartDataUAV3; set { _chartDataUAV3 = value; OnPropertyChanged(nameof(ChartDataUAV3)); } }

        private List<ulong> _currentScenarioTimestamps = new List<ulong>();
        private static readonly uint[] AIRCRAFT_FOR_TIMELINE = new uint[] { 1,4, 5, 6 }; // 통신 대상 UAV
        public View_Communication()
        {
            InitializeComponent();
            this.DataContext = this;

            this.Loaded += View_Communication_Loaded;
            this.Unloaded += View_Communication_Unloaded;
        }

        private void View_Communication_Loaded(object sender, RoutedEventArgs e)
        {
            if (DockingView != null)
            {
                // 자식 뷰의 차트 클릭 이벤트를 구독
                DockingView.ChartClicked += DockingView_ChartClicked;
            }
        }

        private void View_Communication_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DockingView != null)
            {
                // 메모리 누수 방지를 위해 구독 해제
                DockingView.ChartClicked -= DockingView_ChartClicked;
            }
        }

        private void DockingView_ChartClicked(DateTime clickedTime)
        {
            SyncTrackbarToTime(clickedTime);
        }

        private void SyncTrackbarToTime(DateTime targetTime)
        {
            // 데이터가 없으면 무시
            if (_currentScenarioTimestamps == null || _currentScenarioTimestamps.Count == 0) return;

            // 1. 가장 가까운 인덱스 찾기
            int bestIndex = FindClosestTimestampIndex(targetTime);

            // 2. 인덱스 업데이트 -> 트랙바, 지도, 차트 크로스헤어 자동 동기화됨
            if (bestIndex >= 0 && bestIndex < _currentScenarioTimestamps.Count)
            {
                CurrentTimelineIndex = bestIndex;
            }
        }

        private int FindClosestTimestampIndex(DateTime targetDateTime)
        {
            if (_currentScenarioTimestamps == null || _currentScenarioTimestamps.Count == 0) return -1;

            // DateTime -> ulong 변환 (UTC 기준)
            ulong targetTs = (ulong)(targetDateTime.ToUniversalTime() - Epoch).TotalMilliseconds;

            int bestIndex = -1;
            ulong minDiff = ulong.MaxValue;

            for (int i = 0; i < _currentScenarioTimestamps.Count; i++)
            {
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

        private async void UpdateDisplayPanelsAsync()
        {
            var item = _selectedScenarioSummary;
            if(item == null)
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
            CommunicationPanelData loaded = null;

            try
            {
                manager.Show(parent, WindowStartupLocation.CenterOwner, true, InputBlockMode.Window);

                if (item != null && item.OriginalItem != null)
                {
                    //splashViewModel.Status = $"Scenario {item.ScenarioNumber} 시각화 데이터 준비 중...";

                    var preCalcCommResult = item.OriginalItem.CommunicationAnalysisResult;
                    // ✅ AnalysisResult 로드 부분 삭제

                    string fullPath = item.OriginalItem.FullPath;
                    string baseDirectory = Path.GetDirectoryName(fullPath);

                    //loaded = await Task.Run(() => {
                    //    var data = Utils.LoadScenarioData(baseDirectory, item.ScenarioNumber);

                    //    return new CommunicationPanelData
                    //    {
                    //        ScenarioData = data,
                    //        CommResult = preCalcCommResult
                    //        // ✅ AnalysisResult 전달 삭제
                    //    };
                    //});
                    loaded = await Task.Run(async () => // 1. async 추가
                    {
                        // 2. await 추가
                        //var data = await Utils.LoadScenarioData(baseDirectory, item.ScenarioNumber);
                        var data = await Utils.LoadScenarioDataByPath(fullPath);

                        return new CommunicationPanelData { CommResult = preCalcCommResult, ScenarioData = data };
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

        #region 트랙바 헬퍼

        private ulong GetTimestampFromIndex(int index)
        {
            if (_currentScenarioTimestamps == null || index < 0 || index >= _currentScenarioTimestamps.Count)
                return 0;
            return _currentScenarioTimestamps[index];
        }

        //private ulong GetTimestampFromIndex(int index)
        //{
        //    if (_selectedScenarioSummary?.OriginalItem?.CoverageAnalysisResult == null)
        //        return 0;

        //    var analysisResult = _selectedScenarioSummary.OriginalItem.CoverageAnalysisResult;
        //    var uav4Info = analysisResult.UavInfos.FirstOrDefault(u => u.UAVID == 4);

        //    if (uav4Info == null || !uav4Info.Snapshots.Any() || index < 0 || index >= uav4Info.Snapshots.Count)
        //        return 0;

        //    return uav4Info.Snapshots[index].Timestamp;
        //}

        private void UpdateTimestampLabel(int index)
        {
            try
            {
                ulong timestamp = GetTimestampFromIndex(index);
                if (timestamp == 0)
                {
                    CurrentSnapshotTimestampFormatted = "--:--:---";
                    return;
                }

                DateTime originalTimestamp = Epoch.AddMilliseconds(timestamp).ToLocalTime();
                DateTime displayTimestamp = (originalTimestamp.Year == 2055)
                    ? new DateTime(2025, originalTimestamp.Month, originalTimestamp.Day, originalTimestamp.Hour, originalTimestamp.Minute, originalTimestamp.Second, originalTimestamp.Millisecond, originalTimestamp.Kind)
                    : originalTimestamp;

                CurrentSnapshotTimestampFormatted = displayTimestamp.ToString("HH:mm:ss.fff");
            }
            catch { CurrentSnapshotTimestampFormatted = "Error"; }
        }
        private void UpdateMapIconsToTimelineIndex(int index)
        {
            if (_cachedScenarioData == null) return;

            ulong timestamp = GetTimestampFromIndex(index);
            if (timestamp == 0)
            {
                ViewModel_Unit_Map_Communication.SingletonInstance.ClearEvaluationData();
                return;
            }

            ViewModel_Unit_Map_Communication.SingletonInstance.ShowAircraftPositionsAt(timestamp, _cachedScenarioData);
        }
        #endregion


        /// <summary>
        /// 점수 항목을 위한 데이터 클래스
        /// </summary>
        public class ScoreItem
        {
            // 정육각형의 각 꼭짓점 항목 이름 (e.g., "공격력")
            public string Category { get; set; }

            // 해당 항목의 점수 (0~100)
            public double Score { get; set; }
        }



        //private void UpdateCommunicationDisplays()
        //{
        //    // 1. 데이터 클리어
        //    ChartDataUAV1.Clear();
        //    ChartDataUAV2.Clear();
        //    ChartDataUAV3.Clear();
        //    SelectedScenarioGridData.Clear();
        //    ViewModel_Unit_Map_Communication.SingletonInstance.ClearEvaluationData();

        //    // 2. 데이터 준비
        //    if (_selectedScenarioSummary == null || _selectedScenarioSummary.OriginalItem == null)
        //    {
        //        // 데이터 없음 -> UI 초기화 후 종료
        //        _cachedScenarioData = null;
        //        ResetUIControls();
        //        return;
        //    }

        //    var commResult = _selectedScenarioSummary.OriginalItem.CommunicationAnalysisResult;
        //    var analysisResult = _selectedScenarioSummary.OriginalItem.CoverageAnalysisResult;


        //    // [최적화] 시나리오 데이터 로드 (캐시 활용)
        //    // 시나리오 번호가 바뀌었거나 캐시가 없으면 새로 로드
        //    // (ScenarioNumber 속성이 ScenarioData에 있다고 가정, 없으면 매번 로드하거나 별도 관리 필요)
        //    if (_cachedScenarioData == null)
        //    {
        //        // 실제로는 ScenarioData 객체 내부에 번호가 없을 수 있으므로, 
        //        // 여기서는 단순하게 "선택된 항목이 바뀌면 새로 로드"하는 방식을 쓰되,
        //        // View_ScoreMain에서 이미 로드된 객체를 넘겨받을 수 있다면 그게 베스트입니다.
        //        // 현재 구조상으로는 다시 로드해야 안전합니다. (단, 파일 I/O가 발생하므로 비동기가 좋음)

        //        string fullPath = _selectedScenarioSummary.OriginalItem.FullPath;
        //        string baseDirectory = Path.GetDirectoryName(fullPath);

        //        // [주의] 동기 함수 내에서 무거운 작업을 하면 UI가 멈출 수 있음.
        //        // 하지만 기존 구조를 유지하기 위해 동기 로드 (필요 시 비동기 패턴으로 변경 권장)
        //        _cachedScenarioData = Utils.LoadScenarioData(baseDirectory, _selectedScenarioSummary.ScenarioNumber);
        //    }

        //    if (commResult == null || _cachedScenarioData == null || analysisResult == null)
        //    {
        //        ResetUIControls();
        //        return;
        //    }

        //    // 3. [수정] 차트 데이터 채우기 (ID별 분산 저장)
        //    if (commResult.CommunicationDatas != null)
        //    {
        //        foreach (var dataPoint in commResult.CommunicationDatas)
        //        {
        //            var item = new CommunicationDataOutput
        //            {
        //                Timestamp = dataPoint.Timestamp,
        //                AircraftID = dataPoint.AircraftID,
        //                Status = dataPoint.Status
        //            };

        //            switch (dataPoint.AircraftID)
        //            {
        //                case 4: ChartDataUAV1.Add(item); break;
        //                case 5: ChartDataUAV2.Add(item); break;
        //                case 6: ChartDataUAV3.Add(item); break;
        //            }
        //        }
        //    }

        //    // 4. [그리드] 데이터 채우기 (기존 로직 유지)
        //    if (commResult.LOSFalseTimestamps != null)
        //    {
        //        foreach (var kvp in commResult.LOSFalseTimestamps)
        //        {
        //            if (uint.TryParse(kvp.Key.Replace("UAV", ""), out uint uavId))
        //            {
        //                int totalAttempts = commResult.CommunicationDatas.Count(d => d.AircraftID == uavId);
        //                int failCount = kvp.Value.Count;
        //                int successCount = totalAttempts - failCount;
        //                float availability = (totalAttempts > 0) ? ((float)successCount / totalAttempts * 100.0f) : 0;

        //                SelectedScenarioGridData.Add(new CommunicationDetailItem
        //                {
        //                    UAVID = uavId,
        //                    Coverage = availability,
        //                    LosFailCount = failCount,
        //                    TotalConnectCount = totalAttempts
        //                });
        //            }
        //        }
        //    }

        //    // 5. [맵] 업데이트
        //    ViewModel_Unit_Map_Communication.SingletonInstance.UpdateCommunicationTracks(_cachedScenarioData, commResult);

        //    // 6. [트랙바] 설정 (UAV 4번 기준 예시)
        //    var uav4Info = analysisResult.UavInfos.FirstOrDefault(u => u.UAVID == 4);
        //    if (uav4Info != null && uav4Info.Snapshots.Any())
        //    {
        //        UavTrackBar.Minimum = 0;
        //        UavTrackBar.Maximum = uav4Info.Snapshots.Count - 1;
        //        UavTrackBar.TickFrequency = 1;

        //        this.CurrentTimelineIndex = 0;
        //        UpdateMapIconsToTimelineIndex(0);
        //        UpdateTimestampLabel(0);
        //    }
        //    else
        //    {
        //        ResetUIControls();
        //    }

        //    // 7. [차트] UI 갱신 위임
        //    if (DockingView != null)
        //    {
        //        DockingView.RefreshCharts(ChartDataUAV1, ChartDataUAV2, ChartDataUAV3);
        //    }
        //}

        private void UpdateDisplayPanels(CommunicationPanelData loadedData)
        {
            // 1. 초기화 (일괄 교체 방식 - 렌더링 최소화)
            ChartDataUAV1 = new ObservableCollection<CommunicationDataOutput>();
            ChartDataUAV2 = new ObservableCollection<CommunicationDataOutput>();
            ChartDataUAV3 = new ObservableCollection<CommunicationDataOutput>();
            SelectedScenarioGridData = new ObservableCollection<CommunicationDetailItem>();
            _currentScenarioTimestamps.Clear();
            ViewModel_Unit_Map_Communication.SingletonInstance.ClearEvaluationData();

            // 2. 데이터 검사
            if (loadedData == null || loadedData.ScenarioData == null || loadedData.CommResult == null)
            {
                _cachedScenarioData = null;
                ResetUIControls();
                return;
            }

            // 3. 캐싱
            _cachedScenarioData = loadedData.ScenarioData;
            var commResult = loadedData.CommResult;

            // 4. 차트 데이터 채우기 (일괄 교체 - 렌더링 1회)
            if (commResult.CommunicationDatas != null)
            {
                var uav1List = new List<CommunicationDataOutput>();
                var uav2List = new List<CommunicationDataOutput>();
                var uav3List = new List<CommunicationDataOutput>();

                foreach (var dataPoint in commResult.CommunicationDatas)
                {
                    DateTime ts = dataPoint.Timestamp;
                    if (ts.Year == 2055)
                        ts = new DateTime(2025, ts.Month, ts.Day, ts.Hour, ts.Minute, ts.Second, ts.Millisecond, ts.Kind);

                    var item = new CommunicationDataOutput
                    {
                        Timestamp = ts,
                        AircraftID = dataPoint.AircraftID,
                        Status = dataPoint.Status
                    };

                    switch (dataPoint.AircraftID)
                    {
                        case 4: uav1List.Add(item); break;
                        case 5: uav2List.Add(item); break;
                        case 6: uav3List.Add(item); break;
                    }
                }

                ChartDataUAV1 = new ObservableCollection<CommunicationDataOutput>(uav1List);
                ChartDataUAV2 = new ObservableCollection<CommunicationDataOutput>(uav2List);
                ChartDataUAV3 = new ObservableCollection<CommunicationDataOutput>(uav3List);
            }

            // 5. 그리드 데이터 채우기 (일괄 교체 - 렌더링 1회)
            if (commResult.LOSFalseTimestamps != null)
            {
                var gridList = new List<CommunicationDetailItem>();

                foreach (var kvp in commResult.LOSFalseTimestamps)
                {
                    if (uint.TryParse(kvp.Key.Replace("UAV", ""), out uint uavId))
                    {
                        int totalAttempts = commResult.CommunicationDatas.Count(d => d.AircraftID == uavId);
                        int failCount = kvp.Value.Count;
                        int successCount = commResult.CommunicationDatas.Count(d => d.AircraftID == uavId && d.Status == 1);
                        float availability = (totalAttempts > 0) ? ((float)successCount / totalAttempts * 100.0f) : 0;

                        gridList.Add(new CommunicationDetailItem
                        {
                            UAVID = uavId,
                            Coverage = availability,
                            LosFailCount = failCount,
                            TotalConnectCount = totalAttempts
                        });
                    }
                }

                SelectedScenarioGridData = new ObservableCollection<CommunicationDetailItem>(gridList);
            }

            // 6. 맵 업데이트 (인덱스 미리 빌드하여 첫 슬라이더 이동 시 부하 방지)
            ViewModel_Unit_Map_Communication.SingletonInstance.BuildFlightDataIndex(_cachedScenarioData);
            ViewModel_Unit_Map_Communication.SingletonInstance.UpdateCommunicationTracks(_cachedScenarioData, commResult);

            // 7. [수정] 트랙바 설정 (ScenarioData.FlightData 사용)
            // AnalysisResult 대신 로드된 ScenarioData의 FlightData에서 UAV 4,5,6의 타임스탬프를 직접 추출
            if (_cachedScenarioData.FlightData != null)
            {
                var allTimestamps = _cachedScenarioData.FlightData
                    .Where(fd => AIRCRAFT_FOR_TIMELINE.Contains(fd.AircraftID) && fd.Timestamp > 0)
                    .Select(fd => (ulong)fd.Timestamp)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();

                _currentScenarioTimestamps.AddRange(allTimestamps);
            }

            if (_currentScenarioTimestamps.Any())
            {
                UavTrackBar.Minimum = 0;
                UavTrackBar.Maximum = _currentScenarioTimestamps.Count - 1;
                UavTrackBar.TickFrequency = 1;

                this.CurrentTimelineIndex = 0;
                UpdateMapIconsToTimelineIndex(0);
                UpdateTimestampLabel(0);
                UpdateChartCrosshair(0);
            }
            else
            {
                ResetUIControls();
            }

            // 8. 차트 UI 갱신
            if (DockingView != null)
            {
                DockingView.RefreshCharts(ChartDataUAV1, ChartDataUAV2, ChartDataUAV3);
            }
        }

        private void ResetUIControls()
        {
            UavTrackBar.Minimum = 0;
            UavTrackBar.Maximum = 0;
            CurrentSnapshotTimestampFormatted = "--:--:---";

            // 차트 데이터가 비었으므로 빈 상태로 갱신
            if (DockingView != null)
            {
                DockingView.RefreshCharts(ChartDataUAV1, ChartDataUAV2, ChartDataUAV3);
            }
        }

        private void UpdateChartCrosshair(int index)
        {
            ulong timestamp = GetTimestampFromIndex(index);
            if (timestamp == 0) return;

            DateTime originalTimestamp = Epoch.AddMilliseconds(timestamp).ToLocalTime();
            DateTime displayTimestamp;

            if (originalTimestamp.Year == 2055)
            {
                displayTimestamp = new DateTime(2025, originalTimestamp.Month, originalTimestamp.Day,
                                                originalTimestamp.Hour, originalTimestamp.Minute,
                                                originalTimestamp.Second, originalTimestamp.Millisecond, originalTimestamp.Kind);
            }
            else
            {
                displayTimestamp = originalTimestamp;
            }

            // [핵심] 자식 뷰에게 크로스헤어 표시 요청
            if (DockingView != null)
            {
                DockingView.SyncCrosshairs(displayTimestamp);
            }
        }
        


    }
}
