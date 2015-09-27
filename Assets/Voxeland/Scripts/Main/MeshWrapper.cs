using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//TODO: Test performance with appendFn

namespace Voxeland 
{
	[System.Serializable]
	public class Submesh
	{
		public int[] array;
		public int triNum;

		public int this[int num] { get {return array[num];} set {array[num] = value;} }
		public int Length { get{return array.Length;} }

		public Submesh (int max) { array = new int[max]; triNum = 0; }
		public Submesh (int[] src) { array = (int[])src.Clone(); triNum = src.Length; }

		public void Append (Submesh a)
		{
			for (int i=0; i<a.triNum; i++) array[triNum+i] = a.array[i] + triNum;
			triNum += a.triNum;
		}
	}
	
	[System.Serializable]
	public class CountedArray<T>
	{
		public T[] array;
		public int count;

		public CountedArray () { array = new T[0]; count = 0; }
		public CountedArray (int c) { array = new T[c]; count = 0; }
		public CountedArray (T[] src, int c) { array = new T[c]; int max = Mathf.Min(c,src.Length); for (int i=0; i<max; i++) array[i]=src[i]; count = 0; }
		public CountedArray (T[] src) { array = (T[])src.Clone(); }

		public void Append (CountedArray<T> a, System.Func<T,T> appendFn=null)
		{
			if (appendFn==null) for (int i=0; i<a.count; i++) array[count+i] = a.array[i];
			else for (int i=0; i<a.count; i++) array[count+i] = appendFn(a.array[i]);

			count += a.count;
		}
	}

	[System.Serializable]
	public class MeshWrapper
	{
		//verts
		public Vector3[] verts;
		public Vector3[] normals; 
		public Vector4[] tangents;
		public Color[] colors;
		public Vector2[][] uvs;
		public int vertNum = 0;
		
		//triangles
		public Submesh[] tris;

		public int subMeshesCount 
		{get{
			int result = 0;
			for (int i=0; i<tris.Length; i++) 
				if (tris[i] != null) result++; 
			return result; 
		}}


		//constructors
		public MeshWrapper () { Initialize(0,new int[0]); }
		
		public MeshWrapper (int numVerts, int numTris, bool useNormals=false, bool useTangents=false, bool useColors=false, int numUvs=0)
			{ Initialize(numVerts, new int[] {numTris}, useNormals, useTangents, useColors, numUvs); }
		
		public MeshWrapper (int numVerts, int[] numTris, bool useNormals=false, bool useTangents=false, bool useColors=false, int numUvs=0)
			{ Initialize(numVerts, numTris, useNormals, useTangents, useColors, numUvs); }
		
		public void Initialize (int numVerts, int[] numTris, bool useNormals=false, bool useTangents=false, bool useColors=false, int numUvs=0)
		{
			verts = new Vector3[numVerts];
			if (useNormals) normals = new Vector3[numVerts];
			if (useTangents) tangents = new Vector4[numVerts];
			if (useColors) colors = new Color[numVerts];
			
			uvs = new Vector2[numUvs][];
			for (int i=0; i<uvs.Length; i++) uvs[i] = new Vector2[numVerts];

			vertNum = 0;

			tris = new Submesh[numTris.Length];
			for (int t=0; t<tris.Length; t++) 
				if (numTris[t] != 0) 
					tris[t] = new Submesh(numTris[t]);
		}

		public MeshWrapper (Mesh mesh, bool useNormals=false, bool useTangents=false, bool useColors=false)
		{
			verts = mesh.vertices;
			if (useNormals) normals = mesh.normals;
			if (useTangents) tangents = mesh.tangents;
			if (useColors) colors = mesh.colors;
			
			Vector2[] uv0 =  mesh.uv;
			Vector2[] uv1 =  mesh.uv2;
			Vector2[] uv2 =  mesh.uv3;
			Vector2[] uv3 =  mesh.uv4;

			if (uv3 != null) uvs = new Vector2[][] {uv0, uv1, uv2, uv3};
			else if (uv2 != null) uvs = new Vector2[][] {uv0, uv1, uv2};
			else if (uv1 != null) uvs = new Vector2[][] {uv0, uv1};
			else if (uv0 != null) uvs = new Vector2[][] {uv0};

			vertNum = verts.Length;

			tris = new Submesh[1];
			tris[0] = new Submesh(mesh.triangles);
			//TODO: MeshWrapper does not load submeshes
		}


		//append
		public void Append (MeshWrapper addMesh, int submesh=-1, System.Func<Vector3,Vector3> vertFn=null, System.Func<Vector3,Vector3> normalFn=null)
		{
			//verts
			if (vertFn==null) for (int v=0; v<addMesh.verts.Length; v++) verts[vertNum+v] = addMesh.verts[v];
			else for (int v=0; v<addMesh.verts.Length; v++) verts[vertNum+v] = vertFn(addMesh.verts[v]);
			
			if (normals != null)
			{
				if (normalFn == null) 
				{
					if (addMesh.normals != null)
						for (int v=0; v<addMesh.normals.Length; v++) normals[vertNum+v] = addMesh.normals[v];
					//else 
					//	for (int v=0; v<addMesh.verts.Length; v++) normals[vertNum+v] = Vector3.up;
				}
				else 
				{
					if (addMesh.normals != null) 
						for (int v=0; v<addMesh.normals.Length; v++) normals[vertNum+v] = normalFn(addMesh.normals[v]); //kinda complicated there with null-checking
					else 
						for (int v=0; v<addMesh.verts.Length; v++) normals[vertNum+v] = normalFn(Vector3.up);
				}
			}
			
			if (tangents !=null && addMesh.tangents !=null) for (int v=0; v<addMesh.tangents.Length; v++) tangents[vertNum+v] = addMesh.tangents[v];
			if (colors !=null && addMesh.colors !=null) for (int v=0; v<addMesh.colors.Length; v++) colors[vertNum+v] = addMesh.colors[v];

			for (int i=0; i<Mathf.Min(uvs.Length,addMesh.uvs.Length); i++)
			{
				Vector2[] uv = uvs[i]; Vector2[] adduv = addMesh.uvs[i];
				if (uv!=null && adduv!=null)
					for (int v=0; v<addMesh.verts.Length; v++) uv[vertNum+v] = adduv[v];
				
			}

			vertNum += addMesh.verts.Length;

			//tris
			if (submesh < 0) //if submesh number is not defined - appending all of submeshes from additive mesh
			{
				int max = Mathf.Min(addMesh.tris.Length, tris.Length);
				for (int i=0; i<max; i++)
					if (addMesh.tris[i] != null) tris[i].Append(addMesh.tris[i]);
			}
			else
			{
				for (int i=0; i<addMesh.tris.Length; i++)
					tris[submesh].Append(addMesh.tris[i]);
			}
		}

		public void AppendWithOffset (MeshWrapper addMesh, Vector3 offset, int submesh=-1)
		{
			System.Func<Vector3,Vector3> vertFn = delegate(Vector3 addVert) { return addVert+offset; };
			Append(addMesh, submesh, vertFn);
		}

		public void AppendWithFFD (MeshWrapper addMesh, int submesh, Vector3 cornerA, Vector3 cornerB, Vector3 cornerC, Vector3 cornerD, Vector3 normal)
		{
			System.Func<Vector3,Vector3> vertFn = delegate(Vector3 addVert) 
			{
				float xPercent = addVert.x + 0.5f;
				float zPercent = addVert.z + 0.5f;
				
				Vector3 vertX1 = cornerB*xPercent + cornerA*(1-xPercent);
				Vector3 vertX2 = cornerC*xPercent + cornerD*(1-xPercent);
				
				return vertX1*zPercent + vertX2*(1-zPercent) + new Vector3(0, addVert.y, 0);
			};

			System.Func<Vector3,Vector3> normalFn = null;
			if (normal.sqrMagnitude > 0.01f) normalFn = delegate(Vector3 addNormal) { return normal; };

			Append(addMesh, submesh, vertFn, normalFn);
		}

		public void AppendToFace (MeshWrapper addMesh, Chunk.Face face, Vector3[] terrainVerts, int submesh, float height=1f, float incline=0f, float random=0f, bool takeNormals=true)
		{
			System.Func<Vector3,Vector3> vertFn = delegate(Vector3 addVert) 
			{
				float xPercent = addVert.x + 0.5f;
				float zPercent = addVert.z + 0.5f;
				
				Vector3 vertX1 = terrainVerts[face.cornerNums.b]*xPercent + terrainVerts[face.cornerNums.a]*(1-xPercent);
				Vector3 vertX2 = terrainVerts[face.cornerNums.c]*xPercent + terrainVerts[face.cornerNums.d]*(1-xPercent);
				Vector3 vert = vertX1*zPercent + vertX2*(1-zPercent); 

				//TODO: there should be vertex num added, otherwise all grass bush will be of the same height
				return new Vector3(
					vert.x + face.normal.x*incline + (Noise.Random(face.x,face.y,face.z,face.dir)-0.5f)*random, 
					vert.y + addVert.y*height + (Noise.Random(face.x,face.y,face.z,face.dir+1)-0.5f)*random, 
					vert.z + face.normal.z*incline + (Noise.Random(face.x,face.y,face.z,face.dir+2)-0.5f)*random );
			};

			System.Func<Vector3,Vector3> normalFn = null;
			if (takeNormals) normalFn = delegate(Vector3 addNormal) { return face.normal; };

			Append(addMesh, submesh, vertFn, normalFn);
		}


		//apply
		public void ApplyTo (Mesh mesh)
		{
			ApplyVertsTo(mesh);
			ApplyTrisTo(mesh);
		}

		public void ApplyVertsTo (Mesh mesh)
		{
			mesh.Clear();

			//setting verts
			mesh.vertices = verts;
			mesh.vertices = verts;
			if (normals != null && normals.Length == verts.Length) mesh.normals = normals;
			if (tangents != null && tangents.Length == verts.Length) mesh.tangents = tangents;
			if (colors != null && colors.Length == verts.Length) mesh.colors = colors;
			if (uvs.Length>=1 && uvs[0].Length == verts.Length) mesh.uv = uvs[0];
			if (uvs.Length>=2 && uvs[1].Length == verts.Length) mesh.uv2 = uvs[1];
			if (uvs.Length>=3 && uvs[2].Length == verts.Length) mesh.uv3 = uvs[2];
			if (uvs.Length>=4 && uvs[3].Length == verts.Length) mesh.uv4 = uvs[3];

			mesh.RecalculateBounds();
		}

		public void ApplyTrisTo (Mesh mesh)
		{
			mesh.subMeshCount = subMeshesCount;
			
			int counter = 0;
			for (int i=0; i<tris.Length; i++)
			{
				if (tris[i] == null) continue;
				mesh.SetTriangles(tris[i].array, counter);
				counter++;
			}
			
			if (normals == null) mesh.RecalculateNormals(); //if not custom normals - recalculating them. This should be done after applying tris
			mesh.RecalculateBounds();
		}

		//materials
		public Material[] GetMaterials (Material[] allMats)
		{
			Material[] mats = new Material[subMeshesCount];

			int counter = 0;
			for (int i=0; i<Mathf.Min(tris.Length, allMats.Length); i++)
			{
				if (tris[i] == null) continue;
				mats[counter] = allMats[i];
				counter++;
			}

			return mats;
		}

		public void RotateMirror (int rotation, bool mirror) //rotation in 90-degree
		{
			//setting mesh rot-mirror params
			bool mirrorX = false;
			bool mirrorZ = false;
			bool rotate = false;
			
			switch (rotation)
			{
				case 90: rotate = true; mirrorX = true; break;
				case 180: mirrorX = true; mirrorZ = true; break;
				case 270: rotate = true; mirrorZ = true; break;
			}
			
			if (mirror) mirrorX = !mirrorX;
			
			//rotating verts
			for (int v=0; v<verts.Length; v++)
			{ 
				Vector3 pos = verts[v];
				//Vector3 normal = ns.array[v];
				//Vector4 tangent = ts.array[v];
				
				if (rotate)
				{
					float temp;

					temp = pos.x;
					pos.x = pos.z;
					pos.z = temp;
					
					//temp = normal.x;
					//normal.x = normal.z;
					//normal.z = temp;

					//temp = tangent.x;
					//tangent.x = tangent.z;
					//tangent.z = temp;
				}
				
				if (mirrorX) { pos.x = -pos.x;  }
				if (mirrorZ) { pos.z = -pos.z; } 
				
				verts[v] = pos;
				//ns.array[v] = normal;
				//ts.array[v] = tangent;
			}
			
			//mirroring tris
			if (mirror) 
				for (int t=0; t<tris.Length; t++) 
					for (int i=0; i<tris[0].array.Length; i+=3) 
			{
				int temp = tris[0].array[i];
				tris[0].array[i] = tris[0].array[i+2];
				tris[0].array[i+2] = temp;
			}
		}

		/*public void AppendWithFFD (MeshWrapper addMesh, int type, Vector3 cornerA, Vector3 cornerB, Vector3 cornerC, Vector3 cornerD, Vector3 normal)
		{
			//verts
			for (int v=0; v < addMesh.verts.array.Length; v++) 
			{
				float xPercent = addMesh.verts.array[v].x + 0.5f;
				float zPercent = addMesh.verts.array[v].z + 0.5f;
				
				Vector3 vertX1 = cornerB*xPercent + cornerA*(1-xPercent);
				Vector3 vertX2 = cornerC*xPercent + cornerD*(1-xPercent);
				
				verts.array[verts.count+v] = vertX1*zPercent + vertX2*(1-zPercent) + new Vector3(0, addMesh.verts.array[v].y, 0); 
			}
			verts.count += addMesh.verts.array.Length;

			if (normals != null) 
			{
				if (normal.sqrMagnitude > 0.01f) for (int v=0; v<addMesh.verts.Length; v++) normals[vertCounter+v] = normal;
				else for (int v=0; v<addMesh.verts.Length; v++) normals[vertCounter+v] = addMesh.normals[v];
			}

			if (tangents != null) for (int v=0; v<addMesh.verts.Length; v++) tangents[vertCounter+v] = addMesh.tangents[v];
			if (uvs != null) for (int v=0; v<addMesh.verts.Length; v++) uvs[vertCounter+v] = addMesh.uvs[v];
			if (uv2 != null) for (int v=0; v<addMesh.verts.Length; v++) uv2[vertCounter+v] = addMesh.uv2[v];
			if (uv3 != null) for (int v=0; v<addMesh.verts.Length; v++) uv3[vertCounter+v] = addMesh.uv3[v];
			if (uv4 != null) for (int v=0; v<addMesh.verts.Length; v++) uv4[vertCounter+v] = addMesh.uv4[v];
			if (colors != null) for (int v=0; v<addMesh.verts.Length; v++) colors[vertCounter+v] = addMesh.colors[v];

			//tris
		//	for (int t=0; t<addMesh.triangles[0].Length; t++) 
		//		triangles[type][triCounters[type] + t] = addMesh.triangles[0][t] + vertCounter;

			for (int t=0; t<addMesh.tris.Length; t++) 
				triangles[type][triCounters[type] + t] = addMesh.tris[t] + vertCounter;
			triCounters[type] += addMesh.tris.Length;
			
			//increment counters	
		//	triCounters[type] += addMesh.triangles[0].Length;
			vertCounter += addMesh.verts.Length;
		}

		public void AppendToFace (MeshWrapper addMesh, Chunk.Face face, Vector3[] terrainVerts, int type, float height=1f, float incline=0f, float random=0f, bool takeNormals=true)
		{
			//verts
			for (int v=0; v < addMesh.verts.Length; v++) 
			{
				int vBase = v+vertNum;
				
				//random
				int randomNum = (int)Mathf.Repeat(face.x*20 + face.y*10 + face.z*5 + face.dir + v, 990);
				
				float xPercent = addMesh.verts[v].x + 0.5f;
				float zPercent = addMesh.verts[v].z + 0.5f;
				
				Vector3 vertX1 = terrainVerts[face.cornerNums.b]*xPercent + terrainVerts[face.cornerNums.a]*(1-xPercent);
				Vector3 vertX2 = terrainVerts[face.cornerNums.c]*xPercent + terrainVerts[face.cornerNums.d]*(1-xPercent);
				Vector3 vert = vertX1*zPercent + vertX2*(1-zPercent); 

				verts[vBase] = new Vector3(
					vert.x + face.normal.x*incline + (VoxelandTerrain.random[randomNum]-0.5f)*random, 
					vert.y + addMesh.verts[v].y*height + (VoxelandTerrain.random[randomNum+1]-0.5f)*random, 
					vert.z + face.normal.z*incline + (VoxelandTerrain.random[randomNum+2]-0.5f)*random);
				
				if (takeNormals) normals[vBase] = face.normal;
				else normals[vBase] = addMesh.normals[v];
				
				tangents[vBase] = addMesh.tangents[v];
				uvs[vBase] = addMesh.uvs[v]; uv2[vBase] = addMesh.uv2[v]; uv3[vBase] = addMesh.uv3[v]; uv4[vBase] = addMesh.uv4[v];
				
				colors[vBase] = addMesh.colors[v];
			}
			
			//tris
		//	for (int t=0; t<addMesh.triangles[0].Length; t++) 
		//		triangles[type][triCounters[type] + t] = addMesh.triangles[0][t] + vertCounter;

			for (int t=0; t<addMesh.tris.Length; t++) 
				triangles[type][triCounters[type] + t] = addMesh.tris[t] + vertCounter;
			triCounters[type] += addMesh.tris.Length;
			
			//increment counters	
		//	triCounters[type] += addMesh.triangles[0].Length;
			vertCounter += addMesh.verts.Length;
		}*/




//-------------------------
/*
		public int vertNum = 0;
		public Submesh[] subs = new Submesh[128];	
		public int[] tris { get{return subs[0].tris;} set {subs[0].tris = value;} }

		

		public void SetVertCount (int maxVerts) //will clear all verts
		{
			verts = new Vector3[maxVerts];
			normals = new Vector3[maxVerts]; 
			tangents = new Vector4[maxVerts];
			uvs = new Vector2[maxVerts];
			uv1 = new Vector2[maxVerts];
			colors = new Color[maxVerts]; 
		}
		
		public void SetTriCount (int[] triCount) //will clear all tris
		{
			for (int s=0; s<subs.Length; s++)
			{
				if (triCount[s] != 0) subs[s].tris = new int[triCount[s]]; 
				else subs[s].tris = null;
			}
		}
		
		public void SetTriCount (int triCount)
		{
			subs = new Submesh[128];
			subs[0].tris = new int[triCount];
		}
		
		public void ReadMesh (Mesh mesh) //will not read submeshes, assigns all to the first one
		{
			verts = mesh.vertices;
			normals = mesh.normals;
			tangents = mesh.tangents;
			uvs = mesh.uv;
			uv1 = mesh.uv2;
			colors = mesh.colors;
			
			if (colors.Length == 0) colors = new Color[verts.Length];
			if (uv1.Length == 0) uv1 = new Vector2[verts.Length];

			vertNum = 0;
			
			subs[0] = new Submesh();
			subs[0].tris = mesh.triangles;
			subs[0].triNum = 0;
		}
	

		
		public void SetAmbient (Vector4 ambient)
		{
			for (int v=0; v<verts.Length; v++) colors[v] = ambient;
		}
		
		public void SetAmbient (Vector4[] ambient)
		{
			for (int v=0; v<verts.Length; v++)
			{ 
				float percentX = verts[v].x + 0.5f;
				float percentY = verts[v].y + 0.5f;
				float percentZ = verts[v].z + 0.5f;
				
				Vector4 topLeftAmbient = ambient[2]*percentZ + ambient[3]*(1f-percentZ);
				Vector4 topRightAmbient = ambient[1]*percentZ + ambient[0]*(1f-percentZ);
				Vector4 bottomLeftAmbient = ambient[6]*percentZ + ambient[7]*(1f-percentZ);
				Vector4 bottomRightAmbient = ambient[5]*percentZ + ambient[4]*(1f-percentZ);
				
				Vector4 topAmbient = topLeftAmbient*percentX + topRightAmbient*(1f-percentX);
				Vector4 bottomAmbient = bottomLeftAmbient*percentX + bottomRightAmbient*(1f-percentX);
				
				Vector4 resAmbient = topAmbient*percentY + bottomAmbient*(1f-percentY);
				
				colors[v] = resAmbient;
			}
		}
		
		public void Append (MeshWrapper addMesh, float x,float y,float z, int type) //shift xyz. Type is submesh id that should be added to
		{
			//verts
			for (int v=0; v < addMesh.verts.Length; v++) 
			{
				int vBase = v+vertNum;
				
				verts[vBase] = new Vector3(addMesh.verts[v].x+x, addMesh.verts[v].y+y, addMesh.verts[v].z+z);
				normals[vBase] = addMesh.normals[v];
				tangents[vBase] = addMesh.tangents[v];
				uvs[vBase] = addMesh.uvs[v];
				uv1[vBase] = addMesh.uv1[v];
				colors[vBase] = addMesh.colors[v];
			}

			//tris
			for (int t=0; t<addMesh.subs[0].tris.Length; t++) 
				subs[type].tris[t+subs[type].triNum] = addMesh.subs[0].tris[t] + vertNum;
				
			subs[type].triNum += addMesh.subs[0].tris.Length;
				
			vertNum += addMesh.verts.Length;
		}
		

		
		public void Calculatetangents () //do not work yet
		{
			tangents = new Vector4[verts.Length];
		}
		
		public void Apply (Mesh mesh)	
		{
			ApplyVerts(mesh);
			ApplyTris(mesh);
		}

		public void ApplyVerts (Mesh mesh)
		{
			mesh.Clear();
			
			//setting verts
			mesh.vertices = verts;
			if (normals != null && normals.Length == verts.Length) mesh.normals = normals;
			if (tangents != null && tangents.Length == verts.Length) mesh.tangents = tangents;
			if (colors != null && colors.Length == verts.Length) mesh.colors = colors;
			if (uv1 != null && uv1.Length == verts.Length) mesh.uv2 = uv1;
			if (uvs != null && uvs.Length == verts.Length) mesh.uv = uvs;

			mesh.RecalculateBounds();
		}

		public void ApplyTris (Mesh mesh)
		{
			//calculating number of submeshes
			int subCount = 0;
			for (int i=0; i<subs.Length; i++) if (subs[i].tris != null) subCount++;
			mesh.subMeshCount = subCount;
			
			//setting triangles
			int counter = 0;
			for (int i=0; i<subs.Length; i++)
			{
				if (subs[i].tris == null) continue;
				mesh.SetTriangles(subs[i].tris, counter);
				counter ++;
			}
			
			if (normals == null) mesh.RecalculateNormals(); //if not custom normals - recalculating them
		}*/

	}//class
	
	
}//namespace