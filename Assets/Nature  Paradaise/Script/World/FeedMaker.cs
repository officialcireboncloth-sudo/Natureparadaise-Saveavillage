using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FeedJob
{
    public string recipeId;
    public int inputs;
    public int output;
    public double hours;
    public double finish = -1;
}

[Serializable]
public sealed class FeedMakerSaveData
{
    public string id;
    public int level;
    public int output;
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
    [SerializeField] FeedMakerCatalog catalog;
    [SerializeField, Range(1,4)] int level = 1;
    [SerializeField, Min(5)] int outputCapacity = 60;
    [Tooltip("Lv.4 dapat mengirim hasil ke stok kandang ini.")]
    [SerializeField] AnimalHome linkedStorage;
    [SerializeField] FeedSilo linkedSilo;
    readonly List<FeedJob> jobs = new();
    int output;
    Inventory inventory;
    PlayerController player;
    bool open;
    string feedback;
    public int Level => Mathf.Clamp(level, 1, 4);
    public int InputCapacity => new[] {10,20,40,60}[Level-1];
    public int Inputs { get { int total=0; foreach(var job in jobs) total+=job.inputs; return total; } }
    public int Output => output;
    double Now => TimeManager.Instance == null ? 0 : (TimeManager.Instance.day-1)*24d + TimeManager.Instance.hour + TimeManager.Instance.minute/60d;
    void Awake()
    {
        if (catalog == null) catalog = Resources.Load<FeedMakerCatalog>("Catalogs/FeedMakerCatalog");
        if (string.IsNullOrEmpty(machineId)) Debug.LogError("Feed Maker membutuhkan Machine ID permanen.", this);
    }
    void OnEnable()
    {
        Active.Add(this);
        if (!string.IsNullOrEmpty(machineId) && Cached.TryGetValue(machineId, out var saved)) Restore(saved);
    }
    void OnDisable()
    {
        Close();
        if (!string.IsNullOrEmpty(machineId)) Cached[machineId] = Capture();
        Active.Remove(this);
    }
    public void Configure(string id, FeedMakerCatalog data) { machineId=id; catalog=data; }
    public void Connect(AnimalHome home, FeedSilo silo) { linkedStorage=home; linkedSilo=silo; }

    // Event simulation: complete jobs chronologically across sleep, then fill free lanes.
    public void Advance(double now)
    {
        if (double.IsNaN(now) || double.IsInfinity(now)) return;
        int guard = jobs.Count*2+2;
        while (guard-- > 0)
        {
            double next = double.PositiveInfinity;
            foreach (var job in jobs) if (job.finish >= 0) next=Math.Min(next,job.finish);
            if (next > now) break;
            for (int i=jobs.Count-1;i>=0;i--)
                if (jobs[i].finish >= 0 && jobs[i].finish <= next)
                { output+=jobs[i].output; jobs.RemoveAt(i); }
            StartWaiting(next);
        }
        StartWaiting(now);
    }
    void StartWaiting(double at)
    {
        int running=0, reserved=output;
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
        jobs.Add(new FeedJob {recipeId=recipe.id, inputs=recipe.inputCount, output=recipe.outputCount, hours=recipe.hours/new[]{1d,1.25d,1.6d,2d}[Level-1]});
        StartWaiting(Now);
        return true;
    }
    public bool Collect(Inventory target)
    {
        Advance(Now);
        if(output<=0 || target==null || catalog?.animalFeed==null) return false;
        int amount=Math.Min(output,Math.Max(1,catalog.animalFeed.maxStack));
        if(!target.Add(catalog.animalFeed,amount)) return false;
        output-=amount; StartWaiting(Now); return true;
    }
    public bool Upgrade(Inventory source)
    {
        if(Level>=4 || source==null || catalog==null || ScoreManager.Instance==null) return false;
        int wood=Level*5, stone=Level*3, gold=Level*100;
        if(catalog.upgradeWood==null || catalog.upgradeStone==null || source.GetCount(catalog.upgradeWood)<wood || source.GetCount(catalog.upgradeStone)<stone) return false;
        if(!ScoreManager.Instance.TrySpendPoints(gold)) return false;
        source.Remove(catalog.upgradeWood,wood); source.Remove(catalog.upgradeStone,stone);
        Advance(Now); level=Level+1; StartWaiting(Now); return true;
    }
    void Update()
    {
        if(TimeManager.Instance!=null) Advance(Now);
        if(Level==4 && linkedSilo!=null && output>0)
        { output-=linkedSilo.Store(output); StartWaiting(Now); }
        else if(Level==4 && linkedStorage!=null && linkedStorage.Available && output>0)
        { output-=linkedStorage.StoreFeed(output); StartWaiting(Now); }
        if(inventory==null) inventory=FindFirstObjectByType<Inventory>();
        if(open) { if(Input.GetKeyDown(KeyCode.Escape)) Close(); return; }
        if(inventory==null || !PlayerInteractionTarget.Contains(inventory.transform,transform)) return;
        WorldInteractionPrompt.Request(this,transform,$"E: Feed Maker Lv.{Level} — {(output>0 ? "Ready" : jobs.Count>0 ? "Processing" : "Idle")}",Vector3.Distance(inventory.transform.position,transform.position),1.5f);
        if(!PlayerInteractionTarget.Press(inventory.transform,transform,KeyCode.E)) return;
        open=true; player=inventory.GetComponent<PlayerController>();
        player?.AcquireMovementLock(this); TimeManager.Instance?.AcquirePause(this); WorldInteractionPrompt.AcquireSuppression(this);
    }
    void Close()
    {
        open=false; player?.ReleaseMovementLock(this); TimeManager.Instance?.ReleasePause(this); WorldInteractionPrompt.ReleaseSuppression(this);
    }
    void OnGUI()
    {
        if(!open) return;
        GUILayout.BeginArea(new Rect(20,130,540,540),GUI.skin.box);
        GUILayout.Label($"FEED MAKER Lv.{Level} | Bahan {Inputs}/{InputCapacity} | Processing Slot {Level}");
        GUILayout.Label($"Animal Feed Ready: {output}/{outputCapacity}");
        if(catalog?.recipes!=null) for(int i=0;i<catalog.recipes.Length;i++)
        {
            var recipe=catalog.recipes[i]; if(recipe?.input==null) continue;
            string label=recipe.mixedInputs!=null && recipe.mixedInputs.Length>0 ? "Mixed Crop" : recipe.input.itemName;
            if(GUILayout.Button($"{label} x{recipe.inputCount} → Feed x{recipe.outputCount} ({recipe.hours/new[]{1f,1.25f,1.6f,2f}[Level-1]:0.##} jam)")) feedback=Queue(inventory,i)?"Bahan masuk antrean":"Bahan kurang / kapasitas penuh";
        }
        foreach(var job in jobs) GUILayout.Label(job.finish<0 ? "Antrean — menunggu slot/output kosong" : $"Processing: {Math.Max(0,job.finish-Now):0.##} jam lagi");
        if(GUILayout.Button("Ambil Animal Feed")) feedback=Collect(inventory)?"Feed masuk tas":"Belum ready / tas penuh";
        if(Level<4 && GUILayout.Button($"Upgrade: {Level*100} Gold + {Level*5} Wood + {Level*3} Stone")) feedback=Upgrade(inventory)?"Mesin diupgrade":"Bahan / uang kurang";
        if(Level==4) GUILayout.Label(linkedSilo!=null ? $"Silo: {linkedSilo.Stock} Feed" : linkedStorage!=null ? $"Output otomatis → {linkedStorage.Label}" : "Hubungkan Linked Storage/Silo di Inspector untuk output otomatis.");
        GUILayout.Label(feedback??"");
        if(GUILayout.Button("Tutup (Esc)")) Close();
        GUILayout.EndArea();
    }
    FeedMakerSaveData Capture()
    {
        var data=new FeedMakerSaveData {id=machineId,level=Level,output=output,position=transform.position,rotation=transform.rotation};
        foreach(var j in jobs) data.jobs.Add(new FeedJob {recipeId=j.recipeId,inputs=j.inputs,output=j.output,hours=j.hours,finish=j.finish});
        return data;
    }
    void Restore(FeedMakerSaveData data)
    {
        level=Mathf.Clamp(data.level,1,4); output=Mathf.Clamp(data.output,0,outputCapacity);
        transform.SetPositionAndRotation(data.position,data.rotation);
        jobs.Clear(); if(data.jobs!=null) foreach(var j in data.jobs)
            jobs.Add(new FeedJob {recipeId=j.recipeId,inputs=j.inputs,output=j.output,hours=j.hours,finish=j.finish});
    }
    public static List<FeedMakerSaveData> CaptureAll()
    {
        foreach(var machine in Active) if(!string.IsNullOrEmpty(machine.machineId)) Cached[machine.machineId]=machine.Capture();
        return new List<FeedMakerSaveData>(Cached.Values);
    }
    public static void RestoreAll(List<FeedMakerSaveData> data)
    {
        Cached.Clear();
        if(data!=null) foreach(var saved in data) if(saved!=null && !string.IsNullOrEmpty(saved.id)) Cached[saved.id]=saved;
        foreach(var machine in Active)
        {
            machine.Close(); machine.jobs.Clear(); machine.output=0;
            if(!string.IsNullOrEmpty(machine.machineId) && Cached.TryGetValue(machine.machineId,out var saved)) machine.Restore(saved);
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Active.Clear(); Cached.Clear(); }
}
