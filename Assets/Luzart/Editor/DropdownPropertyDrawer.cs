using UnityEngine;
using UnityEditor;
using System;

namespace Luzart
{
    [CustomPropertyDrawer(typeof(DropdownAttribute))]
    [CustomPropertyDrawer(typeof(DropdownNamedAttribute))]
    public class DropdownPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DropdownAttribute dropdownAttribute = (DropdownAttribute)attribute;

            // Validate attribute data
            if (dropdownAttribute.Values == null || dropdownAttribute.Values.Length == 0)
            {
                EditorGUI.LabelField(position, label.text, "Dropdown: No values provided");
                return;
            }

            if (dropdownAttribute.DisplayNames == null ||
                dropdownAttribute.DisplayNames.Length != dropdownAttribute.Values.Length)
            {
                EditorGUI.LabelField(position, label.text, "Dropdown: Values/DisplayNames length mismatch");
                return;
            }

            // Draw label
            position = EditorGUI.PrefixLabel(position, label);

            if (property.propertyType == SerializedPropertyType.String)
            {
                DrawStringDropdown(position, property, dropdownAttribute);
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                DrawIntegerDropdown(position, property, dropdownAttribute);
            }
            else if (property.propertyType == SerializedPropertyType.Float)
            {
                DrawFloatDropdown(position, property, dropdownAttribute);
            }
            else
            {
                EditorGUI.LabelField(position, "Dropdown only works with string, int, or float values");
            }
        }

        private void DrawStringDropdown(Rect position, SerializedProperty property, DropdownAttribute dropdownAttribute)
        {
            string currentValue = property.stringValue;
            int currentIndex = -1;

            // Find current index - compare with Values array
            for (int i = 0; i < dropdownAttribute.Values.Length; i++)
            {
                if (dropdownAttribute.Values[i] != null &&
                    dropdownAttribute.Values[i].ToString() == currentValue)
                {
                    currentIndex = i;
                    break;
                }
            }

            // If not found, default to index 0
            if (currentIndex == -1 && dropdownAttribute.Values.Length > 0)
            {
                currentIndex = 0;
                if (dropdownAttribute.Values[0] != null)
                {
                    property.stringValue = dropdownAttribute.Values[0].ToString();
                }
            }

            // Show dropdown with DisplayNames
            int newIndex = EditorGUI.Popup(position, currentIndex, dropdownAttribute.DisplayNames);

            // Update property if changed
            if (newIndex != currentIndex && newIndex >= 0 && newIndex < dropdownAttribute.Values.Length)
            {
                if (dropdownAttribute.Values[newIndex] != null)
                {
                    property.stringValue = dropdownAttribute.Values[newIndex].ToString();
                }
            }
        }

        private void DrawIntegerDropdown(Rect position, SerializedProperty property, DropdownAttribute dropdownAttribute)
        {
            int currentValue = property.intValue;
            int currentIndex = -1;

            for (int i = 0; i < dropdownAttribute.Values.Length; i++)
            {
                if (dropdownAttribute.Values[i] != null)
                {
                    if (TryConvertToInt(dropdownAttribute.Values[i], out int intValue) && intValue == currentValue)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            if (currentIndex == -1 && dropdownAttribute.Values.Length > 0)
            {
                currentIndex = 0;
                if (dropdownAttribute.Values[0] != null &&
                    TryConvertToInt(dropdownAttribute.Values[0], out int defaultValue))
                {
                    property.intValue = defaultValue;
                }
            }

            int newIndex = EditorGUI.Popup(position, currentIndex, dropdownAttribute.DisplayNames);

            if (newIndex != currentIndex && newIndex >= 0 && newIndex < dropdownAttribute.Values.Length)
            {
                if (dropdownAttribute.Values[newIndex] != null &&
                    TryConvertToInt(dropdownAttribute.Values[newIndex], out int newValue))
                {
                    property.intValue = newValue;
                }
            }
        }

        private void DrawFloatDropdown(Rect position, SerializedProperty property, DropdownAttribute dropdownAttribute)
        {
            float currentValue = property.floatValue;
            int currentIndex = -1;

            for (int i = 0; i < dropdownAttribute.Values.Length; i++)
            {
                if (dropdownAttribute.Values[i] != null)
                {
                    if (TryConvertToFloat(dropdownAttribute.Values[i], out float floatValue) &&
                        Mathf.Approximately(floatValue, currentValue))
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            if (currentIndex == -1 && dropdownAttribute.Values.Length > 0)
            {
                currentIndex = 0;
                if (dropdownAttribute.Values[0] != null &&
                    TryConvertToFloat(dropdownAttribute.Values[0], out float defaultValue))
                {
                    property.floatValue = defaultValue;
                }
            }

            int newIndex = EditorGUI.Popup(position, currentIndex, dropdownAttribute.DisplayNames);

            if (newIndex != currentIndex && newIndex >= 0 && newIndex < dropdownAttribute.Values.Length)
            {
                if (dropdownAttribute.Values[newIndex] != null &&
                    TryConvertToFloat(dropdownAttribute.Values[newIndex], out float newValue))
                {
                    property.floatValue = newValue;
                }
            }
        }

        private bool TryConvertToInt(object value, out int result)
        {
            result = 0;

            if (value is int intValue)
            {
                result = intValue;
                return true;
            }

            if (value is string stringValue && int.TryParse(stringValue, out result))
            {
                return true;
            }

            try
            {
                result = Convert.ToInt32(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryConvertToFloat(object value, out float result)
        {
            result = 0f;

            if (value is float floatValue)
            {
                result = floatValue;
                return true;
            }

            if (value is int intValue)
            {
                result = intValue;
                return true;
            }

            if (value is string stringValue && float.TryParse(stringValue, out result))
            {
                return true;
            }

            try
            {
                result = Convert.ToSingle(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
