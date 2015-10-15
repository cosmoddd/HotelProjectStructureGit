using UnityEngine;
using UnityEditor;
using System.Collections;

public class SuperLevelOptimizerWindow : EditorWindow 
{
    public SuperLevelOptimizer SLO;

    private void OnGUI()
    {
        if (GUILayout.Button("Add selection"))
        {
            if (Selection.gameObjects != null)
                for (int i = 0; i < Selection.gameObjects.Length; i++)
                    AddSelection(Selection.gameObjects[i]);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Remove selection"))
        {
            if (Selection.gameObjects != null)
                for (int i = 0; i < Selection.gameObjects.Length; i++)
                    RemoveSelection(Selection.gameObjects[i]);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Remove all"))
        {
            if (Selection.gameObjects != null)
                RemoveAll();
        }
    }

    private void AddSelection(GameObject gameObject)
    {
        Renderer rend = gameObject.GetComponent<Renderer>();
        if (rend != null)
        {
            if (GameObjectUtility.AreStaticEditorFlagsSet(rend.gameObject, StaticEditorFlags.BatchingStatic) &&
                rend.gameObject.GetComponent<MeshFilter>() != null &&
                rend.gameObject.GetComponent<MeshFilter>().sharedMesh != null &&
                rend.sharedMaterials.Length > 0 &&
                rend.enabled == true
                )

                if (!SLO.renderers.Contains(rend))
                    SLO.renderers.Add(rend);
        }

        if (rend.gameObject.transform.childCount > 1)
        {
            for (int i = 1; i < rend.transform.childCount; i++)
                AddSelection(rend.transform.GetChild(i).gameObject);
        }
    }

    private void RemoveSelection(GameObject gameObject)
    {
        Renderer rend = gameObject.GetComponent<Renderer>();

        if (rend != null)
        {
            if (SLO.renderers.Contains(rend))
                SLO.renderers.Remove(rend);
        }

        if (rend.gameObject.transform.childCount > 1)
        {
            for (int i = 1; i < rend.transform.childCount; i++)
                RemoveSelection(rend.transform.GetChild(i).gameObject);
        }
    }

    private void RemoveAll()
    {
        SLO.renderers = new System.Collections.Generic.List<Renderer>();
    }
}
