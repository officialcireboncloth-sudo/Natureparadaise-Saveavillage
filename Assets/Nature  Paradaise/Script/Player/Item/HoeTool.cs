using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>Level upgrade hoe yang menentukan pola tile yang terkena aksi.</summary>
public enum HoeUpgradeTier
{
    Basic,
    Copper,
    Silver,
    Gold
}

/// <summary>
/// Mengarahkan hoe, watering can, dan fertilizer ke tile di depan player,
/// memvalidasi hambatan/field, memakai stamina, serta mengirim feedback aksi.
/// </summary>
[DefaultExecutionOrder(1000)]
public class FarmingTool : MonoBehaviour
{
    [Header("Raycast")]
    [Tooltip("Layer Field dan Crop.")]
    public LayerMask fieldMask;

    [Header("Legacy Input (Disabled - use held hotbar item + F)")]
    [HideInInspector] public KeyCode actionKey = KeyCode.H;
    [HideInInspector] public KeyCode waterKey = KeyCode.V;
    [HideInInspector] public KeyCode fertilizeKey = KeyCode.N;
    [HideInInspector] public KeyCode cropBoosterKey = KeyCode.M;
    [SerializeField] KeyCode handHarvestKey = KeyCode.E;

    [Header("Raycast Distance")]
    public float maxDist = 100f;

    [Header("Inventory")]
    public Inventory playerInv;

    [Header("Player Stamina")]
    public PlayerStatusSystem playerStatus;
    [Min(0f)] public float hoeCost = 0.1f;
    [Min(0f)] public float waterCost = 0.05f;
    [Min(0f)] public float fertilizeCost = 0.05f;
    [Min(0f)] public float harvestCost = 0.05f;
    [Min(0f)] public float cropBoosterCost = 0.05f;

    [Header("Directional Target")]
    [SerializeField, Min(0.5f)] float targetDistance = 2.1f;
    [SerializeField] LayerMask blockerMask = ~0;
    [SerializeField, Min(0.02f)] float targetRefreshInterval = 0.05f;
    [SerializeField] Color validHighlightColor = new(0.2f, 1f, 0.3f, 0.95f);
    [SerializeField] Color invalidHighlightColor = new(1f, 0.18f, 0.12f, 0.95f);

    [Header("Hoe Upgrade")]
    [SerializeField] HoeUpgradeTier upgradeTier = HoeUpgradeTier.Basic;
    [SerializeField, Range(0.25f, 1f)] float upgradedStaminaEfficiency = 0.8f;

    [Header("Tool Feedback")]
    [SerializeField] Animator animator;
    [SerializeField] string useToolTrigger = "UseTool";
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip hoeSound;
    [SerializeField] ParticleSystem dirtParticles;
    [SerializeField, Min(0f)] float actionLockDuration = 0.28f;
    [SerializeField, Min(0f)] float cameraShakeStrength = 0.07f;
    [SerializeField, Min(0f)] float cameraShakeDuration = 0.1f;
    [SerializeField] bool controllerRumble = true;

    PlayerToolHotbar hotbar;
    SeedTool seedTool;
    InventoryHotbarUI inventoryHotbar;
    PlayerController movement;
    TopDownCameraFollow cameraFollow;
    WateringCanSystem wateringCan;
    FieldArea currentField;
    int currentX = -1;
    int currentZ = -1;
    float nextTargetRefresh;
    bool actionBusy;
    bool hasUseToolTrigger;
    readonly List<HoeTileTarget> hoeTargets = new(9);
    readonly List<LineRenderer> indicators = new(9);
    readonly Collider[] blockerBuffer = new Collider[24];
    Material validIndicatorMaterial;
    Material invalidIndicatorMaterial;
    HoeUpgradeTier CurrentUpgradeTier => playerStatus != null
        ? (HoeUpgradeTier)Mathf.Clamp(playerStatus.GetToolLevel(PlayerToolType.Hoe) - 1, 0, (int)HoeUpgradeTier.Gold)
        : upgradeTier;

    void Awake()
    {
        if (playerInv == null)
            playerInv = FindFirstObjectByType<Inventory>();
        if (playerStatus == null && playerInv != null)
            playerStatus = playerInv.GetComponent<PlayerStatusSystem>();
        if (playerStatus != null && upgradeTier != HoeUpgradeTier.Basic)
            playerStatus.SetToolLevel(PlayerToolType.Hoe, (int)upgradeTier + 1);

        hotbar = GetComponent<PlayerToolHotbar>();
        if (hotbar == null)
            hotbar = gameObject.AddComponent<PlayerToolHotbar>();
        inventoryHotbar = GetComponent<InventoryHotbarUI>();
        movement = GetComponent<PlayerController>();
        wateringCan = GetComponent<WateringCanSystem>();
        if (wateringCan == null) wateringCan = gameObject.AddComponent<WateringCanSystem>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        cameraFollow = Camera.main != null ? Camera.main.GetComponent<TopDownCameraFollow>() : null;
        hasUseToolTrigger = HasAnimatorParameter(useToolTrigger, AnimatorControllerParameterType.Trigger);
        EnsureDirtParticles();
    }

    void Update()
    {
        if (Time.unscaledTime >= nextTargetRefresh)
        {
            nextTargetRefresh = Time.unscaledTime + targetRefreshInterval;
            RefreshCurrentTarget();
            RefreshIndicators();
        }

        RefreshCropInteractionPrompt();
        RefreshSoilInteractionPrompt();

        if (actionBusy)
            return;
        if (movement != null && movement.IsMovementLocked)
            return;

        if (TryUseHandHarvest()) return;

        bool useHoe = hotbar.IsUsePressed(PlayerToolType.Hoe);
        bool useWater = hotbar.IsUsePressed(PlayerToolType.WateringCan);
        bool useFertilizer = hotbar.IsUsePressed(PlayerToolType.Fertilizer);
        bool useCropBooster = hotbar.IsUsePressed(PlayerToolType.CropBooster);

        if (useHoe) UseHoe();
        else if (useWater) UseSoilEffect(true);
        else if (useFertilizer) UseSoilEffect(false);
        else if (useCropBooster) UseCropBooster();
    }

    void RefreshCropInteractionPrompt()
    {
        if (movement != null && movement.IsMovementLocked) return;
        if (currentField == null ||
            !currentField.TryGetSnapshot(currentX, currentZ, out FieldTileSnapshot snapshot) ||
            snapshot.State != TileState.Planted || snapshot.Crop == null)
            return;

        Vector3 promptPosition = currentField.GridToWorld(currentX, currentZ) + currentField.transform.up * 1.05f;
        if (currentField.TryGetCropView(currentX, currentZ, out FieldCropView cropView))
            promptPosition = cropView.transform.position + Vector3.up * 1.05f;

        string cropName = snapshot.Crop.produceItem != null
            ? snapshot.Crop.produceItem.itemName
            : snapshot.Crop.cropId;

        // Nama tanaman adalah informasi gameplay pada satu tile di depan player.
        // Detail growth/care tetap mengikuti toggle debug, bukan nama tanamannya.
        if (!HUDManager.FarmingDebugCluesEnabled)
        {
            string label = snapshot.CropState == CropLifecycleState.HarvestReady
                ? $"{cropName}\n{handHarvestKey} - Panen dengan tangan"
                : cropName;
            WorldInteractionPrompt.Request(this, promptPosition, label,
                Vector3.Distance(transform.position, promptPosition));
            return;
        }

        string stageName = snapshot.Crop.GetStageName(snapshot.GrowthStage);
        string waterClue = snapshot.WateredToday
            ? "sudah disiram hari ini"
            : "perlu air - tekan V";
        string fertilizerClue = snapshot.FertilizedForCurrentCycle
            ? "tanah sudah dipupuk"
            : snapshot.SoilLevel < 5
                ? "soil perlu pupuk setelah panen"
                : "soil sangat subur";
        string boosterClue = snapshot.BoosterLevelToday > 0
            ? $"booster kualitas Lv.{snapshot.BoosterLevelToday} hari ini"
            : "booster belum aktif";

        string text = snapshot.CropState switch
        {
            CropLifecycleState.HarvestReady => $"{cropName} siap panen - tekan {handHarvestKey} dengan tangan",
            CropLifecycleState.Withered => $"{cropName} layu - perlu air untuk pulih | {fertilizerClue}",
            CropLifecycleState.Regrowing =>
                $"{cropName} tumbuh kembali {Mathf.CeilToInt(snapshot.RegrowDaysRemaining)} hari | {waterClue}",
            CropLifecycleState.Dead => "Tanaman mati - bersihkan dengan Sickle",
            _ => $"{stageName} {cropName} | Growth {Mathf.FloorToInt(snapshot.GrowthDays)}/" +
                 $"{Mathf.CeilToInt(snapshot.Crop.TotalGrowthDays)} hari | " +
                 $"{waterClue} | {fertilizerClue} | {boosterClue}"
        };
        float distance = Vector3.Distance(transform.position, promptPosition);
        WorldInteractionPrompt.Request(this, promptPosition, text, distance);
    }

    void RefreshSoilInteractionPrompt()
    {
        if (movement != null && movement.IsMovementLocked) return;
        if (!HUDManager.FarmingDebugCluesEnabled)
            return;

        if (currentField == null ||
            !currentField.TryGetSnapshot(currentX, currentZ, out FieldTileSnapshot snapshot) ||
            snapshot.State != TileState.Hoed)
            return;

        Vector3 promptPosition = currentField.GridToWorld(currentX, currentZ) + currentField.transform.up * 0.75f;
        string condition = snapshot.FertilizedForCurrentCycle
            ? "Tanah sudah dipupuk"
            : snapshot.SoilLevel < 5
                ? "Tanah perlu pupuk - pilih Fertilizer lalu tekan N"
                : "Tanah sangat subur - pupuk belum diperlukan";
        string water = snapshot.WateredToday ? "sudah disiram" : "belum disiram";
        string text = $"{condition} | {water} | Pilih bibit lalu tekan F / Klik Kiri " +
                      $"(Soil {snapshot.SoilDurability}/80)";
        float distance = Vector3.Distance(transform.position, promptPosition);
        WorldInteractionPrompt.Request(this, promptPosition, text, distance);
    }

    public bool TryGetCurrentTile(out FieldArea field, out int x, out int z)
    {
        RefreshCurrentTarget();
        field = currentField;
        x = currentX;
        z = currentZ;
        return field != null && field.TryGetSnapshot(x, z, out _);
    }

    /// <summary>Memakai target yang sama dengan hoe/watering untuk nama dan interaksi pohon.</summary>
    public bool IsCurrentTileTarget(Vector3 worldPosition)
    {
        return TryGetCurrentTile(out FieldArea field, out int x, out int z) &&
            Mathf.Abs(field.transform.InverseTransformPoint(worldPosition).y) <= Mathf.Max(1f, field.CellSize) &&
            field.WorldToGrid(worldPosition, out int targetX, out int targetZ) && x == targetX && z == targetZ;
    }

    public void SetUpgradeTier(int tier)
    {
        upgradeTier = (HoeUpgradeTier)Mathf.Clamp(tier, 0, (int)HoeUpgradeTier.Gold);
        playerStatus?.SetToolLevel(PlayerToolType.Hoe, (int)upgradeTier + 1);
        RefreshIndicators(true);
    }

    void RefreshCurrentTarget()
    {
        Vector3 facing = movement != null ? movement.FacingDirection : transform.forward;
        if (facing.sqrMagnitude < 0.001f)
            facing = transform.forward;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.001f)
            facing = Vector3.forward;
        facing.Normalize();

        IReadOnlyList<FieldArea> areas = FieldArea.ActiveAreas;
        FieldArea bestField = null;
        int bestX = -1;
        int bestZ = -1;
        float bestScore = float.PositiveInfinity;
        for (int i = areas.Count - 1; i >= 0; i--)
        {
            FieldArea field = areas[i];
            if (field == null)
                continue;
            // Field outdoor tidak boleh menjadi target saat player berada di lantai/interior lain.
            if (Mathf.Abs(field.transform.InverseTransformPoint(transform.position).y) > Mathf.Max(1f, field.CellSize))
                continue;

            bool playerInside = field.WorldToGrid(transform.position, out int playerX, out int playerZ);
            float maximumCenterDistance = targetDistance + field.CellSize * 0.8f;
            float maximumCenterDistanceSqr = maximumCenterDistance * maximumCenterDistance;

            // Pilih pusat tile yang paling dekat dengan ray arah hadap di world-space.
            // Ini menjaga target tetap visual di depan player meski FieldArea diputar.
            for (int z = 0; z < field.Rows; z++)
            for (int x = 0; x < field.Columns; x++)
            {
                if (playerInside && x == playerX && z == playerZ)
                    continue;

                Vector3 center = field.GridToWorld(x, z);
                Vector3 delta = center - transform.position;
                delta.y = 0f;
                float distanceSqr = delta.sqrMagnitude;
                if (distanceSqr > maximumCenterDistanceSqr)
                    continue;

                float forwardDistance = Vector3.Dot(delta, facing);
                if (forwardDistance <= 0.05f)
                    continue;

                float lateralDistanceSqr = Mathf.Max(0f,
                    distanceSqr - forwardDistance * forwardDistance);
                // Jarak menyamping lebih mahal daripada jarak maju, sehingga tile
                // yang benar-benar berada di depan menang dari tile diagonal terdekat.
                float score = lateralDistanceSqr * 10f + forwardDistance * 0.12f;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestField = field;
                bestX = x;
                bestZ = z;
            }
        }

        currentField = bestField;
        currentX = bestX;
        currentZ = bestZ;
    }

    void UseHoe()
    {
        RefreshCurrentTarget();
        if (currentField == null)
        {
            ShowFeedback("Tidak ada field di depan Player");
            return;
        }

        BuildHoeTargets();
        int validCount = 0;
        for (int i = 0; i < hoeTargets.Count; i++)
            if (hoeTargets[i].Valid) validCount++;

        if (validCount == 0)
        {
            ShowFeedback("Tile tidak bisa dicangkul: tanah terisi atau terhalang");
            return;
        }

        HoeUpgradeTier activeTier = CurrentUpgradeTier;
        float efficiency = activeTier == HoeUpgradeTier.Basic ? 1f : upgradedStaminaEfficiency;
        float staminaCost = hoeCost * validCount * efficiency;
        if (!HasStamina(staminaCost))
        {
            ShowFeedback("Stamina tidak cukup");
            return;
        }

        int changed = 0;
        for (int i = 0; i < hoeTargets.Count; i++)
        {
            HoeTileTarget target = hoeTargets[i];
            if (target.Valid && target.Field.TryHoe(target.X, target.Z))
                changed++;
        }

        if (changed <= 0)
            return;

        SpendStamina(hoeCost * changed * efficiency);
        Vector3 effectPosition = currentField.GridToWorld(currentX, currentZ);
        PlayHoeFeedback(effectPosition);
        StartCoroutine(ActionLockRoutine());
        RefreshIndicators(true);
        Debug.Log($"[FARMING] {changed} tile berhasil dicangkul ({activeTier}).");
    }

    bool TryHarvestCurrentTile()
    {
        if (!currentField.TryGetSnapshot(currentX, currentZ, out FieldTileSnapshot snapshot) || snapshot.State != TileState.Planted)
            return false;
        if (playerInv == null || !HasStamina(harvestCost))
            return true;

        if (currentField.TryHarvest(currentX, currentZ, playerInv, out CropGrade grade))
        {
            SpendStamina(harvestCost);
            ShowFeedback($"Panen berhasil - {(int)grade + 1} bintang");
        }
        else
            ShowFeedback("Tanaman belum matang atau inventory penuh");
        return true;
    }

    bool TryUseHandHarvest()
    {
        if (!Input.GetKeyDown(handHarvestKey) || currentField == null ||
            !currentField.TryGetSnapshot(currentX, currentZ, out FieldTileSnapshot snapshot) ||
            snapshot.State != TileState.Planted || snapshot.CropState != CropLifecycleState.HarvestReady)
            return false;
        if (!PlayerInteractionTarget.Press(handHarvestKey)) return false;
        return TryHarvestCurrentTile();
    }

    void UseSoilEffect(bool watering)
    {
        RefreshCurrentTarget();
        if (currentField == null)
            return;

        if (watering && currentField.TryGetSnapshot(currentX, currentZ, out FieldTileSnapshot waterTarget) &&
            waterTarget.WateredToday)
        {
            ShowFeedback("Tanah atau tanaman sudah disiram hari ini");
            return;
        }

        if (watering && (wateringCan == null || wateringCan.IsEmpty))
        {
            ShowFeedback("Watering Can kosong. Isi ulang di sumur dekat ladang");
            return;
        }

        ItemSO fertilizer = null;
        if (!watering)
        {
            fertilizer = inventoryHotbar != null ? inventoryHotbar.SelectedItem : null;
            if (fertilizer == null || !fertilizer.IsFertilizer)
            {
                ShowFeedback("Pilih Fertilizer dari hotbar");
                return;
            }

            if (VillageProgressionService.Instance != null &&
                !VillageProgressionService.Instance.MeetsRequirement(fertilizer.requiredVillageLevel))
            {
                ShowFeedback($"Fertilizer terkunci sampai Village Lv.{fertilizer.requiredVillageLevel}");
                return;
            }
        }

        float cost = watering ? waterCost : fertilizeCost;
        if (!HasStamina(cost))
        {
            ShowFeedback("Stamina tidak cukup");
            return;
        }

        bool success = watering
            ? currentField.TryWater(currentX, currentZ)
            : currentField.TryFertilize(currentX, currentZ, (int)fertilizer.fertilizerLevel, fertilizer.SoilRestoreAmount);
        if (success)
        {
            if (watering && !wateringCan.TryUse())
            {
                Debug.LogError("[FARMING] Tile tersiram tetapi kapasitas Watering Can gagal dikurangi.");
                return;
            }
            if (!watering && !playerInv.RemoveFromSlot(inventoryHotbar.SelectedIndex, 1))
            {
                Debug.LogError("[FARMING] Tanah dipupuk tetapi item Fertilizer gagal dikurangi.");
                return;
            }
            SpendStamina(cost);
            ShowFeedback(watering
                ? $"Tanah atau tanaman sudah disiram — air {wateringCan.CurrentWater}/{wateringCan.MaximumWater}"
                : $"Tanah sudah dipupuk dengan {fertilizer.itemName}. Siap ditanami");
        }
        else
            ShowFeedback(watering
                ? "Tanah harus dicangkul terlebih dahulu"
                : "Pupuk hanya bisa dipakai pada tanah cangkul yang belum penuh");
    }

    void UseCropBooster()
    {
        RefreshCurrentTarget();
        if (currentField == null)
        {
            ShowFeedback("Tidak ada tanaman di depan Player");
            return;
        }

        ItemSO booster = inventoryHotbar != null ? inventoryHotbar.SelectedItem : null;
        if (booster == null || !booster.IsCropBooster)
        {
            ShowFeedback("Pilih Crop Booster dari hotbar");
            return;
        }

        if (VillageProgressionService.Instance != null &&
            !VillageProgressionService.Instance.MeetsRequirement(booster.requiredVillageLevel))
        { ShowFeedback("Booster belum terbuka pada Village Level ini"); return; }

        if (!HasStamina(cropBoosterCost))
        {
            ShowFeedback("Stamina tidak cukup");
            return;
        }

        if (!currentField.TryApplyCropBooster(currentX, currentZ, booster.cropBoosterLevel))
        {
            ShowFeedback("Booster hanya untuk tanaman tumbuh, maksimal sekali sehari");
            return;
        }

        if (!playerInv.RemoveFromSlot(inventoryHotbar.SelectedIndex, 1))
        {
            Debug.LogError("[FARMING] Booster terpasang tetapi item gagal dikurangi.");
            return;
        }

        SpendStamina(cropBoosterCost);
        ShowFeedback($"Booster kualitas Lv.{booster.cropBoosterLevel} hari ini. Growth/yield tetap");
    }

    void BuildHoeTargets()
    {
        hoeTargets.Clear();
        if (currentField == null)
            return;

        Vector3 worldFacing = movement != null ? movement.FacingDirection : transform.forward;
        Vector3 localForward = currentField.transform.InverseTransformDirection(worldFacing);
        Vector2Int forward = Mathf.Abs(localForward.x) > Mathf.Abs(localForward.z)
            ? new Vector2Int(localForward.x >= 0f ? 1 : -1, 0)
            : new Vector2Int(0, localForward.z >= 0f ? 1 : -1);
        Vector2Int side = new(-forward.y, forward.x);

        switch (CurrentUpgradeTier)
        {
            case HoeUpgradeTier.Copper:
                for (int offset = -1; offset <= 1; offset++) AddTarget(currentX + side.x * offset, currentZ + side.y * offset);
                break;
            case HoeUpgradeTier.Silver:
                for (int offset = 0; offset < 3; offset++) AddTarget(currentX + forward.x * offset, currentZ + forward.y * offset);
                break;
            case HoeUpgradeTier.Gold:
                for (int depth = 0; depth < 3; depth++)
                    for (int width = -1; width <= 1; width++)
                        AddTarget(currentX + forward.x * depth + side.x * width, currentZ + forward.y * depth + side.y * width);
                break;
            default:
                AddTarget(currentX, currentZ);
                break;
        }
    }

    void AddTarget(int x, int z)
    {
        bool valid = currentField.CanHoe(x, z) && !HasBlockingObject(currentField, x, z);
        hoeTargets.Add(new HoeTileTarget(currentField, x, z, valid));
    }

    bool HasBlockingObject(FieldArea field, int x, int z)
    {
        Vector3 center = field.GridToWorld(x, z);
        Vector3 halfExtents = new(field.CellSize * 0.42f, 0.48f, field.CellSize * 0.42f);
        int count = Physics.OverlapBoxNonAlloc(center + field.transform.up * 0.5f, halfExtents, blockerBuffer, field.transform.rotation, blockerMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider collider = blockerBuffer[i];
            if (collider == null || collider.transform.IsChildOf(transform))
                continue;
            if (collider.gameObject == field.gameObject)
                continue;
            if (collider.bounds.max.y <= center.y + 0.12f)
                continue; // Terrain/ground surface di bawah tile bukan blocker.
            return true;
        }
        return false;
    }

    void RefreshIndicators(bool force = false)
    {
        PlayerToolType selectedTool = hotbar != null ? hotbar.SelectedTool : PlayerToolType.None;
        bool supportedTool = selectedTool == PlayerToolType.Hoe ||
                             selectedTool == PlayerToolType.Seed ||
                             selectedTool == PlayerToolType.WateringCan ||
                             selectedTool == PlayerToolType.Fertilizer ||
                             selectedTool == PlayerToolType.CropBooster;
        bool show = enabled && supportedTool && currentField != null &&
                    !(movement != null && movement.IsMovementLocked);
        if (!show)
        {
            HideIndicators();
            return;
        }

        if (selectedTool == PlayerToolType.Seed)
        {
            if (seedTool == null) seedTool = GetComponent<SeedTool>();
            hoeTargets.Clear();
            bool valid = seedTool != null && seedTool.CanPlantAt(currentField, currentX, currentZ);
            hoeTargets.Add(new HoeTileTarget(currentField, currentX, currentZ, valid));
        }
        else if (selectedTool == PlayerToolType.Hoe)
        {
            BuildHoeTargets();
        }
        else
        {
            hoeTargets.Clear();
            bool valid = currentField.TryGetSnapshot(currentX, currentZ, out FieldTileSnapshot snapshot) &&
                         (selectedTool == PlayerToolType.WateringCan
                             ? snapshot.State == TileState.Hoed || snapshot.State == TileState.Planted
                             : selectedTool == PlayerToolType.CropBooster
                                 ? snapshot.State == TileState.Planted
                                 : currentField.CanFertilize(currentX, currentZ));
            hoeTargets.Add(new HoeTileTarget(currentField, currentX, currentZ, valid));
        }
        EnsureIndicatorCount(hoeTargets.Count);
        for (int i = 0; i < indicators.Count; i++)
        {
            bool active = i < hoeTargets.Count;
            indicators[i].gameObject.SetActive(active);
            if (!active) continue;
            DrawIndicator(indicators[i], hoeTargets[i]);
        }
    }

    void EnsureIndicatorCount(int count)
    {
        EnsureIndicatorMaterials();
        while (indicators.Count < count)
        {
            GameObject indicatorObject = new($"HoeTarget_{indicators.Count + 1}");
            indicatorObject.transform.SetParent(transform, false);
            LineRenderer line = indicatorObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = false;
            line.positionCount = 5;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.textureMode = LineTextureMode.Stretch;
            indicators.Add(line);
        }
    }

    void DrawIndicator(LineRenderer line, HoeTileTarget target)
    {
        float extent = target.Field.CellSize * 0.46f;
        Vector3 center = target.Field.GridToWorld(target.X, target.Z) + target.Field.transform.up * 0.065f;
        Vector3 right = target.Field.transform.right * extent;
        Vector3 forward = target.Field.transform.forward * extent;
        line.startWidth = line.endWidth = Mathf.Max(0.025f, target.Field.CellSize * 0.035f);
        line.material = target.Valid ? validIndicatorMaterial : invalidIndicatorMaterial;
        line.startColor = line.endColor = target.Valid ? validHighlightColor : invalidHighlightColor;
        line.SetPosition(0, center - right - forward);
        line.SetPosition(1, center + right - forward);
        line.SetPosition(2, center + right + forward);
        line.SetPosition(3, center - right + forward);
        line.SetPosition(4, center - right - forward);
    }

    void EnsureIndicatorMaterials()
    {
        if (validIndicatorMaterial != null)
            return;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        validIndicatorMaterial = new Material(shader) { color = validHighlightColor };
        invalidIndicatorMaterial = new Material(shader) { color = invalidHighlightColor };
    }

    void HideIndicators()
    {
        for (int i = 0; i < indicators.Count; i++)
            indicators[i].gameObject.SetActive(false);
    }

    void PlayHoeFeedback(Vector3 position)
    {
        if (animator != null && hasUseToolTrigger)
            animator.SetTrigger(useToolTrigger);
        if (hoeSound != null)
            GameAudio.PlayOneShot(audioSource, hoeSound, GameAudioBus.Main);
        if (dirtParticles != null)
        {
            dirtParticles.transform.position = position + Vector3.up * 0.08f;
            dirtParticles.Emit(CurrentUpgradeTier == HoeUpgradeTier.Gold ? 24 : 12);
        }
        if (cameraFollow == null && Camera.main != null)
            cameraFollow = Camera.main.GetComponent<TopDownCameraFollow>();
        cameraFollow?.AddImpulse(cameraShakeStrength, cameraShakeDuration);
        if (controllerRumble && Gamepad.current != null)
            StartCoroutine(ControllerRumbleRoutine());
    }

    IEnumerator ActionLockRoutine()
    {
        actionBusy = true;
        playerStatus?.AcquireActivity(this, PlayerMovementState.ToolAction);
        movement?.AcquireMovementLock(this);
        if (actionLockDuration > 0f)
            yield return new WaitForSeconds(actionLockDuration);
        movement?.ReleaseMovementLock(this);
        playerStatus?.ReleaseActivity(this);
        actionBusy = false;
    }

    IEnumerator ControllerRumbleRoutine()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) yield break;
        gamepad.SetMotorSpeeds(0.08f, 0.14f);
        yield return new WaitForSecondsRealtime(0.09f);
        gamepad.SetMotorSpeeds(0f, 0f);
    }

    void EnsureDirtParticles()
    {
        if (dirtParticles != null)
            return;
        GameObject particleObject = new("HoeDirtParticles_Runtime");
        dirtParticles = particleObject.AddComponent<ParticleSystem>();
        dirtParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = dirtParticles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.35f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
        main.startColor = new Color(0.38f, 0.20f, 0.08f, 1f);
        main.gravityModifier = 0.7f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule emission = dirtParticles.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = dirtParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.25f;
    }

    bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
            return false;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.name == parameterName && parameter.type == type) return true;
        return false;
    }

    void ShowFeedback(string message)
    {
        SaveLoadFeedback.Instance?.ShowMessage(message);
        Debug.Log($"[FARMING] {message}");
    }

    void OnDisable()
    {
        HideIndicators();
        movement?.ReleaseMovementLock(this);
        playerStatus?.ReleaseActivity(this);
        actionBusy = false;
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);
    }

    readonly struct HoeTileTarget
    {
        public readonly FieldArea Field;
        public readonly int X;
        public readonly int Z;
        public readonly bool Valid;

        public HoeTileTarget(FieldArea field, int x, int z, bool valid)
        {
            Field = field;
            X = x;
            Z = z;
            Valid = valid;
        }
    }

    bool HasStamina(float cost)
    {
        if (playerStatus == null || playerStatus.CanSpendStamina(cost))
            return true;

        Debug.Log("[PLAYER] Stamina tidak cukup.");
        return false;
    }

    void SpendStamina(float cost)
    {
        playerStatus?.TrySpendStamina(cost);
    }
}
