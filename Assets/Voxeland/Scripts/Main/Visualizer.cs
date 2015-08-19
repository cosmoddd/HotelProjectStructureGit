using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Voxeland 
{
	[System.Serializable]
	public class Visualizer
	{
		#region Default Voxealnd Visualization

			public VoxelandTerrain land;
		
			public enum TakeVisualizeCoords { Off=0, Selected=1, Camera=2, Zero=3 }
			public TakeVisualizeCoords takeCoords = TakeVisualizeCoords.Off;
		
			public bool showCoords = false;
			public bool showDistances = false;
			public bool showAmbient = false; public bool rotateAmbient = false;
			public bool showFaceNormal = false;
			public bool showChunk = false;
			public bool showArea = false;
			public bool alwaysRebuild = false;

			public void  Visualize ()
			{
				#if UNITY_EDITOR

				if (takeCoords == TakeVisualizeCoords.Off) return;
			
				Ray aimRay = new Ray();
				switch (takeCoords)
				{
					case TakeVisualizeCoords.Selected: aimRay = land.oldAimRay; break;
					case TakeVisualizeCoords.Camera: 				
						Vector3 camPos;
						if (UnityEditor.SceneView.lastActiveSceneView != null) camPos = UnityEditor.SceneView.lastActiveSceneView.camera.transform.position;
						else camPos = Camera.main.transform.position;
						camPos = land.transform.InverseTransformPoint(camPos);
						aimRay = new Ray(camPos+new Vector3(0,1000,0), Vector3.down*2000);
						break;
					case TakeVisualizeCoords.Zero: aimRay = new Ray(new Vector3(0,1000,0), Vector3.down*2000); break;
				}

				VoxelandTerrain.AimData coordsData = land.GetCoordsByRay(aimRay);
				if (!coordsData.hit) return;
			
				#region Coords
				if (showCoords)
				{
					Gizmos.color = new Color(0,0.5f,0,0.8f);
					Gizmos.DrawCube(new Vector3(coordsData.x+0.5f, coordsData.y+0.5f, coordsData.z+0.5f), Vector3.one);
					Gizmos.DrawWireCube(new Vector3(
						coordsData.x+0.5f+VoxelandTerrain.oppositeDirX[coordsData.dir], 
						coordsData.y+0.5f+VoxelandTerrain.oppositeDirY[coordsData.dir], 
						coordsData.z+0.5f+VoxelandTerrain.oppositeDirZ[coordsData.dir]), Vector3.one);	
				}
				#endregion
			
				#region Distances
				//always camera-space
				if (showDistances)
				{
					Gizmos.color = Color.green;
				
					Vector3 camPos;
					if (UnityEditor.SceneView.lastActiveSceneView != null) camPos = UnityEditor.SceneView.lastActiveSceneView.camera.transform.position;
					else camPos = Camera.main.transform.position;
					camPos = land.transform.InverseTransformPoint(camPos);

					//Gizmos.DrawSphere(new Vector3((int)camPos.x+0.5f, land.data.GetTopPoint((int)camPos.x, (int)camPos.z), (int)camPos.z+0.5f), 0.45f);
					Gizmos.DrawWireCube(new Vector3((int)camPos.x+0.5f, land.data.GetTopPoint((int)camPos.x, (int)camPos.z), (int)camPos.z+0.5f), new Vector3(land.generateDistance*2, land.generateDistance*2, land.generateDistance*2));
					Gizmos.color = Color.red;
					Gizmos.DrawWireCube(new Vector3((int)camPos.x+0.5f, land.data.GetTopPoint((int)camPos.x, (int)camPos.z), (int)camPos.z+0.5f), new Vector3(land.removeDistance*2, land.removeDistance*2, land.removeDistance*2));
				}
				#endregion
			
				#region Ambient
				if (showAmbient)
				{ 
					coordsData.chunk.CalculateAmbient();
				
					for (int x=-land.ambientMargins; x<land.chunkSize+land.ambientMargins; x++)
						for (int z=-land.ambientMargins; z<land.chunkSize+land.ambientMargins; z++)
							for (int y=coordsData.chunk.ambient.offsetY; y<coordsData.chunk.ambient.offsetY+coordsData.chunk.ambient.sizeY; y++)
						{
							if ((!rotateAmbient && coordsData.face.x != x) || (rotateAmbient && coordsData.face.z != z)) continue;

							Gizmos.color = new Color(0f,1f,0f,1f);
							if(x<0 || x>=land.chunkSize || z<0 || z>=land.chunkSize) Gizmos.color = new Color(0.8f, 0.8f, 0f, 1f);
							Vector3 center = new Vector3(coordsData.chunk.offsetX + x + 0.5f,y + 0.5f,coordsData.chunk.offsetZ + z + 0.5f);

							float val = (coordsData.chunk.ambient[x,y,z] / 250f) * 0.45f;
							if (val>0.01f) Gizmos.DrawSphere(center, val);
						}
				}
				#endregion
			
				#region Normal
				if (showFaceNormal)
				{ 
					coordsData.chunk.CalculateAmbient();
				
					Gizmos.color = Color.red;
					Vector3 start = coordsData.chunk.verts[coordsData.face.centerNum] + new Vector3(coordsData.chunk.offsetX, 0, coordsData.chunk.offsetZ);
					Gizmos.DrawSphere(start, 0.1f);
					Gizmos.DrawLine(start, start + coordsData.face.normal);
				}
				#endregion

				#region Chunk
				if (showChunk)
				{ 
					Gizmos.color = Color.green;
				
					//int minX = Mathf.FloorToInt(1f*coordsData.x-2/land.chunkSize); int minZ = Mathf.FloorToInt(1f*coordsData.z-2/land.chunkSize);
					//int maxX = Mathf.FloorToInt(1f*coordsData.x+2/land.chunkSize); int maxZ = Mathf.FloorToInt(1f*coordsData.z+2/land.chunkSize);
				
					for (int x = Mathf.FloorToInt(1f*(coordsData.x-2)/land.chunkSize); x <= Mathf.FloorToInt(1f*(coordsData.x+2)/land.chunkSize); x++)
						for (int z = Mathf.FloorToInt(1f*(coordsData.z-2)/land.chunkSize); z <= Mathf.FloorToInt(1f*(coordsData.z+2)/land.chunkSize); z++)
						{
							Chunk chunk = land.chunks[x,z];
							if (chunk != null) Gizmos.DrawWireCube(
								new Vector3(
									chunk.offsetX + land.chunkSize/2 + 0.5f,
									50f,
									chunk.offsetZ + land.chunkSize/2 + 0.5f),
								new Vector3(land.chunkSize, 100f, land.chunkSize));
						}
				}
				#endregion

				#region Area
				if (showArea)
				{
					Gizmos.color = new Color(0.5f, 0.75f, 1f, 1f);
					Data.Area area = land.data.GetArea(coordsData.x, coordsData.z);
				
					float density = 64;
					for (int x=0; x<density+1; x++)
						for (int z=0; z<density+1; z++)
					{
						float xCoord = area.offsetX + x*area.size/density;
						float zCoord = area.offsetZ + z*area.size/density;
			
						if (x!=density) Gizmos.DrawLine( 
							new Vector3(xCoord, land.data.GetTopPoint((int)xCoord, (int)zCoord), zCoord), 
							new Vector3(xCoord+area.size/density, land.data.GetTopPoint((int)(xCoord+area.size/density), (int)zCoord), zCoord) );
						if (z!=density) Gizmos.DrawLine( 
							new Vector3(xCoord, land.data.GetTopPoint((int)xCoord, (int)zCoord), zCoord), 
							new Vector3(xCoord, land.data.GetTopPoint((int)xCoord, (int)(zCoord+area.size/density)), zCoord+area.size/density) );
					}
				}
				#endregion
			
				#region always rebuild
				if (UnityEditor.EditorApplication.isPlaying && alwaysRebuild)
					land.ResetProgress(coordsData.x,coordsData.z,1); //x,z,extend
				#endregion

				#region selected area for generator
					//if (land.guiGenerate && land.guiSelectedAreaShow) DrawArea(land, land.data.areas[ land.guiSelectedAreaNum ]);
				#endregion
				
				#endif
			}

			public static void DrawArea (VoxelandTerrain land, Data.Area area) //drawing area in OnSceneGui
			{
					#if UNITY_EDITOR
				
					float density = 64;
					for (int x=0; x<density+1; x++)
						for (int z=0; z<density+1; z++)
					{
						float xCoord = area.offsetX + x*area.size/density;
						float zCoord = area.offsetZ + z*area.size/density;
			
						Vector3 start = new Vector3(xCoord, land.data.GetTopPoint((int)xCoord, (int)zCoord), zCoord);
						Vector3 xPoint = new Vector3(xCoord+area.size/density, land.data.GetTopPoint((int)(xCoord+area.size/density), (int)zCoord), zCoord);
						Vector3 zPoint = new Vector3(xCoord, land.data.GetTopPoint((int)xCoord, (int)(zCoord+area.size/density)), zCoord+area.size/density);

						start = land.transform.TransformPoint(start);
						xPoint = land.transform.TransformPoint(xPoint);
						zPoint = land.transform.TransformPoint(zPoint);

						if (x!=density) UnityEditor.Handles.DrawLine(start, xPoint);
						if (z!=density) UnityEditor.Handles.DrawLine(start, zPoint);
					}

					#endif
			}
		
		#endregion

		#region Debug static Gizmos
			
			public struct Gizmo
			{
				public Vector3 pos;
				public Vector3 end;
				public float radius;
				public enum Type {sphere, cube, coord, circle, line};
				public Type type;
				public bool wire;
				public Color color;

				public Gizmo (Vector3 pos, Vector3 end, float radius, Type type, bool wire, Color color)
					{this.pos=pos; this.end=end; this.radius=radius; this.type=type; this.wire=wire; this.color=color;}

				public void Draw ()
				{
					Gizmos.color = color;
					switch (type)
					{
						case Type.sphere:
							if (wire) Gizmos.DrawWireSphere(pos,radius);
							else Gizmos.DrawSphere(pos,radius);
							break;
						case Type.circle:
							Vector3 oldDir = new Vector3(0,0,1)*radius + pos;
							for (int i=1; i<33; i++)
							{
								float angle = i/32f*360f;
								Vector3 dir = new Vector3( Mathf.Sin(angle*Mathf.Deg2Rad), 0, Mathf.Cos(angle*Mathf.Deg2Rad) ) * radius + pos;
								Gizmos.DrawLine(oldDir,dir);
								oldDir=dir;
							}
							break;
						case Type.line:
							Gizmos.DrawLine(pos,end);
							break;
					}
				}

				/*public void ShowDebugCoords ()
				{
					if (instance==null) return;
					Gizmos.color = Color.red;

					//drawing gizmos
					for (int i=0; i<debugCoords.Count; i++)
					{
						if (debugTypes[i]==0)
						{
							Gizmos.DrawWireCube(debugCoords[i], Vector3.one);
							DrawString(
								((int)debugCoords[i].x).ToString() + "," + ((int)debugCoords[i].y).ToString() + "," + ((int)debugCoords[i].z).ToString(),
								debugCoords[i]-new Vector3(0.5f,0.15f,0), 0.06f);
							DrawString(
								(land.data.GetBlock((int)debugCoords[i].x, (int)debugCoords[i].y, (int)debugCoords[i].z)).ToString(),
								debugCoords[i]-new Vector3(0.5f,-0.15f,0), 0.06f);
						}
						else Gizmos.DrawSphere(debugCoords[i], debugParams[i]);
					}
				}*/

				public void DrawString (string st, Vector3 pos, float scale)
				{
					for (int i=0; i<st.Length; i++)
						DrawChar(st[i], new Vector3(pos.x + i*scale + i*scale*0.25f, pos.y, pos.z), scale);
				}

				public void DrawChar (char ch, Vector3 pos, float scale)
				{
					List<Vector3> points = new List<Vector3>();

					switch (ch)
					{
						case '0':	points.Add( new Vector3(0,0,0) );
									points.Add( new Vector3(0,2,0) );
									points.Add( new Vector3(1,2,0) );
									points.Add( new Vector3(1,0,0) );
									points.Add( new Vector3(0,0,0) );
									break;
						case '1':	points.Add( new Vector3(0,1,0) );
									points.Add( new Vector3(1,2,0) );
									points.Add( new Vector3(1,0,0) );
									break;
						case '2':	points.Add( new Vector3(0,2,0) );
									points.Add( new Vector3(1,2,0) );
									points.Add( new Vector3(1,1,0) );
									points.Add( new Vector3(0,0,0) );
									points.Add( new Vector3(1,0,0) );
									break;
						case '3':	points.Add( new Vector3(0,2,0) );
									points.Add( new Vector3(1,2,0) );
									points.Add( new Vector3(0,1,0) );
									points.Add( new Vector3(1,1,0) );
									points.Add( new Vector3(0,0,0) );
									break;
						case '4':	points.Add( new Vector3(0,2,0) );
									points.Add( new Vector3(0,1,0) );
									points.Add( new Vector3(1,1,0) );
									points.Add( new Vector3(1,0,0) );
									break;
						case '5':	points.Add( new Vector3(1,2,0) );
									points.Add( new Vector3(0,2,0) );
									points.Add( new Vector3(0,1,0) );
									points.Add( new Vector3(1,1,0) );
									points.Add( new Vector3(0,0,0) );
									break;
						case '6':	points.Add( new Vector3(1,2,0) );
									points.Add( new Vector3(0,2,0) );
									points.Add( new Vector3(0,0,0) );
									points.Add( new Vector3(1,0,0) );
									points.Add( new Vector3(1,1,0) );
									points.Add( new Vector3(0,1,0) );
									break;
						case '7':	points.Add( new Vector3(0,2,0) );
									points.Add( new Vector3(1,2,0) );
									points.Add( new Vector3(1,0,0) );
									break;
						case '8':	points.Add( new Vector3(1,1,0) );
									points.Add( new Vector3(1,2,0) );
									points.Add( new Vector3(0,2,0) );
									points.Add( new Vector3(0,0,0) );
									points.Add( new Vector3(1,0,0) );
									points.Add( new Vector3(1,1,0) );
									points.Add( new Vector3(0,1,0) );
									break;
						case '9':	points.Add( new Vector3(1,1,0) );
									points.Add( new Vector3(0,1,0) );
									points.Add( new Vector3(0,2,0) );
									points.Add( new Vector3(1,2,0) );
									points.Add( new Vector3(1,0,0) );
									points.Add( new Vector3(0,0,0) );
									break;
						case ',':	points.Add( new Vector3(0.6f,0.1f,0) );
									points.Add( new Vector3(0.4f,-0.3f,0) );
									break;
					}

					for (int i=1; i<points.Count; i++)
						Gizmos.DrawLine(points[i-1] * scale + pos, points[i] * scale + pos);
				}
			}

			public struct GizmoChannel
			{
				public Gizmo[] gizmos;
				public int gizmoCount;
				public string name;

				public GizmoChannel (string name) {this.name=name; this.gizmos=new Gizmo[32]; this.gizmoCount=0; }

				public void AddGizmo (Gizmo gizmo)
				{
					if (gizmoCount==gizmos.Length) System.Array.Resize(ref gizmos, gizmos.Length*2);
					gizmos[gizmoCount] = gizmo;
					gizmoCount++;
				}

				public void Draw () {for (int i=0;i<gizmoCount;i++) gizmos[i].Draw(); }

				public void Clear () {gizmos=new Gizmo[32]; gizmoCount=0;}
			}

			static public GizmoChannel[] channels;
			static public int channelsCount;

			static public void DrawGizmos () { for (int i=0;i<channelsCount;i++) channels[i].Draw(); }

			static public void ClearGizmos (string channel) { int chNum = FindChannel(channel); channels[chNum].Clear(); } //channels[FindChannel(channel)] does not work

			static public void AddChannel (GizmoChannel channel)
			{
				if (channelsCount==channels.Length) System.Array.Resize(ref channels, channels.Length*2);
				channels[channelsCount] = channel;
				channelsCount++;
			}

			static public int FindChannel (string name)
			{
				if (channels==null) channels = new GizmoChannel[32];
				
				for (int i=0; i<channelsCount; i++)
					if (channels[i].name==name)
						return i;
				
				//if not found
				AddChannel( new GizmoChannel(name) );
				return channelsCount-1;
			}

			static public void AddSphere (string channel, Vector3 pos, float radius=1, Color color=new Color()) {AddGizmo(channel, new Gizmo(pos,Vector3.zero,radius,Gizmo.Type.sphere,false,color));}
			static public void AddCircle (string channel, Vector3 pos, float radius=1, Color color=new Color()) {AddGizmo(channel, new Gizmo(pos,Vector3.zero,radius,Gizmo.Type.circle,false,color));}
			static public void AddLine (string channel, Vector3 start, Vector3 end, Color color=new Color()) {AddGizmo(channel, new Gizmo(start,end,0,Gizmo.Type.line,false,color));}
			static public void AddGizmo (string channel, Gizmo gizmo) 
			{ 
				if (gizmo.color.r==0 && gizmo.color.g==0 && gizmo.color.b==0 && gizmo.color.a==0) gizmo.color = Color.green;
				channels[FindChannel(channel)].AddGizmo(gizmo); 
			}
			


		#endregion


	}
}

