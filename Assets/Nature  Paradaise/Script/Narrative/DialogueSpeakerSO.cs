using UnityEngine;

[CreateAssetMenu(menuName = "Nature Paradise/Narrative/Dialogue Speaker", fileName = "Speaker_")]
public sealed class DialogueSpeakerSO : ScriptableObject
{
    public string speakerId = "npc.new";
    public string displayName = "NPC";
    public Sprite portrait;
    public Color nameColor = new(0.22f, 0.12f, 0.06f, 1f);
}
