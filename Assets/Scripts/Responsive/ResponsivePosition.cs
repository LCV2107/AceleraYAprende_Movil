using UnityEngine;

[ExecuteInEditMode]
public class ResponsivePosition : MonoBehaviour
{
    public bool isResponsive = true; // <--- EL INTERRUPTOR
    public Camera mainCamera;
    [Range(0, 1)] public float horizontalPos = 0.9f;
    [Range(0, 1)] public float verticalPos = 0.9f;
    public float distanceFromCamera = 10f;
    public Vector3 offset;

    void LateUpdate()
    {
        // Si el interruptor está apagado, no hacemos nada y dejamos que otros scripts muevan el objeto
        if (!isResponsive) return;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 viewportPoint = new Vector3(horizontalPos, verticalPos, distanceFromCamera);
        Vector3 worldPosition = mainCamera.ViewportToWorldPoint(viewportPoint);
        transform.position = worldPosition + offset;
    }
}