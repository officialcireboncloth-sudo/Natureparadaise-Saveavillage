using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
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
    [SerializeField, Min(0f)] float pullCost = 0.05f;
    [SerializeField, HideInInspector, Min(0f)] float sickleCost = 0.1f;
    [SerializeField, HideInInspector, Min(0f)] float hammerCost = 0.1f;
    [SerializeField, HideInInspector, Min(0f)] float axeCost = 0.1f;

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

    [Header("Animation Impact Timing")]
    [SerializeField, Min(0f)] float pullImpactDelay = 1.05f;
    [SerializeField, Tooltip("Shared timing, blade path and forward cutting fan.")]
    SickleSweepProfile sickleSweepProfile;
    [SerializeField, Min(0f), Tooltip("Saat kepala palu mencapai bagian bawah ayunan Hammering Rock (detik).")]
    float hammerImpactDelay = 0.68f;
    [SerializeField, Min(0f)] float axeImpactDelay = 0.55f;

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
    int carriedQualityStars;
    float carriedFishSizeCm;
    GameObject carriedVisual;
    bool actionBusy;
    SickleSweepAction sickleSweep;
    readonly List<SickleCutTarget> sickleCandidates=new(32);

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
        if (animator == null) animator = movement!=null?movement.CharacterAnimator:GetComponentInChildren<Animator>();
        if(sickleSweepProfile==null)sickleSweepProfile=Resources.Load<SickleSweepProfile>("Profiles/Sickle Sweep");
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        cameraFollow = Camera.main != null ? Camera.main.GetComponent<TopDownCameraFollow>() : null;
        // PlayerHeldTools presents the imported models; keep legacy scene visuals hidden.
        if (sickleVisual != null) sickleVisual.SetActive(false);
        if (hammerVisual != null) hammerVisual.SetActive(false);
        if (axeVisual != null) axeVisual.SetActive(false);
        hotbar.SelectionChanged += UpdateToolVisuals;
        UpdateToolVisuals(hotbar.SelectedTool);
    }

    void OnDestroy()
    {
        if (hotbar != null) hotbar.SelectionChanged -= UpdateToolVisuals;
        if(carriedVisual!=null)Destroy(carriedVisual.transform.parent.gameObject);
        GetComponent<HeldItemPlacementSystem>()?.SetExternalActionSuppressed(this,false);
    }

    void Update()
    {
        if(GetComponent<PlayerToolApproach>()?.IsActive==true)return;
        // Target scan diberi interval untuk menghindari pencarian collider setiap frame,
        // tetapi input tool tetap diperiksa setiap frame agar kontrol terasa responsif.
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + scanInterval;
            RefreshTarget();
        }

        if (actionBusy) return;

        if (carriedItem != null)
        {
            WorldInteractionPrompt.Request(this, transform, $"{pullOrStoreKey}: simpan   {dropCarriedKey}: jatuhkan", 0f, 1.65f);
            if (PlayerInteractionTarget.PressPrompt(this, transform, pullOrStoreKey)) StoreCarriedItem();
            else if (PlayerInteractionTarget.PressPrompt(this, transform, dropCarriedKey)) DropCarriedItem();
            return;
        }

        ShowTargetPrompt();
        if (movement != null && movement.IsMovementLocked) return;
        if(!WorldInteractionPrompt.AllowsAction(this))return;

        var shownTarget=WorldInteractionPrompt.TryGetDisplayedAnchor(this,out var shownAnchor)?shownAnchor.GetComponent<WorldGatherable>():target;
        if (shownTarget != null && shownTarget.CanPull && PlayerInteractionTarget.PressPrompt(this, shownTarget.transform, pullOrStoreKey))
            PullTarget();
        else if (hotbar.IsUsePressed(PlayerToolType.Sickle, this))
            UseSickle();
        else if (hotbar.SelectedTool == PlayerToolType.Hammer && shownTarget != null && shownTarget.CanHammer &&
            PlayerInteractionTarget.PressPrompt(this, shownTarget.transform, pullOrStoreKey))
            UseHammer();
        else if (hotbar.IsUsePressed(PlayerToolType.Hammer, this))
            UseHammer();
        else if (hotbar.IsUsePressed(PlayerToolType.Axe, this))
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
            if(hotbar.SelectedTool==PlayerToolType.Sickle&&!candidate.CanSickle||
               hotbar.SelectedTool==PlayerToolType.Hammer&&!candidate.CanHammer)continue;
            Vector3 observer = transform.position + Vector3.up * 0.8f;
            Vector3 interactionPoint = candidate.GetInteractionPoint(observer);
            Vector3 delta = interactionPoint - observer;
            delta.y = 0f;
            float distance = delta.magnitude;
            float floorTolerance=hotbar.SelectedTool==PlayerToolType.Sickle?(sickleSweepProfile!=null?sickleSweepProfile.verticalTolerance:.8f):1.2f;
            if (distance > interactionRange||Mathf.Abs(candidate.transform.position.y-transform.position.y)>floorTolerance) continue;

            // Jika player menyentuh collider, arah dinilai dari pivot sebagai fallback.
            Vector3 direction = delta;
            if (direction.sqrMagnitude < 0.0025f)
            {
                direction = candidate.transform.position - transform.position;
                direction.y = 0f;
            }
            float dot = direction.sqrMagnitude < 0.0025f ? 1f : Vector3.Dot(facing, direction.normalized);
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
            float surface=PlayerInteractionTarget.SurfaceDistance(transform,candidate.transform);
            if (surface>interactionRange||Mathf.Abs(candidate.transform.position.y-transform.position.y)>1.2f||distance < 0.01f) continue;
            float dot = Vector3.Dot(facing, delta / distance);
            if(dot<minimumFacingDot)continue;
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
            prompt = HammerLevel < target.MinimumHammerLevel ? "Level Hammer belum cukup" : $"{pullOrStoreKey}: Pecahkan Batu\n<size=17>Stamina digunakan</size>";
        else if (hotbar.SelectedTool == PlayerToolType.Sickle && target.CanSickle)
            prompt = "F: Sabit";
        else if (target.CanPull)
            prompt = $"{pullOrStoreKey}: Cabut";
        else if (target.CanSickle)
            prompt = "Butuh Sickle";
        else if (target.CanHammer)
            prompt = "Butuh Hammer";

        if (prompt != null)
        {
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (hotbar.SelectedTool == PlayerToolType.Hammer && target.CanHammer)
                WorldInteractionPrompt.RequestClean(this, target.transform, prompt, distance, target.PromptHeight);
            else WorldInteractionPrompt.Request(this, target.transform, prompt, distance, target.PromptHeight);
        }
    }

    void PullTarget()
    {
        var pending=RequestedGatherable();
        if(pending==null||!pending.CanPull)return;
        PlayerToolApproach.For(this).Begin(this,()=>pending.GetInteractionPoint(transform.position+Vector3.up*.6f),.75f,()=>PullReached(pending),()=>pending!=null&&pending.isActiveAndEnabled&&pending.CanPull,pending.transform);
    }
    void PullReached(WorldGatherable pending)
    {
        if (!SpendStamina(pullCost)) return;
        FaceGatherable(pending);
        status?.PulseActivity(PlayerMovementState.ToolAction);
        TriggerAnimation(pullTrigger);
        target = null;
        StartCoroutine(PullImpactRoutine(pending));
    }

    void UseSickle()
    {
        SickleCutTarget chosen=default;bool found=false;
        var requested=RequestedGatherable();
        if(WorldInteractionPrompt.IsDisplayedFor(this)&&(requested==null||!requested.CanSickle))return;
        if(requested!=null&&requested.CanSickle){chosen=new SickleCutTarget(requested,requested.transform.position,0);found=true;}
        if(!found){
            sickleCandidates.Clear();var search=new SickleSweepRegion(transform.position,transform.forward,interactionRange,360,sickleSweepProfile!=null?sickleSweepProfile.verticalTolerance:.8f);
            foreach(var grass in WorldGatherable.Active)if(grass!=null&&grass.isActiveAndEnabled&&grass.CanSickle&&search.Contains(grass.transform.position,out float a))sickleCandidates.Add(new SickleCutTarget(grass,grass.transform.position,a));
            FieldArea.CollectSickleTargets(search,sickleCandidates);TerrainDetailGrassManager.CollectSickleTargets(search,sickleCandidates);
            float best=float.PositiveInfinity;Vector3 facing=movement!=null?movement.FacingDirection:transform.forward;
            foreach(var candidate in sickleCandidates){var delta=candidate.Position-transform.position;delta.y=0;float dot=delta.sqrMagnitude>.001f?Vector3.Dot(facing,delta.normalized):1;if(dot<minimumFacingDot)continue;float score=delta.magnitude-dot*.5f;if(score>=best)continue;best=score;chosen=candidate;found=true;}
        }
        if(!found){SaveLoadFeedback.Instance?.ShowMessage("Tidak ada rumput atau tanaman mati yang bisa disabit");return;}
        var captured=chosen;
        PlayerToolApproach.For(this).Begin(this,()=>captured.Position,Mathf.Min(PlayerToolApproach.MaximumWorkDistance,SickleRadius()),()=>StartSickleSweep(captured),()=>captured.IsAvailable,captured.Anchor,false);
    }
    WorldGatherable RequestedGatherable()
    {
        if(WorldInteractionPrompt.IsDisplayedFor(this))return WorldInteractionPrompt.TryGetDisplayedAnchor(this,out var anchor)?anchor.GetComponent<WorldGatherable>():null;
        RefreshTarget();return target;
    }
    float SickleRadius()
    {
        if(sickleSweepProfile==null)sickleSweepProfile=ScriptableObject.CreateInstance<SickleSweepProfile>();
        return sickleSweepProfile.baseReach+Mathf.Max(0,SickleLevel-1)*sickleSweepProfile.extraReachPerLevel;
    }
    void StartSickleSweep(SickleCutTarget requested)
    {
        if(!requested.IsAvailable)return;
        Vector3 facing=movement!=null?movement.FacingDirection:transform.forward;
        var region=new SickleSweepRegion(transform.position,facing,SickleRadius(),sickleSweepProfile.angle,sickleSweepProfile.verticalTolerance);
        if(!region.Contains(requested.Position,out _)){
            movement?.FaceTowardsInteraction(requested.Position);facing=movement!=null?movement.FacingDirection:transform.forward;
            region=new SickleSweepRegion(transform.position,facing,SickleRadius(),sickleSweepProfile.angle,sickleSweepProfile.verticalTolerance);
            if(!region.Contains(requested.Position,out _))return;
        }
        if (!SpendStamina(StaminaBalance.Current.Cost("sickle_wild_grass"))) return;
        status?.PulseActivity(PlayerMovementState.ToolAction);
        TriggerAnimation(sickleTrigger);
        BeginTimedAction();
        if(sickleSweep==null)sickleSweep=GetComponent<SickleSweepAction>()??gameObject.AddComponent<SickleSweepAction>();
        sickleSweep.Begin(this,sickleSweepProfile,region);
    }

    /// <summary>Plays the swish once as the physical blade enters its cutting phase.</summary>
    public void PlaySickleSwish(Vector3 position)
    {
        if (sickleSwish != null) GameAudio.PlayOneShot(audioSource, sickleSwish, GameAudioBus.Main);
        else FarmActionEffects.PlaySound(FarmEffect.Swing,position);
    }

    /// <summary>Releases the gathering action after its physical follow-through or cancellation.</summary>
    public void FinishSickleSweep(int cut,bool feedback)
    {
        if(feedback)
        SaveLoadFeedback.Instance?.ShowMessage(cut > 0 ? $"Sabit memotong {cut} target" : "Tidak ada rumput di depan");
        if (target != null && !target.IsAvailable) target = null;
        EndTimedAction();
        movement?.CancelToolAnimation();
    }

    void UseHammer()
    {
        // Input tool tidak boleh bergantung pada interval scan; player bisa berbalik dan
        // langsung memukul pada frame yang sama.
        var pending=RequestedGatherable();
        if (pending == null || !pending.CanHammer)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Tidak ada batu di depan");
            return;
        }
        if (HammerLevel < pending.MinimumHammerLevel)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Level Hammer belum cukup");
            return;
        }
        PlayerToolApproach.For(this).Begin(this,()=>pending.GetInteractionPoint(transform.position+Vector3.up*.6f),.8f,()=>HammerReached(pending),()=>pending!=null&&pending.isActiveAndEnabled&&pending.CanHammer,pending.transform);
    }
    void HammerReached(WorldGatherable pending)
    {
        if(HammerLevel<pending.MinimumHammerLevel||!SpendStamina(pending.StaminaCostPerHit))return;
        FaceGatherable(pending);
        GetComponent<PlayerHeldTools>()?.BeginWorkAction(PlayerToolType.Hammer,pending.GetInteractionPoint(transform.position+Vector3.up),.95f);
        status?.PulseActivity(PlayerMovementState.ToolAction);
        TriggerAnimation(hammerTrigger);
        StartCoroutine(HammerImpactRoutine(pending));
    }

    void UseAxe()
    {
        if(WorldInteractionPrompt.TryGetDisplayedAnchor(this,out var anchor))treeTarget=anchor.GetComponent<WorldTree>();else RefreshTarget();
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
        var pending=treeTarget;
        PlayerToolApproach.For(this).Begin(this,()=>{
            var shape=pending.GetComponent<Collider>();return shape!=null?shape.ClosestPoint(transform.position+Vector3.up*.7f):pending.transform.position;
        },.8f,()=>AxeReached(pending),()=>pending!=null&&pending.isActiveAndEnabled&&pending.IsAvailable,pending.transform);
    }
    void AxeReached(WorldTree pending)
    {
        if(AxeLevel<pending.MinimumAxeLevel||!SpendStamina(pending.StaminaCostPerHit))return;
        movement?.FaceTowardsInteraction(pending.transform.position);
        var trunk=pending.GetComponent<Collider>();
        Vector3 hitPoint=trunk!=null?trunk.ClosestPoint(transform.position+Vector3.up*.7f):pending.transform.position+Vector3.up*.6f;
        GetComponent<PlayerHeldTools>()?.BeginWorkAction(PlayerToolType.Axe,hitPoint,.95f);
        status?.PulseActivity(PlayerMovementState.ToolAction);
        TriggerAnimation(axeTrigger);
        StartCoroutine(AxeImpactRoutine(pending));
    }

    void FaceGatherable(WorldGatherable candidate)
    {
        if (movement == null || candidate == null)
            return;

        Vector3 observer = transform.position + Vector3.up * 0.8f;
        Vector3 point = candidate.GetInteractionPoint(observer);
        Vector3 horizontal = point - transform.position;
        horizontal.y = 0f;
        // ClosestPoint dapat sama dengan posisi player saat collider bersentuhan.
        // Pivot objek menjadi fallback supaya arah lock tetap stabil.
        if (horizontal.sqrMagnitude < 0.0025f)
            point = candidate.transform.position;
        movement.FaceTowardsInteraction(point);
    }

    IEnumerator PullImpactRoutine(WorldGatherable pending)
    {
        BeginTimedAction();
        if(pullImpactDelay>0f) yield return new WaitForSeconds(pullImpactDelay);
        if(pending!=null && pending.IsAvailable) pending.Pull(this);
        yield return new WaitForSeconds(Mathf.Max(0f,1.4f-pullImpactDelay));
        EndTimedAction();
    }

    IEnumerator HammerImpactRoutine(WorldGatherable pending)
    {
        BeginTimedAction();
        if(hammerImpactDelay>0f) yield return new WaitForSeconds(hammerImpactDelay);
        if(pending!=null && pending.IsAvailable)
        {
            pending.Hammer(this,HammerLevel);
            cameraFollow?.AddImpulse(0.09f,0.12f);
            if(target==pending && !pending.IsAvailable) target=null;
        }
        yield return new WaitForSeconds(Mathf.Max(0f,0.95f-hammerImpactDelay));
        EndTimedAction();
    }

    IEnumerator AxeImpactRoutine(WorldTree pending)
    {
        BeginTimedAction();
        if(axeImpactDelay>0f) yield return new WaitForSeconds(axeImpactDelay);
        if(pending!=null && pending.IsAvailable)
        {
            var collider=pending.GetComponent<Collider>();
            Vector3 point=collider!=null?collider.ClosestPoint(transform.position+Vector3.up*.75f):pending.transform.position+Vector3.up*.6f;
            pending.Chop(AxeLevel,point);
            cameraFollow?.AddImpulse(0.07f,0.1f);
            if(treeTarget==pending && !pending.IsAvailable) treeTarget=null;
        }
        yield return new WaitForSeconds(Mathf.Max(0f,0.95f-axeImpactDelay));
        EndTimedAction();
    }

    void BeginTimedAction()
    {
        actionBusy=true;
        movement?.AcquireMovementLock(this);
    }

    void EndTimedAction()
    {
        movement?.ReleaseMovementLock(this);
        actionBusy=false;
    }

    void OnDisable() => CancelAction();

    /// <summary>Cancels pending sickle, hammer, axe and pickup contacts without dropping carried items.</summary>
    public void CancelAction()
    {
        GetComponent<PlayerToolApproach>()?.Cancel(this);
        StopAllCoroutines();
        if(sickleSweep!=null)sickleSweep.Cancel();
        movement?.ReleaseMovementLock(this);
        actionBusy = false;
    }

    bool SpendStamina(float amount)
    {
        if (status == null || status.TrySpendStamina(amount)) return true;
        SaveLoadFeedback.Instance?.ShowMessage("Stamina tidak cukup");
        return false;
    }

    void TriggerAnimation(string trigger)
    {
        if(movement!=null&&movement.CharacterAnimator!=null)animator=movement.CharacterAnimator;
        if (animator != null && !string.IsNullOrEmpty(trigger)) animator.SetTrigger(trigger);
    }

    /// <summary>Mencoba memegang hasil gather sebelum disimpan atau dijatuhkan.</summary>
    public bool TryCarry(ItemSO item, int amount, int qualityStars = 0, float fishSizeCm = 0f)
    {
        if (item == null || amount <= 0 || carriedItem != null) return false;
        PlayerAnimalCarry animalCarry = GetComponent<PlayerAnimalCarry>();
        if (animalCarry != null && animalCarry.HasAnimal) return false;
        carriedItem = item;
        carriedAmount = amount;
        carriedQualityStars = Mathf.Clamp(qualityStars, 0, 4);
        carriedFishSizeCm = Mathf.Max(0f, fishSizeCm);
        var anchor=new GameObject("GatheredItemAnchor").transform;anchor.SetParent(transform,false);HeldItemVisuals.NormalizeAnchor(anchor);
        anchor.position=transform.position+Vector3.up*(PlayerDimensions.Height*.62f)+transform.forward*.38f;
        carriedVisual = HeldItemVisuals.Create(item,anchor,$"Carried_{item.itemName}");
        carriedVisual.transform.localRotation = item.category == ItemCategory.Fish
            ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        HeldItemVisuals.Fit(carriedVisual,item);
        GetComponent<HeldItemPlacementSystem>()?.SetExternalActionSuppressed(this,true);
        movement?.SetCarrying(true);
        UpdateToolVisuals(hotbar.SelectedTool);
        return true;
    }

    void StoreCarriedItem()
    {
        if (inventory == null || !inventory.Add(carriedItem, carriedAmount, carriedQualityStars, carriedFishSizeCm))
        {
            SaveLoadFeedback.Instance?.ShowMessage("Inventory penuh");
            return;
        }
        PlayerPickupNotification.ShowItem(inventory, carriedItem, carriedAmount);
        QuestEventHub.Publish(QuestObjectiveType.Collect, carriedItem.name, carriedAmount, carriedItem);
        ClearCarry();
    }

    void DropCarriedItem()
    {
        Vector3 facing = movement != null ? movement.FacingDirection : transform.forward;
        WorldGatherable.SpawnLoosePickup(carriedItem, carriedAmount,
            transform.position + facing.normalized * 1.1f + Vector3.up * 0.25f,
            carriedQualityStars, carriedFishSizeCm);
        ClearCarry();
    }

    void ClearCarry()
    {
        carriedItem = null;
        carriedAmount = 0;
        carriedQualityStars = 0;
        carriedFishSizeCm = 0f;
        if (carriedVisual != null){carriedVisual.SetActive(false);Destroy(carriedVisual.transform.parent.gameObject);}
        GetComponent<HeldItemPlacementSystem>()?.SetExternalActionSuppressed(this,false);
        movement?.SetCarrying(false);
        UpdateToolVisuals(hotbar.SelectedTool);
    }

    void UpdateToolVisuals(PlayerToolType selected)
    {
        if (sickleVisual != null) sickleVisual.SetActive(false);
        if (hammerVisual != null) hammerVisual.SetActive(false);
        if (axeVisual != null) axeVisual.SetActive(false);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null && player.GetComponent<PlayerGatheringTool>() == null)
            player.gameObject.AddComponent<PlayerGatheringTool>();
    }
}
