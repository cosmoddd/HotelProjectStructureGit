using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Voxeland
{
	[ExecuteInEditMode]
	public class Far : MonoBehaviour 
	{
		public int x; public int z;
		
		public VoxelandTerrain land;
		public MeshFilter filter;
		
		//public Mesh sourceMesh; //no need to use meshwrapper //using land.farMesh
		public Vector3[] builtVerts; //verts that were built, but not adjusted

		//public float lastScale = 1; //to restore mesh's original verts x and z in a scaled farmesh
		
		static public string[] shaderMainTexNames = new string[] {"_MainTex", "_MainTex2", "_MainTex3", "_MainTex4"};
		static public string[] shaderBumpTexNames = new string[] {"_BumpMap", "_BumpMap2", "_BumpMap3", "_BumpMap4"};
		
		public static Far Create (VoxelandTerrain land)
		{
			GameObject obj = new GameObject("Far");
			obj.transform.parent = land.transform;
			obj.transform.localPosition = Vector3.zero;
			
			Far far = obj.AddComponent<Far>();
			far.land = land;
			
			obj.AddComponent<MeshRenderer>();
			far.filter = obj.AddComponent<MeshFilter>();
			//far.filter.sharedMesh = new Mesh();
			
			far.GetComponent<Renderer>().sharedMaterial = land.terrainMaterial;
			if (land.hideChunks) far.transform.hideFlags = HideFlags.HideInHierarchy;

			//hiding wireframe
			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetSelectedWireframeHidden(obj.GetComponent<Renderer>(), land.hideWire);
			
			//copy static flag
			UnityEditor.StaticEditorFlags flags = UnityEditor.GameObjectUtility.GetStaticEditorFlags(land.gameObject);
			UnityEditor.GameObjectUtility.SetStaticEditorFlags(obj, flags);
			#endif

			return far;
		}
		
		public void Build ()
		{
			if (land.profile) Profiler.BeginSample("Build Far");
			
			if (land.farMesh == null) { filter.sharedMesh.Clear(); return; }
			
			//round coordinates to prevent jitter
			int step = (int)(land.farSize / 5f);
			x = Mathf.RoundToInt( 1f*x/step ) * step;
			z = Mathf.RoundToInt( 1f*z/step ) * step;
			
			//creating wrapper
			MeshWrapper wrapper = new MeshWrapper(land.farMesh);
			wrapper.uvs = new Vector2[4][];
			wrapper.uvs[0] = new Vector2[wrapper.verts.Length]; //do not load arrays from mesh, but create them
			wrapper.uvs[1] = new Vector2[wrapper.verts.Length];
			wrapper.uvs[2] = new Vector2[wrapper.verts.Length];
			wrapper.uvs[3] = new Vector2[wrapper.verts.Length];
			if (land.rtpCompatible) wrapper.colors = new Color[wrapper.verts.Length];

			//calculating array of types
			//TODO bring this fn somewhere (to VoxelandTerrain) to avoid calculation 2 types per chunk
			int[] typeToChannel = new int[land.types.Length]; //[0,1,2,3,4,0,0,0]
			int counter = 0; 
			for (int t=0; t<land.types.Length; t++) 
			{
				if (land.types[t].filledTerrain) { typeToChannel[t]=counter; counter++; }
				else typeToChannel[t] = -1;
			}

			//gathering ref array
			bool[] refExist = new bool[land.types.Length];
			for (int i=0; i<land.types.Length; i++) refExist[i] = land.types[i].filledTerrain;

			//setting mesh constant arrays
			if (filter.sharedMesh==null || filter.sharedMesh.vertexCount != wrapper.verts.Length)
			{
				if (filter.sharedMesh==null) filter.sharedMesh = new Mesh();
				filter.sharedMesh.Clear();
				filter.sharedMesh.vertices = new Vector3[wrapper.verts.Length]; //to equalize number of verts with wrapper
				//filter.sharedMesh.tangents = new Vector4[wrapper.verts.Length];
			}

			#region Setting Verts

				for (int v=0; v<wrapper.verts.Length; v++)
				{
					//vert position
					float vertX = wrapper.verts[v].x * land.farSize;
					float vertZ = wrapper.verts[v].z * land.farSize;
					wrapper.verts[v] = new Vector3(vertX, land.data.GetTopPoint((int)vertX+x, (int)vertZ+z), vertZ); //there could be GetTopPoint with refExist, but this is faster

					//vert type
					int type = land.data.GetTopType((int)vertX+x, (int)vertZ+z, refExist);
					if (type>=typeToChannel.Length) type=0;
					int textureNum = typeToChannel[type];

					if (!land.rtpCompatible)
					{
						switch (textureNum)
						{
							case 0: wrapper.uvs[0][v] = new Vector2(1,0); break;
							case 1: wrapper.uvs[0][v] = new Vector2(0,1); break;
							case 2: wrapper.uvs[1][v] = new Vector2(1,0); break;
							case 3: wrapper.uvs[1][v] = new Vector2(0,1); break;
							case 4: wrapper.uvs[2][v] = new Vector2(1,0); break;
							case 5: wrapper.uvs[2][v] = new Vector2(0,1); break;
							case 6: wrapper.uvs[3][v] = new Vector2(1,0); break;
							case 7: wrapper.uvs[3][v] = new Vector2(0,1); break;
						}
					}
					else
					{
						switch (textureNum)
						{
							case 0: wrapper.colors[v] = new Color(1,0,0,0); break;
							case 1: wrapper.colors[v] = new Color(0,1,0,0); break;
							case 2: wrapper.colors[v] = new Color(0,0,1,0); break;
							case 3: wrapper.colors[v] = new Color(0,0,0,1); break;
						}
					}
				}
			#endregion

			#region Calculating normals

				wrapper.ApplyTo(filter.sharedMesh);
				filter.sharedMesh.RecalculateNormals();
				wrapper.normals = filter.sharedMesh.normals;

			#endregion

			#region Adjust (lower verts and disable tris)

				//calculating already built area
				int minX = 2147483000;
				int maxX = -2147483000;
				int minZ = 2147483000;
				int maxZ = -2147483000;

				for (int i=0; i<land.chunks.array.Length; i++)
				{
					Chunk chunk = land.chunks.array[i];
					if (chunk == null) continue;
					if (!chunk.stage.applyTerrainComplete) continue;
					if (chunk.faces == null || chunk.faces.Length == 0) continue;

					if (chunk.offsetX < minX) minX = chunk.offsetX;
					if (chunk.offsetZ < minZ) minZ = chunk.offsetZ;
					if (chunk.offsetX + land.chunkSize > maxX) maxX = chunk.offsetX + land.chunkSize;
					if (chunk.offsetZ + land.chunkSize > maxZ) maxZ = chunk.offsetZ + land.chunkSize;
				}

				//finding verts that are intersecting already built chunks
				bool[] vertInChunk = new bool[wrapper.verts.Length];
				for (int i=0; i<wrapper.verts.Length; i++)
				{
					Vector3 vert = wrapper.verts[i];

					vertInChunk[i] = true;

					if (vert.x+x < minX || vert.z+z < minZ || vert.x+x > maxX || vert.z+z > maxZ) { vertInChunk[i] = false; continue; }

					int vertChunkX = Mathf.FloorToInt(1f*(vert.x+x)/land.chunkSize);
					int vertChunkZ = Mathf.FloorToInt(1f*(vert.z+z)/land.chunkSize);

					if (!land.chunks.CheckInRange(vertChunkX, vertChunkZ)) { vertInChunk[i] = false; continue; }

					Chunk chunk = land.chunks[vertChunkX, vertChunkZ];
					if (chunk == null || !chunk.stage.applyTerrainComplete) vertInChunk[i] = false;
					//Debug.Log(chunk.stage.ToString());

					//flooring vert a bit
					wrapper.verts[i] -= wrapper.normals[i]*0.5f;
				}

				//if all of the triangle verts are in chunk or at floor - disable triangle
				int[] tris = wrapper.tris[0].array;
				for (int t=0; t<tris.Length; t+=3)
				{
					if ((vertInChunk[ tris[t] ] && vertInChunk[ tris[t+1] ] && vertInChunk[ tris[t+2] ]) ||
						(wrapper.verts[ tris[t] ].y<0.01f && wrapper.verts[ tris[t+1] ].y<0.01f && wrapper.verts[ tris[t+2]].y<0.01f) )
						{ tris[t]=0; tris[t+1]=0; tris[t+2]=0; }
				}

			#endregion

			//applying wrapper
			wrapper.ApplyTo(filter.sharedMesh);

			//resetting vertex colors
			if (!land.rtpCompatible)
			{
				Color[] colors = new Color[wrapper.verts.Length];
				for (int v=0; v<wrapper.verts.Length; v++) colors[v] = new Color(0.5f, 0.5f, 0.5f, 1f);
				filter.sharedMesh.colors = colors;
			}

			#region Material

				GetComponent<Renderer>().sharedMaterial = land.terrainMaterial;
				//MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();

				//int defaultType = 0;
				//	for(int i=0; i<typeToChannel.Length; i++) 
				//		if (typeToChannel[i] != 0) defaultType = i;
				
				//for(int i=0; i<4; i++)
				//	{
				//		string propPostfix = i==0 ? "" : (i+1).ToString();
						
				//		int type = defaultType;
				//		for (int j=0; j<typeToChannel.Length; j++) 
				//			if (typeToChannel[j]==i) type = j;

				//		//if (land.types[type].texture != null) materialPropertyBlock.AddTexture("_MainTex" + propPostfix, land.types[type].texture);
				//		//if (land.types[type].bumpTexture != null)materialPropertyBlock.AddTexture("_BumpMap" + propPostfix, land.types[type].bumpTexture);
				//	}

				//materialPropertyBlock.SetFloat("_Tile", 0.1f);
				//GetComponent<Renderer>().SetPropertyBlock(materialPropertyBlock);

			#endregion

			//moving object
			transform.localPosition = new Vector3(x,0,z);

			//shrinking bounds so that they do not exceed build dist
			//filter.sharedMesh.bounds = new Bounds(Vector3.zero, new Vector3(land.removeDistance, filter.sharedMesh.bounds.extents.y, land.removeDistance));

			if (land.profile) Profiler.EndSample();
		}
		
		//Adjust and FastAdjust are outdated
		public void Adjust () //drops down verts that are in chunks
		{
			if (land.farMesh == null || builtVerts == null) return;
			int x = (int)transform.position.x;
			int z = (int)transform.position.z;
			Vector3[] adjustedVerts = new Vector3[builtVerts.Length];

			//calculating already built area
			int minX = 2147483000;
			int maxX = -2147483000;
			int minZ = 2147483000;
			int maxZ = -2147483000;

			for (int i=0; i<land.chunks.array.Length; i++)
			{
				Chunk chunk = land.chunks.array[i];
				if (chunk == null) continue;
				if (!chunk.stage.complete) continue;
				if (chunk.faces == null || chunk.faces.Length == 0) continue;

				if (chunk.offsetX < minX) minX = chunk.offsetX;
				if (chunk.offsetZ < minZ) minZ = chunk.offsetZ;
				if (chunk.offsetX + land.chunkSize > maxX) maxX = chunk.offsetX + land.chunkSize;
				if (chunk.offsetZ + land.chunkSize > maxZ) maxZ = chunk.offsetZ + land.chunkSize;
			}
			
			//finding verts that are intersecting already built chunks
			bool[] vertInChunk = new bool[builtVerts.Length];
			for (int i=0; i<builtVerts.Length; i++)
			{
				Vector3 vert = builtVerts[i];

				vertInChunk[i] = true;

				if (vert.x+x < minX || vert.z+z < minZ || vert.x+x > maxX || vert.z+z > maxZ) { vertInChunk[i] = false; continue; }

				int vertChunkX = Mathf.FloorToInt(1f*(vert.x+x)/land.chunkSize);
				int vertChunkZ = Mathf.FloorToInt(1f*(vert.z+z)/land.chunkSize);

				if (!land.chunks.CheckInRange(vertChunkX, vertChunkZ)) { vertInChunk[i] = false; continue; }

				Chunk chunk = land.chunks[vertChunkX , vertChunkZ ];
				if (chunk == null || !chunk.stage.complete) vertInChunk[i] = false;
			}

			//if one of the triangle verts is not in chunk - all 3 verts should not be floored
			bool[] floorVert = new bool[builtVerts.Length]; //vertInChunk remains to slightly drag vert down
			for (int i=0; i<floorVert.Length; i++) floorVert[i] = true;
			
			int[] tris = filter.sharedMesh.triangles;
			for (int t=0; t<tris.Length; t+=3)
			{
				//if any of the verts is not in chunk 
				if (!vertInChunk[ tris[t] ] || !vertInChunk[ tris[t+1] ] || !vertInChunk[ tris[t+2] ]) 
				{
					floorVert[ tris[t] ] = false;
					floorVert[ tris[t+1] ] = false;
					floorVert[ tris[t] ] = false;
				}
			}

			//flooring and lowering verts
			for (int i=0; i<builtVerts.Length; i++)
			{
				if (floorVert[i]) adjustedVerts[i] = new Vector3(builtVerts[i].x, 0, builtVerts[i].z);
				else if (vertInChunk[i]) adjustedVerts[i] = builtVerts[i] - Vector3.up*0.5f;
				else adjustedVerts[i] = builtVerts[i];
			}

			filter.sharedMesh.vertices = adjustedVerts;
		}

		public void FastAdjust () //for test purposes only
		{
			if (land.farMesh == null || builtVerts == null) return;
			int x = (int)transform.position.x;
			int z = (int)transform.position.z;
			Vector3[] adjustedVerts = new Vector3[builtVerts.Length];

			//calculating already built area
			int minX = 2147483000;
			int maxX = -2147483000;
			int minZ = 2147483000;
			int maxZ = -2147483000;

			for (int i=0; i<land.chunks.array.Length; i++)
			{
				Chunk chunk = land.chunks.array[i];
				if (chunk == null) continue;
				if (!chunk.stage.complete) continue;
				if (chunk.faces == null || chunk.faces.Length == 0) continue;

				if (chunk.offsetX < minX) minX = chunk.offsetX;
				if (chunk.offsetZ < minZ) minZ = chunk.offsetZ;
				if (chunk.offsetX + land.chunkSize > maxX) maxX = chunk.offsetX + land.chunkSize;
				if (chunk.offsetZ + land.chunkSize > maxZ) maxZ = chunk.offsetZ + land.chunkSize;
			}

			//flooring verts
			for (int v=0; v<builtVerts.Length; v++)
			{
				Vector3 vert = builtVerts[v];
				if (vert.x+x < minX || vert.z+z < minZ || vert.x+x > maxX || vert.z+z > maxZ) { adjustedVerts[v] = builtVerts[v]; continue; }

				int vertChunkX = Mathf.FloorToInt(1f*(vert.x+x)/land.chunkSize);
				int vertChunkZ = Mathf.FloorToInt(1f*(vert.z+z)/land.chunkSize);
				
				if (!land.chunks.CheckInRange(vertChunkX, vertChunkZ)) { adjustedVerts[v] = builtVerts[v]; continue; }

				Chunk chunk = land.chunks[vertChunkX , vertChunkZ ];
				if (chunk == null || !chunk.stage.complete) { adjustedVerts[v] = builtVerts[v]; continue; }

				adjustedVerts[v] = new Vector3(builtVerts[v].x, 0, builtVerts[v].z);
			}

			filter.sharedMesh.vertices = adjustedVerts;
		}
	}
	
}//namespace
