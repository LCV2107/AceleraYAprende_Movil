using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public enum TipoDeEspera
{
    Inmediato,
    EsperarCasa,
    EsperarPiso
}

[System.Serializable]
public class OpcionCompleta
{
    public Button botonActivador;
    public Sprite imagenNueva;
    [TextArea(2, 4)]
    public string textoNuevo;
}

[System.Serializable]
public class ElementoUI
{
    public string nombreDelElemento = "Elemento X";
    public TipoDeEspera cuandoCambiar;
    public Image imagenDestino;
    public TextMeshProUGUI textoDestino;

    [Header("Valores Iniciales (para Reset)")]
    public Sprite imagenInicial;
    [TextArea(2, 4)]
    public string textoInicial;

    public List<OpcionCompleta> opcionesDeEsteElemento;
}

public class UISwitcherManager : MonoBehaviour
{
    [Header("Conexión con tu Script Principal")]
    public GameManager gameManager;

    [Header("Configuración Global")]
    public List<ElementoUI> todosLosElementos;

    [Header("Reset")]
    [Tooltip("Asigna aquí el botón que reiniciará todas las imágenes y textos a sus valores iniciales.")]
    public Button botonReset;

    private List<Action> cambiosPendientesCasa = new List<Action>();
    private List<Action> cambiosPendientesPiso = new List<Action>();

    private bool casaYaEstable = false;
    private bool pisoYaEstable = false;

    // Controles para evitar que clics rápidos en frames posteriores sobreescriban la cola
    private int frameUltimoClicCasa = -1;
    private int frameUltimoClicPiso = -1;

    void Start()
    {
        if (gameManager == null)
        {
            Debug.LogError("ERROR CRÍTICO: No has asignado el GameManager en el UISwitcherManager.");
            return;
        }

        gameManager.OnCasaTerminada += EjecutarCambiosCasa;
        gameManager.OnPisoTerminado += EjecutarCambiosPiso;

        // ── GUARDAR ESTADO INICIAL ──────────────────────────────────────
        foreach (ElementoUI elemento in todosLosElementos)
        {
            if (elemento.imagenInicial == null && elemento.imagenDestino != null)
                elemento.imagenInicial = elemento.imagenDestino.sprite;

            if (string.IsNullOrEmpty(elemento.textoInicial) && elemento.textoDestino != null)
                elemento.textoInicial = elemento.textoDestino.text;
        }

        if (botonReset != null)
            botonReset.onClick.AddListener(ResetearTodo);

        // ── REGISTRO DE LISTENERS DE OPCIONES ──────────────────────────
        foreach (ElementoUI elemento in todosLosElementos)
        {
            Image imgTarget = elemento.imagenDestino;
            TextMeshProUGUI txtTarget = elemento.textoDestino;
            TipoDeEspera reglaDeEspera = elemento.cuandoCambiar;

            foreach (OpcionCompleta opcion in elemento.opcionesDeEsteElemento)
            {
                if (opcion.botonActivador == null) continue;

                Sprite spriteCapturado = opcion.imagenNueva;
                string textoCapturado = opcion.textoNuevo;
                TipoDeEspera reglaCapturada = reglaDeEspera;
                Image imgCapturada = imgTarget;
                TextMeshProUGUI txtCapturado = txtTarget;

                opcion.botonActivador.onClick.AddListener(() =>
                {
                    // Iniciamos una corrutina para asegurar que el GameManager valide el clic primero
                    StartCoroutine(ProcesarClicSeguro(reglaCapturada, imgCapturada, txtCapturado, spriteCapturado, textoCapturado));
                });
            }
        }
    }

    private IEnumerator ProcesarClicSeguro(TipoDeEspera regla, Image imgTarget, TextMeshProUGUI txtTarget, Sprite nuevoSprite, string nuevoTexto)
    {
        // Esperamos al final del frame. Esto asegura que si diste un clic inválido (ej. clic rápido),
        // el GameManager ya lo haya ignorado y sepamos el estado real de la simulación.
        yield return new WaitForEndOfFrame();

        bool animacionEnCurso = gameManager.botonBloqueado != null && !gameManager.botonBloqueado.interactable;

        switch (regla)
        {
            case TipoDeEspera.Inmediato:
                if (animacionEnCurso) yield break;
                AplicarCambios(imgTarget, txtTarget, nuevoSprite, nuevoTexto);
                break;

            case TipoDeEspera.EsperarCasa:
                if (gameManager.EnTransicionCasa)
                {
                    // PROTECCIÓN DE PRIORIDAD: Si ya encolamos algo en un clic anterior, ignoramos este nuevo clic rápido
                    if (cambiosPendientesCasa.Count > 0 && frameUltimoClicCasa != Time.frameCount)
                        yield break;

                    cambiosPendientesCasa.Add(() => AplicarCambios(imgTarget, txtTarget, nuevoSprite, nuevoTexto));
                    frameUltimoClicCasa = Time.frameCount;
                    casaYaEstable = false;
                }
                else if (!animacionEnCurso)
                {
                    AplicarCambios(imgTarget, txtTarget, nuevoSprite, nuevoTexto);
                }
                break;

            case TipoDeEspera.EsperarPiso:
                if (gameManager.IsReplacingPiso)
                {
                    // PROTECCIÓN DE PRIORIDAD: Si hay una transición y ya tenemos la imagen del primer clic guardada, 
                    // cualquier clic rápido en otro botón será bloqueado y descartado aquí.
                    if (cambiosPendientesPiso.Count > 0 && frameUltimoClicPiso != Time.frameCount)
                        yield break;

                    cambiosPendientesPiso.Add(() => AplicarCambios(imgTarget, txtTarget, nuevoSprite, nuevoTexto));
                    frameUltimoClicPiso = Time.frameCount;
                    pisoYaEstable = false;
                }
                else if (!animacionEnCurso)
                {
                    AplicarCambios(imgTarget, txtTarget, nuevoSprite, nuevoTexto);
                }
                break;
        }
    }

    public void ResetearTodo()
    {
        cambiosPendientesCasa.Clear();
        cambiosPendientesPiso.Clear();
        casaYaEstable = false;
        pisoYaEstable = false;

        foreach (ElementoUI elemento in todosLosElementos)
        {
            AplicarCambios(
                elemento.imagenDestino,
                elemento.textoDestino,
                elemento.imagenInicial,
                elemento.textoInicial
            );
        }
    }

    private void EjecutarCambiosCasa()
    {
        casaYaEstable = true;
        if (cambiosPendientesCasa.Count == 0) return;

        foreach (Action cambio in cambiosPendientesCasa)
            cambio?.Invoke();

        cambiosPendientesCasa.Clear();
    }

    private void EjecutarCambiosPiso()
    {
        pisoYaEstable = true;
        if (cambiosPendientesPiso.Count == 0) return;

        foreach (Action cambio in cambiosPendientesPiso)
            cambio?.Invoke();

        cambiosPendientesPiso.Clear();
    }

    private void AplicarCambios(Image imgTarget, TextMeshProUGUI txtTarget, Sprite nuevoSprite, string nuevoTexto)
    {
        if (imgTarget != null && nuevoSprite != null)
        {
            imgTarget.sprite = nuevoSprite;
            imgTarget.preserveAspect = true;
        }

        if (txtTarget != null)
            txtTarget.text = nuevoTexto;
    }

    void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnCasaTerminada -= EjecutarCambiosCasa;
            gameManager.OnPisoTerminado -= EjecutarCambiosPiso;
        }
    }
}