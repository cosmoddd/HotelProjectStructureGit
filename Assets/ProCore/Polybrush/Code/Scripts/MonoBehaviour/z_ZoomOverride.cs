using UnityEngine;
using System.Collections.Generic;

namespace Polybrush
{
	/**
	 *	Overrides the default scene zoom with the current values.
	 */
	public class z_ZoomOverride : MonoBehaviour
	{
		// The current weights applied to this mesh
		protected Dictionary<int, float> weights;

		// Normalized brush strength
		protected float normalizedStrength;

		public virtual void SetWeights(Dictionary<int, float> weights, float normalizedStrength)
		{
			this.weights = weights;
			this.normalizedStrength = normalizedStrength;
		}

		public virtual Dictionary<int, float> GetWeights()
		{
			return weights;
		}

		private MeshFilter _meshFilter;
		private SkinnedMeshRenderer _skinnedMeshRenderer;

		public Mesh mesh
		{
			get
			{
				if(_meshFilter == null)
					_meshFilter = gameObject.GetComponent<MeshFilter>();

				if(_meshFilter != null && _meshFilter.sharedMesh != null)
					return _meshFilter.sharedMesh;
					
				if(_skinnedMeshRenderer == null)
					_skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

				if(_skinnedMeshRenderer != null && _skinnedMeshRenderer.sharedMesh != null)
					return _skinnedMeshRenderer.sharedMesh;
				else
					return null;
			}
		}

		/**
		 *	Let the temp mesh know that vertex positions have changed.
		 */
		public virtual void OnVerticesMoved() {}

		protected virtual void OnEnable()
		{
			this.hideFlags = HideFlags.HideAndDontSave;
			Component[] other = GetComponents<z_ZoomOverride>();
			foreach(Component c in other)
				if(c != this)	
					GameObject.DestroyImmediate(c);
		}
	}
}
