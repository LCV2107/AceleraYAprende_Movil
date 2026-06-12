using UnityEngine;

public class ExitButton : MonoBehaviour
{
    public void ExitGame()
    {
#if UNITY_WEBGL

        if (Screen.fullScreen)
        {
            Screen.fullScreen = false;
            Debug.Log("Saliendo de pantalla completa en WebGL.");
        }
        else
        {
            Debug.Log("WebGL: Ya no está en pantalla completa. No se realiza ninguna acción.");
        }

#elif UNITY_EDITOR

            Debug.Log("Deteniendo el modo Play en el Editor.");
            UnityEditor.EditorApplication.isPlaying = false;

#elif UNITY_IOS

            // iOS no expone API pública para minimizar; Application.Quit() termina
            // la app y regresa al home screen. El cierre es iniciado por el usuario,
            // lo cual es aceptado en la práctica por la App Store (guideline 2.4.5).
            Debug.Log("iOS: Cerrando app y regresando al home screen.");
            Application.Quit();

#else

            Debug.Log("Saliendo del juego (Build Standalone)...");
            Application.Quit();

#endif
    }
}