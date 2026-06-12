using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ParametrosOnda
{
    [Tooltip("Altura máxima de la onda")]
    public float amplitud = 1f;

    [Tooltip("Separación entre las crestas (Longitud de onda)")]
    public float longitudOnda = 1f;

    [Tooltip("Velocidad a la que la onda se desplaza hacia un lado")]
    public float velocidadMovimiento = 3f;

    [Tooltip("Intensidad de crestas secundarias (Ruido)")]
    public float fluctuacion = 0.1f;

    [Header("Tiempos del Nivel")]
    [Tooltip("Tiempo que tarda en dibujarse de izquierda a derecha (Creación)")]
    public float tiempoAparicion = 2f;

    [Header("Escala Visual")]
    [Tooltip("Escala horizontal en pantalla")]
    [Range(0.1f, 3f)] public float escalaHorizontal = 1f;

    [Tooltip("Escala vertical en pantalla")]
    [Range(0.1f, 3f)] public float escalaVertical = 1f;
}

[RequireComponent(typeof(LineRenderer))]
public class OndaSismica2D : MonoBehaviour
{
    [Header("Configuración de Capas (Sorting)")]
    [Tooltip("Opcional: Asigna un Canvas. La onda se dibujará un nivel por detrás de este Canvas.")]
    public Canvas canvasReferencia;

    [Tooltip("Si no usas un Canvas de referencia, escribe el nombre del Sorting Layer manual")]
    public string sortingLayerManual = "Default";

    [Tooltip("Si no usas un Canvas de referencia, asigna el Order in Layer manual")]
    public int orderInLayerManual = 0;

    [Header("Asigna los Botones de Nivel UI aquí")]
    public Button botonNivel1;
    public Button botonNivel2;
    public Button botonNivel3;
    public Button botonNivel4;

    [Header("Botón que activa la onda")]
    public Button botonActivarOnda;

    [Header("Parámetros por Nivel (Configurables en tiempo real)")]
    public ParametrosOnda paramsNivel1 = new ParametrosOnda { amplitud = 0.8f, longitudOnda = 4f, velocidadMovimiento = 3f, fluctuacion = 0.1f, tiempoAparicion = 2f, escalaHorizontal = 1f, escalaVertical = 1f };
    public ParametrosOnda paramsNivel2 = new ParametrosOnda { amplitud = 1.2f, longitudOnda = 3.5f, velocidadMovimiento = 3f, fluctuacion = 0.3f, tiempoAparicion = 2f, escalaHorizontal = 1f, escalaVertical = 1f };
    public ParametrosOnda paramsNivel3 = new ParametrosOnda { amplitud = 1.8f, longitudOnda = 2.8f, velocidadMovimiento = 3f, fluctuacion = 0.6f, tiempoAparicion = 2f, escalaHorizontal = 1f, escalaVertical = 1f };
    public ParametrosOnda paramsNivel4 = new ParametrosOnda { amplitud = 2.5f, longitudOnda = 2.0f, velocidadMovimiento = 3f, fluctuacion = 1.0f, tiempoAparicion = 2f, escalaHorizontal = 1f, escalaVertical = 1f };

    [Header("Duración General")]
    public float duracionTotalOnda = 10f;
    public float tiempoFadeOut = 2f;

    [Header("Ajustes base de la onda")]
    public int puntos = 180;
    public float alturaBaseY = 0f;

    [Header("Grosor y contorno")]
    public float grosorPrincipal = 0.08f;
    public float grosorContorno = 0.13f;

    // Privados
    private LineRenderer lrPrincipal;
    private LineRenderer lrContorno;
    private float tiempoInicio;

    // Variables dinámicas actuales
    private float amplitudFisica;
    private float longitudOndaFisica;
    private float velocidadActual;
    private float intensidadCrestasActual;
    private float tiempoAparicionActual;
    private float escalaHorizontalActual;
    private float escalaVerticalActual;

    private Color colorBaseNivel;
    private bool nivelSeleccionado = false;
    private bool ondaActiva = false;
    private int nivelActual = 0;

    // Nivel seleccionado por botón
    private int nivelPulsado = 0;

    void Awake()
    {
        lrPrincipal = GetComponent<LineRenderer>();
        lrPrincipal.positionCount = puntos;
        lrPrincipal.startWidth = grosorPrincipal;
        lrPrincipal.endWidth = grosorPrincipal;
        lrPrincipal.material = new Material(Shader.Find("Sprites/Default"));
        lrPrincipal.useWorldSpace = false;
        lrPrincipal.enabled = false;

        GameObject contornoGO = new GameObject("ContornoOnda");
        contornoGO.transform.SetParent(transform, false);
        lrContorno = contornoGO.AddComponent<LineRenderer>();
        lrContorno.positionCount = puntos;
        lrContorno.startWidth = grosorContorno;
        lrContorno.endWidth = grosorContorno;
        lrContorno.material = new Material(Shader.Find("Sprites/Default"));
        lrContorno.startColor = Color.black;
        lrContorno.endColor = Color.black;
        lrContorno.useWorldSpace = false;
        lrContorno.enabled = false;

        // Listeners de los botones de nivel
        if (botonNivel1 != null) botonNivel1.onClick.AddListener(() => SeleccionarNivel(1));
        if (botonNivel2 != null) botonNivel2.onClick.AddListener(() => SeleccionarNivel(2));
        if (botonNivel3 != null) botonNivel3.onClick.AddListener(() => SeleccionarNivel(3));
        if (botonNivel4 != null) botonNivel4.onClick.AddListener(() => SeleccionarNivel(4));

        if (botonActivarOnda != null)
        {
            botonActivarOnda.onClick.AddListener(ActivarOnda);
        }

        AplicarOrdenDeCapas();
    }

    private void SeleccionarNivel(int nivel)
    {
        // Si pulsas el mismo nivel que ya está activo, lo deselecciona (toggle manual)
        if (nivelPulsado == nivel)
        {
            nivelPulsado = 0;
        }
        else
        {
            nivelPulsado = nivel;
        }

        // Si se deselecciona el nivel, paramos la onda
        if (nivelPulsado == 0)
        {
            ondaActiva = false;
        }
    }

    private void AplicarOrdenDeCapas()
    {
        string capaAAplicar;
        int ordenPrincipal;
        int ordenContorno;

        if (canvasReferencia != null)
        {
            capaAAplicar = canvasReferencia.sortingLayerName;
            ordenPrincipal = canvasReferencia.sortingOrder - 1;
            ordenContorno = canvasReferencia.sortingOrder - 2;
        }
        else
        {
            capaAAplicar = sortingLayerManual;
            ordenPrincipal = orderInLayerManual;
            ordenContorno = orderInLayerManual - 1;
        }

        lrPrincipal.sortingLayerName = capaAAplicar;
        lrPrincipal.sortingOrder = ordenPrincipal;

        lrContorno.sortingLayerName = capaAAplicar;
        lrContorno.sortingOrder = ordenContorno;
    }

    void OnDestroy()
    {
        if (botonNivel1 != null) botonNivel1.onClick.RemoveAllListeners();
        if (botonNivel2 != null) botonNivel2.onClick.RemoveAllListeners();
        if (botonNivel3 != null) botonNivel3.onClick.RemoveAllListeners();
        if (botonNivel4 != null) botonNivel4.onClick.RemoveAllListeners();

        if (botonActivarOnda != null)
        {
            botonActivarOnda.onClick.RemoveListener(ActivarOnda);
        }
    }

    void LateUpdate()
    {
        ActualizarEstadoNivel();

        if (ondaActiva && nivelSeleccionado)
        {
            float tiempoTranscurrido = Time.time - tiempoInicio;

            if (tiempoTranscurrido <= duracionTotalOnda)
            {
                DibujarOndaProgresiva(tiempoTranscurrido);
                lrPrincipal.enabled = true;
                lrContorno.enabled = true;
            }
            else
            {
                ondaActiva = false;
                lrPrincipal.enabled = false;
                lrContorno.enabled = false;
            }
        }
        else
        {
            lrPrincipal.enabled = false;
            lrContorno.enabled = false;
        }
    }

    private void ActualizarEstadoNivel()
    {
        // Ahora leemos el nivel desde nivelPulsado en lugar de los toggles
        int nivelDetectado = nivelPulsado;

        nivelSeleccionado = (nivelDetectado > 0);

        if (!nivelSeleccionado)
        {
            ondaActiva = false;
            return;
        }

        if (nivelDetectado != nivelActual)
        {
            nivelActual = nivelDetectado;
            colorBaseNivel = ObtenerColorPorNivel(nivelDetectado);
        }

        ParametrosOnda p = nivelActual switch
        {
            1 => paramsNivel1,
            2 => paramsNivel2,
            3 => paramsNivel3,
            4 => paramsNivel4,
            _ => paramsNivel1
        };

        amplitudFisica = p.amplitud;
        longitudOndaFisica = p.longitudOnda;
        velocidadActual = p.velocidadMovimiento;
        intensidadCrestasActual = p.fluctuacion;
        tiempoAparicionActual = p.tiempoAparicion;
        escalaHorizontalActual = p.escalaHorizontal;
        escalaVerticalActual = p.escalaVertical;
    }

    private void ActivarOnda()
    {
        if (!nivelSeleccionado) return;
        tiempoInicio = Time.time;
        ondaActiva = true;
    }

    private Color ObtenerColorPorNivel(int nivel)
    {
        return nivel switch
        {
            1 => new Color(0.15f, 1.00f, 0.15f, 1f),
            2 => new Color(1.00f, 0.95f, 0.00f, 1f),
            3 => new Color(1.00f, 0.55f, 0.00f, 1f),
            4 => new Color(1.00f, 0.10f, 0.10f, 1f),
            _ => Color.white
        };
    }

    private float ObtenerAnchoPantalla()
    {
        if (Camera.main == null) return 18f;
        float altoOrtografico = Camera.main.orthographicSize * 2f;
        return altoOrtografico * Camera.main.aspect;
    }

    private void DibujarOndaProgresiva(float tiempoTranscurrido)
    {
        float anchoVisual = ObtenerAnchoPantalla() * escalaHorizontalActual;

        float progresoAparicion = Mathf.Clamp01(tiempoTranscurrido / tiempoAparicionActual);

        float factorFade = 1f;
        if (tiempoTranscurrido > (duracionTotalOnda - tiempoFadeOut))
        {
            float tiempoFade = tiempoTranscurrido - (duracionTotalOnda - tiempoFadeOut);
            factorFade = 1f - Mathf.Clamp01(tiempoFade / tiempoFadeOut);
        }

        for (int i = 0; i < puntos; i++)
        {
            float t = (float)i / (puntos - 1);
            float xLocal = t * anchoVisual - (anchoVisual * 0.5f);
            float yVisual = 0f;

            if (t <= progresoAparicion)
            {
                float faseBase = ((xLocal - tiempoTranscurrido * velocidadActual) / longitudOndaFisica) * Mathf.PI * 2f;
                float yBase = Mathf.Sin(faseBase);

                float ruido = Mathf.PerlinNoise(xLocal * 3f, tiempoTranscurrido * (velocidadActual + 1f)) * 2f - 1f;
                yBase += ruido * intensidadCrestasActual;

                float distanciaAlFrente = progresoAparicion - t;
                float fadeFrontal = Mathf.Clamp01(distanciaAlFrente * 20f);
                float fadeBordes = Mathf.Clamp01(t * 10f) * Mathf.Clamp01((1f - t) * 10f);

                yVisual = yBase * amplitudFisica * escalaVerticalActual * factorFade * fadeFrontal * fadeBordes;
            }

            Vector3 pos = new Vector3(xLocal, alturaBaseY + yVisual, 0f);
            lrPrincipal.SetPosition(i, pos);
            lrContorno.SetPosition(i, pos);
        }

        Color colorFade = new Color(colorBaseNivel.r, colorBaseNivel.g, colorBaseNivel.b, colorBaseNivel.a * factorFade);
        lrPrincipal.startColor = colorFade;
        lrPrincipal.endColor = colorFade;

        Color contornoFade = new Color(0f, 0f, 0f, factorFade * 0.8f);
        lrContorno.startColor = contornoFade;
        lrContorno.endColor = contornoFade;
    }
}