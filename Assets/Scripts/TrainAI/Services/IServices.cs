using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TrainAI.SO.Base;

namespace TrainAI.Services
{
    public interface IGameClock
    {
        int Day { get; }
        int Hour { get; }
        int Minute { get; }
        bool Frozen { get; }

        void Tick(float realDeltaSec);
        void Freeze();
        void Resume();
        void SkipTo(int hour, int minute);
        void AdvanceDay();
    }

    public interface IQuestRouter
    {
        QuestSO Current { get; }
        void StartDay(int day);
        void Tick(float dt);
        void Complete(QuestSO quest, bool success);
        bool IsInteractableAllowed(InteractableSO interactable);
        string GetTodaySummary();
    }

    public interface ISceneRouter
    {
        UniTask LoadAdditive(SceneRefSO scene, string transitionText = null);
        UniTask UnloadAdditive(SceneRefSO scene);
        UniTask LoadSingle(SceneRefSO scene);
        // Replaces all currently loaded scenes with the target (additive load
        // first, then unloads olds). Use for "enter building / change area"
        // transitions where the previous scene shouldn't stay in memory.
        UniTask LoadReplacing(SceneRefSO target, string transitionText = null);
    }

    public interface IScoreSystem
    {
        void ApplyDelta(int hocTapDelta, int renLuyenDelta, string source);
        void OnQuizResult(int correctCount, int totalCount, SubjectSO subject);
    }

    public interface IInteractionRouter
    {
        UniTask HandlePressed(InteractableSO interactable);
    }

    public interface INPCDirector
    {
        void RegisterNpcTransform(string npcId, Transform t);
        void Tick(float dt);
    }

    public interface IDialogueService
    {
        UniTask<string> Reply(NPCSO npc, string userInput);
    }

    public interface IMovementService
    {
        void RegisterAgent(Transform npc, MovementStrategySO strategy);
        void SetTarget(Transform npc, Vector3 target);
        void Unregister(Transform npc);
        void Tick(float dt);
    }

    public interface IUIRouter
    {
        UniTask<bool> ShowConfirm(string text);
        UniTask<QuizResult> ShowQuiz(QuizSetSO set);
        UniTask ShowLoading(string text, float seconds = 3f);
        UniTask ShowDialogue(NPCSO npc);
        void ShowEnding(EndingGrade grade);
        void ShowExpel();
    }

    public interface ISaveService
    {
        bool Save(int slot = 1);
        bool TryLoad(int slot, out SaveDTO dto);
    }

    public struct QuizResult
    {
        public int correctCount;
        public int totalCount;
        public bool cancelled;
    }
}
