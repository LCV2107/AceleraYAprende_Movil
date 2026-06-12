using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SimulationManager : MonoBehaviour
{
    [System.Serializable]
    public class PrefabPair
    {
        public GameObject prefabDefault;     // Ej: A1 o B1 (Idle)
        public GameObject prefabSimulacion;  // Ej: A2 o B2 (Shake/Dance)
        public Transform spawnPoint;         // Punto de aparición
    }

    [Header("Grupo A")]
    public PrefabPair grupoA;

    [Header("Grupo B")]
    public PrefabPair grupoB;

    [Header("Botones UI")]
    public Button botonSpawnA;
    public Button botonSpawnB;
    public Button botonSimular;

    [Header("Duración simulación")]
    [Range(1f, 30f)] 
    public float duracionSimulacion = 7f;

    private GameObject instanciaActual;
    private PrefabPair grupoActivo;
    private Coroutine rutinaSimulacion;

    void Start()
    {
        if (botonSpawnA != null) 
            botonSpawnA.onClick.AddListener(() => SpawnGrupo(grupoA));

        if (botonSpawnB != null) 
            botonSpawnB.onClick.AddListener(() => SpawnGrupo(grupoB));

        if (botonSimular != null) 
            botonSimular.onClick.AddListener(IniciarSimulacion);

        // 🔹 Al inicio, siempre empieza grupo A en su prefab default
        SpawnGrupo(grupoA);
    }

    public void SpawnGrupo(PrefabPair grupo)
    {
        // detener simulación si había
        if (rutinaSimulacion != null) 
            StopCoroutine(rutinaSimulacion);

        // destruir el actual
        if (instanciaActual != null)
        {
            Destroy(instanciaActual);
        }

        // instanciar el default del grupo
        instanciaActual = Instantiate(
            grupo.prefabDefault, 
            grupo.spawnPoint.position, 
            grupo.spawnPoint.rotation
        );

        grupoActivo = grupo;
    }

    public void IniciarSimulacion()
    {
        if (grupoActivo == null) return;

        if (rutinaSimulacion != null) 
            StopCoroutine(rutinaSimulacion);

        rutinaSimulacion = StartCoroutine(RutinaSimulacion());
    }

    private IEnumerator RutinaSimulacion()
    {
        // 🔹 destruir el default
        if (instanciaActual != null) 
            Destroy(instanciaActual);

        // 🔹 instanciar el prefab de simulación
        instanciaActual = Instantiate(
            grupoActivo.prefabSimulacion, 
            grupoActivo.spawnPoint.position, 
            grupoActivo.spawnPoint.rotation
        );

        yield return new WaitForSeconds(duracionSimulacion);

        // 🔹 al terminar, volver al prefab default
        if (instanciaActual != null) 
            Destroy(instanciaActual);

        instanciaActual = Instantiate(
            grupoActivo.prefabDefault, 
            grupoActivo.spawnPoint.position, 
            grupoActivo.spawnPoint.rotation
        );

        rutinaSimulacion = null;
    }
}
