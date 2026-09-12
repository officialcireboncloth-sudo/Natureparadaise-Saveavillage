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

    [Header("Rotation")]
    [SerializeField, Min(0f)] float rotationSpeed = 720f;
    [SerializeField] bool cameraRelativeMovement = true;
    [SerializeField] Camera movementCamera;

    [Header("Jump & Ground")]
    [SerializeField, Min(0.1f)] float jumpHeight = 1.15f;
    [SerializeField] float gravity = -25f;
    [SerializeField, Min(0f)] float groundedForce = 2f;
    [SerializeField, Min(0f)] float jumpStaminaCost = 0.1f;

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

    CharacterController characterController;
    readonly HashSet<object> movementLocks = new();
    Vector2 externalMobileInput;
    Vector3 planarVelocity;
    float verticalVelocity;
    float staminaTimer;
    bool mobileSprintHeld;
    bool jumpRequested;
    bool manualLock;
    bool isCarrying;
    bool hasSpeedParameter;
    bool hasGroundedParameter;
    bool hasSprintParameter;
    bool hasCarryParameter;
    bool hasJumpTrigger;

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
        bool groundedBeforeMove = characterController.isGrounded;
        if (groundedBeforeMove && verticalVelocity < 0f)
            verticalVelocity = -groundedForce;

        if (!IsMovementLocked && (Input.GetKeyDown(jumpKey) || jumpRequested))
            TryJump(groundedBeforeMove);
        jumpRequested = false;

        Vector3 direction = ToCameraRelativeDirection(input);
        Vector3 facingDirection = direction;
        if (facingDirection.sqrMagnitude > 0.001f)
            FacingDirection = facingDirection.normalized;
        float inputMagnitude = Mathf.Clamp01(input.magnitude);
        MovementMode mode = ResolveMovementMode(inputMagnitude, groundedBeforeMove);
        float targetSpeed = ResolveSpeed(mode, inputMagnitude);
        if (isCarrying) targetSpeed *= carrySpeedMultiplier;

        planarVelocity = direction * targetSpeed;
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

    void TryJump(bool grounded)
    {
        if (!grounded || status == null || !status.TrySpendStamina(jumpStaminaCost)) return;
        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        if (animator != null && hasJumpTrigger) animator.SetTrigger(jumpTrigger);
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
        if (hasSpeedParameter) animator.SetFloat(speedParameter, CurrentSpeed / Mathf.Max(0.01f, sprintSpeed), 0.12f, Time.deltaTime);
        if (hasGroundedParameter) animator.SetBool(groundedParameter, grounded);
        if (hasSprintParameter) animator.SetBool(sprintParameter, mode == MovementMode.Sprint);
        if (hasCarryParameter) animator.SetBool(carryParameter, isCarrying);
    }

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
        }
    }

    // Token lock mencegah satu sistem membuka movement yang masih dikunci sistem lain.
    /// <summary>Menahan movement untuk satu owner; aman dipakai beberapa sistem sekaligus.</summary>
    public void AcquireMovementLock(object owner) { if (owner != null) movementLocks.Add(owner); }
    /// <summary>Melepas movement lock milik caller tanpa memengaruhi owner lain.</summary>
    public void ReleaseMovementLock(object owner) { if (owner != null) movementLocks.Remove(owner); }
    public void SetMovementLocked(bool locked) => manualLock = locked;
    /// <summary>Mengaktifkan movement modifier saat player membawa benda.</summary>
    public void SetCarrying(bool carrying) => isCarrying = carrying;
    /// <summary>Menerima input analog dari joystick mobile.</summary>
    public void SetMobileMovement(Vector2 input) => externalMobileInput = Vector2.ClampMagnitude(input, 1f);
    public void SetMobileSprint(bool pressed) => mobileSprintHeld = pressed;
    public void RequestJump() => jumpRequested = true;

    void OnDisable()
    {
        planarVelocity = Vector3.zero;
        mobileSprintHeld = false;
        externalMobileInput = Vector2.zero;
        jumpRequested = false;
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null && hasSpeedParameter)
            animator.SetFloat(speedParameter, 0f);
    }

    void OnValidate()
    {
        walkSpeed = Mathf.Max(0f, walkSpeed);
        runSpeed = Mathf.Max(walkSpeed, runSpeed);
        sprintSpeed = Mathf.Max(runSpeed, sprintSpeed);
        gravity = Mathf.Min(-0.1f, gravity);
    }
}
