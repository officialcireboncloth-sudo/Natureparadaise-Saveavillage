using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Shared house storage; survives interior upgrades and scene unloads.</summary>
public static class HouseStorageService
{
    static readonly List<ItemStack> stacks=new();
    public static event System.Action Changed;
    public static bool IsRestoringSave;
    public static IReadOnlyList<ItemStack> Entries=>stacks;
    public static int Level=>HouseFeatureService.EffectiveLevel;
    public static int Capacity=>Mathf.Max(stacks.Count,Level>=5?64:Level>=4?48:Level>=3?40:Level>=2?32:24);
    public static bool Accepts(ItemSO item)=>item!=null && item.storageDestination!=HomeStorageDestination.Never && (Level==1 || !item.CanStoreInRefrigerator);
    static bool Compatible(ItemStack a,ItemStack b)=>a.item==b.item && a.qualityStars==b.qualityStars && a.fishSizeCm==b.fishSizeCm && a.fishWeightKg==b.fishWeightKg;
    static ItemStack Copy(ItemStack s,int count)=>new ItemStack{item=s.item,count=count,qualityStars=s.qualityStars,fishSizeCm=s.fishSizeCm,fishWeightKg=s.fishWeightKg};
    public static bool CanStore(ItemStack stack)
    {
        if(stack==null || !Accepts(stack.item) || stack.count<=0)return false;
        int space=(Capacity-stacks.Count)*stack.item.StackLimit;
        foreach(var s in stacks)if(Compatible(s,stack))space+=Mathf.Max(0,s.item.StackLimit-s.count);
        return space>=stack.count;
    }
    static void AddPreserved(ItemStack stack)
    {
        int remaining=stack.count;
        foreach(var s in stacks){if(!Compatible(s,stack))continue;int moved=Mathf.Min(remaining,Mathf.Max(0,s.item.StackLimit-s.count));s.count+=moved;remaining-=moved;if(remaining==0)return;}
        while(remaining>0){int moved=Mathf.Min(remaining,stack.item.StackLimit);stacks.Add(Copy(stack,moved));remaining-=moved;}
    }
    public static bool Store(ItemStack stack){if(!CanStore(stack))return false;AddPreserved(stack);Changed?.Invoke();return true;}
    public static ItemStack Remove(int index){var stack=stacks[index];stacks.RemoveAt(index);Changed?.Invoke();return stack;}
    public static void Clear(){stacks.Clear();Changed?.Invoke();}
    public static List<RefrigeratorEntrySaveData> Capture()=>stacks.Select(s=>new RefrigeratorEntrySaveData{itemId=s.item.Id,assetName=s.item.name,itemName=s.item.itemName,count=s.count,qualityStars=s.qualityStars,fishSizeCm=s.fishSizeCm,fishWeightKg=s.fishWeightKg}).ToList();
    public static void Restore(List<RefrigeratorEntrySaveData> saved)
    {
        stacks.Clear();if(saved!=null)foreach(var data in saved){if(data==null||data.count<=0)continue;var item=ItemCatalog.Resolve(data.itemId,data.assetName,data.itemName);if(item!=null)AddPreserved(new ItemStack{item=item,count=data.count,qualityStars=data.qualityStars,fishSizeCm=data.fishSizeCm,fishWeightKg=data.fishWeightKg});}Changed?.Invoke();
    }
    // Migration bypasses new-deposit capacity/filter rules so older saves never discard items.
    public static void SortForHouseLevel()
    {
        if(IsRestoringSave)return;
        foreach(var legacy in ToolStorageService.Entries)if(legacy?.item!=null)AddPreserved(new ItemStack{item=legacy.item,count=legacy.count});
        ToolStorageService.Clear();
        for(int i=RefrigeratorService.Entries.Count-1;i>=0;i--){var e=RefrigeratorService.Entries[i];if(Level>=2&&e.item.CanStoreInRefrigerator)continue;if(RefrigeratorService.Take(i,e.count,out var moved))AddPreserved(moved);}
        if(Level>=2)for(int i=stacks.Count-1;i>=0;i--){var s=stacks[i];if(s.item.CanStoreInRefrigerator&&RefrigeratorService.Store(s.item,s.count,s.qualityStars,s.fishSizeCm,s.fishWeightKg))stacks.RemoveAt(i);}
        Changed?.Invoke();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){stacks.Clear();Changed=null;IsRestoringSave=false;}
}

[DisallowMultipleComponent]
public sealed class HouseStorageChest:MonoBehaviour
{
    static readonly HashSet<HouseStorageChest> active = new();
    public static bool BlocksWorldPointer => active.Any(chest => chest != null && chest.open);
    Inventory inventory;
    bool open => StorageChestUI.Instance != null && StorageChestUI.Instance.HouseChest == this;
    public Inventory PlayerInventory => inventory;
    public string Feedback => feedback;
    Vector2 inventoryScroll,storageScroll;
    Rect rect;
    string feedback;
    void OnEnable(){active.Add(this);}
    public static bool HasCloserStorage(Transform player,float distance)=>active.Any(chest=>chest!=null && PlayerInteractionTarget.ContainsPickup(player,chest.transform,2.2f) && Vector2.Distance(new Vector2(player.position.x,player.position.z),new Vector2(chest.transform.position.x,chest.transform.position.z))<distance);
    void Resolve() {if(inventory==null){var player=FindFirstObjectByType<PlayerController>();if(player!=null)inventory=player.GetComponent<Inventory>();}}
    void Update()
    {
        Resolve();if(inventory==null)return;
        if(open)return;
        if(!PlayerInteractionTarget.ContainsPickup(inventory.transform,transform,2.2f))return;
        var house=GetComponentInParent<HouseInteriorController>();if(house!=null)foreach(var bed in house.GetComponentsInChildren<PlayerBed>())if(bed.CanInteract(inventory.transform))return;
        WorldInteractionPrompt.Request(this,transform,$"E: Storage Rumah | {HouseStorageService.Entries.Count}/{HouseStorageService.Capacity} slot",Vector3.Distance(inventory.transform.position,transform.position),1.4f);
        if(!PlayerInteractionTarget.PressPickup(inventory.transform,transform,KeyCode.E,2.2f))return;
        HouseStorageService.SortForHouseLevel();StorageChestUI.Show(this);
    }
    void OnDisable(){Close();active.Remove(this);}
    void Close(){if(open)StorageChestUI.Instance.Close();}
    public bool TryStore(int slot) => TryStore(slot,int.MaxValue);
    public bool TryStore(int slot,int amount)
    {
        Resolve();var source=inventory!=null?inventory.GetSlot(slot):null;
        if(source==null || amount<=0){feedback="Item tidak dapat disimpan.";return false;}
        var stack=new ItemStack{item=source.item,count=Mathf.Min(amount,source.count),qualityStars=source.qualityStars,fishSizeCm=source.fishSizeCm,fishWeightKg=source.fishWeightKg};
        if(!HouseStorageService.CanStore(stack)){feedback="Storage penuh atau item tidak dapat disimpan.";return false;}
        if(!inventory.RemoveFromSlot(slot,stack.count))return false;
        if(!HouseStorageService.Store(stack)){inventory.Add(stack.item,stack.count,stack.qualityStars,stack.fishSizeCm,stack.fishWeightKg);return false;}
        feedback="Item disimpan.";return true;
    }
    public bool TryTake(int index) => TryTake(index,int.MaxValue);
    public bool TryTake(int index,int amount)
    {
        Resolve();if(inventory==null || index<0 || index>=HouseStorageService.Entries.Count)return false;
        if(amount<=0)return false;
        var source=HouseStorageService.Entries[index];
        var stack=new ItemStack{item=source.item,count=Mathf.Min(amount,source.count),qualityStars=source.qualityStars,fishSizeCm=source.fishSizeCm,fishWeightKg=source.fishWeightKg};
        if(!inventory.CanAdd(stack.item,stack.count,stack.qualityStars,stack.fishSizeCm,stack.fishWeightKg)){feedback="Inventory penuh.";return false;}
        if(!inventory.Add(stack.item,stack.count,stack.qualityStars,stack.fishSizeCm,stack.fishWeightKg))return false;
        source.count-=stack.count;if(source.count<=0)HouseStorageService.Remove(index);feedback="Item diambil.";return true;
    }
    void OnGUI()
    {
        if(StorageChestUI.IsOpen)return;
        if(!open)return;
        float width=Mathf.Min(900,Screen.width-24),height=Mathf.Min(560,Screen.height-24);
        rect=new Rect((Screen.width-width)*.5f,(Screen.height-height)*.5f,width,height);
        GUI.Window(GetInstanceID(),rect,Draw,$"STORAGE RUMAH | {HouseStorageService.Entries.Count}/{HouseStorageService.Capacity} slot");
    }
    void Draw(int id)
    {
        GUILayout.BeginHorizontal();GUILayout.BeginVertical(GUILayout.Width(rect.width*.47f));GUILayout.Label("INVENTORY");
        inventoryScroll=GUILayout.BeginScrollView(inventoryScroll);
        for(int i=0;i<inventory.slots.Count;i++){var s=inventory.GetSlot(i);if(s==null||!HouseStorageService.Accepts(s.item)||s.count<=0)continue;if(GUILayout.Button($"Simpan: {s.DisplayName} x{s.count}",GUILayout.Height(35)))TryStore(i);}
        GUILayout.EndScrollView();GUILayout.EndVertical();GUILayout.BeginVertical();GUILayout.Label("STORAGE");storageScroll=GUILayout.BeginScrollView(storageScroll);
        for(int i=0;i<HouseStorageService.Entries.Count;i++){var s=HouseStorageService.Entries[i];if(GUILayout.Button($"Ambil: {s.DisplayName} x{s.count}",GUILayout.Height(35))){TryTake(i);break;}}
        GUILayout.EndScrollView();GUILayout.EndVertical();GUILayout.EndHorizontal();GUILayout.Label(feedback??"Bahan, makanan, hasil panen, dan alat dapat disimpan.");if(GUILayout.Button("Tutup [E / Esc]"))Close();
    }
}
