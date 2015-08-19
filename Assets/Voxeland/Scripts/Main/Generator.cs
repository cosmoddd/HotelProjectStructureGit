using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Voxeland 
{
	//Testing generator
	public class Generator : MonoBehaviour
	{
		public bool update;
		
		public int seed = 12345;
		public Texture2D texture;
		public Terrain terrain;
		public Data data;

		public int sizeX = 100;
		public int sizeZ = 100;
		public VoxelandTerrain land;

		public NoiseGenerator noiseGenerator;
		public GlenGenerator glenGenerator;
		public ErosionGenerator erosionGenerator;
		public ForestGenerator forestGenerator;

		//public Transform prefab;

		//public CaveGenerator caveGenerator = new CaveGenerator();
		//public Transform caveNodesParent;
		//static public CaveGenerator.Connections connections = new CaveGenerator.Connections();


		void OnDrawGizmos () 
		{
			Visualizer.DrawGizmos();
			
			//Gizmos.DrawWireCube(new Vector3(sizeX/2, 100, sizeZ/2), new Vector3(sizeX,200,sizeZ));

			//cave generator
			//Random.seed = seed;
			//if (connections.a==null || update) { caveGenerator.Generate(null, 0,0, sizeX, sizeZ);}

			//testing noise
			//if (texture!=null && update) Noise.ToTexture(texture);

			
			if (texture!=null && update)
			{
				update = false;
				if (texture.width != sizeX || texture.height != sizeZ) texture.Resize(sizeX, sizeZ);
				
				Matrix2<float> matrix = new Matrix2<float>(texture.width, texture.height);
				TextureToMatrix(matrix, texture);
				
				//testing brush stamp
				Matrix2<float> brush = GlenGenerator.BrushStamp(texture.width/2, texture.height/2, texture.width/2-10, fallof:0.5f, noiseSize:100f);
				for (int x=0; x<matrix.sizeX; x++)
					for (int z=0; z<matrix.sizeZ; z++)
					{
						if (brush.CheckInRange(x,z)) matrix[x,z] = brush[x,z];
					}

				//testing noise
				/*System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
				stopwatch.Start();
				float minValue = 1000000;
				float maxValue = -1000000;
				for (int x=0; x<matrix.sizeX; x++)
					for (int z=0; z<matrix.sizeZ; z++)
					{
						matrix[x,z] = Noise.Fractal(x,z, 300);
						if (matrix[x,z] < minValue) minValue = matrix[x,z];
						if (matrix[x,z] > maxValue) maxValue = matrix[x,z];
					}
				stopwatch.Stop();
				Debug.Log("Noise time:" + (0.001f * stopwatch.ElapsedMilliseconds) + " min:" + minValue + " max:" + maxValue);*/

				MatrixToTexture(matrix, texture);
			}

			if (terrain!=null && update)
			{
				//use VoxelandTerrain.generate fn
			}
		}


		public Matrix2<float> TextureToMatrix (Matrix2<float> matrix, Texture2D texture)
		{
			//Matrix2<float> matrix = new Matrix2<float>(texture.width, texture.height);
			Color[] pixels = texture.GetPixels();
			for (int i=0; i<pixels.Length; i++) matrix.array[i] = (pixels[i].r + pixels[i].g + pixels[i].b)/3f; 
			return matrix;
		}

		public void NormalizeMatrix (Matrix2<float> matrix)
		{
			//finding max
			float max = 0;
			for (int i=0; i<matrix.array.Length; i++) 
				if (matrix.array[i] > max) max = matrix.array[i];

			//normilizing
			for (int i=0; i<matrix.array.Length; i++) 
				matrix.array[i] /= max;

			Debug.Log("Maximum value: " + max);
		}

		public void MatrixToTexture (Matrix2<float> matrix, Texture2D texture)
		{
			texture.Resize(matrix.sizeX, matrix.sizeZ);
			
			Color[] pixels = new Color[matrix.array.Length];
			for (int i=0; i<pixels.Length; i++) pixels[i] = new Color(matrix.array[i], matrix.array[i], matrix.array[i]);
			texture.SetPixels(pixels);
			texture.Apply();
		}

		public void TerrainToMatrix (Matrix2<float> matrix, Terrain terrain)
		{
			if (terrain.terrainData.heightmapWidth != matrix.sizeX ||
				terrain.terrainData.heightmapHeight != matrix.sizeZ)
					matrix.Resize(terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight);

			float[,] heights2D = terrain.terrainData.GetHeights(0,0,matrix.sizeX, matrix.sizeZ);

			for (int x=0; x<matrix.sizeX; x++)
				for (int z=0; z<matrix.sizeZ; z++)
					matrix[x,z] = heights2D[x,z];
		}

		public void MatrixToTerrain (Matrix2<float> matrix, Terrain terrain)
		{
			//terrain.terrainData.SetRes = matrix.sizeX;
			float [,] heights2D = new float[matrix.sizeX, matrix.sizeZ];
			for (int x=0; x<matrix.sizeX; x++)
				for (int z=0; z<matrix.sizeZ; z++)
					heights2D[x,z] = Mathf.Floor(matrix[x,z]) / 1000f;

			terrain.terrainData.SetHeights(0,0,heights2D);
		}

		public void DataToTerrain (Data data, Terrain terrain)
		{
			int size = terrain.terrainData.heightmapResolution;
			float [,] heights2D = new float[size, size];
			float [,,] splats = new float[size-1, size-1, terrain.terrainData.alphamapLayers];

			System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
			stopwatch.Start();
			
			for (int x=0; x<size-1; x++)
				for (int z=0; z<size-1; z++)
			{
					data.GetTopPoint(x,z);
					data.GetTopPoint(x,x);
					data.GetTopPoint(z,z);
					
					heights2D[x,z] = data.GetTopPoint(z,x) / terrain.terrainData.size.y;
					//if (data.GetTopType(x,z)-1 < 0 || data.GetTopType(x,z)-1 >= splats.GetLength(2) || x>=splats.GetLength(0) || z>=splats.GetLength(1) ) Debug.Log("haha");
					splats[x,z,Mathf.Max(0,data.GetTopType(z,x)-1)] = 1;
			}

			stopwatch.Stop();
			Debug.Log("DataToTerrain Time: " + (0.001f * stopwatch.ElapsedMilliseconds));

			terrain.terrainData.SetHeights(0,0,heights2D);
			terrain.terrainData.SetAlphamaps(0,0,splats);
		}

		public Matrix2<float> EqualizedMatrix (Matrix2<float> matrix)
		{
			float max = 0;
			for (int i=0; i<matrix.array.Length; i++)
				if (matrix.array[i] > max)
					max = matrix.array[i];
			
			Matrix2<float> result = new Matrix2<float>(matrix);
			for (int i=0; i<matrix.array.Length; i++)
				result.array[i] = matrix.array[i] / max;

			return result;
		}

		//void OnEnable () { update=true;}
	}
	
	
	[System.Serializable]
	public class BaseGenerator
	{
		public bool active = true;
		public byte type = 1;

		public void ToData (Data data, Matrix2<float> height, int margins=0)
		{
			data.MaxHeightmap(height, type, margins:margins);
			data.ClampHeightmap(height, margins:margins);
		}
		
		public void ToData (Data data, Matrix2<float> clamp, Matrix2<float> add, int margins=0) //only the central part of the matrix is sent to data
		{
			data.ClampExtrudeHeightmap(clamp, margins:margins);
			data.AddHeightmap(add, type, margins:margins);
		}
	}


	[System.Serializable]
	public class LevelGenerator : BaseGenerator
	{
		public int level = 1;

		public void Generate (Matrix2<float> heights)
		{
			heights.Reset(level+1);
		}
	}

	[System.Serializable]
	public class TextureGenerator : BaseGenerator
	{
		public Texture2D texture;
		public float scale = 255;

		public TextureGenerator() { active=false; }
		
		public void Generate (Matrix2<float> heights)
		{
			if (texture == null) return;
			
			Color[] pixels = texture.GetPixels();
			
			for (int x=0; x<texture.width; x++)
				for (int z=0; z<texture.height; z++)
			{
				if (!heights.CheckInRange(x,z)) continue;
				Color pixel = pixels[z*texture.height + x];
				heights[x,z] += pixel.r*scale;
			}
		}
	}

	[System.Serializable]
	public class NoiseGenerator : BaseGenerator
	{
		public int seed = 12345;
		public float amount = 300f;
		public float size = 500f;
		public float detail = 0.55f;
		public float uplift = 0.8f;
		public float ruffle = 1f;

		public void Generate (Matrix2<float> heights) { Generate(heights.array, heights.sizeX, heights.sizeZ, heights.offsetX, heights.offsetZ); }

		//Generate functions work with standard arrays, not matrices
		//this is done due to ErosionBrush compatibility
		public void Generate (float[] heights, int sizeX, int sizeZ, int shiftX, int shiftZ) //note that shiftX and Z swapped
		{
			for (int x=0; x<sizeX; x++)
				for (int z=0; z<sizeZ; z++)
			{
				float height = Noise.Fractal(x+shiftX, z+shiftZ, size, detail);
				height = (height - (1-uplift)) * amount;
				height += Noise.Random(x,z,781) * ruffle; //swithchin noise to 3d to avoid match with fractal

				heights[z*sizeX + x] = height;
			}
		}
	}

	[System.Serializable]
	public class GlenGenerator : BaseGenerator
	{
		public int seed = 123;
		public int glenNum = 4;
		public float minRadius = 30;
		public float maxRadius = 80;
		public float opacity = 0.9f;
		public float fallof = 0f;
		public float fallofNoiseSize = 133f;
		public float depth = 10; //how much type blocks set underneath

		public static Matrix2<float> BrushStamp (int posX, int posZ, float radius, float fallof=0.5f, float noiseSize=100f)
		{
			Matrix2<float> brush = new Matrix2<float>((int)radius*2+1, (int)radius*2+1);
			brush.offsetX = -(int)radius; brush.offsetZ = -(int)radius;
			
			for (int x=brush.offsetX; x<brush.offsetX+brush.sizeX; x++)
				for (int z=brush.offsetZ; z<brush.sizeZ+brush.offsetZ; z++)
			{
				//generating noise
				float value = Noise.Fractal(x+posX,z+posZ,noiseSize);
				value = Mathf.Clamp01(value);

				//finding percent from fallof to rims
				float percent = Mathf.Sqrt(x*x + z*z) / radius;
				percent = 1-percent;
				percent = Mathf.Clamp01(percent/(1-fallof));

				//making center a bit brighter
				float lightPercent = Mathf.Clamp01(percent*2-1);
				lightPercent = 3*lightPercent*lightPercent - 2*lightPercent*lightPercent*lightPercent;
				
				value = value*1.5f;

				percent = Mathf.Pow(percent,value);
				percent = 3*percent*percent - 2*percent*percent*percent;

				brush[x,z] = Mathf.Clamp01(percent);
			}

			brush.offsetX = posX-brush.sizeX/2;
			brush.offsetZ = posZ-brush.sizeZ/2;
			return brush;
		}

		public float GetBoundaryFactor (int x, int z, Matrix2<float> matrix, float dist, int margins=10)
		{
			float distFromBounds = Mathf.Min( Mathf.Min( x-matrix.offsetX, z-matrix.offsetZ ),
											  Mathf.Min( matrix.offsetX+matrix.sizeX - x, matrix.offsetZ+matrix.sizeZ - z ) );
			distFromBounds -= margins;
			if (distFromBounds < 0) return 0; //if in margins
			if (distFromBounds >= dist) return 1;
			
			float percent = distFromBounds/dist;
			percent = 3*percent*percent - 2*percent*percent*percent;
			return percent;
		}

		public void ResetHeightByBumps (Matrix2<float> heights, Matrix2<float> additional) //removes additional if height is bumpy
		{
			for (int x=heights.offsetX+1; x<heights.offsetX+heights.sizeX-1; x++)
				for (int z=heights.offsetZ+1; z<heights.offsetZ+heights.sizeZ-1; z++)
			{
				//lowering depth on popped areas
				heights.SetPos(x,z);
				float curHeight = heights.current;

				float pop = 0; //how high is current height amoung neigs?
				pop += Mathf.Max(0, curHeight - heights.prevX);
				pop += Mathf.Max(0, curHeight - heights.nextX);
				pop += Mathf.Max(0, curHeight - heights.prevZ);
				pop += Mathf.Max(0, curHeight - heights.nextZ);
				additional[x,z] = Mathf.Max(0, additional[x,z]-pop);

				//erasing depth if block is much higher than surrounds anyway
				if (pop > 1.25f) additional[x,z] = 0;
			} 
		}

		public Vector3[] Scatter (int num, Matrix2<float> heights, float minDist, float margins=0, int candidateNum=1500)
		{
			Vector3[] poses = new Vector3[num];
			for (int i=0;i<num;i++)
			{
				float[] dists = new float[candidateNum];
				Vector3[] candidates = new Vector3[candidateNum];
					
				//gathering candidates
				for (int c=0; c<candidateNum; c++)
				{
					//random coordinates
					float posX = heights.offsetX+margins+1 + Random.value*(heights.sizeX-margins*2-2);
					float posZ = heights.offsetZ+margins+1 + Random.value*(heights.sizeZ-margins*2-2);

					//finding minimum distance
					float closestDist = 1000000; 
					for (int p=0; p<i; p++)
					{
						float dist = Mathf.Pow(posX-poses[p].x,2) + Mathf.Pow(posZ-poses[p].z,2);
						if (dist<closestDist) closestDist = dist;
					}

					candidates[c] = new Vector3(posX, heights[(int)posX,(int)posZ], posZ);
					dists[c] = Mathf.Sqrt(closestDist);
				}

				//finding lowest candidate that is out of mindist
				float lowestHeight = 1000000;
				int lowestCandidate = -1;
				for (int c=0; c<candidateNum; c++)
				{
					if (dists[c] < minDist) continue;
					//if (i==num-1) Visualizer.AddSphere("Glen", candidates[c]);
					if (candidates[c].y < lowestHeight)
						{ lowestHeight = candidates[c].y; lowestCandidate = c; }
				}

				//if all candidates are too close - finding futhest, no matter of height
				if (lowestCandidate == -1)
				{
					float maxDist = 0;
					for (int c=0; c<candidateNum; c++)
						if (dists[c] > maxDist) { maxDist = dists[c]; lowestCandidate = c; }
				}

				poses[i] = candidates[lowestCandidate];

				//if (i==num-1) Visualizer.AddSphere("Glen", poses[i], color:Color.red);
			}
			return poses;
		}
		
		public void Generate (Matrix2<float> heights, out Matrix2<float> bedrock, out Matrix2<float> additional, int margins=10)
		{
			//initializing arrays
			bedrock = new Matrix2<float>(heights);
			additional = new Matrix2<float>(heights);
			
			//Visualizer.ClearGizmos("Glen");

			Vector3[] poses = Scatter(glenNum, heights, minDist:maxRadius*1.1f, margins:maxRadius/2 + margins);
			//Vector3[] poses = { new Vector3(250,50,250) };
			
			//finding minimum and maximum height
			float minHeight = 100000; float maxHeight = 0;
			for (int i=0; i<poses.Length; i++)
			{
				if (poses[i].y > maxHeight) maxHeight = poses[i].y;
				if (poses[i].y < minHeight) minHeight = poses[i].y;
			}

			for (int i=0; i<poses.Length; i++)
			{
				//float radius = minRadius + Random.value*((maxRadius-minRadius)/2);
				//get raduius depending on height
				float heightPercent = (poses[i].y - minHeight) / (maxHeight-minHeight);
				float radius = minRadius*heightPercent + maxRadius*(1-heightPercent);

				Matrix2<float> brush = BrushStamp((int)poses[i].x, (int)poses[i].z, radius, fallof, fallofNoiseSize);
				
				//Visualizer.AddCircle("Glen", poses[i], radius);
				//Visualizer.AddCircle("Glen", poses[i], radius*fallof);
				
				for (int x=brush.offsetX; x<brush.offsetX+brush.sizeX; x++)
					for (int z=brush.offsetZ; z<brush.offsetZ+brush.sizeZ; z++)
				{
					if (!heights.CheckInRange(x,z)) continue;
					
					float oldHeight = heights[x,z];
					brush[x,z] *= opacity;
					float newHeight = poses[i].y*brush[x,z] + oldHeight*(1-brush[x,z]);
					
					float boundaryFactor = GetBoundaryFactor(x,z,heights,50,margins);
					newHeight = newHeight*boundaryFactor + oldHeight*(1-boundaryFactor);
					
					heights[x,z] = newHeight;

					//setting underground
					additional[x,z] = Mathf.Max(additional[x,z], brush[x,z] * depth * boundaryFactor); //taking max point to avoid eraising by neig glen
					//additional[x,z] = brush[x,z] * depth * boundaryFactor;
				}
			}

			//adjusting depth by cavity
			ResetHeightByBumps(heights, additional);

			//saving baserock array
			for (int i=0; i<heights.array.Length; i++)
				bedrock.array[i] = heights.array[i] -  additional.array[i];
		}
	}


	[System.Serializable]
	public class ErosionGenerator : BaseGenerator
	{
		public int iterations = 3;
		public float durability = 0.9f;
		public int fluidity = 3;
		public float erosionAmount = 1f; //quantity of erosion made by iteration. Lower values require more iterations, but will give better results
		public float sedimentAmount = 0.5f; //quantity of sediment that was raised by erosion will drop back to land. Lower values will give eroded canyons with washed-out land, but can produce artefacts
		public float windAmount = 0.75f;
		public float windStrength = 5;
		public float smooth = 0.1f;
		
		#region Cross helper
		public struct Cross
		{
			public float c;
			public float px; public float nx;
			public float pz; public float nz;

			public Cross (float c, float px, float nx, float pz, float nz)
				{ this.c=c; this.px=px; this.nx=nx; this.pz=pz; this.nz=nz; }

			public Cross (Cross src)
				{ this.c=src.c; this.px=src.px; this.nx=src.nx; this.pz=src.pz; this.nz=src.nz; }
			

			public Cross (Cross c1, Cross c2) //analog of * static operator, but works in Unity5
				{ this.c=c1.c*c2.c; this.px=c1.px*c2.px; this.nx=c1.nx*c2.nx; this.pz=c1.pz*c2.pz; this.nz=c1.nz*c2.nz; }

			public Cross (float[] m, int sizeX, int sizeZ, int i)
			{
				c = m[i];
				px = m[i-1]; nx = m[i+1];
				pz = m[i-sizeX]; nz = m[i+sizeX];
			}

			public Cross (bool[] m, int sizeX, int sizeZ, int i)
			{
				c = m[i] ? 1f : 0f;
				px = m[i-1] ? 1f : 0f; nx = m[i+1] ? 1f : 0f;
				pz = m[i-sizeX] ? 1f : 0f; nz = m[i+sizeX] ? 1f : 0f;
			}

			public void ToMatrix (float[] m, int sizeX, int sizeZ, int i)
			{
				m[i] = c;
				m[i-1] = px; m[i+1] = nx;
				m[i-sizeX] = pz; m[i+sizeX] = nz;
			}

			public void Percent ()
			{
				float s = c + px + nx + pz + nz;
				if (s>0.01f) {c=c/s; px=px/s; nx=nx/s; pz=pz/s; nz=nz/s; }
				else {c=0; px=0; nx=0; pz=0; nz=0; }
			}

			public void ClampPositive () { c = c<0 ? 0:c; px = px<0 ? 0:px; nx = nx<0 ? 0:nx; pz = pz<0 ? 0:pz; nz = nz<0 ? 0:nz; }

			public float max { get{ return Mathf.Max( Mathf.Max( Mathf.Max(px,nx), Mathf.Max(pz,nz)), c);} }
			public float min { get{ return Mathf.Min( Mathf.Min( Mathf.Min(px,nx), Mathf.Min(pz,nz)), c);} }
			public float sum { get{ return c+px+nx+pz+nz; }}
			public float avg { get{ return (c+px+nx+pz+nz)/5f; }}
			public float avgAround { get{ return (px+nx+pz+nz)/4f; }}

			public void Multiply (Cross c2) { c*=c2.c; px*=c2.px; nx*=c2.nx; pz*=c2.pz; nz*=c2.nz; }
			public void Multiply (float f) { c*=f; px*=f; nx*=f; pz*=f; nz*=f; }
			public void Divide (Cross c2) { c/=c2.c; px/=c2.px; nx/=c2.nx; pz/=c2.pz; nz/=c2.nz; }
			public void Divide (float f) { c/=f; px/=f; nx/=f; pz/=f; nz/=f; }
			public void Subtract (float f) { c-=f; px-=f; nx-=f; pz-=f; nz-=f; }
			public void SubtractInverse (float f) { c=f-c; px=f-px; nx=f-nx; pz=f-pz; nz=f-nz; }
			public float GetMultipliedMax (Cross c2) { return Mathf.Max( Mathf.Max( Mathf.Max(px*c2.px,nx*c2.nx), Mathf.Max(pz*c2.pz,nz*c2.nz)), c*c2.c); }
			public float GetMultipliedSum (Cross c2) { return c*c2.c + px*c2.px + nx*c2.nx + pz*c2.pz + nz*c2.nz; }
			
			public bool isNaN { get{ return float.IsNaN(c) || float.IsNaN(px) || float.IsNaN(pz) || float.IsNaN(nx) ||float.IsNaN(nz);} }

			public float this[int n] {
				get{ switch (n) {case 0: return c; case 1: return px; case 2:return nx; case 3:return pz; case 4:return nz; default: return c;}} 
				set{ switch (n) {case 0: c=value; break; case 1: px=value; break; case 2:nx=value; break; case 3:pz=value; break; case 4:nz=value; break; default:c=value; break;}} }
			
			public void SortByHeight (int[] sorted)
			{
				for (int i=0; i<5; i++) sorted[i] = i;
				
				for (int i=0; i<5; i++) 
					for (int j=0; j<4; j++)
						if (this[sorted[j]] > this[sorted[j+1]])
						{
							int tmp = sorted[j];
							sorted[j] = sorted[j+1];
							sorted[j+1] = tmp;
						}
			}
			
			//operators cause crash in Unity5
			public static Cross operator + (Cross c1, Cross c2)  { return new Cross(c1.c+c2.c, c1.px+c2.px, c1.nx+c2.nx, c1.pz+c2.pz, c1.nz+c2.nz); }
			public static Cross operator + (Cross c1, float f)  { return new Cross(c1.c+f, c1.px+f, c1.nx+f, c1.pz+f, c1.nz+f); }
			public static Cross operator - (Cross c1, Cross c2)  { return new Cross(c1.c-c2.c, c1.px-c2.px, c1.nx-c2.nx, c1.pz-c2.pz, c1.nz-c2.nz); }
			public static Cross operator - (float f, Cross c2)  { return new Cross(f-c2.c, f-c2.px, f-c2.nx, f-c2.pz, f-c2.nz); }
			public static Cross operator - (Cross c1, float f)  { return new Cross(c1.c-f, c1.px-f, c1.nx-f, c1.pz-f, c1.nz-f); }
			public static Cross operator * (Cross c1, Cross c2)  { return new Cross(c1.c*c2.c, c1.px*c2.px, c1.nx*c2.nx, c1.pz*c2.pz, c1.nz*c2.nz); }
			public static Cross operator * (float f, Cross c2)  { return new Cross(f*c2.c, f*c2.px, f*c2.nx, f*c2.pz, f*c2.nz); }
			public static Cross operator * (Cross c1, float f)  { return new Cross(c1.c*f, c1.px*f, c1.nx*f, c1.pz*f, c1.nz*f); }
			public static Cross operator / (Cross c1, float f)  { if (f>0.001f) return new Cross(c1.c/f, c1.px/f, c1.nx/f, c1.pz/f, c1.nz/f); 
				else return new Cross(0,0,0,0,0); } 

			public Cross PercentObsolete ()
			{
				float s = c + px + nx + pz + nz;
				if (s>0.01f) return new Cross(c/s, px/s, nx/s, pz/s, nz/s);
				else return new Cross(0, 0, 0, 0, 0);
			}

			public Cross ClampPositiveObsolete () { return new Cross(c<0 ? 0:c, px<0 ? 0:px, nx<0 ? 0:nx, pz<0 ? 0:pz, nz<0 ? 0:nz); } //obsolete, do not use
		}

		public struct MooreCross
		{
			public float c;
			public float px; public float nx; public float pxpz; public float nxpz;
			public float pz; public float nz; public float pxnz; public float nxnz;
			
			public MooreCross (float c, float px, float nx, float pz, float nz, float pxpz, float nxpz, float pxnz, float nxnz)
				{ this.c=c; this.px=px; this.nx=nx; this.pz=pz; this.nz=nz;  this.pxpz=pxpz; this.nxpz=nxpz; this.pxnz=pxnz; this.nxnz=nxnz; }
			
			public MooreCross (MooreCross src)
				{ this.c=src.c; this.px=src.px; this.nx=src.nx; this.pz=src.pz; this.nz=src.nz;  this.pxpz=src.pxpz; this.nxpz=src.nxpz; this.pxnz=src.pxnz; this.nxnz=src.nxnz; }
			
			public MooreCross (float[] m, int sizeX, int sizeZ, int i)
			{
				c = m[i]; px = m[i-1]; nx = m[i+1]; pz = m[i-sizeX]; nz = m[i+sizeX];
				pxpz = m[i-1-sizeX]; nxpz = m[i+1-sizeX];
				pxnz = m[i-1+sizeX]; nxnz = m[i+1+sizeX]; 
			}

			public void ToMatrix (float[] m, int sizeX, int sizeZ, int i)
			{
				m[i] = c; m[i-1] = px; m[i+1] = nx; m[i-sizeX] = pz; m[i+sizeX] = nz;
				m[i-1-sizeX] = pxpz; m[i+1-sizeX] = nxpz;
				m[i-1+sizeX] = pxnz; m[i+1+sizeX] = nxnz; 
			}

			public void Percent ()
			{
				float s = c + px + nx + pz + nz + pxpz + nxpz + pxnz + nxnz;
				if (s>0.01f) { c/=s; px/=s; nx/=s; pz/=s; nz/=s; pxpz/=s; nxpz/=s; pxnz/=s; nxnz/=s; }
				else { c=0; px=0; nx=0; pz=0; nz=0; pxpz=0; nxpz=0; pxnz=0; nxnz=0; }
			}

			public bool isNaN { get{ return float.IsNaN(c) || float.IsNaN(px) || float.IsNaN(pz) || float.IsNaN(nx) ||float.IsNaN(nz) || float.IsNaN(pxpz) || float.IsNaN(pxnz) || float.IsNaN(nxpz) ||float.IsNaN(nxnz);} }
			public override string ToString() { return "MooreCross: " + c + ", " + px + ", " + pz + ", " + nx + ", " + nz + ", " + pxpz + ", " + nxpz + ", " + pxnz + ", " + nxnz; }

			public void ClampPositive () { c = c<0 ? 0:c; px = px<0 ? 0:px; nx = nx<0 ? 0:nx; pz = pz<0 ? 0:pz; nz = nz<0 ? 0:nz;
				pxpz = pxpz<0 ? 0:pxpz; nxpz = nxpz<0 ? 0:nxpz; pxnz = pxnz<0 ? 0:pxnz; nxnz = nxnz<0 ? 0:nxnz; }

			public float max { get{ return Mathf.Max( Mathf.Max( Mathf.Max(px,nx), Mathf.Max(pz,nz)), c);} }
			public float min { get{ return Mathf.Min( Mathf.Min( Mathf.Min(px,nx), Mathf.Min(pz,nz)), c);} }
			public float sum { get{ return c+px+nx+pz+nz; }}

			public void Multiply (float f) { c*=f; px*=f; nx*=f; pz*=f; nz*=f; pxpz*=f; nxpz*=f; pxnz*=f; nxnz*=f; }
			public void Add (float f) { c+=f; px+=f; nx+=f; pz+=f; nz+=f; pxpz+=f; nxpz+=f; pxnz+=f; nxnz+=f; }
			public void Add (MooreCross c2) { c+=c2.c; px+=c2.px; nx+=c2.nx; pz+=c2.pz; nz+=c2.nz; pxpz+=c2.pxpz; nxpz+=c2.nxpz; pxnz+=c2.pxnz; nxnz+=c2.nxnz; }
			public void Subtract (float f) { c-=f; px-=f; nx-=f; pz-=f; nz-=f; pxpz-=f; nxpz-=f; pxnz-=f; nxnz-=f; }
			public void SubtractInverse (float f) { c=f-c; px=f-px; nx=f-nx; pz=f-pz; nz=f-nz; pxpz=f-pxpz; nxpz=f-nxpz; pxnz=f-pxnz; nxnz=f-nxnz; }

			//Obsolete operators
			public static MooreCross operator + (MooreCross c1, MooreCross c2)  { return new MooreCross(c1.c+c2.c, c1.px+c2.px, c1.nx+c2.nx, c1.pz+c2.pz, c1.nz+c2.nz, c1.pxpz+c2.pxpz, c1.nxpz+c2.nxpz, c1.pxnz+c2.pxnz, c1.nxnz+c2.nxnz); }
			public static MooreCross operator + (MooreCross c1, float f)  { return new MooreCross(c1.c+f, c1.px+f, c1.nx+f, c1.pz+f, c1.nz+f, c1.pxpz+f, c1.nxpz+f, c1.pxnz+f, c1.nxnz+f); }
			public static MooreCross operator - (MooreCross c1, MooreCross c2)  { return new MooreCross(c1.c-c2.c, c1.px-c2.px, c1.nx-c2.nx, c1.pz-c2.pz, c1.nz-c2.nz, c1.pxpz-c2.pxpz, c1.nxpz-c2.nxpz, c1.pxnz-c2.pxnz, c1.nxnz-c2.nxnz); }
			public static MooreCross operator - (float f, MooreCross c2) { return new MooreCross(f-c2.c, f-c2.px, f-c2.nx, f-c2.pz, f-c2.nz, f-c2.pxpz, f-c2.nxpz, f-c2.pxnz, f-c2.nxnz); }
			public static MooreCross operator - (MooreCross c1, float f)  { return new MooreCross(c1.c-f, c1.px-f, c1.nx-f, c1.pz-f, c1.nz-f, c1.pxpz-f, c1.nxpz-f, c1.pxnz-f, c1.nxnz-f); }
			public static MooreCross operator * (MooreCross c1, MooreCross c2) { return new MooreCross(c1.c*c2.c, c1.px*c2.px, c1.nx*c2.nx, c1.pz*c2.pz, c1.nz*c2.nz, c1.pxpz*c2.pxpz, c1.nxpz*c2.nxpz, c1.pxnz*c2.pxnz, c1.nxnz*c2.nxnz); }
			public static MooreCross operator * (float f, MooreCross c2)  { return new MooreCross(f*c2.c, f*c2.px, f*c2.nx, f*c2.pz, f*c2.nz, f*c2.pxpz, f*c2.nxpz, f*c2.pxnz, f*c2.nxnz); }
			public static MooreCross operator * (MooreCross c1, float f)  { return new MooreCross(c1.c*f, c1.px*f, c1.nx*f, c1.pz*f, c1.nz*f, c1.pxpz*f, c1.nxpz*f, c1.pxnz*f, c1.nxnz*f); }
			public static MooreCross operator / (MooreCross c1, float f)  { if (f>0.001f) return new MooreCross(c1.c/f, c1.px/f, c1.nx/f, c1.pz/f, c1.nz/f, c1.pxpz/f, c1.nxpz/f, c1.pxnz/f, c1.nxnz/f);
				else return new MooreCross(0, 0,0,0,0, 0,0,0,0); }

			public MooreCross PercentObsolete ()
			{
				float s = c + px + nx + pz + nz + pxpz + nxpz + pxnz + nxnz;
				if (s>0.01f) return new MooreCross(c/s, px/s, nx/s, pz/s, nz/s, pxpz/s, nxpz/s, pxnz/s, nxnz/s);
				else return new MooreCross(0, 0,0,0,0, 0,0,0,0);
			}

			public MooreCross ClampPositiveObsolete () { return new MooreCross(c<0 ? 0:c, px<0 ? 0:px, nx<0 ? 0:nx, pz<0 ? 0:pz, nz<0 ? 0:nz, 
				pxpz<0 ? 0:pxpz, nxpz<0 ? 0:nxpz, pxnz<0 ? 0:pxnz, nxnz<0 ? 0:nxnz); }

		}
		#endregion
		
		public void Generate (Matrix2<float> heights, Matrix2<float> depths) { Generate(heights.array, depths.array, heights.sizeX, heights.sizeZ); }
		
		public void Generate (float[] heights, float[] sediments, int sizeX, int sizeZ)
		{
			#region Preparing arrays

				float[] torrents = new float[sizeX*sizeZ];
				for (int j=0; j<torrents.Length; j++) torrents[j] = 1f; //0.1 is the most to see the progress and to exclude overflow

				//sediments = new float[sizeX*sizeZ];

				int[] sort = new int[5]; //reuse of sorted heights array

			#endregion
			
			#region Sorting Heighmap
			Profiler.BeginSample("Sorting Heightmap");
				
				int[] order = new int[heights.Length];
				for (int i=0; i<order.Length; i++) order[i] = i;
			
				int[] heightsClone = new int[heights.Length]; //int array sorting is faster
				for (int i=0; i<heightsClone.Length; i++) heightsClone[i] = (int)(heights[i]*10000);
			
				//setting boundary cells in order - all negatives will be ignored
				for (int x=0; x<sizeX; x++) { order[x]=-1; order[(sizeZ-1)*sizeX+x]=-1; }
				for (int z=0; z<sizeZ; z++) { order[z*sizeX]=-1; order[z*sizeX+sizeX-1]=-1; }
				//float[] heightsClone = (float[])heights.array.Clone();
				System.Array.Sort(heightsClone, order);

			Profiler.EndSample();
			#endregion
			
			#region Creating torrents
			Profiler.BeginSample("Creating Torrents");

				for (int j=torrents.Length-1; j>=0; j--)
				{
					//finding column ordered by height
					int pos = order[j];
					if (pos<0) continue;
					
					MooreCross height = new MooreCross(heights, sizeX, sizeZ, pos);
					MooreCross torrent = new MooreCross(torrents, sizeX, sizeZ, pos);
					if (torrent.c > 2000000000) torrent.c = 2000000000;

					//creating torrents
					//MooreCross delta = (height.c - height).ClampPositiveObsolete(); //center - sides, take only positive values. Height itself will get value of 0, so all the torrent goes down
					MooreCross delta = new MooreCross(height);
					delta.SubtractInverse(height.c);
					delta.ClampPositive();

					//delta = delta.PercentObsolete(); //every side now determines a percent - how many water should go to it
					delta.Percent(); //every side now determines a percent - how many water should go to it

					//torrent = torrent + (torrent.c * delta); //spreading central torrent to sides //does not work in Unity5
					delta.Multiply(torrent.c);
					torrent.Add(delta);

					torrent.ToMatrix(torrents, sizeX, sizeZ, pos);

				}

			Profiler.EndSample();
			#endregion
		
			#region Erosion
			Profiler.BeginSample("Eroding");
				
				for (int j=torrents.Length-1; j>=0; j--)
				{
					//finding column ordered by height
					int pos = order[j];
					if (pos<0) continue;
					
					Cross height = new Cross(heights, sizeX, sizeZ, pos);
					Cross torrent = new Cross(torrents, sizeX, sizeZ, pos);
					Cross sediment = new Cross(sediments, sizeX, sizeZ, pos);	

					//erosion
					float erodeLine = (height.c + height.min)/2f; //halfway between current and maximum height

					if (height.c > erodeLine) //raising soil if column is higher than eroded column
					{
						float raised = height.c - erodeLine;
						raised = Mathf.Min(raised, raised*(torrent.c-1) * (1-durability));  //could not raise more soil than height-minHeight. //torrents always have 1 or more

						heights[pos] -= raised * erosionAmount; //raising soil
						height.c -= raised * erosionAmount;
							//if (float.IsNaN(sediment.c)) { Debug.Log("is nan before"); break; }
						sediments[pos] += raised * sedimentAmount; //and saving raised to sediment
							//if (float.IsNaN(sediment.c)) { Debug.Log("is nan inmiddle"); break; }
						sediment.c += raised * sedimentAmount;
							//if (float.IsNaN(sediment.c)) { Debug.Log("is nan after " + raised + " " + sedimentAmount); break; }
					}
				}

			Profiler.EndSample();
			#endregion

			#region Settling sediment
			Profiler.BeginSample("Settling Sediment");

				for (int l=0; l<fluidity; l++)
				for (int j=torrents.Length-1; j>=0; j--)
				{
					//finding column ordered by height
					int pos = order[j];
					if (pos<0) continue;

					Cross height = new Cross(heights, sizeX, sizeZ, pos);
					Cross sediment = new Cross(sediments, sizeX, sizeZ, pos);
					float sedimentSum = sediment.sum;
					if (sedimentSum < 0.001f) continue;
					
					//sorting cross by height
						height.SortByHeight(sort);

						//finding columns that sediment will spread to
						Cross spread = new Cross(1,1,1,1,1);
						for (int i=4; i>=0; i--) //from top to bottom
						{
							if (spread[sort[i]] < 0.5f) continue;
							
							//float curMaxLevel = (height*spread).max;
							float curMaxLevel = height.GetMultipliedMax(spread); //= (height*spread).max;

							//if ((curMaxLevel-height).ClampPositiveObsolete().sum < sedimentSum) break; //sum of lack of heights to current max level  less  then total sediment
							Cross subtraction = new Cross(height); subtraction.SubtractInverse(curMaxLevel); //curMaxLevel-height
							subtraction.ClampPositive();
							if (subtraction.sum < sedimentSum) break;  //sum of lack of heights to current max level  less  then total sediment

							spread[sort[i]] = 0; //I find your lack of sediment disturbing!
						}

						//find sediment-filled level
						float columnsRemain = spread.sum;
						float filledLevel = 0;
						if (columnsRemain > 0.01f) filledLevel = (height.GetMultipliedSum(spread) + sediment.sum)/columnsRemain; //GetMultSum() = (height*spread).sum

						//converting all remaining spread "1" to percent
						//spread = (filledLevel-height).ClampPositiveObsolete()*spread / sedimentSum;
						Cross temp = new Cross(height); 
						temp.SubtractInverse(filledLevel);
						temp.ClampPositive();
						temp.Multiply(spread);
						temp.Divide(sedimentSum);
						spread = temp;

						//transfering sediment
						//sediment = sedimentSum * spread;
						sediment = new Cross(spread);
						sediment.Multiply(sedimentSum);
						
						sediment.ToMatrix(sediments, sizeX, sizeZ, pos);
				}

				//settling down sediment
				for (int j=0; j<heights.Length; j++) heights[j] += Mathf.Max(0, sediments[j]); //for real erosion - remove normals multiplier

			Profiler.EndSample();
			#endregion

			#region Wind erosion
			Profiler.BeginSample("Wind erosion");

			for (int j=torrents.Length-1; j>=0; j--)
			{
				int pos = order[j];
				if (pos<0) continue;

				Cross height = new Cross(heights, sizeX, sizeZ, pos);
			
				//float delta = height.c - height.avgAround;
				//if (delta > 0 && delta > Random.value*windStrength) height[pos] = height.avg; //-= delta*Random.value;
				
				float windStrength = Mathf.Max(0,height.c-height.avg)*0.75f + (height.c-height.min)*0.25f;
				windStrength *= windAmount;
				float stoneStrength = Random.value;

				if (windStrength > stoneStrength) heights[pos] = Random.Range(height.min, (height.c+height.max)/2f);
				
			}
			#endregion

			#region Blur
			for (int j=torrents.Length-1; j>=0; j--)
			{
				int pos = order[j];
				if (pos<0) continue;

				Cross height = new Cross(heights, sizeX, sizeZ, pos);

				heights[pos] = height.avgAround*smooth + height.c*(1-smooth);
			}
			#endregion

		}


		public void GenerateIterational (Matrix2<float> heightmap, out Matrix2<float> bedrockLayer, out Matrix2<float> sedimentLayer, bool displayProgress=false)
		{
			bedrockLayer = heightmap.Clone();
			sedimentLayer = new Matrix2<float>(heightmap);
					
			Matrix2<float> depth = new Matrix2<float>(heightmap);

			for (int i=0; i<iterations; i++)
			{
				#if UNITY_EDITOR
				if (displayProgress &&
					UnityEditor.EditorUtility.DisplayCancelableProgressBar(
						"Voxeland Generator", 
						"Generating erosion: iteration " + i + " of " + iterations, 1f*i/iterations))
				{
					UnityEditor.EditorUtility.ClearProgressBar();
					return;
				}
				#endif
						
				depth.Reset(0);
				Generate(heightmap, depth);

				//clamping current layers to height-sediment
				for (int j=0; j<heightmap.array.Length; j++)
				{
					if (bedrockLayer.array[j] + sedimentLayer.array[j] > heightmap.array[j] - depth.array[j])
					{
						float delta = (bedrockLayer.array[j]+sedimentLayer.array[j]) - (heightmap.array[j]-depth.array[j]);
						sedimentLayer.array[j] -= delta;
								
						if (sedimentLayer.array[j] < 0) //clamping bedrock if sediment is not enough
						{
							bedrockLayer.array[j] += sedimentLayer.array[j];
							sedimentLayer.array[j] = 0;
						}
					}
				}

				//adding sediment layer depth got from erosion
				for (int j=0; j<heightmap.array.Length; j++)
					sedimentLayer.array[j] += depth.array[j];
			}

			#if UNITY_EDITOR
			if (displayProgress) UnityEditor.EditorUtility.ClearProgressBar();
			#endif

			//rounding arrays
			for (int j=0; j<heightmap.array.Length; j++)
			{
				float diff = sedimentLayer.array[j] - (int)sedimentLayer.array[j];
				if (Random.value < diff)
				{
					bedrockLayer.array[j] = Mathf.Floor(bedrockLayer.array[j]);
					sedimentLayer.array[j] = Mathf.Ceil(sedimentLayer.array[j]);
				}
				else
				{
					bedrockLayer.array[j] = Mathf.Ceil(bedrockLayer.array[j]);
					sedimentLayer.array[j] = Mathf.Floor(sedimentLayer.array[j]);
				}
			}
		}
	}



	[System.Serializable]
	public class ForestGenerator : BaseGenerator
	{
		public int initialCount = 300;
		public int iterations = 6;
		public float treeDist = 4f;
		public float[] soilTypes = new float[0];
		public bool guiShowSoilTypes = false;

		public float GetMinDist (Vector3 pos, List<Vector3> poses)
		{
			float minDist = 10000000;
			for (int i=0; i<poses.Count; i++)
			{
				float dist = (pos-poses[i]).sqrMagnitude;
				if (dist < minDist) minDist = dist;
			}
			return Mathf.Sqrt(minDist);
		}

		public List<Vector3> Generate (Data data, int offsetX, int offsetZ, int sizeX, int sizeZ)
		{
			//initial tree scatter
			List<Vector3> trees = new List<Vector3>();
			for (int i=0; i<initialCount; i++)
			{
				Vector3 treePos = new Vector3( 
					(int)(Random.value*sizeX) + offsetX, 0, 
					(int)(Random.value*sizeZ) + offsetZ);
				treePos.y = data.GetTopPoint((int)treePos.x,(int)treePos.z);
				trees.Add(treePos);
			}
			
			//killing or spawning tree
			for (int iteration=0; iteration<iterations; iteration++)
			{
				for (int t=trees.Count-1; t>=0; t--) //we delete trees, so counting backwards
				{
					byte soil = data.GetTopType((int)trees[t].x, (int)trees[t].z);

					float liveChance = 0f;
					if (soil<soilTypes.Length) liveChance = soilTypes[soil];

					//spawning tree
					if (liveChance > Random.value)
					{
						for (int i=0; i<10; i++)
						{
							Vector3 saplingPos = trees[t] + new Vector3( 
								(int)(Random.value*treeDist*4 - treeDist*2), 0, 
								(int)(Random.value*treeDist*4 - treeDist*2) );

							if (saplingPos.x < offsetX+1 || saplingPos.x > offsetX+sizeX-2 ||
								saplingPos.z < offsetZ+1 || saplingPos.z > offsetZ+sizeZ-2) continue;
							
							if (GetMinDist(saplingPos, trees) > treeDist)
							{
								saplingPos.y = data.GetTopPoint((int)saplingPos.x,(int)saplingPos.z);
								trees.Add(saplingPos);
								break;
							}
						}
					}
					
					//killing tree
					else
					{
						trees.RemoveAt(t);
						continue;
					}
				}
			}

			return trees;

			//debugging
			//Visualizer.ClearGizmos("Trees");
			//for (int i=0; i<trees.Count; i++) 
			//	Visualizer.AddSphere("Trees", trees[i] + Vector3.one/2, 2f);
		}

		public void ToData (Data data, List<Vector3> trees)
		{
			for (int t=0; t<trees.Count; t++)
				data.WriteColumn((int)trees[t].x, (int)trees[t].z).AddBlocks(type, 1);
		}
	}

	[System.Serializable]
	public class GrassGenerator : BaseGenerator
	{
		public float heightMinHeight = 50f;
		public float heightMaxHeight = 100f;
		public float heightMinChance = 0.01f;
		public float heightMaxChance = 0.2f;
		
		public int spreadIterations = 10000;

		public float noiseSize = 40f;
		public float noiseDensity = 0.25f;
		
		public Matrix2<byte> Generate (Matrix2<float> heights, List<Vector3> trees)
		{
			Matrix2<byte> grass = new Matrix2<byte>(heights);

			//under-tree grass
			if (trees!=null)
			for (int i=0; i<trees.Count; i++)
			{
			int x = (int)trees[i].x; int z = (int)trees[i].z;
				grass[x,z] = type;
				
				if (grass.IsOnEdge(x,z)) continue;
				grass.SetPos(x,z);
				if (Noise.FastRandom() > 0.5f) grass.nextX = type;
				if (Noise.FastRandom() > 0.5f) grass.nextZ = type;
				if (Noise.FastRandom() > 0.5f) grass.prevX = type;
				if (Noise.FastRandom() > 0.5f) grass.prevZ = type;
			}

			//placing grass by height
			for (int x=heights.offsetX; x<heights.offsetX+heights.sizeX; x++)
				for (int z=heights.offsetZ; z<heights.offsetZ+heights.sizeZ; z++)
			{
				float chance = 1 - (heights[x,z]-heightMinHeight)/heightMaxHeight;
				chance = Mathf.Clamp(chance, heightMinChance, heightMaxChance);
				if (chance > Noise.FastRandom()) grass[x,z] = type;
			}

			//by cavity
			/*for (int x=heights.offsetX+6; x<heights.offsetX+heights.sizeX-6; x+=3)
				for (int z=heights.offsetZ+6; z<heights.offsetZ+heights.sizeZ-6; z+=3)
			{
				float neigHeight = 0;
				for (int xi=-3; xi<=3; xi++)
					for (int zi=-3; zi<=3; zi++)
						neigHeight += heights[x+xi, z+zi];
				neigHeight /= 49;

				if (neigHeight < heights[x,z]-0.1f) grass[x,z] = type;
			}*/
			
			//noise grass
			for (int x=grass.offsetX; x<grass.offsetX+grass.sizeX; x++)
				for (int z=grass.offsetZ; z<grass.offsetZ+grass.sizeZ; z++)
					if (Noise.Fractal(x,z,noiseSize) > (1-noiseDensity)) grass[x,z]=type;

			//randomly expanding single bushes
			for (int i=0; i<10000; i++)
			{
				int num = (int)(Noise.FastRandom() * grass.array.Length);

				while (grass.array[num]==0 && num<grass.array.Length-1) num++;

				if (grass.IsOnEdge(num)) continue;

				grass.pos = num;
				byte type = grass.current;
			
				grass.nextX = type; 
				grass.nextZ = type;
				grass.prevX = type;
				grass.prevZ = type;
			}


			return grass;
		}

		public void ToData (Data data, Matrix2<byte> grass, int margins=0)
		{
			Matrix3<byte> setBlockMatrix = new Matrix3<byte>(1,grass.sizeZ,1);

			for (int x=grass.offsetX+margins; x<grass.offsetX+grass.sizeX-margins; x++)
			{
				for (int z=grass.offsetZ+margins; z<grass.offsetZ+grass.sizeZ-margins; z++)
					setBlockMatrix[0,z-grass.offsetZ+margins,0] = grass[x,z];
				data.WriteGrassColumn(x,grass.offsetZ+margins).FromMatrix(setBlockMatrix, 0,0);
			}
		}
	}

}
