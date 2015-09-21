using UnityEngine;
using System.Collections;

public class TextRead : MonoBehaviour
{

    public TextAsset textFile;
    string[] dialogLines;

    // Use this for initialization
    void Start()
    {
        // Make sure there this a text
        // file assigned before continuing
        if (textFile != null)
        {
            // Add each line of the text file to
            // the array using the new line
            // as the delimiter
            dialogLines = (textFile.text.Split('\n'));
        }
    }
}