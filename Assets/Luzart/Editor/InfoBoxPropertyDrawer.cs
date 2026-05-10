using UnityEngine;
using UnityEditor;

namespace Luzart
{
    [CustomPropertyDrawer(typeof(InfoBoxAttribute))]
    public class InfoBoxPropertyDrawer : DecoratorDrawer
    {
        public override void OnGUI(Rect position)
        {
            InfoBoxAttribute infoBoxAttribute = (InfoBoxAttribute)attribute;

            // Get the appropriate message type
            MessageType messageType = MessageType.Info;
            switch (infoBoxAttribute.Type)
            {
                case InfoBoxType.Warning:
                    messageType = MessageType.Warning;
                    break;
                case InfoBoxType.Error:
                    messageType = MessageType.Error;
                    break;
            }

            // Draw the help box
            EditorGUI.HelpBox(position, infoBoxAttribute.Text, messageType);
        }

        public override float GetHeight()
        {
            InfoBoxAttribute infoBoxAttribute = (InfoBoxAttribute)attribute;

            // Calculate height based on text content
            GUIStyle style = EditorStyles.helpBox;
            float height = style.CalcHeight(new GUIContent(infoBoxAttribute.Text), EditorGUIUtility.currentViewWidth);
            return height + 4; // Add some padding
        }
    }
}
