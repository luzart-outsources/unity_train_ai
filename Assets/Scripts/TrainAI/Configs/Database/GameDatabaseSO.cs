using System.Collections.Generic;
using Luzart;
using UnityEngine;

namespace TrainAI.Configs
{
    // Root catalog - GameBootstrap chi can ref dum file nay.
    [CreateAssetMenu(menuName = "TrainAI/Database/Root", fileName = "_GameDatabase")]
    public class GameDatabaseSO : ScriptableObject
    {
        [Header("Core")]
        public TimeConfigSO timeConfig;
        public ScoreConfigSO scoreConfig;
        public DayCycleConfigSO dayCycle;

        [Header("UI")]
        public UITextSO uiText;

        [Header("Content catalog")]
        public List<SubjectSO> subjects = new List<SubjectSO>();
        public List<NPCProfileSO> npcs = new List<NPCProfileSO>();
        public List<SceneRouteSO> scenes = new List<SceneRouteSO>();
        public List<InteractableSO> interactables = new List<InteractableSO>();

        [Header("Audio")]
        public AudioBankSO audioBank;

        [Header("Settings")]
        [InfoBox("Default name khi vao CharacterCreate. User co the doi.")]
        public string playerNameDefault = "Hoc vien";

        [Slider(0f, 60f)] public float autosaveIntervalMinutes = 5f;

        public bool IsValid(out string error)
        {
            if (timeConfig == null) { error = "timeConfig missing"; return false; }
            if (scoreConfig == null) { error = "scoreConfig missing"; return false; }
            if (dayCycle == null) { error = "dayCycle missing"; return false; }
            if (audioBank == null) { error = "audioBank missing"; return false; }
            if (uiText == null) { error = "uiText missing"; return false; }
            error = null;
            return true;
        }
    }
}
