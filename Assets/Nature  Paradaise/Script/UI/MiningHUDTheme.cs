using UnityEngine;

[CreateAssetMenu(menuName = "Nature Paradise/UI/Mining HUD Theme")]
public sealed class MiningHUDTheme : ScriptableObject
{
    [Header("Optional Sprite slots; item artwork uses ItemSO.icon")]
    public Sprite mineIcon, coinIcon, notificationPanel, staminaBackground, staminaFill;
}
