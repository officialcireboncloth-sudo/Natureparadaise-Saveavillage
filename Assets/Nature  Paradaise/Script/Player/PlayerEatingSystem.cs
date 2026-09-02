using System;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(Inventory), typeof(PlayerStatusSystem))]
/// <summary>
/// Memvalidasi item makanan, mengonsumsi satu stack, dan menerapkan pemulihan status player.
/// Item mentah ditolak sampai memiliki status ReadyToEat.
/// </summary>
public sealed class PlayerEatingSystem : MonoBehaviour
{
    [SerializeField] Inventory inventory;
    [SerializeField] PlayerStatusSystem status;

    [Header("Food Selection")]
    [FormerlySerializedAs("debugFood")]
    [SerializeField] ItemSO selectedFood;

    [Header("Keyboard Testing")]
    [SerializeField] KeyCode eatKey = KeyCode.C;

    public ItemSO SelectedFood => selectedFood;
    public event Action<ItemSO> FoodEaten;

    void Awake()
    {
        if (inventory == null)
            inventory = GetComponent<Inventory>();
        if (status == null)
            status = GetComponent<PlayerStatusSystem>();
    }

    void Update()
    {
        if (Input.GetKeyDown(eatKey))
            EatSelectedFood();
    }

    /// <summary>Memvalidasi kategori, preparation, inventory, dan manfaat status makanan.</summary>
    public bool CanEat(ItemSO item)
    {
        return item != null && item.CanConsume && inventory != null && status != null &&
               inventory.GetCount(item) > 0 &&
               status.WouldBenefitFromFood(item.healthRestore, item.staminaRestore, item.hungerRestore);
    }

    /// <summary>Mengonsumsi satu item valid dan menerapkan efeknya ke status player.</summary>
    public bool TryEat(ItemSO item)
    {
        if (!CanEat(item) || !inventory.Remove(item, 1))
            return false;

        status.ApplyFood(item.healthRestore, item.staminaRestore, item.hungerRestore);
        status.PulseActivity(PlayerMovementState.Eating, 0.65f);
        FoodEaten?.Invoke(item);
        SaveLoadFeedback.Instance?.ShowMessage($"Makan {item.itemName}");
        Debug.Log($"[PLAYER] Memakan {item.itemName}.");
        return true;
    }

    /// <summary>Mengganti makanan yang dipilih UI/hotbar.</summary>
    public void SetSelectedFood(ItemSO item)
    {
        selectedFood = item;
    }

    public void EatSelectedFood()
    {
        ItemSO food = CanEat(selectedFood) ? selectedFood : FindFirstAvailableFood();
        if (food == null)
        {
            string message = selectedFood != null && selectedFood.IsRawFood
                ? $"{selectedFood.itemName} masih mentah"
                : "Tidak ada makanan siap makan";
            SaveLoadFeedback.Instance?.ShowMessage(message);
            Debug.Log("[PLAYER] Tidak ada makanan, atau status masih penuh.");
            return;
        }

        TryEat(food);
    }

    // Tetap tersedia agar tombol UI lama tidak putus.
    public void EatDebugFood()
    {
        EatSelectedFood();
    }

    ItemSO FindFirstAvailableFood()
    {
        if (inventory == null)
            return null;

        for (int i = 0; i < inventory.slots.Count; i++)
        {
            ItemStack stack = inventory.slots[i];
            if (stack != null && stack.count > 0 && CanEat(stack.item))
                return stack.item;
        }

        return null;
    }
}
