using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared glass surfaces for gameplay HUD. Artwork comes from existing item sprites.</summary>
public static class GameplayHUDStyle
{
    public static readonly Color Panel = new(.12f, .24f, .30f, .72f);
    public static readonly Color Slot = new(.20f, .31f, .36f, .72f);
    public static readonly Color Accent = new(.28f, .94f, .74f, 1f);

    public static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        var rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    public static MainMenuRoundedImage Surface(RectTransform rect, Color color, float radius = 12)
    {
        var image = rect.gameObject.AddComponent<MainMenuRoundedImage>();
        image.color = color; image.radius = radius;
        image.borderColor = new Color(.72f, .89f, 1f, .6f);
        image.raycastTarget = false;
        return image;
    }

    public static TMP_Text Text(string name, Transform parent, string value, float size, Vector2 min, Vector2 max)
    {
        var rect = Rect(name, parent, min, max);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset; text.text = value; text.fontSize = size;
        text.color = Color.white; text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        text.enableAutoSizing = true; text.fontSizeMin = 12; text.fontSizeMax = size;
        return text;
    }

    public static void Icon(Transform parent, MainMenuIcon.Kind kind, Color color, Vector2 min, Vector2 max)
    {
        var rect = Rect(kind + " Icon", parent, min, max);
        rect.gameObject.AddComponent<CanvasRenderer>();
        var icon = rect.gameObject.AddComponent<MainMenuIcon>(); icon.kind = kind;
        icon.color = color; icon.raycastTarget = false;
    }
}
