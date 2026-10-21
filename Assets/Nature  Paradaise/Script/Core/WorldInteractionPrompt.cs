using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Satu prompt UI ringan yang mengikuti object di dunia. Semua interactable berbagi
/// satu Canvas sehingga tidak membuat Canvas/Update terpisah untuk setiap object.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(10000)]
public sealed class WorldInteractionPrompt : MonoBehaviour
{
    const float ReferenceWidth = 1920f;
    const float ReferenceHeight = 1080f;

    static WorldInteractionPrompt instance;
    static readonly HashSet<Object> SuppressionOwners = new();

    Canvas canvas;
    RectTransform canvasRect;
    RectTransform panelRect;
    Image panelBackground;
    TMP_Text promptText;
    Camera worldCamera;

    int requestFrame = -1;
    float nearestDistance = float.PositiveInfinity;
    Vector3 requestedWorldPosition;
    string requestedText;
    bool requestedCleanStyle;
    Object requestedOwner;
    Object displayedOwner;
    Transform requestedAnchor, displayedAnchor;
    int displayedFrame = -1;
    System.Action requestedAction, displayedAction;

    /// <summary>
    /// Input in Update belongs to the prompt presented by the preceding LateUpdate,
    /// not whichever nearby component happens to run first this frame.
    /// Callers must also check their current range/availability before consuming input.
    /// </summary>
    public static bool IsDisplayedFor(Object owner)
    {
        if (owner == null || IsSuppressed || instance == null ||
            instance.displayedOwner != owner || instance.displayedFrame < Time.frameCount - 1 ||
            instance.panelRect == null || !instance.panelRect.gameObject.activeInHierarchy)
            return false;
        return !(owner is Behaviour behaviour) || behaviour.isActiveAndEnabled;
    }

    public static bool IsDisplayedFor(Object owner, Transform anchor)
        => IsDisplayedFor(owner) && anchor != null && instance.displayedAnchor == anchor;

    public static bool HasDisplayedPrompt => instance != null && IsDisplayedFor(instance.displayedOwner);

    /// <summary>Tools may act without a world prompt, but cannot take input from another visible interaction.</summary>
    public static bool AllowsAction(Object owner)
    {
        if (owner == null || IsSuppressed) return false;
        if (!HasDisplayedPrompt || IsDisplayedFor(owner)) return true;
        // Farming, seeds, eating and held-item hints share one player action context.
        // A world object (door/NPC/animal/etc.) never grants that access to the player.
        return owner is Component action && instance.displayedOwner is Component prompt &&
            action.gameObject == prompt.gameObject && action.GetComponent<PlayerController>() != null;
    }

    /// <summary>
    /// Minta prompt tampil pada frame ini. Jika beberapa object meminta bersamaan,
    /// object terdekat yang dipilih.
    /// </summary>
    public static void Request(Object owner, Transform anchor, string text, float distance, float height = 1.2f, System.Action action = null)
    {
        if (anchor == null || IsSuppressed)
            return;

        RequestInternal(owner, anchor.position + Vector3.up * height, text, distance, false, anchor,action);
    }

    /// <summary>Prompt ringkas tanpa panel gelap, cocok untuk aksi yang terus tersedia pada player.</summary>
    public static void RequestClean(Object owner, Transform anchor, string text, float distance, float height = 1.2f)
    {
        if (anchor == null || IsSuppressed)
            return;

        RequestInternal(owner,anchor.position+Vector3.up*height,text,distance,true,anchor);
    }

    public static void Request(Object owner, Vector3 worldPosition, string text, float distance)
    {
        RequestInternal(owner,worldPosition,text,distance,false);
    }

    static void RequestInternal(Object owner,Vector3 worldPosition,string text,float distance,bool cleanStyle,Transform anchor = null,System.Action action = null)
    {
        if (owner == null || string.IsNullOrWhiteSpace(text) || IsSuppressed)
            return;

        EnsureExists();
        if (instance == null)
            return;

        int frame = Time.frameCount;
        if (instance.requestFrame != frame)
        {
            instance.requestFrame = frame;
            instance.nearestDistance = float.PositiveInfinity;
            instance.requestedOwner = null;
        }

        // Preserve the visible target across near-equal distances, independent of Update order.
        bool tied = Mathf.Abs(distance - instance.nearestDistance) <= .02f;
        if (tied && instance.requestedOwner != null)
        {
            if (instance.requestedOwner == instance.displayedOwner) return;
            if (owner != instance.displayedOwner && owner.GetInstanceID() > instance.requestedOwner.GetInstanceID()) return;
        }
        else if (distance > instance.nearestDistance)
            return;

        instance.nearestDistance = distance;
        instance.requestedWorldPosition = worldPosition;
        instance.requestedText = text;
        instance.requestedCleanStyle = cleanStyle;
        instance.requestedOwner = owner;
        instance.requestedAnchor = anchor;
        instance.requestedAction = action;
    }

    /// <summary>Touch/click executes the same visible target; its callback rechecks live range.</summary>
    public static bool InvokeDisplayedAction()
    {
        if(instance==null||!HasDisplayedPrompt||instance.displayedAction==null||
           GameplayPauseMenu.BlocksGameplayInput||GameplayInput.ConsumedThisFrame)return false;
        var action=instance.displayedAction;
        action();GameplayInput.ConsumeCurrentFrame();return true;
    }

    /// <summary>True ketika minimal satu modal UI sedang menahan world prompt.</summary>
    public static bool IsSuppressed
    {
        get
        {
            // Membersihkan owner yang hancur saat scene berganti meskipun OnDisable tidak sempat
            // melepas lock, sehingga prompt tidak dapat terkunci permanen.
            if (SuppressionOwners.Count > 0)
                SuppressionOwners.RemoveWhere(IsMissingOwner);
            return SuppressionOwners.Count > 0;
        }
    }

    static bool IsMissingOwner(Object owner) => owner == null;
    public static bool IsSuppressedExcept(Object owner)
    {
        SuppressionOwners.RemoveWhere(IsMissingOwner);
        foreach(var candidate in SuppressionOwners) if(candidate!=owner) return true;
        return false;
    }

    /// <summary>
    /// Menyembunyikan world prompt selama modal milik owner terbuka. Set multi-owner
    /// mencegah satu modal menampilkan prompt ketika modal lain masih aktif.
    /// </summary>
    public static void AcquireSuppression(Object owner)
    {
        if (owner == null)
            return;

        SuppressionOwners.Add(owner);
        if (instance != null) instance.displayedOwner = null;
        if (instance != null && instance.panelRect != null)
            instance.panelRect.gameObject.SetActive(false);
    }

    /// <summary>Melepas suppression milik owner tanpa memengaruhi modal lain.</summary>
    public static void ReleaseSuppression(Object owner)
    {
        if (owner != null)
            SuppressionOwners.Remove(owner);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (instance != null)
            return;

        instance = FindFirstObjectByType<WorldInteractionPrompt>();
        if (instance != null)
            return;

        GameObject root = new("WorldInteractionPrompt_Runtime", typeof(RectTransform));
        instance = root.AddComponent<WorldInteractionPrompt>();
        DontDestroyOnLoad(root);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    void LateUpdate()
    {
        displayedOwner = null;
        displayedAction = null;
        displayedFrame = -1;
        if (panelRect == null)
            return;

        if (IsSuppressed || requestFrame != Time.frameCount)
        {
            panelRect.gameObject.SetActive(false);
            return;
        }

        if (worldCamera == null || !worldCamera.isActiveAndEnabled)
            worldCamera = Camera.main;

        if (worldCamera == null)
        {
            panelRect.gameObject.SetActive(false);
            return;
        }

        Vector3 screenPoint = worldCamera.WorldToScreenPoint(requestedWorldPosition);
        if (screenPoint.z <= 0f || screenPoint.x < 0f || screenPoint.y < 0f ||
            screenPoint.x > Screen.width || screenPoint.y > Screen.height)
        {
            panelRect.gameObject.SetActive(false);
            return;
        }

        promptText.text = requestedText;
        panelBackground.color=requestedCleanStyle
            ? Color.clear
            : GameplayHUDStyle.Panel;
        promptText.outlineWidth=requestedCleanStyle?0.22f:0f;
        promptText.outlineColor=requestedCleanStyle?new Color32(0,0,0,210):Color.clear;
        // Prompt build dapat memiliki beberapa baris requirement. Ukuran panel mengikuti
        // konten agar daftar material tetap terbaca tanpa memenuhi seluruh layar mobile.
        Vector2 preferredSize = promptText.GetPreferredValues(requestedText);
        float horizontalPadding=requestedCleanStyle?12f:44f;
        float verticalPadding=requestedCleanStyle?6f:18f;
        float preferredWidth = Mathf.Clamp(preferredSize.x + horizontalPadding, requestedCleanStyle?80f:180f, 680f);
        float preferredHeight = Mathf.Clamp(preferredSize.y + verticalPadding, requestedCleanStyle?30f:52f, 360f);
        if(requestedAction!=null&&Application.isMobilePlatform)preferredHeight=Mathf.Max(80f,preferredHeight);
        panelRect.sizeDelta = new Vector2(preferredWidth, preferredHeight);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint))
        {
            panelRect.gameObject.SetActive(false);
            return;
        }

        Vector2 half = panelRect.sizeDelta * 0.5f;
        Rect safeRect = canvasRect.rect;
        localPoint.x = Mathf.Clamp(localPoint.x, safeRect.xMin + half.x + 12f, safeRect.xMax - half.x - 12f);
        localPoint.y = Mathf.Clamp(localPoint.y, safeRect.yMin + half.y + 12f, safeRect.yMax - half.y - 12f);
        panelRect.anchoredPosition = localPoint;
        panelRect.gameObject.SetActive(true);
        panelBackground.raycastTarget=requestedAction!=null;
        if(requestedAction!=null)EnsureEventSystem();
        displayedOwner = requestedOwner;
        displayedAnchor = requestedAnchor;
        displayedFrame = Time.frameCount;
        displayedAction=requestedAction;
    }

    void BuildUI()
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        gameObject.layer = uiLayer >= 0 ? uiLayer : 5;

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 290;
        gameObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = GetComponent<RectTransform>();

        GameObject panel = new("Prompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(MainMenuRoundedImage));
        panel.layer = gameObject.layer;
        panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(transform, false);
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(300f, 52f);
        panelBackground = panel.GetComponent<Image>();
        panelBackground.color = GameplayHUDStyle.Panel;
        panelBackground.raycastTarget = false;
        var button=panel.AddComponent<Button>();button.targetGraphic=panelBackground;
        button.onClick.AddListener(()=>InvokeDisplayedAction());

        GameObject textObject = new("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = gameObject.layer;
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(panelRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 4f);
        textRect.offsetMax = new Vector2(-14f, -4f);

        promptText = textObject.GetComponent<TextMeshProUGUI>();
        promptText.font = TMP_Settings.defaultFontAsset;
        promptText.fontSize = 20f;
        promptText.fontStyle = FontStyles.Bold;
        promptText.color = Color.white;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.textWrappingMode = TextWrappingModes.NoWrap;
        promptText.raycastTarget = false;

        panel.SetActive(false);
    }
    void EnsureEventSystem()
    {
        if(EventSystem.current!=null)return;
        var events=new GameObject("World Interaction EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        events.transform.SetParent(transform,false);
    }
}
