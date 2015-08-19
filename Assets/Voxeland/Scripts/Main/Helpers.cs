using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace Voxeland 
{
	

	
	[System.Serializable]
	public struct IdList // could be generic, but Unity does not serialize generic list. Bad for testing.
	{
		public List<int> keys;
		public List<Transform> vals;
		
		public void Init()
		{
			keys = new List<int>();
			vals = new List<Transform>();
		}
		
		public int GetNum (int key) { return GetNum(key, 0, keys.Count-1); }
		public int GetNum (int key, int start, int stop)
		{
			if (stop-start <= 4)
			{
				for (int i=start; i<=stop; i++) 
					if (keys[i]==key) return i; //will return -1 if key not found
				return -1;
			}
			else
			{
				int halfWay = start + (stop-start)/2;
				if (key > keys[halfWay]) return GetNum(key, halfWay, stop);
				else return GetNum(key, start, halfWay);
			}
		}

		public int AddNum (int key) { return AddNum(key, 0, keys.Count-1); }
		public int AddNum (int key, int start, int stop)
		{
			if (stop-start <= 4)
			{
				for (int i=start; i<=stop; i++) 
					if (keys[i]>key) return i; //will return next key when key not found. Same as GetNum in other 
				return -1;
			}
			else
			{
				int halfWay = start + (stop-start)/2;
				if (key > keys[halfWay]) return AddNum(key, halfWay, stop);
				else return AddNum(key, start, halfWay);
			}
		}

		public Transform this[int key]
		{
			get { return vals[GetNum(key)]; }
			set 
			{ 
				int num = GetNum(key);
				if (num >=0 ) vals[num] = value;

			}
		}
		
		public void Add (int key, Transform val)
		{
			
			int num = AddNum(key-1);
			
			if (num >= 0)
			{
				vals.Insert(num, val); 
				keys.Insert(num, key);
			}
			else
			{
				vals.Add(val); 
				keys.Add(key);
			}
		}
		
		public void Remove (int key)
		{
			int num = GetNum(key);
			if (num != -1)
			{
				keys.RemoveAt(num);
				vals.RemoveAt(num);
			}
		}
		
		public Transform Withdraw (int key)
		{
			int num = GetNum(key);
			if (num != -1)
			{
				Transform tfm = vals[num];
				keys.RemoveAt(num);
				vals.RemoveAt(num);
				return tfm;
			}
			else return null; //default(T);
		}
		
		public Transform WithdrawFirst ()
		{
			if (vals.Count==0) return default(Transform);
			Transform tfm = vals[0];
			keys.RemoveAt(0);
			vals.RemoveAt(0);
			return tfm;
		}
		
		public Transform GetValAt (int num) { return vals[num]; }
		public int GetKeyAt (int num) { return keys[num]; }
		public void RemoveAt (int num) { vals.RemoveAt(num); keys.RemoveAt(num); }
		public void Clear () { vals.Clear(); keys.Clear(); }
		
		public int Count { get{ return (keys.Count); } }

		/*
		Testing:
			
		 	int[] checkKeys = {-1, 0, 1, 52, 32, 22, 12, 11, 13, 1456, -83543, 2, 53, 54, 55};
			int[] checkVals = {1 , 2, 3, 10, 9,  8,  6,  5,  7,  14,   0,      4, 11, 12, 13};
		 
			idList.Clear();
		 
			for (int j=0; j<checkKeys.Length; j++) idList.Remove(checkKeys[j]);
			for (int j=0; j<checkKeys.Length; j++) idList.Add(checkKeys[j], checkVals[j]);

			idList.Remove(52);

			idList.WithdrawFirst();
			idList.WithdrawFirst();

			idList.Add(52,10);
			idList.Add(-83,0);
			idList.Add(-1,1);

			bool checkOk = true;
			for (int j=0; j<checkKeys.Length; j++)
				if (idList[ checkKeys[j] ] != checkVals[j]) checkOk = false;
			
			if (!checkOk) Debug.Log("Error");
		*/
	}
}