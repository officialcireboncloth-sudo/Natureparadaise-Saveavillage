using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Presenter journal quest yang membaca QuestService dan tidak menyimpan state gameplay.</summary>
[DisallowMultipleComponent]
public sealed class QuestJournalUI : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] RectTransform questListRoot;
    [SerializeField] Button questButtonPrefab;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text statusText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] TMP_Text objectivesText;
    [SerializeField] TMP_Text rewardsText;
    [SerializeField] Button turnInButton;
    [SerializeField] KeyCode toggleKey = KeyCode.J;
    [SerializeField] bool includeLockedQuests;

    readonly List<Button> buttonPool = new();
    QuestService boundService;
    QuestDefinitionSO selectedQuest;

    void OnEnable()
    {
        Bind();
        if (turnInButton != null) turnInButton.onClick.AddListener(TurnInSelected);
    }

    void Start()
    {
        Bind();
        SetOpen(false);
    }

    void OnDisable()
    {
        if (turnInButton != null) turnInButton.onClick.RemoveListener(TurnInSelected);
        SetModalLock(false);
        Unbind();
    }

    void Update()
    {
        if (boundService == null) Bind();
        if (Input.GetKeyDown(toggleKey)) SetOpen(panelRoot == null || !panelRoot.activeSelf);
    }

    public void SetOpen(bool open)
    {
        if (open && DialogueService.Instance != null && DialogueService.Instance.IsOpen)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Selesaikan percakapan sebelum membuka Quest Journal.");
            return;
        }
        if (panelRoot != null) panelRoot.SetActive(open);
        SetModalLock(open);
        if (open) RebuildList();
    }

    void SetModalLock(bool locked)
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (locked)
        {
            player?.AcquireMovementLock(this);
            TimeManager.Instance?.AcquirePause(this);
            WorldInteractionPrompt.AcquireSuppression(this);
        }
        else
        {
            player?.ReleaseMovementLock(this);
            TimeManager.Instance?.ReleasePause(this);
            WorldInteractionPrompt.ReleaseSuppression(this);
        }
    }

    void Bind()
    {
        QuestService service = QuestService.Instance;
        if (service == boundService) return;
        Unbind();
        boundService = service;
        if (boundService != null) boundService.QuestChanged += HandleQuestChanged;
    }

    void Unbind()
    {
        if (boundService != null) boundService.QuestChanged -= HandleQuestChanged;
        boundService = null;
    }

    void HandleQuestChanged(QuestDefinitionSO quest, QuestStatus status)
    {
        if (panelRoot != null && panelRoot.activeSelf) RebuildList();
        if (selectedQuest == quest) ShowQuest(quest);
    }

    void RebuildList()
    {
        foreach (Button button in buttonPool)
        {
            if (button == null) continue;
            button.onClick.RemoveAllListeners();
            button.gameObject.SetActive(false);
        }
        if (boundService == null || questListRoot == null || questButtonPrefab == null) return;

        int visibleIndex = 0;
        foreach (QuestDefinitionSO quest in boundService.Definitions)
        {
            QuestStatus status = boundService.GetStatus(quest);
            if (!includeLockedQuests && status == QuestStatus.Locked) continue;
            QuestDefinitionSO captured = quest;
            Button button = GetQuestButton(visibleIndex++);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = $"{quest.title}  [{StatusLabel(status)}]";
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ShowQuest(captured));
            button.gameObject.SetActive(true);
        }

        if (selectedQuest == null && visibleIndex > 0)
            foreach (QuestDefinitionSO quest in boundService.Definitions)
                if (includeLockedQuests || boundService.GetStatus(quest) != QuestStatus.Locked) { ShowQuest(quest); break; }
    }

    Button GetQuestButton(int index)
    {
        while (buttonPool.Count <= index)
        {
            Button button = Instantiate(questButtonPrefab, questListRoot);
            buttonPool.Add(button);
        }
        return buttonPool[index];
    }

    public void ShowQuest(QuestDefinitionSO quest)
    {
        selectedQuest = quest;
        if (quest == null || boundService == null) return;
        QuestStatus status = boundService.GetStatus(quest);
        if (titleText != null) titleText.text = quest.title;
        if (statusText != null) statusText.text = StatusLabel(status);
        if (descriptionText != null) descriptionText.text = quest.description;

        StringBuilder objectives = new();
        foreach (QuestObjectiveDefinition objective in quest.objectives ?? new List<QuestObjectiveDefinition>())
        {
            if (objective == null) continue;
            int progress = boundService.GetProgress(quest, objective.objectiveId);
            objectives.AppendLine($"• {objective.description}  {progress}/{Mathf.Max(1, objective.requiredAmount)}");
        }
        if (objectivesText != null) objectivesText.text = objectives.ToString().TrimEnd();

        StringBuilder rewards = new();
        if (quest.goldReward > 0) rewards.AppendLine($"Gold +{quest.goldReward}");
        foreach (QuestItemReward reward in quest.itemRewards ?? new List<QuestItemReward>())
            if (reward?.item != null) rewards.AppendLine($"{reward.item.itemName} ×{reward.amount}");
        if (rewardsText != null) rewardsText.text = rewards.Length > 0 ? rewards.ToString().TrimEnd() : "Tidak ada reward";
        if (turnInButton != null) turnInButton.gameObject.SetActive(status == QuestStatus.ReadyToTurnIn);
    }

    void TurnInSelected()
    {
        if (boundService != null && selectedQuest != null) boundService.TryTurnIn(selectedQuest);
    }

    static string StatusLabel(QuestStatus status) => status switch
    {
        QuestStatus.Locked => "Terkunci",
        QuestStatus.Available => "Tersedia",
        QuestStatus.Active => "Aktif",
        QuestStatus.ReadyToTurnIn => "Siap dilaporkan",
        QuestStatus.Completed => "Selesai",
        QuestStatus.Failed => "Gagal",
        _ => status.ToString()
    };
}
