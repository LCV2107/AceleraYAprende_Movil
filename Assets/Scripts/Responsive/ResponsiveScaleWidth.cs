using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class ResponsiveScaleWidth : MonoBehaviour
{
    private Vector3 escalaInicial;
    private float anchoSpriteConEscalaInicial;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // Guardamos escala inicial real del prefab
        escalaInicial = transform.localScale;

        // Calculamos cuánto mide el sprite en mundo con esa escala
        anchoSpriteConEscalaInicial =
            sr.sprite.bounds.size.x * escalaInicial.x;
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        // Ancho visible real del mundo
        float worldHeight = cam.orthographicSize * 2.05f;
        float worldWidth = worldHeight * cam.aspect;

        if (anchoSpriteConEscalaInicial == 0) return;

        // Factor necesario para ocupar todo el ancho
        float factor = worldWidth / anchoSpriteConEscalaInicial;

        // Escalamos SOLO en X desde la escala inicial
        transform.localScale = new Vector3(
            escalaInicial.x * factor,
            escalaInicial.y,
            escalaInicial.z
        );
    }
}
