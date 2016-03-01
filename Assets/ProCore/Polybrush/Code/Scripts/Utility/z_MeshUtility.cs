using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Polybrush
{
	/**
	 *	Static helper functions for working with meshes.
	 */
	public static class z_MeshUtility
	{
		/**
		 * Duplicate @src and return the copy.
		 */
		public static Mesh DeepCopy(Mesh src)
		{
			Mesh dest = new Mesh();
			Copy(dest, src);
			return dest;
		}

		/**
		 *	Copy @src mesh values to @dest
		 */
		public static void Copy(Mesh dest, Mesh src)
		{
			dest.Clear();
			dest.vertices = src.vertices;

			List<Vector4> uvs = new List<Vector4>();

			src.GetUVs(0, uvs); dest.SetUVs(0, uvs);
			src.GetUVs(1, uvs); dest.SetUVs(1, uvs);
			src.GetUVs(2, uvs); dest.SetUVs(2, uvs);
			src.GetUVs(3, uvs); dest.SetUVs(3, uvs);

			dest.normals = src.normals;
			dest.tangents = src.tangents;
			dest.boneWeights = src.boneWeights;
			dest.colors = src.colors;
			dest.colors32 = src.colors32;
			dest.bindposes = src.bindposes;

			dest.subMeshCount = src.subMeshCount;

			for(int i = 0; i < src.subMeshCount; i++)
				dest.SetIndices(src.GetIndices(i), src.GetTopology(i), i);

			dest.name = z_Util.IncrementPrefix("z", src.name);
		}

		/**
		 *	Creates a new mesh using only the @src positions, normals, and a new color array.
		 */
		public static Mesh CreateOverlayMesh(Mesh src)
		{
			Mesh m = new Mesh();
			m.name = "Overlay Mesh: " + src.name;
			m.vertices = src.vertices;
			m.normals = src.normals;
			m.colors = z_Util.Fill<Color>(new Color(0f, 0f, 0f, 0f), m.vertexCount);
			m.subMeshCount = src.subMeshCount;
			for(int i = 0; i < src.subMeshCount; i++)
			{
				if(src.GetTopology(i) == MeshTopology.Triangles)
				{
					int[] tris = src.GetIndices(i);
					int[] lines = new int[tris.Length * 2];
					int index = 0;
					for(int n = 0; n < tris.Length; n+=3)
					{
						lines[index++] = tris[n+0];
						lines[index++] = tris[n+1];
						lines[index++] = tris[n+1];
						lines[index++] = tris[n+2];
						lines[index++] = tris[n+2];
						lines[index++] = tris[n+0];
					}
					m.SetIndices(lines, MeshTopology.Lines, i);
				}
				else
				{
					m.SetIndices(src.GetIndices(i), src.GetTopology(i), i);
				}
			}
			return m;
		}

		private static readonly Color clear = new Color(0,0f,0,0f);

		public static Mesh CreateVertexBillboardMesh(Mesh src, List<List<int>> common)
		{
			int vertexCount = System.Math.Min( ushort.MaxValue / 4, common.Count() );

			Vector3[] positions = new Vector3[vertexCount * 4];
			Vector2[] uv0 		= new Vector2[vertexCount * 4];
			Vector2[] uv2 		= new Vector2[vertexCount * 4];
			Color[] colors 		= new Color[vertexCount * 4];
			int[] 	  tris 		= new int[vertexCount * 6];

			int n = 0;
			int t = 0;

			Vector3 up = Vector3.up;// * .1f;
			Vector3 right = Vector3.right;// * .1f;

			Vector3[] v = src.vertices;

			for(int i = 0; i < vertexCount; i++)
			{
				int tri = common[i][0];

				positions[t+0] = v[tri];
				positions[t+1] = v[tri];
				positions[t+2] = v[tri];
				positions[t+3] = v[tri];

				uv0[t+0] = Vector3.zero;
				uv0[t+1] = Vector3.right;
				uv0[t+2] = Vector3.up;
				uv0[t+3] = Vector3.one;

				uv2[t+0] = -up-right;
				uv2[t+1] = -up+right;
				uv2[t+2] =  up-right;
				uv2[t+3] =  up+right;

				tris[n+0] = t + 0;
				tris[n+1] = t + 1;
				tris[n+2] = t + 2;
				tris[n+3] = t + 1;
				tris[n+4] = t + 3;
				tris[n+5] = t + 2;

				colors[t+0] = clear;
				colors[t+1] = clear;
				colors[t+2] = clear;
				colors[t+3] = clear;

				t += 4;
				n += 6;
			}

			Mesh m = new Mesh();

			m.vertices = positions;
			m.uv = uv0;
			m.uv2 = uv2;
			m.colors = colors;
			m.triangles = tris;

			return m;
		}

		/**
		 *	Builds a lookup table for each vertex index and it's average normal with other vertices sharing a position.
		 */
		public static Dictionary<int, Vector3> GetSmoothNormalLookup(Mesh mesh)
		{
			Dictionary<int, Vector3> normals = new Dictionary<int, Vector3>();
			Vector3[] n = mesh.normals;
			List<List<int>> groups = GetCommonVertices(mesh);

			Vector3 avg = Vector3.zero;
			Vector3 a = Vector3.zero;
			foreach(var group in groups)
			{
				avg.x = 0f;
				avg.y = 0f;
				avg.z = 0f;

				foreach(int i in group)
				{
					a = n[i];

					avg.x += a.x;
					avg.y += a.y;
					avg.z += a.z;
				}

				avg /= (float) group.Count();

				foreach(int i in group)
					normals.Add(i, avg);
			}

			return normals;
		}

		/// Store a temporary cache of common vertex indices.
		public static Dictionary<Mesh, List<List<int>>> commonVerticesCache = new Dictionary<Mesh, List<List<int>>>();

		/**
		 *	Builds a list<group> with each vertex index and a list of all other vertices sharing a position.
		 * 	key: Index in vertices array
		 *	value: List of other indices in positions array that share a point with this index.
		 */
		public static List<List<int>> GetCommonVertices(Mesh mesh)
		{
			List<List<int>> indices;

			if( commonVerticesCache.TryGetValue(mesh, out indices) )
			{
				if( indices.Select(x => x.Max(y => y)).Max(z => z) + 1 == mesh.vertexCount )
				{
					return indices;
				}
			}

			Vector3[] v = mesh.vertices;
			int[] t = z_Util.Fill<int>((x) => { return x; }, v.Length);
			indices = t.ToLookup( x => (z_RndVec3)v[x] ).Select(y => y.ToList()).ToList();

			if(!commonVerticesCache.ContainsKey(mesh))
				commonVerticesCache.Add(mesh, indices);
			else
				commonVerticesCache[mesh] = indices;


			return indices;
		}

		public static List<z_Edge> GetEdges(Mesh m)
		{
			Dictionary<int, int> lookup = GetCommonVertices(m).GetCommonLookup<int>();
			return GetEdges(m, lookup);
		}

		public static List<z_Edge> GetEdges(Mesh m, Dictionary<int, int> lookup)
		{
			int[] tris = m.triangles;
			int count = tris.Length;

			List<z_Edge> edges = new List<z_Edge>(count);

			for(int i = 0; i < count; i += 3)
			{
				edges.Add( new z_Edge(tris[i+0], tris[i+1], lookup[tris[i+0]], lookup[tris[i+1]]) );
				edges.Add( new z_Edge(tris[i+1], tris[i+2], lookup[tris[i+1]], lookup[tris[i+2]]) );
				edges.Add( new z_Edge(tris[i+2], tris[i+0], lookup[tris[i+2]], lookup[tris[i+0]]) );
			}

			return edges;
		}

		public static HashSet<z_Edge> GetEdgesDistinct(Mesh mesh, out List<z_Edge> duplicates)
		{
			Dictionary<int, int> lookup = GetCommonVertices(mesh).GetCommonLookup<int>();
			return GetEdgesDistinct(mesh, lookup, out duplicates);
		}

		private static HashSet<z_Edge> GetEdgesDistinct(Mesh m, Dictionary<int, int> lookup, out List<z_Edge> duplicates)
		{
			int[] tris = m.triangles;
			int count = tris.Length;

			HashSet<z_Edge> edges = new HashSet<z_Edge>();
			duplicates = new List<z_Edge>();

			for(int i = 0; i < count; i += 3)
			{
				z_Edge a = new z_Edge(tris[i+0], tris[i+1], lookup[tris[i+0]], lookup[tris[i+1]]);
				z_Edge b = new z_Edge(tris[i+1], tris[i+2], lookup[tris[i+1]], lookup[tris[i+2]]);
				z_Edge c = new z_Edge(tris[i+2], tris[i+0], lookup[tris[i+2]], lookup[tris[i+0]]);

				if(!edges.Add(a))
					duplicates.Add(a);

				if(!edges.Add(b))
					duplicates.Add(b);

				if(!edges.Add(c))
					duplicates.Add(c);
			}

			return edges;
		}

		/**
		 *	Returns all vertex indices that are on an open edge.
		 */
		public static HashSet<int> GetNonManifoldIndices(Mesh mesh)
		{
			List<z_Edge> duplicates;
			HashSet<z_Edge> edges = GetEdgesDistinct(mesh, out duplicates);

			edges.ExceptWith(duplicates);

			HashSet<int> hash = z_Edge.ToHashSet( edges );
			return hash;
		}

		/**
		 *	Builds a lookup with each vertex index and a list of all neighboring indices.
		 */
		public static Dictionary<int, List<int>> GetAdjacentVertices(Mesh mesh)
		{
			List<List<int>> common = GetCommonVertices(mesh);
			Dictionary<int, int> lookup = common.GetCommonLookup<int>();

			List<z_Edge> edges = GetEdges(mesh, lookup).ToList();
			List<List<int>> map = new List<List<int>>();

			for(int i = 0; i < common.Count(); i++)
				map.Add(new List<int>());

			for(int i = 0; i < edges.Count; i++)
			{
				map[edges[i].cx].Add(edges[i].y);
				map[edges[i].cy].Add(edges[i].x);
			}

			Dictionary<int, List<int>> adjacent = new Dictionary<int, List<int>>();
			IEnumerable<int> distinctTriangles = mesh.triangles.Distinct();

			foreach(int i in distinctTriangles)
				adjacent.Add(i, map[lookup[i]]);

			return adjacent;
		}

		static Dictionary<Mesh, Dictionary<int, List<int>>> adjacentTriangleCache = new Dictionary<Mesh, Dictionary<int, List<int>>>();

		/**
		 *	Returns a dictionary where key is triangle index and value is a list of other triangle indices that share a common vertex.
		 */
		public static Dictionary<int, List<int>> GetAdjacentTriangles(Mesh m)
		{
 			int[] indices = m.triangles;
 			int vertexCount = m.vertexCount;

			if(indices.Length % 3 != 0)
			{
				Debug.LogWarning("GetAdjacentTriangles requires mesh topology triangles");
				return null;
			}

			Dictionary<int, List<int>> lookup = null;
			bool inCache = false;

			if( adjacentTriangleCache.TryGetValue(m, out lookup) )
			{
				// tris - verts = shared triangle count
				if( lookup.Keys.Count == indices.Length - vertexCount )
				{
					return lookup;
				}

				inCache = true;
				lookup.Clear();
			}
			else
			{
				lookup = new Dictionary<int, List<int>>();
			}

			for(int i = 0; i < indices.Length - 3; i += 3)
			{
				for(int n = i + 3; n < indices.Length; n += 3)
				{
					if
					(
						(indices[i+0] == indices[n+0] ? 1 : 0) +
						(indices[i+1] == indices[n+0] ? 1 : 0) +
						(indices[i+2] == indices[n+0] ? 1 : 0) +
						(indices[i+0] == indices[n+1] ? 1 : 0) +
						(indices[i+1] == indices[n+1] ? 1 : 0) +
						(indices[i+2] == indices[n+1] ? 1 : 0) +
						(indices[i+0] == indices[n+2] ? 1 : 0) +
						(indices[i+1] == indices[n+2] ? 1 : 0) +
						(indices[i+2] == indices[n+2] ? 1 : 0) > 1
					)
					{
						List<int> list;

						if( lookup.TryGetValue(i/3, out list) )
							list.Add(n/3);
						else
							lookup.Add(i/3, new List<int>() { n / 3 } );

						if( lookup.TryGetValue(n/3, out list) )
							list.Add(i/3);
						else
							lookup.Add(n/3, new List<int>() { i / 3 } );

					}
				}
			}

			if(!inCache)
				adjacentTriangleCache.Add(m, lookup);
			else
				adjacentTriangleCache[m] = lookup;

			return lookup;
		}

		private static Dictionary<Mesh, List<List<int>>> commonNormalsCache = new Dictionary<Mesh, List<List<int>>>();

		/**
		 *	Vertices that are common, form a seam, and should be smoothed.
		 */
		public static List<List<int>> GetSmoothSeamLookup(Mesh m)
		{
			Vector3[] normals = m.normals;

			if(normals == null)
				return null;

			List<List<int>> lookup = null;

			if(commonNormalsCache.TryGetValue(m, out lookup))
				return lookup;

			List<List<int>> common = GetCommonVertices(m);

			var z = common
				.SelectMany(x => x.GroupBy( i => (z_RndVec3)normals[i] ))
					.Where(n => n.Count() > 1)
						.Select(t => t.ToList())
							.ToList();

			commonNormalsCache.Add(m, z);

			return z;
		}

		/**
		 *	Recalculates a mesh's normals while retaining smoothed common vertices.
		 */
		public static void RecalculateNormals(Mesh m)
		{
			List<List<int>> smooth = GetSmoothSeamLookup(m);

			m.RecalculateNormals();

			if(smooth != null)
			{
				Vector3[] normals = m.normals;

				foreach(List<int> l in smooth)
				{
					Vector3 n = z_Math.Average(normals, l);
					foreach(int i in l)
						normals[i] = n;
				}

				m.normals = normals;
			}
		}
	}
}
