using System.Collections.Generic;
using UnityEngine;

/// <summary>Controller scene interior Barn/Coop: layout per level, exit, pakan, dan slot hewan.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public sealed class BarnInteriorSceneController : MonoBehaviour
{
    public static BarnInteriorSceneController Instance { get; private set; }
    [SerializeField] List<GameObject> levelLayouts = new();
    [SerializeField] Transform exitDoor;
    [SerializeField] Transform[] animalSpots;
    [SerializeField, Min(1f)] float interactionRadius = 3.5f;
    [Header("Feeding Trough Layout")]
    [Tooltip("Root ini dapat dipindah manual di Hierarchy untuk mengatur posisi seluruh tempat pakan.")]
    [SerializeField] Transform feedingTroughRoot;
    [SerializeField, Min(1f)] float troughInteractionRadius = 2.6f;
    [Header("Editable Interior Camera")]
    [Tooltip("Rotasi marker ini menjadi arah kamera Barn/Coop saat Play.")]
    [SerializeField] Transform cameraAngle;
    [SerializeField, Min(1f)] float cameraDistance = 18f;
    readonly List<Transform> feedingSlots = new();
    readonly List<Renderer> feedFillRenderers = new();
    readonly HashSet<int> occupiedFeedSlots = new();
    GameObject feedPreview;
    Material feedPreviewMaterial;
    Mesh feedPreviewMesh;
    float nextFeedTime;
    readonly List<Collider> troughAccessColliders = new();
    readonly List<Renderer> troughAccessRenderers = new();
    Transform feedMakerPoint;
    FeedMaker feedMaker;
    EggIncubator eggIncubator;
    int activeLayoutLevel=1;
    public int LayoutLevel=>activeLayoutLevel;
    public FeedMaker InteriorFeedMaker=>feedMaker;
    /// <summary>Whether the visible prompt targets a trough rather than this controller's machine or exit.</summary>
    public bool IsFeedingPromptDisplayed=>WorldInteractionPrompt.TryGetDisplayedAnchor(this,out var anchor)&&feedingSlots.Contains(anchor);
    PlayerController player;
    TopDownCameraFollow cameraFollow;
    float previousCameraPitch;
    float previousCameraYaw;
    float previousCameraDistance;
    bool cameraOverridden;
    BarnInteriorLayout authoredLayout;
    readonly HashSet<Renderer> visibleAuthoredFills = new();

    void Awake()
    {
        Instance=this;
        if(GetComponent<BarnInteriorHUD>()==null)gameObject.AddComponent<BarnInteriorHUD>();
    }
    void OnDestroy()
    {
        if(feedPreviewMaterial!=null)Destroy(feedPreviewMaterial);
        if(feedPreviewMesh!=null)Destroy(feedPreviewMesh);
        if(cameraOverridden && cameraFollow!=null)
            cameraFollow.SetFraming(previousCameraPitch,previousCameraYaw,previousCameraDistance,true);
        if(Instance==this) Instance=null;
    }
    void Start()
    {
        ApplyInteriorCamera();
        RefreshLayout();
    }

    public void Configure(List<GameObject> layouts,Transform exit,Transform[] spots,Transform troughRoot=null,Transform cameraMarker=null)
    {
        levelLayouts=layouts??new List<GameObject>();
        exitDoor=exit;
        animalSpots=spots;
        feedingTroughRoot=troughRoot;
        cameraAngle=cameraMarker;
    }

#if UNITY_EDITOR
    /// <summary>Dipakai setup Editor setelah layout scene diubah menjadi prefab instance.</summary>
    public void EditorConfigureLayouts(List<GameObject> layouts)
    {
        levelLayouts=layouts??new List<GameObject>();
    }
#endif

    public void RefreshLayout()
    {
        AnimalHome home=BarnInterior.Current!=null ? BarnInterior.Current.home : null;
        int level=home?.site!=null ? Mathf.Clamp(home.site.CurrentLevel,1,4) : 1;
        ApplyLayout(home,level);
    }

    void ApplyLayout(AnimalHome home,int level)
    {
        activeLayoutLevel=Mathf.Clamp(level,1,4);
        for(int i=0;i<levelLayouts.Count;i++) if(levelLayouts[i]!=null)
            levelLayouts[i].SetActive(i==level-1);
        BindAuthoredLayout(level);
        LayoutAnimalSpots(home,level);
        LayoutFeedingTrough(home,level);
        ResolveFeedMakerPoint(level);
        ResolveDedicatedFeedMaker(home);
        eggIncubator=authoredLayout!=null?authoredLayout.GetComponentInChildren<EggIncubator>(true):null;
        eggIncubator?.Bind(home);
        DisableInteriorDebugLabels();
    }

    void BindAuthoredLayout(int level)
    {
        BarnInteriorLayout next=levelLayouts!=null && levelLayouts.Count>=level && levelLayouts[level-1]!=null
            ? levelLayouts[level-1].GetComponent<BarnInteriorLayout>() : null;
        if(next==authoredLayout) return;
        authoredLayout=next;
        feedingSlots.Clear();feedFillRenderers.Clear();occupiedFeedSlots.Clear();
        if(next==null) return;
        feedingTroughRoot=next.FeedingRoot;
        animalSpots=next.AnimalSpots;
        feedingSlots.AddRange(next.FeedingSlots);
        feedFillRenderers.AddRange(next.FeedFills);
    }

    void DisableInteriorDebugLabels()
    {
        foreach(var label in GetComponentsInChildren<WorldDebugStatusLabel>(true))label.gameObject.SetActive(false);
        foreach(var layout in levelLayouts)if(layout!=null)
            foreach(var label in layout.GetComponentsInChildren<WorldDebugStatusLabel>(true))label.gameObject.SetActive(false);
        if(feedingTroughRoot!=null)
            foreach(var label in feedingTroughRoot.GetComponentsInChildren<WorldDebugStatusLabel>(true))label.gameObject.SetActive(false);
    }

    void ResolveFeedMakerPoint(int level)
    {
        if(authoredLayout!=null) {feedMakerPoint=authoredLayout.FeedMakerPoint;return;}
        feedMakerPoint=null;
        if(levelLayouts==null || levelLayouts.Count<level || levelLayouts[level-1]==null) return;
        foreach(Transform candidate in levelLayouts[level-1].GetComponentsInChildren<Transform>(true))
            if(candidate.name=="FeedMaker_Position_ReplaceMe") {feedMakerPoint=candidate;break;}
    }

    void ResolveDedicatedFeedMaker(AnimalHome home)
    {
        feedMaker=null;
        if(feedMakerPoint==null || home==null) return;

        // Interior Barn/Coop memakai mesin yang menempel pada marker layout aktif.
        // Jangan mencari FeedMaker global: scene world tetap aktif dan pencarian global
        // dapat mengambil mesin Fish Pond atau kandang lain.
        feedMaker=feedMakerPoint.GetComponent<FeedMaker>();
        if(feedMaker==null) feedMaker=feedMakerPoint.gameObject.AddComponent<FeedMaker>();
        feedMaker.Configure($"feedmaker.animal-home.{home.Id}",
            Resources.Load<FeedMakerCatalog>("Catalogs/FeedMakerCatalog"));
        feedMaker.Connect(home,null);
        feedMaker.SetDedicatedOutput(false);
        feedMaker.SetExternalInteraction(true);
        feedMaker.SetWorldDebugVisible(false);
    }

    void LayoutFeedingTrough(AnimalHome home,int level)
    {
        EnsureFeedingRoot(level);
        int capacity=home!=null ? Mathf.Max(1,home.FeedingSlotCapacity) :
            authoredLayout!=null ? authoredLayout.FeedingSlots.Length : new[]{4,8,14,20}[level-1];
        RegisterAuthoredFeedingSlots();
        if(authoredLayout==null)
            while(feedingSlots.Count<capacity) CreateFeedingSlot(feedingSlots.Count);
        for(int i=0;i<feedingSlots.Count;i++)
        {
            bool active=i<capacity;
            feedingSlots[i].gameObject.SetActive(active);
        }
        CacheTroughAccessSurfaces();
        RefreshFeedVisual(home);
    }

    void RegisterAuthoredFeedingSlots()
    {
        if(feedingTroughRoot==null || feedingSlots.Count>0) return;
        for(int i=0;i<feedingTroughRoot.childCount;i++)
            RegisterFeedingSlot(feedingTroughRoot.GetChild(i));
    }

    void EnsureFeedingRoot(int level)
    {
        if(feedingTroughRoot!=null) return;
        Transform existing=transform.Find("FeedingTroughSlots_Editable");
        if(existing!=null) {feedingTroughRoot=existing;return;}
        GameObject root=new("FeedingTroughSlots_Editable");
        root.transform.SetParent(transform,false);
        float floorY=levelLayouts!=null && levelLayouts.Count>=level && levelLayouts[level-1]!=null
            ? levelLayouts[level-1].transform.localPosition.y : 0f;
        root.transform.localPosition=new Vector3(0f,floorY,0f);
        feedingTroughRoot=root.transform;
    }

    void CreateFeedingSlot(int index)
    {
        GameObject slot=new($"FeedSlot_{index+1:00}");
        slot.transform.SetParent(feedingTroughRoot,false);
        slot.transform.localPosition=new Vector3(index%2==0 ? -2.6f : 2.6f,0f,(index/2)*1.8f);
        RegisterFeedingSlot(slot.transform);
    }

    void RegisterFeedingSlot(Transform slot)
    {
        if(slot==null || feedingSlots.Contains(slot)) return;

        GameObject container=GameObject.CreatePrimitive(PrimitiveType.Cube);
        container.name="TroughContainer";
        container.transform.SetParent(slot.transform,false);
        container.transform.localPosition=new Vector3(0f,0.28f,0f);
        container.transform.localScale=new Vector3(1.8f,0.55f,1.2f);
        Collider collider=container.GetComponent<Collider>();
        if(collider!=null) collider.enabled=false;
        SetColor(container.GetComponent<Renderer>(),new Color(0.34f,0.2f,0.08f));

        GameObject fill=GameObject.CreatePrimitive(PrimitiveType.Cube);
        fill.name="FeedFill";
        fill.transform.SetParent(slot.transform,false);
        fill.transform.localPosition=new Vector3(0f,0.59f,0f);
        fill.transform.localScale=new Vector3(1.48f,0.12f,0.88f);
        Collider fillCollider=fill.GetComponent<Collider>();
        if(fillCollider!=null) fillCollider.enabled=false;
        Renderer fillRenderer=fill.GetComponent<Renderer>();
        SetColor(fillRenderer,new Color(0.95f,0.68f,0.14f));
        feedingSlots.Add(slot.transform);
        feedFillRenderers.Add(fillRenderer);
    }

    void ApplyInteriorCamera()
    {
        cameraFollow=Camera.main!=null ? Camera.main.GetComponent<TopDownCameraFollow>() : FindFirstObjectByType<TopDownCameraFollow>();
        if(cameraFollow==null) return;
        previousCameraPitch=cameraFollow.Pitch;
        previousCameraYaw=cameraFollow.Yaw;
        previousCameraDistance=cameraFollow.FollowDistance;
        Vector3 desired=cameraAngle!=null ? cameraAngle.eulerAngles : new Vector3(55f,0f,0f);
        cameraFollow.SetFraming(desired.x,desired.y,cameraDistance,true);
        cameraOverridden=true;
    }

    static void SetColor(Renderer renderer,Color color)
    {
        if(renderer==null) return;
        renderer.material=ToonWorldStyle.CreateMaterial(color);
    }

    void RefreshFeedVisual(AnimalHome home)
    {
        occupiedFeedSlots.Clear();
        if(home!=null)foreach(int index in home.ReconcileFeedingSlots(feedingSlots.Count))occupiedFeedSlots.Add(index);
        // A compartment can display several logical portions. Accumulate first so
        // a later empty portion cannot hide the renderer of an earlier full one.
        if(authoredLayout!=null)
        {
            visibleAuthoredFills.Clear();
            for(int i=0;i<feedFillRenderers.Count;i++)
                if(feedFillRenderers[i]!=null && occupiedFeedSlots.Contains(i) && feedingSlots[i].gameObject.activeSelf)
                    visibleAuthoredFills.Add(feedFillRenderers[i]);
            foreach(Renderer fill in feedFillRenderers)
                if(fill!=null) fill.gameObject.SetActive(visibleAuthoredFills.Contains(fill));
            return;
        }
        for(int i=0;i<feedFillRenderers.Count;i++)
            if(feedFillRenderers[i]!=null) feedFillRenderers[i].gameObject.SetActive(
                occupiedFeedSlots.Contains(i) && feedingSlots[i].gameObject.activeSelf);
    }

    Transform ClosestFeedingSlot(Vector3 position,out float distance)
    {
        Transform closest=null;
        distance=float.PositiveInfinity;
        for(int i=0;i<feedingSlots.Count;i++)
        {
            Transform slot=feedingSlots[i];
            if(slot==null || !slot.gameObject.activeInHierarchy) continue;
            float candidate=HorizontalDistanceToSlotSurface(slot,position);
            bool sameCompartment=authoredLayout!=null && closest!=null &&
                (closest.position-slot.position).sqrMagnitude<.0001f;
            bool preferEmpty=sameCompartment && occupiedFeedSlots.Contains(feedingSlots.IndexOf(closest)) &&
                !occupiedFeedSlots.Contains(i);
            if(candidate>=distance && !preferEmpty) continue;
            distance=candidate;
            closest=slot;
        }
        return closest;
    }

    static float HorizontalDistanceToSlotSurface(Transform slot,Vector3 position)
    {
        float best=float.PositiveInfinity;
        foreach(Renderer renderer in slot.GetComponentsInChildren<Renderer>(true))
        {
            if(renderer==null || !renderer.enabled) continue;
            Vector3 point=renderer.bounds.ClosestPoint(position);
            point.y=position.y;
            best=Mathf.Min(best,Vector3.Distance(position,point));
        }
        if(float.IsPositiveInfinity(best))
        {
            Vector3 delta=slot.position-position;
            delta.y=0f;
            best=delta.magnitude;
        }
        return best;
    }

    void CacheTroughAccessSurfaces()
    {
        troughAccessColliders.Clear();
        troughAccessRenderers.Clear();
        foreach(Transform slot in feedingSlots)
        {
            // Authored portion markers have no geometry. Measure the physical trough,
            // not its feed socket, so all four sides use the same reach rule.
            Transform surface=slot!=null ? (authoredLayout!=null && slot.parent!=null ? slot.parent.Find("TroughCollider") : slot.Find("TroughContainer")) : null;
            troughAccessColliders.Add(surface!=null ? surface.GetComponent<Collider>() : null);
            troughAccessRenderers.Add(surface!=null ? surface.GetComponent<Renderer>() : null);
        }
    }

    Transform ClosestTroughAccess(Vector3 position,out float distance)
    {
        Transform closest=null;
        distance=float.PositiveInfinity;
        for(int i=0;i<feedingSlots.Count;i++)
        {
            Transform slot=feedingSlots[i];
            if(slot==null || !slot.gameObject.activeInHierarchy) continue;
            Collider collider=i<troughAccessColliders.Count ? troughAccessColliders[i] : null;
            Renderer renderer=i<troughAccessRenderers.Count ? troughAccessRenderers[i] : null;
            bool hasBounds=collider!=null && collider.enabled || renderer!=null;
            Bounds bounds=collider!=null && collider.enabled ? collider.bounds : renderer!=null ? renderer.bounds : default;
            if(Mathf.Abs(position.y-(hasBounds ? bounds.center.y : slot.position.y))>2f) continue;
            Vector3 point=hasBounds ? bounds.ClosestPoint(new Vector3(position.x,bounds.center.y,position.z)) : slot.position;
            point.y=position.y;
            float candidate=Vector3.Distance(position,point);
            if(candidate>=distance) continue;
            closest=slot;
            distance=candidate;
        }
        return closest;
    }

    int FirstEmptyFeedingSlot()
    {
        // The authored array is front-to-back order, independent of player position.
        for(int i=0;i<feedingSlots.Count;i++)
            if(feedingSlots[i]!=null && feedingSlots[i].gameObject.activeInHierarchy && !occupiedFeedSlots.Contains(i)) return i;
        return -1;
    }

    /// <summary>Whether held feed belongs to the nearby trough interaction rather than the generic green placement preview.</summary>
    public bool IsFeedingInRange(Transform actor,ItemSO item)
    {
        AnimalHome home=BarnInterior.Current!=null ? BarnInterior.Current.home : null;
        if(!isActiveAndEnabled || actor==null || home==null || !home.Available || FeedTypes.Kind(item)==FeedKind.None) return false;
        return ClosestTroughAccess(actor.position,out float distance)!=null && distance<=troughInteractionRadius;
    }

    /// <summary>Transfers one unit into the slot aimed at by the player; inventory changes only on success.</summary>
    public bool TryDepositSelectedFeed(Inventory source,int inventorySlot)
    {
        AnimalHome home=BarnInterior.Current!=null ? BarnInterior.Current.home : null;
        PlayerController actor=source!=null ? source.GetComponent<PlayerController>() : null;
        if(!isActiveAndEnabled || home==null || !home.Available || actor==null || actor.IsMovementLocked ||
            WorldInteractionPrompt.IsSuppressed || GameplayPauseMenu.IsOpen ||
            ClosestTroughAccess(actor.transform.position,out float distance)==null || distance>troughInteractionRadius) return false;
        RefreshFeedVisual(home);
        ItemStack stack=source.GetSlot(inventorySlot);
        if(stack?.item!=null && stack.count>0 && !home.AcceptsFeedItem(stack.item))
        {
            GameplayInfoPanel.NotifyBesideTools(FeedTypes.WrongTypeMessage);
            return false;
        }
        int destination=AimedFeedingSlot(actor);
        if(home.FeedSpace<=0 || destination<0)
        {
            GameplayInfoPanel.NotifyBesideTools(home.FeedSpace<=0?"Tempat pakan sudah penuh":"Arahkan ke slot tempat pakan");
            return false;
        }
        if(occupiedFeedSlots.Contains(destination)){GameplayInfoPanel.NotifyBesideTools("Slot pakan sudah terisi");return false;}
        if(Time.unscaledTime<nextFeedTime)return false;
        if(stack?.item==null || stack.count<=0 || !home.AcceptsFeedItem(stack.item))
        {
            GameplayInfoPanel.Notify($"Pilih {FeedTypes.Label(home.RequiredFeedKind)} dari hotbar.");
            return false;
        }
        string itemName=stack.DisplayName;
        if(home.DepositFromSlot(source,inventorySlot,1)<=0)
        {
            GameplayInfoPanel.Notify("Pakan gagal dimasukkan.");
            return false;
        }
        occupiedFeedSlots.Add(destination);
        home.RecordFilledFeedingSlot(destination);
        WorldInteractionPrompt.MarkHeldActionPerformed(this);
        nextFeedTime=Time.unscaledTime+.25f;
        RefreshFeedVisual(home);
        Transform target=feedingSlots[destination];
        actor.PlayHandOverAnimation();
        FarmActionEffects.Handover(actor,target,target.position+Vector3.up*.35f);
        GameplayInfoPanel.Notify($"{itemName} x1 dimasukkan");
        return true;
    }

    void DepositSelectedFeed()
    {
        if(player==null) return;
        InventoryHotbarUI hotbar=player.GetComponent<InventoryHotbarUI>();
        if(hotbar!=null) TryDepositSelectedFeed(player.GetComponent<Inventory>(),hotbar.SelectedIndex);
    }

    void LayoutAnimalSpots(AnimalHome home,int level)
    {
        int capacity=home!=null ? Mathf.Max(1,home.Capacity) :
            authoredLayout!=null ? authoredLayout.AnimalSpots.Length : new[]{4,8,14,20}[level-1];
        if(animalSpots==null) return;
        for(int i=0;i<animalSpots.Length;i++)
        {
            if(animalSpots[i]==null) continue;
            animalSpots[i].gameObject.SetActive(i<capacity);
        }
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if(cameraAngle!=null)
        {
            Gizmos.color=new Color(0.2f,0.75f,1f,0.95f);
            Gizmos.DrawRay(cameraAngle.position,cameraAngle.forward*4f);
        }
        if(feedingTroughRoot==null) return;
        Gizmos.color=new Color(1f,0.65f,0.12f,0.9f);
        for(int i=0;i<feedingTroughRoot.childCount;i++)
            Gizmos.DrawWireCube(feedingTroughRoot.GetChild(i).position+Vector3.up*0.3f,new Vector3(1.8f,0.6f,1.2f));
    }
#endif

    public Vector3 AnimalPosition(AnimalRoutine animal)
    {
        AnimalHome home=BarnInterior.Current!=null ? BarnInterior.Current.home : null;
        int index=home!=null ? home.Residents.IndexOf(animal) : -1;
        if(animalSpots!=null && index>=0 && index<animalSpots.Length && animalSpots[index]!=null)
            return animalSpots[index].position;
        return transform.position+new Vector3(0f,0.2f,2f);
    }

    public Quaternion AnimalFacing(AnimalRoutine animal)
    {
        Vector3 position=AnimalPosition(animal);
        Transform trough=ClosestFeedingSlot(position,out _);
        if(trough==null) return animal.transform.rotation;
        Vector3 direction=trough.position-position;
        direction.y=0f;
        return direction.sqrMagnitude>0.001f ? Quaternion.LookRotation(direction) : animal.transform.rotation;
    }

    public Vector3 AnimalSlotPosition(int index)
    {
        if(animalSpots!=null && index>=0 && index<animalSpots.Length && animalSpots[index]!=null)
            return animalSpots[index].position;
        return transform.position+new Vector3(0f,.2f,2f);
    }

    void Update()
    {
        if(feedPreview!=null)feedPreview.SetActive(false);
        if(player==null) player=FindFirstObjectByType<PlayerController>();
        if(player==null || BarnInterior.Current==null || BarnInterior.Current.home==null) return;
        AnimalHome home=BarnInterior.Current.home;
        Inventory playerInventory=player.GetComponent<Inventory>();
        InventoryHotbarUI hotbar=player.GetComponent<InventoryHotbarUI>();
        if(feedMaker==null) ResolveDedicatedFeedMaker(home);
        RefreshFeedVisual(home);
        Transform trough=ClosestTroughAccess(player.transform.position,out float troughDistance);
        bool carryingFeed=FeedTypes.Kind(hotbar!=null?hotbar.SelectedItem:null)!=FeedKind.None;
        Vector3 makerOffset=feedMakerPoint!=null?feedMakerPoint.position-player.transform.position:Vector3.positiveInfinity;
        makerOffset.y=0;
        float competingDistance=Mathf.Min(troughDistance,makerOffset.magnitude);
        if(eggIncubator!=null)competingDistance=Mathf.Min(competingDistance,eggIncubator.DistanceTo(player.transform));
        // Compact rooms can put a trough inside the door's interaction radius.
        // The nearest visible target owns the prompt, so leaving remains possible.
        if(HandleExitInteraction(competingDistance))return;
        if(eggIncubator!=null&&eggIncubator.InRange(player.transform)&&eggIncubator.DistanceTo(player.transform)<=troughDistance&&
            (feedMakerPoint==null||eggIncubator.DistanceTo(player.transform)<=Vector3.Distance(feedMakerPoint.position,player.transform.position))&&eggIncubator.HandleInteraction(player))return;
        if(feedMakerPoint!=null)
        {
            Vector3 makerDelta=feedMakerPoint.position-player.transform.position;
            makerDelta.y=0f;
            float makerDistance=makerDelta.magnitude;
            if(makerDistance<=interactionRadius && makerDistance<=troughDistance && !(carryingFeed&&troughDistance<=troughInteractionRadius))
            {
                string ready=feedMaker!=null ? feedMaker.Ready(home.RequiredFeedKind).ToString() : "-";
                WorldInteractionPrompt.Request(this,feedMakerPoint,
                    $"F: Ambil 1 {FeedTypes.Label(home.RequiredFeedKind)} (Siap {ready}) | E: Kelola Feed Maker",makerDistance,1.2f);
                if(PlayerInteractionTarget.PressPrompt(this, feedMakerPoint, KeyCode.F))
                {
                    if(feedMaker==null || !feedMaker.CollectAnimated(playerInventory,false,feedMakerPoint))
                        SaveLoadFeedback.Instance?.ShowMessage("Animal Feed belum ready / Inventory penuh.");
                }
                else if(PlayerInteractionTarget.PressPrompt(this, feedMakerPoint, KeyCode.E))
                    feedMaker?.OpenFor(playerInventory);
                return;
            }
        }
        if(trough!=null && troughDistance<=troughInteractionRadius)
        {
            ItemStack heldStack=hotbar!=null ? hotbar.SelectedStack : null;
            bool holdingFeed=heldStack?.item!=null && heldStack.count>0 && home.AcceptsFeedItem(heldStack.item);
            string action=home.FeedSpace<=0 || FirstEmptyFeedingSlot()<0
                    ? "F: Isi Pakan — Tempat pakan sudah penuh"
                    : holdingFeed
                        ? $"F: Masukkan 1 {heldStack.DisplayName}"
                        : $"F: Isi Pakan — pilih {FeedTypes.Label(home.RequiredFeedKind)}";
            int aimed=carryingFeed?AimedFeedingSlot(player):-1;
            Transform promptTarget=aimed>=0?feedingSlots[aimed]:trough;
            if(carryingFeed&&!player.IsMovementLocked&&!GameplayInput.BlocksMovement&&!WorldInteractionPrompt.IsSuppressed)
                ShowFeedPreview(aimed,home,heldStack?.item);
            if(carryingFeed)action=home.FeedSpace<=0?"F: Tempat pakan sudah penuh":
                !holdingFeed?"F: Jenis pakan tidak sesuai":
                aimed<0?"F: Arahkan ke slot tempat pakan":
                occupiedFeedSlots.Contains(aimed)?"F: Slot pakan sudah terisi":
                "F: Taruh 1 pakan | Tahan F + berjalan: isi berurutan";
            WorldInteractionPrompt.Request(this,promptTarget,
                $"{action}\nTempat Pakan: {home.TotalFeed}/{home.FeedingSlotCapacity}",
                troughDistance,1.15f,DepositSelectedFeed);
            bool pressed=PlayerInteractionTarget.PressPrompt(this,promptTarget,KeyCode.F);
            bool held=GameplayInput.GetKey(KeyCode.F)||WorldInteractionPrompt.IsHeldFor(this);
            HandleFeedingInput(pressed,held,promptTarget);
            return;
        }
        HandleExitInteraction(float.PositiveInfinity);
    }

    void HandleFeedingInput(bool pressed,bool held,Transform promptTarget)
    {
        if(pressed){DepositSelectedFeed();return;}
        if(!held||player==null||player.CurrentSpeed<=.05f||GameplayInput.BlocksMovement||
            !WorldInteractionPrompt.IsDisplayedFor(this,promptTarget))return;
        AnimalHome home=BarnInterior.Current!=null?BarnInterior.Current.home:null;
        var stack=player.GetComponent<InventoryHotbarUI>()?.SelectedStack;
        int aimed=AimedFeedingSlot(player);
        if(home!=null&&stack?.item!=null&&stack.count>0&&home.AcceptsFeedItem(stack.item)&&
            aimed>=0&&!occupiedFeedSlots.Contains(aimed))DepositSelectedFeed();
    }

    int AimedFeedingSlot(PlayerController actor)
    {
        if(actor==null)return -1;
        Vector3 facing=actor.FacingDirection;facing.y=0;
        if(facing.sqrMagnitude<.01f)facing=actor.transform.forward;
        facing.Normalize();Vector3 aim=actor.transform.position+facing*.75f;
        int best=-1;float score=float.PositiveInfinity;
        for(int i=0;i<feedingSlots.Count;i++)
        {
            var slot=feedingSlots[i];if(slot==null||!slot.gameObject.activeInHierarchy)continue;
            Vector3 delta=slot.position-actor.transform.position;delta.y=0;
            if(Mathf.Abs(slot.position.y-actor.transform.position.y)>2f||delta.magnitude>troughInteractionRadius||
                Vector3.Dot(delta.normalized,facing)<.15f)continue;
            Vector3 toAim=slot.position-aim;toAim.y=0;
            if(toAim.sqrMagnitude<score){score=toAim.sqrMagnitude;best=i;}
        }
        return best;
    }
    void ShowFeedPreview(int slot,AnimalHome home,ItemSO item)
    {
        if(feedPreview==null)
        {
            feedPreview=new GameObject("Feed Slot Target",typeof(MeshFilter),typeof(MeshRenderer));feedPreview.transform.SetParent(transform,false);
            feedPreviewMesh=new Mesh{name="Feed target square"};feedPreviewMesh.vertices=new[]{new Vector3(-.38f,0,-.38f),new Vector3(.38f,0,-.38f),new Vector3(.38f,0,.38f),new Vector3(-.38f,0,.38f)};feedPreviewMesh.triangles=new[]{0,2,1,0,3,2};feedPreviewMesh.RecalculateNormals();
            feedPreview.GetComponent<MeshFilter>().sharedMesh=feedPreviewMesh;
            feedPreviewMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="Feed slot translucent target",renderQueue=3000};
            feedPreviewMaterial.SetFloat("_Surface",1);feedPreviewMaterial.SetFloat("_ZWrite",0);feedPreviewMaterial.SetFloat("_SrcBlend",5);feedPreviewMaterial.SetFloat("_DstBlend",10);feedPreviewMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            var renderer=feedPreview.GetComponent<MeshRenderer>();renderer.sharedMaterial=feedPreviewMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        bool valid=slot>=0&&home.AcceptsFeedItem(item)&&home.FeedSpace>0&&!occupiedFeedSlots.Contains(slot);
        feedPreviewMaterial.SetColor("_BaseColor",valid?new Color(.2f,.95f,.3f,.34f):new Color(1,.15f,.12f,.34f));
        Vector3 point=slot>=0?feedingSlots[slot].position:player.transform.position+player.FacingDirection*.75f;
        feedPreview.transform.position=point+Vector3.up*.035f;feedPreview.SetActive(true);
    }
    void OnDisable(){if(feedPreview!=null)feedPreview.SetActive(false);}

    bool HandleExitInteraction(float competingDistance)
    {
        if(exitDoor==null||player==null||BarnInterior.Current==null||
            !PlayerInteractionTarget.ContainsPickup(player.transform,exitDoor,interactionRadius))return false;
        Vector3 offset=exitDoor.position-player.transform.position;offset.y=0;
        if(offset.magnitude>competingDistance)return false;
        float distance=Vector3.Distance(player.transform.position,exitDoor.position);
        WorldInteractionPrompt.Request(this,exitDoor,"E: Keluar Kandang | I: Kelola Hewan",distance,1.5f,LeaveFromPrompt);
        if(PlayerInteractionTarget.PressPrompt(this, exitDoor, KeyCode.E)) BarnInterior.Current.Leave();
        else if(PlayerInteractionTarget.PressPrompt(this, exitDoor, KeyCode.I))
            AnimalCarePanel.Show(null,BarnInterior.Current.home,player.GetComponent<Inventory>());
        return true;
    }

    void LeaveFromPrompt()
    {
        if(player!=null&&!player.IsMovementLocked&&BarnInterior.Current!=null&&exitDoor!=null&&
            PlayerInteractionTarget.ContainsPickup(player.transform,exitDoor,interactionRadius))BarnInterior.Current.Leave();
    }
}
