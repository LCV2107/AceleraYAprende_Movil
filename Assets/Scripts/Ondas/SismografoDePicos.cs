using UnityEngine;
using UnityEngine.UI;

public class SismografoDePicos : MonoBehaviour
{
    [Header("UI - Controles")]
    public Toggle toggleNivel1;
    public Toggle toggleNivel2;
    public Toggle toggleNivel3;
    public Toggle toggleNivel4;
    public Button botonActivarOnda;

    [Header("Configuración Visual")]
    [Tooltip("Cantidad de líneas verticales (resolución del gráfico)")]
    public int cantidadLineas = 150;

    [Tooltip("Ancho total que ocupará el gráfico en pantalla")]
    public float anchoTotal = 20f;

    [Tooltip("Grosor de cada línea vertical (Muy delgado)")]
    public float grosorLinea = 0.08f;

    [Tooltip("Altura máxima de los picos")]
    public float alturaMaxima = 8f;

    [Header("Colores (Gradiente de Intensidad)")]
    public Gradient colorIntensidad; // Verde -> Amarillo -> Rojo

    [Header("Parámetros Físicos (Niveles)")]
    // x: Amplitud (Altura pico), y: Frecuencia (Vibración/Ruido), z: Velocidad Scroll
    public Vector3 paramsNivel1 = new Vector3(0.3f, 2f, 15f); // Suave
    public Vector3 paramsNivel2 = new Vector3(0.6f, 5f, 20f); // Medio
    public Vector3 paramsNivel3 = new Vector3(1.0f, 10f, 25f); // Fuerte
    public Vector3 paramsNivel4 = new Vector3(1.5f, 20f, 30f); // Terremoto violento

    // Privados
    private LineRenderer[] lineas; // Array de líneas verticales
    private float[] valoresHistorial; // Datos numéricos
    private bool ondaActiva = false;
    private int nivelActual = 0;

    // Valores actuales suavizados
    private float ampActual = 0f;
    private float freqActual = 1f;
    private float velActual = 10f;
    private float tiempoSimulacion = 0f;

    void Start()
    {
        InicializarLineas();

        // Configurar gradiente por defecto si está vacío
        if (colorIntensidad == null || colorIntensidad.colorKeys.Length == 0)
        {
            colorIntensidad = new Gradient();
            colorIntensidad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.green, 0f),
                    new GradientColorKey(Color.yellow, 0.5f),
                    new GradientColorKey(Color.red, 1f)
                },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
        }

        if (botonActivarOnda != null) botonActivarOnda.onClick.AddListener(() => ondaActiva = !ondaActiva);
    }

    void InicializarLineas()
    {
        // Limpiar hijos anteriores si los hubiera
        foreach (Transform child in transform) Destroy(child.gameObject);

        lineas = new LineRenderer[cantidadLineas];
        valoresHistorial = new float[cantidadLineas];

        float pasoX = anchoTotal / cantidadLineas;
        float xInicial = -anchoTotal / 2f;

        // Crear las líneas "quietas" en el espacio
        for (int i = 0; i < cantidadLineas; i++)
        {
            GameObject obj = new GameObject("Pico_" + i);
            obj.transform.SetParent(this.transform, false);

            // Posición fija en X
            float xPos = xInicial + (i * pasoX);
            obj.transform.localPosition = new Vector3(xPos, 0, 0);

            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2; // Punto arriba y punto abajo
            lr.startWidth = grosorLinea;
            lr.endWidth = grosorLinea;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Inicializar en plano (valor 0)
            lr.SetPosition(0, new Vector3(0, 0, 0));
            lr.SetPosition(1, new Vector3(0, 0, 0));

            lineas[i] = lr;
        }
    }

    void Update()
    {
        ActualizarEstadoNiveles();

        // Solo actualizamos datos si la onda está activa o si hay residuos que limpiar
        if (ondaActiva || ampActual > 0.01f)
        {
            float dt = Time.deltaTime;
            tiempoSimulacion += dt * velActual;

            // 1. Desplazar historial (Scroll de datos)
            // Movemos los datos hacia la izquierda (índice 0 es el más antiguo)
            for (int i = 0; i < cantidadLineas - 1; i++)
            {
                valoresHistorial[i] = valoresHistorial[i + 1];
            }

            // 2. Generar NUEVO valor en el extremo derecho
            float nuevoValor = 0f;
            if (ondaActiva && nivelActual > 0)
            {
                // Usamos Perlin Noise para que sea orgánico pero "picudo"
                // Frecuencia alta = picos más juntos y nerviosos
                float ruido = Mathf.PerlinNoise(tiempoSimulacion, 0f);

                // Convertimos el ruido [0,1] a [-1, 1]
                ruido = (ruido * 2f) - 1f;

                // Multiplicamos por la amplitud deseada
                nuevoValor = ruido * ampActual * alturaMaxima;

                // Añadimos un "jitter" aleatorio extra para que parezca sismo (muy sucio)
                if (nivelActual >= 3)
                {
                    nuevoValor += Random.Range(-ampActual, ampActual) * 2f;
                }
            }
            else
            {
                // Si está apagado, tiende a 0 suavemente
                nuevoValor = Mathf.Lerp(valoresHistorial[cantidadLineas - 2], 0f, 0.2f);
            }

            valoresHistorial[cantidadLineas - 1] = nuevoValor;

            // 3. Dibujar
            ActualizarGraficoVisual();
        }
    }

    void ActualizarGraficoVisual()
    {
        for (int i = 0; i < cantidadLineas; i++)
        {
            float valor = valoresHistorial[i];
            float valorAbs = Mathf.Abs(valor);

            // Configuramos la línea vertical: desde -Y hasta +Y (espejo)
            // Esto crea el efecto de onda de sonido/sismo centrada
            lineas[i].SetPosition(0, new Vector3(0, -valorAbs, 0)); // Abajo
            lineas[i].SetPosition(1, new Vector3(0, valorAbs, 0));  // Arriba

            // COLOR: Basado en la altura del pico (Amplitud)
            // Normalizamos respecto a la altura máxima esperada
            float intensidad = Mathf.Clamp01(valorAbs / (alturaMaxima * 1.2f));

            // Factor de desvanecimiento para el lado izquierdo (cola antigua)
            // Los últimos 20% de líneas a la izquierda se desvanecen
            float alpha = 1f;
            if (i < cantidadLineas * 0.2f)
            {
                alpha = i / (cantidadLineas * 0.2f);
            }

            Color c = colorIntensidad.Evaluate(intensidad);
            c.a = alpha;

            lineas[i].startColor = c;
            lineas[i].endColor = c;
        }
    }

    void ActualizarEstadoNiveles()
    {
        int nuevoNivel = 0;
        if (toggleNivel4 != null && toggleNivel4.isOn) nuevoNivel = 4;
        else if (toggleNivel3 != null && toggleNivel3.isOn) nuevoNivel = 3;
        else if (toggleNivel2 != null && toggleNivel2.isOn) nuevoNivel = 2;
        else if (toggleNivel1 != null && toggleNivel1.isOn) nuevoNivel = 1;

        nivelActual = nuevoNivel;

        Vector3 objetivo = Vector3.zero;
        if (nivelActual > 0)
        {
            objetivo = nivelActual switch
            {
                1 => paramsNivel1,
                2 => paramsNivel2,
                3 => paramsNivel3,
                4 => paramsNivel4,
                _ => paramsNivel1
            };
        }

        // Interpolación (Lerp) para que al cambiar de nivel la onda crezca/decrezca suavemente
        ampActual = Mathf.Lerp(ampActual, objetivo.x, Time.deltaTime * 5f);
        freqActual = Mathf.Lerp(freqActual, objetivo.y, Time.deltaTime * 5f);
        velActual = Mathf.Lerp(velActual, objetivo.z, Time.deltaTime * 2f);
    }
}