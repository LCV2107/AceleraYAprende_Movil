using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIGradient : BaseMeshEffect
{
    public Color color1 = Color.cyan;
    public Color color2 = Color.magenta;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        Rect rect = graphic.rectTransform.rect;
        UIVertex vertex = new UIVertex();

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            // Genera el degradado de izquierda a derecha
            float t = (vertex.position.x - rect.xMin) / rect.width;
            vertex.color = Color.Lerp(color1, color2, t);
            vh.SetUIVertex(vertex, i);
        }
    }
}