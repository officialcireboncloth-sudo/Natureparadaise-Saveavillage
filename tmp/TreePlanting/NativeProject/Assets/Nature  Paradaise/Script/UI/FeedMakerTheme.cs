using UnityEngine;
[CreateAssetMenu(menuName="Nature Paradise/UI/Feed Maker Theme")]
public sealed class FeedMakerTheme : ScriptableObject
{
    [Header("Optional artwork — leave empty until sprites are ready")]
    public Sprite panel, logo, leafIcon, productionIcon, collectIcon;
    public Sprite poultryBag, livestockBag, fishBag;
    public Sprite poultryFeed, livestockFeed, fishFeed;
    public Sprite Bag(int category)=>category==2?fishBag:category==1?livestockBag:poultryBag;
    public Sprite Product(int category)=>category==2?fishFeed:category==1?livestockFeed:poultryFeed;
}
