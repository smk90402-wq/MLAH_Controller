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
//using DevExpress.Xpf.CodeView;
//using DevExpress.Xpf.Grid;
using Google.Protobuf;
using Application = System.Windows.Application;
//using Grpc.Core;


namespace MLAH_Mornitoring_UDP
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
            //LoadProtoMessages("MLAHIntefaceProto.proto");
            LoadUdpMessageTypes();
            UpdateFilter();

            Model_Mornitoring_PopUp.SingletonInstance.MessagesAvailable += OnMessagesAvailable;        

        }

        private const int MAX_MESSAGE_COUNT = 1000000;
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


        private bool _isRefreshScheduled = false;
        private void OnMessagesAvailable(object sender, List<ContextInfo> messages)
        {
            // 데이터 가공 로직이 필요 없으므로 contextInfos를 바로 사용
            if (!messages.Any()) return;

            var previouslySelectedItem = this.SelectedMornitoringData;

            MornitoringDataSource.AddRange(messages);
            if (MornitoringDataSource.Count > MAX_MESSAGE_COUNT)
            {
                int removeCount = MornitoringDataSource.Count - MAX_MESSAGE_COUNT;
                MornitoringDataSource.RemoveRange(0, removeCount);
            }

            if (IsPopupActive && !_isRefreshScheduled)
            {
                _isRefreshScheduled = true;
                Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(200);

                    FilteredDataSource.Refresh();

                    if (previouslySelectedItem != null && MornitoringDataSource.Contains(previouslySelectedItem))
                    {
                        this.SelectedMornitoringData = previouslySelectedItem;
                    }

                    _isRefreshScheduled = false;
                });
            }

        }



        #endregion 생성자 & 콜백

    

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
        private void LoadUdpMessageTypes()
        {
            // 모니터링할 UDP 메시지 클래스 이름들을 여기에 직접 추가합니다.
            var udpMessageNames = new List<string>
        {
            "SWStatus",
            "SensorControlCommand",
            "OperatingCommand", 
            "LAHMissionPlan",
            "UAVMissionPlan",
            "MissionPlanOptionInfo",
            "InitScenario",
            "LAHStatesList",
            "SensorInfo",
            "UAVMalFunctionCommand",
            "LAHMalFunctionState",
            "Lah_States",
            // ... 추가할 다른 UDP 메시지 이름 ...
        };

            // MessageItems 컬렉션을 채웁니다.
            foreach (var name in udpMessageNames)
            {
                var messageItem = new MessageItem { Name = name, IsChecked = true };
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
            DetailNodes.Clear();

            // OriginalMessage 대신 OriginalObject를 확인합니다.
            if (SelectedMornitoringData?.OriginalObject != null)
            {
                // ★★★ Model의 파싱 함수를 호출합니다. ★★★
                var nodes = Model_Mornitoring_PopUp.SingletonInstance.ParseObjectToNodes(SelectedMornitoringData.OriginalObject);

                foreach (var node in nodes)
                {
                    DetailNodes.Add(node);
                }
            }
        }



        //private void UpdateJoinDetailView()
        //{
        //    // 기존 컬렉션의 내용물을 비웁니다.
        //    MessageTreeDataJoin.Clear();

        //    if (_selectedMornitoringDataJoin?.OriginalMessage != null)
        //    {
        //        var nodes = Model_Mornitoring_PopUp.SingletonInstance.ParseMessageToNodes(_selectedMornitoringDataJoin.OriginalMessage);

        //        // 기존 컬렉션에 아이템을 추가합니다.
        //        foreach (var node in nodes)
        //        {
        //            MessageTreeDataJoin.Add(node);
        //        }
        //    }
        //}


        //private ContextInfo _selectedMornitoringDataJoin;
        //public ContextInfo SelectedMornitoringDataJoin
        //{
        //    get => _selectedMornitoringDataJoin;
        //    set
        //    {
        //        if (Equals(_selectedMornitoringDataJoin, value)) return;

        //        _selectedMornitoringDataJoin = value;
        //        OnPropertyChanged(nameof(SelectedMornitoringDataJoin));

        //        // Join 상세 뷰 갱신
        //        UpdateJoinDetailView();
        //    }
        //}


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

     
   

      


    }


}