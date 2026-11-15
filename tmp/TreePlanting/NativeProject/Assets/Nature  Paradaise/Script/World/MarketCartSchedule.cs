using System.Collections.Generic;
using UnityEngine;

/// <summary>Moves the persistent market (and keeper) between its stall and an off-camera exit.
/// Stock belongs to MarketStand and stays alive even when the artwork/colliders are hidden.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(MarketStand))]
[RequireComponent(typeof(MarketCartDriver))]
public sealed class MarketCartSchedule : MonoBehaviour
{
    public enum TravelState { Hidden, Arriving, Open, Departing, Fading }
    [SerializeField, Min(.1f)] float walkingSpeed = 1.8f;
    [SerializeField, Min(30f)] float turnSpeed = 135f;
    [Tooltip("Fallback world direction when no gameplay camera is available. Routes otherwise use screen-up.")]
    [SerializeField] Vector3 exitDirection = Vector3.forward;
    [Tooltip("Fade gracefully after this distance from the stall even if the player keeps following.")]
    [SerializeField, Min(12f)] float departureFadeDistance = 18f;
    [SerializeField, Range(12f, 100f)] float maximumRouteLength = 60f;
    [Tooltip("Clearance for the whole cart, including the handles and keeper.")]
    [SerializeField] Vector3 travelHalfExtents = new(1.05f, .8f, 1.65f);

    public TravelState State { get; private set; }
    public bool IsParked => initialized && State == TravelState.Open && debugMode != DebugMode.Departure && debugMode != DebugMode.Hidden;
    public Vector3 HomePosition => homePosition;
    public bool IsPresented => presented;
    public bool IsDebugTesting => debugMode != DebugMode.None;
    public float FadeVisibility => fadeVisibility;
    public string DebugStatus => debugMode == DebugMode.Departure && State == TravelState.Open ? "Tes tutup · menunggu jalur keluar kosong" :
        IsDebugTesting ? "Tes gerobak aktif · " + Status : "Jadwal normal · " + Status;
    public string Status => State == TravelState.Arriving ? "Gerobak sedang datang" :
        State == TravelState.Departing ? "Gerobak sedang pulang" : State == TravelState.Fading ? "Gerobak sedang memudar" : State == TravelState.Open ? "Pasar buka" : "Pasar tutup";

    MarketStand stand;
    Vector3 homePosition, destination;
    Quaternion homeRotation;
    Renderer[] visuals;
    Collider[] shapes;
    NPCCharacterMotion[] keepers;
    bool[] enabledShapes, enabledKeepers, originalRenderingOff;
    readonly RaycastHit[] castHits = new RaycastHit[128];
    readonly Collider[] overlaps = new Collider[128];
    readonly Plane[] planes = new Plane[6];
    readonly List<Terrain> terrains = new();
    bool initialized, presented, restorePending;
    int createdFrame;
    float nextRouteTry;
    Camera worldCamera;
    MarketCartDriver driver;
    int departurePhase;
    float phaseTime;
    enum DebugMode { None, Parked, Departure, Hidden }
    DebugMode debugMode;
    readonly MarketCartFade fade=new();
    float fadeTime, fadeDuration=.65f, fadeVisibility=1;
    bool fadeWhileMoving;
    readonly List<PlayerController> obstructingPlayers=new();
    readonly Collider[] playerClearanceOverlaps=new Collider[64];
    readonly RaycastHit[] playerClearanceHits=new RaycastHit[64];
    float playerBlockedTime;
    public float TravelSpeed => walkingSpeed;
    public Vector3 UpperExitDirection
    {
        get
        {
            if(!worldCamera||!worldCamera.isActiveAndEnabled)worldCamera=Camera.main;
            Vector3 up=worldCamera?Flat(worldCamera.transform.up):Flat(exitDirection);
            return up.sqrMagnitude>.01f?up.normalized:Vector3.forward;
        }
    }

    void Awake()
    {
        stand = GetComponent<MarketStand>();
        driver = GetComponent<MarketCartDriver>() ?? gameObject.AddComponent<MarketCartDriver>();
        homePosition = transform.position;
        homeRotation = transform.rotation;
        visuals = GetComponentsInChildren<Renderer>(true);
        originalRenderingOff = new bool[visuals.Length];
        for (int i = 0; i < visuals.Length; i++) originalRenderingOff[i] = visuals[i].forceRenderingOff;
        shapes = GetComponentsInChildren<Collider>(true);
        enabledShapes = new bool[shapes.Length];
        for (int i = 0; i < shapes.Length; i++) enabledShapes[i] = shapes[i].enabled;
        keepers = GetComponentsInChildren<NPCCharacterMotion>(true);
        enabledKeepers = new bool[keepers.Length];
        for (int i = 0; i < keepers.Length; i++) enabledKeepers[i] = keepers[i].enabled;
        terrains.AddRange(Terrain.activeTerrains);
        createdFrame = Time.frameCount;
        SetPresented(false);
    }

    void Update()
    {
        // A save restores under LoadingScreen. No visible journey/teleport is replayed from a save.
        if (SaveManager.Instance != null && SaveManager.Instance.IsLoading)
        {
            RestorePresentation();
            return;
        }
        if (restorePending)
        {
            restorePending = false;
            ResetAtHome(stand.IsScheduledOpen);
        }
        if (!initialized)
        {
            // Let all clock, weather and gameplay-camera Start/restore callbacks finish first.
            if (Time.frameCount <= createdFrame + 2) return;
            initialized = true;
            ResetAtHome(stand.IsScheduledOpen);
        }
        bool open = debugMode == DebugMode.None ? stand.IsScheduledOpen : debugMode == DebugMode.Parked;
        if (!open) stand.ClosePanel();
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused) return;
        if(State==TravelState.Fading){MoveAlongRoute(Mathf.Min(Time.deltaTime,.1f));return;}

        if (open)
        {
            if (State == TravelState.Hidden && Time.unscaledTime >= nextRouteTry) BeginArrival();
            else if (State == TravelState.Departing) { destination = homePosition; State = TravelState.Arriving; driver?.Play("Cart_Push"); }
        }
        else if (State == TravelState.Open || State == TravelState.Arriving)
        {
            if (Time.unscaledTime >= nextRouteTry) BeginDeparture();
        }

        if (State != TravelState.Arriving && State != TravelState.Departing) return;
        MoveAlongRoute(Mathf.Min(Time.deltaTime, .1f));
    }

    public void RestorePresentation()
    {
        debugMode=DebugMode.None;CancelFade();
        stand.ClosePanel();
        restorePending = true;
        SetPresented(false);
        transform.SetPositionAndRotation(homePosition, homeRotation);
        State = TravelState.Hidden;
        nextRouteTry = 0;
        departurePhase=0;driver?.ResetPresentation();
    }

    void ResetAtHome(bool open)
    {
        CancelFade();
        transform.SetPositionAndRotation(homePosition, homeRotation);
        initialized = true;
        State = open ? TravelState.Open : TravelState.Hidden;
        nextRouteTry = 0;
        SetPresented(open);
        departurePhase=0;driver?.ResetPresentation();
    }

    void BeginArrival()
    {
        nextRouteTry = Time.unscaledTime + 2;
        if (!TryFindExit(homePosition, out var exit)) return;
        // Candidate bounds must be completely outside the current gameplay camera.
        if (VisibleAt(exit)) return;
        transform.SetPositionAndRotation(exit, Quaternion.LookRotation(Flat(homePosition - exit)));
        destination = homePosition;
        State = TravelState.Arriving;
        SetPresented(true);
        driver?.Play("Cart_Push");
    }

    void BeginDeparture()
    {
        nextRouteTry = Time.unscaledTime + 2;
        if (!VisibleAt(transform.position)) { HideOffscreen(); return; }
        if (!PlanExit(transform.position, out var exit, true)) return;
        destination = exit;
        State = TravelState.Departing;
        if(departurePhase==0){departurePhase=1;phaseTime=0;driver?.Play("Cart_Grip");}
    }

    void MoveAlongRoute(float dt)
    {
        if(State!=TravelState.Arriving&&State!=TravelState.Departing&&State!=TravelState.Fading)return;
        if(State==TravelState.Fading)
        {
            if(fadeWhileMoving)MoveForwardForFade(dt);
            fadeTime+=dt;fadeVisibility=1-Mathf.SmoothStep(0,1,fadeTime/fadeDuration);fade.Set(fadeVisibility);if(fadeTime>=fadeDuration)HideOffscreen();return;
        }
        if (State == TravelState.Departing && ShouldFinishDeparture()) { BeginFade(1f,true); return; }
        if(State==TravelState.Departing && departurePhase<3)
        {
            phaseTime+=dt;
            if(departurePhase==1)
            { if(phaseTime>=1.05f){departurePhase=2;phaseTime=0;driver?.Play("Cart_Pull");} return; }
            Vector3 nextPull=Grounded(transform.position-transform.forward*.43f*dt);
            if(ClearMovingSegment(transform.position,nextPull,dt))transform.position=nextPull;
            // A blocked rear side skips the short adjustment; no pushing through an actor/building.
            if(phaseTime>=1.0f){departurePhase=3;driver?.Play("Cart_Push");}
            return;
        }
        Vector3 delta = Flat(destination - transform.position);
        if (delta.sqrMagnitude < .0025f)
        {
            if (State == TravelState.Departing)
            {
                if (ShouldFinishDeparture()) BeginFade(1f,true);
                else if (Time.unscaledTime >= nextRouteTry) BeginDeparture(); // Player followed/zoomed out.
            }
            else
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, homeRotation, turnSpeed * dt);
                if (Quaternion.Angle(transform.rotation, homeRotation) < .5f) ResetAtHome(true);
            }
            return;
        }
        Vector3 direction = delta.normalized;
        Quaternion heading = Quaternion.LookRotation(direction);
        // Turn before rolling; the keeper always travels behind the cart, facing its movement.
        transform.rotation = Quaternion.RotateTowards(transform.rotation, heading, turnSpeed * dt);
        if (Quaternion.Angle(transform.rotation, heading) > 8) return;
        float step = Mathf.Min(delta.magnitude, walkingSpeed * dt);
        Vector3 next = Grounded(transform.position + direction * step);
        if (!ClearMovingSegment(transform.position, next, dt)) return;
        transform.position = next;
        if (State == TravelState.Departing && ShouldFinishDeparture()) BeginFade(1f,true);
    }

    bool ShouldFinishDeparture() => !VisibleAt(transform.position) ||
        departurePhase>=3 && Flat(transform.position-homePosition).magnitude>=departureFadeDistance;
    void MoveForwardForFade(float dt)
    {
        Vector3 next=Grounded(transform.position+transform.forward*walkingSpeed*dt);
        if(GroundAvailable(next)&&ClearMovingSegment(transform.position,next,dt))transform.position=next;
    }

    void BeginFade(float duration, bool moving=false)
    {
        stand.ClosePanel();fadeTime=0;fadeDuration=Mathf.Max(.1f,duration);fadeVisibility=1;
        fadeWhileMoving=moving;
        fade.Begin(visuals);State=TravelState.Fading;
        foreach(var shape in shapes)if(shape)shape.enabled=false;
    }
    void CancelFade(){fade.Restore();fadeVisibility=1;fadeTime=0;fadeWhileMoving=false;playerBlockedTime=0;}
    void OnDestroy()=>fade.Restore();

    // Session-only visual tests. No clock/weather edits, sales, save writes or stored overrides.
    public void DebugShowAtHome()
    {if(!PropertySite.DebugShortcutsEnabled)return;stand.ClosePanel();debugMode=DebugMode.Parked;ResetAtHome(true);}
    public void DebugDepart()
    {if(!PropertySite.DebugShortcutsEnabled)return;DebugShowAtHome();debugMode=DebugMode.Departure;BeginDeparture();}
    public void DebugArrive()
    {if(!PropertySite.DebugShortcutsEnabled)return;stand.ClosePanel();debugMode=DebugMode.Parked;HideOffscreen();nextRouteTry=0;BeginArrival();}
    public void DebugPreviewFade()
    {if(!PropertySite.DebugShortcutsEnabled)return;DebugShowAtHome();debugMode=DebugMode.Hidden;BeginFade(1f);}
    public void DebugHide()
    {if(!PropertySite.DebugShortcutsEnabled)return;stand.ClosePanel();debugMode=DebugMode.Hidden;HideOffscreen();}
    public void DebugUseSchedule()
    {if(!PropertySite.DebugShortcutsEnabled)return;debugMode=DebugMode.None;stand.ClosePanel();ResetAtHome(stand.IsScheduledOpen);}

    void HideOffscreen()
    {
        SetPresented(false);
        CancelFade();
        State = TravelState.Hidden;
        // No movement of the authored home/save identity, no destroyed stock or keeper instance.
        transform.SetPositionAndRotation(homePosition, homeRotation);
        departurePhase=0;driver?.ResetPresentation();
    }

    void SetPresented(bool visible)
    {
        presented = visible;
        for (int i = 0; i < visuals.Length; i++)
            if (visuals[i]) visuals[i].forceRenderingOff = !visible || originalRenderingOff[i];
        for (int i = 0; i < shapes.Length; i++)
            if (shapes[i]) shapes[i].enabled = visible && enabledShapes[i];
        for (int i = 0; i < keepers.Length; i++)
            if (keepers[i]) keepers[i].enabled = visible && enabledKeepers[i];
    }

    bool TryFindExit(Vector3 origin, out Vector3 exit)
        => PlanExit(origin,out exit,false);

    bool PlanExit(Vector3 origin, out Vector3 exit, bool departure)
    {
        exit = origin;
        Vector3 preferred = UpperExitDirection;
        // Straight, obstacle-checked routes avoid a dependency on a baked NavMesh.
        // Retry if a new building blocks every exit instead of teleporting through it.
        // Keep every fallback in the upper half of the screen; never enter from below.
        for (int i = 0; i < 9; i++)
        {
            int turn = i == 0 ? 0 : ((i + 1) / 2) * (i % 2 == 1 ? 1 : -1);
            Vector3 direction = Quaternion.Euler(0, turn * 15f, 0) * preferred;
            Vector3 previous = origin;
            for (float distance = 2; distance <= maximumRouteLength; distance += 2)
            {
                Vector3 candidate = Grounded(origin + direction * distance);
                if (!GroundAvailable(candidate) || Mathf.Abs(candidate.y - previous.y) > .6f ||
                    !SegmentClear(previous, candidate, false)) break;
                previous = candidate;
                if (distance >= 12 && (!VisibleAt(candidate)||departure&&Flat(candidate-homePosition).magnitude>=departureFadeDistance)) { exit = candidate; return true; }
            }
        }
        return false;
    }

    bool SegmentClear(Vector3 from, Vector3 to, bool includeActors)
    {
        Vector3 direction = Flat(to - from);
        if (direction.sqrMagnitude < .000001f) return true;
        // Runtime sweeps retain the real body/keeper orientation while pulling backwards.
        // Route planning instead previews the cart aligned to the proposed heading.
        Quaternion rotation = includeActors ? transform.rotation : Quaternion.LookRotation(direction);
        Vector3 offset = Vector3.up * 1.2f - (includeActors ? transform.forward : direction.normalized) * .3f;
        // A sweep alone misses colliders that already overlap its initial volume.
        int overlapsCount = Physics.OverlapBoxNonAlloc(from + offset, travelHalfExtents, overlaps,
            rotation, ~0, QueryTriggerInteraction.Ignore);
        if (overlapsCount == overlaps.Length) return false;
        for (int i = 0; i < overlapsCount; i++)
            if (BlocksRoute(overlaps[i], from, to, includeActors)) return false;
        int count = Physics.BoxCastNonAlloc(from + offset, travelHalfExtents, direction.normalized,
            castHits, rotation, direction.magnitude + .05f, ~0, QueryTriggerInteraction.Ignore);
        if (count == castHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            if (BlocksRoute(castHits[i].collider, from, to, includeActors)) return false;
        }
        return true;
    }

    bool ClearMovingSegment(Vector3 from,Vector3 to,float dt)
    {
        Vector3 direction=Flat(to-from).normalized;
        if(direction.sqrMagnitude<.01f)return true;
        Quaternion heading=transform.rotation;
        Vector3 center=from+Vector3.up*1.2f-transform.forward*.3f;
        obstructingPlayers.Clear();
        int n=Physics.OverlapBoxNonAlloc(center,travelHalfExtents,overlaps,heading,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<n;i++)CollectObstructingPlayer(overlaps[i]);
        n=Physics.BoxCastNonAlloc(center,travelHalfExtents,direction,castHits,heading,Vector3.Distance(from,to)+.05f,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<n;i++)CollectObstructingPlayer(castHits[i].collider);
        var right=Vector3.Cross(Vector3.up,direction).normalized;
        foreach(var player in obstructingPlayers)
        {
            var capsule=player.GetComponent<CharacterController>();if(!capsule||!capsule.enabled)continue;
            float radius=capsule.radius*Mathf.Max(Mathf.Abs(player.transform.lossyScale.x),Mathf.Abs(player.transform.lossyScale.z));
            float lateral=Vector3.Dot(player.transform.position-center,right);
            float sign=lateral>=0?1:-1;
            for(int side=0;side<2;side++)
            {
                float chosen=side==0?sign:-sign;
                float amount=travelHalfExtents.x+radius+.18f-chosen*lateral;
                Vector3 push=right*chosen*Mathf.Max(.12f,amount);
                if(!SafePlayerSidestep(player,capsule,push))continue;
                player.MoveAsideForWorldObject(Vector3.ClampMagnitude(push,4.5f*Mathf.Min(dt,.1f)));
                break;
            }
        }
        if(obstructingPlayers.Count>0)Physics.SyncTransforms();
        bool clear=SegmentClear(from,to,true);
        if(clear||obstructingPlayers.Count==0)playerBlockedTime=0;
        else playerBlockedTime+=dt;
        // A player boxed in by walls cannot be moved safely. Fade the cart rather than
        // teleporting the player, clipping through a building or waiting indefinitely.
        if(!clear&&playerBlockedTime>=1.5f&&State!=TravelState.Fading&&SegmentClear(from,to,false))BeginFade(1f);
        return clear;
    }
    void CollectObstructingPlayer(Collider shape)
    {
        if(!shape||shape.transform.IsChildOf(transform))return;
        var player=shape.GetComponentInParent<PlayerController>();
        if(player&&!obstructingPlayers.Contains(player))obstructingPlayers.Add(player);
    }
    bool SafePlayerSidestep(PlayerController player,CharacterController capsule,Vector3 push)
    {
        Vector3 target=player.transform.position+push;
        if(!GroundAvailable(target)||Mathf.Abs(Grounded(target).y-player.transform.position.y)>.25f)return false;
        var scale=player.transform.lossyScale;
        float radius=capsule.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z))+.025f;
        float height=Mathf.Max(capsule.height*Mathf.Abs(scale.y),radius*2);
        Vector3 center=capsule.transform.TransformPoint(capsule.center),offset=Vector3.up*(height*.5f-radius);
        int n=Physics.CapsuleCastNonAlloc(center-offset,center+offset,radius,push.normalized,playerClearanceHits,push.magnitude,~0,QueryTriggerInteraction.Ignore);
        if(n==playerClearanceHits.Length)return false;
        for(int i=0;i<n;i++)if(BlocksPlayerSidestep(playerClearanceHits[i].collider,player))return false;
        n=Physics.OverlapCapsuleNonAlloc(center+push-offset,center+push+offset,radius,playerClearanceOverlaps,~0,QueryTriggerInteraction.Ignore);
        if(n==playerClearanceOverlaps.Length)return false;
        for(int i=0;i<n;i++)if(BlocksPlayerSidestep(playerClearanceOverlaps[i],player))return false;
        return true;
    }
    bool BlocksPlayerSidestep(Collider shape,PlayerController player)
    {
        if(!shape||shape.transform.IsChildOf(player.transform)||shape.transform.IsChildOf(transform)||shape is TerrainCollider)return false;
        return shape.bounds.max.y>player.transform.position.y+.12f;
    }

    bool BlocksRoute(Collider c, Vector3 from, Vector3 to, bool includeActors)
    {
        if (!c || c.transform.IsChildOf(transform) || c is TerrainCollider) return false;
        // Flat terrain helper colliders don't obstruct a cart; solid raised obstacles do.
        if (c.bounds.max.y < Mathf.Min(from.y, to.y) + .22f) return false;
        return includeActors || !IsActor(c);
    }

    static bool IsActor(Collider c) => c.GetComponentInParent<PlayerController>() != null ||
        c.GetComponentInParent<NPCCharacterMotion>() != null || c.GetComponentInParent<PersonalAnimal>() != null ||
        c.GetComponentInParent<AnimalGrowthSystem>() != null;

    Vector3 Grounded(Vector3 point)
    {
        foreach (var terrain in terrains)
        {
            if (!terrain || !terrain.terrainData) continue;
            Vector3 local = point - terrain.transform.position, size = terrain.terrainData.size;
            if (local.x < 0 || local.z < 0 || local.x > size.x || local.z > size.z) continue;
            point.y = terrain.SampleHeight(point) + terrain.transform.position.y;
            return point;
        }
        point.y = homePosition.y;
        return point;
    }

    bool GroundAvailable(Vector3 point)
    {
        if (terrains.Count == 0) return true; // Isolated/non-terrain authored scenes.
        foreach (var terrain in terrains)
        {
            if (!terrain || !terrain.terrainData) continue;
            Vector3 local = point - terrain.transform.position, size = terrain.terrainData.size;
            if (local.x < 2 || local.z < 2 || local.x > size.x - 2 || local.z > size.z - 2) continue;
            return terrain.terrainData.GetSteepness(local.x / size.x, local.z / size.z) < 15;
        }
        return false;
    }

    bool VisibleAt(Vector3 point)
    {
        if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsInsideInterior) return false;
        if (!worldCamera || !worldCamera.isActiveAndEnabled) worldCamera = Camera.main;
        if (!worldCamera || !worldCamera.isActiveAndEnabled) return false;
        GeometryUtility.CalculateFrustumPlanes(worldCamera, planes);
        // Includes the handles, keeper and a margin beyond the edge: never just test the pivot.
        return GeometryUtility.TestPlanesAABB(planes, new Bounds(point + Vector3.up, new Vector3(4.5f, 2.8f, 4.5f)));
    }

    static Vector3 Flat(Vector3 value) { value.y = 0; return value; }
}
