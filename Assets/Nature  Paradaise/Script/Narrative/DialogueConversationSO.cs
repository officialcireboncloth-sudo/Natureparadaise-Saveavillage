using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Nature Paradise/Narrative/Dialogue Conversation", fileName = "Dialogue_")]
public sealed class DialogueConversationSO : ScriptableObject
{
    public string conversationId = "dialogue.new";
    public DialogueSpeakerSO defaultSpeaker;
    [Tooltip("Opsional: player khusus percakapan ini. Kosong memakai Narrative/Speaker_Player.")]
    public DialogueSpeakerSO playerSpeaker;
    [Tooltip("Lawan bicara saat node player sedang aktif. Kosong memakai NPC dari default speaker/node.")]
    public DialogueSpeakerSO counterpartSpeaker;
    public string entryNodeId = "start";
    public List<DialogueNode> nodes = new();

    public string Id => string.IsNullOrWhiteSpace(conversationId) ? name : conversationId.Trim();

    public DialogueNode FindNode(string id) =>
        nodes?.Find(node => node != null && node.nodeId == id);
}
