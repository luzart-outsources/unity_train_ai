using System.Collections.Generic;
using TrainAI.Configs;
using UnityEngine;

namespace TrainAI.Systems.Audio
{
    // SOLID Single Responsibility: chi quan ly audio playback. Khong biet ve quest, score, etc.
    // Subscribe events ngoai (vd TrainAI.UI.Audio.AudioEventBindings).
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance => _instance;

        [Header("Bank")]
        [SerializeField] private AudioBankSO bank;

        [Header("Mixing")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.7f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float uiVolume = 1f;
        [Range(0f, 1f)] public float ambientVolume = 0.5f;

        [Header("Pool sizes")]
        [SerializeField] private int sfxPoolSize = 8;
        [SerializeField] private int uiPoolSize = 4;

        private AudioSource _musicSource;
        private AudioSource _ambientSource;
        private readonly List<AudioSource> _sfxPool = new List<AudioSource>();
        private readonly List<AudioSource> _uiPool = new List<AudioSource>();

        private const string PrefMaster = "TrainAI_Vol_Master";
        private const string PrefMusic = "TrainAI_Vol_Music";
        private const string PrefSfx = "TrainAI_Vol_Sfx";
        private const string PrefUi = "TrainAI_Vol_Ui";
        private const string PrefAmbient = "TrainAI_Vol_Ambient";

        public AudioBankSO Bank
        {
            get => bank;
            set { bank = value; if (bank != null) bank.RebuildLookup(); }
        }

        public static AudioManager EnsureInstance()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("[AudioManager]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<AudioManager>();
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVolumes();
            BuildSources();
            if (bank != null) bank.RebuildLookup();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void BuildSources()
        {
            _musicSource = MakeSource("Music", true);
            _ambientSource = MakeSource("Ambient", true);
            for (int i = 0; i < sfxPoolSize; i++) _sfxPool.Add(MakeSource($"SFX_{i}", false));
            for (int i = 0; i < uiPoolSize; i++) _uiPool.Add(MakeSource($"UI_{i}", false));
        }

        private AudioSource MakeSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            return src;
        }

        public void Play(AudioCueId id)
        {
            if (id == AudioCueId.None || bank == null) return;
            if (!bank.TryGet(id, out var cue) || cue == null || cue.clip == null) return;

            switch (cue.channel)
            {
                case AudioChannel.Music:    PlayOnDedicated(_musicSource, cue, musicVolume * masterVolume); break;
                case AudioChannel.Ambient:  PlayOnDedicated(_ambientSource, cue, ambientVolume * masterVolume); break;
                case AudioChannel.UI:       PlayOnPool(_uiPool, cue, uiVolume * masterVolume); break;
                case AudioChannel.SFX:
                default:                    PlayOnPool(_sfxPool, cue, sfxVolume * masterVolume); break;
            }
        }

        public void StopMusic(float fadeSeconds = 0.5f)
        {
            if (_musicSource == null) return;
            if (fadeSeconds <= 0f) { _musicSource.Stop(); return; }
            StartCoroutine(FadeOut(_musicSource, fadeSeconds));
        }

        public void StopAmbient(float fadeSeconds = 0.5f)
        {
            if (_ambientSource == null) return;
            if (fadeSeconds <= 0f) { _ambientSource.Stop(); return; }
            StartCoroutine(FadeOut(_ambientSource, fadeSeconds));
        }

        public void StopAll()
        {
            if (_musicSource != null) _musicSource.Stop();
            if (_ambientSource != null) _ambientSource.Stop();
            foreach (var s in _sfxPool) if (s != null) s.Stop();
            foreach (var s in _uiPool) if (s != null) s.Stop();
        }

        private void PlayOnDedicated(AudioSource src, AudioCueSO cue, float channelVol)
        {
            if (src == null) return;
            src.clip = cue.clip;
            src.loop = cue.loop;
            src.priority = 256 - cue.priority; // Unity: thap = quan trong hon
            src.pitch = RandomPitch(cue);
            src.volume = cue.volume * channelVol;
            src.Play();
        }

        private void PlayOnPool(List<AudioSource> pool, AudioCueSO cue, float channelVol)
        {
            if (pool == null || pool.Count == 0) return;
            var src = FindFreeSource(pool);
            if (src == null) src = pool[0]; // pre-empt slot 0 neu het
            src.clip = cue.clip;
            src.loop = cue.loop;
            src.priority = 256 - cue.priority;
            src.pitch = RandomPitch(cue);
            src.volume = cue.volume * channelVol;
            src.PlayOneShot(cue.clip, src.volume);
        }

        private static AudioSource FindFreeSource(List<AudioSource> pool)
        {
            for (int i = 0; i < pool.Count; i++)
                if (pool[i] != null && !pool[i].isPlaying) return pool[i];
            return null;
        }

        private static float RandomPitch(AudioCueSO cue)
        {
            float min = cue.pitchRange.x, max = cue.pitchRange.y;
            if (Mathf.Approximately(min, max)) return min;
            return UnityEngine.Random.Range(min, max);
        }

        private System.Collections.IEnumerator FadeOut(AudioSource src, float dur)
        {
            float v0 = src.volume;
            float t = 0f;
            while (t < dur && src != null)
            {
                t += UnityEngine.Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(v0, 0f, t / dur);
                yield return null;
            }
            if (src != null) { src.Stop(); src.volume = v0; }
        }

        public void SaveVolumes()
        {
            PlayerPrefs.SetFloat(PrefMaster, masterVolume);
            PlayerPrefs.SetFloat(PrefMusic, musicVolume);
            PlayerPrefs.SetFloat(PrefSfx, sfxVolume);
            PlayerPrefs.SetFloat(PrefUi, uiVolume);
            PlayerPrefs.SetFloat(PrefAmbient, ambientVolume);
            PlayerPrefs.Save();
        }

        public void LoadVolumes()
        {
            masterVolume = PlayerPrefs.GetFloat(PrefMaster, 1f);
            musicVolume = PlayerPrefs.GetFloat(PrefMusic, 0.7f);
            sfxVolume = PlayerPrefs.GetFloat(PrefSfx, 1f);
            uiVolume = PlayerPrefs.GetFloat(PrefUi, 1f);
            ambientVolume = PlayerPrefs.GetFloat(PrefAmbient, 0.5f);
        }
    }
}
