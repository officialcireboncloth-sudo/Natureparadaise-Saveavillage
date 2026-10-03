using UnityEngine;
using UnityEngine.UI;

/// <summary>Rounded UI surface and thin rim, drawn with Unity vertices (no artwork required).</summary>
public sealed class MainMenuRoundedImage : Image
{
    public float radius = 18f;
    public float borderWidth = 1.5f;
    public Color borderColor = new Color(1f, 1f, 1f, 0.5f);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        float r = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * 0.5f);
        const int steps = 8;
        const int count = (steps + 1) * 4;
        Vector2 center = rect.center;
        vh.AddVert(center, color, Vector2.zero);
        for (int corner = 0; corner < 4; corner++)
        {
            Vector2 c = new Vector2(corner == 0 || corner == 3 ? rect.xMax - r : rect.xMin + r,
                corner < 2 ? rect.yMax - r : rect.yMin + r);
            for (int step = 0; step <= steps; step++)
            {
                float angle = (corner * 90f + step * 90f / steps) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vh.AddVert(c + direction * r, color, Vector2.zero);
            }
        }
        for (int i = 0; i < count; i++) vh.AddTriangle(0, 1 + i, 1 + (i + 1) % count);
        if (borderWidth <= 0f) return;
        for (int corner = 0; corner < 4; corner++)
        {
            Vector2 c = new Vector2(corner == 0 || corner == 3 ? rect.xMax - r : rect.xMin + r,
                corner < 2 ? rect.yMax - r : rect.yMin + r);
            for (int step = 0; step <= steps; step++)
            {
                float angle = (corner * 90f + step * 90f / steps) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Color rim = borderColor;
                rim.a *= color.a;
                vh.AddVert(c + direction * r, rim, Vector2.zero);
                vh.AddVert(c + direction * Mathf.Max(0f, r - borderWidth), rim, Vector2.zero);
            }
        }
        for (int i = 0; i < count; i++)
        {
            int a = 1 + count + i * 2, b = 1 + count + ((i + 1) % count) * 2;
            vh.AddTriangle(a, b, a + 1);
            vh.AddTriangle(a + 1, b, b + 1);
        }
    }
}
