using UnityEngine;
[CreateAssetMenu(menuName="Nature Paradise/UI/Fish Pond Theme")]
public sealed class FishPondTheme : ScriptableObject
{
    [Header("Optional artwork — leave empty until sprites are supplied")]
    public Sprite panel, headerFish, bagIcon, pondIcon, detailIcon, feedBowl, feedBag, fishPlaceholder, waterIcon, leafDecoration;
}
