using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FeedJob
{
    public string recipeId;
    public int inputs;
    public int output;
    public bool producesFishFeed;
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
    readonly List<FeedJob> jobs = new();
    int output;
    int fishFeedOutput;
    Inventory inventory;
    PlayerController player;
    bool open;
    string feedback;
    Vector2 inventoryScroll;
    Vector2 machineScroll;
    bool fishFeedMode;
    int draggedInputSlot = -1;
    string draggedInputLabel;
    WorldDebugStatusLabel debugLabel;
    bool worldDebugVisible = true;
    public int Level => Mathf.Clamp(level, 1, 4);
    public int InputCapacity => new[] {10,20,40,60}[Level-1];
    public int Inputs { get { int total=0; foreach(var job in jobs) total+=job.inputs; return total; } }
    public int Output => output + fishFeedOutput;
    public int AnimalFeedOutput => output;
    public int FishFeedOutput => fishFeedOutput;
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

    // Event simulation: complete jobs chronologically across sleep, then fill free lanes.
    public void Advance(double now)
    {
        if (double.IsNaN(now) || double.IsInfinity(now)) return;
        int previousJobs=jobs.Count;
        int previousRunning=RunningJobCount();
        int previousOutput=output;
        int previousFishOutput=fishFeedOutput;
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
                    else output+=jobs[i].output;
                    jobs.RemoveAt(i);
                }
            StartWaiting(next);
        }
        StartWaiting(now);
        if(previousJobs!=jobs.Count || previousRunning!=RunningJobCount() ||
           previousOutput!=output || previousFishOutput!=fishFeedOutput)
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
        int running=0, reserved=output+fishFeedOutput;
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
            producesFishFeed=recipe.producesFishFeed, hours=recipe.hours/new[]{1d,1.25d,1.6d,2d}[Level-1]});
        StartWaiting(Now);
        PublishState();
        return true;
    }
    public bool Collect(Inventory target)
    {
        Advance(Now);
        if(output<=0 || target==null || catalog?.animalFeed==null) return false;
        int amount=Math.Min(output,Math.Max(1,catalog.animalFeed.maxStack));
        if(!target.Add(catalog.animalFeed,amount)) return false;
        output-=amount; StartWaiting(Now); PublishState();
        PlayerPickupNotification.ShowItem(target,catalog.animalFeed,amount);
        return true;
    }
    public bool CollectFishFeed(Inventory target)
    {
        Advance(Now);
        if(fishFeedOutput<=0 || target==null || catalog?.fishFeed==null) return false;
        int amount=Math.Min(fishFeedOutput,Math.Max(1,catalog.fishFeed.maxStack));
        if(!target.Add(catalog.fishFeed,amount)) return false;
        fishFeedOutput-=amount; StartWaiting(Now); PublishState();
        PlayerPickupNotification.ShowItem(target,catalog.fishFeed,amount);
        return true;
    }
    public bool OpenFor(Inventory target)
    {
        if(open || target==null) return false;
        Advance(Now);
        inventory=target;
        open=true;
        player=inventory.GetComponent<PlayerController>();
        player?.AcquireMovementLock(this);
        TimeManager.Instance?.AcquirePause(this);
        WorldInteractionPrompt.AcquireSuppression(this);
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
        if(Level==4 && linkedSilo!=null && output>0)
        { output-=linkedSilo.Store(output); StartWaiting(Now); }
        else if(Level==4 && linkedStorage!=null && linkedStorage.Available && output>0)
        { output-=linkedStorage.StoreFeed(output); StartWaiting(Now); }
        RefreshDebugLabel();
        if(inventory==null) inventory=FindFirstObjectByType<Inventory>();
        if(open) { if(Input.GetKeyDown(KeyCode.Escape)) Close(); return; }
        if(inventory==null || !PlayerInteractionTarget.Contains(inventory.transform,transform)) return;
        string state=Output>0 ? $"Ready: Animal {output} | Fish {fishFeedOutput}" : jobs.Count>0 ? "Processing" : "Idle";
        WorldInteractionPrompt.Request(this,transform,$"E: Buka Feed Maker Lv.{Level} — {state}\nF: Ambil hasil siap",Vector3.Distance(inventory.transform.position,transform.position),1.5f);
        if(PlayerInteractionTarget.Press(inventory.transform,transform,KeyCode.F))
        {
            bool animalCollected=Collect(inventory);
            bool fishCollected=CollectFishFeed(inventory);
            bool collected=animalCollected || fishCollected;
            feedback=collected
                ? $"Hasil masuk Inventory: {(animalCollected ? "Animal Feed " : string.Empty)}{(fishCollected ? "Fish Feed" : string.Empty)}"
                : "Feed belum ready / Inventory penuh";
            SaveLoadFeedback.Instance?.ShowMessage(feedback);
            return;
        }
        if(!PlayerInteractionTarget.Press(inventory.transform,transform,KeyCode.E)) return;
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
            int running=0;
            int waiting=0;
            double next=double.PositiveInfinity;
            foreach(FeedJob job in jobs)
            {
                if(job.finish>=0) {running++; next=Math.Min(next,job.finish);}
                else waiting++;
            }
            string estimate=jobs.Count==0 ? (Output>0 ? "SIAP" : "IDLE")
                : running>0 ? GameTimeDebugText.FormatHours(Math.Max(0d,next-Now))
                : "TUNGGU OUTPUT";
            string owner=string.IsNullOrWhiteSpace(contextLabel) ? string.Empty : $" {contextLabel}";
            return $"MESIN PAKAN{owner} Lv.{Level} | {estimate}\nIN {Inputs}/{InputCapacity} | OUT {Output}/{outputCapacity}\n"+
                   $"Animal {output} | Fish {fishFeedOutput}"+
                   (waiting>0 ? $" | Antre {waiting}" : string.Empty);
        }
    }

    public string CompactDebugStatus
    {
        get
        {
            if(Output>0) return "READY";
            FeedJob running=null;
            foreach(FeedJob job in jobs)
                if(job.finish>=0 && (running==null || job.finish<running.finish)) running=job;
            if(running!=null)
            {
                double remaining=Math.Max(0d,running.finish-Now);
                double duration=Math.Max(0.001d,running.hours);
                int percent=Mathf.Clamp(Mathf.RoundToInt((float)((1d-remaining/duration)*100d)),0,100);
                return $"PROCESS {percent}%";
            }
            return jobs.Count>0 ? "PROCESS 0%" : "EMPTY";
        }
    }

    public void SetWorldDebugVisible(bool visible)
    {
        worldDebugVisible=visible;
        if(visible && debugLabel==null) EnsureDebugLabel();
        if(debugLabel!=null) debugLabel.gameObject.SetActive(visible);
    }

    void RefreshDebugLabel()
    {
        if(!worldDebugVisible)
        {
            if(debugLabel!=null) debugLabel.gameObject.SetActive(false);
            return;
        }
        if(debugLabel==null) EnsureDebugLabel();
        if(debugLabel!=null && !debugLabel.gameObject.activeSelf) debugLabel.gameObject.SetActive(true);
        debugLabel?.SetText(DebugStatus);
    }
    void Close()
    {
        open=false; draggedInputSlot=-1; draggedInputLabel=null;
        player?.ReleaseMovementLock(this); TimeManager.Instance?.ReleasePause(this); WorldInteractionPrompt.ReleaseSuppression(this);
    }
    void OnGUI()
    {
        if(!open) return;
        float width=Mathf.Min(920f,Screen.width-24f);
        float height=Mathf.Min(650f,Screen.height-24f);
        GUILayout.BeginArea(new Rect((Screen.width-width)*0.5f,(Screen.height-height)*0.5f,width,height),GUI.skin.box);
        string owner=string.IsNullOrWhiteSpace(contextLabel) ? string.Empty : $" — {contextLabel}";
        GUILayout.Label($"FEED MAKER{owner} Lv.{Level} — INPUT {Inputs}/{InputCapacity} | PROCESS SLOT {Level} | OUTPUT {Output}/{outputCapacity}");
        GUILayout.Label("Drag bahan dari Inventory ke slot mesin. Setelah proses selesai, ambil hasil ke Inventory lalu masukkan ke tempat pakan.");
        GUILayout.BeginHorizontal();
        if(GUILayout.Toggle(!fishFeedMode,"ANIMAL FEED",GUI.skin.button)) fishFeedMode=false;
        if(GUILayout.Toggle(fishFeedMode,"FISH FEED",GUI.skin.button)) fishFeedMode=true;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        DrawInputInventory(width);
        GUILayout.Space(10f);
        DrawMachineSlots(width);
        GUILayout.EndHorizontal();

        if(Level<4 && GUILayout.Button($"Upgrade: {Level*100} Gold + {Level*5} Wood + {Level*3} Stone"))
            feedback=Upgrade(inventory)?"Mesin diupgrade":"Bahan / uang kurang";
        if(Level==4) GUILayout.Label(linkedSilo!=null ? $"Silo: {linkedSilo.Stock} Feed" : linkedStorage!=null ? $"Output otomatis → {linkedStorage.Label}" : "Hubungkan Linked Storage/Silo di Inspector untuk output otomatis.");
        GUILayout.Label(feedback??"");
        if(GUILayout.Button("Tutup (Esc)")) Close();
        DrawDraggedInputGhost();
        GUILayout.EndArea();
    }

    void DrawInputInventory(float panelWidth)
    {
        GUILayout.BeginVertical(GUI.skin.box,GUILayout.Width((panelWidth-34f)*0.5f));
        GUILayout.Label("PLAYER INVENTORY");
        inventoryScroll=GUILayout.BeginScrollView(inventoryScroll,GUILayout.Height(330f));
        bool found=false;
        if(inventory!=null)
        {
            for(int slotIndex=0;slotIndex<inventory.slots.Count;slotIndex++)
            {
                ItemStack stack=inventory.GetSlot(slotIndex);
                int recipeIndex=stack?.item!=null ? FindRecipeIndex(stack.item,fishFeedMode) : -1;
                if(stack?.item==null || stack.count<=0 || recipeIndex<0) continue;
                FeedRecipe recipe=catalog.recipes[recipeIndex];
                found=true;
                GUILayout.BeginHorizontal(GUI.skin.box);
                Rect dragRect=GUILayoutUtility.GetRect(new GUIContent(stack.DisplayName),GUI.skin.box,
                    GUILayout.ExpandWidth(true),GUILayout.Height(48f));
                GUI.Box(dragRect,$"{stack.DisplayName} x{stack.count}\n{recipe.inputCount} → {recipe.outputCount}",GUI.skin.box);
                HandleInputDragSource(dragRect,slotIndex,stack);
                if(GUILayout.Button("+1",GUILayout.Width(46f),GUILayout.Height(48f))) QueueFromSlot(slotIndex,false);
                if(GUILayout.Button("MAX",GUILayout.Width(52f),GUILayout.Height(48f))) QueueFromSlot(slotIndex,true);
                GUILayout.EndHorizontal();
            }
        }
        if(!found) GUILayout.Label(fishFeedMode
            ? "Tidak ada bahan Fish Feed yang cocok."
            : "Tidak ada Grass/crop yang cocok untuk Animal Feed.");
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    void DrawMachineSlots(float panelWidth)
    {
        GUILayout.BeginVertical(GUI.skin.box,GUILayout.Width((panelWidth-34f)*0.5f));
        GUILayout.Label($"FEED MAKER INPUT — {Inputs}/{InputCapacity}");
        Rect targetRect=GUILayoutUtility.GetRect(GUIContent.none,GUI.skin.box,
            GUILayout.ExpandWidth(true),GUILayout.Height(88f));
        Color previous=GUI.color;
        GUI.color=Inputs<InputCapacity ? new Color(0.72f,1f,0.72f) : new Color(1f,0.58f,0.58f);
        GUI.Box(targetRect,Inputs<InputCapacity
            ? $"DROP BAHAN DI SINI\nSisa kapasitas {InputCapacity-Inputs}\nOutput: {(fishFeedMode?"Fish Feed":"Animal Feed")}"
            : $"INPUT PENUH\n{Inputs}/{InputCapacity}");
        GUI.color=previous;
        HandleMachineDrop(targetRect);

        machineScroll=GUILayout.BeginScrollView(machineScroll,GUILayout.Height(205f));
        if(jobs.Count==0) GUILayout.Label("Belum ada bahan di mesin.");
        for(int i=0;i<jobs.Count;i++)
        {
            FeedJob job=jobs[i];
            FeedRecipe recipe=FindRecipe(job.recipeId);
            string ingredient=recipe?.input!=null ? recipe.input.itemName : job.recipeId;
            string product=job.producesFishFeed ? "Fish Feed" : "Animal Feed";
            string state=job.finish<0 ? "MENUNGGU SLOT" : $"PROCESS {Math.Max(0d,job.finish-Now):0.##} jam lagi";
            GUILayout.Label($"SLOT {i+1}: {ingredient} x{job.inputs} → {product} x{job.output}\n{state}",GUI.skin.box);
        }
        GUILayout.EndScrollView();

        GUILayout.Label($"OUTPUT READY — Animal Feed {output} | Fish Feed {fishFeedOutput} | {Output}/{outputCapacity}");
        GUILayout.BeginHorizontal();
        if(GUILayout.Button($"Ambil Animal Feed x{output}",GUILayout.Height(38f)))
            feedback=Collect(inventory)?"Animal Feed masuk Inventory":"Belum ready / Inventory penuh";
        if(GUILayout.Button($"Ambil Fish Feed x{fishFeedOutput}",GUILayout.Height(38f)))
            feedback=CollectFishFeed(inventory)?"Fish Feed masuk Inventory":"Belum ready / Inventory penuh";
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    int FindRecipeIndex(ItemSO item,bool fishMode)
    {
        if(item==null || catalog?.recipes==null) return -1;
        for(int i=0;i<catalog.recipes.Length;i++)
        {
            FeedRecipe recipe=catalog.recipes[i];
            if(recipe?.input==item && recipe.producesFishFeed==fishMode) return i;
        }
        return -1;
    }

    FeedRecipe FindRecipe(string id)
    {
        if(catalog?.recipes==null) return null;
        for(int i=0;i<catalog.recipes.Length;i++)
            if(catalog.recipes[i]!=null && catalog.recipes[i].id==id) return catalog.recipes[i];
        return null;
    }

    void QueueFromSlot(int slotIndex,bool maximum)
    {
        ItemStack stack=inventory?.GetSlot(slotIndex);
        int recipeIndex=stack?.item!=null ? FindRecipeIndex(stack.item,fishFeedMode) : -1;
        if(recipeIndex<0)
        {
            feedback="Bahan ini tidak cocok untuk output yang dipilih.";
            return;
        }
        FeedRecipe recipe=catalog.recipes[recipeIndex];
        int limit=maximum ? Mathf.Max(1,inventory.GetCount(recipe.input)/Mathf.Max(1,recipe.inputCount)) : 1;
        int queued=0;
        while(queued<limit && Queue(inventory,recipeIndex)) queued++;
        feedback=queued>0
            ? $"{recipe.input.itemName} masuk {queued} batch. Input {Inputs}/{InputCapacity}."
            : $"Butuh {recipe.inputCount} {recipe.input.itemName}, atau slot/input mesin sudah penuh.";
    }

    void HandleInputDragSource(Rect rect,int slotIndex,ItemStack stack)
    {
        Event current=Event.current;
        if(current.type!=EventType.MouseDown || current.button!=0 || !rect.Contains(current.mousePosition)) return;
        draggedInputSlot=slotIndex;
        draggedInputLabel=$"{stack.DisplayName} x{stack.count}";
        current.Use();
    }

    void HandleMachineDrop(Rect rect)
    {
        Event current=Event.current;
        if(draggedInputSlot<0 || current.type!=EventType.MouseUp || current.button!=0) return;
        if(rect.Contains(current.mousePosition))
        {
            QueueFromSlot(draggedInputSlot,true);
            current.Use();
        }
        draggedInputSlot=-1;
        draggedInputLabel=null;
    }

    void DrawDraggedInputGhost()
    {
        if(draggedInputSlot<0) return;
        Event current=Event.current;
        if(current.type==EventType.MouseUp)
        {
            draggedInputSlot=-1;
            draggedInputLabel=null;
            return;
        }
        Vector2 mouse=current.mousePosition;
        GUI.Box(new Rect(mouse.x+12f,mouse.y+12f,160f,34f),draggedInputLabel??"Ingredient");
        if(current.type==EventType.MouseDrag) current.Use();
    }
    FeedMakerSaveData Capture()
    {
        var data=new FeedMakerSaveData {id=machineId,level=Level,outputCapacity=outputCapacity,output=output,fishFeedOutput=fishFeedOutput,position=transform.position,rotation=transform.rotation};
        foreach(var j in jobs) data.jobs.Add(new FeedJob {recipeId=j.recipeId,inputs=j.inputs,output=j.output,producesFishFeed=j.producesFishFeed,hours=j.hours,finish=j.finish});
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
        jobs.Clear(); if(data.jobs!=null) foreach(var j in data.jobs)
            jobs.Add(new FeedJob {recipeId=j.recipeId,inputs=j.inputs,output=j.output,producesFishFeed=j.producesFishFeed,hours=j.hours,finish=j.finish});
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
        int reserved=data.output+data.fishFeedOutput;
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
            machine.Close(); machine.jobs.Clear(); machine.output=0; machine.fishFeedOutput=0;
            if(!string.IsNullOrEmpty(machine.machineId) && Cached.TryGetValue(machine.machineId,out var saved)) machine.Restore(saved);
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Active.Clear(); Cached.Clear(); }
}
