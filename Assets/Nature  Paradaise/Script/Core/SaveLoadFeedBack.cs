using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Menampilkan toast singkat untuk save/load dan feedback gameplay umum.
/// Posisi menggunakan Canvas independen agar tidak dipengaruhi layout UI scene lama.
/// </summary>
public class SaveLoadFeedback : MonoBehaviour
{
    public static SaveLoadFeedback Instance;

    [Header("UI")]
    public TMP_Text feedbackText;

    [Header("Settings")]
    public float showDuration = 1.5f;

    public float fadeInDuration = 0.15f;
    public float fadeOutDuration = 0.25f;

    [Header("Compact Toast Layout")]
    [SerializeField] Vector2 panelSize = new Vector2(560f, 72f);
    [Tooltip("Posisi absolut pada Canvas UI lama. Kalibrasi SampleScene: X 1360, Y -250.")]
    [SerializeField] Vector2 topCenterOffset = new Vector2(1360f, -250f);
    [SerializeField, Range(14f, 28f)] float maximumFontSize = 20f;
    [SerializeField] Color panelColor = new Color(0.035f, 0.045f, 0.055f, 0.9f);
    [SerializeField] Sprite panelSprite;

    private CanvasGroup canvasGroup;
    private Coroutine currentRoutine;
    private RectTransform independentCanvas;

    // =====================================================
    // AWAKE
    // =====================================================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // -------------------------------------------------
        // Pastikan GameObject aktif
        // -------------------------------------------------

        gameObject.SetActive(true);

        // -------------------------------------------------
        // Cari TMP Text otomatis
        // -------------------------------------------------

        if (feedbackText == null)
        {
            feedbackText =
                GetComponentInChildren<TMP_Text>(true);
        }


        ConfigureCompactToast();

        // -------------------------------------------------
        // Cari CanvasGroup
        // -------------------------------------------------

        canvasGroup =
            GetComponent<CanvasGroup>();

        // Kalau belum ada, buat otomatis
        if (canvasGroup == null)
        {
            canvasGroup =
                gameObject.AddComponent<CanvasGroup>();
        }

        // -------------------------------------------------
        // Kondisi awal
        // -------------------------------------------------

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // -------------------------------------------------
        // Debug
        // -------------------------------------------------

        if (feedbackText == null)
        {
            Debug.LogError(
                "[FEEDBACK] TMP_Text tidak ditemukan!"
            );
        }
        else
        {
            Debug.Log(
                "[FEEDBACK] SaveLoadFeedback siap."
            );
        }
    }

    void ConfigureCompactToast()
    {
        EnsureIndependentCanvas();

        // Field baru pada component scene lama dapat terbaca nol setelah deserialize.
        // Fallback ini mencegah panel berukuran 0 dan berada di luar Canvas legacy.
        if (panelSize.x < 100f || panelSize.y < 30f)
            panelSize = new Vector2(560f, 72f);
        if (Mathf.Abs(topCenterOffset.x) < 1f && Mathf.Abs(topCenterOffset.y) < 1f)
            topCenterOffset = new Vector2(1360f, -250f);
        if (maximumFontSize < 14f)
            maximumFontSize = 20f;
        if (panelColor.a <= 0.01f)
            panelColor = new Color(0.035f, 0.045f, 0.055f, 0.9f);

        RectTransform panelRect = transform as RectTransform;
        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -24f);
            panelRect.sizeDelta = panelSize;
        }

        Image panelImage = GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = panelColor;
            panelImage.sprite = panelSprite;
            panelImage.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            panelImage.raycastTarget = false;
        }

        if (feedbackText == null)
            return;

        RectTransform textRect = feedbackText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.offsetMin = new Vector2(18f, 7f);
        textRect.offsetMax = new Vector2(-18f, -7f);

        feedbackText.fontSize = maximumFontSize;
        feedbackText.enableAutoSizing = true;
        feedbackText.fontSizeMin = 14f;
        feedbackText.fontSizeMax = maximumFontSize;
        feedbackText.fontStyle = FontStyles.Bold;
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.textWrappingMode = TextWrappingModes.Normal;
        feedbackText.overflowMode = TextOverflowModes.Ellipsis;
        feedbackText.raycastTarget = false;
    }

    void EnsureIndependentCanvas()
    {
        if (independentCanvas != null)
            return;

        GameObject canvasObject = new("FeedbackToastCanvas_Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        independentCanvas = canvasObject.GetComponent<RectTransform>();
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        transform.SetParent(independentCanvas, false);
    }

    // =====================================================
    // SAVE
    // =====================================================

    /// <summary>Menampilkan toast keberhasilan save.</summary>
    public void ShowSave()
    {
        ShowMessage("GAME SAVED!");
    }

    // =====================================================
    // LOAD
    // =====================================================

    /// <summary>Menampilkan toast keberhasilan load.</summary>
    public void ShowLoad()
    {
        ShowMessage("GAME LOADED!");
    }

    // =====================================================
    // SHOW MESSAGE
    // =====================================================

    /// <summary>Menampilkan feedback gameplay singkat tanpa mengambil fokus input.</summary>
    public void ShowMessage(string message)
    {
        // Terapkan kembali agar perubahan layout langsung berlaku setelah hot-reload
        // saat Play Mode, tanpa bergantung pada Awake dipanggil ulang.
        ConfigureCompactToast();

        // -------------------------------------------------
        // Pastikan object aktif
        // -------------------------------------------------

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning(
                "[FEEDBACK] SaveLoadFeedback masih inactive!"
            );

            return;
        }

        // -------------------------------------------------
        // Pastikan text ada
        // -------------------------------------------------

        if (feedbackText == null)
        {
            Debug.LogError(
                "[FEEDBACK] Feedback Text belum ditemukan!"
            );

            return;
        }

        // -------------------------------------------------
        // Stop routine sebelumnya
        // -------------------------------------------------

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        // -------------------------------------------------
        // Start
        // -------------------------------------------------

        currentRoutine =
            StartCoroutine(
                ShowMessageRoutine(message)
            );
    }

    // =====================================================
    // SHOW ROUTINE
    // =====================================================

    IEnumerator ShowMessageRoutine(
        string message
    )
    {
        Debug.Log(
            "[FEEDBACK] SHOW: " + message
        );

        // -------------------------------------------------
        // SET TEXT
        // -------------------------------------------------

        feedbackText.text =
            message;

        // Pastikan text enabled
        feedbackText.enabled = true;

        // -------------------------------------------------
        // FADE IN
        // -------------------------------------------------

        yield return StartCoroutine(
            FadeCanvasGroup(
                0f,
                1f,
                fadeInDuration
            )
        );

        // -------------------------------------------------
        // WAIT
        // -------------------------------------------------

        yield return new WaitForSeconds(
            showDuration
        );

        // -------------------------------------------------
        // FADE OUT
        // -------------------------------------------------

        yield return StartCoroutine(
            FadeCanvasGroup(
                1f,
                0f,
                fadeOutDuration
            )
        );

        currentRoutine = null;

        Debug.Log(
            "[FEEDBACK] HIDDEN"
        );
    }

    // =====================================================
    // FADE
    // =====================================================

    IEnumerator FadeCanvasGroup(
        float start,
        float target,
        float duration
    )
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            canvasGroup.alpha =
                target;

            yield break;
        }

        float timer = 0f;

        canvasGroup.alpha =
            start;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float progress =
                timer / duration;

            canvasGroup.alpha =
                Mathf.Lerp(
                    start,
                    target,
                    progress
                );

            yield return null;
        }

        canvasGroup.alpha =
            target;
    }
}
