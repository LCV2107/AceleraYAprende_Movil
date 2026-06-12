using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Cinemachine; 

public class SystemManager : MonoBehaviour
{
    private static SystemManager instance;

    [Header("Configuración Base")]
    public float referenceAspect = 16f / 9f; // Relación de aspecto ideal (1.77)
    public Camera mainCamera;
    public CinemachineCamera virtualCam;

    [Header("Modo de Visualización")]
    [Tooltip("TRUE: La cámara hace zoom in/out para llenar la pantalla (Sin barras negras).\nFALSE: Usa barras negras (Letterbox).")]
    public bool expandirVista = false;

    [Header("Configuración Android/Web")]
    public int targetFPS = 60;

    [Header("Configuración Background")]
    public string backgroundTag = "Background";
    public GameObject backgroundObject;
    public bool estirarImagen = false;

    [Header("Configuración UI (Canvas)")]
    public string canvasTag = "Canvas";
    public Vector2 referenciaUI = new Vector2(1920, 1080);
    [Tooltip("Si es TRUE, la UI se mete dentro de las barras negras. Si es FALSE, flota sobre todo.")]
    public bool respetarBarrasNegras = true;

    // --- Variables Internas ---
    private Vector2 _lastScreenSize;
    private float _initialOrthoSize; // Para recordar el zoom original
    
    // Listas para guardar referencias en memoria (Caché)
    private List<CanvasScaler> _cachedScalers = new List<CanvasScaler>();
    private List<Canvas> _cachedCanvases = new List<Canvas>();

    void Awake()
    {
        // Singleton pattern
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        Application.targetFrameRate = targetFPS;
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    void Start()
    {
        // Guardamos el tamaño inicial de la cámara para cálculos de zoom
        if (virtualCam != null)
        {
             _initialOrthoSize = virtualCam.Lens.OrthographicSize;
        }
        else if (mainCamera != null)
        {
             _initialOrthoSize = mainCamera.orthographicSize;
        }

        // Primera inicialización
        InicializarReferencias();
        
        _lastScreenSize = new Vector2(Screen.width, Screen.height);
        ForzarActualizacion();
    }

    void Update()
    {
        // DETECCIÓN DE CAMBIO DE RESOLUCIÓN
        // Solo recalculamos si el tamaño de la ventana cambia (Optimización crítica para Web)
        if (Screen.width != _lastScreenSize.x || Screen.height != _lastScreenSize.y)
        {
            _lastScreenSize = new Vector2(Screen.width, Screen.height);
            ForzarActualizacion();
        }
    }

    void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        // Al cambiar de escena, buscamos las nuevas cámaras y canvas
        InicializarReferencias();
        ForzarActualizacion();
    }

    /// <summary>
    /// Busca y guarda referencias en listas. Operación pesada, solo llamar al inicio.
    /// </summary>
    void InicializarReferencias()
    {
        // 1. Cámara principal
        if (mainCamera == null) mainCamera = Camera.main;

        // 2. Cinemachine (Busca el primero activo)
        if (virtualCam == null) virtualCam = Object.FindFirstObjectByType<CinemachineCamera>();
        
        // Actualizar referencia de zoom base si cambiamos de cámara en nueva escena
        if (virtualCam != null) _initialOrthoSize = virtualCam.Lens.OrthographicSize;

        // 3. Background
        if (backgroundObject == null && !string.IsNullOrEmpty(backgroundTag))
        {
            backgroundObject = GameObject.FindGameObjectWithTag(backgroundTag);
        }

        // 4. Canvas (Limpiamos y volvemos a llenar la lista)
        _cachedScalers.Clear();
        _cachedCanvases.Clear();

        if (!string.IsNullOrEmpty(canvasTag))
        {
            GameObject[] foundCanvases = GameObject.FindGameObjectsWithTag(canvasTag);
            foreach (var obj in foundCanvases)
            {
                var scaler = obj.GetComponent<CanvasScaler>();
                var canvas = obj.GetComponent<Canvas>();
                
                if (scaler != null) _cachedScalers.Add(scaler);
                if (canvas != null) _cachedCanvases.Add(canvas);
            }
        }
    }

    /// <summary>
    /// Ejecuta los cálculos matemáticos de reescalado.
    /// </summary>
    public void ForzarActualizacion()
    {
        if (mainCamera == null) return;

        if (expandirVista)
        {
            AjustarExpandirCamara();
        }
        else
        {
            AjustarLetterboxFit();
        }

        AjustarFondo();
        AjustarCanvas();
    }

    // --- MODO 1: BARRAS NEGRAS (Letterbox) ---
    void AjustarLetterboxFit()
    {
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / referenceAspect;

        if (scaleHeight < 1.0f) // Pantalla más alta (Portrait / Cuadrada)
        {
            float inset = (1.0f - scaleHeight) / 2.0f;
            mainCamera.rect = new Rect(0, inset, 1, scaleHeight);
        }
        else // Pantalla más ancha (Ultra-wide)
        {
            float scaleWidth = 1.0f / scaleHeight;
            float inset = (1.0f - scaleWidth) / 2.0f;
            mainCamera.rect = new Rect(inset, 0, scaleWidth, 1);
        }
    }

    // --- MODO 2: EXPANDIR VISTA (Full Screen) ---
    void AjustarExpandirCamara()
    {
        // Usamos toda la pantalla
        mainCamera.rect = new Rect(0, 0, 1, 1);

        float windowAspect = (float)Screen.width / Screen.height;

        // Ajuste de Zoom para Cinemachine (Solo si es Ortográfica 2D)
        if (virtualCam != null && mainCamera.orthographic)
        {
            if (windowAspect < referenceAspect)
            {
                // Si la pantalla es estrecha, nos alejamos (Zoom Out) para mantener el ancho visible
                // Usamos _initialOrthoSize para no perder la referencia original
                virtualCam.Lens.OrthographicSize = _initialOrthoSize * (referenceAspect / windowAspect);
            }
            else
            {
                // Si la pantalla es ancha, mantenemos el zoom original (se verá más a los lados)
                virtualCam.Lens.OrthographicSize = _initialOrthoSize;
            }
        }
    }

    void AjustarFondo()
    {
        if (backgroundObject == null || mainCamera == null) return;

        SpriteRenderer sr = backgroundObject.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        // Reseteamos escala antes de calcular
        backgroundObject.transform.localScale = Vector3.one;

        float width = sr.sprite.bounds.size.x;
        float height = sr.sprite.bounds.size.y;

        // Altura del mundo vista por la cámara
        float worldScreenHeight = mainCamera.orthographicSize * 2f;
        
        // Si usamos Letterbox, ajustamos porque la cámara no ve toda la pantalla física
        if (!expandirVista && mainCamera.rect.height < 1.0f) 
        {
             worldScreenHeight = worldScreenHeight / mainCamera.rect.height;
        }
        
        float worldScreenWidth = worldScreenHeight * ((float)Screen.width / Screen.height);

        float scaleX = worldScreenWidth / width;
        float scaleY = worldScreenHeight / height;

        if (estirarImagen)
        {
            backgroundObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
        else
        {
            // Escala uniforme para cubrir todo (Cover)
            float maxScale = Mathf.Max(scaleX, scaleY);
            backgroundObject.transform.localScale = new Vector3(maxScale, maxScale, 1f);
        }
    }

    void AjustarCanvas()
    {
        float currentAspect = (float)Screen.width / Screen.height;
        bool isUltraWide = currentAspect > referenceAspect;

        // Iteramos sobre la lista en caché (Rápido)
        for (int i = 0; i < _cachedScalers.Count; i++)
        {
            if (_cachedScalers[i] == null) continue; // Protección por si se destruyó

            CanvasScaler scaler = _cachedScalers[i];
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenciaUI;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            // En pantallas anchas igualamos altura (1), en estrechas anchura (0)
            scaler.matchWidthOrHeight = isUltraWide ? 1f : 0f;
        }

        for (int i = 0; i < _cachedCanvases.Count; i++)
        {
            if (_cachedCanvases[i] == null) continue;

            Canvas canvas = _cachedCanvases[i];

            if (respetarBarrasNegras && !expandirVista && mainCamera != null)
            {
                // UI dentro de la cámara (respeta barras negras)
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = mainCamera;
                canvas.planeDistance = mainCamera.nearClipPlane + 0.1f;
            }
            else
            {
                // UI sobre todo (pantalla completa)
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }
    }

    void OnDestroy()
    {
        // Limpieza de eventos para evitar errores de memoria
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }
}