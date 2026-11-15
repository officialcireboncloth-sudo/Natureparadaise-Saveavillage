using UnityEngine;

[CreateAssetMenu(menuName = "Nature Paradise/UI/Bed Rest Theme")]
public sealed class BedRestTheme : ScriptableObject
{
    [Header("Image slots — optional artwork")]
    public Sprite panel;
    public Sprite sleepIcon;
    public Sprite sleepAndSaveIcon;
    public Sprite loadIcon;
    public Sprite backIcon;
}
