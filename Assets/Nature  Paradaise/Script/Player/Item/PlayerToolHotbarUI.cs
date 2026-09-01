using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerToolHotbar))]
/// <summary>UI prototipe untuk menampilkan tool aktif dari <see cref="PlayerToolHotbar"/>.</summary>
public sealed class PlayerToolHotbarUI : MonoBehaviour
{
    PlayerToolHotbar hotbar;
    readonly Dictionary<PlayerToolType, Image> slotBackgrounds = new();
    Color normalColor = new(0.08f, 0.1f, 0.14f, 0.88f);
    Color selectedColor = new(0.85f, 0.55f, 0.12f, 0.96f);

    void Awake() => hotbar = GetComponent<PlayerToolHotbar>();

    void OnEnable()
    {
        if (hotbar != null) hotbar.SelectionChanged += RefreshSelection;
    }

    void OnDisable()
    {
        if (hotbar != null) hotbar.SelectionChanged -= RefreshSelection;
    }

    void Start()
    {
        BuildUI();
        RefreshSelection(hotbar.SelectedTool);
    }

    void BuildUI()
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        GameObject canvasObject = new("ToolHotbarCanvas_Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = uiLayer >= 0 ? uiLayer : 5;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 210;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        PlayerToolType[] tools = { PlayerToolType.Hoe, PlayerToolType.Seed, PlayerToolType.WateringCan, PlayerToolType.Fertilizer, PlayerToolType.Sickle, PlayerToolType.Hammer };
        const float width = 112f;
        const float spacing = 10f;
        float totalWidth = tools.Length * width + (tools.Length - 1) * spacing;
        for (int i = 0; i < tools.Length; i++)
        {
            PlayerToolType tool = tools[i];
            GameObject slot = new($"Slot_{tool}", typeof(RectTransform), typeof(Image), typeof(Button));
            slot.layer = canvasObject.layer;
            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.SetParent(canvasObject.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(width, 72f);
            rect.anchoredPosition = new Vector2(-totalWidth * 0.5f + i * (width + spacing), 24f);
            Image image = slot.GetComponent<Image>();
            image.color = normalColor;
            slotBackgrounds.Add(tool, image);

            Button button = slot.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => hotbar.SelectTool(tool));

            TextMeshProUGUI label = CreateLabel(slot.transform);
            label.text = $"[{i + 1}]\n{PlayerToolHotbar.GetDisplayName(tool).ToUpperInvariant()}";
        }

        if (Application.isMobilePlatform)
        {
            GameObject use = new("UseToolButton", typeof(RectTransform), typeof(Image), typeof(Button));
            use.layer = canvasObject.layer;
            RectTransform rect = use.GetComponent<RectTransform>();
            rect.SetParent(canvasObject.transform, false);
            rect.anchorMin = rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.sizeDelta = new Vector2(170f, 86f);
            rect.anchoredPosition = new Vector2(-120f, -320f);
            Image image = use.GetComponent<Image>();
            image.color = selectedColor;
            Button button = use.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(hotbar.RequestUseTool);
            TextMeshProUGUI label = CreateLabel(use.transform);
            label.text = "USE TOOL";
        }
    }

    TextMeshProUGUI CreateLabel(Transform parent)
    {
        GameObject textObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 17f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    void RefreshSelection(PlayerToolType selected)
    {
        foreach (KeyValuePair<PlayerToolType, Image> pair in slotBackgrounds)
            pair.Value.color = pair.Key == selected ? selectedColor : normalColor;
    }

    // UI lama dipertahankan untuk kompatibilitas prefab, tetapi tidak lagi dibuat
    // otomatis. InventoryHotbarUI sekarang menjadi hotbar utama.
}
