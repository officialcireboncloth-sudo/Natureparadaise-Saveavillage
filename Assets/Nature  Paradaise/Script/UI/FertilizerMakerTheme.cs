using UnityEngine;

[CreateAssetMenu(menuName = "Nature Paradise/UI/Fertilizer Maker Theme")]
public sealed class FertilizerMakerTheme : ScriptableObject
{
    public Sprite background;
    [Tooltip("Dasar, Plus, Super, Premium. Authored sprites live in Textures/UI/Icons/FertilizerMaker; empty entries use the sack fallback.")]
    public Sprite[] fertilizerIcons = new Sprite[4];
    public Sprite fishIcon, manureIcon, shellIcon, mineralIcon;
    public Color panel = new(.145f, .19f, .17f, .96f);
    public Color border = new(.64f, .70f, .64f, .45f);
    public Color selected = new(.22f, .29f, .20f, .82f);
    public Color selectedBorder = new(.79f, .91f, .70f, 1);
}
