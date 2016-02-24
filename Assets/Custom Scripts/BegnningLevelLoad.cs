using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class BegnningLevelLoad : MonoBehaviour {

//This script loads all the scenes at the beginning of the game that need to be loaded.
	public string[] sceneName;

	void Awake(){

		for (int i = 0; i < sceneName.Length; i++)
		{
			if (SceneManager.GetSceneByName(sceneName[i]).isLoaded == false)
			{
			SceneManager.LoadScene(sceneName[i], LoadSceneMode.Additive);
			}
		}
		}
}