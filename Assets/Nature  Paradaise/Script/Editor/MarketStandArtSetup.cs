#if UNITY_EDITOR
using UnityEditor;

/// <summary>Reapply the current shared-atlas art to the existing market gameplay prefab.</summary>
public static class MarketStandArtSetup
{
    const string Root="Assets/Nature  Paradaise";
    public const string ModelPath=Root+"/mesh/World/MarketStand/MarketStand_A.fbx";
    public const string VisualPath=Root+"/Prefabs/World/MarketStand/MarketStandVisual.prefab";
    public const string GameplayPath=Root+"/Prefabs/World/MarketStand.prefab";
    public const float VisualScale=.8f;
    [MenuItem("Nature Paradise/Art/Apply Atlas Market Stand")]
    public static void Apply()=>MarketPropsCozySetup.Apply("MarketStand");
}
#endif
