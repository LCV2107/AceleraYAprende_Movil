using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
[ExecuteInEditMode]
public class DynamicCanvasScaler : MonoBehaviour
{
    CanvasScaler scaler;
    float lastAspectRatio; // Para no calcular a cada frame si no cambia

    void Start()
    {
        scaler = GetComponent<CanvasScaler>();
        ApplyScale();
    }

    void Update()
    {
        // Solo recalcular si la proporción de la pantalla cambió
        float currentAspect = (float)Screen.width / Screen.height;
        if (Mathf.Abs(currentAspect - lastAspectRatio) > 0.01f)
        {
            ApplyScale();
        }
    }

    void ApplyScale()
    {
        lastAspectRatio = (float)Screen.width / Screen.height;
        float refAspectRatio = 1920f / 1080f; // 1.77

        if (scaler == null) scaler = GetComponent<CanvasScaler>();

        if (lastAspectRatio > refAspectRatio) 
        {
            scaler.matchWidthOrHeight = 1; // Priorizar altura (Pantallas ultra anchas)
        }
        else 
        {
            // Para 4:3 o tablets, priorizar anchura asegura que los bordes no se corten
            scaler.matchWidthOrHeight = 0; 
        }
    }
}