using UnityEngine;
using UnityEditor;
using Kumu;

namespace KumuEditor
{
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
}