using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public enum CompanionSpecies { Dog, Cat, Horse, Other }
public enum CompanionCommand { Follow, Stay, GoHome }

[Serializable]
public sealed class PersonalAnimalSaveData
{
    public string companionId;
    public string displayName;
    public CompanionSpecies species;
    public CompanionCommand command;
    public bool tamed;
    public bool activeCompanion;
    public int heartPoints;
    public string homeScene;
    public float homeX, homeY, homeZ;
    public float x, y, z;
}

/// <summary>Hewan pribadi yang merespons whistle. Jangan pasang pada ternak Barn/Coop.</summary>
[DisallowMultipleComponent]
public sealed class PersonalAnimal : MonoBehaviour
{
    static readonly List<PersonalAnimal> Registry = new();
    static readonly RaycastHit[] MountedGroundHits = new RaycastHit[32];
    static readonly RaycastHit[] MountedCollisionHits = new RaycastHit[32];

    [Header("Identity")]
    [SerializeField] string companionId;
    [SerializeField] string displayName = "Companion";
    [SerializeField] CompanionSpecies species = CompanionSpecies.Dog;
    [SerializeField] bool tamed = true;
    [SerializeField] bool activeCompanion = true;
    [SerializeField, Range(0, 1000)] int heartPoints;

    [Header("Home")]
    [SerializeField] Transform homePoint;
    [SerializeField] Vector3 homePosition;
    [SerializeField] string homeScene;

    [Header("Movement")]
    [SerializeField, Min(0.5f)] float normalSpeed = 3.2f;
    [SerializeField, Min(0.5f)] float highHeartSpeed = 5.2f;
    [SerializeField, Min(0.5f)] float followDistance = 1.8f;
    [SerializeField, Min(1f)] float farArrivalDistance = 35f;
    [SerializeField, Min(0.1f)] float farArrivalDelay = 1.25f;
    [SerializeField, Min(0.1f)] float repathInterval = 0.7f;
    [SerializeField] bool persistBetweenScenes = true;

    [Header("Animation (Optional)")]
    [SerializeField] Animator animator;
    [SerializeField] RuntimeAnimatorController idleController;
    [SerializeField] RuntimeAnimatorController moveController;
    [SerializeField] RuntimeAnimatorController runController;

    [Header("Horse Mount")]
    [SerializeField] Transform mountPoint;
    [SerializeField] Vector3 mountLocalPosition = new(0f, 1.25f, 0f);
    [SerializeField, Min(1f)] float mountedSpeed = 7f;
    [SerializeField, Min(0.1f)] float mountedJumpHeight = 1.35f;
    [SerializeField, Min(1f)] float mountedGravity = 22f;
    [SerializeField, Min(0f)] float mountedJumpCooldown = 0.25f;
    [SerializeField] RuntimeAnimatorController jumpController;
    [SerializeField, Min(0.01f)] float mountedCollisionSkin = 0.08f;
    [SerializeField, Min(1f)] float mountedFallRecoveryDistance = 6f;
    [SerializeField, Min(0.5f)] float mountedFallRecoveryDelay = 3f;

    CompanionCommand command = CompanionCommand.Stay;
    Transform followTarget;
    List<Vector3> path;
    int waypoint;
    float nextPathTime;
    float pathFailedSince = -1f;
    Coroutine arrivalRoutine;
    PlayerController mountedPlayer;
    Transform mountedOriginalParent;
    readonly Dictionary<Collider, bool> mountedPlayerColliders = new();
    RuntimeAnimatorController appliedController;
    float mountedVerticalVelocity;
    float nextMountedJumpTime;
    bool mountedGrounded = true;
    float mountedAirborneSince;
    Vector3 mountedLastSafePosition;

    public string Id => companionId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? species.ToString() : displayName;
    public CompanionSpecies Species => species;
    public CompanionCommand Command => command;
    public bool IsTamed => tamed;
    public bool IsActiveCompanion => activeCompanion;
    public int HeartPoints => heartPoints;
    public bool IsMounted => mountedPlayer != null;
    public static IReadOnlyList<PersonalAnimal> Active => Registry;

    float MoveSpeed => Mathf.Lerp(normalSpeed, highHeartSpeed, Mathf.Clamp01(heartPoints / 1000f));

    void Awake()
    {
        if (string.IsNullOrWhiteSpace(companionId))
            companionId = $"{gameObject.scene.name}/{gameObject.name}/{transform.position.x:0.##}/{transform.position.z:0.##}";
        if (homePoint != null) homePosition = homePoint.position;
        else if (homePosition == Vector3.zero) homePosition = transform.position;
        if (string.IsNullOrWhiteSpace(homeScene)) homeScene = gameObject.scene.name;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        ApplyAnimation(false, false);
        foreach (PersonalAnimal existing in Registry)
            if (existing != null && existing != this && existing.companionId == companionId)
            { Destroy(gameObject); return; }
        if (persistBetweenScenes)
        {
            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
        }
    }

    void OnEnable()
    {
        foreach (PersonalAnimal existing in Registry)
            if (existing != null && existing != this && existing.companionId == companionId)
            { enabled = false; return; }
        if (!Registry.Contains(this)) Registry.Add(this);
    }

    void OnDisable()
    {
        CancelSafeArrival();
        Registry.Remove(this);
        if (mountedPlayer != null) Dismount();
    }

    void Update()
    {
        if (!tamed || Time.timeScale <= 0 || (TimeManager.Instance != null && TimeManager.Instance.IsPaused)) return;
        if (mountedPlayer != null) { UpdateMounted(); return; }
        ShowCommandPrompt();
        if (command == CompanionCommand.Stay) { ApplyAnimation(false, false); return; }

        Vector3 destination;
        float stopDistance;
        if (command == CompanionCommand.GoHome)
        {
            destination = homePoint != null ? homePoint.position : homePosition;
            stopDistance = 0.65f;
        }
        else
        {
            if (followTarget == null)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player == null) return;
                followTarget = player.transform;
            }
            destination = followTarget.position - followTarget.forward * followDistance;
            stopDistance = followDistance;
        }

        float distance = Vector3.Distance(transform.position, destination);
        if (distance <= stopDistance)
        {
            path = null;
            pathFailedSince = -1f;
            CancelSafeArrival();
            if (command == CompanionCommand.GoHome) command = CompanionCommand.Stay;
            ApplyAnimation(false, false);
            return;
        }
        MoveTo(destination, distance);
    }

    void MoveTo(Vector3 destination, float remainingDistance)
    {
        if (Time.time >= nextPathTime && (path == null || waypoint >= path.Count))
        {
            nextPathTime = Time.time + repathInterval;
            // Fallback grid AnimalWalkingPath mempunyai radius terbatas. Untuk tujuan jauh,
            // pecah perjalanan menjadi beberapa segmen supaya companion tetap berjalan.
            Vector3 pathDestination = destination;
            Vector3 flatDelta = destination - transform.position;
            flatDelta.y = 0f;
            float segmentDistance = Mathf.Clamp(farArrivalDistance * 0.65f, 12f, 24f);
            if (flatDelta.magnitude > segmentDistance)
                pathDestination = transform.position + flatDelta.normalized * segmentDistance;

            path = AnimalWalkingPath.Find(transform.position, pathDestination, transform);
            waypoint = 0;
            if (path == null)
            {
                if (pathFailedSince < 0f) pathFailedSince = Time.time;
                if (Time.time - pathFailedSince >= 6f) ScheduleSafeArrival(destination);
                ApplyAnimation(false, false);
                return;
            }
            CancelSafeArrival();
            pathFailedSince = -1f;
        }
        if (path == null || waypoint >= path.Count) return;
        float speed = MoveSpeed;
        Vector3 next = Vector3.MoveTowards(transform.position, path[waypoint], speed * Time.deltaTime);
        if (!AnimalWalkingPath.Ground(next, transform, out Vector3 ground)) { path = null; return; }
        Vector3 direction = ground - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 8f);
        transform.position = ground;
        if (Vector3.Distance(transform.position, path[waypoint]) < 0.15f) waypoint++;
        ApplyAnimation(true, remainingDistance > 8f || heartPoints >= 700);
    }

    void ShowCommandPrompt()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null || !PlayerInteractionTarget.Contains(player.transform, transform)) return;
        string label = species == CompanionSpecies.Horse
            ? $"E: {DisplayName} Commands / Mount"
            : $"E: {DisplayName} Commands";
        WorldInteractionPrompt.Request(this, transform, label, Vector3.Distance(player.transform.position, transform.position), 1.2f);
        if (PlayerInteractionTarget.Press(player.transform, transform, KeyCode.E))
            CompanionCommandPanel.Show(this, player);
    }

    public void Call(Transform player)
    {
        if (!tamed || player == null) return;
        SetActiveCompanion();
        followTarget = player;
        command = CompanionCommand.Follow;
        CancelSafeArrival();
        path = null;
        pathFailedSince = -1f;
        nextPathTime = 0f;
    }

    public void SetCommand(CompanionCommand value, Transform player = null)
    {
        if (!tamed) return;
        if (value == CompanionCommand.Follow) SetActiveCompanion();
        command = value;
        if (player != null) followTarget = player;
        CancelSafeArrival();
        path = null;
        pathFailedSince = -1f;
        nextPathTime = 0f;
        SaveLoadFeedback.Instance?.ShowMessage($"{DisplayName}: {CommandLabel(value)}");
    }

    public void SetActiveCompanion()
    {
        foreach (PersonalAnimal other in Registry)
            if (other != null) other.activeCompanion = other == this;
    }

    public void SetTamed(bool value)
    {
        tamed = value;
        if (value) { SetActiveCompanion(); command = CompanionCommand.Stay; }
    }

    public void AddHeart(int amount) => heartPoints = Mathf.Clamp(heartPoints + amount, 0, 1000);

    public bool TryMount(PlayerController player)
    {
        if (!tamed || species != CompanionSpecies.Horse || player == null || mountedPlayer != null ||
            Vector3.Distance(player.transform.position, transform.position) > 3f) return false;
        PlayerAnimalCarry animalCarry = player.GetComponent<PlayerAnimalCarry>();
        if (animalCarry != null && animalCarry.HasAnimal)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Turunkan hewan yang dibawa sebelum naik kuda.");
            return false;
        }
        SetActiveCompanion();
        mountedPlayer = player;
        mountedOriginalParent = player.transform.parent;
        player.AcquireMovementLock(this);
        mountedPlayerColliders.Clear();
        foreach (Collider collider in player.GetComponents<Collider>())
        {
            mountedPlayerColliders[collider] = collider.enabled;
            collider.enabled = false;
        }
        Transform seat = mountPoint != null ? mountPoint : transform;
        player.transform.SetParent(seat, true);
        player.transform.localPosition = mountPoint != null ? Vector3.zero : mountLocalPosition;
        player.transform.localRotation = Quaternion.identity;
        mountedVerticalVelocity = 0f;
        mountedGrounded = true;
        mountedAirborneSince = 0f;
        mountedLastSafePosition = transform.position;
        command = CompanionCommand.Stay;
        SaveLoadFeedback.Instance?.ShowMessage($"Mounted {DisplayName} — WASD bergerak, Space lompat, E turun");
        return true;
    }

    public void Dismount()
    {
        if (mountedPlayer == null) return;
        PlayerController player = mountedPlayer;
        player.transform.SetParent(mountedOriginalParent, true);
        Vector3[] dismountOffsets =
        {
            transform.right * 1.2f,
            -transform.right * 1.2f,
            -transform.forward * 1.35f
        };
        for (int i = 0; i < dismountOffsets.Length; i++)
        {
            Vector3 candidate = transform.position + dismountOffsets[i];
            if (!TryGround(candidate, out Vector3 ground)) continue;
            player.transform.position = ground;
            break;
        }
        foreach (var pair in mountedPlayerColliders) if (pair.Key != null) pair.Key.enabled = pair.Value;
        mountedPlayerColliders.Clear();
        player.ReleaseMovementLock(this);
        mountedPlayer = null;
        mountedVerticalVelocity = 0f;
        mountedGrounded = true;
        mountedAirborneSince = 0f;
        SaveLoadFeedback.Instance?.ShowMessage($"Turun dari {DisplayName}");
    }

    void UpdateMounted()
    {
        if (Input.GetKeyDown(KeyCode.E)) { Dismount(); return; }
        Vector2 input = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Transform cameraTransform = Camera.main != null ? Camera.main.transform : null;
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
        forward.y = right.y = 0f; forward.Normalize(); right.Normalize();
        input = Vector2.ClampMagnitude(input, 1f);
        Vector3 direction = (forward * input.y + right * input.x);
        if (direction.sqrMagnitude > 0.0001f)
        {
            direction.Normalize();
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 360f * Time.deltaTime);
        }

        Vector3 horizontalDelta = direction * mountedSpeed * Time.deltaTime;
        Vector3 horizontalTarget = ResolveMountedHorizontalTarget(horizontalDelta);
        bool hasGround = TryMountedGround(horizontalTarget, out Vector3 ground);
        if (hasGround && transform.position.y <= ground.y + 0.08f && mountedVerticalVelocity <= 0f)
        {
            mountedGrounded = true;
            mountedVerticalVelocity = 0f;
            horizontalTarget.y = ground.y;
            mountedLastSafePosition = horizontalTarget;
            mountedAirborneSince = 0f;
        }
        else
        {
            if (mountedGrounded) mountedAirborneSince = Time.time;
            mountedGrounded = false;
            horizontalTarget.y = transform.position.y;
        }

        if (mountedGrounded && Input.GetKeyDown(KeyCode.Space) && Time.time >= nextMountedJumpTime)
        {
            mountedVerticalVelocity = Mathf.Sqrt(2f * mountedGravity * mountedJumpHeight);
            mountedGrounded = false;
            mountedAirborneSince = Time.time;
            nextMountedJumpTime = Time.time + mountedJumpCooldown;
        }

        if (!mountedGrounded)
        {
            mountedVerticalVelocity -= mountedGravity * Time.deltaTime;
            horizontalTarget.y = transform.position.y + mountedVerticalVelocity * Time.deltaTime;
            if (hasGround && horizontalTarget.y <= ground.y && mountedVerticalVelocity <= 0f)
            {
                horizontalTarget.y = ground.y;
                mountedVerticalVelocity = 0f;
                mountedGrounded = true;
                mountedLastSafePosition = horizontalTarget;
                mountedAirborneSince = 0f;
            }
        }

        transform.position = horizontalTarget;
        if (!mountedGrounded && mountedVerticalVelocity <= 0f &&
            (transform.position.y < mountedLastSafePosition.y - mountedFallRecoveryDistance ||
             (!hasGround && mountedAirborneSince > 0f && Time.time - mountedAirborneSince >= mountedFallRecoveryDelay)))
        {
            transform.position = mountedLastSafePosition;
            mountedVerticalVelocity = 0f;
            mountedGrounded = true;
            mountedAirborneSince = 0f;
            SaveLoadFeedback.Instance?.ShowMessage($"{DisplayName} kembali ke posisi aman.");
        }
        if (!mountedGrounded && jumpController != null)
            ApplyController(jumpController);
        else
            ApplyAnimation(direction.sqrMagnitude > 0.0001f, true);
    }

    void ScheduleSafeArrival(Vector3 destination)
    {
        if (arrivalRoutine == null) arrivalRoutine = StartCoroutine(SafeArrival(destination));
    }

    IEnumerator SafeArrival(Vector3 destination)
    {
        float remaining = farArrivalDelay;
        while (remaining > 0f)
        {
            if (TimeManager.Instance == null || !TimeManager.Instance.IsPaused)
                remaining -= Time.deltaTime;
            yield return null;
        }

        if (command == CompanionCommand.Stay)
        {
            arrivalRoutine = null;
            yield break;
        }
        if (command == CompanionCommand.Follow && followTarget != null)
            destination = followTarget.position - followTarget.forward * followDistance;
        else if (command == CompanionCommand.GoHome)
            destination = homePoint != null ? homePoint.position : homePosition;

        if (TryGround(destination, out Vector3 ground)) transform.position = ground;
        else transform.position = destination;
        path = null;
        pathFailedSince = -1f;
        arrivalRoutine = null;
        SaveLoadFeedback.Instance?.ShowMessage($"{DisplayName} datang setelah mendengar siulan.");
    }

    void CancelSafeArrival()
    {
        if (arrivalRoutine == null) return;
        StopCoroutine(arrivalRoutine);
        arrivalRoutine = null;
    }

    bool TryGround(Vector3 point, out Vector3 ground)
    {
        // AnimalWalkingPath mengabaikan collider milik companion sendiri. Ini mencegah
        // horse perlahan naik karena raycast mengenai collider tubuhnya saat ditunggangi.
        if (AnimalWalkingPath.Ground(point, transform, out ground)) return true;
        if (NavMesh.SamplePosition(point, out NavMeshHit navHit, 4f, NavMesh.AllAreas))
        { ground = navHit.position; return true; }
        ground = point; return false;
    }

    /// <summary>
    /// Ground check khusus mount. Berbeda dari pathfinding, collider di samping tidak
    /// membatalkan terrain yang valid sehingga kuda tetap bisa mendarat dekat object.
    /// </summary>
    bool TryMountedGround(Vector3 point, out Vector3 ground)
    {
        ground = point;
        Vector3 origin = point + Vector3.up * 3f;
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, MountedGroundHits, 10f, ~0,
            QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        RaycastHit selected = default;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = MountedGroundHits[i];
            if (hit.collider == null || hit.distance >= nearest || hit.normal.y < 0.65f ||
                hit.transform.IsChildOf(transform) ||
                (mountedPlayer != null && hit.transform.IsChildOf(mountedPlayer.transform)))
                continue;
            nearest = hit.distance;
            selected = hit;
        }
        if (float.IsPositiveInfinity(nearest)) return false;
        ground = selected.point;
        return true;
    }

    /// <summary>Menahan gerak horizontal mount sebelum capsule memasuki dinding atau prop.</summary>
    Vector3 ResolveMountedHorizontalTarget(Vector3 delta)
    {
        Vector3 start = transform.position;
        delta.y = 0f;
        float distanceToMove = delta.magnitude;
        if (distanceToMove <= 0.0001f) return start;

        Collider body = GetComponent<Collider>();
        Vector3 desiredTarget = start + delta;
        if (body == null) return desiredTarget;
        Bounds bounds = body.bounds;
        float radius = Mathf.Clamp(Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.9f, 0.15f, 0.85f);
        Vector3 bottom = bounds.center - Vector3.up * Mathf.Max(0f, bounds.extents.y - radius);
        Vector3 top = bounds.center + Vector3.up * Mathf.Max(0f, bounds.extents.y - radius);
        Vector3 moveDirection = delta / distanceToMove;
        int count = Physics.CapsuleCastNonAlloc(bottom, top, radius, moveDirection,
            MountedCollisionHits, distanceToMove + mountedCollisionSkin, ~0, QueryTriggerInteraction.Ignore);
        float nearestWall = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = MountedCollisionHits[i];
            if (hit.collider == null || hit.transform.IsChildOf(transform) || hit.normal.y >= 0.65f ||
                hit.distance <= 0.001f ||
                hit.collider.GetComponentInParent<FieldArea>() != null ||
                (mountedPlayer != null && hit.transform.IsChildOf(mountedPlayer.transform)))
                continue;
            nearestWall = Mathf.Min(nearestWall, hit.distance);
        }
        if (!float.IsPositiveInfinity(nearestWall))
            distanceToMove = Mathf.Max(0f, nearestWall - mountedCollisionSkin);
        return start + moveDirection * distanceToMove;
    }

    void ApplyAnimation(bool moving, bool running)
    {
        if (animator == null) return;
        RuntimeAnimatorController desired = moving ? running && runController != null ? runController : moveController : idleController;
        ApplyController(desired);
    }

    void ApplyController(RuntimeAnimatorController desired)
    {
        if (animator != null && desired != null && desired != appliedController)
        { animator.runtimeAnimatorController = desired; appliedController = desired; }
    }

    public PersonalAnimalSaveData Capture() => new()
    {
        companionId = companionId, displayName = displayName, species = species, command = command,
        tamed = tamed, activeCompanion = activeCompanion, heartPoints = heartPoints,
        homeScene = homeScene, homeX = homePosition.x, homeY = homePosition.y, homeZ = homePosition.z,
        x = transform.position.x, y = transform.position.y, z = transform.position.z
    };

    public void Restore(PersonalAnimalSaveData data)
    {
        if (data == null) return;
        displayName = data.displayName; species = data.species; command = data.command; tamed = data.tamed;
        activeCompanion = data.activeCompanion; heartPoints = Mathf.Clamp(data.heartPoints, 0, 1000);
        homeScene = data.homeScene; homePosition = new Vector3(data.homeX, data.homeY, data.homeZ);
        transform.position = new Vector3(data.x, data.y, data.z); path = null; nextPathTime = 0f;
    }

    public static List<PersonalAnimalSaveData> CaptureAll()
    {
        List<PersonalAnimalSaveData> result = new();
        foreach (PersonalAnimal animal in Registry) if (animal != null) result.Add(animal.Capture());
        return result;
    }

    public static void RestoreAll(List<PersonalAnimalSaveData> data)
    {
        if (data == null) return;
        foreach (PersonalAnimalSaveData saved in data)
            foreach (PersonalAnimal animal in Registry)
                if (animal != null && animal.companionId == saved.companionId) { animal.Restore(saved); break; }
    }

    static string CommandLabel(CompanionCommand value) => value switch
    { CompanionCommand.Follow => "Follow Me", CompanionCommand.GoHome => "Go Home", _ => "Stay" };
}
