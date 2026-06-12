using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ScaleToScreen : MonoBehaviour
{
    void Start()
    {
        FitPerfectlyToScreen();
    }

    public void FitPerfectlyToScreen()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null) return;

        // 1. Reiniciamos escala para tomar medidas limpias
        transform.localScale = Vector3.one;

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        // 2. Opcional pero recomendado: Centrar el sprite exactamente en la cámara
        // Esto evita que el sprite se salga de la pantalla por estar mal posicionado
        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, transform.position.z);

        // 3. Calculamos tamaños
        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth = screenHeight * cam.aspect;

        float spriteWidth = sr.sprite.bounds.size.x;
        float spriteHeight = sr.sprite.bounds.size.y;

        // 4. Calculamos los ratios
        float ratioX = screenWidth / spriteWidth;
        float ratioY = screenHeight / spriteHeight;

        // 5. La clave de oro: Siempre tomar el ratio MÁS PEQUEÑO (Mathf.Min)
        // Esto garantiza que el lado que choca primero con los bordes de la pantalla detiene el escalado.
        float scaleFactor = Mathf.Min(ratioX, ratioY);

        // 6. Aplicamos la escala uniformemente
        transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
    }
}
