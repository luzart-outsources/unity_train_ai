using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TrainAI.SO.Base
{
    public class InteractionContext
    {
        public Transform player;
        public AreaSO area;

        // Injected by InteractionRouter at runtime - SO.Base stays Services-free.
        public Func<string, UniTask<bool>> showConfirm;
        public Func<SceneRefSO, string, UniTask> loadAdditive;
        public Func<SceneRefSO, UniTask> unloadAdditive;
        // Load new scene then unload current ones — for "enter building"
        // transitions that should free the world scene's memory.
        public Func<SceneRefSO, string, UniTask> loadReplacing;
        public Action<int, int> skipTimeTo;
        public Action<bool> completeCurrentQuest;
        public Func<NPCSO, UniTask> showDialogue;
        public Func<QuizSetSO, UniTask<(int correct, int total, bool cancelled)>> showQuiz;
        public Action<int, int, string> applyScoreDelta;
        public Func<string, float, UniTask> showLoading;
        public Action advanceDay;
    }
}
