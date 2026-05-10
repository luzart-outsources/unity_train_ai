using Luzart;
using UnityEngine;

namespace TrainAI.Configs
{
    [CreateAssetMenu(menuName = "TrainAI/Audio/Audio Cue", fileName = "AC_Cue")]
    public class AudioCueSO : ScriptableObject
    {
        public AudioCueId id = AudioCueId.None;

        [InfoBox("Co the de null - AudioManager se log warning va skip.")]
        public AudioClip clip;

        public AudioChannel channel = AudioChannel.SFX;

        [Slider(0f, 1f)] public float volume = 1f;

        [Header("Pitch random")]
        [Tooltip("Random pitch trong khoang [x, y]. = (1,1) thi cho dinh.")]
        public Vector2 pitchRange = new Vector2(1f, 1f);

        public bool loop = false;

        [Slider(0, 256)]
        [Tooltip("Cao hon = quan trong hon. Khi pool day, kick cue priority thap.")]
        public int priority = 128;
    }
}
