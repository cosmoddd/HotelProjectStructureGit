using UnityEngine;
using System.Collections;

public class ObjectRotation : MonoBehaviour {

	public float speed = 1;
	public Camera camera;
	public float drag = 1;
	public float minDrag = 0;
	public float maxDrag = 8;
	float targetDrag;
	float time;
	public float minTime = 1;
	public float maxTime = 2;
	public Vector3 cameraAngle;

	Quaternion deltaRot;
	public bool over = false;

	
	// Update is called once per frame
	void Update () {

		cameraAngle = camera.transform.eulerAngles;

		deltaRot = Quaternion.Slerp (deltaRot, Quaternion.identity, Time.deltaTime * drag);
		transform.rotation = Quaternion.Slerp (transform.rotation, transform.rotation * deltaRot, Time.deltaTime * 5f);
		/*
		if (Input.GetMouseButton (0)) {
			var lastRot = transform.rotation;
			var angle_vector = new Vector3 (-Input.GetAxis ("Mouse Y"),
				                   Input.GetAxis ("Mouse X"), 0);
			transform.Rotate (angle_vector * speed *-1, Space.World);
			deltaRot = Quaternion.Inverse (lastRot) * transform.rotation;

		}
		*/
		//transform.rotation = Quaternion.Slerp (transform.rotation, transform.rotation * deltaRot, Time.deltaTime * 5f);

		drag = Mathf.Lerp(drag, targetDrag, Time.deltaTime*time);  //lerping the actual drag value

	}

	void OnMouseDrag() {

		targetDrag = minDrag;
		time = maxTime;

		var lastRot = transform.rotation;
		var angle_vector = new Vector3 (-Input.GetAxis ("Mouse Y"), Input.GetAxis ("Mouse X"), 0);
		transform.Rotate (angle_vector * speed *1, Space.World);
		deltaRot = Quaternion.Inverse (lastRot) * transform.rotation;


	}

	void OnMouseUp(){

		targetDrag = maxDrag;
		time = minTime;

	}
		
}


