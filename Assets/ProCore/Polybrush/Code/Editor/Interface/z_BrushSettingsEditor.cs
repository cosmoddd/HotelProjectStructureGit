using UnityEngine;
using UnityEditor;

namespace Polybrush
{
	/**
	 *	The default editor for z_BrushSettings.
	 */
	[CustomEditor(typeof(z_BrushSettings), isFallback = true)]
	public class z_BrushSettingsEditor : Editor
	{
		public bool showSettingsBounds = false;
		public Texture settingsIcon;
		private GUIStyle 	settingsButtonStyle,
							settingsBackgroundStyle,
							settingsBackgroundBorderStyle;

		private GUIContent gc_Radius = new GUIContent("Radius", "The distance from the center of a brush to it's outer edge.\n\nShortcut: 'Ctrl + Mouse Wheel'");
		private GUIContent gc_Falloff = new GUIContent("Falloff", "The distance from the center of a brush at which the strength begins to linearly taper to 0.  This value is normalized, 1 means the entire brush gets full strength, 0 means the very center point of a brush is full strength and the edges are 0.\n\nShortcut: 'Shift + Mouse Wheel'");
		private GUIContent gc_Strength = new GUIContent("Strength", "The effectiveness of this brush.  The actual applied strength also depends on the Falloff setting.\n\nShortcut: 'Ctrl + Shift + Mouse Wheel'");

		private static readonly Color SETTINGS_BACKGROUND_COLOR = new Color(.24f, .24f, .24f, 1f);
		private static readonly Color SETTINGS_BORDER_COLOR = new Color(.298f, .298f, .298f, 1f);

		private static readonly Rect RECT_ONE = new Rect(0,0,1,1);

		SerializedProperty 	radius,
							falloff,
							strength,
							brushRadiusMin,
							brushRadiusMax,
							brushStrengthMin,
							brushStrengthMax,
							curve,
							allowNonNormalizedFalloff;

		public void OnEnable()
		{
			settingsIcon = EditorGUIUtility.IconContent("TerrainInspector.TerrainToolSettings").image;

			settingsButtonStyle = new GUIStyle();
			settingsButtonStyle.imagePosition = ImagePosition.ImageOnly;
			const int PAD = 2, MARGIN_HORIZONTAL = 4, MARGIN_VERTICAL = 0;
			settingsButtonStyle.alignment = TextAnchor.MiddleCenter;
			settingsButtonStyle.margin = new RectOffset(MARGIN_HORIZONTAL, MARGIN_HORIZONTAL, MARGIN_VERTICAL, MARGIN_VERTICAL);
			settingsButtonStyle.padding = new RectOffset(PAD, PAD, 4, PAD);

			settingsBackgroundStyle = new GUIStyle();
			settingsBackgroundStyle.normal.background = EditorGUIUtility.whiteTexture;
			settingsBackgroundStyle.margin = new RectOffset(0,0,0,0);
			settingsBackgroundStyle.padding = new RectOffset(2,2,4,4);

			settingsBackgroundBorderStyle = new GUIStyle();
			settingsBackgroundBorderStyle.normal.background = EditorGUIUtility.whiteTexture;
			settingsBackgroundBorderStyle.margin = new RectOffset(6,6,0,6);
			settingsBackgroundBorderStyle.padding = new RectOffset(1,1,1,1);

			/// User settable
			radius = serializedObject.FindProperty("_radius");
			falloff = serializedObject.FindProperty("_falloff");
			curve = serializedObject.FindProperty("_curve");
			strength = serializedObject.FindProperty("_strength");

			/// Bounds
			brushRadiusMin = serializedObject.FindProperty("brushRadiusMin");
			brushRadiusMax = serializedObject.FindProperty("brushRadiusMax");
			allowNonNormalizedFalloff = serializedObject.FindProperty("allowNonNormalizedFalloff");
		}

		private bool approx(float lhs, float rhs)
		{
			return Mathf.Abs(lhs-rhs) < .0001f;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			GUILayout.BeginHorizontal();
				GUILayout.Label("Brush Settings", z_GUI.headerTextStyle);
				GUILayout.FlexibleSpace();
				if(GUILayout.Button(settingsIcon, settingsButtonStyle))
					showSettingsBounds = !showSettingsBounds;
			GUILayout.EndHorizontal();

			if(showSettingsBounds)
			{
				z_GUI.PushBackgroundColor(SETTINGS_BORDER_COLOR);
				GUILayout.BeginVertical(settingsBackgroundBorderStyle);
				z_GUI.PushBackgroundColor(SETTINGS_BACKGROUND_COLOR);
				GUILayout.BeginVertical(settingsBackgroundStyle);
				z_GUI.PopBackgroundColor();
				z_GUI.PopBackgroundColor();

				EditorGUILayout.PropertyField(brushRadiusMin);
				brushRadiusMin.floatValue = Mathf.Clamp(brushRadiusMin.floatValue, .0001f, Mathf.Infinity);

				EditorGUILayout.PropertyField(brushRadiusMax);
				brushRadiusMax.floatValue = Mathf.Clamp(brushRadiusMax.floatValue, brushRadiusMin.floatValue, Mathf.Infinity);

				// EditorGUILayout.PropertyField(brushStrengthMin);
				// brushStrengthMin.floatValue = Mathf.Clamp(brushStrengthMin.floatValue, .0001f, Mathf.Infinity);

				// EditorGUILayout.PropertyField(brushStrengthMax);
				// brushStrengthMax.floatValue = Mathf.Clamp(brushStrengthMax.floatValue, brushStrengthMin.floatValue, Mathf.Infinity);

				EditorGUILayout.PropertyField(allowNonNormalizedFalloff);

				if(GUILayout.Button("Save As"))
				{
					string path = EditorUtility.SaveFilePanelInProject("Save Brush Settings", "New Brush Settings", "asset", "Save current brush settings as template.");

					if(!string.IsNullOrEmpty(path))
					{
						z_BrushSettings settings = ScriptableObject.CreateInstance<z_BrushSettings>();
						((z_BrushSettings)target).CopyTo(settings);

						AssetDatabase.CreateAsset(settings, path );
						AssetDatabase.Refresh();

						if(z_Editor.instance != null)
							z_Editor.instance.SetBrush(settings);
					}
				}

				GUILayout.EndVertical();
				GUILayout.EndVertical();
			}

			radius.floatValue = EditorGUILayout.Slider(gc_Radius, radius.floatValue, brushRadiusMin.floatValue, brushRadiusMax.floatValue);
			
			falloff.floatValue = EditorGUILayout.Slider(gc_Falloff, falloff.floatValue, 0f, 1f);

			if(allowNonNormalizedFalloff.boolValue)
				EditorGUILayout.PropertyField(curve);
			else
				curve.animationCurveValue = EditorGUILayout.CurveField("Falloff Curve", curve.animationCurveValue, Color.green, RECT_ONE);

			Keyframe[] keys = curve.animationCurveValue.keys;

			if( (approx(keys[0].time, 0f) && approx(keys[0].value, 0f) && approx(keys[1].time, 1f) && approx(keys[1].value, 1f)) )
			{
				Keyframe[] rev = new Keyframe[keys.Length];

				for(int i = 0 ; i < keys.Length; i++)
					rev[keys.Length - i -1] = new Keyframe(1f - keys[i].time, keys[i].value, -keys[i].outTangent, -keys[i].inTangent);
					
				curve.animationCurveValue = new AnimationCurve(rev);

			}

			strength.floatValue = EditorGUILayout.Slider(gc_Strength, strength.floatValue, 0f, 1f);

			serializedObject.ApplyModifiedProperties();

			SceneView.RepaintAll();
		}

		public static z_BrushSettings AddNew()
		{
			string path = z_EditorUtility.FindFolder(z_Pref.ProductName + "/" + "Brush Settings");

			if(string.IsNullOrEmpty(path))
				path = "Assets";

			path = AssetDatabase.GenerateUniqueAssetPath(path + "/New Brush.asset");

			if(!string.IsNullOrEmpty(path))
			{
				z_BrushSettings settings = ScriptableObject.CreateInstance<z_BrushSettings>();
				settings.SetDefaultValues();

				AssetDatabase.CreateAsset(settings, path);
				AssetDatabase.Refresh();

				EditorGUIUtility.PingObject(settings);

				return settings;
			}

			return null;
		}
	}
}
