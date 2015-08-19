using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Voxeland 
{
	[System.Serializable]
	public struct Submesh
	{
		public int[] tris;
		public int triNum;

		public int this[int num] { get {return tris[num];} set {tris[num] = value;} }
	}	
	
	[System.Serializable]
	public class MeshWrapper
	{
		//verts
		public Vector3[] verts;
		public Vector3[] normals; 
		public Vector4[] tangents;
		public Vector2[] uvs;
		public Vector2[] uv1;
		public Color[] colors; 

		public int vertCounter = 0;

		//tris
		public int[][] triangles;
		public int[] triCounters;
		public int subCount { get{
			int result = 0;
			for (int i=0; i<triangles.Length; i++) 
				if (triangles[i] != null) result++; 
			return result; }}

		//constructors
		public MeshWrapper () { Initialize(0,new int[0]); }
		public MeshWrapper (int numVerts, int numTris, bool useNormals=false, bool useTangents=false, bool useUvs=false, bool useUv1=false, bool useColors=false)
			{ Initialize(numVerts, new int[] {numTris}, useNormals, useTangents, useUvs, useUv1, useColors); }
		public MeshWrapper (int numVerts, int[] numTris, bool useNormals=false, bool useTangents=false, bool useUvs=false, bool useUv1=false, bool useColors=false)
			{ Initialize(numVerts, numTris, useNormals, useTangents, useUvs, useUv1, useColors); }
		public void Initialize (int numVerts, int[] numTris, bool useNormals=false, bool useTangents=false, bool useUvs=false, bool useUv1=false, bool useColors=false)
		{
			verts = new Vector3[numVerts];
			if (useNormals) normals = new Vector3[numVerts];
			if (useTangents) tangents = new Vector4[numVerts];
			if (useUvs) uvs = new Vector2[numVerts];
			if (useUv1) uv1 = new Vector2[numVerts];
			if (useColors) colors = new Color[numVerts];
			vertCounter = 0;

			triangles = new int[numTris.Length][];
			for (int t=0; t<triangles.Length; t++) 
				if (numTris[t] != 0) 
					triangles[t] = new int[numTris[t]];
			triCounters = new int[numTris.Length];
		}
		public MeshWrapper (Mesh mesh, bool useNormals=false, bool useTangents=false, bool useUvs=false, bool useUv1=false, bool useColors=false)
		{
			verts = mesh.vertices;
			if (useNormals) normals = mesh.normals;
			if (useTangents) tangents = mesh.tangents;
			if (useUvs) uvs = mesh.uv;
			if (useUv1) uv1 = mesh.uv2;
			if (useColors) colors = mesh.colors;
			triangles = new int[1][];
			triCounters = new int[1];
			triangles[0] = mesh.triangles;
		}

		//operators
		public void Append (MeshWrapper addMesh, Vector3 offset) { Append(addMesh, offset, 0); }
		public void Append (MeshWrapper addMesh, Vector3 offset, int type)
		{
			//verts
			for (int v=0; v<addMesh.verts.Length; v++) verts[vertCounter+v] = addMesh.verts[v] + offset;
			if (normals != null) for (int v=0; v<addMesh.verts.Length; v++) verts[vertCounter+v] = addMesh.normals[v];
			if (tangents != null) for (int v=0; v<addMesh.verts.Length; v++) tangents[vertCounter+v] = addMesh.tangents[v];
			if (uvs != null) for (int v=0; v<addMesh.verts.Length; v++) uvs[vertCounter+v] = addMesh.uvs[v];
			if (uv1 != null) for (int v=0; v<addMesh.verts.Length; v++) uv1[vertCounter+v] = addMesh.uv1[v];
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

		public void AppendWithFFD (MeshWrapper addMesh, int type, Vector3 cornerA, Vector3 cornerB, Vector3 cornerC, Vector3 cornerD, Vector3 normal)
		{
			//verts
			for (int v=0; v < addMesh.verts.Length; v++) 
			{
				float xPercent = addMesh.verts[v].x + 0.5f;
				float zPercent = addMesh.verts[v].z + 0.5f;
				
				Vector3 vertX1 = cornerB*xPercent + cornerA*(1-xPercent);
				Vector3 vertX2 = cornerC*xPercent + cornerD*(1-xPercent);
				
				verts[vertCounter+v] = vertX1*zPercent + vertX2*(1-zPercent) + new Vector3(0, addMesh.verts[v].y, 0); 
			}

			if (normals != null) 
			{
				if (normal.sqrMagnitude > 0.01f) for (int v=0; v<addMesh.verts.Length; v++) normals[vertCounter+v] = normal;
				else for (int v=0; v<addMesh.verts.Length; v++) normals[vertCounter+v] = addMesh.normals[v];
			}

			if (tangents != null) for (int v=0; v<addMesh.verts.Length; v++) tangents[vertCounter+v] = addMesh.tangents[v];
			if (uvs != null) for (int v=0; v<addMesh.verts.Length; v++) uvs[vertCounter+v] = addMesh.uvs[v];
			if (uv1 != null) for (int v=0; v<addMesh.verts.Length; v++) uv1[vertCounter+v] = addMesh.uv1[v];
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
				uvs[vBase] = addMesh.uvs[v];
				uv1[vBase] = addMesh.uv1[v];
				
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
			if (normals != null && normals.Length == verts.Length) mesh.normals = normals;
			if (tangents != null && tangents.Length == verts.Length) mesh.tangents = tangents;
			if (colors != null && colors.Length == verts.Length) mesh.colors = colors;
			if (uv1 != null && uv1.Length == verts.Length) mesh.uv2 = uv1;
			if (uvs != null && uvs.Length == verts.Length) mesh.uv = uvs;

			mesh.RecalculateBounds();
		}

		public void ApplyTrisTo (Mesh mesh)
		{
			mesh.subMeshCount = subCount;
			
			int counter = 0;
			for (int i=0; i<triangles.Length; i++)
			{
				if (triangles[i] == null) continue;
				mesh.SetTriangles(triangles[i], counter);
				counter++;
			}
			
			if (normals == null) mesh.RecalculateNormals(); //if not custom normals - recalculating them. This should be done after applying tris
		}

		//materials
		public Material[] GetMaterials (Material[] allMats)
		{
			Material[] mats = new Material[subCount];

			int counter = 0;
			for (int i=0; i<Mathf.Min(triangles.Length, allMats.Length); i++)
			{
				if (triangles[i] == null) continue;
				mats[counter] = allMats[i];
				counter++;
			}

			return mats;
		}


//-------------------------
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
				float posX = verts[v].x;
				float posY = verts[v].y;
				float posZ = verts[v].z;
				
				float normX = normals[v].x;
				float normY = normals[v].y;
				float normZ = normals[v].z;

				float tangentX = tangents[v].x;
				float tangentY = tangents[v].y;
				float tangentZ = tangents[v].z;
				
				if (rotate)
				{
					posX = verts[v].z;
					posZ = verts[v].x;
					
					normX = normals[v].z;
					normZ = normals[v].x;

					tangentX = tangents[v].z;
					tangentZ = tangents[v].x;
				}
				
				if (mirrorX) { posX = -posX; normX = -normX; tangentX = -tangentX; }
				if (mirrorZ) { posZ = -posZ; normZ = -normZ; tangentZ = -tangentZ; } 
				
				verts[v] =  new Vector3(posX, posY, posZ);
				normals[v] = new Vector3(normX, normY, normZ);
				tangents[v] = new Vector4(tangentX, tangentY, tangentZ, tangents[v].w);
			}
			
			//mirroring tris
			if (mirror) 
				for (int v=0; v<subs[0].tris.Length; v+=3) 
			{
				int temp = subs[0].tris[v];
				subs[0].tris[v] = subs[0].tris[v+2];
				subs[0].tris[v+2] = temp;
			}
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
		}

	}//class
	
	
}//namespace