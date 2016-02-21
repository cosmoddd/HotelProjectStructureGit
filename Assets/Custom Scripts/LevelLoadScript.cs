using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LevelLoadScript : MonoBehaviour {

	public string sceneName;
	// Use this for initialization
	/*
	void Start () {
	
	}
*/	
	// Update is called once per frame
	void Update () {
	
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			SceneManager.UnloadScene(sceneName);
		}

	}


	void OnTriggerEnter()
	{
		LoadThisDamnScene(sceneName);
	}

	void OnTriggerExit()
	{
		UnloadThisDamnScene(sceneName);
	}

	void LoadThisDamnScene(string l){

		SceneManager.LoadScene(l, LoadSceneMode.Additive);
		sceneName = l;

	}

	void UnloadThisDamnScene(string l){

		SceneManager.UnloadScene(l);
	}

//	void SceneLoadCheck

	//SceneManager.LoadSceneMode


}
