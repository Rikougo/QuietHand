using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MaterialDissolve))]
public class MaterialDissolveInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Play"))
        {
            ((MaterialDissolve)target).Play();
        }
    }
}