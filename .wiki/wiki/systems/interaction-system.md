---
title: Interaction System
category: systems
tags: [interaction, so, trigger]
sources: [raw/gdd/gdd_inGame.txt, docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

# Interaction System

Trigger collider + uGUI prompt + `InteractionSO` strategy. Per GDD line 19: "nút này sẽ sáng khi mà người chơi quay camera đến vị trí có thể tương tác".

## Components

- `InteractableMarker` (MonoBehaviour on world object): has trigger collider + reference to `InteractableSO`.
- `PlayerInteractor` (on player): handles trigger enter/exit + facing check + Input action callback.
- `InteractionRouter` (service): routes `InteractPressedMsg` to the target's `InteractionSO.Execute`.

## InteractableSO

```csharp
public class InteractableSO : ScriptableObject {
    public AreaSO area;
    public InteractionSO onInteract;
    public Condition gate;   // optional, e.g. requires nighttime
}
```

## InteractionSO base + concretes

```csharp
public abstract class InteractionSO : ScriptableObject {
    public string promptText;
    public Sprite icon;
    public abstract UniTask Execute(InteractionContext ctx);
}
```

| Concrete | Behavior |
|---|---|
| `OpenConfirmInteractionSO` | `await UIRouter.ShowConfirm(text)` |
| `OpenQuizInteractionSO` | `await UIRouter.ShowQuiz(quizSet)` |
| `OpenDialogueInteractionSO` | `await UIRouter.ShowDialogue(npc)` |
| `SceneTransitionInteractionSO` | additive load subscene |
| `SkipTimeInteractionSO` | `GameClock.SkipTo(hour, minute)` |

## Detection flow

```
PlayerInteractor.OnTriggerEnter(other):
   if other has InteractableMarker → currentZone = marker
   → Broadcast InteractZoneEnteredMsg

Update():
   if currentZone != null && Vector3.Dot(player.forward, toZone) > 0.5:
      UIInteractPrompt.Show(currentZone.interactable.promptText)
   else: UIInteractPrompt.Hide()

OnInteract input action callback:
   if currentZone != null:
      Broadcast InteractPressedMsg{currentZone.interactable}

InteractionRouter (subscribes InteractPressedMsg):
   if QuestRouter.IsInteractableAllowed(target) == false:
      UIToast.Show("Chưa tới giờ làm việc này")
   else: await target.onInteract.Execute(ctx)
```

## Gating per quest

Per [[systems/quest-system]] § Gating, interaction is rejected if `target.area != ActiveQuestRSO.current.area`. Prevents the GDD-stated "Đến các khu vực khác ở trong nhiệm vụ không được giao thì không thể tương tác".

## Backlinks
- [[systems/quest-system]]
- [[systems/ui-router]]
- [[entities/player]]
- [[decisions/d01-so-first-modular]]
