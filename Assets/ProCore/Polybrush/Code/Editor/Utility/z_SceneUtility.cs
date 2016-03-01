using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Polybrush
{
	public static class z_SceneUtility
	{
		public static Ray InverseTransformRay(this Transform transform, Ray InWorldRay)
		{
			Vector3 o = InWorldRay.origin;
			o -= transform.position;
			o = transform.worldToLocalMatrix * o;
			Vector3 d = transform.worldToLocalMatrix.MultiplyVector(InWorldRay.direction);
			return new Ray(o, d);
		}

		/**
		 * Find a triangle intersected by InRay on InMesh.  InRay is in world space.
		 * Returns the index in mesh.faces of the hit face, or -1.  Optionally can ignore
		 * backfaces.
		 */
		public static bool MeshRaycast(Ray InRay, MeshFilter meshFilter, out z_RaycastHit hit)
		{
			return MeshRaycast(InRay, meshFilter, out hit, Mathf.Infinity, Culling.Front);
		}

		/**
		 * Find a triangle intersected by InRay on InMesh.  InRay is in world space.
		 * Returns the index in mesh.faces of the hit face, or -1.  Optionally can ignore
		 * backfaces.
		 */
		public static bool WorldRaycast(Ray InWorldRay, MeshFilter meshFilter, out z_RaycastHit hit)
		{
			return WorldRaycast(InWorldRay, meshFilter, out hit, Mathf.Infinity, Culling.Front);
		}

		/**
		 * Find the nearest triangle intersected by InWorldRay on this mesh.  InWorldRay is in world space.
		 * @hit contains information about the hit point.  @distance limits how far from @InWorldRay.origin the hit
		 * point may be.  @cullingMode determines what face orientations are tested (Culling.Front only tests front 
		 * faces, Culling.Back only tests back faces, and Culling.FrontBack tests both).
		 * Ray origin and position values are in local space.
		 */
		public static bool WorldRaycast(Ray InWorldRay, MeshFilter meshFilter, out z_RaycastHit hit, float distance, Culling cullingMode)
		{
			Ray ray = meshFilter.transform.InverseTransformRay(InWorldRay);
			return MeshRaycast(ray, meshFilter, out hit, distance, cullingMode);
		}

		/**
		 *	Cast a ray (in model space) against a mesh.
		 */
		public static bool MeshRaycast(Ray InRay, MeshFilter meshFilter, out z_RaycastHit hit, float distance, Culling cullingMode)
		{
			Mesh mesh = meshFilter.sharedMesh;

			float dist = 0f;
			Vector3 point = Vector3.zero;

			float OutHitPoint = Mathf.Infinity;
			float dot; 		// vars used in loop
			Vector3 nrm;	// vars used in loop
			int OutHitFace = -1;
			Vector3 OutNrm = Vector3.zero;

			/**
			 * Iterate faces, testing for nearest hit to ray origin.  Optionally ignores backfaces.
			 */

			Vector3[] vertices = mesh.vertices;
			int[] triangles = mesh.triangles;

			for(int CurTri = 0; CurTri < triangles.Length; CurTri += 3)
			{
				Vector3 a = vertices[triangles[CurTri+0]];
				Vector3 b = vertices[triangles[CurTri+1]];
				Vector3 c = vertices[triangles[CurTri+2]];

				nrm = Vector3.Cross(b-a, c-a);
				dot = Vector3.Dot(InRay.direction, nrm);

				bool ignore = false;

				switch(cullingMode)
				{
					case Culling.Front:
						if(dot > 0f) ignore = true;
						break;

					case Culling.Back:
						if(dot < 0f) ignore = true;
						break;
				}

				if(!ignore && z_Math.RayIntersectsTriangle(InRay, a, b, c, out dist, out point))
				{
					if(dist > OutHitPoint || dist > distance)
						continue;

					OutNrm = nrm;
					OutHitFace = CurTri / 3;
					OutHitPoint = dist;

					continue;
				}
			}

			hit = new z_RaycastHit( OutHitPoint,
									InRay.GetPoint(OutHitPoint),
									OutNrm,
									OutHitFace);

			return OutHitFace > -1;
		}

		/**
		 *	Returns true if the event is one that should consume the mouse or keyboard.
		 */
		public static bool SceneViewInUse(Event e)
		{
			return 	e.alt
					|| Tools.current == Tool.View  
					|| GUIUtility.hotControl > 0  
					|| (e.isMouse ? e.button > 1 : false)
					|| Tools.viewTool == ViewTool.FPS 
					|| Tools.viewTool == ViewTool.Orbit;
		}

		/**
		 *	Returns a dictionary of the indices of all affected vertices and the weight with 
		 *	which modifications should be applied.
		 */
		public static void GetWeightedVerticesWithBrush(z_BrushTarget target, z_BrushSettings settings)
		{
			if( target.editableObject == null)
				return;

			Dictionary<int, float> weights = target.weights;

			if(target.raycastHits.Count < 1)
			{
				for(int i = 0; i < target.mesh.vertexCount; i++)
					weights[i] = 0f;
				return;
			}

			bool uniformScale = z_Math.VectorIsUniform(target.transform.lossyScale);
			float scale = uniformScale ? 1f / target.transform.lossyScale.x : 1f;

			int vertexCount = target.mesh.vertexCount;
			Transform transform = target.transform;
			Vector3[] vertices = target.editableObject.vertices;

			if(!uniformScale)
			{
				Vector3[] world = new Vector3[vertexCount];
				for(int i = 0; i < vertexCount; i++)	
					world[i] = transform.TransformPoint(vertices[i]);
				vertices = world;
			}

			float radius = settings.radius * scale, falloff_mag = Mathf.Max((radius - radius * settings.falloff), 0.00001f);

			Vector3 hitPosition = Vector3.zero;
			z_RaycastHit hit = target.raycastHits[0];

			hitPosition = uniformScale ? hit.position : transform.TransformPoint(hit.position);

			// apply first brush hit, then add values on subsequent hits
			for(int i = 0; i < vertexCount; i++)
			{
				float dist = Vector3.Distance(hitPosition, vertices[i]);
				weights[i] = Mathf.Clamp(settings.falloffCurve.Evaluate(1f - Mathf.Clamp((radius - dist) / falloff_mag, 0f, 1f)), 0f, 1f);
			}

			for(int n = 1; n < target.raycastHits.Count; n++)
			{
				hit = target.raycastHits[n];

				hitPosition = uniformScale ? hit.position : transform.TransformPoint(hit.position);

				for(int i = 0; i < vertexCount; i++)
				{
					float dist = Vector3.Distance(hitPosition, vertices[i]);
					weights[i] += Mathf.Clamp(settings.falloffCurve.Evaluate(1f - Mathf.Clamp((radius - dist) / falloff_mag, 0f, 1f)), 0f, 1f);
				}
			}
		}

		/**
		 * Store the previous GIWorkflowMode and set the current value to OnDemand (or leave it Legacy).
		 */
		internal static void PushGIWorkflowMode()
		{
#if UNITY_5
			EditorPrefs.SetInt("z_GIWorkflowMode", (int)Lightmapping.giWorkflowMode);

			if(Lightmapping.giWorkflowMode != Lightmapping.GIWorkflowMode.Legacy)
				Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
#endif
		}

		/**
		 * Return GIWorkflowMode to it's prior state.
		 */
		internal static void PopGIWorkflowMode()
		{
#if UNITY_5
			// if no key found (?), don't do anything.
			if(!EditorPrefs.HasKey("z_GIWorkflowMode"))
				return;

			 Lightmapping.giWorkflowMode = (Lightmapping.GIWorkflowMode)EditorPrefs.GetInt("z_GIWorkflowMode");
#endif
		}
	}
}
