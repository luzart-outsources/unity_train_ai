using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Luzart
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class ShowInInspectorEditor : UnityEditor.Editor
    {
        private static Dictionary<string, object[]> methodParameters = new Dictionary<string, object[]>();
        private static Dictionary<string, bool> parameterFoldouts = new Dictionary<string, bool>();

        public override void OnInspectorGUI()
        {
            // Draw the default inspector first
            DrawDefaultInspector();

            // Then draw ShowInInspector members
            DrawShowInInspectorMembers();

            // Then draw button methods
            DrawMethodButtons();
        }

        private void DrawShowInInspectorMembers()
        {
            Type targetType = target.GetType();
            bool hasShowInInspectorMembers = false;

            var fields = targetType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => f.GetCustomAttributes(typeof(ShowInInspectorAttribute), true).Length > 0);

            var properties = targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.GetCustomAttributes(typeof(ShowInInspectorAttribute), true).Length > 0);

            var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttributes(typeof(ShowInInspectorAttribute), true).Length > 0 &&
                           m.GetParameters().Length == 0 &&
                           m.ReturnType != typeof(void));

            if (fields.Any() || properties.Any() || methods.Any())
            {
                if (!hasShowInInspectorMembers)
                {
                    hasShowInInspectorMembers = true;
                }

                foreach (var field in fields)
                {
                    ShowInInspectorAttribute attr = (ShowInInspectorAttribute)field.GetCustomAttributes(typeof(ShowInInspectorAttribute), true)[0];
                    DrawShowInInspectorField(field, attr);
                }

                foreach (var property in properties)
                {
                    ShowInInspectorAttribute attr = (ShowInInspectorAttribute)property.GetCustomAttributes(typeof(ShowInInspectorAttribute), true)[0];
                    DrawShowInInspectorProperty(property, attr);
                }

                foreach (var method in methods)
                {
                    ShowInInspectorAttribute attr = (ShowInInspectorAttribute)method.GetCustomAttributes(typeof(ShowInInspectorAttribute), true)[0];
                    DrawShowInInspectorMethod(method, attr);
                }
            }
        }

        private void DrawShowInInspectorField(FieldInfo field, ShowInInspectorAttribute attribute)
        {
            string label = !string.IsNullOrEmpty(attribute.Label) ?
                attribute.Label : ObjectNames.NicifyVariableName(field.Name);

            try
            {
                object value = field.GetValue(target);
                object newValue = DrawValueField(label, value, field.FieldType, attribute.ReadOnly);

                if (!attribute.ReadOnly && !object.Equals(value, newValue))
                {
                    Undo.RecordObject(target, $"Change {field.Name}");
                    field.SetValue(target, newValue);
                    EditorUtility.SetDirty(target);
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.LabelField(label, $"Error: {ex.Message}");
            }
        }

        private void DrawShowInInspectorProperty(PropertyInfo property, ShowInInspectorAttribute attribute)
        {
            string label = !string.IsNullOrEmpty(attribute.Label) ?
                attribute.Label : ObjectNames.NicifyVariableName(property.Name);

            try
            {
                object value = property.GetValue(target);
                bool isReadOnly = attribute.ReadOnly || !property.CanWrite;

                object newValue = DrawValueField(label, value, property.PropertyType, isReadOnly);

                if (!isReadOnly && property.CanWrite && !object.Equals(value, newValue))
                {
                    Undo.RecordObject(target, $"Change {property.Name}");
                    property.SetValue(target, newValue);
                    EditorUtility.SetDirty(target);
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.LabelField(label, $"Error: {ex.Message}");
            }
        }

        private void DrawShowInInspectorMethod(MethodInfo method, ShowInInspectorAttribute attribute)
        {
            string label = !string.IsNullOrEmpty(attribute.Label) ?
                attribute.Label : ObjectNames.NicifyVariableName(method.Name);

            try
            {
                object value = method.Invoke(target, null);
                DrawValueField(label, value, method.ReturnType, true);
            }
            catch (Exception ex)
            {
                EditorGUILayout.LabelField(label, $"Error: {ex.Message}");
            }
        }

        private object DrawValueField(string label, object value, Type fieldType, bool readOnly)
        {
            bool wasEnabled = GUI.enabled;
            if (readOnly) GUI.enabled = false;

            object newValue = value;

            if (fieldType == typeof(int))
            {
                newValue = EditorGUILayout.IntField(label, value != null ? (int)value : 0);
            }
            else if (fieldType == typeof(float))
            {
                newValue = EditorGUILayout.FloatField(label, value != null ? (float)value : 0f);
            }
            else if (fieldType == typeof(bool))
            {
                newValue = EditorGUILayout.Toggle(label, value != null ? (bool)value : false);
            }
            else if (fieldType == typeof(string))
            {
                newValue = EditorGUILayout.TextField(label, value?.ToString() ?? "");
            }
            else if (fieldType == typeof(Vector2))
            {
                newValue = EditorGUILayout.Vector2Field(label, value != null ? (Vector2)value : Vector2.zero);
            }
            else if (fieldType == typeof(Vector3))
            {
                newValue = EditorGUILayout.Vector3Field(label, value != null ? (Vector3)value : Vector3.zero);
            }
            else if (fieldType == typeof(Color))
            {
                newValue = EditorGUILayout.ColorField(label, value != null ? (Color)value : Color.white);
            }
            else if (fieldType.IsEnum)
            {
                newValue = EditorGUILayout.EnumPopup(label, value != null ? (Enum)value : (Enum)Activator.CreateInstance(fieldType));
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                newValue = EditorGUILayout.ObjectField(label, value as UnityEngine.Object, fieldType, true);
            }
            else
            {
                string displayValue = value?.ToString() ?? "null";
                EditorGUILayout.LabelField(label, displayValue);
                newValue = value;
            }

            GUI.enabled = wasEnabled;
            return newValue;
        }

        private void DrawMethodButtons()
        {
            var allMethods = new List<MethodInfo>();
            Type currentType = target.GetType();

            while (currentType != null && currentType != typeof(object))
            {
                var typeMethods = currentType
                    .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(m => m.GetCustomAttributes(typeof(ButtonAttribute), false).Length > 0);

                foreach (var method in typeMethods)
                {
                    if (!allMethods.Any(existing =>
                        existing.Name == method.Name &&
                        existing.GetParameters().Length == method.GetParameters().Length &&
                        existing.GetParameters().Select(p => p.ParameterType).SequenceEqual(method.GetParameters().Select(p => p.ParameterType))))
                    {
                        allMethods.Add(method);
                    }
                }

                currentType = currentType.BaseType;
            }

            bool hasButtons = false;

            foreach (var method in allMethods)
            {
                if (!hasButtons)
                {
                    EditorGUILayout.Space(10);
                    hasButtons = true;
                }

                ButtonAttribute buttonAttribute = (ButtonAttribute)method.GetCustomAttributes(typeof(ButtonAttribute), true)[0];
                DrawStyledMethodButton(method, buttonAttribute);
                EditorGUILayout.Space(4);
            }
        }

        private void DrawStyledMethodButton(MethodInfo method, ButtonAttribute buttonAttribute)
        {
            string buttonText = !string.IsNullOrEmpty(buttonAttribute.ButtonText) ?
                buttonAttribute.ButtonText :
                ObjectNames.NicifyVariableName(method.Name);

            ParameterInfo[] parameters = method.GetParameters();
            string methodKey = $"{target.GetInstanceID()}.{method}";

            if (!methodParameters.ContainsKey(methodKey))
            {
                methodParameters[methodKey] = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    methodParameters[methodKey][i] = GetDefaultValue(parameters[i].ParameterType);
                }
            }

            if (parameters.Length > 0)
            {
                DrawFramedMethodButton(method, buttonAttribute, buttonText, parameters, methodKey);
            }
            else
            {
                DrawSimpleMethodButton(method, buttonAttribute, buttonText, methodKey);
            }
        }

        private void DrawFramedMethodButton(MethodInfo method, ButtonAttribute buttonAttribute, string buttonText,
            ParameterInfo[] parameters, string methodKey)
        {
            Rect lastRect = GUILayoutUtility.GetLastRect();
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.foldout);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;

            if (!parameterFoldouts.ContainsKey(methodKey))
                parameterFoldouts[methodKey] = true;

            parameterFoldouts[methodKey] = EditorGUILayout.Foldout(
                parameterFoldouts[methodKey],
                $"{buttonText} ({parameters.Length} parameters)",
                true,
                titleStyle
            );

            if (parameterFoldouts[methodKey])
            {
                EditorGUILayout.Space(2);

                EditorGUILayout.BeginVertical();
                EditorGUI.indentLevel++;

                for (int i = 0; i < parameters.Length; i++)
                {
                    string paramLabel = $"{ObjectNames.NicifyVariableName(parameters[i].Name)} ({parameters[i].ParameterType.Name})";

                    methodParameters[methodKey][i] = DrawStyledParameterField(paramLabel,
                        methodParameters[methodKey][i], parameters[i].ParameterType);
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(4);
            }
            GUIStyle buttonStyle = GetEnhancedButtonStyle(buttonAttribute);
            GUIContent buttonContent = new GUIContent($"{buttonText}");

            float buttonHeight = GetButtonHeight(buttonAttribute.Size);

            if (GUILayout.Button(buttonContent, buttonStyle, GUILayout.Height(buttonHeight)))
            {
                foreach (var t in targets)
                {
                    InvokeMethod(t, method, methodKey);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSimpleMethodButton(MethodInfo method, ButtonAttribute buttonAttribute, string buttonText, string methodKey)
        {
            GUIStyle buttonStyle = GetEnhancedButtonStyle(buttonAttribute);
            GUIContent buttonContent = GetEnhancedButtonContent(buttonText, buttonAttribute);

            float buttonHeight = GetButtonHeight(buttonAttribute.Size);

            if (GUILayout.Button(buttonContent, buttonStyle, GUILayout.Height(buttonHeight)))
            {
                foreach (var t in targets)
                {
                    InvokeMethod(t, method, methodKey);
                }
            }
        }

        private GUIStyle GetEnhancedButtonStyle(ButtonAttribute buttonAttribute)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);

            switch (buttonAttribute.Style)
            {
                case ButtonStyle.Framed:
                case ButtonStyle.Default:
                    style.fontStyle = FontStyle.Bold;
                    style.alignment = TextAnchor.MiddleCenter;
                    break;

                case ButtonStyle.Colored:
                    style.fontStyle = FontStyle.Bold;
                    style.normal.textColor = Color.white;
                    style.normal.background = MakeColorTexture(new Color(0.3f, 0.6f, 0.9f));
                    break;

                case ButtonStyle.Flat:
                    style.border = new RectOffset(2, 2, 2, 2);
                    style.normal.background = MakeColorTexture(new Color(0.9f, 0.9f, 0.9f));
                    break;
            }

            switch (buttonAttribute.Size)
            {
                case ButtonSize.Small:
                    style.fontSize = 10;
                    break;
                case ButtonSize.Medium:
                    style.fontSize = 12;
                    break;
                case ButtonSize.Large:
                    style.fontSize = 14;
                    style.fontStyle = FontStyle.Bold;
                    break;
            }

            return style;
        }

        private GUIContent GetEnhancedButtonContent(string text, ButtonAttribute buttonAttribute)
        {
            if (!string.IsNullOrEmpty(buttonAttribute.IconName))
            {
                var icon = EditorGUIUtility.IconContent(buttonAttribute.IconName);
                return new GUIContent($"  {text}", icon.image);
            }

            return new GUIContent(text);
        }

        private object DrawStyledParameterField(string label, object currentValue, Type parameterType)
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.normal.textColor = new Color(0.3f, 0.3f, 0.3f);

            if (parameterType == typeof(int))
            {
                int intValue = 0;
                if (currentValue != null)
                {
                    if (currentValue is int)
                        intValue = (int)currentValue;
                    else if (int.TryParse(currentValue.ToString(), out int parsed))
                        intValue = parsed;
                }
                return EditorGUILayout.IntField(label, intValue);
            }
            else if (parameterType == typeof(float))
            {
                float floatValue = 0f;
                if (currentValue != null)
                {
                    if (currentValue is float)
                        floatValue = (float)currentValue;
                    else if (currentValue is double)
                        floatValue = (float)(double)currentValue;
                    else if (float.TryParse(currentValue.ToString(), out float parsed))
                        floatValue = parsed;
                }
                return EditorGUILayout.FloatField(label, floatValue);
            }
            else if (parameterType == typeof(bool))
            {
                bool boolValue = false;
                if (currentValue != null)
                {
                    if (currentValue is bool)
                        boolValue = (bool)currentValue;
                    else if (bool.TryParse(currentValue.ToString(), out bool parsed))
                        boolValue = parsed;
                }
                return EditorGUILayout.Toggle(label, boolValue);
            }
            else if (parameterType == typeof(string))
            {
                string stringValue = "";
                if (currentValue != null)
                {
                    stringValue = currentValue.ToString();
                }
                return EditorGUILayout.TextField(label, stringValue);
            }
            else if (parameterType == typeof(Vector2))
            {
                Vector2 vector2Value = Vector2.zero;
                if (currentValue != null && currentValue is Vector2)
                {
                    vector2Value = (Vector2)currentValue;
                }
                return EditorGUILayout.Vector2Field(label, vector2Value);
            }
            else if (parameterType == typeof(Vector3))
            {
                Vector3 vector3Value = Vector3.zero;
                if (currentValue != null && currentValue is Vector3)
                {
                    vector3Value = (Vector3)currentValue;
                }
                return EditorGUILayout.Vector3Field(label, vector3Value);
            }
            else if (parameterType == typeof(Color))
            {
                Color colorValue = Color.white;
                if (currentValue != null && currentValue is Color)
                {
                    colorValue = (Color)currentValue;
                }
                return EditorGUILayout.ColorField(label, colorValue);
            }
            else if (parameterType.IsEnum)
            {
                Enum enumValue = null;
                if (currentValue != null && parameterType.IsAssignableFrom(currentValue.GetType()))
                {
                    enumValue = (Enum)currentValue;
                }
                else
                {
                    Array enumValues = Enum.GetValues(parameterType);
                    if (enumValues.Length > 0)
                    {
                        enumValue = (Enum)enumValues.GetValue(0);
                    }
                }
                return EditorGUILayout.EnumPopup(label, enumValue);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(parameterType))
            {
                UnityEngine.Object objectValue = null;
                if (currentValue != null && parameterType.IsAssignableFrom(currentValue.GetType()))
                {
                    objectValue = (UnityEngine.Object)currentValue;
                }
                return EditorGUILayout.ObjectField(label, objectValue, parameterType, true);
            }
            else
            {
                EditorGUILayout.LabelField(label, $"Unsupported type: {parameterType.Name}");
                return currentValue ?? GetDefaultValue(parameterType);
            }
        }

        private Texture2D MakeColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void InvokeMethod(object target, MethodInfo method, string methodKey)
        {
            try
            {
                if (target is UnityEngine.Object unityObject)
                {
                    Undo.RecordObject(unityObject, $"Button {method.Name}");
                }

                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length == 0)
                {
                    method.Invoke(target, null);
                }
                else
                {
                    object[] parameterValues = methodParameters[methodKey];
                    method.Invoke(target, parameterValues);
                }

                if (target is UnityEngine.Object targetObject)
                {
                    EditorUtility.SetDirty(targetObject);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error invoking method {method.Name}: {ex.Message}");
            }
        }

        private object GetDefaultValue(Type type)
        {
            if (type == typeof(int))
                return 0;
            else if (type == typeof(float))
                return 0f;
            else if (type == typeof(double))
                return 0.0;
            else if (type == typeof(bool))
                return false;
            else if (type == typeof(string))
                return "";
            else if (type == typeof(Vector2))
                return Vector2.zero;
            else if (type == typeof(Vector3))
                return Vector3.zero;
            else if (type == typeof(Vector4))
                return Vector4.zero;
            else if (type == typeof(Color))
                return Color.white;
            else if (type.IsEnum)
            {
                Array enumValues = Enum.GetValues(type);
                return enumValues.Length > 0 ? enumValues.GetValue(0) : null;
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return null;
            }
            else if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        private float GetButtonHeight(ButtonSize size)
        {
            return size switch
            {
                ButtonSize.Small => 18f,
                ButtonSize.Medium => 24f,
                ButtonSize.Large => 32f,
                _ => 24f
            };
        }
    }
}
