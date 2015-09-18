using UnityEngine;
using System.Collections;

public class dialogColorFade : MonoBehaviour
{

    //public GameObject objectToFade;  // object selection for fading (eh?)
    public Color objectColor;
    public float currentValue;
    public float target;
    //public Material mat;  // material selection for fading
    Renderer rend;

    // Use this for initialization
    void Start()
    {

        target = 0;

    }

    // Update is called once per frame
    void Update()
    {

        objectColor.a = currentValue;  // the key for lerping colors
        //  mat.color = objectColor;  // assigning the color to the variable in question
        
        updateAlpha();

    }


    void OnMouseOver()
    {
        Debug.Log("the mouse is hovering over this thing");
        target = 1;
    }

    void OnMouseExit()
    {
        Debug.Log("nothing");
        target = 0;
    }

    void updateAlpha()
    {
        currentValue = Mathf.Lerp(currentValue, target, Time.deltaTime * statics.gameTime
            );
    }
}
