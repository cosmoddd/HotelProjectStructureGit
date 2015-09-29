using UnityEngine;
using System.Collections;
using HutongGames.PlayMaker;

public class DottieScript : MonoBehaviour {

	public TextAsset textFile;
    public PlayMakerFSM thisFSM;
    public string[] dottieScript;
    public string dottieTest;
    public int dialogLocator;


	// Use this for initialization
	void Start () {

		{
			// Make sure there this a text
			// file assigned before continuing
			if(textFile != null)
			{
				// Add each line of the text file to
				// the array using the new line
				// as the delimiter
				dottieScript = ( textFile.text.Split( '\n' ) );
			}
		}
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
        dialogLocator = (i-1);
    }


    void CallOnDottie(string dottie)
    {
        thisFSM.FsmVariables.FindFsmString("CurrentDialog").Value = dottieScript[dialogLocator];
       // thisFSM.Fsm.Event()
    }
}
