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
    [Tooltip("Controller Blend Tree idle/walk/trot/gallop. Jika tersedia, controller lama tidak lagi ditukar mendadak.")]
    [SerializeField] RuntimeAnimatorController locomotionController;
    [SerializeField] string locomotionSpeedParameter = "HorseSpeed";
    [SerializeField] RuntimeAnimatorController idleController;
    [SerializeField] RuntimeAnimatorController moveController;
    [SerializeField] RuntimeAnimatorController runController;

    [Header("Horse Mount")]
    [SerializeField, Min(0.1f), Tooltip("Horse companion selalu adult; scale muda/save lama akan dinormalisasi ke ukuran ini saat spawn.")]
    float adultHorseScale = 1.25f;
    [SerializeField] Transform mountPoint;
    [SerializeField] Vector3 mountLocalPosition = new(0f, 1.25f, 0f);
    [Tooltip("Koreksi posisi pantat terhadap saddle dalam world unit. Tidak ikut dikalikan scale kuda supaya rider tidak tenggelam saat ukuran horse berubah.")]
    [SerializeField] Vector3 riderSeatOffset = new(0f, -0.1f, 0f);
    [Tooltip("Posisi awal animasi naik relatif terhadap horse, dalam world unit. X negatif = sisi kiri horse.")]
    [SerializeField] Vector3 mountStartOffset = new(-0.65f, 0f, 0f);
    [Tooltip("Posisi akhir animasi turun relatif terhadap horse, dalam world unit.")]
    [SerializeField] Vector3 dismountEndOffset = new(-0.65f, 0f, 0f);
    [Header("Horse Transition Frames")]
    [Tooltip("Frame source FBX saat script mulai menggeser player mendatar menuju horse.")]
    [SerializeField, Range(0,75)] int mountMoveStartFrame = 0;
    [Tooltip("Frame source FBX saat posisi horizontal player sudah tepat di pelana.")]
    [SerializeField, Range(0,75)] int mountSeatFrame = 18;
    [Tooltip("Frame source FBX saat script mulai menggeser player mendatar menjauhi horse.")]
    [SerializeField, Range(0,90)] int dismountMoveStartFrame = 0;
    [Tooltip("Frame source FBX saat posisi horizontal player sudah berada di sisi horse.")]
    [SerializeField, Range(0,90)] int dismountGroundFrame = 30;
    [SerializeField, Min(1f)] float mountedSpeed = 7f;
    [SerializeField, Min(0.1f)] float mountedAcceleration = 10f;
    [SerializeField, Min(0.1f)] float mountedDeceleration = 14f;
    [SerializeField, Min(0.1f)] float riderSeatFollowSharpness = 22f;
    [Tooltip("Seberapa kuat rider mengikuti pitch/roll tulang punggung kuda.")]
    [SerializeField, Range(0f, 1f)] float riderSpineRotationFollow = 0.6f;
    [SerializeField, Min(0.1f)] float mountedJumpHeight = 1.35f;
    [SerializeField, Min(1f)] float mountedGravity = 22f;
    [SerializeField, Min(0f)] float mountedJumpCooldown = 0.25f;
    [SerializeField] RuntimeAnimatorController jumpController;
    [SerializeField, Min(0.01f)] float mountedCollisionSkin = 0.08f;
    [SerializeField, Min(1f)] float mountedFallRecoveryDistance = 6f;
    [SerializeField, Min(0.5f)] float mountedFallRecoveryDelay = 3f;
    [SerializeField, Min(0f)] float mountActionDuration = 1.25f;
    [SerializeField, Min(0f)] float dismountActionDuration = 1.5f;

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
    float mountedCurrentSpeed;
    Vector3 mountedMoveDirection;
    float nextMountedJumpTime;
    bool mountedGrounded = true;
    float mountedAirborneSince;
    Vector3 mountedLastSafePosition;
    bool mountTransitionBusy;
    bool playerAttachedToMount;
    Transform mountedPlayerHips;
    Renderer saddleRenderer;
    Transform animatedSeatBone;
    Vector3 animatedSeatLocalOffset;
    Quaternion animatedSeatRestRelativeRotation = Quaternion.identity;
    bool hasAnimatedSeatCalibration;
    RiderTransition riderTransition;
    float riderTransitionStartedAt;
    Vector3 riderTransitionStartHips;
    Vector3 riderTransitionEndRoot;
    Vector3 standingHipsLocalOffset;
    Quaternion riderTransitionRotation = Quaternion.identity;
    float riderSideSign = -1f;

    enum RiderTransition { None, Mounting, Dismounting }
    const string HorseMountMirrorParameter = "HorseMountMirror";

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
        if (species == CompanionSpecies.Horse)
            transform.localScale = Vector3.one * Mathf.Max(0.1f, adultHorseScale);
        if (animator == null) animator = GetComponentInChildren<Animator>();
        ResolveSeatReferences();
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
        StopAllCoroutines();
        CancelSafeArrival();
        Registry.Remove(this);
        if (mountedPlayer != null) DismountImmediate();
    }

    void Update()
    {
        if (!tamed || Time.timeScale <= 0 || (TimeManager.Instance != null && TimeManager.Instance.IsPaused)) return;
        if (mountedPlayer != null)
        {
            if(!mountTransitionBusy) UpdateMounted();
            return;
        }
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

    void LateUpdate()
    {
        if (mountedPlayer != null && mountTransitionBusy && riderTransition != RiderTransition.None)
            UpdateRiderTransitionPose();
        // Animator dapat menggeser hips setelah Update. Koreksi di LateUpdate membuat
        // pantat rider tetap menempel pada dudukan meski ukuran/scale horse berubah.
        else if (mountedPlayer != null && playerAttachedToMount)
            SnapRiderToSeat();
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
            mountTransitionBusy || Vector3.Distance(player.transform.position, transform.position) > 3f) return false;
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
        mountTransitionBusy=true;
        playerAttachedToMount=false;
        riderSideSign=Vector3.Dot(player.transform.position-transform.position,transform.right)>=0f?1f:-1f;
        Vector3 sideOffset=mountStartOffset;
        sideOffset.x=Mathf.Abs(sideOffset.x)*riderSideSign;
        PlacePlayerAtHorseOffset(player, sideOffset);
        CacheMountedPlayerHips(player);
        if (mountedPlayerHips != null)
            standingHipsLocalOffset = Quaternion.Inverse(player.transform.rotation) *
                                      (mountedPlayerHips.position - player.transform.position);
        player.SetRidingAnimation(false);
        SetHorseAnimationMirror(player,riderSideSign>0f);
        player.PlayMountHorseAnimation();
        BeginRiderTransition(RiderTransition.Mounting,GetSeatWorldPosition());
        StartCoroutine(MountRoutine());
        return true;
    }

    IEnumerator MountRoutine()
    {
        if(mountActionDuration>0f) yield return new WaitForSeconds(mountActionDuration);
        if(mountedPlayer==null)
        {
            mountTransitionBusy=false;
            riderTransition=RiderTransition.None;
            yield break;
        }
        riderTransition=RiderTransition.None;
        AttachRiderToSeat();
        SnapRiderToSeat();
        mountedPlayer.SetRidingAnimation(true);
        SetHorseAnimationMirror(mountedPlayer,false);
        SnapRiderToSeat();
        mountTransitionBusy=false;
        SaveLoadFeedback.Instance?.ShowMessage($"Mounted {DisplayName} — WASD bergerak, Space lompat, E turun");
    }

    void AttachRiderToSeat()
    {
        if (mountedPlayer == null || playerAttachedToMount)
            return;

        mountedPlayerColliders.Clear();
        foreach (Collider collider in mountedPlayer.GetComponents<Collider>())
        {
            mountedPlayerColliders[collider] = collider.enabled;
            collider.enabled = false;
        }
        Transform seat = mountPoint != null ? mountPoint : transform;
        mountedPlayer.transform.SetParent(seat, true);
        mountedPlayer.transform.localRotation = Quaternion.identity;
        CacheMountedPlayerHips(mountedPlayer);
        SnapRiderToSeat();
        playerAttachedToMount=true;
        mountedVerticalVelocity = 0f;
        mountedCurrentSpeed = 0f;
        mountedMoveDirection = transform.forward;
        mountedGrounded = true;
        mountedAirborneSince = 0f;
        mountedLastSafePosition = transform.position;
        command = CompanionCommand.Stay;
    }

    public void Dismount()
    {
        if (mountedPlayer == null || mountTransitionBusy) return;
        mountTransitionBusy=true;
        PlayerController player=mountedPlayer;
        Vector3 sideOffset=dismountEndOffset;
        sideOffset.x=Mathf.Abs(sideOffset.x)*riderSideSign;
        riderTransitionEndRoot=GetHorseOffsetGroundPosition(sideOffset);
        riderTransitionRotation=transform.rotation;
        if(playerAttachedToMount)
        {
            player.transform.SetParent(mountedOriginalParent,true);
            playerAttachedToMount=false;
        }
        CacheMountedPlayerHips(player);
        BeginRiderTransition(RiderTransition.Dismounting,riderTransitionEndRoot);
        mountedPlayer.SetRidingAnimation(false);
        SetHorseAnimationMirror(mountedPlayer,riderSideSign>0f);
        mountedPlayer.PlayDismountHorseAnimation();
        StartCoroutine(DismountRoutine());
    }

    IEnumerator DismountRoutine()
    {
        PlayerController player=mountedPlayer;
        if(dismountActionDuration>0f) yield return new WaitForSeconds(dismountActionDuration);
        if(player==null || mountedPlayer!=player)
        {
            mountTransitionBusy=false;
            riderTransition=RiderTransition.None;
            yield break;
        }
        riderTransition=RiderTransition.None;
        // X/Z sudah selesai dikontrol selama animasi. Jangan menimpanya lagi di akhir
        // karena itulah yang sebelumnya terlihat sebagai snap setelah klip selesai.
        Vector3 groundedPosition=player.transform.position;
        groundedPosition.y=riderTransitionEndRoot.y;
        player.transform.SetPositionAndRotation(groundedPosition,riderTransitionRotation);
        RestoreMountedPlayerColliders();
        mountedPlayerHips=null;
        mountedVerticalVelocity=0f;
        mountedCurrentSpeed=0f;
        mountedGrounded=true;
        mountedAirborneSince=0f;
        player.ReleaseMovementLock(this);
        SetHorseAnimationMirror(player,false);
        mountedPlayer=null;
        mountTransitionBusy=false;
        SaveLoadFeedback.Instance?.ShowMessage($"Turun dari {DisplayName}");
    }

    void DetachMountedPlayer(PlayerController player)
    {
        if(playerAttachedToMount)
        {
            player.transform.SetParent(mountedOriginalParent, true);
            PlacePlayerAtHorseOffset(player, dismountEndOffset);
            playerAttachedToMount=false;
        }
        RestoreMountedPlayerColliders();
        riderTransition=RiderTransition.None;
        mountedPlayerHips=null;
        mountedVerticalVelocity = 0f;
        mountedCurrentSpeed = 0f;
        mountedGrounded = true;
        mountedAirborneSince = 0f;
    }

    void PlacePlayerAtHorseOffset(PlayerController player, Vector3 localOffset)
    {
        if (player == null) return;
        player.transform.SetPositionAndRotation(GetHorseOffsetGroundPosition(localOffset),transform.rotation);
    }

    Vector3 GetHorseOffsetGroundPosition(Vector3 localOffset)
    {
        // Offset memakai world unit agar tidak ikut membesar saat scale horse berubah.
        Vector3 candidate=transform.position+transform.rotation*localOffset;
        return TryGround(candidate,out Vector3 ground)?ground:candidate;
    }

    void CacheMountedPlayerHips(PlayerController player)
    {
        Animator riderAnimator=player!=null?player.CharacterAnimator:null;
        mountedPlayerHips=riderAnimator!=null&&riderAnimator.isHuman
            ?riderAnimator.GetBoneTransform(HumanBodyBones.Hips)
            :null;
    }

    void BeginRiderTransition(RiderTransition transition,Vector3 endRoot)
    {
        riderTransition=transition;
        riderTransitionStartedAt=Time.time;
        riderTransitionRotation=transform.rotation;
        riderTransitionEndRoot=endRoot;
        riderTransitionStartHips=mountedPlayerHips!=null
            ?mountedPlayerHips.position
            :mountedPlayer.transform.position;
    }

    void UpdateRiderTransitionPose()
    {
        if(mountedPlayer==null) return;
        float duration=riderTransition==RiderTransition.Mounting?mountActionDuration:dismountActionDuration;
        float clipProgress=duration<=0f?1f:Mathf.Clamp01((Time.time-riderTransitionStartedAt)/duration);
        float totalFrames=riderTransition==RiderTransition.Mounting?75f:90f;
        float currentFrame=clipProgress*totalFrames;
        float startFrame=riderTransition==RiderTransition.Mounting?mountMoveStartFrame:dismountMoveStartFrame;
        float endFrame=riderTransition==RiderTransition.Mounting?mountSeatFrame:dismountGroundFrame;
        Vector3 endHips=riderTransition==RiderTransition.Mounting
            ?GetSeatWorldPosition()
            :riderTransitionEndRoot+riderTransitionRotation*standingHipsLocalOffset;
        float frameProgress=Mathf.InverseLerp(startFrame,Mathf.Max(startFrame+1f,endFrame),currentFrame);
        frameProgress=frameProgress*frameProgress*(3f-2f*frameProgress);
        float verticalDistance=endHips.y-riderTransitionStartHips.y;
        float verticalProgress=Mathf.Abs(verticalDistance)>0.05f&&mountedPlayerHips!=null
            ?Mathf.Clamp01((mountedPlayerHips.position.y-riderTransitionStartHips.y)/verticalDistance)
            :0f;
        verticalProgress=verticalProgress*verticalProgress*(3f-2f*verticalProgress);
        // Pose vertikal FBX menjadi sumber timing utama. Frame progress menjadi
        // fallback agar posisi horizontal tetap selesai bila retargeting Y sedikit meleset.
        float progress=Mathf.Max(frameProgress,verticalProgress);
        Vector3 desiredHips=Vector3.Lerp(riderTransitionStartHips,endHips,progress);
        mountedPlayer.transform.rotation=riderTransitionRotation;
        if(mountedPlayerHips!=null)
        {
            // FBX terbaru mengatur gerak vertikal naik/turun. Script hanya
            // mengoreksi arah mendatar agar satu klip dapat dipakai dari kiri/kanan.
            Vector3 horizontalCorrection=desiredHips-mountedPlayerHips.position;
            horizontalCorrection.y=0f;
            mountedPlayer.transform.position+=horizontalCorrection;
        }
        else
        {
            Vector3 horizontalTarget=Vector3.Lerp(mountedPlayer.transform.position,riderTransitionEndRoot,progress);
            horizontalTarget.y=mountedPlayer.transform.position.y;
            mountedPlayer.transform.position=horizontalTarget;
        }
    }

    static void SetHorseAnimationMirror(PlayerController player,bool mirrored)
    {
        Animator riderAnimator=player!=null?player.CharacterAnimator:null;
        if(riderAnimator==null) return;
        foreach(AnimatorControllerParameter parameter in riderAnimator.parameters)
        {
            if(parameter.name!=HorseMountMirrorParameter) continue;
            riderAnimator.SetBool(HorseMountMirrorParameter,mirrored);
            return;
        }
    }

    void RestoreMountedPlayerColliders()
    {
        foreach(var pair in mountedPlayerColliders)
            if(pair.Key!=null) pair.Key.enabled=pair.Value;
        mountedPlayerColliders.Clear();
    }

    Vector3 GetSeatWorldPosition()
    {
        Transform seat=mountPoint!=null?mountPoint:transform;
        Vector3 seatPosition=mountPoint!=null?mountPoint.position:transform.TransformPoint(mountLocalPosition);
        if(hasAnimatedSeatCalibration&&animatedSeatBone!=null)
            seatPosition=animatedSeatBone.TransformPoint(animatedSeatLocalOffset);
        else if(saddleRenderer!=null&&saddleRenderer.gameObject.activeInHierarchy&&saddleRenderer.enabled)
            seatPosition.y=saddleRenderer.bounds.center.y;
        else if(animatedSeatBone!=null)
            seatPosition.y=animatedSeatBone.position.y;
        return seatPosition+seat.rotation*riderSeatOffset;
    }

    void SnapRiderToSeat(bool immediate = false)
    {
        if (mountedPlayer == null)
            return;

        Transform seat = mountPoint != null ? mountPoint : transform;
        Vector3 seatPosition = mountPoint != null
            ? mountPoint.position
            : transform.TransformPoint(mountLocalPosition);
        // Offset ini milik anatomi rider (ukurannya tetap), bukan anatomi horse.
        // Rotasi mengikuti dudukan, tetapi scale root horse tidak boleh menggandakan
        // koreksi vertikal karena akan menarik hips masuk ke badan horse adult.
        Vector3 seatOffset = seat.rotation * riderSeatOffset;
        Quaternion desiredSeatLocalRotation = Quaternion.identity;

        if (hasAnimatedSeatCalibration && animatedSeatBone != null)
        {
            // Posisi ini bergerak bersama Def_Spine, bukan bounds renderer yang relatif
            // statis. Karena itu rider ikut naik-turun dan maju-mundur bersama gait kuda.
            seatPosition = animatedSeatBone.TransformPoint(animatedSeatLocalOffset);
            Quaternion currentRelativeRotation = Quaternion.Inverse(transform.rotation) * animatedSeatBone.rotation;
            Quaternion animationDelta = currentRelativeRotation * Quaternion.Inverse(animatedSeatRestRelativeRotation);
            Quaternion followedDelta = Quaternion.Slerp(Quaternion.identity, animationDelta, riderSpineRotationFollow);
            Quaternion desiredWorldRotation = transform.rotation * followedDelta;
            desiredSeatLocalRotation = Quaternion.Inverse(seat.rotation) * desiredWorldRotation;
        }
        // Gunakan pusat pelana sebagai baseline. Offset kecil di Inspector menjadi
        // kalibrasi akhir terhadap pose Riding Idle milik player.
        else if (saddleRenderer != null && saddleRenderer.gameObject.activeInHierarchy && saddleRenderer.enabled)
            seatPosition.y = saddleRenderer.bounds.center.y;
        else if (animatedSeatBone != null)
            seatPosition.y = animatedSeatBone.position.y;
        seatPosition += seatOffset;

        float followBlend = immediate || mountTransitionBusy
            ? 1f
            : 1f - Mathf.Exp(-riderSeatFollowSharpness * Time.deltaTime);
        mountedPlayer.transform.localRotation = Quaternion.Slerp(
            mountedPlayer.transform.localRotation, desiredSeatLocalRotation, followBlend);
        if (mountedPlayerHips != null)
        {
            // Hips adalah acuan pantat karakter. Memindahkan root dengan selisih ini
            // menjaga pose/gerakan kaki tetap hidup tanpa membuat rider melayang.
            Vector3 targetRoot = mountedPlayer.transform.position + seatPosition - mountedPlayerHips.position;
            mountedPlayer.transform.position = Vector3.Lerp(mountedPlayer.transform.position, targetRoot, followBlend);
            return;
        }

        // Fallback untuk avatar non-Humanoid: perkirakan jarak root-ke-pantat dari
        // CharacterController sehingga root/kaki tidak ditempelkan ke atas saddle.
        CharacterController controller = mountedPlayer.GetComponent<CharacterController>();
        float approximateHipHeight = controller != null ? controller.height * 0.52f : 0.9f;
        Vector3 fallbackRoot = seatPosition - seat.up * approximateHipHeight;
        mountedPlayer.transform.position = Vector3.Lerp(mountedPlayer.transform.position, fallbackRoot, followBlend);
    }

    void ResolveSeatReferences()
    {
        saddleRenderer = null;
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer candidate = renderers[i];
            if (candidate != null && candidate.gameObject.activeInHierarchy && candidate.enabled &&
                candidate.name.IndexOf("saddle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                saddleRenderer = candidate;
                break;
            }
        }

        animatedSeatBone = null;
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform candidate = children[i];
            if (candidate != null && string.Equals(candidate.name, "Def_Spine", StringComparison.OrdinalIgnoreCase))
            {
                animatedSeatBone = candidate;
                break;
            }
        }

        hasAnimatedSeatCalibration = animatedSeatBone != null;
        if (hasAnimatedSeatCalibration)
        {
            Vector3 calibratedSeatPosition = mountPoint != null
                ? mountPoint.position
                : transform.TransformPoint(mountLocalPosition);
            if (saddleRenderer != null && saddleRenderer.gameObject.activeInHierarchy && saddleRenderer.enabled)
                calibratedSeatPosition.y = saddleRenderer.bounds.center.y;
            animatedSeatLocalOffset = animatedSeatBone.InverseTransformPoint(calibratedSeatPosition);
            animatedSeatRestRelativeRotation = Quaternion.Inverse(transform.rotation) * animatedSeatBone.rotation;
        }
    }

    void DismountImmediate()
    {
        PlayerController player=mountedPlayer;
        if(player==null) return;
        player.SetRidingAnimation(false);
        DetachMountedPlayer(player);
        player.ReleaseMovementLock(this);
        mountedPlayer=null;
        mountTransitionBusy=false;
    }

    void UpdateMounted()
    {
        if (GameplayInput.GetKeyDown(KeyCode.E)) { Dismount(); return; }
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
            mountedMoveDirection = direction;
        }

        float targetSpeed = mountedSpeed * input.magnitude;
        float speedChange = targetSpeed > mountedCurrentSpeed ? mountedAcceleration : mountedDeceleration;
        mountedCurrentSpeed = Mathf.MoveTowards(mountedCurrentSpeed, targetSpeed, speedChange * Time.deltaTime);
        if (mountedMoveDirection.sqrMagnitude < 0.0001f)
            mountedMoveDirection = transform.forward;
        Vector3 horizontalDelta = mountedMoveDirection.normalized * mountedCurrentSpeed * Time.deltaTime;
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

        if (mountedGrounded && GameplayInput.GetKeyDown(KeyCode.Space) && Time.time >= nextMountedJumpTime)
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
            ApplyLocomotion(Mathf.Clamp01(mountedCurrentSpeed / Mathf.Max(0.01f, mountedSpeed)));
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
        if (locomotionController != null)
        {
            ApplyLocomotion(moving ? running ? 1f : 0.45f : 0f);
            return;
        }
        RuntimeAnimatorController desired = moving ? running && runController != null ? runController : moveController : idleController;
        ApplyController(desired);
    }

    void ApplyLocomotion(float normalizedSpeed)
    {
        if (animator == null) return;
        if (locomotionController == null)
        {
            ApplyAnimation(normalizedSpeed > 0.03f, normalizedSpeed > 0.62f);
            return;
        }
        ApplyController(locomotionController);
        animator.SetFloat(locomotionSpeedParameter, Mathf.Clamp01(normalizedSpeed), 0.16f, Time.deltaTime);
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
