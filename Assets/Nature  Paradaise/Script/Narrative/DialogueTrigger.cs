using System.Collections.Generic;
using UnityEngine;

/// <summary>Adapter tipis pada NPC/world object untuk memilih conversation berdasarkan condition.</summary>
[DisallowMultipleComponent]
public sealed class DialogueTrigger : MonoBehaviour
{
    [SerializeField] string speakerTargetId = "npc.new";
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField] float promptHeight = 1.7f;
    [SerializeField] string promptText = "E: Bicara";
    [SerializeField] DialogueConversationSO defaultConversation;
    [Tooltip("Urutan atas memiliki prioritas lebih tinggi.")]
    [SerializeField] List<ConditionalConversation> conditionalConversations = new();
    [SerializeField] Inventory playerInventory;

    void Awake()
    {
        if (playerInventory == null) playerInventory = FindFirstObjectByType<Inventory>();
    }

    void Update()
    {
        if (playerInventory == null) playerInventory = FindFirstObjectByType<Inventory>();
        if (playerInventory == null || DialogueService.Instance == null || DialogueService.Instance.IsOpen) return;
        if (!PlayerInteractionTarget.Contains(playerInventory.transform, transform)) return;

        float distance = Vector3.Distance(playerInventory.transform.position, transform.position);
        WorldInteractionPrompt.Request(this, transform, promptText, distance, promptHeight);
        if (!PlayerInteractionTarget.Press(playerInventory.transform, transform, interactKey)) return;

        DialogueConversationSO selected = SelectConversation();
        if (selected == null)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Belum ada percakapan yang tersedia.");
            return;
        }

        if (DialogueService.Instance.StartConversation(selected))
            QuestEventHub.Publish(QuestObjectiveType.Talk, speakerTargetId);
    }

    DialogueConversationSO SelectConversation()
    {
        if (conditionalConversations != null)
            foreach (ConditionalConversation entry in conditionalConversations)
                if (entry?.conversation != null && DialogueService.Instance.ConditionsMet(entry.conditions))
                    return entry.conversation;
        return defaultConversation;
    }
}
