#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;

namespace Polybrush
{
	/**
	 *	An editor-only script that renders a mesh and material list in the scene view only.
	 */
	[ExecuteInEditMode]
	public class z_OverlayRenderer : z_ZoomOverride
	{
		// HideFlags.DontSaveInEditor isn't exposed for whatever reason, so do the bit math on ints 
		// and just cast to HideFlags.
		// HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.NotEditable
		HideFlags SceneCameraHideFlags = (HideFlags) (1 | 4 | 8);

		[SerializeField] private Material _overlayMaterial;
		[SerializeField] private Material _billboardMaterial;

		public Color fullColor = Color.green;
		public Gradient gradient = new Gradient();

		List<List<int>> common = null;
		Color[] w_colors;
		Color[] v_colors;

		private Material OverlayMaterial
		{
			get
			{
				if(_overlayMaterial == null)
				{
					_overlayMaterial = new Material(Shader.Find("Hidden/Polybrush/Overlay"));
					_overlayMaterial.hideFlags = HideFlags.HideAndDontSave;
				}
				return _overlayMaterial;
			}
		}

		private Material VertexBillboardMaterial
		{
			get
			{
				if(_billboardMaterial == null)
				{
					_billboardMaterial = new Material(Shader.Find("Hidden/Polybrush/z_VertexBillboard"));
					_billboardMaterial.hideFlags = HideFlags.HideAndDontSave;
				}
				return _billboardMaterial;
			}
		}

		private void OnDestroy()
		{
			if(wireframeMesh != null) GameObject.DestroyImmediate(wireframeMesh);
			if(vertexMesh != null) GameObject.DestroyImmediate(vertexMesh);
			if(_overlayMaterial != null) GameObject.DestroyImmediate(_overlayMaterial);
			if(_billboardMaterial != null) GameObject.DestroyImmediate(_billboardMaterial);
		}

		private Mesh wireframeMesh, vertexMesh;

		public void SetMesh(Mesh m)
		{
			common = z_MeshUtility.GetCommonVertices(m);

			wireframeMesh			= z_MeshUtility.CreateOverlayMesh(m);
			vertexMesh 				= z_MeshUtility.CreateVertexBillboardMesh(m, common);

			wireframeMesh.hideFlags = HideFlags.HideAndDontSave;
			vertexMesh.hideFlags 	= HideFlags.HideAndDontSave;

			v_colors = new Color[vertexMesh.vertexCount];
			w_colors = new Color[wireframeMesh.vertexCount];
		}

		public override void OnVerticesMoved()
		{
			Vector3[] v = mesh.vertices;

			if( wireframeMesh != null )
				wireframeMesh.vertices = v;

			if( vertexMesh != null )
			{
				Vector3[] v2 = new Vector3[ System.Math.Min(ushort.MaxValue / 4, common.Count) * 4 ];

				for(int i = 0; i < common.Count; i++)
				{
					v2[i * 4 + 0] = v[common[i][0]];
					v2[i * 4 + 1] = v[common[i][0]];
					v2[i * 4 + 2] = v[common[i][0]];
					v2[i * 4 + 3] = v[common[i][0]];
				}

				vertexMesh.vertices = v2;
			}
		}

		/**
		 *	Set the vertex colors to match the brush weights.
		 */
		public override void SetWeights(Dictionary<int, float> weights, float normalizedStrength)
		{
			this.weights = weights;

			const float MIN_ALPHA = .5f;
			const float MAX_ALPHA = 1f;
			const float ALPHA_RANGE = MAX_ALPHA - MIN_ALPHA;

			foreach(KeyValuePair<int, float> weight in weights)
			{
				int ind = weight.Key;

				if(weight.Value < 0.0001f)
				{
					w_colors[ind].a = 0f;
					continue;
				}

				float strength = 1f - weight.Value;

				if(strength < .001f)
				{
					w_colors[ind] = fullColor;
				}
				else
				{
					w_colors[ind] = gradient.Evaluate(strength);
					w_colors[ind].a *= MIN_ALPHA + (ALPHA_RANGE * (1f-strength));
				}
			}

			wireframeMesh.colors = w_colors;

			for(int i = 0; i < vertexMesh.vertexCount; i+=4)
			{
				int ind = i / 4;
				v_colors[i+0] = w_colors[common[ind][0]];
				v_colors[i+1] = w_colors[common[ind][0]];
				v_colors[i+2] = w_colors[common[ind][0]];
				v_colors[i+3] = w_colors[common[ind][0]];
			}

			vertexMesh.colors = v_colors;
		}

		void OnRenderObject()
		{
			// instead of relying on 'SceneCamera' string comparison, check if the hideflags match.
			// this could probably even just check for one bit match, since chances are that any 
			// game view camera isn't going to have hideflags set.
			if((Camera.current.gameObject.hideFlags & SceneCameraHideFlags) != SceneCameraHideFlags || Camera.current.name != "SceneCamera" )
				return;

			if(wireframeMesh != null)
			{
				OverlayMaterial.SetFloat("_Alpha", .3f);
				OverlayMaterial.SetPass(0);
				Graphics.DrawMeshNow(wireframeMesh, transform.localToWorldMatrix);
			}

			if(vertexMesh != null)
			{
				VertexBillboardMaterial.SetPass(0);
				Graphics.DrawMeshNow(vertexMesh, transform.localToWorldMatrix);
			}
		}
	}
}

#endif
