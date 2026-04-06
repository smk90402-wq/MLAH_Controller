using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Google.Protobuf;
using Newtonsoft.Json.Linq;

namespace MLAH_Mornitoring_UDP
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


        private ConcurrentQueue<ContextInfo> _messageQueue = new ConcurrentQueue<ContextInfo>();
        private DispatcherTimer _messageQueueTimer;
        public event EventHandler<List<ContextInfo>> MessagesAvailable;


        private Model_Mornitoring_PopUp()
        {
            _messageQueueTimer = new DispatcherTimer(
                     TimeSpan.FromMilliseconds(33), // 약 30Hz
                     DispatcherPriority.Background,
                     MessageQueue_Timer_Tick,
                     System.Windows.Application.Current.Dispatcher);

            _messageQueueTimer.Start(); // 타이머 시작


        }


        public void EnqueueMessage(ContextInfo message)
        {
            _messageQueue.Enqueue(message);
        }

        private void MessageQueue_Timer_Tick(object sender, EventArgs e)
        {
            if (_messageQueue.IsEmpty) return;

            var messages = new List<ContextInfo>();
            while (_messageQueue.TryDequeue(out var message))
            {
                messages.Add(message);
            }
            MessagesAvailable?.Invoke(this, messages);
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

                foreach (var field in descriptor.Fields.InDeclarationOrder())
                {
                    var fieldValue = field.Accessor.GetValue(currentMessage);
                    var node = new MessageNode { Name = field.Name };

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

        public ObservableCollection<MessageNode> ParseObjectToNodes(object obj)
        {
            var rootNodes = new ObservableCollection<MessageNode>();
            if (obj == null) return rootNodes;
            ParseObjectRecursive(obj, rootNodes, new HashSet<object>());
            return rootNodes;
        }

        private void ParseObjectRecursive(object obj, ObservableCollection<MessageNode> parentNodes, HashSet<object> visited)
        {
            if (obj == null || !visited.Add(obj)) return;

            Type type = obj.GetType();

            // 1. 바이트 배열 처리
            if (obj is byte[] byteArray)
            {
                parentNodes.Add(new MessageNode { Name = "Bytes", Value = $"[{byteArray.Length} bytes]" });
                return;
            }

            // 2. 리스트/배열 처리
            if (obj is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    var childNode = new MessageNode { Name = $"[{i}]" };
                    parentNodes.Add(childNode);

                    if (item != null && (item.GetType().IsClass && !(item is string)))
                    {
                        childNode.Value = $"({item.GetType().Name})";
                        ParseObjectRecursive(item, childNode.Children, new HashSet<object>(visited));
                    }
                    else
                    {
                        childNode.Value = item?.ToString() ?? "null";
                    }
                }
                return;
            }

            // 3. JObject (JSON) 처리
            if (obj is JObject jObj)
            {
                foreach (var prop in jObj.Properties())
                {
                    var node = new MessageNode { Name = prop.Name, Value = prop.Value.ToString() };
                    parentNodes.Add(node);
                }
                return;
            }

            // =========================================================
            // ★★★ [수정 핵심] 필드(Fields)와 프로퍼티(Properties) 모두 조회 ★★★
            // =========================================================

            // A. 필드 조회 (public 변수)
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var node = new MessageNode { Name = field.Name };
                var value = field.GetValue(obj);
                ProcessMemberValue(value, field.FieldType, node, visited);
                parentNodes.Add(node);
            }

            // B. 프로퍼티 조회 (get; set;)
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (prop.GetIndexParameters().Length > 0) continue; // 인덱서 제외

                var node = new MessageNode { Name = prop.Name };
                var value = prop.GetValue(obj);
                ProcessMemberValue(value, prop.PropertyType, node, visited);
                parentNodes.Add(node);
            }
        }

        // [추가] 값 처리 로직을 공통 함수로 분리 (중복 제거)
        private void ProcessMemberValue(object value, Type memberType, MessageNode node, HashSet<object> visited)
        {
            if (value == null)
            {
                node.Value = "null";
            }
            else if (memberType.IsPrimitive || memberType == typeof(string) || memberType.IsEnum || memberType == typeof(DateTime))
            {
                node.Value = value.ToString();
            }
            else
            {
                node.Value = $"({memberType.Name})";
                ParseObjectRecursive(value, node.Children, new HashSet<object>(visited));
            }
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

        public MessageNode()
        {
            Children = new ObservableCollection<MessageNode>();
        }
    }


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

        
        
        public object OriginalObject { get; set; }

       

    }

    public class MessageItem : CommonBase
    {
        // [확인 후 삭제] 미사용 필드 - _IsChecked와 중복, 어디서도 참조 안됨
        //private bool isChecked;
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
