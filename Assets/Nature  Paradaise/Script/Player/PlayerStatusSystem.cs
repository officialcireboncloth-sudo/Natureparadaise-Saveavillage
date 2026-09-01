using System;
using UnityEngine;

[DisallowMultipleComponent]
/// <summary>
/// Sumber data HP, stamina, dan hunger player. Menjaga nilai tetap valid serta mengirim event
/// perubahan status dan faint kepada HUD, lifecycle, tool, dan sistem makanan.
/// </summary>
public sealed class PlayerStatusSystem : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1f)] float maxHealth = 100f;
    [SerializeField] float currentHealth = 100f;

    [Header("Stamina")]
    [SerializeField, Min(1f)] float maxStamina = 100f;
    [SerializeField] float currentStamina = 100f;

    [Header("Hunger / Fullness")]
    [Tooltip("100 berarti kenyang, 0 berarti kelaparan.")]
    [SerializeField, Min(1f)] float maxHunger = 100f;
    [SerializeField] float currentHunger = 100f;
    [SerializeField, Min(0f)] float hungerLossPerGameHour = 1f;
    [SerializeField, Min(0f)] float starvationDamagePerGameHour = 2f;

    bool faintSignalSent;

    public float MaxHealth => maxHealth;
    public float Health => currentHealth;
    public float MaxStamina => maxStamina;
    public float Stamina => currentStamina;
    public float MaxHunger => maxHunger;
    public float Hunger => currentHunger;
    public bool IsFainted => currentHealth <= 0f;

    public event Action<PlayerStatusSystem> Changed;
    public event Action Fainted;

    void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        maxStamina = Mathf.Max(1f, maxStamina);
        maxHunger = Mathf.Max(1f, maxHunger);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
    }

    void OnEnable()
    {
        TimeManager.OnHour += HandleGameHour;
    }

    void OnDisable()
    {
        TimeManager.OnHour -= HandleGameHour;
    }

    /// <summary>Memeriksa apakah stamina cukup tanpa mengubah nilai.</summary>
    public bool CanSpendStamina(float amount)
    {
        return !IsFainted && amount >= 0f && currentStamina >= amount;
    }

    /// <summary>Mengurangi stamina secara atomik jika jumlahnya cukup.</summary>
    public bool TrySpendStamina(float amount)
    {
        if (!CanSpendStamina(amount))
            return false;

        if (amount <= 0f)
            return true;

        currentStamina -= amount;
        NotifyChanged();
        return true;
    }

    public void RestoreStamina(float amount)
    {
        SetStamina(currentStamina + Mathf.Max(0f, amount));
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || IsFainted)
            return;

        SetHealth(currentHealth + amount);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || IsFainted)
            return;

        SetHealth(currentHealth - amount);
        if (currentHealth > 0f || faintSignalSent)
            return;

        faintSignalSent = true;
        Fainted?.Invoke();
    }

    /// <summary>Menerapkan seluruh efek konsumsi dan mengirim satu event perubahan.</summary>
    public void ApplyFood(float health, float stamina, float hunger)
    {
        if (IsFainted)
            return;

        currentHealth = Mathf.Clamp(currentHealth + Mathf.Max(0f, health), 0f, maxHealth);
        currentStamina = Mathf.Clamp(currentStamina + Mathf.Max(0f, stamina), 0f, maxStamina);
        currentHunger = Mathf.Clamp(currentHunger + Mathf.Max(0f, hunger), 0f, maxHunger);
        NotifyChanged();
    }

    public bool WouldBenefitFromFood(float health, float stamina, float hunger)
    {
        if (IsFainted)
            return false;

        return (health > 0f && currentHealth < maxHealth) ||
               (stamina > 0f && currentStamina < maxStamina) ||
               (hunger > 0f && currentHunger < maxHunger);
    }

    /// <summary>Memulihkan status sesuai aturan tidur.</summary>
    public void RestoreAfterSleep()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currentHunger = Mathf.Max(currentHunger, maxHunger * 0.5f);
        faintSignalSent = false;
        NotifyChanged();
    }

    /// <summary>Memulihkan persentase status setelah player faint.</summary>
    public void RestoreAfterFaint(float healthPercent, float staminaPercent)
    {
        currentHealth = maxHealth * Mathf.Clamp01(healthPercent);
        currentStamina = maxStamina * Mathf.Clamp01(staminaPercent);
        currentHunger = Mathf.Max(currentHunger, maxHunger * 0.25f);
        faintSignalSent = false;
        NotifyChanged();
    }

    /// <summary>Menerapkan status hasil load dengan validasi rentang.</summary>
    public void RestoreSavedState(float health, float stamina, float hunger)
    {
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        currentStamina = Mathf.Clamp(stamina, 0f, maxStamina);
        currentHunger = Mathf.Clamp(hunger, 0f, maxHunger);
        faintSignalSent = currentHealth <= 0f;
        NotifyChanged();
    }

    void HandleGameHour()
    {
        if (IsFainted)
            return;

        currentHunger = Mathf.Max(0f, currentHunger - hungerLossPerGameHour);
        if (currentHunger <= 0f && starvationDamagePerGameHour > 0f)
        {
            TakeDamage(starvationDamagePerGameHour);
            return;
        }

        NotifyChanged();
    }

    void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        NotifyChanged();
    }

    void SetStamina(float value)
    {
        currentStamina = Mathf.Clamp(value, 0f, maxStamina);
        NotifyChanged();
    }

    void NotifyChanged()
    {
        Changed?.Invoke(this);
    }

    void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        maxStamina = Mathf.Max(1f, maxStamina);
        maxHunger = Mathf.Max(1f, maxHunger);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
    }
}
