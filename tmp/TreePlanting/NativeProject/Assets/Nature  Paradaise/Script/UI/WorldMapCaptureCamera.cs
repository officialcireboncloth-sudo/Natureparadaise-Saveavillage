using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Membuat satu snapshot ortografik terrain/world untuk background map. Camera aktif
/// satu frame saja; RenderTexture dipakai ulang hingga scene atau ukuran world berubah.
/// </summary>
public sealed class WorldMapCaptureCamera : MonoBehaviour
{
    static WorldMapCaptureCamera instance;

    Camera captureCamera;
    RenderTexture texture;
    Bounds capturedBounds;
    int capturedSceneCount = -1;
    string capturedMapId;
    float capturedRotation;
    bool dirty = true;
    bool captureQueued;

    public static Texture Request(WorldMapService service, bool force = false)
    {
        if (service == null || service.Definition == null || !service.UseTopDownCapture)
            return null;
        EnsureExists();
        instance.QueueCapture(service, force);
        return instance.texture;
    }

    public static void Invalidate()
    {
        if (instance != null) instance.dirty = true;
    }

    static void EnsureExists()
    {
        if (instance != null) return;
        GameObject host = new("WorldMapTopDownCamera_Runtime");
        instance = host.AddComponent<WorldMapCaptureCamera>();
        DontDestroyOnLoad(host);
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        captureCamera = gameObject.AddComponent<Camera>();
        captureCamera.enabled = false;
        captureCamera.orthographic = true;
        captureCamera.usePhysicalProperties = false;
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.allowHDR = false;
        captureCamera.allowMSAA = false;
        captureCamera.useOcclusionCulling = false;
        captureCamera.depth = -100f;
        int uiLayer = LayerMask.NameToLayer("UI");
        captureCamera.cullingMask = uiLayer >= 0 ? ~(1 << uiLayer) : ~0;
        SceneManager.sceneLoaded += MarkDirty;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= MarkDirty;
        ReleaseTexture();
        if (instance == this) instance = null;
    }

    void MarkDirty(Scene scene, LoadSceneMode mode) => dirty = true;

    void QueueCapture(WorldMapService service, bool force)
    {
        Bounds bounds = service.WorldBounds;
        bool boundsChanged = capturedBounds.size != bounds.size || capturedBounds.center != bounds.center;
        bool mapChanged = !string.Equals(capturedMapId, service.CurrentMapId, System.StringComparison.OrdinalIgnoreCase);
        bool rotationChanged = !Mathf.Approximately(capturedRotation, service.MapRotationDegrees);
        if (!force && !dirty && !boundsChanged && !mapChanged && !rotationChanged &&
            capturedSceneCount == SceneManager.sceneCount && texture != null)
            return;
        Configure(service, bounds);
        if (!captureQueued) StartCoroutine(CaptureOneFrame());
    }

    void Configure(WorldMapService service, Bounds bounds)
    {
        MapDefinitionSO definition = service.Definition;
        float rotationRadians = service.MapRotationDegrees * Mathf.Deg2Rad;
        float absCos = Mathf.Abs(Mathf.Cos(rotationRadians));
        float absSin = Mathf.Abs(Mathf.Sin(rotationRadians));
        float width = Mathf.Max(1f, bounds.size.x * absCos + bounds.size.z * absSin);
        float depth = Mathf.Max(1f, bounds.size.x * absSin + bounds.size.z * absCos);
        int baseResolution = Application.isMobilePlatform
            ? definition.mobileCaptureResolution
            : definition.desktopCaptureResolution;
        float largest = Mathf.Max(width, depth);
        int targetWidth = Mathf.Max(256, Mathf.RoundToInt(baseResolution * width / largest));
        int targetHeight = Mathf.Max(256, Mathf.RoundToInt(baseResolution * depth / largest));

        if (texture == null || texture.width != targetWidth || texture.height != targetHeight)
        {
            ReleaseTexture();
            texture = new RenderTexture(targetWidth, targetHeight, 16, RenderTextureFormat.ARGB32)
            {
                name = "WorldMap_TopDownSnapshot",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            texture.Create();
        }

        float cameraHeight = bounds.max.y + Mathf.Max(100f, bounds.size.y + 20f);
        // Strict vertical orthographic capture; yaw follows the editable map layout angle.
        transform.SetPositionAndRotation(new Vector3(bounds.center.x, cameraHeight, bounds.center.z),
            Quaternion.AngleAxis(service.MapRotationDegrees, Vector3.up) *
            Quaternion.LookRotation(Vector3.down, Vector3.forward));
        transform.localScale = Vector3.one;
        captureCamera.orthographic = true;
        captureCamera.orthographicSize = depth * 0.5f;
        captureCamera.aspect = width / depth;
        captureCamera.nearClipPlane = 0.1f;
        captureCamera.farClipPlane = Mathf.Max(500f, cameraHeight - bounds.min.y + 100f);
        captureCamera.backgroundColor = service.IsWorldMap ? definition.captureClearColor : service.CurrentBackgroundColor;
        captureCamera.targetTexture = texture;
        capturedBounds = bounds;
        capturedMapId = service.CurrentMapId;
        capturedRotation = service.MapRotationDegrees;
    }

    IEnumerator CaptureOneFrame()
    {
        captureQueued = true;
        // Camera tambahan dirender oleh URP pada frame berikutnya, lalu langsung dimatikan.
        captureCamera.enabled = true;
        yield return new WaitForEndOfFrame();
        captureCamera.enabled = false;
        captureQueued = false;
        dirty = false;
        capturedSceneCount = SceneManager.sceneCount;
    }

    void ReleaseTexture()
    {
        if (captureCamera != null) captureCamera.targetTexture = null;
        if (texture == null) return;
        texture.Release();
        Destroy(texture);
        texture = null;
    }
}
