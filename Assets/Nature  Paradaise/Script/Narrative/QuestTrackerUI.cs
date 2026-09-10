using System.Text;
using TMPro;
using UnityEngine;

/// <summary>Ringkasan objective aktif yang selalu terlihat tanpa membuka Quest Journal.</summary>
[DisallowMultipleComponent]
public sealed class QuestTrackerUI : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] TMP_Text contentText;
    [SerializeField, Min(1)] int maximumVisibleQuests = 3;

    QuestService boundService;

    void OnEnable()
    {
        Bind();
        Refresh();
    }

    void OnDisable() => Unbind();

    void Update()
    {
        if (boundService == null) Bind();
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
                    ? $"<b>{quest.title}</b>  <color=#65E6A5>SIAP DILAPORKAN</color>"
                    : $"<b>{quest.title}</b>");
                if (quest.objectives != null)
                {
                    foreach (QuestObjectiveDefinition objective in quest.objectives)
                    {
                        if (objective == null) continue;
                        int progress = boundService.GetProgress(quest, objective.objectiveId);
                        text.AppendLine($"• {objective.description}  {progress}/{Mathf.Max(1, objective.requiredAmount)}");
                    }
                }
                if (status == QuestStatus.ReadyToTurnIn) text.AppendLine("Kembali dan bicara dengan Mina");
                text.AppendLine("J: Buka Quest Journal");
            }
        }

        bool hasQuest = visible > 0;
        if (panelRoot != null) panelRoot.SetActive(hasQuest);
        if (contentText != null) contentText.text = text.ToString().TrimEnd();
    }
}
