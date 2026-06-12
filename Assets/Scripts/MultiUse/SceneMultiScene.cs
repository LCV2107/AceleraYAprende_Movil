using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMultiScene : MonoBehaviour
{
  
    public void ChangeScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("¡El nombre de la escena está vacío en el botón!");
        }
    }
}