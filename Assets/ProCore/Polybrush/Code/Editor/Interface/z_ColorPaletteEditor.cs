using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Polybrush
{
	[CustomEditor(typeof(z_ColorPalette), isFallback = true)]
	public class z_ColorPaletteEditor : Editor
	{
		private ReorderableList colorsList;
		private SerializedProperty colorsProperty;

		public Delegate<Color> onSelectIndex = null;

		public Delegate<z_ColorPalette> onSaveAs = null;

		private void OnEnable()
		{
			colorsProperty = serializedObject.FindProperty("colors");

			colorsList = new ReorderableList(	serializedObject,
												colorsProperty,
												true,
												true,
												true,
												true);

			colorsList.drawHeaderCallback = DrawHeader;
			colorsList.drawElementCallback = DrawListElement;
			colorsList.onAddCallback = OnAddItem;

			colorsList.onSelectCallback = (ReorderableList list) => 
			{
				if( onSelectIndex != null )
					onSelectIndex( colorsProperty.GetArrayElementAtIndex(list.index).colorValue );
			};
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			colorsList.DoLayoutList();

			GUILayout.BeginHorizontal();

				if(GUILayout.Button("Set Defaults"))
				{
					Undo.RecordObject(serializedObject.targetObject, "Set Default Color Palette");
					((z_ColorPalette)serializedObject.targetObject).SetDefaultValues();
				}

				if(GUILayout.Button("Save As"))
				{
					string path = EditorUtility.SaveFilePanelInProject("Save Color Palette", "New Color Palette", "asset", "Save current color palette settings as template.");

					if(!string.IsNullOrEmpty(path))
					{
						z_ColorPalette palette = ScriptableObject.CreateInstance<z_ColorPalette>();
						((z_ColorPalette)target).CopyTo(palette);

						AssetDatabase.CreateAsset( palette, path );
						AssetDatabase.Refresh();

						if(onSaveAs != null)
							onSaveAs(palette);
					}
				}
			GUILayout.EndHorizontal();

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawHeader(Rect rect)
		{
			EditorGUI.LabelField(rect, serializedObject.targetObject.name);
		}

		private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			SerializedProperty col = colorsProperty.GetArrayElementAtIndex(index);
			Rect r = new Rect(rect.x, rect.y + 2, rect.width, rect.height - 5);
			EditorGUI.PropertyField(r, col);
		}

		private void OnAddItem(ReorderableList list)
		{
			ReorderableList.defaultBehaviours.DoAddButton(list);

			SerializedProperty col = colorsProperty.GetArrayElementAtIndex(list.index);
			col.colorValue = Color.white;
		}

		public static z_ColorPalette AddNew()
		{
			string path = z_EditorUtility.FindFolder(z_Pref.ProductName + "/" + "Color Palettes");

			if(string.IsNullOrEmpty(path))
				path = "Assets";

			path = AssetDatabase.GenerateUniqueAssetPath(path + "/New Color Palette.asset");

			if(!string.IsNullOrEmpty(path))
			{
				z_ColorPalette palette = ScriptableObject.CreateInstance<z_ColorPalette>();
				palette.SetDefaultValues();

				AssetDatabase.CreateAsset(palette, path);
				AssetDatabase.Refresh();

				EditorGUIUtility.PingObject(palette);

				return palette;
			}

			return null;
		}
	}
}
