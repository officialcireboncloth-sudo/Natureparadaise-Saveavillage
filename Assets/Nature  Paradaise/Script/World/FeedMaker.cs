using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FeedJob
{
    public string recipeId;
    public int inputs;
    public int output;
    public bool producesFishFeed;
    public bool producesPoultryFeed;
    public FeedKind Kind=>producesFishFeed?FeedKind.Fish:producesPoultryFeed?FeedKind.Poultry:FeedKind.Livestock;
    public double hours;
    public double finish = -1;
}

[Serializable]
public sealed class FeedMakerSaveData
{
    public string id;
    public int level;
    public int outputCapacity = 60;
    public int output;
    public int fishFeedOutput;
    public int poultryFeedOutput;
    public Vector3 position;
    public Quaternion rotation;
    public List<FeedJob> jobs = new();
}

/// <summary>Authored machine. Only jobs are dynamic; no machine is created on Play/reload.</summary>
public sealed class FeedMaker : MonoBehaviour
{
    static readonly List<FeedMaker> Active = new();
    static readonly Dictionary<string, FeedMakerSaveData> Cached = new();
    [SerializeField] string machineId;
    [SerializeField] string contextLabel;
    [SerializeField] FeedMakerCatalog catalog;
    [SerializeField, Range(1,4)] int level = 1;
    [SerializeField, Min(5)] int outputCapacity = 60;
    [Tooltip("Lv.4 dapat mengirim hasil ke stok kandang ini.")]
    [SerializeField] AnimalHome linkedStorage;
    [SerializeField] FeedSilo linkedSilo;
    [Tooltip("Fitur lanjutan. Biarkan OFF selama tempat pakan wajib diisi satu per satu.")]
    [SerializeField] bool automaticOutputTransferEnabled;
    [Tooltip("Interaksi F/E mesin ini ditangani controller interior Barn/Coop agar tidak terbaca dua kali.")]
    [SerializeField] bool externalInteraction;
    [Header("Take Feed Animation")]
    [SerializeField, Min(0f), Tooltip("Saat tangan mencapai output mesin dan pakan benar-benar masuk Inventory.")]
    float takeFeedImpactDelay = 0.45f;
    [SerializeField, Min(0.05f)] float takeFeedActionDuration = 0.85f;
    readonly List<FeedJob> jobs = new();
    int output;
    int fishFeedOutput;
    int poultryFeedOutput;
    Inventory inventory;
    PlayerController player;
    bool open;
    string feedback;


    bool fishFeedMode;
    bool dedicatedOutput;
    bool dedicatedFishFeed;


    WorldDebugStatusLabel debugLabel;
    bool worldDebugVisible = true;
    Coroutine animatedCollectionRoutine;
    PlayerController animatedCollectionPlayer;
    public int Level => Mathf.Clamp(level, 1, 4);
    public int InputCapacity => new[] {10,20,40,60}[Level-1];
    public int Inputs { get { int total=0; foreach(var job in jobs) total+=job.inputs; return total; } }
    public int Output => output + fishFeedOutput + poultryFeedOutput;
    public int AnimalFeedOutput => output;
    public int PoultryFeedOutput => poultryFeedOutput;
    public int FishFeedOutput => fishFeedOutput;
    // Read-only UI access. Production and save logic remain owned by this machine.
    public FeedMakerCatalog Catalog => catalog;
    public IReadOnlyList<FeedJob> Jobs => jobs;
    public int OutputCapacity => outputCapacity;
    public bool HasDedicatedOutput => dedicatedOutput;
    public bool DedicatedFishFeed => dedicatedFishFeed;
    public FeedKind DedicatedFeedKind=>dedicatedFishFeed?FeedKind.Fish:
        linkedStorage!=null?linkedStorage.RequiredFeedKind:FeedKind.Livestock;
    public int Ready(FeedKind kind)=>kind switch {FeedKind.Livestock=>output,FeedKind.Poultry=>poultryFeedOutput,FeedKind.Fish=>fishFeedOutput,_=>0};
    public string ContextLabel => contextLabel;
    public double CurrentGameHour => Now;
    double Now => TimeManager.Instance == null ? 0 : (TimeManager.Instance.day-1)*24d + TimeManager.Instance.hour + TimeManager.Instance.minute/60d;
    void Awake()
    {
        if (catalog == null) catalog = Resources.Load<FeedMakerCatalog>("Catalogs/FeedMakerCatalog");
        EnsureDebugLabel();
    }
    void OnEnable()
    {
        Active.Add(this);
        if (!string.IsNullOrEmpty(machineId) && Cached.TryGetValue(machineId, out var saved)) Restore(saved);
    }
    void OnDisable()
    {
        CancelAnimatedCollection();
        Close();
        PublishState();
        Active.Remove(this);
    }
    public void Configure(string id, FeedMakerCatalog data)
    {
        machineId=id;
        if(data!=null) catalog=data;
        if(!string.IsNullOrEmpty(machineId) && Cached.TryGetValue(machineId,out var saved)) Restore(saved);
        EnsureDebugLabel();
    }
    public void Connect(AnimalHome home, FeedSilo silo)
    {
        linkedStorage=home;
        linkedSilo=silo;
        if(home!=null) contextLabel=home.Label;
    }
    public void SetContextLabel(string value) => contextLabel=value;
    public void SetExternalInteraction(bool value) => externalInteraction=value;
    /// <summary>
    /// Mengunci mesin bangunan ke satu produk. Data lama yang salah jenis ikut
    /// dikonversi agar output tidak tampak hilang setelah sleep/save-load.
    /// </summary>
    public void SetDedicatedOutput(bool fishFeed)
    {
        dedicatedOutput=true;
        dedicatedFishFeed=fishFeed;
        fishFeedMode=fishFeed;
        if(NormalizeDedicatedOutput()) PublishState();
    }

    bool NormalizeDedicatedOutput()
    {
        if(!dedicatedOutput) return false;
        bool changed=false;
        FeedKind kind=DedicatedFeedKind;
        int total=Output;
        if(Ready(kind)!=total)
        {output=kind==FeedKind.Livestock?total:0;poultryFeedOutput=kind==FeedKind.Poultry?total:0;fishFeedOutput=kind==FeedKind.Fish?total:0;changed=true;}
        foreach(FeedJob job in jobs)
        {
            if(job.Kind==kind) continue;
            job.producesFishFeed=kind==FeedKind.Fish;
            job.producesPoultryFeed=kind==FeedKind.Poultry;
            changed=true;
        }
        return changed;
    }

    // Event simulation: complete jobs chronologically across sleep, then fill free lanes.
    public void Advance(double now)
    {
        if (double.IsNaN(now) || double.IsInfinity(now)) return;
        int previousJobs=jobs.Count;
        int previousRunning=RunningJobCount();
        int previousOutput=output;
        int previousFishOutput=fishFeedOutput;
        int previousPoultryOutput=poultryFeedOutput;
        int guard = jobs.Count*2+2;
        while (guard-- > 0)
        {
            double next = double.PositiveInfinity;
            foreach (var job in jobs) if (job.finish >= 0) next=Math.Min(next,job.finish);
            if (next > now) break;
            for (int i=jobs.Count-1;i>=0;i--)
                if (jobs[i].finish >= 0 && jobs[i].finish <= next)
                {
                    if (jobs[i].producesFishFeed) fishFeedOutput+=jobs[i].output;
                    else if (jobs[i].producesPoultryFeed) poultryFeedOutput+=jobs[i].output;
                    else output+=jobs[i].output;
                    jobs.RemoveAt(i);
                }
            StartWaiting(next);
        }
        StartWaiting(now);
        if(previousJobs!=jobs.Count || previousRunning!=RunningJobCount() ||
           previousOutput!=output || previousFishOutput!=fishFeedOutput || previousPoultryOutput!=poultryFeedOutput)
            PublishState();
    }

    int RunningJobCount()
    {
        int count=0;
        foreach(FeedJob job in jobs) if(job.finish>=0) count++;
        return count;
    }
    void StartWaiting(double at)
    {
        int running=0, reserved=Output;
        foreach(var job in jobs) if(job.finish>=0) {running++; reserved+=job.output;}
        foreach(var job in jobs)
        {
            if(running>=Level) break;
            if(job.finish>=0) continue;
            if(reserved+job.output>outputCapacity) break;
            job.finish=at+job.hours; reserved+=job.output; running++;
        }
    }
    public bool Queue(Inventory source, int index)
    {
        if(source==null || TimeManager.Instance==null || catalog?.animalFeed==null || catalog.recipes==null || index<0 || index>=catalog.recipes.Length) return false;
        Advance(Now);
        FeedRecipe recipe=catalog.recipes[index];
        if(recipe?.input==null || recipe.inputCount<1 || recipe.outputCount<1 || recipe.hours<=0 || recipe.outputCount>outputCapacity || Inputs+recipe.inputCount>InputCapacity) return false;
        if (recipe.producesFishFeed && catalog.fishFeed==null) return false;
        if(catalog.Item(recipe.Kind)==null || FeedTypes.Kind(catalog.Item(recipe.Kind))!=recipe.Kind || dedicatedOutput && recipe.Kind!=DedicatedFeedKind)return false;
        var needed = new Dictionary<ItemSO,int>();
        int remaining=recipe.inputCount;
        ItemSO[] allowed=recipe.mixedInputs!=null && recipe.mixedInputs.Length>0 ? recipe.mixedInputs : new[]{recipe.input};
        foreach(var item in allowed)
        {
            if(item==null || needed.ContainsKey(item)) continue;
            int take=Math.Min(remaining,source.GetCount(item));
            if(take>0) {needed.Add(item,take); remaining-=take;}
            if(remaining==0) break;
        }
        if(remaining>0) return false;
        foreach(var pair in needed) source.Remove(pair.Key,pair.Value);
        jobs.Add(new FeedJob {recipeId=recipe.id, inputs=recipe.inputCount, output=recipe.outputCount,
            producesFishFeed=recipe.producesFishFeed, producesPoultryFeed=recipe.producesPoultryFeed, hours=recipe.hours/new[]{1d,1.25d,1.6d,2d}[Level-1]});
        StartWaiting(Now);
        PublishState();
        return true;
    }
    public bool Collect(Inventory target)
        =>CollectFeed(target,dedicatedOutput?DedicatedFeedKind:FeedKind.Livestock);

    /// <summary>Collects exactly one unit of the requested feed category without changing other outputs.</summary>
    public bool CollectFeed(Inventory target,FeedKind kind)
    {
        Advance(Now);
        ItemSO item=catalog!=null?catalog.Item(kind):null;
        if(Ready(kind)<=0 || target==null || item==null || FeedTypes.Kind(item)!=kind) return false;
        const int amount=1;
        if(!target.Add(item,amount)) return false;
        if(kind==FeedKind.Poultry)poultryFeedOutput-=amount;
        else if(kind==FeedKind.Fish)fishFeedOutput-=amount;
        else output-=amount;
        StartWaiting(Now); PublishState();
        PlayerPickupNotification.ShowItem(target,item,amount);
        return true;
    }
    public bool CollectFishFeed(Inventory target)
        =>CollectFeed(target,FeedKind.Fish);

    /// <summary>
    /// Mengambil satu output melalui animasi setinggi pinggang. Efek inventory ditunda
    /// sampai tangan menyentuh mesin dan movement tetap terkunci sampai action selesai.
    /// </summary>
    public bool CollectAnimated(Inventory target, bool fishFeed, Transform interactionTarget = null)
        =>CollectAnimatedFeed(target,fishFeed?FeedKind.Fish:dedicatedOutput?DedicatedFeedKind:FeedKind.Livestock,interactionTarget);

    /// <summary>Collects any of the three feeds at the same hand-contact moment.</summary>
    public bool CollectAnimatedFeed(Inventory target,FeedKind kind,Transform interactionTarget=null)
    {
        Advance(Now);
        if(animatedCollectionRoutine!=null || target==null) return false;
        bool ready=Ready(kind)>0 && catalog?.Item(kind)!=null;
        if(!ready) return false;
        animatedCollectionRoutine=StartCoroutine(AnimatedCollectionRoutine(target,kind,
            interactionTarget!=null ? interactionTarget : transform));
        return true;
    }

    IEnumerator AnimatedCollectionRoutine(Inventory target, FeedKind kind, Transform interactionTarget)
    {
        animatedCollectionPlayer=target.GetComponent<PlayerController>();
        if(animatedCollectionPlayer!=null)
        {
            animatedCollectionPlayer.FaceTowardsInteraction(interactionTarget.position);
            animatedCollectionPlayer.AcquireMovementLock(this);
            animatedCollectionPlayer.PlayPickUpWaistAnimation();
        }

        float impactDelay=Mathf.Min(takeFeedImpactDelay,takeFeedActionDuration);
        if(impactDelay>0f) yield return new WaitForSeconds(impactDelay);
        bool collected=CollectFeed(target,kind);
        feedback=collected
            ? $"{FeedTypes.Label(kind)} x1 masuk Inventory."
            : "Pakan belum siap / Inventory penuh.";
        SaveLoadFeedback.Instance?.ShowMessage(feedback);

        float remaining=Mathf.Max(0f,takeFeedActionDuration-impactDelay);
        if(remaining>0f) yield return new WaitForSeconds(remaining);
        FinishAnimatedCollection();
    }

    void CancelAnimatedCollection()
    {
        if(animatedCollectionRoutine!=null) StopCoroutine(animatedCollectionRoutine);
        FinishAnimatedCollection();
    }

    void FinishAnimatedCollection()
    {
        animatedCollectionPlayer?.ReleaseMovementLock(this);
        animatedCollectionPlayer=null;
        animatedCollectionRoutine=null;
    }

    /// <summary>Satu tekanan tombol hanya mengambil satu unit output dari mesin.</summary>
    public bool CollectOneReady(Inventory target)
    {
        Advance(Now);
        if(fishFeedMode)
        {
            if(CollectFishFeed(target)) return true;
            if(Collect(target))return true;
            return CollectFeed(target,FeedKind.Poultry);
        }
        if(Collect(target)) return true;
        if(CollectFeed(target,FeedKind.Poultry)) return true;
        return CollectFishFeed(target);
    }
    public bool OpenFor(Inventory target)
    {
        if(open || target==null || FeedMakerUI.Instance!=null || WorldInteractionPrompt.IsSuppressed || GameplayPauseMenu.BlocksGameplayInput) return false;
        Advance(Now);
        inventory=target;
        open=true;
        player=inventory.GetComponent<PlayerController>();
        player?.AcquireMovementLock(this);
        TimeManager.Instance?.AcquirePause(this);
        WorldInteractionPrompt.AcquireSuppression(this);
        GameplayInput.ConsumeCurrentFrame();
        FeedMakerUI.Show(this, target);
        RefreshDebugLabel();
        return true;
    }
    public bool Upgrade(Inventory source)
    {
        if(Level>=4 || source==null || catalog==null || ScoreManager.Instance==null) return false;
        int wood=Level*5, stone=Level*3, gold=Level*100;
        if(catalog.upgradeWood==null || catalog.upgradeStone==null || source.GetCount(catalog.upgradeWood)<wood || source.GetCount(catalog.upgradeStone)<stone) return false;
        if(!ScoreManager.Instance.TrySpendPoints(gold)) return false;
        source.Remove(catalog.upgradeWood,wood); source.Remove(catalog.upgradeStone,stone);
        Advance(Now); level=Level+1; StartWaiting(Now); PublishState(); return true;
    }
    void Update()
    {
        if(TimeManager.Instance!=null) Advance(Now);
        if(automaticOutputTransferEnabled && Level==4 && linkedSilo!=null && output>0)
        { output-=linkedSilo.Store(output,FeedKind.Livestock); StartWaiting(Now); }
        else if(automaticOutputTransferEnabled && Level==4 && linkedStorage!=null && linkedStorage.Available && output>0)
        { output-=linkedStorage.StoreFeed(output,FeedKind.Livestock); StartWaiting(Now); }
        if(automaticOutputTransferEnabled && Level==4 && poultryFeedOutput>0)
        {poultryFeedOutput-=linkedSilo!=null?linkedSilo.Store(poultryFeedOutput,FeedKind.Poultry):linkedStorage!=null&&linkedStorage.Available?linkedStorage.StoreFeed(poultryFeedOutput,FeedKind.Poultry):0;StartWaiting(Now);}
        RefreshDebugLabel();
        if(inventory==null) inventory=FindFirstObjectByType<Inventory>();
        if(open) { if(GameplayInput.GetKeyDown(KeyCode.Escape)) Close(); return; }
        // Barn/Coop memakai controller interior. Handler generik harus dilewati agar
        // satu tekanan F tidak diproses dua kali oleh dua komponen berbeda.
        if(externalInteraction) return;
        if(inventory==null || !PlayerInteractionTarget.Contains(inventory.transform,transform)) return;
        string state=Output>0 ? $"Siap: Hewan {output} | Unggas {poultryFeedOutput} | Ikan {fishFeedOutput}" : jobs.Count>0 ? "Processing" : "Idle";
        WorldInteractionPrompt.Request(this,transform,$"E: Buka Feed Maker Lv.{Level} — {state}\nF: Ambil 1 pakan",Vector3.Distance(inventory.transform.position,transform.position),1.5f);
        if(PlayerInteractionTarget.PressPrompt(this, KeyCode.F))
        {
            FeedKind kind=dedicatedOutput?DedicatedFeedKind:fishFeedMode&&fishFeedOutput>0?FeedKind.Fish:
                output>0?FeedKind.Livestock:poultryFeedOutput>0?FeedKind.Poultry:FeedKind.Fish;
            if(!CollectAnimatedFeed(inventory,kind,transform))
                SaveLoadFeedback.Instance?.ShowMessage("Feed belum ready / Inventory penuh");
            return;
        }
        if(!PlayerInteractionTarget.PressPrompt(this, KeyCode.E)) return;
        OpenFor(inventory);
    }

    void EnsureDebugLabel()
    {
        if(!worldDebugVisible) return;
        debugLabel=WorldDebugStatusLabel.GetOrCreate(transform,"FeedMaker_DebugLabel",
            new Vector3(0.85f,2.45f,0.35f));
        debugLabel?.SetColor(new Color(1f,0.82f,0.2f,1f));
    }

    public string DebugStatus
    {
        get
        {
            string owner=string.IsNullOrWhiteSpace(contextLabel) ? string.Empty : $" {contextLabel}";
            return $"MESIN PAKAN{owner} Lv.{Level}\n"+
                   $"Animal: {AnimalFeedCompactDebugStatus}\n"+
                   $"Unggas: {PoultryFeedCompactDebugStatus}\n"+
                   $"Fish: {FishFeedCompactDebugStatus}\n"+
                   $"IN {Inputs}/{InputCapacity} | OUT {Output}/{outputCapacity}";
        }
    }

    public string AnimalFeedCompactDebugStatus => CompactDebugStatusFor(false);
    public string PoultryFeedCompactDebugStatus => CompactDebugStatusFor(FeedKind.Poultry);
    public string FishFeedCompactDebugStatus => CompactDebugStatusFor(true);
    public string CompactDebugStatus
    {
        get => $"Hewan {AnimalFeedCompactDebugStatus} | Unggas {PoultryFeedCompactDebugStatus} | Ikan {FishFeedCompactDebugStatus}";
    }

    string CompactDebugStatusFor(bool fishFeed)
        =>CompactDebugStatusFor(fishFeed?FeedKind.Fish:FeedKind.Livestock);

    string CompactDebugStatusFor(FeedKind kind)
    {
        int ready=Ready(kind);
        FeedJob running=null;
        int waiting=0;
        foreach(FeedJob job in jobs)
        {
            if(job.Kind!=kind) continue;
            if(job.finish<0) waiting++;
            else if(running==null || job.finish<running.finish) running=job;
        }
        string process=null;
        if(running!=null)
        {
            double remaining=Math.Max(0d,running.finish-Now);
            double duration=Math.Max(0.001d,running.hours);
            int percent=Mathf.Clamp(Mathf.RoundToInt((float)((1d-remaining/duration)*100d)),0,100);
            process=$"PROCESS {percent}% ({GameTimeDebugText.FormatHours(remaining)})";
        }
        else if(waiting>0) process="PROCESS 0% (ANTRE)";

        if(ready>0 && process!=null) return $"READY x{ready} | {process}";
        if(ready>0) return $"READY x{ready}";
        return process ?? "EMPTY";
    }

    public void SetWorldDebugVisible(bool visible)
    {
        worldDebugVisible=visible;
        if(visible && debugLabel==null) EnsureDebugLabel();
        if(debugLabel!=null) debugLabel.gameObject.SetActive(visible);
    }

    void RefreshDebugLabel()
    {
        if(!worldDebugVisible || open)
        {
            if(debugLabel!=null) debugLabel.gameObject.SetActive(false);
            return;
        }
        if(debugLabel==null) EnsureDebugLabel();
        if(debugLabel!=null && !debugLabel.gameObject.activeSelf) debugLabel.gameObject.SetActive(true);
        debugLabel?.SetText(DebugStatus);
    }
    public void Close()
    {
        open=false;
        if(FeedMakerUI.Instance!=null && FeedMakerUI.Instance.Machine==this) FeedMakerUI.Instance.Dispose();
        player?.ReleaseMovementLock(this); TimeManager.Instance?.ReleasePause(this); WorldInteractionPrompt.ReleaseSuppression(this);
        GameplayInput.ConsumeCurrentFrame();
    }
    FeedMakerSaveData Capture()
    {
        var data=new FeedMakerSaveData {id=machineId,level=Level,outputCapacity=outputCapacity,output=output,fishFeedOutput=fishFeedOutput,poultryFeedOutput=poultryFeedOutput,position=transform.position,rotation=transform.rotation};
        foreach(var j in jobs) data.jobs.Add(new FeedJob {recipeId=j.recipeId,inputs=j.inputs,output=j.output,producesFishFeed=j.producesFishFeed,producesPoultryFeed=j.producesPoultryFeed,hours=j.hours,finish=j.finish});
        return data;
    }

    void PublishState()
    {
        if(string.IsNullOrEmpty(machineId)) return;
        FeedMakerSaveData snapshot=Capture();
        Cached[machineId]=snapshot;
        // Satu ID adalah satu mesin logis. Jika prefab/scene sementara menghasilkan
        // representasi kedua, keduanya harus membaca output dan antrean yang sama.
        foreach(FeedMaker peer in Active)
            if(peer!=null && peer!=this && peer.machineId==machineId)
                peer.Restore(snapshot);
    }
    void Restore(FeedMakerSaveData data)
    {
        if(data.outputCapacity>0) outputCapacity=Mathf.Max(5,data.outputCapacity);
        level=Mathf.Clamp(data.level,1,4); output=Mathf.Clamp(data.output,0,outputCapacity); fishFeedOutput=Mathf.Clamp(data.fishFeedOutput,0,Mathf.Max(0,outputCapacity-output));
        poultryFeedOutput=Mathf.Clamp(data.poultryFeedOutput,0,Mathf.Max(0,outputCapacity-output-fishFeedOutput));
        jobs.Clear(); if(data.jobs!=null) foreach(var j in data.jobs)
            jobs.Add(new FeedJob {recipeId=j.recipeId,inputs=j.inputs,output=j.output,producesFishFeed=j.producesFishFeed,producesPoultryFeed=j.producesPoultryFeed,hours=j.hours,finish=j.finish});
        NormalizeDedicatedOutput();
    }
    public static List<FeedMakerSaveData> CaptureAll()
    {
        double now=CurrentAbsoluteHour();
        FeedMaker[] activeSnapshot=Active.ToArray();
        foreach(FeedMaker machine in activeSnapshot)
            if(machine!=null && !string.IsNullOrEmpty(machine.machineId))
            {
                machine.Advance(now);
                Cached[machine.machineId]=machine.Capture();
            }
        // Mesin interior dapat sedang tidak dimuat ketika player tidur. Proses record
        // cache-nya juga agar save setelah tidur langsung menyimpan output READY.
        foreach(FeedMakerSaveData saved in Cached.Values) AdvanceSaved(saved,now);
        return new List<FeedMakerSaveData>(Cached.Values);
    }

    static double CurrentAbsoluteHour() => TimeManager.Instance==null ? 0d :
        (TimeManager.Instance.day-1)*24d+TimeManager.Instance.hour+TimeManager.Instance.minute/60d;

    static void AdvanceSaved(FeedMakerSaveData data,double now)
    {
        if(data?.jobs==null || double.IsNaN(now) || double.IsInfinity(now)) return;
        int capacity=data.outputCapacity>0 ? Math.Max(5,data.outputCapacity) : 60;
        int lanes=Math.Max(1,Math.Min(4,data.level));
        int guard=data.jobs.Count*2+2;
        while(guard-- > 0)
        {
            double next=double.PositiveInfinity;
            foreach(FeedJob job in data.jobs) if(job.finish>=0) next=Math.Min(next,job.finish);
            if(next>now) break;
            for(int index=data.jobs.Count-1;index>=0;index--)
            {
                FeedJob job=data.jobs[index];
                if(job.finish<0 || job.finish>next) continue;
                if(job.producesFishFeed) data.fishFeedOutput+=job.output;
                else if(job.producesPoultryFeed) data.poultryFeedOutput+=job.output;
                else data.output+=job.output;
                data.jobs.RemoveAt(index);
            }
            StartSavedJobs(data,next,capacity,lanes);
        }
        StartSavedJobs(data,now,capacity,lanes);
    }

    static void StartSavedJobs(FeedMakerSaveData data,double at,int capacity,int lanes)
    {
        int running=0;
        int reserved=data.output+data.fishFeedOutput+data.poultryFeedOutput;
        foreach(FeedJob job in data.jobs)
            if(job.finish>=0) {running++;reserved+=job.output;}
        foreach(FeedJob job in data.jobs)
        {
            if(running>=lanes) break;
            if(job.finish>=0) continue;
            if(reserved+job.output>capacity) break;
            job.finish=at+job.hours;
            reserved+=job.output;
            running++;
        }
    }
    public static void RestoreAll(List<FeedMakerSaveData> data)
    {
        Cached.Clear();
        if(data!=null) foreach(var saved in data) if(saved!=null && !string.IsNullOrEmpty(saved.id)) Cached[saved.id]=saved;
        double now=CurrentAbsoluteHour();
        foreach(FeedMakerSaveData saved in Cached.Values) AdvanceSaved(saved,now);
        foreach(var machine in Active)
        {
            machine.Close(); machine.jobs.Clear(); machine.output=0; machine.fishFeedOutput=0; machine.poultryFeedOutput=0;
            if(!string.IsNullOrEmpty(machine.machineId) && Cached.TryGetValue(machine.machineId,out var saved)) machine.Restore(saved);
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Active.Clear(); Cached.Clear(); }
}
