using UnityEngine;
using TMPro;
using System.Collections;

public class TextDropEffect : MonoBehaviour
{
    [Header("References")]
    public TMP_Text tmpText;
    public TMP_Text swapText; // Opcional

    [Header("Random Offset & Rotation")]
    // Estas variables ya no se usan si quieres que caiga ordenado,
    // pero las dejamos por si quieres volver a usarlas.
    public float randomYOffset = 0f; // Puesto a 0
    public float randomXOffset = 0f; // Puesto a 0
    public float randomRotation = 0f; // Puesto a 0

    [Header("Fall Animation")]
    public float fallStartYOffset = 150f; // Qué tan alto "encima" empiezan las letras
    public float fallDuration = 0.8f;     // Duración de la caída

    [Tooltip("El tiempo de espera entre la caída de cada letra")]
    public float delayBetweenLetters = 0.05f;

    public AnimationCurve fallCurve = new AnimationCurve(new Keyframe(0, 0, 0, 2), new Keyframe(1, 1, 0, 0)); // Curva para la caída (EaseOut)

    [Header("Tremble Effect")]
    public float trembleAmount = 1.5f;   // Magnitud del temblor
    public float trembleRotation = 1f; // Magnitud de la rotación del temblor

    [Header("Font Swap (Opcional)")]
    public TMP_FontAsset swappedFontAsset;

    private TMP_TextInfo textInfo;
    private Vector3[][] originalVertices;

    // Almacena las posiciones y rotaciones FINALES (ordenadas)
    private Vector3[] targetCharOffsets;
    private float[] targetCharRotations;

    // Almacena las posiciones y rotaciones ACTUALES (para la animación)
    private Vector3[] currentCharOffsets;
    private float[] currentCharRotations;

    private bool isTrembling = false;

    private void Start()
    {
        tmpText.ForceMeshUpdate();
        textInfo = tmpText.textInfo;

        // Inicializar arrays
        currentCharOffsets = new Vector3[textInfo.characterCount];
        currentCharRotations = new float[textInfo.characterCount];

        // 1. Calcula y guarda las posiciones finales (que serán 0)
        CacheOriginalVerticesAndTargets();

        // 2. Poner TODAS las letras en su posición inicial (arriba)
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            currentCharOffsets[i] = new Vector3(
                targetCharOffsets[i].x, // X final (0)
                targetCharOffsets[i].y + fallStartYOffset, // Y inicial (arriba)
                0f
            );
            currentCharRotations[i] = 0f; // Rotación inicial
        }

        // Configurar el texto de reemplazo si existe
        if (swapText != null)
        {
            swapText.text = tmpText.text;
            if (swappedFontAsset != null)
                swapText.font = swappedFontAsset;

            swapText.ForceMeshUpdate();
            swapText.maxVisibleCharacters = swapText.textInfo.characterCount;
        }

        // 3. Iniciar la SECUENCIA de caída (la nueva corrutina)
        StartCoroutine(StartFallSequence());
    }

    private void Update()
    {
        // 'Update' ahora se encarga de aplicar las transformaciones CADA frame.
        // Las corrutinas solo modificarán los arrays 'currentCharOffsets' y 'currentCharRotations'.

        if (isTrembling)
        {
            // Si está temblando, actualiza los arrays con valores de temblor
            ApplyTremble();
        }

        // Dibuja la malla con los valores actuales de los arrays
        ApplyDeformation(currentCharOffsets, currentCharRotations);
    }

    /// <summary>
    /// Guarda los vértices originales y calcula la posición/rotación final (ORDENADA).
    /// </summary>
    private void CacheOriginalVerticesAndTargets()
    {
        textInfo = tmpText.textInfo;
        originalVertices = new Vector3[textInfo.meshInfo.Length][];
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            originalVertices[i] = new Vector3[textInfo.meshInfo[i].vertices.Length];
            System.Array.Copy(textInfo.meshInfo[i].vertices, originalVertices[i], textInfo.meshInfo[i].vertices.Length);
        }

        targetCharOffsets = new Vector3[textInfo.characterCount];
        targetCharRotations = new float[textInfo.characterCount];

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            // La posición final es 0 (su sitio original)
            targetCharOffsets[i] = Vector3.zero;
            // La rotación final es 0
            targetCharRotations[i] = 0f;
        }
    }

    /// <summary>
    /// Coroutine que LANZA la animación de caída para cada letra, una por una.
    /// </summary>
    private IEnumerator StartFallSequence()
    {
        int visibleCount = 0;
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (textInfo.characterInfo[i].isVisible) visibleCount++;
        }

        if (visibleCount == 0)
        {
            isTrembling = true;
            yield break;
        }

        // Lanzar una coroutine por cada letra visible
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            // Empezar la animación individual para ESTA letra
            StartCoroutine(AnimateCharacterFall(i));

            // Esperar antes de lanzar la siguiente
            yield return new WaitForSeconds(delayBetweenLetters);
        }

        // Esperar a que la ÚLTIMA letra termine de caer
        // El tiempo que hay que esperar después de que el bucle termina es:
        // (Duración de la caída) - (El delay de la última letra)
        float timeToWait = fallDuration - delayBetweenLetters;

        if (timeToWait > 0)
        {
            yield return new WaitForSeconds(timeToWait);
        }

        // Ahora que todas han caído, activar el temblor
        isTrembling = true;
    }

    /// <summary>
    /// Coroutine que anima UNA SOLA letra desde arriba hasta su posición final.
    /// </summary>
    private IEnumerator AnimateCharacterFall(int i)
    {
        if (!textInfo.characterInfo[i].isVisible) yield break;

        float elapsedTime = 0f;

        // Coger la posición inicial (arriba) que pusimos en Start()
        Vector3 startCharOffset = currentCharOffsets[i];
        float startCharRotation = currentCharRotations[i]; // que es 0

        while (elapsedTime < fallDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = fallCurve.Evaluate(elapsedTime / fallDuration);

            // Interpolar y actualizar el valor de ESTA letra en el array
            currentCharOffsets[i] = Vector3.LerpUnclamped(startCharOffset, targetCharOffsets[i], t);
            currentCharRotations[i] = Mathf.LerpUnclamped(startCharRotation, targetCharRotations[i], t);

            yield return null; // Esperar al siguiente frame
        }

        // Asegurar que termina en la posición final exacta
        currentCharOffsets[i] = targetCharOffsets[i];
        currentCharRotations[i] = targetCharRotations[i];
    }

    /// <summary>
    /// Aplica un pequeño offset y rotación aleatorios (temblor) a la posición final.
    /// </summary>
    private void ApplyTremble()
    {
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            Vector3 trembleOffset = new Vector3(
                Random.Range(-trembleAmount, trembleAmount),
                Random.Range(-trembleAmount, trembleAmount),
                0f
            );
            float trembleRot = Random.Range(-trembleRotation, trembleRotation);

            // Aplicar el temblor SOBRE la posición final (target)
            currentCharOffsets[i] = targetCharOffsets[i] + trembleOffset;
            currentCharRotations[i] = targetCharRotations[i] + trembleRot;
        }
    }

    /// <summary>
    /// Aplica las transformaciones (offsets y rotaciones) a los vértices del texto.
    /// </summary>
    private void ApplyDeformation(Vector3[] offsets, float[] rotations)
    {
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int matIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertIndex = textInfo.characterInfo[i].vertexIndex;

            Vector3[] vertices = textInfo.meshInfo[matIndex].vertices;
            Vector3[] basePositions = new Vector3[4];

            for (int j = 0; j < 4; j++)
                basePositions[j] = originalVertices[matIndex][vertIndex + j];

            // Aplicar transformación (rotación y desplazamiento)
            Vector3 center = (basePositions[0] + basePositions[2]) * 0.5f;
            Quaternion rot = Quaternion.Euler(0, 0, rotations[i]);

            for (int j = 0; j < 4; j++)
            {
                Vector3 relative = basePositions[j] - center;
                vertices[vertIndex + j] = center + rot * relative + offsets[i];
            }
        }

        // Actualizar malla
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            textInfo.meshInfo[i].mesh.vertices = meshInfo.vertices;
            tmpText.UpdateGeometry(meshInfo.mesh, i);
        }
    }
}