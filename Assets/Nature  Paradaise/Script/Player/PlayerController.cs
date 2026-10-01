using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
/// <summary>
/// Controller movement PC/mobile berbasis kamera yang menangani walk, run, sprint, jump,
/// slope, step, facing, carry penalty, stamina, dan movement lock.
/// </summary>
public sealed class PlayerController : MonoBehaviour
{
    /// <summary>State gerakan yang dapat dipakai animator, audio, dan gameplay eksternal.</summary>
    public enum MovementMode { Idle, Walk, Run, Sprint, Airborne }

    [Header("Speed")]
    [SerializeField, Min(0f)] float walkSpeed = 2.2f;
    [FormerlySerializedAs("moveSpeed")]
    [SerializeField, Min(0f)] float runSpeed = 4f;
    [SerializeField, Min(0f)] float sprintSpeed = 6.2f;
    [SerializeField, Range(0.1f, 1f)] float analogRunThreshold = 0.72f;
    [SerializeField, Range(0.1f, 1f)] float carrySpeedMultiplier = 0.82f;

    [Header("Night Fatigue")]
    [SerializeField, Range(0.1f, 1f), Tooltip("Pengali kecepatan gerak ketika Fatigue mencapai 1.")]
    float maximumFatigueSpeedMultiplier = 0.7f;
    [SerializeField, Range(0.1f, 1f), Tooltip("Durasi fade layer Stagger Overlay.")]
    float fatigueBlendTime = 0.45f;

    [Header("Rotation")]
    [SerializeField, Min(0f)] float rotationSpeed = 720f;
    [SerializeField] bool cameraRelativeMovement = true;
    [SerializeField] Camera movementCamera;

    [Header("Jump & Ground")]
    [SerializeField, Min(0.1f)] float jumpHeight = 1.15f;
    [SerializeField] float gravity = -25f;
    [SerializeField, Min(0f)] float groundedForce = 2f;
    [SerializeField, Min(0f)] float jumpStaminaCost = 0.1f;
    [SerializeField, Min(0f), Tooltip("Jarak horizontal total JumpingForward dalam world unit.")]
    float forwardJumpDistance = 4.5f;
    [SerializeField, Range(0f, 0.5f), Tooltip("Jeda sebelum standing jump mulai naik agar takeoff fisik mengikuti pose animasi.")]
    float standingJumpTakeoffDelay = 0.16f;

    [Header("Run / Sprint Stamina (No Passive Recovery)")]
    [SerializeField] PlayerStatusSystem status;
    [SerializeField, Min(0f), Tooltip("Biaya stamina per detik saat Run atau Sprint; mendukung nilai pecahan.")]
    float sprintDrainPerSecond = 0.025f;
    [SerializeField, Range(0.05f, 0.5f)] float staminaUpdateInterval = 0.2f;

    [Header("PC Input")]
    [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] KeyCode walkKey = KeyCode.LeftAlt;
    [SerializeField] KeyCode jumpKey = KeyCode.Space;

    [Header("Animation (Optional)")]
    [SerializeField] Animator animator;
    [SerializeField] string speedParameter = "Speed";
    [SerializeField] string groundedParameter = "Grounded";
    [SerializeField] string sprintParameter = "Sprint";
    [SerializeField] string carryParameter = "Carry";
    [SerializeField] string jumpTrigger = "Jump";
    [SerializeField] string jumpForwardTrigger = "JumpForward";
    [SerializeField] string pickupTrigger = "PickUp";
    [SerializeField] string pickupWaistTrigger = "PickUpWaist";
    [SerializeField] string knockOutTrigger = "KnockOut";
    [SerializeField] string wakeUpTrigger = "WakeUp";
    [SerializeField] string milkingTrigger = "Milking";
    [SerializeField] string pushingTrigger = "Pushing";
    [SerializeField] string wateringTrigger = "Watering";
    [SerializeField] string hoeingTrigger = "Hoeing";
    [SerializeField] string plantingTrigger = "Planting";
    [SerializeField] string refillWateringCanTrigger = "RefillWateringCan";
    [SerializeField] string handOverOneHandTrigger = "HandOverOneHand";
    [SerializeField] string handOverTwoHandsTrigger = "HandOverTwoHands";
    [SerializeField] string holdingItemParameter = "HoldingItem";
    [SerializeField] string carryingAnimalParameter = "CarryingAnimal";
    [SerializeField] string placeItemTrigger = "PlaceItem";
    [SerializeField] string brushAnimalTrigger = "BrushAnimal";
    [SerializeField] string brushingParameter = "BrushingActive";
    [SerializeField] string mountHorseTrigger = "MountHorse";
    [SerializeField] string dismountHorseTrigger = "DismountHorse";
    [SerializeField] string ridingParameter = "Riding";
    [SerializeField] string pickUpChickenTrigger = "PickUpChicken";
    [SerializeField] string placeChickenTrigger = "PlaceChicken";
    [SerializeField] string wakeUpBedTrigger = "WakeUpBed";
    [SerializeField] string yawnTrigger = "Yawn";
    [SerializeField] string shearSheepTrigger = "ShearSheep";
    [SerializeField] string scoopManureTrigger = "ScoopManure";
    [SerializeField] string shearingParameter = "ShearingActive";
    [SerializeField] string tiredParameter = "Tired";
    [SerializeField] string fatigueParameter = "Fatigue";
    [SerializeField] string fatigueLocomotionSpeedParameter = "FatigueLocomotionSpeed";
    [SerializeField] string fatigueLayerName = "Fatigue";

    CharacterController characterController;
    readonly HashSet<object> movementLocks = new();
    Vector2 externalMobileInput;
    Vector3 planarVelocity;
    Vector3 forwardJumpVelocity;
    float verticalVelocity;
    float staminaTimer;
    bool mobileSprintHeld;
    bool jumpRequested;
    bool forwardJumpActive;
    bool standingJumpPending;
    float standingJumpTimer;
    float nightFatigueLevel;
    float fatigueLayerWeight;
    float fatigueLayerVelocity;
    bool manualLock;
    bool isCarrying;
    bool isPushingAnimation;
    bool isHoldingItemAnimation;
    bool isCarryingAnimalAnimation;
    bool isRidingAnimation;
    bool hasSpeedParameter;
    bool hasGroundedParameter;
    bool hasSprintParameter;
    bool hasCarryParameter;
    bool hasJumpTrigger;
    bool hasJumpForwardTrigger;
    bool hasPickupTrigger;
    bool hasPickupWaistTrigger;
    bool hasKnockOutTrigger;
    bool hasWakeUpTrigger;
    bool hasMilkingTrigger;
    bool hasPushingTrigger;
    bool hasWateringTrigger;
    bool hasHoeingTrigger;
    bool hasPlantingTrigger;
    bool hasRefillWateringCanTrigger;
    bool hasHandOverOneHandTrigger;
    bool hasHandOverTwoHandsTrigger;
    bool hasHoldingItemParameter;
    bool hasCarryingAnimalParameter;
    bool hasPlaceItemTrigger;
    bool hasBrushAnimalTrigger;
    bool hasBrushingParameter;
    bool hasMountHorseTrigger;
    bool hasDismountHorseTrigger;
    bool hasRidingParameter;
    bool hasPickUpChickenTrigger;
    bool hasPlaceChickenTrigger;
    bool hasWakeUpBedTrigger;
    bool hasYawnTrigger;
    bool hasShearSheepTrigger;
    bool hasScoopManureTrigger;
    bool hasShearingParameter;
    bool hasTiredParameter;
    bool hasFatigueParameter;
    bool hasFatigueLocomotionSpeedParameter;
    int fatigueLayerIndex = -1;

    public MovementMode CurrentMode { get; private set; }
    public Vector3 PlanarVelocity => planarVelocity;
    public float CurrentSpeed => planarVelocity.magnitude;
    public bool IsGrounded => characterController != null && characterController.enabled && characterController.isGrounded;
    public bool IsMovementLocked => manualLock || movementLocks.Count > 0;
    public bool IsCarrying => isCarrying;
    public Animator CharacterAnimator => animator;
    public Vector3 FacingDirection { get; private set; } = Vector3.forward;
    public event Action<MovementMode> MovementModeChanged;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (status == null) status = GetComponent<PlayerStatusSystem>();
        if (movementCamera == null) movementCamera = Camera.main;
        ResolveRigAnimator();

        FacingDirection = transform.forward.sqrMagnitude > 0.01f ? transform.forward.normalized : Vector3.forward;
        CacheAnimatorParameters();
        if(GetComponent<PlayerAnimalPush>()==null) gameObject.AddComponent<PlayerAnimalPush>();
        if (GetComponent<FootstepAudio>() == null)
            gameObject.AddComponent<FootstepAudio>();
    }

    /// <summary>
    /// Memilih Animator yang berada langsung pada root skeleton FBX. Prefab versi lama
    /// menaruh Animator kedua pada wrapper PlayerVisual; Humanoid lalu menghitung pose
    /// dalam ruang transform yang salah karena model di bawahnya diskalakan.
    /// </summary>
    void ResolveRigAnimator()
    {
        Animator previous = animator;
        Animator[] candidates = GetComponentsInChildren<Animator>(true);
        Animator rigAnimator = null;
        int bestScore = -1;

        foreach (Animator candidate in candidates)
        {
            if (candidate.avatar == null || !candidate.avatar.isValid || !candidate.avatar.isHuman ||
                candidate.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
                continue;

            int depth = 0;
            for (Transform current = candidate.transform; current != null && current != transform; current = current.parent)
                depth++;
            // Animator yang sudah memiliki controller lebih layak daripada wrapper/bone
            // Animator yang lebih dalam tetapi kosong.
            int score = depth + (candidate.runtimeAnimatorController != null ? 1000 : 0);
            if (score <= bestScore) continue;
            rigAnimator = candidate;
            bestScore = score;
        }

        if (rigAnimator == null)
        {
            animator = previous != null ? previous : GetComponentInChildren<Animator>(true);
            return;
        }

        animator = rigAnimator;
    }

    void Update()
    {
        UpdateNightFatigue();
        // Saat player menunggang horse, CharacterController sengaja dinonaktifkan agar
        // collider player tidak melawan gerakan mount. Jangan memanggil Move pada state itu.
        if (characterController == null || !characterController.enabled || !gameObject.activeInHierarchy)
        {
            planarVelocity = Vector3.zero;
            verticalVelocity = 0f;
            SetMovementMode(MovementMode.Idle);
            UpdateAnimator(true, MovementMode.Idle);
            return;
        }

        if (movementCamera == null) movementCamera = Camera.main;

        Vector2 input = IsMovementLocked ? Vector2.zero : ReadMovementInput();
        Vector3 direction = ToCameraRelativeDirection(input);
        bool groundedBeforeMove = characterController.isGrounded;
        if (groundedBeforeMove && verticalVelocity < 0f)
            verticalVelocity = -groundedForce;

        UpdatePendingStandingJump(groundedBeforeMove);

        if (!IsMovementLocked && (Input.GetKeyDown(jumpKey) || jumpRequested))
            TryJump(groundedBeforeMove, direction);
        jumpRequested = false;

        Vector3 facingDirection = direction;
        if (facingDirection.sqrMagnitude > 0.001f)
            FacingDirection = facingDirection.normalized;
        float inputMagnitude = Mathf.Clamp01(input.magnitude);
        MovementMode mode = ResolveMovementMode(inputMagnitude, groundedBeforeMove);
        float targetSpeed = ResolveSpeed(mode, inputMagnitude);
        if (isCarrying) targetSpeed *= carrySpeedMultiplier;
        targetSpeed *= FatigueMovementMultiplier;

        planarVelocity = forwardJumpActive ? forwardJumpVelocity : direction * targetSpeed;
        if (facingDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(facingDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        UpdateStamina(mode);
        verticalVelocity += gravity * Time.deltaTime;
        CollisionFlags collision = characterController.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
        if ((collision & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;

        bool groundedAfterMove = characterController.isGrounded;
        if (forwardJumpActive && groundedAfterMove && verticalVelocity <= 0f)
        {
            forwardJumpActive = false;
            forwardJumpVelocity = Vector3.zero;
        }
        if (!groundedAfterMove && verticalVelocity > 0.01f)
            mode = MovementMode.Airborne;
        SetMovementMode(mode);
        UpdateAnimator(groundedAfterMove, mode);
    }

    Vector2 ReadMovementInput()
    {
        Vector2 keyboard = new(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        Vector2 joystick = MobileJoystick.Active != null ? MobileJoystick.Active.Direction : externalMobileInput;
        Vector2 input = joystick.sqrMagnitude > keyboard.sqrMagnitude ? joystick : keyboard;
        return Vector2.ClampMagnitude(input, 1f);
    }

    Vector3 ToCameraRelativeDirection(Vector2 input)
    {
        Vector3 direction = new(input.x, 0f, input.y);
        if (!cameraRelativeMovement || movementCamera == null)
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;

        Vector3 forward = movementCamera.transform.forward;
        Vector3 right = movementCamera.transform.right;
        forward.y = right.y = 0f;
        forward.Normalize();
        right.Normalize();
        direction = forward * input.y + right * input.x;
        return direction.sqrMagnitude > 1f ? direction.normalized : direction;
    }

    /// <summary>
    /// Membaca arah input dunia dengan aturan kamera yang sama seperti locomotion,
    /// termasuk joystick. Tetap dapat dibaca sistem interaksi saat movement dikunci.
    /// </summary>
    public Vector3 ReadWorldMovementDirection() => ToCameraRelativeDirection(ReadMovementInput());

    MovementMode ResolveMovementMode(float inputMagnitude, bool grounded)
    {
        if (!grounded && verticalVelocity > 0.01f) return MovementMode.Airborne;
        if (inputMagnitude <= 0.01f) return MovementMode.Idle;

        bool wantsSprint = (Input.GetKey(sprintKey) || mobileSprintHeld) && status != null &&
                           !status.IsExhausted && status.CanSpendStamina(0.01f);
        if (wantsSprint) return MovementMode.Sprint;
        // Tombol keyboard selalu bernilai penuh, jadi W sebelumnya langsung dianggap Run.
        // Keyboard normal sekarang Walk; Shift menjadi lari. Stick analog tetap dapat
        // memilih Walk/Run berdasarkan besar input.
        bool keyboardMovement = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                                Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) ||
                                Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) ||
                                Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);
        if (keyboardMovement || Input.GetKey(walkKey) || inputMagnitude < analogRunThreshold)
            return MovementMode.Walk;
        return MovementMode.Run;
    }

    float ResolveSpeed(MovementMode mode, float inputMagnitude)
    {
        return mode switch
        {
            MovementMode.Walk => walkSpeed * Mathf.Clamp01(inputMagnitude / analogRunThreshold),
            MovementMode.Run => runSpeed * inputMagnitude,
            MovementMode.Sprint => sprintSpeed * inputMagnitude,
            _ => 0f
        };
    }

    void TryJump(bool grounded, Vector3 jumpDirection)
    {
        if (!grounded || standingJumpPending || status == null || !status.TrySpendStamina(jumpStaminaCost)) return;
        bool movingForward = jumpDirection.sqrMagnitude > 0.01f;
        if (movingForward)
        {
            verticalVelocity = CalculateJumpVelocity();
            forwardJumpActive = true;
            forwardJumpVelocity = jumpDirection.normalized * CalculateForwardJumpSpeed();
        }
        else
        {
            standingJumpPending = true;
            standingJumpTimer = standingJumpTakeoffDelay;
        }
        if (animator == null) return;
        if (movingForward && hasJumpForwardTrigger)
            animator.SetTrigger(jumpForwardTrigger);
        else if (hasJumpTrigger)
            animator.SetTrigger(jumpTrigger);
    }

    void UpdatePendingStandingJump(bool grounded)
    {
        if (!standingJumpPending) return;
        standingJumpTimer -= Time.deltaTime;
        if (standingJumpTimer > 0f) return;
        standingJumpPending = false;
        // Tetap lakukan takeoff bila tepi collider membuat isGrounded berkedip satu frame.
        if (grounded || verticalVelocity <= 0f)
            verticalVelocity = CalculateJumpVelocity();
    }

    float CalculateJumpVelocity() => Mathf.Sqrt(jumpHeight * -2f * gravity);

    float CalculateForwardJumpSpeed()
    {
        float flightTime = 2f * CalculateJumpVelocity() / -gravity;
        return forwardJumpDistance / Mathf.Max(0.01f, flightTime);
    }

    void UpdateStamina(MovementMode mode)
    {
        if (status == null) return;

        bool running = mode is MovementMode.Run or MovementMode.Sprint && planarVelocity.sqrMagnitude > 0.01f;
        if (running)
        {
            staminaTimer += Time.deltaTime;
            if (staminaTimer >= staminaUpdateInterval)
            {
                float elapsed = staminaTimer;
                staminaTimer = 0f;
                status.TrySpendStamina(sprintDrainPerSecond * elapsed);
            }
            return;
        }

        // Stamina tidak pulih karena diam/jalan. Recovery hanya datang dari sumber gameplay
        // eksplisit seperti makanan, tidur, dan nantinya hot spring.
        staminaTimer = 0f;
    }

    void SetMovementMode(MovementMode mode)
    {
        if (CurrentMode == mode) return;
        CurrentMode = mode;
        MovementModeChanged?.Invoke(mode);
    }

    void UpdateAnimator(bool grounded, MovementMode mode)
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return;
        // Pertahankan pilihan Idle/Walk/Run dari input aslinya. Perlambatan cycle dilakukan
        // oleh FatigueLocomotionSpeed, jadi blend tree tidak ikut merosot ke arah Idle.
        if (hasSpeedParameter) animator.SetFloat(speedParameter,
            CurrentSpeed / Mathf.Max(0.01f,sprintSpeed*FatigueMovementMultiplier),0.12f,Time.deltaTime);
        if (hasGroundedParameter) animator.SetBool(groundedParameter, grounded);
        if (hasSprintParameter) animator.SetBool(sprintParameter, mode == MovementMode.Sprint);
        if (hasCarryParameter) animator.SetBool(carryParameter, isCarrying);
        if (hasTiredParameter)
            animator.SetBool(tiredParameter,status != null && status.IsExhausted && grounded && CurrentSpeed<0.05f && !IsMovementLocked);
    }

    void UpdateNightFatigue()
    {
        float targetNightFatigue=CalculateNightFatigue();
        float blendTime=Mathf.Max(0.01f,fatigueBlendTime);
        nightFatigueLevel=Mathf.MoveTowards(nightFatigueLevel,targetNightFatigue,Time.deltaTime/blendTime);

        bool sleepingOrFainted=status!=null && (status.IsFainted ||
            status.CurrentMovementState is PlayerMovementState.Sleeping or PlayerMovementState.Faint);
        float targetLayerWeight=nightFatigueLevel;
        if(sleepingOrFainted || isRidingAnimation || isPushingAnimation || IsMovementLocked)
            targetLayerWeight=0f;
        else if(isCarrying || isHoldingItemAnimation || isCarryingAnimalAnimation)
            targetLayerWeight*=0.2f;

        if(sleepingOrFainted)
        {
            fatigueLayerWeight=0f;
            fatigueLayerVelocity=0f;
        }
        else
            fatigueLayerWeight=Mathf.SmoothDamp(fatigueLayerWeight,targetLayerWeight,
                ref fatigueLayerVelocity,blendTime,Mathf.Infinity,Time.deltaTime);

        if(animator==null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController==null) return;
        if(hasFatigueParameter) animator.SetFloat(fatigueParameter,fatigueLayerWeight);
        if(hasFatigueLocomotionSpeedParameter)
            animator.SetFloat(fatigueLocomotionSpeedParameter,FatigueMovementMultiplier);
        if(fatigueLayerIndex>=0 && fatigueLayerIndex<animator.layerCount)
            animator.SetLayerWeight(fatigueLayerIndex,fatigueLayerWeight);
    }

    static float CalculateNightFatigue()
    {
        if(TimeManager.Instance==null) return 0f;
        float time=TimeManager.Instance.CurrentTimeHours;
        if(time>=6f) return 0f;
        if(time<1f) return Mathf.Lerp(0f,0.3f,time);
        if(time<2f) return Mathf.Lerp(0.3f,0.7f,time-1f);
        if(time<3f) return Mathf.Lerp(0.7f,1f,time-2f);
        return 1f;
    }

    float FatigueMovementMultiplier =>
        Mathf.Lerp(1f,maximumFatigueSpeedMultiplier,nightFatigueLevel);

    void CacheAnimatorParameters()
    {
        if (animator == null) return;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            string name = parameter.name;
            if (name == speedParameter) hasSpeedParameter = true;
            if (name == groundedParameter) hasGroundedParameter = true;
            if (name == sprintParameter) hasSprintParameter = true;
            if (name == carryParameter) hasCarryParameter = true;
            if (name == jumpTrigger) hasJumpTrigger = true;
            if (name == jumpForwardTrigger) hasJumpForwardTrigger = true;
            if (name == pickupTrigger) hasPickupTrigger = true;
            if (name == pickupWaistTrigger) hasPickupWaistTrigger = true;
            if (name == knockOutTrigger) hasKnockOutTrigger = true;
            if (name == wakeUpTrigger) hasWakeUpTrigger = true;
            if (name == milkingTrigger) hasMilkingTrigger = true;
            if (name == pushingTrigger) hasPushingTrigger = true;
            if (name == wateringTrigger) hasWateringTrigger = true;
            if (name == hoeingTrigger) hasHoeingTrigger = true;
            if (name == plantingTrigger) hasPlantingTrigger = true;
            if (name == refillWateringCanTrigger) hasRefillWateringCanTrigger = true;
            if (name == handOverOneHandTrigger) hasHandOverOneHandTrigger = true;
            if (name == handOverTwoHandsTrigger) hasHandOverTwoHandsTrigger = true;
            if (name == holdingItemParameter) hasHoldingItemParameter = true;
            if (name == carryingAnimalParameter) hasCarryingAnimalParameter = true;
            if (name == placeItemTrigger) hasPlaceItemTrigger = true;
            if (name == brushAnimalTrigger) hasBrushAnimalTrigger = true;
            if (name == brushingParameter) hasBrushingParameter = true;
            if (name == mountHorseTrigger) hasMountHorseTrigger = true;
            if (name == dismountHorseTrigger) hasDismountHorseTrigger = true;
            if (name == ridingParameter) hasRidingParameter = true;
            if (name == pickUpChickenTrigger) hasPickUpChickenTrigger = true;
            if (name == placeChickenTrigger) hasPlaceChickenTrigger = true;
            if (name == wakeUpBedTrigger) hasWakeUpBedTrigger = true;
            if (name == yawnTrigger) hasYawnTrigger = true;
            if (name == shearSheepTrigger) hasShearSheepTrigger = true;
            if (name == scoopManureTrigger) hasScoopManureTrigger = true;
            if (name == shearingParameter) hasShearingParameter = true;
            if (name == tiredParameter) hasTiredParameter = true;
            if (name == fatigueParameter) hasFatigueParameter = true;
            if (name == fatigueLocomotionSpeedParameter) hasFatigueLocomotionSpeedParameter = true;
        }
        fatigueLayerIndex=animator.GetLayerIndex(fatigueLayerName);
    }

    void PlayTrigger(string trigger, bool available)
    {
        if (available && animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            animator.SetTrigger(trigger);
    }

    public void PlayPickupAnimation() => PlayTrigger(pickupTrigger, hasPickupTrigger);
    public void PlayPickUpWaistAnimation() => PlayTrigger(pickupWaistTrigger, hasPickupWaistTrigger);
    public void PlayKnockOutAnimation()
    {
        ClearFatigueOverlay();
        PlayTrigger(knockOutTrigger,hasKnockOutTrigger);
    }
    public void PlayWakeUpAnimation() => PlayTrigger(wakeUpTrigger, hasWakeUpTrigger);
    public void PlayMilkingAnimation() => PlayTrigger(milkingTrigger, hasMilkingTrigger);
    public void PlayPushingAnimation() => SetPushingAnimation(true);
    public void SetPushingAnimation(bool pushing)
    {
        isPushingAnimation=pushing;
        if(hasPushingTrigger && animator!=null)
            animator.SetBool(pushingTrigger,pushing);
    }
    public void PlayWateringAnimation() => PlayTrigger(wateringTrigger, hasWateringTrigger);
    public void PlayHoeingAnimation() => PlayTrigger(hoeingTrigger, hasHoeingTrigger);
    public void PlayPlantingAnimation() => PlayTrigger(plantingTrigger, hasPlantingTrigger);
    public void PlayRefillWateringCanAnimation() => PlayTrigger(refillWateringCanTrigger, hasRefillWateringCanTrigger);
    public void PlayHandOverAnimation(bool twoHands = false) => PlayTrigger(
        twoHands ? handOverTwoHandsTrigger : handOverOneHandTrigger,
        twoHands ? hasHandOverTwoHandsTrigger : hasHandOverOneHandTrigger);
    public void SetHoldingItemAnimation(bool holding)
    {
        isHoldingItemAnimation=holding;
        if (hasHoldingItemParameter && animator != null)
            animator.SetBool(holdingItemParameter, holding);
    }
    public void SetCarryingAnimalAnimation(bool carrying)
    {
        isCarryingAnimalAnimation=carrying;
        if (hasCarryingAnimalParameter && animator != null)
            animator.SetBool(carryingAnimalParameter,carrying);
    }
    public void PlayPlaceItemAnimation() => PlayTrigger(placeItemTrigger, hasPlaceItemTrigger);
    public void PlayBrushAnimalAnimation() => PlayTrigger(brushAnimalTrigger, hasBrushAnimalTrigger);
    public void SetBrushingAnimation(bool active)
    {
        if (animator == null || animator.runtimeAnimatorController == null || (active && !animator.isActiveAndEnabled)) return;
        if (hasBrushingParameter) animator.SetBool(brushingParameter,active);
        if (active) PlayBrushAnimalAnimation();
        else if (hasBrushAnimalTrigger) animator.ResetTrigger(brushAnimalTrigger);
    }
    public void PlayMountHorseAnimation() => PlayTrigger(mountHorseTrigger, hasMountHorseTrigger);
    public void PlayDismountHorseAnimation() => PlayTrigger(dismountHorseTrigger, hasDismountHorseTrigger);
    public void PlayPickUpChickenAnimation() => PlayTrigger(pickUpChickenTrigger, hasPickUpChickenTrigger);
    public void PlayPlaceChickenAnimation() => PlayTrigger(placeChickenTrigger, hasPlaceChickenTrigger);
    public void PlayWakeUpBedAnimation() => PlayTrigger(wakeUpBedTrigger, hasWakeUpBedTrigger);
    public void PlayYawnAnimation() => PlayTrigger(yawnTrigger, hasYawnTrigger);
    public void PlayShearSheepAnimation() => PlayTrigger(shearSheepTrigger, hasShearSheepTrigger);
    public void PlayScoopManureAnimation() => PlayTrigger(scoopManureTrigger, hasScoopManureTrigger);
    public void SetShearingAnimation(bool active)
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return;
        if (hasShearingParameter) animator.SetBool(shearingParameter, active);
        if (active) PlayShearSheepAnimation();
        else if (hasShearSheepTrigger) animator.ResetTrigger(shearSheepTrigger);
    }
    public void SetRidingAnimation(bool riding)
    {
        isRidingAnimation=riding;
        if (hasRidingParameter && animator != null)
            animator.SetBool(ridingParameter, riding);
    }

    void ClearFatigueOverlay()
    {
        fatigueLayerWeight=0f;
        fatigueLayerVelocity=0f;
        if(animator==null) return;
        if(hasFatigueParameter) animator.SetFloat(fatigueParameter,0f);
        if(fatigueLayerIndex>=0 && fatigueLayerIndex<animator.layerCount)
            animator.SetLayerWeight(fatigueLayerIndex,0f);
    }

    // Token lock mencegah satu sistem membuka movement yang masih dikunci sistem lain.
    /// <summary>Menahan movement untuk satu owner; aman dipakai beberapa sistem sekaligus.</summary>
    public void AcquireMovementLock(object owner) { if (owner != null) movementLocks.Add(owner); }
    /// <summary>Melepas movement lock milik caller tanpa memengaruhi owner lain.</summary>
    public void ReleaseMovementLock(object owner) { if (owner != null) movementLocks.Remove(owner); }
    /// <summary>Benar bila movement dikunci sistem lain selain owner yang disebut.</summary>
    public bool HasMovementLockOtherThan(object owner)
    {
        if(manualLock) return true;
        foreach(object lockOwner in movementLocks)
            if(!ReferenceEquals(lockOwner,owner)) return true;
        return false;
    }
    public void SetMovementLocked(bool locked) => manualLock = locked;
    /// <summary>
    /// Menghadap langsung ke target interaksi pada bidang horizontal. Dipakai sebelum
    /// animasi tool agar input gerak terakhir tidak membuat badan mengayun menyamping.
    /// </summary>
    public void FaceTowardsInteraction(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0025f)
            return;

        FacingDirection = direction.normalized;
        transform.rotation = Quaternion.LookRotation(FacingDirection, Vector3.up);
        planarVelocity = Vector3.zero;
    }
    /// <summary>Mengaktifkan movement modifier saat player membawa benda.</summary>
    public void SetCarrying(bool carrying) => isCarrying = carrying;
    /// <summary>Menerima input analog dari joystick mobile.</summary>
    public void SetMobileMovement(Vector2 input) => externalMobileInput = Vector2.ClampMagnitude(input, 1f);
    public void SetMobileSprint(bool pressed) => mobileSprintHeld = pressed;
    public void RequestJump() => jumpRequested = true;

    void OnDisable()
    {
        SetBrushingAnimation(false);
        SetShearingAnimation(false);
        planarVelocity = Vector3.zero;
        mobileSprintHeld = false;
        externalMobileInput = Vector2.zero;
        jumpRequested = false;
        forwardJumpActive = false;
        forwardJumpVelocity = Vector3.zero;
        standingJumpPending = false;
        SetPushingAnimation(false);
        standingJumpTimer = 0f;
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null && hasSpeedParameter)
            animator.SetFloat(speedParameter, 0f);
    }

    void OnValidate()
    {
        walkSpeed = Mathf.Max(0f, walkSpeed);
        runSpeed = Mathf.Max(walkSpeed, runSpeed);
        sprintSpeed = Mathf.Max(runSpeed, sprintSpeed);
        gravity = Mathf.Min(-0.1f, gravity);
        forwardJumpDistance = Mathf.Max(0f, forwardJumpDistance);
        standingJumpTakeoffDelay = Mathf.Max(0f, standingJumpTakeoffDelay);
    }
}
