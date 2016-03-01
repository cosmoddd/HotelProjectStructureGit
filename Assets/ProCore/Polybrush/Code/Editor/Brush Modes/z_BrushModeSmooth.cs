using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Polybrush
{
	/**
	 *	Brush mode for moving vertices in a direction.
	 */
	public class z_BrushModeSmooth : z_BrushModeSculpt
	{
		const float SMOOTH_STRENGTH_MODIFIER = .1f;

		Vector3[] cached_vertices, cached_normals;
		Dictionary<int, List<int>> neighborLookup = new Dictionary<int, List<int>>();

		bool relax = false;
		GUIContent gc_relax = new GUIContent("Relax", "In addition to smoothing vertices along their normal, positions will also be moved to be more evenly spaced.");
		
		public override string UndoMessage { get { return "Smooth Vertices"; } }

		protected override string ModeSettingsHeader { get { return "Smooth Settings"; } }

		public override void OnEnable()
		{
			base.OnEnable();
			relax = z_Pref.GetBool(z_Pref.smoothRelax);
		}

		public override void DrawGUI(z_BrushSettings settings)
		{
			base.DrawGUI(settings);

			if(direction == z_Direction.Normal)
				relax = EditorGUILayout.Toggle(gc_relax, relax);
		}
	
		public override void OnBrushEnter(z_EditableObject target, z_BrushSettings settings)
		{
			base.OnBrushEnter(target, settings);
			
			neighborLookup = z_MeshUtility.GetAdjacentVertices(target.mesh);
			int vertexCount = target.mesh.vertexCount;
			cached_vertices = new Vector3[vertexCount];
			cached_normals = new Vector3[vertexCount];
			System.Array.Copy(target.mesh.vertices, cached_vertices, vertexCount);
			System.Array.Copy(target.mesh.normals, cached_normals, vertexCount);
		}

		public override void OnBrushApply(z_BrushTarget target, z_BrushSettings settings)
		{
			Vector3[] vertices = target.editableObject.vertices;
			Vector3 v, t, avg, dirVec = direction.ToVector3();
			Plane plane = new Plane(Vector3.up, Vector3.zero);

			foreach(KeyValuePair<int, float> weight in target.weights)
			{
				if(weight.Value < .0001f || (ignoreNonManifoldIndices && nonManifoldIndices.Contains(weight.Key)))
					continue;

				v = vertices[weight.Key];

				if(direction == z_Direction.Normal && relax)
					avg = z_Math.Average(cached_vertices, neighborLookup[weight.Key]);
				else
					avg = z_Math.WeightedAverage(cached_vertices, neighborLookup[weight.Key], target.weights);

				if(direction != z_Direction.Normal || !relax)
				{
					if(direction == z_Direction.Normal)
						dirVec = z_Math.WeightedAverage(cached_normals, neighborLookup[weight.Key], target.weights).normalized;

					plane.SetNormalAndPosition(dirVec, avg);
					avg = v - dirVec * plane.GetDistanceToPoint(v);
				}

				t = Vector3.Lerp(v, avg, weight.Value);

				vertices[weight.Key] = v + (t-v) * settings.strength * SMOOTH_STRENGTH_MODIFIER;
			}

			target.editableObject.mesh.vertices = vertices;

			if(tempComponent != null)
				tempComponent.OnVerticesMoved();

			base.OnBrushApply(target, settings);
		}
	}
}
