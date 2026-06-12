using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class RoundedButton : MonoBehaviour
{
    public float radius = 30f;

    Image img;
    RectTransform rect;

    void Start()
    {
        img = GetComponent<Image>();
        rect = GetComponent<RectTransform>();

        UpdateMaterial();
    }

    void UpdateMaterial()
    {
        Material mat = img.material;

        mat.SetFloat("_Radius", radius);
        mat.SetFloat("_Width", rect.rect.width);
        mat.SetFloat("_Height", rect.rect.height);
    }
}
