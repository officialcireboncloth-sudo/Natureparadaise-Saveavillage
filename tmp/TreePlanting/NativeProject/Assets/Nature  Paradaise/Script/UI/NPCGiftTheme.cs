using UnityEngine;

/// <summary>Optional artwork; colors and typography come from the shared gameplay UI.</summary>
[CreateAssetMenu(menuName = "Nature Paradise/UI/NPC Gift Theme")]
public sealed class NPCGiftTheme : ScriptableObject
{
    [Header("Optional image slots")]
    public Sprite panel, card, button, headerIcon, giveIcon, qualityStar, decoration;
}
