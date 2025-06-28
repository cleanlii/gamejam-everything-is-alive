using UnityEngine;

namespace PackageGame.Level.Gameplay
{
    public class EventManager : MonoBehaviour
    {
        private EventData _defaultEvent;

        public void Initialize()
        {
            RegisterDefaultEvent();
        }

        private void RegisterDefaultEvent()
        {
            _defaultEvent = new EventData
            {
                eventName = "随机事件"
            };
        }

        public EventData GetDefaultEvent()
        {
            return _defaultEvent;
        }
    }

    public class Event
    {
    }

    public class TaskEvent : Event
    {
    }

    public class StoreEvent : Event
    {
    }

    public class RandomEvent : Event
    {
    }

    public enum EventType
    {
        Task,
        Store,
        Random
    }

    public class EventData
    {
        public string eventName;
    }
}