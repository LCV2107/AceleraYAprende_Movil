using UnityEngine;

public class ResolutionTester : MonoBehaviour
{
    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetKeyDown(KeyCode.F1)) Screen.SetResolution(1920, 1080, false);
        if (Input.GetKeyDown(KeyCode.F2)) Screen.SetResolution(1280, 720, false);
        if (Input.GetKeyDown(KeyCode.F3)) Screen.SetResolution(854, 480, false);
        if (Input.GetKeyDown(KeyCode.F4)) Screen.SetResolution(1024, 768, false);
#endif
    }
}