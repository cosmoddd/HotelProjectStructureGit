using UnityEngine;
using System.Collections;

public class MouseOver : MonoBehaviour {

    public bool mouseOver;
	// Use this for initialization
	void Start () {

        mouseOver = false;
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnMouseEnter()
    {
        mouseOver = true;

    }

    void OnMouseExit()
    {
        mouseOver = false;
    }
}
