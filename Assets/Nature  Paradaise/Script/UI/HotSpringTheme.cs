using UnityEngine;

/// <summary>Optional hot-spring HUD artwork; uses the existing shared gameplay palette.</summary>
[CreateAssetMenu(menuName = "Nature Paradise/UI/Hot Spring Theme")]
public sealed class HotSpringTheme : ScriptableObject
{
    [Header("Optional image slots")]
    public Sprite panel, weatherIcon, coinIcon, springIcon, exitIcon, staminaBackground, staminaFill;
}
