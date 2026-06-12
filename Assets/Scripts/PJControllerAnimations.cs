using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PJControllerAnimations : MonoBehaviour
{
    [Header("Animator del personaje (Prefab)")]
    public Animator animator;

    [Header("Animaciones")]
    [SerializeField] private string defaultAnimation = "Idle";   // animación fija a la que volver
    [SerializeField] private string specialAnimation = "Dance"; // animación especial

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        // Reproducir la animación fija al inicio
        animator.Play(defaultAnimation);
    }

    // Llamar desde un botón para iniciar la animación especial
    public void PlaySpecialAnimation()
    {
        animator.Play(specialAnimation);
    }

    // Llamar desde otro botón para volver a la animación fija
    public void ReturnToDefaultAnimation()
    {
        animator.Play(defaultAnimation);
    }
}