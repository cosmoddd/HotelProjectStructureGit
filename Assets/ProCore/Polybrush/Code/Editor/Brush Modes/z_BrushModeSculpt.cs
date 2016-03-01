using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Polybrush
{
	/**
	 *	Base class for brush modes that move vertices around.  Implements an overlay preview.
	 */
	public abstract class z_BrushModeSculpt : z_BrushModeMesh
	{
		/// Modifier to apply on top of strength.  Translates to brush applications per second roughly.
		public const float STRENGTH_MODIFIER = .01f;

		public Color[] gradient = new Color[3]
		{
			Color.green,
			Color.yellow,
			Color.black
		};

		/// What direction to push vertices in.
		public z_Direction direction = z_Direction.Up;
		
		public GUIContent gc_direction = new GUIContent("Direction", "How vertices are moved when the brush is applied.  You can explicitly set an axis, or use the vertex normal.");
		public GUIContent gc_ignoreOpenEdges = new GUIContent("Ignore Open Edges", "When on, edges that are not connected on both sides will be ignored by brush strokes.");

		public override string UndoMessage { get { return "Sculpt Vertices"; } }

		protected virtual string ModeSettingsHeader { get { return "Sculpt Settings"; } }

		/// If true vertices on the edge of a mesh will not be affected by brush strokes.  It is up to inheriting
		/// classes to implement this preference (use `nonManifoldIndices` HashSet to check if a vertex index is 
		/// non-manifold).
		protected bool ignoreNonManifoldIndices = true;

		protected HashSet<int> nonManifoldIndices = null;

		public override void OnEnable()
		{
			base.OnEnable();
			direction = (z_Direction) z_Pref.GetEnum(z_Pref.sculptDirection);
			// innerColor = z_Pref.GetColor(z_Pref.handleInnerColor);
			// outerColor = z_Pref.GetColor(z_Pref.handleOuterColor);
		}

		public override void OnBrushEnter(z_EditableObject target, z_BrushSettings settings)
		{
			base.OnBrushEnter(target, settings);
			nonManifoldIndices = z_MeshUtility.GetNonManifoldIndices(target.mesh);
		}

		public override void DrawGUI(z_BrushSettings settings)
		{
			base.DrawGUI(settings);

			GUILayout.Label(ModeSettingsHeader, z_GUI.headerTextStyle);

			ignoreNonManifoldIndices = EditorGUILayout.Toggle(gc_ignoreOpenEdges, ignoreNonManifoldIndices);

			EditorGUI.BeginChangeCheck();
			direction = (z_Direction) EditorGUILayout.EnumPopup(gc_direction, direction);
			if(EditorGUI.EndChangeCheck())
				EditorPrefs.SetInt(z_Pref.sculptDirection, (int) direction);
		}

		protected override void CreateTempComponent(z_EditableObject target, z_BrushSettings settings)
		{
			z_OverlayRenderer ren = target.gameObject.AddComponent<z_OverlayRenderer>();
			ren.SetMesh(target.mesh);
			
			ren.fullColor = z_Pref.GetColor(z_Pref.brushColor);
			ren.gradient = z_Pref.GetGradient(z_Pref.brushGradient);

			tempComponent = ren;
		}

		protected override void UpdateTempComponent(z_BrushTarget target, z_BrushSettings settings)
		{
			if(tempComponent != null && target.weights != null)
			{
				((z_OverlayRenderer)tempComponent).SetWeights(target.weights, settings.strength);
			}
		}
	}
}
