
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Voxeland 
{
	[CustomEditor(typeof(VoxelandTerrain))]
	public class VoxelandEditor : Editor
	{
		public bool guiShowAbout = false;
		GUIStyle linkStyle;
		Texture2D guiPluginIcon;

		VoxelandTerrain script; //aka target

		public int backgroundHeight = 0; //to draw type background
		public int oldSelectedType = 0; //to repaint gui with new background if new type was selected

		public void OnDisable ()
		{
			script = (VoxelandTerrain)target;
			
			//assigning delegates in Voxeland.OnDrawGizmos to make run withous selecting Voxeland
			UnityEditor.SceneView.onSceneGUIDelegate -= script.GetMouseButton;
			UnityEditor.EditorApplication.update -= script.EditorUpdate;
		}
		
		
		public void  OnSceneGUI ()
		{	
			if (script == null) script = (VoxelandTerrain)target;
			if (!script.enabled) return;

			//disabling selection
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
			
			Vector2 mousePos = Event.current.mousePosition; 
			mousePos.y = Screen.height - mousePos.y - 40;
			
			#region Undo-Redo
			if (Event.current.commandName == "UndoRedoPerformed" && Event.current.type == EventType.ValidateCommand && script.undoSteps.Count != 0) //if not check event type than undo will be done twice: on validate and on layout
			{
				int lastStepNum = script.undoSteps.Count-1;
				Matrix2<Data.Column> lastStep = script.undoSteps[lastStepNum];
				
				//marking chunks progress to reset
				script.SetBlocks(lastStep.offsetX + lastStep.sizeX/2, 0, lastStep.offsetZ + lastStep.sizeZ/2, extend:lastStep.sizeX/2, mode:VoxelandTerrain.SetBlockMode.none); 
				//SetBlockMode.none just resets progress
				//and btw we can do with these columns whatever, they will be replaced with undo
				
				script.data.SetColumnMatrix( script.undoSteps[lastStepNum] ); //setting last undo step matrix
				script.undoSteps.RemoveAt(lastStepNum); //removing last step
			}
			#endregion
			
			
			//script.Display (UnityEditor.SceneView.lastActiveSceneView.camera.transform.position); //is done in EditorUpdate delegate
			if (!Event.current.alt && !script.guiSelectArea && script.enabled)
			{
				Ray aimRay = UnityEditor.SceneView.lastActiveSceneView.camera.ScreenPointToRay(mousePos);
				bool mouseDown = Event.current.type == EventType.MouseDown && Event.current.button == 0;
				bool shift = Event.current.shift;
				bool control = Event.current.control;
			
				script.Edit(aimRay, script.selected,
					mouseDown && !shift && !control,
					mouseDown && shift && !control,
					mouseDown && control && !shift,
					mouseDown && control && shift);
			}

			#region Area selection

				//drawing selected area
				if (script.guiGenerate && script.guiSelectedAreaShow && script.data.areas != null)
				{
					Handles.color = new Color(0.5f, 0.7f, 1f, 1f);
					Visualizer.DrawArea(script, script.data.areas[ script.guiSelectedAreaNum ]);
				}
				
				//selecting area (if select area mode is on)
				if (script.guiSelectArea && script.data.areas != null)
				{
					Ray aimRay = UnityEditor.SceneView.lastActiveSceneView.camera.ScreenPointToRay(mousePos);
					VoxelandTerrain.AimData coordsData = script.GetCoordsByRay(aimRay);
					if (coordsData.hit)
					{
						Handles.color = new Color(0.5f, 0.7f, 1f, 0.7f);
						Visualizer.DrawArea(script, script.data.GetArea(coordsData.x, coordsData.z));

						if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
						{
							script.guiSelectedAreaNum =script.data.GetAreaNum(coordsData.x, coordsData.z);
							script.guiSelectArea = false;
						}
					}
					SceneView.RepaintAll();
				}
			#endregion

			#region Focusing on Brush

				if (script.guiFocusOnBrush && Event.current.keyCode == KeyCode.G && Event.current.type == EventType.KeyDown) 
				{ 
					Ray aimRay = UnityEditor.SceneView.lastActiveSceneView.camera.ScreenPointToRay(mousePos);
					VoxelandTerrain.AimData coordsData = script.GetCoordsByRay(aimRay);
					if (coordsData.hit)
					{
						//UnityEditor.SceneView.lastActiveSceneView.pivot = new Vector3(coordsData.x, coordsData.y, coordsData.z);
						//UnityEditor.SceneView.lastActiveSceneView.size = 10f;

						UnityEditor.SceneView.lastActiveSceneView.LookAt( 
							new Vector3(coordsData.x, coordsData.y, coordsData.z), 
							UnityEditor.SceneView.lastActiveSceneView.rotation,
							20f, 
							UnityEditor.SceneView.lastActiveSceneView.orthographic, 
							false);
					}
				}

			#endregion
		}

		public void Rebuild ()
		{
			//re-loading data
			script.data.LoadFromByteList(script.data.compressed);
					
			//setting terrain size
			//if (script.guiLimitedSize != script.terrainSize || script.guiAreaSize != script.data.areaSize)
			if (script.guiAreaSize != script.data.areaSize)
				New(reason:"Change Terrain Area Size");
					
			//applying settings
			script.chunkSize = script.guiNewChunkSize;

			script.data.emptyColumn = new Data.Column(true); //sending boolean argument with fn will create a list
			script.data.emptyColumn.AddBlocks(script.guiEmptyColumnType, script.guiEmptyColumnHeight);
			
			//adding renderer if rtp compatibility is on
			if (script.rtpCompatible)
			{
				MeshRenderer renderer = script.GetComponent<MeshRenderer>();
				if (renderer==null) 
				{ 
					renderer = script.gameObject.AddComponent<MeshRenderer>(); 
					renderer.hideFlags = HideFlags.HideInInspector;
				}
				script.terrainMaterial = renderer.sharedMaterial;
			}

			//rebuilding
			script.chunks.Clear();
			script.Display(true);
		}

		public void Clear (string reason="Clear Terrain")
		{
			if (EditorUtility.DisplayDialog(reason, "This will remove all terrain data. This operation cannot be undone.", "Clear", "Cancel"))
			{
				script.data.areaSize = script.guiAreaSize;

				//clearing terrain and saving data
				script.data.Clear();
				script.data.compressed = script.data.SaveToByteList();
				UnityEditor.EditorUtility.SetDirty(script.data);

				//does not contain rebuild operation as it could be called from rebuild
			}
			script.guiAreaSize = script.data.areaSize; //if was not cleared - resetting gui area size
		}

		public void New (string reason="Create New Terrain")
		{
			Clear(reason:reason);

			//creating initial level
			script.data.AddHeight(30, 0,0,script.data.areaSize,script.data.areaSize, type:1);
			script.data.areas[5050].save = true;

			//saving data
			script.data.compressed = script.data.SaveToByteList();
			UnityEditor.EditorUtility.SetDirty(script.data);
		}
		
		public override void  OnInspectorGUI ()
		{
			script = (VoxelandTerrain)target;
			
			EditorGUI.indentLevel = 0;
	
			LayoutTools.Start();
			
			LayoutTools.NewLine(5);
			LayoutTools.QuickInt(ref script.brushSize, "Brush Size:", "The extend of the brush. If size is more than 0 a volume brush is turned on, which allows to set multiple blocks in one click.", max:6);
			LayoutTools.QuickBool(ref script.brushSphere, "Spherify", "Smoothes volume brush and shapes it in form of pseudo-sphere. Please note that Voxeland data is a cubical structure which cannot give clear spheres – all round objects will become a bit aliased");
			LayoutTools.NewLine(50); EditorGUI.HelpBox(LayoutTools.AutoRect(), "Press Left Click to add block,\nShift-Left Click to dig block,\nCtrl-Left Click to smooth blocks,\nCtrl-Shift-Left Click to replace block", MessageType.None);

			#region Rebuild
				LayoutTools.NewLine(5);
				if (LayoutTools.QuickButton("Rebuild", "Removes all chunks and creates new terrain mesh. This will not delete terrain data (unless terrain size is changed), it just re-creates same terrain with new settings. Rebuild operation is performed after each script compile."))
					Rebuild();
			#endregion

			#region Block Types
				LayoutTools.QuickFoldout(ref script.guiTypes, "Block Types", "This section displays the list of of block type cards. Each card corresponds to a block type on terrain. All block settings changes require Rebuild operation.");
				if (script.guiTypes)
				{
					LayoutTools.margin += 5;

					//creating first two types if array is empty
					if (script.types==null || script.types.Length==0) script.types = new VoxelandBlockType[] {new VoxelandBlockType("Empty", false), new VoxelandBlockType("Ground", true)};

					//drawing types
					//DrawType(script.types[t], t, t==script.selected);
					for (int t=1; t<script.types.Length; t++)
					{
						VoxelandBlockType type = script.types[t];
						
						//drawing back rect
						LayoutTools.NewLine(1);
						if (t==script.selected)
						{
							Rect backRect = new Rect(LayoutTools.lastPos.x-3, LayoutTools.lastPos.y-2, EditorGUIUtility.currentViewWidth-LayoutTools.margin, backgroundHeight+8);
							GUI.Box(backRect, "");
						}
						
						//texture thumb
						LayoutTools.NewLine(27); 
						if (GUI.Button(LayoutTools.AutoRect(27+3), "", "Box")) script.selected = (byte)t;
						if (type.filledTerrain && type.texture != null) EditorGUI.DrawPreviewTexture(new Rect(LayoutTools.lastPos.x-27-3+1,LayoutTools.lastPos.y+1,25,25), type.texture);
	
						//selector by name (if unselected)
						LayoutTools.lastPos.y+=3; LayoutTools.lastHeight-=8; 
						if (t!=script.selected && GUI.Button(LayoutTools.AutoRect(), type.name, "Label")) script.selected = (byte)t;
						LayoutTools.lastHeight+=8; 
						
						#region Draw Selected Type
						if (t==script.selected)
						{
							LayoutTools.margin += 30;

							int oldRectY = (int)LayoutTools.lastPos.y; //to calculate background height

							LayoutTools.lastHeight-=10; 
							type.name = EditorGUI.TextField(LayoutTools.AutoRect(), type.name);
							LayoutTools.lastHeight+=10;  

							if (LayoutTools.QuickBool(ref type.filledTerrain, "Terrain", "Check this the block is filled with land stratum, and uncheck if it is the air or prefab block", isLeft:true))
							{
								LayoutTools.margin += 17;

								/*LayoutTools.NewLine(); 
								EditorGUI.LabelField(LayoutTools.AutoRect(50), new GUIContent("Main:", "Diffuse texture map used by this block."));
								EditorGUI.LabelField(LayoutTools.AutoRect(50), new GUIContent("Bump:", "Normals map used by this block. To use it assign BumpSpec shader in terrain material"));

								LayoutTools.NewLine(50);
								type.texture = (Texture)EditorGUI.ObjectField(LayoutTools.AutoRect(50), type.texture, typeof(Texture), false);
								type.bumpTexture = (Texture)EditorGUI.ObjectField(LayoutTools.AutoRect(50), type.bumpTexture, typeof(Texture), false);*/
							//	if (!script.rtpCompatible)
							//	{
									LayoutTools.QuickColor(ref type.color, "Color", "Color tint, multiplies texture color", fieldSize:0.25f);
									LayoutTools.QuickTextureSingleLine(ref type.texture, "Albedo", "Albedo (RGB), and Gloss/Metallic (A) if material does not have Specular/Metallic map in any channel", fieldSize:0.25f);
									LayoutTools.QuickTextureSingleLine(ref type.bumpTexture, "Normal Map", "Normal Map", fieldSize:0.25f);
									LayoutTools.QuickTextureSingleLine(ref type.specGlossMap, "Spec/Metal, Gloss Map", "Specular or Metallic (RGB) (depending on shader setup) and Smoothness (A)", fieldSize:0.25f);
									LayoutTools.QuickColor(ref type.specular, "Spec/Metal, Gloss Color", "Specular or Metallic (RGB) (depending on shader setup) and Smoothness (A)", fieldSize:0.25f);
									if (type.specGlossMap==null)
									LayoutTools.NewLine(30); EditorGUI.HelpBox(LayoutTools.AutoRect(), "Using Albedo alpha as Spec/Metal if Spec/Metal map is not assigned.", MessageType.None);
									LayoutTools.QuickFloat(ref type.tile, "Tile", "Number of times texture repeated per 1 world unit", fieldSize:0.25f);
							//	}
							//	else { LayoutTools.NewLine(); EditorGUI.HelpBox(LayoutTools.AutoRect(), "RTP Compatibility mode is on.", MessageType.None); }

								LayoutTools.NewLine(5);
								LayoutTools.QuickBool(ref type.grass, "Grass", "Turn this on if block has standing grass above. The type of grass is set using Grass section. You can place different grass types on the same blocks.", isLeft:true);
								if (type.grass)
									LayoutTools.QuickColor(ref type.grassTint, "Grass Tint", "Grass growing above this block will be multiplied by this color. Use white to leave the grass unchanged.", fieldSize:0.25f);

								LayoutTools.margin -= 17;
							}

							LayoutTools.NewLine(5);
							LayoutTools.QuickBool(ref type.filledPrefabs, "Prefabs", "Objects could be placed or removed using terrain editor. Placing object with prefab will instantiate prefab in scene. Objects are taken from the list at random.", isLeft:true);
							if (type.filledPrefabs)
							{
								LayoutTools.margin +=15;
								
								//NewLine(); 
								//EditorGUI.LabelField(AutoRect(0.78f), "Prefabs:");
								//EditorGUI.LabelField(AutoRect(0.2f), "Chance:");
					
								if (type.prefabs == null) type.prefabs = new Transform[1];
								if (type.prefabs.Length == 0) type.prefabs = new Transform[1];
					
								for (int i=0; i<type.prefabs.Length; i++)
								{
									LayoutTools.NewLine();
									type.prefabs[i] = (Transform)EditorGUI.ObjectField(LayoutTools.AutoRect(0.78f), type.prefabs[i], typeof(Transform), false);
									//type.prefabs[i].chance = EditorGUI.FloatField(AutoRect(0.2f), type.prefabs[i].chance);
								}
					
								int selectedPrefab = type.prefabs.Length-1;
								LayoutTools.ArrayButtons(ref type.prefabs, ref selectedPrefab, false);

								LayoutTools.margin -= 15;
							}

							LayoutTools.NewLine(5);
							LayoutTools.QuickBool(ref type.filledAmbient, "Occlude Ambient", "This block will occlude ambient if it is turned on.", isLeft:true);

							LayoutTools.NewLine(5);
							LayoutTools.margin -= 30;
							LayoutTools.AutoRect();

							//calculating background height
							int temp = (int)LayoutTools.lastPos.y - oldRectY;
							if (temp > 25) backgroundHeight = temp;
						}//if type selected
						#endregion
					
					}//for in types

					//making gui repaint if new type was selected - to draw background properly
					if (oldSelectedType != script.selected) { oldSelectedType=script.selected; this.Repaint(); return; }

					//drawing array buttons
					LayoutTools.ArrayButtons<VoxelandBlockType>(ref script.types, ref script.selected, drawUpDown:false);
					for (int i=0; i<script.types.Length; i++) 
						if (script.types[i] == null)
							script.types[i] = new VoxelandBlockType("New", true);

					LayoutTools.margin -= 5;
				}

			#endregion
			
			#region Grass
			LayoutTools.QuickFoldout(ref script.guiGrass, "Grass", "Vertical grass meshes. Grass is set per-column (in 2D), not per-block (3D).");
			if (script.guiGrass) 
			{
				LayoutTools.margin += 5; 
				
				if (script.grass==null || script.grass.Length==0) script.grass = new VoxelandGrassType[] {new VoxelandGrassType("Empty")};
				
				for (int t=1; t<script.grass.Length; t++) //DrawGrass(script.grass[t], t, t==-script.selected);
				{
						VoxelandGrassType type = script.grass[t];
						
						//drawing back rect
						LayoutTools.NewLine(1);
						if (t==-script.selected)
						{
							Rect backRect = new Rect(LayoutTools.lastPos.x-3, LayoutTools.lastPos.y-2, EditorGUIUtility.currentViewWidth-LayoutTools.margin, backgroundHeight+8);
							GUI.Box(backRect, "");
						}
						
						//texture thumb
						LayoutTools.NewLine(27); 
						if (GUI.Button(LayoutTools.AutoRect(27+3), "", "Box")) script.selected = -t;
						if (type.material!=null && type.material.mainTexture!=null) EditorGUI.DrawPreviewTexture(new Rect(LayoutTools.lastPos.x-27-3+1,LayoutTools.lastPos.y+1,25,25), type.material.mainTexture);
	
						//selector by name (if unselected)
						LayoutTools.lastPos.y+=3; LayoutTools.lastHeight-=8; 
						if (t!=-script.selected && GUI.Button(LayoutTools.AutoRect(), type.name, "Label")) script.selected = -t;
						LayoutTools.lastHeight+=8; 
						
						#region Draw Selected Type
						if (t==-script.selected)
						{
							LayoutTools.margin += 30;

							int oldRectY = (int)LayoutTools.lastPos.y; //to calculate background height

							LayoutTools.lastHeight-=10; 
							type.name = EditorGUI.TextField(LayoutTools.AutoRect(), type.name);
							LayoutTools.lastHeight+=10;  

							LayoutTools.NewLine(); Mesh newMesh = (Mesh)EditorGUI.ObjectField(LayoutTools.AutoRect(), new GUIContent("Mesh", "Grass mesh. Recommended mesh is 1-unit in diameter with pivot positioned in center on ground level."), type.sourceMesh, typeof(Mesh), false); 
							//resetting wrappers on mesh change
							if (type.sourceMesh != newMesh)
							{
								type.meshes = null;
								type.sourceMesh = newMesh;
							}

							LayoutTools.NewLine(); type.material = (Material)EditorGUI.ObjectField(LayoutTools.AutoRect(), new GUIContent("Material", "Grass material. Use Voxeland Grass shader to use the functionality of grass tint."), type.material, typeof(Material), false); 

							//LayoutTools.QuickFloat(ref type.height, "Height", max:10);
							//LayoutTools.QuickFloat(ref type.incline, "Incline", max:1);
							//LayoutTools.QuickFloat(ref type.random, "Random", max:1);
							LayoutTools.QuickBool(ref type.normalsFromTerrain, "Take Terrain Normals", "If turned on grass mesh will use underlying terrain normals instead of mesh normals.", isLeft:true);

							LayoutTools.NewLine(5);
							LayoutTools.margin -= 30;
							LayoutTools.AutoRect();

							//calculating background height
							int temp = (int)LayoutTools.lastPos.y - oldRectY;
							if (temp > 25) backgroundHeight = temp;
						}//if type selected
						#endregion
				}//for in types

				//making gui repaint if new type was selected - to draw background properly
				if (oldSelectedType != script.selected) { oldSelectedType=script.selected; this.Repaint(); return; }

				script.selected = -script.selected;
				LayoutTools.ArrayButtons<VoxelandGrassType>(ref script.grass, ref script.selected, drawUpDown:false);
				for (int i=0; i<script.grass.Length; i++) 
						if (script.grass[i] == null)
							script.grass[i] = new VoxelandGrassType("New");
				script.selected = -script.selected;
				
				LayoutTools.margin -= 5; 
			}
			#endregion
	
			#region Generator
            LayoutTools.QuickFoldout(ref script.guiGenerate, "Generator", "Algorithms for terrain generation. Terrain generation is done per-area (are size could be set in settings). Depending on the generation mode generated area will or will not be saved to terrain data. If area is not saved it will be generated every time game started (or scripts compiled). Area will be saved if area block is set or remove. Saving area increases data size.");
            if (script.guiGenerate)
            {
                LayoutTools.margin += 15;

                if (script.limited) script.guiSelectedAreaNum = 5050;

                //LayoutTools.NewLine(); script.guiSelectArea = GUI.Toggle(LayoutTools.AutoRect(), script.guiSelectArea, "Select Area", "Button");
                if (LayoutTools.QuickButton("Get Current Area", "Gets area where camera is currently positioned as currently selected area. Currently selected area will be generated on pressing Generate button."))
                {
                    script.guiSelectArea = false;
                    Vector3 camCoords = UnityEditor.SceneView.lastActiveSceneView.camera.transform.position + script.transform.position;
                    script.guiSelectedAreaNum = script.data.GetAreaNum((int)camCoords.x, (int)camCoords.z);    
                    SceneView.RepaintAll();    
                }

                LayoutTools.NewLine();  EditorGUI.LabelField(LayoutTools.AutoRect(140), new GUIContent("Highlight Selected Area", "Highlights area which is currently selected. Currently selected area will be generated on pressing Generate button."));
                bool guiSelectedAreaShow = EditorGUI.Toggle(LayoutTools.AutoRect(0.1f), script.guiSelectedAreaShow);
                if (guiSelectedAreaShow != script.guiSelectedAreaShow) SceneView.RepaintAll();
                script.guiSelectedAreaShow = guiSelectedAreaShow;

                GUIContent[] typeNames = new GUIContent[script.types.Length];
                for (int i=0; i<script.types.Length; i++) typeNames[i] = new GUIContent(script.types[i].name);

                LayoutTools.QuickInt(ref script.guiNoiseSeed, "Seed:", "Number to initialize random generator. With the same seed and noise size the noise value will be constant for each heightmap coordinate.");
                LayoutTools.QuickBool(ref script.clearGenerator, "Clear Area before Generating", "Will generate new terrain instead of instead of the existing one. If turned off generated terrain will be _added_ to the existing terrain", isLeft:true);

                LayoutTools.NewLine(10);
                LayoutTools.QuickBool(ref script.levelGenerator.active, "Level", "Generates flat homogeneous bedrock", isLeft:true);
                if (script.levelGenerator.active)
                {
                    LayoutTools.margin+=15;
                    LayoutTools.NewLine(); script.levelGenerator.type = (byte)EditorGUI.Popup(LayoutTools.AutoRect(), new GUIContent("Type", "Type that will be used to fill terrain by current generator."), script.levelGenerator.type, typeNames);
                    LayoutTools.QuickInt(ref script.levelGenerator.level, "Level", "Terrain will be filled with the selected type up to this level.", max:200);
                    LayoutTools.margin-=15;
                }

                LayoutTools.NewLine(10);
                LayoutTools.QuickBool(ref script.textureGenerator.active, "Texture", "Imports 2D heightmap to Voxeland. Use it to create base for your terrain. Then it could be modified with noise and erosion.", isLeft:true);
                if (script.textureGenerator.active)
                {
                    LayoutTools.margin+=15;
                    LayoutTools.NewLine(); script.textureGenerator.type = (byte)EditorGUI.Popup(LayoutTools.AutoRect(), new GUIContent("Type", "Type that will be used to fill terrain by current generator."), script.levelGenerator.type, typeNames);
                  
				    LayoutTools.NewLine(20);
					EditorGUI.LabelField(LayoutTools.AutoRect(0.8f), new GUIContent("Height Map (R)", "Texture read/write attribute has to be enabled and texture compression shuld be set to Automatic Truecolor. Only the red channel will be used."));
					script.textureGenerator.texture = (Texture2D)EditorGUI.ObjectField(LayoutTools.AutoRect(20), script.textureGenerator.texture, typeof(Texture2D), false);
					LayoutTools.NewLine(40); EditorGUI.HelpBox(LayoutTools.AutoRect(), "Texture read/write attribute has to be enabled and texture compression should be set to Automatic Truecolor", MessageType.None);

					LayoutTools.QuickFloat(ref script.textureGenerator.scale, "Height Scale", "When scale is set to 255 every color grade (0-255) corresponds to 1-block height.", max:255);
                   
                    LayoutTools.margin-=15;
                }
                
                LayoutTools.NewLine(10);
                LayoutTools.QuickBool(ref script.noiseGenerator.active, "Noise", "Fractal perlin noise algorithm. It can help to create terrain mountains and hollows.", isLeft:true);
                if (script.noiseGenerator.active)
                {
                    LayoutTools.margin+=15;
                    LayoutTools.NewLine(); script.noiseGenerator.type = (byte)EditorGUI.Popup(LayoutTools.AutoRect(), new GUIContent("Type", "Type that will be used to fill terrain by current generator."), script.noiseGenerator.type, typeNames);
                    LayoutTools.QuickFloat(ref script.noiseGenerator.amount, "Noise Amount", "Magnitude. How much noise affects the surface", max:500);
                    LayoutTools.QuickFloat(ref script.noiseGenerator.size, "Noise Size", "Wavelength. Sets the size of the highest iteration of fractal noise. High values will create more irregular noise. This parameter represents the percentage of brush size.", max:1000);
                    LayoutTools.QuickFloat(ref script.noiseGenerator.detail, "Detail", "Defines the bias of each fractal. Low values sets low influence of low-sized fractals and high influence of high fractals. Low values will give smooth terrain, high values - detailed and even too noisy.", max:1);
                    LayoutTools.QuickFloat(ref script.noiseGenerator.uplift, "Uplift", "When value is 0, noise is subtracted from terrain. When value is 1, noise is added to terrain. Value of 0.5 will mainly remain terrain on the same level, lifting or lowering individual areas.", max:1);
                    LayoutTools.QuickFloat(ref script.noiseGenerator.ruffle, "Ruffle", "Adds additional shallow (1-unit) noise to the resulting heightmap", max:2);
                    LayoutTools.margin-=15;
                }
                
                LayoutTools.NewLine(10);
                LayoutTools.QuickBool(ref script.glenGenerator.active, "Glen", "Generates flat plains between mountains.", isLeft:true);
                if (script.glenGenerator.active)
                {
                    LayoutTools.margin+=15;
                    LayoutTools.NewLine(); script.glenGenerator.type = (byte)EditorGUI.Popup(LayoutTools.AutoRect(), new GUIContent("Type", "Type that will be used to fill terrain by current generator."), script.glenGenerator.type, typeNames);
                    LayoutTools.QuickInt(ref script.glenGenerator.glenNum, "Number", "Number of flat plains", min:1, max:10);
                    LayoutTools.QuickFloat(ref script.glenGenerator.minRadius, "Min Radius", "Minimum radius of the plains", min:1, max:250);
                    LayoutTools.QuickFloat(ref script.glenGenerator.maxRadius, "Max Radius", "Maximum radius of the plain", min:1, max:250);
                    LayoutTools.QuickFloat(ref script.glenGenerator.opacity, "Opacity", "Flattness of the plains. Value of 1 will make plains absolutely flat, value of 0 will make plains not noticeable", max:1);
                    LayoutTools.QuickFloat(ref script.glenGenerator.fallof, "Fallof", "Size of the noise gradient between plain and surrounding mountains. Value of 1 will make glens perfectly circular.", max:1);
                    LayoutTools.QuickFloat(ref script.glenGenerator.fallofNoiseSize, "Fallof Noise Size", "Size of the noise on the fallof area. Higher values will make glen forms more complex.", max:250);
                    LayoutTools.QuickFloat(ref script.glenGenerator.depth, "Depth", "Underground depth willed with selected Type stratum under the glen", max:10);
                    LayoutTools.margin-=15;
                }
                
                LayoutTools.NewLine(10);
                LayoutTools.QuickBool(ref script.erosionGenerator.active, "Erosion", "Erosion that is caused mainly by water factors - rains and torrents. It will erode terrain (make a little canons where torrents flow) and return raised sediment in hollows. Moreover, it uses wind algorithm on convex surfaces - because hydraulic erosion does not work properly without wind. This is the most slow generation algorithm.", isLeft:true);
                if (script.erosionGenerator.active)
                {
                    LayoutTools.margin+=15;
                    LayoutTools.NewLine(); script.erosionGenerator.type = (byte)EditorGUI.Popup(LayoutTools.AutoRect(), new GUIContent("Type", "Type that will be used to fill terrain by current generator."), script.erosionGenerator.type, typeNames);
                    LayoutTools.QuickInt(ref script.erosionGenerator.iterations, "Iterations", "Number of algorithm iterations. Higher values will make terrain more eroded. Lowering this value and increasing amounts can speed up terrain generation, but will affect terrain quality.", min:1, max:20);
                    LayoutTools.QuickFloat(ref script.erosionGenerator.durability, "Terrain Durability", "Baserock resistance to water erosion. Low values erode terrain more. Lowering this parameter is mainly needed to reduce the number of brush passes (iterations), but will reduce terrain quality as well.", max:1);
                    LayoutTools.QuickInt(ref script.erosionGenerator.fluidity, "Fluidity Iterations", "This parameter sets how liquid sediment (bedrock raised by torrents) is. Low parameter value will stick sediment on sheer cliffs, high value will allow sediment to drain in hollows. As this parameter sets number of iterations, increasing it to very high values can slow down performance.", min:1, max:10);
                    LayoutTools.QuickFloat(ref script.erosionGenerator.erosionAmount, "Erosion Amount", "Amount of bedrock that is washed away by torrents. Unlike sediment amount, this parameter sets the amount of bedrock that is subtracted from original terrain. Zero value will not erode terrain by water at all.", max:2);
                    LayoutTools.QuickFloat(ref script.erosionGenerator.sedimentAmount, "Sediment Amount", "Percent of bedrock raised by torrents that returns back to earth ) Unlike erosion amount, this parameter sets amount of land that is added to terrain. Zero value will not generate any sediment at all.", max:2);
                    LayoutTools.QuickFloat(ref script.erosionGenerator.windAmount, "Wind Amount", "Wind sets the amount of bedrock that was carried away by wind, rockfall and other factors non-related with water erosion. Technically it randomly smoothes the convex surfaces of the terrain. Use low values for tropical rocks (as they are more influenced by monsoon, rains and water erosion than by wind), and high values for highland pikes (as all streams freeze at high altitudes).", max:2);
                    LayoutTools.QuickFloat(ref script.erosionGenerator.smooth, "Smooth", "Applies additional smoothness to terrain in order to fit brush terrain into an existing terrain made with Unity standard tools. Low, but non-zero values can remove small pikes made by wind randomness or left from water erosion. Use low values if your terrain heightmap resolution is low.", max:1);
                    LayoutTools.margin-=15;
                }
                
                LayoutTools.NewLine(10);
                LayoutTools.QuickBool(ref script.forestGenerator.active, "Forest", "Places trees using soil accordance. This generator literally plants some initial trees. If soil is suitable for tree it continues to grow and even gives sprouts, forming groups and groves.", isLeft:true);
                if (script.forestGenerator.active)
                {
                    LayoutTools.margin+=15;
                    LayoutTools.NewLine(); script.forestGenerator.type = (byte)EditorGUI.Popup(LayoutTools.AutoRect(), new GUIContent("Type", "Type that will be used to fill terrain by current generator."), script.forestGenerator.type, typeNames);
                    LayoutTools.QuickInt(ref script.forestGenerator.initialCount, "Initial Count", "Initial tree count. Randomly places this amount of trees on terrain surface, no matter of soil.", min:1, max:500);
                    LayoutTools.QuickInt(ref script.forestGenerator.iterations, "Iterations", "Number of iterations during which tree can die,survive or breed.", min:1, max:500);
                    LayoutTools.QuickFloat(ref script.forestGenerator.treeDist, "Tree Distance", "How far trees can breed saplings.", min:1, max:200);
                    LayoutTools.NewLine(); script.forestGenerator.guiShowSoilTypes = EditorGUI.Foldout(LayoutTools.AutoRect(), script.forestGenerator.guiShowSoilTypes, new GUIContent("Soil Type Quality", "How soil is suitable for tree. Value of 0 will kill tree in first iteration. Value 1 will always make tree breed."));
                    if (script.forestGenerator.soilTypes.Length != script.types.Length)
                    {
                        float[] newSoilTypes = new float[script.types.Length];
                        for (int i=1; i<Mathf.Min(newSoilTypes.Length, script.forestGenerator.soilTypes.Length); i++)
                            newSoilTypes[i] = script.forestGenerator.soilTypes[i];
                        script.forestGenerator.soilTypes = newSoilTypes;
                    }
                    if (script.forestGenerator.guiShowSoilTypes)
                        for (int t=1; t<script.forestGenerator.soilTypes.Length; t++)
                    {
                        LayoutTools.margin += 15;
                        LayoutTools.QuickFloat(ref script.forestGenerator.soilTypes[t], script.types[t].name, max:1);
                        LayoutTools.margin-=15;
                    }
                    LayoutTools.margin-=15;
                }
                
                typeNames = new GUIContent[script.grass.Length];
                for (int i=0; i<script.grass.Length; i++) typeNames[i] = new GUIContent(script.grass[i].name);
                LayoutTools.NewLine(10);
                LayoutTools.QuickBool(ref script.grassGenerator.active, "Grass", "Plants a grass using height and noise algorithms. It also creates grass under trees.", isLeft:true);
                if (script.grassGenerator.active)
                {
                    LayoutTools.margin+=15;
                    LayoutTools.NewLine(); script.grassGenerator.type = (byte)EditorGUI.Popup(LayoutTools.AutoRect(), new GUIContent("Type", "Type that will be used to fill grass."), script.grassGenerator.type, typeNames);
                    LayoutTools.QuickFloat(ref script.grassGenerator.heightMinHeight, "Height Min", "Grass starting level. All the terrain below will have grass with maximum chance.", max:300);
                    LayoutTools.QuickFloat(ref script.grassGenerator.heightMaxHeight, "Height Max", "Grass end level. All the terrain above will have grass with minimum chance.", max:300);
                    LayoutTools.QuickFloat(ref script.grassGenerator.heightMinChance, "Height Min Chance", "Grass density on the Min Level", max:1);
                    LayoutTools.QuickFloat(ref script.grassGenerator.heightMaxChance, "Height Max Chance", "Grass density on the Max Level", max:1);
                    LayoutTools.QuickInt(ref script.grassGenerator.spreadIterations, "Spread Iterations", "Spread will select grass block (bush) at random and will create grass blocks around it. Iterations is a number of blocks that will be spread.", max:30000);
                    LayoutTools.QuickFloat(ref script.grassGenerator.noiseSize, "Noise Size", "Additional fractal noise algorithm. Noise size determines size of the grass groups and spaces.", min:2, max:300);
                    LayoutTools.QuickFloat(ref script.grassGenerator.noiseDensity, "Noise Density", "The intensity noise algorithm is influenced on final the grass density.", max:1);
                    LayoutTools.margin-=15;
                }

                LayoutTools.NewLine(15); if (LayoutTools.QuickButton("Generate Selected Area", "Manually generates currently selected area. To select are use Get Current button. Generated area will be saved to data."))
                {
                    if (script.limited) script.Generate(5050);
                    else script.Generate(script.guiSelectedAreaNum);

                    //resetting progress
                    //for (int cx = Mathf.FloorToInt(1f*area.offsetX/script.chunkSize); cx <= Mathf.FloorToInt(1f*(area.offsetX+area.size)/script.chunkSize); cx++)
                    //    for (int cz = Mathf.FloorToInt(1f*area.offsetZ/script.chunkSize); cz <= Mathf.FloorToInt(1f*(area.offsetZ+area.size)/script.chunkSize); cz++)
                    //{
                    //    if (!script.chunks.CheckInRange(cx,cz)) continue;
                    //    Chunk chunk = script.chunks[cx,cz];
                    //    if (chunk==null) continue;
                    //    chunk.Init(cx,cz);
                    //    chunk.stage = Chunk.Stage.gradual;
                    //}

                    //saving data
                    script.data.areas[script.guiSelectedAreaNum].save = true;
                    script.data.compressed = script.data.SaveToByteList();
                    UnityEditor.EditorUtility.SetDirty(script.data);

                    //rebuilding
                    script.chunks.Clear();
                    script.Display(true);
                }

				LayoutTools.QuickBool(ref script.autoGeneratePlaymode, "Auto Generate New Areas in Playmode", "Will automatically generate new areas when they get in build distance range in playmode and in final game build. Generated areas will not be saved to data.", isLeft:true);
                LayoutTools.QuickBool(ref script.autoGenerateEditor, "Auto Generate New Areas in Editor", "Will automatically generate new areas when they get in build distance range in editor only. Generated areas will not be saved to data, unless toggle below is checked.", isLeft:true);
                EditorGUI.BeginDisabledGroup(!script.autoGenerateEditor);
                LayoutTools.QuickBool(ref script.saveGenerated, "Save Auto Generated Areas", "Areas generated _in editor_ will be saved to data if this toggle is checked. Saving areas increases data size.", isLeft:true);
                EditorGUI.EndDisabledGroup();

                LayoutTools.margin -= 15;
                LayoutTools.NewLine(10);
            }
            #endregion
                
			#region Settings

            LayoutTools.QuickFoldout(ref script.guiSettings, "Settings", "Various Voxeland parameters. For the settings changes to take effect perform Rebuild operation.");
            if (script.guiSettings)
            {
                LayoutTools.margin += 15;
                
                LayoutTools.NewLine(30); EditorGUI.HelpBox(LayoutTools.AutoRect(), "For the settings changes to take effect perform Rebuild operation", MessageType.None);

                LayoutTools.NewLine(5); LayoutTools.NewLine(); EditorGUI.PrefixLabel(LayoutTools.AutoRect(0.25f), new GUIContent("Data:", "Terrain voxels data. Think of it like a lossless-compressed 3d image, with block types instead pixel colors."));
                script.data = EditorGUI.ObjectField(LayoutTools.AutoRect(0.5f), script.data, typeof(Voxeland.Data), false) as Voxeland.Data;
                if (GUI.Button(LayoutTools.AutoRect(0.25f), new GUIContent("Save", "Terrain data could be saved in separate .asset file. When it is not saved to file Data is stored in current scene."))) SaveData();

                LayoutTools.QuickBool(ref script.limited, "Limited Size", "When this toggle is checked terrain size is limited by the size of one area. This is useful for small terrains that do not require dynamic build or horizon plane. Moreover, only limited-size terrains could be baked to meshes.");
                if (script.limited)
                {
                    LayoutTools.NewLine();
					script.guiAreaSize = EditorGUI.IntField(LayoutTools.AutoRect(0.75f), 
						new GUIContent("Terrain Size", "Size of the limited terrain. Internally it is the size of terrain area. Resizing terrain will destroy all the terrain data."),
						script.guiAreaSize);
                    //rounding terrain size to chunk size
                    script.guiAreaSize = Mathf.RoundToInt(1f*script.guiAreaSize/script.guiNewChunkSize) * script.guiNewChunkSize;
					if (GUI.Button(LayoutTools.AutoRect(0.25f),"Revert")) script.guiAreaSize = script.data.areaSize;
                }
                else
                {
					LayoutTools.NewLine();
					script.guiAreaSize = EditorGUI.IntField(LayoutTools.AutoRect(0.75f), 
						new GUIContent("Area Size", "Size of the data area. Terrain generation is done per-area. Big areas increase data size, small areas increase generation time."),
						script.guiAreaSize);
					if (GUI.Button(LayoutTools.AutoRect(0.25f),"Revert")) script.guiAreaSize = script.data.areaSize;
					
                    LayoutTools.QuickFloat(ref script.generateDistance, "Build Distance", "Chunks that will get in this range from camera will be automatically build. When camera moves, new chunks will be built as they get in range.");
                    LayoutTools.QuickFloat(ref script.removeDistance, "Remove Distance", "Chunks that are further from camera that this distance will be destroyed. As camera moves new chunks will be destroyed as they get out of destroy range. Remove Distance should be larger than Build Distance");
                }
                
                LayoutTools.QuickFloat(ref script.lodDistance, "LOD Distance", "The distance of turning high-poly chunk mesh off and enabling it's low-poly version, which has 4 times less triangles");
                
                LayoutTools.QuickInt(ref script.guiNewChunkSize, "Chunk Size:", "Dimensions of a terrain element mesh. Higher values will speed up the whole terrain building and reduce the number of draw calls but will slow down the terrain editing");
                if (script.guiNewChunkSize < script.terrainMargins*2) script.guiNewChunkSize = script.terrainMargins*2;
                
                LayoutTools.QuickInt(ref script.terrainMargins, "Terrain Margins", "Chunk invisible faces overlap. Lowering this value will speed up chunk build, especially on small chunks, but will open gaps between chunks. Setting this value more than 2 does not have a sense.");
                LayoutTools.QuickBool(ref script.playmodeEdit, "Playmode Edit", "Enable terrain editing in playmode. Adds a possibility to edit terrain in-game the way it is done in editor. It is recommended to use special Edit() controller instead (see Demo VoxelandController example script)", fieldSize:0.1f);
                
                LayoutTools.QuickBool(ref script.multiThreadEdit, "Multithread (experimental)", "Can cause Unity crash. Use it on your own risc.", fieldSize:0.1f);
                //if (!script.multiThreadEdit && script.threadsId!=0) script.StopThreads(); //turning off all threads if multithreaded edit off
                
                LayoutTools.QuickBool(ref script.saveMeshes, "Save Meshes with Scene", "Saves terrain mesh with scene. Turn this off if you do not plan to bake your terrain as terrain rebuilds its mesh any time the scene loaded anyway.", fieldSize:0.1f);
                LayoutTools.QuickBool(ref script.guiFocusOnBrush, "G Key Focuses on Brush", "Works like an F key, but centers camera pivot on current brush position, not the whole terrain.", fieldSize:0.1f);

                LayoutTools.QuickBool(ref script.lodWithMaterial, "Switch LODs Removing Material", "Slower, but more convenient (it does not show collider wireframe) way to switch lods. Turn this toggle off when creating release build.", fieldSize:0.1f);
                LayoutTools.NewLine(); EditorGUI.HelpBox(LayoutTools.AutoRect(), "Disabling will show collider wireframe", MessageType.None);

				LayoutTools.QuickBool(ref script.rtpCompatible, "RTP Compatibility", "Prepares the terrain for use with ReliefTerrainPMTriplanarStandalone shader. Please note that shader line 326: #define WNORMAL_COVERAGE_X_Z_Ypos_Yneg should be commented out.", fieldSize:0.1f);
				if (script.rtpCompatible)
				{
					LayoutTools.NewLine(40); 
					EditorGUI.HelpBox(LayoutTools.AutoRect(), "Comment the line 326\n(#define WNORMAL_COVERAGE_X_Z_Ypos_Yneg)\nof ReliefTerrainPMTriplanarStandalone shader.", MessageType.None);
				}

                LayoutTools.NewLine(5);
                LayoutTools.NewLine(); EditorGUI.LabelField(LayoutTools.AutoRect(), new GUIContent("Display non-generated Terrain:", "The way non-generated terrain is displayed. Sets the default height and type for all non-generated areas."));
                    LayoutTools.margin += 15;

                    LayoutTools.QuickInt(ref script.guiEmptyColumnHeight, "Level", "Height of non-generated terrain.");
                    string[] typeNames = new string[script.types.Length];
                    for (int i=0; i<script.types.Length; i++) typeNames[i] = script.types[i].name;
                    LayoutTools.NewLine(); script.guiEmptyColumnType = (byte)EditorGUI.Popup(LayoutTools.AutoRect(), "Type", script.guiEmptyColumnType, typeNames);
                    LayoutTools.margin-=15;

                //ambient
                LayoutTools.QuickBool(ref script.ambient, "Ambient Occlusion", "Calculates ambient occlusion using block structure immediately on terrain change. Turn it off if you plan to use baked or realtime GI.", isLeft:true);
                if (script.ambient)
                {
                    LayoutTools.margin+=15;
                    LayoutTools.QuickFloat(ref script.ambientFade, "Fade", "Ambient occlusion blur. Higher values will bright the dark hollows", max:1);
                    LayoutTools.QuickInt(ref script.ambientMargins, "Margins", "Increase this to avoid seams in ambient between chunks. Higher values will give less artifacts but will slow down calculations.", max:16);
                    LayoutTools.margin-=15;
                }

                //far
                LayoutTools.QuickBool(ref script.useFar, "Horizon Plane", "Planar (non-voxel) terrain to display on far distances as lod", isLeft:true);
                if (script.useFar)
                {
                    LayoutTools.margin+=15;
                
					//why don't use far on limited terrain while not all chunks build?
					
					//if (script.limited)
                    //   { LayoutTools.NewLine(30); EditorGUI.HelpBox(LayoutTools.AutoRect(), "Horizon Plane could not be used without\ndynamic build on limited terrain", MessageType.None); }
                    //
                    //else
                    //{
                        if (script.far==null) script.far = Far.Create(script);
                        LayoutTools.NewLine(); script.farMesh = (Mesh)EditorGUI.ObjectField(LayoutTools.AutoRect(), new GUIContent("Mesh", "A flat mesh that is used as a horizon plane base."), script.farMesh, typeof(Mesh), false);
                        LayoutTools.QuickFloat(ref script.farSize, "Size", "Scale of far mesh.");
                    //}

                    LayoutTools.margin-=15;
                }

                LayoutTools.NewLine(5);
                LayoutTools.NewLine(); script.terrainMaterial = (Material)EditorGUI.ObjectField(LayoutTools.AutoRect(), new GUIContent("Terrain Material", "Material of the terrain. Double-click it to set terrain material parameter, or assign a new terrain."), script.terrainMaterial, typeof(Material), false);
                LayoutTools.NewLine(); script.highlightMaterial = (Material)EditorGUI.ObjectField(LayoutTools.AutoRect(), new GUIContent("Highlight Material", "Material of the highlight object. Highlight color or decal dist could be set by modifying this material."), script.highlightMaterial, typeof(Material), false);

                LayoutTools.NewLine(5); LayoutTools.NewLine();
				if (GUI.Button(LayoutTools.AutoRect(0.5f), new GUIContent("New Terrain", "Creates new terrain. It will create a default 30-block terrain layer in origin area."))) { New(); Rebuild(); }
				if (GUI.Button(LayoutTools.AutoRect(0.5f), new GUIContent("Clear Terrain", "Creates empty terrain. It will not display any chunks unless they are manually generated"))) { Clear(); Rebuild(); }

                LayoutTools.NewLine(5);
                LayoutTools.margin -= 15;
            }
            #endregion

			#region Auto-turning Shader Normalmap and Specmap on/off
			//TODO: maybe should be in rebuild?
			if (script.terrainMaterial != null)
			{
				bool hasBump = false;
				bool hasSpec = false;
				for (int t=0; t<script.types.Length; t++)
				{
					if (!script.types[t].filledTerrain) continue;
					if (script.types[t].bumpTexture != null) hasBump = true;
					if (script.types[t].specGlossMap != null) hasSpec = true;
				}
				if (hasBump) script.terrainMaterial.EnableKeyword("_NORMALMAP"); else script.terrainMaterial.DisableKeyword("_NORMALMAP");
				if (hasSpec) script.terrainMaterial.EnableKeyword("_SPECGLOSSMAP"); else script.terrainMaterial.DisableKeyword("_SPECGLOSSMAP");
			}
			#endregion
			
			#region Import and Export
			LayoutTools.QuickFoldout(ref script.guiImportExport, "Import and Export");
			if (script.guiImportExport)
			{
				LayoutTools.margin+=15;

				#region Import 2 Data
				/*NewLine(); if (GUI.Button(AutoRect(), "Import v2 Data"))
				{
					string path= UnityEditor.EditorUtility.OpenFilePanel(
						"Load Voxeland v2 Data",
						"Assets",
						"asset");
					if (path!=null)
					{
						//TODO: import data
						
						path = path.Replace(Application.dataPath, "Assets");
						VoxelandData oldData = (VoxelandData)AssetDatabase.LoadAssetAtPath(path, typeof(VoxelandData));
						
						script.data.Load20(oldData);
						for (int i=0; i<script.types.Length; i++) script.types[i].smooth=1f;
						script.data.compressed = script.data.SaveToByteList();
						EditorUtility.SetDirty(script.data);
						
						script.chunks.Clear();
						script.Display(true);
					}
				}*/
				#endregion

				#region Center v3.1 Data
				if (LayoutTools.QuickButton("Center v3.0/3.1 Limited Data", "When opening old limited terrain scene you can notice that it's position is shifted at half of the area. Use this to fix terrain position."))
				{
					if (EditorUtility.DisplayDialog("Center Data", "This will clear all terrain areas except this one. This operation cannot be undone.", "Continue", "Cancel"))
					{
						script.data.Center31();
						
						//rebuilding
						script.chunks.Clear();
						script.Display(true);
					}
				}
				#endregion
				
				#region TXT
				if (LayoutTools.QuickButton("Load TXT", "Loads string data from text format.")) 
				{ 
					string path= UnityEditor.EditorUtility.OpenFilePanel(
						"Load Data from String",
						"Assets",
						"txt");
					if (path!=null)
					{
						path = path.Replace(Application.dataPath, "Assets");
						
						using (System.IO.FileStream fs = new System.IO.FileStream(path, System.IO.FileMode.Open))
							using (System.IO.StreamReader reader = new System.IO.StreamReader(fs))
								script.data.LoadFromString( reader.ReadToEnd() );
						
						script.chunks.Clear();
						script.Display(true);
					}
					script.data.compressed = script.data.SaveToByteList();
					EditorUtility.SetDirty(script.data);	
				}
				
				LayoutTools.NewLine(10);
				if (LayoutTools.QuickButton("Save TXT", "Saves data to text format")) 
				{
					string path= UnityEditor.EditorUtility.SaveFilePanel(
						"Save Data to String",
						"Assets",
						"data.txt", 
						"txt");
					if (path!=null)
					{
						path = path.Replace(Application.dataPath, "Assets");
						
						using (System.IO.FileStream fs = new System.IO.FileStream(path, System.IO.FileMode.Create))
							using (System.IO.StreamWriter writer = new System.IO.StreamWriter(fs))
								writer.Write(script.data.SaveToString());
					}
				}
				#endregion
				
				#region Bake  
				if (LayoutTools.QuickButton("Bake meshes to Scene", "Bakes terrain to Unity meshes and turns Voxeland script off. Meshes are saved to current scene. Note that terrain should not be infinite."))
				{
					Bake();
				}
				
				if (LayoutTools.QuickButton("Bake meshes to Assets", "Bakes terrain to Unity meshes and turns Voxeland script off. Meshes are stored in separate .asset files. Note that terrain should not be infinite."))
				{
					string path= EditorUtility.SaveFolderPanel("Save meshes to directory", "Assets", "VoxelandBakedMeshes");
					if (path==null) return;
					path = path.Replace(Application.dataPath, "Assets");

					Bake();
					
					//Mesh[] meshes = new Mesh[script.activeChunks.Count];
					//for (int i=0; i<script.activeChunks.Count; i++) meshes[i] = script.activeChunks[i].hiFilter.sharedMesh;
					//AssetDatabase.CreateAsset(meshes, path);
					
					for (int i=0; i<script.chunks.array.Length; i++)
					{
						AssetDatabase.CreateAsset(script.chunks.array[i].hiFilter.sharedMesh, path + "/" + script.transform.name + "_Chunk" + i + ".asset");
						//if (script.chunks.array[i].hiFilter.GetComponent<Renderer>().sharedMaterial != null)
						//	AssetDatabase.CreateAsset(script.chunks.array[i].hiFilter.GetComponent<Renderer>().sharedMaterial, path + "/" + script.transform.name + "_Chunk" + i + ".mat");
					}
					AssetDatabase.SaveAssets();
					
					//AssetDatabase.CreateAsset(script.activeChunks[0].hiFilter.sharedMesh, path);
					
					//for (int i=1; i<script.activeChunks.Count; i++)
					//	AssetDatabase.AddObjectToAsset(script.activeChunks[i].hiFilter.sharedMesh, script.activeChunks[0].hiFilter.sharedMesh);
					
					//AssetDatabase.CreateAsset(script.transform, path);
				}
				
				LayoutTools.QuickBool(ref script.guiBakeLightmap, "Bake Lightmap", "Will generate lightmaps when baking terrain. Enabling Lightmaps can greatly increase baking time.", isLeft:true);
				if (script.guiBakeLightmap) 
				{
					LayoutTools.NewLine(30); 
					EditorGUI.HelpBox(LayoutTools.AutoRect(), "Warning: Enabling Lightmaps can greatly\nincrease baking time.", MessageType.None);
				}
				//NewLine(); script.lightmapPadding = EditorGUI.Slider (AutoRect(), "Lightmap Padding:", script.lightmapPadding, 0, 0.5f);
				#endregion
				
				LayoutTools.NewLine(); if (LayoutTools.QuickButton("Save Data Asset", "Saves Voxeland data to .asset file.")) SaveData();
				
				LayoutTools.margin-=15;
			}
			#endregion
			
			//about
			#region About
			LayoutTools.QuickFoldout(ref guiShowAbout, "About");
			if (guiShowAbout)
			{
				//LayoutTools.margin = 10;
				LayoutTools.NewLine(100+2);
				if (guiPluginIcon==null) guiPluginIcon = Resources.Load("VoxelandIcon") as Texture2D;
				EditorGUI.DrawPreviewTexture(LayoutTools.AutoRect(50+2), guiPluginIcon); 
				LayoutTools.lastPos.y -= 100; LayoutTools.lastPos.y -= 7;

				LayoutTools.margin += 52; 
				LayoutTools.NewLine(); EditorGUI.LabelField(LayoutTools.AutoRect(), new GUIContent("Voxeland v4.2U51")); 
				LayoutTools.NewLine(); EditorGUI.LabelField(LayoutTools.AutoRect(), new GUIContent("by Denis Pahunov"));
				LayoutTools.NewLine(5);

				GUIStyle linkStyle = new GUIStyle(EditorStyles.label);
				linkStyle.normal.textColor = new Color(0.3f, 0.5f, 1f);
				LayoutTools.NewLine(); EditorGUI.LabelField(LayoutTools.AutoRect(), new GUIContent("Useful Links:"));

				LayoutTools.NewLine(); if (GUI.Button(LayoutTools.AutoRect(), " - Online Documentation", linkStyle)) Application.OpenURL("http://www.denispahunov.ru/voxeland/doc.html");
				//LayoutTools.NewLine(); if (GUI.Button(LayoutTools.AutoRect(), " - Video Tutorial", linkStyle)) Application.OpenURL("http://www.youtube.com/watch?v=bU88tkrBbb0");
				LayoutTools.NewLine(); if (GUI.Button(LayoutTools.AutoRect(), " - Forum Thread", linkStyle)) Application.OpenURL("http://forum.unity3d.com/threads/voxeland-voxel-terrain-tool.187741/");
			
				//LayoutTools.NewLine(5);
				//LayoutTools.NewLine(); EditorGUI.LabelField(LayoutTools.AutoRect(), new GUIContent("Review or rating vote on "));
				//LayoutTools.NewLine(); LayoutTools.lastPos.y -= 4; if (GUI.Button(LayoutTools.AutoRect(72), "Asset Store", linkStyle)) Application.OpenURL("https://www.assetstore.unity3d.com/en/#!/content/9180");
				//LayoutTools.lastPos.x -= 3;
				//EditorGUI.LabelField(LayoutTools.AutoRect(), new GUIContent("would be appreciated"));

				LayoutTools.NewLine(42); EditorGUI.LabelField(LayoutTools.AutoRect(), new GUIContent("On any issues related with plugin \nfunctioning you can contact the \nauthor by mail:"));
				LayoutTools.NewLine(); LayoutTools.lastPos.y -= 4; if (GUI.Button(LayoutTools.AutoRect(), "mail@denispahunov.ru", linkStyle)) Application.OpenURL("mailto:mail@denispahunov.ru");
				LayoutTools.margin -= 52; 
			}
			#endregion

			#region Debug
			
			LayoutTools.QuickFoldout(ref script.guiDebug, "Debug");
			if (script.guiDebug)
			{
				LayoutTools.margin += 15; 

				int initializedAreas = 0; 
				int savedAreas = 0;
				if (script.data.areas != null)
					for (int i=0; i<script.data.areas.Length; i++) 
					{
						if (script.data.areas[i].initialized) initializedAreas++;
						if (script.data.areas[i].save) savedAreas++;
					}
				LayoutTools.NewLine(); EditorGUI.LabelField(LayoutTools.AutoRect(), "Initialized Areas: " + initializedAreas);
				LayoutTools.NewLine(); EditorGUI.LabelField(LayoutTools.AutoRect(), "Saved Areas: " + savedAreas);
				LayoutTools.NewLine(); EditorGUI.LabelField(LayoutTools.AutoRect(), "Selected Area Num: " + script.guiSelectedAreaNum);
				LayoutTools.NewLine(5);

				LayoutTools.QuickBool(ref script.hideChunks, "Hide Chunks");
				LayoutTools.QuickBool(ref script.hideWire, "Hide Wireframe");
				LayoutTools.NewLine(5);

				if (script.visualizer == null) script.visualizer = new Visualizer();
				script.visualizer.land = script;

				LayoutTools.NewLine(); script.visualizer.takeCoords = (Visualizer.TakeVisualizeCoords)EditorGUI.EnumPopup(LayoutTools.AutoRect(), new GUIContent("Visalize Coords:"), script.visualizer.takeCoords);
				
				LayoutTools.QuickBool(ref script.visualizer.showCoords, "Show Coords");
				LayoutTools.QuickBool(ref script.visualizer.showFaceNormal, "Show Face Normal");
				LayoutTools.QuickBool(ref script.visualizer.showDistances, "Show Distances");
				LayoutTools.NewLine(); 
					script.visualizer.showAmbient = EditorGUI.Toggle(LayoutTools.AutoRect(0.6f), "Show Ambient", script.visualizer.showAmbient);
					EditorGUI.PrefixLabel(LayoutTools.AutoRect(0.18f), new GUIContent("Rotate"));
					script.visualizer.rotateAmbient = EditorGUI.Toggle(LayoutTools.AutoRect(0.1f), script.visualizer.rotateAmbient);
				LayoutTools.QuickBool(ref script.visualizer.showChunk, "Show Chunk");
				LayoutTools.QuickBool(ref script.visualizer.showArea, "Show Area");
				
				LayoutTools.QuickBool(ref script.visualizer.alwaysRebuild, "Always Rebuild");

				LayoutTools.margin -= 15;
			}
			#endregion

			LayoutTools.Finish();
		}

		#region Save and Bake export fns
			public void SaveData ()
			{
					string path= UnityEditor.EditorUtility.SaveFilePanel(
						"Save Data as Unity Asset",
						"Assets",
						"VoxelandData.asset", 
						"asset");
					if (path!=null)
					{
						path = path.Replace(Application.dataPath, "Assets");
					
						AssetDatabase.CreateAsset(script.data, path);
						AssetDatabase.SaveAssets();
					}
			}
		
			public void Bake ()
			{
				if (!script.limited)
				{
					EditorUtility.DisplayDialog("Error", "Baking is not possible on infinite terrain.\nTurn on limited terrain in settings and\nadjust terrain size.", "OK");	
					return;
				}
			
				bool oldSaveMeshes = script.saveMeshes;
				bool oldHideChunks = script.hideChunks;
				bool oldHideWire = script.hideWire;
				bool oldMultiThreadEdit = script.multiThreadEdit;
				float oldLodDist = script.lodDistance;
			
				script.saveMeshes = true;
				script.hideChunks = false;
				script.hideWire = false;
				script.multiThreadEdit = false;
				script.generateLightmaps = script.guiBakeLightmap;
				script.lodDistance = 2000000000;
			
				//clearing and re-creating chunks
				script.chunks.Clear();
				script.Display(true);

				//rebuilding //setting all chunk stages to "Force all"
				for (int i=0; i<script.chunks.array.Length; i++)
				{
					script.chunks.array[i].stage = Chunk.Stage.forceAll;
					script.chunks.array[i].Process();
				}
			
				script.saveMeshes = oldSaveMeshes;
				script.hideChunks = oldHideChunks;
				script.hideWire = oldHideWire;
				script.multiThreadEdit = oldMultiThreadEdit;
				script.generateLightmaps = false;
				script.lodDistance = oldLodDist;

				script.enabled = false;
				OnDisable();
			}
		#endregion
	
		#region Create terrain
		[MenuItem("GameObject/3D Object/Voxeland Terrain")]
		static void  Init ()
		{
			//VoxelandCreate window= ScriptableObject.CreateInstance<VoxelandCreate>();
			//window.position = new Rect(Screen.width/2,Screen.height/2, 370, 170);
			//window.ShowUtility();

			GameObject terrainObj= new GameObject("Voxeland");
			VoxelandTerrain terrain= terrainObj.AddComponent<VoxelandTerrain>();
			terrain.limited = true;
			
			//setting initial types
			terrain.types = new VoxelandBlockType[] {new VoxelandBlockType("Empty", false), new VoxelandBlockType("Ground", true)};
			terrain.grass = new VoxelandGrassType[] {new VoxelandGrassType("Empty")};
			terrain.selected = 1;

			//creating initial data
			terrain.data = ScriptableObject.CreateInstance<Data>();
			terrain.data.name = "VoxelandData";
			terrain.data.areaSize = terrain.chunkSize*7;
			terrain.guiAreaSize = terrain.data.areaSize;
			terrain.data.Clear();

			//creating initial level
			terrain.data.AddHeight(30, 0,0,terrain.data.areaSize,terrain.data.areaSize, type:1);
			terrain.data.areas[5050].save = true;

			//saving data
			terrain.data.compressed = terrain.data.SaveToByteList();
			UnityEditor.EditorUtility.SetDirty(terrain.data);

			//rebuilding
			terrain.chunks.Clear();
			terrain.Display(true);
		}
		#endregion
	}
	
	
	
/*	#region processor Do Not save Meshes
	[ExecuteInEditMode]
	public class VoxelandModificationProcessor : UnityEditor.AssetModificationProcessor  
	{
		static string[] OnWillSaveAssets (string[] paths) 
		{
			for (int p=0; p<paths.Length; p++)
				if (paths[p].EndsWith(".unity")) //when saving scene
			{	
				VoxelandTerrain[] voxelands = (VoxelandTerrain[])GameObject.FindObjectsOfType(typeof(VoxelandTerrain));
				
				for (int i=0; i<voxelands.Length; i++)
					if (!voxelands[i].saveMeshes) 
					{
						if (voxelands[i].hideChunks)
						{ 
							foreach (Transform child in voxelands[i].transform)
								VoxelandTerrain.SetHideFlagsRecursively(HideFlags.HideAndDontSave, child);
						}
						else 
						{
							foreach (Transform child in voxelands[i].transform)
								VoxelandTerrain.SetHideFlagsRecursively(HideFlags.DontSave, child);
						}
						for (int c=0; c<voxelands[i].chunks.array.Length; c++) voxelands[i].chunks.array[c].transform.parent = null;
						if (voxelands[i].far != null) voxelands[i].far.transform.parent = null;
						if (voxelands[i].highlight != null) voxelands[i].highlight.transform.parent = null;
						voxelands[i].chunksUnparented = true;
					}
			}
			
			return paths;
		}
	}
	#endregion*/
	
} //namespace