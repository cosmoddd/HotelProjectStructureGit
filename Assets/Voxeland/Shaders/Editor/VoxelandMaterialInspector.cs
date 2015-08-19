using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

 
public class VoxelandMaterialInspector : MaterialEditor
{
	public override void OnInspectorGUI ()
	{
		if (!isVisible) { base.OnInspectorGUI (); return; }

		Material targetMat = target as Material;
		
		bool temp = targetMat.IsKeywordEnabled("_NORMALMAP");
		temp = EditorGUILayout.ToggleLeft("Normal Map", temp);
		if (temp) targetMat.EnableKeyword("_NORMALMAP"); else targetMat.DisableKeyword("_NORMALMAP"); 

		temp = targetMat.IsKeywordEnabled("_SPECGLOSSMAP");
		temp = EditorGUILayout.ToggleLeft("Specular/Metallic and Gloss Map", temp);
		if (temp) targetMat.EnableKeyword("_SPECGLOSSMAP"); else targetMat.DisableKeyword("_SPECGLOSSMAP"); 

		temp = targetMat.IsKeywordEnabled("_PARALLAXMAP");
		temp = EditorGUILayout.ToggleLeft("Parallax Map", temp);
		if (temp) targetMat.EnableKeyword("_PARALLAXMAP"); else targetMat.DisableKeyword("_PARALLAXMAP"); 

		temp = targetMat.IsKeywordEnabled("_WIND");
		temp = EditorGUILayout.ToggleLeft("Use Wind Animation", temp);
		if (temp) targetMat.EnableKeyword("_WIND"); else targetMat.DisableKeyword("_WIND"); 

		//draw default inspector
		base.OnInspectorGUI();
	}
}
