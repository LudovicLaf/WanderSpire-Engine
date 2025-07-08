using System;

namespace Game.Events
{
    public static class GameEventBus
    {
        public static class Event<T>
        {
            public static event Action<T> Handlers = delegate { };
            public static void Publish(T ev) => Handlers(ev);
            public static void Subscribe(Action<T> h) => Handlers += h;
            public static void Unsubscribe(Action<T> h) => Handlers -= h;
        }
    }

}
