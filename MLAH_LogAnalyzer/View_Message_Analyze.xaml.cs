// 필수 using 문들
using DevExpress.Mvvm;
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
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
// using System.Windows.Media; // C# 코드에서 직접 색상을 사용하지 않으므로 불필요

namespace MLAH_LogAnalyzer
{

    internal class TimelineCalcResult
    {
        public ObservableCollection<TimelineEvent> Events { get; set; }
        public ObservableCollection<RowFill> Fills { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
    public partial class View_Message_Analyze : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion

        #region Properties for Data Binding
        private static readonly DateTime Epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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

        private ulong _ConvertedTime;
        public ulong ConvertedTime
        {
            get => _ConvertedTime;
            set { _ConvertedTime = value; OnPropertyChanged(nameof(ConvertedTime)); OnPropertyChanged(nameof(ConvertedTimeFormatted)); }
        }

        private bool isCreatingRegion = false;  // 현재 영역을 생성 중인지 여부를 나타내는 플래그
        private Strip previewStrip;             // 마우스를 따라다니는 반투명한 미리보기 영역
        private Strip finalStrip; // ✅ 최종 확정된 영역을 저장하기 위해 이 변수를 추가하세요.
        private DateTime regionStartTime;       // 우클릭으로 지정한 영역의 시작 시간
        private SeriesPoint lastSnappedPoint;

        public DateTime LogStartTimeFormatted => _logStartTime == 0 ? DateTime.Now.Date : Epoch.AddMilliseconds(_logStartTime).ToLocalTime();
        public DateTime LogEndTimeFormatted => _logEndTime == 0 ? DateTime.Now.Date.AddSeconds(1) : Epoch.AddMilliseconds(_logEndTime).ToLocalTime();

        public DateTime ConvertedTimeFormatted => _ConvertedTime == 0 ? DateTime.Now.Date : Epoch.AddMilliseconds(_logStartTime).ToLocalTime();

        private string _lnputConvertedTime;
        public string lnputConvertedTime
        {
            get => _lnputConvertedTime;
            set { _lnputConvertedTime = value; OnPropertyChanged(nameof(lnputConvertedTime)); }
        }

        private string _logDirectoryName;
        public string LogDirectoryName
        {
            get => _logDirectoryName;
            set { _logDirectoryName = value; OnPropertyChanged(nameof(LogDirectoryName)); }
        }

        public ObservableCollection<MessageItem> MessageItems { get; set; }
        private ObservableCollection<LogEntry> AllLogEntries { get; set; }
        public ICollectionView FilteredDataSource { get; private set; }

        private LogEntry _selectedLogEntry;
        public LogEntry SelectedLogEntry
        {
            get => _selectedLogEntry;
            set
            {
                _selectedLogEntry = value;
                OnPropertyChanged(nameof(SelectedLogEntry));

                // 기존 로직
                UpdateDetailTree();

                //차트에 선택된 시간 표시 (선 그리기)
                ShowCrosshairOnChart();
            }
        }

        public ObservableCollection<MessageNode> DetailNodes { get; set; }

        private ObservableCollection<TimelineEvent> _allTimelineEvents = new ObservableCollection<TimelineEvent>();
        public ObservableCollection<TimelineEvent> AllTimelineEvents
        {
            get => _allTimelineEvents;
            set { _allTimelineEvents = value; OnPropertyChanged(nameof(AllTimelineEvents)); }
        }

        private string _selectedTimelineMessageName;
        public string SelectedTimelineMessageName
        {
            get => _selectedTimelineMessageName;
            set
            {
                _selectedTimelineMessageName = value;
                OnPropertyChanged(nameof(SelectedTimelineMessageName));
                FilteredDataSource?.Refresh();
            }
        }

        private bool _openTreeChecked = true;
        public bool OpenTreeChecked
        {
            get => _openTreeChecked;
            set
            {
                _openTreeChecked = value;
                OnPropertyChanged(nameof(OpenTreeChecked));

                // ✅ 체크박스가 true가 되면 모든 노드를 펼칩니다.
                if (value)
                {
                    treeListView.ExpandAllNodes();
                }
                else // (옵션) false가 되면 모든 노드를 접습니다.
                {
                    treeListView.CollapseAllNodes();
                }
            }
        }

        private bool _isBulkUpdating = false;

        private ObservableCollection<RowFill> _rowFills = new ObservableCollection<RowFill>();
        public ObservableCollection<RowFill> RowFills
        {
            get => _rowFills;
            set { _rowFills = value; OnPropertyChanged(nameof(RowFills)); }
        }
        #endregion

        // 0201 메시지는 InputMissionPlan 폴더의 inputMissionPackageID 값과 일치하는 파일을 찾음
        private static readonly Dictionary<string, DetailMapInfo> MessageDetailMappings = new Dictionary<string, DetailMapInfo>
{
    { "0201", new DetailMapInfo { FolderName = "InputMissionPlan", KeyField = "inputMissionPackageID" } },
    { "0302", new DetailMapInfo { FolderName = "IndividualMissionPlan", KeyField = "individualMissionPlanID" } },
    //{ "51311", new DetailMapInfo { FolderName = "MissionReferenceInfo", KeyField = "missionReferencePackageID" } },
    //{ "0701", new DetailMapInfo { FolderName = "MissionPlanOptionInfo", KeyField = "missionReferencePackageID" } }, 
    // 필요한 만큼 추가하세요 (예: MissionPlan, VehicleStatus 등)
    { "0301", new DetailMapInfo { FolderName = "MissionPlan", KeyField = "missionPlanID" } },
    { "0303", new DetailMapInfo { FolderName = "FlightPath", KeyField = "pathID" } },
    { "0304", new DetailMapInfo { FolderName = "FlightPath", KeyField = "pathID" } },
    { "0203", new DetailMapInfo { FolderName = "MissionReferenceInfo", KeyField = "missionReferencePackageID" } },
    //{ "0202", new DetailMapInfo { FolderName = "MissionReferenceInfo", KeyField = "missionReferencePackageID" } },
};

        public View_Message_Analyze()
        {
            InitializeComponent();
            this.DataContext = this;

            MessageItems = new ObservableCollection<MessageItem>();
            AllLogEntries = new ObservableCollection<LogEntry>();
            DetailNodes = new ObservableCollection<MessageNode>();
            AllTimelineEvents = new ObservableCollection<TimelineEvent>();

            FilteredDataSource = CollectionViewSource.GetDefaultView(AllLogEntries);
            FilteredDataSource.Filter = FilterLogEntries;
        }

        private void ShowCrosshairOnChart()
        {
            // 1. 데이터 및 다이어그램 유효성 검사
            if (_selectedLogEntry == null || chart.Diagram is not XYDiagram2D diagram) return;

            // 2. 시간 변환 (View_Message_Analyze의 Epoch 로직 사용)
            DateTime pointTime = Epoch.AddMilliseconds(_selectedLogEntry.Timestamp).ToLocalTime();
            string pointArg = _selectedLogEntry.MessageName;

            // 3. [중요] 화면 밖의 데이터일 경우 차트 스크롤 이동 (이 로직은 유지하는 게 좋습니다)
            var visualRange = diagram.ActualAxisY.VisualRange;
            DateTime minVis = (DateTime)visualRange.MinValue;
            DateTime maxVis = (DateTime)visualRange.MaxValue;

            if (pointTime < minVis || pointTime > maxVis)
            {
                TimeSpan rangeSpan = maxVis - minVis;
                DateTime newMin = pointTime.AddMilliseconds(-rangeSpan.TotalMilliseconds / 2);
                DateTime newMax = pointTime.AddMilliseconds(rangeSpan.TotalMilliseconds / 2);
                diagram.ActualAxisY.VisualRange.SetMinMaxValues(newMin, newMax);
            }

            // 4. Crosshair 표시
            // 차트가 Rotated 상태이므로 Argument(메시지이름)와 Value(시간)를 명확히 지정해줍니다.
            // 이렇게 하면 해당 포인트에 정확히 십자선이 찍힙니다.
            diagram.ShowCrosshair(pointArg, pointTime);
        }

        private async void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            
            //string initialDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestLogDirectory");

            // 1. 현재 로그인한 사용자의 바탕화면 경로 가져오기
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // 2. 바탕화면 아래의 '분석데이터' 폴더 경로 조합
            string targetPath = Path.Combine(desktopPath, "분석데이터","raw");

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
                string fullPath = dialog.FileName;
                LogDirectoryName = System.IO.Path.GetFileName(fullPath);
                await LoadLogFilesAsync(fullPath);
            }
        }


        private async Task LoadLogFilesAsync(string directoryPath)
        {
            // 데이터 초기화
            AllLogEntries.Clear();
            MessageItems.Clear();
            DetailNodes.Clear();
            AllTimelineEvents.Clear();
            RowFills.Clear();
            SelectedTimelineMessageName = null;
            ulong tempMinTs = ulong.MaxValue;
            ulong tempMaxTs = ulong.MinValue;

            LogDirectoryName = Path.GetFileName(directoryPath);
            LogStartTime = 0;
            LogEndTime = 0;

            var progressWindow = new SimpleProgressWindow();
            progressWindow.UpdateProgress(0, "Analyzing folder structure...");
            progressWindow.Show();

            try
            {
                // 1. 모든 JSON 파일 스캔 및 분류
                var allFiles = Directory.GetFiles(directoryPath, "*.json", SearchOption.AllDirectories);

                // 메인 로그 그룹 (폴더명이 숫자인 것)
                var mainLogGroups = new Dictionary<string, List<string>>();

                // 상세 데이터 캐시 (폴더명 -> (파일명(ID) -> JObject))
                // 예: cache["InputMissionPlan"]["100"] = { json content }
                var detailDataCache = new Dictionary<string, Dictionary<string, JObject>>();

                await Task.Run(() =>
                {
                    foreach (var filePath in allFiles)
                    {
                        string parentDirName = Path.GetFileName(Path.GetDirectoryName(filePath));

                        // A. 숫자 폴더 (메인 메시지 로그)
                        if (Regex.IsMatch(parentDirName, @"^\d+"))
                        {
                            if (!mainLogGroups.ContainsKey(parentDirName))
                                mainLogGroups[parentDirName] = new List<string>();
                            mainLogGroups[parentDirName].Add(filePath);
                        }
                        // B. 문자 폴더 (상세 데이터 폴더) - 매핑에 정의된 폴더만 캐싱
                        else
                        {
                            // 우리가 매핑에 정의한 폴더인지 확인 (InputMissionPlan 등)
                            bool isDetailFolder = MessageDetailMappings.Values.Any(m => m.FolderName == parentDirName);

                            if (isDetailFolder)
                            {
                                if (!detailDataCache.ContainsKey(parentDirName))
                                    detailDataCache[parentDirName] = new Dictionary<string, JObject>();

                                try
                                {
                                    string fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
                                    string content = File.ReadAllText(filePath);
                                    var jObj = JObject.Parse(content);

                                    // 딕셔너리에 저장 (Key: 파일명 "100", Value: Json객체)
                                    detailDataCache[parentDirName][fileNameNoExt] = jObj;
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Failed to cache detail file {filePath}: {ex.Message}");
                                }
                            }
                        }
                    }

                    // 메인 로그 정렬
                    foreach (var key in mainLogGroups.Keys.ToList())
                    {
                        mainLogGroups[key].Sort();
                    }
                });

                if (!mainLogGroups.Any())
                {
                    progressWindow.Close();
                    MessageBox.Show("유효한 숫자 폴더(메시지 로그)가 없습니다.");
                    return;
                }

                // 2. 메인 로그 로드 및 상세 데이터 병합
                var tempLogEntries = new List<LogEntry>();

                // ★ [수정 1] 사용된(병합된) 상세 데이터의 키를 추적하기 위한 HashSet
                var usedDetailKeys = new HashSet<string>();

                int totalFiles = mainLogGroups.Sum(g => g.Value.Count);
                int loadedCount = 0;

                await Task.Run(() =>
                {
                    foreach (var group in mainLogGroups)
                    {
                        string messageName = group.Key; // 예: "0201"

                        // 이 메시지가 상세 데이터와 연결되어 있는지 확인
                        bool hasDetailLink = MessageDetailMappings.TryGetValue(messageName, out DetailMapInfo mapInfo);

                        foreach (var filePath in group.Value)
                        {
                            loadedCount++;
                            int percentage = (int)((double)loadedCount / totalFiles * 100);
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressWindow.UpdateProgress(percentage, $"Loading {messageName}...");
                            });

                            if (!File.Exists(filePath)) continue;

                            string jsonContent = File.ReadAllText(filePath).Trim();
                            if (string.IsNullOrWhiteSpace(jsonContent)) continue;

                            try
                            {
                                // 파일이 배열([...]) 형태일 수도 있고 객체({...}) 형태일 수도 있음
                                // 보통 메인 로그는 배열 안에 객체가 하나 들어있는 경우가 많음
                                var tokens = new List<JObject>();

                                using (var stringReader = new StringReader(jsonContent))
                                using (var jsonReader = new JsonTextReader(stringReader) { SupportMultipleContent = true })
                                {
                                    var serializer = new JsonSerializer();
                                    while (jsonReader.Read())
                                    {
                                        if (jsonReader.TokenType == JsonToken.StartArray)
                                        {
                                            var jArray = JArray.Load(jsonReader);
                                            tokens.AddRange(jArray.Children<JObject>());
                                            break;
                                        }
                                        else if (jsonReader.TokenType == JsonToken.StartObject)
                                        {
                                            tokens.Add(serializer.Deserialize<JObject>(jsonReader));
                                        }
                                    }
                                }

                                foreach (var mainJObj in tokens)
                                {
                                    // 타임스탬프 파싱
                                    if (mainJObj["timestamp"] != null && ulong.TryParse(mainJObj["timestamp"].ToString(), out ulong timestamp))
                                    {
                                        if (timestamp < tempMinTs) tempMinTs = timestamp;
                                        if (timestamp > tempMaxTs) tempMaxTs = timestamp;

                                        // ★★★ 상세 데이터 병합 로직 시작 ★★★
                                        if (hasDetailLink)
                                        {
                                            // 1. 메인 로그에서 ID 값 추출 (예: "inputMissionPackageID": 100)
                                            if (mainJObj.TryGetValue(mapInfo.KeyField, out JToken idToken))
                                            {
                                                string idValue = idToken.ToString(); // "100"

                                                // 2. 캐시해둔 상세 데이터에서 해당 ID 파일 찾기
                                                if (detailDataCache.ContainsKey(mapInfo.FolderName) &&
                                                    detailDataCache[mapInfo.FolderName].TryGetValue(idValue, out JObject detailJObj))
                                                {
                                                    // 3. 병합 (Merge)
                                                    // 상세 데이터의 내용을 메인 로그 객체에 합칩니다.
                                                    // "DetailData"라는 별도 프로퍼티로 넣을 수도 있고, 그냥 합칠 수도 있습니다.
                                                    // 여기서는 보기 좋게 "DetailInfo"라는 키 아래에 넣거나, 
                                                    // 그냥 Root에 Merge할 수 있습니다. 

                                                    // 방법 A: Root에 덮어쓰기 (키가 중복되면 상세 데이터가 우선)
                                                    mainJObj.Merge(detailJObj, new JsonMergeSettings
                                                    {
                                                        MergeArrayHandling = MergeArrayHandling.Union
                                                    });

                                                    //병합 성공한 하위 데이터 키를 기록 ("폴더명_파일ID")
                                                    usedDetailKeys.Add($"{mapInfo.FolderName}_{idValue}");

                                                    // 방법 B (추천): 구분되게 하려면 별도 필드에 삽입
                                                    // mainJObj["_DetailedData"] = detailJObj; 
                                                }
                                            }
                                        }
                                        // ★★★ 상세 데이터 병합 로직 끝 ★★★

                                        var newLogEntry = new LogEntry
                                        {
                                            Timestamp = timestamp,
                                            MessageName = messageName,
                                            OriginalData = mainJObj // 병합된 JObject가 들어감
                                        };

                                        // From/To 설정 로직 (기존 코드 유지)
                                        SetFromToByType(newLogEntry, messageName, mainJObj);

                                        tempLogEntries.Add(newLogEntry);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error parsing {filePath}: {ex.Message}");
                            }
                        }
                        //부모 로그가 없어서 병합되지 못한 "고아(Orphan)" 하위 데이터 구제
                        foreach (var detailFolder in detailDataCache)
                        {
                            string folderName = detailFolder.Key; // 예: "InputMissionPlan"

                            // 이 폴더명이 어떤 메인 메시지와 매핑되는지 역추적 (예: "InputMissionPlan" -> "0201")
                            var parentMapping = MessageDetailMappings.FirstOrDefault(m => m.Value.FolderName == folderName);
                            string virtualMessageName = parentMapping.Key; // 예: "0201"

                            if (string.IsNullOrEmpty(virtualMessageName)) continue; // 매핑 정보가 없으면 패스

                            foreach (var detailFile in detailFolder.Value)
                            {
                                string idValue = detailFile.Key;

                                // 이미 부모와 병합된 데이터면 건너뜀
                                if (usedDetailKeys.Contains($"{folderName}_{idValue}")) continue;

                                JObject orphanJObj = detailFile.Value;

                                // 하위 데이터 자체에 있는 timestamp 추출
                                if (orphanJObj["timestamp"] != null && ulong.TryParse(orphanJObj["timestamp"].ToString(), out ulong orphanTimestamp))
                                {
                                    // Min/Max 타임스탬프 갱신
                                    if (orphanTimestamp < tempMinTs) tempMinTs = orphanTimestamp;
                                    if (orphanTimestamp > tempMaxTs) tempMaxTs = orphanTimestamp;

                                    // UI에서 부모가 없었음을 표시하기 위해 임의의 플래그 추가 (옵션)
                                    orphanJObj["_IsOrphanedDetail"] = true;

                                    // 가상의 부모 LogEntry 생성
                                    var orphanLogEntry = new LogEntry
                                    {
                                        Timestamp = orphanTimestamp,
                                        MessageName = virtualMessageName,
                                        IsOrphan = true,
                                        OriginalData = orphanJObj // 하위 데이터를 메인 데이터처럼 사용
                                    };

                                    // From/To 설정 (하위 데이터 기준으로 추정)
                                    SetFromToByType(orphanLogEntry, virtualMessageName, orphanJObj);

                                    tempLogEntries.Add(orphanLogEntry);
                                }
                            }
                        }
                    }
                });

                // 3. UI 업데이트 및 마무리 (기존 코드와 동일)
                progressWindow.Close();

                if (tempMinTs != ulong.MaxValue) LogStartTime = tempMinTs;
                if (tempMaxTs != ulong.MinValue) LogEndTime = tempMaxTs;

                OnPropertyChanged(nameof(LogStartTimeFormatted));
                OnPropertyChanged(nameof(LogEndTimeFormatted));

                tempLogEntries.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

                AllLogEntries.Clear();
                foreach (var entry in tempLogEntries)
                {
                    entry.TimeString = $"{Epoch.AddMilliseconds(entry.Timestamp).ToLocalTime():yyyy/MM/dd HH:mm:ss.fff} ({entry.Timestamp})";
                    AllLogEntries.Add(entry);
                }

                MessageItems.Clear();
                //foreach (var key in mainLogGroups.Keys.OrderBy(k => k))
                //{
                //    var messageItem = new MessageItem { Name = key, IsChecked = false };
                //    messageItem.PropertyChanged += OnMessageItemPropertyChanged;
                //    MessageItems.Add(messageItem);
                //}

                //MessageItems(체크박스 목록) 생성 기준 변경
                // 기존에는 폴더명(mainLogGroups.Keys) 기준이었으나, 고아 데이터만 있는 메시지도 포함하기 위해
                // 실제로 로드된 전체 로그(tempLogEntries)의 유니크한 MessageName을 기준으로 생성
                var uniqueMessageNames = tempLogEntries.Select(e => e.MessageName).Distinct().OrderBy(k => k);
                foreach (var messageName in uniqueMessageNames)
                {
                    //해당 메시지 이름의 로그 중 고아 로그가 하나라도 있는지 확인
                    bool hasOrphans = tempLogEntries.Any(e => e.MessageName == messageName && e.IsOrphan);

                    var messageItem = new MessageItem { Name = messageName, IsChecked = false, HasOrphans = hasOrphans, };
                    messageItem.PropertyChanged += OnMessageItemPropertyChanged;
                    MessageItems.Add(messageItem);
                }

                UpdateBackgroundLines();
                FilteredDataSource.Refresh();

                Dispatcher.InvokeAsync(() => {
                    ApplyChartRange(LogStartTimeFormatted, LogEndTimeFormatted);
                }, System.Windows.Threading.DispatcherPriority.Loaded);

            }
            catch (Exception ex)
            {
                progressWindow?.Close();
                MessageBox.Show($"로그 로드 중 오류 발생: {ex.Message}");
            }
        }

        // From/To 설정 로직을 깔끔하게 분리
        private void SetFromToByType(LogEntry entry, string messageName, JObject json)
        {
            // 4자리 ID
            if (messageName.Length == 4)
            {
                entry.From = json["source"]?.ToString() ?? "N/A";
                if (MessageNameMapping.FourDigitIdToDestinationsMap.TryGetValue(messageName, out string toValue))
                    entry.To = toValue;
                else
                    entry.To = "Unknown";
            }
            // 5자리 ID
            else if (messageName.Length == 5)
            {
                string prefix = messageName.Substring(0, 2);
                switch (prefix)
                {
                    case "51":
                        entry.From = "SBC#1 (종합)";
                        entry.To = messageName.StartsWith("512") ? "SBC#2 (단위1)" : "SBC#3 (단위2)";
                        break;
                    case "52":
                        entry.From = "SBC#2 (단위1)";
                        entry.To = messageName.StartsWith("521") ? "SBC#1 (종합)" : "SBC#3 (단위2)";
                        break;
                    case "53":
                        entry.From = "SBC#3 (단위2)";
                        entry.To = "SBC#1 (종합)";
                        break;
                    default:
                        entry.From = "Unknown";
                        entry.To = "Unknown";
                        break;
                }
            }
        }

        private void UpdateTimelineEvents(MessageItem item)
        {
            if (_isBulkUpdating) return;
            // 이 메서드는 이제 개별 체크박스 클릭 시에만 사용됨
            if (item == null) return;

            var existingPoints = AllTimelineEvents.Where(p => p.MessageName == item.Name).ToList();
            foreach (var point in existingPoints) AllTimelineEvents.Remove(point);
            var existingFill = RowFills.FirstOrDefault(f => f.MessageName == item.Name);
            if (existingFill != null) RowFills.Remove(existingFill);
            if (item.IsChecked)
            {
                var logsForMessage = AllLogEntries.Where(l => l.MessageName == item.Name);
                foreach (var log in logsForMessage)
                    AllTimelineEvents.Add(new TimelineEvent { MessageName = item.Name, Timestamp = Epoch.AddMilliseconds(log.Timestamp).ToLocalTime() });
                RowFills.Add(new RowFill { MessageName = item.Name, Start = LogStartTimeFormatted, End = LogEndTimeFormatted });
            }

            UpdateChartRangeBasedOnCheckedItems();
        }

  

        private void UpdateBackgroundLines()
        {
            if (axisX == null)
                return;

            // ConstantLinesBehind가 아닌 Strips 컬렉션을 사용합니다.
            axisX.Strips.Clear();

            // IsChecked가 true인 항목에 대해서만 스트립을 그립니다.
            foreach (var item in MessageItems.Where(x => x.IsChecked))
            {
                // ✅ ConstantLine 대신 Strip 객체를 생성합니다.
                var strip = new Strip
                {
                    // 질적(Qualitative) 축에서는 Min/Max Limit에 같은 값을 주어
                    // 해당 항목의 영역 전체를 칠하도록 합니다.
                    MinLimit = item.Name,
                    MaxLimit = item.Name,


                    Brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 70, 130, 180)),
                };

                // axisX.ConstantLinesBehind 대신 axisX.Strips 에 추가합니다.
                axisX.Strips.Add(strip);
            }
        }



        private void Chart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var cc = sender as ChartControl;
            if (cc == null) return;
            var hit = cc.CalcHitInfo(e.GetPosition(cc));

            var arg = hit?.SeriesPoint?.Argument as string;
            if (!string.IsNullOrEmpty(arg))
            {
                // ✅ 차트 클릭 시에만 SelectedTimelineMessageName을 업데이트
                SelectedTimelineMessageName = arg;
            }
        }

        private bool FilterLogEntries(object item)
        {
            if (string.IsNullOrEmpty(SelectedTimelineMessageName)) return false;
            if (item is LogEntry entry) return entry.MessageName == SelectedTimelineMessageName;
            return false;
        }

        private void UpdateDetailTree()
        {
            DetailNodes.Clear();
            if (SelectedLogEntry?.OriginalData is JObject jObject)
            {
                // 데이터를 파싱해서 DetailNodes 컬렉션에 추가하는 부분 (이전과 동일)
                var rootNode = new MessageNode { Name = SelectedLogEntry.MessageName, ParentId = null };
                DetailNodes.Add(rootNode);
                ParseAndAddChildren(jObject, rootNode.Id);

                // ✅ UI가 노드를 그리기를 기다린 후, 펼치기 로직을 실행
                Dispatcher.InvokeAsync(() =>
                {
                    // 만약 'Open Tree'가 체크되어 있다면, 모든 노드를 펼친다.
                    if (OpenTreeChecked)
                    {
                        treeListView.ExpandAllNodes();
                    }
                    // 그렇지 않다면, 기존처럼 최상위 노드만 펼친다.
                    else if (treeListView.Nodes.Any())
                    {
                        treeListView.ExpandNode(treeListView.Nodes[0].RowHandle);
                    }
                }, System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void ParseAndAddChildren(JToken token, Guid? parentId, int depth = 0)
        {
            // 재귀 깊이 제한 (StackOverflow 방지)
            if (depth > 100)
            {
                DetailNodes.Add(new MessageNode
                {
                    Name = "[깊이 초과]",
                    Value = "JSON 구조가 너무 깊습니다 (100단계 초과)",
                    ParentId = parentId
                });
                return;
            }

            try
            {
                if (token is JObject obj)
                {
                    foreach (var property in obj.Properties())
                    {
                        var childNode = new MessageNode { Name = property.Name, ParentId = parentId };
                        DetailNodes.Add(childNode);

                        // 자식 노드가 또 다른 객체나 배열을 가지고 있으면 재귀 호출
                        if (property.Value.HasValues)
                        {
                            ParseAndAddChildren(property.Value, childNode.Id, depth + 1);
                        }
                        else
                        {
                            childNode.Value = property.Value.ToString();
                        }
                    }
                }
                else if (token is JArray array)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        var childNode = new MessageNode { Name = $"[{i}]", ParentId = parentId };
                        DetailNodes.Add(childNode);

                        // 배열의 항목이 또 다른 객체나 배열을 가지고 있으면 재귀 호출
                        if (array[i].HasValues)
                        {
                            ParseAndAddChildren(array[i], childNode.Id, depth + 1);
                        }
                        else
                        {
                            childNode.Value = array[i].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DetailNodes.Add(new MessageNode
                {
                    Name = "[파싱 오류]",
                    Value = ex.Message,
                    ParentId = parentId
                });
            }
        }

        private void AddChildrenRecursive(MessageNode parentNode)
        {
            foreach (var child in parentNode.Children)
            {
                DetailNodes.Add(child);
                AddChildrenRecursive(child);
            }
        }

        // ✅ ID를 생성하고 할당하는 새 파싱 메서드
        private List<MessageNode> ParseJTokenToNodes(JToken token, string name, Guid? parentId)
        {
            var nodes = new List<MessageNode>();
            var currentNode = new MessageNode { Name = name, ParentId = parentId };

            if (token is JObject obj)
            {
                foreach (var property in obj.Properties())
                {
                    currentNode.Children.AddRange(ParseJTokenToNodes(property.Value, property.Name, currentNode.Id));
                }
            }
            else if (token is JArray array)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    currentNode.Children.AddRange(ParseJTokenToNodes(array[i], $"[{i}]", currentNode.Id));
                }
            }
            else // JValue
            {
                currentNode.Value = token.ToString();
            }

            nodes.Add(currentNode);
            return nodes;
        }

        private TimeSpan _initialVisibleSpan = TimeSpan.FromMinutes(5);

        private void Chart_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyChartRange(LogStartTimeFormatted, LogEndTimeFormatted);
        }

        // ✅ ApplyInitialRange -> ApplyChartRange 로 이름 변경 및 로직 수정
        private void ApplyChartRange(DateTime start, DateTime end)
        {
            if (chart?.Diagram is not XYDiagram2D d) return;
            if (end <= start) end = start.AddSeconds(1);

            d.ActualAxisY.WholeRange.SetMinMaxValues(start, end);

            // ✅ 이제 VisualRange는 항상 WholeRange와 동일하게 시작
            d.ActualAxisY.VisualRange.SetMinMaxValues(start, end);
        }

        private void Chart_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (chart == null) return;

            // 마우스 커서가 차트의 어느 부분에 있는지 확인
            var hitInfo = chart.CalcHitInfo(e.GetPosition(chart));

            // 마우스가 차트의 메인 영역(다이어그램) 위에 있으면
            if (hitInfo.InDiagram)
            {
                // 이벤트를 여기서 끝내서 부모인 ScrollViewer로 전달되지 않게 함
                e.Handled = true;
            }
            // 그 외의 경우 (축 위, 빈 공간 등)에는 이벤트를 처리하지 않음
            // -> 시간 축 위에서는 차트의 기본 줌 기능이 동작
            // -> 메시지 축 위에서는 부모인 ScrollViewer가 스크롤 동작
        }

        private void chart_BoundDataChanged(object sender, RoutedEventArgs e)
        {
            if (chart?.Diagram is not XYDiagram2D d) return;

        }


        // ✅ 축 범위 계산 로직을 별도 메서드로 분리
        private void UpdateChartRangeBasedOnCheckedItems()
        {
            var checkedItems = MessageItems.Where(mi => mi.IsChecked).Select(mi => mi.Name).ToList();
            if (checkedItems.Any())
            {
                var relevantLogs = AllLogEntries.Where(log => checkedItems.Contains(log.MessageName));
                if (relevantLogs.Any())
                {
                    var minTimestamp = relevantLogs.Min(log => log.Timestamp);
                    var maxTimestamp = relevantLogs.Max(log => log.Timestamp);
                    var startTime = Epoch.AddMilliseconds(minTimestamp).ToLocalTime();
                    var endTime = Epoch.AddMilliseconds(maxTimestamp).ToLocalTime();
                    if ((endTime - startTime).TotalMilliseconds < 100)
                    {
                        startTime = startTime.AddMilliseconds(-50);
                        endTime = endTime.AddMilliseconds(50);
                    }
                    ApplyChartRange(startTime, endTime);
                }
            }
            else
            {
                ApplyChartRange(LogStartTimeFormatted, LogEndTimeFormatted);
            }
        }


        // ✅ [수정] 전체 선택 (Select All) - 로딩 스크린 적용
        private async void Button_PreviewMouseDown_1(object sender, MouseButtonEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow == null) return;

            // 1. 스플래시 스크린 설정
            var splashViewModel = new DXSplashScreenViewModel
            {
                Status = "모든 메시지 타임라인 분석 중...",
                Title = "데이터 처리",
                IsIndeterminate = true,
            };
            var manager = SplashScreenManager.CreateWaitIndicator(splashViewModel);

            try
            {
                manager.Show(parentWindow, WindowStartupLocation.CenterOwner, true, InputBlockMode.None);

                // 2. UI 상태 변경 (체크박스 갱신 - UI 스레드)
                // _isBulkUpdating을 사용하여 개별 이벤트 트리거 방지
                _isBulkUpdating = true;
                foreach (var item in MessageItems) item.IsChecked = true;
                _isBulkUpdating = false;

                SelectedTimelineMessageName = null;

                // 3. [핵심] 무거운 계산 작업을 백그라운드 스레드에서 수행
                // UI 스레드에서 스냅샷을 먼저 생성 (크로스 스레드 접근 방지)
                var logEntriesSnapshot = AllLogEntries.ToList();
                var messageItemsSnapshot = MessageItems.Select(m => m.Name).ToList();
                var logStart = LogStartTimeFormatted;
                var logEnd = LogEndTimeFormatted;

                var result = await Task.Run(() =>
                {
                    var newEvents = new ObservableCollection<TimelineEvent>();
                    var newFills = new ObservableCollection<RowFill>();

                    // 스냅샷 기반으로 작업 (UI 컬렉션 직접 접근 안 함)
                    var logsByMessage = logEntriesSnapshot
                        .GroupBy(l => l.MessageName)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var itemName in messageItemsSnapshot)
                    {
                        if (logsByMessage.TryGetValue(itemName, out var logs))
                        {
                            foreach (var log in logs)
                            {
                                newEvents.Add(new TimelineEvent
                                {
                                    MessageName = itemName,
                                    Timestamp = Epoch.AddMilliseconds(log.Timestamp).ToLocalTime()
                                });
                            }
                            newFills.Add(new RowFill
                            {
                                MessageName = itemName,
                                Start = logStart,
                                End = logEnd
                            });
                        }
                    }

                    // 차트 범위 계산
                    DateTime? minT = null;
                    DateTime? maxT = null;

                    if (newEvents.Any())
                    {
                        minT = newEvents.Min(x => x.Timestamp);
                        maxT = newEvents.Max(x => x.Timestamp);

                        if ((maxT.Value - minT.Value).TotalMilliseconds < 100)
                        {
                            minT = minT.Value.AddMilliseconds(-50);
                            maxT = maxT.Value.AddMilliseconds(50);
                        }
                    }

                    return new TimelineCalcResult
                    {
                        Events = newEvents,
                        Fills = newFills,
                        StartTime = minT,
                        EndTime = maxT
                    };
                });

                // 4. [UI 스레드] 결과 적용
                splashViewModel.Status = "차트 렌더링 중...";

                chart.BeginInit(); // 렌더링 일시 중지
                AllTimelineEvents = result.Events;
                RowFills = result.Fills;

                if (result.StartTime.HasValue && result.EndTime.HasValue)
                {
                    ApplyChartRange(result.StartTime.Value, result.EndTime.Value);
                }
                else
                {
                    ApplyChartRange(LogStartTimeFormatted, LogEndTimeFormatted);
                }
                chart.EndInit(); // 렌더링 재개

                await Dispatcher.InvokeAsync(() =>
                {
                    manager.Close();
                }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류 발생: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                try { manager.Close(); } catch { }
            }
        }

        // ✅ [수정] 전체 해제 (Deselect All) - 로딩 스크린 적용
        private async void Button_PreviewMouseDown_2(object sender, MouseButtonEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow == null) return;

            var splashViewModel = new DXSplashScreenViewModel
            {
                Status = "차트 데이터 초기화 중...",
                Title = "초기화",
                IsIndeterminate = true,
            };
            var manager = SplashScreenManager.CreateWaitIndicator(splashViewModel);

            try
            {
                manager.Show(parentWindow, WindowStartupLocation.CenterOwner, true, InputBlockMode.None);

                // 1. UI 상태 변경
                _isBulkUpdating = true;
                foreach (var item in MessageItems) item.IsChecked = false;
                _isBulkUpdating = false;

                SelectedTimelineMessageName = null;

                // 2. 데이터 초기화 (백그라운드 혹은 비동기 대기)
                await Task.Run(() =>
                {
                    // 큰 컬렉션을 비우는 것도 렌더링에 부담이 될 수 있으므로
                    // 빈 컬렉션을 새로 생성해서 교체하는 방식을 사용
                    return new TimelineCalcResult
                    {
                        Events = new ObservableCollection<TimelineEvent>(),
                        Fills = new ObservableCollection<RowFill>()
                    };
                });

                // 3. UI 적용
                if (axisX != null) axisX.ConstantLinesBehind.Clear();

                chart.BeginInit();
                AllTimelineEvents = new ObservableCollection<TimelineEvent>();
                RowFills = new ObservableCollection<RowFill>();
                ApplyChartRange(LogStartTimeFormatted, LogEndTimeFormatted);
                chart.EndInit();
                await Dispatcher.InvokeAsync(() =>
                {
                    manager.Close();
                }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류 발생: {ex.Message}");
            }
            //finally
            //{
            //    manager.Close();
            //}
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

        private void OnMessageItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MessageItem.IsChecked))
            {
                UpdateTimelineEvents(sender as MessageItem);
                UpdateBackgroundLines();
            }
        }
        // 1. 우클릭: 영역 생성 시작
        private void Chart_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var hitInfo = chart.CalcHitInfo(e.GetPosition(chart));
            if (hitInfo == null) return;

            // (영역 초기화 로직은 그대로)
            if (hitInfo.InStrip)
            {
                axisY.Strips.Clear();
                finalStrip = null; // ✅ 저장된 최종 영역 정보도 함께 삭제합니다.
                isCreatingRegion = false;
                //previewAnnotation.Visible = false;
                defaultCrosshair.ShowCrosshairLabels = true;
                e.Handled = true;
                return;
            }

            // (영역 생성 취소 로직은 그대로)
            if (isCreatingRegion)
            {
                axisY.Strips.Remove(previewStrip);
                isCreatingRegion = false;
                //previewAnnotation.Visible = false;
                defaultCrosshair.ShowCrosshairLabels = true;
            }

            if(hitInfo.SeriesPoint != null)
            //if (lastSnappedPoint != null)
            {
                regionStartTime = lastSnappedPoint.DateTimeValue;
                isCreatingRegion = true; // ★★★ 상태 플래그만 true로 변경 ★★★

                // 미리보기 Strip 객체만 생성 (차트에 추가는 CustomDrawCrosshair에서 처리)
                previewStrip = new Strip
                {
                    MinLimit = regionStartTime,
                    MaxLimit = regionStartTime,
                    Brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 138, 43, 226))
                };

                if (!axisY.Strips.Contains(previewStrip))
                {
                    axisY.Strips.Add(previewStrip);
                }

                e.Handled = true;
            }
        }

        private void Chart_MouseLeftButtonDown_Handler(object sender, MouseButtonEventArgs e)
        {
            // 1. '영역 생성' 모드일 경우 -> 좌클릭은 '영역 확정' 기능
            if (isCreatingRegion)
            {
                DiagramCoordinates coords = diagram.PointToDiagram(e.GetPosition(chart));
                if (coords != null && !string.IsNullOrEmpty(coords.QualitativeArgument))
                {
                    // 최종 영역 확정
                    DateTime finalTime = coords.DateTimeValue;
                    DateTime finalMin = (finalTime < regionStartTime) ? finalTime : regionStartTime;
                    DateTime finalMax = (finalTime < regionStartTime) ? regionStartTime : finalTime;

                    // ✅ 수정된 부분: 'var'를 삭제하여 지역 변수가 아닌 클래스 멤버 변수에 값을 할당합니다.
                    finalStrip = new Strip
                    {
                        MinLimit = finalMin,
                        MaxLimit = finalMax,
                        Brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 71, 223, 227)), // OrangeRed
                        
                    };
                    axisY.Strips.Add(finalStrip);

                    // 상태 초기화
                    axisY.Strips.Remove(previewStrip);
                    previewStrip = null;
                    isCreatingRegion = false;
                    defaultCrosshair.ShowCrosshairLabels = true;
                    e.Handled = true;
                }
            }
            // 2. '영역 생성' 모드가 아닐 경우 -> 좌클릭은 기존의 '포인트 선택' 기능
            else
            {
                var hitInfo = chart.CalcHitInfo(e.GetPosition(chart));
                //var arg = hitInfo?.SeriesPoint?.Argument as string;
                //if (!string.IsNullOrEmpty(arg))
                //{
                //    SelectedTimelineMessageName = arg;
                //}
                // 사용자가 '데이터 점(SeriesPoint)'을 클릭했는지 확인
                if (hitInfo != null && hitInfo.InSeriesPoint && hitInfo.SeriesPoint != null)
                {
                    // 3. 클릭한 점의 정보 추출
                    string clickedMessageName = hitInfo.SeriesPoint.Argument; // 메시지 이름 (예: "0601")
                    DateTime clickedTime = hitInfo.SeriesPoint.DateTimeValue; // 시간

                    // 4. 원본 데이터(AllLogEntries)에서 해당 로그 찾기
                    // (DateTime 비교 시 미세한 오차를 허용하기 위해 1ms 이내 차이로 비교)
                    var targetLog = AllLogEntries.FirstOrDefault(log =>
                        log.MessageName == clickedMessageName &&
                        Math.Abs((Epoch.AddMilliseconds(log.Timestamp).ToLocalTime() - clickedTime).TotalMilliseconds) < 1.0
                    );

                    if (targetLog != null)
                    {
                        // 5. 그리드의 선택 항목(SelectedItem)으로 설정
                        SelectedLogEntry = targetLog;

                        // 6. 그리드에서 해당 행으로 스크롤 이동 (시각적 강조)
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            // 그리드 내에서 해당 아이템의 핸들(RowHandle)을 찾음
                            int rowHandle = MessageGridControl.FindRow(targetLog);

                            if (rowHandle != DevExpress.Xpf.Grid.GridControl.InvalidRowHandle)
                            {
                                // 해당 행이 화면에 보이도록 스크롤
                                MessageGridControl.View.ScrollIntoView(rowHandle);
                                // 확실하게 포커스
                                MessageGridControl.View.FocusedRowHandle = rowHandle;
                            }
                        }));
                    }

                    // 차트 선택 효과를 위해 SelectedTimelineMessageName 업데이트 (기존 로직)
                    SelectedTimelineMessageName = clickedMessageName;
                }
            }
        }

        private void Chart_QueryChartCursor(object sender, QueryChartCursorEventArgs e)
        {
            e.Cursor = Cursors.Arrow;
            e.Handled = true;
        }

        private void chart_CustomDrawCrosshair(object sender, CustomDrawCrosshairEventArgs e)
        {
            // A. 영역을 '생성하는 중'일 때의 로직
            if (isCreatingRegion)
            {
                var group = e.CrosshairElementGroups.FirstOrDefault();
                if (group == null) return;
                var element = group.CrosshairElements.FirstOrDefault();
                if (element == null) return;

                group.HeaderElement.Visible = false;

                var coords = diagram.PointToDiagram(Mouse.GetPosition(chart));
                if (coords == null || string.IsNullOrEmpty(coords.QualitativeArgument)) return;
                DateTime mouseTime = coords.DateTimeValue;

                TimeSpan duration = mouseTime - regionStartTime;

                string durationText = $"{duration.TotalSeconds:F3} 초"; 

                string text = $"기준: {regionStartTime:HH:mm:ss.fff}\n" +
                              $"커서: {mouseTime:HH:mm:ss.fff}\n" +
                              $"길이: {durationText}"; 

                element.LabelElement.Text = text;

                for (int i = group.CrosshairElements.Count - 1; i > 0; i--)
                    group.CrosshairElements.RemoveAt(i);

                return; // 다른 로직을 실행하지 않고 종료
            }

            // B. '생성 완료된' 영역 위에 마우스를 올렸을 때의 로직
            var hitInfo = chart.CalcHitInfo(Mouse.GetPosition(chart));
            if (finalStrip != null && hitInfo != null && hitInfo.Strip == finalStrip)
            {
                var group = e.CrosshairElementGroups.FirstOrDefault();
                if (group == null) return;
                var element = group.CrosshairElements.FirstOrDefault();
                if (element == null) return;

                group.HeaderElement.Visible = false;

                DateTime finalStartTime = (DateTime)finalStrip.MinLimit;
                DateTime finalEndTime = (DateTime)finalStrip.MaxLimit;
                TimeSpan duration = finalEndTime - finalStartTime;
                string text = $"시작: {finalStartTime:HH:mm:ss.fff}\n" +
                              $"종료: {finalEndTime:HH:mm:ss.fff}\n" +
                              $"길이: {duration:ss\\.fff} 초";

                element.LabelElement.Text = text;

                for (int i = group.CrosshairElements.Count - 1; i > 0; i--)
                    group.CrosshairElements.RemoveAt(i);

                return; // 다른 로직을 실행하지 않고 종료
            }

            // C. 그 외 평상시의 로직 (다음 우클릭을 위해 포인트 저장)
            var snappedElement = e.CrosshairElementGroups?.FirstOrDefault()?.CrosshairElements?.FirstOrDefault();
            lastSnappedPoint = snappedElement?.SeriesPoint;
        }



        private void chart_MouseLeave(object sender, MouseEventArgs e)
        {
            lastSnappedPoint = null;
        }

        private void chart_MouseMove(object sender, MouseEventArgs e)
        {
            // 영역 생성 중이 아닐 때는 아무것도 하지 않습니다.
            if (!isCreatingRegion || previewStrip == null) return;

            // 마우스 위치를 차트의 데이터 좌표로 변환합니다.
            DiagramCoordinates coords = diagram.PointToDiagram(e.GetPosition(chart));
            if (coords != null && !string.IsNullOrEmpty(coords.QualitativeArgument))
            {
                DateTime mouseTime = coords.DateTimeValue;

                // 미리보기 Strip의 범위를 마우스 위치에 따라 실시간으로 업데이트합니다.
                if (mouseTime < regionStartTime)
                {
                    previewStrip.MinLimit = mouseTime;
                    previewStrip.MaxLimit = regionStartTime;
                }
                else
                {
                    previewStrip.MinLimit = regionStartTime;
                    previewStrip.MaxLimit = mouseTime;
                }
            }
        }
    }
}