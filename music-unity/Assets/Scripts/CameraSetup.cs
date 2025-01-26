using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    void Start()
    {
        // ensure the camera is positioned to view the waveform
        Camera.main.transform.position = new Vector3(0, 0, -10); // move the camera back
        Camera.main.orthographic = true; // set the camera to orthographic for better 2D views
        Camera.main.orthographicSize = 5; // adjust size for the 2D camera
    }
}
