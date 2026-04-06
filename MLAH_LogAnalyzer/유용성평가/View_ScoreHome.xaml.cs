// 필수 using 문들
using DevExpress.Xpf.Charts;
using DevExpress.Xpf.Map;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    //public class ScoreItem : INotifyPropertyChanged
    //{
    //    private string _Category;
    //    public string Category
    //    {
    //        get => _Category;
    //        set { _Category = value; OnPropertyChanged("Category"); }
    //    }

    //    private double _Score;
    //    public double Score
    //    {
    //        get => _Score;
    //        set { _Score = value; OnPropertyChanged("Score"); }
    //    }

    //    public event PropertyChangedEventHandler PropertyChanged;
    //    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    //}
    public partial class View_ScoreHome : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion

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

        private View_ScoreMain _mainVM;
        private ObservableCollection<ScoreScenarioSummary> _checkedScenariosForDisplay = new ObservableCollection<ScoreScenarioSummary>();
        public ObservableCollection<ScoreScenarioSummary> CheckedScenariosForDisplay
        {
            get => _checkedScenariosForDisplay;
            set
            {
                if (_checkedScenariosForDisplay != null)
                {
                    _checkedScenariosForDisplay.CollectionChanged -= OnCheckedScenariosChanged; // 기존 이벤트 해제
                }
                _checkedScenariosForDisplay = value;
                if (_checkedScenariosForDisplay != null)
                {
                    _checkedScenariosForDisplay.CollectionChanged += OnCheckedScenariosChanged; // 새 이벤트 연결
                }
                OnPropertyChanged(nameof(CheckedScenariosForDisplay));

                // [!!!] 트리거 1: 컬렉션 자체가 변경되면 즉시 평균 업데이트
                UpdateDisplayData();
            }
        }

        private ScoreScenarioSummary _selectedScenarioSummary;
        public ScoreScenarioSummary SelectedScenarioSummary
        {
            get => _selectedScenarioSummary;
            set
            {
                _selectedScenarioSummary = value;
                OnPropertyChanged(nameof(SelectedScenarioSummary));

                // [!!!] 트리거 2: 선택 항목 변경 시 [!!!]
                UpdateDisplayData();

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

        // --- 3. 하단 RadioButton 바인딩 속성 ---
        private bool _isSingleScenarioMode = true; // "단일 시나리오" 기본 체크

        // (XAML: IsChecked="{Binding IsSingleScenarioMode, Mode=TwoWay}")
        public bool IsSingleScenarioMode
        {
            get => _isSingleScenarioMode;
            set
            {
                _isSingleScenarioMode = value;
                OnPropertyChanged(nameof(IsSingleScenarioMode));
                OnPropertyChanged(nameof(IsAverageMode)); // 반대편 라디오 버튼도 갱신

                // [!!!] 트리거 2: 라디오 버튼 변경 시 [!!!]
                UpdateScoreTextBlocks(); // 텍스트만 즉시 갱신
            }
        }
        // (XAML: IsChecked="{Binding IsAverageMode, Mode=TwoWay}")
        public bool IsAverageMode
        {
            get => !_isSingleScenarioMode;
            set => IsSingleScenarioMode = !value; // IsSingleScenarioMode의 set 접근자를 호출
        }

        // --- 4. 하단 점수 리스트 바인딩 속성 ---
        // (XAML: Text="{Binding DisplayCommScore}")
        private double _displayCommScore;
        public double DisplayCommScore { get => _displayCommScore; set { _displayCommScore = value; OnPropertyChanged(nameof(DisplayCommScore)); } }
        private double _displaySafetyScore;
        public double DisplaySafetyScore { get => _displaySafetyScore; set { _displaySafetyScore = value; OnPropertyChanged(nameof(DisplaySafetyScore)); } }
        private double _displayMissionDistScore;
        public double DisplayMissionDistScore { get => _displayMissionDistScore; set { _displayMissionDistScore = value; OnPropertyChanged(nameof(DisplayMissionDistScore)); } }
        private double _displayMissionSuccessScore;
        public double DisplayMissionSuccessScore { get => _displayMissionSuccessScore; set { _displayMissionSuccessScore = value; OnPropertyChanged(nameof(DisplayMissionSuccessScore)); } }
        private double _displaySpatialResScore;
        public double DisplaySpatialResScore { get => _displaySpatialResScore; set { _displaySpatialResScore = value; OnPropertyChanged(nameof(DisplaySpatialResScore)); } }
        private double _displayCoverageScore;
        public double DisplayCoverageScore { get => _displayCoverageScore; set { _displayCoverageScore = value; OnPropertyChanged(nameof(DisplayCoverageScore)); } }

        // --- 5. 우측 레이더 차트 바인딩 속성 ---
        // (XAML: DataSource="{Binding RadarChart_Selected}")
        private ObservableCollection<ScoreItem> _RadarChart_Selected = new ObservableCollection<ScoreItem>();
        public ObservableCollection<ScoreItem> RadarChart_Selected
        {
            get
            {
                return _RadarChart_Selected;
            }
            set
            {
                _RadarChart_Selected = value;
                OnPropertyChanged("RadarChart_Selected");
            }
        }

        private ObservableCollection<ScoreItem> _RadarChart_Average = new ObservableCollection<ScoreItem>();
        public ObservableCollection<ScoreItem> RadarChart_Average
        {
            get
            {
                return _RadarChart_Average;
            }
            set
            {
                _RadarChart_Average = value;
                OnPropertyChanged("RadarChart_Average");
            }
        }
        // (XAML: DataSource="{Binding RadarChart_Average}")
        //public ObservableCollection<ScoreItem> RadarChart_Average { get; set; } = new ObservableCollection<ScoreItem>();

        // (ScoreItem 클래스는 View_ScoreMain.xaml.cs에 이미 정의됨)
        // (만약 분리되어 있다면, 이 파일에도 ScoreItem 클래스 정의가 필요합니다.)

        // --- 6. 평균 점수 저장 변수 ---
        // (C# 내부 계산용)
        private Dictionary<string, double> _averageScores = new Dictionary<string, double>();

        private static readonly DateTime Epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private bool _isBulkUpdating = false;

        

        public View_ScoreHome()
        {
            InitializeComponent();
            this.DataContext = this;
            InitializeRadarCharts();

        }

        private void OnCheckedScenariosChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // 목록에 추가/삭제가 발생하면 평균을 다시 계산
            UpdateDisplayData();
        }

        private void InitializeRadarCharts()
        {
            var categories = new[]
            {
                "CommScore", "SafetyScore", "MissionDistScore",
                "MissionSuccessScore", "SpatialResScore", "CoverageScore"
            };

            var categoryNames = new[]
            {
                "통신 가용도", "유인기 안전도", "임무분배 효율도",
                "임무 성취도", "촬영 유효도", "관측 달성도"
            };

            RadarChart_Selected.Clear();
            RadarChart_Average.Clear();
            _averageScores.Clear();

            for (int i = 0; i < categories.Length; i++)
            {
                RadarChart_Selected.Add(new ScoreItem { Category = categoryNames[i], Score = 20 });
                RadarChart_Average.Add(new ScoreItem { Category = categoryNames[i], Score = 0 });
                _averageScores[categories[i]] = 0; // 평균 점수 0으로 초기화
                //_averageScores[categories[i]] = 0; // 평균 점수 0으로 초기화
            }
        }


        private ObservableCollection<ScoreItem> _ScoreChartData = new ObservableCollection<ScoreItem>();
        public ObservableCollection<ScoreItem> ScoreChartData
        {
            get
            {
                return _ScoreChartData;
            }
            set
            {
                _ScoreChartData = value;
                OnPropertyChanged("ScoreChartData");
            }
        }


        /// <summary>
        /// 점수 항목을 위한 데이터 클래스
        /// </summary>
        public class ScoreItem : CommonBase
        {
            private string _Category;
            public string Category
            {
                get
                {
                    return _Category;
                }
                set
                {
                    _Category = value;
                    OnPropertyChanged("Category");
                }
            }

            private double _Score;
            public double Score
            {
                get
                {
                    return _Score;
                }
                set
                {
                    _Score = value;
                    OnPropertyChanged("Score");
                }
            }

          
        }

        private void UpdateScoreTextBlocks()
        {
            if (IsSingleScenarioMode) // "단일 시나리오"
            {
                if (SelectedScenarioSummary != null)
                {
                    DisplayCommScore = SelectedScenarioSummary.CommScore;
                    DisplaySafetyScore = SelectedScenarioSummary.SafetyScore;
                    DisplayMissionDistScore = SelectedScenarioSummary.MissionDistScore;
                    DisplayMissionSuccessScore = SelectedScenarioSummary.MissionSuccessScore;
                    DisplaySpatialResScore = SelectedScenarioSummary.SpatialResScore;
                    DisplayCoverageScore = SelectedScenarioSummary.CoverageScore;
                }
                else
                {
                    // 선택된 항목이 없으면 0
                    DisplayCommScore = 0; DisplaySafetyScore = 0; DisplayMissionDistScore = 0;
                    DisplayMissionSuccessScore = 0; DisplaySpatialResScore = 0; DisplayCoverageScore = 0;
                }
            }
            else // "시나리오 평균"
            {
                // 계산된 평균값 사용
                DisplayCommScore = _averageScores["CommScore"];
                DisplaySafetyScore = _averageScores["SafetyScore"];
                DisplayMissionDistScore = _averageScores["MissionDistScore"];
                DisplayMissionSuccessScore = _averageScores["MissionSuccessScore"];
                DisplaySpatialResScore = _averageScores["SpatialResScore"];
                DisplayCoverageScore = _averageScores["CoverageScore"];
            }
        }
        private void UpdateDisplayData()
        {
            // --- 1. 시나리오 정보 업데이트 (상단 좌측) ---
            if (SelectedScenarioSummary != null)
            {
                SelectedScenarioName = SelectedScenarioSummary.ScenarioName;
            }
            else
            {
                SelectedScenarioName = "N/A";
            }

            // --- 2. 평균 점수 계산 (우측 차트 및 하단 리스트용) ---
            // [!!!] _mainVM.CheckedScenariosForDisplay -> this.CheckedScenariosForDisplay [!!!]
            _averageScores["CommScore"] = 0;
            _averageScores["SafetyScore"] = 0;
            _averageScores["MissionDistScore"] = 0;
            _averageScores["MissionSuccessScore"] = 0;
            _averageScores["SpatialResScore"] = 0;
            _averageScores["CoverageScore"] = 0;

            if (CheckedScenariosForDisplay != null && CheckedScenariosForDisplay.Any())
            {
                _averageScores["CommScore"] = (uint)CheckedScenariosForDisplay.Average(s => s.CommScore);
                _averageScores["SafetyScore"] = (uint)CheckedScenariosForDisplay.Average(s => s.SafetyScore);
                _averageScores["MissionDistScore"] = (uint)CheckedScenariosForDisplay.Average(s => s.MissionDistScore);
                _averageScores["MissionSuccessScore"] = (uint)CheckedScenariosForDisplay.Average(s => s.MissionSuccessScore);
                _averageScores["SpatialResScore"] = (uint)CheckedScenariosForDisplay.Average(s => s.SpatialResScore);
                _averageScores["CoverageScore"] = (uint)CheckedScenariosForDisplay.Average(s => s.CoverageScore);
            }

            //// --- 3. 레이더 차트 데이터 업데이트 (우측) ---
            //RadarChart_Average[0].Score = _averageScores["CommScore"];
            //RadarChart_Average[1].Score = _averageScores["SafetyScore"];
            //RadarChart_Average[2].Score = _averageScores["MissionDistScore"];
            //RadarChart_Average[3].Score = _averageScores["MissionSuccessScore"];
            //RadarChart_Average[4].Score = _averageScores["SpatialResScore"];
            //RadarChart_Average[5].Score = _averageScores["CoverageScore"];

            //if (SelectedScenarioSummary != null)
            //{
            //    RadarChart_Selected[0].Score = SelectedScenarioSummary.CommScore;
            //    RadarChart_Selected[1].Score = SelectedScenarioSummary.SafetyScore;
            //    RadarChart_Selected[2].Score = SelectedScenarioSummary.MissionDistScore;
            //    RadarChart_Selected[3].Score = SelectedScenarioSummary.MissionSuccessScore;
            //    RadarChart_Selected[4].Score = SelectedScenarioSummary.SpatialResScore;
            //    RadarChart_Selected[5].Score = SelectedScenarioSummary.CoverageScore;
            //}
            //else
            //{
            //    foreach (var item in RadarChart_Selected) item.Score = 0;
            //}

            // 수정된 코드 (새 컬렉션으로 교체)
            var newSelectedData = new ObservableCollection<ScoreItem>
{
    new ScoreItem { Category = "통신 가용도", Score = SelectedScenarioSummary?.CommScore ?? 0 },
    new ScoreItem { Category = "유인기 안전도", Score = SelectedScenarioSummary?.SafetyScore ?? 0 },
    new ScoreItem { Category = "임무분배 효율도", Score = SelectedScenarioSummary?.MissionDistScore ?? 0 },
    new ScoreItem { Category = "임무 성취도", Score = SelectedScenarioSummary?.MissionSuccessScore ?? 0 },
    new ScoreItem { Category = "촬영 유효도", Score = SelectedScenarioSummary?.SpatialResScore ?? 0 },
    new ScoreItem { Category = "관측 달성도", Score = SelectedScenarioSummary?.CoverageScore ?? 0 }
};

            // 프로퍼티에 할당하여 UI 갱신 유도
            RadarChart_Selected = newSelectedData;


            // Average도 마찬가지로 새로 생성해서 할당하는 것이 안전합니다.
            var newAverageData = new ObservableCollection<ScoreItem>
{
    new ScoreItem { Category = "통신 가용도", Score = _averageScores["CommScore"] },
    new ScoreItem { Category = "유인기 안전도", Score = _averageScores["SafetyScore"] },
    new ScoreItem { Category = "임무분배 효율도", Score = _averageScores["MissionDistScore"] },
    new ScoreItem { Category = "임무 성취도", Score = _averageScores["MissionSuccessScore"] },
    new ScoreItem { Category = "촬영 유효도", Score = _averageScores["SpatialResScore"] },
    new ScoreItem { Category = "관측 달성도", Score = _averageScores["CoverageScore"] }
};

            RadarChart_Average = newAverageData;

            // --- 4. 텍스트 점수판 업데이트 (하단 좌측) ---
            UpdateScoreTextBlocks();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_mainVM != null) return;

            var parent = LogicalTreeHelper.GetParent(this);
            while (parent != null && !(parent is View_ScoreMain))
            {
                parent = LogicalTreeHelper.GetParent(parent);
            }

            if (parent is View_ScoreMain mainView)
            {
                _mainVM = mainView;

                // 1. 부모의 "체크된 목록"을 이 뷰의 "CheckedScenariosForDisplay" 속성에 할당
                CheckedScenariosForDisplay = _mainVM.CheckedScenariosForDisplay;

                // 2. 부모의 "체크된 목록"이 변경될 때마다(추가/삭제) 평균을 다시 계산하도록 이벤트 연결
                _mainVM.CheckedScenariosForDisplay.CollectionChanged += OnCheckedScenariosChanged;

                // [!!!] 4. 부모의 "선택된 항목"이 변경될 때(다른 탭에서), 이 뷰의 속성도 업데이트 [!!!]
                _mainVM.PropertyChanged += (s, args) => {
                    if (args.PropertyName == nameof(View_ScoreMain.SelectedScenarioSummary))
                    {
                        // 부모의 변경 사항을 이 뷰의 속성에 반영
                        // (이 set 접근자는 UpdateDisplayData()를 트리거함)
                        this.SelectedScenarioSummary = _mainVM.SelectedScenarioSummary;
                    }
                };

                // 5. 초기 데이터 로드
                UpdateDisplayData();
            }
            else
            {
                Debug.WriteLine("View_ScoreHome: 부모 뷰(View_ScoreMain)를 찾을 수 없습니다.");
            }
        }
    }
}