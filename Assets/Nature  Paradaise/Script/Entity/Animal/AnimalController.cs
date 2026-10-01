using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controller prototipe ternak untuk hunger, pemberian makan, produksi milk,
/// interaction range, dan sinkronisasi hasil dengan inventory player.
/// </summary>
public class AnimalController : MonoBehaviour
{
    static readonly System.Collections.Generic.List<AnimalController> Active = new();
    void OnEnable() => Active.Add(this);
    void OnDisable()
    {
        StopAllCoroutines();
        Active.Remove(this);
        if (playerInv != null)
        {
            var controller = playerInv.GetComponent<PlayerController>();
            if (brushBusy) controller?.SetBrushingAnimation(false);
            if (productCollectBusy && growth != null && growth.Type == AnimalType.Sheep)
                controller?.SetShearingAnimation(false);
            controller?.ReleaseMovementLock(this);
            playerInv.GetComponent<PlayerAnimalPush>()?.CancelIfTarget(growth);
        }
        brushLockedRoutine?.ReleaseCareLock(this);
        brushLockedRoutine=null;
        productLockedRoutine?.ReleaseCareLock(this);
        productLockedRoutine=null;
        brushBusy=false;
        brushProgress=0f;
        productProgress=0f;
        productCollectBusy=false;
    }

    [Header("Heart Care Items")]
    public ItemSO favoriteTreatItem;
    public ItemSO medicineItem;
    [Header("Growth System")]
    [SerializeField] AnimalGrowthSystem growth;
    [SerializeField] KeyCode petKey = KeyCode.R;
    [SerializeField] KeyCode interactKey = KeyCode.T;
    [SerializeField] KeyCode debugNextStageKey = KeyCode.F10;

    [Header("Hunger")]
    public float maxHunger = 100f;
    public float hunger = 0f;

    [Header("Feeding")]
    [Tooltip("Berapa Hunger yang diberikan oleh 1 Cabbage.")]
    public float cabbageHunger = 50f;

    [Header("Milk Production")]
    [Tooltip("Berapa detik sampai hewan menghasilkan susu.")]
    public float milkProductionTime = 5f;

    [Tooltip("Hunger yang dikonsumsi untuk menghasilkan 1 Milk.")]
    public float milkHungerCost = 25f;

    [Header("Interaction")]
    public float interactionRadius = 2f;
    [SerializeField, Min(0f)] float brushImpactDelay = 4.25f;
    [SerializeField, Min(0f)] float brushActionDuration = 5f;
    [SerializeField, Min(0f)] float brushStandPadding = 0.12f;
    [SerializeField, Min(0f)] float productImpactDelay = 1.1f;
    [SerializeField, Min(0f)] float productActionDuration = 2.2f;
    [SerializeField, Min(0f)] float shearImpactDelay = 4.6f;
    [Tooltip("Durasi cukur dalam detik nyata; animasi berulang sampai proses selesai.")]
    [SerializeField, Range(5f,10f)] float shearActionDuration = 5f;
    public float ShearActionDuration => Mathf.Clamp(shearActionDuration,5f,10f);
    public bool IsShearing => productCollectBusy && growth != null && growth.Type == AnimalType.Sheep;

    [InspectorName("Use Held Care Item Key")]
    public KeyCode feedKey = KeyCode.F;
    public KeyCode milkKey = KeyCode.G;

    [Header("Inventory")]
    public Inventory playerInv;

    [InspectorName("Animal Feed Item")]
    [Tooltip("Pakan olahan dari Feed Maker; nama field legacy dipertahankan agar referensi scene lama tetap terbaca.")]
    public ItemSO cabbageItem;

    [Tooltip("ItemSO Milk.")]
    public ItemSO milkItem;

    [Header("Debug UI")]
    public TMP_Text hungerText;

    bool milkReady = false;
    float milkTimer = 0f;
    bool brushBusy;
    float brushProgress;
    float productProgress;
    float brushSideSign=1f;
    AnimalRoutine brushLockedRoutine;
    AnimalRoutine productLockedRoutine;
    SheepWoolVisual sheepWoolVisual;
    bool productCollectBusy;

    ItemSO SelectedMedicine
    {
        get
        {
            InventoryHotbarUI hotbar = playerInv != null ? playerInv.GetComponent<InventoryHotbarUI>() : null;
            ItemSO selected = hotbar != null ? hotbar.SelectedItem : null;
            return selected != null && selected.IsAnimalMedicine ? selected : null;
        }
    }

    ItemSO SelectedItem => playerInv != null
        ? playerInv.GetComponent<InventoryHotbarUI>()?.SelectedItem
        : null;

    void Awake()
    {
        // P bentrok dengan Place Item pada save/prefab lama. R dipakai kontekstual untuk Rawat/Elus.
        if(petKey==KeyCode.P) petKey=KeyCode.R;
        // Scene prototipe lama pernah menghubungkan debug text Cow ke label milik NPCSeller.
        // Jangan menulis status hewan ke UI yang bukan child dari hewan ini.
        if (hungerText != null && !hungerText.transform.IsChildOf(transform))
            hungerText = null;
        ApplyCareDefaults();
        if (playerInv == null)
            playerInv = FindFirstObjectByType<Inventory>();
        if (growth == null)
            growth = GetComponent<AnimalGrowthSystem>();
    }

    public void ConfigureRuntime(
        Inventory inventory,
        ItemSO feedItem,
        ItemSO productItem,
        AnimalGrowthSystem growthSystem)
    {
        playerInv = inventory;
        cabbageItem = feedItem;
        milkItem = productItem;
        growth = growthSystem;
        ApplyCareDefaults();
        EnsureSheepWoolVisual();
    }

    public void ApplyCareDefaults()
    {
        if (growth == null) growth = GetComponent<AnimalGrowthSystem>();
        AnimalCareCatalog catalog = AnimalCareCatalog.Load();
        if (catalog == null) return;
        if (cabbageItem == null || cabbageItem.itemName == "Cabbage" || cabbageItem.name == "Grass") cabbageItem = catalog.fodder;
        if (favoriteTreatItem == null) favoriteTreatItem = catalog.treat;
        if (medicineItem == null) medicineItem = catalog.medicine;
        if (milkItem == null && growth != null) milkItem = catalog.Product(growth.Type);
    }

    void Update()
    {
        if (growth == null) growth = GetComponent<AnimalGrowthSystem>();
        EnsureSheepWoolVisual();
        UpdateMilkProduction();
        UpdateDebugUI();
        if (playerInv == null)
            return;
        PlayerAnimalCarry animalCarry = playerInv.GetComponent<PlayerAnimalCarry>();
        if (animalCarry != null && animalCarry.IsCarrying(growth)) return;
        PlayerAnimalPush animalPush=playerInv.GetComponent<PlayerAnimalPush>();
        if(animalPush!=null && animalPush.HasAnimal)
        {
            if(animalPush.IsPushing(growth))
            {
                float pushDistance=Vector3.Distance(transform.position,playerInv.transform.position);
                WorldInteractionPrompt.RequestClean(this,transform,"WASD: Arah dorong | Lepas tombol: Selesai",pushDistance,1.75f);
            }
            return;
        }
        AnimalRoutine routine = GetComponent<AnimalRoutine>();
        bool housed = routine?.IsHoused ?? false;
        bool visitingThisHome = housed && BarnInterior.Current != null &&
                                BarnInterior.Current.home == routine?.Home;
        bool carryable = growth != null && growth.HasBeenBorn && AnimalGrowthProfileSO.IsBird(growth.Type);
        bool pushable = growth != null && growth.HasBeenBorn && !AnimalGrowthProfileSO.IsBird(growth.Type);
        // Model housed disembunyikan di world, tetapi saat player berada di interior
        // kandang yang benar semua jenis hewan harus bisa di-pet/feed/inspect.
        if (housed && !visitingThisHome) return;
        if (playerInv.GetComponent<PlayerController>()?.IsMovementLocked ?? false) return;
        if (WorldInteractionPrompt.IsSuppressed) return;

        float distance = Vector3.Distance(
            transform.position,
            playerInv.transform.position
        );

        // Player terlalu jauh dari hewan
        if (!PlayerInteractionTarget.ContainsPickup(playerInv.transform, transform, interactionRadius))
            return;
        // Hanya hewan terdekat menerima satu tombol interaksi, bukan seluruh kandang.
        foreach (AnimalController other in Active)
        {
            if (other == this || other == null) continue;
            float otherDistance = Vector3.Distance(other.transform.position, playerInv.transform.position);
            if (PlayerInteractionTarget.ContainsPickup(playerInv.transform, other.transform, other.interactionRadius) &&
                (otherDistance < distance || (Mathf.Approximately(otherDistance, distance) && other.GetInstanceID() < GetInstanceID()))) return;
        }

        string displayName=LocalizedAnimalName(growth);
        string interactionPrompt=growth!=null
            ? growth.Type == AnimalType.Cow
                ? $"{displayName} {growth.GenderSymbol}   ♥ {growth.CowHeartLevel}/5 ({growth.CowCarePercent}%)   {growth.AgeLabel}"
                : $"{displayName}   ♥ {growth.HeartLevel}/10   {MoodLabel(growth.Happiness)}"
            : displayName;
        List<string> actions=new();
        if(growth!=null && !growth.PetToday) actions.Add($"{petKey}: Gosok");
        if(growth!=null && growth.Type==AnimalType.Cow)
            actions.Add(growth.InteractedToday ? $"{interactKey}: Interaksi (✓ hari ini)" : $"{interactKey}: Interaksi harian");
        if (carryable && animalCarry != null && !animalCarry.HasAnimal)
            actions.Add("E: Angkat");
        else if(pushable) actions.Add("E lalu WASD: Dorong");
        if (IsProductReady)
            actions.Add(growth!=null && growth.Type==AnimalType.Sheep
                ? (SelectedItem!=null && SelectedItem.equippedTool==PlayerToolType.Shears
                    ? $"{milkKey}: Cukur bulu (5 Wool)"
                    : "Pilih Shears untuk mencukur")
                : $"{milkKey}: Ambil produk");
        else if(growth!=null && growth.Type==AnimalType.Sheep)
            actions.Add(growth.SheepShearingSummary);
        ItemSO selectedItem = SelectedItem;
        if (selectedItem != null && selectedItem == cabbageItem && growth != null && !growth.FedToday)
            actions.Add($"{feedKey}: Beri makan");
        else if (selectedItem != null && selectedItem == favoriteTreatItem && growth != null && growth.CanReceiveTreat)
            actions.Add($"{feedKey}: Beri treat");
        ItemSO selectedMedicine = SelectedMedicine;
        if (selectedMedicine != null && growth != null && growth.CanReceiveMedicine)
            actions.Add($"{feedKey}: Beri obat");
        actions.Add("I: Detail");
        interactionPrompt+="\n"+string.Join("   ",actions);
        if(HUDManager.DebugCluesEnabled && growth!=null)
            interactionPrompt+=$"\nDEBUG  {growth.GrowthStage} | {growth.HealthSummary} | " +
                $"Makan {(growth.FedToday?"✓":"✗")} | Produk {(growth.HasProductReady?"✓":"✗")} | {debugNextStageKey}: Next";
        WorldInteractionPrompt.RequestClean(this,transform,interactionPrompt,distance,1.75f);

        HandleInput();
    }

    void HandleInput()
    {
        PlayerAnimalCarry animalCarry = playerInv.GetComponent<PlayerAnimalCarry>();
        if (growth != null && growth.HasBeenBorn && AnimalGrowthProfileSO.IsBird(growth.Type) &&
            animalCarry != null && !animalCarry.HasAnimal &&
            PlayerInteractionTarget.PressPickup(playerInv.transform, transform, KeyCode.E, interactionRadius))
        {
            animalCarry.TryPickup(growth);
            return;
        }
        if(growth!=null && growth.HasBeenBorn && !AnimalGrowthProfileSO.IsBird(growth.Type) &&
           PlayerInteractionTarget.PressPickup(playerInv.transform,transform,KeyCode.E,interactionRadius))
        {
            PlayerAnimalPush animalPush=playerInv.GetComponent<PlayerAnimalPush>();
            if(animalPush==null) animalPush=playerInv.gameObject.AddComponent<PlayerAnimalPush>();
            animalPush.TryBeginPush(growth);
            return;
        }

        if (PlayerInteractionTarget.Press(playerInv.transform, transform, KeyCode.I))
        {
            AnimalCarePanel.Show(growth, null, playerInv);
            return;
        }

        if (growth != null && growth.Type == AnimalType.Cow &&
            PlayerInteractionTarget.Press(playerInv.transform, transform, interactKey))
        {
            PlayerController controller = playerInv.GetComponent<PlayerController>();
            controller?.FaceTowardsInteraction(transform.position);
            controller?.PlayHandOverAnimation();
            if (!growth.Interact())
                SaveLoadFeedback.Instance?.ShowMessage(
                    $"Interaksi dengan {growth.AnimalName} sudah dihitung hari ini. " +
                    $"{growth.CowDailyCareSummary}. Bisa dihitung lagi besok.");
            return;
        }
        // =========================
        // FEED CABBAGE
        // =========================

        if (PlayerInteractionTarget.Press(playerInv.transform, transform, feedKey))
        {
            ItemSO selected = SelectedItem;
            if (selected != null && selected.IsAnimalMedicine)
                TryGiveMedicine();
            else if (selected != null && selected == favoriteTreatItem)
                TryGiveTreat();
            else if (selected != null && selected == cabbageItem)
                FeedCabbage();
        }

        // =========================
        // TAKE MILK
        // =========================

        if (PlayerInteractionTarget.Press(playerInv.transform, transform, milkKey))
        {
            TakeMilk();
        }

        if (!brushBusy && !productCollectBusy && growth != null && growth.HasBeenBorn && !growth.PetToday &&
            PlayerInteractionTarget.Press(playerInv.transform, transform, petKey))
            TryStartBrush();
        if (HUDManager.DebugCluesEnabled && Input.GetKeyDown(debugNextStageKey))
            growth?.DebugAdvanceToNextStage();
    }

    public void FeedCabbage()
    {
        if (growth != null && (!growth.HasBeenBorn || growth.FedToday)) return;
        if (cabbageItem == null)
        {
            Debug.LogWarning(
                "[ANIMAL] Cabbage Item belum di-assign."
            );
            return;
        }

        int cabbageCount = playerInv.GetCount(cabbageItem);

        if (cabbageCount <= 0)
        {
            Debug.Log(
                "[ANIMAL] Tidak punya Cabbage untuk memberi makan."
            );
            return;
        }

        // Hapus 1 Cabbage dari inventory
        if (!playerInv.Remove(cabbageItem, 1))
            return;

        // Tambahkan hunger
        hunger += cabbageHunger;

        // Clamp supaya tidak lebih dari max
        hunger = Mathf.Clamp(hunger, 0f, maxHunger);
        growth?.RegisterFeeding(cabbageHunger, AnimalFoodSource.HandFeed);
        playerInv.GetComponent<PlayerController>()?.PlayHandOverAnimation();

        Debug.Log(
            $"[ANIMAL] Diberi makan {cabbageItem.itemName}. " +
            $"Hunger: {hunger}/{maxHunger}"
        );
    }

    void UpdateMilkProduction()
    {
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused) return;
        // Growth adalah sumber nutrisi utama, termasuk makanan dari grazing tanpa FeedCabbage.
        if (growth != null) hunger = growth.Fullness;
        if(growth!=null && growth.Type==AnimalType.Sheep)
        {
            growth.RefreshSheepWoolReadiness();
            return;
        }
        // Kalau susu sudah siap, jangan produksi lagi
        // sampai player mengambilnya.
        if (IsProductReady)
            return;

        // Hewan hanya memulai produksi setelah Adult dan kondisi hari ini ideal.
        if (growth != null && !growth.IsProductionEligible)
        {
            milkTimer = 0f;
            return;
        }

        // Tidak punya Hunger → tidak bisa menghasilkan susu
        if (hunger <= 0f)
        {
            milkTimer = 0f;
            return;
        }

        milkTimer += Time.deltaTime;

        if (milkTimer >= milkProductionTime)
        {
            milkTimer = 0f;

            // Konsumsi Hunger
            hunger -= milkHungerCost;
            hunger = Mathf.Max(hunger, 0f);

            // Susu siap diambil
            milkReady = true;
            growth?.SetProductReady(true);

            Debug.Log(
                $"[ANIMAL] Milk READY! " +
                $"Hunger: {hunger}/{maxHunger}"
            );
        }
    }

    public void TakeMilk()
    {
        if (productCollectBusy || brushBusy) return;
        if (!IsProductReady)
        {
            Debug.Log(
                "[ANIMAL] Milk belum siap."
            );
            return;
        }

        if (milkItem == null)
        {
            Debug.LogWarning(
                "[ANIMAL] Milk Item belum di-assign."
            );
            return;
        }

        productCollectBusy=true;
        PlayerController controller=playerInv != null ? playerInv.GetComponent<PlayerController>() : null;
        controller?.AcquireMovementLock(this);
        bool shearing=growth != null && growth.Type==AnimalType.Sheep;
        if(shearing)
        {
            if(SelectedItem==null || SelectedItem.equippedTool!=PlayerToolType.Shears)
            {
                productCollectBusy=false;
                controller?.ReleaseMovementLock(this);
                SaveLoadFeedback.Instance?.ShowMessage("Pilih Shears di hotbar untuk mencukur domba.");
                return;
            }
            AnimalRoutine routine=growth.GetComponent<AnimalRoutine>();
            if(routine!=null && !routine.AcquireCareLock(this))
            {
                productCollectBusy=false;
                controller?.ReleaseMovementLock(this);
                SaveLoadFeedback.Instance?.ShowMessage("Domba sedang melakukan aktivitas lain. Coba lagi sebentar.");
                return;
            }
            productLockedRoutine=routine;
            AlignPlayerForBrush(controller,true);
            controller?.SetShearingAnimation(true);
        }
        else controller?.PlayMilkingAnimation();
        StartCoroutine(CollectProductRoutine(controller,shearing));
    }

    void EnsureSheepWoolVisual()
    {
        if (growth == null || growth.Type != AnimalType.Sheep || sheepWoolVisual != null) return;
        sheepWoolVisual = GetComponent<SheepWoolVisual>();
        if (sheepWoolVisual == null) sheepWoolVisual = gameObject.AddComponent<SheepWoolVisual>();
    }

    static string LocalizedAnimalName(AnimalGrowthSystem animal)
    {
        if(animal==null) return "Hewan";
        string defaultName=animal.Type switch
        {
            AnimalType.Chicken=>"Ayam",
            AnimalType.Duck=>"Bebek",
            AnimalType.Goat=>"Kambing",
            AnimalType.Sheep=>"Domba",
            AnimalType.Cow=>"Sapi",
            _=>animal.Type.ToString()
        };
        return string.Equals(animal.AnimalName,animal.Type.ToString(),System.StringComparison.OrdinalIgnoreCase)
            ? defaultName
            : animal.AnimalName;
    }

    static string MoodLabel(float happiness) => happiness>=70f?"Senang":happiness>=35f?"Tenang":"Stres";

    IEnumerator CollectProductRoutine(PlayerController controller,bool shearing)
    {
        float impact=shearing?shearImpactDelay:productImpactDelay;
        float duration=shearing?ShearActionDuration:productActionDuration;
        if(shearing)
        {
            productProgress=0f;
            float elapsed=0f;
            duration=Mathf.Max(0.01f,duration);
            while(elapsed<duration)
            {
                elapsed+=Time.deltaTime;
                productProgress=Mathf.Clamp01(elapsed/duration);
                AlignPlayerForBrush(controller,false);
                yield return null;
            }
            controller?.SetShearingAnimation(false);
        }
        else if(impact>0f) yield return new WaitForSeconds(impact);

        if(IsProductReady && milkItem!=null)
        {
            // Grade ditentukan saat produk siap, bukan sesudah status produksi direset.
            int quality=growth != null?growth.ProductQualityLevel:1;
            int amount=shearing?5:1;
            if(playerInv != null && playerInv.Add(milkItem,amount,quality))
            {
                milkReady=false;
                growth?.MarkProductCollected();
                sheepWoolVisual?.Refresh();
                PlayerPickupNotification.ShowItem(playerInv,milkItem,amount);
                SaveLoadFeedback.Instance?.ShowMessage(shearing
                    ? $"Pencukuran selesai: {amount} Wool masuk inventory. Wol tumbuh penuh lagi dalam 12 hari."
                    : $"{milkItem.itemName} ({(growth != null ? growth.ProductQualityLabel : AnimalCareCatalog.QualityName(quality))}) berhasil diambil");
                milkTimer=0f;
            }
            else SaveLoadFeedback.Instance?.ShowMessage("Inventory penuh; produk tetap tersimpan pada hewan");
        }

        if(!shearing)
        {
            float remaining=Mathf.Max(0f,duration-impact);
            if(remaining>0f) yield return new WaitForSeconds(remaining);
        }
        controller?.ReleaseMovementLock(this);
        productLockedRoutine?.ReleaseCareLock(this);
        productLockedRoutine=null;
        productProgress=0f;
        productCollectBusy=false;
    }

    void UpdateDebugUI()
    {
        if (hungerText == null)
            return;

        // Status rinci hewan adalah debug clue dan mengikuti master toggle HUD (F9).
        hungerText.gameObject.SetActive(HUDManager.DebugCluesEnabled);
        if (!HUDManager.DebugCluesEnabled)
            return;

        string milkStatus = growth != null && growth.Type == AnimalType.Sheep
            ? (IsShearing ? $"Mencukur: {ShearActionDuration*(1f-productProgress):0.0} detik tersisa" : growth.SheepShearingSummary)
            : IsProductReady
            ? "READY TO MILK!"
            : "Milk: Not Ready";

        hungerText.text = growth != null
            ? $"{growth.StatusSummary}\n{milkStatus}"
            : $"Hunger: {Mathf.RoundToInt(hunger)}/{Mathf.RoundToInt(maxHunger)}\n{milkStatus}";
    }

    void Start()
    {
        UpdateDebugUI();
    }

    IEnumerator BrushRoutine()
    {
        brushBusy=true;
        brushProgress=0f;
        PlayerController controller=playerInv != null ? playerInv.GetComponent<PlayerController>() : null;
        controller?.AcquireMovementLock(this);
        AlignPlayerForBrush(controller,true);
        controller?.SetBrushingAnimation(true);

        float duration=Mathf.Max(0.01f,brushActionDuration);
        float impact=Mathf.Clamp(brushImpactDelay,0f,duration);
        float elapsed=0f;
        bool careApplied=false;
        while(elapsed<duration)
        {
            elapsed+=Time.deltaTime;
            brushProgress=Mathf.Clamp01(elapsed/duration);
            AlignPlayerForBrush(controller,false);
            if(!careApplied && elapsed>=impact)
            {
                growth?.Pet();
                careApplied=true;
            }
            yield return null;
        }
        if(!careApplied) growth?.Pet();
        controller?.SetBrushingAnimation(false);
        brushProgress=1f;
        controller?.ReleaseMovementLock(this);
        brushLockedRoutine?.ReleaseCareLock(this);
        brushLockedRoutine=null;
        brushBusy=false;
        brushProgress=0f;
    }

    public bool TryStartBrush()
    {
        if (brushBusy || productCollectBusy || growth == null || !growth.HasBeenBorn || growth.PetToday) return false;
        AnimalRoutine routine=growth.GetComponent<AnimalRoutine>();
        if(routine!=null && !routine.AcquireCareLock(this)) return false;
        brushLockedRoutine=routine;
        StartCoroutine(BrushRoutine());
        return true;
    }

    void AlignPlayerForBrush(PlayerController controller,bool chooseSide)
    {
        if(controller==null) return;
        Transform player=controller.transform;
        Vector3 side=transform.right;
        side.y=0f;
        if(side.sqrMagnitude<0.001f) side=Vector3.right;
        side.Normalize();
        if(chooseSide)
            brushSideSign=Vector3.Dot(player.position-transform.position,side)>=0f?1f:-1f;

        float animalExtent=0.65f;
        Collider animalCollider=GetComponent<Collider>();
        if(animalCollider!=null)
        {
            Vector3 extents=animalCollider.bounds.extents;
            animalExtent=Mathf.Abs(side.x)*extents.x+Mathf.Abs(side.z)*extents.z;
        }
        CharacterController character=player.GetComponent<CharacterController>();
        float playerRadius=character!=null?character.radius:0.35f;
        Vector3 target=transform.position+side*(brushSideSign*(animalExtent+playerRadius+brushStandPadding));
        target.y=transform.position.y;
        if(AnimalWalkingPath.Ground(target,transform,player,out Vector3 grounded,true))
            target=grounded;
        else
            target.y=player.position.y;
        bool characterEnabled=character!=null && character.enabled;
        if(characterEnabled) character.enabled=false;
        player.position=target;
        if(characterEnabled) character.enabled=true;
        controller.FaceTowardsInteraction(transform.position);
    }

    void OnGUI()
    {
        bool showingShearing=productCollectBusy && growth!=null && growth.Type==AnimalType.Sheep;
        if(!brushBusy && !showingShearing) return;

        float width=Mathf.Min(420f,Screen.width-32f);
        float panelHeight=72f;
        float x=(Screen.width-width)*0.5f;
        float y=Mathf.Max(20f,Screen.height-230f);
        Rect panel=new(x,y,width,panelHeight);
        Rect label=new(x+14f,y+8f,width-28f,24f);
        Rect track=new(x+14f,y+39f,width-28f,18f);
        float shownProgress=showingShearing?productProgress:brushProgress;
        Rect fill=new(track.x+2f,track.y+2f,(track.width-4f)*Mathf.Clamp01(shownProgress),track.height-4f);

        Color previous=GUI.color;
        GUI.color=new Color(0.03f,0.05f,0.05f,0.78f);
        GUI.DrawTexture(panel,Texture2D.whiteTexture);
        GUI.color=new Color(0.02f,0.04f,0.04f,0.9f);
        GUI.DrawTexture(track,Texture2D.whiteTexture);
        GUI.color=new Color(0.48f,0.94f,0.55f,0.95f);
        GUI.DrawTexture(fill,Texture2D.whiteTexture);
        GUI.color=Color.white;
        float shownDuration=showingShearing?ShearActionDuration:brushActionDuration;
        float seconds=Mathf.Max(0f,shownDuration*(1f-shownProgress));
        string animalLabel=growth!=null?LocalizedAnimalName(growth):"Hewan";
        GUI.Label(label,$"{(showingShearing?"Mencukur":"Menggosok")} {animalLabel}...  {seconds:0.0} detik");
        GUI.color=previous;
    }

    bool IsProductReady => growth != null ? growth.HasProductReady : milkReady;

    public bool TryGiveTreat()
    {
        if (growth == null || !growth.CanReceiveTreat || favoriteTreatItem == null || playerInv == null ||
            !playerInv.Remove(favoriteTreatItem, 1)) return false;
        growth.GiveFavoriteTreat();
        playerInv.GetComponent<PlayerController>()?.PlayHandOverAnimation();
        SaveLoadFeedback.Instance?.ShowMessage($"{growth.AnimalName} menyukai treat ini");
        return true;
    }

    public bool TryGiveMedicine()
    {
        return TryAdministerMedicine(SelectedMedicine);
    }

    /// <summary>Dipakai panel kandang: pilih obat terendah yang cukup, atau stok terkuat jika semuanya terlalu lemah.</summary>
    public bool TryGiveBestMedicine()
    {
        if (growth == null || playerInv == null || !growth.CanReceiveMedicine) return false;
        int required = growth.IllnessStage == AnimalIllnessStage.Critical ? 3 :
            growth.IllnessStage == AnimalIllnessStage.Severe ? 2 : 1;
        AnimalCareCatalog catalog = AnimalCareCatalog.Load();
        if (catalog == null) return false;
        for (int i = required; i <= 3; i++)
        {
            ItemSO candidate = catalog.Medicine((AnimalMedicineLevel)i);
            if (candidate != null && playerInv.GetCount(candidate) > 0) return TryAdministerMedicine(candidate);
        }
        for (int i = required - 1; i >= 1; i--)
        {
            ItemSO candidate = catalog.Medicine((AnimalMedicineLevel)i);
            if (candidate != null && playerInv.GetCount(candidate) > 0) return TryAdministerMedicine(candidate);
        }
        return false;
    }

    void OnValidate()
    {
        if(petKey==KeyCode.P) petKey=KeyCode.R;
        brushImpactDelay=Mathf.Max(0f,brushImpactDelay);
        brushActionDuration=Mathf.Max(brushImpactDelay,brushActionDuration);
        brushStandPadding=Mathf.Max(0f,brushStandPadding);
        productImpactDelay=Mathf.Max(0f,productImpactDelay);
        productActionDuration=Mathf.Max(productImpactDelay,productActionDuration);
        shearImpactDelay=Mathf.Max(0f,shearImpactDelay);
        shearActionDuration=Mathf.Clamp(shearActionDuration,5f,10f);
    }

    bool TryAdministerMedicine(ItemSO item)
    {
        if (growth == null || playerInv == null) return false;
        if (!growth.CanReceiveMedicine)
        {
            SaveLoadFeedback.Instance?.ShowMessage($"{growth.AnimalName} tidak membutuhkan obat.");
            return false;
        }
        if (item == null || !item.IsAnimalMedicine || playerInv.GetCount(item) <= 0)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Pilih Animal Medicine dari hotbar.");
            return false;
        }
        if (!playerInv.Remove(item, 1)) return false;
        AnimalMedicineResult result = growth.TreatWithMedicine(item.animalMedicineLevel);
        if (result == AnimalMedicineResult.Rejected)
        {
            playerInv.Add(item, 1);
            return false;
        }
        PlayMedicineAnimation();
        return true;
    }

    void PlayMedicineAnimation()
    {
        PlayerController controller = playerInv != null ? playerInv.GetComponent<PlayerController>() : null;
        if (controller != null)
        {
            controller.PlayHandOverAnimation();
            return;
        }
        Animator animator = playerInv != null ? playerInv.GetComponentInChildren<Animator>() : null;
        if (animator == null) return;
        string[] candidates = { "GiveMedicine", "UseItem", "UseTool" };
        for (int i = 0; i < candidates.Length; i++)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == candidates[i])
                {
                    animator.SetTrigger(candidates[i]);
                    return;
                }
        }
    }

    /// <summary>Reset timer legacy setelah load agar state runtime tidak bocor ke save yang dipulihkan.</summary>
    public void RestoreProduction(float savedFullness)
    {
        hunger = Mathf.Clamp(savedFullness, 0f, maxHunger);
        milkTimer = 0f;
        milkReady = false;
    }
}
