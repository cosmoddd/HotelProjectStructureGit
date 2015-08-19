using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Specialized;

namespace Voxeland 
{

	[System.Serializable]
	public class ObjectPool
	{
		public IdList used;
		public IdList free; //destroyed, but still visually active objects
		public IdList inactive;

		public Transform prefab;
		public bool autoActivate = true; //if disabled SetActive function has to be called each update

		//for smart prefabs, just for convenience
		public float chance;

		public ObjectPool ()
		{
			used.Init();
			free.Init();
			inactive.Init();
		}

		public Transform Instantiate (Vector3 pos, Quaternion rotation, Transform parent)
		{
			if (prefab==null) return null;
			
			//taking object from array
			Transform tfm = free.WithdrawFirst();
			if (tfm==null) 
			{ 
				tfm = inactive.WithdrawFirst();
				if (autoActivate && tfm != null) tfm.gameObject.SetActive(true);
			}
			
			//spawning object if it was not found
			if (tfm==null) 
			{
				tfm = (Transform)Transform.Instantiate(prefab);
				tfm.name = prefab.name + " " + tfm.GetInstanceID().ToString();
			}
			
			//placing obj
			//object should be positioned before it's enable to be compatable with static batching
			tfm.position = pos;
			tfm.rotation = rotation;
			//if (tfm.parent != parent) 
			tfm.parent = parent;
			 //tfm.GetInstanceID().ToString();

			//signing in listdict
			used.Add(tfm.GetInstanceID(), tfm);
			//already activated when took from inactive
			return tfm;
		}

		public void Remove (Transform srctfm)
		{
			int id = srctfm.GetInstanceID();
			Transform tfm = used.Withdraw(id);

			if (tfm != null)
			{	
				if (autoActivate) { inactive.Add(id,tfm); tfm.gameObject.SetActive(false); }
				else free.Add(id,tfm);
			}
		}

		public void RemoveChildren (Transform parent)
		{
			for (int i=0; i<parent.childCount; i++)
				Remove(parent.GetChild(i));
		}

		
		public void SetActiveState ()
		{
			//turning on all used
			for (int i=0; i<used.Count; i++) 
			{
				Transform tfm = used.GetValAt(i);
				if (tfm!=null && !tfm.gameObject.activeSelf) tfm.gameObject.SetActive(true);
			}

			//turning off all free
			for (int i=0; i<free.Count; i++) 
			{
				Transform tfm = free.GetValAt(i);
				if (tfm.gameObject.activeSelf) tfm.gameObject.SetActive(false);
				inactive.Add(tfm.GetInstanceID(), tfm);
			}
			free.Clear();
		}
		
		
		public void RemoveAll ()
		{
			if (used.keys==null) return;

			for (int i=used.Count-1; i>=0; i--)
			{
				int id = used.GetKeyAt(i);
				Transform tfm = used.GetValAt(i);
				//removing tfm from used when clearing all the dict
				
				if (autoActivate) { inactive.Add(id,tfm); tfm.gameObject.SetActive(false); }
				else free.Add(id, tfm);
			}

			used.Clear();
		}

		public void Clear () //removing all objects
		{
			if (used.keys==null) return;

			for (int i=used.Count-1; i>=0; i--) GameObject.DestroyImmediate(used.GetValAt(i).gameObject);
			for (int i=free.Count-1; i>=0; i--) GameObject.DestroyImmediate(free.GetValAt(i).gameObject);
			for (int i=inactive.Count-1; i>=0; i--) GameObject.DestroyImmediate(inactive.GetValAt(i).gameObject);

			used.Clear(); 
			free.Clear(); 
			inactive.Clear();
		}
	}
}
