using System.Text;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Ringkasan objective aktif yang selalu terlihat tanpa membuka Quest Journal.</summary>
[DisallowMultipleComponent]
public sealed class QuestTrackerUI : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] TMP_Text contentText;
    [SerializeField, Min(1)] int maximumVisibleQuests = 3;

    QuestService boundService;
    bool hasVisibleQuest;
    bool lastSuppressed;
    bool collapsed;
    TMP_Text collapseLabel;
    string trackedQuest;
    public void Track(QuestDefinitionSO quest)
    {
        if (quest == null) return;
        trackedQuest = quest.Id;
        Refresh();
    }

    void Awake() => ApplyCompactStyle();

    void OnEnable()
    {
        ApplyCompactStyle();
        Bind();
        Refresh();
    }

    void OnDisable() => Unbind();

    void Update()
    {
        if (boundService == null) Bind();
        bool suppressed = WorldInteractionPrompt.IsSuppressed ||
                          (DialogueService.Instance != null && DialogueService.Instance.IsOpen);
        if (suppressed != lastSuppressed)
        {
            lastSuppressed = suppressed;
            ApplyVisibility();
        }
    }

    void Bind()
    {
        QuestService service = QuestService.Instance;
        if (service == boundService) return;
        Unbind();
        boundService = service;
        if (boundService != null) boundService.QuestChanged += HandleQuestChanged;
        Refresh();
    }

    void Unbind()
    {
        if (boundService != null) boundService.QuestChanged -= HandleQuestChanged;
        boundService = null;
    }

    void HandleQuestChanged(QuestDefinitionSO quest, QuestStatus status) => Refresh();

    public void Refresh()
    {
        StringBuilder text = new();
        int visible = 0;
        if (boundService != null)
        {
            foreach (QuestDefinitionSO quest in boundService.Definitions.OrderBy(quest => quest.Id == trackedQuest ? 0 : 1))
            {
                QuestStatus status = boundService.GetStatus(quest);
                if (status != QuestStatus.Active && status != QuestStatus.ReadyToTurnIn) continue;
                if (visible++ >= maximumVisibleQuests) break;
                text.AppendLine(status == QuestStatus.ReadyToTurnIn
                    ? $"<b><color=#F3D47A>{quest.title}</color></b>  <color=#65E6A5>SELESAI</color>"
                    : $"<b><color=#F3D47A>{quest.title}</color></b>");
                if (quest.objectives != null)
                {
                    foreach (QuestObjectiveDefinition objective in quest.objectives)
                    {
                        if (objective == null) continue;
                        int progress = boundService.GetProgress(quest, objective.objectiveId);
                        text.AppendLine($"<color=#E8EDF2>◆ {objective.description}</color>  " +
                                        $"<color=#BFD5E5>{progress}/{Mathf.Max(1, objective.requiredAmount)}</color>");
                    }
                }
                if (status == QuestStatus.ReadyToTurnIn)
                    text.AppendLine("<color=#65E6A5>Kembali ke pemberi quest</color>");
            }
        }

        if (visible > 0) text.Append("<size=75%><color=#AAB6C2>[J] Quest Journal</color></size>");
        hasVisibleQuest = visible > 0;
        if (contentText != null) contentText.text = text.ToString().TrimEnd();
        ResizeToContent();
        ApplyVisibility();
    }

    void ApplyVisibility()
    {
        if (panelRoot != null) panelRoot.SetActive(hasVisibleQuest && !lastSuppressed);
    }

    void ApplyCompactStyle()
    {
        if (panelRoot == null || contentText == null) return;
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-24f, -122f);
            panelRect.sizeDelta = new Vector2(330f, 160f);
            if (panelRoot.transform.Find("Tracker Surface") == null)
            {
                RectTransform surface = GameplayHUDStyle.Rect("Tracker Surface", panelRect, Vector2.zero, Vector2.one);
                GameplayHUDStyle.Surface(surface, GameplayHUDStyle.Panel);
                surface.SetAsFirstSibling();
                GameplayHUDStyle.Text("Header", panelRect, "Quest Dilacak", 20, new(.05f,1f), new(.82f,1f)).rectTransform.sizeDelta = new Vector2(0, 42);
                RectTransform header = panelRoot.transform.Find("Header") as RectTransform;
                header.pivot = new Vector2(.5f,1f);
                RectTransform toggle = GameplayHUDStyle.Rect("Collapse", panelRect, new(.84f,1f), new(.98f,1f));
                toggle.pivot = new Vector2(.5f,1f); toggle.sizeDelta = new Vector2(0,42);
                var toggleImage = GameplayHUDStyle.Surface(toggle, Color.clear); toggleImage.raycastTarget = true;
                var button = toggle.gameObject.AddComponent<Button>(); button.targetGraphic = toggleImage;
                collapseLabel = GameplayHUDStyle.Text("Label", toggle, "−", 24, Vector2.zero, Vector2.one);
                collapseLabel.alignment = TextAlignmentOptions.Center;
                button.onClick.AddListener(() => { collapsed = !collapsed; contentText.gameObject.SetActive(!collapsed); collapseLabel.text = collapsed ? "+" : "−"; ResizeToContent(); });
            }
        }

        Image background = panelRoot.GetComponent<Image>();
        if (background != null)
        {
            background.enabled = false;
            background.raycastTarget = false;
        }
        CanvasGroup group = panelRoot.GetComponent<CanvasGroup>();
        if (group == null) group = panelRoot.AddComponent<CanvasGroup>();
        group.interactable = true;
        group.blocksRaycasts = true;

        contentText.fontSize = 18f;
        contentText.fontStyle = FontStyles.Normal;
        contentText.alignment = TextAlignmentOptions.TopLeft;
        contentText.textWrappingMode = TextWrappingModes.Normal;
        contentText.margin = Vector4.zero;
        contentText.rectTransform.anchorMin = Vector2.zero;
        contentText.rectTransform.anchorMax = Vector2.one;
        contentText.rectTransform.offsetMin = new Vector2(16, 12);
        contentText.rectTransform.offsetMax = new Vector2(-16, -48);
        contentText.overflowMode = TextOverflowModes.Ellipsis;
        contentText.raycastTarget = false;
    }

    void ResizeToContent()
    {
        if (panelRoot == null || contentText == null) return;
        contentText.ForceMeshUpdate();
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        if (panelRect != null)
            panelRect.sizeDelta = new Vector2(330f, collapsed ? 44f : Mathf.Clamp(contentText.preferredHeight + 64f, 104f, 310f));
    }
}
