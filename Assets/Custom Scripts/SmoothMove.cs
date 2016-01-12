using UnityEngine;
using System.Collections;

public class SmoothMove : MonoBehaviour {

    public float target;
    public float currentValue;
    public float time;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

        updateTarget();
	
	}

    void updateTarget()
    {
        currentValue = Mathf.Lerp(currentValue, target, Time.deltaTime * Statics.gameTime); //

    }
}
