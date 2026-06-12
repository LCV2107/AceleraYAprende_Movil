using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CanvasControlManager : MonoBehaviour
{
    [Header("Canvas a controlar")]
    public Canvas canvas;

    [Header("Botón que inicia la simulación")]
    public Button botonIniciar;

    [Header("Toggles requeridos (al menos uno activo)")]
    public Toggle[] toggles;

    [Header("Sección (mínimo 1 botón)")]
    public Button[] botonesSeccion;

    [Header("Duración de la simulación (segundos)")]
    [Range(1f, 60f)]
    public float duracionSimulacion = 7f;  // 🔹 Valor por defecto 7s, editable en Inspector

    private Selectable[] elementosUI;

    private bool simulacionActiva = false;
    private bool seccionOK = false;

    void Start()
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();

        elementosUI = canvas.GetComponentsInChildren<Selectable>(true);

        if (botonIniciar != null)
        {
            botonIniciar.onClick.AddListener(IniciarSimulacion);
            botonIniciar.interactable = false; // deshabilitado al inicio
        }

        // Suscribir botones de sección
        foreach (Button b in botonesSeccion)
        {
            b.onClick.AddListener(() => {
                seccionOK = true;
                VerificarCondiciones();
            });
        }

        // Suscribir toggles
        foreach (Toggle t in toggles)
        {
            t.onValueChanged.AddListener(delegate { VerificarCondiciones(); });
        }
    }

    bool AlMenosUnToggleActivo()
    {
        foreach (Toggle t in toggles)
        {
            if (t.isOn) return true;
        }
        return false;
    }

    void VerificarCondiciones()
    {
        if (!simulacionActiva)
        {
            botonIniciar.interactable = seccionOK && AlMenosUnToggleActivo();
        }
    }

    void IniciarSimulacion()
    {
        if (simulacionActiva) return;
        simulacionActiva = true;

        // Desactivar todos los elementos UI
        foreach (Selectable elemento in elementosUI)
        {
            elemento.interactable = false;
        }

        // Iniciar temporizador
        StartCoroutine(TemporizadorSimulacion());
    }

    IEnumerator TemporizadorSimulacion()
    {
        yield return new WaitForSeconds(duracionSimulacion);
        DetenerSimulacion();
    }

    void DetenerSimulacion()
    {
        if (!simulacionActiva) return;
        simulacionActiva = false;

        // Reactivar elementos UI
        foreach (Selectable elemento in elementosUI)
        {
            elemento.interactable = true;
        }

        // Revalidar condiciones para habilitar "Iniciar"
        VerificarCondiciones();
    }
}
