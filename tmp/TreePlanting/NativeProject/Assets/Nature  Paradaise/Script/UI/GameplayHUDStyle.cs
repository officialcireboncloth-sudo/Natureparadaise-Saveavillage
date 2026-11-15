using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared glass surfaces for gameplay HUD. Artwork comes from existing item sprites.</summary>
public static class GameplayHUDStyle
{
    // Visual rules shared by HUD, inventory and every runtime modal.
    public static readonly Color Modal = new(.10f, .16f, .19f, .97f);
    public static readonly Color Card = new(.20f, .28f, .30f, .94f);
    public static readonly Color TextColor = new(.95f, .94f, .89f, 1f);
    public static readonly Color Muted = new(.70f, .76f, .75f, 1f);
    public const float CornerRadius = 12f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InstallVisualRules()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= ApplyScene;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += ApplyScene;
    }

    static void ApplyScene(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        foreach (var root in scene.GetRootGameObjects()) ApplyHierarchy(root);
    }

    public static void ApplyHierarchy(GameObject root)
    {
        foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
        {
            foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(true))
                Typography(text, text.enableAutoSizing ? text.fontSizeMax : text.fontSize);
            foreach (var button in canvas.GetComponentsInChildren<Button>(true)) ButtonStates(button);
        }
    }

    public static void Typography(TMP_Text text, float size)
    {
        if (size <= 0) return;
        // Do not shrink long content to tiny text; scroll areas own full descriptions.
        if (text.enableAutoSizing) text.fontSizeMin = Mathf.Min(size, Mathf.Max(14, size * .85f));
        if (text.textWrappingMode == TextWrappingModes.NoWrap && text.overflowMode == TextOverflowModes.Overflow)
            text.overflowMode = TextOverflowModes.Ellipsis;
    }

    public static void ButtonStates(Button button)
    {
        var colors = button.colors;
        colors.selectedColor = colors.normalColor;
        colors.fadeDuration = .12f;
        button.colors = colors;
    }

    // Backdrops cover the viewport; safe-area padding belongs to content only.
    public static void FullScreenBackground(Image image)
    {
        if (image == null) return;
        var canvas = image.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var rect = image.rectTransform;
        rect.SetParent(canvas.transform, false);
        rect.SetAsFirstSibling();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        image.type = Image.Type.Simple; image.preserveAspect = false;
        foreach (var effect in image.GetComponents<Shadow>()) effect.enabled = false;
        if (image is MainMenuRoundedImage rounded)
        { rounded.radius = 0; rounded.borderWidth = 0; rounded.SetVerticesDirty(); }
    }
    public static readonly Color Panel = new(.20f, .25f, .21f, .56f);
    public static readonly Color Slot = new(.28f, .33f, .28f, .46f);
    public static readonly Color Accent = new(.64f, .78f, .52f, 1f);

    public static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        var rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    public static MainMenuRoundedImage Surface(RectTransform rect, Color color, float radius = CornerRadius)
    {
        var image = rect.gameObject.AddComponent<MainMenuRoundedImage>();
        image.color = color; image.radius = radius;
        image.borderColor = Color.clear;
        image.borderWidth = 0f;
        image.raycastTarget = false;
        return image;
    }

    public static TMP_Text Text(string name, Transform parent, string value, float size, Vector2 min, Vector2 max)
    {
        var rect = Rect(name, parent, min, max);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset; text.text = value; text.fontSize = size;
        text.color = TextColor; text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        text.enableAutoSizing = true; text.fontSizeMin = Mathf.Min(size, Mathf.Max(14, size * .85f)); text.fontSizeMax = size;
        text.overflowMode = TextOverflowModes.Ellipsis;
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
