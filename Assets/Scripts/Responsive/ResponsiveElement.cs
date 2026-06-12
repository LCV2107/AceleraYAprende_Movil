using UnityEngine;

public class ResponsiveElement : MonoBehaviour
{
    [Header("Configuración de Resolución Base")]
    // Pon aquí la resolución en la que diseñaste el elemento (ej. 1920x1080)
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);

    [Range(0f, 1f)]
    [Tooltip("0 = Escala por Ancho, 1 = Escala por Alto, 0.5 = Promedio")]
    public float matchWidthOrHeight = 0.5f;

    void OnEnable()
    {
        AdjustScaleToScreen();
    }

    public void AdjustScaleToScreen()
    {
        // 1. Calculamos la proporción de la pantalla actual respecto a tu diseño base
        float scaleX = Screen.width / referenceResolution.x;
        float scaleY = Screen.height / referenceResolution.y;

        // 2. Mezclamos ambas proporciones usando el valor de matchWidthOrHeight
        float finalScale = Mathf.Lerp(scaleX, scaleY, matchWidthOrHeight);

        // 3. Aplicamos la escala uniformemente para no perder la relación de aspecto
        transform.localScale = new Vector3(finalScale, finalScale, 1f);
    }
}