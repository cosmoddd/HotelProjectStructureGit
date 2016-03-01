using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Polybrush
{
	/**
	 *	Vertex texture painter brush mode.
	 * 	Similar to z_BrushModePaint, except it packs blend information into both the Colors32 and UV3/4 channels.
	 */
	public class z_BrushModeTexture : z_BrushModeMesh
	{
		// how many applications it should take to reach the full strength
		const float STRENGTH_MODIFIER = 1f/8f;

		[SerializeField] z_PaintMode paintMode = z_PaintMode.Brush;

		[SerializeField] bool likelySupportsTextureBlending = true;

		[SerializeField] z_SplatSet colors_cache = null,
									target_colors = null,
									erase_colors = null,
									colors = null;

		[SerializeField] z_SplatWeight brushColor = new z_SplatWeight(8);

		public z_MeshChannel[] meshAttributes = new z_MeshChannel[]
		{
			z_MeshChannel.COLOR,
			z_MeshChannel.UV3,
			z_MeshChannel.UV4
		};

		[SerializeField] Texture2D[] textures = null;

		[SerializeField] int vertexCount = 0;
		Dictionary<int, List<int>> triangleLookup = null;

		public GUIContent[] modeIcons = new GUIContent[]
		{
			new GUIContent( (Texture2D) null, "Brush" ),
			new GUIContent( (Texture2D) null, "Fill" )
		};

		/// The message that will accompany Undo commands for this brush.  Undo/Redo is handled by z_Editor.
		public override string UndoMessage { get { return "Paint Brush"; } }

		public override void OnEnable()
		{
			base.OnEnable();

			modeIcons[0].image = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "paintbrush_pro" : "paintbrush_free");
			modeIcons[1].image = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "paintbucket_pro" : "paintbucket_free");

			foreach(GameObject go in Selection.gameObjects)
			{
				MeshRenderer mr = go.GetComponent<MeshRenderer>();

				Material mat = mr == null ? null : mr.sharedMaterials.FirstOrDefault(x => x != null && z_ShaderUtil.SupportsTextureBlending(x.shader));

				likelySupportsTextureBlending = mat != null;

				if(likelySupportsTextureBlending)
				{
					textures = z_ShaderUtil.GetBlendTextures(mat);

					if( textures != null )
						brushColor.Resize(textures.Length);

					z_MeshChannel[] c = z_ShaderUtil.GetUsedMeshAttributes(mat);
					if(c != null) meshAttributes = c;

					break;
				}
			}
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

			// GUILayout.Label( brushColor.ToString() );
			// GUILayout.Label( meshAttributes.ToString("\n"));
			// GUILayout.Space(4);
			
			if(!likelySupportsTextureBlending)
				EditorGUILayout.HelpBox("It doesn't look like any of the materials on this object support texture blending!\n\nSee the readme for information on creating custom texture blend shaders.", MessageType.Warning);
			else
				brushColor = z_SplatWeightEditor.OnInspectorGUI(brushColor, textures);

		}

		public override void OnBrushSettingsChanged(z_BrushTarget target, z_BrushSettings settings)
		{
			base.OnBrushSettingsChanged(target, settings);
			RebuildColorTargets(brushColor, settings.strength);
		}

		private void RebuildColorTargets(z_SplatWeight blend, float strength)
		{
			if( colors_cache == null ||
				target_colors == null ||
				colors_cache.Length != target_colors.Length)
				return;

			for(int i = 0; i < colors_cache.Length; i++)
			{
				target_colors[i] = z_SplatWeight.Lerp(colors_cache[i], blend, strength);
				erase_colors[i] = z_SplatWeight.Lerp(colors_cache[i], z_SplatWeight.Channel0, strength);
			}
		}

		/// Called when the mouse begins hovering an editable object.
		public override void OnBrushEnter(z_EditableObject target, z_BrushSettings settings)
		{
			base.OnBrushEnter(target, settings);

			if(target.mesh == null)
				return;

			MeshRenderer mr = target.gameObject.GetComponent<MeshRenderer>();

			Material mat = mr == null ? null : mr.sharedMaterials.FirstOrDefault(x => x != null && z_ShaderUtil.SupportsTextureBlending(x.shader));

			likelySupportsTextureBlending = mat != null;

			if(likelySupportsTextureBlending)
			{
				textures = z_ShaderUtil.GetBlendTextures(mat);

				z_MeshChannel[] c = z_ShaderUtil.GetUsedMeshAttributes(mat);
				if(c != null) meshAttributes = c;
			}

			if( textures != null && brushColor.Length != textures.Length )
				brushColor.Resize(textures.Length);

			RebuildCaches(target.mesh, settings);
		}

		private void RebuildCaches(Mesh m, z_BrushSettings settings)
		{
			vertexCount = m.vertexCount;

			colors_cache = new z_SplatSet(m, meshAttributes);

			colors = new z_SplatSet(vertexCount, meshAttributes);
			target_colors = new z_SplatSet(vertexCount, meshAttributes);
			erase_colors = new z_SplatSet(vertexCount, meshAttributes);

			triangleLookup = z_MeshUtility.GetAdjacentTriangles(m);

			for(int i = 0; i < colors_cache.Length; i++)
			{
				target_colors[i] = z_SplatWeight.Lerp(colors_cache[i], brushColor, settings.strength);
				erase_colors[i] = z_SplatWeight.Lerp(colors_cache[i], z_SplatWeight.Channel0, settings.strength);
			}
		}

		/// Called whenever the brush is moved.  Note that @target may have a null editableObject.
		public override void OnBrushMove(z_BrushTarget target, z_BrushSettings settings)
		{
			base.OnBrushMove(target, settings);

			if(!z_Util.IsValid(target) || !likelySupportsTextureBlending)
				return;

			bool shift = Event.current.shift;

			switch(paintMode)
			{
				case z_PaintMode.Fill:
				
					colors_cache.CopyTo(colors);

					int[] indices = target.mesh.triangles;
					int index = 0;

					foreach(z_RaycastHit hit in target.raycastHits)
					{
						if(hit.triangle > -1)
						{
							index = hit.triangle * 3;

							colors[indices[index + 0]] = shift ? z_SplatWeight.Channel0 : target_colors[indices[index + 0]];
							colors[indices[index + 1]] = shift ? z_SplatWeight.Channel0 : target_colors[indices[index + 1]];
							colors[indices[index + 2]] = shift ? z_SplatWeight.Channel0 : target_colors[indices[index + 2]];

							if(triangleLookup.ContainsKey(hit.triangle))
							{
								foreach(int i in triangleLookup[hit.triangle])
								{
									index = i * 3;

									colors[indices[index + 0]] = shift ? z_SplatWeight.Channel0 : target_colors[indices[index + 0]];
									colors[indices[index + 1]] = shift ? z_SplatWeight.Channel0 : target_colors[indices[index + 1]];
									colors[indices[index + 2]] = shift ? z_SplatWeight.Channel0 : target_colors[indices[index + 2]];
								}
							}
						}
					}

					break;

				default:
					foreach(KeyValuePair<int, float> weight in target.weights)
						colors[weight.Key] = z_SplatWeight.Lerp( colors_cache[weight.Key],
											shift ? erase_colors[weight.Key] : target_colors[weight.Key],
											weight.Value);
					break;
			}

			colors.Apply(target.mesh);
		}

		/// Called when the mouse exits hovering an editable object.
		public override void OnBrushExit(z_EditableObject target)
		{
			base.OnBrushExit(target);

			colors_cache.Apply(target.mesh);
			likelySupportsTextureBlending = true;
		}

		/// Called every time the brush should apply itself to a valid target.  Default is on mouse move.
		public override void OnBrushApply(z_BrushTarget target, z_BrushSettings settings)
		{
			if(!likelySupportsTextureBlending)
				return;

			colors.CopyTo(colors_cache);
			colors_cache.Apply(target.mesh);

			base.OnBrushApply(target, settings);
		}

		/// set mesh colors back to their original state before registering for undo
		public override void RegisterUndo(z_BrushTarget brushTarget)
		{
			colors_cache.Apply(brushTarget.mesh);
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
						Handles.color = target_colors[indices[index]].GetColor32(0);

						index = hit.triangle * 3;

						Handles.DrawLine(vertices[indices[index+0]] + hit.normal * .1f, vertices[indices[index+1]] + hit.normal * .1f);
						Handles.DrawLine(vertices[indices[index+1]] + hit.normal * .1f, vertices[indices[index+2]] + hit.normal * .1f);
						Handles.DrawLine(vertices[indices[index+2]] + hit.normal * .1f, vertices[indices[index+0]] + hit.normal * .1f);

						if(triangleLookup.ContainsKey(hit.triangle))
						{
							foreach(int i in triangleLookup[hit.triangle])
							{
								Handles.color = target_colors[indices[index]].GetColor32(0);

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
