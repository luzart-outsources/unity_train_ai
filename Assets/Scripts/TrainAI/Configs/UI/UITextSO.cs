using System;
using System.Collections.Generic;
using Luzart;
using UnityEngine;

namespace TrainAI.Configs
{
    [CreateAssetMenu(menuName = "TrainAI/UI/Text Catalog", fileName = "UI_Text")]
    public class UITextSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string key;
            [TextArea(1, 4)] public string text;
        }

        [InfoBox("Tat ca string UI co the i18n sau. Vd:\n" +
                 "EndDay -> 'Het ngay, dang chuyen sang ngay tiep theo...'\n" +
                 "MainMenu_NewGame -> 'Bat dau moi'.")]
        public List<Entry> entries = new List<Entry>();

        private Dictionary<string, string> lookup;

        public void RebuildLookup()
        {
            lookup = new Dictionary<string, string>();
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (string.IsNullOrEmpty(e.key)) continue;
                lookup[e.key] = e.text;
            }
        }

        public string Get(string key, string fallback = null)
        {
            if (lookup == null) RebuildLookup();
            if (lookup.TryGetValue(key, out var t)) return t;
            return fallback ?? key;
        }
    }
}
