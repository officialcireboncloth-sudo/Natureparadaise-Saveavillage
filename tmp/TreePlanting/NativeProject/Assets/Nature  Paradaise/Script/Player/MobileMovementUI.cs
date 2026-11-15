using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Bootstrap UI mobile yang membuat joystick dan tombol movement saat runtime hanya pada
/// platform mobile. Tidak membuat Canvas tambahan jika scene sudah menyediakan kontrol sendiri.
/// </summary>
public static class MobileMovementUI
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BuildForMobile()
    {
        if (!Application.isMobilePlatform || Object.FindFirstObjectByType<MobileJoystick>() != null)
            return;

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
            return;

        HeldItemPlacementSystem heldItem = player.GetComponent<HeldItemPlacementSystem>();
        if (heldItem == null && player.GetComponent<Inventory>() != null)
            heldItem = player.gameObject.AddComponent<HeldItemPlacementSystem>();

        int uiLayer = LayerMask.NameToLayer("UI");
        GameObject canvasObject = new("MobileMovementCanvas_Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = uiLayer >= 0 ? uiLayer : 5;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 220;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform joystickBackground = CreatePanel("MoveJoystick", canvasObject.transform, new Vector2(190f, 190f), new Vector2(150f, 150f), new Vector2(0f, 0f));
        Image backgroundImage = joystickBackground.gameObject.AddComponent<Image>();
        backgroundImage.color = new Color(0.05f, 0.07f, 0.09f, 0.62f);
        RectTransform handle = CreatePanel("Handle", joystickBackground, new Vector2(86f, 86f), Vector2.zero, new Vector2(0.5f, 0.5f));
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = new Color(0.85f, 0.9f, 1f, 0.72f);
        handleImage.raycastTarget = false;
        MobileJoystick joystick = joystickBackground.gameObject.AddComponent<MobileJoystick>();
        joystick.Configure(joystickBackground, handle);

        Button jump = CreateButton("JumpButton", canvasObject.transform, "JUMP", new Vector2(-110f, -110f));
        jump.onClick.AddListener(player.RequestJump);

        Button sprint = CreateButton("SprintButton", canvasObject.transform, "SPRINT", new Vector2(-280f, -110f));
        EventTrigger trigger = sprint.gameObject.AddComponent<EventTrigger>();
        AddTrigger(trigger, EventTriggerType.PointerDown, _ => player.SetMobileSprint(true));
        AddTrigger(trigger, EventTriggerType.PointerUp, _ => player.SetMobileSprint(false));
        AddTrigger(trigger, EventTriggerType.PointerExit, _ => player.SetMobileSprint(false));

        if (heldItem != null)
        {
            // Mobile memakai aksi satu-item agar tap tidak membuang seluruh stack tanpa sengaja.
            Button place = CreateButton("PlaceItemButton", canvasObject.transform, "PLACE", new Vector2(-110f, -210f));
            place.onClick.AddListener(() => heldItem.RequestPlace(false));

            Button drop = CreateButton("DropItemButton", canvasObject.transform, "DROP", new Vector2(-280f, -210f));
            drop.onClick.AddListener(() => heldItem.RequestDrop(false));
        }
    }

    static RectTransform CreatePanel(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor)
    {
        GameObject target = new(name, typeof(RectTransform));
        target.layer = parent.gameObject.layer;
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor.x < 0.5f ? Vector2.zero : new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    static Button CreateButton(string name, Transform parent, string label, Vector2 position)
    {
        RectTransform rect = CreatePanel(name, parent, new Vector2(150f, 82f), position, Vector2.one);
        rect.pivot = Vector2.one;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.1f, 0.14f, 0.78f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        GameObject textObject = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 22f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return button;
    }

    static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new() { eventID = type };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }
}
