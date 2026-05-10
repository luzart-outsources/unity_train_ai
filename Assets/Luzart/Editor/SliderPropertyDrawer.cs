using UnityEngine;
using UnityEditor;

namespace Luzart
{
    [CustomPropertyDrawer(typeof(SliderAttribute))]
    public class SliderPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SliderAttribute sliderAttribute = (SliderAttribute)attribute;

            if (property.propertyType == SerializedPropertyType.Float)
            {
                property.floatValue = EditorGUI.Slider(position, label, property.floatValue, sliderAttribute.Min, sliderAttribute.Max);
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = EditorGUI.IntSlider(position, label, property.intValue, (int)sliderAttribute.Min, (int)sliderAttribute.Max);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Slider only works with float or int values");
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
