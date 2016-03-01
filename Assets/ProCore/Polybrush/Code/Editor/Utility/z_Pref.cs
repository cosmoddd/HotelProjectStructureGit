using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Polybrush
{
	/**
	 *	Editor preferences and defaults.
	 */
	public static class z_Pref
	{
		public const string ProductName = "Polybrush";
		public static string DocumentationLink { get { return "File://" + Application.dataPath + "/ProCore/Polybrush/Documentation/readme.html"; } }
		public const string ContactLink = "mailto:contact@procore3d.com";
		public const string WebsiteLink = "http://www.procore3d.com";

		/// Assembly Qualified name for ProBuilder Editor window.
		public const string PB_EDITOR_TYPE = "ProBuilder2.EditorCommon.pb_Editor";
		public const string PB_EDITOR_MESH_UTILITY_TYPE = "ProBuilder2.EditorCommon.pb_Editor_Mesh_Utility";
		public const string PB_EDITOR_ASSEMBLY = "ProBuilderEditor";

		public const string floatingEditorWindow = "z_pref_floatingEditorWindow";
		public const string lockBrushToFirst = "z_pref_lockBrushToFirst";
		public const string rebuildNormals = "z_pref_rebuildNormals";
		public const string hideWireframe = "z_pref_hideWireframe";
		public const string lockBrushSettings = "z_pref_lockBrushSettings";

		public const string brushColor = "z_pref_brushColor";
		public const string brushGradient = "z_pref_brushGradient";

		public const string sculptDirection = "z_pref_sculptDirection";
		public const string smoothRelax = "z_pref_smoothRelax";

		// [MenuItem("Tools/Clear Preferences")]
		public static void ClearPrefs()
		{
			EditorPrefs.DeleteKey(PB_EDITOR_TYPE);
			EditorPrefs.DeleteKey(PB_EDITOR_MESH_UTILITY_TYPE);
			EditorPrefs.DeleteKey(PB_EDITOR_ASSEMBLY);
			EditorPrefs.DeleteKey(floatingEditorWindow);
			EditorPrefs.DeleteKey(lockBrushToFirst);
			EditorPrefs.DeleteKey(rebuildNormals);
			EditorPrefs.DeleteKey(hideWireframe);
			EditorPrefs.DeleteKey(lockBrushSettings);
			EditorPrefs.DeleteKey(brushColor);
			EditorPrefs.DeleteKey(brushGradient);
			EditorPrefs.DeleteKey(sculptDirection);
			EditorPrefs.DeleteKey(smoothRelax);
		}

		static readonly Dictionary<string, bool> BoolDefaults = new Dictionary<string, bool>()
		{
			{ floatingEditorWindow, false },
			{ lockBrushToFirst, true },
			{ rebuildNormals, true },
			{ hideWireframe, true },
			{ lockBrushSettings, false },
			{ smoothRelax, true }
		};

		static readonly Dictionary<string, Color> ColorDefaults = new Dictionary<string, Color>()
		{
			{ brushColor, Color.green }
		};

		static readonly Dictionary<string, int> EnumDefaults = new Dictionary<string, int>()
		{
			{ sculptDirection, (int) z_Direction.Up }
		};

		public static bool GetBool(string key)
		{
			if(EditorPrefs.HasKey(key))
				return EditorPrefs.GetBool(key);
			else if( BoolDefaults.ContainsKey(key) )
				return BoolDefaults[key];
			else
				return true;
		}

		public static Color GetColor(string key)
		{
			if(EditorPrefs.HasKey(key))
			{
				uint u = (uint) EditorPrefs.GetInt(key);
				// conversion to byte truncates high bits
				return new Color(
					((byte)(u >> 24)) / 255f,
					((byte)(u >> 16)) / 255f,
					((byte)(u >>  8)) / 255f,
					((byte)(u 	   )) / 255f
					);
			}
			else if( ColorDefaults.ContainsKey(key) )
			{
				return ColorDefaults[key];
			}
			else
			{
				return Color.white;
			}
		}

		/**
		 *	Store @color as a 32 bit unsigned int.
		 */
		public static void SetColor(string key, Color color)
		{
			byte r = (byte) (color.r * 255);
			byte g = (byte) (color.g * 255);
			byte b = (byte) (color.b * 255);
			byte a = (byte) (color.a * 255);

			uint packed = (uint) ( (r << 24) | (g << 16) | (b << 8) | a );

			EditorPrefs.SetInt(key, (int) packed);
		}

		public static int GetEnum(string key)
		{
			if(EditorPrefs.HasKey(key))
				return EditorPrefs.GetInt(key);
			else if( BoolDefaults.ContainsKey(key) )
				return EnumDefaults[key];
			else
				return 0;
		}

		public static Gradient GetGradient(string key)
		{
			Gradient gradient = null;

			if( z_GradientSerializer.Deserialize(EditorPrefs.GetString(key), out gradient) )
				return gradient;

			Gradient g = new Gradient();
			
			g.SetKeys(
				new GradientColorKey[] { 
					new GradientColorKey(new Color(.1f, 0f, 1f, 1f), 0f),
					new GradientColorKey(Color.black, 1f)
					},
				new GradientAlphaKey[] {
					new GradientAlphaKey(1f, 0f),
					new GradientAlphaKey(1f, 1f),
					});

			return g;

		}

		public static void SetGradient(string key, Gradient gradient)
		{
			EditorPrefs.SetString(key, z_GradientSerializer.Serialize(gradient));
		}
	}
}
