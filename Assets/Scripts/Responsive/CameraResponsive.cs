using UnityEngine;

public class CameraResponsive : MonoBehaviour
{
    public float targetAspect = 16f / 9f;

    void Start()
    {
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        Camera cam = Camera.main;

        if (scaleHeight < 1.0f)
        {
            cam.orthographicSize /= scaleHeight;
        }
    }
}
