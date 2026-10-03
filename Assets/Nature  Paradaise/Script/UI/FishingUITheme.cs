using UnityEngine;

[CreateAssetMenu(menuName="Nature Paradise/UI/Fishing Theme")]
public sealed class FishingUITheme : ScriptableObject
{
    [Header("Optional artwork — empty slots until sprites are supplied")]
    public Sprite panel, fishSilhouette, reelIndicator, progressRing, upwardArrow, catchPlaceholder;
}
