using UnityEngine;

/// <summary>Portal authored di Map menuju scene interior Barn atau Coop sesuai jenis AnimalHome.</summary>
public sealed class BarnInterior : MonoBehaviour
{
    public static BarnInterior Current { get; private set; }
    public AnimalHome home;
    [Header("Manual Exterior Setup")]
    [Tooltip("Pindahkan marker ini manual ke depan pintu Barn.")]
    public Transform exteriorDoor;
    [Header("Separate Interior Scene")]
    [SerializeField] string interiorSceneName = "BarnInterior";
    [SerializeField] string entrySpawnId = "barn-interior-entry";
    [SerializeField, Min(1f)] float interactionRadius = 3.5f;
    [SerializeField, Min(.5f), Tooltip("Jangkauan pintu prefab sesuai skala player sekarang, dalam meter.")]
    float authoredEntranceRadius = 1.3f;
    [Tooltip("Collider badan Barn. Gunakan Edit Collider untuk mengatur Center dan Size secara manual.")]
    [SerializeField] BoxCollider exteriorBarrier;
    [Tooltip("Jika aktif, ukuran collider dihitung ulang dari model ketika Play.")]
    [SerializeField] bool autoFitBarrierToModel;
    [Tooltip("Jika aktif, marker pintu dipindahkan ulang berdasarkan model ketika Play.")]
    [SerializeField] bool autoPlaceEntranceFromModel;
    [SerializeField, Min(0.1f)] float entranceClearance = 0.65f;

    [HideInInspector] public Transform roomRoot;
    [HideInInspector] public Transform entry;
    [HideInInspector] public Transform exitDoor;
    [HideInInspector] public GameObject[] barnLevelVisuals;
    [HideInInspector] public GameObject[] fallbackVisuals;
    [HideInInspector] public Transform[] animalSpots;

    Vector3 returnPosition;
    PlayerController player;
    Transform runtimeExteriorDoor;
    public bool Visiting => Current == this;
    /// <summary>Keep authored bell/door zones separate even when placement scales the prefab.</summary>
    public float EntranceInteractionRadius
    {
        get
        {
            if(runtimeExteriorDoor==null)return interactionRadius;
            float radius=Mathf.Min(interactionRadius,authoredEntranceRadius);
            var bell=home!=null?home.BellStation:null;
            if(bell!=null&&bell.InteractionPoint!=null)
            {
                Vector3 delta=bell.InteractionPoint.position-runtimeExteriorDoor.position;delta.y=0;
                radius=Mathf.Min(radius,delta.magnitude*.42f);
            }
            return radius;
        }
    }

    void Awake()
    {
        if (exteriorBarrier == null) exteriorBarrier = GetComponent<BoxCollider>();
        if (exteriorBarrier != null) exteriorBarrier.enabled = false;
    }

    public static bool TryGetReturnPosition(out Vector3 position)
    {
        position=Current!=null ? Current.returnPosition : Vector3.zero;
        return Current!=null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Current=null; }
    public static void ResetVisit() { Current=null; }

    public void ConfigureScene(AnimalHome targetHome,Transform exterior,BoxCollider barrier=null)
    {
        home=targetHome;
        exteriorDoor=exterior;
        exteriorBarrier=barrier!=null ? barrier : GetComponent<BoxCollider>();
        if (exteriorBarrier != null) exteriorBarrier.enabled = false;
        ResolveInteriorDestination();
        if(roomRoot!=null) roomRoot.gameObject.SetActive(false);
        DisableLegacyRoomChildren();
    }

    public bool Enter()
    {
        if(Current!=null || home==null || !home.Available || player==null || player.IsMovementLocked ||
           SceneTransitionManager.Instance==null) return false;
        ResolveInteriorDestination();
        returnPosition=player.transform.position;
        Current=this;
        if(SceneTransitionManager.Instance.EnterInterior(interiorSceneName,entrySpawnId)) return true;
        Current=null;
        return false;
    }

    void ResolveInteriorDestination()
    {
        bool coop=home!=null && home.Kind==AnimalHousingKind.Coop;
        interiorSceneName=coop ? "CoopInterior" : "BarnInterior";
        entrySpawnId=coop ? "coop-interior-entry" : "barn-interior-entry";
    }

    public void Leave()
    {
        if(Current!=this || SceneTransitionManager.Instance==null) return;
        if(SceneTransitionManager.Instance.ReturnToWorld(string.Empty)) Current=null;
    }

    public Vector3 AnimalPosition(AnimalRoutine animal)
    {
        if(BarnInteriorSceneController.Instance!=null)
            return BarnInteriorSceneController.Instance.AnimalPosition(animal);
        return home!=null ? home.Entry : transform.position;
    }

    void Update()
    {
        if(roomRoot!=null && roomRoot.gameObject.activeSelf) roomRoot.gameObject.SetActive(false);
        DisableLegacyRoomChildren();
        if(player==null) player=FindFirstObjectByType<PlayerController>();
        RefreshExteriorAccess();
        if(player==null || home==null || !home.Available || Visiting ||
           (SceneTransitionManager.Instance!=null && SceneTransitionManager.Instance.IsInsideInterior)) return;
        Transform target=runtimeExteriorDoor!=null ? runtimeExteriorDoor : exteriorDoor!=null ? exteriorDoor : transform;
        float radius = EntranceInteractionRadius;
        if(!PlayerInteractionTarget.ContainsPickup(player.transform,target,radius)) return;
        float distance=Vector3.Distance(player.transform.position,target.position);
        WorldInteractionPrompt.Request(this,target,Application.isMobilePlatform?$"Ketuk: Masuk {home.Label}":$"E: Masuk {home.Label}",Mathf.Max(0f,distance-0.25f),1.5f,EnterFromPrompt);
        if(PlayerInteractionTarget.PressPrompt(this,KeyCode.E)) Enter();
    }

    void EnterFromPrompt()
    {
        Transform target=runtimeExteriorDoor!=null?runtimeExteriorDoor:exteriorDoor!=null?exteriorDoor:transform;
        float radius=EntranceInteractionRadius;
        if(player!=null&&PlayerInteractionTarget.ContainsPickup(player.transform,target,radius)&&Enter())GameplayInput.ConsumeCurrentFrame();
    }

    void DisableLegacyRoomChildren()
    {
        for(int index=0;index<transform.childCount;index++)
        {
            Transform child=transform.GetChild(index);
            if(child.name=="Room_Editable" && child.gameObject.activeSelf)
                child.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Menempelkan marker interaksi pada pintu model Barn aktif dan menyelaraskan collider
    /// blocker dengan bounds bangunan. Tidak membuat object runtime baru.
    /// </summary>
    void RefreshExteriorAccess()
    {
        if(home==null || home.site==null)
        {
            if(exteriorBarrier!=null) exteriorBarrier.enabled=false;
            runtimeExteriorDoor=null;
            return;
        }

        Transform anchor=home.site.BuildingAnchor;
        transform.SetPositionAndRotation(anchor.position,anchor.rotation);
        transform.localScale=Vector3.one;
        if(exteriorBarrier==null) exteriorBarrier=GetComponent<BoxCollider>();
        if(exteriorBarrier!=null)
        {
            exteriorBarrier.isTrigger=false;
            // Volume portal lama bukan collision bangunan. Jangan mengaktifkannya saat
            // visual belum ada, sedang diganti levelnya, atau sudah memiliki collider prefab.
            exteriorBarrier.enabled=false;
        }

        Transform visualRoot=home.site.CompletedVisualRoot;
        if(visualRoot==null)
        {
            runtimeExteriorDoor=null;
            return;
        }

        BarnExteriorAuthoring authored=visualRoot.GetComponentInChildren<BarnExteriorAuthoring>(true);
        if(authored!=null)
        {
            runtimeExteriorDoor=authored.Entrance;
            // The combined model has no individual Door renderer. Both the
            // animal route and the portal use its explicit entrance marker.
            home.door=runtimeExteriorDoor;
            // Collision prefab adalah sumber tunggal agar collider portal tidak menumpuk.
            if(exteriorBarrier!=null) exteriorBarrier.enabled=false;
            return;
        }

        runtimeExteriorDoor=null;
        bool hasPrefabCollision = false;
        foreach (var collision in visualRoot.GetComponentsInChildren<Collider>(false))
            if (collision.enabled && !collision.isTrigger) { hasPrefabCollision = true; break; }
        if(!autoFitBarrierToModel && !autoPlaceEntranceFromModel) return;

        Renderer[] renderers=visualRoot.GetComponentsInChildren<Renderer>(false);
        if(renderers.Length==0)
        {
            if(exteriorBarrier!=null) exteriorBarrier.enabled=false;
            return;
        }

        GetLocalBounds(anchor,renderers,out Vector3 localMin,out Vector3 localMax);

        if(autoFitBarrierToModel && exteriorBarrier!=null && !hasPrefabCollision)
        {
            exteriorBarrier.center=(localMin+localMax)*0.5f;
            exteriorBarrier.size=localMax-localMin;
            exteriorBarrier.enabled=home.Available;
        }

        if(!autoPlaceEntranceFromModel || exteriorDoor==null) return;
        Vector3 localDoor=FindPreferredDoorLocal(visualRoot,anchor,home.site.CurrentLevel,(localMin+localMax)*0.5f);
        Vector3 center=(localMin+localMax)*0.5f;
        Vector3 fromCenter=localDoor-center;
        if(Mathf.Abs(fromCenter.x)>Mathf.Abs(fromCenter.z))
            localDoor.x=fromCenter.x>=0f ? localMax.x+entranceClearance : localMin.x-entranceClearance;
        else
            localDoor.z=fromCenter.z>=0f ? localMax.z+entranceClearance : localMin.z-entranceClearance;

        Vector3 worldDoor=anchor.TransformPoint(localDoor);
        if(Physics.Raycast(worldDoor+Vector3.up*6f,Vector3.down,out RaycastHit hit,16f,~0,QueryTriggerInteraction.Ignore))
            worldDoor.y=hit.point.y;
        else worldDoor.y=anchor.position.y;
        exteriorDoor.SetPositionAndRotation(worldDoor,anchor.rotation);
    }

    public void ConfigureManualExterior(bool autoFitBarrier,bool autoPlaceEntrance)
    {
        autoFitBarrierToModel=autoFitBarrier;
        autoPlaceEntranceFromModel=autoPlaceEntrance;
    }

    void OnDrawGizmosSelected()
    {
        if(exteriorDoor==null) return;
        Gizmos.color=new Color(0.15f,1f,0.3f,0.95f);
        Gizmos.DrawSphere(exteriorDoor.position+Vector3.up*0.15f,0.3f);
        Gizmos.DrawLine(exteriorDoor.position,exteriorDoor.position+Vector3.up*2f);
    }

    static Vector3 FindPreferredDoorLocal(Transform visualRoot,Transform anchor,int level,Vector3 fallback)
    {
        string[] preferred=level switch
        {
            1=>new[]{"Door_01"},
            2=>new[]{"Door_01","Door_02"},
            3=>new[]{"Door_02"},
            4=>new[]{"Door_03","Door_04"},
            _=>System.Array.Empty<string>()
        };
        Vector3 total=Vector3.zero;
        int count=0;
        foreach(Collider candidate in visualRoot.GetComponentsInChildren<Collider>(false))
        {
            string candidateName=candidate.name;
            if(candidateName.IndexOf("Door",System.StringComparison.OrdinalIgnoreCase)<0 ||
               candidateName.IndexOf("LOD",System.StringComparison.OrdinalIgnoreCase)>=0) continue;
            bool accepted=preferred.Length==0;
            foreach(string token in preferred)
                if(candidateName.IndexOf(token,System.StringComparison.OrdinalIgnoreCase)>=0) {accepted=true;break;}
            if(!accepted) continue;
            total+=anchor.InverseTransformPoint(candidate.bounds.center);
            count++;
        }
        return count>0 ? total/count : fallback+Vector3.back;
    }

    static void GetLocalBounds(Transform anchor,Renderer[] renderers,out Vector3 minimum,out Vector3 maximum)
    {
        minimum=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);
        maximum=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);
        foreach(Renderer renderer in renderers)
        {
            Bounds bounds=renderer.localBounds;
            for(int x=-1;x<=1;x+=2)
            for(int y=-1;y<=1;y+=2)
            for(int z=-1;z<=1;z+=2)
            {
                Vector3 rendererLocal=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
                Vector3 local=anchor.InverseTransformPoint(renderer.transform.TransformPoint(rendererLocal));
                minimum=Vector3.Min(minimum,local);
                maximum=Vector3.Max(maximum,local);
            }
        }
    }
}
