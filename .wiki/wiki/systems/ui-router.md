---
title: UI Router
category: systems
tags: [ui, ugui]
sources: [raw/gdd/gdd_inGame.txt, docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

# UI Router (uGUI)

Centralized popup manager. `OverlayCanvas` is `DontDestroyOnLoad` in Bootstrap. Popups instantiated on demand. uGUI-only per [[decisions/d04-ugui-not-toolkit]].

## IUIRouter

```csharp
public interface IUIRouter {
    UniTask<bool> ShowConfirm(string text);
    UniTask<QuizResult> ShowQuiz(QuizSetSO set);
    UniTask ShowLoading(string text, float seconds = 3f);
    UniTask ShowDialogue(NPCSO npc);
    void ShowEnding(EndingGrade grade);
    void ShowExpel();
}
```

## Popup catalog

| Popup | Trigger | Returns | Notes |
|---|---|---|---|
| `UIMainMenu` | bootstrap | NewGame/Continue/Exit | per GDD line 2-6 |
| `UICreateChar` | post-cutscene | playerName | inputField + confirm |
| `UILoading` | every scene transition | — | 3s black + text per GDD line 23-25 |
| `UIConfirm` | confirm interaction | OK | text from `InteractionSO.confirmText` |
| `UIQuiz` | classroom interact | `QuizResult` | 15s/câu × 10 câu × 4 đáp án |
| `UIDialogue` | NPC interact | — | InputField + Send button |
| `UIInteractPrompt` | trigger enter+facing | — | bottom-right button, mờ↔sáng |
| `UIQuestHUD` | always | — | quest title + remaining time |
| `UIClockHUD` | always | — | time + weekday/day per GDD line 17 |
| `UIScoreHUD` | always | — | face + score, top-left |
| `UIMiniMap` | World only | — | top-right with player dot |
| `UIJoystick` | mobile | — | bottom-left |
| `UIEnding` | day 30 done | — | grade + scores per GDD line 39-43 |
| `UIExpel` | renLuyen=0 | — | "Bạn bị đuổi học" |

## Quiz UI details (per GDD line 28-29)

- Timer top-right, countdown 15s, no answer = treated as wrong.
- 1 question + 4 answers. Click → answer turns green (correct) or red (wrong).
- Show "Continue" button → next question or end.
- 10 questions per set. End → unload sub-scene, return to World at the door location.

## HUD wiring via BroadcastListener

Each HUD subscribes to messages in `OnEnable`:
- `ClockHUD` → `TimeTickMsg`
- `ScoreHUD` → `ScoreChangedMsg`
- `QuestHUD` → `QuestActivatedMsg`, `QuestCompletedMsg`
- `InteractPrompt` → `InteractZoneEnteredMsg`, `InteractZoneExitedMsg`

## Backlinks
- [[systems/scene-flow]]
- [[systems/quest-system]]
- [[systems/interaction-system]]
- [[decisions/d04-ugui-not-toolkit]]
