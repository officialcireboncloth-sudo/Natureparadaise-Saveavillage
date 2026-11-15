using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fog map berbentuk grid tetapi digambar sebagai satu mesh. Cell yang pernah dijelajahi
/// tidak digambar lagi, sehingga tidak membuat ratusan GameObject UI.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MapFogOfWarGraphic : MaskableGraphic
{
    WorldMapService service;
    int columns;
    int rows;

    public void Configure(WorldMapService mapService, int gridColumns, int gridRows, Color fogColor)
    {
        service = mapService;
        columns = Mathf.Max(1, gridColumns);
        rows = Mathf.Max(1, gridRows);
        color = fogColor;
        raycastTarget = false;
        SetVerticesDirty();
    }

    public void RefreshFog() => SetVerticesDirty();

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        if (service == null || !service.FogOfWarEnabled) return;
        Rect rect = GetPixelAdjustedRect();
        float cellWidth = rect.width / columns;
        float cellHeight = rect.height / rows;
        Color32 tint = color;
        for (int y = 0; y < rows; y++)
        for (int x = 0; x < columns; x++)
        {
            if (service.IsFogCellExplored(service.CurrentMapId, x, y)) continue;
            float xMin = rect.xMin + x * cellWidth;
            float yMin = rect.yMin + y * cellHeight;
            int start = vertexHelper.currentVertCount;
            vertexHelper.AddVert(new Vector3(xMin, yMin), tint, Vector2.zero);
            vertexHelper.AddVert(new Vector3(xMin, yMin + cellHeight), tint, Vector2.up);
            vertexHelper.AddVert(new Vector3(xMin + cellWidth, yMin + cellHeight), tint, Vector2.one);
            vertexHelper.AddVert(new Vector3(xMin + cellWidth, yMin), tint, Vector2.right);
            vertexHelper.AddTriangle(start, start + 1, start + 2);
            vertexHelper.AddTriangle(start, start + 2, start + 3);
        }
    }
}
