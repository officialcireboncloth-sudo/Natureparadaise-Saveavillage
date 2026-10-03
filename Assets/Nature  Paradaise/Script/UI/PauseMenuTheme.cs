using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Nature Paradise/UI/Pause Menu Theme")]
public sealed class PauseMenuTheme : ScriptableObject
{
    [Header("Optional artwork slots (leave empty until artwork is ready)")]
    public Sprite background;
    public Sprite panel;
    public Sprite summaryIcon;
    public Sprite animalsIcon;
    public Sprite farmIcon;
    public Sprite questsIcon;
    public Sprite settingsIcon;
    public Sprite defaultNPCPortrait;
    public Sprite defaultAnimalPortrait;
    public Sprite defaultAnimalIllustration;
    public AnimalArtwork[] animals = Array.Empty<AnimalArtwork>();
    public QuestArtwork[] quests = Array.Empty<QuestArtwork>();
    [Header("Editor testing; used only while Game View has focus")]
    public KeyCode editorPauseKey = KeyCode.F7;
    public bool allowEscapeInEditor;

    [Serializable] public sealed class AnimalArtwork
    {
        public AnimalType type;
        public Sprite portrait;
        public Sprite illustration;
    }
    [Serializable] public sealed class QuestArtwork
    {
        public string questId;
        public Sprite giverPortrait;
    }
    public Sprite AnimalSprite(AnimalType type, bool illustration)
    {
        foreach (var entry in animals)
            if (entry != null && entry.type == type)
                return illustration ? entry.illustration : entry.portrait;
        return illustration ? defaultAnimalIllustration : defaultAnimalPortrait;
    }
    public Sprite QuestSprite(string id)
    {
        foreach (var entry in quests) if (entry != null && entry.questId == id) return entry.giverPortrait;
        return defaultNPCPortrait;
    }
}
