using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneSwap : MonoBehaviour
{
    [Header("Fade Settings")]
    public RectTransform fadePanel;   // Panel negro (UI)
    public float fadeDuration = 1f;   // Tiempo del barrido
    public float cooldownTime = 1f;   // Tiempo antes de cargar la escena

    private Vector2 startPos;
    private Vector2 endPos;

    private void Start()
    {
        // ✅ Obtener el ancho del canvas en lugar de Screen.width
        Canvas canvas = fadePanel.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;

        // 📌 El panel inicia fuera a la derecha
        startPos = new Vector2(canvasWidth, 0);
        // 📌 Termina en el centro cubriendo la pantalla
        endPos = Vector2.zero;

        fadePanel.anchoredPosition = startPos;
        fadePanel.gameObject.SetActive(false);
    }

    // 🚀 Llamar este método desde el botón en el inspector
    public void ChangeScene(string sceneName)
    {
        StartCoroutine(FadeAndChange(sceneName));
    }

    private IEnumerator FadeAndChange(string sceneName)
    {
        // 1️⃣ Activar panel de fade
        fadePanel.gameObject.SetActive(true);

        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / fadeDuration);

            // Movimiento desde derecha → centro
            fadePanel.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);

            yield return null;
        }

        // 2️⃣ Espera opcional antes de cargar la escena
        yield return new WaitForSeconds(cooldownTime);

        // 3️⃣ Cargar nueva escena
        SceneManager.LoadScene(sceneName);
    }
}