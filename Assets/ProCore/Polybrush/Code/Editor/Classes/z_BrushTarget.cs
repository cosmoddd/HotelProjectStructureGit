using UnityEngine;
using System.Collections.Generic;

namespace Polybrush
{
	/**
	 *	Stores information about the object a brush is currently hovering.
	 */
	public class z_BrushTarget : z_IValid
	{
		/// List of hit locations on this target mesh.
		public List<z_RaycastHit> raycastHits = new List<z_RaycastHit>();

		/// The GameObject the brush is currently hovering.
		[SerializeField] z_EditableObject _editableObject = null;

		public z_EditableObject editableObject
		{
			get { return _editableObject; }
			set
			{
				if(_editableObject != value)
				{
					_editableObject = value;

					if(_editableObject != null)
						weights = z_Util.InitDictionary<int, float>(k => { return k; }, v => { return 0f; }, _editableObject.vertexCount);
					else
						weights.Clear();
				}
			}
		}

		/// A dictionary of the vertices on a mesh with the normalized weight of the brush.
		public Dictionary<int, float> weights = new Dictionary<int, float>();

		/// Convenience getter for editableObject.gameObject
		public GameObject gameObject { get { return editableObject == null ? null : editableObject.gameObject; } }

		/// Convenience getter for editableObject.gameObject.transform
		public Transform transform { get { return editableObject == null ? null : editableObject.gameObject.transform; } }
		
		/// Convenience getter for editableObject.mesh
		public Mesh mesh { get { return editableObject == null ? null : editableObject.mesh; } }
		
		/// Convenience getter for gameObject.transform.localToWorldMatrix
		public Matrix4x4 localToWorldMatrix { get { return editableObject == null ? Matrix4x4.identity : editableObject.gameObject.transform.localToWorldMatrix; } }

		/**
		 *	Explicit constructor.
		 */
		public z_BrushTarget(List<z_RaycastHit> hits, z_EditableObject editableObject = null)
		{
			this.raycastHits = hits;
			this.editableObject = editableObject;

			int vertexCount = editableObject.vertexCount;

			for(int i = 0; i < vertexCount; i++)
				weights.Add(i, 0f);
		}

		public z_BrushTarget()
		{
			this.raycastHits = new List<z_RaycastHit>();
		}

		public void Clear()
		{
			raycastHits.Clear();
			weights.Clear();
			editableObject = null;
		}

		/**
		 *	Check that the mesh caches are valid.  Returns false if they weren't.
		 */
		public bool VerifyMeshCache()
		{
			if( !editableObject.VerifyMeshCache() )
			{
				weights = z_Util.InitDictionary<int, float>(k => { return k; }, v => { return 0f; }, editableObject.vertexCount);
				return false;
			}
			return true;
		}

		public bool valid { get { return editableObject.IsValid(); } }
	}
}
