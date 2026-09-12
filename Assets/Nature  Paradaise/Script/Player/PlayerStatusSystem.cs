using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>State utama player yang dapat dibaca animator, HUD, save, dan gameplay lain.</summary>
public enum PlayerMovementState
{
    Idle,
    Walk,
    Run,
    Carry,
    ToolAction,
    Fishing,
    Eating,
    Sleeping,
    Faint
}

/// <summary>Kategori slot status effect; detail perilaku effect disediakan modul terpisah nanti.</summary>
public enum PlayerStatusEffectType
{
    Buff,
    Debuff
}

/// <summary>Satu slot effect yang menyimpan identitas stabil dan sisa durasi tanpa menerapkan efek.</summary>
[Serializable]
public sealed class PlayerStatusEffectSlot
{
    [SerializeField] string effectId;
    [SerializeField, Min(0f)] float remainingDuration;

    public string EffectId => effectId;
    public float RemainingDuration => remainingDuration;
    public bool IsOccupied => !string.IsNullOrWhiteSpace(effectId);

    internal void Assign(string id, float duration)
    {
        effectId = id;
        remainingDuration = Mathf.Max(0f, duration);
    }

    internal void Clear()
    {
        effectId = string.Empty;
        remainingDuration = 0f;
    }

    internal bool Tick(float deltaTime)
    {
        if (!IsOccupied || remainingDuration <= 0f)
            return false;

        remainingDuration = Mathf.Max(0f, remainingDuration - deltaTime);
        if (remainingDuration > 0f)
            return false;

        Clear();
        return true;
    }
}

/// <summary>Data save satu buff/debuff aktif beserta index slotnya.</summary>
[Serializable]
public sealed class PlayerStatusEffectSaveData
{
    public int slotIndex;
    public string effectId;
    public float remainingDuration;
}

/// <summary>Level progression untuk satu jenis tool.</summary>
[Serializable]
public sealed class PlayerToolLevelData
{
    public PlayerToolType tool;
    [Min(1)] public int level = 1;
}

[DisallowMultipleComponent]
/// <summary>
/// Sumber data HP, stamina, dan hunger player. Menjaga nilai tetap valid serta mengirim event
/// perubahan status dan faint kepada HUD, lifecycle, tool, dan sistem makanan.
/// </summary>
public sealed class PlayerStatusSystem : MonoBehaviour
{
    // Feature flag sementara. Ubah ke true ketika hunger siap diaktifkan kembali.
    const bool HungerFeatureAvailable = false;

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
    [Tooltip("Matikan jika desain game tidak menggunakan hunger.")]
    [SerializeField] bool hungerEnabled;

    [Header("Exhaustion")]
    [Tooltip("Player dianggap lelah saat stamina berada di bawah persentase ini.")]
    [SerializeField, Range(0f, 1f)] float exhaustionThreshold = 0.1f;

    [Header("Tool Progression")]
    [SerializeField] List<PlayerToolLevelData> toolLevels = new();

    [Header("Buff / Debuff Slots")]
    [Tooltip("Slot data kosong untuk effect positif modular. Timer belum berjalan otomatis.")]
    [SerializeField, Min(1)] int buffSlotCount = 6;
    [SerializeField] List<PlayerStatusEffectSlot> buffSlots = new();
    [Tooltip("Slot data kosong untuk effect negatif modular. Timer belum berjalan otomatis.")]
    [SerializeField, Min(1)] int debuffSlotCount = 6;
    [SerializeField] List<PlayerStatusEffectSlot> debuffSlots = new();

    readonly Dictionary<object, PlayerMovementState> activityOwners = new();
    PlayerController movement;
    PlayerToolHotbar toolHotbar;
    Inventory inventory;
    HeldItemPlacementSystem heldItemSystem;
    bool faintSignalSent;
    PlayerMovementState currentMovementState;
    float statusEffectNotifyTimer;

    public float MaxHealth => maxHealth;
    public float Health => currentHealth;
    public float MaxStamina => maxStamina;
    public float Stamina => currentStamina;
    public float MaxHunger => maxHunger;
    public float Hunger => currentHunger;
    public bool HungerEnabled => HungerFeatureAvailable && hungerEnabled;
    public bool IsExhausted => !IsFainted && currentStamina <= maxStamina * exhaustionThreshold;
    public bool IsFainted => currentHealth <= 0f;
    public PlayerMovementState CurrentMovementState => currentMovementState;
    public PlayerToolType CurrentTool
    {
        get
        {
            if (toolHotbar == null) toolHotbar = GetComponent<PlayerToolHotbar>();
            return toolHotbar != null ? toolHotbar.SelectedTool : PlayerToolType.None;
        }
    }
    public ItemSO HeldItem
    {
        get
        {
            if (heldItemSystem == null) heldItemSystem = GetComponent<HeldItemPlacementSystem>();
            return heldItemSystem != null ? heldItemSystem.HeldItem : null;
        }
    }
    public int CurrentMoney => ScoreManager.Instance != null ? ScoreManager.Instance.points : 0;
    public int CurrentBagLevel => inventory != null ? inventory.BackpackLevel : 0;
    public string PlayerLocation => SceneManager.GetActiveScene().name;
    public int CurrentDay => TimeManager.Instance != null ? TimeManager.Instance.day : 1;
    public int CurrentHour => TimeManager.Instance != null ? TimeManager.Instance.hour : 0;
    public int CurrentMinute => TimeManager.Instance != null ? TimeManager.Instance.minute : 0;
    public IReadOnlyList<PlayerStatusEffectSlot> BuffSlots => buffSlots;
    public IReadOnlyList<PlayerStatusEffectSlot> DebuffSlots => debuffSlots;

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
        movement = GetComponent<PlayerController>();
        toolHotbar = GetComponent<PlayerToolHotbar>();
        inventory = GetComponent<Inventory>();
        heldItemSystem = GetComponent<HeldItemPlacementSystem>();
        EnsureEffectSlots();
        EnsureToolLevels();
        RefreshMovementState();
    }

    void Update()
    {
        bool effectExpired = TickStatusEffects(buffSlots, Time.deltaTime) |
                             TickStatusEffects(debuffSlots, Time.deltaTime);
        bool hasTimedEffect = HasTimedStatusEffect(buffSlots) || HasTimedStatusEffect(debuffSlots);
        statusEffectNotifyTimer -= Time.deltaTime;
        if (effectExpired || (hasTimedEffect && statusEffectNotifyTimer <= 0f))
        {
            statusEffectNotifyTimer = 0.25f;
            NotifyChanged();
        }

        RefreshMovementState();
    }

    void OnEnable()
    {
        TimeManager.OnHour += HandleGameHour;
    }

    void OnDisable()
    {
        TimeManager.OnHour -= HandleGameHour;
        StopAllCoroutines();
        activityOwners.Clear();
    }

    /// <summary>Memeriksa apakah stamina cukup tanpa mengubah nilai.</summary>
    public bool CanSpendStamina(float amount)
    {
        float effectiveAmount = GetWeatherAdjustedStaminaCost(amount);
        return !IsFainted && amount >= 0f && currentStamina >= effectiveAmount;
    }

    /// <summary>Mengurangi stamina secara atomik jika jumlahnya cukup.</summary>
    public bool TrySpendStamina(float amount)
    {
        float effectiveAmount = GetWeatherAdjustedStaminaCost(amount);
        if (!CanSpendStamina(amount))
        {
            if (!IsFainted && WeatherSystem.Instance != null &&
                WeatherSystem.IsPlayerOutdoors() && WeatherSystem.Instance.CurrentWeather == WeatherType.WindRainStorm)
            {
                currentStamina = 0f;
                NotifyChanged();
                PlayerLifeCycle lifeCycle = GetComponent<PlayerLifeCycle>();
                if (lifeCycle != null)
                    lifeCycle.RequestWeatherFaint(12, false, "Kamu memaksakan aktivitas saat hujan angin dan pingsan.");
            }
            return false;
        }

        if (amount <= 0f)
            return true;

        currentStamina -= effectiveAmount;
        NotifyChanged();
        return true;
    }

    float GetWeatherAdjustedStaminaCost(float amount)
    {
        if (amount <= 0f || WeatherSystem.Instance == null || !WeatherSystem.IsPlayerOutdoors())
            return amount;
        return amount * WeatherSystem.GetOutdoorStaminaMultiplier(WeatherSystem.Instance.CurrentWeather);
    }

    public void RestoreStamina(float amount)
    {
        SetStamina(currentStamina + Mathf.Max(0f, amount));
    }

    /// <summary>
    /// Hook modular untuk sumber pemulihan selain makanan, misalnya hot spring.
    /// Pemanggil menentukan jumlah HP/stamina dan durasi/interaksinya sendiri.
    /// </summary>
    public void RestoreFromRecoverySource(float healthAmount, float staminaAmount)
    {
        if (IsFainted || (healthAmount <= 0f && staminaAmount <= 0f))
            return;

        currentHealth = Mathf.Clamp(currentHealth + Mathf.Max(0f, healthAmount), 0f, maxHealth);
        currentStamina = Mathf.Clamp(currentStamina + Mathf.Max(0f, staminaAmount), 0f, maxStamina);
        NotifyChanged();
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

    /// <summary>Memicu faint dari waktu atau bahaya yang tidak memakai damage biasa.</summary>
    public void ForceFaint()
    {
        if (IsFainted || faintSignalSent)
            return;

        currentHealth = 0f;
        faintSignalSent = true;
        RefreshMovementState();
        NotifyChanged();
        Fainted?.Invoke();
    }

    /// <summary>Meningkatkan atau mengatur batas status untuk progression.</summary>
    public void SetMaximumStatus(float health, float stamina, bool refill = false)
    {
        maxHealth = Mathf.Max(1f, health);
        maxStamina = Mathf.Max(1f, stamina);
        currentHealth = refill ? maxHealth : Mathf.Min(currentHealth, maxHealth);
        currentStamina = refill ? maxStamina : Mathf.Min(currentStamina, maxStamina);
        NotifyChanged();
    }

    /// <summary>Memulihkan konfigurasi progression dan pilihan hunger dari save.</summary>
    public void RestoreStatusConfiguration(float health, float stamina, float hunger, bool enableHunger)
    {
        maxHealth = Mathf.Max(1f, health);
        maxStamina = Mathf.Max(1f, stamina);
        maxHunger = Mathf.Max(1f, hunger);
        hungerEnabled = enableHunger;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        currentStamina = Mathf.Min(currentStamina, maxStamina);
        currentHunger = Mathf.Min(currentHunger, maxHunger);
        NotifyChanged();
    }

    /// <summary>Mengaktifkan atau menonaktifkan hunger tanpa membuang nilai yang tersimpan.</summary>
    public void SetHungerEnabled(bool enabledState)
    {
        if (hungerEnabled == enabledState)
            return;
        hungerEnabled = enabledState;
        NotifyChanged();
    }

    /// <summary>Hook modular untuk sistem fishing yang akan mengelola cast dan minigame.</summary>
    public bool TrySpendFishingStamina(float amount)
    {
        bool spent = TrySpendStamina(amount);
        if (spent)
            PulseActivity(PlayerMovementState.Fishing, 0.5f);
        return spent;
    }

    /// <summary>Memulai sesi fishing panjang dan mempertahankan state sampai EndFishing dipanggil.</summary>
    public bool BeginFishing(object owner, float initialStaminaCost)
    {
        if (owner == null || !TrySpendStamina(initialStaminaCost))
            return false;
        AcquireActivity(owner, PlayerMovementState.Fishing);
        return true;
    }

    public void EndFishing(object owner) => ReleaseActivity(owner);

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

    /// <summary>Mengisi slot kosong atau menyegarkan slot dengan Effect ID yang sama.</summary>
    public bool TryAssignStatusEffect(PlayerStatusEffectType type, string effectId, float duration)
    {
        if (string.IsNullOrWhiteSpace(effectId))
            return false;

        List<PlayerStatusEffectSlot> slots = GetEffectSlots(type);
        PlayerStatusEffectSlot target = slots.Find(slot => slot.IsOccupied && slot.EffectId == effectId)
            ?? slots.Find(slot => !slot.IsOccupied);
        if (target == null)
            return false;

        target.Assign(effectId, duration);
        NotifyChanged();
        return true;
    }

    /// <summary>Mengosongkan effect tertentu tanpa mengetahui implementasi perilakunya.</summary>
    public bool ClearStatusEffect(PlayerStatusEffectType type, string effectId)
    {
        if (string.IsNullOrWhiteSpace(effectId))
            return false;

        PlayerStatusEffectSlot target = GetEffectSlots(type)
            .Find(slot => slot.IsOccupied && slot.EffectId == effectId);
        if (target == null)
            return false;

        target.Clear();
        NotifyChanged();
        return true;
    }

    /// <summary>Mengambil level tool terpadu; tool yang belum tersimpan dimulai dari level satu.</summary>
    public int GetToolLevel(PlayerToolType tool)
    {
        PlayerToolLevelData entry = toolLevels.Find(item => item != null && item.tool == tool);
        return entry != null ? Mathf.Max(1, entry.level) : 1;
    }

    /// <summary>Mengubah level tool dan memberitahu sistem yang memakai snapshot status.</summary>
    public void SetToolLevel(PlayerToolType tool, int level)
    {
        if (tool == PlayerToolType.None)
            return;

        EnsureToolLevels();
        PlayerToolLevelData entry = toolLevels.Find(item => item.tool == tool);
        int validatedLevel = Mathf.Max(1, level);
        if (entry.level == validatedLevel)
            return;
        entry.level = validatedLevel;
        NotifyChanged();
    }

    public List<PlayerToolLevelData> CaptureToolLevels()
    {
        EnsureToolLevels();
        List<PlayerToolLevelData> result = new();
        foreach (PlayerToolLevelData entry in toolLevels)
            result.Add(new PlayerToolLevelData { tool = entry.tool, level = entry.level });
        return result;
    }

    public void RestoreToolLevels(List<PlayerToolLevelData> savedLevels)
    {
        EnsureToolLevels();
        if (savedLevels != null)
        {
            foreach (PlayerToolLevelData saved in savedLevels)
                if (saved != null && saved.tool != PlayerToolType.None)
                    SetToolLevel(saved.tool, saved.level);
        }
        NotifyChanged();
    }

    /// <summary>Menahan activity state sampai owner melepasnya; state berprioritas tinggi menang.</summary>
    public void AcquireActivity(object owner, PlayerMovementState state)
    {
        if (owner == null)
            return;
        activityOwners[owner] = state;
        RefreshMovementState();
    }

    public void ReleaseActivity(object owner)
    {
        if (owner != null && activityOwners.Remove(owner))
            RefreshMovementState();
    }

    /// <summary>Menampilkan state aksi singkat tanpa mewajibkan setiap tool membuat coroutine sendiri.</summary>
    public void PulseActivity(PlayerMovementState state, float duration = 0.35f)
    {
        StartCoroutine(PulseActivityRoutine(new object(), state, duration));
    }

    /// <summary>Mengambil hanya slot aktif agar format save tetap ringkas.</summary>
    public List<PlayerStatusEffectSaveData> CaptureStatusEffects(PlayerStatusEffectType type)
    {
        List<PlayerStatusEffectSlot> slots = GetEffectSlots(type);
        List<PlayerStatusEffectSaveData> result = new();
        for (int index = 0; index < slots.Count; index++)
        {
            PlayerStatusEffectSlot slot = slots[index];
            if (!slot.IsOccupied)
                continue;
            result.Add(new PlayerStatusEffectSaveData
            {
                slotIndex = index,
                effectId = slot.EffectId,
                remainingDuration = slot.RemainingDuration
            });
        }
        return result;
    }

    /// <summary>Mengembalikan isi slot dari save tanpa menjalankan effect gameplay.</summary>
    public void RestoreStatusEffects(
        List<PlayerStatusEffectSaveData> savedBuffs,
        List<PlayerStatusEffectSaveData> savedDebuffs)
    {
        EnsureEffectSlots();
        RestoreEffectList(buffSlots, savedBuffs);
        RestoreEffectList(debuffSlots, savedDebuffs);
        NotifyChanged();
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
        if (IsFainted || !HungerEnabled)
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

    System.Collections.IEnumerator PulseActivityRoutine(object owner, PlayerMovementState state, float duration)
    {
        AcquireActivity(owner, state);
        if (duration > 0f)
            yield return new WaitForSeconds(duration);
        else
            yield return null;
        ReleaseActivity(owner);
    }

    void RefreshMovementState()
    {
        PlayerMovementState next = IsFainted ? PlayerMovementState.Faint : ResolveOwnedActivity();
        if (!IsFainted && next == PlayerMovementState.Idle && activityOwners.Count == 0)
        {
            if (movement != null && movement.IsCarrying)
                next = PlayerMovementState.Carry;
            else if (movement != null)
                next = movement.CurrentMode == PlayerController.MovementMode.Walk
                    ? PlayerMovementState.Walk
                    : movement.CurrentMode == PlayerController.MovementMode.Run ||
                      movement.CurrentMode == PlayerController.MovementMode.Sprint ||
                      movement.CurrentMode == PlayerController.MovementMode.Airborne
                        ? PlayerMovementState.Run
                        : PlayerMovementState.Idle;
        }

        if (currentMovementState == next)
            return;
        currentMovementState = next;
        NotifyChanged();
    }

    PlayerMovementState ResolveOwnedActivity()
    {
        PlayerMovementState result = PlayerMovementState.Idle;
        int bestPriority = -1;
        foreach (PlayerMovementState state in activityOwners.Values)
        {
            int priority = GetActivityPriority(state);
            if (priority <= bestPriority)
                continue;
            bestPriority = priority;
            result = state;
        }
        return result;
    }

    static int GetActivityPriority(PlayerMovementState state)
    {
        return state switch
        {
            PlayerMovementState.Faint => 100,
            PlayerMovementState.Sleeping => 90,
            PlayerMovementState.Eating => 70,
            PlayerMovementState.Fishing => 60,
            PlayerMovementState.ToolAction => 50,
            _ => 0
        };
    }

    static bool TickStatusEffects(List<PlayerStatusEffectSlot> slots, float deltaTime)
    {
        bool expired = false;
        foreach (PlayerStatusEffectSlot slot in slots)
            expired |= slot != null && slot.Tick(deltaTime);
        return expired;
    }

    static bool HasTimedStatusEffect(List<PlayerStatusEffectSlot> slots)
    {
        foreach (PlayerStatusEffectSlot slot in slots)
            if (slot != null && slot.IsOccupied && slot.RemainingDuration > 0f)
                return true;
        return false;
    }

    List<PlayerStatusEffectSlot> GetEffectSlots(PlayerStatusEffectType type)
        => type == PlayerStatusEffectType.Buff ? buffSlots : debuffSlots;

    void EnsureEffectSlots()
    {
        buffSlotCount = Mathf.Max(1, buffSlotCount);
        debuffSlotCount = Mathf.Max(1, debuffSlotCount);
        buffSlots ??= new List<PlayerStatusEffectSlot>();
        debuffSlots ??= new List<PlayerStatusEffectSlot>();
        EnsureSlotCapacity(buffSlots, buffSlotCount);
        EnsureSlotCapacity(debuffSlots, debuffSlotCount);
    }

    void EnsureToolLevels()
    {
        toolLevels ??= new List<PlayerToolLevelData>();
        toolLevels.RemoveAll(entry => entry == null || entry.tool == PlayerToolType.None);
        foreach (PlayerToolType tool in Enum.GetValues(typeof(PlayerToolType)))
        {
            if (tool != PlayerToolType.None && toolLevels.Find(entry => entry.tool == tool) == null)
                toolLevels.Add(new PlayerToolLevelData { tool = tool, level = 1 });
        }
        foreach (PlayerToolLevelData entry in toolLevels)
            entry.level = Mathf.Max(1, entry.level);
    }

    static void EnsureSlotCapacity(List<PlayerStatusEffectSlot> slots, int count)
    {
        // Unity dapat mempertahankan elemen null pada list serialized setelah perubahan Inspector.
        // Normalisasi dahulu supaya pencarian dan proses save slot selalu aman.
        for (int index = 0; index < slots.Count; index++)
            slots[index] ??= new PlayerStatusEffectSlot();

        while (slots.Count < count)
            slots.Add(new PlayerStatusEffectSlot());
        while (slots.Count > count && !slots[^1].IsOccupied)
            slots.RemoveAt(slots.Count - 1);
    }

    static void RestoreEffectList(
        List<PlayerStatusEffectSlot> slots,
        List<PlayerStatusEffectSaveData> savedEffects)
    {
        foreach (PlayerStatusEffectSlot slot in slots)
            slot.Clear();
        if (savedEffects == null)
            return;

        foreach (PlayerStatusEffectSaveData saved in savedEffects)
        {
            if (saved == null || saved.slotIndex < 0 || saved.slotIndex >= slots.Count ||
                string.IsNullOrWhiteSpace(saved.effectId))
            {
                continue;
            }
            slots[saved.slotIndex].Assign(saved.effectId, saved.remainingDuration);
        }
    }

    void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        maxStamina = Mathf.Max(1f, maxStamina);
        maxHunger = Mathf.Max(1f, maxHunger);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
        EnsureEffectSlots();
        EnsureToolLevels();
    }
}
