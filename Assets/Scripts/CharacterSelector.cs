using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CharacterSelector : MonoBehaviour
{
    [System.Serializable]
    public class CharacterButton
    {
        public string characterName;
        public Button button;
        public Image image;
        public Sprite baseSprite;
        public Sprite selectedSprite;

        [Header("Text Settings")]
        public TMP_Text label;          // Texto asociado
        public Color selectedColor = Color.yellow; // Color al seleccionar

        [HideInInspector] public Color originalColor;
        [HideInInspector] public RectTransform rectTransform;
        [HideInInspector] public Coroutine runningAnimation;
    }

    [Header("Characters")]
    public CharacterButton[] characters;

    [Header("Animation Settings")]
    public float scaleUp = 1.2f;       // Escala máxima al hacer click
    public float scaleDuration = 0.2f; // Velocidad del efecto de pop

    private CharacterButton selectedCharacter;

    private void Start()
    {
        foreach (var character in characters)
        {
            character.rectTransform = character.image.GetComponent<RectTransform>();
            character.image.sprite = character.baseSprite;

            // Escala inicial
            character.rectTransform.localScale = Vector3.one;

            // Guardar color original del texto
            if (character.label != null)
                character.originalColor = character.label.color;

            character.button.onClick.AddListener(() => OnCharacterClicked(character));
        }
    }

    private void OnCharacterClicked(CharacterButton clicked)
    {
        // Restaurar el sprite y color del anterior
        if (selectedCharacter != null && selectedCharacter != clicked)
        {
            selectedCharacter.image.sprite = selectedCharacter.baseSprite;

            if (selectedCharacter.label != null)
                selectedCharacter.label.color = selectedCharacter.originalColor;

            PlayScaleAnimation(selectedCharacter, 1f); // volver a escala base
        }

        // Cambiar sprite al seleccionado
        clicked.image.sprite = clicked.selectedSprite;

        // Cambiar color del texto
        if (clicked.label != null)
            clicked.label.color = clicked.selectedColor;

        selectedCharacter = clicked;

        // Animación "pop"
        PlayScaleAnimation(clicked, scaleUp, 1f);

        Debug.Log("Seleccionado: " + clicked.characterName);
    }

    private void PlayScaleAnimation(CharacterButton character, float upScale, float endScale = 1f)
    {
        // Si ya hay una animación corriendo en este personaje, detenerla
        if (character.runningAnimation != null)
            StopCoroutine(character.runningAnimation);

        // Lanzar nueva
        character.runningAnimation = StartCoroutine(AnimateScale(character.rectTransform, upScale, endScale, () =>
        {
            character.runningAnimation = null;
        }));
    }

    private IEnumerator AnimateScale(RectTransform rect, float upScale, float endScale, System.Action onFinish)
    {
        // Agrandar
        float t = 0;
        Vector3 start = rect.localScale;
        Vector3 target = Vector3.one * upScale;

        while (t < scaleDuration)
        {
            t += Time.deltaTime;
            rect.localScale = Vector3.Lerp(start, target, t / scaleDuration);
            yield return null;
        }

        // Encoger de vuelta
        t = 0;
        start = rect.localScale;
        target = Vector3.one * endScale;

        while (t < scaleDuration)
        {
            t += Time.deltaTime;
            rect.localScale = Vector3.Lerp(start, target, t / scaleDuration);
            yield return null;
        }

        rect.localScale = Vector3.one * endScale; // asegurar valor final exacto
        onFinish?.Invoke();
    }
}
