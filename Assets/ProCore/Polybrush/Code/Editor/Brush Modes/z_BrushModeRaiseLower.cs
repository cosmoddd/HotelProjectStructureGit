using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Polybrush
{
	/**
	 *	Brush mode for moving vertices in a direction.
	 */
	public class z_BrushModeRaiseLower : z_BrushModeSculpt
	{		
		const string BRUSH_EFFECT_PREF = "z_pref_raise_lower_brush_effect";

		Dictionary<int, Vector3> normalLookup = null;

		[SerializeField] float brushStrength = 1f;

		public override string UndoMessage { get { return "Raise/Lower Vertices"; } }

		protected override string ModeSettingsHeader { get { return "Raise / Lower Settings"; } }

		private GUIContent gc_BrushEffect = new GUIContent("Brush Effect", "Defines the baseline distance that vertices will be moved when a brush is applied at full strength.");

		public override void OnEnable()
		{
			base.OnEnable();
			brushStrength = EditorPrefs.HasKey(BRUSH_EFFECT_PREF) ? EditorPrefs.GetFloat(BRUSH_EFFECT_PREF) : 1f;
		}

		public override void DrawGUI(z_BrushSettings settings)
		{
			base.DrawGUI(settings);

			EditorGUI.BeginChangeCheck();
			brushStrength = EditorGUILayout.FloatField(gc_BrushEffect, brushStrength);
			if(EditorGUI.EndChangeCheck())
				EditorPrefs.SetFloat(BRUSH_EFFECT_PREF, brushStrength);
		}

		public override void OnBrushEnter(z_EditableObject target, z_BrushSettings settings)
		{
			base.OnBrushEnter(target, settings);
			normalLookup = z_MeshUtility.GetSmoothNormalLookup(target.mesh);
		}

		public override void OnBrushApply(z_BrushTarget target, z_BrushSettings settings)
		{
			Vector3[] vertices = target.editableObject.vertices;
			Vector3 v, n = direction.ToVector3();

			float scale = 1f / ( Vector3.Scale(target.transform.lossyScale, n).magnitude );
			float sign = Event.current.shift ? -1f : 1f;

			float maxMoveDistance = settings.strength * STRENGTH_MODIFIER * sign * brushStrength;

			foreach(KeyValuePair<int, float> weight in target.weights)
			{
				if(ignoreNonManifoldIndices && nonManifoldIndices.Contains(weight.Key))
					continue;

				v = vertices[weight.Key];

				if(direction == z_Direction.Normal)
				{
					n = normalLookup[weight.Key];
					scale = 1f / ( Vector3.Scale(target.transform.lossyScale, n).magnitude );
				}

				vertices[weight.Key] = v + n * weight.Value * maxMoveDistance * scale;
			}

			target.editableObject.mesh.vertices = vertices;

			if(tempComponent != null)
				tempComponent.OnVerticesMoved();

			base.OnBrushApply(target, settings);
		}
	}
}
