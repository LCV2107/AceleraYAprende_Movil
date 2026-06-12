using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class ButtonTextDropEffect : MonoBehaviour
{
    [Header("References")]
    public Button button;
    public TMP_Text tmpText;

    [Header("Drop Settings")]
    public float dropHeight = 200f;
    public float dropDuration = 0.3f;
    public float delayBetweenLetters = 0.05f;

    [Header("Shake Settings")]
    public float shakeDuration = 0.2f;
    public float shakeStrength = 5f;

    private TMP_TextInfo textInfo;
    private Vector3[][] originalVertices;
    private bool isAnimating = false;

    // 👉 Evento que se dispara al terminar toda la animación
    public event Action OnAnimationComplete;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(PlayDropEffect);
    }

    public void PlayDropEffect()
    {
        if (isAnimating) return;
        StartCoroutine(AnimateLetters());
    }

    private IEnumerator AnimateLetters()
    {
        isAnimating = true;

        tmpText.ForceMeshUpdate();
        textInfo = tmpText.textInfo;

        originalVertices = new Vector3[textInfo.meshInfo.Length][];
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            originalVertices[i] = new Vector3[textInfo.meshInfo[i].vertices.Length];
            Array.Copy(textInfo.meshInfo[i].vertices, originalVertices[i], textInfo.meshInfo[i].vertices.Length);
        }

        // Caída letra por letra
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            yield return StartCoroutine(AnimateLetter(i));
            yield return new WaitForSeconds(delayBetweenLetters);
        }

        isAnimating = false;

        // 👉 Avisar que la animación terminó
        OnAnimationComplete?.Invoke();
    }

    private IEnumerator AnimateLetter(int index)
    {
        int materialIndex = textInfo.characterInfo[index].materialReferenceIndex;
        int vertexIndex = textInfo.characterInfo[index].vertexIndex;

        Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

        Vector3[] initialPositions = new Vector3[4];
        for (int j = 0; j < 4; j++)
            initialPositions[j] = originalVertices[materialIndex][vertexIndex + j];

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

        // Temblor
        yield return StartCoroutine(ShakeLetter(index, shakeDuration, shakeStrength));
    }

    private IEnumerator ShakeLetter(int index, float duration, float strength)
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
            Vector3 offset = new Vector3(UnityEngine.Random.Range(-strength, strength), UnityEngine.Random.Range(-strength, strength), 0);

            for (int j = 0; j < 4; j++)
                vertices[vertexIndex + j] = basePositions[j] + offset;

            UpdateMesh(materialIndex, vertices);
            yield return null;
        }

        for (int j = 0; j < 4; j++)
            vertices[vertexIndex + j] = basePositions[j];

        UpdateMesh(materialIndex, vertices);
    }

    private void UpdateMesh(int materialIndex, Vector3[] vertices)
    {
        textInfo.meshInfo[materialIndex].mesh.vertices = vertices;
        tmpText.UpdateGeometry(textInfo.meshInfo[materialIndex].mesh, materialIndex);
    }
}
