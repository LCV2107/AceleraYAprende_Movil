using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonShakeEffect : MonoBehaviour
{
    [Header("Shake Settings")]
    public float shakeDuration = 0.2f;   // Duración total del temblor
    public float shakeStrength = 10f;    // Intensidad del temblor

    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;

        // Si el botón tiene componente Button, le añadimos listener
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(PlayShake);
        }
    }

    public void PlayShake()
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(Shake());
    }

    private IEnumerator Shake()
    {
        float t = 0;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;

            Vector2 offset = Random.insideUnitCircle * shakeStrength;

            rectTransform.anchoredPosition = originalPosition + new Vector3(offset.x, offset.y, 0);

            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
    }
}
