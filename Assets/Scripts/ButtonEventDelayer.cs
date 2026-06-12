using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class ButtonEventDelayer : MonoBehaviour
{
    [Header("Eventos")]
    public UnityEvent firstEvent;
    public UnityEvent delayedEvent;
    public float delay = 1f;

    private bool isRunning = false;

    public void ExecuteEvents()
    {
        if (!isRunning)
            StartCoroutine(RunEvents());
    }

    private IEnumerator RunEvents()
    {
        isRunning = true;

        if (firstEvent != null)
            firstEvent.Invoke();

        yield return new WaitForSeconds(delay);

        if (delayedEvent != null)
            delayedEvent.Invoke();

        isRunning = false; // vuelve a permitir clics
    }
}