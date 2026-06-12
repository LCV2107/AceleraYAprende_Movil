using UnityEngine;

public class FrameRateManager : MonoBehaviour
{
    void Awake()
    {
        QualitySettings.vSyncCount = 0;     // Desactiva VSync
        Application.targetFrameRate = 60;   // Fija el límite en 60 FPS
    }
}
