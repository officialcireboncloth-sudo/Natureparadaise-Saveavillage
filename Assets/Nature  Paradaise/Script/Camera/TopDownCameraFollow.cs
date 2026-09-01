using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
/// <summary>
/// Kamera top-down yang mengikuti player secara halus, menyesuaikan framing untuk aspect ratio
/// layar kecil, dan menyediakan impulse ringan untuk feedback tool.
/// </summary>
public sealed class TopDownCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform target;
    [SerializeField] Vector3 focusOffset = new Vector3(0f, 1f, 0f);

    [Header("Top Down Framing")]
    [SerializeField, Range(35f, 75f)] float pitch = 55f;
    [SerializeField, Range(-180f, 180f)] float yaw;
    [SerializeField, Min(1f)] float distance = 18f;

    [Header("Screen Adaptation")]
    [Tooltip("Opsional. Jika mati, Camera > Size adalah nilai final dan tidak akan ditimpa script.")]
    [SerializeField] bool adaptSizeToNarrowScreens;
    [Tooltip("Aspect ratio acuan sebelum pandangan mulai diperluas pada layar yang lebih sempit.")]
    [SerializeField, Min(0.1f)] float referenceAspect = 16f / 9f;
    [Tooltip("Batas maksimum Size ketika adaptasi layar sempit diaktifkan.")]
    [SerializeField, Min(1f)] float maximumOrthographicSize = 12f;

    [Header("Follow")]
    [SerializeField, Min(0f)] float smoothTime = 0.15f;
    [SerializeField, Min(0.1f)] float maximumFollowSpeed = 60f;
    [SerializeField] bool snapToTargetOnEnable = true;

    Camera cachedCamera;
    Vector3 followVelocity;
    int cachedScreenWidth = -1;
    int cachedScreenHeight = -1;
    float baseOrthographicSize;
    float impulseRemaining;
    float impulseDuration;
    float impulseStrength;

    public Transform Target => target;

    void Awake()
    {
        cachedCamera = GetComponent<Camera>();
        // Camera.orthographicSize adalah satu-satunya nilai framing yang disimpan di scene.
        // Script hanya menyimpan salinannya selama Play apabila adaptasi aspect ratio dipakai.
        baseOrthographicSize = Mathf.Max(0.01f, cachedCamera.orthographicSize);
        ApplyFixedRotation();
        RefreshProjection(true);
    }

    void OnDisable()
    {
        // Mengembalikan nilai scene agar Enter Play Mode tanpa domain reload tidak meninggalkan
        // ukuran hasil adaptasi di Inspector setelah Play dihentikan.
        if (cachedCamera != null && baseOrthographicSize > 0f)
            cachedCamera.orthographicSize = baseOrthographicSize;
    }

    void OnEnable()
    {
        followVelocity = Vector3.zero;

        if (snapToTargetOnEnable && target != null)
            transform.position = GetDesiredPosition();
    }

    void LateUpdate()
    {
        RefreshProjection(false);

        if (target == null)
            return;

        Vector3 desiredPosition = GetDesiredPosition();

        if (smoothTime <= 0f)
        {
            transform.position = desiredPosition;
            followVelocity = Vector3.zero;
            ApplyImpulseOffset();
            return;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            smoothTime,
            maximumFollowSpeed,
            Time.deltaTime
        );
        ApplyImpulseOffset();
    }

    /// <summary>Mengganti target kamera dan opsional langsung memindahkan framing tanpa smoothing.</summary>
    public void SetTarget(Transform newTarget, bool snapImmediately = true)
    {
        target = newTarget;
        followVelocity = Vector3.zero;

        if (snapImmediately && target != null)
            transform.position = GetDesiredPosition();
    }

    /// <summary>Menambahkan camera impulse ringan untuk feedback tool atau impact.</summary>
    public void AddImpulse(float strength = 0.08f, float duration = 0.12f)
    {
        impulseStrength = Mathf.Max(impulseStrength, Mathf.Max(0f, strength));
        impulseDuration = Mathf.Max(0.01f, duration);
        impulseRemaining = impulseDuration;
    }

    void ApplyImpulseOffset()
    {
        if (impulseRemaining <= 0f || impulseStrength <= 0f)
            return;

        impulseRemaining = Mathf.Max(0f, impulseRemaining - Time.unscaledDeltaTime);
        float fade = impulseRemaining / impulseDuration;
        Vector2 random = Random.insideUnitCircle * impulseStrength * fade;
        transform.position += transform.right * random.x + transform.up * random.y;
    }

    Vector3 GetDesiredPosition()
    {
        Vector3 focusPoint = target.position + focusOffset;
        return focusPoint - transform.forward * distance;
    }

    void ApplyFixedRotation()
    {
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void RefreshProjection(bool force)
    {
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;

        if (!force &&
            screenWidth == cachedScreenWidth &&
            screenHeight == cachedScreenHeight)
        {
            return;
        }

        cachedScreenWidth = screenWidth;
        cachedScreenHeight = screenHeight;

        if (cachedCamera == null)
            cachedCamera = GetComponent<Camera>();

        cachedCamera.orthographic = true;

        // Saat adaptasi mati, jangan pernah menulis Size. Dengan begitu perubahan langsung
        // pada komponen Camera (misalnya Size 10) tetap menjadi sumber konfigurasi final.
        if (!adaptSizeToNarrowScreens)
            return;

        float adjustedSize = baseOrthographicSize;
        if (screenHeight > 0)
        {
            float currentAspect = (float)screenWidth / screenHeight;
            if (currentAspect > 0f && currentAspect < referenceAspect)
                adjustedSize *= referenceAspect / currentAspect;
        }

        cachedCamera.orthographicSize = Mathf.Min(
            adjustedSize,
            Mathf.Max(baseOrthographicSize, maximumOrthographicSize)
        );
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        maximumOrthographicSize = Mathf.Max(1f, maximumOrthographicSize);

        if (!isActiveAndEnabled)
            return;

        cachedCamera = GetComponent<Camera>();
        cachedCamera.orthographic = true;
        ApplyFixedRotation();

        if (!Application.isPlaying && target != null)
            transform.position = GetDesiredPosition();
    }
#endif
}
