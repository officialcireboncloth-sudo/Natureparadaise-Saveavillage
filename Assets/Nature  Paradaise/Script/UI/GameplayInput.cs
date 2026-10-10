using UnityEngine;

/// <summary>Gameplay keys are blocked while paused, including the frame that opens/closes it.</summary>
public static class GameplayInput
{
    static int consumedFrame = -1;
    public static bool ConsumedThisFrame => consumedFrame == Time.frameCount;
    static bool Blocked => KitchenUI.Instance != null || ConsumedThisFrame || LumberConstructionMenu.Active != null || DataDrivenModal.Active != null || AquariumUI.Active != null || MarketStand.IsAnyOpen || UpgradeShopFront.Active != null || ShopFront.IsAnyOpen || GameplayPauseMenu.BlocksGameplayInput || BedRestMenu.IsOpen || StorageChestUI.IsOpen || WeatherForecastTVUI.Instance != null || FeedMakerUI.Instance != null || FishPondUI.Instance != null || AnimalBellUI.Instance != null || (DialogueService.Instance != null && DialogueService.Instance.IsOpen);
    public static void ConsumeCurrentFrame() => consumedFrame = Time.frameCount;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => consumedFrame = -1;
    public static bool GetKeyDown(KeyCode key) => !Blocked && Input.GetKeyDown(key);
    public static bool GetKey(KeyCode key) => !Blocked && Input.GetKey(key);
    public static bool GetKeyDown(string key) => !Blocked && Input.GetKeyDown(key);
    public static bool GetKey(string key) => !Blocked && Input.GetKey(key);
    public static bool GetMouseButton(int button) => !Blocked && Input.GetMouseButton(button);
    public static bool GetMouseButtonDown(int button) => !Blocked && Input.GetMouseButtonDown(button);
    public static bool GetMouseButtonUp(int button) => !Blocked && Input.GetMouseButtonUp(button);
}

