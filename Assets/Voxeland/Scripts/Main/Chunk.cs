
using UnityEngine;
using System.Collections.Generic;

#region Processing order scheme
//											Calc Ambient x.1
//												|
// Calc Mesh 1.y --	Compile Mesh 2.y	--	Compile Ambient 3.2
//						|					
// 					Build Grass					
//
// Build Prefabs
#endregion

namespace Voxeland 
{
	public class Chunk : MonoBehaviour 
	{
		static public int vertUniqueNum; //used to give each vert unique coords
		
		public VoxelandTerrain land;
		
		public Coord coord; //chunk coordiantes in array of chunks (chunks[0,1], chunks[-1,2], etc)
		
		public int coordX;
		public int coordZ;
		
		public int offsetX;
		public int offsetZ;	
		public int size = 32; //chunk size, not including invisible extended blocks
	
		//top and bottom coords - calculated every CalculateTerrain
		public int topPoint;
		public int bottomPoint;
		public int shortHeight; //difference between top and bottom points
		[System.NonSerialized] public bool[] existMatrix; 
		
		//to boost collider and do not calculate them every raycast
		public Vector2 boundsMin;
		public Vector2 boundsMax; 
		public Vector2 boundsCenter;

		public MeshFilter hiFilter;
		public MeshFilter loFilter;
		public MeshCollider collision;
		
		public MeshFilter grassFilter;
		//public MeshFilter constructorFilter; //dconstructor
		//public MeshCollider constructorCollider; //dconstructor

		public enum VoxelandFaceDir {top=0, bottom=1, front=2, back=3, left=4, right=5}; //y,z,x: left and right are on x axis
		
		#region vars Ambient
		
		/*
		[System.NonSerialized] public byte[] ambient;
		[System.NonSerialized] public bool[] ambientBool;
		[System.NonSerialized] public bool[] ambientExist;
		[System.NonSerialized] public int ambientSize;
		[System.NonSerialized] public int ambientHeight;
		[System.NonSerialized] public int ambientBottomPoint;
		[System.NonSerialized] public int ambientTopPoint;
		*/
		#endregion
		
		//in-progress marks
		/*
		public enum PartProgress { notCalculated=0, threadStarted=1, calculated=2, applied=3, dontChange=4 };
		[System.NonSerialized] public PartProgress terrainProgress = PartProgress.notCalculated;
		[System.NonSerialized] public PartProgress ambientProgress = PartProgress.notCalculated;
		[System.NonSerialized] public PartProgress constructorProgress = PartProgress.notCalculated;
		[System.NonSerialized] public PartProgress grassProgress = PartProgress.notCalculated;
		[System.NonSerialized] public PartProgress prefabsProgress = PartProgress.notCalculated;
		*/

		public enum Stage { gradual, calculateTerrain, applyTerrain, constructor, far, resetCollider, grassPrefabs, forceAll, forceAmbient, complete };
		public Stage stage;
		
	#region Standard functions

		public void Process ()
		{
			//if (stage == Stage.terrain) stage = Stage.forceAll;
			
			switch (stage)
			{
				//building frame-by-frame from scratch
				case Stage.gradual:			CalculateAmbient();									stage=Stage.calculateTerrain;		break;
				case Stage.calculateTerrain:CalculateTerrain(); 								stage=Stage.applyTerrain;	break;
				case Stage.applyTerrain:	ApplyTerrain(); ApplyAmbientToTerrain();			stage=Stage.far;	break;
				//case Stage.constructor:		BuildConstructor();	ApplyAmbientToConstructor();	stage=Stage.far;			break;
				case Stage.far:				if (land.far!=null) land.far.Build();				stage=Stage.resetCollider;	break;
				case Stage.resetCollider:	ResetCollider();									stage=Stage.grassPrefabs;	break;
				case Stage.grassPrefabs:	BuildGrass(); BuildPrefabs(); ApplyAmbientToGrass(); ApplyAmbientToPrefabs(); stage=Stage.complete;	break;
				
				//building at once from scratch
				case Stage.forceAll:

					CalculateAmbient();
					
					CalculateTerrain(); ApplyTerrain(); ApplyAmbientToTerrain();
					//BuildConstructor();	ApplyAmbientToConstructor();
					ResetCollider();

					BuildGrass(); ApplyAmbientToGrass(); 
					BuildPrefabs(); ApplyAmbientToPrefabs();

					stage = Stage.complete;
					break;

				//building ambient only
				case Stage.forceAmbient:
					CalculateAmbient();
					ApplyAmbient();
					stage = Stage.complete;
					break;

				case Stage.complete: return;
			}
		}
		
		static public Chunk CreateChunk (VoxelandTerrain land)
		{
			GameObject chunkObj = new GameObject("Chunk");
			chunkObj.transform.parent = land.transform;
			Chunk chunk= chunkObj.AddComponent<Chunk>();
			chunk.land = land;
			
			chunk.hiFilter = chunk.CreateFilter("HiResChunk");
			chunk.loFilter = chunk.CreateFilter("LoResChunk");
			chunk.grassFilter = chunk.CreateFilter("Grass");
			chunk.grassFilter.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; 
			//chunk.constructorFilter = chunk.CreateFilter("Constructor"); //dconstructor
			
			chunk.collision = chunk.loFilter.gameObject.AddComponent<MeshCollider>();
			chunk.collision.sharedMesh = chunk.loFilter.sharedMesh;
			
			//dconstructor
			//creating constructor collider separately - not smart. TODO make it's lod filter, same as terrain
			//chunk.constructorCollider = chunk.constructorFilter.gameObject.AddComponent<MeshCollider>(); 
			//chunk.constructorCollider.sharedMesh = new Mesh();
			//if (!land.saveMeshes) chunk.constructorCollider.sharedMesh.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

			#if UNITY_EDITOR
			//copy static flag
			UnityEditor.StaticEditorFlags flags = UnityEditor.GameObjectUtility.GetStaticEditorFlags(land.gameObject);
			UnityEditor.GameObjectUtility.SetStaticEditorFlags(chunkObj, flags);
			
			//hiding in hierarchy
			if (land.hideChunks) chunkObj.hideFlags = HideFlags.HideInHierarchy;
			#endif

			return chunk;
		}
		
		public MeshFilter CreateFilter (string name) //creates new object with filter and mesh
		{
			GameObject obj = new GameObject(name);
			obj.transform.parent = transform;
			obj.transform.localPosition = new Vector3(0,0,0);
			obj.transform.localScale = new Vector3(1,1,1);
			obj.layer = gameObject.layer;
			MeshFilter objFilter = obj.AddComponent<MeshFilter>();
			obj.AddComponent<MeshRenderer>();
			objFilter.sharedMesh = new Mesh ();
			
			//if (land==null) land = transform.parent.GetComponent<Voxeland>();
			//if (land!=null) obj.renderer.material = mat;
			
			#if UNITY_EDITOR
			//hiding wireframe
			UnityEditor.EditorUtility.SetSelectedWireframeHidden(objFilter.GetComponent<Renderer>(), land.hideWire);
			
			//copy static flag
			UnityEditor.StaticEditorFlags flags = UnityEditor.GameObjectUtility.GetStaticEditorFlags(land.gameObject);
			UnityEditor.GameObjectUtility.SetStaticEditorFlags(obj, flags);
			
			//hiding in hierarchy / notsaving
			if (land.hideChunks && !land.saveMeshes) obj.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
			else if (!land.saveMeshes) obj.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
			else if (land.hideChunks) obj.hideFlags = HideFlags.HideInHierarchy;

			//not saving mesh
			if (!land.saveMeshes) objFilter.sharedMesh.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

			#endif
			
			return objFilter;
		}

		public void Init (int x, int z)
		{
			if (transform == null) return;
			transform.localPosition = new Vector3(x*land.chunkSize, 0, z*land.chunkSize);
			transform.localScale = new Vector3(1,1,1);
			gameObject.layer = land.gameObject.layer;
			gameObject.SetActive(false);
			
			coord = new Coord(x,0,z);
			coordX = x;
			coordZ = z;
			offsetX = x*land.chunkSize; 
			offsetZ = z*land.chunkSize;
			size = land.chunkSize;
			
			#if UNITY_EDITOR
			//same procedures like in CreateFilter, but on chunk obj instead
			//hiding chunk objects
			//if (land.hideChunks) transform.hideFlags = HideFlags.HideInHierarchy;
			//transform.hideFlags = HideFlags.HideAndDontSave;
			//and DontSave is made with OnWillSaveAssets processor in Editor
			
			//copy static flag
			UnityEditor.StaticEditorFlags flags = UnityEditor.GameObjectUtility.GetStaticEditorFlags(land.gameObject);
			UnityEditor.GameObjectUtility.SetStaticEditorFlags(gameObject, flags);
			foreach (Transform subTfm in transform) UnityEditor.GameObjectUtility.SetStaticEditorFlags(subTfm.gameObject, flags);
			#endif

			//resetting progress
			stage = Stage.gradual;
		}
		
		public void Clear ()
		{
			hiFilter.sharedMesh.Clear();
			loFilter.sharedMesh.Clear();
			grassFilter.sharedMesh.Clear();
			
			stage = Stage.gradual;
		}
		
		public void SwitchLod (bool lod)
		{
			if (hiFilter == null) return;
			
			//switching or checking lod
			if (lod)
			{
				//turning hifilter off
				if (hiFilter.GetComponent<Renderer>().enabled) hiFilter.GetComponent<Renderer>().enabled = false;
				
				//turning lofilter on (no matter how it was disabled)
				if (loFilter.GetComponent<Renderer>().sharedMaterials.Length == 0) loFilter.GetComponent<Renderer>().sharedMaterial = hiFilter.GetComponent<Renderer>().sharedMaterial;
				if (!loFilter.GetComponent<Renderer>().enabled) loFilter.GetComponent<Renderer>().enabled = true;
				
			}
			
			//if main obj
			else
			{
				//turning hifilter on
				if (!hiFilter.GetComponent<Renderer>().enabled) hiFilter.GetComponent<Renderer>().enabled = true;
				
				//turning lofilter off
				if (land.lodWithMaterial) loFilter.GetComponent<Renderer>().sharedMaterials = new Material[0];
				else loFilter.GetComponent<Renderer>().enabled = false;
			}
		}

	#endregion
		
	
	#region Terrain
		
		[System.NonSerialized] public Face[] faces;
		[System.NonSerialized] public Vector3[] verts; //first go corners (to convert to lopoly quick), then sides, then invisible

		[System.NonSerialized] public int[] visibleFaceNums; //needed to get face by tri

		//probably outdated
		[System.NonSerialized] private int numVisibleVerts = 0;
		[System.NonSerialized] private int numColliderVisibleVerts = 0;
		//public MeshWrapper terrainMesh = new MeshWrapper();
		//MeshWrapper terrainCollider = new MeshWrapper();
		
		#region Static arrays
		//static readonly int[] dirToPosX = {0,0,1,-1,0,0};
		//static readonly int[] dirToPosY = {1,-1,0,0,0,0};
		//static readonly int[] dirToPosZ = {0,0,0,0,-1,1};
		
		//static readonly int[] opposite = {1,0,3,2,5,4};
		
		//static readonly int[] prewPoint = {7,0,1,2,3,4,5,6};
		//static readonly int[] nextPoint = {1,2,3,4,5,6,7,0};
		
		//should be /2f to get actual pos		
		//static readonly int[] vertPosesM = {2,2,2,1,0,0,0,1,1}; //main
		//static readonly int[] vertPosesF = {0,1,2,2,2,1,0,0,1}; //forward
		//static readonly int[] vertPosesI = {2,1,0,0,0,1,2,2,1}; //inverse
		// 0:F,1,M  1:I,0,M  2:1,M,F  3:0,M,I  4:M,F,1  5:M,I,0

												//dir0				  dir1				   dir2					dir3
		static readonly float[] sidePosesX = 	{0.5f,1.0f,0.5f,0.0f, 0.5f,0.0f,0.5f,1.0f, 1.0f,1.0f,1.0f,1.0f, 0.0f,0.0f,0.0f,0.0f, 1.0f,0.5f,0.0f,0.5f, 1.0f,0.5f,0.0f,0.5f};
		static readonly float[] sidePosesY = 	{1.0f,1.0f,1.0f,1.0f, 0.0f,0.0f,0.0f,0.0f, 1.0f,0.5f,0.0f,0.5f, 1.0f,0.5f,0.0f,0.5f, 0.5f,1.0f,0.5f,0.0f, 0.5f,0.0f,0.5f,1.0f};
		static readonly float[] sidePosesZ = 	{1.0f,0.5f,0.0f,0.5f, 1.0f,0.5f,0.0f,0.5f, 0.5f,1.0f,0.5f,0.0f, 0.5f,0.0f,0.5f,1.0f, 1.0f,1.0f,1.0f,1.0f, 0.0f,0.0f,0.0f,0.0f};
		
		static readonly float[] cornerPosesX =	{0.0f,1.0f,1.0f,0.0f, 1.0f,0.0f,0.0f,1.0f, 1.0f,1.0f,1.0f,1.0f, 0.0f,0.0f,0.0f,0.0f, 1.0f,1.0f,0.0f,0.0f, 1.0f,1.0f,0.0f,0.0f};
		static readonly float[] cornerPosesY =	{1.0f,1.0f,1.0f,1.0f, 0.0f,0.0f,0.0f,0.0f, 1.0f,1.0f,0.0f,0.0f, 1.0f,1.0f,0.0f,0.0f, 0.0f,1.0f,1.0f,0.0f, 1.0f,0.0f,0.0f,1.0f};
		static readonly float[] cornerPosesZ =	{1.0f,1.0f,0.0f,0.0f, 1.0f,1.0f,0.0f,0.0f, 0.0f,1.0f,1.0f,0.0f, 1.0f,0.0f,0.0f,1.0f, 1.0f,1.0f,1.0f,1.0f, 0.0f,0.0f,0.0f,0.0f};
		
		static readonly float[] centerPosesX =	{0.5f,0.5f,1.0f,0.0f,0.5f,0.5f};
		static readonly float[] centerPosesY =	{1.0f,0.0f,0.5f,0.5f,0.5f,0.5f};
		static readonly float[] centerPosesZ =	{0.5f,0.5f,0.5f,0.5f,1.0f,0.0f};
		
		//dir (side (neig) )
		//								dir0							dir1							dir2							dir3							dir4
		//								side1	side2  side3 side4		side1
		static readonly int[] neigDir = {4,0,5, 2,0,3, 5,0,4, 3,0,2, 	4,1,5, 3,1,2, 5,1,4, 2,1,3, 	0,2,1, 4,2,5, 1,2,0, 5,2,4, 	0,3,1, 5,3,4, 1,3,0, 4,3,5, 	2,4,3, 0,4,1, 3,4,2, 1,4,0, 	2,5,3, 1,5,0, 3,5,2, 0,5,1};
		static readonly int[] neigSide= {1,2,1, 0,3,2, 3,0,3, 0,1,2, 	3,2,3, 2,3,0, 1,0,1, 2,1,0, 	1,2,1, 0,3,2, 3,0,3, 0,1,2, 	3,2,3, 2,3,0, 1,0,1, 2,1,0, 	1,2,1, 0,3,2, 3,0,3, 0,1,2, 	3,2,3, 2,3,0, 1,0,1, 2,1,0};
		static readonly int[] neigX =	{0,0,0, 0,1,1, 0,0,0, 0,-1,-1,	0,0,0,0,-1,-1,0,0,0, 0,1,1, 	0,0,1, 0,0,1, 0,0,1, 0,0,1, 	0,0,-1,0,0,-1,0,0,-1,0,0,-1, 	0,1,1, 0,0,0,0,-1,-1,0,0,0, 	0,1,1, 0,0,0, 0,-1,-1,0,0,0};
		static readonly int[] neigY =   {0,0,1, 0,0,1, 0,0,1, 0,0,1,	0,0,-1,0,0,-1,0,0,-1,0,0,-1,	0,1,1, 0,0,0,0,-1,-1,0,0,0, 	0,1,1, 0,0,0,0,-1,-1,0,0,0, 	0,0,0, 0,1,1, 0,0,0, 0,-1,-1, 	0,0,0, 0,-1,-1, 0,0,0, 0,1,1};
		static readonly int[] neigZ =   {0,1,1, 0,0,0,0,-1,-1,0,0,0,	0,1,1, 0,0,0,0,-1,-1,0,0,0, 	0,0,0, 0,1,1, 0,0,0, 0,-1,-1,	0,0,0, 0,-1,-1,0,0,0, 0,1,1,	0,0,1, 0,0,1, 0,0,1, 0,0,1, 	0,0,-1,0,0,-1,0,0,-1,0,0,-1};
		
		//static public string[] shaderMainTexNames = new string[] {"_MainTex", "_MainTex2", "_MainTex3", "_MainTex4", "_MainTex5", "_MainTex6", "_MainTex7", "_MainTex8"};
		//static public string[] shaderBumpTexNames = new string[] {"_BumpMap", "_BumpMap2", "_BumpMap3", "_BumpMap4", "_BumpMap5", "_BumpMap6", "_BumpMap7", "_BumpMap8"};
		//static public string[] shaderChannelNames = new string[] { "_ch0","_ch1","_ch2","_ch3","_ch4","_ch5","_ch6","_ch7" };
		
		#endregion
	
		#region Face
		public struct Nodes<T>
		{
			public T a;
			public T b;
			public T c;
			public T d;

			public T this[int num]
			{
				get 
				{ 
					switch (num)
					{
						case -1:return d;
						case 0: return a;
						case 1: return b;
						case 2: return c;
						case 3: return d;
						case 4: return a;
						default: return a;
					}
				}
				set 
				{ 
					switch (num)
					{
						case -1:d = value; break;
						case 0: a = value; break;
						case 1: b = value; break;
						case 2: c = value; break;
						case 3: d = value; break;
						case 4: a = value; break;
					}
				}
			}
			
			public Nodes (T n0, T n1, T n2, T n3)
			{
				a=n0; b=n1; c=n2; d=n3;
			}
		}
		
		public struct Face
		{
			public int x; 
			public int y; 
			public int z; 
			public byte dir;
			
			public bool visible;

			public Nodes<int> cornerNums;
			public Nodes<int> sideNums;
			public int centerNum;
			
			public Nodes<int> neigFaceNums;
			public Nodes<int> neigSides;
			
			public Vector3 normal;
			//public Nodes<Vector3> normals;
			public float ambient;

			public byte type;
			public Nodes<byte> blendedMaterial; //how much each of mat-spaced types affect face texture
			
			public int this[int num]
			{
				get 
				{ 
					switch (num)
					{
						case 0: return cornerNums.a;
						case 1: return sideNums.a;
						case 2: return cornerNums.b;
						case 3: return sideNums.b;
						case 4: return cornerNums.c;
						case 5: return sideNums.c;
						case 6: return cornerNums.d;
						case 7: return sideNums.d;
						case 8: return centerNum;
						default: return centerNum;
					}
				}
				set 
				{ 
					switch (num)
					{
						case 0: cornerNums.a = value; break;
						case 1: sideNums.a = value; break;
						case 2: cornerNums.b = value; break;
						case 3: sideNums.b = value; break;
						case 4: cornerNums.c = value; break;
						case 5: sideNums.c = value; break;
						case 6: cornerNums.d = value; break;
						case 7: sideNums.d = value; break;
						case 8: centerNum = value; break;
					}
				}
			}
			
			/*
			public Face (byte x, byte y, byte z, byte dir, int faceNum) //faceNum is number of face in array of all faces, to create verts with unique nums
			{
				this.x=x; this.y=y; this.z=z; this.dir=dir;
				
				cornerNums = new Nodes(faceNum*9,   faceNum*9+2, faceNum*9+4, faceNum*9+6);
				sideNums   = new Nodes(faceNum*9+1, faceNum*9+3, faceNum*9+5, faceNum*9+7);
				centerNum = faceNum*9+8;

				neigFaceNums = new Nodes(-1,-1,-1,-1); //neigFaceNums[1]=neigFaceNums[2]=neigFaceNums[3] = -1;
				neigSides = new Nodes(-1,-1,-1,-1); //[0]=neigSides[1]=neigSides[2]=neigSides[3] = -1;
			}
			*/
		}
		#endregion
		
		#region Functions
		public Vector3 GetSmoothedCorner (int f, int c, Vector3[] verts)
		{
			int div = 1; Vector3 sum = verts[ faces[f].sideNums[c] ]; //this face side on the beginning
			int nextFace = f; int nextCorner = c;
			
			while (div < 8) //could not be more than 7 welded faces
			{
				//getting next face and next corner
				int temp = faces[nextFace].neigFaceNums[nextCorner]; //storing next face num to temporary value, we'll need original to get corner
				nextCorner = faces[nextFace].neigSides[nextCorner] + 1; //it's a next corner after side, so adding 1
				nextFace = temp;
				
				if (nextFace == -1 || nextFace == f) break; //if reached boundary
				
				sum += verts[ faces[nextFace].sideNums[nextCorner] ]; //adding side vert (with same number as corner vert)
				div++;
			}
			
			return sum/div;
		}
		#endregion
		
		public void CalculateTerrain (object stateInfo) { CalculateTerrain(); } //for multithreading
		public void CalculateTerrain ()
		{	
			if (land.profile) Profiler.BeginSample("CalculateTerrain");
			int counter = 0;
			
			#region Calculating top and bottom points and offset
			int topPoint = land.data.GetTopPoint(offsetX-land.terrainMargins, offsetZ-land.terrainMargins, offsetX+land.chunkSize+land.terrainMargins, offsetZ+land.chunkSize+land.terrainMargins)+1;
			int bottomPoint = Mathf.Max(1, land.data.GetBottomPoint(offsetX-land.terrainMargins, offsetZ-land.terrainMargins, offsetX+land.chunkSize+land.terrainMargins, offsetZ+land.chunkSize+land.terrainMargins, land.terrainExist)-2);
			int shortHeight = topPoint - bottomPoint;
			
			//Coord offset = new Coord(coord.x*land.chunkSize-land.terrainMargins, bottomPoint, coord.z*land.chunkSize-land.terrainMargins); //coords where real invisible faces start
			
			int fullSize = land.chunkSize + land.terrainMargins*2;
			#endregion
			
			#region emergency exit if chunk contains no blocks
			if (topPoint<=1) 
			{ 
				faces = null; 
				gameObject.SetActive(false);
				if (land.profile) Profiler.EndSample();
				return; 
			}
			#endregion
			
			#region Gathering exist matrix
			if (land.profile) Profiler.BeginSample ("Get Matrix");
			
			Matrix3<byte> matrix = new Matrix3<byte>(fullSize+2,shortHeight+2,fullSize+2); //taking large matrix with 1 block boundary

			//setting matrix absolute coordinates and filling it
			matrix.offsetX = offsetX - land.terrainMargins-1;
			matrix.offsetY = bottomPoint-1;
			matrix.offsetZ = offsetZ - land.terrainMargins-1;

			land.data.ToMatrix(matrix);

			//matrix to chunk coords
			matrix.offsetX -= offsetX;
			matrix.offsetZ -= offsetZ;

			//checking if there are out-of-range types, replacing them with 0
			for (int i=0; i<matrix.array.Length; i++) 
				if (matrix.array[i] >= land.types.Length)
					matrix.array[i] = 0;
			
			/*
			land.data.GetMatrix (matrix.array,  
				offsetX+matrix.offsetX, 				matrix.offsetY, 				offsetZ+matrix.offsetX,  
			    offsetX+matrix.offsetX+matrix.sizeX, 	matrix.offsetY+matrix.sizeY, 	offsetZ+matrix.offsetX+matrix.sizeZ);
			*/    

			
			//land.data.GetExistMatrix (matrix.array,  offsetX-1-land.terrainMargins, bottomPoint-1, offsetZ-1-land.terrainMargins,  offsetX+land.chunkSize+1+land.terrainMargins, topPoint+1, offsetZ+land.chunkSize+1+land.terrainMargins,land.terrainExist);
			if (land.profile) Profiler.EndSample ();
			#endregion
			
			#region Calculating Face Number, Gathering Coords matrix
			if (land.profile) Profiler.BeginSample ("Calc face num, gather coords");
			
			int faceCount = 0;
			Matrix4<short> faceCoords = new Matrix4<short>(fullSize, shortHeight, fullSize, 6); //please note that faceCoords matrix does not have 1-block boundaries
			faceCoords.offsetX = -land.terrainMargins;
			faceCoords.offsetY = bottomPoint;
			faceCoords.offsetZ = -land.terrainMargins;
			faceCoords.Reset(-1);
			
			//writing face matrix
			VoxelandBlockType type = null;
			for (int y=bottomPoint; y<topPoint; y++)
				for (int x=-land.terrainMargins; x<land.chunkSize+land.terrainMargins; x++)	
					for (int z=-land.terrainMargins; z<land.chunkSize+land.terrainMargins; z++)
				{
					//int i = (z+1)*matrix.sizeX*matrix.sizeY + (y+1)*matrix.sizeX + (x+1); //taking boundaries shift into account
					matrix.SetPos(x,y,z);
					
					if (land.types[matrix.array[matrix.pos]].filledTerrain)
					{
						type = land.types[matrix.nextX];
						if (!type.filledTerrain) { faceCoords[x,y,z,2] = (short)faceCount; faceCount++; }
						type = land.types[matrix.prevX];
						if (!type.filledTerrain) { faceCoords[x,y,z,3] = (short)faceCount; faceCount++; }
						type = land.types[matrix.nextY];
						if (!type.filledTerrain) { faceCoords[x,y,z,0] = (short)faceCount; faceCount++; }
						type = land.types[matrix.prevY];
						if (!type.filledTerrain) { faceCoords[x,y,z,1] = (short)faceCount; faceCount++; }
						type = land.types[matrix.nextZ];
						if (!type.filledTerrain) { faceCoords[x,y,z,4] = (short)faceCount; faceCount++; }
						type = land.types[matrix.prevZ];
						if (!type.filledTerrain) { faceCoords[x,y,z,5] = (short)faceCount; faceCount++; }
					}
				}

			if (land.profile) Profiler.EndSample ();
			#endregion

			#region Creating Faces
			if (land.profile) Profiler.BeginSample ("Creating faces");
			
			faces = new Face[faceCount]; //face 0 is an empty face

			for (int y=bottomPoint; y<topPoint; y++)
				for (int x=-land.terrainMargins; x<land.chunkSize+land.terrainMargins; x++)	
					for (int z=-land.terrainMargins; z<land.chunkSize+land.terrainMargins; z++)
						for (int dir=0; dir<6; dir++)
					{
						short f = faceCoords[x,y,z,dir];
						if (f != -1) //faces[faceNum] = new Face((byte)x,(byte)y,(byte)z, (byte)dir, faceNum);
						{
							faces[f].x=x;//+offset.x; 
							faces[f].y=y;//+offset.y; 
							faces[f].z=z;//+offset.z; 
							faces[f].dir=(byte)dir;
							faces[f].type = matrix[x,y,z];
							faces[f].visible = land.types[ faces[f].type ].filledTerrain;
							if (faces[f].x<0 || faces[f].z<0 || faces[f].x>=land.chunkSize || faces[f].z>=land.chunkSize) faces[f].visible = false;
							faces[f].cornerNums.a=f*9; faces[f].cornerNums.b=f*9+2; faces[f].cornerNums.c=f*9+4; faces[f].cornerNums.d=f*9+6;
							faces[f].sideNums.a=f*9+1; faces[f].sideNums.b=f*9+3; faces[f].sideNums.c=f*9+5; faces[f].sideNums.d=f*9+7;
							faces[f].centerNum = f*9+8;
							
							faces[f].neigFaceNums.a=faces[f].neigFaceNums.b=faces[f].neigFaceNums.c=faces[f].neigFaceNums.d= -1;
							faces[f].neigSides.a=faces[f].neigSides.b=faces[f].neigSides.c=faces[f].neigSides.d= -1;
						}
					}

			if (land.profile) Profiler.EndSample ();
			#endregion
			
			#region Generating array of visible faces
			//calculating vis face num
			int facesNum = 0;
			for (int f=0; f<faces.Length; f++) 
				if (faces[f].visible) facesNum++;
				
			//creating array
			visibleFaceNums = new int[facesNum];
			counter = 0;
			for (int f=0; f<faces.Length; f++) 
			{
				if (!faces[f].visible) continue;
				
				visibleFaceNums[counter] = f;
				counter++;
			}
			#endregion
			
			#region Finding Neig faces
			if (land.profile) Profiler.BeginSample ("Finding neigs");
			for (int f=0; f<faces.Length; f++)
			{
				for (int side=0; side<4; side++)
				{
					if (faces[f].neigFaceNums[side] != -1) continue; //already has neig
					
					for (int neig=0; neig<3; neig++)
					{
						int i = faces[f].dir*12 + side*3 + neig;
						
						if (!faceCoords.CheckInRange(faces[f].x+neigX[i], faces[f].y+neigY[i], faces[f].z+neigZ[i])) continue;
					 
						int faceNum = faceCoords[ faces[f].x+neigX[i], faces[f].y+neigY[i], faces[f].z+neigZ[i], neigDir[i] ];
						if (faceNum != -1) //if face exists
						{
							faces[f].neigFaceNums[side] = faceNum;
							faces[f].neigSides[side] = neigSide[i];
							break;
						}
					}
				}
				
			}

			if (land.profile) Profiler.EndSample ();
			#endregion
			
			
			#region Welding Verts
			if (land.profile) Profiler.BeginSample("Welding");
			
			bool[] processed = new bool[faces.Length*9];
			
			for (int f=0; f<faces.Length; f++)
			{
				//side verts
				for (int s=0; s<4; s++) 
				{
					//no need to add processing test there - it will only take a time
					if (faces[f].neigFaceNums[s] == -1) continue;
					faces[f].sideNums[s] = faces[faces[f].neigFaceNums[s]].sideNums[faces[f].neigSides[s]];
				}
				
				//corners
				for (int c=0; c<4; c++)
				{
					int cornerNum = faces[f].cornerNums[c];
					if (processed[cornerNum]) continue;
					
					int nextFace = f; int nextCorner = c;
					
					while(true)
					{
						//getting next face and next corner
						int temp = faces[nextFace].neigFaceNums[nextCorner]; //storing next face num to temporary value, we'll need original to get corner
						nextCorner = faces[nextFace].neigSides[nextCorner]+1;
						nextFace = temp;
						
						if (nextFace == -1) break; //if reached boundary
						faces[nextFace].cornerNums[nextCorner] = cornerNum;
						if (nextFace == f) { processed[cornerNum]=true; break; } //if returned to same face
					}	
				}
			}
	 
			if (land.profile) Profiler.EndSample();
			#endregion
			
			
			#region Creating List of used verts, calculating num verts (including numVisibleVerts and numColliderVisibleVerts)
			if (land.profile) Profiler.BeginSample("Used Verts");
			
			int numVerts = 0;
			
			//creating array of used nums
			int[] usedVerts = new int[faces.Length*9];
			for (int i=0; i<usedVerts.Length; i++) usedVerts[i] = -1;

			//processing corner verts first to create collider mesh in easy way
			for (int f=0; f<faces.Length; f++)
			{
				if (!faces[f].visible) continue; //processing only visible verts first
				
				for (int c=0; c<4; c++)
				{
					int num = faces[f].cornerNums[c];
					if (usedVerts[num] < 0) { usedVerts[num] = numVerts; numVerts++; }
				}
			}
			numColliderVisibleVerts = numVerts; //saving number of visible corners

			//then side and center verts
			for (int f=0; f<faces.Length; f++)
			{
				if (!faces[f].visible) continue;
				
				for (int c=0; c<4; c++)
				{
					int num = faces[f].sideNums[c];
					if (usedVerts[num] < 0) { usedVerts[num] = numVerts; numVerts++; }
				}
				
				usedVerts[ faces[f].centerNum ] = numVerts; numVerts++;
			}
			numVisibleVerts = numVerts; //saving num of all visible verts
			
			//and finally processing invisible verts
			for (int f=0; f<faces.Length; f++)
			{
				if (faces[f].visible) continue;
				
				for (int c=0; c<4; c++)
				{
					int num = faces[f].cornerNums[c]; if (usedVerts[num] < 0) { usedVerts[num] = numVerts; numVerts++; }
					num = faces[f].sideNums[c]; if (usedVerts[num] < 0) { usedVerts[num] = numVerts; numVerts++; }
				}
				usedVerts[ faces[f].centerNum ] = numVerts; numVerts++;
			}
			
			//replacing verts in faces with array nums
			for (int f=0; f<faces.Length; f++)
			{
				for (int c=0; c<4; c++) 
				{
					faces[f].cornerNums[c] = usedVerts[ faces[f].cornerNums[c] ];
					faces[f].sideNums[c] = usedVerts[ faces[f].sideNums[c] ];
				}
				faces[f].centerNum = usedVerts[ faces[f].centerNum ];
			}
			
			//int numVerts = faces.Length*9;
			if (land.profile) Profiler.EndSample ();
			#endregion
			
			
			#region Creating Verts
			if (land.profile) Profiler.BeginSample ("Creating verts");
			
			verts = new Vector3[numVerts];
			
			for (int f=0; f<faces.Length; f++)
				for (int n=0; n<4; n++)
				{
					int i = faces[f].dir*4 + n;
					verts[ faces[f].cornerNums[n] ] = new Vector3(
						faces[f].x + cornerPosesX[i],
						faces[f].y + cornerPosesY[i], 
						faces[f].z + cornerPosesZ[i]);
					
					verts[ faces[f].sideNums[n] ]   = new Vector3(
						faces[f].x + sidePosesX[i],   
						faces[f].y + sidePosesY[i],   
						faces[f].z + sidePosesZ[i]);
					
					verts[ faces[f].centerNum ] = new Vector3( //TODO: take it outta n loop
						faces[f].x + centerPosesX[faces[f].dir],   
						faces[f].y + centerPosesY[faces[f].dir],   
						faces[f].z + centerPosesZ[faces[f].dir]);
				}

			if (land.profile) Profiler.EndSample ();
			
			//random
			//for (int v=0; v<verts.Length; v++) verts[v] += new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.15f, 0.15f), Random.Range(-0.15f, 0.15f));
			
			/*
			for (int f=0; f<faces.Length; f++)
				for (int s=0; s<4; s++)
				{
					int neigFaceNum = faces[f].neigFaceNums[s];
					if (neigFaceNum == -1) continue;
					//verts[faces[f].sideNums[s]] = verts[ faces[neigFaceNum].centerNum ];
					
					verts[faces[f].sideNums[s]] = verts[ faces[neigFaceNum].sideNums[ faces[f].neigSides[s] ] ];
				}
			*/
			#endregion
			
			
			#region Relax 1
			if (land.profile) Profiler.BeginSample ("Relax 1");
			
			processed = new bool[verts.Length];
			bool[] boundary = new bool[verts.Length];
			
			//fixing boundary verts
			for (int f=0; f<faces.Length; f++)
				for (int c=0; c<4; c++)
					if (faces[f].neigFaceNums[c] == -1) //if no neig face - marking all 3 verts as boundary
					{
						boundary[ faces[f].cornerNums[c] ] = true;
						boundary[ faces[f].cornerNums[c+1] ] = true;
						boundary[ faces[f].sideNums[c] ] = true;
					}
			
			//
			
			/*
			//smoothing boundary corners first
			for (int f=0; f<faces.Length; f++)
			{
				for (int c=0; c<4; c++)
				{
					if (faces[f].neigFaceNums[c-1] != -1) continue;
					if (faces[f].neigFaceNums[c] == -1) continue;
					
					int cornerNum = faces[f].cornerNums[c];
					//if (processed[cornerNum]) continue;
					verts[cornerNum] = GetSmoothedCorner(f,c);
					processed[cornerNum] = true;
				}
			}
			*/
				
			//corners - smoothing on base level
			for (int f=0; f<faces.Length; f++)
			{
				for (int c=0; c<4; c++)
				{
					int cornerNum = faces[f].cornerNums[c];
					if (processed[cornerNum] || boundary[cornerNum]) continue;
					
					if (faces[f].neigFaceNums[c] == -1) continue;

					verts[cornerNum] = GetSmoothedCorner(f,c,verts)*2f - verts[cornerNum]; //smoothing on base verts only, so adding double relax vector (this equivalent to v+=(G-v)*2; )
					//verts[cornerNum] = GetSmoothedCorner(f,c); 
					
					processed[cornerNum] = true;
				}
			}
			
			for (int f=0; f<faces.Length; f++)
			{
				//sides - setting average value between corners
				for (int s=0; s<4; s++)
				{
					int sideNum = faces[f].sideNums[s];
					if (processed[sideNum] || boundary[sideNum]) continue;
					
					//if first iteration - setting verts on average pos (using relaxed array)
					verts[sideNum] = (verts[ faces[f].cornerNums[s] ] + verts[ faces[f].cornerNums[s+1] ]) / 2f;

					processed[sideNum] = true;
				}
				
				//center - between four sides
				verts[ faces[f].centerNum ] = (verts[ faces[f].sideNums.a ] + verts[ faces[f].sideNums.c ]) / 2f;
			}
			
			for (int v=0; v<processed.Length; v++) processed[v] = false;

			if (land.profile) Profiler.EndSample ();
			#endregion
			
			#region Relax 2
			if (land.profile) Profiler.BeginSample ("Relax 2");

			Vector3[] relaxed = new Vector3[verts.Length];

			//corners - smoothing on base level
			for (int f=0; f<faces.Length; f++)
			{
				for (int c=0; c<4; c++)
				{
					int cornerNum = faces[f].cornerNums[c];
					if (processed[cornerNum]) continue;
					if (boundary[cornerNum]) { relaxed[cornerNum] = verts[cornerNum]; processed[cornerNum] = true; continue; }
					
					relaxed[cornerNum] = GetSmoothedCorner(f,c,verts); //smoothing as usual

					processed[cornerNum] = true;
				}
			}
			
			for (int f=0; f<faces.Length; f++)
			{
				//sides - setting average value between corners
				for (int s=0; s<4; s++)
				{
					int sideNum = faces[f].sideNums[s];
					if (processed[sideNum]) continue;
					if (boundary[sideNum]) { relaxed[sideNum] = verts[sideNum]; processed[sideNum] = true; continue; }
					
					else relaxed[sideNum] = (verts[ faces[f].cornerNums[s] ] + verts[ faces[f].cornerNums[s+1] ] +
							verts[ faces[f].centerNum ] + verts[ faces[faces[f].neigFaceNums[s] ].centerNum]) / 4f;
					
					processed[sideNum] = true;
				}
				
				//center - between four sides
				relaxed[ faces[f].centerNum ] = (verts[ faces[f].sideNums.a ] + verts[ faces[f].sideNums.c ] +
				                                 verts[ faces[f].sideNums.b ] + verts[ faces[f].sideNums.d ]) / 4f;
			}
		
			verts = relaxed;
			
			if (land.profile) Profiler.EndSample ();
			#endregion

			#region Types
			if (land.profile) Profiler.BeginSample ("Types");

				//calculating array of types
				int[] typeToChannel = new int[land.types.Length]; //[0,1,2,3,4,0,0,0]
				counter = 0; 
				for (int t=0; t<land.types.Length; t++) 
				{
					if (land.types[t].filledTerrain) { typeToChannel[t]=counter; counter++; }
					else typeToChannel[t] = -1;
				}

				//setting face blend values
				for (int f=0; f<faces.Length; f++)
				{
					//writing this face type
					faces[f].blendedMaterial[ typeToChannel[faces[f].type] ] = 1; //-1 as type count starts from 1

					//adding neig faces in 2-block range
					for (int n=0; n<4; n++)
					{
						int faceNum = faces[f].neigFaceNums[n];
						if (faceNum<0) continue;
						faces[f].blendedMaterial[ typeToChannel[faces[faceNum].type] ] ++;

						for (int nn=0; nn<4; nn++)
						{
							faceNum = faces[faces[f].neigFaceNums[n] ].neigFaceNums[nn]; //TODO: do a normal recursion, bring iterations num to settings
							if (faceNum<0) continue;
							faces[f].blendedMaterial[ typeToChannel[faces[faceNum].type] ] ++;
						}
					}

					//improving opacity if it is a single block
					int neigsOfSameType = 0;
					for (int n=0; n<4; n++)
					{
						int neigFaceNum = faces[f].neigFaceNums[n];
						if (neigFaceNum<0) continue; //end of the terrain
						if (faces[f].type == faces[neigFaceNum].type)  //if same type
						{
							if (faces[f].x == faces[neigFaceNum].x &&
								faces[f].y == faces[neigFaceNum].y &&
								faces[f].z == faces[neigFaceNum].z) continue; //same block disregarded
							neigsOfSameType++;
						}
					}
					if (neigsOfSameType==0) faces[f].blendedMaterial[ typeToChannel[faces[f].type] ] += 100;
					//else if (neigsOfSameType == 1) faces[f].blendedMaterial[ typeToChannel[faces[f].type] ] *= 2;
				}

			if (land.profile) Profiler.EndSample ();
			#endregion

			if (land.profile) Profiler.EndSample();
		}
		
		public void ApplyTerrain ()
		{
			if (land.profile) Profiler.BeginSample("ApplyTerrain");
			//old: 1.2 Mb, 40 ms
			
			#region Hiding wireframe
			#if UNITY_EDITOR
			if (land.hideWire)
			{
				UnityEditor.EditorUtility.SetSelectedWireframeHidden(hiFilter.GetComponent<Renderer>(), true);
				UnityEditor.EditorUtility.SetSelectedWireframeHidden(loFilter.GetComponent<Renderer>(), true); 
			}
			#endif
			#endregion

			#region Exit if no faces
			if (faces == null || faces.Length == 0)
			{
				hiFilter.sharedMesh.Clear();
				loFilter.sharedMesh.Clear();
				if (land.profile) Profiler.EndSample();
				return;
			}
			#endregion
			
			#region Mesh and collider Visible Verts
			if (land.profile) Profiler.BeginSample ("Mesh Visible Verts");

				Vector3[] vertsHi = new Vector3[numVisibleVerts];
				for (int i=0; i<numVisibleVerts; i++) vertsHi[i] = verts[i];
			
				Vector3[] vertsLo = new Vector3[numColliderVisibleVerts];
				for (int i=0; i<numColliderVisibleVerts; i++) vertsLo[i] = verts[i];

			if (land.profile) Profiler.EndSample ();
			#endregion

			#region Normals
			if (land.profile) Profiler.BeginSample ("Normals");
			
				Vector3[] normalsHi = new Vector3[vertsHi.Length];
				bool[] processed = new bool[vertsHi.Length];
			
				//gathering face normals
				if (land.profile) Profiler.BeginSample ("Gathering");
				for (int f=0; f<faces.Length; f++)
				{
					//calculating centeral face normal
					Vector3 v0 = verts[faces[f].sideNums.c] - verts[faces[f].sideNums.a];
					Vector3 v1 = verts[faces[f].sideNums.d] - verts[faces[f].sideNums.b];
				
					faces[f].normal = new Vector3(v0.y*v1.z - v0.z*v1.y, v0.z*v1.x - v0.x*v1.z, v0.x*v1.y - v0.y*v1.x).normalized;
				}
				if (land.profile) Profiler.EndSample ();
			
				//setting verts normals
				if (land.profile) Profiler.BeginSample ("Setting");
				for (int f=0; f<faces.Length; f++)
				{
					if (!faces[f].visible) continue;
					int num = 0;
				
					//corners
					for (int c=0; c<4; c++)
					{
						num = faces[f].cornerNums[c];
						if (processed[num]) continue;
					
						int div = 1; Vector3 sum = faces[f].normal;//s[c]; //this face normal
						int nextFace = f; int nextCorner = c;
					
						while (div < 8) //could not be more than 7 welded faces
						{
							//getting next face and next corner
							int temp = faces[nextFace].neigFaceNums[nextCorner]; //storing next face num to temporary value, we'll need original to get corner
							nextCorner = faces[nextFace].neigSides[nextCorner] + 1; //it's a next corner after side, so adding 1
							nextFace = temp;
						
							if (nextFace == -1 || nextFace == f) break; //if reached boundary
						
							//sum += faces[nextFace].normals[nextCorner];
							sum += faces[nextFace].normal;
							div++;
						}
					
						normalsHi[num] = (sum/div).normalized;
						processed[num] = true;
					}
				
					//sides
					for (int s=0; s<4; s++)
					{
						num = faces[f].sideNums[s];
						if (processed[num]) continue;
						if (faces[f].neigFaceNums[s] < 0) { normalsHi[num] = faces[f].normal; continue; }
					
						//normalsHi[num] = (faces[f].normals[s] + faces[f].normals[s+1] +
						//	faces[ faces[f].neigFaceNums[s] ].normals[ faces[f].neigSides[s] ] +
						//	faces[ faces[f].neigFaceNums[s] ].normals[ faces[f].neigSides[s+1] ]) / 4f;
						normalsHi[num] = ((faces[f].normal + faces[ faces[f].neigFaceNums[s] ].normal) / 2f).normalized;
					
						processed[num] = true;
					}
				
					//center
					//normalsHi[faces[f].centerNum] = (faces[f].normals.a + faces[f].normals.b + faces[f].normals.c + faces[f].normals.d) /4f;
					normalsHi[faces[f].centerNum] = faces[f].normal;
				}
				if (land.profile) Profiler.EndSample ();
			
				//saving normals to lopoly
				Vector3[] normalsLo = new Vector3[numColliderVisibleVerts];
				for (int i=0; i<numColliderVisibleVerts; i++) normalsLo[i] = normalsHi[i];
			
			if (land.profile) Profiler.EndSample ();
			#endregion
			
			#region Types and materials
			if (land.profile) Profiler.BeginSample ("Material and vertex data");

				//calculating array of types
				//TODO bring this fn somewhere (to VoxelandTerrain) to avoid calculation 2 types per chunk
				int[] typeToChannel = new int[land.types.Length]; //[0,1,2,3,4,0,0,0]
				int counter = 0; 
				for (int t=0; t<land.types.Length; t++) 
				{
					if (land.types[t].filledTerrain) { typeToChannel[t]=counter; counter++; }
					else typeToChannel[t] = -1;
				}

				//declaring arrays
				Vector2[] uvsHi = null; Vector2[] uv1Hi = null; Vector2[] uvsLo = null; Vector2[] uv1Lo = null; 
				Color[] colors=null; Color[] colorsLo=null;

				#region Standard
				if (!land.rtpCompatible)
				{
					//setting uvs (hi)
					uvsHi = new Vector2[vertsHi.Length];
					uv1Hi = new Vector2[vertsHi.Length];
				
					for (int f=0; f<faces.Length; f++)
					{
						for (int v=0; v<9; v++)
						{
							if (faces[f][v] >= uvsHi.Length) continue; //do not skip invisible faces, but skip verts out of range

							uvsHi[faces[f][v]].x += faces[f].blendedMaterial.a;
							uvsHi[faces[f][v]].y += faces[f].blendedMaterial.b;
							uv1Hi[faces[f][v]].x += faces[f].blendedMaterial.c;
							uv1Hi[faces[f][v]].y += faces[f].blendedMaterial.d;
						}
					}

					//equalizing uvs
					for (int v=0; v<uvsHi.Length; v++)
					{
						float sum = uvsHi[v].x + uvsHi[v].y + uv1Hi[v].x + uv1Hi[v].y;
						uvsHi[v] = uvsHi[v]/sum; uv1Hi[v] = uv1Hi[v]/sum;
					}

					//baking uvs to lopoly
					uvsLo = new Vector2[numColliderVisibleVerts];
					for (int i=0; i<numColliderVisibleVerts; i++) uvsLo[i] = uvsHi[i];

					uv1Lo = new Vector2[numColliderVisibleVerts];
					for (int i=0; i<numColliderVisibleVerts; i++) uv1Lo[i] = uv1Hi[i];
			
					if (land.terrainMaterial != null)
					{
						//setting material
						Material material = land.terrainMaterial;
						hiFilter.GetComponent<Renderer>().sharedMaterial = material;
						loFilter.GetComponent<Renderer>().sharedMaterial = material;

						//MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();

						int defaultType = 0;
						for(int i=0; i<typeToChannel.Length; i++) 
							if (typeToChannel[i] != 0) defaultType = i;

						for(int i=0; i<4; i++)
						{
							string propPostfix = i==0 ? "" : (i+1).ToString();
						
							int type = defaultType;
							for (int j=0; j<typeToChannel.Length; j++) 
								if (typeToChannel[j]==i) type = j;

							material.SetColor("_Color"+propPostfix, land.types[type].color);
							if (land.types[type].texture != null) material.SetTexture("_MainTex"+propPostfix, land.types[type].texture);
							if (land.types[type].bumpTexture != null) material.SetTexture("_BumpMap"+propPostfix, land.types[type].bumpTexture);
							if (land.types[type].specGlossMap != null) material.SetTexture("_SpecGlossMap"+propPostfix, land.types[type].specGlossMap);
							material.SetColor("_Specular"+propPostfix, land.types[type].specular);
							material.SetFloat("_Tile"+propPostfix, land.types[type].tile);

							//using material props instead of material
							//materialPropertyBlock.AddColor("_Color"+propPostfix, land.types[type].color);
							//if (land.types[type].texture != null) materialPropertyBlock.AddTexture("_MainTex"+propPostfix, land.types[type].texture);
							//if (land.types[type].bumpTexture != null) materialPropertyBlock.AddTexture("_BumpMap"+propPostfix, land.types[type].bumpTexture);
							//if (land.types[type].specGlossMap != null) materialPropertyBlock.AddTexture("_SpecGlossMap"+propPostfix, land.types[type].specGlossMap);
							//materialPropertyBlock.AddColor("_Specular"+propPostfix, land.types[type].specular);
							//materialPropertyBlock.AddFloat("_Tile"+propPostfix, land.types[type].tile);
						}

						//hiFilter.GetComponent<Renderer>().SetPropertyBlock(materialPropertyBlock);
						//loFilter.GetComponent<Renderer>().SetPropertyBlock(materialPropertyBlock);
					}
				}
				#endregion

				#region RTP
				else
				{
					colors = new Color[vertsHi.Length];
					for (int f=0; f<faces.Length; f++)
					{
						for (int v=0; v<9; v++)
						{
							if (faces[f][v] >= colors.Length) continue; //do not skip invisible faces, but skip verts out of range

							colors[faces[f][v]].r += faces[f].blendedMaterial.a;
							colors[faces[f][v]].g += faces[f].blendedMaterial.b;
							colors[faces[f][v]].b += faces[f].blendedMaterial.c;
							colors[faces[f][v]].a += faces[f].blendedMaterial.d;
						}
					}

					//equalizing uvs
					for (int v=0; v<colors.Length; v++)
					{
						float sum = colors[v].r + colors[v].g + colors[v].b + colors[v].a;
						colors[v] = colors[v]/sum;
					}

					//baking uvs to lopoly
					colorsLo = new Color[numColliderVisibleVerts];
					for (int i=0; i<numColliderVisibleVerts; i++) colorsLo[i] = colors[i];
			
					if (land.terrainMaterial != null)
					{
						//setting material
						Material material = land.terrainMaterial;
						hiFilter.GetComponent<Renderer>().sharedMaterial = material;
						loFilter.GetComponent<Renderer>().sharedMaterial = material; 
					}
				}
				#endregion


			if (land.profile) Profiler.EndSample ();
			#endregion

			#region Lowing down lod boundary verts
			if (land.profile) Profiler.BeginSample ("Lod Boundary");
			
				float min = 0.5f;
				float max = land.chunkSize-0.5f;
			
				for (int v=0; v<vertsLo.Length; v++)
				{
					Vector3 vert = vertsLo[v];
				
					if (vert.x < min || vert.x > max) 
						vertsLo[v] -= new Vector3(0, normalsLo[v].y, normalsLo[v].z) / 8f;
					
					if (vert.z < min || vert.z > max)
						vertsLo[v] -= new Vector3(normalsLo[v].x, normalsLo[v].y, 0) / 8f;
				}
			
			if (land.profile) Profiler.EndSample ();
			#endregion

			#region Creating Tris
			if (land.profile) Profiler.BeginSample ("Creating tris");
			
				//using visibleFaceNums array (the compliance list) to determine tri count
				int[] trisHi = new int[visibleFaceNums.Length*8*3];
				int[] trisLo = new int[visibleFaceNums.Length*2*3];
			
				counter = 0;
				for (int f=0; f<faces.Length; f++)
				{
					if (!faces[f].visible) continue;
				
					//mesh (hi)
					for (int s=0; s<4; s++)
					{
						//00-S3-C0, 00-C0-S0
						int j = counter*24 + s*6;
						trisHi[j] = trisHi[j+3] = faces[f].centerNum;
						trisHi[j+1] = faces[f].sideNums[s-1];
						trisHi[j+2] = trisHi[j+4] = faces[f].cornerNums[s];
						trisHi[j+5] = faces[f].sideNums[s];
					}
				
					//collider (lo)
					trisLo[counter*6] = faces[f].cornerNums.a;   trisLo[counter*6+1] = faces[f].cornerNums.b; trisLo[counter*6+2] = faces[f].cornerNums.d;
					trisLo[counter*6+3] = faces[f].cornerNums.c; trisLo[counter*6+4] = faces[f].cornerNums.d; trisLo[counter*6+5] = faces[f].cornerNums.b;
				
					counter++;
				}
			
			if (land.profile) Profiler.EndSample ();
			#endregion
			
			#region Applying

				gameObject.SetActive(true); //remove after chunks will use object pool

				hiFilter.sharedMesh.Clear();
				hiFilter.sharedMesh.vertices = vertsHi; hiFilter.sharedMesh.normals = normalsHi;
				if (!land.rtpCompatible) hiFilter.sharedMesh.uv = uvsHi; hiFilter.sharedMesh.uv2 = uv1Hi;
				hiFilter.sharedMesh.triangles = trisHi;
				if (land.rtpCompatible) hiFilter.sharedMesh.colors = colors;
				else if (!land.ambient) 
				{
					colors = hiFilter.sharedMesh.colors;
					for (int v=0; v<colors.Length; v++) colors[v] = new Color(0.5f, 0.5f, 0.5f, 1f);
					hiFilter.sharedMesh.colors = colors;
				}
				//hiFilter.sharedMesh.colors = new Color[hiFilter.sharedMesh.vertices.Length]; //otherwise it will create random color array


				loFilter.sharedMesh.Clear();
				loFilter.sharedMesh.vertices = vertsLo; loFilter.sharedMesh.normals = normalsLo;
				if (!land.rtpCompatible) loFilter.sharedMesh.uv = uvsLo; loFilter.sharedMesh.uv2 = uv1Lo;
				loFilter.sharedMesh.triangles = trisLo;
				if (land.rtpCompatible) loFilter.sharedMesh.colors = colorsLo;
				else if (!land.ambient) 
				{
					colors = loFilter.sharedMesh.colors;
					for (int v=0; v<colors.Length; v++) colors[v] = new Color(0.5f, 0.5f, 0.5f, 1f);
					loFilter.sharedMesh.colors = colors;
				}
				//loFilter.sharedMesh.colors = new Color[loFilter.sharedMesh.vertices.Length];


				//creating tangents - to avoid an error "Shader wants tangents". Shader does not require them, but Unity thinks it is. Even an empty surface shader.
				//hiFilter.sharedMesh.tangents = new Vector4[vertsHi.Length];
				//loFilter.sharedMesh.tangents = new Vector4[vertsLo.Length];

			#endregion
			
			#region RecalculateNormals (for test purposes)
			if (land.profile) Profiler.BeginSample ("RecalculateNormals");
			//hiFilter.sharedMesh.RecalculateNormals();
			//loFilter.sharedMesh.RecalculateNormals();
			if (land.profile) Profiler.EndSample ();
			#endregion

			#if UNITY_EDITOR
			if (land.generateLightmaps) UnityEditor.Unwrapping.GenerateSecondaryUVSet(hiFilter.sharedMesh);
			#endif

			if (land.profile) Profiler.EndSample();
		}

		public void ResetCollider()
		{
			if (land.profile) Profiler.BeginSample ("Reset Collider");

			Mesh colliderMesh = collision.sharedMesh;
			collision.sharedMesh = null;
			collision.sharedMesh = colliderMesh;

			if (land.profile) Profiler.EndSample ();
		}
		
	#endregion
	

	#region Constructor, Grass, Prefabs	
		
		public void  BuildGrass ()
		{
			if (land.profile) Profiler.BeginSample("BuildGrass");
			
			if (faces == null) return;

			#region Getting grass types array, Calculating grass number
				
				byte[] typeNums = null; //per-face
				int vertNum = 0; int[] triNums = null; //per-type;
			
				for (int f=0; f<faces.Length; f++) 
				{
					if (!faces[f].visible) continue;
					if (!land.types[ faces[f].type ].grass) continue;
					if (faces[f].normal.y<0.7f) continue;
				
					Face face = faces[f];
				
					byte typeNum = land.data.GetGrass(face.x+offsetX, face.z+offsetZ);
					if (typeNum == 0 ) continue;
					if (typeNum >= land.grass.Length) Debug.Log(typeNum + ": " + face.x + " " + face.z);
					VoxelandGrassType type = land.grass[typeNum]; 
					if (type.sourceMesh == null) continue;
				
					//creating arrays if grass detected
					if (triNums == null) triNums = new int[land.grass.Length];
					if (typeNums == null) typeNums = new byte[faces.Length];

					//loading grass mesh wrappers from source mesh
					if (type.meshes == null) 
					{
						type.meshes = new MeshWrapper[4];
						for (int i=0; i<4; i++)
						{
							type.meshes[i] = new MeshWrapper();
							type.meshes[i].ReadMesh(type.sourceMesh);
							type.meshes[i].RotateMirror(i*90, false);
						}
					}

					//adding to types array
					typeNums[f] = typeNum;
					
					//calculating vert and tri num
					vertNum += type.meshes[0].verts.Length;
					triNums[typeNum] += type.meshes[0].subs[0].tris.Length;

					/* //debug
					int cx = face.x+offsetX; int cz = face.z+offsetZ;
					int areaNum = land.data.GetAreaNum(cx,cz);
					int areaSize = Data.Area.areaSize;
					int areaOffsetX = land.data.areas[areaNum].offsetX;
					int columnNum = areaSize*areaSize + cx-areaOffsetX;
					Debug.Log("Grass: " + cx + "," + cz + " area: " + areaNum 
						+ " column:" + columnNum); 
					Debug.Log("PreTest: " + land.data.areas[areaNum].columns[columnNum].list.Count);
					for (int i=0; i<land.data.areas[areaNum].columns[columnNum].list.Count;i+=2)
					{
						Debug.Log(land.data.areas[areaNum].columns[columnNum].list[i] + " " + land.data.areas[areaNum].columns[columnNum].list[i+1]);
					}*/
				}
			#endregion

			#region Empty exit
				if (vertNum == 0) 
				{
					if (grassFilter.gameObject.activeSelf) grassFilter.gameObject.SetActive(false);
					grassFilter.sharedMesh.Clear();
					if (land.profile) Profiler.EndSample();
					return; 
				}
			#endregion
			
			#region Mesh

				MeshWrapper mesh = new MeshWrapper(vertNum, triNums, useNormals:true, useUvs:true, useUv1:true, useColors:true);

				for (int f=0; f<faces.Length; f++) 
				{
					if (typeNums[f] == 0) continue;

					Face face = faces[f];
					VoxelandGrassType type = land.grass[typeNums[f]];

					int randomNum = (int)Mathf.Repeat(face.x*20 + face.y*10 + face.z*5 + face.dir, 998);
					MeshWrapper grassSub = type.meshes[ Mathf.FloorToInt(VoxelandTerrain.random[randomNum]*4) ];

					mesh.AppendWithFFD(grassSub, typeNums[f], 
						verts[face.cornerNums.a], verts[face.cornerNums.b], verts[face.cornerNums.c], verts[face.cornerNums.d],
						type.normalsFromTerrain ? face.normal : Vector3.zero);

					//setting grass tint
					Color tint = land.types[face.type].grassTint;
					//TODO make a smooth blend based on blendedMaterial
					for (int v=mesh.vertCounter-grassSub.verts.Length; v<mesh.vertCounter; v++)
						mesh.colors[v] = tint;
				}

				//apply
				mesh.ApplyTo(grassFilter.sharedMesh);
				if (!grassFilter.gameObject.activeSelf) grassFilter.gameObject.SetActive(true);

				//materials
				Material[] allMats = new Material[land.grass.Length];
				for (int i=0; i<allMats.Length; i++) 
						allMats[i] = land.grass[i].material;
				grassFilter.GetComponent<Renderer>().sharedMaterials = mesh.GetMaterials(allMats);

			#endregion

			if (land.profile) Profiler.EndSample();
		}

		[System.NonSerialized] public Dictionary<int,Transform> prefabTransforms;
		[System.NonSerialized] public Dictionary<int,Transform> prefabSources;
		[System.NonSerialized] public Dictionary<int,bool> prefabConfirmed;

		public void BuildPrefabs ()
		{
			//creating ref exist array
			bool[] refExist = new bool[land.types.Length];
			for(int i=0;i< land.types.Length;i++) 
			{
				if (land.types[i].prefabs==null || land.types[i].prefabs.Length==0 || land.types[i].prefabs[0]==null) continue;
				refExist[i] = land.types[i].filledPrefabs;
			}

			//clearing confirmed vals
			if (prefabConfirmed != null)
			{
				prefabConfirmed.Clear();
				//foreach ( int key in prefabTransforms.Keys ) prefabConfirmed[key] = false;
			}
			
			//adding prefabs
			for (int x=0; x<size; x++)	
				for (int z=0; z<size; z++)
				{
					if (!land.data.HasBlock(x+offsetX,z+offsetZ,refExist)) continue;

					//working directly with column
					Data.Column column = land.data.ReadColumn(x+offsetX,z+offsetZ);

					int y = 0;
					int listCount = column.count;
					for(int i=0; i<listCount; i++)
					{ 
						//defining type and type height
						byte curType = column.GetType(i);
						int curHeight = column.GetLevel(i);
						
						if (curType>=refExist.Length) curType = 0;

						if (refExist[curType]) //note that types with prefabs==null or ==0 are not included in refExist
						{
							//placing several (or one) prefabs in column
							for (int j=0; j<curHeight; j++)
							{
								//creating dictionary if it does not exists
								if (prefabTransforms==null) prefabTransforms = new Dictionary<int,Transform>();
								if (prefabSources==null) prefabSources = new Dictionary<int,Transform>();
								if (prefabConfirmed==null) prefabConfirmed = new Dictionary<int,bool>();
								
								//finding suitable pool
								float random = Noise.Random(x,y,z);
								int prefabNum = Mathf.RoundToInt((land.types[curType].prefabs.Length-1) * random);
								Transform prefabSource = land.types[curType].prefabs[prefabNum];

								//random rotation
								random = Noise.Random(x,y,z,0);
								Vector3 prefabRotationEuler = land.types[curType].prefabs[prefabNum].rotation.eulerAngles;
								Quaternion rotation = Quaternion.Euler(prefabRotationEuler.x, random*360, prefabRotationEuler.z);

								//instantiating (if needed)
								Transform tfm = null;
								int coord = x*1000000 + y*1000 + z;
								
								if (prefabTransforms.ContainsKey(coord) && prefabSources[coord] == prefabSource && prefabTransforms[coord] != null)
									{ tfm = prefabTransforms[coord].transform; 
									prefabConfirmed[coord] = true; 
									}
								else
								{
									tfm = Instantiate(prefabSource, Vector3.zero, rotation) as Transform;
									if (prefabTransforms.ContainsKey(coord) && prefabTransforms[coord] != null) 
										DestroyImmediate(prefabTransforms[coord].gameObject); //removing old object if any
									tfm.parent = transform;
									tfm.localPosition = new Vector3(x+0.5f, y, z+0.5f);
									tfm.localScale = Vector3.one;

									prefabTransforms[coord] = tfm;
									prefabSources[coord] = prefabSource;
									prefabConfirmed[coord] = true;
								}

								//apply material
								MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
								Renderer[] renderers = tfm.GetComponentsInChildren<Renderer>();
								for (int r=0; r<renderers.Length; r++)
								{
									renderers[r].GetPropertyBlock(propBlock);
									//if (propBlock == null) { propBlock = new MaterialPropertyBlock(); renderers[r].SetPropertyBlock(propBlock); }
									if (ambient!=null) //if ambient was calculated
										propBlock.AddFloat("_AmbientPower", ambient[x,y,z]/250f);
									renderers[r].SetPropertyBlock(propBlock);

									//hiding wireframe
									#if UNITY_EDITOR
									UnityEditor.EditorUtility.SetSelectedWireframeHidden(renderers[r], land.hideWire);
									#endif
								}
								
							}
						}
						y += curHeight;
					}
				}

			//removing unconfirmed prefabs
			if (prefabTransforms != null)
			foreach( KeyValuePair<int,Transform> entry in prefabTransforms )
			{
				if (entry.Value==null) continue;

				if (!prefabConfirmed.ContainsKey(entry.Key) ||
					prefabConfirmed[entry.Key] == false)
						DestroyImmediate(entry.Value.gameObject); 
			}
		}
		
	#endregion


	#region Ambient
		
		[System.NonSerialized] public Matrix3<byte> ambient;
		
		public void CalculateAmbient (object stateInfo) { CalculateAmbient(); }
		public void CalculateAmbient ()
		{
			if (!land.ambient) return;
			
			if (land.profile) Profiler.BeginSample("CalculateAmbient");
			
			//initializing repeated vars
			byte max = 250;

			//creating ref exist array
			bool[] refExist = new bool[land.types.Length];
			for (int i=0; i< land.types.Length; i++) refExist[i] = land.types[i].filledAmbient;
			
			#region Finding Top and Bottom
			if (land.profile) Profiler.BeginSample ("Top and Bottom");

			//ambientSize= size + land.ambientMargins*2;
			int ambientTopPoint = land.data.GetTopPoint(
				offsetX-land.ambientMargins, 
				offsetZ-land.ambientMargins,
				offsetX+land.chunkSize+land.ambientMargins, 
				offsetZ+land.chunkSize+land.ambientMargins) +2; //+1 to make non-inclusive, and +1 to place top-light
			int ambientBottomPoint = land.data.GetBottomPoint(
				offsetX-land.ambientMargins, 
				offsetZ-land.ambientMargins,
				offsetX+land.chunkSize+land.ambientMargins, 
				offsetZ+land.chunkSize+land.ambientMargins, 
				refExist)-3; //-2 to add 1 layer of fill, -1 for make array inclusive
			
			if (ambientBottomPoint<0) ambientBottomPoint=0;
			if (ambientTopPoint-ambientBottomPoint <= 2) { if (ambient != null) ambient.Reset(0); if (land.profile) Profiler.EndSample (); return; }
			
			if (land.profile) Profiler.EndSample ();
			#endregion
			
			#region Preparing Array
			if (land.profile) Profiler.BeginSample ("Preparing Array");
			
			//re-creating matrix if size do not match (usually when ambientHeight changed)
			if (ambient == null ||
				ambient.sizeX != land.chunkSize + land.ambientMargins*2 ||
			    ambient.sizeY != ambientTopPoint-ambientBottomPoint ||
			    ambient.sizeZ != land.chunkSize + land.ambientMargins*2 ||
				land.profile) //for benchmarking
			{
				ambient = new Matrix3<byte>(
					land.chunkSize + land.ambientMargins*2, 
					ambientTopPoint-ambientBottomPoint, 
					land.chunkSize + land.ambientMargins*2);
			}
			//else ambient.Reset(0);
			
			if (land.profile) Profiler.EndSample ();
			#endregion
			
			#region Get matrix
			if (land.profile) Profiler.BeginSample ("Get matrix");

			//setting worldpos offset
			ambient.offsetX = offsetX-land.ambientMargins;
			ambient.offsetY = ambientBottomPoint;
			ambient.offsetZ = offsetZ-land.ambientMargins;
			
			//get matrix
			land.data.ToExistMatrix (ambient, refExist);
			
			//returning offsets
			ambient.offsetX = -land.ambientMargins;
			ambient.offsetY = ambientBottomPoint;
			ambient.offsetZ = -land.ambientMargins;

			if (land.profile) Profiler.EndSample ();
			#endregion

			#region Get Heightmap
			if(land.profile) Profiler.BeginSample("Get Heightmap");

			Matrix2<int> heightmap = new Matrix2<int>(land.chunkSize+land.ambientMargins*4, land.chunkSize+land.ambientMargins*4); //margins are twice bigger
			heightmap.offsetX = offsetX-land.ambientMargins*2;
			heightmap.offsetZ = offsetZ-land.ambientMargins*2;
			land.data.ToHeightMatrix(heightmap, refExist);

			//moving height matrix to chunk-relative coordinates
			heightmap.offsetX -= offsetX;
			heightmap.offsetZ -= offsetZ;

			if(land.profile) Profiler.EndSample();
			#endregion

			#region Smoothing Heightmap
			if(land.profile) Profiler.BeginSample("Smoothing Heightmap");

			for (int i=0; i<10; i++)
			{
				for(int x=heightmap.offsetX+1;x<heightmap.offsetX+heightmap.sizeX-1;x++)
					for(int z=heightmap.offsetZ+1;z<heightmap.offsetZ+heightmap.sizeZ-1;z++)
					{
						heightmap.pos = (z-heightmap.offsetZ)*heightmap.sizeX + x - heightmap.offsetX; //SetPos(x,z);

						heightmap.current = (
							Mathf.Max(heightmap.current, heightmap.nextX) + 
							Mathf.Max(heightmap.current, heightmap.prevX) + 
							Mathf.Max(heightmap.current, heightmap.nextZ) + 
							Mathf.Max(heightmap.current, heightmap.prevZ) ) / 4;
					}
			}

			if(land.profile) Profiler.EndSample();
			#endregion

			#region Filling Heightmap
			if(land.profile) Profiler.BeginSample("Filling Heightmap");

			for(int x=-land.ambientMargins;x<land.chunkSize+land.ambientMargins;x++) //two pyramids - at the borders
				for(int z=-land.ambientMargins;z<land.chunkSize+land.ambientMargins;z++)
				{
					int highPoint = heightmap[x,z];
					for(int y=ambientTopPoint-2; y>=highPoint; y--)
						ambient[x,y,z] = max;
				}

			if(land.profile) Profiler.EndSample();
			#endregion
 		
/*
			#region Light Pyramids
			if (land.profile) Profiler.BeginSample ("Light Pyramid");
			
			for (int y=ambientTopPoint-1; y>=ambientBottomPoint+1; y--)
			{
				for (int x=-land.ambientMargins+1; x<land.chunkSize+land.ambientMargins-1; x++) 
					for (int z=-land.ambientMargins+1; z<land.chunkSize+land.ambientMargins-1; z++) 
				{
					ambient.pos = (z-ambient.offsetZ)*ambient.sizeX*ambient.sizeY + (y-ambient.offsetY)*ambient.sizeX + x - ambient.offsetX;

					if (ambient.current == 255) continue; //if filled

					if(ambient.nextY == max)
					{
						//if(y%2 == 0) ambient.current = max; else //shrinking pyramid every second line
						if((ambient.prevXnextY==max && ambient.nextXnextY==max) || (ambient.prevZnextY==max && ambient.nextZnextY==max)) ambient.current = max;
						//if(ambient.prevXnextY==max && ambient.nextXnextY==max && ambient.prevZnextY==max && ambient.nextZnextY==max) ambient.current = max; //if four blocks around above filled
						else ambient.current = 0;
					}
				}
			}

			if (land.profile) Profiler.EndSample ();
			#endregion
*/
	 
			#region Spreading
			if (land.profile) Profiler.BeginSample ("Spreading");
			
			for (int itaration=0; itaration<2; itaration++)
			{				
				for (int x=ambient.offsetX+1; x<ambient.offsetX+ambient.sizeX; x++) //count till the end of matrix, not chunk (to some avoid bugs)
					for (int z=ambient.offsetZ+1; z<ambient.offsetZ+ambient.sizeZ; z++) 
					{
						ambient.SetPos(x, ambient.offsetY+1, z);
						for(int y=ambient.offsetY+1;y<ambient.offsetY+ambient.sizeY;y++)
						{
							byte current = ambient.current;
							if (current >= max) { ambient.MovePosNextY(); continue; } //if filled(255) or already maximum(max)
							
							byte neigMax = current;
							byte neig = ambient.prevY; if (neig>neigMax && neig<=max) neigMax = neig;
							neig = ambient.prevX; if (neig>neigMax && neig<=max) neigMax = neig;
							neig = ambient.prevZ; if (neig>neigMax && neig<=max) neigMax = neig;

							neigMax = (byte)(neigMax*land.ambientFade);
							ambient.current = neigMax;

							ambient.MovePosNextY();
						}
					}

				for (int x=ambient.offsetX+ambient.sizeX-2; x>ambient.offsetX; x--) //the same but in different direction
					for (int z=ambient.offsetZ+ambient.sizeZ-2; z>ambient.offsetZ; z--) 
					{
						ambient.SetPos(x, ambient.offsetY+ambient.sizeY-2, z);
						for(int y=ambient.offsetY+ambient.sizeY-2;y>ambient.offsetY;y--)
						{
							byte current = ambient.current;
							if (current >= max) { ambient.MovePosPrevY(); continue; } //if filled(255) or already maximum(max)
							
							byte neigMax = current;
							byte neig = ambient.nextY; if (neig>neigMax && neig<=max) neigMax = neig;
							neig = ambient.nextX; if (neig>neigMax && neig<=max) neigMax = neig;
							neig = ambient.nextZ; if (neig>neigMax && neig<=max) neigMax = neig;

							neigMax = (byte)(neigMax*land.ambientFade);
							ambient.current = neigMax;

							ambient.MovePosPrevY();
						}
					}
			}

			if (land.profile) Profiler.EndSample ();
			#endregion

			#region Removing temporary values
			if (land.profile) Profiler.BeginSample ("Clearing");
			
			for (int i=0; i<ambient.array.Length; i++) 
				if (ambient.array[i] > max)
					ambient.array[i] = 0;
		
			if (land.profile) Profiler.EndSample ();
			#endregion
			
			if (land.profile) Profiler.EndSample();
		}
		

		static readonly int[] cornerCoordsX = { 0,1,1,0, 1,0,0,1, 1,1,1,1, 0,0,0,0, 1,1,0,0, 1,1,0,0 };
		static readonly int[] cornerCoordsY = { 1,1,1,1, 0,0,0,0, 1,1,0,0, 1,1,0,0, 0,1,1,0, 1,0,0,1 };
		static readonly int[] cornerCoordsZ = { 1,1,0,0, 1,1,0,0, 0,1,1,0, 1,0,0,1, 1,1,1,1, 0,0,0,0 };

		public Color GetNodeAmbient (int xi, int yi, int zi)
		{
			ambient.SetPos(xi,yi,zi);

			int ambientSum = 0;
			int ambientDiv = 0;
			
			byte cur = ambient.current; if(cur != 0) { ambientSum += cur; ambientDiv++; }
			byte x = ambient.prevX; if(x != 0) { ambientSum += x; ambientDiv++; }
			byte y = ambient.prevY; if(y != 0) { ambientSum += y; ambientDiv++; }
			byte z = ambient.prevZ; if(z != 0) { ambientSum += z; ambientDiv++; }
			byte xy = ambient.prevXprevY; if(xy != 0) { ambientSum += xy; ambientDiv++; }
			byte zy = ambient.prevZprevY; if(zy != 0) { ambientSum += zy; ambientDiv++; }
			byte xz = ambient[xi-1, yi, zi-1]; if(xz != 0) { ambientSum += xz; ambientDiv++; }
			byte xyz = ambient[xi-1,yi-1,zi-1]; if(xyz != 0) { ambientSum += xyz; ambientDiv++; }
			if (ambientDiv == 0) return new Vector4(0,0,0,0);

			//finding ambient direction
			float dirX = 0;  int dirDiv = 0;
			if (cur != 0 && x!=0) { dirX += GetAmbientDir(cur,x); dirDiv++; }
			if (y != 0 && xy!=0) { dirX += GetAmbientDir(y,xy); dirDiv++; }
			if (z != 0 && xz!=0) { dirX += GetAmbientDir(z,xz); dirDiv++; }
			if (zy != 0 && xyz!=0) { dirX += GetAmbientDir(zy,xyz); dirDiv++; }
			if (dirDiv != 0) dirX = dirX/dirDiv;

			float dirY = 0; dirDiv = 0;
			if(cur != 0 && y!=0) { dirY += GetAmbientDir(cur,y); dirDiv++; }
			if(x != 0 && xy!=0) { dirY += GetAmbientDir(x,xy); dirDiv++; }
			if(z != 0 && zy!=0) { dirY += GetAmbientDir(z,zy); dirDiv++; }
			if(xz != 0 && xyz!=0) { dirY += GetAmbientDir(xz,xyz); dirDiv++; }
			if(dirDiv != 0) dirY = dirY/dirDiv;

			float dirZ = 0; dirDiv = 0;
			if(cur != 0 && z!=0) { dirZ += GetAmbientDir(cur,z); dirDiv++; }
			if(x != 0 && xz!=0) { dirZ += GetAmbientDir(x,xz); dirDiv++; }
			if(y != 0 && zy!=0) { dirZ += GetAmbientDir(y,zy); dirDiv++; }
			if(xy != 0 && xyz!=0) { dirZ += GetAmbientDir(xy,xyz); dirDiv++; }
			if(dirDiv != 0) dirZ = dirZ/dirDiv;
			
			return new Color(dirX/2+0.5f,dirY/2+0.5f,dirZ/2+0.5f, ambientSum / ambientDiv / 250f);
		}

		public float GetAmbientDir (byte val1, byte val2)
		{
			float valMax = val1>val2 ? val1 : val2;
			float invFade = 1f/land.ambientFade;

			return (val1/valMax-land.ambientFade)*invFade - (val2/valMax-land.ambientFade)*invFade;
			//		 (  1      -      0.66    ) *    3    -  (  0.66   -    0.66   )    *   3
		}


		public void ApplyAmbient ()
		{
			if (!land.ambient) return;

			ApplyAmbientToTerrain();
			//ApplyAmbientToConstructor(); //dconstructor
			ApplyAmbientToGrass();
			ApplyAmbientToPrefabs();
		}
		
		public void ApplyAmbientToTerrain () 
		{
			if (!land.ambient) return;
			if (land.rtpCompatible) return;
			if (land.profile) Profiler.BeginSample("ApplyAmbientToTerrain");
			
			#region Return if empty
				if (faces==null || faces.Length==0 ||
					verts==null || verts.Length==0 ||
					ambient==null || ambient.array.Length==0)
					{
						if (land.profile) Profiler.EndSample (); 
						return; 
					}
			#endregion

			#region Calculating number of all verts (including  invisible ones)
			Profiler.BeginSample("Calculating number of all verts");
			//TODO: Use number of verts that was determined by chunk

				int numAllVerts = 0;
				bool[] vertUsed = new bool[faces.Length*9];
				for(int f=0;f<faces.Length;f++) 
					for (int v=0;v<9;v++) 
						vertUsed[ faces[f][v] ] = true;

				for (int v=0;v<vertUsed.Length;v++)
					if (vertUsed[v]) numAllVerts++;

			Profiler.EndSample();
			#endregion

			#region Setting Vert Ambient
			Profiler.BeginSample ("Setting Vert Ambient");
						
				Color[] allColors = new Color[numAllVerts];

				for (int f=0; f<faces.Length; f++)
				{ 
					//corners
					for (int c=0; c<4; c++) 
					{
						int x = faces[f].x; int y=faces[f].y; int z = faces[f].z;
						int i = faces[f].dir*4 + c;

						allColors[faces[f].cornerNums[c]] = GetNodeAmbient(
							x+cornerCoordsX[i], 
							y+cornerCoordsY[i],
							z+cornerCoordsZ[i]);
					}

					//sides
					for (int s=0; s<4; s++)
						allColors[faces[f].sideNums[s]] = (allColors[faces[f].cornerNums[s]] + allColors[faces[f].cornerNums[s+1]]) / 2;

					//center
					allColors[faces[f].centerNum] = (allColors[faces[f].sideNums.a] + allColors[faces[f].sideNums.b] + allColors[faces[f].sideNums.c] + allColors[faces[f].sideNums.d]) / 4;
				}
	
			Profiler.EndSample ();
			#endregion
			
			#region Vert Ambient to mesh
			Profiler.BeginSample ("To Mesh");

				Mesh mesh = hiFilter.sharedMesh;
				Color[] colors = new Color[mesh.vertexCount];
				for (int v=0; v<mesh.vertexCount; v++) colors[v] = allColors[v]; //as all the visible verts go first
				mesh.colors = colors;
			
			Profiler.EndSample ();
			#endregion
			
			#region Baking to lopoly
			Profiler.BeginSample ("To Lopoly");

				mesh = loFilter.sharedMesh;
				colors = new Color[mesh.vertexCount];
				for (int v=0; v<mesh.vertexCount; v++) colors[v] = allColors[v];
				mesh.colors = colors;

			Profiler.EndSample ();
			#endregion
			
			if (land.profile) Profiler.EndSample();
		}		

		public void ApplyAmbientToGrass ()
		{
			if (!land.ambient) return;
			if (grassFilter.sharedMesh.vertexCount == 0) return;

			Vector3[] meshVerts = grassFilter.sharedMesh.vertices;
			Color[] meshColors = grassFilter.sharedMesh.colors;
			int[] meshTris = grassFilter.sharedMesh.triangles;

			for (int t=0; t<meshTris.Length; t+=3)
			{
				Vector3 faceCenter = (meshVerts[ meshTris[t] ] + meshVerts[ meshTris[t+1] ] + meshVerts[ meshTris[t+2] ]) / 3f;
				//faceCenter += Vector3.up;
				float amb = ambient[ (int)(faceCenter.x+0.5f), (int)(faceCenter.y+0.5f), (int)(faceCenter.z+0.5f) ] / 250f; 
				amb = 1;

				meshColors[ meshTris[t] ].a = amb;
				meshColors[ meshTris[t+1] ].a = amb;
				meshColors[ meshTris[t+2] ].a = amb;
			}
			

			/*
			for (int v=0; v<meshVerts.Length; v++)
			{
				meshColors[v] = new Color(0,0,0,
					ambient[ Mathf.FloorToInt(meshVerts[v].x), Mathf.FloorToInt(meshVerts[v].y), Mathf.FloorToInt(meshVerts[v].z) ] / 250f);
			}*/

			grassFilter.sharedMesh.colors = meshColors;
		}

		public void ApplyAmbientToPrefabs ()
		{
			if (!land.ambient) return;
		}

	#endregion

	} //class
} //namespace



