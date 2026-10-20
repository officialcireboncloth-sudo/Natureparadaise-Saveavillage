using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Status satu slot penetasan yang disimpan lintas pergantian scene.</summary>
public enum EggIncubatorState { Empty, Incubating, ChickReady }

[Serializable]
/// <summary>Identitas telur, pemilik kandang, serta tanggal penetasan untuk save game.</summary>
public sealed class EggIncubatorSaveData
{
    public string id,homeId,animalId;
    public int startedDay,readyDay;
    public EggIncubatorState state;
    public EggIncubatorSaveData Copy() => (EggIncubatorSaveData)MemberwiseClone();
}

/// <summary>
/// Mengelola satu telur per kandang, animasi penempatan, dan penetasan lintas scene.
/// Inventory serta kapasitas kandang berubah hanya pada momen kontak tangan dengan sarang.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1200)]
public sealed class EggIncubator : MonoBehaviour
{
    static readonly Dictionary<string,EggIncubatorSaveData> Records=new();
    static readonly List<EggIncubator> Views=new();
    static bool transaction;
    [SerializeField,Min(1)] int incubationDays=3;
    [SerializeField,Min(.5f)] float interactionRadius=1.6f;
    [SerializeField] Transform nest,interactionPoint,releasePoint;
    [SerializeField] GameObject eggVisual,coverVisual;
    [Header("Placement Animation")]
    [SerializeField, Min(.05f), Tooltip("Durasi membuka atau menutup penutup, dalam detik nyata.")]
    float coverDuration = .25f;
    [SerializeField, Range(0f, 100f), Tooltip("Sudut penutup terhadap engsel belakang.")]
    float coverOpenAngle = 75f;
    [SerializeField, Min(.05f), Tooltip("Momen kontak sejak Place Item dimulai; mengikuti animasi player yang sudah ada.")]
    float placementContactDelay = .65f;
    [SerializeField, Min(.1f), Tooltip("Durasi Place Item sebelum penutup kembali ditutup.")]
    float placementDuration = 1.15f;
    [SerializeField, Range(0f, .5f), Tooltip("Jarak maju dari marker berdiri agar tangan mencapai pusat sarang di atas meja, dalam meter.")]
    float standForwardOffset = .22f;
    PlayerController placingPlayer;
    HeldItemPlacementSystem heldItems;
    PlayerHeldTools playerHands;
    TimeManager placementClock;
    GameObject movingEgg;
    bool placementBusy, reachingNest, eggCommitted, coverPoseCached;
    float reachWeight;
    float lastPalmDistance, contactDistance;
    Vector3 coverClosedPosition, coverPivot;
    Quaternion coverClosedRotation;
    AnimalHome home;
    /// <summary>True selama penutup dan animasi player masih menyelesaikan satu penempatan.</summary>
    public bool IsPlacing => placementBusy;
    public string Id => home!=null?"incubator.coop."+home.Id+".01":null;
    public AnimalHome Home => home;
    public Transform Nest => nest;
    public int IncubationDays => Mathf.Max(1,incubationDays);
    public EggIncubatorSaveData Snapshot => GetRecord()?.Copy();
    public EggIncubatorState State => GetRecord()?.state??EggIncubatorState.Empty;
    public int RemainingDays => State==EggIncubatorState.Incubating?Mathf.Max(0,GetRecord().readyDay-CurrentDay):0;
    public static int CurrentDay => TimeManager.Instance!=null?TimeManager.Instance.day:1;
    public string StatusText => State==EggIncubatorState.ChickReady?"Anak Ayam\nStatus: Sudah Menetas":State==EggIncubatorState.Incubating?
        $"Telur Ayam\nStatus: Sedang Ditetaskan\nSisa Hari: {RemainingDays} Hari":"Penetasan Kosong";
    EggIncubatorSaveData GetRecord() => Id!=null&&Records.TryGetValue(Id,out var record)?record:null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Records.Clear();Views.Clear();transaction=false;
        TimeManager.OnDay-=AdvanceToday;TimeManager.OnDay+=AdvanceToday;
    }
    static void AdvanceToday()=>AdvanceAll(CurrentDay);
    void OnEnable(){if(!Views.Contains(this))Views.Add(this);}
    void OnDisable(){CancelPlacement();Views.Remove(this);home=null;}
    void Update(){AdvanceAll(CurrentDay);RefreshVisual();}
    public void Bind(AnimalHome target)
    {
        if (home != target) CancelPlacement();
        if(isActiveAndEnabled&&!Views.Contains(this))Views.Add(this);
        home=target!=null&&target.Kind==AnimalHousingKind.Coop?target:null;
        AdvanceAll(CurrentDay);RefreshVisual();
    }
#if UNITY_EDITOR
    public void EditorConfigure(Transform nestPoint,Transform usePoint,Transform exitPoint,GameObject egg,GameObject cover)
    {nest=nestPoint;interactionPoint=usePoint;releasePoint=exitPoint;eggVisual=egg;coverVisual=cover;}
#endif
    public float DistanceTo(Transform player)
    {
        if(home==null||player==null||interactionPoint==null)return float.PositiveInfinity;
        var delta=player.position-interactionPoint.position;
        return Mathf.Abs(delta.y)>1.8f?float.PositiveInfinity:new Vector2(delta.x,delta.z).magnitude;
    }
    public bool InRange(Transform player)=>DistanceTo(player)<=interactionRadius;
    public bool HandleInteraction(PlayerController player)
    {
        if (placementBusy) return true;
        if(!CanInteract(player))return false;
        // Do not steal E from the normal "Turunkan Ayam" action while hands are occupied.
        if(player.GetComponent<PlayerAnimalCarry>()?.HasAnimal==true)return false;
        var hotbar=player.GetComponent<InventoryHotbarUI>();
        bool selectedEgg=IsChickenEgg(hotbar?.SelectedItem)&&hotbar.SelectedStack.count>0;
        string text=State==EggIncubatorState.Empty?
            selectedEgg?(home.HasRoom(AnimalType.Chicken)?"E: Taruh Telur":"Kandang penuh — perlu 1 slot kosong"):
                "Penetasan Kosong\nPilih telur ayam di hotbar":
            State==EggIncubatorState.Incubating?StatusText:StatusText+"\n"+(Application.isMobilePlatform?"Angkat Anak Ayam | Keluarkan":"E: Angkat Anak Ayam | F: Keluarkan");
        if (Application.isMobilePlatform) text = text.Replace("E: Taruh Telur", "Taruh Telur");
        WorldInteractionPrompt.Request(this,interactionPoint,text,DistanceTo(player.transform),.6f);
        if(State==EggIncubatorState.Empty&&selectedEgg&&PlayerInteractionTarget.PressPrompt(this,KeyCode.E))
        {
            TryBeginPlacement(player,out string message);
            SaveLoadFeedback.Instance?.ShowMessage(message);
        }
        else if(State==EggIncubatorState.ChickReady)
        {
            if(PlayerInteractionTarget.PressPrompt(this,KeyCode.E))
            {
                TryTake(player.GetComponent<PlayerAnimalCarry>(),out string message);SaveLoadFeedback.Instance?.ShowMessage(message);
            }
            else if(PlayerInteractionTarget.PressPrompt(this,KeyCode.F))
            {
                TryRelease(out _,out string message);SaveLoadFeedback.Instance?.ShowMessage(message);
            }
        }
        return true;
    }

    bool CanInteract(PlayerController player) => isActiveAndEnabled && home != null && home.Available &&
        player != null && player.isActiveAndEnabled && InRange(player.transform) &&
        Time.timeScale > 0f && !GameplayPauseMenu.BlocksGameplayInput &&
        !player.IsMovementLocked && !WorldInteractionPrompt.IsSuppressed &&
        player.GetComponent<PlayerAnimalCarry>()?.HasAnimal != true;

    /// <summary>Mencari penetasan pemilik prompt yang benar-benar ditampilkan untuk tombol sentuh.</summary>
    public static EggIncubator GetMobileTarget(PlayerController player)
    {
        foreach (var view in Views)
            if (view != null && view.CanInteract(player) && WorldInteractionPrompt.IsDisplayedFor(view)) return view;
        return null;
    }

    /// <summary>Memeriksa kesiapan penempatan tanpa mengubah inventory atau memulai animasi.</summary>
    public bool CanPlaceSelectedEgg(PlayerController player)
    {
        var hotbar = player != null ? player.GetComponent<InventoryHotbarUI>() : null;
        return CanInteract(player) && hotbar != null &&
            CanInsert(player.GetComponent<Inventory>(), hotbar.SelectedIndex, false, out _);
    }

    /// <summary>Jalur aksi bersama PC dan mobile; selalu memeriksa ulang target serta kondisi player.</summary>
    public bool RequestInteraction(PlayerController player, bool release = false)
    {
        if (!CanInteract(player) || !WorldInteractionPrompt.IsDisplayedFor(this) || GameplayInput.ConsumedThisFrame) return false;
        string message;
        bool accepted;
        if (State == EggIncubatorState.Empty && !release) accepted = TryBeginPlacement(player, out message);
        else if (State == EggIncubatorState.ChickReady)
            accepted = release ? TryRelease(out _, out message) : TryTake(player.GetComponent<PlayerAnimalCarry>(), out message);
        else return false;
        GameplayInput.ConsumeCurrentFrame();
        SaveLoadFeedback.Instance?.ShowMessage(message);
        return accepted;
    }

    /// <summary>Memulai animasi tanpa mengonsumsi telur. Transaksi dilakukan pada frame kontak.</summary>
    public bool TryBeginPlacement(PlayerController player, out string message)
    {
        message = "Selesaikan aksi sebelumnya dan dekati penetasan.";
        if (!Application.isPlaying || placementBusy || !CanInteract(player)) return false;
        var hotbar = player.GetComponent<InventoryHotbarUI>();
        var inventory = player.GetComponent<Inventory>();
        int slot = hotbar != null ? hotbar.SelectedIndex : -1;
        if (!CanInsert(inventory, slot, false, out message)) return false;
        if (eggVisual == null) { message = "Visual telur belum tersedia."; return false; }
        placementBusy = true;
        placingPlayer = player;
        heldItems = player.GetComponent<HeldItemPlacementSystem>();
        playerHands = player.GetComponent<PlayerHeldTools>();
        placementClock = TimeManager.Instance;
        eggCommitted = false;
        player.AcquireMovementLock(this);
        placementClock?.AcquirePause(this);
        WorldInteractionPrompt.AcquireSuppression(this);
        heldItems?.SetExternalActionSuppressed(this, true);
        player.SetHoldingItemAnimation(false);
        Face(player);
        StartCoroutine(PlacementRoutine(inventory, slot));
        message = "";
        return true;
    }

    IEnumerator PlacementRoutine(Inventory inventory, int slot)
    {
        try
        {
            movingEgg = Instantiate(eggVisual, transform);
            movingEgg.name = "Egg_Placement_Hand";
            movingEgg.SetActive(true);
            foreach (var collider in movingEgg.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            // Dekati marker lewat CharacterController agar interaksi dari tepi radius tidak
            // memindahkan telur melintasi ruangan atau menembus perabot.
            float approachTime = 0f;
            Vector3 standPosition = interactionPoint.position;
            Vector3 towardNest = nest.position - standPosition;
            towardNest.y = 0f;
            standPosition += towardNest.normalized * standForwardOffset;
            while (placingPlayer != null && interactionPoint != null && approachTime < 2f)
            {
                Vector3 delta = standPosition - placingPlayer.transform.position;
                delta.y = 0f;
                if (delta.magnitude < .06f) break;
                placingPlayer.MoveForInteraction(this, Vector3.ClampMagnitude(delta, 1.2f * Time.deltaTime));
                approachTime += Time.deltaTime;
                yield return null;
            }
            if (placingPlayer == null || interactionPoint == null) yield break;
            placingPlayer.MoveForInteraction(this, Vector3.zero);
            Vector3 remainingApproach = standPosition - placingPlayer.transform.position;
            remainingApproach.y = 0f;
            if (remainingApproach.magnitude > .12f)
            {
                SaveLoadFeedback.Instance?.ShowMessage("Dekati bagian depan penetasan; jalannya terhalang.");
                yield break;
            }
            yield return AnimateCover(0f, 1f);
            if (placingPlayer == null) yield break;
            // Hand Over bekerja pada ketinggian meja; Place Item adalah pose jongkok ke lantai.
            placingPlayer.PlayHandOverAnimation();
            reachingNest = true;
            float contactDelay = Mathf.Max(.05f, placementContactDelay);
            float duration = Mathf.Max(contactDelay + .1f, placementDuration);
            float elapsed = 0f;
            bool contactPoseReady = false;
            // LateUpdate menempatkan tangan dan visual telur sesudah Animator mengevaluasi pose.
            // Commit menunggu LateUpdate terakhir agar telur tidak muncul sebelum tangan tiba.
            while (elapsed < duration)
            {
                if (placingPlayer == null || !placingPlayer.isActiveAndEnabled || home == null || !home.Available) yield break;
                if (!eggCommitted && elapsed >= contactDelay)
                {
                    if (!contactPoseReady)
                    {
                        reachWeight = 1f;
                        contactPoseReady = true;
                        yield return null;
                        continue;
                    }
                    contactDistance = lastPalmDistance;
                    if (contactDistance > .04f)
                    {
                        SaveLoadFeedback.Instance?.ShowMessage("Tangan belum dapat mencapai sarang; telur tetap di tas.");
                        yield break;
                    }
                    if (!TryInsertInternal(inventory, slot, out string message, null, true))
                    {
                        SaveLoadFeedback.Instance?.ShowMessage(message);
                        yield break;
                    }
                    eggCommitted = true;
                    if (movingEgg != null) movingEgg.SetActive(false);
                    SaveLoadFeedback.Instance?.ShowMessage(message);
                }
                reachWeight = elapsed <= contactDelay ? Mathf.SmoothStep(0f, 1f, elapsed / contactDelay) :
                    1f - Mathf.SmoothStep(0f, 1f, (elapsed - contactDelay) / (duration - contactDelay));
                elapsed += Time.deltaTime;
                yield return null;
            }
            reachingNest = false;
            yield return AnimateCover(1f, 0f);
        }
        finally { FinishPlacement(); }
    }

    void LateUpdate()
    {
        if (!placementBusy || placingPlayer == null || movingEgg == null || nest == null) return;
        Vector3 gripOffset = placingPlayer.transform.right * .05f + Vector3.up * .06f;
        Vector3 gripTarget = nest.position + gripOffset;
        Vector3 palm = playerHands != null ? playerHands.ReachRightPalm(gripTarget, reachingNest ? reachWeight : 0f) :
            placingPlayer.transform.position + placingPlayer.transform.forward * .35f + Vector3.up;
        lastPalmDistance = Vector3.Distance(palm, gripTarget);
        if (!eggCommitted) movingEgg.transform.SetPositionAndRotation(palm - gripOffset, nest.rotation);
        // Ukuran telur sama dengan visual sarang, meskipun rig tangan memakai skala impor berbeda.
        movingEgg.transform.localScale = eggVisual.transform.lossyScale;
        movingEgg.transform.localScale = new Vector3(
            movingEgg.transform.localScale.x / Mathf.Max(.0001f, transform.lossyScale.x),
            movingEgg.transform.localScale.y / Mathf.Max(.0001f, transform.lossyScale.y),
            movingEgg.transform.localScale.z / Mathf.Max(.0001f, transform.lossyScale.z));
    }

    void CacheCoverPose()
    {
        if (coverPoseCached || coverVisual == null) return;
        Transform cover = coverVisual.transform;
        coverClosedPosition = cover.localPosition;
        coverClosedRotation = cover.localRotation;
        var mesh = cover.GetComponent<MeshFilter>()?.sharedMesh;
        Bounds bounds = mesh != null ? mesh.bounds : new Bounds(Vector3.zero, Vector3.one * .4f);
        Vector3 hinge = new(bounds.center.x, bounds.min.y, bounds.max.z);
        coverPivot = coverClosedPosition + coverClosedRotation * Vector3.Scale(hinge, cover.localScale);
        coverPoseCached = true;
    }

    void SetCoverPose(float amount)
    {
        CacheCoverPose();
        if (coverVisual == null) return;
        Quaternion rotation = Quaternion.AngleAxis(coverOpenAngle * amount, Vector3.right);
        coverVisual.transform.localPosition = coverPivot + rotation * (coverClosedPosition - coverPivot);
        coverVisual.transform.localRotation = rotation * coverClosedRotation;
    }

    IEnumerator AnimateCover(float from, float to)
    {
        float elapsed = 0f;
        if (coverVisual != null) coverVisual.SetActive(true);
        while (elapsed < coverDuration)
        {
            SetCoverPose(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / coverDuration)));
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetCoverPose(to);
    }

    void CancelPlacement()
    {
        StopAllCoroutines();
        FinishPlacement();
    }

    void FinishPlacement()
    {
        if (!placementBusy) return;
        if (movingEgg != null) { movingEgg.SetActive(false); Destroy(movingEgg); }
        movingEgg = null;
        SetCoverPose(0f);
        placingPlayer?.ReleaseMovementLock(this);
        placementClock?.ReleasePause(this);
        WorldInteractionPrompt.ReleaseSuppression(this);
        heldItems?.SetExternalActionSuppressed(this, false);
        placingPlayer = null;
        heldItems = null;
        playerHands = null;
        placementClock = null;
        placementBusy = reachingNest = false;
        RefreshVisual();
    }
    void Face(PlayerController player)
    {
        var toward=nest.position-player.transform.position;toward.y=0;
        if(toward.sqrMagnitude>.001f)player.transform.rotation=Quaternion.LookRotation(toward);
    }
    public static bool IsChickenEgg(ItemSO item)=>item!=null&&item==AnimalCareCatalog.Load()?.Product(AnimalType.Chicken);
    public bool TryInsert(Inventory inventory,int slot,out string message,ShopManager animalService=null)
        => TryInsertInternal(inventory,slot,out message,animalService,false);

    bool CanInsert(Inventory inventory, int slot, bool ownPlacement, out string message)
    {
        message="Tidak dapat menaruh telur.";
        if(transaction|| (placementBusy && !ownPlacement) || home==null||!home.Available||nest==null)return false;
        if(inventory!=null&&(inventory.GetComponent<PlayerAnimalCarry>()?.HasAnimal==true||
            inventory.GetComponent<PlayerGatheringTool>()?.IsCarrying==true||
            (inventory.GetComponent<PlayerController>()?.IsMovementLocked==true &&
             !(ownPlacement && placingPlayer != null && inventory.gameObject == placingPlayer.gameObject))))
        {message="Kosongkan tangan dan selesaikan aksi sebelumnya dulu.";return false;}
        if(State!=EggIncubatorState.Empty){message="Penetasan sudah terisi.";return false;}
        var stack=inventory!=null?inventory.GetSlot(slot):null;
        if(stack==null||stack.count<1||!IsChickenEgg(stack.item)){message="Pilih 1 telur ayam di hotbar.";return false;}
        if(!home.HasRoom(AnimalType.Chicken)){message="Kandang penuh — perlu 1 slot kosong.";return false;}
        return true;
    }

    bool TryInsertInternal(Inventory inventory,int slot,out string message,ShopManager animalService,bool ownPlacement)
    {
        if (!CanInsert(inventory, slot, ownPlacement, out message)) return false;
        var service=animalService!=null?animalService:ShopManager.AnimalService;
        if(service==null){message="Layanan hewan belum tersedia; telur tidak dikurangi.";return false;}
        transaction=true;AnimalGrowthSystem animal=null;bool committed=false;string id=Id;
        try
        {
            animal=service.CreateIncubatorEgg(home,id);
            if(animal==null){message="Anak ayam belum bisa disiapkan; telur tidak dikurangi.";return false;}
            Records[id]=new EggIncubatorSaveData{id=id,homeId=home.Id,animalId=animal.AnimalId,startedDay=CurrentDay,
                readyDay=CurrentDay+IncubationDays,state=EggIncubatorState.Incubating};
            // Satu commit pada kontak tangan: kegagalan tidak memakan telur atau membuat penghuni ganda.
            if(!inventory.RemoveFromSlot(slot,1))return false;
            committed=true;RefreshVisual();message=$"Telur diletakkan — menetas dalam {IncubationDays} hari.";return true;
        }
        finally
        {
            if(!committed&&animal!=null)
            {
                Records.Remove(id);animal.gameObject.SetActive(false);
                if(Application.isPlaying)Destroy(animal.gameObject);else DestroyImmediate(animal.gameObject);
            }
            transaction=false;
        }
    }
    public bool TryTake(PlayerAnimalCarry carry,out string message)
    {
        if(carry==null||carry.HasAnimal){message="Tangan harus kosong untuk mengangkat anak ayam.";return false;}
        return TransferToPickup(carry.TryPickup,out message);
    }
    /// <summary>Transactional bridge: a refused pickup never clears the nest or changes the chick's ID.</summary>
    public bool TransferToPickup(Func<AnimalGrowthSystem,bool> requestPickup,out string message)
    {
        message="Anak ayam belum siap diangkat.";AdvanceAll(CurrentDay);
        var record=GetRecord();var animal=FindAnimal(record);
        if(record?.state!=EggIncubatorState.ChickReady||animal==null||requestPickup==null)return false;
        // Pickup's existing care lock keeps the chick stable until hand contact.
        if(!requestPickup(animal))return false;
        animal.LeaveIncubator();Records.Remove(record.id);RefreshVisual();message="Mengangkat anak ayam.";return true;
    }
    public bool TryRelease(out AnimalGrowthSystem animal,out string message)
    {
        message="Anak ayam belum siap keluar.";AdvanceAll(CurrentDay);var record=GetRecord();animal=FindAnimal(record);
        if(record?.state!=EggIncubatorState.ChickReady||animal==null||home==null||releasePoint==null)return false;
        var routine=animal.GetComponent<AnimalRoutine>();
        animal.LeaveIncubator();Records.Remove(record.id);
        // It joins this coop's normal indoor animal slots, not a dropped inventory item.
        routine.RestoreHome(home.Id,true,false);animal.transform.SetPositionAndRotation(releasePoint.position,releasePoint.rotation);
        RefreshVisual();message="Anak ayam keluar dan menjadi penghuni kandang.";return true;
    }
    public static AnimalGrowthSystem FindAnimal(EggIncubatorSaveData data)
    {
        if(data==null)return null;
        foreach(var animal in AnimalGrowthSystem.ActiveAnimals)
            if(animal!=null&&animal.AnimalId==data.animalId&&animal.IncubatorId==data.id)return animal;
        return null;
    }
    public static void AdvanceAll(int day)
    {
        foreach(var record in Records.Values)
        {
            if(record.state!=EggIncubatorState.Incubating||day<record.readyDay)continue;
            var animal=FindAnimal(record);if(animal==null)continue;
            animal.HatchInIncubator(record.readyDay);record.state=EggIncubatorState.ChickReady;
        }
    }
    public static bool TryGetNest(AnimalGrowthSystem animal,out Transform point)
    {
        point=null;if(animal==null)return false;
        foreach(var view in Views)
            if(view!=null&&view.isActiveAndEnabled&&view.Id==animal.IncubatorId&&view.GetRecord()?.animalId==animal.AnimalId)
            {point=view.nest;return point!=null;}
        return false;
    }
    public void RefreshVisual()
    {
        if(eggVisual!=null)eggVisual.SetActive(home!=null&&State==EggIncubatorState.Incubating);
        if(coverVisual!=null && !placementBusy)coverVisual.SetActive(State!=EggIncubatorState.ChickReady);
    }
    public static List<EggIncubatorSaveData> CaptureAll()
    {
        AdvanceAll(CurrentDay);var result=new List<EggIncubatorSaveData>();
        foreach(var record in Records.Values)result.Add(record.Copy());return result;
    }
    public static void RestoreAll(List<EggIncubatorSaveData> records)
    {
        foreach (var view in Views.ToArray()) if (view != null) view.CancelPlacement();
        Records.Clear();transaction=false;
        if(records!=null)foreach(var record in records)
            if(record!=null&&!string.IsNullOrEmpty(record.id)&&!string.IsNullOrEmpty(record.animalId)&&
                (record.state==EggIncubatorState.Incubating||record.state==EggIncubatorState.ChickReady))Records[record.id]=record.Copy();
        foreach(var view in Views)if(view!=null)view.RefreshVisual();
    }
}
