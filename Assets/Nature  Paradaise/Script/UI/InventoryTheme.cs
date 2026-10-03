using UnityEngine;

[CreateAssetMenu(menuName = "Nature Paradise/UI/Inventory Theme")]
public sealed class InventoryTheme : ScriptableObject
{
    [Header("Optional artwork — empty slots stay empty")]
    public Sprite background;
    public Sprite panel;
    public Sprite slot;
    public Sprite button;
    public Sprite allIcon;
    public Sprite toolsIcon;
    public Sprite plantsIcon;
    public Sprite fishIcon;
    public Sprite materialsIcon;
    public Sprite coinIcon;
    public Sprite trashIcon;
    public Sprite filledStar;
    public Sprite emptyStar;
}
