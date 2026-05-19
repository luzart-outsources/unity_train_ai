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
            if (!_handlers.TryGetValue(typeof(T), out var d)) return;
            // Iterate the invocation list one subscriber at a time and trap
            // exceptions per-subscriber. Previously a single throwing handler
            // would (a) short-circuit the multicast chain so later subscribers
            // never ran, and (b) propagate the throw up to the Send caller.
            // Since Send sits inside GameClockService.Tick (TimeTickMsg) and
            // QuestRouter completion paths, an unhandled subscriber throw
            // froze the game loop. Catching here keeps the broadcast bus
            // healthy regardless of subscriber bugs.
            var invocations = d.GetInvocationList();
            for (int i = 0; i < invocations.Length; i++)
            {
                try { ((Action<T>)invocations[i])(msg); }
                catch (Exception e) { UnityEngine.Debug.LogWarning($"[Broadcast<{typeof(T).Name}>] subscriber #{i} threw: {e.Message}"); }
            }
        }

        public static void Clear() => _handlers.Clear();
    }
}
