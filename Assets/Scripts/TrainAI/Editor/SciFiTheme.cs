using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.Editor
{
    public static class SciFiTheme
    {
        public const string Root = "Assets/GUI_Sci_FI/Sliced Elements";

        public static readonly Color TextWhite = new(0.96f, 0.98f, 1f, 1f);
        public static readonly Color TextMuted = new(0.74f, 0.85f, 0.96f, 1f);
        public static readonly Color AccentCyan = new(0.20f, 0.92f, 0.98f, 1f);
        public static readonly Color SciFiDeep = new(0.02f, 0.05f, 0.12f, 1f);
        public static readonly Color SciFiBlue = new(0.05f, 0.13f, 0.26f, 1f);
        public static readonly Color SciFiNavy = new(0.03f, 0.08f, 0.18f, 1f);
        public static readonly Color HUDChip = new(0.04f, 0.10f, 0.22f, 0.92f);
        public static readonly Color WarnRed = new(0.85f, 0.18f, 0.20f, 1f);

        struct SpriteSpec { public string path; public Vector4 border; }

        static readonly Dictionary<string, SpriteSpec> Sprites = new()
        {
            // panels / backgrounds
            { "popup_bg_02",       new SpriteSpec { path = $"{Root}/99_Popup/popup_bg_02.png",       border = new Vector4(64, 64, 64, 64) } },
            { "popup_bg_01",       new SpriteSpec { path = $"{Root}/99_Popup/popup_bg_01.png",       border = new Vector4(64, 64, 64, 64) } },
            { "popup_bg_03",       new SpriteSpec { path = $"{Root}/99_Popup/popup_bg_03.png",       border = new Vector4(64, 64, 64, 64) } },
            { "popup_title_01",    new SpriteSpec { path = $"{Root}/99_Popup/popup_title_01.png",    border = new Vector4(48, 24, 48, 24) } },
            { "common_bg",         new SpriteSpec { path = $"{Root}/00_Common/common_bg.png",        border = new Vector4(8, 8, 8, 8) } },
            { "screen_dimmed",     new SpriteSpec { path = $"{Root}/00_Common/screen_dimmed.png",    border = Vector4.zero } },
            { "loading_bg",        new SpriteSpec { path = $"{Root}/00_Common/loading_bg.png",       border = new Vector4(64, 64, 64, 64) } },
            { "list_bg_n",         new SpriteSpec { path = $"{Root}/00_Common/list_bg_n.png",        border = new Vector4(24, 24, 24, 24) } },
            { "count_bg",          new SpriteSpec { path = $"{Root}/00_Common/count_bg.png",         border = new Vector4(24, 24, 24, 24) } },

            // buttons (n=normal, f=focus/highlighted, d=disabled)
            { "btn_common_n",      new SpriteSpec { path = $"{Root}/00_Common/btn_common_n.png",     border = new Vector4(20, 20, 20, 20) } },
            { "btn_common_f",      new SpriteSpec { path = $"{Root}/00_Common/btn_common_f.png",     border = new Vector4(20, 20, 20, 20) } },
            { "btn_common_d",      new SpriteSpec { path = $"{Root}/00_Common/btn_common_d.png",     border = new Vector4(20, 20, 20, 20) } },
            { "popup_btn_n",       new SpriteSpec { path = $"{Root}/99_Popup/popup_btn_n.png",       border = new Vector4(24, 24, 24, 24) } },
            { "popup_btn_f",       new SpriteSpec { path = $"{Root}/99_Popup/popup_btn_f.png",       border = new Vector4(24, 24, 24, 24) } },
            { "popup_btn_close_n", new SpriteSpec { path = $"{Root}/99_Popup/popup_btn_close_n.png", border = Vector4.zero } },
            { "popup_btn_close_f", new SpriteSpec { path = $"{Root}/99_Popup/popup_btn_close_f.png", border = Vector4.zero } },
            { "btn_common_back_n", new SpriteSpec { path = $"{Root}/00_Common/btn_common_back_n.png", border = Vector4.zero } },
            { "btn_common_back_f", new SpriteSpec { path = $"{Root}/00_Common/btn_common_back_f.png", border = Vector4.zero } },

            // input field
            { "txt_input_n",       new SpriteSpec { path = $"{Root}/00_Common/txt_input_n.png",      border = new Vector4(24, 20, 24, 20) } },
            { "txt_input_f",       new SpriteSpec { path = $"{Root}/00_Common/txt_input_f.png",      border = new Vector4(24, 20, 24, 20) } },

            // loading bar (vertical 9-slice horizontal only)
            { "loading_bar",       new SpriteSpec { path = $"{Root}/00_Common/loading_bar.png",      border = new Vector4(10, 0, 10, 0) } },
            { "loading_bar_bg",    new SpriteSpec { path = $"{Root}/00_Common/loading_bar_bg.png",   border = new Vector4(10, 0, 10, 0) } },

            // frames / borders
            { "item_frame_n",      new SpriteSpec { path = $"{Root}/00_Common/item_frame_n.png",     border = new Vector4(24, 24, 24, 24) } },
            { "item_frame_f",      new SpriteSpec { path = $"{Root}/00_Common/item_frame_f.png",     border = new Vector4(24, 24, 24, 24) } },
        };

        public static Sprite Load(string key)
        {
            if (!Sprites.TryGetValue(key, out var spec))
            {
                Debug.LogWarning($"[SciFiTheme] Unknown sprite key '{key}'");
                return null;
            }
            EnsureImport(spec.path, spec.border);
            return AssetDatabase.LoadAssetAtPath<Sprite>(spec.path);
        }

        static void EnsureImport(string path, Vector4 border)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            bool changed = false;
            if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; changed = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; changed = true; }
            if (imp.mipmapEnabled) { imp.mipmapEnabled = false; changed = true; }
            if (imp.filterMode != FilterMode.Bilinear) { imp.filterMode = FilterMode.Bilinear; changed = true; }
            if (imp.wrapMode != TextureWrapMode.Clamp) { imp.wrapMode = TextureWrapMode.Clamp; changed = true; }

            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            if (settings.spriteBorder != border)
            {
                settings.spriteBorder = border;
                imp.SetTextureSettings(settings);
                changed = true;
            }
            if (changed) imp.SaveAndReimport();
        }

        public static GameObject FindOrAddBG(GameObject panel)
        {
            var existing = panel.transform.Find("BG");
            if (existing != null && existing.GetComponent<Image>() != null) return existing.gameObject;
            var bg = new GameObject("BG");
            bg.transform.SetParent(panel.transform, false);
            bg.transform.SetAsFirstSibling();
            var rect = bg.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            bg.AddComponent<Image>();
            return bg;
        }

        public static void StylePanel(GameObject panel, string bgKey = "popup_bg_02", Color? tint = null)
        {
            var bg = FindOrAddBG(panel);
            var img = bg.GetComponent<Image>();
            img.sprite = Load(bgKey);
            img.type = Image.Type.Sliced;
            img.color = tint ?? Color.white;
            img.raycastTarget = true;
        }

        public static void StyleScreenDim(GameObject panel, float alpha = 0.78f)
        {
            var bg = FindOrAddBG(panel);
            var img = bg.GetComponent<Image>();
            img.sprite = Load("screen_dimmed");
            img.type = Image.Type.Sliced;
            img.color = new Color(0f, 0f, 0f, alpha);
            img.raycastTarget = true;
        }

        public static void StyleButton(Button btn, bool primary = false)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img == null) return;

            string nKey = primary ? "popup_btn_n" : "btn_common_n";
            string fKey = primary ? "popup_btn_f" : "btn_common_f";

            img.sprite = Load(nKey);
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            btn.transition = Selectable.Transition.SpriteSwap;
            var ss = btn.spriteState;
            ss.highlightedSprite = Load(fKey);
            ss.pressedSprite = Load(fKey);
            ss.selectedSprite = Load(fKey);
            ss.disabledSprite = Load("btn_common_d");
            btn.spriteState = ss;

            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.color = TextWhite;
                label.fontStyle = FontStyles.Bold;
                label.outlineWidth = 0.18f;
                label.outlineColor = new Color(0f, 0f, 0f, 0.7f);
            }
        }

        public static void StyleCloseButton(Button btn)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img == null) return;
            img.sprite = Load("popup_btn_close_n");
            img.type = Image.Type.Simple;
            img.color = Color.white;

            btn.transition = Selectable.Transition.SpriteSwap;
            var ss = btn.spriteState;
            ss.highlightedSprite = Load("popup_btn_close_f");
            ss.pressedSprite = Load("popup_btn_close_f");
            ss.selectedSprite = Load("popup_btn_close_f");
            btn.spriteState = ss;

            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = "";
        }

        public static void StyleInputField(TMP_InputField input)
        {
            if (input == null) return;
            var img = input.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = Load("txt_input_n");
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            if (input.textComponent != null)
            {
                input.textComponent.color = TextWhite;
            }
        }

        public static void StyleHeader(TMP_Text text, int fontSize)
        {
            if (text == null) return;
            text.fontSize = fontSize;
            text.color = AccentCyan;
            text.fontStyle = FontStyles.Bold;
            text.outlineWidth = 0.22f;
            text.outlineColor = new Color(0f, 0.40f, 0.50f, 0.95f);
        }

        public static void StyleBody(TMP_Text text, int fontSize)
        {
            if (text == null) return;
            text.fontSize = fontSize;
            text.color = TextWhite;
            text.outlineWidth = 0.12f;
            text.outlineColor = new Color(0f, 0f, 0f, 0.7f);
        }

        public static void StyleHUDChip(GameObject hudPanel)
        {
            var bg = FindOrAddBG(hudPanel);
            var img = bg.GetComponent<Image>();
            img.sprite = Load("count_bg");
            img.type = Image.Type.Sliced;
            img.color = HUDChip;
            img.raycastTarget = false;
        }

        // Clean uniform dark navy backdrop. No two-tone split (that read as "screen broken
        // in half"); instead a single deep tone with a subtle top vignette + a hairline
        // accent at the horizon for visual depth.
        public static GameObject AddSceneBackdrop(Transform canvasTransform)
        {
            var bg = new GameObject("SciFiBackdrop");
            bg.transform.SetParent(canvasTransform, false);
            bg.transform.SetAsFirstSibling();
            var rect = bg.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            var img = bg.AddComponent<Image>();
            img.color = SciFiDeep;
            img.raycastTarget = false;

            // Soft top-glow band — adds depth without splitting the screen visually.
            var glow = new GameObject("TopGlow");
            glow.transform.SetParent(bg.transform, false);
            var grect = glow.AddComponent<RectTransform>();
            grect.anchorMin = new Vector2(0f, 0.7f); grect.anchorMax = new Vector2(1f, 1f);
            grect.offsetMin = Vector2.zero; grect.offsetMax = Vector2.zero;
            var gimg = glow.AddComponent<Image>();
            gimg.color = new Color(SciFiBlue.r, SciFiBlue.g, SciFiBlue.b, 0.55f);
            gimg.raycastTarget = false;

            // Thin horizon line near the centre for sci-fi "scanner" feel.
            var accent = new GameObject("AccentLine");
            accent.transform.SetParent(bg.transform, false);
            var arect = accent.AddComponent<RectTransform>();
            arect.anchorMin = new Vector2(0f, 0.42f); arect.anchorMax = new Vector2(1f, 0.42f);
            arect.pivot = new Vector2(0.5f, 0.5f);
            arect.sizeDelta = new Vector2(0f, 1.5f);
            arect.anchoredPosition = Vector2.zero;
            var aimg = accent.AddComponent<Image>();
            aimg.color = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.45f);
            aimg.raycastTarget = false;
            return bg;
        }
    }
}
