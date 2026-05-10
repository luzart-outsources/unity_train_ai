using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LocationManager))]
public class LocationManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("Output Coords"))
        {
            GameObject markerParent = GameObject.Find("NPC Markers");
            foreach(Transform point in markerParent.transform)
            {
                //Output information of each marker
                Debug.Log(point.gameObject.name + " " + point.position + " " + point.rotation.eulerAngles);
                
            }
        }
    }
}
