using System;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Base class for conditional property attributes
    /// </summary>
    public abstract class ConditionalPropertyAttribute : PropertyAttribute
    {
        public string SiblingPropertyPath { get; }
        public object ConditionValue { get; }

        protected ConditionalPropertyAttribute(string siblingPropertyPath, object conditionValue)
        {
            SiblingPropertyPath = siblingPropertyPath;
            ConditionValue = conditionValue;
        }
    }

    /// <summary>
    /// Creates a button in the Inspector that calls a method when clicked.
    /// Usage: [Button] or [Button("Custom Button Text")]
    /// Can be applied to both fields and methods
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false)]
    public class ButtonAttribute : PropertyAttribute
    {
        public string ButtonText { get; }
        public ButtonSize Size { get; }
        public ButtonStyle Style { get; }
        public string IconName { get; }

        /// <summary>
        /// Create a button with default text (method name or field name)
        /// </summary>
        public ButtonAttribute()
        {
            ButtonText = null;
            Size = ButtonSize.Small;
            Style = ButtonStyle.Default;
            IconName = null;
        }

        /// <summary>
        /// Create a button with custom text
        /// </summary>
        /// <param name="buttonText">Text to display on the button</param>
        public ButtonAttribute(string buttonText)
        {
            ButtonText = buttonText;
            Size = ButtonSize.Small;
            Style = ButtonStyle.Default;
            IconName = null;
        }

        /// <summary>
        /// Create a button with custom text and size
        /// </summary>
        /// <param name="buttonText">Text to display on the button</param>
        /// <param name="size">Size of the button</param>
        public ButtonAttribute(string buttonText, ButtonSize size)
        {
            ButtonText = buttonText;
            Size = size;
            Style = ButtonStyle.Default;
            IconName = null;
        }

        /// <summary>
        /// Create a button with custom text, size and style
        /// </summary>
        /// <param name="buttonText">Text to display on the button</param>
        /// <param name="size">Size of the button</param>
        /// <param name="style">Style of the button</param>
        public ButtonAttribute(string buttonText, ButtonSize size, ButtonStyle style)
        {
            ButtonText = buttonText;
            Size = size;
            Style = style;
            IconName = null;
        }

        /// <summary>
        /// Create a button with custom text, size, style and icon
        /// </summary>
        /// <param name="buttonText">Text to display on the button</param>
        /// <param name="size">Size of the button</param>
        /// <param name="style">Style of the button</param>
        /// <param name="iconName">Unity built-in icon name (e.g., "d_PlayButton", "d_Settings")</param>
        public ButtonAttribute(string buttonText, ButtonSize size, ButtonStyle style, string iconName)
        {
            ButtonText = buttonText;
            Size = size;
            Style = style;
            IconName = iconName;
        }
    }

    /// <summary>
    /// Size options for Button attribute
    /// </summary>
    public enum ButtonSize
    {
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// Style options for Button attribute
    /// </summary>
    public enum ButtonStyle
    {
        Default,        // Standard Unity button
        Framed,         // Button with parameters in a nice frame
        Colored,        // Colored button
        Rounded,        // Rounded button
        Flat           // Flat style button
    }

    /// <summary>
    /// Shows any field, property, or method return value in the Inspector
    /// Works with private fields, properties, and methods
    /// Usage: [ShowInInspector] above any field, property, or method
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
    public class ShowInInspectorAttribute : PropertyAttribute
    {
        public string Label { get; }
        public bool ReadOnly { get; }

        /// <summary>
        /// Show in inspector with default label
        /// </summary>
        public ShowInInspectorAttribute()
        {
            Label = null;
            ReadOnly = false;
        }

        /// <summary>
        /// Show in inspector with custom label
        /// </summary>
        /// <param name="label">Custom label to display</param>
        public ShowInInspectorAttribute(string label)
        {
            Label = label;
            ReadOnly = false;
        }

        /// <summary>
        /// Show in inspector with custom label and read-only option
        /// </summary>
        /// <param name="label">Custom label to display</param>
        /// <param name="readOnly">Whether the field should be read-only</param>
        public ShowInInspectorAttribute(string label, bool readOnly)
        {
            Label = label;
            ReadOnly = readOnly;
        }

        /// <summary>
        /// Show in inspector as read-only
        /// </summary>
        /// <param name="readOnly">Whether the field should be read-only</param>
        public ShowInInspectorAttribute(bool readOnly)
        {
            Label = null;
            ReadOnly = readOnly;
        }
    }

    /// <summary>
    /// Makes a field read-only in the inspector
    /// Usage: [ReadOnly]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }

    /// <summary>
    /// Shows the property if the condition is met. For multiple conditions, use ShowIfAny or ShowIfAll
    /// Usage: [ShowIf("propertyName", expectedValue)]
    /// Path navigation: "../propertyName" goes up one level, "propertyName.subProperty" goes deeper
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShowIfAttribute : ConditionalPropertyAttribute
    {
        public ShowIfAttribute(string siblingPropertyPath, object conditionValue)
            : base(siblingPropertyPath, conditionValue) { }
    }

    /// <summary>
    /// Shows the property if ANY of the conditions are met
    /// Usage: [ShowIfAny("prop1", value1, "prop2", value2, ...)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShowIfAnyAttribute : PropertyAttribute
    {
        public string[] PropertyPaths { get; }
        public object[] ConditionValues { get; }

        public ShowIfAnyAttribute(params object[] pathsAndValues)
        {
            if (pathsAndValues.Length % 2 != 0)
                throw new ArgumentException("Must provide pairs of property paths and condition values");

            PropertyPaths = new string[pathsAndValues.Length / 2];
            ConditionValues = new object[pathsAndValues.Length / 2];

            for (int i = 0; i < pathsAndValues.Length; i += 2)
            {
                PropertyPaths[i / 2] = pathsAndValues[i] as string;
                ConditionValues[i / 2] = pathsAndValues[i + 1];
            }
        }
    }

    /// <summary>
    /// Shows the property if ALL of the conditions are met
    /// Usage: [ShowIfAll("prop1", value1, "prop2", value2, ...)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShowIfAllAttribute : PropertyAttribute
    {
        public string[] PropertyPaths { get; }
        public object[] ConditionValues { get; }

        public ShowIfAllAttribute(params object[] pathsAndValues)
        {
            if (pathsAndValues.Length % 2 != 0)
                throw new ArgumentException("Must provide pairs of property paths and condition values");

            PropertyPaths = new string[pathsAndValues.Length / 2];
            ConditionValues = new object[pathsAndValues.Length / 2];

            for (int i = 0; i < pathsAndValues.Length; i += 2)
            {
                PropertyPaths[i / 2] = pathsAndValues[i] as string;
                ConditionValues[i / 2] = pathsAndValues[i + 1];
            }
        }
    }

    /// <summary>
    /// Hides the property if the condition is met
    /// Usage: [HideIf("propertyName", expectedValue)]
    /// Path navigation: "../propertyName" goes up one level, "propertyName.subProperty" goes deeper
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class HideIfAttribute : ConditionalPropertyAttribute
    {
        public HideIfAttribute(string siblingPropertyPath, object conditionValue)
            : base(siblingPropertyPath, conditionValue) { }
    }

    /// <summary>
    /// Enables the property if the condition is met
    /// Usage: [EnableIf("propertyName", expectedValue)]
    /// Path navigation: "../propertyName" goes up one level, "propertyName.subProperty" goes deeper
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EnableIfAttribute : ConditionalPropertyAttribute
    {
        public EnableIfAttribute(string siblingPropertyPath, object conditionValue)
            : base(siblingPropertyPath, conditionValue) { }
    }

    /// <summary>
    /// Disables the property if the condition is met
    /// Usage: [DisableIf("propertyName", expectedValue)]
    /// Path navigation: "../propertyName" goes up one level, "propertyName.subProperty" goes deeper
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DisableIfAttribute : ConditionalPropertyAttribute
    {
        public DisableIfAttribute(string siblingPropertyPath, object conditionValue)
            : base(siblingPropertyPath, conditionValue) { }
    }
}
