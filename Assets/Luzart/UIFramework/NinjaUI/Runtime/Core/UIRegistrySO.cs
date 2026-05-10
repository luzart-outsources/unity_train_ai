using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// ScriptableObject chua toan bo UI config. Moi UIConfig giu direct prefab reference
    /// (drag-drop GameObject vao UIConfig.AssetRef) - khong dung Addressables.
    ///
    /// Tao asset: Right-click Project -> Create -> NinjaUI -> UI Registry.
    /// Gan vao UIManager trong scene.
    /// </summary>
    [CreateAssetMenu(fileName = "UIRegistry", menuName = "NinjaUI/UI Registry", order = 0)]
    public class UIRegistrySO : ScriptableObject
    {
        [SerializeField]
        private List<UIConfig> entries = new List<UIConfig>();

        private Dictionary<UIId, UIConfig> byId;
        private Dictionary<string, UIConfig> byStringId;

        public IReadOnlyList<UIConfig> Entries => entries;

        public void BuildLookup()
        {
            byId = new Dictionary<UIId, UIConfig>(entries.Count);
            byStringId = new Dictionary<string, UIConfig>(entries.Count);

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || !e.IsValid)
                {
                    Debug.LogError($"[NinjaUI] Invalid UIConfig at index {i} in {name}");
                    continue;
                }

                if (byId.ContainsKey(e.Id))
                {
                    Debug.LogError($"[NinjaUI] Duplicate UIId {e.Id} in {name}");
                    continue;
                }
                byId[e.Id] = e;

                if (!string.IsNullOrEmpty(e.StringId))
                {
                    if (byStringId.ContainsKey(e.StringId))
                    {
                        Debug.LogError($"[NinjaUI] Duplicate StringId '{e.StringId}' in {name}");
                        continue;
                    }
                    byStringId[e.StringId] = e;
                }
            }
        }

        public bool TryGet(UIId id, out UIConfig config)
        {
            if (byId == null) BuildLookup();
            return byId.TryGetValue(id, out config);
        }

        public bool TryGet(string stringId, out UIConfig config)
        {
            if (byStringId == null) BuildLookup();
            return byStringId.TryGetValue(stringId, out config);
        }

        public UIConfig Get(UIId id)
        {
            if (TryGet(id, out var c)) return c;
            throw new KeyNotFoundException($"[NinjaUI] UIConfig not found for {id}. Check UIRegistrySO asset.");
        }

        public IEnumerable<UIConfig> GetPreloadEntries()
        {
            foreach (var e in entries)
                if (e != null && e.IsValid && e.PreloadOnBoot)
                    yield return e;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Rebuild lookup ở editor để catch duplicate sớm.
            byId = null;
            byStringId = null;
        }
#endif
    }
}
