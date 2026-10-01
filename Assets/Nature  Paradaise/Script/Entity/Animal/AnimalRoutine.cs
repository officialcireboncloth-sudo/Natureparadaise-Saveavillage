using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Penugasan kandang, perjalanan merumput, dan boarding; tidak menonaktifkan daily/save hewan.</summary>
public sealed class AnimalRoutine : MonoBehaviour
{
    public static readonly List<AnimalRoutine> Active = new();
    [SerializeField] string homeId;
    [SerializeField] bool housed;
    [SerializeField] bool returning;
    [SerializeField, Min(2f)] float maximumRecallSeconds = 12f;
    [Min(0.1f)] public float walkSpeed = 1.5f;
    [Min(1f)] public float grazingSearchRadius = 20f;
    [Tooltip("Jarak horizontal dari marker pintu saat hewan dianggap sudah masuk kandang.")]
    [SerializeField, Min(0.5f)] float boardingDistance = 2.25f;
    [Header("Eating")]
    [SerializeField, Min(0.1f)] float eatingDuration = 1.35f;
    [SerializeField] string eatingTrigger = "Eat";
    [SerializeField] RuntimeAnimatorController eatingController;
    [Header("Outdoor Roaming")]
    [Tooltip("Radius jelajah santai dari pintu kandang ketika hewan tidak sedang mencari pakan.")]
    [SerializeField, Min(1f)] float roamingRadius = 9f;
    [SerializeField, Min(0.5f)] float minimumRoamingDistance = 1.75f;
    [SerializeField, Min(0.5f)] float minimumRoamingPause = 2f;
    [SerializeField, Min(0.5f)] float maximumRoamingPause = 5f;

    [Header("Locomotion Animation")]
    [SerializeField] RuntimeAnimatorController idleController;
    [SerializeField] RuntimeAnimatorController walkController;
    [Tooltip("Kecepatan horizontal minimum (meter/detik) sebelum animasi Walk aktif.")]
    [SerializeField, Min(0.01f)] float movingAnimationThreshold = 0.08f;

    [Header("Movement Debug")]
    [Tooltip("Tampilkan garis tujuan dan status AI di Scene view.")]
    [SerializeField] bool debugMovement = true;
    [Tooltip("Tulis status tujuan AI ke Console secara berkala.")]
    [SerializeField] bool debugMovementConsole = true;
    [SerializeField, Min(0.1f)] float debugLogInterval = 1f;
    [Tooltip("Jika aktif, pencarian WorldGatherable Grass memakai posisi hewan, bukan pintu kandang.")]
    [SerializeField] bool searchGrassFromAnimalPosition = true;
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
    bool eating;
    bool playerPushing;
    readonly HashSet<object> careLocks = new();
    Transform playerPusher;
    Animator activeEatingAnimator;
    Animator locomotionAnimator;
    Transform animationRoot;
    AnimalLocomotionCatalog locomotionCatalog;
    RuntimeAnimatorController activeIdleController;
    RuntimeAnimatorController activeWalkController;
    RuntimeAnimatorController activeEatController;
    RuntimeAnimatorController controllerDuringEating;
    float lastAnimationMovementTime = float.NegativeInfinity;
    RuntimeAnimatorController controllerBeforeEating;
    Vector3 previousAnimationPosition;
    bool correctedOutdoorEntry;
    bool correctedOutdoorHeight;
    bool correctedOutdoorCrowding;
    Vector3 pathDestination;
    Vector3 roamingDestination;
    Vector3 unassignedRoamingOrigin;
    bool hasRoamingDestination;
    float nextRoamingTime;
    float nextHomeSearchTime;
    float returningSince = -1f;
    float nextDebugLogTime;
    string lastDebugGoal = "";
    Vector3 debugGoalPosition;
    bool hasDebugGoal;
    readonly Dictionary<Renderer, bool> renderers = new();
    readonly Dictionary<Collider, bool> colliders = new();
    public string Activity { get; private set; } = "Idle";
    void Awake()
    {
        Animal = GetComponent<AnimalGrowthSystem>();
        body = GetComponent<Rigidbody>();
        if (body != null) originalKinematic = body.isKinematic;
        unassignedRoamingOrigin = transform.position;
        locomotionCatalog = AnimalLocomotionCatalog.Load();
        previousAnimationPosition = transform.position;
    }
    void OnEnable()
    {
        Active.Add(this); TimeManager.OnDay += Overnight;
        if (body != null) body.isKinematic = true;
    }
    void OnDisable()
    {
        Active.Remove(this); TimeManager.OnDay -= Overnight; StopAllCoroutines(); RestoreEatingAnimation(); eating = false; careLocks.Clear(); ShowModel();
        if (body != null) body.isKinematic = originalKinematic;
    }
    public bool Assign(AnimalHome home)
    {
        if (home == null || !home.HasRoom(Animal.Type)) return false;
        // Baca saklar sebelum routine ini menjadi resident. Kandang yang masih kosong
        // harus default DI DALAM, bukan menganggap hewan liar ini sebagai resident luar.
        bool destinationOutside = home.AnimalsOutside;
        if (housed) { transform.position = Home != null ? Home.Entry : transform.position; ShowModel(); }
        homeId = home.Id; path = null; ClearRoamingTarget();
        if (!Animal.HasBeenBorn || !destinationOutside)
        {
            transform.position = home.Entry;
            housed = true; returning = false;
            returningSince = -1f;
            Animal.SetSheltered(true);
            return true;
        }
        // Pembelian baru mengikuti saklar kandang: ketika kelompok sedang di luar,
        // hewan baru langsung muncul di luar tanpa fase campuran "sedang pulang".
        int releaseIndex = Mathf.Max(0, home.Residents.Count - 1);
        transform.position = home.OutdoorReleasePosition(releaseIndex);
        housed = false; returning = false;
        returningSince = -1f;
        ShowModel();
        Animal.SetSheltered(false);
        Activity = "Di luar";
        return true;
    }

    /// <summary>
    /// Memulihkan assignment yang kosong atau menunjuk kandang lama setelah build/load.
    /// Dipanggil Animal Bell secara paksa dan routine secara berkala agar tidak scan tiap frame.
    /// </summary>
    public bool TryRepairHomeAssignment(bool force = false)
    {
        if (Animal == null) return false;
        AnimalHome current = Home;
        if (current != null && current.Accepts(Animal.Type)) return true;
        if (!force && Time.unscaledTime < nextHomeSearchTime) return false;
        nextHomeSearchTime = Time.unscaledTime + 2f;
        AnimalHome available = AnimalHome.FindVacancy(Animal.Type);
        return available != null && Assign(available);
    }
    public bool Release() => ReleaseAt(null);
    public bool ReleaseAt(Vector3? outsidePosition)
    {
        int hour = TimeManager.Instance != null ? TimeManager.Instance.hour : 6;
        // Tombol keluar juga membatalkan perjalanan pulang yang belum selesai.
        if (returning && Animal.HasBeenBorn && Animal.Health == AnimalHealthState.Healthy &&
            Home != null && hour >= 6 && hour < 18)
        {
            returning = false;
            path = null;
            nextPathTime = 0f;
            ClearGrassTarget();
            ClearRoamingTarget();
            ShowModel();
            Animal.SetSheltered(false);
            Activity = "Di luar";
            return true;
        }
        if (!AnimalCareRules.CanTurnOut(Animal.HasBeenBorn, Animal.Health == AnimalHealthState.Healthy, Animal.CanGrazeToday, hour, Home != null, housed)) return false;
        transform.position = outsidePosition ?? Home.Entry;
        housed = returning = false;
        correctedOutdoorHeight = false;
        correctedOutdoorCrowding = false;
        ClearGrassTarget(); ClearRoamingTarget(); path = null; nextPathTime = 0;
        ShowModel(); Animal.SetSheltered(false); Activity = "Di luar";
        return true;
    }
    public bool CanReleaseNow()
    {
        if (Animal == null || !Animal.HasBeenBorn || Home == null) return false;
        if (!housed && !returning) return true;
        int hour = TimeManager.Instance != null ? TimeManager.Instance.hour : 6;
        return Animal.Health == AnimalHealthState.Healthy && hour >= 6 && hour < 18;
    }
    public string ReleaseBlockReason()
    {
        if (Animal == null) return "data hewan belum siap";
        if (!Animal.HasBeenBorn) return $"{Animal.AnimalName} belum menetas/lahir";
        if (Home == null) return $"{Animal.AnimalName} belum memiliki kandang";
        if (Animal.Health != AnimalHealthState.Healthy) return $"{Animal.AnimalName} sedang sakit";
        int hour = TimeManager.Instance != null ? TimeManager.Instance.hour : 6;
        if (hour < 6 || hour >= 18) return "hewan hanya dapat dikeluarkan pukul 06:00–18:00";
        return string.Empty;
    }
    public void Recall()
    {
        if (!housed && Home != null)
        {
            returning = true;
            returningSince = Time.unscaledTime;
            path = null;
            nextPathTime = 0;
            ClearRoamingTarget();
        }
    }
    // Hewan outdoor yang belum berhasil makan grass tetap memakai pakan trough saat
    // daily care. RegisterFeeding mencegah konsumsi ganda jika grass sudah dimakan.
    public void PrepareDailyCare() { Home?.Feed(Animal); }
    void Overnight()
    {
        // Pergantian hari mewakili perjalanan pulang saat waktu tidur dilewati.
        if (Home != null) Board(false);
    }
    void Board(bool feedFromTrough = true)
    {
        if (Home == null) return;
        transform.position = Home.Entry;
        housed = true; returning = false; returningSince = -1f; path = null; ClearGrassTarget(); ClearRoamingTarget();
        Animal.SetSheltered(true); Activity = "Di kandang";
        if (feedFromTrough) PrepareDailyCare();
    }
    void Update()
    {
        if (Time.timeScale <= 0 || (TimeManager.Instance != null && TimeManager.Instance.IsPaused) || Animal == null) return;
        if(careLocks.Count>0)
        {
            Activity="Sedang dirawat";
            ClearDebugGoal("ANIMAL_CARE");
            return;
        }
        if(playerPushing)
        {
            Activity="Didorong player";
            ClearDebugGoal("PLAYER_PUSH");
            return;
        }
        TryRepairHomeAssignment();
        AnimalHome home = Home;
        if (home == null)
        {
            if (housed) ShowModel();
            housed = returning = false;
            Animal.SetSheltered(false);
            UpdateWithoutHome();
            return;
        }
        CorrectOutdoorEntrySpawn(home);
        CorrectOutdoorHeight(home);
        CorrectOutdoorCrowding(home);
        if (housed) { Animal.SetSheltered(true); PrepareDailyCare(); Activity = "Di kandang"; ClearDebugGoal("HOUSED"); DebugMovementTick(); return; }
        Animal.SetSheltered(false);
        if (eating) { Activity = "Makan rumput"; ClearDebugGoal("EATING"); DebugMovementTick(); return; }
        int hour = TimeManager.Instance != null ? TimeManager.Instance.hour : 6;
        // Hujan/badai tidak memaksa pulang: player boleh mengambil risiko membiarkan
        // hewan di luar. Malam, belum lahir, atau sakit tetap memicu recall otomatis.
        if (hour >= 18 || hour < 6 || !Animal.HasBeenBorn || Animal.Health != AnimalHealthState.Healthy) RecallIfNeeded();
        Vector3 destination;
        if (returning)
        {
            // Pathfinding yang gagal tidak boleh meninggalkan sebagian kelompok macet
            // di depan/sekitar kandang setelah saklar IN ditekan.
            if (returningSince >= 0f && Time.unscaledTime - returningSince >= maximumRecallSeconds)
            {
                Board();
                return;
            }
            destination = home.Entry; Activity = "Pulang ke kandang"; SetDebugGoal("RETURN_HOME", destination);
            Vector3 entranceDelta = destination - transform.position;
            entranceDelta.y = 0f;
            // Marker dapat bersinggungan dengan collider bangunan. Begitu tubuh hewan
            // sudah sampai di area depan pintu, boarding diselesaikan tanpa memaksanya
            // menembus collider menuju titik pusat marker.
            if (entranceDelta.sqrMagnitude <= boardingDistance * boardingDistance) { Board(); return; }
        }
        else
        {
            // Wild grass hanya dicari otomatis oleh herbivore besar.
            // Chicken dan Duck tetap roaming biasa dan mengandalkan trough/hand feed.
            if (CanEatWildGrass() && !Animal.FedToday)
            {
                if (grass != null && !grass.IsAvailable) grass = null;
                if (terrainGrass != null && !terrainGrass.IsPatchAvailable(terrainGrassIndex))
                {
                    terrainGrass = null;
                    terrainGrassIndex = -1;
                }

                if (grass == null && terrainGrass == null)
                {
                    Vector3 grassSearchOrigin = searchGrassFromAnimalPosition
                        ? transform.position
                        : home.Entry;
                    grass = FindGrass(grassSearchOrigin);
                    if (grass == null)
                        TerrainDetailGrassManager.TryFindNearest(transform.position, home.Entry, grazingSearchRadius,
                            out terrainGrass, out terrainGrassIndex, out _);
                    path = null;
                }

                if (grass != null || terrainGrass != null)
                {
                    ClearRoamingTarget();
                    Vector3 grassPosition = grass != null
                        ? grass.transform.position : terrainGrass.GetPatchPosition(terrainGrassIndex);
                    Vector3 delta = transform.position - grassPosition;
                    delta.y = 0f;

                    if (delta.magnitude < 1.35f)
                    {
                        StartCoroutine(EatGrass());
                        return;
                    }

                    destination = grassPosition +
                        (delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.forward);
                    Activity = "Menuju rumput"; SetDebugGoal("SEEK_GRASS", destination);
                }
                else
                {
                    ClearGrassTarget();
                    if (!TryGetRoamingDestination(home.Entry, out destination))
                    {
                        Activity = "Mencari rumput / berkeliaran";
                        return;
                    }
                    Activity = "Berkeliaran"; SetDebugGoal("ROAM", destination);
                }
            }
            else
            {
                // Sudah makan, atau spesies yang tidak memakai wild grass.
                ClearGrassTarget();
                if (!TryGetRoamingDestination(home.Entry, out destination))
                {
                    Activity = Animal.FedToday ? "Beristirahat" : "Berkeliaran";
                    return;
                }
                Activity = "Berkeliaran"; SetDebugGoal("ROAM", destination);
            }
        }
        DebugMovementTick();
        FollowPath(destination, home);
    }

    void UpdateWithoutHome()
    {
        int hour = TimeManager.Instance != null ? TimeManager.Instance.hour : 6;
        if (!Animal.HasBeenBorn)
        {
            Activity = "Belum menetas / belum punya Coop";
            return;
        }
        if (Animal.Health != AnimalHealthState.Healthy || hour < 6 || hour >= 18)
        {
            Activity = $"Menunggu {(AnimalGrowthProfileSO.IsBird(Animal.Type) ? "Coop" : "Barn")}";
            return;
        }
        if (!TryGetRoamingDestination(unassignedRoamingOrigin, out Vector3 destination))
        {
            Activity = $"Berkeliaran tanpa {(AnimalGrowthProfileSO.IsBird(Animal.Type) ? "Coop" : "Barn")}";
            return;
        }
        Activity = $"Berkeliaran (belum punya {(AnimalGrowthProfileSO.IsBird(Animal.Type) ? "Coop" : "Barn")})";
        SetDebugGoal("ROAM_NO_HOME", destination);
        DebugMovementTick();
        FollowPath(destination, null);
    }

    void FollowPath(Vector3 destination, AnimalHome home)
    {
        if (Time.time >= nextPathTime && (path == null || (destination - pathDestination).sqrMagnitude > 1f))
        {
            nextPathTime = Time.time + 3f + (GetInstanceID() & 7) * 0.1f;
            path = AnimalWalkingPath.Find(transform.position, destination, transform);
            waypoint = 0; pathDestination = destination;
        }
        if (path == null)
        {
            if (returning && home != null)
            {
                Vector3 entranceDelta = home.Entry - transform.position;
                entranceDelta.y = 0f;
                if (entranceDelta.sqrMagnitude <= boardingDistance * boardingDistance * 2.25f)
                { Board(); return; }
            }
            Activity = "Jalur terhalang";
            if (debugMovementConsole && Time.unscaledTime >= nextDebugLogTime)
            {
                nextDebugLogTime = Time.unscaledTime + Mathf.Max(0.1f, debugLogInterval);
                Debug.LogWarning($"[AnimalAI] {name}/{Animal?.AnimalName} PATH FAILED | goal={lastDebugGoal} | from={transform.position} | to={destination} | distance={Vector3.Distance(transform.position, destination):0.0}m");
            }
            return;
        }
        if (waypoint >= path.Count) { path = null; return; }
        Vector3 next = Vector3.MoveTowards(transform.position, path[waypoint], walkSpeed * Time.deltaTime);
        if (!AnimalWalkingPath.Ground(next, transform, out Vector3 ground)) { path = null; return; }
        Vector3 direction = ground - transform.position; direction.y = 0;
        if (direction.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 6f);
        transform.position = ground;
        if (Vector3.Distance(transform.position, path[waypoint]) < 0.12f) waypoint++;
        if (hasRoamingDestination)
        {
            Vector3 roamDelta = roamingDestination - transform.position;
            roamDelta.y = 0f;
            if (roamDelta.sqrMagnitude <= 0.2f * 0.2f)
            {
                hasRoamingDestination = false;
                path = null;
                nextRoamingTime = Time.time + UnityEngine.Random.Range(
                    Mathf.Min(minimumRoamingPause, maximumRoamingPause),
                    Mathf.Max(minimumRoamingPause, maximumRoamingPause));
                Activity = "Beristirahat";
            }
        }
    }

    public bool BeginPlayerPush(Transform pusher)
    {
        if(Animal==null || !Animal.HasBeenBorn || AnimalGrowthProfileSO.IsBird(Animal.Type) || eating || careLocks.Count>0)
            return false;
        playerPushing=true;
        playerPusher=pusher;
        path=null;
        ClearGrassTarget();
        ClearRoamingTarget();
        Activity="Didorong player";
        if(body!=null) body.isKinematic=true;
        return true;
    }

    public void ConfigureLocomotionAnimation(
        RuntimeAnimatorController idle,
        RuntimeAnimatorController walk)
    {
        idleController = idle != null ? idle : idleController;
        walkController = walk;
        animationRoot = null;
        ResolveLocomotionAnimator();
    }

    void CorrectOutdoorEntrySpawn(AnimalHome home)
    {
        if (correctedOutdoorEntry || housed || returning || home == null) return;
        correctedOutdoorEntry = true;
        Vector3 fromEntry = transform.position - home.Entry;
        fromEntry.y = 0f;
        if (fromEntry.sqrMagnitude > 1.75f * 1.75f) return;

        int releaseIndex = Mathf.Max(0, home.Residents.IndexOf(this));
        transform.position = home.OutdoorReleasePosition(releaseIndex);
        unassignedRoamingOrigin = transform.position;
        path = null;
        nextPathTime = 0f;
        ClearRoamingTarget();
        Activity = "Di luar";
    }

    void CorrectOutdoorHeight(AnimalHome home)
    {
        if (correctedOutdoorHeight || housed || home == null) return;
        correctedOutdoorHeight = true;
        if (!home.TryGroundOutdoorPosition(transform.position, out Vector3 grounded)) return;
        if (Mathf.Abs(transform.position.y - grounded.y) <= 0.12f) return;
        transform.position = grounded;
        unassignedRoamingOrigin = grounded;
        path = null;
        nextPathTime = 0f;
    }

    void CorrectOutdoorCrowding(AnimalHome home)
    {
        if (correctedOutdoorCrowding || housed || returning || home == null) return;
        correctedOutdoorCrowding = true;
        List<AnimalRoutine> residents = home.Residents;
        int ownIndex = residents.IndexOf(this);
        if (ownIndex < 0) return;

        float ownRadius = GetGroundRadius(transform);
        bool overlapping = false;
        for (int index = 0; index < residents.Count; index++)
        {
            AnimalRoutine other = residents[index];
            if (other == null || other == this || other.IsHoused) continue;
            Vector3 separation = transform.position - other.transform.position;
            separation.y = 0f;
            float minimumDistance = ownRadius + GetGroundRadius(other.transform) + 0.35f;
            if (separation.sqrMagnitude >= minimumDistance * minimumDistance) continue;
            overlapping = true;
            break;
        }

        if (!overlapping) return;
        transform.position = home.OutdoorReleasePosition(ownIndex);
        unassignedRoamingOrigin = transform.position;
        path = null;
        nextPathTime = 0f;
        ClearRoamingTarget();
    }

    static float GetGroundRadius(Transform root)
    {
        Collider collider = root != null ? root.GetComponent<Collider>() : null;
        if (collider == null) return 0.5f;
        return Mathf.Max(0.25f, Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.z));
    }

    public bool TryMoveByPlayer(Vector3 delta)
    {
        if(!playerPushing || delta.sqrMagnitude<0.000001f) return false;
        Vector3 candidate=transform.position+delta;
        if(!AnimalWalkingPath.Ground(candidate,transform,playerPusher,out Vector3 grounded,false)) return false;
        transform.position=grounded;
        path=null;
        return true;
    }

    public void EndPlayerPush()
    {
        if(!playerPushing) return;
        playerPushing=false;
        playerPusher=null;
        path=null;
        unassignedRoamingOrigin=transform.position;
        ClearRoamingTarget();
        nextRoamingTime=Time.time+Mathf.Max(0.5f,minimumRoamingPause);
        Activity="Beristirahat";
    }

    public bool AcquireCareLock(object owner)
    {
        if(owner==null || eating || playerPushing || Animal==null || !Animal.HasBeenBorn ||
           (careLocks.Count>0 && !careLocks.Contains(owner))) return false;
        careLocks.Add(owner);
        path=null;
        ClearGrassTarget();
        ClearRoamingTarget();
        Activity="Sedang dirawat";
        if(body!=null)
        {
            body.isKinematic=true;
            body.linearVelocity=Vector3.zero;
            body.angularVelocity=Vector3.zero;
        }
        return true;
    }

    public void ReleaseCareLock(object owner)
    {
        if(owner==null || !careLocks.Remove(owner)) return;
        if(careLocks.Count>0) return;
        path=null;
        unassignedRoamingOrigin=transform.position;
        nextRoamingTime=Time.time+Mathf.Max(1f,minimumRoamingPause);
        Activity="Beristirahat";
    }
    void RecallIfNeeded() { if (!returning) Recall(); }

    IEnumerator EatGrass()
    {
        eating = true;
        Activity = "Makan rumput";
        ResolveLocomotionAnimator();
        activeEatingAnimator = locomotionAnimator;
        controllerBeforeEating = activeEatingAnimator != null ? activeEatingAnimator.runtimeAnimatorController : null;
        if (eatingController == null && Animal != null)
            eatingController = AnimalCareCatalog.Load()?.EatingController(Animal.Type);
        controllerDuringEating = activeEatController != null ? activeEatController : eatingController;
        if (activeEatingAnimator != null && controllerDuringEating != null)
            activeEatingAnimator.runtimeAnimatorController = controllerDuringEating;
        else if (activeEatingAnimator != null && !string.IsNullOrWhiteSpace(eatingTrigger))
            foreach (AnimatorControllerParameter parameter in activeEatingAnimator.parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == eatingTrigger)
                { activeEatingAnimator.SetTrigger(eatingTrigger); break; }

        yield return new WaitForSeconds(eatingDuration);

        if (Animal != null && CanEatWildGrass() && !Animal.FedToday &&
            Animal.Health == AnimalHealthState.Healthy && Animal.CanGrazeToday)
        {
            bool consumed = grass != null ? grass.TryGraze() :
                terrainGrass != null && terrainGrass.TryGraze(terrainGrassIndex);
            if (consumed) Animal.RegisterGrazing();
        }
        RestoreEatingAnimation();
        ClearGrassTarget();
        path = null;
        eating = false;
        Activity = Animal != null && Animal.FedToday ? "Sudah makan / beristirahat" : "Mencari rumput";
    }

    void RestoreEatingAnimation()
    {
        if (activeEatingAnimator != null && controllerDuringEating != null &&
            activeEatingAnimator.runtimeAnimatorController == controllerDuringEating)
            activeEatingAnimator.runtimeAnimatorController = controllerBeforeEating;
        activeEatingAnimator = null;
        controllerBeforeEating = null;
        controllerDuringEating = null;
    }

    void ClearGrassTarget() { grass = null; terrainGrass = null; terrainGrassIndex = -1; }
    void ClearRoamingTarget()
    {
        hasRoamingDestination = false;
        nextRoamingTime = 0f;
    }

    bool TryGetRoamingDestination(Vector3 homePosition, out Vector3 destination)
    {
        if (hasRoamingDestination)
        {
            destination = roamingDestination;
            return true;
        }
        destination = transform.position;
        if (Time.time < nextRoamingTime) return false;

        float minDistance = Mathf.Min(minimumRoamingDistance, roamingRadius);
        for (int attempt = 0; attempt < 4; attempt++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized *
                UnityEngine.Random.Range(minDistance, Mathf.Max(minDistance, roamingRadius));
            Vector3 candidate = homePosition + new Vector3(offset.x, 0f, offset.y);
            candidate.y = transform.position.y;
            if (!AnimalWalkingPath.Ground(candidate, transform, out Vector3 ground)) continue;
            roamingDestination = ground;
            hasRoamingDestination = true;
            destination = ground;
            path = null;
            nextPathTime = 0f;
            return true;
        }

        nextRoamingTime = Time.time + Mathf.Max(1f, minimumRoamingPause);
        return false;
    }
    /// <summary>
    /// Spesies yang boleh mencari dan memakan wild grass secara otomatis.
    /// </summary>
    bool CanEatWildGrass()
    {
        if (Animal == null) return false;
        return Animal.Type == AnimalType.Cow ||
               Animal.Type == AnimalType.Goat ||
               Animal.Type == AnimalType.Sheep;
    }

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
    void SetDebugGoal(string goal, Vector3 position)
    {
        lastDebugGoal = goal;
        debugGoalPosition = position;
        hasDebugGoal = true;
    }

    void ClearDebugGoal(string state)
    {
        lastDebugGoal = state;
        hasDebugGoal = false;
    }

    string GrassDebugSummary()
    {
        int available = 0;
        float nearestAnimal = float.PositiveInfinity;
        float nearestSearchOrigin = float.PositiveInfinity;
        Vector3 searchOrigin = searchGrassFromAnimalPosition ? transform.position : (Home != null ? Home.Entry : transform.position);

        foreach (WorldGatherable candidate in WorldGatherable.Active)
        {
            if (candidate == null || candidate.Kind != GatherableKind.Grass || !candidate.IsAvailable)
                continue;

            available++;
            nearestAnimal = Mathf.Min(nearestAnimal, Vector3.Distance(transform.position, candidate.transform.position));
            nearestSearchOrigin = Mathf.Min(nearestSearchOrigin, Vector3.Distance(searchOrigin, candidate.transform.position));
        }

        string nearestA = float.IsPositiveInfinity(nearestAnimal) ? "none" : $"{nearestAnimal:0.0}m";
        string nearestO = float.IsPositiveInfinity(nearestSearchOrigin) ? "none" : $"{nearestSearchOrigin:0.0}m";
        return $"grass={available}, nearestFromAnimal={nearestA}, nearestFromSearchOrigin={nearestO}, radius={grazingSearchRadius:0.0}m";
    }

    void DebugMovementTick()
    {
        if (!debugMovementConsole || Animal == null || Time.unscaledTime < nextDebugLogTime)
            return;

        nextDebugLogTime = Time.unscaledTime + Mathf.Max(0.1f, debugLogInterval);

        string target = hasDebugGoal
            ? $"{lastDebugGoal} -> {debugGoalPosition} ({Vector3.Distance(transform.position, debugGoalPosition):0.0}m)"
            : lastDebugGoal;

        Debug.Log(
            $"[AnimalAI] {name}/{Animal.AnimalName} | Type={Animal.Type} | Activity={Activity} | " +
            $"Fed={Animal.FedToday} | Housed={housed} | Returning={returning} | " +
            $"CanWildGrass={CanEatWildGrass()} | Goal={target} | {GrassDebugSummary()}");
    }

    void OnDrawGizmosSelected()
    {
        if (!debugMovement) return;

        // Putih: tujuan AI aktif.
        if (hasDebugGoal)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.25f, debugGoalPosition + Vector3.up * 0.25f);
            Gizmos.DrawWireSphere(debugGoalPosition + Vector3.up * 0.15f, 0.35f);
        }

        // Cyan: target WorldGatherable grass.
        if (grass != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, grass.transform.position + Vector3.up * 0.5f);
            Gizmos.DrawWireSphere(grass.transform.position + Vector3.up * 0.15f, 0.5f);
        }

        // Hijau: tujuan roaming.
        if (hasRoamingDestination)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(roamingDestination + Vector3.up * 0.15f, 0.4f);
        }

        // Kuning: radius pencarian grass.
        Gizmos.color = Color.yellow;
        Vector3 origin = searchGrassFromAnimalPosition
            ? transform.position
            : (Home != null ? Home.Entry : transform.position);
        Gizmos.DrawWireSphere(origin, grazingSearchRadius);
    }

    void LateUpdate()
    {
        UpdateLocomotionAnimation();
        if (!housed) return;
        BarnInterior interior = BarnInterior.Current;
        if (interior != null && interior.home == Home && Animal.HasBeenBorn)
        {
            ShowModel();
            // Perawatan mempertahankan posisi dan arah hewan selama animasi cukur.
            if(careLocks.Count==0)
            {
                var controller=BarnInteriorSceneController.Instance;
                transform.SetPositionAndRotation(interior.AnimalPosition(this),
                    controller!=null ? controller.AnimalFacing(this) : transform.rotation);
            }
            return;
        }
        if (Home != null) transform.position = Home.Entry;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        { if (!renderers.ContainsKey(renderer)) renderers[renderer] = renderer.enabled; renderer.enabled = false; }
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
        { if (!colliders.ContainsKey(collider)) colliders[collider] = collider.enabled; collider.enabled = false; }
    }

    void UpdateLocomotionAnimation()
    {
        ResolveLocomotionAnimator();

        Vector3 delta = transform.position - previousAnimationPosition;
        delta.y = 0f;
        previousAnimationPosition = transform.position;
        if (locomotionAnimator == null || eating) return;

        float horizontalSpeed = Time.deltaTime > 0.0001f ? delta.magnitude / Time.deltaTime : 0f;
        if (horizontalSpeed >= movingAnimationThreshold) lastAnimationMovementTime = Time.time;
        // Brief path/collision pauses must not restart the walk clip on every frame.
        bool moving = !housed && careLocks.Count == 0 && !playerPushing &&
            Time.time - lastAnimationMovementTime < 0.12f;
        RuntimeAnimatorController desired = moving && activeWalkController != null
            ? activeWalkController
            : activeIdleController;
        if (desired != null && locomotionAnimator.runtimeAnimatorController != desired)
            locomotionAnimator.runtimeAnimatorController = desired;
    }

    void ResolveLocomotionAnimator()
    {
        Transform root = Animal != null ? Animal.CurrentAnimationRoot : transform;
        if (root == animationRoot && locomotionAnimator != null && locomotionAnimator.isActiveAndEnabled) return;
        animationRoot = root;
        locomotionAnimator = root != null ? root.GetComponentInChildren<Animator>() : null;
        activeIdleController = activeWalkController = activeEatController = null;
        if (locomotionAnimator == null) return;
        RuntimeAnimatorController original = locomotionAnimator.runtimeAnimatorController;
        AnimalLocomotionCatalog.Rig rig = locomotionCatalog != null ? locomotionCatalog.Find(locomotionAnimator) : null;
        activeIdleController = rig != null ? rig.idle : (idleController != null ? idleController : original);
        activeWalkController = rig != null ? rig.walk : walkController;
        activeEatController = rig != null ? rig.eat : null;
        // Authored overrides remain usable, but an adult controller must not drive a calf/chick rig.
        if (rig != null && locomotionCatalog.Find(idleController) == rig) activeIdleController = idleController;
        if (rig != null && locomotionCatalog.Find(walkController) == rig) activeWalkController = walkController;
        if (rig != null && locomotionCatalog.Find(eatingController) == rig) activeEatController = eatingController;
        locomotionAnimator.applyRootMotion = false;
        locomotionAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        lastAnimationMovementTime = float.NegativeInfinity;
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
        returningSince = goingHome ? Time.unscaledTime : -1f;
        path = null; ClearGrassTarget(); ClearRoamingTarget(); nextPathTime = 0;
    }
    public void RelocateHome(string id)
    {
        homeId = id; path = null; ClearRoamingTarget(); nextPathTime = 0;
        if (housed && Home != null) transform.position = Home.Entry;
    }
}
