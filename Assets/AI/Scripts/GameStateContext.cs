// Phase C — Live game-state provider for HybridChatClient.
//
// Reads PlayerStateRSO + GameClockRSO + ActiveQuestRSO and exposes a
// flat IGameStateProvider view that the chat server can consume.
//
// Note on asmdef: this script lives in Assembly-CSharp (no asmdef in
// Assets/AI/Scripts/) while the RSO types live in TrainAI.SO.Base
// (with autoReferenced:false). We therefore accept the references as
// generic ScriptableObject + reflect into the fields at runtime — no
// compile-time dependency on TrainAI.* types. This keeps the chat
// runtime portable across asmdef boundaries.

using System.Reflection;
using UnityEngine;

namespace TrainAI.AI
{
    /// <summary>
    /// Wire-once monobehaviour: drag PlayerStateRSO, GameClockRSO and
    /// ActiveQuestRSO assets into the Inspector. HybridChatClient picks
    /// this up via FindObjectOfType.
    /// </summary>
    public class GameStateContext : MonoBehaviour, IGameStateProvider
    {
        [Header("Runtime SO references (any ScriptableObject)")]
        [Tooltip("PlayerStateRSO asset — expected fields: playerName, currentScene, hocTap, renLuyen")]
        public ScriptableObject playerState;

        [Tooltip("GameClockRSO asset — expected fields: day, hour, minute")]
        public ScriptableObject gameClock;

        [Tooltip("ActiveQuestRSO asset — expected field: current (a QuestSO with id/title)")]
        public ScriptableObject activeQuest;

        [Header("Static fallbacks (used if RSOs missing)")]
        public int    fallbackDay  = 1;
        public string fallbackTime = "07:30";
        public string fallbackArea = "doanh trại";

        // =====================================================================
        // IGameStateProvider — values derived from RSOs via reflection
        // =====================================================================

        public int CurrentDay
        {
            get
            {
                var d = ReadInt(gameClock, "day");
                return d > 0 ? d : fallbackDay;
            }
        }

        public string CurrentTime
        {
            get
            {
                if (gameClock == null) return fallbackTime;
                int h = ReadInt(gameClock, "hour");
                int m = ReadInt(gameClock, "minute");
                return $"{h:D2}:{m:D2}";
            }
        }

        public string CurrentArea
        {
            get
            {
                string scene = ReadString(playerState, "currentScene");
                return SceneToAreaVi(scene);
            }
        }

        public string PlayerName
        {
            get
            {
                string n = ReadString(playerState, "playerName");
                return string.IsNullOrEmpty(n) ? "đồng chí" : n;
            }
        }

        public string ActiveQuest
        {
            get
            {
                if (activeQuest == null) return "";
                // Read activeQuest.current -> ScriptableObject -> title or id field.
                var currentField = activeQuest.GetType().GetField(
                    "current",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                if (currentField == null) return "";
                var currentSO = currentField.GetValue(activeQuest) as ScriptableObject;
                if (currentSO == null) return "";
                string title = ReadString(currentSO, "title");
                if (!string.IsNullOrEmpty(title)) return title;
                return ReadString(currentSO, "id");
            }
        }

        // =====================================================================
        // Reflection helpers
        // =====================================================================

        private static int ReadInt(ScriptableObject so, string fieldName)
        {
            if (so == null) return 0;
            var f = so.GetType().GetField(fieldName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null) return 0;
            object v = f.GetValue(so);
            return v is int i ? i : 0;
        }

        private static string ReadString(ScriptableObject so, string fieldName)
        {
            if (so == null) return "";
            var f = so.GetType().GetField(fieldName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null) return "";
            object v = f.GetValue(so);
            return v as string ?? "";
        }

        // =====================================================================
        // Scene -> Vietnamese area mapping (matches qa_metadata.json)
        // =====================================================================

        private string SceneToAreaVi(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return fallbackArea;
            string s = sceneName.ToLowerInvariant();
            if (s.Contains("lophoc"))    return "lớp học";
            if (s.Contains("nhaan"))     return "nhà ăn";
            if (s.Contains("kytucxa"))   return "ký túc xá";
            if (s.Contains("ktx"))       return "ký túc xá";
            if (s.Contains("vandong") || s.Contains("svd")) return "sân vận động";
            if (s.Contains("donvesinh")) return "khu dọn vệ sinh";
            if (s.Contains("freearea"))  return "khu tự do";
            if (s.Contains("ending"))    return "lễ tốt nghiệp";
            if (s.Contains("mainmenu") || s.Contains("title") || s.Contains("createchar"))
                                         return "menu";
            return "doanh trại";
        }

#if UNITY_EDITOR
        [ContextMenu("Print Current State")]
        private void PrintCurrent()
        {
            Debug.Log($"[GameStateContext] day={CurrentDay} time={CurrentTime} " +
                      $"area={CurrentArea} player={PlayerName} quest={ActiveQuest}");
        }
#endif
    }
}
