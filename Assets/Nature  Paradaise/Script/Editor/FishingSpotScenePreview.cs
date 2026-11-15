using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
static class FishingSpotScenePreview
{
    static readonly Color FillColor = new(0.18f, 0.9f, 1f, 0.16f);
    static readonly Color EdgeShadow = new(0.02f, 0.13f, 0.19f, 0.95f);
    static readonly Color EdgeColor = new(0.42f, 0.96f, 1f, 1f);
    static GUIStyle labelStyle;

    static FishingSpotScenePreview() => SceneView.duringSceneGui += DrawSelection;

    static void DrawSelection(SceneView view)
    {
        if (Event.current.type != EventType.Repaint) return;
        foreach (GameObject selected in Selection.gameObjects)
        {
            if (!selected.scene.IsValid() || !selected.TryGetComponent(out FishingSpot spot)) continue;
            DrawZone(view, spot);
        }
    }

    static void DrawZone(SceneView view, FishingSpot spot)
    {
        Vector3 center = spot.LandingCenter;
        float radius = spot.Radius;
        CompareFunction previousDepthTest = Handles.zTest;
        Handles.zTest = CompareFunction.Always;
        try
        {
            using (new Handles.DrawingScope(Matrix4x4.identity))
            {
                Handles.color = FillColor;
                Handles.DrawSolidDisc(center, Vector3.up, radius);
                Handles.color = EdgeShadow;
                Handles.DrawWireDisc(center, Vector3.up, radius, 5f);
                Handles.color = EdgeColor;
                Handles.DrawWireDisc(center, Vector3.up, radius, 2.5f);
            }
        }
        finally
        {
            Handles.zTest = previousDepthTest;
        }

        labelStyle ??= new GUIStyle(EditorStyles.helpBox)
        {
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(7, 7, 4, 4)
        };
        Vector3 labelPosition = center + view.camera.transform.right * radius;
        Handles.Label(labelPosition, $"Diameter {radius * 2f:0.##} m", labelStyle);
    }
}
