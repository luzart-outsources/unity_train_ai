using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Property drawer for conditional attributes (ShowIf, HideIf, EnableIf, DisableIf, ShowIfAny, ShowIfAll)
    /// Supports path navigation: "../" to go up levels, "." to go deeper into properties
    /// Examples:
    /// - "propertyName" - sibling property
    /// - "../propertyName" - property in parent object
    /// - "../../typeAnimation" - property two levels up
    /// - "subObject.propertyName" - nested property
    /// </summary>
    [CustomPropertyDrawer(typeof(ConditionalPropertyAttribute), true)]
    [CustomPropertyDrawer(typeof(ShowIfAnyAttribute), true)]
    [CustomPropertyDrawer(typeof(ShowIfAllAttribute), true)]
    public class ConditionalPropertyDrawer : PropertyDrawer
    {
        private const BindingFlags MemberBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly Dictionary<(Type, string), MemberInfo> MemberCache = new Dictionary<(Type, string), MemberInfo>();
        private static readonly HashSet<string> LoggedWarnings = new HashSet<string>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool conditionMet = EvaluateAllConditions(property);
            bool shouldShow = ShouldShowProperty(conditionMet);
            bool shouldEnable = ShouldEnableProperty(conditionMet);

            if (!shouldShow)
                return;

            var wasEnabled = GUI.enabled;
            GUI.enabled = shouldEnable;

            EditorGUI.PropertyField(position, property, label, true);

            GUI.enabled = wasEnabled;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            bool conditionMet = EvaluateAllConditions(property);
            bool shouldShow = ShouldShowProperty(conditionMet);

            if (!shouldShow)
                return -EditorGUIUtility.standardVerticalSpacing;

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private bool EvaluateAllConditions(SerializedProperty property)
        {
            if (attribute is ConditionalPropertyAttribute conditionalAttribute)
            {
                return EvaluateCondition(property, conditionalAttribute.SiblingPropertyPath, conditionalAttribute.ConditionValue);
            }
            else if (attribute is ShowIfAnyAttribute showIfAnyAttribute)
            {
                bool anyMet = false;
                for (int i = 0; i < showIfAnyAttribute.PropertyPaths.Length; i++)
                {
                    if (EvaluateCondition(property, showIfAnyAttribute.PropertyPaths[i], showIfAnyAttribute.ConditionValues[i]))
                    {
                        anyMet = true;
                        break;
                    }
                }
                return anyMet;
            }
            else if (attribute is ShowIfAllAttribute showIfAllAttribute)
            {
                bool allMet = true;
                for (int i = 0; i < showIfAllAttribute.PropertyPaths.Length; i++)
                {
                    if (!EvaluateCondition(property, showIfAllAttribute.PropertyPaths[i], showIfAllAttribute.ConditionValues[i]))
                    {
                        allMet = false;
                        break;
                    }
                }
                return allMet;
            }

            return true;
        }

        private bool EvaluateCondition(SerializedProperty property, string path, object conditionValue)
        {
            try
            {
                var targetProperty = GetTargetProperty(property, path);
                object currentValue = null;

                if (targetProperty != null)
                {
                    // Found as SerializedProperty
                    currentValue = GetPropertyValue(targetProperty);
                }
                else
                {
                    // Try to get as computed property using reflection with proper path navigation
                    currentValue = GetComputedPropertyValueWithNavigation(property, path);
                }

                if (currentValue == null)
                {
                    LogWarningOnce($"ConditionalPropertyDrawer: Property not found at path '{path}' from '{property.propertyPath}'");
                    return false;
                }

                bool isEqual = AreValuesEqual(currentValue, conditionValue);

                return isEqual;
            }
            catch (Exception ex)
            {
                LogWarningOnce($"ConditionalPropertyDrawer: Failed to evaluate condition for path '{path}': {ex.Message}");
                return false;
            }
        }

        private object GetComputedPropertyValueWithNavigation(SerializedProperty sourceProperty, string path)
        {
            try
            {
                // Parse the path to handle navigation
                var (targetObjectPath, finalPropertyName) = ParsePathWithNavigation(sourceProperty.propertyPath, path);

                // Get the target object that contains the property
                var targetObject = sourceProperty.serializedObject.targetObject;

                // Navigate to the correct object level
                object currentObject = GetObjectAtPath(targetObject, targetObjectPath);

                if (currentObject == null)
                    return null;

                // Get the final property from the current object
                return GetFieldOrPropertyValue(currentObject, finalPropertyName);
            }
            catch (Exception ex)
            {
                LogWarningOnce($"ConditionalPropertyDrawer: Failed to get computed property value for '{path}': {ex.Message}");
                return null;
            }
        }

        private object GetObjectAtPath(object rootObject, string path)
        {
            if (rootObject == null)
                return null;

            if (string.IsNullOrEmpty(path))
                return rootObject;

            var parts = path.Split('.');
            object current = rootObject;

            foreach (var part in parts)
            {
                if (current == null)
                    return null;

                // Handle array elements
                if (part.Contains("[") && part.Contains("]"))
                {
                    current = GetArrayElement(current, part);
                }
                else
                {
                    current = GetFieldOrPropertyValue(current, part);
                }
            }

            return current;
        }

        private (string targetPath, string propertyName) ParsePathWithNavigation(string currentPropertyPath, string navigationPath)
        {
            if (string.IsNullOrEmpty(navigationPath))
                return (string.Empty, string.Empty);

            // Check if path contains navigation operators
            bool hasNavigation = navigationPath.Contains("../") || navigationPath.Contains(".");

            if (!hasNavigation)
            {
                // Simple sibling property - same level as current property
                string parentPath = NavigateUpPath(currentPropertyPath);
                return (parentPath, navigationPath);
            }

            // Start from the current property's parent path
            string currentPath = NavigateUpPath(currentPropertyPath);

            // Split by "/" to handle "../" navigation
            var pathParts = navigationPath.Split('/');
            string remainingPath = string.Empty;

            // Process navigation parts
            foreach (var part in pathParts)
            {
                if (part == "..")
                {
                    // Navigate up one level
                    currentPath = NavigateUpPath(currentPath);
                }
                else if (!string.IsNullOrEmpty(part))
                {
                    // This is the property path (may contain "." for nested properties)
                    remainingPath = part;
                    break;
                }
            }

            // Handle nested property access with "."
            if (remainingPath.Contains("."))
            {
                var nestedParts = remainingPath.Split('.');
                string baseProperty = nestedParts[0];
                string nestedProperty = string.Join(".", nestedParts, 1, nestedParts.Length - 1);

                // The base property is at the current level
                string finalTargetPath = string.IsNullOrEmpty(currentPath) ? baseProperty : $"{currentPath}.{baseProperty}";

                return (finalTargetPath, nestedProperty);
            }

            // Simple property at the current level
            return (currentPath, remainingPath);
        }

        private SerializedProperty GetTargetProperty(SerializedProperty sourceProperty, string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // Parse the path with proper navigation
            var (targetObjectPath, finalPropertyName) = ParsePathWithNavigation(sourceProperty.propertyPath, path);

            // Build the final property path
            string finalPath;
            if (string.IsNullOrEmpty(targetObjectPath))
            {
                finalPath = finalPropertyName;
            }
            else if (string.IsNullOrEmpty(finalPropertyName))
            {
                finalPath = targetObjectPath;
            }
            else
            {
                finalPath = $"{targetObjectPath}.{finalPropertyName}";
            }

            if (string.IsNullOrEmpty(finalPath))
                return null;

            var targetProperty = sourceProperty.serializedObject.FindProperty(finalPath);

            // If not found and finalPath doesn't contain dots, it might be a direct property of the serialized object
            if (targetProperty == null && !finalPath.Contains("."))
            {
                // Try to find it directly in the serialized object
                var so = sourceProperty.serializedObject;
                var iterator = so.GetIterator();
                if (iterator.NextVisible(true))
                {
                    do
                    {
                        if (iterator.name == finalPath)
                        {
                            targetProperty = iterator.Copy();
                            break;
                        }
                    }
                    while (iterator.NextVisible(false));
                }
            }

            return targetProperty;
        }

        private string NavigateUpPath(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
                return string.Empty;

            int lastDotIndex = propertyPath.LastIndexOf('.');
            if (lastDotIndex == -1)
            {
                // Already at root level
                return string.Empty;
            }

            return propertyPath.Substring(0, lastDotIndex);
        }

        private object GetPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return property.boolValue;
                case SerializedPropertyType.Integer:
                    return property.intValue;
                case SerializedPropertyType.Float:
                    return property.floatValue;
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Enum:
                    return property.enumValueFlag;
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue;
                case SerializedPropertyType.Vector2:
                    return property.vector2Value;
                case SerializedPropertyType.Vector3:
                    return property.vector3Value;
                case SerializedPropertyType.Vector4:
                    return property.vector4Value;
                case SerializedPropertyType.Color:
                    return property.colorValue;
                case SerializedPropertyType.LayerMask:
                    return property.intValue;
                case SerializedPropertyType.Rect:
                    return property.rectValue;
                case SerializedPropertyType.AnimationCurve:
                    return property.animationCurveValue;
                case SerializedPropertyType.Bounds:
                    return property.boundsValue;
                default:
                    // For complex types, try reflection approach
                    return GetComplexPropertyValue(property);
            }
        }

        private object GetComplexPropertyValue(SerializedProperty property)
        {
            try
            {
                var targetObject = property.serializedObject.targetObject;
                var propertyPath = property.propertyPath;

                return GetValueFromPath(targetObject, propertyPath);
            }
            catch (Exception ex)
            {
                LogWarningOnce($"ConditionalPropertyDrawer: Failed to get complex property value for '{property.propertyPath}': {ex.Message}");
                return null;
            }
        }

        private object GetValueFromPath(object obj, string path)
        {
            if (obj == null || string.IsNullOrEmpty(path))
                return null;

            var parts = path.Split('.');
            object current = obj;

            foreach (var part in parts)
            {
                if (current == null)
                    return null;

                // Handle array elements
                if (part.Contains("[") && part.Contains("]"))
                {
                    current = GetArrayElement(current, part);
                }
                else
                {
                    current = GetFieldOrPropertyValue(current, part);
                }
            }

            return current;
        }

        private object GetArrayElement(object obj, string propertyWithIndex)
        {
            try
            {
                var openBracket = propertyWithIndex.IndexOf('[');
                var closeBracket = propertyWithIndex.IndexOf(']');

                if (openBracket == -1 || closeBracket == -1)
                    return null;

                var propertyName = propertyWithIndex.Substring(0, openBracket);
                var indexStr = propertyWithIndex.Substring(openBracket + 1, closeBracket - openBracket - 1);

                if (!int.TryParse(indexStr, out int index))
                    return null;

                var arrayObj = GetFieldOrPropertyValue(obj, propertyName);
                if (arrayObj is IList list && index >= 0 && index < list.Count)
                    return list[index];

                return null;
            }
            catch
            {
                return null;
            }
        }

        private object GetFieldOrPropertyValue(object obj, string name)
        {
            if (obj == null || string.IsNullOrEmpty(name))
                return null;

            var type = obj.GetType();
            var key = (type, name);

            if (!MemberCache.TryGetValue(key, out MemberInfo cached))
            {
                cached = (MemberInfo)type.GetField(name, MemberBindingFlags)
                       ?? type.GetProperty(name, MemberBindingFlags);
                MemberCache[key] = cached;
            }

            return cached switch
            {
                FieldInfo field => field.GetValue(obj),
                PropertyInfo property when property.CanRead => property.GetValue(obj),
                _ => null
            };
        }

        private static void LogWarningOnce(string message)
        {
            if (LoggedWarnings.Add(message))
            {
                Debug.LogWarning(message);
            }
        }

        private bool AreValuesEqual(object value1, object value2)
        {
            if (value1 == null && value2 == null)
                return true;
            if (value1 == null || value2 == null)
                return false;

            // Handle enum comparisons more robustly
            if (value1 is int enumValue1 && value2 is Enum enumValue2)
                return enumValue1 == Convert.ToInt32(enumValue2);
            if (value1 is Enum enumValue3 && value2 is int enumValue4)
                return Convert.ToInt32(enumValue3) == enumValue4;
            if (value1 is Enum enum1 && value2 is Enum enum2)
                return Convert.ToInt32(enum1) == Convert.ToInt32(enum2);

            // Handle Unity Object references (including null checks)
            if (value1 is UnityEngine.Object unityObj1)
            {
                if (value2 == null)
                    return unityObj1 == null;
                if (value2 is UnityEngine.Object unityObj2)
                    return unityObj1 == unityObj2;
                return false;
            }

            // Special handling for checking null against UnityEngine.Object
            if (value2 is UnityEngine.Object unityObj3)
            {
                if (value1 == null)
                    return unityObj3 == null;
                return false;
            }

            // Handle string comparisons (case-sensitive by default)
            if (value1 is string str1 && value2 is string str2)
                return string.Equals(str1, str2);

            return value1.Equals(value2);
        }

        private bool ShouldShowProperty(bool conditionMet)
        {
            return attribute switch
            {
                ShowIfAttribute => conditionMet,
                ShowIfAnyAttribute => conditionMet,
                ShowIfAllAttribute => conditionMet,
                HideIfAttribute => !conditionMet,
                _ => true
            };
        }

        private bool ShouldEnableProperty(bool conditionMet)
        {
            return attribute switch
            {
                EnableIfAttribute => conditionMet,
                DisableIfAttribute => !conditionMet,
                _ => GUI.enabled
            };
        }
    }
}
