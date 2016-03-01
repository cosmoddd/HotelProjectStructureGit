using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Polybrush
{
	/**
	 *	Interface and settings for Vertex Sculpting
	 */
	[System.Serializable]
	public class z_Editor : EditorWindow
	{
		private static z_Editor _instance = null;
		public static z_Editor instance { get { return _instance; } }

		/// Path to EditorWindow icon.
		const string ICON_PATH = "icon";

		/// The current editing mode (RaiseLower, Smooth, Color, etc).
		private z_BrushMode mode
		{
			get
			{
				return modes.Count > 0 ? modes[0] : null;
			}
			set
			{
				if(modes.Contains(value))
					modes.Remove(value);
				modes.Insert(0, value);
			}
		}

		[SerializeField] List<z_BrushMode> modes = new List<z_BrushMode>();

		/// The current editing mode (RaiseLower, Smooth, Color, etc).
		public z_BrushTool tool = z_BrushTool.None;

		/// Editor for the current brush settings.
		private Editor _brushEditor = null;

		public Editor brushEditor
		{
			get
			{
				if(_brushEditor == null || _brushEditor.target != brushSettings)
					_brushEditor = Editor.CreateEditor(brushSettings);
				return _brushEditor;
			}
		}

		/// All objects that have been hovered by the mouse
		public List<z_EditableObject> hovering = new List<z_EditableObject>();

		/// The current brush status
		public z_BrushTarget brushTarget = new z_BrushTarget();

		/// The current brush settings
		public z_BrushSettings brushSettings;

		/// Mirror settings for this brush.
		public z_BrushMirror brushMirror = z_BrushMirror.None;

		/// In which coordinate space the brush ray is flipped.
		public z_MirrorCoordinateSpace mirrorSpace = z_MirrorCoordinateSpace.World;

		int currentBrushIndex = 0;
		List<z_BrushSettings> availableBrushes = null;
		string[] availableBrushes_str = null;

		private GUIContent[] mirrorGuiContent = null;

		private GUIContent[] mirrorSpaceGuiContent = new GUIContent[]
		{
			new GUIContent("World", "Mirror rays in world space"),
			new GUIContent("Camera", "Mirror rays in camera space")
		};

		public GUIContent[] modeIcons = null;

		/// Keep track of the objects that have been registered for undo, allowing the editor to
		/// restrict undo calls to only the necessary meshes when applying a brush swath.
		private List<GameObject> undoQueue = new List<GameObject>();

		GameObject lastHoveredGameObject = null;
		bool applyingBrush = false;
		Vector2 scroll = Vector2.zero;

		[MenuItem("Tools/Polybrush/About", false, 0)]
		public static void MenuOpenAbout()
		{
			EditorWindow.GetWindow<z_About>(true, "Polybrush About", true).Show();
		}

		[MenuItem("Tools/Polybrush/Documentation", false, 1)]
		public static void MenuOpenDocumentation()
		{
			Application.OpenURL( z_Pref.DocumentationLink );
		}

		[MenuItem("Tools/Polybrush/Polybrush Window %#v", false, 20)]
		public static void MenuInitEditorWindow()
		{
			EditorWindow.GetWindow<z_Editor>(z_Pref.GetBool(z_Pref.floatingEditorWindow)).Show();
		}

		[MenuItem("Tools/Polybrush/Next Brush #Q", true, 100)]
		static bool VerifyCycleBrush()
		{
			return instance != null;
		}

		[MenuItem("Tools/Polybrush/Next Brush #Q", false, 100)]
		static void MenuCycleBrush()
		{
			if(instance != null)
			{
				z_BrushTool tool = (z_BrushTool) instance.tool.Next();
				instance.SetTool( (z_BrushTool) System.Math.Max((int)tool, 1) );
			}
		}

		void SetWindowFloating(bool floating)
		{
			EditorPrefs.SetBool(z_Pref.floatingEditorWindow, floating);
			EditorWindow.GetWindow<z_Editor>().Close();
			MenuInitEditorWindow();
		}

		void OnEnable()
		{
			z_Editor._instance = this;

			this.titleContent = new GUIContent("Polybrush", (Texture2D)Resources.Load(ICON_PATH, typeof(Texture2D)));

			if(modeIcons == null)
			{
				modeIcons = new GUIContent[]
				{
					EditorGUIUtility.IconContent("TerrainInspector.TerrainToolRaise", "|Raise and lower the terrain height."),
					EditorGUIUtility.IconContent("TerrainInspector.TerrainToolSmoothHeight", "|Smooth the terrain height."),
					EditorGUIUtility.IconContent("TerrainInspector.TerrainToolSplat", "|Paint the terrain texture."),
#if PREFAB_MODE_ENABLED
					EditorGUIUtility.IconContent("TerrainInspector.TerrainToolPlants", "|Place plants, stones and other small foilage"),
#endif
					EditorGUIUtility.IconContent("TerrainInspector.TerrainToolPlants", "|Place plants, stones and other small foilage"),
					EditorGUIUtility.IconContent("TerrainInspector.TerrainToolSettings", "|Settings for the terrain")
				};
			}

			if(mirrorGuiContent == null)
			{
				 mirrorGuiContent = new GUIContent[]
				{
					new GUIContent("None", "No Mirroring"),
					new GUIContent(" X ", "Mirror across the X axis, with Y up"),
					new GUIContent(" Y ", "Mirror the brush up/down."),
					new GUIContent(" Z ", "Mirror across the Z axis, with Y up")
				};
			}

			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			SceneView.onSceneGUIDelegate += OnSceneGUI;

			Undo.undoRedoPerformed -= UndoRedoPerformed;
			Undo.undoRedoPerformed += UndoRedoPerformed;

			// force update the preview
			lastHoveredGameObject = null;

			if(brushSettings == null)
				brushSettings = z_EditorUtility.GetDefaultAsset<z_BrushSettings>("Brush Settings/Default.asset");

			RefreshAvailableBrushes();

			SetTool(tool == z_BrushTool.None ? z_BrushTool.RaiseLower : tool);
		}

		void OnDisable()
		{
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			Undo.undoRedoPerformed -= UndoRedoPerformed;

			// don't iterate here!  FinalizeAndReset does that
			z_EditableObject o = hovering.FirstOrDefault(x => x.Equals(lastHoveredGameObject));
			OnBrushExit(o);
		}

		void OnDestroy()
		{
			SetTool(z_BrushTool.None);

			if(z_ReflectionUtil.pbEditor != null)
				z_ReflectionUtil.Invoke(z_ReflectionUtil.pbEditor, "SetEditLevel", BindingFlags.Public | BindingFlags.Instance, 4);

			foreach(z_BrushMode m in modes)
				GameObject.DestroyImmediate(m);
		}

		void OnGUI()
		{
			Event e = Event.current;

			if(e.type == EventType.ContextClick)
				OpenContextMenu();

			GUILayout.Space(8);

			GUILayout.BeginHorizontal(GUILayout.MaxHeight(24));
			GUILayout.FlexibleSpace();

				EditorGUI.BeginChangeCheck();

				int toolbarIndex = (int) tool - 1;

				GUILayout.FlexibleSpace();
					toolbarIndex = GUILayout.Toolbar(toolbarIndex, modeIcons, "Command", null);
				GUILayout.FlexibleSpace();

				if(EditorGUI.EndChangeCheck())
					SetTool( (z_BrushTool)toolbarIndex+1 );

				GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			/// Call current mode GUI
			if(mode != null && tool != z_BrushTool.Settings)
			{
				EditorGUI.BeginChangeCheck();
				currentBrushIndex = EditorGUILayout.Popup("Brush", currentBrushIndex, availableBrushes_str);

				if(EditorGUI.EndChangeCheck())
				{
					if(currentBrushIndex >= availableBrushes.Count)
						AddNewBrush();
					else
						SetBrush(availableBrushes[currentBrushIndex]);
				}

				Rect r = GUILayoutUtility.GetLastRect();

				if(e.type == EventType.MouseDown && r.Contains(e.mousePosition))
					EditorGUIUtility.PingObject(brushSettings);

				GUILayout.BeginHorizontal();
					EditorGUILayout.PrefixLabel("Mirror");
					brushMirror = (z_BrushMirror) z_GUI.CycleButton((int)brushMirror, mirrorGuiContent, "ButtonLeft");
					mirrorSpace = (z_MirrorCoordinateSpace) z_GUI.CycleButton((int)mirrorSpace, mirrorSpaceGuiContent, "ButtonRight");
				GUILayout.EndHorizontal();

				if(!z_Pref.GetBool(z_Pref.lockBrushSettings))
					scroll = EditorGUILayout.BeginScrollView(scroll);

				EditorGUI.BeginChangeCheck();

					brushEditor.OnInspectorGUI();

					if(z_Pref.GetBool(z_Pref.lockBrushSettings))
						scroll = EditorGUILayout.BeginScrollView(scroll);

					mode.DrawGUI(brushSettings);

				if(EditorGUI.EndChangeCheck())
					mode.OnBrushSettingsChanged(brushTarget, brushSettings);

				EditorGUILayout.EndScrollView();
			}
			else
			{
				if(tool == z_BrushTool.Settings)
				{
					z_GlobalSettingsEditor.OnGUI();
				}
				else
				{
					/// ...yo dawg, heard you like FlexibleSpace
					GUILayout.BeginVertical();
						GUILayout.FlexibleSpace();
							GUILayout.BeginHorizontal();
								GUILayout.FlexibleSpace();
									GUILayout.Label("Select an Edit Mode", z_GUI.headerTextStyle);
								GUILayout.FlexibleSpace();
							GUILayout.EndHorizontal();
						GUILayout.FlexibleSpace();
					GUILayout.EndVertical();
				}
			}
		}

		void OpenContextMenu()
		{
			GenericMenu menu = new GenericMenu();

			menu.AddItem (new GUIContent("Open as Floating Window", ""), false, () => { SetWindowFloating(true); } );
			menu.AddItem (new GUIContent("Open as Dockable Window", ""), false, () => { SetWindowFloating(false); } );

			menu.ShowAsContext ();
		}

		void SetTool(z_BrushTool brushTool)
		{
			if(brushTool == tool && mode != null)
				return;

			if(mode != null)
			{
				/// Exiting edit mode
				if(lastHoveredGameObject != null)
				{
					z_EditableObject o = hovering.FirstOrDefault(x => x.Equals(lastHoveredGameObject));
					OnBrushExit(o);
				}

				mode.OnDisable();

				if(z_ReflectionUtil.pbEditor != null && brushTool == z_BrushTool.None)
					z_ReflectionUtil.Invoke(z_ReflectionUtil.pbEditor, "SetEditLevel", BindingFlags.Public | BindingFlags.Instance, 0);
			}
			else
			{
				if(z_ReflectionUtil.pbEditor != null && brushTool != z_BrushTool.None)
					z_ReflectionUtil.Invoke(z_ReflectionUtil.pbEditor, "SetEditLevel", BindingFlags.Public | BindingFlags.Instance, 4);
			}

			lastHoveredGameObject = null;

			System.Type modeType = brushTool.GetModeType();

			if(modeType != null)
			{
				mode = modes.FirstOrDefault(x => x != null && x.GetType() == modeType);

				if(mode == null)
					mode = (z_BrushMode) ScriptableObject.CreateInstance( modeType );
			}

			tool = brushTool;

			if(tool != z_BrushTool.None)
			{
				Tools.current = Tool.None;
				mode.OnEnable();
			}

			Repaint();
		}

		private void RefreshAvailableBrushes()
		{
			availableBrushes = Resources.FindObjectsOfTypeAll<z_BrushSettings>().Where(x => !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(x))).ToList();

			if(availableBrushes.Count < 1)
				availableBrushes.Add(z_EditorUtility.GetDefaultAsset<z_BrushSettings>("Brush Settings/Default.asset"));

			currentBrushIndex = System.Math.Max(availableBrushes.IndexOf(brushSettings), 0);

			availableBrushes_str = availableBrushes.Select(x => x.name).ToArray();

			ArrayUtility.Add<string>(ref availableBrushes_str, string.Empty);
			ArrayUtility.Add<string>(ref availableBrushes_str, "Add Brush...");
		}

		public void SetBrush(z_BrushSettings settings)
		{
			if(settings == null)
				return;

			brushSettings = settings;

			RefreshAvailableBrushes();

			Repaint();
		}

		private void AddNewBrush()
		{
			brushSettings = z_BrushSettingsEditor.AddNew();
			RefreshAvailableBrushes();
		}

		void OnSceneGUI(SceneView sceneView)
		{
			if( tool == z_BrushTool.Settings || mode == null)
				return;

			Event e = Event.current;

			if(Tools.current != Tool.None)
				SetTool(z_BrushTool.None);

			if(brushSettings == null)
				SetBrush(z_EditorUtility.GetDefaultAsset<z_BrushSettings>("Brush Settings/Default.asset"));

			if(z_SceneUtility.SceneViewInUse(e) || tool == z_BrushTool.None)
				return;

			int controlID = GUIUtility.GetControlID(FocusType.Passive);

			if( brushTarget.IsValid() )
				HandleUtility.AddDefaultControl(controlID);

			switch( e.GetTypeForControl(controlID) )
			{
				case EventType.MouseMove:
					/// Handles:
					///		OnBrushEnter
					///		OnBrushExit
					///		OnBrushMove
					UpdateBrush(e.mousePosition);
					break;

				case EventType.MouseDown:
				case EventType.MouseDrag:
					/// Handles:
					///		OnBrushBeginApply
					///		OnBrushApply
					///		OnBrushFinishApply
					UpdateBrush(e.mousePosition);
					ApplyBrush();
					break;

				case EventType.MouseUp:
					if(applyingBrush)
						OnFinishApplyingBrush();
					break;

				case EventType.ScrollWheel:
					ScrollBrushSettings(e);
					break;
			}

			if( brushTarget.IsValid() )
				mode.DrawGizmos(brushTarget, brushSettings);
		}

		void UpdateBrush(Vector2 mousePosition)
		{
			/// Must check HandleUtility.PickGameObject only during MouseMoveEvents or errors will rain.
			GameObject go = HandleUtility.PickGameObject(mousePosition, false);

			bool mouseHoverTargetChanged = false;

			if( go != lastHoveredGameObject )
			{
				if(lastHoveredGameObject != null)
				{
					z_EditableObject o = hovering.FirstOrDefault(x => x.Equals(lastHoveredGameObject));
					OnBrushExit(o);
				}

				if(go != null && !Selection.transforms.Contains(go.transform))
					go = null;

				mouseHoverTargetChanged = true;
				lastHoveredGameObject = go;
			}

			z_EditableObject editable = null;

			if(go != null && Selection.transforms.Contains(go.transform))
			{
				editable = hovering.FirstOrDefault(x => x.Equals(go));

				if(editable == null)
				{
					editable = z_EditableObject.Create(go);

					if(editable != null)
						hovering.Add( editable );
					else
						return;
				}
				else
				{
					if(!editable.VerifyMeshCache())
					{
						OnBrushExit(editable);
						return;
					}
				}
			}

			brushTarget.raycastHits.Clear();

			Ray mouseRay = HandleUtility.GUIPointToWorldRay(mousePosition);

			if(go == null)
			{
				foreach(z_EditableObject obj in hovering)
				{
					if(obj.IsValid() && EditableObjectRaycast(mouseRay, obj) )
					{
						editable = obj;
						lastHoveredGameObject = editable.gameObject;
						break;
					}
				}
			}
			else
			{
				EditableObjectRaycast(mouseRay, editable);
			}

			if(mouseHoverTargetChanged && editable != null)
			{
				OnBrushEnter(editable, brushSettings);

				/// brush is in use, adding a new object to the undo
				if(applyingBrush && !undoQueue.Contains(go))
				{
					undoQueue.Add(go);

					int curGroup = Undo.GetCurrentGroup();

					editable.isDirty = true;

					OnBrushBeginApply(brushTarget, brushSettings);

					Undo.CollapseUndoOperations(curGroup);
				}
			}

			OnBrushMove();

			SceneView.RepaintAll();
			this.Repaint();
		}

		bool EditableObjectRaycast(Ray mouseRay, z_EditableObject editable)
		{
			if(editable == null)
				return false;

			List<Ray> rays = new List<Ray>() { mouseRay };

			if(brushMirror != z_BrushMirror.None)
			{
				Vector3 flipVec = brushMirror.ToVector3();

				if(mirrorSpace == z_MirrorCoordinateSpace.World)
				{
					Vector3 cen = editable.gameObject.GetComponent<Renderer>().bounds.center;
					rays.Add( new Ray(	Vector3.Scale(rays[0].origin - cen, flipVec) + cen,
										Vector3.Scale(rays[0].direction, flipVec)));
				}
				else
				{
					Transform t = SceneView.lastActiveSceneView.camera.transform;
					Vector3 o = t.InverseTransformPoint(rays[0].origin);
					Vector3 d = t.InverseTransformDirection(rays[0].direction);
					rays.Add(new Ray( 	t.TransformPoint(Vector3.Scale(o, flipVec)),
										t.TransformDirection(Vector3.Scale(d, flipVec))));
				}
			}

			bool hitMesh = false;

			foreach(Ray ray in rays)
			{
				z_RaycastHit hit;

				if( z_SceneUtility.WorldRaycast(ray, editable.meshFilter, out hit) )
				{
					brushTarget.raycastHits.Add(hit);
					brushTarget.editableObject = editable;

					z_SceneUtility.GetWeightedVerticesWithBrush(brushTarget, brushSettings);

					hitMesh = true;
				}
			}

			return hitMesh;
		}

		void ApplyBrush()
		{
			if(!brushTarget.valid)
				return;

			if(!applyingBrush)
			{
				undoQueue.Clear();
				applyingBrush = true;
				OnBrushBeginApply(brushTarget, brushSettings);
			}

			mode.OnBrushApply(brushTarget, brushSettings);
		}

		void OnBrushBeginApply(z_BrushTarget brushTarget, z_BrushSettings settings)
		{
			z_SceneUtility.PushGIWorkflowMode();
			mode.RegisterUndo(brushTarget);
			undoQueue.Add(brushTarget.gameObject);
			mode.OnBrushBeginApply(brushTarget, brushSettings);
		}

		void ScrollBrushSettings(Event e)
		{
			float nrm = 1f;

			switch(e.modifiers)
			{
				case EventModifiers.Control:
					nrm = Mathf.Sin(Mathf.Max(.001f, brushSettings.normalizedRadius)) * .03f * (brushSettings.brushRadiusMax - brushSettings.brushRadiusMin);
					brushSettings.radius = brushSettings.radius - (e.delta.y * nrm);
					break;

				case EventModifiers.Shift:
					nrm = Mathf.Sin(Mathf.Max(.001f, brushSettings.falloff)) * .03f;
					brushSettings.falloff = brushSettings.falloff - e.delta.y * nrm;
					break;

				case EventModifiers.Control | EventModifiers.Shift:
					nrm = Mathf.Sin(Mathf.Max(.001f, brushSettings.strength)) * .03f;
					brushSettings.strength = brushSettings.strength - e.delta.y * nrm;
					break;

				default:
					return;
			}

			EditorUtility.SetDirty(brushSettings);

			if(mode != null)
			{
				UpdateBrush(Event.current.mousePosition);
				mode.OnBrushSettingsChanged(brushTarget, brushSettings);
			}

			e.Use();
			Repaint();
			SceneView.RepaintAll();
		}

		void OnBrushEnter(z_EditableObject editableObject, z_BrushSettings settings)
		{
			mode.OnBrushEnter(editableObject, settings);
		}

		void OnBrushMove()
		{
			mode.OnBrushMove( brushTarget, brushSettings );
		}

		void OnBrushExit(z_EditableObject editableObject)
		{
			brushTarget.Clear();

			if(editableObject != null)
			{
				mode.OnBrushExit(editableObject);

				if(!applyingBrush)
					FinalizeAndResetHovering();
			}

		}

		void OnFinishApplyingBrush()
		{
			z_SceneUtility.PopGIWorkflowMode();

			applyingBrush = false;
			mode.OnBrushFinishApply(brushTarget, brushSettings);
			FinalizeAndResetHovering();
		}

		void FinalizeAndResetHovering()
		{
			foreach(z_EditableObject editable in hovering)
			{
				if(!editable.IsValid())
					continue;

				// if mesh hasn't been modified, revert it back
				// to the original mesh so that unnecessary assets
				// aren't allocated.  if it has been modified, let
				// the editableObject apply those changes to the
				// pb_Object if necessary.
				if(!editable.isDirty)
				{
					editable.Revert();
				}
				else
				{
					editable.Apply(true, true);
				}
			}

			hovering.Clear();
			lastHoveredGameObject = null;

			EditorWindow pbEditor = z_ReflectionUtil.pbEditor;

			if(pbEditor != null)
				z_ReflectionUtil.Invoke(	pbEditor,
											pbEditor.GetType(),
											"UpdateSelection",
											new System.Type[] { typeof(bool) },
											BindingFlags.Instance | BindingFlags.Public,
											new object[] { true });
		}

		void UndoRedoPerformed()
		{
			hovering.Clear();
			lastHoveredGameObject = null;

			mode.UndoRedoPerformed(undoQueue);
			undoQueue.Clear();

			SceneView.RepaintAll();
		}
	}
}
