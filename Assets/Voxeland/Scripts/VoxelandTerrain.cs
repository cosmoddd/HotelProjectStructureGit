
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Voxeland 
{
	public struct Coord
	{
		public int x; public int y; public int z; public byte dir; public byte extend;
		
		static readonly public int[] oppositeDirX = {0,0,1,-1,0,0};
		static readonly public int[] oppositeDirY = {1,-1,0,0,0,0};
		static readonly public int[] oppositeDirZ = {0,0,0,0,1,-1};
		
		public Coord (int x, int y, int z) {this.x=x; this.y=y; this.z=z; this.dir=0; this.extend=0;}
		public static Coord operator + (Coord a, Coord b) { return new Coord(a.x+b.x, a.y+b.y, a.z+b.z); }
		public static Coord operator - (Coord a, Coord b) { return new Coord(a.x-b.x, a.y-b.y, a.z-b.z); }
		public static Coord operator ++ (Coord a) { return new Coord(a.x+1, a.y+1, a.z+1); }
		
		public static bool operator > (Coord a, Coord b) { return (a.x>b.x || a.z>b.z); } //do not compare y's
		public static bool operator >= (Coord a, Coord b) { return (a.x>=b.x || a.z>=b.z); }
		public static bool operator < (Coord a, Coord b) { return (a.x<b.x || a.z<b.z); }
		public static bool operator <= (Coord a, Coord b) { return (a.x<=b.x || a.z<=b.z); }
		
		public Coord GetChunkCoord (Coord worldCoord, int chunkSize) //gets chunk coordinates using wholeterrain unit coord
		{
			return new Coord
				(
					worldCoord.x>=0 ? (int)(worldCoord.x/chunkSize) : (int)((worldCoord.x+1)/chunkSize)-1,
					0,
					worldCoord.z>=0 ? (int)(worldCoord.z/chunkSize) : (int)((worldCoord.z+1)/chunkSize)-1
					);
		}
		
		/*
		public int Extend
		{
			get { return y; } 
			set { y=value; }
		}*/
		
		public int BlockMagnitude2 { get { return Mathf.Abs(x)+Mathf.Abs(z); } }
		public int BlockMagnitude3 { get { return Mathf.Abs(x)+Mathf.Abs(y)+Mathf.Abs(z); } }
	}
	
	[System.Serializable]
	public class VoxelandBlockType
	{
		#region class BlockType
		public string name;
		
		public bool filledTerrain = true; //just gui storage, using terrainExist instead. terrainExist is created upon display from 'fiiled' values
		
		public Color color = new Color(0.77f, 0.77f, 0.77f, 1f);
		public Texture texture; //aka mainTex
		public Texture bumpTexture; //aka bumpMap
		public Texture specGlossMap;
		public Color specular;
		public float tile = 0.25f;

		/*public bool differentTop;
		public Texture topTexture;
		public Texture topBumpTexture;*/
		public bool  grass = true;
		//public Texture grassTexture;
		public Color grassTint = Color.white;
		public float smooth = 1f; //not used
		
		//ambient
		public bool filledAmbient = true;
		
		//prefabs
		public bool filledPrefabs = false;
		public Transform[] prefabs = new Transform[0]; //new ObjectPool[0];
		
		//dconstructor
	//	public bool filledConstructor = false;
	//	public bool replaceTerrain = false;
	//	public Constructor constructor;
		
		//outdated
	//	public bool filled;
	//	public bool visible = true; //dconstructor
	//	public Transform obj; 
	//	public List<Transform> prefabPool = new List<Transform>();
		
		public VoxelandBlockType (string n, bool f) { name = n; filledTerrain = f; filledAmbient = f; }
		#endregion
	}
	
	[System.Serializable]
	public class VoxelandGrassType 
	{
		#region class GrassType
		public string name;
		public Mesh sourceMesh; //for editor only
		[System.NonSerialized] public MeshWrapper[] meshes;
		public Material material;
		
		public float height = 1;
		public float width = 1;
		public float elevation = 0;
		public float incline = 0.1f;
		public float random = 0.1f;
		public bool normalsFromTerrain = true;
		
		public VoxelandGrassType (string n) { name = n; }
		#endregion
	}
	
	//[ExecuteInEditMode] //to rebuild on scene loading. Only OnEnable uses it
	public class VoxelandTerrain : MonoBehaviour
	{
		public enum RebuildType {none, all, terrain, ambient, constructor, grass, prefab}; 
		
		public Data data;
		public DataHolder dataholder;

		[System.NonSerialized] public Matrix2<Chunk> chunks = new Matrix2<Chunk>(0,0);
		[System.NonSerialized] public int[] chunkNumByDistance = new int[0]; //chunks ordered by distance from center. Always the same length as chunks.array
		
		public VoxelandBlockType[] types;
		public VoxelandGrassType[] grass;
		public int selected; //negative is for grass
		
		public bool[] terrainExist = new bool[0];
		public bool[] ambientExist = new bool[0];
		public bool[] prefabsExist = new bool[0];
		//public bool[] constructorExist = new bool[0]; //dconstructor
		//public bool[] terrainOrConstructorExist = new bool[0]; //dconstructor
		
		public int chunkSize = 30;
		public int terrainMargins = 2;
		
		public Highlight highlight;
		public Material highlightMaterial;
		
		public int brushSize = 0;
		public bool  brushSphere;
		
		public bool  playmodeEdit;
		//public bool  independPlaymode;
		public bool  usingPlayData = false;
		
		//public bool  weldVerts = true;
		
		public bool limited = true;
		//public int terrainSize = 30*10;  //using area size instead
		
		public int chunksBuiltThisFrame = 0; //for statistics and farmesh
		public int chunksUnbuiltLeft = 0;

		#region vars Far
		public bool useFar = false;
		public Mesh farMesh;
		public float farSize;
		public Far far;
		//public bool farNeedRebuild = false;
		//public int lastFarX;
		//public int lastFarZ;
		#endregion
		
		public bool  ambient = true;
		public float ambientFade = 0.666f;
		public int ambientMargins = 5;
		//public int ambientSpread = 4;
		
		public Material terrainMaterial;

		//public float normalsRandom = 0;
		//public VoxelandNormalsSmooth normalsSmooth = VoxelandNormalsSmooth.mesh;
		
		public bool displayArea = false;
		
		#region vars GUI
		public bool  guiData;
		public bool  guiTypes = true;
		public bool  guiGrass = true;
		public bool  guiGenerate = false;
		public bool  guiExport;
		public bool  guiLod;
		public bool  guiTerrainMaterial;
		public bool  guiAmbient;
		public bool  guiMaterials;
		public bool  guiSettings;
		public bool  guiDebug;
		public bool  guiRebuild = true;
		public bool  guiArea = false;
		public bool  guiImportExport = false;
		public bool guiFar = true;
		public int guiFarChunks = 25;
		public int guiFarDensity = 10;
		public bool guiBake;
		public bool guiBakeLightmap;
		public int guiBakeSize = 100;
		public Transform guiBakeTfm;
		public bool guiGeneratorOverwrite = false;
		public int guiGeneratorCenterX = 0;
		public int guiGeneratorCenterZ = 0;
		public bool guiGeneratorUseCamPos = true;
		public bool guiSelectArea = false;
		public int guiSelectedAreaNum = 5050;
		public bool guiSelectedAreaShow = true;
		public int guiEmptyColumnHeight = 0;
		public byte guiEmptyColumnType = 1;
		public int guiNewChunkSize = 30;
		public bool guiFocusOnBrush = true;
		public int guiAreaSize = 512;
		public int guiNoiseSeed = 12345;
		#endregion
	
		public bool generateLightmaps = false; //this should be always off exept baking with lightmaps
		public float lightmapPadding = 0.1f;
		public bool  saveMeshes = false;
		
		public float lodDistance = 50;
		public bool lodWithMaterial = true; //switch lod object using it's material, not renderer
		
		public float removeDistance = 120;
		public float generateDistance = 60;
		public bool gradualGenerate = true;
		public bool multiThreadEdit;
		
		public bool  generateCollider = true; 
		public bool  generateLod = true;

		public bool rtpCompatible = false;

		#region Generators
		public bool clearGenerator = true;
		public LevelGenerator levelGenerator;
		public TextureGenerator textureGenerator;
		public NoiseGenerator noiseGenerator;
		public GlenGenerator glenGenerator;
		public ErosionGenerator erosionGenerator;
		public ForestGenerator forestGenerator;
		public GrassGenerator grassGenerator;
		public bool autoGeneratePlaymode = false;
		public bool autoGenerateEditor = false;
		public bool saveGenerated = false;
		public bool autoGenerateStarted = false; //for skipping frame when autogenerate starts
		#endregion
		
		#if UNITY_EDITOR
		private System.Diagnostics.Stopwatch timerSinceSetBlock = new System.Diagnostics.Stopwatch(); //to save byte list
		#endif
		
		//update speed-ups
		[System.NonSerialized] public Ray oldAimRay; //public for vizualizer
		
		static public readonly int[] oppositeDirX = {0,0,1,-1,0,0};
		static public readonly int[] oppositeDirY = {1,-1,0,0,0,0};
		static public readonly int[] oppositeDirZ = {0,0,0,0,1,-1};
		
		#if UNITY_EDITOR
		private int mouseButton = 0; //for EditorUpdate, currently pressed mouse button
		#endif

		//debug
		public bool  hideChunks = true;
		public bool hideWire = true;
		public bool profile = false;
		public Visualizer visualizer;
		
		//public static int mainThreadId;

		
		public bool chunksUnparented = false; //for saving scene without meshes. If chunks unparented they should be returned
		
		#region undo
		public bool undo; //foo to be changed on each undo to record undo state
		public bool recordUndo = true;
		public List< Matrix2<Data.Column> > undoSteps = new List<Matrix2<Data.Column>>();

		#endregion
		

		public void  Update ()
		{
			//if in scene view while in playmode
			#if UNITY_EDITOR
			//if (!UnityEditor.EditorApplication.isPlaying) return;
			if (UnityEditor.SceneView.lastActiveSceneView == UnityEditor.EditorWindow.focusedWindow) return;
			
			//removing delegates
			UnityEditor.EditorApplication.update -= EditorUpdate;	
			UnityEditor.SceneView.onSceneGUIDelegate -= GetMouseButton; 
			#endif
			
			//display
			Display(false);
			
			//edit
			//this is an example of how Voxeland could be edited in playmode
			if (playmodeEdit)
			{
				Ray aimRay = Camera.main.ScreenPointToRay(Input.mousePosition);
				bool mouseDown = Input.GetMouseButtonDown(0);
				bool shift = Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);
				bool control = Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl);
			
				Edit(aimRay, selected,
					mouseDown && !shift && !control,
					mouseDown && shift && !control,
					mouseDown && control && !shift,
					mouseDown && control && shift);
			}
		}
		
		public void EditorUpdate ()
		{
			#if UNITY_EDITOR

			#region Removing Delegate in case it was not removed
			if (this == null) 
			{ 
				UnityEditor.SceneView.onSceneGUIDelegate -= GetMouseButton;
				UnityEditor.EditorApplication.update -= EditorUpdate;
				return;
			}
			#endregion

			if (!this.enabled) return; //EditorUpdate is called even when the script is off

			if (UnityEditor.EditorApplication.isPlaying ||
				UnityEditor.SceneView.lastActiveSceneView == null ||
			    UnityEditor.EditorWindow.focusedWindow == null || 
			    UnityEditor.EditorWindow.focusedWindow.GetType() == System.Type.GetType("UnityEditor.GameView,UnityEditor")
			    ) return;
			    
			//display
			Display(true);

			//edit is done with in onSceneGui - it can catch mouse events
			
			//removing highlight
			if (UnityEditor.Selection.activeGameObject != gameObject && highlight != null) highlight.Clear();

			#endif
		}

	
		#region Display

			//public void Display () { Display(false); }
			public void Display (bool isEditor) //called every frame
			{			
				if (profile) Profiler.BeginSample ("Display");
				if (data == null) return;
			
				#region Load dataholder data 
				#if UNITY_EDITOR
				if (dataholder != null)
				{
					/*
					string path = UnityEditor.EditorApplication.currentScene;
					path.Replace(".unity", ".asset");
					path.Replace(".UNITY", ".ASSET");
					Debug.Log(path);
				
					data = ScriptableObject.CreateInstance<Data>();
					//data.compressed = dataholder.data.SaveToByteList();
					//data.areas = new Voxeland.Data.Area[dataholder.data.areas.Length];
					data.New();
					for (int a=0; a<data.areas.Length; a++) 
					{
						data.areas[a].columns = new Data.ListWrapper[dataholder.data.areas[a].columns.Length]; 
						for (int c=0; c<data.areas[a].columns.Length; c++) 
						{
							data.areas[a].columns[c] = new Data.ListWrapper();
							data.areas[a].columns[c].list = dataholder.data.areas[a].columns[c].list;
						}
						//data.areas[a].grass = new Data.ListWrapper(); data.areas[a].grass.list = dataholder.data.areas[a].grass.list;
						data.areas[a].initialized = dataholder.data.areas[a].initialized;
						data.areas[a].serializable = dataholder.data.areas[a].serializable;
					}
					data.compressed = data.SaveToByteList();
					data.name = "VoxelandData";
					UnityEditor.EditorUtility.SetDirty(data);
					*/

					DestroyImmediate(dataholder.gameObject);
				}
				#endif
				#endregion

				#region Loading compressed data
				if (data.areas == null) data.LoadFromByteList(data.compressed);
				#endregion
			
				#region Creating material if it has not been assigned
				if (terrainMaterial==null) terrainMaterial = new Material(Shader.Find("Voxeland/Standard"));
				#endregion
			
				#region Returning unparented chunks
				if (chunksUnparented)
				{
					if (chunks.array != null) for (int i=0; i<chunks.array.Length; i++) chunks.array[i].transform.parent = transform;
					if (far != null) far.transform.parent = transform;
					if (highlight != null) highlight.transform.parent = transform;
					chunksUnparented = false;
				}
				#endregion

				#region Rebuild: Clearing children if no terrain
				if (chunks.array == null || chunks.array.Length == 0)
					for(int i=transform.childCount-1; i>=0; i--) 
					{
						//clearing all meshes
						Chunk chunk = transform.GetChild(i).GetComponent<Chunk>();
						if (chunk != null)
						{
							if (chunk.hiFilter != null && chunk.hiFilter.sharedMesh != null) DestroyImmediate(chunk.hiFilter.sharedMesh);
							if (chunk.loFilter != null && chunk.loFilter.sharedMesh  != null) DestroyImmediate(chunk.loFilter.sharedMesh);
							if (chunk.grassFilter != null && chunk.grassFilter.sharedMesh != null)  DestroyImmediate(chunk.grassFilter.sharedMesh);
							//if (chunk.constructorFilter != null && chunk.constructorFilter.sharedMesh != null)  DestroyImmediate(chunk.constructorFilter.sharedMesh); //dconstructor
							//if (chunk.constructorCollider != null && chunk.constructorCollider.sharedMesh != null)  DestroyImmediate(chunk.constructorCollider.sharedMesh); //dconstructor
						}

						//destroing object. Whatever it is
						DestroyImmediate(transform.GetChild(i).gameObject);
					}
				#endregion

				#region Save Compressed Data after delay
				#if UNITY_EDITOR
				if (timerSinceSetBlock.ElapsedMilliseconds > 2000 && mouseButton == 0 && !UnityEditor.EditorApplication.isPlaying) 
				{
					data.compressed = data.SaveToByteList();
					UnityEditor.EditorUtility.SetDirty(data);
					timerSinceSetBlock.Stop();
					timerSinceSetBlock.Reset();
				}
				#endif
				#endregion
			
				#region Setting exist types
				if (terrainExist.Length != types.Length)
				{
					terrainExist = new bool[types.Length];
					ambientExist = new bool[types.Length];
					prefabsExist = new bool[types.Length];
					//dconstructor
					//constructorExist = new bool[types.Length]; 
					//terrainOrConstructorExist = new bool[types.Length];
				}
			
				for (int i=0; i<types.Length; i++)
				{
					terrainExist[i] = types[i].filledTerrain;
					ambientExist[i] = types[i].filledAmbient;
					prefabsExist[i] = types[i].filledPrefabs;
					//dconstructor
					//constructorExist[i] = types[i].filledConstructor;
					//terrainOrConstructorExist[i] = types[i].filledTerrain || types[i].filledConstructor;
				}
				#endregion
			
				#region Finding camera position

					Vector3 camPos;

					#if UNITY_EDITOR
					if (isEditor && UnityEditor.SceneView.lastActiveSceneView != null) 
						camPos = UnityEditor.SceneView.lastActiveSceneView.camera.transform.position;
					else camPos = Camera.main.transform.position;
					#else
					camPos = Camera.main.transform.position;
					#endif
			
					camPos = transform.InverseTransformPoint(camPos);

				#endregion
			
				#region Preparing chunk matrix
					//all the chunks.array should be filled with chunks. There may be empty chunks (if they are out of build dist), but they should not be null
					//TODO: matrix changes too often on chunk seams. Make an offset.

					//finding build center and range (they differ on limited and infinite tarrains)
					float range = 0; Vector3 center = Vector3.zero;
					if (limited) { range = (data.areaSize-chunkSize)/2f; center = new Vector3(range,range,range); }
					else { range = removeDistance; center = camPos; }
					
					//looking if size changed
					bool matrixChanged = false;
					int newSize = Mathf.CeilToInt(range*2/chunkSize)+1;
					if (chunks.sizeX != newSize || chunks.sizeZ != newSize) matrixChanged = true;
			
					//looking if coordinates changed. If old center chunk != new center chunk
					int newOffsetX = Mathf.FloorToInt((center.x - range) / chunkSize);
					int newOffsetZ = Mathf.FloorToInt((center.z - range) / chunkSize);
					if (chunks.offsetX != newOffsetX || chunks.offsetZ != newOffsetZ) matrixChanged = true;

					//moving or resizing matrix
					if (matrixChanged)
					{
						//creating new chunks matrix
						Matrix2<Chunk> newChunks = new Matrix2<Chunk>(newSize, newSize);
						newChunks.offsetX = newOffsetX;
						newChunks.offsetZ = newOffsetZ;

						//taking all ready chunks
						for (int x=newChunks.offsetX; x<newChunks.offsetX+newChunks.sizeX; x++)
							for (int z=newChunks.offsetZ; z<newChunks.offsetZ+newChunks.sizeZ; z++)
								if (chunks.CheckInRange(x,z)) { newChunks[x,z] = chunks[x,z]; chunks[x,z] = null; }

						//creating list of unused chunks
						List<Chunk> unusedChunks = new List<Chunk>();
						for (int x=chunks.offsetX; x<chunks.offsetX+chunks.sizeX; x++)
							for (int z=chunks.offsetZ; z<chunks.offsetZ+chunks.sizeZ; z++)
								if (chunks[x,z] != null) unusedChunks.Add(chunks[x,z]);

						//filling empty holes with unused chunks (or creating new if needed - upon expanding range or rebuild)
						for (int x=newChunks.offsetX; x<newChunks.offsetX+newChunks.sizeX; x++)
							for (int z=newChunks.offsetZ; z<newChunks.offsetZ+newChunks.sizeZ; z++)
						{
							if (newChunks[x,z]==null)
							{
							//	if (unusedChunks.Count!=0) { newChunks[x,z] = unusedChunks[0]; unusedChunks.RemoveAt(0); }
							//	else 
								newChunks[x,z] = Chunk.CreateChunk(this);
								newChunks[x,z].Init(x,z);
							}
						}

						//removing unused chunks left
						for (int i=0; i<unusedChunks.Count; i++)
							DestroyImmediate(unusedChunks[i].gameObject);

						//saving array
						chunks = newChunks;
						
						//create an array of chunks ordered by distance
						if (chunkNumByDistance.Length != chunks.array.Length)
						{
							chunkNumByDistance = new int[chunks.array.Length];
							for (int i=0; i<chunkNumByDistance.Length; i++) chunkNumByDistance[i] = i;

							int[] distances = new int[chunks.array.Length];
							for (int z=0; z<chunks.sizeZ; z++)
								for (int x=0; x<chunks.sizeX; x++)
									distances[z*chunks.sizeX + x] = (x-chunks.sizeX/2)*(x-chunks.sizeX/2) + (z-chunks.sizeZ/2)*(z-chunks.sizeZ/2);
									//Mathf.Max( Mathf.Abs(x-chunks.sizeX/2), Mathf.Abs(z-chunks.sizeZ/2) );

							for (int i=0; i<distances.Length; i++) 
								for (int d=0; d<distances.Length-1; d++)
									if (distances[d] > distances[d+1])
									{
										int temp = distances[d+1];
										distances[d+1] = distances[d];
										distances[d] = temp;

										temp = chunkNumByDistance[d+1];
										chunkNumByDistance[d+1] = chunkNumByDistance[d];
										chunkNumByDistance[d] = temp;
									}
						}
					}
				#endregion

				#region AutoGenerating

					//finding a need to autogenerate
					bool autoGenerate = false;
					if (isEditor && autoGenerateEditor) autoGenerate = true;
					if (!isEditor && autoGeneratePlaymode) autoGenerate = true;
					
					//finding if there are uninitialized areas within build distance
					//note that build dist should be less than area size
					//this section is so complex to display progress bar
					if (!limited && autoGenerate)
					{
						int boundsDist = (int)(generateDistance + chunkSize + 1);

						//finding if any area is not initialized
						bool hasNotInitializedArea = false;
						for (int x=(int)camPos.x-boundsDist; x<=(int)camPos.x+boundsDist; x+=boundsDist)
							for (int z=(int)camPos.z-boundsDist; z<=(int)camPos.z+boundsDist; z+=boundsDist)
								if (!data.areas[ data.GetAreaNum(x,z) ].initialized) hasNotInitializedArea = true;

						if (hasNotInitializedArea)
						{
							//skipping frame if autogenerate just started
							//this should redraw game gui to display "Please Wait" panel
							if (!autoGenerateStarted) { autoGenerateStarted = true; return; }
							
							//calculating number of non-initialized areas
							List<int> notInitializedNums = new List<int>();
							for (int x=(int)camPos.x-boundsDist; x<=(int)camPos.x+boundsDist; x+=boundsDist)
								for (int z=(int)camPos.z-boundsDist; z<=(int)camPos.z+boundsDist; z+=boundsDist)
							{
								int areaNum = data.GetAreaNum(x,z);
								if (!data.areas[areaNum].initialized && !notInitializedNums.Contains(areaNum)) notInitializedNums.Add(areaNum);
							}


							//generating
							int counter = 0;
							for (int x=(int)camPos.x-boundsDist; x<=(int)camPos.x+boundsDist; x+=boundsDist)
								for (int z=(int)camPos.z-boundsDist; z<=(int)camPos.z+boundsDist; z+=boundsDist)
							{
								int areaNum = data.GetAreaNum(x,z);
								if (data.areas[areaNum].initialized) continue; 

								#if UNITY_EDITOR
								if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) 
									UnityEditor.EditorUtility.DisplayProgressBar(
										"Voxeland Generator", 
										"Generating new terrain " + counter + " of " + notInitializedNums.Count, 1f*counter/notInitializedNums.Count);
								#endif
							
								Generate(areaNum);

								//saving data
								if (saveGenerated)
								{
									data.areas[areaNum].save = true;
									data.compressed = data.SaveToByteList();
									//UnityEditor.EditorUtility.SetDirty(data);
								}

								counter++;
							}
							#if UNITY_EDITOR
							if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) UnityEditor.EditorUtility.ClearProgressBar();
							#endif
						}
						autoGenerateStarted = false;
					}

				#endregion

				#region Far mesh
				if (profile) Profiler.BeginSample ("Far");
			
					if (useFar)
					{
						if (far == null) 
						{ 
							far = Far.Create(this);
							far.x = (int)camPos.x; far.z = (int)camPos.z;
							far.Build(); 
						}
						far.x = (int)camPos.x; far.z = (int)camPos.z;
						
						//rebuilding far on matrix change. Rebuild on chunk build is done in chunk via process
						if (matrixChanged) far.Build();
					}
					else if (far != null) DestroyImmediate(far.gameObject);

				if (profile) Profiler.EndSample();
				#endregion
				
				#region Building terrain
				if (profile) Profiler.BeginSample ("Rebuild");
				
					chunksBuiltThisFrame = 0;
					for (int i=0; i<chunks.array.Length; i++)
					{
						Chunk chunk = chunks.array[chunkNumByDistance[i]];
						
						//skipping chunks that are out of build distance
						if (!limited && //in limited terrain all chunks should be built
							(chunk.offsetX + chunkSize < camPos.x - generateDistance ||
							chunk.offsetX > camPos.x + generateDistance ||
							chunk.offsetZ + chunkSize < camPos.z - generateDistance ||
							chunk.offsetZ > camPos.z + generateDistance))
								continue;
						
						//building forced chunks forced
						if (chunk.stage == Chunk.Stage.forceAll || chunk.stage == Chunk.Stage.forceAmbient)
							chunk.Process();

						//building gradual chunks gradually
						else if (chunk.stage != Chunk.Stage.complete) 
						{
							chunk.Process();
							if (gradualGenerate) break;
						}
					}
			
				if (profile) Profiler.EndSample();
				#endregion
			
				#region Calculating number of unbuilt chunks
				chunksUnbuiltLeft = 0;
				for (int i=0; i<chunks.array.Length; i++)
				{
					Chunk chunk = chunks.array[i];
						
					//skipping chunks that are out of build distance
					if (!limited && //in limited terrain all chunks should be built
						(chunk.offsetX + chunkSize < camPos.x - generateDistance ||
						chunk.offsetX > camPos.x + generateDistance ||
						chunk.offsetZ + chunkSize < camPos.z - generateDistance ||
						chunk.offsetZ > camPos.z + generateDistance))
							continue;
					
					if (chunk.stage != Chunk.Stage.complete)
						chunksUnbuiltLeft++;
				}
				//this should be in statistics
				//if (chunksBuiltThisFrame!=0 || chunksUnbuiltLeft!=0) Debug.Log(chunksBuiltThisFrame + " " + chunksUnbuiltLeft);
				#endregion
				
				#region Switching lods
				if (profile) Profiler.BeginSample ("Switching Lods");

					int minX = Mathf.FloorToInt(1f*(camPos.x-lodDistance)/chunkSize); int maxX = Mathf.FloorToInt(1f*(camPos.x+lodDistance)/chunkSize);
					int minZ = Mathf.FloorToInt(1f*(camPos.z-lodDistance)/chunkSize); int maxZ = Mathf.FloorToInt(1f*(camPos.z+lodDistance)/chunkSize);
					
					for (int x = chunks.offsetX; x < chunks.offsetX+chunks.sizeX; x++)
						for (int z = chunks.offsetZ; z < chunks.offsetZ+chunks.sizeZ; z++)
						{
							Chunk chunk = chunks[x,z];
							if (chunk == null) continue;

							if (x>=minX && x<=maxX && z>=minZ && z<=maxZ) chunk.SwitchLod(false);
							else chunk.SwitchLod(true);
						}
				
				if (profile) Profiler.EndSample();
				#endregion
				

				if (profile) Profiler.EndSample();
			}
		#endregion
		

		#region Edit

			/*public void Edit (Camera activeCam, Vector2 mousePos, bool mouseDown, bool shift, bool control)
			{
				Ray aimRay = activeCam.ScreenPointToRay(mousePos);
			
				//getting controls
				bool add = mouseDown && !shift && !control;
				bool dig = mouseDown && shift && !control;
				bool smooth = mouseDown && control && !shift;
				bool replace = mouseDown && control && shift;
	
				Edit(aimRay, add, dig, smooth, replace);
			}*/
		
			public void  Edit (Ray aimRay, int type, bool add=false, bool dig=false, bool smooth=false, bool replace=false)
			{
				if (profile) Profiler.BeginSample ("Edit");
		
				#region Getting aim ray change
				if (profile) Profiler.BeginSample ("Getting aim ray change");
			
				if (Mathf.Approximately(aimRay.origin.x, oldAimRay.origin.x) && 
					Mathf.Approximately(aimRay.origin.y, oldAimRay.origin.y) && 
					Mathf.Approximately(aimRay.origin.z, oldAimRay.origin.z) &&
					Mathf.Approximately(aimRay.direction.x, oldAimRay.direction.x) && 
					Mathf.Approximately(aimRay.direction.y, oldAimRay.direction.y) && 
					Mathf.Approximately(aimRay.direction.z, oldAimRay.direction.z) &&
					!add && !dig && !smooth && !replace) return;
				oldAimRay = aimRay;
			
				if (profile) Profiler.EndSample ();
				#endregion
			
				#region Aiming
				if (profile) Profiler.BeginSample ("Aiming");
				AimData aimData = GetCoordsByRay(aimRay);
				if (profile) Profiler.EndSample ();
				#endregion
			
				#region Drawing highlight
				if (profile) Profiler.BeginSample ("Drawing highlight");
			
				if (aimData.hit)
				{
					if (highlight == null) highlight = Highlight.Create(this);
				
					if (aimData.type == AimData.Type.face)
					{
						if (brushSize==0) highlight.DrawFace(aimData.chunk, aimData.face);
						else if (brushSphere) highlight.DrawSphere(new Vector3(aimData.x-130.6f, aimData.y-79.5f, aimData.z-180.4f), brushSize);
						else highlight.DrawBox(new Vector3(aimData.x-130.6f, aimData.y-79.5f, aimData.z-180.4f), new Vector3(brushSize,brushSize,brushSize));
					}
				
					if (aimData.type == AimData.Type.obj)
					{
						highlight.DrawBox(aimData.collider.bounds.center, aimData.collider.bounds.extents);
					}
				
					//dconstructor
					//if (aimData.type == AimData.Type.constructor)
					//{
					//	highlight.DrawPlane(aimData.x, aimData.y, aimData.z, aimData.dir);
					//}
				}
			
				if (profile) Profiler.EndSample ();
				#endregion
			
				if (aimData.hit && (add || dig || smooth || replace))
				{
					#region Setting block
					if (type>0) 
					{
						//setting big-brushing blocks
						if (brushSize > 0)
						{
							if (add) SetBlocks(aimData.x, aimData.y, aimData.z, (byte)type, brushSize, brushSphere, SetBlockMode.standard);
							else if (dig) SetBlocks(aimData.x, aimData.y, aimData.z, 0, brushSize, brushSphere, SetBlockMode.standard);
							else if (smooth) SetBlocks(aimData.x, aimData.y, aimData.z, (byte)type, brushSize, brushSphere, SetBlockMode.blur);
							else if (replace) SetBlocks(aimData.x, aimData.y, aimData.z, (byte)type, brushSize, brushSphere, SetBlockMode.replace);
						}
					
						//setting single block
						else
						{
							if (add) SetBlock(aimData.x+Coord.oppositeDirX[aimData.dir], aimData.y+Coord.oppositeDirY[aimData.dir], aimData.z+Coord.oppositeDirZ[aimData.dir], (byte)type);
							else if (dig) SetBlock(aimData.x, aimData.y, aimData.z, 0);
							else if (replace) SetBlock(aimData.x, aimData.y, aimData.z, (byte)type);
						}
					}
					#endregion 
				
					/*
					#region Setting object
					if (type>0 && !filled) 
					{
						if (add) SetBlock(aimData.x+oppositeDirX[aimData.dir], aimData.y+oppositeDirY[aimData.dir], aimData.z+oppositeDirZ[aimData.dir], (byte)type);
						else if (replace) SetBlock(aimData.x, aimData.y, aimData.z, (byte)type);
						else if (dig) SetBlock(aimData.x, aimData.y, aimData.z, 0);
					}
					#endregion
					*/
				
					#region Setting grass
					if (selected<0)
					{
						if (add || replace) SetGrass(aimData.x, aimData.z,(byte)(-selected),brushSize,brushSphere);
						else if (dig) SetGrass(aimData.x, aimData.z, 0, brushSize,brushSphere);
					}
					#endregion
				}
			
				if (profile) Profiler.EndSample ();
			}

		
			
			public struct AimData
			{
				public enum Type {none, face, obj, constructor};
				
				public bool hit;
				public Type type;
				public int x; public int y; public int z; public byte dir;
				public Chunk.Face face;
				public Collider collider;
				public int triIndex;
				public Chunk chunk;
			}
		
			public AimData GetCoordsByRay (Ray aimRay) 
			{
				AimData data = new AimData();
			
				RaycastHit raycastHitData; 
				if (!Physics.Raycast(aimRay, out raycastHitData) || //if was not hit
					!raycastHitData.collider.transform.IsChildOf(transform)) //if was hit in object that is not child of voxeland
						return data;
			
				data.collider = raycastHitData.collider;
			
				#region Terrain block
				if (raycastHitData.collider.gameObject.name == "LoResChunk")
				{
					data.chunk = raycastHitData.collider.transform.parent.GetComponent<Chunk>();
				
					if (data.chunk == null || data.chunk.faces==null || data.chunk.faces.Length==0) return data;
					if (data.chunk.stage != Chunk.Stage.complete) return data; //can hit only fully built chunks
				
					data.hit = true;
					data.type = AimData.Type.face;
					data.face = data.chunk.faces[ data.chunk.visibleFaceNums[raycastHitData.triangleIndex/2] ];
					data.x = data.face.x + data.chunk.coordX*chunkSize;
					data.y = data.face.y;
					data.z = data.face.z + data.chunk.coordZ*chunkSize;
					data.dir = data.face.dir;
				
					data.triIndex = raycastHitData.triangleIndex;
				
					return data;
				}
				#endregion
			
				#region Constructor
				//dconstructor 
				/*if (raycastHitData.collider.gameObject.name == "Constructor" && raycastHitData.collider.transform.IsChildOf(transform))
				{
					data.hit = true;
					data.chunk = raycastHitData.collider.transform.parent.GetComponent<Chunk>();
				
					//determining dir
					if (Mathf.Abs(raycastHitData.normal.y) >= Mathf.Abs(raycastHitData.normal.x) && Mathf.Abs(raycastHitData.normal.y) >= Mathf.Abs(raycastHitData.normal.z))
					{
						if (raycastHitData.normal.y > 0) data.dir = 0;
						else data.dir = 1;
					}
					else if (Mathf.Abs(raycastHitData.normal.x) >= Mathf.Abs(raycastHitData.normal.y) && Mathf.Abs(raycastHitData.normal.x) >= Mathf.Abs(raycastHitData.normal.z))
					{
						if (raycastHitData.normal.x > 0) data.dir = 2;
						else data.dir = 3;
					}
					else
					{
						if (raycastHitData.normal.z > 0) data.dir = 4;
						else data.dir = 5;
					}
	
					//coordinates
					data.x = Mathf.FloorToInt(raycastHitData.point.x - raycastHitData.normal.x*0.01f);
					data.y = Mathf.FloorToInt(raycastHitData.point.y - raycastHitData.normal.y*0.01f);
					data.z = Mathf.FloorToInt(raycastHitData.point.z - raycastHitData.normal.z*0.01f);

					//other
					data.type = AimData.Type.constructor;
				
					data.triIndex = raycastHitData.triangleIndex;
				
					return data;
				}*/
				#endregion
			
				#region Object block
				else
				{
					Transform parent = raycastHitData.collider.transform.parent;
					while (parent != null)
					{
						if (parent.name=="Chunk" && parent.IsChildOf(transform))
						{
							data.chunk = parent.GetComponent<Chunk>();
							break;
						}
						parent = parent.parent;
					}
				
					if (data.chunk == null) return data; //aiming other obj
				
					data.hit = true;
					data.type = AimData.Type.obj;
				
					Vector3 pos= raycastHitData.collider.transform.localPosition;
				
					data.x = (int)pos.x + data.chunk.offsetX; 
					data.y = (int)pos.y; 
					data.z = (int)pos.z + data.chunk.offsetZ;
	
					return data;
				}
				#endregion	
			}
		#endregion


		#region Get/Set Block
	
			public byte GetBlock (int x, int y, int z) { return data.GetBlock(x,y,z); }
		
			public void SetBlock (int x, int y, int z, byte type) { SetBlocks(x,y,z,type, 0, false, SetBlockMode.standard); } 
		
			public enum SetBlockMode {none, standard, replace, blur};
			
			public void SetBlocks (int x, int y, int z, byte type=0, int extend=0, bool spherify=false, SetBlockMode mode=SetBlockMode.none) //x,y,z are center, extend is half size (radius)
			{
				//registering undo
				#if UNITY_EDITOR
				if (recordUndo && mode != SetBlockMode.none)
				{
					undoSteps.Add( data.GetColumnMatrix(x-extend, z-extend, extend*2+1, extend*2+1) );
					//Debug.Log(undoSteps[undoSteps.Count-1].array.Length);
					if (undoSteps.Count > 32) { undoSteps.RemoveAt(0); undoSteps.RemoveAt(0); } //removing two steps...just to be sure
					UnityEditor.Undo.RecordObject(this,"Voxeland Edit");
					undo = !undo; UnityEditor.EditorUtility.SetDirty(this); //setting object change
				}
				#endif
			
				//starting timer
				#if UNITY_EDITOR
				timerSinceSetBlock.Reset();
				timerSinceSetBlock.Start();
				#endif
			
				//setting
				if (mode == SetBlockMode.standard || mode == SetBlockMode.replace)
				{
					//dconstructor
					//if (types[type].constructor != null) Scaffolding.Add(x,y,z);
					//else Scaffolding.Remove(x,y,z);			

					for (int xi = x-extend; xi<= x+extend; xi++)
						for (int yi = y-extend; yi<= y+extend; yi++) 
							for (int zi = z-extend; zi<= z+extend; zi++)
					{
						if (spherify && Mathf.Abs(Mathf.Pow(xi-x,2)) + Mathf.Abs(Mathf.Pow(yi-y,2)) + Mathf.Abs(Mathf.Pow(zi-z,2)) - 1 > extend*extend) continue;
						if (mode==SetBlockMode.replace && !types[ data.GetBlock(xi,yi,zi) ].filledTerrain) continue;
				
						data.SetBlock(xi, yi, zi, type);
					}
				}

				//blurring
				if (mode == SetBlockMode.blur) 
				{
					bool[] refExist = new bool[types.Length];
					for (int i=0; i<types.Length; i++) refExist[i] = types[i].filledTerrain;
					data.Blur(x,y,z, extend, spherify, refExist);
				}

				//mark areas as 'saved'
				for (int xi = x-extend; xi<= x+extend; xi++)
					for (int zi = z-extend; zi<= z+extend; zi++)
						data.areas[ data.GetAreaNum(xi,zi) ].save = true;

				//resetting progress
				
				//ambient
				for (int cx = Mathf.FloorToInt(1f*(x-extend-ambientMargins)/chunkSize); cx <= Mathf.FloorToInt(1f*(x+extend+ambientMargins)/chunkSize); cx++)
					for (int cz = Mathf.FloorToInt(1f*(z-extend-ambientMargins)/chunkSize); cz <= Mathf.FloorToInt(1f*(z+extend+ambientMargins)/chunkSize); cz++)
				{
					if (!chunks.CheckInRange(cx,cz)) continue;
					Chunk chunk = chunks[cx,cz];
					if (chunk!=null) chunk.stage = Chunk.Stage.forceAmbient;
					
				}

				//terrain
				for (int cx = Mathf.FloorToInt(1f*(x-extend-terrainMargins)/chunkSize); cx <= Mathf.FloorToInt(1f*(x+extend+terrainMargins)/chunkSize); cx++)
					for (int cz = Mathf.FloorToInt(1f*(z-extend-terrainMargins)/chunkSize); cz <= Mathf.FloorToInt(1f*(z+extend+terrainMargins)/chunkSize); cz++)
				{
					if (!chunks.CheckInRange(cx,cz)) continue;
					Chunk chunk = chunks[cx,cz];
					if (chunk!=null) chunk.stage = Chunk.Stage.forceAll;
					//and then calculating grass, prefabs, etc. Just because grass should change with land geometry
				}
			}
		
			public void SetGrass (int x, int z, byte type, int extend, bool spherify)
			{
				#if UNITY_EDITOR
				timerSinceSetBlock.Reset();
				timerSinceSetBlock.Start();
				#endif
			
				int minX = x-extend; int minZ = z-extend;
				int maxX = x+extend; int maxZ = z+extend;
			
				for (int xi = minX; xi<=maxX; xi++)
					for (int zi = minZ; zi<=maxZ; zi++)
						if (!spherify || Mathf.Abs(Mathf.Pow(xi-x,2)) + Mathf.Abs(Mathf.Pow(zi-z,2)) - 1 <= extend*extend) 
							data.SetGrass(xi, zi, type);
						
				Chunk chunk = chunks[Mathf.FloorToInt(1f*x/chunkSize), Mathf.FloorToInt(1f*z/chunkSize)];
				if (chunk!=null) chunk.stage = Chunk.Stage.forceAll;
			}
		
			public void ResetProgress (int x, int z, int extend) { SetBlocks(x,0,z,0,extend,false,SetBlockMode.none); }

		#endregion

		
		#region Generate
		
			public void Generate(int areaNum, int margins=20)
			{			
				//generating noise
				Noise.seed = guiNoiseSeed;
				Random.seed = guiNoiseSeed;
					
				//finding generated offset-size
				Data.Area area = data.areas[areaNum]; //it will be used only to determine size
				int offsetX=area.offsetX; 
				int offsetZ=area.offsetZ; 
				int size=area.size;
					
				//preparing matrices
				Matrix2<float> heightmap = new Matrix2<float>(size+margins*2, size+margins*2);
				heightmap.offsetX = offsetX-margins; heightmap.offsetZ = offsetZ-margins;

				//clearing
				if (clearGenerator) area.Initialize();

				//level
				if (levelGenerator.active)
				{
					levelGenerator.Generate(heightmap);
					levelGenerator.ToData(data, heightmap, margins);
				}

				if (textureGenerator.active)
				{
					textureGenerator.Generate(heightmap);
					textureGenerator.ToData(data, heightmap, margins);
				}

				if (noiseGenerator.active)
				{
					noiseGenerator.Generate(heightmap);
					noiseGenerator.ToData(data, heightmap, margins);
				}

				if (glenGenerator.active)
				{
					Matrix2<float> bedrock; Matrix2<float> additional; 
					glenGenerator.Generate(heightmap, out bedrock, out additional, margins);
					glenGenerator.ToData(data, bedrock, additional, margins);
					//data.ClampHeightmap(heightmap);
				}

				if (erosionGenerator.active)
				{
					Matrix2<float> bedrock; Matrix2<float> sediment;
					erosionGenerator.GenerateIterational(heightmap, out bedrock, out sediment);
					erosionGenerator.ToData(data, bedrock, sediment, margins);
				}

				List<Vector3> trees = null;
				if (forestGenerator.active)
				{
					trees = forestGenerator.Generate(data, offsetX,offsetZ,size,size);
					forestGenerator.ToData(data, trees);
				}

				if (grassGenerator.active)
				{
					Matrix2<byte> grass = grassGenerator.Generate(heightmap, trees);
					grassGenerator.ToData(data, grass, margins);
				}
			}

		#endregion
		
		#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			if (!this.enabled) return;
			
			Profiler.BeginSample("DrawGizmos");

		//	if (!delegatesAdded)
		//	{
				UnityEditor.EditorApplication.update -= EditorUpdate;	
				UnityEditor.SceneView.onSceneGUIDelegate -= GetMouseButton; 	
					
				//registering delegates (removing in Editor OnDisable)
				UnityEditor.SceneView.onSceneGUIDelegate += GetMouseButton; //20 ms on mouse button pressed
				UnityEditor.EditorApplication.update += EditorUpdate; //5 ms when camera moved
		//	}
				
			//releasing right button if mouse is not in scene view
			if (mouseButton == 1 &&
			    UnityEditor.SceneView.lastActiveSceneView != null &&
				UnityEditor.EditorWindow.mouseOverWindow != UnityEditor.SceneView.lastActiveSceneView &&
			    Event.current.button != 1) 
			    	mouseButton = 0;
			
			//visualizing
			if (visualizer == null) visualizer = new Visualizer(); 
			visualizer.land = this;
			visualizer.Visualize();
			Visualizer.DrawGizmos();

			Profiler.EndSample(); 
		}
		
		private bool wasRepaintEvent = false;
		public void GetMouseButton (UnityEditor.SceneView sceneview) //is registered in OnDrawGizmos
		{
			if (UnityEditor.SceneView.lastActiveSceneView == null) return;
			
			//setting mouse position and modifiers

			//if mouse is within scene view scope - all is clear
			if (UnityEditor.SceneView.mouseOverWindow == UnityEditor.SceneView.lastActiveSceneView)
			{
				//Debug.Log("SceneEvent: " + Event.current);
				if (Event.current.type == EventType.MouseDown) mouseButton = Event.current.button;
				if (Event.current.type == EventType.MouseUp) mouseButton = 0;
			}
			
			//if mouse travalled away from scene view
			else
			{
				if (mouseButton == 2)
				{
					if (wasRepaintEvent && Event.current.type == EventType.Repaint) mouseButton = 0;
					else if (Event.current.type == EventType.Repaint) wasRepaintEvent = true;
					else if (Event.current.type == EventType.MouseDrag) wasRepaintEvent = false;
				}
				
				//releasing button 1 in OnDrawGizmos	
			}
		}
	
		static public void SetHideFlagsRecursively (HideFlags flag, Transform transform)
		{
			transform.gameObject.hideFlags = flag;
			foreach (Transform child in transform) SetHideFlagsRecursively(flag,child);
		}
		#endif
		

		#region random array
		public static float[] random = {0.15f, 0.5625f, 0.69375f, 0.6125f, 0.76875f, 0.6625f, 0.4f, 0.99375f, 0.15625f, 0.68125f, 0.375f, 0.96875f, 
			0.025f, 0.63125f, 0.975f, 0.575f, 0.8875f, 0.69375f, 0.5875f, 0.775f, 0.7375f, 0.55f, 0.14375f, 0.025f, 0.69375f, 0.75f, 0.26875f, 0.35625f, 0.9f, 
			0.20625f, 0.33125f, 0.55f, 0.71875f, 0.40625f, 0.0625f, 0.98125f, 0.74375f, 0.38125f, 0.00625f, 0.8125f, 0.48125f, 0.1125f, 0.5f, 0.98125f, 0.26875f, 
			0.425f, 0.34375f, 0.7625f, 0.35625f, 0.94375f, 0.04375f, 0.29375f, 0.63125f, 0.2375f, 0.6625f, 0.85625f, 0.91875f, 0.78125f, 0.05625f, 0.33125f, 
			0.4125f, 0.6625f, 0.85f, 0.6f, 0.1125f, 0.89375f, 0.5875f, 0.25f, 0.79375f, 0.15625f, 0.5875f, 0.50625f, 0.4125f, 0.8f, 0.025f, 0.58125f, 0.5625f, 
			0.9625f, 0.475f, 0.4375f, 0.7375f, 0.60625f, 0.73125f, 0.2375f, 0.0375f, 0.50625f, 0.0125f, 0.2875f, 0.28125f, 0.65625f, 0.39375f, 0.925f, 0.83125f, 
			0.35625f, 0.49375f, 0.05625f, 0.29375f, 0.55f, 0.39375f, 0.86875f, 0.4875f, 0.3625f, 0.74375f, 0.30625f, 0.5875f, 0.625f, 0.96875f, 0.975f, 0.0375f, 
			0.3125f, 0.71875f, 0.98125f, 0.06875f, 0.66875f, 0.25625f, 0.3375f, 0.4f, 0.89375f, 0.04375f, 0.60625f, 0.0375f, 0.1625f, 0.25f, 0.13125f, 0.6375f, 
			0.04375f, 0.35625f, 0.8625f, 0.41875f, 0.78125f, 0.60625f, 0.85f, 0.10625f, 0.7125f, 0.54375f, 0.5375f, 0.85f, 0.3f, 0.3f, 0.96875f, 0.5375f, 0.925f, 
			0.89375f, 0.8625f, 0.64375f, 0.94375f, 0.225f, 0.93125f, 0.85625f, 0.275f, 0.0625f, 0.1625f, 0.63125f, 0.975f, 0.60625f, 0.5625f, 0.025f, 0.825f, 
			0.975f, 0.0125f, 0.875f, 0.8125f, 0.2875f, 0.83125f, 0.675f, 0.25625f, 0.53125f, 0.6125f, 0.25f, 0.4125f, 0.96875f, 0.94375f, 0.6125f, 0.54375f, 
			0.275f, 0.63125f, 0.94375f, 0.0625f, 0.2f, 0.9125f, 0.26875f, 0.9f, 0.84375f, 0.4375f, 0.73125f, 0.55625f, 0.375f, 0.4f, 0.925f, 0.13125f, 0.36875f, 
			0.6375f, 0.25625f, 0.725f, 0.49375f, 0.00625f, 0.89375f, 0.4f, 0.725f, 0.03125f, 0.0875f, 0.36875f, 0.88125f, 0.90625f, 0.55625f, 0.29375f, 0.625f, 
			0.9625f, 0.6875f, 0.55f, 0.70625f, 0.8125f, 0.21875f, 0.31875f, 0.63125f, 0.975f, 0.2375f, 0.25f, 0.4625f, 0.4375f, 0.96875f, 0.75625f, 0.0375f, 
			0.925f, 0.4f, 0.43125f, 0.2375f, 0.2125f, 0.21875f, 0.89375f, 0.6f, 0.40625f, 0.8625f, 0.41875f, 0.925f, 0.425f, 0.35f, 0.63125f, 0.15625f, 0.59375f, 
			0.8f, 0.7375f, 0.25f, 0.73125f, 0.01875f, 0.4625f, 0.14375f, 0.425f, 0.2625f, 0.24375f, 0.74375f, 0.05f, 0.50625f, 0.81875f, 0.9375f, 0.175f, 0.34375f, 
			0.70625f, 0.5125f, 0.8375f, 0.8125f, 0.81875f, 0.10625f, 0.43125f, 0.28125f, 0.66875f, 0.95625f, 0.68125f, 0.725f, 0.35f, 0.58125f, 0.38125f, 0.4375f, 
			0.8125f, 0.29375f, 0.3625f, 0.69375f, 0.5875f, 0.5625f, 0.53125f, 0.13125f, 0.55625f, 0.03125f, 0.2125f, 0.63125f, 0.79375f, 0.475f, 0.79375f, 0.3375f, 
			0.14375f, 0.25f, 0.875f, 0.55f, 0.60625f, 0.66875f, 0.9f, 0.29375f, 0.0625f, 0.78125f, 0.45f, 0.1567f, 0.6f, 0.03125f, 0.25f, 0.975f, 0.8f, 0.28125f, 
			0.0125f, 0.96875f, 0.29375f, 0.54375f, 0.13125f, 0.39375f, 0.04375f, 0.15625f, 0.49375f, 0.11875f, 0.5f, 0.5625f, 0.24375f, 0.86875f, 0.2125f, 0.39375f, 
			0.925f, 0.03125f, 0.03125f, 0.15625f, 0.68125f, 0.05625f, 0.3875f, 0.2f, 0.09375f, 0.65f, 0.8625f, 0.15625f, 0.24375f, 0.9875f, 0.58125f, 0.6125f, 
			0.56875f, 0.3f, 0.5375f, 0.19375f, 0.15625f, 0.81875f, 0.425f, 0.88125f, 0.04375f, 0.50625f, 0.0875f, 0.5625f, 0.11875f, 0.625f, 0.2f, 0.825f, 0.15625f, 
			0.5875f, 0.225f, 0.94375f, 0.76875f, 0.95625f, 0.275f, 0.975f, 0.56875f, 0.23125f, 0.975f, 0.04375f, 0.4875f, 0.28125f, 0.875f, 0.6625f, 0.5f, 0.8875f, 
			0.60625f, 0.125f, 0.6375f, 0.3875f, 0.25f, 0.94375f, 0.6375f, 0f, 0.06875f, 0.3f, 0.0875f, 0.53125f, 0.25f, 0.39375f, 0.36875f, 0.23125f, 0.64375f, 
			0.8375f, 0.21875f, 0.56875f, 0.2f, 0.81875f, 0.41875f, 0.44375f, 0.09375f, 0.26875f, 0.19375f, 0.9f, 0.23125f, 0.78125f, 0.89375f, 0.60625f, 0.1375f, 
			0.375f, 0.675f, 0.3125f, 0.91875f, 0.05f, 0.60625f, 0.56875f, 0.2875f, 0.075f, 0.925f, 0.725f, 0.3375f, 0.94375f, 0.91875f, 0.74375f, 0.09375f, 0.1375f, 
			0.2125f, 0.71875f, 0.48125f, 0.43125f, 0.80625f, 0.6f, 0.7625f, 0.8625f, 0.675f, 0.0625f, 0.0625f, 0.26875f, 0.4f, 0.075f, 0.10625f, 0.4875f, 0.49375f, 
			0.0125f, 0.66875f, 0.9f, 0.9f, 0.875f, 0.93125f, 0.6625f, 0.475f, 0.54375f, 0.29375f, 0.2625f, 0.775f, 0.58125f, 0.73125f, 0.61875f, 0.999f, 0.4375f, 
			0.2875f, 0.48125f, 0.45f, 0.71875f, 0.83125f, 0.3125f, 0.34375f, 0.24375f, 0.625f, 0.41875f, 0.30625f, 0.6375f, 0.84375f, 0.44375f, 0.39375f, 0.13125f, 
			0.5875f, 0.11875f, 0.05f, 0.30625f, 0.53125f, 0.29375f, 0.7f, 0.575f, 0.79375f, 0.1125f, 0.5f, 0.94375f, 0.0375f, 0.4875f, 0.93125f, 0.9875f, 0.61875f,
			0.59375f, 0.7f, 0.55f, 0.5f, 0.61875f, 0.70625f, 0.13125f, 0.999f, 0.475f, 0.45f, 0.3375f, 0.00625f, 0.725f, 0.78125f, 0.525f, 0.6f, 0.3375f, 0.1875f, 
			0.975f, 0.975f, 0.6875f, 0.65f, 0.6f, 0.3375f, 0.2375f, 0.24375f, 0.7375f, 0.875f, 0.4625f, 0.45625f, 0.81875f, 0.31875f, 0.375f, 0.475f, 0.56875f, 
			0.33125f, 0.2f, 0.3375f, 0.61875f, 0.125f, 0.65625f, 0.29375f, 0.95625f, 0.6125f, 0.24375f, 0.1375f, 0.28125f, 0.48125f, 0.1375f, 0.0375f, 0.7f, 
			0.475f, 0f, 0.4625f, 0.6375f, 0.3f, 0.3375f, 0.75625f, 0.6f, 0.4f, 0.7f, 0.10625f, 0.63125f, 0.09375f, 0.39375f, 0.29375f, 0.95625f, 0.20625f, 
			0.98125f, 0.95f, 0.81875f, 0.7875f, 0.15f, 0.875f, 0.96875f, 0.00625f, 0.41875f, 0.0625f, 0.7f, 0.85f, 0.075f, 0.925f, 0.025f, 0.6f, 0.9f, 0.36875f, 
			0.90625f, 0.88125f, 0.08125f, 0.36875f, 0.88125f, 0.15625f, 0.84375f, 0.53125f, 0.33125f, 0.48125f, 0.19375f, 0.275f, 0.6125f, 0.36875f, 0.325f, 
			0.30625f, 0.075f, 0.6f, 0.1125f, 0.825f, 0.5875f, 0.90625f, 0.1125f, 0.23125f, 0.28125f, 0.15625f, 0.675f, 0.74375f, 0.23125f, 0.25625f, 0.48125f, 
			0.7125f, 0.85625f, 0.10625f, 0.1125f, 0.025f, 0.64375f, 0.00625f, 0.9875f, 0.45f, 0.05f, 0.85625f, 0.275f, 0.11875f, 0.15625f, 0.4f, 0.09375f, 
			0.2875f, 0.95625f, 0.0375f, 0.675f, 0.38125f, 0.75f, 0.325f, 0.66875f, 0.4125f, 0.7875f, 0.71875f, 0.425f, 0f, 0.28125f, 0.70625f, 0.33125f, 0.9f, 
			0.15f, 0.9f, 0.03125f, 0.25625f, 0.675f, 0.16875f, 0.71875f, 0.675f, 0.13125f, 0.45f, 0.1125f, 0.1375f, 0.95f, 0.21875f, 0.60625f, 0.875f, 0.20625f, 
			0.4125f, 0.925f, 0.375f, 0.09375f, 0.675f, 0.9f, 0.48125f, 0.14375f, 0.675f, 0.7875f, 0.8125f, 0.8875f, 0.53125f, 0.5f, 0.45f, 0.6875f, 0.9625f, 
			0.43125f, 0.175f, 0.4875f, 0.19375f, 0.06875f, 0.13125f, 0.1375f, 0.325f, 0.1625f, 0.21875f, 0.03125f, 0.187f, 0.63125f, 0.1125f, 0.85625f, 0.88125f, 
			0.91875f, 0.2125f, 0.99375f, 0.675f, 0.0625f, 0.39375f, 0.55f, 0.00625f, 0.18125f, 0.85625f, 0.45625f, 0.68125f, 0.50625f, 0.98125f, 0.55625f, 0.8875f, 
			0.85f, 0.50625f, 0.29375f, 0.99375f, 0.5625f, 0.83125f, 0.85625f, 0.39375f, 0.83125f, 0.85625f, 0.1875f, 0.65625f, 0.43125f, 0.16875f, 0.0875f, 
			0.43125f, 0.69375f, 0.91875f, 0.51875f, 0.3875f, 0.9375f, 0.20625f, 0.3875f, 0.56875f, 0.025f, 0.4125f, 0.1375f, 0.4375f, 0.34375f, 0.1875f, 0.075f, 
			0.0875f, 0.79375f, 0.55f, 0.05f, 0.8f, 0.1625f, 0.86875f, 0.275f, 0.425f, 0.5f, 0.125f, 0.925f, 0.8375f, 0.73125f, 0.29375f, 0.35f, 0.91875f, 0.24375f, 
			0.0875f, 0.475f, 0.24375f, 0.70625f, 0.06875f, 0.26875f, 0.025f, 0.675f, 0.375f, 0.7875f, 0.425f, 0.4625f, 0.88125f, 0.80625f, 0.84375f, 0.7625f, 
			0.20625f, 0.4125f, 0.20625f, 0.3375f, 0.43125f, 0.63125f, 0f, 0.525f, 0.7125f, 0.44375f, 0.94375f, 0.7125f, 0.8375f, 0.2875f, 0.01875f, 0.55f, 0.93125f, 
			0.39375f, 0f, 0.0625f, 0.23125f, 0.01875f, 0.90625f, 0.4625f, 0.2125f, 0.98125f, 0.625f, 0.79375f, 0.9375f, 0.4875f, 0.2375f, 0.2f, 0.99375f, 0.7625f, 
			0.15f, 0.99375f, 0.83125f, 0.35625f, 0.5875f, 0.21875f, 0.88125f, 0.69375f, 0.61875f, 0.04375f, 0.21875f, 0.775f, 0.26875f, 0.54375f, 0.23125f, 0.5125f, 
			0.7625f, 0.16875f, 0.01875f, 0.69375f, 0.575f, 0.7f, 0.99375f, 0.00625f, 0.34375f, 0.725f, 0.35f, 0.1890f, 0.24375f, 0.53125f, 0.975f, 0.19375f, 0.90625f, 
			0.5125f, 0.4375f, 0.10625f, 0.45f, 0.88125f, 0.49375f, 0.31875f, 0.85625f, 0.8625f, 0.99375f, 0.8875f, 0.0375f, 0.125f, 0.38125f, 0.78125f, 0.975f, 
			0.6375f, 0.875f, 0.55625f, 0.23125f, 0.65625f, 0.36875f, 0.8875f, 0.2375f, 0.125f, 0.19375f, 0.85625f, 0.65f, 0.14375f, 0.85f, 0.53125f, 0.08125f, 
			0.7625f, 0.725f, 0.7625f, 0.7375f, 0.75625f, 0.0125f, 0.7f, 0.98125f, 0.74375f, 0.76875f, 0.94375f, 0.55625f, 0.175f, 0.36875f, 0.88125f, 0.43125f, 
			0.55625f, 0.3125f, 0.0875f, 0.95f, 0.575f, 0.60625f, 0.35f, 0.31875f, 0.325f, 0.64375f, 0.15625f, 0.09375f, 0.40625f, 0.9f, 0.30625f, 0.6625f, 0.05f, 
			0.275f, 0.41875f, 0.4125f, 0.075f, 0.49375f, 0.91875f, 0.66875f, 0.76875f, 0.26875f, 0.4625f, 0.86875f, 0.53125f, 0.75625f, 0.20625f, 0.2625f, 0.4875f, 
			0.95625f, 0.00625f, 0.6125f, 0.66875f, 0.55f, 0.84375f, 0.9375f, 0.11875f, 0.96875f, 0.64375f, 0.0625f, 0.63125f, 0.9875f, 0.78125f, 0.9375f, 0.1875f, 
			0.75f, 0.3875f, 0.30625f, 0.0375f, 0.56875f, 0.79375f, 0.9375f, 0.46875f, 0.3125f, 0.18125f, 0.88125f, 0.0125f, 0.71875f, 0.8875f, 0.9625f, 0.45625f, 
			0.50625f, 0.225f, 0.9625f, 0.10625f, 0.6375f, 0.54375f, 0.7625f, 0.7875f, 0.525f, 0.1625f, 0.88125f, 0.125f, 0.575f, 0.925f, 0.5625f, 0.43125f, 0.9f, 
			0.2f, 0.06875f, 0.8125f, 0.4875f, 0.85625f, 0.14375f, 0.85f, 0.8125f, 0.08125f, 0.35f, 0.4f, 0.01875f, 0.7375f, 0.33125f, 0.9625f, 0.78125f, 0.89375f, 
			0.58125f, 0.05f, 0.90625f, 0.33125f, 0.84375f, 0.05625f, 0.76875f, 0.79375f, 0.7125f, 0.15f, 0.7375f, 0.43125f, 0.5375f, 0.5f, 0.58125f};
		#endregion
	}

}//namespace