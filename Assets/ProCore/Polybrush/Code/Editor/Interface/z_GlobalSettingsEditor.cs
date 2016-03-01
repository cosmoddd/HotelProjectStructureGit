using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Polybrush
{
	internal class z_GlobalSettingsEditor : Editor
	{
		private static bool initialized = false;

		private static readonly GUIContent gc_rebuildNormals = new GUIContent("Rebuild Normals", "After a mesh modification the normals will be recalculated.");
		private static readonly GUIContent gc_lockBrushSettings = new GUIContent("Anchor Brush Settings", "Locks the Brush Settings to the top of the window.");
		private static readonly GUIContent gc_hideWireframe = new GUIContent("Hide Wireframe", "Hides the object wireframe when a brush is hovering.");
		private static readonly GUIContent gc_fullStrengthColor = new GUIContent("Brush Handle Color", "The color that the brush handle will render.");
		private static readonly GUIContent gc_BrushGradient = new GUIContent("Brush Gradient", "");

		private static bool rebuildNormals { get { return z_Pref.GetBool(z_Pref.rebuildNormals); } set { EditorPrefs.SetBool(z_Pref.rebuildNormals, value); } }
		private static bool hideWireframe { get { return z_Pref.GetBool(z_Pref.hideWireframe); } set { EditorPrefs.SetBool(z_Pref.hideWireframe, value); } }
		private static bool lockBrushSettings { get { return z_Pref.GetBool(z_Pref.lockBrushSettings); } set { EditorPrefs.SetBool(z_Pref.lockBrushSettings, value); } }
		
		private static Color fullStrengthColor { get { return z_Pref.GetColor(z_Pref.brushColor); } set { z_Pref.SetColor(z_Pref.brushColor, value); } }
		private static Color brushGradient { get { return z_Pref.GetColor(z_Pref.brushGradient); } set { z_Pref.SetColor(z_Pref.brushGradient, value); } }

		private static Gradient gradient;

		static void GetPreferences()
		{
			gradient = z_Pref.GetGradient(z_Pref.brushGradient);
		}

		static void SetPreferences()
		{
			z_Pref.SetGradient(z_Pref.brushGradient, gradient);
		}

		internal static void OnGUI()
		{
			if(!initialized)
				GetPreferences();

			GUILayout.Label("Settings", z_GUI.headerTextStyle);

			rebuildNormals = EditorGUILayout.Toggle(gc_rebuildNormals, rebuildNormals);
			hideWireframe = EditorGUILayout.Toggle(gc_hideWireframe, hideWireframe);
			lockBrushSettings = EditorGUILayout.Toggle(gc_lockBrushSettings, lockBrushSettings);
			fullStrengthColor = EditorGUILayout.ColorField(gc_fullStrengthColor, fullStrengthColor);

			try
			{
				EditorGUI.BeginChangeCheck();
				
				object out_gradient = z_ReflectionUtil.Invoke(	null,
																typeof(EditorGUILayout),
																"GradientField",
																new System.Type[] { typeof(GUIContent), typeof(Gradient), typeof(GUILayoutOption[]) },
																BindingFlags.NonPublic | BindingFlags.Static,
																new object[] { gc_BrushGradient, gradient, null });
				gradient = (Gradient) out_gradient;

				if(EditorGUI.EndChangeCheck())
					SetPreferences();
			}
			catch
			{
				// internal editor gripe about something unimportant
			}
		}
	}
}
