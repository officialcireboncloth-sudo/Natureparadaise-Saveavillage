using UnityEngine;
using UnityEngine.UI;

/// <summary>Placeholder icon lingkaran satu draw call tanpa texture.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MapCircleGraphic : MaskableGraphic
{
    [SerializeField, Range(8, 32)] int segments = 20;

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        Color32 tint = color;
        vertexHelper.AddVert(center, tint, new Vector2(0.5f, 0.5f));
        for (int index = 0; index <= segments; index++)
        {
            float angle = index * Mathf.PI * 2f / segments;
            Vector2 direction = new(Mathf.Sin(angle), Mathf.Cos(angle));
            vertexHelper.AddVert(center + direction * radius, tint, direction * 0.5f + Vector2.one * 0.5f);
            if (index > 0) vertexHelper.AddTriangle(0, index, index + 1);
        }
    }
}
