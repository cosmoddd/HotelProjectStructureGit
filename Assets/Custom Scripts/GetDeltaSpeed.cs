using UnityEngine;

public class   GetDeltaSpeed : MonoBehaviour
{
    // store how much the mouse has moved since the last frame
    public Vector3 mouseDelta = Vector3.zero;

    private Vector3 lastMousePosition = Vector3.zero;




    void Update()
    {
        mouseDelta = Input.mousePosition - lastMousePosition;

        lastMousePosition = Input.mousePosition;

        //Debug.Log(mouseDelta.ToString("F3"));
    }
}