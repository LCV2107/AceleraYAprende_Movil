using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using System.Collections;

public class CameraShakeManager : MonoBehaviour
{
    [System.Serializable]
    public class ShakeConfig
    {
        [Range(0f, 10f)] public float amplitud = 0f;
        [Range(0f, 20f)] public float frecuencia = 0f;
    }

    [Header("Referencia a la Virtual Camera")]
    public CinemachineCamera virtualCamera;

    [Header("Toggles de Intensidad (elige uno)")]
    public Toggle toggleNivel1;
    public Toggle toggleNivel2;
    public Toggle toggleNivel3;
    public Toggle toggleNivel4;

    [Header("Botón de acción")]
    public Button botonIniciar;

    [Header("Configuración por Nivel")]
    public ShakeConfig nivel1 = new ShakeConfig();
    public ShakeConfig nivel2 = new ShakeConfig();
    public ShakeConfig nivel3 = new ShakeConfig();
    public ShakeConfig nivel4 = new ShakeConfig();

    [Header("Tiempo de transición")]
    [Range(0.1f, 5f)] public float duracionTransicion = 1f;

    [Header("Duración del shake (segundos)")]
    [Range(1f, 30f)] public float duracionShake = 7f; // 🔹 Editable en inspector

    private CinemachineBasicMultiChannelPerlin noise;
    private ShakeConfig nivelSeleccionado; 
    private Coroutine rutinaShake;

    void Start()
    {
        noise = virtualCamera.GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();

        toggleNivel1.onValueChanged.AddListener(delegate { OnToggleChanged(1, toggleNivel1.isOn); });
        toggleNivel2.onValueChanged.AddListener(delegate { OnToggleChanged(2, toggleNivel2.isOn); });
        toggleNivel3.onValueChanged.AddListener(delegate { OnToggleChanged(3, toggleNivel3.isOn); });
        toggleNivel4.onValueChanged.AddListener(delegate { OnToggleChanged(4, toggleNivel4.isOn); });

        botonIniciar.onClick.AddListener(IniciarShake);

        SetShake(0f, 0f);
    }

    private void OnToggleChanged(int nivel, bool activo)
    {
        if (activo)
        {
            DesactivarOtros(nivel);
            switch (nivel)
            {
                case 1: nivelSeleccionado = nivel1; break;
                case 2: nivelSeleccionado = nivel2; break;
                case 3: nivelSeleccionado = nivel3; break;
                case 4: nivelSeleccionado = nivel4; break;
            }
        }
        else if (!toggleNivel1.isOn && !toggleNivel2.isOn && !toggleNivel3.isOn && !toggleNivel4.isOn)
        {
            nivelSeleccionado = null;
        }
    }

    private void IniciarShake()
    {
        if (nivelSeleccionado != null)
        {
            if (rutinaShake != null) StopCoroutine(rutinaShake);
            rutinaShake = StartCoroutine(RutinaShake());
        }
    }

    private IEnumerator RutinaShake()
    {
        // 🔹 Transición de entrada
        yield return StartCoroutine(TransicionShake(
            noise.AmplitudeGain, nivelSeleccionado.amplitud,
            noise.FrequencyGain, nivelSeleccionado.frecuencia
        ));

        // 🔹 Mantener el shake por X segundos
        yield return new WaitForSeconds(duracionShake);

        // 🔹 Transición de salida
        yield return StartCoroutine(TransicionShake(
            noise.AmplitudeGain, 0f,
            noise.FrequencyGain, 0f
        ));
    }

    private IEnumerator TransicionShake(float desdeAmp, float hastaAmp, float desdeFreq, float hastaFreq)
    {
        float tiempo = 0f;

        while (tiempo < duracionTransicion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracionTransicion;

            noise.AmplitudeGain = Mathf.Lerp(desdeAmp, hastaAmp, t);
            noise.FrequencyGain = Mathf.Lerp(desdeFreq, hastaFreq, t);

            yield return null;
        }

        noise.AmplitudeGain = hastaAmp;
        noise.FrequencyGain = hastaFreq;
    }

    private void SetShake(float amplitud, float frecuencia)
    {
        if (noise != null)
        {
            noise.AmplitudeGain = amplitud;
            noise.FrequencyGain = frecuencia;
        }
    }

    private void DesactivarOtros(int activo)
    {
        if (activo != 1) toggleNivel1.isOn = false;
        if (activo != 2) toggleNivel2.isOn = false;
        if (activo != 3) toggleNivel3.isOn = false;
        if (activo != 4) toggleNivel4.isOn = false;
    }
}
