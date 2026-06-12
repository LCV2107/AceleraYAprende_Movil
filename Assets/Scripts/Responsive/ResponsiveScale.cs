using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRenderer))]
public class ResponsiveScale : MonoBehaviour
{
    public bool isResponsive = true; // <--- EL INTERRUPTOR
    [Range(0, 1)] public float widthPercent = 1f;
    private SpriteRenderer sr;
    private Vector3 originalScale;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
    }

    void LateUpdate()
    {
        // Si el interruptor está apagado, no forzamos la escala
        if (!isResponsive) return;

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        float alturaVisible = cam.orthographicSize * 2f;
        float anchoVisible = alturaVisible * cam.aspect;
        float spriteWidth = sr.sprite.bounds.size.x;
        float targetWidth = anchoVisible * widthPercent;
        float newScaleX = targetWidth / spriteWidth;

        transform.localScale = new Vector3(newScaleX, originalScale.y, originalScale.z);
    }
}