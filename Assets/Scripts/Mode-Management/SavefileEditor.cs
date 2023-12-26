using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(Savefile))]
[CanEditMultipleObjects]
public class SavefileEditor : Editor
{
    public override void OnInspectorGUI()
    {

        DrawDefaultInspector();

        Savefile myScript = (Savefile)target;

        if (GUILayout.Button("Save JSON"))
            myScript.Save();

        if (GUILayout.Button("Load JSON"))
            myScript.Load();
    }
}
#endif