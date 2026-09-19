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

    [Header("Player Zoom")]
    [SerializeField] bool allowPlayerZoom = true;
    [Tooltip("Pilihan Camera Size. Mouse wheel atau pinch dua jari berpindah satu level.")]
    [SerializeField] float[] zoomLevels = { 10f, 11f, 12f, 13f };
    [SerializeField, Min(10f)] float mobilePinchStepPixels = 65f;

    [Header("Screen Adaptation")]
    [Tooltip("Jika aktif, level zoom diperluas pada layar sempit agar area horizontal tetap nyaman.")]
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
    float sceneOrthographicSize;
    float pinchAccumulator;
    int zoomIndex;
    float impulseRemaining;
    float impulseDuration;
    float impulseStrength;

    public Transform Target => target;
    public int ZoomIndex => zoomIndex;
    public float CurrentZoomSize => baseOrthographicSize;
    public float Pitch => pitch;
    public float Yaw => yaw;
    public float FollowDistance => distance;

    const string ZoomPreferenceKey = "NatureParadise.CameraZoomLevel";

    void Awake()
    {
        cachedCamera = GetComponent<Camera>();
        sceneOrthographicSize = Mathf.Max(0.01f, cachedCamera.orthographicSize);
        EnsureZoomLevels();
        zoomIndex = PlayerPrefs.HasKey(ZoomPreferenceKey)
            ? Mathf.Clamp(PlayerPrefs.GetInt(ZoomPreferenceKey), 0, zoomLevels.Length - 1)
            : FindClosestZoomLevel(sceneOrthographicSize);
        baseOrthographicSize = zoomLevels[zoomIndex];
        ApplyFixedRotation();
        RefreshProjection(true);
    }

    void OnDisable()
    {
        // Mengembalikan nilai scene agar Enter Play Mode tanpa domain reload tidak meninggalkan
        // ukuran hasil adaptasi di Inspector setelah Play dihentikan.
        if (cachedCamera != null && sceneOrthographicSize > 0f)
            cachedCamera.orthographicSize = sceneOrthographicSize;
    }

    void OnEnable()
    {
        followVelocity = Vector3.zero;

        if (snapToTargetOnEnable && target != null)
            transform.position = GetDesiredPosition();
    }

    void Update()
    {
        if (!allowPlayerZoom || zoomLevels == null || zoomLevels.Length == 0)
            return;

        if (Input.touchCount >= 2)
        {
            Touch first = Input.GetTouch(0);
            Touch second = Input.GetTouch(1);
            Vector2 firstPrevious = first.position - first.deltaPosition;
            Vector2 secondPrevious = second.position - second.deltaPosition;
            pinchAccumulator += Vector2.Distance(first.position, second.position) -
                                Vector2.Distance(firstPrevious, secondPrevious);

            while (Mathf.Abs(pinchAccumulator) >= mobilePinchStepPixels)
            {
                if (pinchAccumulator > 0f)
                {
                    ZoomIn();
                    pinchAccumulator -= mobilePinchStepPixels;
                }
                else
                {
                    ZoomOut();
                    pinchAccumulator += mobilePinchStepPixels;
                }
            }
            return;
        }

        pinchAccumulator = 0f;
        float wheel = Input.mouseScrollDelta.y;
        if (wheel > 0.01f) ZoomIn();
        else if (wheel < -0.01f) ZoomOut();
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

    /// <summary>Mengganti framing per scene tanpa menulis nilai prefab kamera dunia.</summary>
    public void SetFraming(float nextPitch,float nextYaw,float nextDistance,bool snapImmediately=true)
    {
        pitch=Mathf.Clamp(nextPitch,35f,75f);
        yaw=Mathf.Repeat(nextYaw+180f,360f)-180f;
        distance=Mathf.Max(1f,nextDistance);
        ApplyFixedRotation();
        followVelocity=Vector3.zero;
        if(snapImmediately && target!=null) transform.position=GetDesiredPosition();
    }

    /// <summary>Zoom mendekat satu tingkat. Bisa dipanggil tombol UI mobile.</summary>
    public void ZoomIn() => SetZoomIndex(zoomIndex - 1);

    /// <summary>Zoom menjauh satu tingkat. Bisa dipanggil tombol UI mobile.</summary>
    public void ZoomOut() => SetZoomIndex(zoomIndex + 1);

    /// <summary>Memilih tingkat zoom dan menyimpan pilihan player.</summary>
    public void SetZoomIndex(int index)
    {
        EnsureZoomLevels();
        int next = Mathf.Clamp(index, 0, zoomLevels.Length - 1);
        if (next == zoomIndex && Mathf.Approximately(baseOrthographicSize, zoomLevels[next]))
            return;

        zoomIndex = next;
        baseOrthographicSize = zoomLevels[zoomIndex];
        PlayerPrefs.SetInt(ZoomPreferenceKey, zoomIndex);
        RefreshProjection(true);
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

        float adjustedSize = baseOrthographicSize;
        if (adaptSizeToNarrowScreens && screenHeight > 0)
        {
            float currentAspect = (float)screenWidth / screenHeight;
            if (currentAspect > 0f && currentAspect < referenceAspect)
                adjustedSize *= referenceAspect / currentAspect;
        }

        cachedCamera.orthographicSize = adaptSizeToNarrowScreens
            ? Mathf.Min(adjustedSize, Mathf.Max(baseOrthographicSize, maximumOrthographicSize))
            : adjustedSize;
    }

    void EnsureZoomLevels()
    {
        if (zoomLevels == null || zoomLevels.Length == 0)
            zoomLevels = new[] { 10f, 11f, 12f, 13f };
        for (int i = 0; i < zoomLevels.Length; i++)
            zoomLevels[i] = Mathf.Max(1f, zoomLevels[i]);
    }

    int FindClosestZoomLevel(float size)
    {
        int closest = 0;
        float difference = Mathf.Abs(zoomLevels[0] - size);
        for (int i = 1; i < zoomLevels.Length; i++)
        {
            float candidate = Mathf.Abs(zoomLevels[i] - size);
            if (candidate >= difference) continue;
            closest = i;
            difference = candidate;
        }
        return closest;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        EnsureZoomLevels();
        mobilePinchStepPixels = Mathf.Max(10f, mobilePinchStepPixels);
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
