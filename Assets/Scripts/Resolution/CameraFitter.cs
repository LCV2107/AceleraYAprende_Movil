using UnityEngine;

[ExecuteInEditMode]
public class CameraFitter : MonoBehaviour
{
    public float targetAspectRatio = 16f / 9f; // Tu resolución base (1920/1080)
    
    // Si es 2D (Orthographic)
    public float initialSize = 5f; // El tamaño 'ideal' que pusiste en Unity
    
    // Si es 3D (Perspective)
    public float initialFOV = 60f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam.orthographic) initialSize = cam.orthographicSize;
        else initialFOV = cam.fieldOfView;
        
        AdjustCamera();
    }

    void Update()
    {
        #if UNITY_EDITOR
        AdjustCamera();
        #endif
    }

    void AdjustCamera()
    {
        if (cam == null) cam = GetComponent<Camera>();

        float currentAspectRatio = (float)Screen.width / Screen.height;
        
        // Calculamos la diferencia entre la pantalla actual y la deseada
        float scaleHeight = currentAspectRatio / targetAspectRatio;

        if (cam.orthographic) // LOGICA PARA JUEGOS 2D
        {
            if (scaleHeight < 1.0f)
            {
                // La pantalla es más estrecha (ej. iPad 4:3)
                // Hacemos "Zoom Out" aumentando el size para mantener el ancho visible
                cam.orthographicSize = initialSize / scaleHeight;
            }
            else
            {
                // La pantalla es más ancha o igual (ej. móviles largos)
                // Mantenemos la altura original, ganamos visión a los lados
                cam.orthographicSize = initialSize;
            }
        }
        else // LOGICA PARA JUEGOS 3D
        {
            if (scaleHeight < 1.0f)
            {
                // Convertimos el FOV vertical para mantener el FOV horizontal fijo
                // (Matemática trigonométrica para adaptar la lente)
                float radAngle = initialFOV * Mathf.Deg2Rad;
                float radHFOV = 2 * Mathf.Atan(Mathf.Tan(radAngle / 2) * targetAspectRatio);
                float newFOV = 2 * Mathf.Atan(Mathf.Tan(radHFOV / 2) / currentAspectRatio) * Mathf.Rad2Deg;
                
                cam.fieldOfView = newFOV;
            }
            else
            {
                cam.fieldOfView = initialFOV;
            }
        }
    }
}