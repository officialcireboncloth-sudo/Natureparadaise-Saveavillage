using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class QuestSignal
{
    public QuestObjectiveType type;
    public string targetId;
    public ItemSO item;
    public int amount = 1;
}

/// <summary>Bridge modular antara gameplay dan quest; sistem lain cukup Publish tanpa mengenal QuestService.</summary>
public static class QuestEventHub
{
    public static event Action<QuestSignal> Raised;

    public static void Publish(QuestObjectiveType type, string targetId, int amount = 1, ItemSO item = null)
    {
        if (amount <= 0) return;
        Raised?.Invoke(new QuestSignal { type = type, targetId = targetId, item = item, amount = amount });
    }
}

[Serializable]
public sealed class QuestObjectiveProgressSaveData
{
    public string objectiveId;
    public int amount;
}

[Serializable]
public sealed class QuestRuntimeSaveData
{
    public string questId;
    public QuestStatus status;
    public int completionCount;
    public List<QuestObjectiveProgressSaveData> objectives = new();
}

[Serializable]
public sealed class QuestSystemSaveData
{
    public List<QuestRuntimeSaveData> quests = new();
}

[DisallowMultipleComponent]
public sealed class QuestService : MonoBehaviour
{
    public static QuestService Instance { get; private set; }

    [SerializeField] List<QuestDefinitionSO> questCatalog = new();

    readonly Dictionary<string, QuestDefinitionSO> definitions = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, QuestRuntimeSaveData> states = new(StringComparer.OrdinalIgnoreCase);

    public event Action<QuestDefinitionSO, QuestStatus> QuestChanged;
    public event Action<QuestDefinitionSO> QuestCompleted;
    public IReadOnlyList<QuestDefinitionSO> Definitions => definitions.Values.OrderBy(quest => quest.title).ToList();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        RebuildCatalog();
    }

    void OnEnable()
    {
        if (Instance == null) Instance = this;
        QuestEventHub.Raised += HandleSignal;
    }

    void OnDisable() => QuestEventHub.Raised -= HandleSignal;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void RebuildCatalog()
    {
        definitions.Clear();
        IEnumerable<QuestDefinitionSO> source = questCatalog != null && questCatalog.Any(quest => quest != null)
            ? questCatalog
            : Resources.LoadAll<QuestDefinitionSO>(string.Empty);
        foreach (QuestDefinitionSO quest in source)
            Register(quest);
    }

    public void Register(QuestDefinitionSO quest)
    {
        if (quest != null && !string.IsNullOrWhiteSpace(quest.Id)) definitions[quest.Id] = quest;
    }

    public QuestDefinitionSO FindQuest(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId)) return null;
        definitions.TryGetValue(questId, out QuestDefinitionSO quest);
        return quest;
    }

    public QuestStatus GetStatus(QuestDefinitionSO quest)
    {
        if (quest == null) return QuestStatus.Locked;
        Register(quest);
        if (states.TryGetValue(quest.Id, out QuestRuntimeSaveData state)) return state.status;
        return MeetsPrerequisites(quest) ? QuestStatus.Available : QuestStatus.Locked;
    }

    public bool StartQuest(QuestDefinitionSO quest)
    {
        if (quest == null) return false;
        Register(quest);
        QuestStatus status = GetStatus(quest);
        if (status != QuestStatus.Available && !(quest.repeatable && status == QuestStatus.Completed)) return false;

        int completed = states.TryGetValue(quest.Id, out QuestRuntimeSaveData old) ? old.completionCount : 0;
        QuestRuntimeSaveData state = new()
        {
            questId = quest.Id,
            status = QuestStatus.Active,
            completionCount = completed,
            objectives = new List<QuestObjectiveProgressSaveData>()
        };
        states[quest.Id] = state;
        InitializeObjectives(quest, state);
        RefreshCompletionState(quest, state);
        QuestChanged?.Invoke(quest, state.status);
        SaveLoadFeedback.Instance?.ShowMessage($"Quest diterima: {quest.title}");
        return true;
    }

    public bool AddProgress(QuestDefinitionSO quest, string objectiveId, int amount = 1)
    {
        if (quest == null || amount <= 0 || !states.TryGetValue(quest.Id, out QuestRuntimeSaveData state) ||
            state.status != QuestStatus.Active || quest.objectives == null) return false;
        int index = quest.objectives.FindIndex(objective => objective != null && objective.objectiveId == objectiveId);
        if (index < 0) return false;
        ApplyProgress(quest, state, index, amount);
        return true;
    }

    public bool TryTurnIn(QuestDefinitionSO quest)
    {
        if (quest == null || !states.TryGetValue(quest.Id, out QuestRuntimeSaveData state)) return false;
        if (state.status == QuestStatus.Active) RefreshCompletionState(quest, state);
        if (state.status != QuestStatus.ReadyToTurnIn) return false;
        return GrantRewardsAndComplete(quest, state);
    }

    public int GetProgress(QuestDefinitionSO quest, string objectiveId)
    {
        if (quest == null || !states.TryGetValue(quest.Id, out QuestRuntimeSaveData state)) return 0;
        return Mathf.Max(0, state.objectives?.Find(progress => progress.objectiveId == objectiveId)?.amount ?? 0);
    }

    public bool MeetsPrerequisites(QuestDefinitionSO quest)
    {
        if (quest == null) return false;
        if (VillageProgressionService.Instance != null &&
            !VillageProgressionService.Instance.MeetsRequirement(quest.requiredVillageLevel)) return false;
        if (quest.prerequisites == null) return true;
        return quest.prerequisites.All(required => required == null || GetStatus(required) == QuestStatus.Completed);
    }

    void HandleSignal(QuestSignal signal)
    {
        if (signal == null || signal.amount <= 0) return;
        foreach (QuestRuntimeSaveData state in states.Values.ToList())
        {
            if (state.status != QuestStatus.Active || !definitions.TryGetValue(state.questId, out QuestDefinitionSO quest)) continue;
            if (quest.objectives == null) continue;
            for (int index = 0; index < quest.objectives.Count; index++)
            {
                QuestObjectiveDefinition objective = quest.objectives[index];
                if (!Matches(objective, signal)) continue;
                ApplyProgress(quest, state, index, signal.amount);
            }
        }
    }

    void InitializeObjectives(QuestDefinitionSO quest, QuestRuntimeSaveData state)
    {
        Inventory inventory = FindFirstObjectByType<Inventory>();
        if (quest.objectives == null) return;
        foreach (QuestObjectiveDefinition objective in quest.objectives)
        {
            if (objective == null) continue;
            int initial = objective.type == QuestObjectiveType.Collect && objective.countExistingInventory &&
                          objective.targetItem != null && inventory != null
                ? inventory.GetCount(objective.targetItem)
                : 0;
            state.objectives.Add(new QuestObjectiveProgressSaveData
            {
                objectiveId = objective.objectiveId,
                amount = Mathf.Clamp(initial, 0, Mathf.Max(1, objective.requiredAmount))
            });
        }
    }

    void ApplyProgress(QuestDefinitionSO quest, QuestRuntimeSaveData state, int objectiveIndex, int amount)
    {
        QuestObjectiveDefinition objective = quest.objectives[objectiveIndex];
        QuestObjectiveProgressSaveData progress = FindOrCreateProgress(state, objective.objectiveId);
        int before = progress.amount;
        progress.amount = Mathf.Clamp(progress.amount + amount, 0, Mathf.Max(1, objective.requiredAmount));
        if (progress.amount == before) return;
        RefreshCompletionState(quest, state);
        QuestChanged?.Invoke(quest, state.status);
    }

    void RefreshCompletionState(QuestDefinitionSO quest, QuestRuntimeSaveData state)
    {
        if (state.status != QuestStatus.Active) return;
        bool complete = quest.objectives == null || quest.objectives.Count == 0 || quest.objectives.All(objective => objective == null ||
            GetProgress(quest, objective.objectiveId) >= Mathf.Max(1, objective.requiredAmount));
        if (!complete) return;
        state.status = QuestStatus.ReadyToTurnIn;
        if (quest.completionMode == QuestCompletionMode.AutoComplete)
            GrantRewardsAndComplete(quest, state);
    }

    bool GrantRewardsAndComplete(QuestDefinitionSO quest, QuestRuntimeSaveData state)
    {
        Inventory inventory = FindFirstObjectByType<Inventory>();
        List<QuestItemReward> granted = new();
        foreach (QuestItemReward reward in quest.itemRewards ?? new List<QuestItemReward>())
        {
            if (reward?.item == null || reward.amount <= 0) continue;
            if (inventory == null || !inventory.Add(reward.item, reward.amount, reward.qualityStars))
            {
                foreach (QuestItemReward rollback in granted) inventory?.Remove(rollback.item, rollback.amount);
                SaveLoadFeedback.Instance?.ShowMessage("Tas penuh. Kosongkan slot sebelum mengambil reward quest.");
                return false;
            }
            granted.Add(reward);
        }

        if (quest.goldReward > 0) ScoreManager.Instance?.AddPoints(quest.goldReward);
        if (quest.villageConditionReward > 0)
            VillageProgressionService.Instance?.AddConditionPoints(quest.villageConditionReward, $"Quest {quest.title}");
        state.status = QuestStatus.Completed;
        state.completionCount++;
        QuestChanged?.Invoke(quest, state.status);
        QuestCompleted?.Invoke(quest);
        SaveLoadFeedback.Instance?.ShowMessage($"Quest selesai: {quest.title}");
        return true;
    }

    static bool Matches(QuestObjectiveDefinition objective, QuestSignal signal)
    {
        if (objective == null || objective.type != signal.type) return false;
        if (objective.targetItem != null) return signal.item == objective.targetItem;
        if (string.IsNullOrWhiteSpace(objective.targetId)) return true;
        if (string.Equals(objective.targetId, signal.targetId, StringComparison.OrdinalIgnoreCase)) return true;
        return signal.item != null &&
               (string.Equals(objective.targetId, signal.item.name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(objective.targetId, signal.item.itemName, StringComparison.OrdinalIgnoreCase));
    }

    static QuestObjectiveProgressSaveData FindOrCreateProgress(QuestRuntimeSaveData state, string objectiveId)
    {
        state.objectives ??= new List<QuestObjectiveProgressSaveData>();
        QuestObjectiveProgressSaveData progress = state.objectives.Find(entry => entry.objectiveId == objectiveId);
        if (progress != null) return progress;
        progress = new QuestObjectiveProgressSaveData { objectiveId = objectiveId };
        state.objectives.Add(progress);
        return progress;
    }

    public QuestSystemSaveData Capture() => new()
    {
        quests = states.Values.Select(state => new QuestRuntimeSaveData
        {
            questId = state.questId,
            status = state.status,
            completionCount = state.completionCount,
            objectives = state.objectives?.Select(progress => new QuestObjectiveProgressSaveData
            {
                objectiveId = progress.objectiveId,
                amount = progress.amount
            }).ToList() ?? new List<QuestObjectiveProgressSaveData>()
        }).ToList()
    };

    public void Restore(QuestSystemSaveData data)
    {
        states.Clear();
        if (data?.quests != null)
            foreach (QuestRuntimeSaveData state in data.quests)
                if (state != null && !string.IsNullOrWhiteSpace(state.questId)) states[state.questId] = state;
        foreach (QuestDefinitionSO quest in definitions.Values) QuestChanged?.Invoke(quest, GetStatus(quest));
    }
}
