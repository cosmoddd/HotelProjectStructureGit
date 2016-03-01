using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Polybrush
{
	[CustomEditor(typeof(z_PrefabPalette), isFallback = true)]
	public class z_PrefabPaletteEditor : Editor
	{
		private ReorderableList reorderableList;
		private SerializedProperty listProperty;

		public Delegate<GameObject> onSelectIndex = null;

		private void OnEnable()
		{
			listProperty = serializedObject.FindProperty("prefabs");
			reorderableList = new ReorderableList(	serializedObject,
													listProperty,
													true,
													true,
													true,
													true);

			reorderableList.drawHeaderCallback = DrawHeader;
			reorderableList.drawElementCallback = DrawListElement;
			reorderableList.onAddCallback = OnAddItem;

			reorderableList.onSelectCallback = (ReorderableList list) => 
			{
				if( onSelectIndex != null )
					onSelectIndex( (GameObject) listProperty.GetArrayElementAtIndex(list.index).objectReferenceValue );
			};
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			reorderableList.DoLayoutList();

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawHeader(Rect rect)
		{
			EditorGUI.LabelField(rect, serializedObject.targetObject.name);
		}

		private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			SerializedProperty col = listProperty.GetArrayElementAtIndex(index);
			Rect r = new Rect(rect.x, rect.y + 2, rect.width, rect.height - 5);
			EditorGUI.PropertyField(r, col);
		}

		private void OnAddItem(ReorderableList list)
		{
			ReorderableList.defaultBehaviours.DoAddButton(list);
		}
	}
}
