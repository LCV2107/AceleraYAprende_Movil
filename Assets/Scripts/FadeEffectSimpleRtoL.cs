using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeEffectSimpleRtoL : MonoBehaviour
{
    [Header("Panel Settings")]
    public RectTransform panel;         // Asigna el panel UMG aquí
    public float slideDuration = 1f;    // Tiempo de deslizamiento

    private Vector2 startPos;
    private Vector2 endPos;

    private void Start()
    {
        // ✅ Obtener el ancho del Canvas (no usar Screen.width)
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;

        // 📌 Posición inicial un poco dentro de la pantalla
        startPos = new Vector2(50f, panel.anchoredPosition.y);
        panel.anchoredPosition = startPos;

        // 📌 Posición final fuera completamente del canvas (a la izquierda)
        endPos = new Vector2(-canvasWidth - panel.rect.width, panel.anchoredPosition.y);

        // Inicia la animación
        StartCoroutine(SlideAndDestroy());
    }

    private IEnumerator SlideAndDestroy()
    {
        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / slideDuration);

            // Movimiento suave derecha → izquierda
            panel.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);

            yield return null;
        }

        // 🧹 Destruye el panel al finalizar
        Destroy(panel.gameObject);
    }
}