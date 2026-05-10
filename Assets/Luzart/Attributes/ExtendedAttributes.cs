using System;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Displays an info box above the property
    /// Usage: [InfoBox("This is an information message")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class InfoBoxAttribute : PropertyAttribute
    {
        public string Text { get; }
        public InfoBoxType Type { get; }

        public InfoBoxAttribute(string text, InfoBoxType type = InfoBoxType.Info)
        {
            Text = text;
            Type = type;
        }
    }

    /// <summary>
    /// Type of info box to display
    /// </summary>
    public enum InfoBoxType
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Shows a progress bar for float values between 0 and 1
    /// Usage: [ProgressBar] or [ProgressBar("Health", 0f, 100f)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ProgressBarAttribute : PropertyAttribute
    {
        public string Label { get; }
        public float MinValue { get; }
        public float MaxValue { get; }
        public bool ShowValue { get; }

        public ProgressBarAttribute(string label = null, float minValue = 0f, float maxValue = 1f, bool showValue = true)
        {
            Label = label;
            MinValue = minValue;
            MaxValue = maxValue;
            ShowValue = showValue;
        }
    }

    /// <summary>
    /// Makes a field foldout/expandable in the inspector
    /// Usage: [Foldout("Group Name")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class FoldoutAttribute : PropertyAttribute
    {
        public string GroupName { get; }

        public FoldoutAttribute(string groupName)
        {
            GroupName = groupName;
        }
    }

    /// <summary>
    /// Displays a color picker for Color fields with additional options
    /// Usage: [ColorPicker] or [ColorPicker(true, true, false)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ColorPickerAttribute : PropertyAttribute
    {
        public bool ShowAlpha { get; }
        public bool HDR { get; }
        public bool ShowEyeDropper { get; }

        public ColorPickerAttribute(bool showAlpha = true, bool hdr = false, bool showEyeDropper = true)
        {
            ShowAlpha = showAlpha;
            HDR = hdr;
            ShowEyeDropper = showEyeDropper;
        }
    }

    /// <summary>
    /// Creates a dropdown selection for string or int values
    /// Usage: [Dropdown("option1", "option2", "option3")] for string
    /// Usage: [Dropdown(0, 1, 2, 3)] for int
    /// Usage: [DropdownNamed("value1|Display1", "value2|Display2", "value3|Display3")] for custom display names
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DropdownAttribute : PropertyAttribute
    {
        public object[] Values { get; }
        public string[] DisplayNames { get; }

        public DropdownAttribute(params object[] values)
        {
            Values = values;
            DisplayNames = new string[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                DisplayNames[i] = values[i]?.ToString() ?? "Null";
            }
        }

        // Internal constructor for DropdownNamed to use
        internal DropdownAttribute(object[] values, string[] displayNames)
        {
            Values = values;
            DisplayNames = displayNames;
        }
    }

    /// <summary>
    /// Creates a dropdown with custom display names using pipe (|) separator
    /// Usage: [DropdownNamed("1|Very Slow", "5|Slow", "10|Normal", "25|Fast", "50|Very Fast")]
    /// The part before | is the value, the part after | is the display name
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DropdownNamedAttribute : DropdownAttribute
    {
        public DropdownNamedAttribute(params string[] valueDisplayPairs)
            : base(ParseValues(valueDisplayPairs), ParseDisplayNames(valueDisplayPairs))
        {
        }

        private static object[] ParseValues(string[] valueDisplayPairs)
        {
            object[] values = new object[valueDisplayPairs.Length];
            for (int i = 0; i < valueDisplayPairs.Length; i++)
            {
                string pair = valueDisplayPairs[i];
                string[] parts = pair.Split('|');

                if (parts.Length >= 1)
                {
                    string valueStr = parts[0].Trim();
                    // Try to parse as int first, then as string
                    if (int.TryParse(valueStr, out int intValue))
                    {
                        values[i] = intValue;
                    }
                    else if (float.TryParse(valueStr, out float floatValue))
                    {
                        values[i] = floatValue;
                    }
                    else
                    {
                        values[i] = valueStr;
                    }
                }
                else
                {
                    values[i] = "";
                }
            }
            return values;
        }

        private static string[] ParseDisplayNames(string[] valueDisplayPairs)
        {
            string[] displayNames = new string[valueDisplayPairs.Length];
            for (int i = 0; i < valueDisplayPairs.Length; i++)
            {
                string pair = valueDisplayPairs[i];
                string[] parts = pair.Split('|');

                if (parts.Length >= 2)
                {
                    displayNames[i] = parts[1].Trim();
                }
                else
                {
                    displayNames[i] = parts[0].Trim();
                }
            }
            return displayNames;
        }
    }

    /// <summary>
    /// Creates a slider for numeric values
    /// Usage: [Slider(0f, 10f)] for float or [Slider(0, 100)] for int
    /// Note: This is similar to Unity's built-in Range attribute but with a more descriptive name
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SliderAttribute : PropertyAttribute
    {
        public float Min { get; }
        public float Max { get; }

        public SliderAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public SliderAttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }
}
