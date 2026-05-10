using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

[CustomPropertyDrawer(typeof(BlackboardEntryData))]
public class BlackboardEntryDataEditor : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {

        // Create property container element.
        var container = new VisualElement();

        //Get the property of the Key name and Value Type
        SerializedProperty keyNameProp = property.FindPropertyRelative("keyName");
        SerializedProperty valueTypeProp = property.FindPropertyRelative("valueType");

        //Add the Key Name and Value Type fields to the property container element
        container.Add(new PropertyField(keyNameProp));
        var valueTypeField = new PropertyField(property.FindPropertyRelative("valueType"));
        container.Add(valueTypeField);
        
        //The value field (empty for now)
        var valueField = new VisualElement();
        container.Add(valueField);

        //The value of the valueType field determines the field to render 
        valueTypeField.RegisterValueChangeCallback(evt => {
            //Clear whatever is currently being displayed first
            valueField.Clear();

            //By default an int field 
            string fieldName = "intValue";
            //Get the current valueType property 
            SerializedProperty valueTypeProp = property.FindPropertyRelative("valueType");

            //Get the correct property name accordingly
            switch ((BlackboardEntryData.ValueType)valueTypeProp.enumValueIndex)
            {
                case BlackboardEntryData.ValueType.Int:
                    fieldName = "intValue";
                    break;
                case BlackboardEntryData.ValueType.Float:
                    fieldName = "floatValue";
                    break;
                case BlackboardEntryData.ValueType.String:
                    fieldName = "stringValue";
                    break;
                case BlackboardEntryData.ValueType.Bool:
                    fieldName = "boolValue";
                    break;
                case BlackboardEntryData.ValueType.Vector3:
                    fieldName = "vector3Value";
                    break;
            }
            //Get the correct field
            var value = property.FindPropertyRelative(fieldName);
            var field = new PropertyField(value);
            //Have it displayed in the valueField 
            valueField.Add(field);

            //Force the inspector to update the field
            field.BindProperty(value);


        });
        
        

        return container;
    }


}
