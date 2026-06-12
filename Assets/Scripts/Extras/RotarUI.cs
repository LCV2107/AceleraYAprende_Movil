using UnityEngine;

public class RotarUI : MonoBehaviour
{
    public float velocidad = 150f;

    void Update()
    {
        // Rota sobre el eje Z (perfecto para elementos 2D/UI)
        transform.Rotate(0, 0, velocidad * Time.deltaTime);
    }
}