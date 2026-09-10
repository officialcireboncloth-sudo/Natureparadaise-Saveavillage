using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Nature Paradise/Narrative/Dialogue Conversation", fileName = "Dialogue_")]
public sealed class DialogueConversationSO : ScriptableObject
{
    public string conversationId = "dialogue.new";
    public DialogueSpeakerSO defaultSpeaker;
    public string entryNodeId = "start";
    public List<DialogueNode> nodes = new();

    public string Id => string.IsNullOrWhiteSpace(conversationId) ? name : conversationId.Trim();

    public DialogueNode FindNode(string id) =>
        nodes?.Find(node => node != null && node.nodeId == id);
}
