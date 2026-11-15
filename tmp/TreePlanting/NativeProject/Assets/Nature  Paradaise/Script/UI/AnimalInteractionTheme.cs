using System;
using UnityEngine;

[CreateAssetMenu(menuName="Nature Paradise/UI/Animal Interaction Theme")]
public sealed class AnimalInteractionTheme : ScriptableObject
{
    [Header("Image slots — leave empty until artwork is ready")]
    public Sprite panel, portraitFrame, defaultPortrait;
    public Sprite ageIcon, affectionIcon, femaleIcon, maleIcon, milkIcon, moodIcon;
    public Sprite fullHeart, halfHeart, emptyHeart;
    public Sprite sunIcon, coinIcon, brushIcon, shearsIcon, productIcon;
    public Sprite brushReactionBubble;
    public AnimalPortrait[] portraits=Array.Empty<AnimalPortrait>();
    [Serializable] public sealed class AnimalPortrait { public AnimalType type; public Sprite portrait; }
    public Sprite Portrait(AnimalType type)
    {
        foreach(var entry in portraits) if(entry!=null && entry.type==type) return entry.portrait!=null?entry.portrait:defaultPortrait;
        return defaultPortrait;
    }
}
