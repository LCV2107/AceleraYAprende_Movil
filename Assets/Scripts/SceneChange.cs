using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneChange : MonoBehaviour
{
    [Header("Fade Settings")]
    public RectTransform fadePanel;   // Panel negro
    public float fadeDuration = 1f;   // Tiempo del barrido
    public float cooldownTime = 1f;   // Tiempo antes de cargar la escena

    private Vector2 startPos;
    private Vector2 endPos;
    private bool isChangingScene = false;

    private void Start()
    {
        Canvas canvas = fadePanel.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        float canvasWidth = canvasRect.rect.width;

        startPos = new Vector2(canvasWidth, 0);
        endPos = Vector2.zero;

        fadePanel.anchoredPosition = startPos;
        fadePanel.gameObject.SetActive(false);
    }

    public void ChangeScene(string sceneName)
    {
        if (isChangingScene) return;

        isChangingScene = true;
        StartCoroutine(FadeAndChangeScene(sceneName));
    }

    private IEnumerator FadeAndChangeScene(string sceneName)
    {
        fadePanel.gameObject.SetActive(true);

        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / fadeDuration);
            fadePanel.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);
            yield return null;
        }

        yield return new WaitForSeconds(cooldownTime);

        SceneManager.LoadScene(sceneName);
    }
}
