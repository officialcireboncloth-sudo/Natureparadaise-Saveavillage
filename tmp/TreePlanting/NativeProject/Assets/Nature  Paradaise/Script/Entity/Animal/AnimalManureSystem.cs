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
    public int createdDay;
    public Vector3 position;
}
[Serializable]
public sealed class AnimalManureTimerData
{
    public string animalId;
    public double nextHour;
    public int scheduledDay;
    public int lastProducedDay=-1;
    public int attemptedDay=-1;
}
[Serializable]
public sealed class AnimalManureSaveData
{
    public int version;
    public List<AnimalManureDropData> drops = new();
    public List<AnimalManureTimerData> timers = new();
}

/// <summary>One random daily opportunity and at most one outstanding pile per animal, including unvisited barns.</summary>
[DefaultExecutionOrder(-250)]
public sealed class AnimalManureSystem : MonoBehaviour
{
    public static AnimalManureSystem Instance { get; private set; }
    readonly List<AnimalManureDropData> drops = new();
    readonly Dictionary<string,AnimalManureTimerData> timers = new();
    readonly Dictionary<string,GameObject> visuals = new();
    AnimalManureSettings settings;
    Inventory playerInventory;
    PlayerController collectingPlayer;
    bool collecting;
    float nextRefresh;
    public IReadOnlyList<AnimalManureDropData> Drops => drops;
    public IEnumerable<GameObject> ActiveDropVisuals => visuals.Values;
    public bool IsCollecting => collecting;
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
        StopAllCoroutines();EndCollection();
        collectingPlayer=null;collecting=false;
    }
    void OnDestroy(){if(Instance==this)Instance=null;}
    public double NextDropHour(string animalId)=>timers.TryGetValue(animalId,out var timer)?timer.nextHour:0;
    AnimalManureTimerData Schedule(string id)
    {
        int today=TimeManager.Instance.day;
        if(!timers.TryGetValue(id,out var timer))
            timers[id]=timer=new AnimalManureTimerData{animalId=id};
        if(timer.scheduledDay!=today)
        {
            float earliest=Mathf.Clamp(settings!=null?settings.dailyEarliestHour:6f,0,23.98f);
            float latest=Mathf.Clamp(settings!=null?settings.dailyLatestHour:22f,earliest,23.99f);
            // Newly purchased animals get a remaining-day opportunity; skipped days never catch up.
            earliest=Mathf.Min(23.98f,Mathf.Max(earliest,TimeManager.Instance.CurrentTimeHours));
            latest=Mathf.Max(earliest,latest);
            timer.nextHour=(today-1)*24d+UnityEngine.Random.Range(earliest,latest);
            timer.scheduledDay=today;
        }
        return timer;
    }
    bool IsDue(string id,double now)
    {
        var timer=Schedule(id);int today=TimeManager.Instance.day;
        if(timer.attemptedDay==today||timer.lastProducedDay==today||now<timer.nextHour)return false;
        timer.attemptedDay=today;return true;
    }
    bool CanProduce(string id)=>!string.IsNullOrEmpty(id)&&TimeManager.Instance!=null&&
        Schedule(id).lastProducedDay!=TimeManager.Instance.day&&!drops.Exists(d=>d.animalId==id);
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
        string id=nearest.id;
        WorldInteractionPrompt.Request(this,target.transform,hasFork?"E: Serok kotoran · 1 detik → Compost x1":"Pilih Garpu untuk menyerok kotoran",distance,.65f,
            hasFork?()=>TryCollect(id,playerInventory):null);
        if(hasFork && PlayerInteractionTarget.PressPrompt(this, target.transform, KeyCode.E))
            TryCollect(nearest.id,playerInventory);
    }
    public void ProduceDueDrops()
    {
        if(TimeManager.Instance==null)return;
        double now=Now;
        foreach(var animal in AnimalGrowthSystem.ActiveAnimals)
        {
            if(animal==null || !animal.HasBeenBorn || animal.IsInIncubator || string.IsNullOrEmpty(animal.AnimalId))continue;
            if(IsDue(animal.AnimalId,now))SpawnDrop(animal);
        }
        foreach(var horse in PersonalAnimal.Active)
            if(horse!=null&&horse.Species==CompanionSpecies.Horse&&horse.IsTamed&&IsDue("companion:"+horse.Id,now))
                SpawnHorseDrop(horse);
    }
    public AnimalManureDropData SpawnDrop(AnimalGrowthSystem animal)
    {
        if(animal==null || !animal.HasBeenBorn || animal.IsInIncubator || !CanProduce(animal.AnimalId))return null;
        var routine=animal.GetComponent<AnimalRoutine>();
        bool indoors=routine!=null && routine.IsHoused && routine.Home!=null;
        var jitter=UnityEngine.Random.insideUnitCircle*.6f;
        var drop=new AnimalManureDropData{ id=Guid.NewGuid().ToString("N"),animalId=animal.AnimalId,
            poultry=AnimalGrowthProfileSO.IsBird(animal.Type),sceneName=animal.gameObject.scene.name,
            homeId=indoors?routine.HomeId:null,indoorSlot=indoors?Mathf.Max(0,routine.Home.Residents.IndexOf(routine)):0,
            position=indoors?new Vector3(jitter.x,0,jitter.y):animal.transform.position-animal.transform.forward*.8f+new Vector3(jitter.x,0,jitter.y)};
        if(!indoors && AnimalWalkingPath.Ground(drop.position,animal.transform,null,out var ground,true))drop.position=ground;
        RegisterDrop(drop);return drop;
    }
    AnimalManureDropData SpawnHorseDrop(PersonalAnimal horse)
    {
        string id="companion:"+horse.Id;if(!CanProduce(id))return null;
        var jitter=UnityEngine.Random.insideUnitCircle*.6f;
        Vector3 point=horse.transform.position-horse.transform.forward*.8f+new Vector3(jitter.x,0,jitter.y);
        if(AnimalWalkingPath.Ground(point,horse.transform,null,out var ground,true))point=ground;
        // Companion objects live in DontDestroyOnLoad, but their piles belong to the outdoor world.
        string scene=horse.gameObject.scene.name;
        if(scene=="DontDestroyOnLoad")scene=horse.Capture().homeScene;
        var drop=new AnimalManureDropData{id=Guid.NewGuid().ToString("N"),animalId=id,sceneName=scene,position=point};
        RegisterDrop(drop);return drop;
    }
    void RegisterDrop(AnimalManureDropData drop)
    {
        drop.createdDay=TimeManager.Instance.day;
        var timer=Schedule(drop.animalId);timer.lastProducedDay=drop.createdDay;timer.attemptedDay=drop.createdDay;
        drops.Add(drop);
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
        if(!isActiveAndEnabled || collecting || !HasPitchfork(inventory) || Time.timeScale<=0 || WorldInteractionPrompt.IsSuppressed || GameplayPauseMenu.BlocksGameplayInput ||
           (TimeManager.Instance!=null && TimeManager.Instance.IsPaused))return false;
        var player=inventory.GetComponent<PlayerController>();
        var drop=drops.Find(d=>d.id==id);
        if(player==null || player.IsMovementLocked || drop==null || !visuals.TryGetValue(id,out var visual) ||
           visual==null || !visual.activeSelf || Vector3.Distance(player.transform.position,visual.transform.position)>(settings!=null?settings.pickupRadius:2.4f))return false;
        ItemSO item=Resources.Load<ItemSO>("Items/Materials/Compost");
        if(!StaminaBalance.Can(inventory,"clean_manure")) return false;
        if(item==null || !inventory.CanAdd(item,1))
        {SaveLoadFeedback.Instance?.ShowMessage("Inventory penuh; kotoran tetap di tanah.");return false;}
          if(!PlayerToolApproach.InReach(player.transform,visual.transform.position,.8f,visual.transform))
              return PlayerToolApproach.For(player).Begin(this,()=>visual.transform.position,.8f,()=>TryCollect(id,inventory),
                  ()=>isActiveAndEnabled&&visual!=null&&visual.activeInHierarchy&&drops.Contains(drop)&&HasPitchfork(inventory),visual.transform);
        player.FaceTowardsInteraction(visual.transform.position);player.AcquireMovementLock(this);player.PlayScoopManureAnimation();
        player.GetComponent<PlayerHeldTools>()?.BeginWorkAction(PlayerToolType.Pitchfork,visual.transform.position,AnimalManureSettings.CollectionSeconds);
        collectingPlayer=player;collecting=true;
        StartCoroutine(FinishCollection(drop,inventory,item,visual));return true;
    }
    IEnumerator FinishCollection(AnimalManureDropData drop,Inventory inventory,ItemSO item,GameObject visual)
    {
        yield return new WaitForSeconds(AnimalManureSettings.CollectionSeconds);
        if(inventory!=null && HasPitchfork(inventory) && visual!=null && visual.activeInHierarchy && drops.Contains(drop) &&
           !WorldInteractionPrompt.IsSuppressed && !GameplayPauseMenu.BlocksGameplayInput &&
           StaminaBalance.Can(inventory,"clean_manure") && inventory.CanAdd(item,1))
        {
            // Remove before InventoryChanged can save the world: the item and pile must never both persist.
            int index=drops.IndexOf(drop);drops.RemoveAt(index);visuals.Remove(drop.id);visual.SetActive(false);
            if(inventory.Add(item,1))
            {
                StaminaBalance.Spend(inventory,"clean_manure");
                FarmActionEffects.Burst(FarmEffect.Hoe,visual.transform.position,radius:.15f);
                Destroy(visual);GameplayInfoPanel.NotifyBesideTools("Compost x1");
            }
            else{drops.Insert(index,drop);visuals[drop.id]=visual;visual.SetActive(true);}
        }
        else if(inventory!=null&&!inventory.CanAdd(item,1))GameplayInfoPanel.NotifyBesideTools("Inventory penuh; kotoran tetap di tanah.");
        EndCollection();
    }
    void EndCollection()
    {
        collectingPlayer?.ReleaseMovementLock(this);collectingPlayer?.GetComponent<PlayerHeldTools>()?.EndWorkAction();
        collectingPlayer=null;collecting=false;
    }
    /// <summary>Putting the fork away cancels uncommitted cleaning and releases only its movement lock.</summary>
    public void CancelCollection(PlayerController player)
    {
        if(!collecting || collectingPlayer!=player)return;
        StopAllCoroutines();EndCollection();
    }
    public static AnimalManureSaveData Capture()
    {
        var system=Ensure();var data=new AnimalManureSaveData{version=2};
        foreach(var drop in system.drops)data.drops.Add(JsonUtility.FromJson<AnimalManureDropData>(JsonUtility.ToJson(drop)));
        foreach(var timer in system.timers.Values)data.timers.Add(JsonUtility.FromJson<AnimalManureTimerData>(JsonUtility.ToJson(timer)));
        return data;
    }
    public static void Restore(AnimalManureSaveData data)
    {
        var system=Ensure();system.StopAllCoroutines();system.EndCollection();
        foreach(var visual in system.visuals.Values)if(visual!=null)Destroy(visual);
        system.visuals.Clear();system.drops.Clear();system.timers.Clear();
        if(data?.drops!=null)foreach(var drop in data.drops)
            if(drop!=null && !string.IsNullOrEmpty(drop.id) && !system.drops.Exists(d=>d.id==drop.id))system.drops.Add(drop);
        if(data?.timers!=null)foreach(var timer in data.timers)
            if(timer!=null && !string.IsNullOrEmpty(timer.animalId) && !double.IsNaN(timer.nextHour)&&!double.IsInfinity(timer.nextHour))
            {
                var restored=JsonUtility.FromJson<AnimalManureTimerData>(JsonUtility.ToJson(timer));
                // Legacy interval timers have no daily quota. Conservatively skip their current day.
                if(data.version<2){restored.scheduledDay=0;restored.lastProducedDay=TimeManager.Instance!=null?TimeManager.Instance.day:1;restored.attemptedDay=restored.lastProducedDay;}
                system.timers[timer.animalId]=restored;
            }
        if(TimeManager.Instance!=null)foreach(var drop in system.drops)
            if(!string.IsNullOrEmpty(drop.animalId))
            {
                var timer=system.Schedule(drop.animalId);
                timer.lastProducedDay=Mathf.Max(timer.lastProducedDay,drop.createdDay>0?drop.createdDay:TimeManager.Instance.day);
            }
        system.RefreshVisuals();
    }
}
