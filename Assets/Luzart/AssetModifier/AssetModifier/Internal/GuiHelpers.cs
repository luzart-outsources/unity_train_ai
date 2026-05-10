#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace AssetModifier.Internal
{
    /// <summary>
    /// Self-contained GUI helpers. UnityEngine.GUI / UnityEditor APIs only.
    /// </summary>
    internal static class GuiHelpers
    {
        public const char ArrowSymbol = '→';

        static GUIStyle _miniBtnStyle;
        public static GUIStyle MiniBtnStyle =>
            _miniBtnStyle ??= new GUIStyle(EditorStyles.miniButton) { fontSize = 10, padding = new RectOffset(3, 3, 2, 2) };

        static GUIStyle _guideStyle;
        public static GUIStyle GuideStyle =>
            _guideStyle ??= new GUIStyle(EditorStyles.wordWrappedMiniLabel) { richText = true };

        static GUIStyle _subHeaderStyle;
        public static GUIStyle SubHeaderStyle =>
            _subHeaderStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };

        public static void GuideText(string msg) => GUILayout.Label("• " + msg, GuideStyle);
        public static void SubSectionLabel(string msg) => GUILayout.Label(msg, SubHeaderStyle);

        public static void Separator()
        {
            GUILayout.Space(3);
            var r = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(0f, 0f, 0f, 0.3f));
            GUILayout.Space(3);
        }

        public static bool MiniIconBtn(string label)
            => GUILayout.Button(label, MiniBtnStyle, GUILayout.Width(22));

        public static string Substring(string s) => $"<color=#808080ff>{s}</color>";

        /// <summary>Mini box section with a header label.</summary>
        public static void BoxedGroup(string title, Action drawBody)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(title, EditorStyles.miniBoldLabel);
                drawBody();
            }
        }

        // -------- scoped IDisposable helpers --------

        public readonly struct Disabled : IDisposable
        {
            public Disabled(bool disabled) { EditorGUI.BeginDisabledGroup(disabled); }
            public void Dispose() => EditorGUI.EndDisabledGroup();
        }

        public readonly struct Colored : IDisposable
        {
            readonly Color _old;
            public Colored(Color c) { _old = GUI.color; GUI.color = c; }
            public void Dispose() { GUI.color = _old; }
        }

        public readonly struct Indent : IDisposable
        {
            readonly int _delta;
            public Indent(int delta) { _delta = delta; EditorGUI.indentLevel += delta; }
            public void Dispose() { EditorGUI.indentLevel -= _delta; }
        }

        // -------- reserved rects for evenly-spaced buttons --------

        public static Rect[] ReserveEqualRects(int count, float height)
        {
            var total = GUILayoutUtility.GetRect(10, height, GUILayout.ExpandWidth(true));
            var rs = new Rect[count];
            float w = total.width / count;
            for (int i = 0; i < count; i++)
                rs[i] = new Rect(total.x + i * w, total.y, w - 2, total.height);
            return rs;
        }

        /// <summary>
        /// Dead-simple pagination loop. Calls <paramref name="drawItem"/> for the current page slice.
        /// Returns the (possibly updated) page index.
        /// </summary>
        public static int Paginated<T>(System.Collections.Generic.IList<T> list, int page, int pageSize, Action<T, int> drawItem)
        {
            int count = list.Count;
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(count / (float)pageSize));
            page = Mathf.Clamp(page, 0, pageCount - 1);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new Disabled(page <= 0))
                    if (GUILayout.Button("<", MiniBtnStyle, GUILayout.Width(24))) page = Mathf.Max(0, page - 1);
                GUILayout.Label($"Page {page + 1}/{pageCount}  ({count} items)", EditorStyles.centeredGreyMiniLabel);
                using (new Disabled(page >= pageCount - 1))
                    if (GUILayout.Button(">", MiniBtnStyle, GUILayout.Width(24))) page = Mathf.Min(pageCount - 1, page + 1);
            }

            int start = page * pageSize;
            int end = Mathf.Min(count, start + pageSize);
            for (int i = start; i < end; i++) drawItem(list[i], i);

            return page;
        }
    }
}
#endif
