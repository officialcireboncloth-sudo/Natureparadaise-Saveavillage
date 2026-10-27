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
    CropBooster,
    Shears,
    Pitchfork,
    Milker
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
    [SerializeField] bool allowDebugToolShortcuts;
    [Header("Use Tool")]
    [SerializeField] KeyCode useToolKey = KeyCode.F;
    [SerializeField] bool allowLeftMouse;

    bool mobileUsePending;

    public PlayerToolType SelectedTool => selectedTool;
    public event Action<PlayerToolType> SelectionChanged;

    void Update()
    {
        if (!allowDebugToolShortcuts) return;
        if (GameplayInput.GetKeyDown(hoeKey)) ToggleTool(PlayerToolType.Hoe);
        else if (GameplayInput.GetKeyDown(seedKey)) ToggleTool(PlayerToolType.Seed);
        else if (GameplayInput.GetKeyDown(waterKey)) ToggleTool(PlayerToolType.WateringCan);
        else if (GameplayInput.GetKeyDown(fertilizerKey)) ToggleTool(PlayerToolType.Fertilizer);
        else if (GameplayInput.GetKeyDown(sickleKey)) ToggleTool(PlayerToolType.Sickle);
        else if (GameplayInput.GetKeyDown(hammerKey)) ToggleTool(PlayerToolType.Hammer);
        else if (GameplayInput.GetKeyDown(axeKey)) ToggleTool(PlayerToolType.Axe);
        else if (GameplayInput.GetKeyDown(cropBoosterKey)) ToggleTool(PlayerToolType.CropBooster);
    }

    /// <summary>True satu frame saat input penggunaan untuk tool aktif diterima.</summary>
    public bool IsUsePressed(PlayerToolType tool, UnityEngine.Object promptOwner)
    {
        if (selectedTool != tool)
            return false;

        if (!WorldInteractionPrompt.AllowsAction(promptOwner))
        {
            mobileUsePending = false;
            return false;
        }

        bool mousePressed = allowLeftMouse && GameplayInput.GetKeyDown(KeyCode.Mouse0);
        if (FertilizerProcessor.BlocksWorldPointer || MarketStand.BlocksWorldPointer ||
            ShippingBin.BlocksWorldPointer || ToolStorageChest.BlocksWorldPointer || Refrigerator.BlocksWorldPointer || HouseStorageChest.BlocksWorldPointer)
            mousePressed = false;
        if (mousePressed && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            mousePressed = false;

        bool keyboardPressed=PlayerInteractionTarget.PressAction(promptOwner, useToolKey);
        bool requested = keyboardPressed || mousePressed || mobileUsePending;
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
        mobileUsePending = false;
        SelectionChanged?.Invoke(tool);
    }

    /// <summary>User selection toggles the current tool off; programmatic SelectTool remains idempotent.</summary>
    public void ToggleTool(PlayerToolType tool)
    {
        var inventoryHotbar = GetComponent<InventoryHotbarUI>();
        if (tool == PlayerToolType.None || tool == selectedTool)
        {
            if (inventoryHotbar != null) inventoryHotbar.ClearSelection();
            else { CancelCurrentAction(); SelectTool(PlayerToolType.None); }
            return;
        }
        var inventory = GetComponent<Inventory>();
        if (inventoryHotbar != null && inventory != null)
            for (int i = 0; i < inventory.HotbarSlotCount; i++)
                if (inventory.GetSlot(i)?.item?.equippedTool == tool)
                { inventoryHotbar.SelectSlot(i); return; }
        CancelCurrentAction();
        SelectTool(tool);
    }

    /// <summary>Stops pending tool contacts and releases only action-owned locks when putting tools away.</summary>
    public void CancelCurrentAction()
    {
        mobileUsePending = false;
        GetComponent<PlayerToolApproach>()?.Cancel();
        GetComponent<FarmingTool>()?.CancelAction();
        GetComponent<SeedTool>()?.CancelAction();
        GetComponent<PlayerGatheringTool>()?.CancelAction();
        var movement = GetComponent<PlayerController>();
        AnimalManureSystem.Instance?.CancelCollection(movement);
        var care = AnimalController.CurrentCareAction;
        if (movement != null && care != null && care.CarePlayer == movement) care.CancelCareAction(false);
        GetComponent<FishingSystem>()?.CancelToolUse();
        GetComponent<PlayerHeldTools>()?.EndWorkAction();
        movement?.CancelToolAnimation();
    }

    // Untuk Unity Button mobile.
    /// <summary>Memilih tool berdasarkan urutan shortcut legacy.</summary>
    public void SelectToolByIndex(int toolIndex)
    {
        if (Enum.IsDefined(typeof(PlayerToolType), toolIndex))
            ToggleTool((PlayerToolType)toolIndex);
    }

    public void RequestUseTool(){if(GetComponent<PlayerToolApproach>()?.IsActive!=true)mobileUsePending=true;}

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
            PlayerToolType.Shears => "Shears",
            PlayerToolType.Pitchfork => "Garpu",
            PlayerToolType.Milker => "Milker",
            _ => "Tangan Kosong"
        };
    }
}
