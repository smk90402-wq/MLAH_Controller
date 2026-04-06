//using System.Collections.ObjectModel;
//using System.Collections.Specialized;
//using System.ComponentModel;
//using System.Linq;
//using MLAH_Contoller;

//namespace MLAH_Controller
//{
//    public class ViewModel_UC_Unit_Tree : CommonBase
//    {
//        #region Singleton
//        private static ViewModel_UC_Unit_Tree _instance = null;
//        public static ViewModel_UC_Unit_Tree SingletonInstance
//        {
//            get
//            {
//                if (_instance == null)
//                {
//                    _instance = new ViewModel_UC_Unit_Tree();
//                }
//                return _instance;
//            }
//        }
//        #endregion

//        // Model
//        //private Model_CompScenario _model;
//        //public Model_CompScenario Model
//        //{
//        //    get => _model;
//        //    set
//        //    {
//        //        _model = value;
//        //        OnPropertyChanged(nameof(Model));
//        //    }
//        //}

//        private UnitTreeItem _selectedTreeItem;
//        public UnitTreeItem SelectedTreeItem
//        {
//            get => _selectedTreeItem;
//            set
//            {
//                _selectedTreeItem = value;
//                OnPropertyChanged(nameof(SelectedTreeItem));
//                UpdateSelectedNodeState();
//            }
//        }

//        // 최종 트리 항목: Self-Referential
//        private ObservableCollection<UnitTreeItem> _allTreeItems
//            = new ObservableCollection<UnitTreeItem>();
//        public ObservableCollection<UnitTreeItem> AllTreeItems
//        {
//            get => _allTreeItems;
//            set
//            {
//                _allTreeItems = value;
//                OnPropertyChanged(nameof(AllTreeItems));
//            }
//        }

//        // 카테고리 ID 상수 (루트 노드)
//        private const int ID_LAH = 1;
//        private const int ID_UAV = 2;
//        private const int ID_FriendHostile = 5;

//        // 아이템 노드 Id 증가
//        private int _nextItemId = 100;

//        public ViewModel_UC_Unit_Tree()
//        {
//            // 샘플 Model
//            //Model = new Model_CompScenario();
//            //// 샘플 데이터
//            //Model.LAHList.Add(new CompScenario_LAH { Name = "LAH-Test1" });
//            //Model.LAHList.Add(new CompScenario_LAH { Name = "LAH-Test2" });
//            //Model.UAVList.Add(new CompScenario_UAV { Name = "UAV-Alpha" });
//            //Model.UAVList.Add(new CompScenario_UAV { Name = "UAV-Bravo" });

//            // 초기화
//            InitializeTree();
//        }

//        //public ViewModel_UC_Comp_Tree(Model_CompScenario model)
//        //{
//        //    this.Model = model;
//        //    InitializeTree();
//        //}

//        private void InitializeTree()
//        {
//            // 1) 카테고리 노드(루트)
//            AllTreeItems.Clear();
//            AllTreeItems.Add(new UnitTreeItem { Id = ID_LAH, ParentId = 0, Title = "LAH(유인공격헬기)" });
//            AllTreeItems.Add(new UnitTreeItem { Id = ID_FriendHostile, ParentId = 0, Title = "아/적군 객체" });
//            AllTreeItems.Add(new UnitTreeItem { Id = ID_UAV, ParentId = 0, Title = "UAV(무인기)" });

//            // 2) 기존 목록 -> 아이템 노드 변환
//            foreach (var lah in ViewModel_Comp_ScenarioMainView.SingletonInstance.model_CompScenario.LAHList)
//                AddLAHItem(lah);


//            // 3) CollectionChanged
//            ViewModel_Comp_ScenarioMainView.SingletonInstance.model_CompScenario.LAHList.CollectionChanged += OnLAHListChanged;


//            // KUHList, MovingList, FixedList 등도 모두 구독
//        }

//        #region 동기화 (CollectionChanged)
//        private void OnLAHListChanged(object sender, NotifyCollectionChangedEventArgs e)
//        {
//            if (e.Action == NotifyCollectionChangedAction.Add)
//            {
//                foreach (var newItem in e.NewItems)
//                {
//                    AddLAHItem((CompScenario_LAH)newItem);
//                }
//            }
//            else if (e.Action == NotifyCollectionChangedAction.Remove)
//            {
//                foreach (var oldItem in e.OldItems)
//                {
//                    RemoveItem(oldItem);
//                }
//            }
//        }
       
//        #endregion

//        #region 아이템 추가/제거 함수
//        private void AddLAHItem(CompScenario_LAH lah)
//        {
//            int newId = System.Threading.Interlocked.Increment(ref _nextItemId);
//            var node = new UnitTreeItem
//            {
//                Id = newId,
//                ParentId = ID_LAH,
//                Title = lah.Name,
//                RefModel = lah
//            };
//            AllTreeItems.Add(node);
//            // 새로 추가한 아이템을 바로 선택(포커스)
//            SelectedTreeItem = node;

//            // (1) LAH 객체(CompScenario_LAH)가 INotifyPropertyChanged 구현 (Name 속성 변경 시 이벤트 발생)
//            lah.PropertyChanged += (s, e) =>
//            {
//                // (2) Name 속성이 바뀌면, TreeItem.Title을 갱신
//                if (e.PropertyName == nameof(lah.Name))
//                {
//                    node.Title = lah.Name;
//                }
//            };
//        }

       

//        private void RemoveItem(object oldModel)
//        {
//            // 해당 model을 RefModel로 가진 TreeItem을 찾아 제거
//            var found = AllTreeItems.FirstOrDefault(x => x.RefModel == oldModel);
//            if (found != null)
//            {
//                AllTreeItems.Remove(found);
//            }
//        }
//        #endregion

//        private void UpdateSelectedNodeState()
//        {
//            if (SelectedTreeItem == null)
//            {
//                return;
//            }
//            // 어떤 모델인지 확인
//            if (SelectedTreeItem.RefModel is CompScenario_LAH lahObj)
//            {
//                // LAHList 아이템
//                CommonEvent.OnTreeItemSelectedLAH?.Invoke(lahObj);
//            }
//        }
//    }

//    // Self-Referential 아이템
//    public class UnitTreeItem : CommonBase
//    {
//        private int _id;
//        public int Id
//        {
//            get => _id;
//            set
//            {
//                if (_id != value)
//                {
//                    _id = value;
//                    OnPropertyChanged(nameof(Id));
//                }
//            }
//        }

//        private int _parentId;
//        public int ParentId
//        {
//            get => _parentId;
//            set
//            {
//                if (_parentId != value)
//                {
//                    _parentId = value;
//                    OnPropertyChanged(nameof(ParentId));
//                }
//            }
//        }

//        private string _title;
//        public string Title
//        {
//            get => _title;
//            set
//            {
//                if (_title != value)
//                {
//                    _title = value;
//                    OnPropertyChanged(nameof(Title));
//                }
//            }
//        }

//        public object RefModel { get; set; }

//    }
//}
