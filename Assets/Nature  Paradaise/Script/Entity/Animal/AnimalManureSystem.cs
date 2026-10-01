using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public sealed class AnimalManureDropData
{
    public string id, animalId, homeId, sceneName;
    public bool poultry, anchored;
    public int indoorSlot;
    public Vector3 position;
}
[Serializable]
public sealed class AnimalManureTimerData
{
    public string animalId;
    public double nextHour;
}
[Serializable]
public sealed class AnimalManureSaveData
{
    public List<AnimalManureDropData> drops = new();
    public List<AnimalManureTimerData> timers = new();
}

/// <summary>Persistent piles and per-animal game-calendar timers, including unvisited barns.</summary>
[DefaultExecutionOrder(-250)]
public sealed class AnimalManureSystem : MonoBehaviour
{
    public static AnimalManureSystem Instance { get; private set; }
    readonly List<AnimalManureDropData> drops = new();
    readonly Dictionary<string,double> timers = new();
    readonly Dictionary<string,GameObject> visuals = new();
    AnimalManureSettings settings;
    Inventory playerInventory;
    PlayerController collectingPlayer;
    bool collecting;
    float nextRefresh;
    public IReadOnlyList<AnimalManureDropData> Drops => drops;
    double Now => (TimeManager.Instance.day-1)*24d+TimeManager.Instance.CurrentTimeHours;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic() => Instance=null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap() => Ensure();
    static AnimalManureSystem Ensure()
    {
        if(Instance==null) new GameObject("AnimalManureSystem_Runtime").AddComponent<AnimalManureSystem>();
        return Instance;
    }
    void Awake()
    {
        if(Instance!=null && Instance!=this){Destroy(gameObject);return;}
        Instance=this;DontDestroyOnLoad(gameObject);
        settings=Resources.Load<AnimalManureSettings>("AnimalManureSettings");
    }
    void OnDisable()
    {
        StopAllCoroutines();collectingPlayer?.ReleaseMovementLock(this);
        collectingPlayer=null;collecting=false;
    }
    void OnDestroy(){if(Instance==this)Instance=null;}
    public double NextDropHour(string animalId)=>timers.TryGetValue(animalId,out var hour)?hour:0;
    double NextInterval() => UnityEngine.Random.Range(settings!=null?settings.minimumHours:3f,
        settings!=null?Mathf.Max(settings.minimumHours,settings.maximumHours):6f);
    void Update()
    {
        var clock=TimeManager.Instance;
        if(clock==null || clock.IsPaused || Time.timeScale<=0) return;
        if(Time.unscaledTime>=nextRefresh)
        {
            nextRefresh=Time.unscaledTime+.5f;
            ProduceDueDrops();RefreshVisuals();
        }
        if(collecting) return;
        if(playerInventory==null)playerInventory=FindFirstObjectByType<Inventory>();
        if(playerInventory==null) return;
        var player=playerInventory.GetComponent<PlayerController>();
        if(player==null || player.IsMovementLocked) return;
        AnimalManureDropData nearest=null;GameObject target=null;float distance=float.PositiveInfinity;
        foreach(var drop in drops)
        {
            if(!visuals.TryGetValue(drop.id,out var visual)||visual==null||!visual.activeSelf)continue;
            float d=Vector3.Distance(playerInventory.transform.position,visual.transform.position);
            if(d<distance){distance=d;nearest=drop;target=visual;}
        }
        float radius=settings!=null?settings.pickupRadius:2.4f;
        if(target==null || distance>radius) return;
        bool hasFork=HasPitchfork(playerInventory);
        WorldInteractionPrompt.Request(this,target.transform,hasFork?"[E] Ambil Kotoran":"Pilih Garpu untuk mengambil kotoran",distance,.65f);
        if(hasFork && PlayerInteractionTarget.PressPickup(playerInventory.transform,target.transform,KeyCode.E,radius))
            TryCollect(nearest.id,playerInventory);
    }
    public void ProduceDueDrops()
    {
        if(TimeManager.Instance==null)return;
        double now=Now;
        foreach(var animal in AnimalGrowthSystem.ActiveAnimals)
        {
            if(animal==null || !animal.HasBeenBorn || string.IsNullOrEmpty(animal.AnimalId))continue;
            if(!timers.TryGetValue(animal.AnimalId,out double next))
            {timers[animal.AnimalId]=now+NextInterval();continue;}
            if(now<next)continue;
            // Sleeping or loading never catches up by spawning many piles in one frame.
            timers[animal.AnimalId]=now+NextInterval();
            int count=0;foreach(var drop in drops)if(drop.animalId==animal.AnimalId)count++;
            if(count<(settings!=null?settings.maximumPilesPerAnimal:6))SpawnDrop(animal);
        }
    }
    public AnimalManureDropData SpawnDrop(AnimalGrowthSystem animal)
    {
        if(animal==null || !animal.HasBeenBorn)return null;
        var routine=animal.GetComponent<AnimalRoutine>();
        bool indoors=routine!=null && routine.IsHoused && routine.Home!=null;
        var jitter=UnityEngine.Random.insideUnitCircle*.6f;
        var drop=new AnimalManureDropData{ id=Guid.NewGuid().ToString("N"),animalId=animal.AnimalId,
            poultry=AnimalGrowthProfileSO.IsBird(animal.Type),sceneName=animal.gameObject.scene.name,
            homeId=indoors?routine.HomeId:null,indoorSlot=indoors?Mathf.Max(0,routine.Home.Residents.IndexOf(routine)):0,
            position=indoors?new Vector3(jitter.x,0,jitter.y):animal.transform.position-animal.transform.forward*.8f+new Vector3(jitter.x,0,jitter.y)};
        if(!indoors && AnimalWalkingPath.Ground(drop.position,animal.transform,null,out var ground,true))drop.position=ground;
        drops.Add(drop);return drop;
    }
    public void RefreshVisuals()
    {
        var barn=BarnInterior.Current;
        var room=BarnInteriorSceneController.Instance;
        bool inside=SceneTransitionManager.Instance!=null && SceneTransitionManager.Instance.IsInsideInterior;
        foreach(var drop in drops)
        {
            bool visible=!string.IsNullOrEmpty(drop.homeId)
                ? barn!=null && barn.home!=null && barn.home.Id==drop.homeId && room!=null
                : !inside && SceneManager.GetSceneByName(drop.sceneName).isLoaded;
            if(!visible){if(visuals.TryGetValue(drop.id,out var hidden)&&hidden!=null)hidden.SetActive(false);continue;}
            Vector3 point=drop.position;
            if(!string.IsNullOrEmpty(drop.homeId))
            {
                if(!drop.anchored)
                {
                    point=room.AnimalSlotPosition(drop.indoorSlot)+drop.position;
                    drop.position=room.transform.InverseTransformPoint(point);drop.anchored=true;
                }
                point=room.transform.TransformPoint(drop.position);
            }
            if(!visuals.TryGetValue(drop.id,out var visual)||visual==null)
            {
                GameObject prefab=settings!=null?settings.manurePrefab:Resources.Load<GameObject>("World/AnimalManure");
                if(prefab==null)continue;
                visual=Instantiate(prefab,point,Quaternion.Euler(0,UnityEngine.Random.Range(0f,360f),0),transform);
                visual.name="KotoranHewan_"+drop.id;
                foreach(var collider in visual.GetComponentsInChildren<Collider>(true))collider.enabled=false;
                var renderers=visual.GetComponentsInChildren<Renderer>();
                if(renderers.Length>0)
                {
                    Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    visual.transform.localScale*= (drop.poultry ? .35f : .65f)/Mathf.Max(.01f,Mathf.Max(bounds.size.x,bounds.size.z));
                }
                visuals[drop.id]=visual;
            }
            visual.SetActive(true);
            visual.transform.position=point;
            // Snap the actual mesh bottom to the floor; disable colliders to avoid animal path blockers.
            var hits=Physics.RaycastAll(point+Vector3.up*1.2f,Vector3.down,2.7f,~0,QueryTriggerInteraction.Ignore);
            float floor=float.NegativeInfinity;
            foreach(var hit in hits)
                if(hit.normal.y>.7f && hit.collider.GetComponentInParent<AnimalGrowthSystem>()==null &&
                   hit.collider.GetComponentInParent<PlayerController>()==null && hit.point.y<=point.y+.35f)
                    floor=Mathf.Max(floor,hit.point.y);
            if(!float.IsNegativeInfinity(floor))
            {
                float bottom=float.PositiveInfinity;foreach(var r in visual.GetComponentsInChildren<Renderer>())bottom=Mathf.Min(bottom,r.bounds.min.y);
                if(!float.IsPositiveInfinity(bottom))visual.transform.position+=Vector3.up*(floor+.015f-bottom);
            }
        }
    }
    public static bool HasPitchfork(Inventory inventory)
    {
        var stack=inventory!=null?inventory.GetComponent<InventoryHotbarUI>()?.SelectedStack:null;
        return stack!=null && stack.count>0 && stack.item!=null && stack.item.equippedTool==PlayerToolType.Pitchfork;
    }
    public bool TryCollect(string id,Inventory inventory)
    {
        if(collecting || !HasPitchfork(inventory) || Time.timeScale<=0 ||
           (TimeManager.Instance!=null && TimeManager.Instance.IsPaused))return false;
        var player=inventory.GetComponent<PlayerController>();
        var drop=drops.Find(d=>d.id==id);
        if(player==null || player.IsMovementLocked || drop==null || !visuals.TryGetValue(id,out var visual) ||
           visual==null || !visual.activeSelf || Vector3.Distance(player.transform.position,visual.transform.position)>(settings!=null?settings.pickupRadius:2.4f))return false;
        ItemSO item=Resources.Load<ItemSO>(drop.poultry?"Items/Materials/Poultry Manure":"Items/Materials/Livestock Manure");
        if(item==null || !inventory.Add(item,1))
        {SaveLoadFeedback.Instance?.ShowMessage("Inventory penuh; kotoran tetap di tanah.");return false;}
        player.FaceTowardsInteraction(visual.transform.position);player.AcquireMovementLock(this);player.PlayScoopManureAnimation();
        collectingPlayer=player;collecting=true;
        drops.Remove(drop);visuals.Remove(id);Destroy(visual);
        PlayerPickupNotification.ShowItem(inventory,item,1);
        StartCoroutine(FinishCollection());return true;
    }
    IEnumerator FinishCollection()
    {
        yield return new WaitForSeconds(settings!=null?settings.pickupAnimationSeconds:1.1f);
        collectingPlayer?.ReleaseMovementLock(this);collectingPlayer=null;collecting=false;
    }
    public static AnimalManureSaveData Capture()
    {
        var system=Ensure();var data=new AnimalManureSaveData();
        foreach(var drop in system.drops)data.drops.Add(JsonUtility.FromJson<AnimalManureDropData>(JsonUtility.ToJson(drop)));
        foreach(var timer in system.timers)data.timers.Add(new AnimalManureTimerData{animalId=timer.Key,nextHour=timer.Value});
        return data;
    }
    public static void Restore(AnimalManureSaveData data)
    {
        var system=Ensure();system.StopAllCoroutines();system.collectingPlayer?.ReleaseMovementLock(system);
        system.collectingPlayer=null;system.collecting=false;
        foreach(var visual in system.visuals.Values)if(visual!=null)Destroy(visual);
        system.visuals.Clear();system.drops.Clear();system.timers.Clear();
        if(data?.drops!=null)foreach(var drop in data.drops)
            if(drop!=null && !string.IsNullOrEmpty(drop.id) && !system.drops.Exists(d=>d.id==drop.id))system.drops.Add(drop);
        if(data?.timers!=null)foreach(var timer in data.timers)
            if(timer!=null && !string.IsNullOrEmpty(timer.animalId) && !double.IsNaN(timer.nextHour)&&!double.IsInfinity(timer.nextHour))system.timers[timer.animalId]=timer.nextHour;
        system.RefreshVisuals();
    }
}
