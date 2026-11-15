using UnityEngine;

/// <summary>Shared companion panel palette and optional sprite overrides; vector icons remain readable without art.</summary>
[CreateAssetMenu(menuName="Nature Paradise/UI/Companion Command Theme")]
public sealed class CompanionCommandTheme : ScriptableObject
{
    [Header("Panel")]
    public Color panel = new(.12f,.18f,.10f,.95f);
    public Color border = new(.55f,.63f,.44f,.65f);
    public Color row = new(.22f,.28f,.19f,.94f);
    public Color selectedRow = new(.32f,.42f,.27f,.98f);
    public Color mountRow = new(.36f,.54f,.30f,.98f);
    public Color text = new(.96f,.96f,.91f,1);
    public Color muted = new(.74f,.78f,.69f,1);
    public Color affection = new(.94f,.43f,.34f,1);
    public Color active = new(.64f,.82f,.48f,1);
    [Header("Optional artwork")]
    public Sprite horseIcon, dogIcon, catIcon, followIcon, stayIcon, homeIcon, mountIcon;
}
