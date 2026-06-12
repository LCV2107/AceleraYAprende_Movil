using UnityEngine;

public class ManagerFloor : MonoBehaviour
{
    [Header("Prefabs de pisos")]
    public GameObject[] prefabsPisos;   // Lista de prefabs (Piso A, B, C)

    [Header("Background único (asignar en Inspector)")]
    public GameObject backgroundUnico;  // Tu background fijo

    [Header("Índice inicial")]
    public int indiceInicial = 0;

    [Header("Posición fija de los pisos")]
    public Vector3 posicionDestinoPiso = new Vector3(0f, -1f, 0f);

    [Header("Animación")]
    public float velocidadEntrada = 10f;
    public float offsetEntradaX = 20f;  // desde dónde entra el piso (derecha de la cámara)

    private GameObject pisoActual;
    private GameObject pisoQueEntra;
    private bool enTransicion = false;

    void Start()
    {
        // Instanciar piso inicial en su posición
        if (prefabsPisos.Length > 0 && indiceInicial >= 0 && indiceInicial < prefabsPisos.Length)
        {
            pisoActual = Instantiate(prefabsPisos[indiceInicial], posicionDestinoPiso, Quaternion.identity);
        }

        // Instanciar background único si no está en escena
        if (backgroundUnico != null && backgroundUnico.scene.rootCount == 0)
        {
            Instantiate(backgroundUnico, Vector3.zero, Quaternion.identity);
        }
    }

    // 🔹 Llamar este método desde un botón
    public void ReemplazarPorIndice(int indice)
    {
        if (enTransicion) return; // evitar doble cambio
        if (indice < 0 || indice >= prefabsPisos.Length) return;

        enTransicion = true;

        // Posición inicial fuera de cámara (derecha)
        Vector3 spawnPos = new Vector3(
            posicionDestinoPiso.x + offsetEntradaX,
            posicionDestinoPiso.y,
            posicionDestinoPiso.z
        );

        // Crear nuevo piso
        pisoQueEntra = Instantiate(prefabsPisos[indice], spawnPos, Quaternion.identity);
    }

    void Update()
    {
        if (pisoQueEntra != null)
        {
            // Mover el piso entrante hacia la posición destino
            pisoQueEntra.transform.position = Vector3.MoveTowards(
                pisoQueEntra.transform.position,
                posicionDestinoPiso,
                velocidadEntrada * Time.deltaTime
            );

            // Cuando llegue a destino
            if (Vector3.Distance(pisoQueEntra.transform.position, posicionDestinoPiso) < 0.01f)
            {
                // Destruir el piso anterior
                if (pisoActual != null)
                {
                    Destroy(pisoActual);
                }

                // Nuevo piso pasa a ser el actual
                pisoActual = pisoQueEntra;
                pisoQueEntra = null;
                enTransicion = false;
            }
        }
    }
}
