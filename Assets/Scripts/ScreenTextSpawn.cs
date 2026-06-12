using UnityEngine;
using TMPro;
using System.Collections;

public class ScreenTextSpawn : MonoBehaviour
{
    [Header("References")]
    public TMP_Text tmpText;            // Texto TMP
    public GameObject targetObject;     // Objeto a vigilar en el inspector

    [Header("Timing")]
    public float waitAfterDestroy = 1f;     // Tiempo de espera tras destruirse el objeto
    public float dropHeight = 200f;         // Desde qué altura caen las letras
    public float dropDuration = 0.3f;       // Tiempo que tarda cada letra en caer
    public float delayBetweenLetters = 0.05f; // Retraso entre letras

    [Header("Shake")]
    public float shakeDuration = 0.2f;    // (Ya no se usa, pero se deja por compatibilidad del Inspector)
    public float shakeStrength = 5f;      // Intensidad del temblor
    public float shakeSpeed = 10f;      // <-- NUEVO: Velocidad del temblor suave

    [Header("Crack Effect")]
    public float crackDuration = 0.3f;      // Duración de la rajadura
    public float crackAngle = 15f;          // Ángulo de inclinación parte inferior
    public float crackDrop = 10f;           // Qué tanto baja la parte inferior

    private TMP_TextInfo textInfo;
    private Vector3[][] originalVertices;

    private bool triggered = false; // Para que se ejecute solo una vez

    private float perlinOffset; // <-- NUEVO: Para que el ruido perlin sea único

    private void Awake()
    {
        // Forzamos actualización
        tmpText.ForceMeshUpdate();
        textInfo = tmpText.textInfo;

        // Ocultar geometría de inicio
        tmpText.enabled = false;

        // Guardar posiciones originales
        originalVertices = new Vector3[textInfo.meshInfo.Length][];
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            originalVertices[i] = new Vector3[textInfo.meshInfo[i].vertices.Length];
            System.Array.Copy(textInfo.meshInfo[i].vertices, originalVertices[i], textInfo.meshInfo[i].vertices.Length);
        }

        // --- INICIO DE LA CORRECCIÓN ---
        // Después de guardar las posiciones, movemos la malla actual
        // a la posición inicial (arriba) para que esté lista.
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices; // La malla actual

            for (int j = 0; j < 4; j++)
            {
                // Mover los vértices actuales
                vertices[vertexIndex + j] = originalVertices[materialIndex][vertexIndex + j] + Vector3.up * dropHeight;
            }
        }

        // Actualizamos la geometría para que "recuerde" esta posición
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            if (textInfo.meshInfo[i].mesh != null && textInfo.meshInfo[i].vertexCount > 0)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
        // --- FIN DE LA CORRECCIÓN ---

        perlinOffset = Random.Range(0f, 100f); // <-- NUEVO: Asignamos un offset aleatorio
    }

    private void Update()
    {
        // Si el objeto asignado ya no existe y aún no hemos lanzado el efecto
        if (!triggered && targetObject == null)
        {
            triggered = true;
            StartCoroutine(WaitAndPlay());
        }
    }

    private IEnumerator WaitAndPlay()
    {
        yield return new WaitForSeconds(waitAfterDestroy);

        // 🔥 Activamos el texto y lo hacemos visible
        tmpText.enabled = true;
        tmpText.alpha = 1;

        StartCoroutine(AnimateLetters());
    }


    private IEnumerator AnimateLetters()
    {
        tmpText.ForceMeshUpdate();
        textInfo = tmpText.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            StartCoroutine(AnimateLetter(i));
            yield return new WaitForSeconds(delayBetweenLetters);
        }
    }

    private IEnumerator AnimateLetter(int index)
    {
        int materialIndex = textInfo.characterInfo[index].materialReferenceIndex;
        int vertexIndex = textInfo.characterInfo[index].vertexIndex;

        Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

        // Posiciones originales
        Vector3[] initialPositions = new Vector3[4];
        for (int j = 0; j < 4; j++)
            initialPositions[j] = originalVertices[materialIndex][vertexIndex + j];

        // Empezar desde más arriba
        Vector3[] startPositions = new Vector3[4];
        for (int j = 0; j < 4; j++)
            startPositions[j] = initialPositions[j] + Vector3.up * dropHeight;

        float t = 0;
        while (t < dropDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dropDuration);

            for (int j = 0; j < 4; j++)
                vertices[vertexIndex + j] = Vector3.Lerp(startPositions[j], initialPositions[j], p);

            UpdateMesh(materialIndex, vertices);
            yield return null;
        }

        // 💥 Efecto de rajadura
        yield return StartCoroutine(CrackLetter(index, crackDuration, crackAngle, crackDrop));

        // --- MODIFICADO ---
        // Temblor al llegar (ahora es infinito y ya no usa shakeDuration)
        yield return StartCoroutine(ShakeLetter(index, shakeStrength));
    }

    private IEnumerator CrackLetter(int index, float duration, float angle, float dropAmount)
    {
        int materialIndex = textInfo.characterInfo[index].materialReferenceIndex;
        int vertexIndex = textInfo.characterInfo[index].vertexIndex;

        Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
        Vector3[] basePositions = new Vector3[4];
        for (int j = 0; j < 4; j++)
            basePositions[j] = originalVertices[materialIndex][vertexIndex + j];

        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);

            // Mitad superior (0 y 1): se quedan fijas
            vertices[vertexIndex + 0] = basePositions[0];
            vertices[vertexIndex + 1] = basePositions[1];

            // Mitad inferior (2 y 3): baja y rota
            Quaternion rot = Quaternion.Euler(0, 0, Mathf.Lerp(0, -angle, p));
            Vector3 centerBottom = (basePositions[2] + basePositions[3]) * 0.5f;

            for (int j = 2; j <= 3; j++)
            {
                Vector3 offset = basePositions[j] - centerBottom;
                vertices[vertexIndex + j] = centerBottom + rot * offset + Vector3.down * (dropAmount * p);
            }

            UpdateMesh(materialIndex, vertices);
            yield return null;
        }

        // 🔧 Restaurar posiciones originales
        for (int j = 0; j < 4; j++)
            vertices[vertexIndex + j] = basePositions[j];

        UpdateMesh(materialIndex, vertices);
    }

    // --- SECCIÓN MODIFICADA ---
    // La corrutina ShakeLetter ahora es infinita y usa Ruido Perlin
    private IEnumerator ShakeLetter(int index, float strength)
    {
        int materialIndex = textInfo.characterInfo[index].materialReferenceIndex;
        int vertexIndex = textInfo.characterInfo[index].vertexIndex;

        Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
        Vector3[] basePositions = new Vector3[4];
        for (int j = 0; j < 4; j++)
            basePositions[j] = originalVertices[materialIndex][vertexIndex + j];

        // Offset único para esta letra, para que no tiemblen todas idénticas
        float letterOffset = perlinOffset + index * 10f;

        // Bucle infinito
        while (true)
        {
            // Usamos Ruido Perlin para un movimiento suave y continuo
            // Mathf.PerlinNoise devuelve 0..1, así que lo mapeamos a -1..1
            float xNoise = (Mathf.PerlinNoise(letterOffset, Time.time * shakeSpeed) - 0.5f) * 2f;
            float yNoise = (Mathf.PerlinNoise(letterOffset + 50f, Time.time * shakeSpeed) - 0.5f) * 2f; // +50 para que X e Y sean diferentes

            Vector3 offset = new Vector3(xNoise * strength, yNoise * strength, 0);

            for (int j = 0; j < 4; j++)
                vertices[vertexIndex + j] = basePositions[j] + offset;

            UpdateMesh(materialIndex, vertices);
            yield return null; // Esperar al siguiente frame
        }

        // El código de "Restaurar posición original" ya no es necesario
        // ni alcanzable, porque el bucle es infinito.
    }

    private void UpdateMesh(int materialIndex, Vector3[] vertices)
    {
        textInfo.meshInfo[materialIndex].mesh.vertices = vertices;
        tmpText.UpdateGeometry(textInfo.meshInfo[materialIndex].mesh, materialIndex);
    }
}