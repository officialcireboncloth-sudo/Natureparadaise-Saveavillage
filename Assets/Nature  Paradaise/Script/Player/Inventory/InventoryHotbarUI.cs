using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hotbar empat slot ala Minecraft. Slot ini adalah empat slot pertama Inventory,
/// sehingga stack, perpindahan item, dan Save/Load memakai satu sumber data.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Inventory))]
public sealed class InventoryHotbarUI : MonoBehaviour
{
    [Header("Optional PNG Sprites")]
    [SerializeField] Sprite hotbarPanelSprite;
    [SerializeField] Sprite slotSprite;
    [SerializeField] Sprite selectedSlotSprite;

    [Header("Minimalist Fallback Colors")]
    [SerializeField] Color panelColor = new(0.025f, 0.032f, 0.045f, 0.92f);
    [SerializeField] Color slotColor = new(0.10f, 0.12f, 0.16f, 0.96f);
    [SerializeField] Color selectedColor = new(0.18f, 0.72f, 0.82f, 1f);

    Inventory inventory;
    PlayerToolHotbar toolHotbar;
    PlayerEatingSystem eatingSystem;
    readonly List<SlotView> views = new();
    int selectedIndex;

    public int SelectedIndex => selectedIndex;
    public ItemStack SelectedStack => inventory != null ? inventory.GetSlot(selectedIndex) : null;
    public ItemSO SelectedItem => SelectedStack?.item;
    public event Action<int> SelectionChanged;

    void Awake()
    {
        inventory = GetComponent<Inventory>();
        toolHotbar = GetComponent<PlayerToolHotbar>();
        if (toolHotbar == null) toolHotbar = gameObject.AddComponent<PlayerToolHotbar>();
        eatingSystem = GetComponent<PlayerEatingSystem>();
    }

    void OnEnable()
    {
        if (inventory != null) inventory.OnInventoryChanged += Refresh;
    }

    void OnDisable()
    {
        if (inventory != null) inventory.OnInventoryChanged -= Refresh;
    }

    void Start()
    {
        // Pastikan data inventory siap terlepas dari urutan Start antar-komponen.
        inventory.EnsureStartupLoadout();
        BuildUI();
        SelectSlot(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
        else if (Input.GetKeyDown(KeyCode.Alpha6)) SelectSlot(5);
        else if (Input.GetKeyDown(KeyCode.Alpha7)) SelectSlot(6);
        else if (Input.GetKeyDown(KeyCode.Alpha8)) SelectSlot(7);
    }

    public void SelectSlot(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, inventory.HotbarSlotCount - 1);
        EquipSelectedItem();
        Refresh();
        SelectionChanged?.Invoke(selectedIndex);
    }

    public void Refresh()
    {
        if (inventory == null || views.Count == 0) return;
        for (int i = 0; i < views.Count; i++)
        {
            SlotView view = views[i];
            bool selected = i == selectedIndex;
            view.Background.sprite = selected && selectedSlotSprite != null ? selectedSlotSprite : slotSprite;
            view.Background.type = view.Background.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            view.Background.color = selected ? selectedColor : slotColor;

            ItemStack stack = inventory.GetSlot(i);
            bool hasItem = stack?.item != null && stack.count > 0;
            view.Icon.enabled = hasItem && stack.item.icon != null;
            view.Icon.sprite = hasItem ? stack.item.icon : null;
            view.Label.text = hasItem
                ? $"{i + 1}  {stack.item.itemName}\nx{stack.count}"
                : $"{i + 1}\nEMPTY";
        }
        EquipSelectedItem();
    }

    void EquipSelectedItem()
    {
        ItemStack stack = inventory.GetSlot(selectedIndex);
        PlayerToolType selectedTool = PlayerToolType.None;
        if (stack?.item != null)
        {
            selectedTool = stack.item.equippedTool;
            if (selectedTool == PlayerToolType.None && stack.item.category == ItemCategory.Seed)
                selectedTool = PlayerToolType.Seed;
            if (stack.item.category == ItemCategory.Food)
                eatingSystem?.SetSelectedFood(stack.item);
        }
        toolHotbar.SelectTool(selectedTool);
    }

    void BuildUI()
    {
        int count = inventory.HotbarSlotCount;
        const float slotWidth = 126f;
        const float slotHeight = 82f;
        const float gap = 8f;
        float totalWidth = count * slotWidth + (count - 1) * gap + 20f;

        GameObject canvasObject = new("InventoryHotbarCanvas_Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 235;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform panel = CreateRect("HotbarPanel", canvasObject.transform);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 22f);
        panel.sizeDelta = new Vector2(totalWidth, slotHeight + 20f);
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.sprite = hotbarPanelSprite;
        panelImage.type = hotbarPanelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        panelImage.color = panelColor;

        for (int i = 0; i < count; i++)
        {
            int captured = i;
            RectTransform slot = CreateRect($"QuickSlot_{i + 1}", panel);
            slot.anchorMin = slot.anchorMax = new Vector2(0f, 0.5f);
            slot.pivot = new Vector2(0f, 0.5f);
            slot.anchoredPosition = new Vector2(10f + i * (slotWidth + gap), 0f);
            slot.sizeDelta = new Vector2(slotWidth, slotHeight);
            Image background = slot.gameObject.AddComponent<Image>();
            Button button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => SelectSlot(captured));

            RectTransform iconRect = CreateRect("Icon", slot);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(8f, 0f);
            iconRect.sizeDelta = new Vector2(52f, 52f);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TextMeshProUGUI label = CreateRect("Label", slot).gameObject.AddComponent<TextMeshProUGUI>();
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(64f, 5f);
            label.rectTransform.offsetMax = new Vector2(-5f, -5f);
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 13f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            views.Add(new SlotView(background, icon, label));
        }
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject target = new(name, typeof(RectTransform));
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        Inventory inventory = FindFirstObjectByType<Inventory>();
        if (inventory != null && inventory.GetComponent<InventoryHotbarUI>() == null)
            inventory.gameObject.AddComponent<InventoryHotbarUI>();
    }

    /// <summary>Cache background, icon, dan label satu slot hotbar.</summary>
    sealed class SlotView
    {
        public readonly Image Background;
        public readonly Image Icon;
        public readonly TMP_Text Label;
        public SlotView(Image background, Image icon, TMP_Text label) { Background = background; Icon = icon; Label = label; }
    }
}
