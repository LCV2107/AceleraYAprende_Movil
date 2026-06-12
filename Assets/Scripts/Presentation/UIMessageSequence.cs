using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class UIMessageSequence : MonoBehaviour
{
    // --- CLASE INTERNA ---
    // (La mantenemos aquí dentro, ya que todo está en un solo script)
    [System.Serializable]
    public class MessageBox
    {
        [Header("UI del cuadro")]
        public GameObject panel;
        public TextMeshProUGUI texto;
        [TextArea(2, 5)]
        public string mensaje;

        [Header("Objeto asociado (solo para cuadro 1 y 2)")]
        public GameObject objeto;

        [Header("Sincronización")]
        [Tooltip("Define cuántos segundos debe tomar el tecleado de este mensaje para sincronizarlo con el audio.")]
        public float duracionEsteMensaje = 3.0f;

        [HideInInspector] public Vector3 originalScale;
    }

    // --- REFERENCIAS DE ESCENA ---
    // <<< ¡NUEVO! Arrastra aquí tus objetos de la escena en el Inspector
    [Header("--- REFERENCIAS DE ESCENA ---")]
    [Tooltip("Arrastra aquí el audio y los elementos de UI para la secuencia")]
    public AudioClip audioDeLaEscena;
    [Space(10)]
    public GameObject panel1;
    public TextMeshProUGUI texto1;
    public GameObject objeto1;
    [Space(10)]
    public GameObject panel2;
    public TextMeshProUGUI texto2;
    public GameObject objeto2;
    [Space(10)]
    public GameObject panel3;
    public TextMeshProUGUI texto3;
    // ... puedes añadir más referencias si tu secuencia es más larga (panel4, texto4...)


    // --- CONFIGURACIÓN (Llenado por código) ---
    [Header("Configuración de cuadros (Se llena desde código)")]
    public MessageBox[] cuadros;

    [Header("Botón final (Arrastrar desde la escena)")]
    public Button botonFinal;
    public float duracionEscalaBoton = 0.5f;

    [Header("Transición final (Arrastrar desde la escena)")]
    public Image imagenTransicion;
    public float duracionSlide = 1f;
    public Vector3 desplazamientoSlide = new Vector3(-1920f, 0, 0);
    public string nombreEscenaDestino;

    [Header("Tiempos y parámetros")]
    public float delayEntreCuadros = 1f;
    public float duracionFade = 0.5f;
    public float duracionEscala = 0.5f;
    public float escalaInicial = 0f;

    [Header("Configuración de Audio (Automático)")]
    public AudioSource audioSource;
    public AudioClip audioFondoMensaje; // Se llenará desde el código

    [Tooltip("Tiempo de pausa en segundos ANTES de que comiencen a aparecer las letras y el audio.")]
    public float tiempoInicialPausa = 0.5f;

    [Header("Opciones")]
    public bool disableAnimatorDuringScale = true;


    // <<< ¡NUEVO! ---
    // Awake() se ejecuta ANTES que Start()
    // Usamos esto para construir nuestra secuencia en código.
    void Awake()
    {
        // --- 1. AQUÍ CONFIGURAS TU SECUENCIA ---
        // Define tus mensajes y reparte tus 15 minutos (902 segundos)

        cuadros = new MessageBox[]
        {
            // --- Bloque 1 ---
            new MessageBox {
                panel = panel1,       // Objeto de la escena
                texto = texto1,       // Objeto de la escena
                objeto = objeto1,     // Objeto de la escena
                mensaje = "Aquí va tu primer mensaje. El narrador habla durante...",
                duracionEsteMensaje = 120.0f  // ...120 segundos (2 minutos)
            },

            // --- Bloque 2 ---
            new MessageBox {
                panel = panel2,
                texto = texto2,
                objeto = objeto2,
                mensaje = "Luego, aparece el segundo mensaje, que es una explicación larga.",
                duracionEsteMensaje = 300.0f  // ...300 segundos (5 minutos)
            },

            // --- Bloque 3 ---
            new MessageBox {
                panel = panel3,
                texto = texto3,
                objeto = null, // Este no tiene objeto
                mensaje = "Y finalmente, el último mensaje antes del botón. El resto del audio...",
                duracionEsteMensaje = 482.0f // ...dura 482 segundos (aprox 8 min)
            }
            
            // ¡Añade más bloques 'new MessageBox {}' si los necesitas!
            // Solo recuerda añadir las referencias (panel4, texto4) arriba.
        };

        // --- 2. CONFIGURAR AUDIO ---
        // (Tomado de la lógica del Configurador)
        if (audioSource == null)
        {
            if (!TryGetComponent<AudioSource>(out audioSource))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        audioSource.clip = audioDeLaEscena;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioFondoMensaje = audioDeLaEscena; // Asigna al campo existente
    }

    // <<< ¡MODIFICADO! ---
    // Start() ahora está más limpio. Solo prepara la UI
    // y lanza la secuencia, ya que Awake() hizo la configuración.
    void Start()
    {
        // 1. Ocultar paneles y objetos al inicio
        // (Usa el array 'cuadros' que llenamos en Awake)
        foreach (var cuadro in cuadros)
        {
            if (cuadro.panel != null)
                cuadro.panel.SetActive(false);

            if (cuadro.objeto != null)
            {
                cuadro.originalScale = cuadro.objeto.transform.localScale;
                cuadro.objeto.SetActive(false);
            }
        }

        // 2. Configurar Botón
        // (Usa el 'botonFinal' que arrastraste al Inspector)
        if (botonFinal != null)
        {
            botonFinal.gameObject.SetActive(false);
            botonFinal.onClick.AddListener(() => StartCoroutine(AccionBotonFinal()));
        }

        // 3. Iniciar Secuencia
        StartCoroutine(IniciarSecuencia());
    }

    //-------------------------------------------------------------------------
    // CORRUTINAS PRINCIPALES
    // (Sin cambios. Todo esto funciona igual que antes)
    //-------------------------------------------------------------------------

    IEnumerator IniciarSecuencia()
    {
        yield return new WaitForSeconds(tiempoInicialPausa);

        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }

        yield return StartCoroutine(MostrarSecuencia());

        if (audioSource.isPlaying)
        {
            // Opcional: podrías dejarlo sonar si quieres
            // audioSource.Stop(); 
        }

        yield return StartCoroutine(MostrarBotonFinal());
    }

    IEnumerator MostrarSecuencia()
    {
        for (int i = 0; i < cuadros.Length; i++)
        {
            var actual = cuadros[i];

            if (actual.objeto != null)
            {
                actual.objeto.SetActive(true);

                Animator anim = null;
                if (disableAnimatorDuringScale)
                {
                    anim = actual.objeto.GetComponent<Animator>();
                    if (anim != null)
                        anim.enabled = false;
                }

                yield return StartCoroutine(AnimarEscala(actual.objeto.transform, actual.originalScale));

                if (anim != null)
                    anim.enabled = true;

                yield return new WaitForSeconds(0.15f);
            }

            if (actual.panel != null)
            {
                CanvasGroup cg = actual.panel.GetComponent<CanvasGroup>();
                if (cg == null) cg = actual.panel.AddComponent<CanvasGroup>();

                actual.panel.SetActive(true);
                cg.alpha = 0f;
                actual.texto.text = "";

                yield return StartCoroutine(FadeCanvasGroup(cg, 0f, 1f));

                float velocidadLetraDinamica = 0.03f;

                if (actual.duracionEsteMensaje > 0 && actual.mensaje.Length > 0)
                {
                    velocidadLetraDinamica = actual.duracionEsteMensaje / actual.mensaje.Length;
                }
                else if (actual.mensaje.Length == 0)
                {
                    yield return new WaitForSeconds(actual.duracionEsteMensaje);
                }

                foreach (char c in actual.mensaje)
                {
                    actual.texto.text += c;
                    yield return new WaitForSeconds(velocidadLetraDinamica);
                }

                if (i < cuadros.Length - 1)
                {
                    yield return new WaitForSeconds(delayEntreCuadros);
                    yield return StartCoroutine(FadeCanvasGroup(cg, 1f, 0f));
                    actual.panel.SetActive(false);

                    if (actual.objeto != null)
                    {
                        actual.objeto.SetActive(false);
                    }
                }
            }
            else
            {
                yield return new WaitForSeconds(delayEntreCuadros);
            }
        }
    }

    IEnumerator MostrarBotonFinal()
    {
        if (botonFinal == null)
            yield break;

        botonFinal.gameObject.SetActive(true);
        RectTransform tr = botonFinal.transform as RectTransform;

        Vector3 escalaFinal = Vector3.one;
        Vector3 escalaInicialBoton = escalaFinal * 0f;
        tr.localScale = escalaInicialBoton;

        float tiempo = 0f;
        while (tiempo < duracionEscalaBoton)
        {
            tiempo += Time.deltaTime;
            float norm = Mathf.Clamp01(tiempo / duracionEscalaBoton);
            float eased = EaseOutBack(norm);
            tr.localScale = Vector3.LerpUnclamped(escalaInicialBoton, escalaFinal, eased);
            yield return null;
        }

        tr.localScale = escalaFinal;
    }

    IEnumerator AccionBotonFinal()
    {
        if (imagenTransicion != null)
        {
            RectTransform tr = imagenTransicion.GetComponent<RectTransform>();
            Vector3 inicio = tr.localPosition;
            Vector3 fin = inicio + desplazamientoSlide;

            float tiempo = 0f;
            while (tiempo < duracionSlide)
            {
                tiempo += Time.deltaTime;
                float t = Mathf.Clamp01(tiempo / duracionSlide);
                tr.localPosition = Vector3.Lerp(inicio, fin, t);
                yield return null;
            }
        }

        if (!string.IsNullOrEmpty(nombreEscenaDestino))
        {
            SceneManager.LoadScene(nombreEscenaDestino);
        }
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float inicio, float fin)
    {
        if (duracionFade <= 0f) { cg.alpha = fin; yield break; }

        float t = 0f;
        while (t < duracionFade)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(inicio, fin, t / duracionFade);
            yield return null;
        }
        cg.alpha = fin;
    }

    IEnumerator AnimarEscala(Transform obj, Vector3 escalaFinal)
    {
        if (obj == null)
            yield break;

        if (duracionEscala <= 0f)
        {
            obj.localScale = escalaFinal;
            yield break;
        }

        Vector3 inicio = escalaFinal * escalaInicial;
        obj.localScale = inicio;

        float tiempo = 0f;
        while (tiempo < duracionEscala)
        {
            tiempo += Time.deltaTime;
            float norm = Mathf.Clamp01(tiempo / duracionEscala);
            float eased = EaseOutBack(norm);
            obj.localScale = Vector3.LerpUnclamped(inicio, escalaFinal, eased);
            yield return null;
        }

        obj.localScale = escalaFinal;
    }

    float EaseOutBack(float t)
    {
        float c1 = 0.8f; 
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
