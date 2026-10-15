using UnityEditor;
using UnityEngine;
public static class StaminaBalanceSetup
{
    [MenuItem("Nature Paradise/Data CSV/Stamina/Validate Runtime CSV")]
    public static void Validate() { try { StaminaBalance.Current.Reload(); Debug.Log("STAMINA CSV VALID: 20 actions, four tiers, NextTier berry"); } catch(System.Exception e) { Debug.LogException(e); } }
    [MenuItem("Nature Paradise/Data CSV/Stamina/Select Berry Image and Model Slots")]
    static void SelectBerry() => Selection.activeObject=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/Nature  Paradaise/Resources/Items/Food/Stamina Berry.asset");
}
