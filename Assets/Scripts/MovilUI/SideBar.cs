using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SideBar : MonoBehaviour
{
    [System.Serializable]
    public class BotonAnimado
    {
        public RectTransform boton;

        [Tooltip("Distancia a la que se moverá desde su posición original (Ej: X=150, Y=0)")]
        public Vector2 offsetMovimiento;

        [HideInInspector] public Vector2 posicionInicial;
        [HideInInspector] public Vector2 posicionDesplegada;
    }

    [System.Serializable]
    public class GrupoMenu
    {
        [Tooltip("El botón principal que abre/cierra los botones normales de este grupo")]
        public Button botonActivador;
        public List<BotonAnimado> botonesEnlistados = new List<BotonAnimado>();

        [HideInInspector] public bool estaDesplegado = false;
        [HideInInspector] public Coroutine animacionActual;
    }

    [System.Serializable]
    public class PanelEspecial
    {
        [Tooltip("El botón exclusivo que activa la animación de este panel")]
        public Button botonActivador;

        [Tooltip("El RectTransform del panel UI ancho")]
        public RectTransform panelRect;

        [Header("Movimiento Relativo")]
        [Tooltip("Distancia a la que se moverá el panel desde su posición original (Ej: X=-300, Y=0)")]
        public Vector2 offsetMovimiento;

        [HideInInspector] public Vector2 posicionInicial;
        [HideInInspector] public Vector2 posicionDesplegada;

        [Header("Escala Visual")]
        [Tooltip("Escala al estar escondido. Ajusta X para hacerlo más delgado (Ej: 0.1)")]
        public Vector3 escalaInicialThin = new Vector3(0.1f, 1f, 1f);
        [Tooltip("Escala al estar desplegado (Normalmente 1, 1, 1)")]
        public Vector3 escalaDesplegadaNormal = Vector3.one;

        [HideInInspector] public bool estaDesplegado = false;
        [HideInInspector] public Coroutine animacionActual;
    }

    [Header("Configuración Global")]
    public float velocidadAnimacion = 5f;

    [Header("Configuración de Colores")]
    public Color colorNormal = Color.white;
    public Color colorSeleccionado = new Color(1f, 0.976f, 0.875f, 1f);
    public Color colorSeleccionadoDisabled = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Color colorDisabled = new Color(1f, 0.976f, 0.875f, 1f);

    [Header("Interacción Externa")]
    [Tooltip("Arrastra aquí el botón que deshabilitará los colores y cerrará los menús al ser presionado")]
    public Button botonExternoDeshabilitador;

    [Tooltip("Arrastra aquí los componentes Image de otros paneles o botones extra. Al darles clic, SÓLO cerrarán los menús sin deshabilitar nada.")]
    public List<Image> elementosExternosExtra = new List<Image>();

    [Tooltip("Tiempo en segundos antes de que los botones vuelvan a funcionar (sólo aplica al botonExternoDeshabilitador)")]
    public float tiempoDeshabilitado = 7f;

    private bool estaDeshabilitadoExternamente = false;
    private Coroutine rutinaTemporizador;

    [Header("1. Grupos de Botones Normales")]
    public List<GrupoMenu> gruposDeMenu = new List<GrupoMenu>();

    [Header("2. Paneles Especiales Independientes")]
    public List<PanelEspecial> panelesEspeciales = new List<PanelEspecial>();

    void Start()
    {
        if (botonExternoDeshabilitador != null)
        {
            botonExternoDeshabilitador.onClick.AddListener(() => ActivarModoDeshabilitado(true));
        }

        foreach (var imagenExtra in elementosExternosExtra)
        {
            if (imagenExtra != null && imagenExtra.TryGetComponent<Button>(out Button botonExtra))
            {
                botonExtra.onClick.AddListener(() => CerrarTodosLosMenus());
            }
        }

        for (int i = 0; i < gruposDeMenu.Count; i++)
        {
            int indiceGrupo = i;
            if (gruposDeMenu[i].botonActivador != null)
            {
                gruposDeMenu[i].botonActivador.onClick.AddListener(() => AlternarGrupo(indiceGrupo));
            }

            foreach (var item in gruposDeMenu[i].botonesEnlistados)
            {
                if (item.boton != null)
                {
                    item.posicionInicial = item.boton.anchoredPosition;
                    item.posicionDesplegada = item.posicionInicial + item.offsetMovimiento;
                }
            }
        }

        for (int i = 0; i < panelesEspeciales.Count; i++)
        {
            int indicePanel = i;
            PanelEspecial panel = panelesEspeciales[i];

            if (panel.botonActivador != null)
            {
                panel.botonActivador.onClick.AddListener(() => AlternarPanelEspecial(indicePanel));
            }

            if (panel.panelRect != null)
            {
                panel.posicionInicial = panel.panelRect.anchoredPosition;
                panel.posicionDesplegada = panel.posicionInicial + panel.offsetMovimiento;

                panel.panelRect.anchoredPosition = panel.posicionInicial;
                panel.panelRect.localScale = panel.escalaInicialThin;
            }
        }

        ActualizarColoresYEstado();
    }

    public void ActivarModoDeshabilitado(bool deshabilitar)
    {
        estaDeshabilitadoExternamente = deshabilitar;

        if (rutinaTemporizador != null)
        {
            StopCoroutine(rutinaTemporizador);
            rutinaTemporizador = null;
        }

        if (deshabilitar)
        {
            CerrarTodosLosMenus();
            rutinaTemporizador = StartCoroutine(RutinaRestaurarEstado());
        }

        ActualizarColoresYEstado();
    }

    public void CerrarTodosLosMenus()
    {
        CerrarOtrosGruposYPaneles(null, null);
        ActualizarColoresYEstado();
    }

    private IEnumerator RutinaRestaurarEstado()
    {
        yield return new WaitForSeconds(tiempoDeshabilitado);

        estaDeshabilitadoExternamente = false;
        ActualizarColoresYEstado();
    }

    private void ActualizarColoresYEstado()
    {
        foreach (var grupo in gruposDeMenu)
        {
            if (grupo.botonActivador != null)
            {
                grupo.botonActivador.enabled = !estaDeshabilitadoExternamente;
                if (grupo.botonActivador.image != null)
                {
                    if (grupo.estaDesplegado)
                        grupo.botonActivador.image.color = estaDeshabilitadoExternamente ? colorSeleccionadoDisabled : colorSeleccionado;
                    else
                        grupo.botonActivador.image.color = estaDeshabilitadoExternamente ? colorDisabled : colorNormal;
                }
            }
        }

        foreach (var panel in panelesEspeciales)
        {
            if (panel.botonActivador != null)
            {
                panel.botonActivador.enabled = !estaDeshabilitadoExternamente;
                if (panel.botonActivador.image != null)
                {
                    if (panel.estaDesplegado)
                        panel.botonActivador.image.color = estaDeshabilitadoExternamente ? colorSeleccionadoDisabled : colorSeleccionado;
                    else
                        panel.botonActivador.image.color = estaDeshabilitadoExternamente ? colorDisabled : colorNormal;
                }
            }
        }

        foreach (var imagenExtra in elementosExternosExtra)
        {
            if (imagenExtra != null)
            {
                imagenExtra.color = estaDeshabilitadoExternamente ? colorDisabled : colorNormal;
                if (imagenExtra.TryGetComponent<Button>(out Button botonExtra))
                {
                    botonExtra.enabled = !estaDeshabilitadoExternamente;
                }
            }
        }
    }

    private void CerrarOtrosGruposYPaneles(GrupoMenu grupoIgnorado, PanelEspecial panelIgnorado)
    {
        foreach (var grupo in gruposDeMenu)
        {
            if (grupo != grupoIgnorado && grupo.estaDesplegado)
            {
                grupo.estaDesplegado = false;
                if (grupo.animacionActual != null) StopCoroutine(grupo.animacionActual);
                grupo.animacionActual = StartCoroutine(MoverGrupo(grupo));
            }
        }

        foreach (var panel in panelesEspeciales)
        {
            if (panel != panelIgnorado && panel.estaDesplegado)
            {
                panel.estaDesplegado = false;
                if (panel.animacionActual != null) StopCoroutine(panel.animacionActual);
                panel.animacionActual = StartCoroutine(MoverPanel(panel));
            }
        }
    }

    private void AlternarGrupo(int indice)
    {
        if (estaDeshabilitadoExternamente) return;

        GrupoMenu grupo = gruposDeMenu[indice];

        if (!grupo.estaDesplegado)
        {
            CerrarOtrosGruposYPaneles(grupo, null);
        }

        grupo.estaDesplegado = !grupo.estaDesplegado;
        ActualizarColoresYEstado();

        if (grupo.animacionActual != null) StopCoroutine(grupo.animacionActual);
        grupo.animacionActual = StartCoroutine(MoverGrupo(grupo));
    }

    private IEnumerator MoverGrupo(GrupoMenu grupo)
    {
        float progreso = 0f;
        Vector2[] posActuales = new Vector2[grupo.botonesEnlistados.Count];

        for (int i = 0; i < grupo.botonesEnlistados.Count; i++)
        {
            if (grupo.botonesEnlistados[i].boton != null)
                posActuales[i] = grupo.botonesEnlistados[i].boton.anchoredPosition;
        }

        while (progreso < 1f)
        {
            progreso += Time.deltaTime * velocidadAnimacion;
            float suavizado = Mathf.SmoothStep(0f, 1f, progreso);

            for (int i = 0; i < grupo.botonesEnlistados.Count; i++)
            {
                if (grupo.botonesEnlistados[i].boton == null) continue;
                Vector2 destino = grupo.estaDesplegado ? grupo.botonesEnlistados[i].posicionDesplegada : grupo.botonesEnlistados[i].posicionInicial;
                grupo.botonesEnlistados[i].boton.anchoredPosition = Vector2.Lerp(posActuales[i], destino, suavizado);
            }
            yield return null;
        }
    }

    private void AlternarPanelEspecial(int indice)
    {
        if (estaDeshabilitadoExternamente) return;

        PanelEspecial panel = panelesEspeciales[indice];

        if (!panel.estaDesplegado)
        {
            CerrarOtrosGruposYPaneles(null, panel);
        }

        panel.estaDesplegado = !panel.estaDesplegado;
        ActualizarColoresYEstado();

        if (panel.animacionActual != null) StopCoroutine(panel.animacionActual);
        panel.animacionActual = StartCoroutine(MoverPanel(panel));
    }

    private IEnumerator MoverPanel(PanelEspecial panel)
    {
        float progreso = 0f;
        Vector2 posActual = panel.panelRect.anchoredPosition;
        Vector3 escalaActual = panel.panelRect.localScale;

        Vector2 destinoPos = panel.estaDesplegado ? panel.posicionDesplegada : panel.posicionInicial;
        Vector3 destinoEscala = panel.estaDesplegado ? panel.escalaDesplegadaNormal : panel.escalaInicialThin;

        while (progreso < 1f)
        {
            progreso += Time.deltaTime * velocidadAnimacion;
            float suavizado = Mathf.SmoothStep(0f, 1f, progreso);

            panel.panelRect.anchoredPosition = Vector2.Lerp(posActual, destinoPos, suavizado);
            panel.panelRect.localScale = Vector3.Lerp(escalaActual, destinoEscala, suavizado);

            yield return null;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (HayMenusDesplegados())
            {
                if (ClickFueraDelMenu())
                {
                    CerrarTodosLosMenus();
                }
            }
        }
    }

    private bool HayMenusDesplegados()
    {
        foreach (var grupo in gruposDeMenu)
        {
            if (grupo.estaDesplegado) return true;
        }

        foreach (var panel in panelesEspeciales)
        {
            if (panel.estaDesplegado) return true;
        }

        return false;
    }

    private bool ClickFueraDelMenu()
    {
        if (EventSystem.current == null) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> resultados = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, resultados);

        if (resultados.Count == 0) return true;

        foreach (RaycastResult resultado in resultados)
        {
            if (EsParteDelMenu(resultado.gameObject.transform))
            {
                return false; 
            }
        }

        return true; 
    }

    private bool EsParteDelMenu(Transform objetoTocado)
    {
        foreach (var grupo in gruposDeMenu)
        {
            if (grupo.botonActivador != null && objetoTocado.IsChildOf(grupo.botonActivador.transform)) return true;

            foreach (var btnAnim in grupo.botonesEnlistados)
            {
                if (btnAnim.boton != null && objetoTocado.IsChildOf(btnAnim.boton)) return true;
            }
        }

        foreach (var panel in panelesEspeciales)
        {
            if (panel.botonActivador != null && objetoTocado.IsChildOf(panel.botonActivador.transform)) return true;
            if (panel.panelRect != null && objetoTocado.IsChildOf(panel.panelRect)) return true;
        }

        if (botonExternoDeshabilitador != null && objetoTocado.IsChildOf(botonExternoDeshabilitador.transform)) return true;

        foreach (var extra in elementosExternosExtra)
        {
            if (extra != null && objetoTocado.IsChildOf(extra.transform)) return true;
        }

        return false;
    }
}