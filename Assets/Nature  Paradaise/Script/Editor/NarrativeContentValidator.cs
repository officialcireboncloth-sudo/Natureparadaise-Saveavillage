using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Validasi authoring untuk ID dan link narrative; hanya berjalan melalui menu Editor.</summary>
public static class NarrativeContentValidator
{
    [MenuItem("Nature Paradise/Narrative/Validate Content")]
    public static void ValidateContent()
    {
        int errors = ValidateQuests() + ValidateDialogues();
        if (errors == 0) Debug.Log("[NARRATIVE] Validasi selesai: semua ID dan link valid.");
        else Debug.LogError($"[NARRATIVE] Validasi menemukan {errors} masalah. Periksa Console.");
    }

    static int ValidateQuests()
    {
        int errors = 0;
        HashSet<string> questIds = new(System.StringComparer.OrdinalIgnoreCase);
        foreach (string guid in AssetDatabase.FindAssets("t:QuestDefinitionSO"))
        {
            QuestDefinitionSO quest = AssetDatabase.LoadAssetAtPath<QuestDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (quest == null) continue;
            if (!questIds.Add(quest.Id)) errors += Report(quest, $"Quest ID duplicate: '{quest.Id}'.");
            HashSet<string> objectiveIds = new(System.StringComparer.OrdinalIgnoreCase);
            if (quest.objectives == null) continue;
            foreach (QuestObjectiveDefinition objective in quest.objectives)
            {
                if (objective == null || string.IsNullOrWhiteSpace(objective.objectiveId))
                    errors += Report(quest, "Objective memiliki ID kosong.");
                else if (!objectiveIds.Add(objective.objectiveId))
                    errors += Report(quest, $"Objective ID duplicate: '{objective.objectiveId}'.");
            }
        }
        return errors;
    }

    static int ValidateDialogues()
    {
        int errors = 0;
        HashSet<string> conversationIds = new(System.StringComparer.OrdinalIgnoreCase);
        foreach (string guid in AssetDatabase.FindAssets("t:DialogueConversationSO"))
        {
            DialogueConversationSO conversation = AssetDatabase.LoadAssetAtPath<DialogueConversationSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (conversation == null) continue;
            if (!conversationIds.Add(conversation.Id)) errors += Report(conversation, $"Conversation ID duplicate: '{conversation.Id}'.");
            HashSet<string> nodeIds = new(System.StringComparer.OrdinalIgnoreCase);
            if (conversation.nodes != null)
                foreach (DialogueNode node in conversation.nodes)
                    if (node == null || string.IsNullOrWhiteSpace(node.nodeId)) errors += Report(conversation, "Node memiliki ID kosong.");
                    else if (!nodeIds.Add(node.nodeId)) errors += Report(conversation, $"Node ID duplicate: '{node.nodeId}'.");

            if (!nodeIds.Contains(conversation.entryNodeId)) errors += Report(conversation, $"Entry node '{conversation.entryNodeId}' tidak ada.");
            if (conversation.nodes == null) continue;
            foreach (DialogueNode node in conversation.nodes)
            {
                if (node == null) continue;
                if (!string.IsNullOrWhiteSpace(node.nextNodeId) && !nodeIds.Contains(node.nextNodeId))
                    errors += Report(conversation, $"Node '{node.nodeId}' menuju node yang tidak ada: '{node.nextNodeId}'.");
                if (node.choices == null) continue;
                foreach (DialogueChoice choice in node.choices)
                    if (choice != null && !string.IsNullOrWhiteSpace(choice.nextNodeId) && !nodeIds.Contains(choice.nextNodeId))
                        errors += Report(conversation, $"Choice pada '{node.nodeId}' menuju node yang tidak ada: '{choice.nextNodeId}'.");
            }
        }
        return errors;
    }

    static int Report(Object context, string message)
    {
        Debug.LogError($"[NARRATIVE] {context.name}: {message}", context);
        return 1;
    }
}
