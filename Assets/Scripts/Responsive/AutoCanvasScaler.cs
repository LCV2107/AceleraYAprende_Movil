using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class AutoCanvasScaler : MonoBehaviour
{
    private CanvasScaler canvasScaler;

    [Header("Tu resolución base (Ej: 1920x1080)")]
    public Vector2 resolucionReferencia = new Vector2(1920, 1080);

    void Start()
    {
        canvasScaler = GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = resolucionReferencia;

        AjustarEscala();
    }

    // El Update solo lo usamos en el editor para que veas el cambio en tiempo real 
    // al arrastrar la ventana de Game. En la build final no es estrictamente necesario.
    void Update()
    {
#if UNITY_EDITOR
        AjustarEscala();
#endif
    }

    private void AjustarEscala()
    {
        if (canvasScaler == null) return;

        // Calculamos la proporción (Aspect Ratio) de tu diseño original vs la pantalla actual
        float proporcionReferencia = resolucionReferencia.x / resolucionReferencia.y;
        float proporcionActual = (float)Screen.width / (float)Screen.height;

        if (proporcionActual >= proporcionReferencia)
        {
            // Pantalla ancha (Celulares modernos 18:9, 21:9) -> Priorizamos la Altura
            canvasScaler.matchWidthOrHeight = 1f;
        }
        else
        {
            // Pantalla estrecha (Tablets, 4:3, 800x600) -> Priorizamos la Anchura para que no tape
            canvasScaler.matchWidthOrHeight = 0f;
        }
    }
}