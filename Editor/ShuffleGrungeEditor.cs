using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ShuffleGrunge))]
public class ShuffleGrungeEdtor : Editor
{
    private ShuffleGrunge grunge { get { return target as ShuffleGrunge; } }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();
        if (EditorGUI.EndChangeCheck())
        {
            if (Application.isPlaying)
            {
                grunge.Refresh();
            }
        }
    }
}
