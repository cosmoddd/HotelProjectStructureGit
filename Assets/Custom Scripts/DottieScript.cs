using UnityEngine;
using System.Collections;
using HutongGames.PlayMaker;

public class DottieScript : MonoBehaviour {

    public PlayMakerFSM thisFSM;
    public string[] dottieScript;
    public string dottieTest;
    public int dialogLocator;


	// Use this for initialization
	void Start () {

        dottieTest = "nothing";
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void TimeVacuum()
    {
        Debug.Log("hot pants in the evening sun");
    }

    void SetDialogLocator(int i)
    {
        dialogLocator = i;
    }


    void CallOnDottie(string dottie)
    {
        thisFSM.FsmVariables.FindFsmString("CurrentDialog").Value = dottieScript[dialogLocator];
       // thisFSM.Fsm.Event()
    }
}
