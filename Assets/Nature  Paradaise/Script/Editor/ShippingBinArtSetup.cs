#if UNITY_EDITOR
using UnityEditor;

/// <summary>Reapply the current articulated bin art without replacing its gameplay identity.</summary>
public static class ShippingBinArtSetup
{
    const string Root="Assets/Nature  Paradaise";
    public const string ModelPath=Root+"/mesh/World/ShippingBin/ShippingBin_A.fbx";
    public const string VisualPath=Root+"/Prefabs/World/ShippingBin/ShippingBinVisual.prefab";
    public const string GameplayPath=Root+"/Prefabs/World/ShippingBin.prefab";
    [MenuItem("Nature Paradise/Art/Apply Atlas Shipping Bin")]
    public static void Apply()=>MarketPropsCozySetup.Apply("ShippingBin");
}
#endif
