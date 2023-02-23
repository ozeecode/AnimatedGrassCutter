using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(GrassChunk))]
public class GrassChunkEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GrassChunk grassChunk = (GrassChunk)target;

        if (GUILayout.Button("Generate"))
        {
            grassChunk.Generate();
        }
        if (GUILayout.Button("Clear"))
        {
            grassChunk.Clear();
        }
    }


}
