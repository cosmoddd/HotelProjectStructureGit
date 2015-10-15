using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SuperLevelOptimizer : MonoBehaviour 
{
    public List<Renderer> renderers = new List<Renderer>();
    public bool superOptimization;

    public enum Search_State { Automatical, User };
    public enum Combine_State { CombineToScene, CombineToPrefab };

    public Search_State SearchState;
    public Combine_State CombineState;

    public string folderPatch = "Assets/";
}
