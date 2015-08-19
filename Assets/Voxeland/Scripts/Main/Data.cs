using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace Voxeland 
{
	//[System.Serializable]
	public class Data : ScriptableObject
	{
		//classes there are pure data holders.
		[System.Serializable]
		public struct Column 
		{ 
			static readonly byte specialLevel = 245;
			//static readonly byte compressedLevel = 245;
			
			public List<byte> list; 

			[System.NonSerialized] public static Matrix3<byte> setBlockMatrix = new Matrix3<byte>(1,1024,1);
			
			public Column (Column source) { list = new List<byte>(); list.AddRange(source.list); } //copy column
			public Column (bool empty) { list = new List<byte>(); if (!empty) { list.Add(1); list.Add(1); } }
			
			#region Split list
			//splits one list virtually in two - one for types and one for levels
				public int count { get{ return list.Count/2; } }

				public byte GetType (int num) { return list[num*2]; }
				public byte GetLevel (int num) 
				{ 
					byte level = list[num*2 + 1];
					if (level>specialLevel) level=1;
					return level;
				}
			#endregion

			#region Default functions

				public byte GetBlock (int y)
				{
					int layer = 0;
					for (int i=0; i<count; i++)
					{
						layer += GetLevel(i);
						if (layer > y) return GetType(i);
					}
					return 0;
				}

				public void SetBlock (int y, byte t)
				{
					ToMatrix(setBlockMatrix, 0,0);
					setBlockMatrix[0,y,0] = t;
					FromMatrix(setBlockMatrix, 0,0);
				}

				public void SetSpecialBlock (int y, byte t, byte special)
				{
					
				}

				//public byte GetSpecialBlock (

				public void Optimize ()
				{
					ToMatrix(setBlockMatrix, 0,0);
					FromMatrix(setBlockMatrix, 0,0);
				}

				public bool HasBlock (byte t)
				{
					int listCount = count;
					for (int i=0; i<listCount; i++)
						if (GetType(i) == t) return true;
					return false;
				}

				public bool HasBlock (bool[] exist)
				{
					int listCount = count;
					for (int i=0; i<listCount; i++)
					{
						byte type = GetType(i);
						if (type>=exist.Length) type=0;
						if (exist.Length>type && exist[type]) return true;
					}
					return false;
				}

				public int GetTopPoint ()
				{ 
					int layer = 0;
					int listCount = count;
					for (int i=0; i<listCount; i++)
						layer += GetLevel(i);
					return layer;
				}

				public int GetTopPoint (bool[] exist)
				{ 
					int listCount = count;
					
					//finding empty level
					int emptyLevel = count;
					for (int i=listCount-1; i>=0; i--)
					{
						byte type = GetType(i);
						if (type>=exist.Length) type=0;
						if (!exist[type]) emptyLevel--;
					}
					
					//getting top point
					int layer = 0;
					for (int i=0; i<emptyLevel; i++)
						layer += GetLevel(i);
					return layer;
				}
			
				public int GetBottomPoint (bool[] exist) 
				{ 
					int layer = 0;
					int listCount = count;
					for (int i=0; i<listCount; i++) //type
					{
						byte type = GetType(i);
						if (type>=exist.Length) type=0;
						if (!exist[type]) return layer; //type
						layer += GetLevel(i);
					}
					return layer;
				}
			
				public byte GetTopType ()
				{
					if (list.Count==0) return 0; 
					return GetType(count-1);
				}

				public byte GetTopType (bool[] exist)
				{
					if (list.Count==0) return 0;
					for (int i=count-1; i>=0; i--)
					{
						byte type = GetType(i);
						if (type>=exist.Length) type=0;
						if (exist[type]) return type;
					}
					return 1;
				}

				public void AddBlocks (byte type, int level)
				{
					if (level<=0) return;
					
					//adding 245-levels
					int iterations = (int)(level / specialLevel);
					for (int i=0; i<iterations; i++) 
						{ list.Add(type); list.Add(specialLevel); }

					//adding last level
					list.Add(type); list.Add((byte)(level - (iterations*specialLevel)));
				}

				public void Clamp (int level)
				{
					if (list.Count == 0) return;
					level = Mathf.Max(level,2); //minimum level is 2... don't remember why
					
					//finding clamp num
					int maxLevel = 0; int maxNum = 0;
					int listCount = count;
					for (int i=0; i<listCount; i++)
					{
						maxLevel += GetLevel(i); 
						maxNum++;
						if (maxLevel >= level) break;
					}

					//removing all list enteries after num
					if (maxNum > listCount) return; //summar level lower than level
					list.RemoveRange(maxNum*2, (listCount-maxNum)*2);

					//clamping the last entry
					int difference = Mathf.Max(0, maxLevel-level);
					list[list.Count-1] = (byte)(list[list.Count-1] - difference);
				}

				public bool HasThinBlock () //if a column has 1-level block in height
				{
					int listCount = count;
					for (int i=0; i<listCount; i++)
						if (GetLevel(i) == 1) return true;
					return false;
				}

				public int FindThinBlock () //finds the level of 1-height block in first occurence from bottom. If nothing found returns -1
				{
					int layer = 0;
					for (int i=0; i<count; i++)
					{
						int level = GetLevel(i);
						if (level==1 && GetType(i)!=0) return layer;
						layer += level;
					}
					return -1;
				}

				/*public void Load20 (int x, int z, VoxelandData data)
				{
					//clearing matrix column
					for (int y=0; y<setBlockMatrix.sizeY; y++) setBlockMatrix[0,y,0] = 0;
					
					//loading 2.0 data
					for (int y=0; y<data.sizeY; y++)
						setBlockMatrix[0,y,0] = (byte)(data.GetNode(x,y,z).type);

					//baking it
					FromMatrix(setBlockMatrix, 0,0);
				}*/

				public bool Check ()
				{
					if (list.Count%2 == 1)
					{
						string s = "Column list Count error: " + list.Count.ToString() + ":";
						for (int i=0;i<list.Count;i++) s += list[i].ToString() + ",";
						Debug.Log(s);
						return false;
					}
					return true;
				}

			#endregion

			#region Matrix operations

				public void ToMatrix (Matrix3<byte> matrix, int x, int z) 
				{
					matrix.SetPos(x, matrix.offsetY, z);

					//resetting matrix if column empty
					if(list.Count==0)
					{
						for(int y=matrix.offsetY;y<matrix.offsetY+matrix.sizeY;y++)
						{
							matrix.current = 0;
							matrix.MovePosNextY();
						}
						return;
					}

					//writing byte column to matrix
					int i = 0;
					int curMaxLevel = GetLevel(0);
					byte curType = GetType(0);

					for(int y=0; y<matrix.offsetY+matrix.sizeY; y++) //starting from 0, not from offsetY
					{
						//changing cur type if reached type max level
						if(y >= curMaxLevel)
						{
							i++;

							if(i >= count) curType = 0;
							else
							{
								curType = GetType(i);
								curMaxLevel += GetLevel(i);
							}
						}

						//filling matrix
						if(y >= matrix.offsetY)
						{
							matrix.current = curType;
							matrix.MovePosNextY();
						}
					}
				}

				public void ToExistMatrix (Matrix3<byte> matrix, bool[] exist, int x, int z) //0 if block is empty, 255 if filled
				{
					matrix.SetPos(x, matrix.offsetY, z);

					//resetting matrix if column empty
					if(list.Count==0)
					{
						for(int y=matrix.offsetY;y<matrix.offsetY+matrix.sizeY;y++)
						{
							matrix.current = 0;
							matrix.MovePosNextY();
						}
						return;
					}

					//writing byte column to matrix
					int i = 0;
					int curMaxLevel = GetLevel(0);
					byte curType = GetType(0);

					for(int y=0; y<matrix.offsetY+matrix.sizeY; y++) //starting from 0, not from offsetY
					{
						//changing cur type if reached type max level
						if(y >= curMaxLevel)
						{
							i++;

							if(i >= count) curType = 0;
							else
							{
								curType = GetType(i);
								if (curType >= exist.Length) curType = 0;
								curMaxLevel += GetLevel(i);
							}
						}

						//filling matrix
						if(y >= matrix.offsetY)
						{
							//matrix.current = exist[curType] ? 255 | 0;
							if (curType>=exist.Length) curType=0;

							if (exist[curType]) matrix.current = 255;
							else matrix.current = 0;

							matrix.MovePosNextY();
						}
					}
				}

				public void FromMatrix (Matrix3<byte> matrix, int x, int z)
				{
					list.Clear();
					matrix.SetPos(x, 0, z);
				
					byte curType = 255; //should be unequal to matrix.current

					for (int y=0; y<matrix.offsetY+matrix.sizeY; y++)
					{
						if (matrix.current != curType || list[list.Count-1] >= specialLevel) //if type changed or prev level is more than 245 - creating new list entry
						{
							curType = matrix.current;
						
							list.Add(curType); //type
							list.Add(0);	//depth
						}
					
						list[list.Count-1]++; //adding depth to last entry
						matrix.MovePosNextY();
					}
				
					//removing top level
					while (list.Count != 0 && list[list.Count-2]==0)
						list.RemoveRange(list.Count-2, 2);
				}

				/*public void FromMatrix (Matrix2<byte> matrix, int x) //for grass
				{
					list.Clear();
					matrix.SetPos(x, 0);
				
					byte curType = 255; //should be unequal to matrix.current

					for (int z=0; z<matrix.offsetZ+matrix.sizeZ; z++)
					{
						if (matrix.current != curType || list[list.Count-1] >= specialLevel) //if type changed or prev level is more than 245 - creating new list entry
						{
							curType = matrix.current;
						
							list.Add(curType); //type
							list.Add(0);	//depth
						}
					
						list[list.Count-1]++; //adding depth to last entry
						matrix.MovePosNextZ();
					}
				
					//removing top level
					while (list.Count != 0 && list[list.Count-2]==0)
						list.RemoveRange(list.Count-2, 2);
				}*/

				public void SetBlocksWithMask (byte t, Matrix3<bool> mask, int x, int z)
				{
					ToMatrix(setBlockMatrix, 0,0);
					
					mask.SetPos(x,mask.offsetY,z);
					setBlockMatrix.SetPos(0,mask.offsetY,0);

					for (int y=mask.offsetY; y<mask.offsetY+mask.sizeY; y++)
					{
						if (mask.current) setBlockMatrix[0,y,0] = t;
						mask.MovePosNextY();
						setBlockMatrix.MovePosNextY();
					}
					
					FromMatrix(setBlockMatrix, 0,0);
				}

			#endregion

			#region TODO Outdated
			/*
			public void LoadFromData (int x, int z, VoxelandData data)
			{
				//generating byte column
				for (int y=0; y<data.sizeY; y++)
					byteColumn[y] = (byte)(data.GetNode(x,y,z).type);
	
				//baking it
				BakeByteColumn();
			}
			
			public void WriteByteColumn ()
			{
				if (list.Count==0) { byteColumnLength=0; return; }
				
				int typemax = list[1];
				int num = 0;
				byte curType = list[0];
				
				for (int y=0; y<byteColumn.Length; y++)
				{
					if (y >= typemax) 
					{
						num+=2;
						
						if (num>=list.Count-1) { byteColumnLength=y; break; }
						
						curType = list[num];
						typemax += list[num+1];
					}
					
					byteColumn[y] = curType;
				}
			}
			
			public void BakeByteColumn ()
			{		
				list.Clear();
				
				byte curType = 255;
				for (int y=0; y<byteColumnLength; y++)
				{
					if (byteColumn[y] != curType || list[list.Count-1] >= 250)
					{
						curType = byteColumn[y];
						
						list.Add(curType); //type
						list.Add(0);	//depth
					}
					
					list[list.Count-1]++; //depth
				}
				
				//removing top level
				while (list.Count != 0 && list[list.Count-2]==0)
					list.RemoveRange(list.Count-2, 2);
			}
			
			public byte GetBlock (int y) 
			{ 
				int layer = 0;
				for (int num=0; num<list.Count; num+=2)
				{
					layer += list[num+1]; //depth
					if (layer > y) return list[num]; //type
				}
				return 0;
			}
			
			public void  SetBlock (int y, byte t)
			{
				WriteByteColumn();
	
				byteColumn[y] = t;
				
				//adding zeros if y is larger than byte list length
				if (y>=byteColumnLength)
				{
					for (int i=byteColumnLength; i<y; i++) byteColumn[i] = 0;
					byteColumnLength = Mathf.Max(byteColumnLength, y+1);
				}
	
				BakeByteColumn();
			}

			public bool HasBlock (byte t)
			{
				for(int num=0;num<list.Count;num+=2) 
					if(list[num] == t) return true;
				return false;
			}

			public bool HasBlock (bool[] exist)
			{
				for(int num=0;num<list.Count;num+=2)
					if(exist[list[num]]) return true;
				return false;
			}
			
			public int GetTopPoint () //todo: bool[] exist
			{ 
				int layer = 0;
				for (int num=1; num<list.Count; num+=2) //depth
				{
					layer += list[num]; //depth
				}
				return layer;
			}
			
			public int GetBottomPoint (bool[] exist) 
			{ 
				int layer = 0;
				for (int num=0; num<list.Count; num+=2) //type
				{
					if (!exist[ list[num] ]) return layer; //type
					layer += list[num+1]; //depth
				}
				return layer;
			}
			
			public byte GetTopType ()
			{
				if (list.Count==0) return 0;
				return list[list.Count-2];
			}
			
			public void SetHeight (int height, byte type)
			{
				WriteByteColumn();
				
				for (int i=byteColumnLength; i<height; i++) byteColumn[i] = type;
				
				byteColumnLength = height;
				byteColumnLength = Mathf.Max(1, byteColumnLength);
				
				BakeByteColumn();
			}
			*/
			#endregion
		}
		
		//[System.Serializable]
		public struct Area 
		{ 
			//public static readonly int areaSize = 250;
			
			public Column[] columns;

			public int offsetX;
			public int offsetZ;
			public int size;

			public bool initialized;
			public bool save;
			
			public void Initialize ()
			{
				if (initialized) //clearing columns if it is re-initializing
				{
					for (int c=0; c<columns.Length; c++) 
						columns[c].list.Clear();
				}
				
				else //creating columns if it is the new area
				{
					columns = new Column[size*(size+1)];
					for (int c=0; c<columns.Length; c++) 
					{
						columns[c] = new Column(); 
						columns[c].list = new List<byte>(); 
					}
				}

				//filling area with empty column
				/*
				for (int c=0; c<columns.Length; c++) 
					for (int i=0; i<emptyColumn.list.Count; i++) 
						columns[c].list.Add(emptyColumn.list[i]);
				*/

				initialized = true;
			}

			public Column GetColumn(int x, int z) { return columns[(z-offsetZ)*size + x-offsetX]; }
			//public Column GetGrass(int x) { return grass[x-offsetX]; }
			public Column GetGrass(int x) { return columns[size*size + x-offsetX]; }
			//public Column GetGrass(int x) { return columns[512*areaSize + x-offsetX]; } 
		}
		
		public List<byte> compressed = new List<byte>();
		
		[System.NonSerialized] public Area[] areas = null; //new Area[100*100];

		public Column emptyColumn = new Column(true);

		#region Undo

			public class UndoColumns
			{
				public Column[] columns;
				public int x;
				public int z;
				public int range;
			
				public UndoColumns (int sx, int sz, int sr) { x=sx; z=sz; range=sr; columns = new Column[(range*2+1) * (range*2+1)]; }
			
				public void PerformUndo (Data data)
				{
					int minX = x-range; int minZ = z-range;
					int maxX = x+range; int maxZ = z+range;
				
					for (int xi = 0; xi<=maxX-minX; xi++)
						for (int zi = 0; zi<=maxZ-minZ; zi++)
						{
							//working directly with columns
							Area area = data.areas[ data.GetAreaNum( xi+minX, zi+minZ ) ];
							int columnNum =  (z+minZ-area.offsetZ)*area.size + x+minX - area.offsetX;
							area.columns[columnNum] = new Column( columns[xi*(range*2+1) + zi] ); //copy from columns[]
							//data.GetArea(xi+minX, zi+minZ).SetColumn(xi+minX, zi+minZ, new Column(columns[xi*(range*2+1) + zi]) ); 
						}
				}
			}
			private List<UndoColumns> undos = new List<UndoColumns>();

			public void RegisterUndo (int x, int z, int extend) 
			{ 
				UndoColumns undo = new UndoColumns(x,z,extend);
			
				int minX = x-extend; int minZ = z-extend;
				int maxX = x+extend; int maxZ = z+extend;
			
				for (int xi = 0; xi<=maxX-minX; xi++)
					for (int zi = 0; zi<=maxZ-minZ; zi++)
						undo.columns[xi*(extend*2+1) + zi] = new Column( ReadColumn(xi+minX, zi+minZ) ); //copy to columns[]
			
				if (undos.Count > 16) undos.RemoveAt(0);
				undos.Add(undo); 

				Debug.Log("Registered undo");
			}
			public void PerformUndo ()
			{
				if (undos.Count==0) return;
			
				undos[undos.Count-1].PerformUndo(this);
				undos.RemoveAt(undos.Count-1);
			}

			//public void RegisterUndo (int x, int z, int extend) {}
			//public void PerformUndo () {}

		#endregion

		#region TODO outdated
		

		
		/*
		public void Load20 (VoxelandData data)
		{
			byteColumnLength = data.sizeY;
			
			areas = new Area[100*100];
			for (int i=0; i<areas.Length; i++) areas[i] = new Area();
			
			areas[5050].Initialize(areaSize);
			
			//loading from data
			for (int x=0; x<data.sizeX; x++)
				for (int z=0; z<data.sizeZ; z++)
					GetColumn(x,z).LoadFromData(x,z,data);
			
			//saving compressed
			compressed = SaveToByteList();
			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
			#endif
		}
		
		public void WriteByteColumn (int x, int z) { GetColumn(x,z).WriteByteColumn(); }
		public void BakeByteColumn(int x,int z) { GetColumn(x,z).BakeByteColumn(); } 
		
		public void SetHeightmap (float[] heightMap, bool[] mask, int offsetX, int offsetZ, int range, byte type)
		{
			int minX = offsetX-range;
			int minZ = offsetZ-range;
			
			for (int x = 0; x<range*2; x++)
				for (int z = 0; z<range*2; z++)
			{
				if (!mask[z*range*2 + x]) continue;
				int height = (int)heightMap[z*range*2 + x];
				GetColumn(x+minX, z+minZ).SetHeight(height, type);
			}
		}
		
		public void SetLevel (int level, int offsetX, int offsetZ, int range, byte type)
		{
			int minX = offsetX-range;
			int minZ = offsetZ-range;
			
			for (int x = 0; x<range*2; x++)
				for (int z = 0; z<range*2; z++)
			{
				GetColumn(x+minX, z+minZ).SetHeight(level, type);
			}
			
			//saving compressed
			compressed = SaveToByteList();
			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
			#endif
		}

		public void Blur (int x, int y, int z, int extend, bool spherify)
		{
			//getting exists matrix
			bool[] existsMatrix = new bool[(extend*2)*(extend*2)*(extend*2)];
			bool[] refExist = new bool[128]; for (int i=1; i<refExist.Length; i++) refExist[i]=true;
			GetExistMatrix(existsMatrix, x-extend, y-extend, z-extend, x+extend, y+extend, z+extend, refExist);
			
			//blurring exists matrix
			float[] blurMatrix = new float[existsMatrix.Length];
			for (int i=0; i<existsMatrix.Length; i++) 
				if (existsMatrix[i]) blurMatrix[i] = 1.0f;
			
			for (int iteration=0; iteration<10; iteration++)
				for (int ix=1;ix<extend*2-1;ix++)
					for (int iy=1;iy<extend*2-1;iy++)
						for (int iz=1;iz<extend*2-1;iz++)
					{
						int i = iz*extend*extend*4 + iy*extend*2 + ix;
						
						blurMatrix[i] = blurMatrix[i]*0.4f + 
							(blurMatrix[i-1] + blurMatrix[i+1] + blurMatrix[i-extend*2] + blurMatrix[i+extend*2] + blurMatrix[i-extend*extend*4] + blurMatrix[i+extend*extend*4])*0.1f;
					}
			
			//setting blocks
			for (int ix=0;ix<extend*2;ix++)
				for (int iy=0;iy<extend*2;iy++)
					for (int iz=0;iz<extend*2;iz++)
				{
					if ( Mathf.Pow(ix-extend,2) + Mathf.Pow(iy-extend,2) + Mathf.Pow(iz-extend,2) > extend*extend ) continue;
					
					int i = iz*extend*extend*4 + iy*extend*2 + ix;
					if (blurMatrix[i] < 0.5f) SetBlock(x-extend+ix, y-extend+iy, z-extend+iz, 0);
					else 
					{	
						byte closestType = 0;
						if (closestType == 0) closestType = GetBlock(x-extend+ix, y-extend+iy, z-extend+iz);
						if (closestType == 0) closestType = GetBlock(x-extend+ix, y-extend+iy-1, z-extend+iz);
						if (closestType == 0) closestType = GetBlock(x-extend+ix, y-extend+iy+1, z-extend+iz);
						if (closestType == 0) closestType = GetBlock(x-extend+ix-1, y-extend+iy, z-extend+iz);
						if (closestType == 0) closestType = GetBlock(x-extend+ix+1, y-extend+iy, z-extend+iz);
						if (closestType == 0) closestType = GetBlock(x-extend+ix, y-extend+iy, z-extend+iz-1);
						if (closestType == 0) closestType = GetBlock(x-extend+ix, y-extend+iy, z-extend+iz+1);
						if (closestType == 0) closestType = GetBlock(x-extend+ix, y-extend+iy-2, z-extend+iz);
						if (closestType == 0) closestType = GetBlock(x-extend+ix, y-extend+iy+2, z-extend+iz);
						//if (closestType == 0) closestType = (byte)selected;
						
						SetBlock(x-extend+ix, y-extend+iy, z-extend+iz, closestType);
					}
				}
		}

		*/

		public void SetHeightmap (float[] heightMap, bool[] mask, int offsetX, int offsetZ, int range, byte type) { }
		public void Generate (int offsetX, int offsetZ, int range, bool overwrite) { }
	//	public void Load20 (VoxelandData data) {}
	//	public void GetMatrix (byte[] matrix, int sx, int sy, int sz, int ex, int ey, int ez) { }
	//	public void Blur (int x, int y, int z, int extend, bool spherify) { }

		#endregion
	
	
		public int areaSize = 512;
		

		public void Clear () //clear all - creating new areas array
		{
			areas = new Area[100*100];

			for (int x=0; x<100; x++)
				for (int z=0; z<100; z++)
			{
				areas[z*100 + x].offsetX = x*areaSize - areaSize*50;
				areas[z*100 + x].offsetZ = z*areaSize - areaSize*50;
				areas[z*100 + x].size = areaSize;
			}
		}

		public void Clear (int offsetX, int offsetZ, int range)
		{
			int minX = offsetX-range;
			int minZ = offsetZ-range;
			
			for (int x = 0; x<range*2; x++)
				for (int z = 0; z<range*2; z++)
			{
				int areaNum = GetAreaNum(x+minX,z+minZ);
				if (areas[areaNum].initialized) areas[areaNum].GetColumn(x+minX, z+minZ).list.Clear();
			}	
		}

		#region Get Area and Column

			public int GetAreaNum (int x, int z) { return (int)(z + areaSize*50)/areaSize*100 + (int)(x + areaSize*50)/areaSize; }
			public Area GetArea (int x, int z) { return areas[ GetAreaNum(x,z) ]; }

			public Column ReadColumn (int x, int z) //return empty column if area is not initialized
			{
				int areaNum = (int)(z + areaSize*50)/areaSize*100 + (int)(x + areaSize*50)/areaSize;
				if (!areas[areaNum].initialized) return emptyColumn;
				else return areas[areaNum].GetColumn(x,z);
			}

			public Column WriteColumn (int x, int z) //initialize area if it is not initialized
			{
				int areaNum = (int)(z + areaSize*50)/areaSize*100 + (int)(x + areaSize*50)/areaSize;
				if (!areas[areaNum].initialized) areas[areaNum].Initialize();
				return areas[areaNum].GetColumn(x,z);
			}

			public Column ReadGrassColumn (int x, int z)
			{
				int areaNum = (int)(z + areaSize*50)/areaSize*100 + (int)(x + areaSize*50)/areaSize;
				if (!areas[areaNum].initialized) return emptyColumn;
				else return areas[areaNum].GetGrass(x);
			}

			public Column WriteGrassColumn (int x, int z)
			{
				int areaNum = (int)(z + areaSize*50)/areaSize*100 + (int)(x + areaSize*50)/areaSize;
				if (!areas[areaNum].initialized) areas[areaNum].Initialize();
				return areas[areaNum].GetGrass(x);
			}

			//TODO I don't like this system - Read, Write column, and there is a Save column at last... It's better do one GetColumn

		#endregion
		
		#region Column wrappers

			public byte GetBlock (int x, int y, int z) { return ReadColumn(x,z).GetBlock(y); }
			public void SetBlock (int x, int y, int z, byte t) { WriteColumn(x,z).SetBlock(y,t); }
			public byte GetGrass (int x, int z) { return ReadGrassColumn(x,z).GetBlock(z - GetArea(x,z).offsetZ); }
			public void SetGrass (int x, int z, byte t) { WriteGrassColumn(x,z).SetBlock(z - GetArea(x,z).offsetZ,t); }
			public bool HasBlock(int x, int z, byte t) { return ReadColumn(x,z).HasBlock(t); }
			public bool HasBlock(int x, int z, bool[] exist) { return ReadColumn(x,z).HasBlock(exist); }
		
			public int GetTopPoint (int x, int z) { return ReadColumn(x,z).GetTopPoint(); }
			public int GetTopPoint (int sx, int sz, int ex, int ez)
			{
				int result = 0;
				for (int x=sx; x<=ex; x++)
					for (int z=sz; z<=ez; z++)
						result = Mathf.Max( ReadColumn(x,z).GetTopPoint(), result);
				return result;
			}
			public int GetTopPoint (int x, int z, bool[] exist) { return ReadColumn(x,z).GetTopPoint(exist); }
			public int GetTopPoint (int sx, int sz, int ex, int ez, bool[] exist)
			{
				int result = 0;
				for (int x=sx; x<=ex; x++)
					for (int z=sz; z<=ez; z++)
						result = Mathf.Max( ReadColumn(x,z).GetTopPoint(exist), result);
				return result;
			}
			public int GetBottomPoint (int x, int z, bool[] exist) { return ReadColumn(x,z).GetBottomPoint(exist); }
			public int GetBottomPoint (int sx, int sz, int ex, int ez, bool[] exist)
			{
				int result = 2147483646;
				for (int x=sx; x<=ex; x++)
					for (int z=sz; z<=ez; z++) 
						result = Mathf.Min( ReadColumn(x,z).GetBottomPoint(exist), result);
				return result;
			}
			public byte GetTopType (int x, int z) { return ReadColumn(x,z).GetTopType(); }
			public byte GetTopType (int x, int z, bool[] exist) { return ReadColumn(x,z).GetTopType(exist); }

		#endregion

		#region Matrix operations 

			public void ToMatrix (Matrix3<byte> matrix)
			{
				for (int x = matrix.offsetX; x<matrix.offsetX+matrix.sizeX; x++) 
					for (int z = matrix.offsetZ; z<matrix.offsetZ+matrix.sizeZ; z++)
						ReadColumn(x,z).ToMatrix(matrix, x,z);
			}

			public void ToExistMatrix (Matrix3<byte> matrix, bool[] exist)
			{
				for (int x = matrix.offsetX; x<matrix.offsetX+matrix.sizeX; x++) 
					for (int z = matrix.offsetZ; z<matrix.offsetZ+matrix.sizeZ; z++)
						ReadColumn(x,z).ToExistMatrix(matrix, exist, x,z);
			}

			public void SetBlocksWithMask (byte type, Matrix3<bool> mask)
			{
				for (int x = mask.offsetX; x<mask.offsetX+mask.sizeX; x++) 
					for (int z = mask.offsetZ; z<mask.offsetZ+mask.sizeZ; z++)
					{
						//checking if this column needs to be set
						bool set = false;
						mask.SetPos(x,mask.offsetY,z);
						for (int y=mask.offsetY; y<mask.offsetY+mask.sizeY; y++)
						{
							if (mask.current) { set=true; break; }
							mask.MovePosNextY();
						}
						
						//setting
						if (set) WriteColumn(x,z).SetBlocksWithMask(type, mask, x, z);
					}
			}
		
			public void ToHeightMatrix (Matrix2<int> heights, bool[] exist, int margins=0)
			{
				//TODO: use exist array
				for (int x = heights.offsetX+margins; x<heights.offsetX+heights.sizeX-margins; x++) 
					for (int z = heights.offsetZ+margins; z<heights.offsetZ+heights.sizeZ-margins; z++)
						heights[x,z] = GetTopPoint(x,z,exist);
			}

			public void ClampHeightmap (Matrix2<float> heights, int margins=0)
			{
				for (int x = heights.offsetX+margins; x<heights.offsetX+heights.sizeX-margins; x++) 
					for (int z = heights.offsetZ+margins; z<heights.offsetZ+heights.sizeZ-margins; z++)
						WriteColumn(x,z).Clamp( (int)heights[x,z] );
			}

			public void AddHeightmap (Matrix2<float> heights, byte type, int margins=0)
			{
				for (int x = heights.offsetX+margins; x<heights.offsetX+heights.sizeX-margins; x++) 
					for (int z = heights.offsetZ+margins; z<heights.offsetZ+heights.sizeZ-margins; z++)
						WriteColumn(x,z).AddBlocks( type, (int)heights[x,z]);
			}

			public void AddHeight (int level, int offsetX, int offsetZ, int sizeX, int sizeZ, byte type, int margins=0)
			{
				for (int x = offsetX; x<offsetX+sizeX; x++) 
					for (int z = offsetZ; z<offsetZ+sizeZ; z++)
						WriteColumn(x,z).AddBlocks(type, level);
			}

			public void MaxHeightmap (Matrix2<float> heights, byte type, int margins=0) //same as add, but existing height is subtracted from add level
			{
				for (int x = heights.offsetX+margins; x<heights.offsetX+heights.sizeX-margins; x++) 
					for (int z = heights.offsetZ+margins; z<heights.offsetZ+heights.sizeZ-margins; z++)
						WriteColumn(x,z).AddBlocks( type, Mathf.Max(0, (int)heights[x,z]-ReadColumn(x,z).GetTopPoint()) );
			}

			public void ExtrudeHeightmap (Matrix2<float> heights, int margins=0) //raises the last block type so that data is never lower than heightmap
			{
				for (int x = heights.offsetX+margins; x<heights.offsetX+heights.sizeX-margins; x++) 
					for (int z = heights.offsetZ+margins; z<heights.offsetZ+heights.sizeZ-margins; z++)
						WriteColumn(x,z).AddBlocks( GetTopType(x,z), (int)heights[x,z]-GetTopPoint(x,z) );
			}

			public void ClampExtrudeHeightmap (Matrix2<float> heights, int margins=0) //clamps and extrudes top type to level, so data become exactly the same as heightmap
			{
				for (int x = heights.offsetX+margins; x<heights.offsetX+heights.sizeX-margins; x++) 
					for (int z = heights.offsetZ+margins; z<heights.offsetZ+heights.sizeZ-margins; z++)
					{
						Column column = WriteColumn(x,z);
						int clampLevel = (int)heights[x,z];
						
						column.Clamp(clampLevel); //clamp
						column.AddBlocks( column.GetTopType(), clampLevel - column.GetTopPoint() ); //extruding to clamp level
					}
			}

			public void ClampExtrudeAddHeightmap (Matrix2<float> heights, Matrix2<float> depth, byte type, int margins=0) //clamps/extrudes terrain to height-depth, adds depth with selected type
			{
				for (int x = heights.offsetX+margins; x<heights.offsetX+heights.sizeX-margins; x++) 
					for (int z = heights.offsetZ+margins; z<heights.offsetZ+heights.sizeZ-margins; z++)
					{
						Column column = WriteColumn(x,z);
						int clampLevel = (int)(heights[x,z] - depth[x,z]);
						
						column.Clamp(clampLevel); //clamp
						column.AddBlocks( column.GetTopType(), clampLevel - column.GetTopPoint() ); //extruding to clamp level
						column.AddBlocks( type, (int)depth[x,z] ); //adding type
					}
			}

		#endregion

		#region Other Operations

			public Matrix2<Column> GetColumnMatrix (int offsetX, int offsetZ, int sizeX, int sizeZ) //gets deepcopy of columns in a matrix for undo
			{
				Matrix2<Column> matrix = new Matrix2<Column>(sizeX,sizeZ);
				matrix.offsetX=offsetX; matrix.offsetZ=offsetZ;
				for (int x = offsetX; x<offsetX+sizeX; x++) 
					for (int z = offsetZ; z<offsetZ+sizeZ; z++)
						matrix[x,z] = new Column( ReadColumn(x,z) );
				return matrix;
			}

			public void SetColumnMatrix (Matrix2<Column> matrix)
			{
				for (int x = matrix.offsetX; x<matrix.offsetX+matrix.sizeX; x++) 
					for (int z = matrix.offsetZ; z<matrix.offsetZ+matrix.sizeZ; z++)
					{
						//manually copy column list (as column is the struct)
						List<byte> list = WriteColumn(x,z).list;
						list.Clear();
						list.AddRange( matrix[x,z].list );
					}
			}
			
			public void Optimize (int offsetX, int offsetZ, int sizeX, int sizeZ)
			{
				for (int x = offsetX; x<offsetX+sizeX; x++) 
					for (int z = offsetZ; z<offsetZ+sizeZ; z++)
						WriteColumn(x,z).Optimize();
			}
			
			public void RemoveFloatingBlocks (int offsetX, int offsetZ, int sizeX, int sizeZ)
			{
				for (int x = offsetX; x<offsetX+sizeX; x++) 
					for (int z = offsetZ; z<offsetZ+sizeZ; z++)
				{
					Column column = WriteColumn(x,z);
					if (column.HasThinBlock()) 
					{
						int level = column.FindThinBlock();
						if (level==-1) continue; //not that type

						//checking if block really floating
						if (GetBlock(x-1,level,z)==0 && GetBlock(x+1,level,z)==0 && GetBlock(x,level,z-1)==0 && GetBlock(x,level,z+1)==0)
							SetBlock(x,level,z, 0); //Visualizer.DebugCoord(x,level,z,60f);
					}
				}
			}

			public void Check ()
			{
				for (int a=0; a<areas.Length; a++)
				{
					if (!areas[a].initialized) continue;
					Area area = areas[a];

					//for (int i=0; i<area.columns.Length; i++)
					//	if (!area.columns[i].Check())
					//		return;
					
					for (int x=area.offsetX; x<area.size+area.offsetX; x++)
						for (int z=area.offsetZ; z<area.size+area.offsetZ; z++)
						{
							if (!area.GetColumn(x,z).Check())
								{Debug.Log("Error checking column " + x + "," + z + " num:" + ((z-area.offsetZ)*area.size + x-area.offsetX) ); return; }
							if (!area.GetGrass(x).Check())
								{Debug.Log("Error checking grass " + x); return; }
						}
				}
				Debug.Log("Check ok");
			}
			

		#endregion

		#region Blur

			public byte GetClosestFilledBlock (int x, int y, int z, bool[] refExist)
			{
				byte closestType = 0;

				if (closestType == 0) closestType = GetBlock(x, y, z); if (closestType>refExist.Length || !refExist[closestType]) closestType = 0;
				if (closestType == 0) closestType = GetBlock(x, y-1, z); if (closestType>refExist.Length || !refExist[closestType]) closestType = 0;
				if (closestType == 0) closestType = GetBlock(x, y+1, z); if (closestType>refExist.Length || !refExist[closestType]) closestType = 0;
				if (closestType == 0) closestType = GetBlock(x-1, y, z); if (closestType>refExist.Length || !refExist[closestType]) closestType = 0;
				if (closestType == 0) closestType = GetBlock(x+1, y, z); if (closestType>refExist.Length || !refExist[closestType]) closestType = 0;
				if (closestType == 0) closestType = GetBlock(x, y, z-1); if (closestType>refExist.Length || !refExist[closestType]) closestType = 0;
				if (closestType == 0) closestType = GetBlock(x, y, z+1); if (closestType>refExist.Length || !refExist[closestType]) closestType = 0;
				if (closestType == 0) closestType = GetBlock(x, y-2, z); if (closestType>refExist.Length || !refExist[closestType]) closestType = 0;
				if (closestType == 0) closestType = GetBlock(x, y+2, z); if (closestType>refExist.Length || !refExist[closestType]) closestType = 0;

				return closestType;
			}
			
			public void Blur (int x, int y, int z, int extend, bool spherify, bool[] refExist) //
			{
				//getting exists matrix
				Matrix3<byte> exists = new Matrix3<byte>(extend*2, extend*2, extend*2);
				exists.offsetX = x-extend; exists.offsetY = y-extend; exists.offsetZ = z-extend;
				ToExistMatrix(exists, refExist);
				
				//blurring exists matrix
				for (int iteration=0; iteration<10; iteration++)
					for (int ix=exists.offsetX + 1; ix<exists.offsetX+exists.sizeX - 1; ix++)
						for (int iy=exists.offsetY + 1; iy<exists.offsetY+exists.sizeY - 1; iy++)
							for (int iz=exists.offsetZ + 1; iz<exists.offsetZ+exists.sizeZ - 1; iz++)
						{
							exists.SetPos(ix,iy,iz);
							exists.current = (byte)(exists.current/5*2 + (exists.prevX + exists.nextX + exists.prevY + exists.nextY + exists.prevZ + exists.nextZ)/10); //current*0.4 + (6 nights)*0.1
						}
			
				//setting blocks
				for (int ix=exists.offsetX + 1; ix<exists.offsetX+exists.sizeX - 1; ix++)
					for (int iy=exists.offsetY + 1; iy<exists.offsetY+exists.sizeY - 1; iy++)
						for (int iz=exists.offsetZ + 1; iz<exists.offsetZ+exists.sizeZ - 1; iz++)
					{
						if ( spherify && Mathf.Pow(ix-exists.offsetX-extend,2) + Mathf.Pow(iy-exists.offsetY-extend,2) + Mathf.Pow(iz-exists.offsetZ-extend,2) > extend*extend ) continue;
					
						if (exists[ix, iy, iz] < 125) SetBlock(ix, iy, iz, 0);
						else SetBlock(ix, iy, iz, GetClosestFilledBlock(ix,iy,iz,refExist));
					}
			}
		#endregion

		#region Save and load
		
			public List<byte> SaveToByteList ()
			{
				System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
				stopwatch.Start();
			
				//254 - uninitialized area, then 2 bytes count (* then +) of un-initialized areas
				//253 - initialized area
				//252 - empty column, then 2 number of empty columns
				//251 - ordinary column
				//250 - grass separator
			
				int a = 0;
				int c = 0;
				int emptyNum = 0;
			
				List<byte> byteList = new List<byte>();

				while (a<areas.Length)
				{
					if (!areas[a].initialized || !areas[a].save) 
					{
						byteList.Add(254); //uninitialized area, then 2 bytes count (* then +) of un-initialized areas
					
						//getting number of uninitialized areas
						emptyNum = 0;
						while (a < areas.Length && (!areas[a].initialized||!areas[a].save) && emptyNum <= 60000) 
							{ emptyNum++; a++; }
					
						byteList.Add( (byte)(emptyNum / 245) );
						byteList.Add( (byte)(emptyNum % 245) );
					}
					else
					{
						byteList.Add(253); //initialized area
					
						c = 0;
						while (c<areas[a].columns.Length)
						{
							if (areas[a].columns[c].list.Count == 0) 
							{
								byteList.Add(252); //empty column, then 2 number of empty columns
							
								emptyNum = 0;
								while (c < areas[a].columns.Length && areas[a].columns[c].list.Count == 0 && emptyNum < 60000) 
									{ emptyNum++; c++; }
							
								byteList.Add( (byte)(emptyNum / 245) );
								byteList.Add( (byte)(emptyNum % 245) );
							}
							else
							{
								//if (a==4950 && c==262191) Debug.Log("Test: " + areas[a].columns[c].list[0] + " " + areas[a].columns[c].list[1] + " " + areas[a].columns[c].list[2] + " " + areas[a].columns[c].list[3]);
								byteList.Add(251); //ordinary column
								byteList.AddRange( areas[a].columns[c].list );
								c++;
							}
						}
					
						a++;
					}
				}
			
				stopwatch.Stop();

				return byteList;
			}
		
			public void LoadFromByteList (List<byte> byteList)
			{
				//System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
				//stopwatch.Start();
			
				Clear();

				int areaNum = 0;
				int columnNum = 0;

				Column[] currentAreaList = null;
				List<byte> currentColumnList = null;
				//cannot use are or column directly because they are structs

			//	Column[] curColumns = null;
			//	List<byte> curList = null;
				int emptyNum = 0;
			
				for (int i=0; i<byteList.Count; i++)
				{
					byte b = byteList[i];
				
					//254 - uninitialized area, then byte count of un-initialized areas
					//253 - initialized area
					//252 - empty column, then number of empty columns
					//251 - ordinary column
				
					switch (b)
					{
						//uninitialized area, then byte count of un-initialized areas
						case 254:
							emptyNum = byteList[i+1] * 245 +  byteList[i+2];
							for (int j=0; j<emptyNum; j++) { areas[areaNum].initialized = false; areaNum++; }
							i+=2;
							//Debug.Log("Empty areas: " + emptyNum);
							break;
						
						//initialized area
						case 253:
							areas[areaNum].Initialize(); 
							areas[areaNum].save = true; //as we load this area, then it was saved somehow. Then it is 'save' area.
							currentAreaList = areas[areaNum].columns;
							areaNum++;
							columnNum = 0; //resetting columns count
							break;

						//empty column, then number of empty columns
						case 252:
							emptyNum = byteList[i+1] * 245 +  byteList[i+2];
							for (int j=0; j<emptyNum; j++) { currentAreaList[columnNum].list = new List<byte>(); columnNum++; }
							i+=2;
							break;

						//ordinary column
						case 251: 
							currentColumnList = new List<byte>();
							currentAreaList[columnNum].list = currentColumnList;
							columnNum++;
							break;

						//grass switch, not used
						case 250: break;

						default:
							currentColumnList.Add(b);
							break;
					}
				}

				//Check();

				//stopwatch.Stop();
				//Debug.Log("Load byte list: " + (0.001f * stopwatch.ElapsedMilliseconds) + " " + areas[5050].initialized);
			}
		
			public string SaveToString ()
			{
				System.IO.StringWriter str = new System.IO.StringWriter();
			
				//saving blocks
				List<byte> byteList = SaveToByteList();
				for (int b=0; b<byteList.Count; b++) str.Write( System.Convert.ToChar(byteList[b]) );
			
				//saving 
			
				return str.ToString();
			}
		
			public void LoadFromString (string s)
			{
				System.IO.StringReader str = new System.IO.StringReader(s);
				List<byte> byteList = new List<byte>();
			
				while (str.Peek() >= 0) 
					byteList.Add((byte)str.Read());
			
				LoadFromByteList(byteList);
			}

			/*public void Load20 (VoxelandData data)
			{
				Clear();
				areas[5050].Initialize();
			
				//loading from data
				for (int x=0; x<data.sizeX; x++)
					for (int z=0; z<data.sizeZ; z++)
						WriteColumn(x,z).Load20(x,z,data);

				//saving compressed
				compressed = SaveToByteList();
				#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(this);
				#endif
			}*/

			public void Center31 ()
			{
				int halfAreaSize = areaSize/2;
				for (int x = areaSize-1; x>=0; x--)
					for (int z = areaSize-1; z>=0; z--)
				{
					int areaNum = (int)(z + areaSize*50)/areaSize*100 + (int)(x + areaSize*50)/areaSize;
					if (!areas[areaNum].initialized) continue;
					Area destArea = areas[areaNum];

					areaNum = (int)(z-halfAreaSize + areaSize*50)/areaSize*100 + (int)(x-halfAreaSize + areaSize*50)/areaSize;
					if (!areas[areaNum].initialized) continue;
					Area srcArea = areas[areaNum];
					
					destArea.columns[(z-destArea.offsetZ)*areaSize + x-destArea.offsetX] = srcArea.columns[(z-halfAreaSize-srcArea.offsetZ)*areaSize + x-halfAreaSize-srcArea.offsetX];
				}

				//clearing preserving area
				Area area = areas[5050];
				Clear();
				areas[5050] = area;

				//saving compressed
				compressed = SaveToByteList();
				#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(this);
				#endif
			}

		#endregion
	}
}//namespace
