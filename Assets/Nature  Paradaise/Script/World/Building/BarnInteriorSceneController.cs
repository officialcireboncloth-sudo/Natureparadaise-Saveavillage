using System.Collections.Generic;
using UnityEngine;

/// <summary>Controller scene interior Barn/Coop: layout per level, exit, pakan, dan slot hewan.</summary>
[DisallowMultipleComponent]
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
    [SerializeField, Min(1f)] float cameraDistance = 25f;
    readonly List<Transform> feedingSlots = new();
    readonly List<Renderer> feedFillRenderers = new();
    readonly HashSet<int> occupiedFeedSlots = new();
    Transform feedMakerPoint;
    FeedMaker feedMaker;
    WorldDebugStatusLabel troughDebugLabel;
    PlayerController player;
    TopDownCameraFollow cameraFollow;
    float previousCameraPitch;
    float previousCameraYaw;
    float previousCameraDistance;
    bool cameraOverridden;

    void Awake() => Instance=this;
    void OnDestroy()
    {
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
        for(int i=0;i<levelLayouts.Count;i++) if(levelLayouts[i]!=null)
            levelLayouts[i].SetActive(i==level-1);
        LayoutAnimalSpots(home,level);
        LayoutFeedingTrough(home,level);
        ResolveFeedMakerPoint(level);
        ResolveDedicatedFeedMaker(home);
        EnsureDebugLabels();
    }

    void EnsureDebugLabels()
    {
        if(feedingTroughRoot!=null)
            troughDebugLabel=WorldDebugStatusLabel.GetOrCreate(feedingTroughRoot,"Trough_DebugLabel",
                new Vector3(-1.2f,2.1f,0f));
        troughDebugLabel?.SetColor(new Color(0.25f,0.9f,1f,1f));
    }

    void ResolveFeedMakerPoint(int level)
    {
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
        feedMaker.SetWorldDebugVisible(true);
    }

    void LayoutFeedingTrough(AnimalHome home,int level)
    {
        EnsureFeedingRoot(level);
        int capacity=home!=null ? Mathf.Max(1,home.FeedingSlotCapacity) : new[]{4,8,14,20}[level-1];
        RegisterAuthoredFeedingSlots();
        while(feedingSlots.Count<capacity) CreateFeedingSlot(feedingSlots.Count);
        for(int i=0;i<feedingSlots.Count;i++)
        {
            bool active=i<capacity;
            feedingSlots[i].gameObject.SetActive(active);
        }
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
        Shader shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if(shader!=null) renderer.material=new Material(shader){color=color};
    }

    void RefreshFeedVisual(AnimalHome home)
    {
        int filled=home!=null ? home.TotalFeed : 0;
        while(occupiedFeedSlots.Count>filled)
        {
            int remove=-1;
            foreach(int index in occupiedFeedSlots) if(index>remove) remove=index;
            if(remove<0) break;
            occupiedFeedSlots.Remove(remove);
        }
        for(int i=0;occupiedFeedSlots.Count<filled && i<feedingSlots.Count;i++)
            if(feedingSlots[i]!=null && feedingSlots[i].gameObject.activeSelf) occupiedFeedSlots.Add(i);
        for(int i=0;i<feedFillRenderers.Count;i++)
            if(feedFillRenderers[i]!=null) feedFillRenderers[i].gameObject.SetActive(
                occupiedFeedSlots.Contains(i) && feedingSlots[i].gameObject.activeSelf);
    }

    Transform ClosestFeedingSlot(Vector3 position,out float distance)
    {
        Transform closest=null;
        distance=float.PositiveInfinity;
        foreach(Transform slot in feedingSlots)
        {
            if(slot==null || !slot.gameObject.activeInHierarchy) continue;
            Vector3 delta=slot.position-position;
            delta.y=0f;
            float candidate=delta.magnitude;
            if(candidate>=distance) continue;
            distance=candidate;
            closest=slot;
        }
        return closest;
    }

    void LayoutAnimalSpots(AnimalHome home,int level)
    {
        int capacity=home!=null ? Mathf.Max(1,home.Capacity) : new[]{4,8,14,20}[level-1];
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

    void Update()
    {
        if(player==null) player=FindFirstObjectByType<PlayerController>();
        if(player==null || BarnInterior.Current==null) return;
        AnimalHome home=BarnInterior.Current.home;
        Inventory playerInventory=player.GetComponent<Inventory>();
        InventoryHotbarUI hotbar=player.GetComponent<InventoryHotbarUI>();
        if(feedMaker==null) ResolveDedicatedFeedMaker(home);
        if(troughDebugLabel==null) EnsureDebugLabels();
        troughDebugLabel?.SetText($"PAKAN {home.Label}: {home.TotalFeed}/{home.FeedingSlotCapacity}\n"+
            $"Feed {home.Fodder} | Grass {home.Grass}\nReset 00:00 ({GameTimeDebugText.UntilMidnight()})");
        if(feedMakerPoint!=null)
        {
            Vector3 makerDelta=feedMakerPoint.position-player.transform.position;
            makerDelta.y=0f;
            float makerDistance=makerDelta.magnitude;
            if(makerDistance<=interactionRadius)
            {
                string ready=feedMaker!=null ? feedMaker.AnimalFeedOutput.ToString() : "-";
                WorldInteractionPrompt.Request(this,feedMakerPoint,
                    $"F: Ambil 1 Animal Feed ke Inventory (Ready {ready}) | E: Kelola Feed Maker",makerDistance,1.2f);
                if(PlayerInteractionTarget.PressPickup(player.transform,feedMakerPoint,KeyCode.F,interactionRadius))
                {
                    bool collected=feedMaker!=null && feedMaker.Collect(playerInventory);
                    SaveLoadFeedback.Instance?.ShowMessage(collected
                        ? "Animal Feed x1 masuk ke Inventory."
                        : "Animal Feed belum ready / Inventory penuh.");
                }
                else if(PlayerInteractionTarget.PressPickup(player.transform,feedMakerPoint,KeyCode.E,interactionRadius))
                    feedMaker?.OpenFor(playerInventory);
                return;
            }
        }
        RefreshFeedVisual(home);
        Transform trough=ClosestFeedingSlot(player.transform.position,out float troughDistance);
        if(trough!=null && troughDistance<=troughInteractionRadius)
        {
            int troughIndex=feedingSlots.IndexOf(trough);
            bool occupied=troughIndex>=0 && occupiedFeedSlots.Contains(troughIndex);
            ItemStack heldStack=hotbar!=null ? hotbar.SelectedStack : null;
            bool holdingFeed=heldStack?.item!=null && heldStack.count>0 && home.AcceptsFeedItem(heldStack.item);
            string action=occupied
                ? $"Box {troughIndex+1}: TERISI"
                : home.FeedSpace<=0
                    ? "Tempat pakan penuh"
                    : holdingFeed
                        ? $"F: Masukkan 1 {heldStack.DisplayName} ke Box {troughIndex+1}"
                        : $"Box {troughIndex+1}: KOSONG — pilih Animal Feed/Grass di hotbar";
            WorldInteractionPrompt.Request(this,trough,
                $"{action}\nTempat Pakan {home.Label}: {home.TotalFeed}/{home.FeedingSlotCapacity} " +
                $"(Feed {home.Fodder} | Grass {home.Grass}) | Belum makan {home.RequiredFeedToday}",
                troughDistance,1.15f);
            if(PlayerInteractionTarget.PressPickup(player.transform,trough,KeyCode.F,troughInteractionRadius))
            {
                if(occupied)
                    SaveLoadFeedback.Instance?.ShowMessage($"Box {troughIndex+1} sudah terisi.");
                else if(!holdingFeed)
                    SaveLoadFeedback.Instance?.ShowMessage("Pilih Animal Feed atau Grass dari hotbar terlebih dahulu.");
                else
                {
                    int moved=home.DepositFromSlot(playerInventory,hotbar.SelectedIndex,1);
                    if(moved>0)
                    {
                        occupiedFeedSlots.Add(troughIndex);
                        PlayerPickupNotification.Show(player.transform,$"{heldStack.DisplayName} x1 → Box {troughIndex+1}");
                        SaveLoadFeedback.Instance?.ShowMessage($"Box {troughIndex+1} diisi 1 {heldStack.DisplayName}.");
                        RefreshFeedVisual(home);
                    }
                    else SaveLoadFeedback.Instance?.ShowMessage(home.FeedSpace<=0
                        ? "Tempat pakan penuh."
                        : "Pakan gagal dimasukkan.");
                }
            }
            return;
        }
        if(exitDoor==null) return;
        if(!PlayerInteractionTarget.ContainsPickup(player.transform,exitDoor,interactionRadius)) return;
        float distance=Vector3.Distance(player.transform.position,exitDoor.position);
        WorldInteractionPrompt.Request(this,exitDoor,"E: Keluar Kandang | I: Kelola Hewan",distance,1.5f);
        if(PlayerInteractionTarget.PressPickup(player.transform,exitDoor,KeyCode.E,interactionRadius)) BarnInterior.Current.Leave();
        else if(PlayerInteractionTarget.PressPickup(player.transform,exitDoor,KeyCode.I,interactionRadius))
            AnimalCarePanel.Show(null,BarnInterior.Current.home,player.GetComponent<Inventory>());
    }
}
