#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Luzart
{
    /// <summary>
    /// Editor utility: reset the selected RectTransform's localScale to (1,1,1)
    /// while preserving the visual size of all its children (sizeDelta, anchoredPosition, font sizes).
    /// Useful when cleaning up prefab hierarchies where a non-1 scale has crept in.
    /// </summary>
    public static class ResetParentKeepScale
    {
        [MenuItem("Luzart/Reset Parent Scale & Keep Child Size")]
        public static void ResetScaleForSelected()
        {
            Transform[] selectedTransforms = Selection.transforms;

            if (selectedTransforms.Length == 0)
            {
                Debug.LogWarning("Please select at least one RectTransform in the hierarchy.");
                return;
            }

            foreach (Transform selected in selectedTransforms)
            {
                if (selected is RectTransform parentTransform)
                {
                    ResetScaleForParent(parentTransform);
                }
            }
        }

        private static void ResetScaleForParent(RectTransform parentTransform)
        {
            Vector2 originalParentSize = parentTransform.rect.size;
            Vector3 originalParentScale = parentTransform.localScale;
            Vector3 originalParentAnchors = parentTransform.anchoredPosition3D;

            RectTransform[] childTransforms = parentTransform.GetComponentsInChildren<RectTransform>(true);
            Vector3[] originalChildSizeDeltas = new Vector3[childTransforms.Length];
            Vector2[] originalChildPositions = new Vector2[childTransforms.Length];
            TMP_Text[] txts = parentTransform.GetComponentsInChildren<TMP_Text>(true);
            Text[] txtNormals = parentTransform.GetComponentsInChildren<Text>(true);

            for (int i = 0; i < childTransforms.Length; i++)
            {
                var child = childTransforms[i];
                if (child != null)
                {
                    originalChildSizeDeltas[i] = child.sizeDelta;
                    originalChildPositions[i] = child.anchoredPosition;
                }
            }

            parentTransform.localScale = Vector3.one;

            for (int i = 0; i < childTransforms.Length; i++)
            {
                if (childTransforms[i] != null)
                {
                    var child = childTransforms[i];

                    child.sizeDelta = new Vector2(
                        originalChildSizeDeltas[i].x * originalParentScale.x,
                        originalChildSizeDeltas[i].y * originalParentScale.y
                    );

                    child.anchoredPosition = new Vector2(
                        originalChildPositions[i].x * originalParentScale.x,
                        originalChildPositions[i].y * originalParentScale.y
                    );

                    EditorUtility.SetDirty(childTransforms[i]);
                }
            }

            for (int i = 0; i < txts.Length; i++)
            {
                float size = txts[i].fontSize;
                size = size * originalParentScale.x;
                txts[i].fontSize = size;
            }
            for (int i = 0; i < txtNormals.Length; i++)
            {
                float size = txtNormals[i].fontSize;
                size = size * originalParentScale.x;
                int sizeInt = Mathf.RoundToInt(size);
                txtNormals[i].fontSize = sizeInt;
            }

            parentTransform.anchoredPosition3D = originalParentAnchors;

            EditorUtility.SetDirty(parentTransform);
        }
    }
}
#endif
