using UnityEngine;

[ExecuteAlways]
public class AjustarAnchoResponsive : MonoBehaviour
{
    private Vector3 escalaOriginal;
    private SpriteRenderer sr;
    private Camera cam;

    private int lastWidth;
    private int lastHeight;

    void Awake()
    {
        escalaOriginal = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
        cam = Camera.main;
    }

    void Start()
    {
        Ajustar();
        lastWidth = Screen.width;
        lastHeight = Screen.height;
    }

    void Update()
    {
        // Solo recalcula si cambia resolución
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            Ajustar();
        }
    }

    void Ajustar()
    {
        if (cam == null || sr == null || !cam.orthographic)
            return;

        // Altura visible del mundo
        float alturaVisible = cam.orthographicSize * 2f;

        // Ancho visible del mundo
        float anchoVisible = alturaVisible * cam.aspect;

        // Ancho real del sprite SIN escala
        float anchoSpriteOriginal = sr.sprite.bounds.size.x;

        // Escala base X original
        float escalaBaseX = escalaOriginal.x;

        // Ancho actual considerando escala original
        float anchoActual = anchoSpriteOriginal * escalaBaseX;

        // Factor necesario
        float factorEscalaX = anchoVisible / anchoActual;

        // Aplicar solo en X respetando escala original
        transform.localScale = new Vector3(
            escalaBaseX * factorEscalaX,
            escalaOriginal.y,
            escalaOriginal.z
        );
    }
}
