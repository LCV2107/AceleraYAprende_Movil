using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public class BloqueTexto
{
    public float marcaDeTiempo;
    [TextArea(3, 10)] public string contenido;
    public bool iniciarEnRenglonNuevo;
    [Range(0f, 1f)] public float nivelVolumen = 1f;
}

public class StartAudio : MonoBehaviour
{
    public AudioSource reproductor;
    public TextMeshProUGUI visualizadorTexto;
    public float caracteresPorSegundo = 30f;
    public float esperaTrasFinalizar = 2.0f;
    public string nombreEscenaSiguiente;
    public BloqueTexto[] listaDeSubtitulos;

    private int indiceActual = 0;
    private Coroutine procesoEscritura;

    void Start()
    {
        if (reproductor != null && visualizadorTexto != null)
        {
            visualizadorTexto.text = "";
            // Aseguramos que el AudioSource sea audible antes de empezar
            StartCoroutine(GestionarNarracion());
        }
    }

    IEnumerator GestionarNarracion()
    {
        yield return new WaitForSeconds(0.5f);

        indiceActual = 0;

        // 1. Configurar volumen inicial ANTES de dar Play
        if (listaDeSubtitulos.Length > 0)
            reproductor.volume = listaDeSubtitulos[0].nivelVolumen;
        else
            reproductor.volume = 1f;

        // 2. Iniciar el audio
        reproductor.Play();

        while (indiceActual < listaDeSubtitulos.Length)
        {
            BloqueTexto bloque = listaDeSubtitulos[indiceActual];

            // Esperar al momento exacto
            while (reproductor.time < bloque.marcaDeTiempo && reproductor.isPlaying)
            {
                yield return null;
            }

            // Actualizar volumen y texto
            reproductor.volume = bloque.nivelVolumen;

            if (procesoEscritura != null) StopCoroutine(procesoEscritura);

            string prefijo = (indiceActual > 0) ? (bloque.iniciarEnRenglonNuevo ? "\n" : " ") : "";
            procesoEscritura = StartCoroutine(EscribirLetras(prefijo + bloque.contenido));

            indiceActual++;
        }

        // Esperar final
        while (reproductor.isPlaying) yield return null;
        yield return new WaitForSeconds(esperaTrasFinalizar);

        if (!string.IsNullOrEmpty(nombreEscenaSiguiente))
            SceneManager.LoadScene(nombreEscenaSiguiente);
    }

    IEnumerator EscribirLetras(string textoAñadir)
    {
        string textoPrevio = visualizadorTexto.text;
        float intervalo = 1f / caracteresPorSegundo;

        for (int i = 0; i <= textoAñadir.Length; i++)
        {
            visualizadorTexto.text = textoPrevio + textoAñadir.Substring(0, i);
            yield return new WaitForSeconds(intervalo);
        }
        procesoEscritura = null;
    }
}