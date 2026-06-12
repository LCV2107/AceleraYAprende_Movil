using UnityEngine;
using UnityEngine.SceneManagement;
public class CambioDeEscena : MonoBehaviour
{
    public void CargarEscena(string nombreDeEscena)
    {
        SceneManager.LoadScene(nombreDeEscena);
    }
}