using UnityEngine;

[CreateAssetMenu(menuName = "Nature Paradise/Narrative/Dialogue Speaker", fileName = "Speaker_")]
public sealed class DialogueSpeakerSO : ScriptableObject
{
    public string speakerId = "npc.new";
    public string displayName = "NPC";
    [Tooltip("Portrait NPC untuk panel dialog. Boleh kosong.")] public Sprite portrait;
    public string roleDescription;
    public Color nameColor = new(0.22f, 0.12f, 0.06f, 1f);
}

