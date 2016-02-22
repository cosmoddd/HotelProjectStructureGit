using UnityEngine;
using System.Collections;

public class KeepThisObject : MonoBehaviour
{
	
public GameObject objectToKeep;

	void Awake() {

		objectToKeep = this.gameObject;

		DontDestroyOnLoad(this.gameObject);
	}
}
