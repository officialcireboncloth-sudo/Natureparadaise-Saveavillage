using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
/// <summary>
/// Orkestrator interaksi resource di depan player: mencabut weed, menyabit rumput,
/// memukul batu, menebang pohon, membawa hasil, dan memakai stamina/tool level.
/// </summary>
public sealed class PlayerGatheringTool : MonoBehaviour
{
    [Header("Target")]
    [SerializeField, Min(0.5f)] float interactionRange = 2.5f;
    [SerializeField, Range(-1f, 1f)] float minimumFacingDot = 0.2f;
    [SerializeField, Min(0.02f)] float scanInterval = 0.08f;

    [Header("Input")]
    [SerializeField] KeyCode pullOrStoreKey = KeyCode.E;
    [SerializeField] KeyCode dropCarriedKey = KeyCode.Q;

    [Header("Stamina")]
    [SerializeField, Min(0f)] float pullCost = 1f;
    [SerializeField, Min(0f)] float sickleCost = 2f;
    [SerializeField, Min(0f)] float hammerCost = 3f;
    [SerializeField, Min(0f)] float axeCost = 3f;

    [Header("Tool Levels")]
    [SerializeField, Min(1)] int sickleLevel = 1;
    [SerializeField, Min(1)] int hammerLevel = 1;
    [SerializeField, Min(1)] int axeLevel = 1;

    [Header("Tool Feedback Slots")]
    [SerializeField] Animator animator;
    [SerializeField] string pullTrigger = "Pull";
    [SerializeField] string sickleTrigger = "Sickle";
    [SerializeField] string hammerTrigger = "Hammer";
    [SerializeField] string axeTrigger = "Axe";
    [SerializeField] AudioClip sickleSwish;
    [SerializeField] AudioClip hammerImpact;
    [SerializeField] AudioClip axeImpact;
    [SerializeField] ParticleSystem grassParticles;
    [SerializeField] ParticleSystem stoneParticles;
    [SerializeField] GameObject sickleVisual;
    [SerializeField] GameObject hammerVisual;
    [SerializeField] GameObject axeVisual;

    PlayerController movement;
    PlayerToolHotbar hotbar;
    PlayerStatusSystem status;
    Inventory inventory;
    AudioSource audioSource;
    TopDownCameraFollow cameraFollow;
    WorldGatherable target;
    WorldTree treeTarget;
    float nextScan;

    ItemSO carriedItem;
    int carriedAmount;
    GameObject carriedVisual;

    int SickleLevel => status != null ? status.GetToolLevel(PlayerToolType.Sickle) : sickleLevel;
    int AxeLevel => status != null ? status.GetToolLevel(PlayerToolType.Axe) : axeLevel;
    public int HammerLevel => status != null ? status.GetToolLevel(PlayerToolType.Hammer) : hammerLevel;
    public bool IsCarrying => carriedItem != null;

    void Awake()
    {
        movement = GetComponent<PlayerController>();
        hotbar = GetComponent<PlayerToolHotbar>();
        if (hotbar == null) hotbar = gameObject.AddComponent<PlayerToolHotbar>();
        status = GetComponent<PlayerStatusSystem>();
        if (status != null)
        {
            if (sickleLevel > 1) status.SetToolLevel(PlayerToolType.Sickle, sickleLevel);
            if (hammerLevel > 1) status.SetToolLevel(PlayerToolType.Hammer, hammerLevel);
            if (axeLevel > 1) status.SetToolLevel(PlayerToolType.Axe, axeLevel);
        }
        inventory = GetComponent<Inventory>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        cameraFollow = Camera.main != null ? Camera.main.GetComponent<TopDownCameraFollow>() : null;
        EnsureDebugToolVisuals();
        hotbar.SelectionChanged += UpdateToolVisuals;
        UpdateToolVisuals(hotbar.SelectedTool);
    }

    void OnDestroy()
    {
        if (hotbar != null) hotbar.SelectionChanged -= UpdateToolVisuals;
    }

    void Update()
    {
        // Target scan diberi interval untuk menghindari pencarian collider setiap frame,
        // tetapi input tool tetap diperiksa setiap frame agar kontrol terasa responsif.
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + scanInterval;
            RefreshTarget();
        }

        if (carriedItem != null)
        {
            WorldInteractionPrompt.Request(this, transform, $"{pullOrStoreKey}: simpan   {dropCarriedKey}: jatuhkan", 0f, 1.65f);
            if (Input.GetKeyDown(pullOrStoreKey)) StoreCarriedItem();
            else if (Input.GetKeyDown(dropCarriedKey)) DropCarriedItem();
            return;
        }

        ShowTargetPrompt();
        if (movement != null && movement.IsMovementLocked) return;

        if (target != null && Input.GetKeyDown(pullOrStoreKey) && target.CanPull)
            PullTarget();
        else if (hotbar.IsUsePressed(PlayerToolType.Sickle))
            UseSickle();
        else if (hotbar.IsUsePressed(PlayerToolType.Hammer))
            UseHammer();
        else if (hotbar.IsUsePressed(PlayerToolType.Axe))
            UseAxe();
    }

    void RefreshTarget()
    {
        Vector3 facing = movement != null ? movement.FacingDirection : transform.forward;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.01f) facing = transform.forward;
        facing.Normalize();

        WorldGatherable best = null;
        float bestScore = float.PositiveInfinity;
        IReadOnlyList<WorldGatherable> all = WorldGatherable.Active;
        for (int i = 0; i < all.Count; i++)
        {
            WorldGatherable candidate = all[i];
            if (candidate == null || !candidate.IsAvailable) continue;
            Vector3 delta = candidate.transform.position - transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance > interactionRange || distance < 0.01f) continue;
            float dot = Vector3.Dot(facing, delta / distance);
            if (dot < minimumFacingDot) continue;
            float score = distance - dot * 0.5f;
            if (score >= bestScore) continue;
            bestScore = score;
            best = candidate;
        }

        if (target != best)
        {
            if (target != null) target.SetHighlighted(false);
            target = best;
            if (target != null) target.SetHighlighted(hotbar.SelectedTool != PlayerToolType.Axe);
        }

        WorldTree bestTree = null;
        float bestTreeScore = float.PositiveInfinity;
        IReadOnlyList<WorldTree> trees = WorldTree.Active;
        for (int i = 0; i < trees.Count; i++)
        {
            WorldTree candidate = trees[i];
            if (candidate == null || !candidate.IsAvailable) continue;
            Vector3 delta = candidate.transform.position - transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance > interactionRange || distance < 0.01f) continue;
            float dot = Vector3.Dot(facing, delta / distance);
            if (dot < minimumFacingDot) continue;
            float score = distance - dot * 0.5f;
            if (score < bestTreeScore) { bestTreeScore = score; bestTree = candidate; }
        }
        if (treeTarget != bestTree)
        {
            if (treeTarget != null) treeTarget.SetHighlighted(false);
            treeTarget = bestTree;
            if (treeTarget != null) treeTarget.SetHighlighted(hotbar.SelectedTool == PlayerToolType.Axe);
        }
        if (target != null) target.SetHighlighted(hotbar.SelectedTool != PlayerToolType.Axe);
        if (treeTarget != null) treeTarget.SetHighlighted(hotbar.SelectedTool == PlayerToolType.Axe);
    }

    void ShowTargetPrompt()
    {
        if (hotbar.SelectedTool == PlayerToolType.Axe && treeTarget != null)
        {
            string treePrompt = AxeLevel < treeTarget.MinimumAxeLevel
                ? "Level Axe belum cukup"
                : $"F: Tebang {(treeTarget.IsStump ? "Tunggul" : "Pohon")}  HP {treeTarget.Durability}";
            WorldInteractionPrompt.Request(this, treeTarget.transform, treePrompt,
                Vector3.Distance(transform.position, treeTarget.transform.position), treeTarget.PromptHeight);
            return;
        }
        if (target == null) return;
        string prompt = null;
        if (hotbar.SelectedTool == PlayerToolType.Hammer && target.CanHammer)
            prompt = HammerLevel < target.MinimumHammerLevel ? "Level Hammer belum cukup" : $"F: Hantam  HP {target.Durability}";
        else if (hotbar.SelectedTool == PlayerToolType.Sickle && target.CanSickle)
            prompt = "F: Sabit";
        else if (target.CanPull)
            prompt = $"{pullOrStoreKey}: Cabut";
        else if (target.CanSickle)
            prompt = "Butuh Sickle";
        else if (target.CanHammer)
            prompt = "Butuh Hammer";

        if (prompt != null)
            WorldInteractionPrompt.Request(this, target.transform, prompt, Vector3.Distance(transform.position, target.transform.position), target.PromptHeight);
    }

    void PullTarget()
    {
        if (!SpendStamina(pullCost)) return;
        status?.PulseActivity(PlayerMovementState.ToolAction);
        TriggerAnimation(pullTrigger);
        target.Pull(this);
        target = null;
    }

    void UseSickle()
    {
        if (!SpendStamina(sickleCost)) return;
        status?.PulseActivity(PlayerMovementState.ToolAction);
        TriggerAnimation(sickleTrigger);
        if (sickleSwish != null) audioSource.PlayOneShot(sickleSwish);
        if (grassParticles != null) grassParticles.Play();

        Vector3 facing = movement != null ? movement.FacingDirection : transform.forward;
        facing.y = 0f;
        facing.Normalize();
        float radius = 0.85f + SickleLevel * 0.35f;
        Vector3 center = transform.position + facing * 1.35f;
        int cut = 0;
        IReadOnlyList<WorldGatherable> all = WorldGatherable.Active;
        for (int i = 0; i < all.Count; i++)
        {
            WorldGatherable candidate = all[i];
            if (candidate != null && candidate.CanSickle && Vector3.Distance(candidate.transform.position, center) <= radius && candidate.Cut(this))
                cut++;
        }
        cut += FieldArea.CutCropsInRadius(center, radius);
        SaveLoadFeedback.Instance?.ShowMessage(cut > 0 ? $"Sabit memotong {cut} target" : "Tidak ada rumput di depan");
        if (target != null && !target.IsAvailable) target = null;
    }

    void UseHammer()
    {
        if (target == null || !target.CanHammer)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Tidak ada batu di depan");
            return;
        }
        if (HammerLevel < target.MinimumHammerLevel)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Level Hammer belum cukup");
            return;
        }
        if (!SpendStamina(hammerCost)) return;
        status?.PulseActivity(PlayerMovementState.ToolAction);
        TriggerAnimation(hammerTrigger);
        if (hammerImpact != null) audioSource.PlayOneShot(hammerImpact);
        if (stoneParticles != null) stoneParticles.Play();
        target.Hammer(this, HammerLevel);
        cameraFollow?.AddImpulse(0.09f, 0.12f);
        if (!target.IsAvailable) target = null;
    }

    void UseAxe()
    {
        if (treeTarget == null)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Tidak ada pohon di depan");
            return;
        }
        if (AxeLevel < treeTarget.MinimumAxeLevel)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Level Axe belum cukup");
            return;
        }
        if (!SpendStamina(axeCost)) return;
        status?.PulseActivity(PlayerMovementState.ToolAction);
        TriggerAnimation(axeTrigger);
        if (axeImpact != null) audioSource.PlayOneShot(axeImpact);
        treeTarget.Chop(AxeLevel);
        cameraFollow?.AddImpulse(0.07f, 0.1f);
        if (!treeTarget.IsAvailable) treeTarget = null;
    }

    bool SpendStamina(float amount)
    {
        if (status == null || status.TrySpendStamina(amount)) return true;
        SaveLoadFeedback.Instance?.ShowMessage("Stamina tidak cukup");
        return false;
    }

    void TriggerAnimation(string trigger)
    {
        if (animator != null && !string.IsNullOrEmpty(trigger)) animator.SetTrigger(trigger);
    }

    /// <summary>Mencoba memegang hasil gather sebelum disimpan atau dijatuhkan.</summary>
    public bool TryCarry(ItemSO item, int amount)
    {
        if (item == null || amount <= 0 || carriedItem != null) return false;
        carriedItem = item;
        carriedAmount = amount;
        carriedVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        carriedVisual.name = $"Carried_{item.itemName}";
        Collider collider = carriedVisual.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
        carriedVisual.transform.SetParent(transform, false);
        carriedVisual.transform.localPosition = new Vector3(0f, 1.45f, 0.38f);
        carriedVisual.transform.localScale = new Vector3(0.35f, 0.22f, 0.35f);
        movement?.SetCarrying(true);
        UpdateToolVisuals(hotbar.SelectedTool);
        return true;
    }

    void StoreCarriedItem()
    {
        if (inventory == null || !inventory.Add(carriedItem, carriedAmount))
        {
            SaveLoadFeedback.Instance?.ShowMessage("Inventory penuh");
            return;
        }
        ClearCarry();
    }

    void DropCarriedItem()
    {
        Vector3 facing = movement != null ? movement.FacingDirection : transform.forward;
        WorldGatherable.SpawnLoosePickup(carriedItem, carriedAmount, transform.position + facing.normalized * 1.1f + Vector3.up * 0.25f);
        ClearCarry();
    }

    void ClearCarry()
    {
        carriedItem = null;
        carriedAmount = 0;
        if (carriedVisual != null) Destroy(carriedVisual);
        movement?.SetCarrying(false);
        UpdateToolVisuals(hotbar.SelectedTool);
    }

    void EnsureDebugToolVisuals()
    {
        if (sickleVisual == null)
        {
            sickleVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sickleVisual.name = "SickleVisual_Debug";
            sickleVisual.transform.SetParent(transform, false);
            sickleVisual.transform.localPosition = new Vector3(0.38f, 0.85f, 0.45f);
            sickleVisual.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            sickleVisual.transform.localScale = new Vector3(0.08f, 0.65f, 0.1f);
            Collider collider = sickleVisual.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }
        if (hammerVisual == null)
        {
            hammerVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hammerVisual.name = "HammerVisual_Debug";
            hammerVisual.transform.SetParent(transform, false);
            hammerVisual.transform.localPosition = new Vector3(0.38f, 0.9f, 0.42f);
            hammerVisual.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
            hammerVisual.transform.localScale = new Vector3(0.22f, 0.65f, 0.16f);
            Collider collider = hammerVisual.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }
        if (axeVisual == null)
        {
            axeVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            axeVisual.name = "AxeVisual_Debug";
            axeVisual.transform.SetParent(transform, false);
            axeVisual.transform.localPosition = new Vector3(0.38f, 0.9f, 0.42f);
            axeVisual.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
            axeVisual.transform.localScale = new Vector3(0.16f, 0.7f, 0.12f);
            Collider collider = axeVisual.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }
    }

    void UpdateToolVisuals(PlayerToolType selected)
    {
        if (sickleVisual != null) sickleVisual.SetActive(selected == PlayerToolType.Sickle && !IsCarrying);
        if (hammerVisual != null) hammerVisual.SetActive(selected == PlayerToolType.Hammer && !IsCarrying);
        if (axeVisual != null) axeVisual.SetActive(selected == PlayerToolType.Axe && !IsCarrying);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null && player.GetComponent<PlayerGatheringTool>() == null)
            player.gameObject.AddComponent<PlayerGatheringTool>();
    }
}
