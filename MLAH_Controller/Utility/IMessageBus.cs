using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MLAH_Controller
{
    public interface IMessageBus
    {
        void Subscribe<T>(Action<T> action);
        void Unsubscribe<T>(Action<T> action);
        void Publish<T>(T message);
    }

    public class MessageBus : IMessageBus
    {
        private readonly Dictionary<Type, List<object>> _subscribers = new Dictionary<Type, List<object>>();
        private readonly object _lock = new object();

        public void Subscribe<T>(Action<T> action)
        {
            lock (_lock)
            {
                var messageType = typeof(T);
                if (!_subscribers.ContainsKey(messageType))
                {
                    _subscribers[messageType] = new List<object>();
                }
                _subscribers[messageType].Add(action);
            }
        }

        public void Unsubscribe<T>(Action<T> action)
        {
            lock (_lock)
            {
                var messageType = typeof(T);
                if (_subscribers.ContainsKey(messageType))
                {
                    _subscribers[messageType].Remove(action);
                }
            }
        }

        public void Publish<T>(T message)
        {
            var messageType = typeof(T);
            List<object> actions;
            lock (_lock)
            {
                if (!_subscribers.ContainsKey(messageType)) return;
                actions = _subscribers[messageType].ToList(); // 복사본 생성
            }

            foreach (var action in actions)
            {
                ((Action<T>)action)(message);
            }
        }
    }

    public class MapUnitClickedMessage
    {
        public uint UnitID { get; }
        public MapUnitClickedMessage(uint unitId)
        {
            UnitID = unitId;
        }
    }
}
