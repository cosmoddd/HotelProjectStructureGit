using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Polybrush
{
	/**
	 *	Vertex painter brush mode.
	 */
	public class z_BrushModePaint : z_BrushModeMesh
	{
		/// how many applications it should take to reach the full strength
		const float STRENGTH_MODIFIER = 1f/8f;

		private static readonly Color32 WHITE = new Color32(255, 255, 255, 255);

		[SerializeField] z_PaintMode paintMode = z_PaintMode.Brush;

		[SerializeField] bool likelySupportsVertexColors = false;

		/// mesh vertex colors
		[SerializeField] Color32[] colors_cache = null, target_colors = null, erase_colors = null, colors = null;
		[SerializeField] Color32 brushColor = Color.green;
		[SerializeField] int vertexCount = 0;
		/// used for fill mode
		Dictionary<int, List<int>> triangleLookup = null;

		public GUIContent[] modeIcons = new GUIContent[]
		{
			new GUIContent( (Texture2D) null, "Brush" ),
			new GUIContent( (Texture2D) null, "Fill" )
		};

		/// The current color palette.
		[SerializeField] z_ColorPalette _colorPalette = null;

		private z_ColorPalette colorPalette
		{
			get
			{
				if(_colorPalette == null)
					colorPalette = z_EditorUtility.GetDefaultAsset<z_ColorPalette>("Color Palettes/Default.asset");
				return _colorPalette;
			}
			set
			{
				_colorPalette = value;
			}
		}

		/// An Editor for the colorPalette.
		[SerializeField] z_ColorPaletteEditor _colorPaletteEditor = null;

		private z_ColorPaletteEditor colorPaletteEditor
		{
			get
			{
				if(_colorPaletteEditor == null || _colorPaletteEditor.target != colorPalette)
					_colorPaletteEditor = (z_ColorPaletteEditor) Editor.CreateEditor(colorPalette);

				return _colorPaletteEditor;
			}
		}

		/// The message that will accompany Undo commands for this brush.  Undo/Redo is handled by z_Editor.
		public override string UndoMessage { get { return "Paint Brush"; } }

		GUIStyle paletteStyle;

		z_ColorPalette[] availablePalettes = null;
		string[] availablePalettes_str = null;
		int currentPaletteIndex = -1;

		public override void OnEnable()
		{
			base.OnEnable();

			modeIcons[0].image = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "paintbrush_pro" : "paintbrush_free");
			modeIcons[1].image = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "paintbucket_pro" : "paintbucket_free");

			paletteStyle = new GUIStyle();
			paletteStyle.padding = new RectOffset(8, 8, 8, 8);

			RefreshAvailablePalettes();
		}

		/// Inspector GUI shown in the Editor window.  Base class shows z_BrushSettings by default
		public override void DrawGUI(z_BrushSettings brushSettings)
		{
			base.DrawGUI(brushSettings);

			GUILayout.Label("Paint Settings", z_GUI.headerTextStyle);

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			paintMode = (z_PaintMode) GUILayout.Toolbar( (int) paintMode, modeIcons, "Command", null);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.Space(4);

			if(!likelySupportsVertexColors)
				EditorGUILayout.HelpBox("It doesn't look like any of the materials on this object support vertex colors!", MessageType.Warning);

			brushColor = EditorGUILayout.ColorField("Brush Color", brushColor);

			if(colorPalette == null)
				RefreshAvailablePalettes();

			EditorGUI.BeginChangeCheck();
			currentPaletteIndex = EditorGUILayout.Popup("Palettes", currentPaletteIndex, availablePalettes_str);
			if(EditorGUI.EndChangeCheck())
			{
				if(currentPaletteIndex >= availablePalettes.Length)
					SetColorPalette( z_ColorPaletteEditor.AddNew() );
				else
					SetColorPalette(availablePalettes[currentPaletteIndex]);
			}

			Rect r = GUILayoutUtility.GetLastRect();
			if(Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
				EditorGUIUtility.PingObject(colorPalette);

			colorPaletteEditor.onSelectIndex = (color) => { SetBrushColor(color, brushSettings.strength); };
			colorPaletteEditor.onSaveAs = SetColorPalette;

			GUILayout.BeginVertical( paletteStyle );
				colorPaletteEditor.OnInspectorGUI();
			GUILayout.EndHorizontal();
		}

		private void SetBrushColor(Color color, float strength)
		{
			brushColor = color;
			RebuildColorTargets(color, strength);
		}

		private void RefreshAvailablePalettes()
		{
			if(colorPalette == null)
				colorPalette = z_EditorUtility.GetDefaultAsset<z_ColorPalette>("Color Palettes/Default.asset");

			availablePalettes = Resources.FindObjectsOfTypeAll<z_ColorPalette>().Where(x => !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(x))).ToArray();
			availablePalettes_str = availablePalettes.Select(x => x.name).ToArray();
			ArrayUtility.Add<string>(ref availablePalettes_str, string.Empty);
			ArrayUtility.Add<string>(ref availablePalettes_str, "Add Palette...");
			currentPaletteIndex = System.Array.IndexOf(availablePalettes, colorPalette);
		}

		private void SetColorPalette(z_ColorPalette palette)
		{
			colorPalette = palette;
			RefreshAvailablePalettes();
		}

		public override void OnBrushSettingsChanged(z_BrushTarget target, z_BrushSettings settings)
		{
			base.OnBrushSettingsChanged(target, settings);
			RebuildColorTargets(brushColor, settings.strength);
		}
		
		private void RebuildColorTargets(Color color, float strength)
		{
			if( colors_cache == null ||
				target_colors == null ||
				colors_cache.Length != target_colors.Length)
				return;

			if(strength > .99f)
			{
				for(int i = 0; i < colors_cache.Length; i++)
				{
					target_colors[i] = color;
					erase_colors[i] = Color.white;
				}
			}
			else
			{
				for(int i = 0; i < colors_cache.Length; i++)
				{
					target_colors[i] = z_Util.Lerp(colors_cache[i], color, strength);
					erase_colors[i] = z_Util.Lerp(colors_cache[i], Color.white, strength);
				}
			}
		}

		/// Called when the mouse begins hovering an editable object.
		public override void OnBrushEnter(z_EditableObject target, z_BrushSettings settings)
		{
			base.OnBrushEnter(target, settings);

			if(target.mesh == null)
				return;

			RebuildCaches(target.mesh, settings);

			MeshRenderer mr = target.gameObject.GetComponent<MeshRenderer>();

			if(mr != null && mr.sharedMaterials != null)
				likelySupportsVertexColors = mr.sharedMaterials.Any(x => x.shader != null && z_ShaderUtil.SupportsVertexColors(x.shader));
			else
				likelySupportsVertexColors = false;
		}

		private void RebuildCaches(Mesh m, z_BrushSettings settings)
		{
			vertexCount = m.vertexCount;

			colors_cache = m.colors32;

			if(colors_cache == null || colors_cache.Length != vertexCount)
				colors_cache = z_Util.Fill<Color32>( x => { return Color.white; }, vertexCount);

			colors = new Color32[vertexCount];
			target_colors = new Color32[vertexCount];
			erase_colors = new Color32[vertexCount];

			triangleLookup = z_MeshUtility.GetAdjacentTriangles(m);

			RebuildColorTargets(brushColor, settings.strength);
		}

		/// Called whenever the brush is moved.  Note that @target may have a null editableObject.
		public override void OnBrushMove(z_BrushTarget target, z_BrushSettings settings)
		{
			base.OnBrushMove(target, settings);

			if(!z_Util.IsValid(target))
				return;

			bool shift = Event.current.shift && Event.current.type != EventType.ScrollWheel;

			switch(paintMode)
			{
				case z_PaintMode.Fill:

					System.Array.Copy(colors_cache, colors, vertexCount);
					int[] indices = target.mesh.triangles;
					int index = 0;

					foreach(z_RaycastHit hit in target.raycastHits)
					{
						if(hit.triangle > -1)
						{
							index = hit.triangle * 3;

							colors[indices[index + 0]] = shift ? WHITE : target_colors[indices[index + 0]];
							colors[indices[index + 1]] = shift ? WHITE : target_colors[indices[index + 1]];
							colors[indices[index + 2]] = shift ? WHITE : target_colors[indices[index + 2]];

							if(triangleLookup.ContainsKey(hit.triangle))
							{
								foreach(int i in triangleLookup[hit.triangle])
								{
									index = i * 3;

									colors[indices[index + 0]] = shift ? WHITE : target_colors[indices[index + 0]];
									colors[indices[index + 1]] = shift ? WHITE : target_colors[indices[index + 1]];
									colors[indices[index + 2]] = shift ? WHITE : target_colors[indices[index + 2]];
								}
							}
						}
					}

					break;

				default:
					foreach(KeyValuePair<int, float> weight in target.weights)
						colors[weight.Key] = z_Util.Lerp(	colors_cache[weight.Key],
															shift ? erase_colors[weight.Key] : target_colors[weight.Key],
															weight.Value);
					break;
			}

			target.mesh.colors32 = colors;
		}

		/// Called when the mouse exits hovering an editable object.
		public override void OnBrushExit(z_EditableObject target)
		{
			base.OnBrushExit(target);

			if(target.mesh != null)
				target.mesh.colors32 = colors_cache;

			likelySupportsVertexColors = true;
		}

		/// Called every time the brush should apply itself to a valid target.  Default is on mouse move.
		public override void OnBrushApply(z_BrushTarget target, z_BrushSettings settings)
		{
			System.Array.Copy(colors, colors_cache, colors.Length);
			target.mesh.colors32 = colors_cache;

			base.OnBrushApply(target, settings);
		}

		/// set mesh colors back to their original state before registering for undo
		public override void RegisterUndo(z_BrushTarget brushTarget)
		{
			brushTarget.mesh.colors32 = colors_cache;
			base.RegisterUndo(brushTarget);
		}

		public override void DrawGizmos(z_BrushTarget target, z_BrushSettings settings)
		{
			if(z_Util.IsValid(target) && paintMode == z_PaintMode.Fill)
			{
				Vector3[] vertices = target.mesh.vertices;
				int[] indices = target.mesh.triangles;

				z_Handles.PushMatrix();
				z_Handles.PushHandleColor();

				Handles.matrix = target.transform.localToWorldMatrix;

				int index = 0;

				foreach(z_RaycastHit hit in target.raycastHits)
				{
					if(hit.triangle > -1)
					{
						Handles.color = target_colors[indices[index]];

						index = hit.triangle * 3;

						Handles.DrawLine(vertices[indices[index+0]] + hit.normal * .1f, vertices[indices[index+1]] + hit.normal * .1f);
						Handles.DrawLine(vertices[indices[index+1]] + hit.normal * .1f, vertices[indices[index+2]] + hit.normal * .1f);
						Handles.DrawLine(vertices[indices[index+2]] + hit.normal * .1f, vertices[indices[index+0]] + hit.normal * .1f);

						if(triangleLookup.ContainsKey(hit.triangle))
						{
							foreach(int i in triangleLookup[hit.triangle])
							{
								Handles.color = target_colors[indices[index]];

								index = i * 3;

								Handles.DrawLine(vertices[indices[index+0]] + hit.normal * .1f, vertices[indices[index+1]] + hit.normal * .1f);
								Handles.DrawLine(vertices[indices[index+1]] + hit.normal * .1f, vertices[indices[index+2]] + hit.normal * .1f);
								Handles.DrawLine(vertices[indices[index+2]] + hit.normal * .1f, vertices[indices[index+0]] + hit.normal * .1f);
							}
						}
					}
				}

				z_Handles.PopHandleColor();
				z_Handles.PopMatrix();
			}
			else
			{
				base.DrawGizmos(target, settings);
			}
		}
	}
}
