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
public class FarmingTool : MonoBehaviour
{
    [Header("Raycast")]
    [Tooltip("Layer Field dan Crop.")]
    public LayerMask fieldMask;

    [Header("Input")]
    public KeyCode actionKey = KeyCode.H;
    public KeyCode waterKey = KeyCode.V;
    public KeyCode fertilizeKey = KeyCode.N;

    [Header("Raycast Distance")]
    public float maxDist = 100f;

    [Header("Inventory")]
    public Inventory playerInv;

    [Header("Player Stamina")]
    public PlayerStatusSystem playerStatus;
    [Min(0f)] public float hoeCost = 2f;
    [Min(0f)] public float waterCost = 1f;
    [Min(0f)] public float fertilizeCost = 1f;
    [Min(0f)] public float harvestCost = 1f;

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
    PlayerController movement;
    TopDownCameraFollow cameraFollow;
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

    void Awake()
    {
        if (playerInv == null)
            playerInv = FindFirstObjectByType<Inventory>();
        if (playerStatus == null && playerInv != null)
            playerStatus = playerInv.GetComponent<PlayerStatusSystem>();

        hotbar = GetComponent<PlayerToolHotbar>();
        if (hotbar == null)
            hotbar = gameObject.AddComponent<PlayerToolHotbar>();
        movement = GetComponent<PlayerController>();
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

        if (actionBusy)
            return;
        if (movement != null && movement.IsMovementLocked)
            return;

        bool legacyHoe = Input.GetKeyDown(actionKey);
        bool useHoe = legacyHoe || hotbar.IsUsePressed(PlayerToolType.Hoe);
        bool useWater = Input.GetKeyDown(waterKey) || hotbar.IsUsePressed(PlayerToolType.WateringCan);
        bool useFertilizer = Input.GetKeyDown(fertilizeKey) || hotbar.IsUsePressed(PlayerToolType.Fertilizer);

        if (useHoe) UseHoe(legacyHoe);
        else if (useWater) UseSoilEffect(true);
        else if (useFertilizer) UseSoilEffect(false);
    }

    void RefreshCropInteractionPrompt()
    {
        if (currentField == null ||
            !currentField.TryGetSnapshot(currentX, currentZ, out FieldTileSnapshot snapshot) ||
            snapshot.State != TileState.Planted || snapshot.Crop == null)
            return;

        Vector3 promptPosition = currentField.GridToWorld(currentX, currentZ) + currentField.transform.up * 1.05f;
        if (currentField.TryGetCropView(currentX, currentZ, out FieldCropView cropView))
            promptPosition = cropView.transform.position + Vector3.up * 1.05f;

        bool mature = snapshot.GrowthDays >= snapshot.Crop.TotalGrowthDays;
        string text = mature ? $"Tekan {actionKey} untuk panen" : "Tanaman sedang tumbuh";
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

    public void SetUpgradeTier(int tier)
    {
        upgradeTier = (HoeUpgradeTier)Mathf.Clamp(tier, 0, (int)HoeUpgradeTier.Gold);
        RefreshIndicators(true);
    }

    void RefreshCurrentTarget()
    {
        Vector3 facing = movement != null ? movement.FacingDirection : transform.forward;
        if (facing.sqrMagnitude < 0.001f)
            facing = transform.forward;

        IReadOnlyList<FieldArea> areas = FieldArea.ActiveAreas;
        for (int i = areas.Count - 1; i >= 0; i--)
        {
            FieldArea field = areas[i];
            if (field == null)
                continue;

            Vector3 localFacing = field.transform.InverseTransformDirection(facing.normalized);
            Vector2Int gridDirection = Mathf.Abs(localFacing.x) > Mathf.Abs(localFacing.z)
                ? new Vector2Int(localFacing.x >= 0f ? 1 : -1, 0)
                : new Vector2Int(0, localFacing.z >= 0f ? 1 : -1);

            // Jika Player berdiri di area field, pilih persis satu sel tetangga
            // di depan—tidak bergantung jarak raycast atau posisi di dalam sel.
            if (field.WorldToGrid(transform.position, out int playerX, out int playerZ))
            {
                int targetX = playerX + gridDirection.x;
                int targetZ = playerZ + gridDirection.y;
                if (field.TryGetSnapshot(targetX, targetZ, out _))
                {
                    currentField = field;
                    currentX = targetX;
                    currentZ = targetZ;
                    return;
                }
            }

            // Player boleh berdiri sedikit di luar tepi field; ambil sel pertama
            // di depan selama masih dalam jangkauan satu tile.
            float reach = Mathf.Min(targetDistance, field.CellSize);
            Vector3 edgePoint = transform.position + facing.normalized * reach;
            if (field.WorldToGrid(edgePoint, out int edgeX, out int edgeZ))
            {
                currentField = field;
                currentX = edgeX;
                currentZ = edgeZ;
                return;
            }
        }

        currentField = null;
        currentX = -1;
        currentZ = -1;
    }

    void UseHoe(bool allowLegacyHarvest)
    {
        RefreshCurrentTarget();
        if (currentField == null)
        {
            ShowFeedback("Tidak ada field di depan Player");
            return;
        }

        if (allowLegacyHarvest && TryHarvestCurrentTile())
            return;

        BuildHoeTargets();
        int validCount = 0;
        for (int i = 0; i < hoeTargets.Count; i++)
            if (hoeTargets[i].Valid) validCount++;

        if (validCount == 0)
        {
            ShowFeedback("Tile tidak bisa dicangkul: tanah terisi atau terhalang");
            return;
        }

        float efficiency = upgradeTier == HoeUpgradeTier.Basic ? 1f : upgradedStaminaEfficiency;
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
        Debug.Log($"[FARMING] {changed} tile berhasil dicangkul ({upgradeTier}).");
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
            ShowFeedback($"Panen berhasil - Grade {grade}");
        }
        else
            ShowFeedback("Tanaman belum matang atau inventory penuh");
        return true;
    }

    void UseSoilEffect(bool watering)
    {
        RefreshCurrentTarget();
        if (currentField == null)
            return;

        float cost = watering ? waterCost : fertilizeCost;
        if (!HasStamina(cost))
            return;

        bool success = watering
            ? currentField.TryWater(currentX, currentZ)
            : currentField.TryFertilize(currentX, currentZ);
        if (success)
        {
            SpendStamina(cost);
            ShowFeedback(watering ? "Tanah disiram" : "Tanah diberi pupuk");
        }
        else
            ShowFeedback("Tanah harus dicangkul terlebih dahulu");
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

        switch (upgradeTier)
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
        bool show = enabled && hotbar != null && hotbar.SelectedTool == PlayerToolType.Hoe && currentField != null;
        if (!show)
        {
            HideIndicators();
            return;
        }

        BuildHoeTargets();
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
            audioSource.PlayOneShot(hoeSound);
        if (dirtParticles != null)
        {
            dirtParticles.transform.position = position + Vector3.up * 0.08f;
            dirtParticles.Emit(upgradeTier == HoeUpgradeTier.Gold ? 24 : 12);
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
        movement?.AcquireMovementLock(this);
        if (actionLockDuration > 0f)
            yield return new WaitForSeconds(actionLockDuration);
        movement?.ReleaseMovementLock(this);
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
