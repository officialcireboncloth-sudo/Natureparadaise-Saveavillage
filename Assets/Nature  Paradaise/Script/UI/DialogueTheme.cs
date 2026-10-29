using UnityEngine;
[CreateAssetMenu(menuName="Nature Paradise/UI/Dialogue Theme")]
public sealed class DialogueTheme : ScriptableObject
{
    [Header("Optional image slots")]
    public Sprite panel, relationshipIcon;
    [Header("Dialogue portraits")]
    [Tooltip("Fallback jika speaker NPC belum memiliki portrait.")]
    public Sprite missingPortrait;
    public Color panelColor = new(.97f, .94f, .86f, 1f);
    public Color textColor = new(.19f, .16f, .12f, 1f);
    public Color accentColor = new(.40f, .48f, .34f, 1f);
    [Range(.35f, 1f)] public float listenerBrightness = .68f;
}
