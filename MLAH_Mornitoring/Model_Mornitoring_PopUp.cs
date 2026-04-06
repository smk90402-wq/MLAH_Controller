using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Google.Protobuf;

namespace MLAH_Mornitoring
{
    class Model_Mornitoring_PopUp : CommonBase
    {
        #region Singleton
  
        private static readonly Lazy<Model_Mornitoring_PopUp> _lazyInstance = new Lazy<Model_Mornitoring_PopUp>(() => new Model_Mornitoring_PopUp());

        /// <summary>
        /// 싱글턴 로직 public 인스턴스.
        /// </summary>
        public static Model_Mornitoring_PopUp SingletonInstance => _lazyInstance.Value;

        #endregion Singleton

        //private Tuple<IMessage, ContextInfo> _lastReceivedMessage;
        //private readonly object _messageLock = new object(); // 스레드 동기화를 위한 lock 객체


        private ConcurrentQueue<Tuple<IMessage, ContextInfo>> gRPCmessageQueue = new ConcurrentQueue<Tuple<IMessage, ContextInfo>>();

        private DispatcherTimer gRPCmessageQueue_dispatcherTimer;
        

        public event EventHandler<List<Tuple<IMessage, ContextInfo>>> gRPCMessagesAvailable;


        private Model_Mornitoring_PopUp()
        {
            // 일반 메시지 타이머 (30Hz에 맞춰 약 33ms로 설정)
            gRPCmessageQueue_dispatcherTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(33),
                DispatcherPriority.Background, // UI 반응성을 위해 Background 우선순위 사용
                gRPCmessageQueue_dispatcherTimer_Tick,
                System.Windows.Application.Current.Dispatcher);

   
        }

        private readonly Dictionary<string, HashSet<string>> _highlightTargets = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// 색칠하고 싶은 메시지 타입과 필드명을 등록하는 함수
        /// 예: AddHighlightTarget("ObservationHelicopterInfo", "fuel");
        /// </summary>
        public void AddHighlightTarget(string messageName, string fieldName)
        {
            if (!_highlightTargets.ContainsKey(messageName))
            {
                _highlightTargets[messageName] = new HashSet<string>();
            }
            _highlightTargets[messageName].Add(fieldName);
        }


        public void EnqueueMessage(Tuple<IMessage, ContextInfo> message)
        {
            gRPCmessageQueue.Enqueue(message);

            JsonFileLogger.Instance.EnqueueLog(message.Item1, message.Item2.MessageName);
            
        }



      
        private void gRPCmessageQueue_dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (gRPCmessageQueue.IsEmpty) return;

            var messages = new List<Tuple<IMessage, ContextInfo>>();
            while (gRPCmessageQueue.TryDequeue(out var message))
            {
                messages.Add(message);
            }
            // 200ms 동안 쌓인 메시지 묶음을 이벤트로 '한 번만' 전달
            gRPCMessagesAvailable?.Invoke(this, messages);
        }
  
        public ObservableCollection<MessageNode> ParseMessageToNodes(IMessage message)
        {
            if (message == null)
            {
                return new ObservableCollection<MessageNode>();
            }

            var rootNodes = new ObservableCollection<MessageNode>();
            // 스택 아이템: (처리할 메시지, 부모 노드 컬렉션, 현재 경로에서 방문한 메시지 Set)
            var stack = new Stack<Tuple<IMessage, ObservableCollection<MessageNode>, HashSet<IMessage>>>();

            // 시작점: 최상위 메시지, 루트 노드, 그리고 새로운 방문 기록용 HashSet 추가
            stack.Push(Tuple.Create(message, rootNodes, new HashSet<IMessage>()));

            while (stack.Count > 0)
            {
                var (currentMessage, parentNodes, visited) = stack.Pop();

                // ★★★ 핵심: 현재 메시지를 방문 목록에 추가 ★★★
                if (!visited.Add(currentMessage))
                {
                    // 이미 이 경로에서 처리한 메시지라면 순환이므로 건너뛴다.
                    continue;
                }

                var descriptor = currentMessage.Descriptor;

                // 👇 추가: 현재 파싱 중인 메시지가 색칠 대상 목록에 있는지 확인
                string currentMsgName = descriptor.Name;
                bool hasHighlightTarget = _highlightTargets.ContainsKey(currentMsgName);

                foreach (var field in descriptor.Fields.InDeclarationOrder())
                {
                    var fieldValue = field.Accessor.GetValue(currentMessage);
                    var node = new MessageNode { Name = field.Name };

                    if (hasHighlightTarget && _highlightTargets[currentMsgName].Contains(field.Name))
                    {
                        node.IsHighlighted = true;
                    }

                    if (fieldValue is IMessage nestedMessage)
                    {
                        node.Value = $"({nestedMessage.Descriptor.Name})";
                        parentNodes.Add(node);

                        // ★★★ 핵심: 자식 노드로 내려갈 때 현재까지의 방문 기록을 복사해서 넘겨준다. ★★★
                        stack.Push(Tuple.Create(nestedMessage, node.Children, new HashSet<IMessage>(visited)));
                    }
                    else if (fieldValue is IList list)
                    {
                        node.Value = $"[Count: {list.Count}]";
                        parentNodes.Add(node);

                        for (int i = 0; i < list.Count; i++)
                        {
                            var item = list[i];
                            var childNode = new MessageNode { Name = $"[{i}]" };

                            if (item is IMessage listItemMessage)
                            {
                                childNode.Value = $"({listItemMessage.Descriptor.Name})";
                                node.Children.Add(childNode);

                                // 리스트 아이템에 대해서도 방문 기록을 복사해서 넘겨준다.
                                stack.Push(Tuple.Create(listItemMessage, childNode.Children, new HashSet<IMessage>(visited)));
                            }
                            else
                            {
                                childNode.Value = item?.ToString() ?? "null";
                                node.Children.Add(childNode);
                            }
                        }
                    }
                    else
                    {
                        node.Value = fieldValue?.ToString() ?? "null";
                        parentNodes.Add(node);
                    }
                }
            }

            return rootNodes;
        }

    }


    

    public class MessageNode : CommonBase
    {
        // 노드의 이름 (필드명 또는 인덱스)
        public string Name { get; set; }

        // 노드의 값 (단순 타입일 경우에만 표시)
        public string Value { get; set; }

        // 자식 노드들의 컬렉션. 이 프로퍼티가 트리 구조의 핵심입니다.
        public ObservableCollection<MessageNode> Children { get; set; }

        private bool _isHighlighted;
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                _isHighlighted = value;
                OnPropertyChanged(nameof(IsHighlighted));
            }
        }

        public MessageNode()
        {
            Children = new ObservableCollection<MessageNode>();
        }
    }

    // [확인 후 삭제] 미사용 클래스 - 주석처리된 코드에서만 참조됨 (line 321-322)
    //public class FieldValuePair : CommonBase
    //{
    //    private string _FieldName = "";
    //    public string FieldName
    //    {
    //        get { return _FieldName; }
    //        set { _FieldName = value; OnPropertyChanged("FieldName"); }
    //    }
    //    private string _FieldValue = "";
    //    public string FieldValue
    //    {
    //        get { return _FieldValue; }
    //        set { _FieldValue = value; OnPropertyChanged("FieldValue"); }
    //    }
    //}

    public class ContextInfo : CommonBase
    {

        public ContextInfo()
        {
            // NullReferenceException 방지를 위해 생성자에서 초기화
            FieldNodes = new ObservableCollection<MessageNode>();
        }

        private string _IP;
        public string IP
        {
            get
            {
                return _IP;
            }
            set
            {
                _IP = value;
                OnPropertyChanged("IP");
            }
        }

        private string _Protocol;
        public string Protocol
        {
            get
            {
                return _Protocol;
            }
            set
            {
                _Protocol = value;
                OnPropertyChanged("Protocol");
            }
        }

        private string _Port;
        public string Port
        {
            get
            {
                return _Port;
            }
            set
            {
                _Port = value;
                OnPropertyChanged("Port");
            }
        }

        private string _MessageName;
        public string MessageName
        {
            get
            {
                return _MessageName;
            }
            set
            {
                _MessageName = value;
                OnPropertyChanged("MessageName");
            }
        }

        private string _ReceivedTime;
        public string ReceivedTime
        {
            get
            {
                return _ReceivedTime;
            }
            set
            {
                _ReceivedTime = value;
                OnPropertyChanged("ReceivedTime");
            }
        }

        //private List<FieldValuePair>  _fieldValues = new List<FieldValuePair>();
        //public List<FieldValuePair> fieldValues
        //{
        //    get
        //    {
        //        return _fieldValues;
        //    }
        //    set
        //    {
        //        _fieldValues = value;
        //        OnPropertyChanged("fieldValues");
        //    }
        //}

        

        private int _ID;
        public int ID
        {
            get
            {
                return _ID;
            }
            set
            {
                _ID = value;
                OnPropertyChanged("ID");
            }
        }

        public ObservableCollection<MessageNode> FieldNodes { get; set; }

        // ★★★ 원본 Protobuf 메시지를 저장할 속성 추가 ★★★
        [System.ComponentModel.Browsable(false)] // UI에 불필요하게 표시되지 않도록 설정
        public IMessage OriginalMessage { get; set; }

    }

    public class MessageItem : CommonBase
    {
        
        public string Name { get; set; }

        private bool _IsChecked = true;
        public bool IsChecked
        {
            get { return _IsChecked; }
            set
            {
                if (_IsChecked != value)
                {
                    _IsChecked = value;
                    OnPropertyChanged("IsChecked");
                    //ViewModel_Mornitoring_PopUp.SingletonInstance.UpdateFilter();
                }
            }
        }
        //public int Id { get; set; }
    }

}
