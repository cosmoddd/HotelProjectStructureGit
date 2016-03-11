using UnityEngine;
using System.Collections;

public class HDRColorPulse : MonoBehaviour {

	[ColorUsageAttribute(true,true,0f,8f,0.125f,3f)]
	public Color color1;
	[ColorUsageAttribute(true,true,0f,8f,0.125f,3f)]
	public Color color2;

	[ColorUsageAttribute(true,true,0f,8f,0.125f,3f)]
	Color targetColor;
	[ColorUsageAttribute(true,true,0f,8f,0.125f,3f)]
	Color currentColor;

	public Material material;

	public float length; // amount of time between color transitions

	public float speed;  // the speed at which the colors transition

	void Awake(){

	//	StartCoroutine("ColorPulse");
	}

	void Start () {

		StartCoroutine("ColorPulse");


	}
	
	// Update is called once per frame
	void Update () {

		currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime* speed);

		material.SetColor("_EmissionColor", currentColor);

	}

	IEnumerator ColorPulse()
	{
		while (true)
		{
			targetColor = color1;
			yield return new WaitForSeconds(length);
			targetColor = color2;
			yield return new WaitForSeconds(length);
		}
	}

}
