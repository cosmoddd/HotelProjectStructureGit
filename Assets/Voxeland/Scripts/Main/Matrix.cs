using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Voxeland 
{
	[System.Serializable]
	public class Matrix2<T>
	{
		public T[] array; //must be private
		
		public static int[] spiral; //number of array member depending on its distance from center

		public int sizeX;
		public int sizeZ;

		public int offsetX;
		public int offsetZ;

		public int pos;

		public int[] orderedByDistance; //from center
		
		public T this[int x, int z] 
		{
			get { return array[(z-offsetZ)*sizeX + x - offsetX]; }
			set { array[(z-offsetZ)*sizeX + x - offsetX] = value; }
		}
		
		public bool CheckInRange (int x, int z)
		{
			return (x- offsetX >= 0 && x- offsetX < sizeX &&
			        z- offsetZ >= 0 && z- offsetZ < sizeZ);
		}

		public int GetNum (int x, int z) { return (z-offsetZ)*sizeX + x - offsetX; }
		
		public Matrix2 (int x, int z)
		{
			array = new T[x*z];
			sizeX = x;
			sizeZ = z;
			offsetX = 0;
			offsetZ = 0;
			pos = 0;
			orderedByDistance = new int[0];
		}

		public Matrix2 (Matrix2<T> m)
		{
			sizeX = m.sizeX;
			sizeZ = m.sizeZ;
			array = new T[sizeX*sizeZ];
			offsetX = m.offsetX;
			offsetZ = m.offsetZ;
			pos = m.pos;
			orderedByDistance = new int[0];
		}

		public Matrix2 (Matrix2<float> m)
		{
			sizeX = m.sizeX;
			sizeZ = m.sizeZ;
			array = new T[sizeX*sizeZ];
			offsetX = m.offsetX;
			offsetZ = m.offsetZ;
			pos = m.pos;
			orderedByDistance = new int[0];
		}

		public void GetValuesFrom (Matrix2<T> m)
		{
			for (int x=offsetX; x<offsetX+sizeX; x++)
				for (int z=offsetZ; z<offsetZ+sizeZ; z++)
					if (m.CheckInRange(x,z)) this[x,z] = m[x,z];
		}

		public void Clear ()
		{
			array = new T[0];
			sizeX = 0;
			sizeZ = 0;
		}

		public Matrix2<T> Clone ()
		{
			Matrix2<T> clone = new Matrix2<T>(this);
			for (int i=0; i<array.Length; i++) clone.array[i] = array[i]; //array.Clone() is slower
			return clone;
		}

		public void SetPos(int x,int z) { pos = (z-offsetZ)*sizeX + x - offsetX; }

		public T current { get { return array[pos]; } set { array[pos] = value; } }
		public T nextX { get { return array[pos+1]; } set { array[pos+1] = value; } }
		public T prevX { get { return array[pos-1]; } set { array[pos-1] = value; } }
		public T nextZ { get { return array[pos+sizeX]; } set { array[pos+sizeX] = value; } }
		public T prevZ { get { return array[pos-sizeX]; } set { array[pos-sizeX] = value; } }
		public T nextXnextZ { get { return array[pos+sizeX+1]; } set { array[pos+sizeX+1] = value; } }
		public T prevXnextZ { get { return array[pos+sizeX-1]; } set { array[pos+sizeX-1] = value; } }
		public T nextXprevZ { get { return array[pos-sizeX+1]; } set { array[pos-sizeX+1] = value; } }
		public T prevXprevZ { get { return array[pos-sizeX-1]; } set { array[pos-sizeX-1] = value; } }

		public int centerX { get {return offsetX + Mathf.FloorToInt(sizeX/2f) -1; } set {offsetX = value - Mathf.FloorToInt(sizeX/2f) +1; } }
		public int centerZ { get {return offsetZ + Mathf.FloorToInt(sizeZ/2f) -1; } set {offsetZ = value - Mathf.FloorToInt(sizeZ/2f) +1; } }
		public int maxX { get {return offsetX + sizeX; } }
		public int maxZ { get {return offsetZ + sizeZ; } }

		public void MovePosNextZ () { pos += sizeX; }

		public bool IsOnEdge (int x, int z) { return x==offsetX || x==offsetX+sizeX-1 || z==offsetZ || z==offsetZ+sizeZ-1; }

		public bool IsOnEdge (int pos)
		{ 
			return 
				pos < sizeX ||
				pos >= array.Length - sizeX ||
				pos%sizeX == 0 ||
				pos%sizeX == sizeX-1; 
				//test: if (temp.IsOnEdge(x,z) != temp.IsOnEdge(temp.pos))  Debug.Log("error " + x + " " + z + " " + temp.pos + " " + temp.IsOnEdge(temp.pos));
		}

		public T GetByDistance(int num) //from center
		{
			//generating spiral if it does not exist
			if (spiral==null)
			{
				spiral = new int[2500]; //50*50

			}

			//generating ordered array if needed
			if (orderedByDistance.Length == 0) 
			{
				List<int> arrayNums = new List<int>();
				for (int z=0; z<sizeZ; z++)
					for (int x=0; x<sizeX; x++)
						arrayNums.Add(z*sizeX + x);

				List<int> distances = new List<int>();
				for (int z=0; z<sizeZ; z++)
					for (int x=0; x<sizeX; x++)
						//distances.Add((int)Mathf.Pow(x-sizeX/2,2) + (int)Mathf.Pow(z-sizeZ/2,2));
						distances.Add(Mathf.Max( Mathf.Abs(x-sizeX/2), Mathf.Abs(z-sizeZ/2) ));

				for (int i=0; i<array.Length; i++) 
					for (int d=0; d<distances.Count-1; d++)
						if (distances[d] > distances[d+1])
						{
							int temp = distances[d+1];
							distances[d+1] = distances[d];
							distances[d] = temp;

							temp = arrayNums[d+1];
							arrayNums[d+1] = arrayNums[d];
							arrayNums[d] = temp;
						}
				
				orderedByDistance = arrayNums.ToArray();
			}
			
			return array[orderedByDistance[num]];
		}

		public void Reset(T def)
		{
			if(array == null) array = new T[0];
			for(int i=0;i<array.Length;i++) array[i] = def;
		}

		public void Resize (int x, int z)
		{
			sizeX = x; sizeZ = z;
			array = new T[sizeX*sizeZ];
		}
	}
	
	[System.Serializable]
	public class Matrix3<T>
	{
		public T[] array; //must be private
		
		public int sizeX;
		public int sizeY;
		public int sizeZ;
		
		public int offsetX;
		public int offsetY;
		public int offsetZ;
		
		public int pos;
		
		public T this[int x, int y, int z] 
		{
			get { return array[(z-offsetZ)*sizeX*sizeY + (y-offsetY)*sizeX + x - offsetX]; }
			set { array[(z-offsetZ)*sizeX*sizeY + (y-offsetY)*sizeX + x - offsetX] = value; }
			/*
				try {array[(z-offsetZ)*sizeX*sizeY + (y-offsetY)*sizeX + x - offsetX] = value; }
				catch(System.Exception ex) { Debug.Log("offsetX:" + offsetX + " sizeX:" + sizeX + 
					"offsetY:" + offsetY + " sizeY:" + sizeY + 
					"offsetZ:" + offsetZ + " sizeZ:" + sizeZ +
					"   coords:" + x + ", " + y + ", " + z); throw; }
				}
			/*
			get 
			{ 
				if (x-offsetX < 0) Debug.LogError("Value of x (" + x.ToString() + ") is less then offset (" + offsetX.ToString() + ")" );
				if (y-offsetY < 0) Debug.LogError("Value of y (" + y.ToString() + ") is less then offset (" + offsetY.ToString() + ")" );
				if (z-offsetZ < 0) Debug.LogError("Value of z (" + z.ToString() + ") is less then offset (" + offsetZ.ToString() + ")" );
				if (x-offsetX >= sizeX) Debug.LogError("Value of x (" + x.ToString() + ") is equal or more then size (" + sizeX.ToString() + ") + offset(" + offsetX.ToString() + ")" );
				if (y-offsetY >= sizeY) Debug.LogError("Value of y (" + y.ToString() + ") is equal or more then size (" + sizeY.ToString() + ") + offset(" + offsetY.ToString() + ")" );
				if (z-offsetZ >= sizeZ) Debug.LogError("Value of z (" + z.ToString() + ") is equal or more then size (" + sizeZ.ToString() + ") + offset(" + offsetZ.ToString() + ")" );
				return array[(z-offsetZ)*sizeX*sizeY + (y-offsetY)*sizeX + x - offsetX]; 
			}
			*/
		}
		
		public bool CheckInRange (int x, int y, int z)
		{
			return (x-offsetX >= 0 && x-offsetX < sizeX &&
				y-offsetY >= 0 && y-offsetY < sizeY &&
				z-offsetZ >= 0 && z-offsetZ < sizeZ);
		}

		public int GetPos (int x,int y,int z) { return (z-offsetZ)*sizeX*sizeY + (y-offsetY)*sizeX + x - offsetX; } 
		public void SetPos (int x, int y, int z) { pos = (z-offsetZ)*sizeX*sizeY + (y-offsetY)*sizeX + x - offsetX; } 
			//if (pos>=array.Length) Debug.Log("pos:" + x + " " + y + " " + z + " offset:" + offsetX + " " + offsetY + " " + offsetZ + " size:" + sizeX + " " + sizeY + " " + " " + sizeZ + " offsetSize:" + (sizeX+offsetX) + " " + (sizeY+offsetY) + " " + (sizeZ+offsetZ));}
		public void MovePos (int x, int y, int z) { pos += z*sizeX*sizeY + y*sizeX + x; }
		public void MovePosNextY () { pos += sizeX; }
		public void MovePosPrevY () { pos -= sizeX; }
		
		public T current { get { return array[pos]; } 			set { array[pos] = value; } }
		public T nextX { get { return array[pos+1]; } 			set { array[pos+1] = value; }  }
		public T prevX { get { return array[pos-1]; } 			set { array[pos-1] = value; }  }
		public T nextY { get { return array[pos+sizeX]; }		set { array[pos+sizeX] = value; }  }
		public T prevY { get { return array[pos-sizeX]; } 		set { array[pos-sizeX] = value; }  }
		public T nextZ { get { return array[pos+sizeX*sizeY]; } set { array[pos+sizeX*sizeY] = value; }  }
		public T prevZ { get { return array[pos-sizeX*sizeY]; } set { array[pos-sizeX*sizeY] = value; }  }
		public T nextXnextY { get { return array[pos+1+sizeX]; } set { array[pos+1+sizeX] = value; } }
		public T prevXnextY { get { return array[pos-1+sizeX]; } set { array[pos-1+sizeX] = value; } }
		public T nextZnextY { get { return array[pos+sizeX*sizeY+sizeX]; } set { array[pos+sizeX*sizeY+sizeX] = value; } }
		public T prevZnextY { get { return array[pos-sizeX*sizeY+sizeX]; } set { array[pos-sizeX*sizeY+sizeX] = value; } }
		public T nextXprevY { get { return array[pos+1-sizeX]; } set { array[pos+1-sizeX] = value; } }
		public T prevXprevY { get { return array[pos-1-sizeX]; } set { array[pos-1-sizeX] = value; } }
		public T nextZprevY { get { return array[pos+sizeX*sizeY-sizeX]; } set { array[pos+sizeX*sizeY-sizeX] = value; } }
		public T prevZprevY { get { return array[pos-sizeX*sizeY-sizeX]; } set { array[pos-sizeX*sizeY-sizeX] = value; } }
		
		/*
		public T SafeGet (int x, int y, int z)
		{
			x = Mathf.Clamp(x, 0,sizeX);
			y = Mathf.Clamp(y, 0,sizeY);
			z = Mathf.Clamp(z, 0,sizeZ);
			return array[z*sizeX*sizeY + y*sizeX + x];
		}
		*/
		
		public Matrix3 (int x, int y, int z)
		{
			array = new T[x*y*z];
			sizeX = x;
			sizeY = y;
			sizeZ = z;
		}
		
		public Matrix3 (Matrix3<T> m)
		{
			sizeX = m.sizeX;
			sizeY = m.sizeY;
			sizeZ = m.sizeZ;
			array = new T[sizeX*sizeY*sizeZ];
			offsetX = m.offsetX;
			offsetY = m.offsetY;
			offsetZ = m.offsetZ;
		}

		public void Reset(T def)
		{
			if(array == null) array = new T[0];
			for(int i=0;i<array.Length;i++) array[i] = def;
		}
	}
	
	[System.Serializable]
	public struct Matrix4<T>
	{
		public T[] array; //must be private
		
		public int sizeX;
		public int sizeY;
		public int sizeZ;
		public int sizeW;

		public int stepZ;
		public int stepW;
		
		public int offsetX;
		public int offsetY;
		public int offsetZ;
		public int offsetW;
		
		public int pos;
		
		public T this[int x, int y, int z, int w] 
		{
			get { return array[(w-offsetW)*stepW + (z-offsetZ)*stepZ + (y-offsetY)*sizeX + x - offsetX]; }
			set { array[(w-offsetW)*stepW + (z-offsetZ)*stepZ + (y-offsetY)*sizeX + x - offsetX] = value; }
		}
		
		public bool CheckInRange (int x, int y, int z, int w)
		{
			return (x-offsetX >= 0 && x-offsetX < sizeX &&
			        y-offsetY >= 0 && y-offsetY < sizeY &&
			        z-offsetZ >= 0 && z-offsetZ < sizeZ &&
			        w-offsetW >= 0 && w-offsetW < sizeW);
		}
		
		public bool CheckInRange (int x, int y, int z)
		{
			return (x-offsetX >= 0 && x-offsetX < sizeX &&
			        y-offsetY >= 0 && y-offsetY < sizeY &&
			        z-offsetZ >= 0 && z-offsetZ < sizeZ);
		}
		
		public void SetPos (int x, int y, int z, int w) { pos = (w-offsetW)*stepW + (z-offsetZ)*stepZ + (y-offsetY)*sizeX + x - offsetX; }
		public void MovePos (int x, int y, int z, int w) { pos += stepW + z*stepZ + y*sizeX + x; }
		public void MovePosNextY ()  { pos += sizeX; }
		
		public T current { get { return array[pos]; } 			set { array[pos] = value; } }
		public T nextX { get { return array[pos+1]; } 			set { array[pos+1] = value; }  }
		public T prevX { get { return array[pos-1]; } 			set { array[pos-1] = value; }  }
		public T nextY { get { return array[pos+sizeX]; }		set { array[pos+sizeX] = value; }  }
		public T prevY { get { return array[pos-sizeX]; } 		set { array[pos-sizeX] = value; }  }
		public T nextZ { get { return array[pos+stepZ]; } set { array[pos+stepZ] = value; }  }
		public T prevZ { get { return array[pos-stepZ]; } set { array[pos-stepZ] = value; }  }
		public T nextW { get { return array[pos+stepZ]; } set { array[pos+stepW] = value; }  }
		public T prevW { get { return array[pos-stepZ]; } set { array[pos-stepW] = value; }  }
		public T nextXnextY { get { return array[pos+1+sizeX]; } set { array[pos+1+sizeX] = value; } }
		public T prevXnextY { get { return array[pos-1+sizeX]; } set { array[pos-1+sizeX] = value; } }
		public T nextZnextY { get { return array[pos+stepZ+sizeX]; } set { array[pos+stepZ+sizeX] = value; } }
		public T prevZnextY { get { return array[pos-stepZ+sizeX]; } set { array[pos-stepZ+sizeX] = value; } }
		
		public Matrix4 (int x, int y, int z, int w)
		{
			array = new T[w*x*y*z];
			sizeX = x;
			sizeY = y;
			sizeZ = z;
			sizeW = w;
			offsetX = 0;
			offsetY = 0;
			offsetZ = 0;
			offsetW = 0;
			pos = 0;
			stepW = sizeX*sizeY*sizeZ;
			stepZ = sizeX*sizeY;
		}
		
		public void Reset (T def)
		{
			if (array == null) array = new T[0];
			for (int i=0; i<array.Length; i++) array[i] = def;
		}
	}
	

}


