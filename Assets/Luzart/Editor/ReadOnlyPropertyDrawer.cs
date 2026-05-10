using UnityEngine;
using UnityEditor;

namespace Luzart
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Store the original GUI enabled state
            bool wasEnabled = GUI.enabled;

            // Disable GUI for read-only effect
            GUI.enabled = false;

            // Draw the property field as normal but disabled
            EditorGUI.PropertyField(position, property, label, true);

            // Restore the original GUI enabled state
            GUI.enabled = wasEnabled;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
