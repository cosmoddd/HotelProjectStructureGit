using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NGS.SuperLevelOptimizer;

[CustomEditor(typeof(SuperLevelOptimizer))]
public class SuperLevelOptimizerEditor : Editor
{
    private List<GameObject> sources = new List<GameObject>();
    private List<Renderer> renderers = new List<Renderer>();


    [MenuItem("Tools/NGSTools/SuperLevelOptimizer/Create Optimizer")]
    public static void CreateOptimizer()
    {
        GameObject go = new GameObject("Super Level Optimizer", typeof(SuperLevelOptimizer));
    }

    [MenuItem("Tools/NGSTools/SuperLevelOptimizer/Create Prefab")]
    public static void CreatePrefab()
    {
        if (Selection.gameObjects != null)
        {
            for (int i = 0; i < Selection.gameObjects.Length; i++)
            {
                AssetsReimporter.ImportAssets(Selection.gameObjects[i], "Assets/SLO/");

                SuperLevelOptimizerEditor.RecursivelyReimport(Selection.gameObjects[i]);

                AssetsReimporter.CreatePrefab(Selection.gameObjects[i], "Assets/SLO/Prefabs/");
            }
        }
    }

    private static void RecursivelyReimport(GameObject go)
    {
        AssetsReimporter.ImportAssets(go, "Assets/SLO/");

        if (go.transform.childCount > 0)
        {
            for (int i = 0; i < go.transform.childCount; i++)
                RecursivelyReimport(go.transform.GetChild(i).gameObject);
        }
    }

    private static void RecursivelyReimport(GameObject go, string folderPath)
    {
        try
        {
            AssetsReimporter.ImportAssets(go, folderPath);
        }
        catch { }

        if (go.transform.childCount > 0)
        {
            for (int i = 0; i < go.transform.childCount; i++)
                RecursivelyReimport(go.transform.GetChild(i).gameObject, folderPath);
        }
    }

    public override void OnInspectorGUI()
    {
        SuperLevelOptimizer SLO = (SuperLevelOptimizer)target;

        EditorGUILayout.HelpBox("SuperLevelOptimizer only works with static objects.",
            UnityEditor.MessageType.Info);

        SLO.superOptimization = GUILayout.Toggle(SLO.superOptimization, "Super Optimization");

        SLO.SearchState = (SuperLevelOptimizer.Search_State)EditorGUILayout.EnumPopup("Search State : ", SLO.SearchState);
        if (SLO.SearchState == SuperLevelOptimizer.Search_State.User)
        {
            if (GUILayout.Button("Open Manager Window"))
            {
                SuperLevelOptimizerWindow SLOWindow = (SuperLevelOptimizerWindow)EditorWindow.GetWindow(typeof(SuperLevelOptimizerWindow));
                SLOWindow.SLO = SLO;
            }

            SerializedProperty s_prop = serializedObject.FindProperty("renderers");

            serializedObject.Update();
            if (EditorGUILayout.PropertyField(s_prop))
            {
                for (int i = 0; i < s_prop.arraySize; i++)
                    EditorGUILayout.PropertyField(s_prop.GetArrayElementAtIndex(i));
            }
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
        }

        SLO.CombineState = (SuperLevelOptimizer.Combine_State)EditorGUILayout.EnumPopup("Combine State :", SLO.CombineState);
        if (SLO.CombineState == SuperLevelOptimizer.Combine_State.CombineToPrefab)
            SLO.folderPatch = EditorGUILayout.TextField("Folder patch", SLO.folderPatch);

        if (GUILayout.Button("Create Atlases"))
        {
            List<Renderer> rend = new List<Renderer>();

            if (SLO.SearchState == SuperLevelOptimizer.Search_State.Automatical)
            {
                rend = FindObjectsOfType<Renderer>().ToList();

                rend = rend.Where(r =>
                GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.BatchingStatic) &&
                r.gameObject.GetComponent<MeshFilter>() != null &&
                r.gameObject.GetComponent<MeshFilter>().sharedMesh != null &&
                r.sharedMaterials.Length > 0 &&
                r.enabled == true 
                ).ToList();
            }
            else
                rend = SLO.renderers;

            for (int i = 0; i < rend.Count; i++)
            {
                try
                {
                    TexturePacker.SplitMesh(rend[i], ref rend);
                }
                catch { continue; }
            }

            ShaderGroup[] shaderGroups = MaterialsSorter.SortByShaders(rend.ToArray());

            System.GC.Collect();

            TexturePacker.SetReadableFlag = AssetsReimporter.SetReadableFlag;

            for (int i = 0; i < shaderGroups.Length; i++)
            {
                try
                {
                    TexturePacker.PackTextures(shaderGroups[i], SLO.superOptimization);
                }
                catch
                { continue; }
                System.GC.Collect();
            }
        }

        if (GUILayout.Button("Combine Meshes"))
        {
            List<Renderer> rend = new List<Renderer>();

            if (SLO.SearchState == SuperLevelOptimizer.Search_State.Automatical)
            {
                rend = FindObjectsOfType<Renderer>().ToList();

                rend = rend.Where(r =>
                GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.BatchingStatic) &&
                r.gameObject.GetComponent<MeshFilter>() != null &&
                r.gameObject.GetComponent<MeshFilter>().sharedMesh != null &&
                r.sharedMaterials.Length > 0 &&
                r.enabled == true
                ).ToList();
            }
            else
                rend = SLO.renderers;

            MaterialGroup[] materialGroups = MaterialsSorter.SortByMaterials(rend.ToArray());

            System.GC.Collect();

            GameObject oldObjects = new GameObject("Old objects");
            GameObject combinedMeshes = new GameObject("Combined Meshes");

            for (int i = 0; i < materialGroups.Length; i++)
            {
                try
                {
                    GameObject go = new GameObject("Combine Mesh " + materialGroups[i].renderers[0].gameObject.name);

                    MeshFilter filter = go.AddComponent<MeshFilter>();
                    Renderer renderer = (Renderer)go.AddComponent(materialGroups[i].renderers[0].GetType());

                    System.GC.Collect();

                    renderer.sharedMaterials = materialGroups[i].renderers[0].sharedMaterials;
                    filter.mesh = MeshCombiner.CombineMeshes(materialGroups[i]);

                    System.GC.Collect();

                    if (SLO.CombineState == SuperLevelOptimizer.Combine_State.CombineToScene)
                    {
                        for (int c = 0; c < materialGroups[i].renderers.Count; c++)
                            materialGroups[i].renderers[c].transform.parent = oldObjects.transform;
                    }

                    go.transform.parent = combinedMeshes.transform;
                }
                catch { continue; };
            }

            if (SLO.CombineState == SuperLevelOptimizer.Combine_State.CombineToPrefab)
            {
                RecursivelyReimport(combinedMeshes, SLO.folderPatch);

                AssetsReimporter.CreatePrefab(combinedMeshes, SLO.folderPatch);

                DestroyImmediate(combinedMeshes);
                DestroyImmediate(oldObjects);
            }
            else
                sources.Add(oldObjects);
        }

        if (GUILayout.Button("Destroy Sources"))
        {
            for (int i = 0; i < sources.Count; i++)
                DestroyImmediate(sources[i]);
        }
    }
}

namespace NGS.SuperLevelOptimizer
{
    public class AssetsReimporter
    {
        public static void SetReadableFlag(Texture texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter texturesImporter = TextureImporter.GetAtPath(path) as TextureImporter;

            if (texturesImporter != null)
            {
                if (texturesImporter.isReadable == false)
                {
                    texturesImporter.isReadable = true;
                    AssetDatabase.ImportAsset(path);
                }
            }
        }

        public static void ResizeTexture(Texture2D texture, int width, int height)
        {

        }

        public static void ImportAssets(GameObject mesh, string folderPatch)
        {
            string path = folderPatch;

            if (!Directory.Exists(folderPatch))
                Directory.CreateDirectory(folderPatch);

            path = folderPatch + "Meshes/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            if (mesh.GetComponent<MeshFilter>() != null)
                if (mesh.GetComponent<MeshFilter>().sharedMesh != null)
                    if (AssetDatabase.GetAssetPath(mesh.GetComponent<MeshFilter>().sharedMesh) == "")
                        AssetDatabase.CreateAsset(mesh.GetComponent<MeshFilter>().sharedMesh, path + mesh.GetComponent<MeshFilter>().sharedMesh.name + mesh.GetComponent<MeshFilter>().sharedMesh.GetInstanceID() + ".asset");

            path = folderPatch + "Textures/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            if (mesh.GetComponent<MeshRenderer>() != null)
                if (mesh.GetComponent<MeshRenderer>().sharedMaterials.Length > 0)
                    for (int i = 0; i < mesh.GetComponent<MeshRenderer>().sharedMaterials.Length; i++)
                    {
                        if (mesh.GetComponent<MeshRenderer>().sharedMaterials[i] != null)
                        {
                            if (mesh.GetComponent<MeshRenderer>().sharedMaterials[i].mainTexture != null)
                                if (AssetDatabase.GetAssetPath(mesh.GetComponent<MeshRenderer>().sharedMaterials[i].mainTexture) == "")
                                {
                                    Texture2D texture = (Texture2D)mesh.GetComponent<MeshRenderer>().sharedMaterials[i].mainTexture;

                                    FileStream fStream = new FileStream(path + "Texture" + mesh.GetComponent<MeshRenderer>().sharedMaterials[i].mainTexture.name + mesh.GetComponent<MeshRenderer>().sharedMaterials[i].mainTexture.GetInstanceID() + ".png", FileMode.Create, FileAccess.Write);

                                    BinaryWriter writer = new BinaryWriter(fStream);

                                    Color[] colors = texture.GetPixels();

                                    Texture2D newTexture = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, true);

                                    newTexture.SetPixels(colors);

                                    byte[] bytes = newTexture.EncodeToPNG();

                                    writer.Write(bytes);

                                    writer.Close();
                                    fStream.Close();

                                    AssetDatabase.ImportAsset(path + "Texture" + mesh.GetComponent<MeshRenderer>().sharedMaterials[i].mainTexture.name + mesh.GetComponent<MeshRenderer>().sharedMaterials[i].mainTexture.GetInstanceID() + ".png");

                                    AssetDatabase.SaveAssets();

                                    mesh.GetComponent<MeshRenderer>().sharedMaterials[i].mainTexture = (Texture)AssetDatabase.LoadAssetAtPath(path + "Texture" + mesh.GetComponent<MeshRenderer>().sharedMaterials[i].mainTexture.name + mesh.GetComponent<MeshRenderer>().sharedMaterials[i].mainTexture.GetInstanceID() + ".png", typeof(Texture));
                                }

                            path = folderPatch + "Materials/";
                            if (!Directory.Exists(path))
                                Directory.CreateDirectory(path);

                            if (AssetDatabase.GetAssetPath(mesh.GetComponent<MeshRenderer>().sharedMaterials[i]) == "")
                            {
                                AssetDatabase.CreateAsset(mesh.GetComponent<MeshRenderer>().sharedMaterials[i], path + "Material" + mesh.GetComponent<MeshRenderer>().sharedMaterials[i].name + mesh.GetComponent<MeshRenderer>().sharedMaterials[i].GetInstanceID() + ".asset");
                            }
                        }
                    }
        }

        public static void CreatePrefab(GameObject mesh, string folderPatch)
        {
            if (!Directory.Exists(folderPatch + "Prefabs/"))
                Directory.CreateDirectory(folderPatch + "Prefabs/");

            PrefabUtility.CreatePrefab(folderPatch + "Prefabs/" + mesh.name + mesh.GetHashCode() + ".prefab", mesh);
        }
    }
}
