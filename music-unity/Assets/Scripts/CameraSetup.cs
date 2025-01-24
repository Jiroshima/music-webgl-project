using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    void Start()
    {
        // Ensure the camera is positioned to view the waveform
        Camera.main.transform.position = new Vector3(0, 0, -10); // Move the camera back
        Camera.main.orthographic = true; // Set the camera to orthographic for better 2D views
        Camera.main.orthographicSize = 5; // Adjust size for the 2D camera
    }
}
