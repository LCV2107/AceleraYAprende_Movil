using UnityEngine;

public class Clicker : MonoBehaviour
{
    void OnMouseDown()
    {
        Debug.Log("¡Hiciste click en " + gameObject.name + "!");
    }
}