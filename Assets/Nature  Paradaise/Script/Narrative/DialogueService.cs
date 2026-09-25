using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class DialogueFlagSaveData
{
    public string flagId;
    public bool value;
}

[Serializable]
public sealed class DialogueSystemSaveData
{
    public List<DialogueFlagSaveData> flags = new();
}

/// <summary>Menjalankan conversation asset, condition, command, dan flag tanpa bergantung pada prefab UI.</summary>
[DisallowMultipleComponent]
public sealed class DialogueService : MonoBehaviour
{
    public static DialogueService Instance { get; private set; }

    [SerializeField] Inventory playerInventory;
    [SerializeField] PlayerController playerController;
    [SerializeField] bool pauseWorldDuringDialogue = true;

    readonly Dictionary<string, bool> flags = new(StringComparer.OrdinalIgnoreCase);
    readonly List<DialogueChoice> visibleChoices = new();
    DialogueConversationSO activeConversation;
    DialogueNode currentNode;

    public DialogueConversationSO ActiveConversation => activeConversation;
    public DialogueNode CurrentNode => currentNode;
    public DialogueSpeakerSO CurrentSpeaker => currentNode?.speakerOverride != null
        ? currentNode.speakerOverride : activeConversation?.defaultSpeaker;
    public IReadOnlyList<DialogueChoice> VisibleChoices => visibleChoices;
    public bool IsOpen => activeConversation != null && currentNode != null;

    public event Action ConversationStarted;
    public event Action NodeChanged;
    public event Action ConversationEnded;
    public event Action<string, bool> FlagChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResolvePlayerReferences();
    }

    void OnEnable()
    {
        if (Instance == null) Instance = this;
    }

    void OnDisable()
    {
        if (IsOpen) EndConversation();
        else ReleaseModalLocks();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool StartConversation(DialogueConversationSO conversation)
    {
        if (conversation == null || conversation.nodes == null || conversation.nodes.Count == 0) return false;
        DialogueNode entry = conversation.FindNode(conversation.entryNodeId);
        if (entry == null)
        {
            Debug.LogWarning($"[DIALOGUE] Entry node '{conversation.entryNodeId}' tidak ditemukan di {conversation.name}.");
            return false;
        }

        if (IsOpen) EndConversation();
        activeConversation = conversation;
        ResolvePlayerReferences();
        playerController?.AcquireMovementLock(this);
        if (pauseWorldDuringDialogue) TimeManager.Instance?.AcquirePause(this);
        WorldInteractionPrompt.AcquireSuppression(this);
        ConversationStarted?.Invoke();
        EnterNode(entry);
        return true;
    }

    public void Continue()
    {
        if (!IsOpen || visibleChoices.Count > 0) return;
        if (string.IsNullOrWhiteSpace(currentNode.nextNodeId)) { EndConversation(); return; }
        EnterNode(activeConversation.FindNode(currentNode.nextNodeId));
    }

    public void SelectChoice(int visibleChoiceIndex)
    {
        if (!IsOpen || visibleChoiceIndex < 0 || visibleChoiceIndex >= visibleChoices.Count) return;
        DialogueChoice choice = visibleChoices[visibleChoiceIndex];
        ExecuteCommands(choice.commands);
        if (string.IsNullOrWhiteSpace(choice.nextNodeId)) { EndConversation(); return; }
        EnterNode(activeConversation.FindNode(choice.nextNodeId));
    }

    public void EndConversation()
    {
        if (!IsOpen) return;
        activeConversation = null;
        currentNode = null;
        visibleChoices.Clear();
        ReleaseModalLocks();
        ConversationEnded?.Invoke();
    }

    void EnterNode(DialogueNode node)
    {
        if (node == null)
        {
            Debug.LogWarning("[DIALOGUE] Link mengarah ke node yang tidak ada. Conversation ditutup.");
            EndConversation();
            return;
        }

        currentNode = node;
        ExecuteCommands(node.commands);
        if (!IsOpen) return;
        visibleChoices.Clear();
        if (node.choices != null)
            visibleChoices.AddRange(node.choices.Where(choice => choice != null && ConditionsMet(choice.conditions)));
        NodeChanged?.Invoke();
    }

    public bool ConditionsMet(IReadOnlyList<DialogueCondition> conditions)
    {
        if (conditions == null) return true;
        for (int i = 0; i < conditions.Count; i++)
        {
            DialogueCondition condition = conditions[i];
            if (condition == null) continue;
            bool result = EvaluateCondition(condition);
            if (condition.invert ? result : !result) return false;
        }
        return true;
    }

    bool EvaluateCondition(DialogueCondition condition)
    {
        ResolvePlayerReferences();
        return condition.type switch
        {
            DialogueConditionType.Always => true,
            DialogueConditionType.QuestStatus => QuestService.Instance != null &&
                QuestService.Instance.GetStatus(condition.quest) == condition.requiredQuestStatus,
            DialogueConditionType.HasItem => condition.item != null && playerInventory != null &&
                playerInventory.GetCount(condition.item) >= Mathf.Max(1, condition.amount),
            DialogueConditionType.HasGold => ScoreManager.Instance != null &&
                ScoreManager.Instance.points >= Mathf.Max(0, condition.amount),
            DialogueConditionType.Flag => GetFlag(condition.flagId) == condition.flagValue,
            DialogueConditionType.VillageLevel => VillageProgressionService.Instance != null &&
                VillageProgressionService.Instance.MeetsRequirement(condition.villageLevel),
            _ => false
        };
    }

    void ExecuteCommands(IReadOnlyList<DialogueCommand> commands)
    {
        if (commands == null) return;
        ResolvePlayerReferences();
        for (int i = 0; i < commands.Count; i++)
        {
            DialogueCommand command = commands[i];
            if (command == null) continue;
            switch (command.type)
            {
                case DialogueCommandType.SetFlag: SetFlag(command.flagId, command.flagValue); break;
                case DialogueCommandType.StartQuest: QuestService.Instance?.StartQuest(command.quest); break;
                case DialogueCommandType.CompleteQuest: QuestService.Instance?.TryTurnIn(command.quest); break;
                case DialogueCommandType.AddQuestProgress:
                    QuestService.Instance?.AddProgress(command.quest, command.objectiveId, Mathf.Max(1, command.amount));
                    break;
                case DialogueCommandType.GiveItem:
                    if (command.item != null) playerInventory?.Add(command.item, Mathf.Max(1, command.amount));
                    break;
                case DialogueCommandType.TakeItem:
                    if (command.item != null)
                    {
                        int amount = Mathf.Max(1, command.amount);
                        if (playerInventory != null && playerInventory.Remove(command.item, amount))
                            playerController?.PlayHandOverAnimation(amount > 1);
                    }
                    break;
                case DialogueCommandType.AddGold: ScoreManager.Instance?.AddPoints(command.goldAmount); break;
            }
        }
    }

    public bool GetFlag(string flagId) => !string.IsNullOrWhiteSpace(flagId) &&
        flags.TryGetValue(flagId.Trim(), out bool value) && value;

    public void SetFlag(string flagId, bool value)
    {
        if (string.IsNullOrWhiteSpace(flagId)) return;
        string id = flagId.Trim();
        if (flags.TryGetValue(id, out bool current) && current == value) return;
        flags[id] = value;
        FlagChanged?.Invoke(id, value);
    }

    void ResolvePlayerReferences()
    {
        if (playerInventory == null) playerInventory = FindFirstObjectByType<Inventory>();
        if (playerController == null)
            playerController = playerInventory != null ? playerInventory.GetComponent<PlayerController>() : FindFirstObjectByType<PlayerController>();
    }

    void ReleaseModalLocks()
    {
        playerController?.ReleaseMovementLock(this);
        TimeManager.Instance?.ReleasePause(this);
        WorldInteractionPrompt.ReleaseSuppression(this);
    }

    public DialogueSystemSaveData Capture() => new()
    {
        flags = flags.Select(pair => new DialogueFlagSaveData { flagId = pair.Key, value = pair.Value }).ToList()
    };

    public void Restore(DialogueSystemSaveData data)
    {
        flags.Clear();
        if (data?.flags == null) return;
        foreach (DialogueFlagSaveData flag in data.flags)
            if (flag != null && !string.IsNullOrWhiteSpace(flag.flagId)) flags[flag.flagId.Trim()] = flag.value;
    }
}
