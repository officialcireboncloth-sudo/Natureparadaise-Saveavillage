using System.Text;
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
            foreach (QuestDefinitionSO quest in boundService.Definitions)
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
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(22f, -190f);
            panelRect.sizeDelta = new Vector2(380f, 104f);
        }

        Image background = panelRoot.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color(0.018f, 0.03f, 0.04f, 0.58f);
            background.raycastTarget = false;
        }
        CanvasGroup group = panelRoot.GetComponent<CanvasGroup>();
        if (group == null) group = panelRoot.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        contentText.fontSize = 16f;
        contentText.fontStyle = FontStyles.Normal;
        contentText.alignment = TextAlignmentOptions.TopLeft;
        contentText.textWrappingMode = TextWrappingModes.Normal;
        contentText.margin = new Vector4(12f, 9f, 12f, 9f);
        contentText.raycastTarget = false;
    }

    void ResizeToContent()
    {
        if (panelRoot == null || contentText == null) return;
        contentText.ForceMeshUpdate();
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        if (panelRect != null)
            panelRect.sizeDelta = new Vector2(380f, Mathf.Clamp(contentText.preferredHeight + 18f, 70f, 210f));
    }
}
