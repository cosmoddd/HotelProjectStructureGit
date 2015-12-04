using UnityEngine;
using System.Collections;
using HutongGames.PlayMaker;

public class DialogFeeder : MonoBehaviour {

	public TextAsset textFile;
    public PlayMakerFSM thisFSM;
    public string[] lines;
    //public string currentLine;
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
				lines = (textFile.text.Split('\n'));
                /*
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i].Trim();
                }
                */
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


    void GetCurrentLine()
    {
        thisFSM.FsmVariables.FindFsmString("CurrentDialog").Value = lines[dialogLocator];
       // thisFSM.Fsm.Event()
    }
}
