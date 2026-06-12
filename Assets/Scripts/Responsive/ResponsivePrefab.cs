using UnityEngine;

[ExecuteInEditMode]
public class ResponsivePrefab : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Si está activo, el objeto se moverá automáticamente para mantener su posición relativa.")]
    public bool lockPosition = true;

    [Tooltip("La posición relativa a la pantalla (0,0 = Abajo-Izq, 1,1 = Arriba-Der)")]
    public Vector2 anchorPoint = new Vector2(0.5f, 0.5f);

    private Camera cam;
    private float zDistance;

    void OnEnable()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Calculamos la distancia Z del objeto a la cámara para mantener la profundidad
        zDistance = transform.position.z - cam.transform.position.z;

        if (lockPosition)
        {
            // MODO BLOQUEO: Forzamos la posición basada en el anchorPoint
            // Convertimos el % de pantalla (0 a 1) a coordenadas de mundo
            Vector3 targetPosition = cam.ViewportToWorldPoint(new Vector3(anchorPoint.x, anchorPoint.y, zDistance));

            // Aplicamos la posición manteniendo la Z original del objeto para evitar errores de renderizado
            transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        }
        else
        {
            // MODO EDICIÓN: Leemos dónde pones el objeto y actualizamos el porcentaje
            // Si mueves el objeto con el ratón, esto recalcula el "anchorPoint"
            Vector3 viewportPoint = cam.WorldToViewportPoint(transform.position);
            anchorPoint = new Vector2(viewportPoint.x, viewportPoint.y);
        }
    }
}
