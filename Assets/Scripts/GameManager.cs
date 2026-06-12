using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using TMPro;
using System;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public class AnimacionTextoPiso
    {
        [Header("Texto Automático (Aparece al inicio)")]
        public TMP_Text textoPiso;
        [Header("Texto Interactivo (Aparece con el botón)")]
        public TMP_Text textoInteractivo;

        [Header("Configuración de Posiciones (Aplica a ambos)")]
        [Tooltip("La posición X es solo referencial para saber si viene de izquierda o derecha. Y es altura exacta.")]
        public Vector3 posInicio;
        public Vector3 posFin;

        [Header("Botón que activa el Texto Interactivo")]
        public Button botonActivarTexto;
        [Header("Botón que cierra todo")]
        public Button botonCerrarTexto;
    }

    [System.Serializable]
    public class SpritesPorCasa
    {
        public string nombreCasa;
        public Sprite[] spritesPiso0;
        public Sprite[] spritesPiso1;
        public Sprite[] spritesPiso2;
    }

    public event Action OnCasaTerminada;
    public event Action OnPisoTerminado;
    public bool IsReplacingPiso => isReplacingPiso;
    public bool EnTransicionCasa => enTransicionCasa || moverCasa || caerCasa;

    [Header("Canvas a controlar")]
    public Canvas canvas;
    [Header("Botón que inicia simulación + shake")]
    public Button botonSimular;
    [Header("Toggles requeridos (al menos uno activo)")]
    public Toggle[] toggles;
    [Header("Botones de sección (mínimo 1)")]
    public Button[] botonesSeccion;

    [Header("Duración de la simulación (segundos)")]
    [Range(1f, 60f)] public float duracionSimulacion = 7f;

    private Selectable[] elementosUI;
    private bool simulacionActiva = false;
    private bool seccionOK = false;

    [Header("Audio")]
    public AudioSource musicaFondo;
    public AudioSource audioSimulacion;
    [Header("Clips de Música por Casa (3 clips)")]
    public AudioClip[] clipsMusicaFondo;
    [Header("Volumen Objetivo por Casa (3 valores)")]
    [Range(0f, 1f)] public float[] volumenesMusicaFondo;
    [Header("Duración del Fundido de Audio")]
    [Range(0.1f, 5f)] public float duracionFundidoAudio = 1.0f;

    private float tiempoMusicaPausada = 0f;
    private Coroutine rutinaFundidoMusica;

    // PERSONAJES
    [System.Serializable]
    public class PersonajeConFases
    {
        public string nombre;
        public GameObject[] fases;
        public Vector3 posicionInicial;
    }
    [Header("Personajes y sus Fases")]
    public PersonajeConFases[] personajes;
    [Header("Botones Personajes")]
    public Button[] botonesPersonajes;

    private GameObject personajeActual;
    private int indicePersonajeActual = 0;
    private bool enTransicionPersonaje = false;

    private int indiceCasaActual = 0;
    private int indicePisoActual = 0;

    // CASAS
    [Header("Prefabs Casas (3 prefabs)")]
    public GameObject[] prefabsCasas;
    [Header("Botones Casas")]
    public Button[] botonesCasas;
    [Header("Posiciones destino Casas")]
    public Vector2[] posicionesCasas;

    [Header("Ajuste Dinámico en Pantalla")]
    [Tooltip("Posición de CASA: 0 (Borde Izq) - 0.5 (Centro) - 1 (Borde Der)")]
    [Range(0f, 1f)] public float posicionXEnPantalla = 0.5f;
    [Tooltip("Posición final del TEXTO: 0 (Borde Izq visible) - 0.5 (Centro) - 1 (Borde Der visible)")]
    [Range(0f, 1f)] public float posicionXTextoEnPantalla = 0.5f;

    [Header("Animación Casas")]
    public float velocidadSalidaCasa = 40f;
    public float alturaSpawnCasa = 10f;
    public float velocidadCaidaCasa = 12f;
    [Tooltip("Delay en segundos antes de mostrar el primer sprite de evolución en la simulación.")]
    public float delaySpriteCasa = 0.3f;

    private GameObject casaActual;
    private GameObject casaQueCae;
    private Vector2 destinoCasa;
    private Vector3 salidaCasa;
    private bool moverCasa = false;
    private bool caerCasa = false;
    private bool enTransicionCasa = false;

    // ─── OBJECT POOL DE CASAS ───────────────────────────────────────────────────
    private Dictionary<int, GameObject> poolCasas = new Dictionary<int, GameObject>();
    private int indiceCasaAnterior = 0;
    // ────────────────────────────────────────────────────────────────────────────

    [Header("Sprites de Evolución (por Casa y Piso)")]
    public SpritesPorCasa[] spritesDeEvolucion;

    [Header("Efecto Torre de Pisa")]
    public float offsetHundimiento = 1.5f;
    public float velocidadHundimiento = 2f;
    public float anguloInclinacion = 15f;
    public float velocidadInclinacion = 30f;
    private bool efectoPisa = false;
    private Vector3 destinoHundimiento;
    private Quaternion rotacionObjetivo;

    // PISOS
    [Header("Prefabs Pisos")]
    public GameObject[] prefabsPisos;
    [Header("Botones Pisos")]
    public Button[] botonesPisos;

    [Header("Textos informativos de cada piso")]
    public AnimacionTextoPiso[] animacionesTextosPisos;

    [Header("Animación Texto Piso")]
    [Range(0.1f, 2f)] public float duracionEntradaTexto = 0.4f;
    [Range(0f, 200f)] public float distanciaDerrapeTexto = 80f;
    [Range(0.1f, 1f)] public float duracionReboteTexto = 0.15f;
    [Range(0.1f, 1f)] public float duracionTemblorTexto = 0.2f;
    [Range(0f, 20f)] public float magnitudTemblorTexto = 5f;

    private Dictionary<TMP_Text, Vector3[]> cacheVerticesOriginales;
    private bool animandoTexto = false;

    //BGS
    [Header("Fondos por cada casa (en orden con prefabsCasas)")]
    public GameObject[] fondosCasas;
    private GameObject fondoActual;
    private GameObject fondoNuevo;
    private bool cambiandoFondo = false;

    [Header("Posición fija de los pisos")]
    public Vector3 posicionDestinoPiso = new Vector3(0f, -1f, 0f);

    [Header("Animación Pisos")]
    public float velocidadEntradaPiso = 10f;
    public float velocidadSalidaPiso = 10f;
    public float offsetEntradaXPiso = 20f;
    public float offsetSalidaXPiso = -20f;

    private GameObject pisoActual;
    private bool enTransicionPiso = false;
    private bool isReplacingPiso = false;

    [Header("Alturas extra por prefab (mismo orden que prefabsPisos)")]
    public float[] alturasExtraPisos;
    public float velocidadPiso = 5f;

    // SHAKE
    [System.Serializable]
    public class ShakeConfig
    {
        [Range(0f, 10f)] public float amplitud = 0f;
        [Range(0f, 20f)] public float frecuencia = 0f;
    }

    [System.Serializable]
    public class ShakeConfigPorPiso
    {
        public ShakeConfig nivel1 = new ShakeConfig();
        public ShakeConfig nivel2 = new ShakeConfig();
        public ShakeConfig nivel3 = new ShakeConfig();
        public ShakeConfig nivel4 = new ShakeConfig();
    }

    [Header("Virtual Camera")]
    public CinemachineCamera virtualCamera;

    // ── BOTONES DE NIVEL (reemplazan a los Toggles) ──────────────────────────
    [Header("Botones de Nivel de Sismo (reemplazan Toggles)")]
    public Button botonNivel1;
    public Button botonNivel2;
    public Button botonNivel3;
    public Button botonNivel4;

    [Header("Color del botón de nivel ACTIVO")]
    public Color colorNivelActivo = new Color(0.843f, 0.675f, 0f, 1f);
    [Header("Color del botón de nivel INACTIVO")]
    public Color colorNivelInactivo = Color.white;

    // Índice del nivel seleccionado: -1 = ninguno, 0-3 = nivel 1-4
    private int nivelSeleccionadoIndex = -1;
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Configuraciones Shake (por Piso)")]
    public ShakeConfigPorPiso[] configsShakePorPiso;
    [Header("Duraciones Shake")]
    [Range(0.1f, 5f)] public float duracionTransicion = 1f;
    [Range(1f, 30f)] public float duracionShake = 7f;

    private CinemachineBasicMultiChannelPerlin noise;
    private ShakeConfig nivelSeleccionado;
    private Coroutine rutinaShake;
    private Coroutine rutinaVibracion;
    private CanvasGroup cgBotonSimular;

    // CLICK INDEPENDIENTE
    private Dictionary<GameObject, PisoEstado> estadosPisos = new Dictionary<GameObject, PisoEstado>();
    private class PisoEstado
    {
        public Vector3 posicionInicial;
        public Vector3 posicionObjetivo;
        public bool desplegado;
        public float alturaExtra;
        public int indicePrefab;
    }

    //Reset button
    [Header("Botón de Reset")]
    public Button botonReset;

    // ICONOS / TEXTOS
    [System.Serializable]
    public class BotonTexto
    {
        public Button boton;
        public GameObject contenedor;
        [HideInInspector] public CanvasGroup cg;
    }

    [Header("Botones de texto por piso (0,1,2)")]
    public BotonTexto[] textosPiso0;
    public BotonTexto[] textosPiso1;
    public BotonTexto[] textosPiso2;
    [Header("Duración del fade")]
    public float duracionFade = 0.5f;

    private BotonTexto textoActivo = null;
    private Coroutine rutinaFade = null;

    [Header("Botón Bloqueado Durante Animaciones")]
    public Button botonBloqueado;

    // ═══════════════════════════════════════════════════════════════════════════
    // START
    // ═══════════════════════════════════════════════════════════════════════════
    void Start()
    {
        cacheVerticesOriginales = new Dictionary<TMP_Text, Vector3[]>();

        if (botonSimular != null)
        {
            botonSimular.onClick.AddListener(BotonSimularPresionado);
            botonSimular.interactable = false;
            cgBotonSimular = botonSimular.GetComponent<CanvasGroup>();
            if (cgBotonSimular == null)
                cgBotonSimular = botonSimular.gameObject.AddComponent<CanvasGroup>();
            cgBotonSimular.alpha = 0.6f;
        }

        int indiceCasaInicial = 0;
        if (prefabsCasas.Length > 1)
            indiceCasaInicial = 1;

        indiceCasaActual = indiceCasaInicial;
        indiceCasaAnterior = indiceCasaInicial;

        PrecargarCasas();

        if (fondosCasas.Length > indiceCasaInicial)
            fondoActual = Instantiate(fondosCasas[indiceCasaInicial], Vector3.zero, Quaternion.identity);
        else if (prefabsCasas.Length > 0)
            fondoActual = Instantiate(fondosCasas[0], Vector3.zero, Quaternion.identity);

        if (canvas == null)
            canvas = GetComponent<Canvas>();

        elementosUI = canvas.GetComponentsInChildren<Selectable>(true);

        if (botonesSeccion != null)
        {
            foreach (Button b in botonesSeccion)
            {
                if (b != null)
                    b.onClick.AddListener(() => { seccionOK = true; VerificarCondiciones(); });
            }
        }

        if (toggles != null)
        {
            foreach (Toggle t in toggles)
            {
                if (t != null) t.onValueChanged.AddListener(delegate { VerificarCondiciones(); });
            }
        }

        InicializarBotonesCasas();

        if (prefabsCasas.Length > indiceCasaInicial && posicionesCasas.Length > indiceCasaInicial)
        {
            Vector2 destino = ObtenerPosicionDinamica(indiceCasaInicial);
            Vector3 spawnPos = new Vector3(destino.x, destino.y + alturaSpawnCasa, 0);

            casaQueCae = ObtenerCasaDelPool(indiceCasaInicial);
            casaQueCae.transform.position = spawnPos;
            casaQueCae.transform.rotation = Quaternion.identity;

            destinoCasa = destino;
            caerCasa = true;
            enTransicionCasa = true;
        }

        InicializarBotonesPisos();
        if (prefabsPisos != null && prefabsPisos.Length > 0)
        {
            indicePisoActual = 0;
            pisoActual = Instantiate(prefabsPisos[0], posicionDestinoPiso, Quaternion.identity);
            RegistrarPiso(pisoActual, 0);
        }

        if (virtualCamera != null)
            noise = virtualCamera.GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();

        // ── Registrar listeners de los botones de nivel ──
        InicializarBotonesNivel();

        SetShake(0f, 0f);

        InicializarBotonesPersonajes();
        if (personajes != null && personajes.Length > 0 && personajes[0].fases != null && personajes[0].fases.Length > 0)
        {
            indicePersonajeActual = 0;
            personajeActual = Instantiate(personajes[0].fases[0], personajes[0].posicionInicial, Quaternion.identity);
        }

        if (botonReset != null)
            botonReset.onClick.AddListener(ResetearEscena);

        if (musicaFondo != null)
        {
            musicaFondo.loop = true;
            float initialVolume = 1.0f;
            int initialClipIndex = Mathf.Clamp(indiceCasaInicial, 0, volumenesMusicaFondo.Length - 1);

            if (volumenesMusicaFondo != null && volumenesMusicaFondo.Length > initialClipIndex)
                initialVolume = volumenesMusicaFondo[initialClipIndex];

            musicaFondo.volume = initialVolume;

            int initialVolumeIndex = Mathf.Clamp(indiceCasaInicial, 0, clipsMusicaFondo.Length - 1);
            if (clipsMusicaFondo != null && clipsMusicaFondo.Length > initialVolumeIndex)
                musicaFondo.clip = clipsMusicaFondo[initialVolumeIndex];

            if (musicaFondo.clip != null && !musicaFondo.isPlaying)
                musicaFondo.Play();
        }

        InicializarTextos(textosPiso0);
        InicializarTextos(textosPiso1);
        InicializarTextos(textosPiso2);

        if (animacionesTextosPisos != null)
        {
            for (int i = 0; i < animacionesTextosPisos.Length; i++)
            {
                var anim = animacionesTextosPisos[i];
                if (anim != null)
                {
                    if (anim.textoPiso != null) anim.textoPiso.gameObject.SetActive(false);
                    if (anim.textoInteractivo != null) anim.textoInteractivo.gameObject.SetActive(false);

                    if (anim.botonActivarTexto != null)
                    {
                        int indexCapturado = i;
                        anim.botonActivarTexto.onClick.AddListener(() => ActivarTextoInformativoManual(indexCapturado));
                    }
                    if (anim.botonCerrarTexto != null)
                        anim.botonCerrarTexto.onClick.AddListener(CerrarTextoInformativoManual);
                }
            }
        }

        ActualizarVisibilidadTextosPiso(-1);
        MostrarBotonesPorPiso(0);
        ActivarTextoAutomatico(0);

        DeshabilitarBotonesSeleccion(botonesCasas, indiceCasaActual);
        DeshabilitarBotonesSeleccion(botonesPisos, indicePisoActual);
        DeshabilitarBotonesSeleccion(botonesPersonajes, indicePersonajeActual);

        VerificarCondiciones();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BOTONES DE NIVEL  (lógica que antes usaba Toggles)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registra los listeners de los cuatro botones de nivel.
    /// </summary>
    void InicializarBotonesNivel()
    {
        if (botonNivel1 != null) botonNivel1.onClick.AddListener(() => OnBotonNivelPresionado(0));
        if (botonNivel2 != null) botonNivel2.onClick.AddListener(() => OnBotonNivelPresionado(1));
        if (botonNivel3 != null) botonNivel3.onClick.AddListener(() => OnBotonNivelPresionado(2));
        if (botonNivel4 != null) botonNivel4.onClick.AddListener(() => OnBotonNivelPresionado(3));

        ActualizarVisualesNivel();
    }

    /// <summary>
    /// Llamado al presionar cualquier botón de nivel.
    /// Si ya estaba activo lo deselecciona (comportamiento toggle); si no, lo selecciona.
    /// </summary>
    void OnBotonNivelPresionado(int indice)
    {
        if (simulacionActiva) return;

        if (nivelSeleccionadoIndex == indice)
            nivelSeleccionadoIndex = -1;   // deseleccionar
        else
            nivelSeleccionadoIndex = indice; // seleccionar nuevo

        ActualizarVisualesNivel();
        VerificarCondiciones();
    }

    /// <summary>
    /// Aplica el color activo/inactivo a cada botón de nivel según el estado actual.
    /// </summary>
    void ActualizarVisualesNivel()
    {
        AplicarColorNivel(botonNivel1, nivelSeleccionadoIndex == 0);
        AplicarColorNivel(botonNivel2, nivelSeleccionadoIndex == 1);
        AplicarColorNivel(botonNivel3, nivelSeleccionadoIndex == 2);
        AplicarColorNivel(botonNivel4, nivelSeleccionadoIndex == 3);
    }

    void AplicarColorNivel(Button boton, bool activo)
    {
        if (boton == null) return;

        boton.interactable = !activo;

        CanvasGroup cg = boton.GetComponent<CanvasGroup>();
        if (cg == null) cg = boton.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        ColorBlock cb = boton.colors;
        cb.normalColor = Color.white;
        cb.disabledColor = activo ? new Color(1f, 0.976f, 0.875f, 1f) : Color.white;
        cb.highlightedColor = ColorBlock.defaultColorBlock.highlightedColor;
        boton.colors = cb;

        Outline outline = boton.GetComponent<Outline>();
        if (outline != null)
            outline.effectColor = activo ? new Color(0.843f, 0.675f, 0f, 1f) : Color.black;
    }
    /// <summary>
    /// Desactiva visualmente todos los botones de nivel (para estado simulación).
    /// </summary>
    void DeshabilitarBotonesNivel()
    {
        Button[] botonesNivel = { botonNivel1, botonNivel2, botonNivel3, botonNivel4 };
        Color colorActivo = new Color(1f, 0.976f, 0.875f, 1f);
        Color colorInactivo = new Color(0.9f, 0.9f, 0.9f, 1f);

        for (int i = 0; i < botonesNivel.Length; i++)
        {
            Button b = botonesNivel[i];
            if (b == null) continue;

            bool esActivo = (i == nivelSeleccionadoIndex);
            b.interactable = false;

            CanvasGroup cg = b.GetComponent<CanvasGroup>();
            if (cg == null) cg = b.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            ColorBlock cb = b.colors;
            cb.disabledColor = esActivo ? colorActivo : colorInactivo;
            cb.normalColor = colorInactivo;
            b.colors = cb;
        }
    }

    /// <summary>
    /// Reactiva la interactividad de los botones de nivel al terminar la simulación.
    /// </summary>
    void RehabilitarBotonesNivel()
    {
        AplicarColorNivel(botonNivel1, nivelSeleccionadoIndex == 0);
        AplicarColorNivel(botonNivel2, nivelSeleccionadoIndex == 1);
        AplicarColorNivel(botonNivel3, nivelSeleccionadoIndex == 2);
        AplicarColorNivel(botonNivel4, nivelSeleccionadoIndex == 3);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OBJECT POOL DE CASAS
    // ═══════════════════════════════════════════════════════════════════════════

    void PrecargarCasas()
    {
        poolCasas.Clear();
        for (int i = 0; i < prefabsCasas.Length; i++)
        {
            if (prefabsCasas[i] == null) continue;

            GameObject casaPreCargada = Instantiate(
                prefabsCasas[i],
                new Vector3(9999f, 9999f, 0f),
                Quaternion.identity
            );
            casaPreCargada.name = prefabsCasas[i].name + "_Pool";
            casaPreCargada.SetActive(false);
            poolCasas[i] = casaPreCargada;
        }
    }

    GameObject ObtenerCasaDelPool(int indice)
    {
        if (poolCasas.TryGetValue(indice, out GameObject casa) && casa != null)
        {
            casa.transform.rotation = Quaternion.identity;
            casa.SetActive(true);
            return casa;
        }

        Debug.LogWarning($"[Pool] La casa con índice {indice} no estaba en el pool. Instanciando como fallback.");
        GameObject nuevaCasa = Instantiate(prefabsCasas[indice]);
        poolCasas[indice] = nuevaCasa;
        return nuevaCasa;
    }

    void DevolverCasaAlPool(GameObject casa, int indice)
    {
        if (casa == null) return;

        if (indice >= 0 && indice < prefabsCasas.Length)
        {
            SpriteRenderer prefabSr = prefabsCasas[indice].GetComponentInChildren<SpriteRenderer>();
            SpriteRenderer actualSr = casa.GetComponentInChildren<SpriteRenderer>();
            if (prefabSr != null && actualSr != null)
                actualSr.sprite = prefabSr.sprite;
        }

        casa.transform.rotation = Quaternion.identity;
        casa.transform.position = new Vector3(9999f, 9999f, 0f);
        casa.SetActive(false);

        poolCasas[indice] = casa;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEXTOS AUTOMÁTICOS / INTERACTIVOS
    // ═══════════════════════════════════════════════════════════════════════════

    void ActivarTextoAutomatico(int index)
    {
        if (index < 0 || index >= animacionesTextosPisos.Length) return;
        var anim = animacionesTextosPisos[index];

        if (anim != null && anim.textoPiso != null)
        {
            Vector3 posInicioReal = ObtenerPosicionTextoDinamica(anim.posInicio, anim.textoPiso, false);
            Vector3 posFinReal = ObtenerPosicionTextoDinamica(anim.posFin, anim.textoPiso, true);

            anim.textoPiso.gameObject.SetActive(true);
            StartCoroutine(AnimarTextoGenerico(anim.textoPiso, posInicioReal, posFinReal));
        }
    }

    void ActivarTextoInformativoManual(int index)
    {
        if (index != indicePisoActual) return;
        if (simulacionActiva) return;

        if (index >= 0 && index < animacionesTextosPisos.Length)
        {
            var anim = animacionesTextosPisos[index];
            if (anim.textoPiso != null)
                ResetearUnTexto(anim.textoPiso, ObtenerPosicionTextoDinamica(anim.posInicio, anim.textoPiso, false));

            if (anim.textoInteractivo != null)
            {
                Vector3 posInicioReal = ObtenerPosicionTextoDinamica(anim.posInicio, anim.textoInteractivo, false);
                Vector3 posFinReal = ObtenerPosicionTextoDinamica(anim.posFin, anim.textoInteractivo, true);

                anim.textoInteractivo.gameObject.SetActive(true);
                StartCoroutine(AnimarTextoGenerico(anim.textoInteractivo, posInicioReal, posFinReal));
            }
        }
    }

    void CerrarTextoInformativoManual()
    {
        if (indicePisoActual >= 0 && indicePisoActual < animacionesTextosPisos.Length)
        {
            var anim = animacionesTextosPisos[indicePisoActual];
            if (anim.textoInteractivo != null)
                ResetearUnTexto(anim.textoInteractivo, ObtenerPosicionTextoDinamica(anim.posInicio, anim.textoInteractivo, false));

            if (anim.textoPiso != null)
            {
                Vector3 posInicioReal = ObtenerPosicionTextoDinamica(anim.posInicio, anim.textoPiso, false);
                Vector3 posFinReal = ObtenerPosicionTextoDinamica(anim.posFin, anim.textoPiso, true);

                anim.textoPiso.gameObject.SetActive(true);
                StartCoroutine(AnimarTextoGenerico(anim.textoPiso, posInicioReal, posFinReal));
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UI — Estilos y selección
    // ═══════════════════════════════════════════════════════════════════════════

    void DeshabilitarBotonesSeleccion(Button[] botones, int indiceActivo)
    {
        if (botones == null) return;
        Color colorBordeSeleccionado = new Color(0.843f, 0.675f, 0f, 1f);

        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i] != null)
            {
                bool esActivo = (i == indiceActivo);
                botones[i].interactable = !esActivo;

                CanvasGroup cg = botones[i].GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;

                ColorBlock cb = botones[i].colors;
                cb.normalColor = Color.white;
                cb.disabledColor = new Color(1f, 0.976f, 0.875f, 1f);
                cb.highlightedColor = ColorBlock.defaultColorBlock.highlightedColor;
                botones[i].colors = cb;

                Outline outline = botones[i].GetComponent<Outline>();
                if (outline != null)
                    outline.effectColor = esActivo ? colorBordeSeleccionado : Color.black;
            }
        }
    }

    void AplicarEstiloSimulacion(Button[] botones, int indiceActivo)
    {
        if (botones == null) return;
        Color colorFondoActivo = new Color(1f, 0.976f, 0.875f, 1f);
        Color colorFondoInactivo = new Color(0.9f, 0.9f, 0.9f, 1f);
        Color colorOutlineActivo = new Color(0.843f, 0.675f, 0f, 1f);
        Color colorOutlineInactivo = Color.black;

        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i] != null)
            {
                bool esActivo = (i == indiceActivo);
                botones[i].interactable = false;

                CanvasGroup cg = botones[i].GetComponent<CanvasGroup>();
                if (cg == null) cg = botones[i].gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 1f;

                ColorBlock cb = botones[i].colors;
                cb.disabledColor = esActivo ? colorFondoActivo : colorFondoInactivo;
                cb.normalColor = colorFondoInactivo;
                botones[i].colors = cb;

                Outline outline = botones[i].GetComponent<Outline>();
                if (outline != null)
                    outline.effectColor = esActivo ? colorOutlineActivo : colorOutlineInactivo;
            }
        }
    }

    void InicializarTextos(BotonTexto[] grupo)
    {
        foreach (var bt in grupo)
        {
            if (bt.contenedor == null || bt.boton == null) continue;
            bt.cg = bt.contenedor.GetComponent<CanvasGroup>();
            if (bt.cg == null) bt.cg = bt.contenedor.AddComponent<CanvasGroup>();
            bt.cg.alpha = 0f;
            bt.contenedor.SetActive(false);

            BotonTexto captura = bt;
            bt.boton.onClick.AddListener(() => ToggleTexto(captura));
        }
    }

    void ActualizarVisibilidadTextosPiso(int indicePisoActivo)
    {
        if (animacionesTextosPisos == null) return;
        foreach (var anim in animacionesTextosPisos)
        {
            if (anim.textoPiso != null)
                ResetearUnTexto(anim.textoPiso, ObtenerPosicionTextoDinamica(anim.posInicio, anim.textoPiso, false));

            if (anim.textoInteractivo != null)
                ResetearUnTexto(anim.textoInteractivo, ObtenerPosicionTextoDinamica(anim.posInicio, anim.textoInteractivo, false));
        }
    }

    void ResetearUnTexto(TMP_Text t, Vector3 posInicio)
    {
        if (t != null)
        {
            t.gameObject.SetActive(false);
            t.rectTransform.anchoredPosition3D = posInicio;

            if (cacheVerticesOriginales.ContainsKey(t))
            {
                t.ForceMeshUpdate();
                Vector3[] verticesOriginales = cacheVerticesOriginales[t];
                Vector3[] targetVertices = t.textInfo.meshInfo[0].vertices;

                if (targetVertices.Length == verticesOriginales.Length)
                {
                    for (int k = 0; k < verticesOriginales.Length; k++)
                        targetVertices[k] = verticesOriginales[k];

                    t.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
                }
                cacheVerticesOriginales.Remove(t);
            }
        }
    }

    private IEnumerator AnimarTextoGenerico(TMP_Text texto, Vector3 posInicioLocal, Vector3 posFinLocal)
    {
        if (texto == null) yield break;
        animandoTexto = true;

        float direccionDerrape = (posInicioLocal.x < posFinLocal.x) ? distanciaDerrapeTexto : -distanciaDerrapeTexto;
        Vector3 posDerrapeLocal = new Vector3(posFinLocal.x + direccionDerrape, posFinLocal.y, posFinLocal.z);

        texto.rectTransform.anchoredPosition3D = posInicioLocal;

        float tiempo = 0f;
        while (tiempo < duracionEntradaTexto)
        {
            if (texto == null) { animandoTexto = false; yield break; }
            float t = tiempo / duracionEntradaTexto;
            t = 1 - Mathf.Pow(1 - t, 3);
            texto.rectTransform.anchoredPosition3D = Vector3.Lerp(posInicioLocal, posDerrapeLocal, t);
            tiempo += Time.deltaTime;
            yield return null;
        }

        if (texto == null) { animandoTexto = false; yield break; }
        texto.rectTransform.anchoredPosition3D = posDerrapeLocal;

        tiempo = 0f;
        while (tiempo < duracionReboteTexto)
        {
            if (texto == null) { animandoTexto = false; yield break; }
            float t = tiempo / duracionReboteTexto;
            texto.rectTransform.anchoredPosition3D = Vector3.Lerp(posDerrapeLocal, posFinLocal, t * t);
            tiempo += Time.deltaTime;
            yield return null;
        }

        if (texto == null) { animandoTexto = false; yield break; }
        texto.rectTransform.anchoredPosition3D = posFinLocal;

        texto.ForceMeshUpdate();
        TMP_TextInfo textInfo = texto.textInfo;

        if (textInfo.meshInfo.Length == 0 || textInfo.meshInfo[0].vertices == null)
        {
            animandoTexto = false;
            yield break;
        }

        int totalVertices = textInfo.meshInfo[0].vertices.Length;
        Vector3[] verticesOriginales = new Vector3[totalVertices];
        Vector3[] currentVertices = textInfo.meshInfo[0].vertices;

        for (int k = 0; k < totalVertices; k++)
            verticesOriginales[k] = currentVertices[k];

        if (!cacheVerticesOriginales.ContainsKey(texto))
            cacheVerticesOriginales[texto] = verticesOriginales;

        Vector3[] verticesActuales = textInfo.meshInfo[0].vertices;
        tiempo = 0f;

        while (tiempo < duracionTemblorTexto)
        {
            if (texto == null || !texto.gameObject.activeInHierarchy)
            {
                animandoTexto = false;
                yield break;
            }

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int vertexIndex = charInfo.vertexIndex;
                Vector3 offset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) * magnitudTemblorTexto;

                verticesActuales[vertexIndex + 0] = verticesOriginales[vertexIndex + 0] + offset;
                verticesActuales[vertexIndex + 1] = verticesOriginales[vertexIndex + 1] + offset;
                verticesActuales[vertexIndex + 2] = verticesOriginales[vertexIndex + 2] + offset;
                verticesActuales[vertexIndex + 3] = verticesOriginales[vertexIndex + 3] + offset;
            }

            texto.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            tiempo += Time.deltaTime;
            yield return null;
        }

        if (texto != null)
        {
            texto.rectTransform.anchoredPosition3D = posFinLocal;

            if (cacheVerticesOriginales.ContainsKey(texto))
            {
                Vector3[] cachedOriginals = cacheVerticesOriginales[texto];
                for (int k = 0; k < cachedOriginals.Length; k++)
                    verticesActuales[k] = cachedOriginals[k];

                texto.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
                cacheVerticesOriginales.Remove(texto);
            }
        }

        animandoTexto = false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ICONOS / FADE BOTONES
    // ═══════════════════════════════════════════════════════════════════════════

    private void FadeGroup(BotonTexto[] grupo, bool show)
    {
        float targetAlpha = show ? 1f : 0f;
        if (grupo == null) return;
        foreach (var bt in grupo)
        {
            if (bt != null && bt.boton != null)
            {
                CanvasGroup cg = bt.boton.GetComponent<CanvasGroup>();
                float alphaInicial = (cg != null) ? cg.alpha : (bt.boton.gameObject.activeSelf ? 1f : 0f);
                StartCoroutine(FadeBoton(bt.boton, alphaInicial, targetAlpha, 0.5f));
            }
        }
    }

    void MostrarBotonesPorPiso(int index)
    {
        FadeGroup(textosPiso0, index == 0);
        FadeGroup(textosPiso1, index == 1);
        FadeGroup(textosPiso2, index == 2);
        CerrarTextoActivo();
    }

    private void ActualizarEstadoBotonBloqueado()
    {
        bool animacionEnCurso = enTransicionCasa || enTransicionPiso || enTransicionPersonaje
                                || moverCasa || caerCasa || isReplacingPiso || rutinaFundidoMusica != null;
        if (simulacionActiva) animacionEnCurso = true;

        if (botonBloqueado != null)
        {
            botonBloqueado.interactable = !animacionEnCurso;
            CanvasGroup cg = botonBloqueado.GetComponent<CanvasGroup>();
            if (cg == null) cg = botonBloqueado.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = botonBloqueado.interactable ? 1f : 0.5f;
            cg.blocksRaycasts = botonBloqueado.interactable;
        }
    }

    IEnumerator FadeBoton(Button boton, float inicio, float fin, float duracion)
    {
        if (boton == null) yield break;
        CanvasGroup cg = boton.GetComponent<CanvasGroup>();
        if (cg == null) cg = boton.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = inicio;
        boton.gameObject.SetActive(true);

        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(inicio, fin, t / duracion);
            yield return null;
        }
        cg.alpha = fin;
        if (fin <= 0f) boton.gameObject.SetActive(false);
    }

    void ToggleTexto(BotonTexto nuevo)
    {
        if (textoActivo == nuevo)
        {
            CerrarTextoActivo();
            return;
        }
        if (textoActivo != null)
        {
            if (rutinaFade != null) StopCoroutine(rutinaFade);
            textoActivo.contenedor.SetActive(false);
        }

        textoActivo = nuevo;
        textoActivo.contenedor.SetActive(true);
        if (rutinaFade != null) StopCoroutine(rutinaFade);
        rutinaFade = StartCoroutine(Fade(textoActivo, 0f, 1f, duracionFade));
    }

    void CerrarTextoActivo()
    {
        if (textoActivo == null) return;
        if (rutinaFade != null) StopCoroutine(rutinaFade);
        rutinaFade = StartCoroutine(Fade(textoActivo, 1f, 0f, duracionFade, () =>
        {
            textoActivo.contenedor.SetActive(false);
            textoActivo = null;
            rutinaFade = null;
        }));
    }

    void DetectarClickFuera()
    {
        if (textoActivo == null) return;
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                    textoActivo.contenedor.GetComponent<RectTransform>(),
                    mousePos,
                    null))
            {
                CerrarTextoActivo();
            }
        }
    }

    IEnumerator Fade(BotonTexto bt, float inicio, float fin, float tiempo, System.Action onFinish = null)
    {
        float t = 0f;
        while (t < tiempo)
        {
            t += Time.deltaTime;
            bt.cg.alpha = Mathf.Lerp(inicio, fin, t / tiempo);
            yield return null;
        }
        bt.cg.alpha = fin;
        onFinish?.Invoke();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AUDIO
    // ═══════════════════════════════════════════════════════════════════════════

    private IEnumerator FadeMusic(AudioSource source, float targetVolume, float duration, AudioClip newClip = null, float startTime = 0f)
    {
        float startVolume = source.volume;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0, time / duration);
            yield return null;
        }

        source.volume = 0;
        if (newClip != null)
        {
            source.Stop();
            source.clip = newClip;
            source.time = startTime;
            source.Play();
        }

        time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            source.volume = Mathf.Lerp(0, targetVolume, time / duration);
            yield return null;
        }
        source.volume = targetVolume;
        rutinaFundidoMusica = null;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PERSONAJES
    // ═══════════════════════════════════════════════════════════════════════════

    void InicializarBotonesPersonajes()
    {
        if (botonesPersonajes != null && botonesPersonajes.Length > 0)
        {
            for (int i = 0; i < botonesPersonajes.Length; i++)
            {
                int index = i;
                if (botonesPersonajes[i] != null)
                    botonesPersonajes[i].onClick.AddListener(() => ReemplazarPersonaje(index));
            }
        }
    }

    public void ReemplazarPersonaje(int indice)
    {
        if (simulacionActiva || enTransicionPersonaje) return;
        if (indice < 0 || personajes == null || indice >= personajes.Length
            || personajes[indice].fases == null || personajes[indice].fases.Length == 0)
        {
            Debug.LogWarning("Índice de personaje no válido o no tiene fases asignadas.");
            return;
        }
        indicePersonajeActual = indice;
        ResetearEstadoSimulacion();
        DeshabilitarBotonesSeleccion(botonesPersonajes, indice);
    }

    private void CambiarPrefabPersonaje(int indiceFase)
    {
        if (personajes == null || indicePersonajeActual >= personajes.Length) return;
        GameObject[] fases = personajes[indicePersonajeActual].fases;
        if (fases == null || indiceFase < 0 || indiceFase >= fases.Length || fases[indiceFase] == null)
        {
            Debug.LogWarning($"El personaje no tiene un prefab para la fase {indiceFase}.");
            return;
        }

        if (personajeActual != null) Destroy(personajeActual);

        Vector3 posicionActualPersonaje = personajes[indicePersonajeActual].posicionInicial;
        personajeActual = Instantiate(fases[indiceFase], posicionActualPersonaje, Quaternion.identity);
    }

    private IEnumerator EvolucionarPersonaje(int objetivoIndex, float duracion)
    {
        if (personajes[indicePersonajeActual].fases.Length < objetivoIndex + 1)
        {
            Debug.LogWarning("No hay suficientes prefabs de fase para el nivel seleccionado.");
            yield break;
        }

        switch (objetivoIndex)
        {
            case 0:
            case 1:
                CambiarPrefabPersonaje(1);
                yield return new WaitForSeconds(duracion);
                break;
            case 2:
                float mitadDuracion = duracion / 2f;
                CambiarPrefabPersonaje(1);
                yield return new WaitForSeconds(mitadDuracion);
                CambiarPrefabPersonaje(2);
                yield return new WaitForSeconds(mitadDuracion);
                break;
            case 3:
                float tercioDuracion = duracion / 3f;
                CambiarPrefabPersonaje(1);
                yield return new WaitForSeconds(tercioDuracion);
                CambiarPrefabPersonaje(2);
                yield return new WaitForSeconds(tercioDuracion);
                CambiarPrefabPersonaje(3);
                yield return new WaitForSeconds(tercioDuracion);
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RESET
    // ═══════════════════════════════════════════════════════════════════════════

    public void ResetearEscena()
    {
        if (enTransicionCasa || enTransicionPiso || enTransicionPersonaje || simulacionActiva)
        {
            Debug.Log("No se puede resetear durante una transición o simulación activa.");
            return;
        }

        if (rutinaFundidoMusica != null)
        {
            StopCoroutine(rutinaFundidoMusica);
            rutinaFundidoMusica = null;
        }

        StopAllCoroutines();
        rutinaVibracion = null;
        animandoTexto = false;

        cacheVerticesOriginales.Clear();
        simulacionActiva = false;
        efectoPisa = false;

        if (audioSimulacion != null) audioSimulacion.Stop();

        if (musicaFondo != null && !musicaFondo.isPlaying)
        {
            float resetVolume = 1.0f;
            int currentClipIndex = Mathf.Clamp(indiceCasaActual, 0, volumenesMusicaFondo.Length - 1);
            if (volumenesMusicaFondo != null && volumenesMusicaFondo.Length > currentClipIndex)
                resetVolume = volumenesMusicaFondo[currentClipIndex];

            musicaFondo.volume = resetVolume;
            musicaFondo.Play();
        }

        // ── Restaurar casa actual a su posición y sprite original SIN cambiar de prefab ──
        if (casaActual != null)
        {
            efectoPisa = false;
            casaActual.transform.rotation = Quaternion.identity;

            Vector2 posDestino = ObtenerPosicionDinamica(indiceCasaActual);
            casaActual.transform.position = new Vector3(posDestino.x, posDestino.y, 0f);

            // Restaurar sprite original del prefab actual
            if (indiceCasaActual >= 0 && indiceCasaActual < prefabsCasas.Length)
            {
                SpriteRenderer prefabSr = prefabsCasas[indiceCasaActual].GetComponentInChildren<SpriteRenderer>();
                SpriteRenderer actualSr = casaActual.GetComponentInChildren<SpriteRenderer>();
                if (prefabSr != null && actualSr != null)
                    actualSr.sprite = prefabSr.sprite;
            }
        }
        indiceCasaAnterior = indiceCasaActual;

        // ── Restaurar personaje actual a fase 0 SIN cambiar de personaje ──
        if (personajes != null && indicePersonajeActual >= 0 && indicePersonajeActual < personajes.Length &&
            personajes[indicePersonajeActual].fases != null && personajes[indicePersonajeActual].fases.Length > 0)
        {
            if (personajeActual != null) Destroy(personajeActual);
            personajeActual = Instantiate(
                personajes[indicePersonajeActual].fases[0],
                personajes[indicePersonajeActual].posicionInicial,
                Quaternion.identity
            );
        }

        // ── Restaurar piso a su posición inicial SIN cambiar de piso ──
        if (pisoActual != null && estadosPisos.ContainsKey(pisoActual))
        {
            PisoEstado estado = estadosPisos[pisoActual];
            pisoActual.transform.position = estado.posicionInicial;
            estado.desplegado = false;
        }

        ActualizarVisibilidadTextosPiso(indicePisoActual);
        ActivarTextoAutomatico(indicePisoActual);
        CerrarTextoActivo();

        enTransicionCasa = false;
        enTransicionPiso = false;
        enTransicionPersonaje = false;
        moverCasa = false;
        caerCasa = false;
        isReplacingPiso = false;

        DeshabilitarBotonesSeleccion(botonesCasas, indiceCasaActual);
        DeshabilitarBotonesSeleccion(botonesPisos, indicePisoActual);
        DeshabilitarBotonesSeleccion(botonesPersonajes, indicePersonajeActual);

        nivelSeleccionadoIndex = -1;
        RehabilitarBotonesNivel();

        SetShake(0f, 0f);

        foreach (Selectable elemento in elementosUI)
        {
            if (elemento != null) elemento.interactable = true;
        }

        VerificarCondiciones();
        ActualizarEstadoBotonBloqueado();
    }

    void ResetearEstadoSimulacion()
    {
        if (casaActual != null)
        {
            efectoPisa = false;
            casaActual.transform.rotation = Quaternion.identity;
            if (indiceCasaActual >= 0 && indiceCasaActual < posicionesCasas.Length)
            {
                Vector2 posDestinoOriginal = ObtenerPosicionDinamica(indiceCasaActual);
                casaActual.transform.position = new Vector3(
                    posDestinoOriginal.x,
                    posDestinoOriginal.y,
                    casaActual.transform.position.z
                );
            }

            if (indiceCasaActual >= 0 && indiceCasaActual < prefabsCasas.Length)
            {
                SpriteRenderer prefabSr = prefabsCasas[indiceCasaActual].GetComponentInChildren<SpriteRenderer>();
                SpriteRenderer actualSr = casaActual.GetComponentInChildren<SpriteRenderer>();
                if (prefabSr != null && actualSr != null)
                    actualSr.sprite = prefabSr.sprite;
            }
        }

        if (personajes != null && indicePersonajeActual >= 0 && indicePersonajeActual < personajes.Length &&
            personajes[indicePersonajeActual].fases != null && personajes[indicePersonajeActual].fases.Length > 0)
        {
            CambiarPrefabPersonaje(0);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VERIFICAR CONDICIONES / SIMULACIÓN
    // ═══════════════════════════════════════════════════════════════════════════

    bool AlMenosUnNivelActivo()
    {
        return nivelSeleccionadoIndex >= 0;
    }

    void VerificarCondiciones()
    {
        if (simulacionActiva)
        {
            if (botonSimular != null) botonSimular.interactable = false;
            return;
        }

        bool personajeOK = (personajeActual != null);
        bool pisoOK = (pisoActual != null);
        bool casaOK = (casaActual != null && !enTransicionCasa);
        bool nivelOK = AlMenosUnNivelActivo();

        bool puede = personajeOK && pisoOK && casaOK && nivelOK
            && !enTransicionCasa && !enTransicionPiso && !enTransicionPersonaje
            && !isReplacingPiso && rutinaFundidoMusica == null;

        if (botonSimular != null)
        {
            botonSimular.interactable = puede;

            Image img = botonSimular.GetComponent<Image>();
            if (img != null)
                img.color = puede ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);

            CanvasGroup cg = cgBotonSimular;
            if (cg != null)
            {
                cg.alpha = puede ? 1f : 0.5f;
                cg.blocksRaycasts = puede;
                cg.interactable = puede;
            }
        }
    }

    void BotonSimularPresionado()
    {
        StartCoroutine(RutinaIniciarSimulacion());
    }

    IEnumerator RutinaIniciarSimulacion()
    {
        if (simulacionActiva || enTransicionCasa || enTransicionPiso || enTransicionPersonaje
            || isReplacingPiso || rutinaFundidoMusica != null)
        {
            yield break;
        }

        ResetearEstadoSimulacion();
        simulacionActiva = true;

        foreach (Selectable elemento in elementosUI)
        {
            if (elemento != null) elemento.interactable = false;
        }

        // Deshabilita los botones de nivel durante la simulación
        DeshabilitarBotonesNivel();

        if (botonBloqueado != null)
        {
            botonBloqueado.interactable = false;
            CanvasGroup cg = botonBloqueado.GetComponent<CanvasGroup>();
            if (cg == null) cg = botonBloqueado.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0.5f;
            cg.blocksRaycasts = false;
        }

        if (animacionesTextosPisos != null)
        {
            foreach (var anim in animacionesTextosPisos)
            {
                if (anim.textoPiso != null) anim.textoPiso.gameObject.SetActive(false);
                if (anim.textoInteractivo != null) anim.textoInteractivo.gameObject.SetActive(false);
            }
        }
        MostrarBotonesPorPiso(-1);
        CerrarTextoActivo();

        AplicarEstiloSimulacion(botonesCasas, indiceCasaActual);
        AplicarEstiloSimulacion(botonesPisos, indicePisoActual);
        AplicarEstiloSimulacion(botonesPersonajes, indicePersonajeActual);

        yield return new WaitForSeconds(0.5f);

        IniciarShake();

        if (musicaFondo != null && musicaFondo.isPlaying)
        {
            tiempoMusicaPausada = musicaFondo.time;
            musicaFondo.Pause();
        }

        if (audioSimulacion != null)
        {
            audioSimulacion.time = 0f;
            audioSimulacion.Play();
        }

        StartCoroutine(TemporizadorSimulacion());

        int objetivoIndex = nivelSeleccionadoIndex;   // usa directamente el índice guardado
        if (objetivoIndex >= 0)
        {
            if (casaActual != null)
            {
                int iCasa = indiceCasaActual;
                int iPiso = indicePisoActual;

                if (spritesDeEvolucion != null &&
                    iCasa >= 0 && iCasa < spritesDeEvolucion.Length &&
                    iPiso >= 0 && iPiso <= 2)
                {
                    SpritesPorCasa configCasa = spritesDeEvolucion[iCasa];
                    Sprite[] spritesAUsar = null;

                    switch (iPiso)
                    {
                        case 0: spritesAUsar = configCasa.spritesPiso0; break;
                        case 1: spritesAUsar = configCasa.spritesPiso1; break;
                        case 2: spritesAUsar = configCasa.spritesPiso2; break;
                    }

                    if (spritesAUsar != null && spritesAUsar.Length > 0)
                        StartCoroutine(EvolucionarCasa(casaActual, spritesAUsar, objetivoIndex, duracionSimulacion));
                    else
                        Debug.LogWarning($"No se encontraron sprites para Casa {iCasa}, Piso {iPiso}");
                }
                else
                {
                    Debug.LogWarning("Configuración 'spritesDeEvolucion' no válida para los índices actuales.");
                }
            }
            StartCoroutine(EvolucionarPersonaje(objetivoIndex, duracionSimulacion));
        }
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

        if (rutinaShake != null) { StopCoroutine(rutinaShake); rutinaShake = null; }
        if (rutinaVibracion != null) { StopCoroutine(rutinaVibracion); rutinaVibracion = null; }

        SetShake(0f, 0f);

        foreach (Selectable elemento in elementosUI)
        {
            if (elemento != null) elemento.interactable = true;
        }

        // Rehabilita y restaura el visual de los botones de nivel
        RehabilitarBotonesNivel();

        DeshabilitarBotonesSeleccion(botonesCasas, indiceCasaActual);
        DeshabilitarBotonesSeleccion(botonesPisos, indicePisoActual);
        DeshabilitarBotonesSeleccion(botonesPersonajes, indicePersonajeActual);

        if (botonBloqueado != null)
        {
            botonBloqueado.interactable = true;
            CanvasGroup cg = botonBloqueado.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; }
        }

        int indicePisoActualLocal = -1;
        if (pisoActual != null)
        {
            for (int i = 0; i < prefabsPisos.Length; i++)
            {
                if (pisoActual.name.Contains(prefabsPisos[i].name))
                {
                    indicePisoActualLocal = i;
                    break;
                }
            }
        }

        if (indicePisoActualLocal != -1)
        {
            ActualizarVisibilidadTextosPiso(indicePisoActualLocal);
            MostrarBotonesPorPiso(indicePisoActualLocal);
            ActivarTextoAutomatico(indicePisoActualLocal);
        }

        if (audioSimulacion != null) audioSimulacion.Stop();

        if (musicaFondo != null)
        {
            int indiceCasaLocal = -1;
            for (int i = 0; i < prefabsCasas.Length; i++)
            {
                if (casaActual != null && casaActual.name.Contains(prefabsCasas[i].name))
                {
                    indiceCasaLocal = i;
                    break;
                }
            }

            float currentVolume = 1.0f;
            if (indiceCasaLocal >= 0 && volumenesMusicaFondo != null && indiceCasaLocal < volumenesMusicaFondo.Length)
                currentVolume = volumenesMusicaFondo[indiceCasaLocal];

            musicaFondo.volume = currentVolume;
            musicaFondo.time = tiempoMusicaPausada;
            musicaFondo.Play();
        }

        int indiceCasa = -1;
        if (casaActual != null)
        {
            for (int i = 0; i < prefabsCasas.Length; i++)
            {
                if (casaActual.name.Contains(prefabsCasas[i].name)) { indiceCasa = i; break; }
            }
        }

        if ((indiceCasa == 1 || indiceCasa == 2) && nivelSeleccionadoIndex == 3 && indicePisoActualLocal == 2)
        {
            efectoPisa = true;
            destinoHundimiento = casaActual.transform.position + Vector3.down * offsetHundimiento;
            float lado = Random.value > 0.5f ? 1f : -1f;
            rotacionObjetivo = Quaternion.Euler(0, 0, anguloInclinacion * lado);
        }

        // Se elimina la restricción del nivel 3 para que siempre se reinicie el personaje
        CambiarPrefabPersonaje(0);

        VerificarCondiciones();
        ActualizarEstadoBotonBloqueado();
    }

    private IEnumerator EvolucionarCasa(GameObject casa, Sprite[] sprites, int objetivoIndex, float duracion)
    {
        if (casa == null || sprites == null)
        {
            Debug.LogWarning("EvolucionarCasa: La Casa o el array de Sprites es nulo.");
            yield break;
        }

        if (sprites.Length < (objetivoIndex + 1))
        {
            Debug.LogWarning($"EvolucionarCasa: Faltan sprites. El array tiene ({sprites.Length}) elementos pero se necesita hasta el índice ({objetivoIndex}).");
            yield break;
        }

        if (sprites[objetivoIndex] == null)
        {
            Debug.LogWarning($"EvolucionarCasa: El sprite en el índice {objetivoIndex} es nulo.");
            yield break;
        }

        SpriteRenderer sr = casa.GetComponent<SpriteRenderer>();
        if (sr == null) sr = casa.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) yield break;

        int pasos = objetivoIndex + 1;
        float tiempoPorPaso = (pasos > 0) ? (duracion / pasos) : 0f;

        for (int i = 0; i <= objetivoIndex; i++)
        {
            if (sprites[i] != null) sr.sprite = sprites[i];
            yield return new WaitForSeconds(tiempoPorPaso);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CASAS
    // ═══════════════════════════════════════════════════════════════════════════

    void InicializarBotonesCasas()
    {
        if (botonesCasas != null && botonesCasas.Length > 0)
        {
            for (int i = 0; i < botonesCasas.Length; i++)
            {
                int index = i;
                if (botonesCasas[i] != null)
                    botonesCasas[i].onClick.AddListener(() => ReemplazarCasa(index));
            }
        }
    }

    public void ReemplazarCasa(int indice)
    {
        if (simulacionActiva || enTransicionCasa) return;
        if (indice < 0 || prefabsCasas == null || indice >= prefabsCasas.Length) return;

        if (personajes != null && indicePersonajeActual >= 0 && indicePersonajeActual < personajes.Length &&
            personajes[indicePersonajeActual].fases != null && personajes[indicePersonajeActual].fases.Length > 0)
        {
            CambiarPrefabPersonaje(0);
        }

        DeshabilitarBotonesSeleccion(botonesCasas, indice);

        indiceCasaAnterior = indiceCasaActual;
        indiceCasaActual = indice;
        destinoCasa = ObtenerPosicionDinamica(indice);

        if (fondosCasas != null && indice < fondosCasas.Length)
            StartCoroutine(CambiarFondoCasa(indice));

        if (!simulacionActiva && musicaFondo != null
            && clipsMusicaFondo != null && indice < clipsMusicaFondo.Length
            && volumenesMusicaFondo != null && indice < volumenesMusicaFondo.Length)
        {
            if (rutinaFundidoMusica != null) StopCoroutine(rutinaFundidoMusica);
            float targetVolume = volumenesMusicaFondo[indice];
            rutinaFundidoMusica = StartCoroutine(FadeMusic(musicaFondo, targetVolume, duracionFundidoAudio, clipsMusicaFondo[indice]));
        }

        if (casaActual == null)
        {
            Vector3 spawnPos = new Vector3(destinoCasa.x, destinoCasa.y + alturaSpawnCasa, 0);
            casaQueCae = ObtenerCasaDelPool(indice);
            casaQueCae.transform.position = spawnPos;
            casaQueCae.transform.rotation = Quaternion.identity;
            caerCasa = true;
            enTransicionCasa = true;
            return;
        }

        Vector3 spawnPosSalida = new Vector3(destinoCasa.x, destinoCasa.y + alturaSpawnCasa, 0);
        casaQueCae = ObtenerCasaDelPool(indice);
        casaQueCae.transform.position = spawnPosSalida;
        casaQueCae.transform.rotation = Quaternion.identity;
        casaQueCae.SetActive(false);

        salidaCasa = new Vector3(-30f, casaActual.transform.position.y, casaActual.transform.position.z);
        moverCasa = true;
        enTransicionCasa = true;
    }

    private IEnumerator CambiarFondoCasa(int indice)
    {
        if (cambiandoFondo) yield break;
        cambiandoFondo = true;

        if (fondoActual != null) Destroy(fondoActual);

        if (fondosCasas[indice] != null)
            fondoActual = Instantiate(fondosCasas[indice], Vector3.zero, Quaternion.identity);

        yield return null;
        cambiandoFondo = false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PISOS
    // ═══════════════════════════════════════════════════════════════════════════

    void InicializarBotonesPisos()
    {
        if (botonesPisos != null && botonesPisos.Length > 0)
        {
            for (int i = 0; i < botonesPisos.Length; i++)
            {
                int index = i;
                if (botonesPisos[i] != null)
                    botonesPisos[i].onClick.AddListener(() => ReemplazarPiso(index));
            }
        }
    }

    public void ReemplazarPiso(int indice)
    {
        if (simulacionActiva || isReplacingPiso) return;
        if (prefabsPisos == null || indice < 0 || indice >= prefabsPisos.Length) return;

        ResetearEstadoSimulacion();
        DeshabilitarBotonesSeleccion(botonesPisos, indice);
        indicePisoActual = indice;
        StartCoroutine(ReplacePisoCoroutine(indice));
    }

    private IEnumerator ReplacePisoCoroutine(int indice)
    {
        isReplacingPiso = true;
        enTransicionPiso = true;

        ActualizarVisibilidadTextosPiso(-1);
        MostrarBotonesPorPiso(-1);
        CerrarTextoActivo();

        GameObject old = pisoActual;
        if (old != null && estadosPisos.ContainsKey(old))
            estadosPisos.Remove(old);

        Vector3 oldTarget = Vector3.zero;
        bool hasOld = old != null;

        if (hasOld)
        {
            float bordeIzquierdo = ObtenerBordePantalla(false);
            oldTarget = new Vector3(bordeIzquierdo, posicionDestinoPiso.y, posicionDestinoPiso.z);
        }

        float bordeDerecho = ObtenerBordePantalla(true);
        Vector3 spawnPos = new Vector3(bordeDerecho, posicionDestinoPiso.y, posicionDestinoPiso.z);

        GameObject nuevo = Instantiate(prefabsPisos[indice], spawnPos, Quaternion.identity);
        bool nuevoRegistrado = false;
        float newThreshold = 0.01f;
        float oldThreshold = 0.1f;

        yield return null;

        while (true)
        {
            if (nuevo != null)
                nuevo.transform.position = Vector3.MoveTowards(nuevo.transform.position, posicionDestinoPiso, velocidadEntradaPiso * Time.deltaTime);

            if (hasOld && old != null)
                old.transform.position = Vector3.MoveTowards(old.transform.position, oldTarget, velocidadSalidaPiso * Time.deltaTime);

            bool nuevoEnCentro = (nuevo != null) && (Vector3.SqrMagnitude(nuevo.transform.position - posicionDestinoPiso) <= newThreshold);
            bool viejoFuera = !hasOld || old == null || (Vector3.SqrMagnitude(old.transform.position - oldTarget) <= oldThreshold);

            if (nuevoEnCentro && !nuevoRegistrado)
            {
                pisoActual = nuevo;
                RegistrarPiso(pisoActual, indice);
                nuevoRegistrado = true;
            }

            if (nuevoRegistrado && viejoFuera)
            {
                if (hasOld && old != null) Destroy(old);
                break;
            }
            yield return null;
        }

        ActualizarVisibilidadTextosPiso(indice);
        MostrarBotonesPorPiso(indice);
        ActivarTextoAutomatico(indice);

        enTransicionPiso = false;
        isReplacingPiso = false;
        VerificarCondiciones();

        OnPisoTerminado?.Invoke();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SHAKE
    // ═══════════════════════════════════════════════════════════════════════════

    private void IniciarShake()
    {
        if (configsShakePorPiso == null || configsShakePorPiso.Length <= indicePisoActual || indicePisoActual < 0)
        {
            Debug.LogError($"Error: No hay 'configsShakePorPiso' definidos para el índice de piso {indicePisoActual}.");
            nivelSeleccionado = null;
            return;
        }

        ShakeConfigPorPiso configDelPisoActual = configsShakePorPiso[indicePisoActual];

        switch (nivelSeleccionadoIndex)
        {
            case 0: nivelSeleccionado = configDelPisoActual.nivel1; break;
            case 1: nivelSeleccionado = configDelPisoActual.nivel2; break;
            case 2: nivelSeleccionado = configDelPisoActual.nivel3; break;
            case 3: nivelSeleccionado = configDelPisoActual.nivel4; break;
            default: nivelSeleccionado = null; break;
        }

        if (nivelSeleccionado != null)
        {
            if (rutinaShake != null) StopCoroutine(rutinaShake);
            rutinaShake = StartCoroutine(RutinaShake());

            if (rutinaVibracion != null) StopCoroutine(rutinaVibracion);
            rutinaVibracion = StartCoroutine(RutinaVibracionCoherente(nivelSeleccionado.frecuencia, nivelSeleccionado.amplitud));
        }
    }

    private IEnumerator RutinaVibracionCoherente(float frecuencia, float amplitud)
    {
        if (frecuencia <= 0.1f || amplitud <= 0.1f) yield break;
        float intervalo = Mathf.Clamp(1f / frecuencia, 0.1f, 1f);
        while (true)
        {
            Handheld.Vibrate();
            yield return new WaitForSeconds(intervalo);
        }
    }

    private IEnumerator RutinaShake()
    {
        if (noise == null || nivelSeleccionado == null) yield break;
        yield return TransicionShake(
            noise.AmplitudeGain, nivelSeleccionado.amplitud,
            noise.FrequencyGain, nivelSeleccionado.frecuencia
        );
    }

    private IEnumerator TransicionShake(float desdeAmp, float hastaAmp, float desdeFreq, float hastaFreq)
    {
        float tiempo = 0f;
        while (tiempo < duracionTransicion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracionTransicion;
            if (noise != null)
            {
                noise.AmplitudeGain = Mathf.Lerp(desdeAmp, hastaAmp, t);
                noise.FrequencyGain = Mathf.Lerp(desdeFreq, hastaFreq, t);
            }
            yield return null;
        }

        if (noise != null)
        {
            noise.AmplitudeGain = hastaAmp;
            noise.FrequencyGain = hastaFreq;
        }
    }

    private void SetShake(float amplitud, float frecuencia)
    {
        if (noise != null)
        {
            noise.AmplitudeGain = amplitud;
            noise.FrequencyGain = frecuencia;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PISOS — Estado
    // ═══════════════════════════════════════════════════════════════════════════

    private void RegistrarPiso(GameObject piso, int indicePrefab)
    {
        if (piso == null) return;

        float alturaExtra = 2f;
        if (alturasExtraPisos != null && indicePrefab >= 0 && indicePrefab < alturasExtraPisos.Length)
            alturaExtra = alturasExtraPisos[indicePrefab];

        if (!estadosPisos.ContainsKey(piso))
        {
            estadosPisos.Add(piso, new PisoEstado
            {
                posicionInicial = piso.transform.position,
                posicionObjetivo = piso.transform.position,
                desplegado = false,
                alturaExtra = alturaExtra,
                indicePrefab = indicePrefab
            });
        }
        else
        {
            var e = estadosPisos[piso];
            e.posicionInicial = piso.transform.position;
            e.posicionObjetivo = piso.transform.position;
            e.desplegado = false;
            e.alturaExtra = alturaExtra;
            e.indicePrefab = indicePrefab;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UPDATE
    // ═══════════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (moverCasa && casaActual != null)
        {
            casaActual.transform.position = Vector3.MoveTowards(
                casaActual.transform.position,
                salidaCasa,
                velocidadSalidaCasa * Time.deltaTime
            );

            if (Vector3.Distance(casaActual.transform.position, salidaCasa) < 0.1f)
            {
                DevolverCasaAlPool(casaActual, indiceCasaAnterior);
                casaActual = null;

                casaQueCae.SetActive(true);
                casaQueCae.transform.position = new Vector3(destinoCasa.x, destinoCasa.y + alturaSpawnCasa, 0);

                moverCasa = false;
                caerCasa = true;
            }
        }

        if (caerCasa && casaQueCae != null)
        {
            casaQueCae.transform.position = Vector3.MoveTowards(
                casaQueCae.transform.position,
                destinoCasa,
                velocidadCaidaCasa * Time.deltaTime
            );

            if (Vector3.Distance(casaQueCae.transform.position, destinoCasa) < 0.01f)
            {
                caerCasa = false;
                enTransicionCasa = false;
                casaActual = casaQueCae;
                casaQueCae = null;
                VerificarCondiciones();
                OnCasaTerminada?.Invoke();
            }
        }

        if (efectoPisa && casaActual != null)
        {
            casaActual.transform.position = Vector3.MoveTowards(
                casaActual.transform.position,
                destinoHundimiento,
                velocidadHundimiento * Time.deltaTime
            );

            casaActual.transform.rotation = Quaternion.RotateTowards(
                casaActual.transform.rotation,
                rotacionObjetivo,
                velocidadInclinacion * Time.deltaTime
            );

            bool llegoPos = Vector3.Distance(casaActual.transform.position, destinoHundimiento) < 0.01f;
            bool llegoRot = Quaternion.Angle(casaActual.transform.rotation, rotacionObjetivo) < 0.5f;

            if (llegoPos && llegoRot) efectoPisa = false;
        }

        if (botonSimular != null) VerificarCondiciones();

        DetectarClickFuera();
        ActualizarEstadoBotonBloqueado();

        if (!simulacionActiva && !caerCasa && !moverCasa && !enTransicionCasa && !efectoPisa && casaActual != null)
        {
            Vector2 posDinamica = ObtenerPosicionDinamica(indiceCasaActual);
            casaActual.transform.position = new Vector3(posDinamica.x, posDinamica.y, casaActual.transform.position.z);
        }

        if (!simulacionActiva && !enTransicionPiso && !animandoTexto
            && indicePisoActual >= 0 && indicePisoActual < animacionesTextosPisos.Length)
        {
            var anim = animacionesTextosPisos[indicePisoActual];

            if (anim.textoPiso != null && anim.textoPiso.gameObject.activeSelf)
            {
                Vector3 posFinReal = ObtenerPosicionTextoDinamica(anim.posFin, anim.textoPiso, true);
                anim.textoPiso.rectTransform.anchoredPosition3D = posFinReal;
            }

            if (anim.textoInteractivo != null && anim.textoInteractivo.gameObject.activeSelf)
            {
                Vector3 posFinReal = ObtenerPosicionTextoDinamica(anim.posFin, anim.textoInteractivo, true);
                anim.textoInteractivo.rectTransform.anchoredPosition3D = posFinReal;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // POSICIONAMIENTO DINÁMICO
    // ═══════════════════════════════════════════════════════════════════════════

    private Vector3 ObtenerPosicionTextoDinamica(Vector3 posReferencia, TMP_Text texto, bool esPosicionFin)
    {
        if (texto == null) return posReferencia;

        RectTransform parentRect = texto.rectTransform.parent as RectTransform;
        if (parentRect == null) return posReferencia;

        float anchoMitadPadre = parentRect.rect.width / 2f;
        float anchoMitadTexto = texto.preferredWidth / 2f;
        if (anchoMitadTexto == 0f) anchoMitadTexto = texto.rectTransform.rect.width / 2f;

        Vector3 posDinamica = posReferencia;

        if (esPosicionFin)
        {
            float margen = 20f;
            float limiteIzquierdo = -anchoMitadPadre + anchoMitadTexto + margen;
            float limiteDerecho = anchoMitadPadre - anchoMitadTexto - margen;
            posDinamica.x = Mathf.Lerp(limiteIzquierdo, limiteDerecho, posicionXTextoEnPantalla);
        }
        else
        {
            float offsetFuera = anchoMitadTexto + 200f;
            if (posReferencia.x <= 0)
                posDinamica.x = -anchoMitadPadre - offsetFuera;
            else
                posDinamica.x = anchoMitadPadre + offsetFuera;
        }

        return posDinamica;
    }

    private Vector2 ObtenerPosicionDinamica(int indice)
    {
        if (indice < 0 || indice >= posicionesCasas.Length) return Vector2.zero;

        Vector2 posicionOriginal = posicionesCasas[indice];

        if (Camera.main != null)
        {
            float xDinamico = Camera.main.ViewportToWorldPoint(new Vector3(
                posicionXEnPantalla,
                0f,
                Mathf.Abs(Camera.main.transform.position.z)
            )).x;
            return new Vector2(xDinamico, posicionOriginal.y);
        }

        return posicionOriginal;
    }

    private float ObtenerBordePantalla(bool ladoDerecho)
    {
        if (Camera.main != null)
        {
            float viewportX = ladoDerecho ? 1.5f : -0.5f;
            Vector3 worldPos = Camera.main.ViewportToWorldPoint(
                new Vector3(viewportX, 0.5f, Mathf.Abs(Camera.main.transform.position.z))
            );
            return worldPos.x;
        }

        return ladoDerecho ? offsetEntradaXPiso : offsetSalidaXPiso;
    }
}