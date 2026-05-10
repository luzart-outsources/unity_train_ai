using Luzart;
using UnityEngine;

namespace TrainAI.Configs
{
    // Route den 1 scene - vd Classroom, World, Cafeteria.
    [CreateAssetMenu(menuName = "TrainAI/Scene/Route", fileName = "R_Route")]
    public class SceneRouteSO : ScriptableObject
    {
        public string id = "R_Classroom";

        [InfoBox("Phai khop ten scene trong File > Build Settings.")]
        public string sceneName = "Classroom";

        [InfoBox("GDD: 'Dang vao lop hoc...', 'Sang ngay hom sau...', etc.")]
        public string loadingText = "Dang chuyen canh...";

        [Header("Fade")]
        [Slider(0f, 2f)] public float fadeOutSeconds = 0.4f;
        [Slider(0f, 2f)] public float fadeInSeconds = 0.4f;

        [InfoBox("GDD: Loading screen ton tai 3s + chuyen scene.")]
        [Slider(0.5f, 10f)] public float minLoadingScreenSeconds = 3f;

        [Header("Time freeze")]
        [InfoBox("GDD: scene quest (lop hoc, ky tuc xa, nha an) -> dong bang dong ho.")]
        public bool freezeTimeWhileLoaded = true;

        [Header("Audio")]
        public AudioCueId backgroundMusic = AudioCueId.None;
        public AudioCueId ambient = AudioCueId.None;
    }
}
