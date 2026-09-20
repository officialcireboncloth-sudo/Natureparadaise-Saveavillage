using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class AnimalHomeSaveData
{
    public string id;
    public int fodder;
    public int grass;
    public int level;
    public int maxCapacity;
    public int currentAnimals;
    public int reservedSlots;
    public int feedPortionsReserved;
    public bool hasBellState;
    public bool animalsOutside;
    public List<string> animalIds = new();
    public List<AnimalType> animalTypes = new();
}

/// <summary>Kandang persisten dan trough. PropertySite memberi jenis, kapasitas, serta lokasi bangunan.</summary>
public sealed class AnimalHome : MonoBehaviour
{
    public static readonly List<AnimalHome> Active = new();
    public string homeId;
    public AnimalHousingKind kind = AnimalHousingKind.Barn;
    [Min(1)] public int capacity = 4;
    public Transform door;
    public PropertySite site;
    [SerializeField, Min(0)] int fodderStock;
    [SerializeField, Min(0)] int grassStock;
    [SerializeField, Min(0)] int feedPortionsReserved;
    [Header("Animal Bell State")]
    [SerializeField] bool bellStateInitialized;
    [SerializeField] bool animalsOutside;
    [Header("Auto Feeder")]
    [SerializeField] bool forceAutoFeeder;
    [SerializeField, Min(1)] int autoFeederStartingLevel = 3;
    AnimalBellStation runtimeBellStation;
    public string Id => site != null ? site.SiteId : homeId;
    public AnimalHousingKind Kind => site != null ? site.ActiveDefinition?.HousingKind ?? AnimalHousingKind.None : kind;
    public int Capacity => site != null ? site.ActiveDefinition?.GetLevel(site.CurrentLevel)?.capacity ?? 0 : capacity;
    /// <summary>
    /// Satu kompartemen tempat makan menyediakan satu slot penghuni. Dengan begitu
    /// kapasitas kandang selalu sama dengan jumlah slot trough pada level aktif.
    /// </summary>
    public int FeedingSlotCapacity => Capacity;
    public bool Available => site == null || (site.CurrentLevel > 0 && site.State != BuildingConstructionState.Available && Kind != AnimalHousingKind.None);
    public int Fodder => fodderStock;
    public int Grass => grassStock;
    public int TotalFeed => fodderStock + grassStock;
    public int FeedSpace => Mathf.Max(0, FeedingSlotCapacity - TotalFeed);
    public int FeedPortionsReserved => Mathf.Clamp(feedPortionsReserved,0,TotalFeed);
    public int DailyFeedRequirement => AnimalCount;
    public int EstimatedFeedDays => AnimalCount<=0 ? 0 : Mathf.CeilToInt(TotalFeed/(float)AnimalCount);
    public int RequiredFeedToday => Residents.FindAll(animal =>
        animal != null && animal.Animal != null && animal.Animal.HasBeenBorn && !animal.Animal.FedToday).Count;
    public bool HasAutoFeeder => forceAutoFeeder || (site != null && site.CurrentLevel >= autoFeederStartingLevel);
    public AnimalBellStation BellStation => runtimeBellStation;
    public bool AnimalsOutside
    {
        get
        {
            EnsureBellState();
            return animalsOutside;
        }
    }
    public string Label => site != null ? site.ActiveDefinition?.displayName ?? name : name;
    public List<AnimalRoutine> Residents => AnimalRoutine.Active.FindAll(a => a != null && a.HomeId == Id);
    // Prenatal animals already have persistent IDs and growth/save data. They reserve one slot.
    public int AnimalCount => Residents.FindAll(a => a.Animal != null && a.Animal.HasBeenBorn).Count;
    public int ReservedSlots => Residents.FindAll(a => a.Animal != null && !a.Animal.HasBeenBorn).Count;
    public int AvailableSlots => Mathf.Max(0, Capacity - Residents.Count);
    public bool Accepts(AnimalType type) => Available && Kind == (AnimalGrowthProfileSO.IsBird(type) ? AnimalHousingKind.Coop : AnimalHousingKind.Barn);
    public bool HasRoom(AnimalType type) => Available && AnimalCareRules.Fits(AnimalGrowthProfileSO.IsBird(type), Kind, Residents.Count, Capacity);
    public Vector3 Entry
    {
        get
        {
            if (door != null) return door.position;
            Transform anchor = site != null ? site.BuildingAnchor : transform;
            Bounds bounds = new(anchor.position, Vector3.one * 2f);
            bool found = false;
            Renderer doorRenderer = null;
            foreach (Renderer renderer in anchor.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || renderer is LineRenderer) continue;
                if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds);
                string childName = renderer.name.ToLowerInvariant();
                if (doorRenderer == null && (childName.Contains("door") || childName.Contains("entrance")))
                    doorRenderer = renderer;
            }
            Vector3 point;
            if (doorRenderer != null)
            {
                point = doorRenderer.bounds.center;
                Vector3 outward = point - bounds.center;
                outward.y = 0f;
                if (outward.sqrMagnitude < 0.01f) outward = -anchor.forward;
                point += outward.normalized * 1.25f;
            }
            else
            {
                // Fallback menghormati rotasi bangunan; bounds.min.z dunia tidak dapat dipakai
                // karena menghasilkan sisi yang salah ketika Property Site diputar.
                Vector3 localCenter = anchor.InverseTransformPoint(bounds.center);
                Vector3 localCorner = anchor.InverseTransformPoint(new Vector3(bounds.center.x,bounds.center.y,bounds.min.z));
                float halfDepth = Mathf.Max(1f,Mathf.Abs(localCorner.z-localCenter.z));
                point = anchor.TransformPoint(new Vector3(localCenter.x,0f,localCenter.z-halfDepth-1.2f));
            }
            if (Physics.Raycast(point + Vector3.up * 4f, Vector3.down, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore)) point.y = hit.point.y;
            else point.y = anchor.position.y;
            return point;
        }
    }

    /// <summary>Titik aman di luar collider kandang untuk hasil saklar bell.</summary>
    public Vector3 OutdoorReleasePosition(int groupIndex = 0)
    {
        Transform anchor = site != null ? site.BuildingAnchor : transform;
        Vector3 entry = Entry;
        Vector3 outward = entry - anchor.position;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.01f) outward = -anchor.forward;
        outward.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, outward);
        float lateral = ((groupIndex % 5) - 2) * 0.75f;
        float row = groupIndex / 5 * 0.9f;
        Vector3 position = entry + outward * (2.25f + row) + side * lateral;
        if (Physics.Raycast(position + Vector3.up * 6f, Vector3.down, out RaycastHit hit, 14f,
            ~0, QueryTriggerInteraction.Ignore)) position.y = hit.point.y;
        else position.y = entry.y;
        return position;
    }
    void Awake()
    {
        if (string.IsNullOrWhiteSpace(homeId)) homeId = FormattableString.Invariant($"{gameObject.scene.name}/{name}/{transform.position.x:R}/{transform.position.z:R}");
    }
    void OnEnable()
    {
        Active.Add(this);
        // Setiap hewan yang benar-benar makan dari trough mencadangkan satu box.
        // Box yang dicadangkan dikonsumsi setelah kalender berganti; sisanya menetap.
        TimeManager.OnDay += ConsumeReservedFeedAtMidnight;
    }
    void OnDisable()
    {
        Active.Remove(this);
        TimeManager.OnDay -= ConsumeReservedFeedAtMidnight;
    }
    void OnDrawGizmosSelected()
    {
        Vector3 position = door != null ? door.position : Entry;
        Gizmos.color = new Color(1f,0.78f,0.12f,0.95f);
        Gizmos.DrawSphere(position+Vector3.up*0.15f,0.22f);
        Gizmos.DrawLine(position,position+Vector3.up*2f);
    }
    void Update()
    {
        if (!Available) return;
        if (runtimeBellStation == null)
            runtimeBellStation = AnimalBellStation.Create(this);
    }
    public bool Deposit(Inventory source, int amount)
    {
        ItemSO feed = AnimalCareCatalog.Load()?.fodder;
        int accepted = source == null || feed == null || amount <= 0
            ? 0
            : Mathf.Min(1, FeedSpace, source.GetCount(feed));
        if (accepted <= 0 || !source.Remove(feed, accepted)) return false;
        fodderStock += accepted;
        PrepareHousedAnimals();
        return true;
    }

    /// <summary>
    /// Memindahkan Grass mentah atau Animal Feed dari slot inventory tertentu ke trough.
    /// Nilai balik adalah jumlah yang benar-benar masuk setelah dibatasi kapasitas.
    /// </summary>
    public int DepositFromSlot(Inventory source, int slotIndex, int amount)
    {
        ItemStack stack = source != null ? source.GetSlot(slotIndex) : null;
        if (stack?.item == null || stack.count <= 0 || amount <= 0 || FeedSpace <= 0)
            return 0;

        bool processed = IsProcessedFeed(stack.item);
        bool rawGrass = IsRawGrass(stack.item);
        if (!processed && !rawGrass) return 0;

        // Satu interaksi mengisi tepat satu kompartemen trough.
        int accepted = Mathf.Min(1, stack.count, FeedSpace);
        if (accepted <= 0 || !source.RemoveFromSlot(slotIndex, accepted)) return 0;
        if (processed) fodderStock += accepted;
        else grassStock += accepted;
        PrepareHousedAnimals();
        return accepted;
    }

    public bool AcceptsFeedItem(ItemSO item) => IsProcessedFeed(item) || IsRawGrass(item);

    static bool IsProcessedFeed(ItemSO item)
    {
        ItemSO configured = AnimalCareCatalog.Load()?.fodder;
        return item != null && (item == configured || item.itemId == "item.animal_feed");
    }

    static bool IsRawGrass(ItemSO item) => item != null && item.itemId == "item.grass";

    void PrepareHousedAnimals()
    {
        // Hewan outdoor mencoba grazing lebih dahulu. Penghuni di dalam kandang
        // dapat langsung mengambil satu unit dari trough yang baru diisi.
        foreach (AnimalRoutine animal in Residents)
            if (animal.IsHoused) animal.PrepareDailyCare();
    }

    public int StoreFeed(int amount)
    {
        int accepted = Mathf.Clamp(amount, 0, FeedSpace);
        fodderStock += accepted;
        return accepted;
    }

    /// <summary>Mengisi satu kompartemen trough dari pakan yang diangkut player.</summary>
    public bool StoreCarriedFeedUnit()
    {
        if(!Available || FeedSpace<=0) return false;
        fodderStock++;
        PrepareHousedAnimals();
        return true;
    }
    public bool Withdraw(Inventory target, bool rawGrass = false)
    {
        ItemSO feed = rawGrass
            ? Resources.Load<ItemSO>("Items/Materials/Grass")
            : AnimalCareCatalog.Load()?.fodder;
        int stock = rawGrass ? grassStock : fodderStock;
        if (target == null || stock <= 0 || feed == null || !target.Add(feed, stock)) return false;
        if (rawGrass) grassStock = 0;
        else fodderStock = 0;
        return true;
    }
    public bool Feed(AnimalGrowthSystem animal)
    {
        if (!Available || animal == null || !animal.HasBeenBorn || animal.FedToday) return false;

        // Isi trough mewakili box terisi dan baru dikosongkan pada 00:00. Jangan
        // mengurangi stok saat routine hewan mengecek makan, karena secara visual
        // pakan akan tampak hilang sesaat setelah player menaruhnya.
        // Hewan yang sudah makan dari grass/hand-feed tidak mencadangkan box.
        // Hanya porsi yang benar-benar diberikan oleh trough yang dihitung di sini.
        if(feedPortionsReserved>=TotalFeed) return false;

        AnimalFoodSource source=HasAutoFeeder ? AnimalFoodSource.AutoFeeder : AnimalFoodSource.FeedingTrough;
        bool registered=animal.RegisterFeeding(50f,source);
        if(registered) feedPortionsReserved=Mathf.Min(TotalFeed,feedPortionsReserved+1);
        return registered;
    }
    void ConsumeReservedFeedAtMidnight()
    {
        int remaining=Mathf.Clamp(feedPortionsReserved,0,TotalFeed);
        int usedFodder=Mathf.Min(fodderStock,remaining);
        fodderStock-=usedFodder;
        remaining-=usedFodder;
        if(remaining>0) grassStock=Mathf.Max(0,grassStock-remaining);
        feedPortionsReserved=0;
        // Daily transition selalu memulangkan penghuni kandang.
        animalsOutside = false;
        bellStateInitialized = true;
    }
    public void SetAnimalsOutsideState(bool outside)
    {
        animalsOutside = outside;
        bellStateInitialized = true;
    }
    public void SynchronizeBellStateFromResidents()
    {
        List<AnimalRoutine> born = Residents.FindAll(routine =>
            routine != null && routine.Animal != null && routine.Animal.HasBeenBorn);
        if (born.Count == 0) return;
        bool allInside = born.TrueForAll(routine => routine.IsHoused);
        bool allOutside = born.TrueForAll(routine => !routine.IsHoused && !routine.Returning);
        if (allInside) animalsOutside = false;
        else if (allOutside) animalsOutside = true;
        bellStateInitialized = true;
    }
    void EnsureBellState()
    {
        if (bellStateInitialized) return;
        List<AnimalRoutine> bornResidents = Residents.FindAll(routine =>
            routine != null && routine.Animal != null && routine.Animal.HasBeenBorn);
        animalsOutside = bornResidents.Count > 0 &&
                         bornResidents.TrueForAll(routine => !routine.IsHoused && !routine.Returning);
        bellStateInitialized = true;
    }
    public static bool HasResidents(string id) => AnimalRoutine.Active.Exists(a => a != null && a.HomeId == id);
    public static AnimalHome Find(string id) => Active.Find(h => h != null && h.Available && h.Id == id);
    public static AnimalHome FindVacancy(AnimalType type) => Active.Find(h => h != null && h.HasRoom(type));
    public static void Transfer(string sourceId, PropertySite destination)
    {
        AnimalHome source = Active.Find(home => home != null && home.Id == sourceId);
        if (source == null) return;
        AnimalHome target = destination.GetComponent<AnimalHome>();
        if (target == null) target = destination.gameObject.AddComponent<AnimalHome>();
        target.site = destination;
        target.fodderStock = source.fodderStock;
        target.grassStock = source.grassStock;
        target.feedPortionsReserved = source.feedPortionsReserved;
        source.fodderStock = 0;
        source.grassStock = 0;
        source.feedPortionsReserved = 0;
        foreach (AnimalRoutine routine in AnimalRoutine.Active)
            if (routine != null && routine.HomeId == sourceId) routine.RelocateHome(target.Id);
    }
    public static List<AnimalHomeSaveData> CaptureAll()
    {
        List<AnimalHomeSaveData> result = new();
        foreach (AnimalHome home in Active) if (home != null)
        {
            home.EnsureBellState();
            var saved = new AnimalHomeSaveData { id=home.Id, fodder=home.fodderStock, grass=home.grassStock,
                level=home.site!=null ? home.site.CurrentLevel : 1, maxCapacity=home.Capacity,
                currentAnimals=home.AnimalCount, reservedSlots=home.ReservedSlots,
                feedPortionsReserved=home.feedPortionsReserved,
                hasBellState=true, animalsOutside=home.animalsOutside };
            foreach(var resident in home.Residents) if(resident.Animal!=null)
            { saved.animalIds.Add(resident.Animal.AnimalId); saved.animalTypes.Add(resident.Animal.Type); }
            result.Add(saved);
        }
        return result;
    }
    public static void RestoreAll(List<AnimalHomeSaveData> data)
    {
        AnimalHusbandrySystem.Scan();
        foreach (AnimalHome home in Active)
        {
            AnimalHomeSaveData saved = data?.Find(d => d.id == home.Id);
            home.fodderStock = Mathf.Clamp(saved?.fodder ?? 0, 0, home.FeedingSlotCapacity);
            home.grassStock = Mathf.Clamp(saved?.grass ?? 0, 0,
                Mathf.Max(0, home.FeedingSlotCapacity - home.fodderStock));
            home.feedPortionsReserved = Mathf.Clamp(saved?.feedPortionsReserved ?? 0,0,home.TotalFeed);
            home.bellStateInitialized = saved?.hasBellState ?? false;
            home.animalsOutside = saved?.animalsOutside ?? false;
        }
    }
}
