
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Voxeland 
{
	public struct Coord
	{
		public int x; public int y; public int z; 
		public byte dir; //0-5 sides, 6 is itself, 7 is non-existing coord
		
		static readonly public int[] oppositeDirX = {0,0,1,-1,0,0};
		static readonly public int[] oppositeDirY = {1,-1,0,0,0,0};
		static readonly public int[] oppositeDirZ = {0,0,0,0,1,-1};
		static readonly public byte[] oppositeDir = {1,0,3,2,5,4};
		
		public Coord opposite 
			{get{ return new Coord(x+oppositeDirX[dir], y+Coord.oppositeDirY[dir], z+Coord.oppositeDirZ[dir], oppositeDir[dir]);}}

		public Coord (bool e) {this.x=0; this.y=0; this.z=0; this.dir=0; if (!e) dir=7; } 
		public Coord (int x, int y, int z) {this.x=x; this.y=y; this.z=z; this.dir=0; }
		public Coord (int x, int y, int z, byte d) {this.x=x; this.y=y; this.z=z; this.dir=d; }
		
		public bool exists {get{return dir!=7;}}
		public Vector3 center { get{ return new Vector3(x+0.5f,y+0.5f,z+0.5f); } }
		public Vector3 pos { get{ return new Vector3(x,y,z); } }
		
		public static Coord operator + (Coord a, Coord b) { return new Coord(a.x+b.x, a.y+b.y, a.z+b.z); }
		public static Coord operator - (Coord a, Coord b) { return new Coord(a.x-b.x, a.y-b.y, a.z-b.z); }
		public static Coord operator ++ (Coord a) { return new Coord(a.x+1, a.y+1, a.z+1); }
		
		public static bool operator == (Coord a, Coord b) { return (a.x==b.x && a.y==b.y && a.z==b.z && a.dir==b.dir); }
		public static bool operator != (Coord a, Coord b) { return !(a.x==b.x && a.y==b.y && a.z==b.z && a.dir==b.dir); }
		public override bool Equals(object obj) { return base.Equals(obj); }
		public override int GetHashCode() {return x*10000000 + y*10000 + z*10 + dir;}

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
		
		public int BlockMagnitude2 { get { return Mathf.Abs(x)+Mathf.Abs(z); } }
		public int BlockMagnitude3 { get { return Mathf.Abs(x)+Mathf.Abs(y)+Mathf.Abs(z); } }

		public override string ToString() { return "x:"+x+" y:"+y+" z:"+z+" dir:"+dir; }
	}

	public enum EditMode {none, standard, dig, add, replace, smooth}; //standard mode is similar to add one, except the preliminary switch to opposite in add
	
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
	
	[ExecuteInEditMode] //to use OnEnable and OnDisable
	public class VoxelandTerrain : MonoBehaviour
	{
		public Data data;
		public DataHolder dataholder;

		[System.NonSerialized] public Matrix2<Chunk> chunks = new Matrix2<Chunk>(0,0);

		public VoxelandBlockType[] types;
		public VoxelandGrassType[] grass;
		public int selected; //negative is for grass
		
		public bool[] terrainExist = new bool[0];
		public bool[] ambientExist = new bool[0];
		public bool[] prefabsExist = new bool[0];
		//public bool[] constructorExist = new bool[0]; //dconstructor
		//public bool[] terrainOrConstructorExist = new bool[0]; //dconstructor

		#region vars Queue

			public Queue<System.Action[]> immediateQueue = new Queue<System.Action[]>();
			public Queue<System.Action[]> gradualQueue = new Queue<System.Action[]>();
			
			public int maxQueue = 0; //made public for editor
			public float queueProgress {get{ return 1.0f * gradualQueue.Count / maxQueue; }}

		#endregion

		private Highlight _highlight;
		public Highlight highlight
		{
			get{ if (_highlight==null) _highlight = Highlight.Create(this); return _highlight; }
			set{ _highlight = value; }
		}

		

		#region Controls

			private Ray aimRay;
			private int mouseButton = -1; //for EditorUpdate, currently pressed mouse button

			private EditMode editMode;
			private EditMode GetEditMode (bool control, bool shift)
			{
				if (!control && shift) return EditMode.dig;
				if (control && !shift) return EditMode.smooth;
				if (control && shift) return EditMode.replace;
				return EditMode.add;
			}
			
			[System.NonSerialized] public Ray oldAimRay; //made public for visualizer, assigned in Update (not in Display)
			private int oldMouseButton = -1;
			private EditMode oldEditMode;
			private Coord usedCoord;

		#endregion

		#region Settings

			public int brushSize = 0;
			public bool  brushSphere;

			public float removeDistance = 120;
			public float generateDistance = 60;
			public bool gradualGenerate = true;
			public bool multiThreadEdit;

			public float lodDistance = 50;
			public bool lodWithMaterial = true; //switch lod object using it's material, not renderer
			
			public bool limited = true;
			public int chunkSize = 30;
			public int terrainMargins = 2;
			
			public bool saveMeshes = false;
			public bool generateLightmaps = false; //this should be always off exept baking with lightmaps
			public bool rtpCompatible = false;
			public bool playmodeEdit;
			public enum ContinuousPaintingType {none,layer,unlimited}
			public ContinuousPaintingType continuousType = ContinuousPaintingType.layer;

			public bool  ambient = true;
			public float ambientFade = 0.666f;
			public int ambientMargins = 5;

			public Material highlightMaterial;
			public Material terrainMaterial;

		#endregion

		#region Far
			public bool useFar = false;
			public Mesh farMesh;
			public float farSize;
			public Far far;
		#endregion
		
		#region GUI
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
			public bool guiDisplayMaterial = false; 
		#endregion

		#region Generators

			public bool displayArea = false;

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
		
		#region Debug

			public bool  hideChunks = true;
			public bool hideWire = true;
			public bool profile = false;
			public Visualizer visualizer;

		#endregion

		#region Undo
			public bool undo; //foo to be changed on each undo to record undo state
			public bool recordUndo = true;
			public List< Matrix2<Data.Column> > undoSteps = new List<Matrix2<Data.Column>>();
		#endregion
		
		#region UnityEditor-only params
			
			public bool isEditor {get{
				#if UNITY_EDITOR
					return 
						!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode; //if not playing
						//(UnityEditor.EditorWindow.focusedWindow != null && UnityEditor.EditorWindow.focusedWindow.GetType() == System.Type.GetType("UnityEditor.GameView,UnityEditor")) //if game view is focused
						//UnityEditor.SceneView.lastActiveSceneView == UnityEditor.EditorWindow.focusedWindow; //if scene view is focused
				#else
					return false;
				#endif
			}}

			public bool isSelected {get{
				#if UNITY_EDITOR
					return UnityEditor.Selection.activeTransform == this.transform;
				#else
					return false;
				#endif
			}}

		#endregion

		#if UNITY_EDITOR
		public void OnEnable ()
		{
			//adding delegates
			UnityEditor.EditorApplication.update -= Update;	
			UnityEditor.SceneView.onSceneGUIDelegate -= GetEditorControls; 	
			
			if (isEditor) 
			{
				UnityEditor.SceneView.onSceneGUIDelegate += GetEditorControls;
				UnityEditor.EditorApplication.update += Update;
			}
		}

		public void OnDisable ()
		{
			//removing delegates
			UnityEditor.EditorApplication.update -= Update;	
			UnityEditor.SceneView.onSceneGUIDelegate -= GetEditorControls; 
		}
		#endif

		public void  Update ()
		{
			if (!this.enabled) { highlight.Clear(); return; } //Update delegate is called even when the script is off
			
			//getting non-editor controls (before Display, as it uses aimRay)
			if (!isEditor)
			{
				aimRay = Camera.main.ScreenPointToRay(Input.mousePosition);
				aimRay.origin = Camera.main.transform.position;
				
				mouseButton = Input.GetMouseButton(0) ? 0 : -1;

				if (mouseButton == 0)
					editMode = GetEditMode( control:Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl), shift:Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift) );
				else 
					editMode = EditMode.none;
			}

			//display
			Display();

			//edit
			if ((isEditor && isSelected) || playmodeEdit )
			{ 
				//if aim ray change OR edit mode change
				if (!Mathf.Approximately(aimRay.origin.x,oldAimRay.origin.x) || !Mathf.Approximately(aimRay.origin.y,oldAimRay.origin.y) || !Mathf.Approximately(aimRay.origin.z,oldAimRay.origin.z) ||
					!Mathf.Approximately(aimRay.direction.x,oldAimRay.direction.x) || !Mathf.Approximately(aimRay.direction.y,oldAimRay.direction.y) || !Mathf.Approximately(aimRay.direction.z,oldAimRay.direction.z) || 
					editMode != oldEditMode)
					{
						Coord aimCoord = PointOut(aimRay, highlight);
				
						//if pointing to terrain and mouse pressed
						if (aimCoord.exists && aimCoord != usedCoord && editMode != EditMode.none) 
						{
							if (selected>0)
								SetBlocks(aimCoord, (byte)selected, brushSize, brushSphere, editMode, updateCollider:continuousType==ContinuousPaintingType.unlimited);
							else //for grass (negative selected num)
								SetGrass(aimCoord, (byte)(-selected), brushSize, brushSphere, editMode);
				
							usedCoord = aimCoord;
						}

						//if pointing to non-terrain
						if (!aimCoord.exists) highlight.Clear(); 
					}
			}
			else highlight.Clear(); //clearing highlight if not editable - just in case

			//controls change
			oldAimRay = aimRay;
			oldMouseButton = mouseButton;
			oldEditMode = editMode;
		}
	
		#region Display

			//public void Display () { Display(false); }
			public void Display () //called every frame
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
			
				#region Creating material if it has not been assigned
				if (terrainMaterial==null) terrainMaterial = new Material(Shader.Find("Voxeland/Standard"));
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
			
				#region Preparing chunk matrix
					//all the chunks.array should be filled with chunks. There may be empty chunks (if they are out of build dist), but they should not be null
					//TODO: matrix changes too often on chunk seams. Make an offset.

					//finding build center and range (they differ on limited and infinite tarrains)
					float range = 0; Vector3 center = Vector3.zero;
					if (limited) { range = (data.areaSize-chunkSize)/2f; center = new Vector3(range,range,range); }
					else { range = removeDistance; center = aimRay.origin; }
					
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

						//re-filling new matrix using old chunks
						List<Chunk> unusedChunks = new List<Chunk>();
						List<Chunk> movedChunks = new List<Chunk>();
						newChunks.Refill(chunks, unused:unusedChunks, swapped:movedChunks);

						//filling empty holes with new chunks
						for (int x=newChunks.offsetX; x<newChunks.offsetX+newChunks.sizeX; x++)
							for (int z=newChunks.offsetZ; z<newChunks.offsetZ+newChunks.sizeZ; z++)
						{
							if (newChunks[x,z]==null)
							{
								newChunks[x,z] = Chunk.CreateChunk(this);
								newChunks[x,z].Init(x,z);
							}
						}

						//removing unused chunks left
						for (int i=0; i<unusedChunks.Count; i++)
							DestroyImmediate(unusedChunks[i].gameObject);

						//saving array
						chunks = newChunks;

						//clearing queue
						gradualQueue.Clear();

						foreach (Chunk chunk in chunks.OrderedFromCenter())
						{ 
							//skipping chunks that are out of build distance
							if (!limited && //in limited terrain all chunks should be built
								(chunk.offsetX + chunkSize < aimRay.origin.x - generateDistance ||
								chunk.offsetX > aimRay.origin.x + generateDistance ||
								chunk.offsetZ + chunkSize < aimRay.origin.z - generateDistance ||
								chunk.offsetZ > aimRay.origin.z + generateDistance))
									continue;

							//building only unbuilt chunks
							if (chunk.stage.complete) continue;
							
							chunk.EnqueueAll(gradualQueue);
							chunk.EnqueueUpdateCollider(gradualQueue);
						}

						//setting max queue number to display progress
						maxQueue = gradualQueue.Count;
					}
				#endregion

				#region AutoGenerating

					//finding a need to autogenerate
					bool autoGenerate = false;

					bool isPlaying = true;
					#if UNITY_EDITOR
					if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) isPlaying=false;
					#endif

					if (!isPlaying && autoGenerateEditor) autoGenerate = true;
					if (isPlaying && autoGeneratePlaymode) autoGenerate = true;
					
					//finding if there are uninitialized areas within build distance
					//note that build dist should be less than area size
					//this section is so complex to display progress bar
					if (!limited && autoGenerate)
					{
						int boundsDist = (int)(generateDistance + chunkSize + 1);

						//finding if any area is not initialized
						bool hasNotInitializedArea = false;
						for (int x=(int)aimRay.origin.x-boundsDist; x<=(int)aimRay.origin.x+boundsDist; x+=boundsDist)
							for (int z=(int)aimRay.origin.z-boundsDist; z<=(int)aimRay.origin.z+boundsDist; z+=boundsDist)
								if (!data.areas[ data.GetAreaNum(x,z) ].initialized) hasNotInitializedArea = true;

						if (hasNotInitializedArea)
						{
							//skipping frame if autogenerate just started
							//this should redraw game gui to display "Please Wait" panel
							if (!autoGenerateStarted) { autoGenerateStarted = true; return; }
							
							//calculating number of non-initialized areas
							List<int> notInitializedNums = new List<int>();
							for (int x=(int)aimRay.origin.x-boundsDist; x<=(int)aimRay.origin.x+boundsDist; x+=boundsDist)
								for (int z=(int)aimRay.origin.z-boundsDist; z<=(int)aimRay.origin.z+boundsDist; z+=boundsDist)
							{
								int areaNum = data.GetAreaNum(x,z);
								if (!data.areas[areaNum].initialized && !notInitializedNums.Contains(areaNum)) notInitializedNums.Add(areaNum);
							}


							//generating
							int counter = 0;
							for (int x=(int)aimRay.origin.x-boundsDist; x<=(int)aimRay.origin.x+boundsDist; x+=boundsDist)
								for (int z=(int)aimRay.origin.z-boundsDist; z<=(int)aimRay.origin.z+boundsDist; z+=boundsDist)
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
			
					if (limited) useFar = false;
					
					if (useFar)
					{
						if (far == null) 
						{ 
							far = Far.Create(this);
							far.x = (int)aimRay.origin.x; far.z = (int)aimRay.origin.z;
							far.Build(); 
						}
						far.x = (int)aimRay.origin.x; far.z = (int)aimRay.origin.z;
						
						//rebuilding far on matrix change. Rebuild on chunk build is done in chunk via process
						if (matrixChanged) far.Build();
					}
					else if (far != null) DestroyImmediate(far.gameObject);

				if (profile) Profiler.EndSample();
				#endregion
				
				#region Building terrain
				if (profile) Profiler.BeginSample ("Rebuild");
				
					//building forced chunks forced
					while (immediateQueue.Count != 0)
					{
						System.Action[] actions = immediateQueue.Dequeue();
						for (int a=0; a<actions.Length; a++) actions[a]();
					}

					//building gradual chunks gradually
					while (gradualQueue.Count != 0)
					{
						System.Action[] actions = gradualQueue.Dequeue();
						for (int a=0; a<actions.Length; a++) actions[a]();
						if (gradualGenerate) break;
					}
					

			
				if (profile) Profiler.EndSample();
				#endregion
				
				#region Switching lods
				if (profile) Profiler.BeginSample ("Switching Lods");

					int minX = Mathf.FloorToInt(1f*(aimRay.origin.x-lodDistance)/chunkSize); int maxX = Mathf.FloorToInt(1f*(aimRay.origin.x+lodDistance)/chunkSize);
					int minZ = Mathf.FloorToInt(1f*(aimRay.origin.z-lodDistance)/chunkSize); int maxZ = Mathf.FloorToInt(1f*(aimRay.origin.z+lodDistance)/chunkSize);
					
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
				
				#region Resetting Collision after mouse up

					if (oldMouseButton>=0 && mouseButton<0) 
					{
						for (int i=0; i<chunks.array.Length; i++)
						{
							Chunk chunk = chunks.array[i];
							if (chunk!=null && !chunk.stage.updateColliderComplete) chunk.UpdateCollider();
						}
					}

				#endregion
 
				if (profile) Profiler.EndSample();
			}
		#endregion
		

		#region Point Out

			public Coord PointOut (Ray ray, Highlight highlight=null)
			{
				//raycasting and miss-hit
				RaycastHit raycastHitData; 
				if ( !Physics.Raycast(aimRay, out raycastHitData) || //if was not hit
					!raycastHitData.collider.transform.IsChildOf(transform) ) //if was hit in object that is not child of voxeland
				{
					if (highlight!=null) highlight.Clear();
					return new Coord(false);
				}

				#region Terrain block
				if (raycastHitData.collider.gameObject.name == "LoResChunk")
				{
					Chunk chunk = raycastHitData.collider.transform.parent.GetComponent<Chunk>();

					if (chunk == null || chunk.faces==null || chunk.faces.Length==0 || !chunk.stage.complete) //can hit only fully built chunks with faces
						{ if (highlight!=null) highlight.Clear(); return new Coord(false); }

					Coord coord = chunk.triToCoord[raycastHitData.triangleIndex/2];

					if (highlight!=null)
					{
						if (brushSize==0) highlight.DrawFace(chunk, chunk.GetFaceByCoord(coord));
						else if (brushSphere) highlight.DrawSphere(coord.center + transform.position, brushSize);
						else highlight.DrawBox(coord.center + transform.position, new Vector3(brushSize,brushSize,brushSize));
					}

					return coord;
				}
				#endregion
			
				#region Constructor
				//dconstructor 
				//TODO: old approach using aimData
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
					Chunk chunk = null;
					
					Transform parent = raycastHitData.collider.transform.parent;
					while (parent != null)
					{
						if (parent.name=="Chunk" && parent.IsChildOf(transform))
						{
							chunk = parent.GetComponent<Chunk>();
							break;
						}
						parent = parent.parent;
					}
				
					if (chunk == null) //aiming other obj
						{ if (highlight!=null) highlight.Clear(); return new Coord(false); } 
				
					if (highlight != null)
						highlight.DrawBox(raycastHitData.collider.bounds.center, raycastHitData.collider.bounds.extents);

					Vector3 pos= raycastHitData.collider.transform.localPosition;
					return new Coord((int)pos.x + chunk.offsetX, (int)pos.y, (int)pos.z + chunk.offsetZ);
				}
				#endregion	
			}

			//old PointOut name
			public Coord GetCoordsByRay (Ray ray) { return PointOut(ray,highlight:null); }

			//outdated
			public void  Edit (Ray aimRay, int type, bool add=false, bool dig=false, bool smooth=false, bool replace=false)
			{
				//getting edit mode
				EditMode editMode = EditMode.none;
				if (dig) editMode = EditMode.dig;
				else if (add) editMode = EditMode.add;
				else if (replace) editMode = EditMode.replace;
				else if (smooth) editMode = EditMode.smooth;
				
				//aiming terrain block
				Voxeland.Coord coord = PointOut(aimRay, highlight:highlight); //note that a custom highlight could be added there
				
				//editing
				if (coord.exists && editMode != Voxeland.EditMode.none) //if aiming to terrain (not the sky or othe object) and clicked one of edit modes
				{
					//setting terrain
					if (selected>0) SetBlocks(coord, (byte)selected, extend:0, spherify:false, mode:editMode);

					//setting grass (negative selected num)
					else SetGrass(coord, (byte)(-selected), extend:0, spherify:false, mode:editMode);
				}
			}

		#endregion


		#region Get/Set Block
			
			public byte GetBlock (Coord coord) { return data.GetBlock(coord.x, coord.y, coord.z); }
		
			public void SetBlock (Coord coord, byte type, EditMode mode=EditMode.standard, bool updateCollider=true) 
				{ SetBlocks(coord, type, extend:0, spherify:false, mode:mode, updateCollider:updateCollider); } 

			public void SetBlocks (Coord coord, byte type=0, int extend=0, bool spherify=false, EditMode mode=EditMode.standard, bool updateCollider=true)
			{
				//switching coord to opposite if add-mode with zero-extend
				if (mode == EditMode.add && extend == 0) coord = coord.opposite;

				//setting type 0 if mode is dig
				if (mode == EditMode.dig) type = 0;
				
				//registering undo
				#if UNITY_EDITOR
				if (recordUndo && mode!=EditMode.none)
				{
					undoSteps.Add( data.GetColumnMatrix(coord.x-extend, coord.z-extend, extend*2+1, extend*2+1) );
					//Debug.Log(undoSteps[undoSteps.Count-1].array.Length);
					if (undoSteps.Count > 32) { undoSteps.RemoveAt(0); undoSteps.RemoveAt(0); } //removing two steps...just to be sure
					UnityEditor.Undo.RecordObject(this,"Voxeland Edit");
					undo = !undo; UnityEditor.EditorUtility.SetDirty(this); //setting object change
				}
				#endif
			
				//setting
				if (mode != EditMode.smooth) //all modes except smooth
				{
					//dconstructor
					//if (types[type].constructor != null) Scaffolding.Add(x,y,z);
					//else Scaffolding.Remove(x,y,z);			

					if (mode!=EditMode.none) 
					for (int xi = coord.x-extend; xi<= coord.x+extend; xi++)
						for (int yi = coord.y-extend; yi<= coord.y+extend; yi++) 
							for (int zi = coord.z-extend; zi<= coord.z+extend; zi++)
					{
						//skipping blocks that are out-of-radius
						if (spherify && Mathf.Abs(Mathf.Pow(xi-coord.x,2)) + Mathf.Abs(Mathf.Pow(yi-coord.y,2)) + Mathf.Abs(Mathf.Pow(zi-coord.z,2)) - 1 > extend*extend) continue;
						
						//if replace then leaving empty blocks empty
						if (mode==EditMode.replace && !types[ data.GetBlock(xi,yi,zi) ].filledTerrain) continue;
				
						data.SetBlock(xi, yi, zi, type);
					}
				}

				//blurring
				else
				{
					bool[] refExist = new bool[types.Length];
					for (int i=0; i<types.Length; i++) refExist[i] = types[i].filledTerrain;
					data.Blur(coord.x,coord.y,coord.z, extend, spherify, refExist);
				}

				//mark areas as 'saved'
				for (int xi = coord.x-extend; xi<= coord.x+extend; xi++)
					for (int zi = coord.z-extend; zi<= coord.z+extend; zi++)
						data.areas[ data.GetAreaNum(xi,zi) ].save = true;

				//resetting progress

				
				//ambient
				for (int cx = Mathf.FloorToInt(1f*(coord.x-extend-ambientMargins)/chunkSize); cx <= Mathf.FloorToInt(1f*(coord.x+extend+ambientMargins)/chunkSize); cx++)
					for (int cz = Mathf.FloorToInt(1f*(coord.z-extend-ambientMargins)/chunkSize); cz <= Mathf.FloorToInt(1f*(coord.z+extend+ambientMargins)/chunkSize); cz++)
				{
					if (!chunks.CheckInRange(cx,cz)) continue;
					Chunk chunk = chunks[cx,cz];
					if (chunk!=null) chunk.EnqueueAmbient(immediateQueue);
				}

				//terrain
				for (int cx = Mathf.FloorToInt(1f*(coord.x-extend-terrainMargins)/chunkSize); cx <= Mathf.FloorToInt(1f*(coord.x+extend+terrainMargins)/chunkSize); cx++)
					for (int cz = Mathf.FloorToInt(1f*(coord.z-extend-terrainMargins)/chunkSize); cz <= Mathf.FloorToInt(1f*(coord.z+extend+terrainMargins)/chunkSize); cz++)
				{
					if (!chunks.CheckInRange(cx,cz)) continue;
					Chunk chunk = chunks[cx,cz];
					if (chunk!=null) 
					{
						chunk.EnqueueAll(immediateQueue);
						if (updateCollider) chunk.EnqueueUpdateCollider(immediateQueue);
					}
				}
			}
		
			public void SetGrass (Coord coord, byte type, int extend=0, bool spherify=false, EditMode mode = EditMode.standard)
			{
				if (mode == EditMode.none) return;
				
				//setting type 0 if mode is dig
				if (mode == EditMode.dig) type = 0;

				int minX = coord.x-extend; int minZ = coord.z-extend;
				int maxX = coord.x+extend; int maxZ = coord.z+extend;
				
				for (int xi = minX; xi<=maxX; xi++)
					for (int zi = minZ; zi<=maxZ; zi++)
						if (Mathf.Abs(Mathf.Pow(xi-coord.x,2)) + Mathf.Abs(Mathf.Pow(zi-coord.z,2)) - 1 <= extend*extend) 
				{
					//skipping blocks that are out-of-radius
					if (spherify && Mathf.Abs(Mathf.Pow(xi-coord.x,2)) + Mathf.Abs(Mathf.Pow(zi-coord.z,2)) - 1 > extend*extend) continue;
					
					//if replace then leaving empty blocks empty
					if (mode==EditMode.replace && data.GetGrass(xi,zi)==0) continue;

					//set block
					data.SetGrass(xi, zi, type);
				}
						
				Chunk chunk = chunks[Mathf.FloorToInt(1f*coord.x/chunkSize), Mathf.FloorToInt(1f*coord.z/chunkSize)];
				if (chunk!=null) chunk.EnqueueGrass(immediateQueue);
			}

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
				int size=data.areaSize;
					
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

			if (visualizer == null) visualizer = new Visualizer(); 
			visualizer.land = this;
			visualizer.Visualize();
			Visualizer.DrawGizmos();
		}
		
		//private bool wasRepaintEvent = false;
		public void GetEditorControls (UnityEditor.SceneView sceneview) //is registered in OnDrawGizmos
		{
			if (!isEditor) return;
			
			//finding aiming ray
			Vector2 mousePos = Event.current.mousePosition; 
			mousePos.y = Screen.height - mousePos.y - 40;
			aimRay = UnityEditor.SceneView.lastActiveSceneView.camera.ScreenPointToRay(mousePos);

			//setting current camera position as aiming ray origin
			aimRay.origin = UnityEditor.SceneView.lastActiveSceneView.camera.transform.position;

			//assigning mouse button
			if (Event.current.type == EventType.MouseDown) mouseButton = Event.current.button;
			if (Event.current.rawType == EventType.MouseUp) mouseButton = -1;

			//setting edit mode
			if (mouseButton == 0 && !Event.current.alt)
				editMode = GetEditMode( control:Event.current.control, shift:Event.current.shift );
			else 
				editMode = EditMode.none;

			//releasing mode if non-continious and mouse was already pressed
			if (editMode!=EditMode.none && continuousType==ContinuousPaintingType.none && oldMouseButton==0) editMode = EditMode.none;
			
			//resetting controls if Voxeland is not selected
			if (!isSelected)
			{ 
				mouseButton = -1; 
				editMode = EditMode.none;
			}
		}

		#endif
		


	}

}//namespace