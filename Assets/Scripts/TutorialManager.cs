using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class TutorialManager : MonoBehaviour
{
    [Header("Botones")]
    public Button botonAbrir;
    public Button botonCerrar;

    [Header("Objetos del efecto")]
    public GameObject square;

    [Header("UI común (aparece junto al efecto)")]
    public Graphic[] elementosUIComunes;

    [Header("UI asociada a tags (por cada objeto con tag)")]
    public TagUIGroup[] gruposPorTag;

    [Header("UI permanente (siempre visibles, debajo del prioritario)")]
    public Graphic[] elementosSiempreVisibles;

    [Header("Elemento prioritario (siempre encima de todo)")]
    public Graphic elementoPrioritario;

    [Header("Botones que deben ocultarse durante el efecto")]
    public GameObject[] botonesADesactivar;

    [Header("Canvas cuyo contenido no debe ser interactivo durante el efecto")]
    public Canvas[] canvasADesactivar;

    [Header("Configuración de animación")]
    [Tooltip("Tiempo independiente que tarda el elemento en hacer el escalado inicial")]
    public float duracionEntrada = 0.5f;
    [Tooltip("Duración de la animación de salida (escalar de tamaño original a 0)")]
    public float duracionSalida = 0.3f;
    [Tooltip("Segundos independientes que cada elemento permanece visible antes de pasar al siguiente")]
    public float tiempoVisualizacion = 10f;

    // ──────────────────────────────────────────────
    // Estado interno
    // ──────────────────────────────────────────────
    private List<Selectable> componentesDesactivados = new List<Selectable>();
    private List<Canvas> canvasTemporales = new List<Canvas>();
    private List<Canvas> canvasSiempreVisibles = new List<Canvas>();
    private Canvas canvasPrioritario;
    private Dictionary<SpriteRenderer, int> ordenOriginal = new Dictionary<SpriteRenderer, int>();
    private bool activo = false;

    // Secuencia de pasos del tutorial
    private List<TutorialStep> pasos = new List<TutorialStep>();
    private int pasoActual = -1;
    private Coroutine corrutinaTutorial = null;
    private bool esperandoInput = false;
    private bool saltarPaso = false;
    private bool cancelarTodo = false;

    // Escalas originales de cada Graphic
    private Dictionary<Graphic, Vector3> escalasOriginales = new Dictionary<Graphic, Vector3>();

    // ──────────────────────────────────────────────
    private class TutorialStep
    {
        public List<Graphic> elementos = new List<Graphic>();
    }

    // ──────────────────────────────────────────────
    private void Start()
    {
        if (square != null)
            square.SetActive(false);

        foreach (var ui in elementosUIComunes)
        {
            if (ui != null)
            {
                escalasOriginales[ui] = ui.transform.localScale;
                ui.gameObject.SetActive(false);
            }
        }

        foreach (var grupo in gruposPorTag)
            foreach (var ui in grupo.elementosUI)
                if (ui != null)
                {
                    escalasOriginales[ui] = ui.transform.localScale;
                    ui.gameObject.SetActive(false);
                }

        if (botonCerrar != null)
            botonCerrar.gameObject.SetActive(false);

        ConfigurarSiempreVisibles();
        ConfigurarPrioritario();

        if (botonAbrir != null)
            botonAbrir.onClick.AddListener(Abrir);
        if (botonCerrar != null)
            botonCerrar.onClick.AddListener(CerrarManual);
    }

    private void ConfigurarSiempreVisibles()
    {
        foreach (var ui in elementosSiempreVisibles)
        {
            if (ui == null) continue;
            ui.gameObject.SetActive(true);
            Canvas c = ObtenerOCrearCanvas(ui.gameObject, 999);
            if (!canvasSiempreVisibles.Contains(c))
                canvasSiempreVisibles.Add(c);
        }
    }

    private void ConfigurarPrioritario()
    {
        if (elementoPrioritario == null) return;
        elementoPrioritario.gameObject.SetActive(true);
        canvasPrioritario = ObtenerOCrearCanvas(elementoPrioritario.gameObject, 1000);
    }

    private void Update()
    {
        if (!activo) return;

        bool tapDetectado = false;

        if (Input.GetMouseButtonDown(0))
            tapDetectado = true;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            tapDetectado = true;

        if (tapDetectado)
        {
            if (esperandoInput)
                saltarPaso = true;
            else
                cancelarTodo = true;
        }
    }

    private void Abrir()
    {
        if (activo) return;
        activo = true;

        foreach (var b in botonesADesactivar)
            if (b != null) b.SetActive(false);

        DesactivarInteractividadCanvas();

        if (square != null)
        {
            square.SetActive(true);
            SpriteRenderer sr = square.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 100;
        }

        SubirObjetosPrioritarios();
        ConstruirPasos();

        cancelarTodo = false;
        saltarPaso = false;
        corrutinaTutorial = StartCoroutine(EjecutarTutorial());
    }

    private void ConstruirPasos()
    {
        pasos.Clear();

        // YA NO agregamos la UI común aquí como un "paso" que cause demoras.
        // Solo construimos los pasos de los elementos importantes (Tags).
        foreach (var grupo in gruposPorTag)
        {
            GameObject[] encontrados = GameObject.FindGameObjectsWithTag(grupo.tagAsociado);
            if (encontrados.Length > 0)
            {
                var paso = new TutorialStep();
                foreach (var ui in grupo.elementosUI)
                    if (ui != null) paso.elementos.Add(ui);
                if (paso.elementos.Count > 0) pasos.Add(paso);
            }
        }
    }

    private IEnumerator EjecutarTutorial()
    {
        // 1. Animar la entrada de la UI común en PARALELO (se queda visible todo el tutorial)
        List<Graphic> comunesList = new List<Graphic>();
        foreach (var ui in elementosUIComunes) if (ui != null) comunesList.Add(ui);

        if (comunesList.Count > 0)
        {
            foreach (var ui in comunesList) PrepararElemento(ui, 105);
            StartCoroutine(AnimarEntrada(comunesList));
        }

        // 2. Ejecutar los pasos del tutorial (elementos asociados a tags)
        for (int i = 0; i < pasos.Count; i++)
        {
            if (cancelarTodo) break;

            pasoActual = i;
            TutorialStep paso = pasos[i];

            foreach (var ui in paso.elementos)
                if (ui != null) PrepararElemento(ui, 105);

            // La animación ahora solo dura lo que dicta duracionEntrada (0.5s)
            yield return StartCoroutine(AnimarEntrada(paso.elementos));

            if (cancelarTodo) break;

            esperandoInput = true;
            saltarPaso = false;
            float tiempoTranscurrido = 0f;

            // Espera el tiempoVisualizacion independiente (ej: 10s) o hasta que el usuario toque
            while (tiempoTranscurrido < tiempoVisualizacion)
            {
                if (cancelarTodo || saltarPaso) break;
                tiempoTranscurrido += Time.unscaledDeltaTime;
                yield return null;
            }

            esperandoInput = false;
            if (cancelarTodo) break;

            yield return StartCoroutine(AnimarSalida(paso.elementos));

            foreach (var ui in paso.elementos)
                if (ui != null) ui.gameObject.SetActive(false);
        }

        // 3. Al terminar todo, ocultamos suavemente la UI común
        if (!cancelarTodo && comunesList.Count > 0)
        {
            yield return StartCoroutine(AnimarSalida(comunesList));
        }

        Cerrar();
    }

    // ──────────────────────────────────────────────
    // Animaciones
    // ──────────────────────────────────────────────
    private void PrepararElemento(Graphic ui, int orden)
    {
        ui.transform.localScale = Vector3.zero;
        ui.gameObject.SetActive(true);
        AsegurarCanvasTemporal(ui, orden);
    }

    private IEnumerator AnimarEntrada(List<Graphic> elementos)
    {
        float tiempo = 0f;
        List<Vector3> escalasDestino = new List<Vector3>();
        foreach (var ui in elementos)
        {
            Vector3 destino = escalasOriginales.ContainsKey(ui) ? escalasOriginales[ui] : Vector3.one;
            escalasDestino.Add(destino);
        }

        while (tiempo < duracionEntrada)
        {
            if (cancelarTodo) break;
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / duracionEntrada));
            for (int i = 0; i < elementos.Count; i++)
                if (elementos[i] != null)
                    elementos[i].transform.localScale = Vector3.LerpUnclamped(Vector3.zero, escalasDestino[i], t);
            yield return null;
        }

        for (int i = 0; i < elementos.Count; i++)
            if (elementos[i] != null)
                elementos[i].transform.localScale = escalasDestino[i];
    }

    private IEnumerator AnimarSalida(List<Graphic> elementos)
    {
        float tiempo = 0f;
        List<Vector3> escalasOrigen = new List<Vector3>();
        foreach (var ui in elementos)
        {
            Vector3 origen = escalasOriginales.ContainsKey(ui) ? escalasOriginales[ui] : Vector3.one;
            escalasOrigen.Add(origen);
        }

        while (tiempo < duracionSalida)
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / duracionSalida));
            for (int i = 0; i < elementos.Count; i++)
                if (elementos[i] != null)
                    elementos[i].transform.localScale = Vector3.LerpUnclamped(escalasOrigen[i], Vector3.zero, t);
            yield return null;
        }

        for (int i = 0; i < elementos.Count; i++)
            if (elementos[i] != null)
                elementos[i].transform.localScale = Vector3.zero;
    }

    // ──────────────────────────────────────────────
    // CERRAR
    // ──────────────────────────────────────────────
    private void Cerrar()
    {
        activo = false;
        esperandoInput = false;
        saltarPaso = false;
        cancelarTodo = false;

        foreach (var b in botonesADesactivar)
            if (b != null) b.SetActive(true);

        ActivarInteractividadCanvas();

        foreach (var ui in elementosUIComunes)
        {
            if (ui == null) continue;
            ui.gameObject.SetActive(false);
            if (escalasOriginales.ContainsKey(ui))
                ui.transform.localScale = escalasOriginales[ui];
        }

        foreach (var grupo in gruposPorTag)
            foreach (var ui in grupo.elementosUI)
            {
                if (ui == null) continue;
                ui.gameObject.SetActive(false);
                if (escalasOriginales.ContainsKey(ui))
                    ui.transform.localScale = escalasOriginales[ui];
            }

        foreach (Canvas c in new List<Canvas>(canvasTemporales))
        {
            if (c == null) continue;
            foreach (var r in c.GetComponents<GraphicRaycaster>())
                if (r != null) Destroy(r);
            Destroy(c);
        }
        canvasTemporales.Clear();

        foreach (var kvp in ordenOriginal)
            if (kvp.Key != null)
                kvp.Key.sortingOrder = kvp.Value;
        ordenOriginal.Clear();

        if (square != null)
            square.SetActive(false);

        if (botonCerrar != null)
            botonCerrar.gameObject.SetActive(false);

        ReforzarOrdenes();
    }

    private void CerrarManual()
    {
        if (!activo) return;
        cancelarTodo = true;

        if (corrutinaTutorial != null)
        {
            StopCoroutine(corrutinaTutorial);
            corrutinaTutorial = null;
        }

        foreach (var ui in elementosUIComunes)
            if (ui != null) ui.gameObject.SetActive(false);
        foreach (var grupo in gruposPorTag)
            foreach (var ui in grupo.elementosUI)
                if (ui != null) ui.gameObject.SetActive(false);

        Cerrar();
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────
    private void DesactivarInteractividadCanvas()
    {
        componentesDesactivados.Clear();

        foreach (Canvas c in canvasADesactivar)
        {
            if (c == null) continue;
            Selectable[] elementos = c.GetComponentsInChildren<Selectable>(true);
            foreach (Selectable s in elementos)
            {
                if (s == null) continue;
                if (botonCerrar != null && s.gameObject == botonCerrar.gameObject) continue;
                if (s.interactable)
                {
                    s.interactable = false;
                    componentesDesactivados.Add(s);
                }
            }
        }
    }

    private void ActivarInteractividadCanvas()
    {
        foreach (Selectable s in componentesDesactivados)
            if (s != null) s.interactable = true;
        componentesDesactivados.Clear();
    }

    private void SubirObjetosPrioritarios()
    {
        ordenOriginal.Clear();

        string[] tagsCasas = { "CasaAdobe", "CasaAuto", "CasaNorma" };
        string[] tagsPisos = { "PisoDuro", "PisoIntermedio", "PisoBlando" };

        foreach (string tag in tagsCasas)
            foreach (GameObject go in GameObject.FindGameObjectsWithTag(tag))
            {
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr == null) continue;
                if (!ordenOriginal.ContainsKey(sr)) ordenOriginal.Add(sr, sr.sortingOrder);
                sr.sortingOrder = 103;
            }

        foreach (string tag in tagsPisos)
            foreach (GameObject go in GameObject.FindGameObjectsWithTag(tag))
            {
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr == null) continue;
                if (!ordenOriginal.ContainsKey(sr)) ordenOriginal.Add(sr, sr.sortingOrder);
                sr.sortingOrder = 104;
            }
    }

    private bool AsegurarCanvasTemporal(Graphic ui, int orden)
    {
        if (ui == null) return false;
        Canvas c = ui.GetComponent<Canvas>();
        bool creado = false;
        if (c == null)
        {
            c = ui.gameObject.AddComponent<Canvas>();
            creado = true;
        }
        c.overrideSorting = true;
        c.sortingOrder = orden;
        if (ui.GetComponent<GraphicRaycaster>() == null)
            ui.gameObject.AddComponent<GraphicRaycaster>();
        if (creado) canvasTemporales.Add(c);
        return creado;
    }

    private Canvas ObtenerOCrearCanvas(GameObject obj, int orden)
    {
        Canvas c = obj.GetComponent<Canvas>();
        if (c == null) c = obj.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = orden;
        if (obj.GetComponent<GraphicRaycaster>() == null)
            obj.AddComponent<GraphicRaycaster>();
        return c;
    }

    private void ReforzarOrdenes()
    {
        foreach (var c in canvasSiempreVisibles)
            if (c != null) { c.overrideSorting = true; c.sortingOrder = 999; }

        if (canvasPrioritario != null) { canvasPrioritario.overrideSorting = true; canvasPrioritario.sortingOrder = 1000; }

        foreach (var ui in elementosSiempreVisibles)
            if (ui != null && !ui.gameObject.activeSelf) ui.gameObject.SetActive(true);

        if (elementoPrioritario != null && !elementoPrioritario.gameObject.activeSelf)
            elementoPrioritario.gameObject.SetActive(true);
    }
}

[System.Serializable]
public class TagUIGroup
{
    public string tagAsociado;
    public Graphic[] elementosUI;
}