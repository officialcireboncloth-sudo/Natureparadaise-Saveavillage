using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Inventory))]
/// <summary>
/// Panel bag berbasis grid yang mendukung buka/tutup, sorting, icon, stack label,
/// drag-and-drop, serta pertukaran item dengan hotbar.
/// </summary>
public sealed class InventoryUI : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] KeyCode closeKey = KeyCode.Escape;
    [SerializeField] KeyCode sortKey = KeyCode.R;

    [Header("Grid")]
    [SerializeField] Vector2 slotSize = new(104f, 92f);
    [SerializeField] float spacing = 9f;

    [Header("Optional PNG Sprites")]
    [SerializeField] Sprite panelSprite;
    [SerializeField] Sprite slotSprite;
    [SerializeField] Sprite buttonSprite;

    Inventory inventory;
    PlayerEatingSystem eatingSystem;
    PlayerController movement;
    FarmingTool farmingTool;
    SeedTool seedTool;
    InventoryHotbarUI hotbarUI;
    GameObject canvasObject;
    GameObject panel;
    TMP_Text capacityText;
    readonly List<SlotView> slotViews = new();
    GameObject dragGhost;
    int draggedSlotIndex = -1;
    bool isOpen;
    bool farmingWasEnabled;
    bool seedWasEnabled;

    public bool IsOpen => isOpen;

    void Awake()
    {
        inventory = GetComponent<Inventory>();
        eatingSystem = GetComponent<PlayerEatingSystem>();
        movement = GetComponent<PlayerController>();
        farmingTool = GetComponent<FarmingTool>();
        seedTool = GetComponent<SeedTool>();
        hotbarUI = GetComponent<InventoryHotbarUI>();
        if (hotbarUI == null) hotbarUI = gameObject.AddComponent<InventoryHotbarUI>();
    }

    void OnEnable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged += Refresh;
    }

    void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;
        if (isOpen)
            SetOpen(false);
    }

    void OnDestroy()
    {
        // Canvas dibuat sebagai root runtime; bersihkan langsung saat scene berhenti
        // agar tidak tertinggal ketika Inventory sedang terbuka.
        if (canvasObject != null)
            DestroyImmediate(canvasObject);
        dragGhost = null;
    }

    void Start()
    {
        BuildUI();
        Refresh();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleInventory();
        else if (isOpen && Input.GetKeyDown(closeKey))
            SetOpen(false);

        if (isOpen && Input.GetKeyDown(sortKey))
            SortByType();
    }

    // Siap dipanggil dari Button mobile.
    public void ToggleInventory()
    {
        SetOpen(!isOpen);
    }

    public void SortByType()
    {
        inventory.SortByCategory();
        SaveLoadFeedback.Instance?.ShowMessage("Inventory diurutkan berdasarkan tipe");
    }

    void SetOpen(bool open)
    {
        if (panel == null)
            BuildUI();

        isOpen = open;
        panel.SetActive(open);

        if (open)
        {
            WorldInteractionPrompt.AcquireSuppression(this);
            farmingWasEnabled = farmingTool != null && farmingTool.enabled;
            seedWasEnabled = seedTool != null && seedTool.enabled;
            if (movement != null) movement.AcquireMovementLock(this);
            if (farmingTool != null) farmingTool.enabled = false;
            if (seedTool != null) seedTool.enabled = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Refresh();
        }
        else
        {
            WorldInteractionPrompt.ReleaseSuppression(this);
            if (movement != null) movement.ReleaseMovementLock(this);
            if (farmingTool != null) farmingTool.enabled = farmingWasEnabled;
            if (seedTool != null) seedTool.enabled = seedWasEnabled;
        }
    }

    void BuildUI()
    {
        if (panel != null)
            return;

        int uiLayer = LayerMask.NameToLayer("UI");
        canvasObject = new("BagInventoryCanvas_Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = uiLayer >= 0 ? uiLayer : 5;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panel = CreateUIObject("BagPanel", canvasObject.transform).gameObject;
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        int columns = inventory.BagColumnCount;
        int rows = inventory.BagRowCount;
        float gridWidth = columns * slotSize.x + (columns - 1) * spacing;
        float gridHeight = rows * slotSize.y + (rows - 1) * spacing;
        float hotbarWidth = inventory.HotbarSlotCount * slotSize.x + (inventory.HotbarSlotCount - 1) * spacing;
        float hotbarTop = -126f - gridHeight - 58f;
        float panelWidth = Mathf.Max(600f, Mathf.Max(gridWidth, hotbarWidth) + 44f);
        float panelHeight = 126f + gridHeight + 58f + slotSize.y + 28f;
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        Image panelImage = AddImage(panel, new Color(0.035f, 0.045f, 0.06f, 0.97f));
        ApplySprite(panelImage, panelSprite);

        TMP_Text title = CreateText(panel.transform, "Title", "BAG INVENTORY", 30f, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(22f, -16f), new Vector2(360f, 46f));

        capacityText = CreateText(panel.transform, "Capacity", string.Empty, 18f, FontStyles.Normal);
        capacityText.alignment = TextAlignmentOptions.TopRight;
        SetRect(capacityText.rectTransform, new Vector2(380f, -22f), new Vector2(186f, 34f));

        Button sortButton = CreateButton(panel.transform, "SortButton", "SORT TYPE [R]", new Vector2(22f, -68f), new Vector2(210f, 40f));
        sortButton.onClick.AddListener(SortByType);

        TMP_Text hint = CreateText(panel.transform, "Hint", "Drag item antar slot / ke hotbar  |  C: makan item terpilih", 14f, FontStyles.Normal);
        hint.color = new Color(0.72f, 0.78f, 0.85f, 1f);
        SetRect(hint.rectTransform, new Vector2(244f, -75f), new Vector2(322f, 34f));

        TMP_Text hotbarLabel = CreateText(panel.transform, "HotbarLabel", "HOTBAR — SLOT INI TAMPIL DI BAWAH LAYAR", 15f, FontStyles.Bold);
        hotbarLabel.color = new Color(0.35f, 0.82f, 0.9f, 1f);
        SetRect(hotbarLabel.rectTransform, new Vector2(22f, hotbarTop + 34f), new Vector2(panelWidth - 44f, 28f));

        for (int i = 0; i < inventory.Capacity; i++)
        {
            int index = i;
            bool isHotbar = i < inventory.HotbarSlotCount;
            int displayIndex = isHotbar ? i : i - inventory.HotbarSlotCount;
            int column = displayIndex % (isHotbar ? inventory.HotbarSlotCount : columns);
            int row = isHotbar ? 0 : displayIndex / columns;
            float startX = isHotbar ? 22f + (gridWidth - hotbarWidth) * 0.5f : 22f;
            float x = startX + column * (slotSize.x + spacing);
            float y = isHotbar ? hotbarTop : -126f - row * (slotSize.y + spacing);

            string slotName = isHotbar ? $"HotbarSlot_{i + 1}" : $"BagSlot_{displayIndex + 1}";
            Button button = CreateButton(panel.transform, slotName, string.Empty, new Vector2(x, y), slotSize, slotSprite);
            button.onClick.AddListener(() => OnSlotClicked(index));
            button.gameObject.AddComponent<InventorySlotDragHandler>().Initialize(this, index);
            Image icon = CreateUIObject("Icon", button.transform).gameObject.AddComponent<Image>();
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -7f);
            iconRect.sizeDelta = new Vector2(46f, 46f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TMP_Text label = CreateText(button.transform, "Label", "EMPTY", 12f, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.BottomLeft;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(7f, 5f);
            labelRect.offsetMax = new Vector2(-7f, -54f);

            slotViews.Add(new SlotView(button.GetComponent<Image>(), icon, label));
        }

        panel.SetActive(false);
    }

    void Refresh()
    {
        if (inventory == null)
            return;

        // Canvas inventory dibuat saat runtime dan dapat dibangun ulang ketika kapasitas tas
        // berubah. Jangan memakai cache komponen UI lama yang sudah dihancurkan oleh Unity.
        if (panel == null || capacityText == null || slotViews.Count != inventory.Capacity || !AreSlotViewsValid())
            RebuildUI();

        if (panel == null || capacityText == null || slotViews.Count != inventory.Capacity)
            return;

        int completeRows = inventory.MainCapacity / inventory.BagColumnCount;
        int partialRow = inventory.MainCapacity % inventory.BagColumnCount;
        string bagLayout = partialRow == 0
            ? $"{completeRows}x{inventory.BagColumnCount}"
            : $"{completeRows}x{inventory.BagColumnCount} + {partialRow}";
        capacityText.text = $"{inventory.UsedSlots}/{inventory.Capacity}  |  TAS {bagLayout}";
        for (int i = 0; i < slotViews.Count; i++)
        {
            SlotView view = slotViews[i];
            ItemStack stack = inventory.GetSlot(i);
            // Data hasil save/transaction lama dapat menyisakan container ItemStack tanpa
            // ItemSO. Secara visual kondisi itu diperlakukan sebagai slot kosong.
            if (stack == null || stack.item == null || stack.count <= 0)
            {
                view.Background.color = new Color(0.11f, 0.13f, 0.16f, 1f);
                view.Icon.enabled = false;
                view.Label.text = i < inventory.HotbarSlotCount
                    ? $"HOTBAR {i + 1}\nEMPTY"
                    : $"BAG {i - inventory.HotbarSlotCount + 1}\nEMPTY";
                view.Label.color = new Color(0.5f, 0.55f, 0.6f, 1f);
                continue;
            }

            ItemSO item = stack.item;
            view.Background.color = CategoryColor(item.category);
            view.Icon.sprite = item.icon;
            view.Icon.enabled = item.icon != null;
            string foodState = item.category == ItemCategory.Food
                ? (item.CanConsume ? "  READY" : item.IsRawFood ? "  RAW" : string.Empty)
                : string.Empty;
            view.Label.text = $"{stack.DisplayName}  x{stack.count}\n{item.category.ToString().ToUpperInvariant()}{foodState}";
            view.Label.color = Color.white;
        }
    }

    /// <summary>Membangun ulang Canvas inventory sambil mempertahankan status buka/tutup panel.</summary>
    void RebuildUI()
    {
        if (canvasObject != null)
            Destroy(canvasObject);

        panel = null;
        canvasObject = null;
        capacityText = null;
        dragGhost = null;
        slotViews.Clear();
        BuildUI();
        if (panel != null)
            panel.SetActive(isOpen);
    }

    /// <summary>Memastikan seluruh cache slot masih menunjuk komponen UI Unity yang hidup.</summary>
    bool AreSlotViewsValid()
    {
        for (int index = 0; index < slotViews.Count; index++)
        {
            SlotView view = slotViews[index];
            if (view == null || view.Background == null || view.Icon == null || view.Label == null)
                return false;
        }

        return true;
    }

    void OnSlotClicked(int index)
    {
        ItemStack stack = inventory.GetSlot(index);
        if (stack == null)
            return;

        if (index < inventory.HotbarSlotCount)
        {
            hotbarUI.SelectSlot(index);
            return;
        }
        // Item tas dipindahkan dengan drag agar klik biasa tidak menukar item
        // secara tidak sengaja pada layar sentuh.
    }

    /// <summary>Memulai drag dan membuat ghost visual yang tidak memblok raycast.</summary>
    public void BeginSlotDrag(int index, PointerEventData eventData)
    {
        ItemStack stack = inventory.GetSlot(index);
        if (!isOpen || stack?.item == null || stack.count <= 0)
            return;

        EndSlotDrag();
        draggedSlotIndex = index;
        dragGhost = CreateUIObject("DraggedItemGhost", canvasObject.transform).gameObject;
        dragGhost.transform.SetAsLastSibling();
        RectTransform rect = dragGhost.GetComponent<RectTransform>();
        rect.sizeDelta = slotSize * 0.82f;
        rect.position = eventData.position;

        Image image = AddImage(dragGhost, stack.item.icon != null ? Color.white : CategoryColor(stack.item.category));
        image.sprite = stack.item.icon;
        image.type = Image.Type.Simple;
        image.preserveAspect = stack.item.icon != null;
        image.raycastTarget = false;
        CanvasGroup group = dragGhost.AddComponent<CanvasGroup>();
        group.alpha = 0.88f;
        group.blocksRaycasts = false;

        TMP_Text label = CreateText(dragGhost.transform, "GhostLabel", $"{stack.DisplayName} x{stack.count}", 13f, FontStyles.Bold);
        label.alignment = TextAlignmentOptions.Bottom;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(3f, 3f);
        label.rectTransform.offsetMax = new Vector2(-3f, -3f);
    }

    public void UpdateSlotDrag(PointerEventData eventData)
    {
        if (dragGhost != null)
            dragGhost.transform.position = eventData.position;
    }

    /// <summary>Menukar slot sumber dan target, termasuk tas ↔ hotbar.</summary>
    public void DropOnSlot(int targetIndex)
    {
        if (draggedSlotIndex < 0 || targetIndex < 0 || targetIndex == draggedSlotIndex)
            return;
        inventory.SwapSlots(draggedSlotIndex, targetIndex);
        if (targetIndex < inventory.HotbarSlotCount)
            hotbarUI?.SelectSlot(targetIndex);
    }

    public void EndSlotDrag()
    {
        draggedSlotIndex = -1;
        if (dragGhost != null)
            Destroy(dragGhost);
        dragGhost = null;
    }

    static RectTransform CreateUIObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.layer = parent.gameObject.layer;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    static Image AddImage(GameObject target, Color color)
    {
        Image image = target.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static TMP_Text CreateText(Transform parent, string name, string value, float size, FontStyles style)
    {
        TextMeshProUGUI text = CreateUIObject(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Sprite sprite = null)
    {
        GameObject target = CreateUIObject(name, parent).gameObject;
        Image image = AddImage(target, new Color(0.12f, 0.15f, 0.19f, 1f));
        ApplySprite(image, sprite != null ? sprite : buttonSprite);
        Button button = target.AddComponent<Button>();
        button.targetGraphic = image;
        SetRect(target.GetComponent<RectTransform>(), position, size);

        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text text = CreateText(target.transform, "Text", label, 16f, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
        }
        return button;
    }

    static void ApplySprite(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
    }

    static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static Color CategoryColor(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Food => new Color(0.18f, 0.34f, 0.22f, 1f),
            ItemCategory.Seed => new Color(0.30f, 0.25f, 0.12f, 1f),
            ItemCategory.Tool => new Color(0.18f, 0.28f, 0.38f, 1f),
            ItemCategory.Weapon => new Color(0.38f, 0.16f, 0.16f, 1f),
            ItemCategory.Material => new Color(0.28f, 0.23f, 0.20f, 1f),
            _ => new Color(0.16f, 0.18f, 0.22f, 1f)
        };
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInventoryUIExists()
    {
        if (FindFirstObjectByType<InventoryUI>() != null)
            return;
        Inventory inventory = FindFirstObjectByType<Inventory>();
        if (inventory != null)
            inventory.gameObject.AddComponent<InventoryUI>();
    }

    /// <summary>Cache komponen visual satu slot untuk menghindari pencarian hierarchy saat refresh.</summary>
    sealed class SlotView
    {
        public readonly Image Background;
        public readonly Image Icon;
        public readonly TMP_Text Label;

        public SlotView(Image background, Image icon, TMP_Text label)
        {
            Background = background;
            Icon = icon;
            Label = label;
        }
    }
}
