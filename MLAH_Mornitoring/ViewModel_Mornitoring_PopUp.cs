//using DevExpress.Xpf.CodeView;
//using DevExpress.Xpf.Grid;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
// [확인 후 삭제] 미사용 using
//using System.Security.Cryptography.Pkcs;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Application = System.Windows.Application;
//using Grpc.Core;


namespace MLAH_Mornitoring
{
    // 트리 노드 클래스 추가
    

    public class ViewModel_Mornitoring_PopUp : CommonBase
    {
        #region Singleton

        private static readonly Lazy<ViewModel_Mornitoring_PopUp> _lazyInstance =
       new Lazy<ViewModel_Mornitoring_PopUp>(() => new ViewModel_Mornitoring_PopUp());

        /// <summary>
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>
        public static ViewModel_Mornitoring_PopUp SingletonInstance => _lazyInstance.Value;

        #endregion Singleton

        #region 생성자 & 콜백
        public ViewModel_Mornitoring_PopUp()
        {
            //ApplyCommand = new RelayCommand(ApplyCommandAction);
            CloseCommand = new RelayCommand(CloseCommandAction);
            MessageItems = new ObservableCollection<MessageItem>();
            //LoadProtoMessages("ExampleApplication.proto");
            

            // [수정] 데이터 소스를 ObservableRangeCollection으로 변경
            MornitoringDataSource = new ObservableRangeCollection<ContextInfo>();
            
            // 필터링 설정
            FilteredDataSource = CollectionViewSource.GetDefaultView(MornitoringDataSource);
            FilteredDataSource.Filter = FilterMessages;

            // A. MLAHInterop: 블랙리스트 방식 (null을 넘기면 _excludedMessageNames를 제외하고 다 넣음)
            LoadProtoMessages(Path.Combine("gRPC", "MLAHIntefaceProto.proto"), null);

            // B. FederateInterface: 화이트리스트 방식 (지정한 이름만 넣음)
            var federateAllowList = new HashSet<string>
    {
        "JoinFederationExecutionReq",
        "ResignFederationExecutionReq"
    };
            LoadProtoMessages(Path.Combine("gRPC", "FederateInterface.proto"), federateAllowList);

            UpdateFilter();

            // [수정] Model의 배치 이벤트 구독
            //Model_Mornitoring_PopUp.SingletonInstance.gRPCMessagesAvailable += Callback_gRPCMessagesAvailable;
            //Model_Mornitoring_PopUp.SingletonInstance.gRPCMessageAvailable += Callback_gRPCMessageAvailable;
            Model_Mornitoring_PopUp.SingletonInstance.gRPCMessagesAvailable += OnGrpcMessagesAvailable;
            //ClearMessagesCommand = new RelayCommand(ClearMessagesAction);

            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("ObservationHelicopterInfo", "velocity");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("ObservationHelicopterInfo", "abnormalcause");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("ObservationHelicopterInfo", "HealthStatus");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("ObservationHelicopterInfo", "FuelStatus");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("EntityAbnormalCause", "hit");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("EntityAbnormalCause", "loss_1");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("EntityAbnormalCause", "loss_2");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("EntityAbnormalCause", "loss_3");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("EntityAbnormalCause", "crash");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("ObservationUAVInfo", "search_target_info");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("ObservationUAVInfo", "detected_target_info");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("ObservationUAVInfo", "interest_region_info");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("ObservationUAVInfo", "recognition_pixel");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("ObservationUAVInfo", "detection_pixel");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("ObservationUAVInfo", "HealthStatus");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("ObservationUAVInfo", "FuelStatus");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("ObservationUAVInfo", "SensorStatus");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("ObservationTargetInfo", "unit1targetid");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("ScenarioMission", "mission_type");
            Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("ScenarioMission", "mission");
            //Model_Mornitoring_PopUp.SingletonInstance.AddHighlightTarget("LLAPosition", "altitude"); // 구조체 내부도 가능!
        }

        private static readonly HashSet<string> _excludedMessageNames = new HashSet<string>
{
    // === 기본 구조체 (Component) ===
    "LLAPosition", "UnrealPosition", "Orientation", "Velocity", "Vector4",
    "CameraAction", 

    // === 서브 정보 (Sub-Info) ===
    "ActionEntityinfo",
    "ObservationSimulationInfo", "ObservationEntityInfo",
    "ObservationHelicopterInfo", "ObservationUAVInfo", "ObservationTargetInfo",
    "ObservationInterestRegion",
    "EntityAbnormalCause",
    "ScenarioMission",          // ScenarioMissionResponse의 부속
    "UpdateEntityInfo",         // UpdateEntity의 부속
    "CoperationTimeInformation",// 독립적으로 잘 안 쓰임
    "IndividualTimeInformation",

    // === Enums (열거형은 보통 메시지 로그로 찍지 않음) ===
    "enumInputStatus", "enumEntityStatus", "enumActionType",
    "enumMoveActionType", "enumAttackActionType", "enumMissionType",

    // === FederateInterface 제외 (화이트리스트 로직을 쓰더라도 안전장치로 둠) ===
    "FERsp", "FederateInfo", "FederateDiscovered", "FederateRemoved",
    "ObjectDiscovered", "ObjectRemoved", "ObjectReflected", "MessageReceived",
    "TimeAdvanceRequest", "TimeAdvanceGrant", "Notification", "enumNotifType", "enumFEResult"
};

        private const int MAX_MESSAGE_COUNT = 200000;
        private bool _isUpdatingFilter = false;
        //private bool _isMornitoringUpdateScheduled = false;
        public bool IsPopupActive { get; private set; } = false;

        private bool _isAutoScrollEnabled = true;
        public bool IsAutoScrollEnabled
        {
            get => _isAutoScrollEnabled;
            set
            {
                _isAutoScrollEnabled = value;
                OnPropertyChanged(nameof(IsAutoScrollEnabled));
            }
        }

        private ObservableCollection<MessageNode> _detailNodes = new ObservableCollection<MessageNode>();
        public ObservableCollection<MessageNode> DetailNodes
        {
            get => _detailNodes;
            set
            {
                _detailNodes = value;
                OnPropertyChanged(nameof(DetailNodes));
            }
        }

        
        public event Action OnBatchUpdateStart;
        public event Action OnBatchUpdateEnd;
        private void OnGrpcMessagesAvailable(object sender, List<Tuple<IMessage, ContextInfo>> messages)
        {
            // 데이터 가공 및 원본 컬렉션에 데이터 추가는 항상 수행
            var contextInfos = messages.Select(tuple =>CreateNewContextInfo(tuple.Item1, tuple.Item2)).ToList();

            if (!contextInfos.Any()) return;

            //var previouslySelectedItem = this.SelectedMornitoringData;

            //MornitoringDataSource.AddRange(contextInfos);
            //if (MornitoringDataSource.Count > MAX_MESSAGE_COUNT)
            //{
            //    int removeCount = MornitoringDataSource.Count - MAX_MESSAGE_COUNT;
            //    MornitoringDataSource.RemoveRange(0, removeCount);
            //}

            //if (IsPopupActive && !_isRefreshScheduled)
            //{
            //    _isRefreshScheduled = true;
            //    Application.Current.Dispatcher.InvokeAsync(async () =>
            //    {
            //        await Task.Delay(200); // 0.2초 대기

            //        // 새로고침과 선택 항목 유지를 이 안에서 모두 처리
            //        FilteredDataSource.Refresh();

            //        if (previouslySelectedItem != null && MornitoringDataSource.Contains(previouslySelectedItem))
            //        {
            //            this.SelectedMornitoringData = previouslySelectedItem;
            //        }

            //        _isRefreshScheduled = false; // 작업 완료 후 플래그 리셋
            //    });
            //}

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // View에게 업데이트 시작 알림 -> Grid.BeginDataUpdate() 호출 유도
                OnBatchUpdateStart?.Invoke();

                try
                {
                    MornitoringDataSource.AddRange(contextInfos);

                    // ... (개수 제한 로직) ...
                    if (MornitoringDataSource.Count > MAX_MESSAGE_COUNT)
                    {
                        int removeCount = MornitoringDataSource.Count - MAX_MESSAGE_COUNT;
                        MornitoringDataSource.RemoveRange(0, removeCount);
                    }
                }
                finally
                {
                    // View에게 업데이트 종료 알림 -> Grid.EndDataUpdate() 호출 유도
                    OnBatchUpdateEnd?.Invoke();
                }
            });
        }



        #endregion 생성자 & 콜백

        // 트리 데이터 소스 추가
        //private ObservableCollection<MessageNode> _messageTreeData = new ObservableCollection<MessageNode>();
        //public ObservableCollection<MessageNode> MessageTreeData
        //{
        //    get => _messageTreeData;
        //    set
        //    {
        //        _messageTreeData = value;
        //        OnPropertyChanged(nameof(MessageTreeData));
        //    }
        //}

        private bool _isOpenTreeChecked = true; // 기본값 true
        public bool IsOpenTreeChecked
        {
            get => _isOpenTreeChecked;
            set
            {
                _isOpenTreeChecked = value;
                OnPropertyChanged(nameof(IsOpenTreeChecked));

                // 체크 상태가 변경되면 이벤트를 발생시켜 View에게 알림 (선택 사항)
                // 여기서는 View에서 PropertyChanged를 감지하도록 할 예정입니다.
            }
        }

        private ObservableCollection<MessageNode> _messageTreeDataJoin = new ObservableCollection<MessageNode>();
        public ObservableCollection<MessageNode> MessageTreeDataJoin
        {
            get => _messageTreeDataJoin;
            set
            {
                _messageTreeDataJoin = value;
                OnPropertyChanged(nameof(MessageTreeDataJoin));
            }
        }


        private int _ObservationCount = 0;
        public int ObservationCount
        {
            get
            {
                return _ObservationCount;
            }
            set
            {
                _ObservationCount = value;
                OnPropertyChanged("ObservationCount");
            }
        }

        private ICollectionView _FilteredDataSource;
        public ICollectionView FilteredDataSource
        {
            get { return _FilteredDataSource; }
            set
            {
                _FilteredDataSource = value;
                OnPropertyChanged("FilteredDataSource");
            }
        }

        // [개선] 필터링 성능 향상을 위해 HashSet 사용
        private HashSet<string> _checkedMessageNames = new HashSet<string>();

        private bool FilterMessages(object item)
        {
            if (item is ContextInfo contextInfo)
            {
                bool isNameMatch = _checkedMessageNames.Contains(contextInfo.MessageName);
                if (!isNameMatch) return false;

                if (FilterId == null || FilterId == 0)
                {
                    return true;
                }
                else
                {
                    return contextInfo.ID == FilterId;
                }
            }
            return false;
        }

        public void UpdateFilter()
        {
            if (_isUpdatingFilter)
                return;

            _isUpdatingFilter = true;

            // 필터 조건이 변경될 때, 체크된 메시지 이름 목록을 갱신
            _checkedMessageNames = new HashSet<string>(MessageItems.Where(m => m.IsChecked).Select(m => m.Name));

            try
            {
                // 기존의 필터링 로직
                _checkedMessageNames = new HashSet<string>(MessageItems.Where(m => m.IsChecked).Select(m => m.Name));

                if (FilteredDataSource != null)
                {
                    FilteredDataSource.Refresh();
                }
            }
            finally
            {
                // 4. try-finally를 사용해 작업이 성공하든 실패하든 반드시 플래그를 false로 되돌려 놓습니다.
                _isUpdatingFilter = false;
            }
        }

        public ObservableCollection<MessageItem> MessageItems { get; set; }
        //private void LoadProtoMessages(string protoFilePath)
        //{
        //    try
        //    {
        //        var protoContent = File.ReadAllText(protoFilePath);
        //        var matches = Regex.Matches(protoContent, @"message\s+(\w+)\s*{");

        //        foreach (Match match in matches)
        //        {
        //            if (match.Groups[1].Value != "LLAPosition" &&
        //                match.Groups[1].Value != "Orientation" &&
        //                match.Groups[1].Value != "Velocity" &&
        //                match.Groups[1].Value != "Vector4" &&
        //                match.Groups[1].Value != "CameraAction" &&
        //                match.Groups[1].Value != "ActionEntityInfo" &&
        //                match.Groups[1].Value != "ObservationSimulationInfo" &&
        //                match.Groups[1].Value != "ActionEntityinfo" &&
        //                match.Groups[1].Value != "ObservationEntityInfo" &&
        //                match.Groups[1].Value != "ObservationHelicopterInfo" &&
        //                match.Groups[1].Value != "ObservationUAVInfo" &&
        //                match.Groups[1].Value != "ObservationTargetInfo" &&
        //                match.Groups[1].Value != "EntityAbnormalCause" &&
        //                match.Groups[1].Value != "ObservationInterestRegion" &&
        //                match.Groups[1].Value != "enumEntityStatus" &&
        //                match.Groups[1].Value != "enumInputStatus" &&
        //                match.Groups[1].Value != "UnrealPosition")
        //            {
        //                var messageItem = new MessageItem { Name = match.Groups[1].Value, IsChecked = true };
        //                messageItem.PropertyChanged += (s, e) =>
        //                {
        //                    if (e.PropertyName == nameof(MessageItem.IsChecked))
        //                    {
        //                        UpdateFilter();
        //                    }
        //                };
        //                MessageItems.Add(messageItem);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle exceptions (e.g., file not found, read errors)
        //        Console.WriteLine(ex.Message);
        //    }
        //}

        private void LoadProtoMessages(string protoFilePath, HashSet<string> allowList)
        {
            try
            {
                // 파일이 없으면 그냥 리턴 (에러 방지)
                if (!File.Exists(protoFilePath)) return;

                var protoContent = File.ReadAllText(protoFilePath);
                var matches = Regex.Matches(protoContent, @"(message|enum)\s+(\w+)\s*{");

                foreach (Match match in matches)
                {
                    string messageName = match.Groups[2].Value;
                    bool shouldAdd = false;

                    // 전략 분기
                    if (allowList != null)
                    {
                        // [화이트리스트 모드] 허용 목록에 있는 것만 추가
                        if (allowList.Contains(messageName))
                        {
                            shouldAdd = true;
                        }
                    }
                    else
                    {
                        // [블랙리스트 모드] 제외 목록에 없는 것만 추가
                        if (!_excludedMessageNames.Contains(messageName))
                        {
                            shouldAdd = true;
                        }
                    }

                    // 추가 대상이고, 아직 리스트에 없다면 추가
                    if (shouldAdd)
                    {
                        if (!MessageItems.Any(item => item.Name == messageName))
                        {
                            var messageItem = new MessageItem { Name = messageName, IsChecked = false };
                            messageItem.PropertyChanged += (s, e) =>
                            {
                                if (e.PropertyName == nameof(MessageItem.IsChecked))
                                {
                                    UpdateFilter();
                                }
                            };
                            MessageItems.Add(messageItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading proto {protoFilePath}: {ex.Message}");
            }
        }

        private ObservableRangeCollection<ContextInfo> _MornitoringDataSource = new ObservableRangeCollection<ContextInfo>();
        public ObservableRangeCollection<ContextInfo> MornitoringDataSource
        {
            get
            {
                return _MornitoringDataSource;
            }
            set
            {
                _MornitoringDataSource = value;
                OnPropertyChanged("MornitoringDataSource");
            }
        }

        private ObservableRangeCollection<ContextInfo> _MornitoringDataSourceJoin = new ObservableRangeCollection<ContextInfo>();
        public ObservableRangeCollection<ContextInfo> MornitoringDataSourceJoin
        {
            get
            {
                return _MornitoringDataSourceJoin;
            }
            set
            {
                _MornitoringDataSourceJoin = value;
                OnPropertyChanged("MornitoringDataSourceJoin");
            }
        }




        private ContextInfo _selectedMornitoringData;
        public ContextInfo SelectedMornitoringData
        {
            get => _selectedMornitoringData;
            set
            {
                // ★★★ 2. set 에서는 값만 바꾸고, 실제 처리는 별도 메서드로 분리합니다. ★★★
                if (Equals(_selectedMornitoringData, value))
                    return;

                _selectedMornitoringData = value;
                OnPropertyChanged(nameof(SelectedMornitoringData));

                // 선택된 항목이 변경되었으니, 상세 뷰를 갱신하라고 명령합니다.
                UpdateDetailView();
            }
        }

        private void UpdateDetailView()
        {
            // 컬렉션을 새로 생성하지 않고, 기존 컬렉션의 내용만 비웁니다.
            DetailNodes.Clear();

            if (SelectedMornitoringData?.OriginalMessage != null)
            {
                var nodes = Model_Mornitoring_PopUp.SingletonInstance.ParseMessageToNodes(SelectedMornitoringData.OriginalMessage);
               
                foreach (var node in nodes)
                {
                    DetailNodes.Add(node);
                }
            }
            OnPropertyChanged(nameof(DetailNodes));
        }



        private void UpdateJoinDetailView()
        {
            // 기존 컬렉션의 내용물을 비웁니다.
            MessageTreeDataJoin.Clear();

            if (_selectedMornitoringDataJoin?.OriginalMessage != null)
            {
                var nodes = Model_Mornitoring_PopUp.SingletonInstance.ParseMessageToNodes(_selectedMornitoringDataJoin.OriginalMessage);

                // 기존 컬렉션에 아이템을 추가합니다.
                foreach (var node in nodes)
                {
                    MessageTreeDataJoin.Add(node);
                }
            }
        }


        private ContextInfo _selectedMornitoringDataJoin;
        public ContextInfo SelectedMornitoringDataJoin
        {
            get => _selectedMornitoringDataJoin;
            set
            {
                if (Equals(_selectedMornitoringDataJoin, value)) return;

                _selectedMornitoringDataJoin = value;
                OnPropertyChanged(nameof(SelectedMornitoringDataJoin));

                // Join 상세 뷰 갱신
                UpdateJoinDetailView();
            }
        }


        private int? _filterId;
        public int? FilterId
        {
            get { return _filterId; }
            set
            {
                if (_filterId != value)
                {
                    _filterId = value;
                    OnPropertyChanged(nameof(FilterId));
                    //UpdateFilter();  // 값이 변경될 때마다 필터를 새로고침
                }
            }
        }
        //public RelayCommand ApplyFilterCommand { get; set; }

        private bool _isFilteringEnabled;
        public bool IsFilteringEnabled
        {
            get { return _isFilteringEnabled; }
            set
            {
                if (_isFilteringEnabled != value)
                {
                    _isFilteringEnabled = value;
                    OnPropertyChanged(nameof(IsFilteringEnabled));
                    UpdateFilter();  // 체크박스 상태 변경 시 필터 업데이트
                }
            }
        }



        

        public void ActivatePopup()
        {
            this.IsPopupActive = true;

            // 팝업이 다시 활성화될 때, 그동안 쌓인 모든 데이터를 UI에 반영하도록
            // 필터를 한번 새로고침 해줍니다.
            Application.Current.Dispatcher.InvokeAsync(() => FilteredDataSource.Refresh());
        }

        public void DeactivatePopup()
        {
            this.IsPopupActive = false;
        }

        public RelayCommand CloseCommand { get; set; }
        // CloseCommand에서 DeactivatePopup 호출
        public void CloseCommandAction(object param)
        {
            //if (param is Window window)
            //{
            //    DeactivatePopup(); // '비활성' 상태로 전환
            //    window.Close();
            //}
            //Application.Current.MainWindow?.Close();
            Application.Current.Shutdown();
        }

        //public RelayCommand ClearMessagesCommand { get; set; }

        //private void ClearMessagesAction(object obj)
        //{
        //    MornitoringDataSource.Clear();
        //    MornitoringDataSourceJoin.Clear();
        //    // 필요하다면 다른 관련 데이터도 여기서 초기화
        //    MessageTreeData.Clear();
        //    MessageTreeDataJoin.Clear();
        //}

        private ContextInfo CreateNewContextInfo(IMessage message, ContextInfo initialContext)
        {
            // 1. 메시지의 전체 이름(네임스페이스 포함)을 가져옵니다.
            string fullName = message.Descriptor.FullName;

            // 2. 알려진 모든 네임스페이스를 순차적으로 제거합니다.
            string typeName = fullName
                .Replace("MLAHInterop.", "")
                .Replace("REALTIMEVISUAL.Native.FederateInterface.", ""); // ✅ 추가

            var finalContext = new ContextInfo
            {
                IP = initialContext.IP,
                Port = initialContext.Port,
                Protocol = initialContext.Protocol,
                ReceivedTime = DateTime.Now.ToString("HH:mm:ss"),
                MessageName = typeName, // 정리된 이름을 할당
                ID = ExtractIdFromMessage(message),
                OriginalMessage = message
            };

            return finalContext;
        }

        // 캐시 저장소 (메시지 타입별 ID 필드 정보 저장)
        private static readonly Dictionary<MessageDescriptor, FieldDescriptor> _idFieldCache = new Dictionary<MessageDescriptor, FieldDescriptor>();
        private static readonly object _cacheLock = new object();

        //public int ExtractIdFromMessage(IMessage message)
        //{
        //    var messageDescriptor = message.Descriptor;
        //    foreach (var field in messageDescriptor.Fields.InDeclarationOrder())
        //    {
        //        if (field.FieldType == Google.Protobuf.Reflection.FieldType.Message && field.IsRepeated)
        //        {
        //            // 반복 필드에 대한 처리
        //            var repeatedField = (IEnumerable)field.Accessor.GetValue(message);
        //            foreach (IMessage subMessage in repeatedField)
        //            {
        //                int id = ExtractIdFromMessage(subMessage);
        //                if (id != 0) return id;  // 배열 내에서 유효한 id를 찾으면 반환
        //            }
        //        }

        //        else if (field.Name == "id" && field.FieldType == Google.Protobuf.Reflection.FieldType.Int32)
        //        {
        //            var idValue = field.Accessor.GetValue(message);
        //            if (int.TryParse(idValue.ToString(), out int id))
        //            {
        //                return id;  // 단일 id 필드를 찾았으면 반환
        //            }
        //        }
        //    }
        //    return 0; // 'id' 필드가 없는 경우
        //}

        public int ExtractIdFromMessage(IMessage message)
        {
            if (message == null) return 0;
            var descriptor = message.Descriptor;

            FieldDescriptor idField = null;
            bool fieldFound = false;

            // 1. 캐시 확인
            lock (_cacheLock)
            {
                if (_idFieldCache.TryGetValue(descriptor, out idField))
                {
                    fieldFound = true;
                    // idField가 null이면 '이 메시지엔 ID가 없다'는 것을 의미 (이미 확인함)
                }
            }

            // 2. 캐시에 없으면 탐색 (최초 1회만 실행됨)
            if (!fieldFound)
            {
                foreach (var field in descriptor.Fields.InDeclarationOrder())
                {
                    // "id" 라는 이름의 int32 필드를 찾음
                    if (field.Name.ToLower() == "id" && field.FieldType == FieldType.Int32)
                    {
                        idField = field;
                        break;
                    }
                    // (옵션) 반복 필드 내의 ID 검색 로직이 필요하다면 여기에 추가하지만, 
                    // 보통 최상위 ID를 찾는 것이 목적이므로 생략하거나 별도 처리
                }

                // 결과 캐싱 (찾았든 못 찾았든 저장하여 재탐색 방지)
                lock (_cacheLock)
                {
                    _idFieldCache[descriptor] = idField;
                }
            }

            // 3. 필드가 존재하면 값 추출
            if (idField != null)
            {
                var val = idField.Accessor.GetValue(message);
                if (val is int intVal) return intVal;
            }

            return 0;
        }


    }


}