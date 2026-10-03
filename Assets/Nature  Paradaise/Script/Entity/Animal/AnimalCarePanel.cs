using UnityEngine;

/// <summary>State and gameplay commands for the left-side animal/home detail UI.</summary>
public sealed class AnimalCarePanel : MonoBehaviour
{
    public static AnimalCarePanel Instance { get; private set; }
    public static bool IsOpen => Instance != null;
    public AnimalGrowthSystem Animal { get; private set; }
    public AnimalHome Home { get; private set; }
    public Inventory Inventory { get; private set; }
    public string EditedName { get; set; }
    public string Feedback { get; private set; }
    public int Revision { get; private set; }
    PlayerController player;
    bool released;
    public static void Show(AnimalGrowthSystem selected, AnimalHome selectedHome, Inventory inv)
    {
        if(inv==null || (selected==null && selectedHome==null) || IsOpen || AnimalController.CurrentCareAction!=null) return;
        Instance=new GameObject("AnimalDetail_Runtime").AddComponent<AnimalCarePanel>();
        Instance.Animal=selected; Instance.Home=selectedHome; Instance.Inventory=inv;
        Instance.EditedName=selected!=null?selected.AnimalName:string.Empty;
        Instance.player=inv.GetComponent<PlayerController>();
        Instance.player?.AcquireMovementLock(Instance);
        TimeManager.Instance?.AcquirePause(Instance);
        WorldInteractionPrompt.AcquireSuppression(Instance);
        if(inv.GetComponent<AnimalInteractionHUD>()==null) inv.gameObject.AddComponent<AnimalInteractionHUD>();
    }
    void Update()
    {
        if(GameplayInput.GetKeyDown(KeyCode.Escape) || (Animal==null && Home==null)) Close();
    }
    public void Close()
    {
        Release(); GameplayInput.ConsumeCurrentFrame(); Destroy(gameObject);
    }
    void Release()
    {
        if(released) return; released=true;
        player?.ReleaseMovementLock(this);TimeManager.Instance?.ReleasePause(this);
        WorldInteractionPrompt.ReleaseSuppression(this);
        if(Instance==this) Instance=null;
    }
    void OnDisable()=>Release();
    void OnDestroy()=>Release();
    public void Select(AnimalGrowthSystem selected)
    {Animal=selected;EditedName=selected!=null?selected.AnimalName:string.Empty;Feedback="";Revision++;}
    public void Rename()
    {if(Animal==null)return;Animal.SetAnimalName(EditedName);EditedName=Animal.AnimalName;Feedback="Nama disimpan.";Revision++;}
    AnimalController Controller()
    {
        var controller=Animal!=null?Animal.GetComponent<AnimalController>():null;
        if(controller!=null)controller.playerInv=Inventory;
        return controller;
    }
    public void Feed()
    {
        var controller=Controller(); if(controller==null)return;
        bool before=Animal.FedToday;controller.FeedCabbage();
        Feedback=!before && Animal.FedToday?"Hewan diberi makan.":"Sudah makan hari ini atau Animal Feed tidak tersedia.";
    }
    public void Brush()
    {
        if(Controller()?.TryStartBrush()==true)Close();
        else Feedback="Sudah digosok hari ini atau hewan sedang melakukan aktivitas lain.";
    }
    public void Collect()
    {
        var controller=Controller();if(controller==null)return;
        controller.TakeMilk();if(controller.IsCareBusy)Close();
        else Feedback=Animal.Type==AnimalType.Sheep?"Produk belum siap atau Shears belum dipilih.":"Produk belum siap.";
    }
    public void Interact()
    {if(Animal!=null)Feedback=Animal.Interact()?"Interaksi harian selesai.":"Interaksi sudah dihitung hari ini.";}
    public void Treat()=>Feedback=Controller()?.TryGiveTreat()==true?"Treat diberikan.":"Treat tidak tersedia atau sudah diberikan hari ini.";
    public void Medicine()=>Feedback=Controller()?.TryGiveBestMedicine()==true?"Obat diberikan; kondisi hewan diperbarui.":"Obat tidak tersedia atau hewan belum memerlukan obat.";
    public void Breed()
    {
        var shop=FindFirstObjectByType<ShopManager>();
        Feedback=Animal!=null && shop!=null && shop.TryStartBreeding(Animal,Inventory)?"Proses dimulai; slot kandang direservasi.":
            "Perlu induk dewasa, sehat, sudah makan, dan slot kosong. Inkubasi juga memerlukan telur.";
        Revision++;
    }
    public void Assign(AnimalHome destination)
    {
        var routine=Animal!=null?Animal.GetComponent<AnimalRoutine>():null;
        Feedback=routine!=null && destination!=null && routine.Assign(destination)?"Penugasan kandang diperbarui.":"Hewan tidak dapat dipindahkan ke kandang ini.";
        Revision++;
    }
}
