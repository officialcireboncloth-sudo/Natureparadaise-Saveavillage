using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Satu prompt aksi pada dock informasi bawah. Semua interactable berbagi
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

    /// <summary>Returns the exact anchor the player was shown, so an action cannot silently switch targets.</summary>
    public static bool TryGetDisplayedAnchor(Object owner,out Transform anchor)
    {anchor=IsDisplayedFor(owner)?instance.displayedAnchor:null;return anchor!=null;}

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

        Vector3 viewport = worldCamera.WorldToViewportPoint(requestedWorldPosition);
        if(viewport.z<=0 || viewport.x<0 || viewport.x>1 || viewport.y<0 || viewport.y>1)
        {
            panelRect.gameObject.SetActive(false);
            return;
        }
        promptText.text = requestedText;
        float width=GameplayInfoPanel.ContentWidth-32;
        float height=Mathf.Clamp(promptText.GetPreferredValues(requestedText,width,0).y+18,Application.isMobilePlatform?88:54,152);
        GameplayInfoPanel.ReportInteraction(height);
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
        panelRect=GameplayHUDStyle.Rect("Interaction",GameplayInfoPanel.ActionRoot,Vector2.zero,Vector2.one);
        panelBackground=panelRect.gameObject.AddComponent<Image>();panelBackground.color=Color.clear;panelBackground.canvasRenderer.cullTransparentMesh=false;panelBackground.raycastTarget=false;
        var button=panelRect.gameObject.AddComponent<Button>();button.targetGraphic=panelBackground;button.onClick.AddListener(()=>InvokeDisplayedAction());
        promptText=GameplayHUDStyle.Text("Action",panelRect,"",22,Vector2.zero,Vector2.one);
        GameplayInfoPanel.StyleMinimalText(promptText);
        promptText.color=Color.white;promptText.fontStyle=FontStyles.Bold;promptText.alignment=TextAlignmentOptions.Center;
        promptText.rectTransform.offsetMin=new Vector2(16,8);promptText.rectTransform.offsetMax=new Vector2(-16,-8);
        promptText.textWrappingMode=TextWrappingModes.Normal;promptText.fontSizeMin=18;
        panelRect.gameObject.SetActive(false);EnsureEventSystem();
    }
    void EnsureEventSystem()
    {
        if(EventSystem.current!=null)return;
        var events=new GameObject("World Interaction EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        events.transform.SetParent(transform,false);
    }
}
