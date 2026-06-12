using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/*
 * CLASE DE DATOS (¡MODIFICADA CON VOLUMEN!)
 */
[System.Serializable]
public class SubtitleLine
{
    public float time;

    [TextArea(2, 5)] // Hace más cómodo escribir texto largo en el inspector
    public string text;

    [Tooltip("Marca esta casilla si quieres que este texto aparezca en una nueva línea.")]
    public bool forceNewLine;

    // --- ¡NUEVA VARIABLE DE VOLUMEN! ---
    [Range(0f, 1f)] // Crea un slider de 0 a 1 en el Inspector
    [Tooltip("Volumen del AudioSource al llegar a este subtítulo (0 = silencio, 1 = máximo).")]
    public float volume = 1f;
    // --- FIN NUEVA VARIABLE ---
}


/*
 * CLASE PRINCIPAL (MONOBEHAVIOUR)
 */
public class SubtitleSync : MonoBehaviour
{
    // --- Referencias ---
    [Header("Componentes")]
    public AudioSource audioSource;
    public TextMeshProUGUI uiText;

    [Header("Diálogo (Fondo y Texto)")]
    public GameObject dialogueBoxObject;
    public float dialogueFadeDuration = 0.5f;
    private CanvasGroup dialogueCanvasGroup;

    [Header("Prefab de Introducción")]
    public GameObject introPrefab;
    public float introAppearDuration = 0.8f;

    [Header("Botón de Continuar")]
    public GameObject continueButton;
    public float buttonAppearDuration = 0.5f;

    [Header("Configuración de Escritura")]
    public float charsPerSecond = 20f;

    [Header("Datos de Subtítulos")]
    public SubtitleLine[] lines;

    // --- Control Interno ---
    private int currentLineIndex = 0;
    private Coroutine activeTypingCoroutine;
    private string currentFullText = "";
    private Vector3 introOriginalScale;
    private Vector3 buttonOriginalScale;

    void Start()
    {
        if (uiText != null) uiText.text = "";

        if (continueButton != null)
        {
            buttonOriginalScale = continueButton.transform.localScale;
            continueButton.SetActive(false);
            continueButton.transform.localScale = Vector3.zero;
        }

        if (introPrefab != null)
        {
            introOriginalScale = introPrefab.transform.localScale;
            introPrefab.SetActive(false);
            introPrefab.transform.localScale = Vector3.zero;
        }

        if (dialogueBoxObject != null)
        {
            dialogueCanvasGroup = dialogueBoxObject.GetComponent<CanvasGroup>();
            if (dialogueCanvasGroup == null)
            {
                dialogueCanvasGroup = dialogueBoxObject.AddComponent<CanvasGroup>();
            }
            dialogueCanvasGroup.alpha = 0f;
            dialogueCanvasGroup.gameObject.SetActive(true);
        }

        StartCoroutine(RunFullSequence());
    }

    IEnumerator RunFullSequence()
    {
        if (introPrefab != null)
        {
            yield return StartCoroutine(AnimateScaleIn(introPrefab, introAppearDuration, introOriginalScale));
        }

        if (dialogueCanvasGroup != null)
        {
            yield return StartCoroutine(AnimateFadeIn(dialogueCanvasGroup, dialogueFadeDuration));
        }

        yield return StartCoroutine(PlayDialogue());

        if (continueButton != null)
        {
            yield return StartCoroutine(AnimateScaleIn(continueButton, buttonAppearDuration, buttonOriginalScale));
        }
    }


    // --- CORRUTINA PlayDialogue (¡MODIFICADA CON VOLUMEN!) ---
    IEnumerator PlayDialogue()
    {
        currentLineIndex = 0;
        currentFullText = "";
        uiText.text = "";

        // Aseguramos que empiece con el volumen del primer subtítulo si existe
        if (lines.Length > 0)
        {
            audioSource.volume = lines[0].volume;
        }

        audioSource.Play();

        while (currentLineIndex < lines.Length)
        {
            SubtitleLine line = lines[currentLineIndex];
            float timeToShow = line.time;

            // Esperar hasta el momento exacto
            while (audioSource.time < timeToShow)
            {
                yield return null;
            }

            // --- APLICAR CAMBIO DE VOLUMEN ---
            // Al llegar al tiempo indicado, ajustamos el volumen del AudioSource
            audioSource.volume = line.volume;
            // ---------------------------------

            if (activeTypingCoroutine != null)
            {
                StopCoroutine(activeTypingCoroutine);
            }
            uiText.text = currentFullText;

            // Lógica de salto de línea
            string chunkToType;

            if (currentLineIndex == 0)
            {
                chunkToType = line.text;
            }
            else if (line.forceNewLine)
            {
                chunkToType = "\n" + line.text;
            }
            else
            {
                chunkToType = " " + line.text;
            }

            activeTypingCoroutine = StartCoroutine(TypeLine(chunkToType));

            currentLineIndex++;
        }

        while (activeTypingCoroutine != null)
        {
            yield return null;
        }
    }
    // --- FIN CORRUTINA MODIFICADA ---

    IEnumerator TypeLine(string textToType)
    {
        string textBeforeTyping = uiText.text;
        currentFullText = textBeforeTyping + textToType;

        if (charsPerSecond <= 0)
        {
            uiText.text = currentFullText;
            activeTypingCoroutine = null;
            yield break;
        }

        float timePerChar = 1.0f / charsPerSecond;
        float timer = 0f;
        int charIndex = 0;

        while (charIndex < textToType.Length)
        {
            timer += Time.deltaTime;
            while (timer >= timePerChar && charIndex < textToType.Length)
            {
                charIndex++;
                uiText.text = textBeforeTyping + textToType.Substring(0, charIndex);
                timer -= timePerChar;
            }
            yield return null;
        }

        uiText.text = currentFullText;
        activeTypingCoroutine = null;
    }


    IEnumerator AnimateScaleIn(GameObject targetObject, float duration, Vector3 targetScale)
    {
        Transform objTransform = targetObject.transform;
        objTransform.localScale = Vector3.zero;
        targetObject.SetActive(true);

        float timer = 0f;
        Vector3 startScale = Vector3.zero;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float norm = Mathf.Clamp01(timer / duration);
            float eased = EaseOutBack(norm);
            objTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);

            yield return null;
        }

        objTransform.localScale = targetScale;
    }

    IEnumerator AnimateFadeIn(CanvasGroup canvasGroup, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }


    private float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
    }


    public void StartDialogue()
    {
        StopAllCoroutines();

        if (uiText != null) { uiText.text = ""; }

        if (continueButton != null)
        {
            continueButton.SetActive(false);
            continueButton.transform.localScale = Vector3.zero;
        }
        if (introPrefab != null)
        {
            introPrefab.SetActive(false);
            introPrefab.transform.localScale = Vector3.zero;
        }

        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 0f;
        }

        StartCoroutine(RunFullSequence());
    }
}