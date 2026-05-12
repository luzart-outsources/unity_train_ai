---
title: BroadcastService
category: systems
tags: [events, broadcast, pubsub]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

# BroadcastService

Typed static pub/sub. Event backbone per [[decisions/d02-broadcast-service]].

## API

```csharp
public static class BroadcastService {
    static readonly Dictionary<Type, Delegate> _handlers = new();

    public static void Subscribe<T>(Action<T> handler) where T : struct {
        if (_handlers.TryGetValue(typeof(T), out var d))
            _handlers[typeof(T)] = Delegate.Combine(d, handler);
        else _handlers[typeof(T)] = handler;
    }
    public static void Unsubscribe<T>(Action<T> handler) where T : struct {
        if (_handlers.TryGetValue(typeof(T), out var d)) {
            var nd = Delegate.Remove(d, handler);
            if (nd == null) _handlers.Remove(typeof(T));
            else _handlers[typeof(T)] = nd;
        }
    }
    public static void Send<T>(T msg) where T : struct {
        if (_handlers.TryGetValue(typeof(T), out var d))
            ((Action<T>)d)?.Invoke(msg);
    }
    public static void Clear() => _handlers.Clear();
}
```

## Conventions

- Messages are `struct` named `XxxMsg`.
- Listener subscribes in `OnEnable`, unsubscribes in `OnDisable`.
- `Clear()` called when loading MainMenu — prevents leaked listeners from prior playthrough.

## Message catalog

```
DayStartedMsg{day, weekday}        DayEndedMsg{day}
TimeTickMsg{hour, minute}
QuestActivatedMsg{quest}           QuestCompletedMsg{quest, success, scoreDelta}
QuestMissedMsg{quest}
ScoreChangedMsg{hocTap, renLuyen}
InteractZoneEnteredMsg{target}     InteractZoneExitedMsg{target}
InteractPressedMsg{target}
SceneTransitionRequestedMsg{scene}
QuizStartedMsg{set}                QuizEndedMsg{result}
DialogueRequestMsg{npc, input}     DialogueRepliedMsg{npc, text}
ExpelTriggeredMsg{}                GameEndedMsg{grade}
```

## Helper: `BroadcastListenerComponent<T>`

For Inspector-friendly wiring (animator triggers, AudioPlayer):

```csharp
public abstract class BroadcastListenerComponent<T> : MonoBehaviour where T : struct {
    [SerializeField] UnityEvent<T> response;
    void OnEnable()  => BroadcastService.Subscribe<T>(OnMsg);
    void OnDisable() => BroadcastService.Unsubscribe<T>(OnMsg);
    void OnMsg(T m)  => response?.Invoke(m);
}
public class DayStartedListener : BroadcastListenerComponent<DayStartedMsg> { }
```

Drops on a GameObject, wire UnityEvent in Inspector.

## Backlinks
- [[decisions/d02-broadcast-service]]
- [[technical/architecture]]
