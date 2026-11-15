using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One non-interactive status card projected above each active site, clear of 3D occlusion.</summary>
public sealed class ConstructionSiteStatus : MonoBehaviour
{
    RectTransform card;
    TextMeshProUGUI title, detail;
    Image fill;
    Camera view;
    Canvas canvas;
    Transform site;
    Vector3 point;
    float range;
    bool visible = true;
    string lastTitle, lastDetail;

    public void Configure(Vector3 position, ConstructionWorkerSettings settings)
    {
        point = position; range = settings != null ? settings.displayDistance : 42f;
        float width = settings != null ? settings.barPixelWidth : 260f;
        // Screen projection keeps walls and the builder from cutting through letters/progress.
        site = transform.parent; transform.SetParent(null, false);
        canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 40;
        var panel = new GameObject("StatusCard", typeof(RectTransform)); panel.transform.SetParent(transform, false);
        card = (RectTransform)panel.transform; card.sizeDelta = new Vector2(width, 92); card.pivot = new Vector2(.5f, 0);
        card.anchorMin = card.anchorMax = Vector2.zero;
        var background = panel.AddComponent<MainMenuRoundedImage>(); background.radius = 9; background.borderWidth = 0;
        background.color = new Color(.07f, .13f, .12f, .86f); background.raycastTarget = false;
        title = Text("Building", new Vector2(12, -8), new Vector2(width - 24, 24), 19);
        title.fontStyle = FontStyles.Bold;
        title.enableAutoSizing = true; title.fontSizeMin = 13; title.fontSizeMax = 19; title.textWrappingMode = TextWrappingModes.NoWrap;
        detail = Text("Status", new Vector2(12, -33), new Vector2(width - 24, 39), 15);
        var track = new GameObject("Progress", typeof(RectTransform), typeof(Image));
        track.transform.SetParent(card, false);
        var rect = (RectTransform)track.transform; rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(12, -79); rect.sizeDelta = new Vector2(width - 24, 5);
        var image = track.GetComponent<Image>(); image.color = new Color(1, 1, 1, .2f); image.raycastTarget = false;
        var progress = new GameObject("Fill", typeof(RectTransform), typeof(Image)); progress.transform.SetParent(track.transform, false);
        fill = progress.GetComponent<Image>(); fill.color = new Color(.61f, .8f, .44f); fill.raycastTarget = false;
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
    }
    TextMeshProUGUI Text(string name, Vector2 position, Vector2 size, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(card, false);
        var text = go.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize; text.color = Color.white; text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.Normal;
        var rect = text.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, 1);
        rect.anchoredPosition = new Vector2(0, position.y); rect.sizeDelta = size;
        return text;
    }
    public void SetStatus(string building, int level, string status, float progress)
    {
        string heading = $"{building} · Lv. {level} · {Mathf.Clamp01(progress):P0}";
        if (heading != lastTitle) { title.text = heading; lastTitle = heading; }
        if (status != lastDetail) { detail.text = status; lastDetail = status; }
        fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(progress), 1);
    }
    public void Hide() { visible = false; gameObject.SetActive(false); }
    void LateUpdate()
    {
        if (view == null || !view.isActiveAndEnabled) view = Camera.main;
        if (view == null || card == null) { if (canvas != null) canvas.enabled = false; return; }
        var screen = view.WorldToScreenPoint(point);
        var viewport = view.pixelRect;
        var distance = view.transform.position - point; distance.y = 0;
        bool show = visible && site != null && site.gameObject.activeInHierarchy && screen.z > 0 &&
            viewport.Contains(new Vector2(screen.x, screen.y)) && distance.sqrMagnitude <= range * range;
        canvas.enabled = show;
        // Keep letters readable at normal scroll/pinch zoom instead of shrinking with the world.
        float uiScale = Mathf.Clamp(Mathf.Min(view.pixelWidth / 960f, view.pixelHeight / 600f), .65f, 1.2f);
        canvas.scaleFactor = uiScale;
        card.anchoredPosition = new Vector2(screen.x, screen.y) / uiScale;
    }
}
