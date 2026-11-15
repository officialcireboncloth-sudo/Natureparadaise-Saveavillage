using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueExpression { Neutral, Happy, Annoyed, Sad, Surprised, Thinking }

[Serializable]
public sealed class DialogueExpressionPortrait
{
    public DialogueExpression expression;
    public Sprite portrait;
}

[CreateAssetMenu(menuName = "Nature Paradise/Narrative/Dialogue Speaker", fileName = "Speaker_")]
public sealed class DialogueSpeakerSO : ScriptableObject
{
    public string speakerId = "npc.new";
    public string displayName = "NPC";
    [Tooltip("Aktifkan untuk speaker player, agar portrait selalu berada di sisi kiri.")]
    public bool isPlayer;
    [Tooltip("Portrait NPC untuk panel dialog. Boleh kosong.")] public Sprite portrait;
    [Tooltip("Preset wajah per ekspresi. Preset kosong memakai portrait netral.")]
    public List<DialogueExpressionPortrait> expressions = new();
    public string roleDescription;
    public Color nameColor = new(0.22f, 0.12f, 0.06f, 1f);

    /// <summary>Resolves authored emotion names while retaining legacy portrait-only speakers.</summary>
    public Sprite GetPortrait(string emotion)
    {
        var requested = ParseExpression(emotion);
        Sprite neutral = portrait;
        if (expressions != null)
            foreach (var entry in expressions)
            {
                if (entry == null || entry.portrait == null) continue;
                if (entry.expression == requested) return entry.portrait;
                if (entry.expression == DialogueExpression.Neutral) neutral = entry.portrait;
            }
        return neutral;
    }

    public static DialogueExpression ParseExpression(string emotion) => emotion?.Trim().ToLowerInvariant() switch
    {
        "happy" or "friendly" or "senang" or "cheerful" => DialogueExpression.Happy,
        "angry" or "annoyed" or "kesal" or "marah" => DialogueExpression.Annoyed,
        "sad" or "sedih" or "worried" => DialogueExpression.Sad,
        "surprised" or "surprise" or "kaget" => DialogueExpression.Surprised,
        "thinking" or "thoughtful" or "question" or "reminder" or "berpikir" => DialogueExpression.Thinking,
        _ => DialogueExpression.Neutral
    };
}

