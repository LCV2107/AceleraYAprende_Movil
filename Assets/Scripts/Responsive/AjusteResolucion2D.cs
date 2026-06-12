using UnityEngine;

public class AjusteResolucion2D : MonoBehaviour
{
    [Header("Posición deseada (0.0 a 1.0)")]
    [Range(0, 1)] public float puntoHorizontal = 0.5f; // 0.5 es el centro
    [Range(0, 1)] public float puntoVertical = 0.5f;   // 0.5 es el centro

    void Awake()
    {
        // 1. Obtenemos la referencia a la cámara principal
        Camera cam = Camera.main;

        if (cam != null)
        {
            // 2. Calculamos la posición en el mundo basada en el Viewport (la pantalla)
            // El Viewport ignora la resolución y usa porcentajes (0 a 1)
            Vector3 posicionViewport = new Vector3(puntoHorizontal, puntoVertical, cam.nearClipPlane);
            Vector3 posicionMundo = cam.ViewportToWorldPoint(posicionViewport);

            // 3. Aplicamos la posición manteniendo el Z original del prefab (para no perderlo)
            transform.position = new Vector3(posicionMundo.x, posicionMundo.y, transform.position.z);

            // 4. El script se destruye a sí mismo para no consumir recursos ni interferir más
            Destroy(this);
        }
    }
}