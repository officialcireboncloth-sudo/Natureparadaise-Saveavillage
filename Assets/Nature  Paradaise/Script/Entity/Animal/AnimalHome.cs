using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class AnimalHomeSaveData
{
    public string id;
    public int fodder;
    public int level;
    public int maxCapacity;
    public int currentAnimals;
    public int reservedSlots;
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
    [Header("Auto Feeder")]
    [SerializeField] bool forceAutoFeeder;
    [SerializeField, Min(1)] int autoFeederStartingLevel = 3;
    Transform runtimeDoor;
    Inventory inventory;
    public string Id => site != null ? site.SiteId : homeId;
    public AnimalHousingKind Kind => site != null ? site.ActiveDefinition?.HousingKind ?? AnimalHousingKind.None : kind;
    public int Capacity => site != null ? site.ActiveDefinition?.GetLevel(site.CurrentLevel)?.capacity ?? 0 : capacity;
    public bool Available => site == null || (site.CurrentLevel > 0 && site.State != BuildingConstructionState.Available && Kind != AnimalHousingKind.None);
    public int Fodder => fodderStock;
    public bool HasAutoFeeder => forceAutoFeeder || (site != null && site.CurrentLevel >= autoFeederStartingLevel);
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
    void Awake()
    {
        if (string.IsNullOrWhiteSpace(homeId)) homeId = FormattableString.Invariant($"{gameObject.scene.name}/{name}/{transform.position.x:R}/{transform.position.z:R}");
    }
    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);
    void OnDestroy() { if (runtimeDoor != null) Destroy(runtimeDoor.gameObject); }
    void OnDrawGizmosSelected()
    {
        Vector3 position = door != null ? door.position : Entry;
        Gizmos.color = new Color(1f,0.78f,0.12f,0.95f);
        Gizmos.DrawSphere(position+Vector3.up*0.15f,0.22f);
        Gizmos.DrawLine(position,position+Vector3.up*2f);
    }
    void Update()
    {
        if (runtimeDoor != null) runtimeDoor.gameObject.SetActive(Available);
        if (!Available) return;
        if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
        if (runtimeDoor == null)
        {
            runtimeDoor = new GameObject("AnimalTrough_Runtime").transform;
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(runtimeDoor, false);
            visual.transform.localPosition = new Vector3(1.1f, 0.25f, 0);
            visual.transform.localScale = new Vector3(0.7f, 0.5f, 0.5f);
            visual.GetComponent<Collider>().enabled = false;
        }
        runtimeDoor.position = Entry;
        if (inventory == null || !PlayerInteractionTarget.Contains(inventory.transform, runtimeDoor)) return;
        WorldInteractionPrompt.Request(this, runtimeDoor, $"{Label} {Residents.Count}/{Capacity} | Fodder {fodderStock}\nI: Kelola hewan / isi pakan", 0.5f);
        if (PlayerInteractionTarget.Press(inventory.transform, runtimeDoor, KeyCode.I)) AnimalCarePanel.Show(null, this, inventory);
    }
    public bool Deposit(Inventory source, int amount)
    {
        ItemSO feed = AnimalCareCatalog.Load()?.fodder;
        if (source == null || feed == null || amount <= 0 || amount > 999 - fodderStock || !source.Remove(feed, amount)) return false;
        fodderStock += amount;
        foreach (AnimalRoutine animal in Residents) animal.PrepareDailyCare();
        return true;
    }
    public int StoreFeed(int amount)
    {
        int accepted = Mathf.Clamp(amount, 0, 999 - fodderStock);
        fodderStock += accepted;
        return accepted;
    }
    public bool Withdraw(Inventory target)
    {
        ItemSO feed = AnimalCareCatalog.Load()?.fodder;
        if (target == null || fodderStock <= 0 || feed == null || !target.Add(feed, fodderStock)) return false;
        fodderStock = 0;
        return true;
    }
    public bool Feed(AnimalGrowthSystem animal)
    {
        if (!Available || animal == null || !AnimalCareRules.ConsumeFeed(ref fodderStock, animal.HasBeenBorn, animal.FedToday)) return false;
        animal.RegisterFeeding(50f, HasAutoFeeder ? AnimalFoodSource.AutoFeeder : AnimalFoodSource.FeedingTrough);
        return true;
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
        source.fodderStock = 0;
        foreach (AnimalRoutine routine in AnimalRoutine.Active)
            if (routine != null && routine.HomeId == sourceId) routine.RelocateHome(target.Id);
    }
    public static List<AnimalHomeSaveData> CaptureAll()
    {
        List<AnimalHomeSaveData> result = new();
        foreach (AnimalHome home in Active) if (home != null)
        {
            var saved = new AnimalHomeSaveData { id=home.Id, fodder=home.fodderStock,
                level=home.site!=null ? home.site.CurrentLevel : 1, maxCapacity=home.Capacity,
                currentAnimals=home.AnimalCount, reservedSlots=home.ReservedSlots };
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
            home.fodderStock = Mathf.Clamp(saved?.fodder ?? 0, 0, 999);
        }
    }
}
