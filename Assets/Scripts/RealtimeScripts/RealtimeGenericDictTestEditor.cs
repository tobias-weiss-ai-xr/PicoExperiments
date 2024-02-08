using UnityEditor;
using UnityEngine;


#if UNITY_EDITOR
[CustomEditor(typeof(RealtimeGenericDictTest))]
[CanEditMultipleObjects]
public class RealtimeGenericDictTestEditor : Editor
{
    public override void OnInspectorGUI()
    {

        DrawDefaultInspector();

        RealtimeGenericDictTest myScript = (RealtimeGenericDictTest)target;

        if (GUILayout.Button("Create new dict"))
            myScript.NewRandomDict();

        if (GUILayout.Button("Print dict"))
            myScript.PrintDict();
    }
}
#endif