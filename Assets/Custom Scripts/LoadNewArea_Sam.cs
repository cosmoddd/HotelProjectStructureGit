using UnityEngine;
using System.Collections;

public class LoadNewArea_Sam : MonoBehaviour {
	public GameObject[] objectsToLoad;
	public bool activate = true;
//	public GameObject[] objectToDestroy;

	private bool destroySelf = true;

	public GameObject thisObject;


	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTriggerEnter(Collider other){

		thisObject.GetComponent<Collider>().enabled = false;

		if (other.tag == "Player"){
			//load our gameobject over time
			for(int i = 0; i < objectsToLoad.Length; i++){
				StartCoroutine(LoadOverTime(objectsToLoad[i]));
			}
		
			}

	}

	IEnumerator LoadOverTime(GameObject g){
		Transform[] children = g.GetComponentsInChildren<Transform>(true);
		Debug.Log(children.Length);
		for(int i = 0; i < children.Length; i++){
			children[i].gameObject.SetActive(activate);
			if (i % 10 == 0)
			{
				yield return new WaitForEndOfFrame();
			}
			//Destroy(gameObject);
		}
	}

	
}
