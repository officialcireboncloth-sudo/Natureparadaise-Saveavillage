using System.Collections.Generic;
using UnityEngine;

/// <summary>Penugasan kandang, perjalanan merumput, dan boarding; tidak menonaktifkan daily/save hewan.</summary>
public sealed class AnimalRoutine : MonoBehaviour
{
    public static readonly List<AnimalRoutine> Active = new();
    [SerializeField] string homeId;
    [SerializeField] bool housed;
    [SerializeField] bool returning;
    [Min(0.1f)] public float walkSpeed = 1.5f;
    [Min(1f)] public float grazingSearchRadius = 20f;
    public string HomeId => homeId;
    public bool IsHoused => housed;
    public bool Returning => returning;
    public AnimalHome Home => AnimalHome.Find(homeId);
    public AnimalGrowthSystem Animal { get; private set; }
    List<Vector3> path;
    int waypoint;
    WorldGatherable grass;
    TerrainDetailGrassManager terrainGrass;
    int terrainGrassIndex = -1;
    Rigidbody body;
    bool originalKinematic;
    float nextPathTime;
    Vector3 pathDestination;
    readonly Dictionary<Renderer, bool> renderers = new();
    readonly Dictionary<Collider, bool> colliders = new();
    public string Activity { get; private set; } = "Idle";
    void Awake()
    {
        Animal = GetComponent<AnimalGrowthSystem>();
        body = GetComponent<Rigidbody>();
        if (body != null) originalKinematic = body.isKinematic;
    }
    void OnEnable()
    {
        Active.Add(this); TimeManager.OnDay += Overnight;
        if (body != null) body.isKinematic = true;
    }
    void OnDisable()
    {
        Active.Remove(this); TimeManager.OnDay -= Overnight; ShowModel();
        if (body != null) body.isKinematic = originalKinematic;
    }
    public bool Assign(AnimalHome home)
    {
        if (home == null || !home.HasRoom(Animal.Type)) return false;
        if (housed) { transform.position = Home != null ? Home.Entry : transform.position; ShowModel(); }
        homeId = home.Id; housed = false; returning = true; path = null;
        Animal.SetSheltered(false);
        return true;
    }
    public bool Release()
    {
        int hour = TimeManager.Instance != null ? TimeManager.Instance.hour : 6;
        if (!AnimalCareRules.CanTurnOut(Animal.HasBeenBorn, Animal.Health == AnimalHealthState.Healthy, Animal.CanGrazeToday, hour, Home != null, housed)) return false;
        transform.position = Home.Entry;
        housed = returning = false;
        ClearGrassTarget(); path = null; nextPathTime = 0;
        ShowModel(); Animal.SetSheltered(false);
        return true;
    }
    public void Recall() { if (!housed && Home != null) { returning = true; path = null; nextPathTime = 0; } }
    public void PrepareDailyCare() { if (housed) Home?.Feed(Animal); }
    void Overnight()
    {
        // Pergantian hari mewakili perjalanan pulang saat waktu tidur dilewati.
        if (Home != null) Board();
    }
    void Board()
    {
        if (Home == null) return;
        transform.position = Home.Entry;
        housed = true; returning = false; path = null; ClearGrassTarget();
        Animal.SetSheltered(true); Activity = "Di kandang";
        PrepareDailyCare();
    }
    void Update()
    {
        if (Time.timeScale <= 0 || (TimeManager.Instance != null && TimeManager.Instance.IsPaused) || Animal == null) return;
        if (string.IsNullOrEmpty(homeId))
        {
            AnimalHome available = AnimalHome.FindVacancy(Animal.Type);
            if (available != null) Assign(available);
        }
        AnimalHome home = Home;
        if (home == null)
        {
            if (housed) ShowModel();
            housed = false; Animal.SetSheltered(false); Activity = "Belum punya kandang";
            return;
        }
        if (housed) { Animal.SetSheltered(true); PrepareDailyCare(); Activity = "Di kandang"; return; }
        Animal.SetSheltered(false);
        int hour = TimeManager.Instance != null ? TimeManager.Instance.hour : 6;
        if (hour >= 18 || hour < 6 || !Animal.CanGrazeToday || !Animal.HasBeenBorn || Animal.Health != AnimalHealthState.Healthy) RecallIfNeeded();
        Vector3 destination;
        if (returning)
        {
            destination = home.Entry; Activity = "Pulang ke kandang";
            if (Vector3.Distance(transform.position, destination) < 1f) { Board(); return; }
        }
        else
        {
            if (Animal.GrazedToday) { Activity = "Merumput / beristirahat"; return; }
            if (grass != null && !grass.IsAvailable) grass = null;
            if (terrainGrass != null && !terrainGrass.IsPatchAvailable(terrainGrassIndex))
            {
                terrainGrass = null;
                terrainGrassIndex = -1;
            }
            if (grass == null && terrainGrass == null)
            {
                grass = FindGrass(home.Entry);
                if (grass == null)
                    TerrainDetailGrassManager.TryFindNearest(transform.position, home.Entry, grazingSearchRadius,
                        out terrainGrass, out terrainGrassIndex, out _);
                path = null;
            }
            if (grass == null && terrainGrass == null) { Activity = "Tidak ada rumput tersedia"; return; }
            Vector3 grassPosition = grass != null
                ? grass.transform.position : terrainGrass.GetPatchPosition(terrainGrassIndex);
            Vector3 delta = transform.position - grassPosition; delta.y = 0;
            if (delta.magnitude < 1.35f)
            {
                bool grazed = grass != null ? grass.TryGraze() : terrainGrass.TryGraze(terrainGrassIndex);
                if (grazed) Animal.RegisterGrazing();
                ClearGrassTarget(); path = null; return;
            }
            destination = grassPosition + (delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.forward);
            Activity = "Menuju rumput";
        }
        if (Time.time >= nextPathTime && (path == null || (destination - pathDestination).sqrMagnitude > 1f))
        {
            nextPathTime = Time.time + 3f + (GetInstanceID() & 7) * 0.1f;
            path = AnimalWalkingPath.Find(transform.position, destination, transform);
            waypoint = 0; pathDestination = destination;
        }
        if (path == null) { Activity = "Jalur terhalang"; return; }
        if (waypoint >= path.Count) { path = null; return; }
        Vector3 next = Vector3.MoveTowards(transform.position, path[waypoint], walkSpeed * Time.deltaTime);
        if (!AnimalWalkingPath.Ground(next, transform, out Vector3 ground)) { path = null; return; }
        Vector3 direction = ground - transform.position; direction.y = 0;
        if (direction.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 6f);
        transform.position = ground;
        if (Vector3.Distance(transform.position, path[waypoint]) < 0.12f) waypoint++;
    }
    void RecallIfNeeded() { if (!returning) Recall(); }
    void ClearGrassTarget() { grass = null; terrainGrass = null; terrainGrassIndex = -1; }
    WorldGatherable FindGrass(Vector3 origin)
    {
        WorldGatherable best = null; float bestDistance = float.PositiveInfinity;
        foreach (WorldGatherable candidate in WorldGatherable.Active)
        {
            if (candidate == null || candidate.Kind != GatherableKind.Grass || !candidate.IsAvailable ||
                Vector3.Distance(origin, candidate.transform.position) > grazingSearchRadius) continue;
            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance < bestDistance) { best = candidate; bestDistance = distance; }
        }
        return best;
    }
    void LateUpdate()
    {
        if (!housed) return;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        { if (!renderers.ContainsKey(renderer)) renderers[renderer] = renderer.enabled; renderer.enabled = false; }
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
        { if (!colliders.ContainsKey(collider)) colliders[collider] = collider.enabled; collider.enabled = false; }
    }
    void ShowModel()
    {
        foreach (var pair in renderers) if (pair.Key != null) pair.Key.enabled = pair.Value;
        foreach (var pair in colliders) if (pair.Key != null) pair.Key.enabled = pair.Value;
        renderers.Clear(); colliders.Clear();
    }
    public void RestoreHome(string id, bool inside, bool goingHome)
    {
        ShowModel(); homeId = id; housed = inside; returning = goingHome;
        path = null; ClearGrassTarget(); nextPathTime = 0;
    }
    public void RelocateHome(string id)
    {
        homeId = id; path = null; nextPathTime = 0;
        if (housed && Home != null) transform.position = Home.Entry;
    }
}
