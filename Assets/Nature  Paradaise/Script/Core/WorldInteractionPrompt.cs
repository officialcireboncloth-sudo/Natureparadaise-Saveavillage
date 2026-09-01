using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    TMP_Text promptText;
    Camera worldCamera;

    int requestFrame = -1;
    float nearestDistance = float.PositiveInfinity;
    Vector3 requestedWorldPosition;
    string requestedText;

    /// <summary>
    /// Minta prompt tampil pada frame ini. Jika beberapa object meminta bersamaan,
    /// object terdekat yang dipilih.
    /// </summary>
    public static void Request(Object owner, Transform anchor, string text, float distance, float height = 1.2f)
    {
        if (anchor == null || IsSuppressed)
            return;

        Request(owner, anchor.position + Vector3.up * height, text, distance);
    }

    public static void Request(Object owner, Vector3 worldPosition, string text, float distance)
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
        }

        if (distance > instance.nearestDistance)
            return;

        instance.nearestDistance = distance;
        instance.requestedWorldPosition = worldPosition;
        instance.requestedText = text;
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

    /// <summary>
    /// Menyembunyikan world prompt selama modal milik owner terbuka. Set multi-owner
    /// mencegah satu modal menampilkan prompt ketika modal lain masih aktif.
    /// </summary>
    public static void AcquireSuppression(Object owner)
    {
        if (owner == null)
            return;

        SuppressionOwners.Add(owner);
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
        // Prompt build dapat memiliki beberapa baris requirement. Ukuran panel mengikuti
        // konten agar daftar material tetap terbaca tanpa memenuhi seluruh layar mobile.
        Vector2 preferredSize = promptText.GetPreferredValues(requestedText);
        float preferredWidth = Mathf.Clamp(preferredSize.x + 44f, 180f, 680f);
        float preferredHeight = Mathf.Clamp(preferredSize.y + 18f, 52f, 360f);
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
    }

    void BuildUI()
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        gameObject.layer = uiLayer >= 0 ? uiLayer : 5;

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 290;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = GetComponent<RectTransform>();

        GameObject panel = new("Prompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.layer = gameObject.layer;
        panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(transform, false);
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(300f, 52f);
        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.035f, 0.045f, 0.055f, 0.88f);
        background.raycastTarget = false;

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
}
