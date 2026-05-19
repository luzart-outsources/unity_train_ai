using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainAI.UI
{
    public class SceneAwareHUDRoot : MonoBehaviour
    {
        [SerializeField]
        List<string> gameplayScenes = new()
        {
            "10_World", "11_LopHoc", "12_NhaAn", "13_KyTucXa"
        };

        [SerializeField] Canvas hudCanvas;
        [SerializeField] CanvasGroup hudGroup;

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnLoaded;
            Apply(SceneManager.GetActiveScene().name);
        }

        void OnDisable() => SceneManager.sceneLoaded -= OnLoaded;
        void OnLoaded(Scene s, LoadSceneMode _) => Apply(s.name);

        void Apply(string sceneName)
        {
            bool show = gameplayScenes.Contains(sceneName);
            if (hudGroup != null)
            {
                hudGroup.alpha = show ? 1f : 0f;
                hudGroup.blocksRaycasts = show;
                hudGroup.interactable = show;
            }
            if (hudCanvas != null) hudCanvas.enabled = show;
        }
    }
}
