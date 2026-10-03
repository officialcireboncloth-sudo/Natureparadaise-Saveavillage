using UnityEngine;

[CreateAssetMenu(menuName="Nature Paradise/UI/Storage Chest Theme")]
public sealed class StorageChestTheme : ScriptableObject
{
    [Header("Image slots — leave empty until artwork is ready")]
    public Sprite panel, logo, leafIcon, chestIcon, bagIcon;
    public Sprite storeIcon, takeIcon, storeAllIcon, takeAllIcon;
}
