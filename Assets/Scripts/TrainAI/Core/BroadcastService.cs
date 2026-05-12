using System;
using System.Collections.Generic;

namespace TrainAI.Core
{
    public static class BroadcastService
    {
        static readonly Dictionary<Type, Delegate> _handlers = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            if (_handlers.TryGetValue(typeof(T), out var d))
                _handlers[typeof(T)] = Delegate.Combine(d, handler);
            else
                _handlers[typeof(T)] = handler;
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            if (!_handlers.TryGetValue(typeof(T), out var d)) return;
            var nd = Delegate.Remove(d, handler);
            if (nd == null) _handlers.Remove(typeof(T));
            else _handlers[typeof(T)] = nd;
        }

        public static void Send<T>(T msg) where T : struct
        {
            if (_handlers.TryGetValue(typeof(T), out var d))
                ((Action<T>)d)?.Invoke(msg);
        }

        public static void Clear() => _handlers.Clear();
    }
}
