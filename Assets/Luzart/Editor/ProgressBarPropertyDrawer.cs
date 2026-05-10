using UnityEngine;
using UnityEditor;

namespace Luzart
{
    [CustomPropertyDrawer(typeof(ProgressBarAttribute))]
    public class ProgressBarPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ProgressBarAttribute progressBarAttribute = (ProgressBarAttribute)attribute;

            if (property.propertyType != SerializedPropertyType.Float && property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.LabelField(position, label.text, "ProgressBar only works with float or int values");
                return;
            }

            // Get current value
            float currentValue = property.propertyType == SerializedPropertyType.Float ?
                property.floatValue : property.intValue;

            // Calculate progress (0-1)
            float progress = Mathf.InverseLerp(progressBarAttribute.MinValue, progressBarAttribute.MaxValue, currentValue);
            progress = Mathf.Clamp01(progress);

            // Prepare label text
            string labelText = !string.IsNullOrEmpty(progressBarAttribute.Label) ?
                progressBarAttribute.Label : property.displayName;

            string valueText = progressBarAttribute.ShowValue ?
                $" ({currentValue:F1}/{progressBarAttribute.MaxValue:F1})" : "";

            // Draw the progress bar
            EditorGUI.ProgressBar(position, progress, labelText + valueText);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
