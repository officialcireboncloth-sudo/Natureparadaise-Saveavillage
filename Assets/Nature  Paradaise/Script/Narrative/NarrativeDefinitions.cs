using System;
using System.Collections.Generic;
using UnityEngine;

public enum QuestStatus { Locked, Available, Active, ReadyToTurnIn, Completed, Failed }
public enum QuestCompletionMode { AutoComplete, TurnIn }
public enum QuestObjectiveType { Talk, Collect, Harvest, Ship, MarketSale, Defeat, Tame, Build, Custom }

[Serializable]
public sealed class QuestObjectiveDefinition
{
    public string objectiveId = "objective-01";
    [TextArea] public string description;
    public QuestObjectiveType type;
    [Tooltip("ID NPC, nama asset item, creature ID, building ID, atau custom target.")]
    public string targetId;
    [Tooltip("Reference item opsional untuk objective Collect/Harvest/Ship/Market Sale.")]
    public ItemSO targetItem;
    [Min(1)] public int requiredAmount = 1;
    [Tooltip("Untuk Collect: hitung item yang sudah dimiliki ketika quest diterima.")]
    public bool countExistingInventory = true;
}

[Serializable]
public sealed class QuestItemReward
{
    public ItemSO item;
    [Min(1)] public int amount = 1;
    [Range(0, 5)] public int qualityStars;
}

public enum DialogueConditionType
{
    Always,
    QuestStatus,
    HasItem,
    HasGold,
    Flag,
    VillageLevel
}

[Serializable]
public sealed class DialogueCondition
{
    public DialogueConditionType type;
    public bool invert;
    public QuestDefinitionSO quest;
    public QuestStatus requiredQuestStatus = QuestStatus.Active;
    public ItemSO item;
    [Min(1)] public int amount = 1;
    public string flagId;
    public bool flagValue = true;
    [Min(1)] public int villageLevel = 1;
}

public enum DialogueCommandType
{
    None,
    SetFlag,
    StartQuest,
    CompleteQuest,
    AddQuestProgress,
    GiveItem,
    TakeItem,
    AddGold
}

[Serializable]
public sealed class DialogueCommand
{
    public DialogueCommandType type;
    public string flagId;
    public bool flagValue = true;
    public QuestDefinitionSO quest;
    public string objectiveId;
    [Min(1)] public int amount = 1;
    public ItemSO item;
    public int goldAmount;
}

[Serializable]
public sealed class DialogueChoice
{
    public string text = "Continue";
    public string nextNodeId;
    public List<DialogueCondition> conditions = new();
    public List<DialogueCommand> commands = new();
}

[Serializable]
public sealed class DialogueNode
{
    public string nodeId = "start";
    public DialogueSpeakerSO speakerOverride;
    [TextArea(2, 8)] public string text;
    [Tooltip("Kunci localization opsional; text tetap menjadi fallback.")]
    public string localizationKey;
    public string emotion;
    public string nextNodeId;
    public List<DialogueCommand> commands = new();
    public List<DialogueChoice> choices = new();
}

[Serializable]
public sealed class ConditionalConversation
{
    public DialogueConversationSO conversation;
    public List<DialogueCondition> conditions = new();
}
