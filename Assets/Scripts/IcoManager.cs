using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class IcoManager : MonoBehaviour
{
    [System.Serializable]
    public class BotonTexto
    {
        public Button boton;          
        public GameObject contenedor; 
        [HideInInspector] public CanvasGroup cg; 
    }

    public BotonTexto[] botonesTextos;

    public float duracionFade = 1f;
    public float tiempoVisible = 10f;

    private BotonTexto textoActivo = null;   // El que está actualmente en pantalla
    private Coroutine rutinaActual = null;   // La corrutina en curso

    void Start()
    {
        foreach (var bt in botonesTextos)
        {
            if (bt.contenedor == null || bt.boton == null)
            {
                Debug.LogWarning("Un par Botón/Contenedor no está asignado.");
                continue;
            }

            bt.cg = bt.contenedor.GetComponent<CanvasGroup>();
            if (bt.cg == null) bt.cg = bt.contenedor.AddComponent<CanvasGroup>();

            bt.cg.alpha = 0f;
            bt.contenedor.SetActive(false);

            BotonTexto captura = bt;
            bt.boton.onClick.AddListener(() => MostrarTexto(captura));
        }
    }

    void MostrarTexto(BotonTexto nuevo)
    {
        if (rutinaActual != null)
        {
            StopCoroutine(rutinaActual); // cancelar la rutina anterior
            rutinaActual = null;
        }

        rutinaActual = StartCoroutine(ReemplazarRutina(nuevo));
    }

    IEnumerator ReemplazarRutina(BotonTexto nuevo)
    {
        // Si ya hay uno activo, desvanecerlo primero
        if (textoActivo != null && textoActivo != nuevo)
        {
            yield return StartCoroutine(Fade(textoActivo, 1f, 0f, duracionFade));
            textoActivo.contenedor.SetActive(false);
        }

        // Activar el nuevo
        textoActivo = nuevo;
        textoActivo.contenedor.SetActive(true);
        yield return StartCoroutine(Fade(textoActivo, 0f, 1f, duracionFade));

        // Mantener visible el tiempo configurado
        yield return new WaitForSeconds(tiempoVisible);

        // Desaparecer
        yield return StartCoroutine(Fade(textoActivo, 1f, 0f, duracionFade));
        textoActivo.contenedor.SetActive(false);
        textoActivo = null;
        rutinaActual = null;
    }

    IEnumerator Fade(BotonTexto bt, float inicio, float fin, float tiempo)
    {
        float t = 0f;
        while (t < tiempo)
        {
            t += Time.deltaTime;
            bt.cg.alpha = Mathf.Lerp(inicio, fin, t / tiempo);
            yield return null;
        }
        bt.cg.alpha = fin;
    }
}
