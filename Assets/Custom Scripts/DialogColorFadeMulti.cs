﻿using UnityEngine;
using System.Collections;

public class DialogColorFadeMulti : MonoBehaviour
{

    //public GameObject objectToFade;  // object selection for fading (eh?)
    public Color objectColor;
    public float currentValue;
    public float target;
    //public Material mat;  // material selection for fading
    public TypogenicText[] text;
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
        for (int i = 0; i < text.Length; i++)
        { 
            text[i].ColorTopLeft.a = currentValue;
        }
        //text.ColorTopLeft.a = currentValue;

        updateAlpha();

    }

    void OnTriggerEnter()
    {

        // Debug.Log("step in the areana");
        target = 1;
    }

    void OnTriggerExit()
    {

        //  Debug.Log("leave the areana");
        target = 0;
    }



    void updateAlpha()
    {
        currentValue = Mathf.Lerp(currentValue, target, Time.deltaTime * Statics.gameTime);

    }

}
