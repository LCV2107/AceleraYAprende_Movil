using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneChangeNoCoin : MonoBehaviour
{
    [Header("Settings")]
    public RectTransform fadePanel;   // Panel que se desliza
    public float slideDuration = 1f;  // Tiempo del movimiento

    private Vector2 startPos;
    private Vector2 endPos;

    private void Start()
    {
        // 📏 Obtener el RectTransform del canvas padre
        Canvas canvas = fadePanel.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        // 📐 Usar el ancho real del Canvas, no de la pantalla
        float canvasWidth = canvasRect.rect.width;

        // 📌 Inicia cubriendo toda la pantalla
        startPos = Vector2.zero;

        // 📌 Termina completamente fuera a la izquierda
        endPos = new Vector2(-canvasWidth, 0);

        fadePanel.anchoredPosition = startPos;

        // Inicia la animación automática
        StartCoroutine(SlideOutAndDestroy());
    }

    private IEnumerator SlideOutAndDestroy()
    {
        float t = 0;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / slideDuration);

            // Movimiento centro → izquierda
            fadePanel.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);

            yield return null;
        }

        // Cuando sale de pantalla, destruir
        Destroy(fadePanel.gameObject);
    }
}
