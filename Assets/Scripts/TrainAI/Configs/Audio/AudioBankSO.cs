using System.Collections.Generic;
using Luzart;
using UnityEngine;

namespace TrainAI.Configs
{
    [CreateAssetMenu(menuName = "TrainAI/Audio/Audio Bank", fileName = "AudioBank")]
    public class AudioBankSO : ScriptableObject
    {
        [InfoBox("Drag tat ca AudioCueSO vao day. AudioManager.Play(id) lookup theo cue.id.")]
        public List<AudioCueSO> cues = new List<AudioCueSO>();

        private Dictionary<AudioCueId, AudioCueSO> lookup;

        public void RebuildLookup()
        {
            lookup = new Dictionary<AudioCueId, AudioCueSO>();
            for (int i = 0; i < cues.Count; i++)
            {
                var c = cues[i];
                if (c == null || c.id == AudioCueId.None) continue;
                if (lookup.ContainsKey(c.id))
                {
                    Debug.LogWarning($"[AudioBank] Duplicate cue id {c.id} in {name}. Using first.");
                    continue;
                }
                lookup[c.id] = c;
            }
        }

        public bool TryGet(AudioCueId id, out AudioCueSO cue)
        {
            if (lookup == null) RebuildLookup();
            return lookup.TryGetValue(id, out cue);
        }
    }
}
