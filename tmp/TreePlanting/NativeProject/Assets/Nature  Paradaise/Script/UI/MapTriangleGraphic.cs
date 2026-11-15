using UnityEngine;
using UnityEngine.UI;

/// <summary>Graphic UI segitiga ringan untuk arah player/waypoint tanpa texture tambahan.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MapTriangleGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        Rect area = GetPixelAdjustedRect();
        Color32 tint = color;
        vertexHelper.AddVert(new Vector3(area.center.x, area.yMax), tint, new Vector2(0.5f, 1f));
        vertexHelper.AddVert(new Vector3(area.xMin, area.yMin), tint, Vector2.zero);
        vertexHelper.AddVert(new Vector3(area.xMax, area.yMin), tint, Vector2.right);
        vertexHelper.AddTriangle(0, 1, 2);
    }
}
