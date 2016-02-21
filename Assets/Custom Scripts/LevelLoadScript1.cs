using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LevelLoadScript1 : MonoBehaviour {

	public string sceneName;
	public GameObject placeHolderObject;  //object that's created when the level isn't loaded, usually a large blank surface

	void OnTriggerEnter(){

		if (SceneManager.GetSceneByName(sceneName).isLoaded == false){
			StartCoroutine("LoadSubScene");
			//de-activate placeholder object

		}
	}

	void OnTriggerExit(){

		if (SceneManager.GetSceneByName(sceneName).isLoaded == true){
			StartCoroutine("UnloadSubScene");
			//re-activate placeholder object
		}
		}


			IEnumerator LoadSubScene(){
				yield return new WaitForSeconds(.10f);
				SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
			}

			IEnumerator UnloadSubScene(){
				yield return new WaitForSeconds(.10f);
				SceneManager.UnloadScene(sceneName);
			}
			}
	