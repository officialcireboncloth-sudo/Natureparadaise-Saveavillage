using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Nature Paradise/Narrative/Quest Definition", fileName = "Quest_")]
public sealed class QuestDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public string questId = "quest.new";
    public string title = "New Quest";
    [TextArea(2, 6)] public string description;
    public string category = "Story";
    public Sprite icon;

    [Header("Availability")]
    public List<QuestDefinitionSO> prerequisites = new();
    [Min(1)] public int requiredVillageLevel = 1;
    public bool repeatable;

    [Header("Progress")]
    public QuestCompletionMode completionMode = QuestCompletionMode.TurnIn;
    public List<QuestObjectiveDefinition> objectives = new();

    [Header("Rewards")]
    [Min(0)] public int goldReward;
    [Tooltip("Condition Point desa yang diberikan ketika quest selesai.")]
    [Min(0)] public int villageConditionReward;
    public List<QuestItemReward> itemRewards = new();

    public string Id => string.IsNullOrWhiteSpace(questId) ? name : questId.Trim();
}
