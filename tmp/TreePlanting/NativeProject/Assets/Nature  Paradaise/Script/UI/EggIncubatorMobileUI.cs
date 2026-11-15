using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Tombol sentuh kontekstual penetasan yang memakai aksi dan validasi yang sama dengan PC.</summary>
public sealed class EggIncubatorMobileUI : MonoBehaviour
{
    static EggIncubatorMobileUI instance;
    PlayerController player;
    EggIncubator target;
    Button primary, secondary;
    TMP_Text primaryText;
    float nextPlayerSearch;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        if (!Application.isMobilePlatform || instance != null) return;
        var host = new GameObject("EggIncubatorMobileUI_Runtime", typeof(RectTransform));
        DontDestroyOnLoad(host);
        instance = host.AddComponent<EggIncubatorMobileUI>();
    }

    void Awake()
    {
        gameObject.layer = 5;
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 230;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();
        var safe = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
        safe.SetParent(transform, false);
        safe.anchorMin = Vector2.zero;
        safe.anchorMax = Vector2.one;
        safe.offsetMin = safe.offsetMax = Vector2.zero;
        safe.gameObject.AddComponent<SafeAreaFitter>();
        primary = MakeButton(safe, "Taruh Telur", 210f, out primaryText);
        secondary = MakeButton(safe, "Keluarkan", 125f, out _);
        primary.onClick.AddListener(UsePrimary);
        secondary.onClick.AddListener(UseSecondary);
        primary.gameObject.SetActive(false);
        secondary.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null && Time.unscaledTime >= nextPlayerSearch)
        {
            player = FindFirstObjectByType<PlayerController>();
            nextPlayerSearch = Time.unscaledTime + .5f;
        }
        bool available = player != null && player.isActiveAndEnabled && !player.IsMovementLocked &&
            !WorldInteractionPrompt.IsSuppressed && !GameplayPauseMenu.BlocksGameplayInput && Time.timeScale > 0f;
        var carry = available ? player.GetComponent<PlayerAnimalCarry>() : null;
        bool canDrop = carry != null && carry.CarriedAnimal != null;
        target = available && !canDrop ? EggIncubator.GetMobileTarget(player) : null;
        bool ready = target != null && target.State == EggIncubatorState.ChickReady;
        bool empty = target != null && target.State == EggIncubatorState.Empty;
        primary.gameObject.SetActive(canDrop || ready || empty);
        secondary.gameObject.SetActive(ready);
        primaryText.text = canDrop ? "Turunkan Ayam" : ready ? "Angkat Anak Ayam" : "Taruh Telur";
        primary.interactable = canDrop || ready || (empty && target.CanPlaceSelectedEgg(player));
    }

    void UsePrimary()
    {
        if (player == null || player.IsMovementLocked || WorldInteractionPrompt.IsSuppressed ||
            GameplayPauseMenu.BlocksGameplayInput || GameplayInput.ConsumedThisFrame) return;
        var carry = player.GetComponent<PlayerAnimalCarry>();
        if (carry != null && carry.CarriedAnimal != null)
        {
            if (carry.Drop()) GameplayInput.ConsumeCurrentFrame();
            return;
        }
        target?.RequestInteraction(player);
    }

    void UseSecondary() => target?.RequestInteraction(player, true);

    static Button MakeButton(Transform parent, string label, float height, out TMP_Text text)
    {
        var root = new GameObject(label, typeof(RectTransform), typeof(MainMenuRoundedImage), typeof(Button));
        root.layer = 5;
        var rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(246f, 72f);
        rect.anchoredPosition = new Vector2(-32f, height);
        var image = root.GetComponent<MainMenuRoundedImage>();
        image.radius = 12f;
        image.borderWidth = 0f;
        image.color = new Color(.28f, .38f, .29f, .96f);
        var button = root.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(.85f, .95f, .85f);
        colors.pressedColor = new Color(.65f, .8f, .65f);
        colors.disabledColor = new Color(.4f, .4f, .4f, .65f);
        colors.fadeDuration = .12f;
        button.colors = colors;
        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.layer = 5;
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(16f, 8f);
        labelRect.offsetMax = new Vector2(-16f, -8f);
        text = labelObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 22f;
        text.color = new Color(.96f, .94f, .85f);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return button;
    }
}
