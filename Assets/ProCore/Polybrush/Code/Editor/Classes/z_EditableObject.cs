using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Polybrush
{
	/**
	 *	Stores a cache of the unmodified mesh and meshrenderer
	 *	so that the z_Editor can work non-destructively.  Also
	 * 	handles ProBuilder compatibility so that brush modes don't
	 * 	have to deal with it.
	 */
	[Serializable]
	public class z_EditableObject : IEquatable<z_EditableObject>, z_IValid
	{
		const string INSTANCE_MESH_GUID = null;

		private static HashSet<string> UnityPrimitiveMeshNames = new HashSet<string>()
		{
			"Sphere",
			"Capsule",
			"Cylinder",
			"Cube",
			"Plane",
			"Quad"
		};

		public GameObject gameObject = null;
		
		/// An instance mesh guaranteed to have at least vertex positions and normals.
		public Mesh mesh = null; 

		/// The original mesh.  Can be the same as mesh.
		public Mesh originalMesh = null;

		/// Where this mesh originated.
		public z_ModelSource source { get; private set; }

		/// If mesh was an asset or model, save the original GUID
		public string sourceGUID { get; private set; }

		/// Marks this object as having been modified.
		public bool modified = false;

		/// The vertices of the original mesh before any modifications. 
		public Vector3[] vertices { get; private set; }

		/// Vertex count
		public int vertexCount { get { return mesh != null ? mesh.vertexCount : 0; } }

		/// The normals of the original mesh before any modifications. 
		public Vector3[] normals { get; private set; }

		/// Convenience getter for gameObject.GetComponent<MeshFilter>().
		public MeshFilter meshFilter { get; private set; }

		/// Convenience getter for gameObject.transform
		public Transform transform { get { return gameObject.transform; } }

		/// Convenience getter for gameObject.renderer
		public Renderer renderer { get { return gameObject.GetComponent<MeshRenderer>(); } }

		/// If this object's mesh has been edited, isDirty will be flagged meaning that the mesh should not be 
		/// cleaned up when finished editing.
		public bool isDirty = false;

		/**
		 *	Shorthand for checking if object and mesh are non-null.
		 */
		public bool valid
		{
			get
			{
				return gameObject != null && mesh != null;
			}
		}

		/**
		 *	Public constructor for editable objects.  Guarantees that a mesh
		 *	is editable and takes care of managing the asset.
		 */
		public static z_EditableObject Create(GameObject go)
		{
			if(go == null)
				return null;

			MeshFilter mf = go.GetComponent<MeshFilter>();
			SkinnedMeshRenderer sf = go.GetComponent<SkinnedMeshRenderer>();

			if((mf == null || mf.sharedMesh == null) && (sf == null || sf.sharedMesh == null))
				return null;

			return new z_EditableObject(go);
		}

		/**
		 *	Internal constructor. 
		 *	\sa Create
		 */
		private z_EditableObject(GameObject go)
		{
			this.gameObject = go;

			meshFilter = this.gameObject.GetComponent<MeshFilter>();
			SkinnedMeshRenderer skinFilter = this.gameObject.GetComponent<SkinnedMeshRenderer>();

			this.originalMesh = meshFilter.sharedMesh;

			if(originalMesh == null && skinFilter != null)
				originalMesh = skinFilter.sharedMesh;

			string guid = INSTANCE_MESH_GUID;
			this.source = z_EditorUtility.GetMeshGUID(originalMesh, ref guid);
			this.sourceGUID = guid;

			// if editing a non-scene instance mesh, make it an instance
			if(source != z_ModelSource.Scene || UnityPrimitiveMeshNames.Contains(originalMesh.name))
				this.mesh = z_MeshUtility.DeepCopy(meshFilter.sharedMesh);
			else
				this.mesh = originalMesh;

			// if it's a probuilder object rebuild the mesh without optimization
			if( z_ReflectionUtil.IsProBuilderObject(go) )
			{
				object pb = go.GetComponent("pb_Object");

				if(pb != null)
				{
					z_ReflectionUtil.Invoke(pb, "ToMesh");
					z_ReflectionUtil.Invoke(pb, "Refresh");
				}
			}

			UpdateMeshElementCache();

			gameObject.SetMesh(this.mesh);
		}

		/**
		 *	Applies mesh changes back to the pb_Object (if necessary).  Optionally does a 
		 *	mesh rebuild.
		 */
		public void Apply(bool rebuildMesh, bool optimize = false)
		{
			// if it's a probuilder object rebuild the mesh without optimization
			if( z_ReflectionUtil.IsProBuilderObject(gameObject) )
			{
				object pb = gameObject.GetComponent("pb_Object");

				if(pb != null)
				{
					z_ReflectionUtil.Invoke(pb, "SetVertices", BindingFlags.Public | BindingFlags.Instance, mesh.vertices);
					z_ReflectionUtil.Invoke(pb, "SetColors", BindingFlags.Public | BindingFlags.Instance, mesh.colors);

					if(rebuildMesh)
					{
						z_ReflectionUtil.Invoke(pb, "ToMesh");
						z_ReflectionUtil.Invoke(pb, "Refresh");

						if(optimize)
							z_ReflectionUtil.Invoke(null, z_Pref.PB_EDITOR_MESH_UTILITY_TYPE, "Optimize", BindingFlags.Public | BindingFlags.Static, z_Pref.PB_EDITOR_ASSEMBLY, pb);
					}
				}
			}
			else
			{
				if( z_Pref.GetBool(z_Pref.rebuildNormals) )
					z_MeshUtility.RecalculateNormals(mesh);

				mesh.RecalculateBounds();
			}
		}

		/**
		 *	Set the MeshFilter or SkinnedMeshRenderer back to originalMesh.
		 */
		public void Revert()
		{
			if(originalMesh == null || (source == z_ModelSource.Scene && !UnityPrimitiveMeshNames.Contains(originalMesh.name)))
				return;

			if(mesh != null)
				GameObject.DestroyImmediate(mesh);

			gameObject.SetMesh(this.originalMesh);
		}

		public bool Equals(z_EditableObject rhs)
		{
			return rhs.GetHashCode() == this.GetHashCode();
		}

		public override bool Equals(object rhs)
		{
			if(rhs == null)
				return this.gameObject == null ? true : false;
			else if(this.gameObject == null)	
				return false;

			if(rhs is z_EditableObject)
				return rhs.Equals(this);
			else if(rhs is GameObject)
				return ((GameObject)rhs).GetHashCode() == gameObject.GetHashCode();

			return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/**
		 *	Check that the stored mesh elements match the actual mesh.  Returns false if a rebuild was required.
		 */
		public bool VerifyMeshCache()
		{
			if(mesh == null || mesh.vertexCount != vertices.Length)
			{
				UpdateMeshElementCache();
				return false;
			}
			return true;
		}

		/**
		 *	Sets the internal caches of vertex and normal points.  Also generates normals for mesh if none are present.
		 */
		private void UpdateMeshElementCache()
		{
			Mesh m = this.mesh;

			if(m == null)
				return;

			if(m.normals == null || m.normals.Length != m.vertices.Length)
				m.RecalculateNormals();

			vertices = m.vertices;
			normals = m.normals;
		}
	}	
}
