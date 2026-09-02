using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Jenis tool gameplay yang dapat dipilih melalui hotbar.</summary>
public enum PlayerToolType
{
    None,
    Hoe,
    Seed,
    WateringCan,
    Fertilizer,
    Sickle,
    Hammer,
    Axe,
    FishingRod,
    CropBooster
}

[DisallowMultipleComponent]
/// <summary>
/// Menyimpan pilihan tool aktif dan menyatukan input penggunaan tool dari keyboard,
/// mouse, hotbar inventory, dan tombol mobile.
/// </summary>
public sealed class PlayerToolHotbar : MonoBehaviour
{
    [SerializeField] PlayerToolType selectedTool = PlayerToolType.Hoe;
    [Header("PC Selection")]
    [Tooltip("Shortcut debug. Pemilihan normal berasal dari item hotbar 1-4.")]
    [SerializeField] KeyCode hoeKey = KeyCode.F1;
    [SerializeField] KeyCode seedKey = KeyCode.F2;
    [SerializeField] KeyCode waterKey = KeyCode.F3;
    [SerializeField] KeyCode fertilizerKey = KeyCode.F4;
    [SerializeField] KeyCode sickleKey = KeyCode.F5;
    [SerializeField] KeyCode hammerKey = KeyCode.F6;
    [SerializeField] KeyCode axeKey = KeyCode.F7;
    [SerializeField] KeyCode cropBoosterKey = KeyCode.F8;
    [Header("Use Tool")]
    [SerializeField] KeyCode useToolKey = KeyCode.F;
    [SerializeField] bool allowLeftMouse = true;

    bool mobileUsePending;

    public PlayerToolType SelectedTool => selectedTool;
    public event Action<PlayerToolType> SelectionChanged;

    void Update()
    {
        if (Input.GetKeyDown(hoeKey)) SelectTool(PlayerToolType.Hoe);
        else if (Input.GetKeyDown(seedKey)) SelectTool(PlayerToolType.Seed);
        else if (Input.GetKeyDown(waterKey)) SelectTool(PlayerToolType.WateringCan);
        else if (Input.GetKeyDown(fertilizerKey)) SelectTool(PlayerToolType.Fertilizer);
        else if (Input.GetKeyDown(sickleKey)) SelectTool(PlayerToolType.Sickle);
        else if (Input.GetKeyDown(hammerKey)) SelectTool(PlayerToolType.Hammer);
        else if (Input.GetKeyDown(axeKey)) SelectTool(PlayerToolType.Axe);
        else if (Input.GetKeyDown(cropBoosterKey)) SelectTool(PlayerToolType.CropBooster);
    }

    /// <summary>True satu frame saat input penggunaan untuk tool aktif diterima.</summary>
    public bool IsUsePressed(PlayerToolType tool)
    {
        if (selectedTool != tool)
            return false;

        bool mousePressed = allowLeftMouse && Input.GetKeyDown(KeyCode.Mouse0);
        if (mousePressed && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            mousePressed = false;

        bool requested = Input.GetKeyDown(useToolKey) || mousePressed || mobileUsePending;
        if (mobileUsePending)
            mobileUsePending = false;
        return requested;
    }

    /// <summary>Mengganti tool aktif dan memberitahu listener UI/gameplay.</summary>
    public void SelectTool(PlayerToolType tool)
    {
        if (selectedTool == tool)
            return;
        selectedTool = tool;
        SelectionChanged?.Invoke(tool);
        SaveLoadFeedback.Instance?.ShowMessage($"Tool: {GetDisplayName(tool)}");
    }

    // Untuk Unity Button mobile.
    /// <summary>Memilih tool berdasarkan urutan shortcut legacy.</summary>
    public void SelectToolByIndex(int toolIndex)
    {
        if (Enum.IsDefined(typeof(PlayerToolType), toolIndex))
            SelectTool((PlayerToolType)toolIndex);
    }

    public void RequestUseTool() => mobileUsePending = true;

    public static string GetDisplayName(PlayerToolType tool)
    {
        return tool switch
        {
            PlayerToolType.Hoe => "Hoe",
            PlayerToolType.Seed => "Seed",
            PlayerToolType.WateringCan => "Water",
            PlayerToolType.Fertilizer => "Fertilizer",
            PlayerToolType.Sickle => "Sickle",
            PlayerToolType.Hammer => "Hammer",
            PlayerToolType.Axe => "Axe",
            PlayerToolType.FishingRod => "Fishing Rod",
            PlayerToolType.CropBooster => "Crop Booster",
            _ => "Empty"
        };
    }
}
