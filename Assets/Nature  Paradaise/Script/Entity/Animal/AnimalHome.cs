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
    [Header("Animal Bell State")]
    [SerializeField] bool bellStateInitialized;
    [SerializeField] bool animalsOutside;
    [Header("Auto Feeder")]
    [SerializeField] bool forceAutoFeeder;
    [SerializeField, Min(1)] int autoFeederStartingLevel = 3;
    [Header("Feeding Trough")]
    [SerializeField, Min(1f)] float troughInteractionRadius = 2.25f;
    Transform runtimeTrough;
    AnimalBellStation runtimeBellStation;
    Inventory inventory;
    public string Id => site != null ? site.SiteId : homeId;
    public AnimalHousingKind Kind => site != null ? site.ActiveDefinition?.HousingKind ?? AnimalHousingKind.None : kind;
    public int Capacity => site != null ? site.ActiveDefinition?.GetLevel(site.CurrentLevel)?.capacity ?? 0 : capacity;
    public bool Available => site == null || (site.CurrentLevel > 0 && site.State != BuildingConstructionState.Available && Kind != AnimalHousingKind.None);
    public int Fodder => fodderStock;
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
    void Awake()
    {
        if (string.IsNullOrWhiteSpace(homeId)) homeId = FormattableString.Invariant($"{gameObject.scene.name}/{name}/{transform.position.x:R}/{transform.position.z:R}");
    }
    void OnEnable()
    {
        Active.Add(this);
        // Hewan menilai care hari lama pada OnBeforeDayChange. Trough baru dikosongkan
        // setelah kalender berganti agar urutan subscription object tidak mengubah hasil.
        TimeManager.OnDay += EmptyTroughAtMidnight;
    }
    void OnDisable()
    {
        Active.Remove(this);
        TimeManager.OnDay -= EmptyTroughAtMidnight;
    }
    void OnDestroy() { if (runtimeTrough != null) Destroy(runtimeTrough.gameObject); }
    void OnDrawGizmosSelected()
    {
        Vector3 position = door != null ? door.position : Entry;
        Gizmos.color = new Color(1f,0.78f,0.12f,0.95f);
        Gizmos.DrawSphere(position+Vector3.up*0.15f,0.22f);
        Gizmos.DrawLine(position,position+Vector3.up*2f);
    }
    void Update()
    {
        if (runtimeTrough != null) runtimeTrough.gameObject.SetActive(Available);
        if (!Available) return;
        if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
        if (runtimeTrough == null)
        {
            runtimeTrough = new GameObject("AnimalTrough_Runtime").transform;
            runtimeTrough.SetParent(transform, true);
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "FeedingTrough_Visual";
            visual.transform.SetParent(runtimeTrough, false);
            visual.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            visual.transform.localScale = new Vector3(1.6f, 0.55f, 0.8f);
            visual.GetComponent<Collider>().enabled = false;
            Renderer renderer = visual.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (renderer != null && shader != null)
                renderer.material = new Material(shader) { color = new Color(0.34f, 0.2f, 0.08f) };

            GameObject labelObject = new("FeedingTrough_Label");
            labelObject.transform.SetParent(runtimeTrough, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = "FEED";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 42;
            label.characterSize = 0.09f;
            label.color = Color.white;
        }
        if (runtimeBellStation == null)
            runtimeBellStation = AnimalBellStation.Create(this);
        Transform building = site != null && site.BuildingAnchor != null ? site.BuildingAnchor : transform;
        runtimeTrough.position = Entry + building.right * 5f;
        runtimeTrough.rotation = Quaternion.Euler(0f, building.eulerAngles.y, 0f);
        if (inventory == null || !PlayerInteractionTarget.ContainsPickup(inventory.transform, runtimeTrough, troughInteractionRadius)) return;
        float distance = Vector3.Distance(inventory.transform.position, runtimeTrough.position);
        WorldInteractionPrompt.Request(this, runtimeTrough,
            $"E: Tempat Pakan {Label}\nStok {fodderStock} | Belum makan {RequiredFeedToday}", distance, 1.1f);
        if (PlayerInteractionTarget.PressPickup(inventory.transform, runtimeTrough, KeyCode.E, troughInteractionRadius))
            AnimalCarePanel.Show(null, this, inventory);
    }
    public bool Deposit(Inventory source, int amount)
    {
        ItemSO feed = AnimalCareCatalog.Load()?.fodder;
        if (source == null || feed == null || amount <= 0 || amount > 999 - fodderStock || !source.Remove(feed, amount)) return false;
        fodderStock += amount;
        // Hewan outdoor harus mencoba grass terlebih dahulu. Hanya penghuni yang sedang
        // berada di dalam kandang yang langsung makan dari trough ketika stok ditambah.
        foreach (AnimalRoutine animal in Residents)
            if (animal.IsHoused) animal.PrepareDailyCare();
        return true;
    }

    public bool IsTroughPlayerInRange(Transform playerTransform)
    {
        if (playerTransform == null || runtimeTrough == null) return false;
        Vector3 delta = runtimeTrough.position - playerTransform.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= troughInteractionRadius * troughInteractionRadius;
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
    void EmptyTroughAtMidnight()
    {
        fodderStock = 0;
        // Daily transition selalu memulangkan penghuni kandang.
        animalsOutside = false;
        bellStateInitialized = true;
    }
    public void SetAnimalsOutsideState(bool outside)
    {
        animalsOutside = outside;
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
        source.fodderStock = 0;
        foreach (AnimalRoutine routine in AnimalRoutine.Active)
            if (routine != null && routine.HomeId == sourceId) routine.RelocateHome(target.Id);
    }
    public static List<AnimalHomeSaveData> CaptureAll()
    {
        List<AnimalHomeSaveData> result = new();
        foreach (AnimalHome home in Active) if (home != null)
        {
            home.EnsureBellState();
            var saved = new AnimalHomeSaveData { id=home.Id, fodder=home.fodderStock,
                level=home.site!=null ? home.site.CurrentLevel : 1, maxCapacity=home.Capacity,
                currentAnimals=home.AnimalCount, reservedSlots=home.ReservedSlots,
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
            home.fodderStock = Mathf.Clamp(saved?.fodder ?? 0, 0, 999);
            home.bellStateInitialized = saved?.hasBellState ?? false;
            home.animalsOutside = saved?.animalsOutside ?? false;
        }
    }
}
