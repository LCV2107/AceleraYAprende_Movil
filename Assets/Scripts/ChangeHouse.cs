using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class CasaData
{
    public GameObject prefab;            // Prefab original
    public Sprite spriteRoto;            // Sprite cuando está roto
    public Sprite spriteDestrozado;      // Sprite cuando está destrozado

    public Toggle[] togglesRoto;         // Cualquier toggle que active "roto"
    public Toggle[] togglesDestrozado;   // Cualquier toggle que active "destrozado"
}

public class ChangeHouse : MonoBehaviour
{
    [Header("Casas disponibles")]
    public CasaData[] casas;

    [Header("Posiciones destino (una por prefab)")]
    public Vector2[] posicionesDestino;

    [Header("Animación")]
    public float velocidadSalida = 40f;
    public float alturaSpawn = 10f;
    public float velocidadCaida = 12f;

    private GameObject objetoActual;
    private CasaData casaActualData;
    private GameObject objetoQueCae;

    private bool moverActual = false;
    private bool caerNuevo = false;
    private bool enTransicion = false;

    private Vector3 destinoSalida;
    private Vector2 destinoActual;

    void Start()
    {
        objetoActual = null;
    }

    public void ReemplazarPorIndice(int indiceCasa)
    {
        if (enTransicion) return;
        if (indiceCasa < 0 || indiceCasa >= casas.Length) return;

        CasaData data = casas[indiceCasa];
        destinoActual = posicionesDestino[indiceCasa];

        // Caso inicial: no hay objeto → cae directo
        if (objetoActual == null)
        {
            Vector3 spawnPos = new Vector3(destinoActual.x, destinoActual.y + alturaSpawn, 0);
            objetoQueCae = Instantiate(data.prefab, spawnPos, Quaternion.identity);
            casaActualData = data;
            caerNuevo = true;
            enTransicion = true;

            StartCoroutine(ActualizarSpriteDespues(data, objetoQueCae));
            return;
        }

        // Caso reemplazo: primero animar salida
        objetoQueCae = data.prefab;
        casaActualData = data;
        destinoSalida = new Vector3(-30f, objetoActual.transform.position.y, objetoActual.transform.position.z);

        moverActual = true;
        enTransicion = true;
    }

    void Update()
    {
        if (moverActual && objetoActual != null)
        {
            objetoActual.transform.position = Vector3.MoveTowards(
                objetoActual.transform.position,
                destinoSalida,
                velocidadSalida * Time.deltaTime
            );

            if (Vector3.Distance(objetoActual.transform.position, destinoSalida) < 0.1f)
            {
                Destroy(objetoActual);

                Vector3 spawnPos = new Vector3(destinoActual.x, destinoActual.y + alturaSpawn, 0);
                objetoQueCae = Instantiate(objetoQueCae, spawnPos, Quaternion.identity);

                moverActual = false;
                caerNuevo = true;

                StartCoroutine(ActualizarSpriteDespues(casaActualData, objetoQueCae));
            }
        }

        if (caerNuevo && objetoQueCae != null)
        {
            objetoQueCae.transform.position = Vector3.MoveTowards(
                objetoQueCae.transform.position,
                destinoActual,
                velocidadCaida * Time.deltaTime
            );

            if (Vector3.Distance(objetoQueCae.transform.position, destinoActual) < 0.01f)
            {
                caerNuevo = false;
                enTransicion = false;
                objetoActual = objetoQueCae;
            }
        }
    }

    // Cambia el sprite después de 7 segundos según toggles asignados
    private IEnumerator ActualizarSpriteDespues(CasaData data, GameObject obj)
    {
        yield return new WaitForSeconds(7f);

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        // Primero revisamos destrozado
        bool activarDestrozado = false;
        if (data.togglesDestrozado != null)
        {
            foreach (var t in data.togglesDestrozado)
            {
                if (t != null && t.isOn)
                {
                    activarDestrozado = true;
                    break;
                }
            }
        }

        if (activarDestrozado)
        {
            sr.sprite = data.spriteDestrozado;
            yield break;
        }

        // Luego revisamos roto
        bool activarRoto = false;
        if (data.togglesRoto != null)
        {
            foreach (var t in data.togglesRoto)
            {
                if (t != null && t.isOn)
                {
                    activarRoto = true;
                    break;
                }
            }
        }

        if (activarRoto)
            sr.sprite = data.spriteRoto;
        else
            sr.sprite = data.prefab.GetComponent<SpriteRenderer>().sprite; // original
    }
}
