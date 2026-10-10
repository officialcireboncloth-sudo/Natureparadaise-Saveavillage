using UnityEngine;

[CreateAssetMenu(menuName="Nature Paradise/UI/Kitchen Theme")]
public sealed class KitchenTheme : ScriptableObject
{
    [Header("Optional artwork — item icons come from the shared ItemSO")]
    public Sprite panel, leafIcon, cookIcon, sufficientIcon, missingIcon, lockedRecipeIcon;
}
