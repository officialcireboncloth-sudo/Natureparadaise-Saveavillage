using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>Temporary, budgeted view of the actual farm. Never moves the gameplay camera.</summary>
public sealed class SoilScannerDroneView : MonoBehaviour
{
    readonly List<FieldArea> fields = new();
    readonly List<Canvas> worldCanvases = new();
    readonly List<Renderer> worldLabels = new();
    readonly List<Behaviour> hiddenCanvases = new();
    readonly List<Renderer> hiddenLabels = new();
    RawImage image;
    Canvas ownerCanvas;
    Camera drone;
    RenderTexture target;
    float nextRender, nextLabelDiscovery;
    bool disposed;
    Rect frameArea = new Rect(.03f,.14f,.70f,.74f);
    public Camera ViewCamera => drone;
    public RenderTexture Texture => target;
    public int RenderCount { get; private set; }
    public event System.Action FrameChanged;
    public static int MaximumTextureSize => Application.isMobilePlatform ? 1024 : 1536;
    public const float ViewPitch = 62f;

    public void SetFrameArea(Rect area)
    {
        frameArea = area;
        nextRender = 0;
    }

    public void Initialize(RawImage view)
    {
        image = view;
        ownerCanvas = view.GetComponentInParent<Canvas>();
        if (drone != null) return;
        var go = new GameObject("Soil Scanner Drone Camera") { hideFlags = HideFlags.DontSave };
        drone = go.AddComponent<Camera>();
        var main = Camera.main;
        drone.enabled = false;
        drone.orthographic = true;
        drone.clearFlags = CameraClearFlags.SolidColor;
        drone.backgroundColor = new Color(.18f,.24f,.20f);
        drone.cullingMask = (main != null ? main.cullingMask : ~0) & ~(1 << 5);
        drone.allowHDR = false;
        drone.allowMSAA = false;
        drone.useOcclusionCulling = false;
        var extra = drone.GetUniversalAdditionalCameraData();
        extra.renderPostProcessing = false;
        extra.renderShadows = true;
        extra.requiresColorOption = CameraOverrideOption.Off;
        extra.requiresDepthOption = CameraOverrideOption.Off;
    }

    public void SetFields(IReadOnlyList<FieldArea> activeFields)
    {
        foreach (var field in fields) if (field != null) field.SetScannerVisibility(this,false);
        fields.Clear();
        foreach (var field in activeFields)
            if (field != null) { fields.Add(field); field.SetScannerVisibility(this,true); }
        nextRender = 0;
    }

    void LateUpdate()
    {
        if (disposed || Time.unscaledTime < nextRender ||
            (SoilScannerUI.Instance != null && SoilScannerUI.Instance.IsBagOpen)) return;
        RenderNow();
    }

    public void RenderNow()
    {
        if (disposed || image == null || drone == null || fields.Count == 0) return;
        Canvas.ForceUpdateCanvases();
        EnsureTexture();
        FrameCamera();
        if (Time.unscaledTime >= nextLabelDiscovery)
        {
            nextLabelDiscovery = Time.unscaledTime + 1;
            worldCanvases.Clear(); worldLabels.Clear();
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) worldCanvases.Add(canvas);
            foreach (var label in FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None))
                if (label.TryGetComponent<Renderer>(out var renderer)) worldLabels.Add(renderer);
        }
        hiddenCanvases.Clear(); hiddenLabels.Clear();
        try
        {
            // World-space shop/debug text is not part of the soil scan. Restore it in the same render call.
            foreach (var canvas in worldCanvases)
                if (canvas != null && canvas.enabled) { hiddenCanvases.Add(canvas); canvas.enabled = false; }
            if (ownerCanvas != null && ownerCanvas.enabled) { hiddenCanvases.Add(ownerCanvas); ownerCanvas.enabled = false; }
            foreach (var label in worldLabels)
                if (label != null && label.enabled) { hiddenLabels.Add(label); label.enabled = false; }
            if (GraphicsSettings.currentRenderPipeline != null)
                RenderPipeline.SubmitRenderRequest(drone,new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            else
            {
                drone.targetTexture = target;
                drone.Render();
                drone.targetTexture = null;
            }
            RenderCount++;
        }
        finally
        {
            foreach (var canvas in hiddenCanvases) if (canvas != null) canvas.enabled = true;
            foreach (var label in hiddenLabels) if (label != null) label.enabled = true;
            // URP restores the camera's screen target after its request; retain the texture aspect for picking.
            drone.aspect = target.width / (float)target.height;
        }
        FrameChanged?.Invoke();
        Shader.SetGlobalTexture("_ScannerFarmView",target);
        Shader.SetGlobalVector("_ScannerFarmView_TexelSize",new Vector4(1f/target.width,1f/target.height,target.width,target.height));
        Shader.SetGlobalFloat("_ScannerGlassReady",1);
        nextRender = Time.unscaledTime + 1f / (Application.isMobilePlatform ? 12f : 20f);
    }

    void EnsureTexture()
    {
        var size = image.rectTransform.rect.size * ownerCanvas.scaleFactor;
        float limit = MaximumTextureSize;
        float scale = Mathf.Min(1,limit / Mathf.Max(1,Mathf.Max(size.x,size.y)));
        int width = Mathf.Max(128,Mathf.RoundToInt(size.x * scale));
        int height = Mathf.Max(128,Mathf.RoundToInt(size.y * scale));
        if (target != null && target.width == width && target.height == height) { image.texture = target; return; }
        ReleaseTexture();
        target = new RenderTexture(width,height,24,RenderTextureFormat.ARGB32)
        { name = "Soil Scanner Live Farm", antiAliasing = 1, useMipMap = false, filterMode = FilterMode.Bilinear };
        target.Create(); image.texture = target;
    }

    void FrameCamera()
    {
        FieldArea reference = null;
        foreach (var field in fields) if (field != null) { reference = field; break; }
        if (reference == null) return;
        var basis = Quaternion.Euler(ViewPitch,reference.transform.eulerAngles.y,0);
        var inverse = Quaternion.Inverse(basis);
        Vector3 min = Vector3.one * float.PositiveInfinity, max = Vector3.one * float.NegativeInfinity;
        foreach (var field in fields)
        {
            if (field == null) continue;
            for (int i=0;i<4;i++)
            {
                var corner = field.transform.TransformPoint(new Vector3((i%2==0?-.5f:.5f)*field.Columns*field.CellSize,
                    0,(i<2?-.5f:.5f)*field.Rows*field.CellSize));
                corner = FieldGroundSurface.GroundPosition(corner);
                var local = inverse * corner;
                min = Vector3.Min(min,local); max = Vector3.Max(max,local);
            }
        }
        float aspect = target.width / (float)target.height;
        var span = max-min;
        float halfHeight = Mathf.Max(span.y/(2*Mathf.Max(.1f,frameArea.height)),
            span.x/(2*aspect*Mathf.Max(.1f,frameArea.width)));
        drone.aspect = aspect;
        drone.orthographicSize = Mathf.Max(2,halfHeight*1.08f + .35f);
        var center = (min+max)*.5f;
        center.x -= (frameArea.center.x-.5f)*drone.orthographicSize*2*aspect;
        center.y -= (frameArea.center.y-.5f)*drone.orthographicSize*2;
        center.z = min.z-32;
        drone.transform.SetPositionAndRotation(basis*center,basis);
        drone.nearClipPlane = .1f;
        drone.farClipPlane = 64 + Mathf.Max(0,span.z);
    }

    public Vector3 Project(Vector3 position) => drone != null ? drone.WorldToViewportPoint(position) : Vector3.zero;
    public float FieldAngle(FieldArea field)
    {
        var right = Project(field.transform.position + field.transform.right) - Project(field.transform.position);
        return Mathf.Atan2(right.y*target.height,right.x*target.width)*Mathf.Rad2Deg;
    }

    void ReleaseTexture()
    {
        if (image != null) image.texture = null;
        if (target == null) return;
        target.Release(); Destroy(target); target = null;
    }
    public void Dispose()
    {
        if (disposed) return; disposed = true;
        Shader.SetGlobalFloat("_ScannerGlassReady",0);
        Shader.SetGlobalTexture("_ScannerFarmView",Texture2D.blackTexture);
        foreach (var field in fields) if (field != null) field.SetScannerVisibility(this,false);
        fields.Clear(); ReleaseTexture();
        if (drone != null) { drone.enabled = false; Destroy(drone.gameObject); drone = null; }
    }
    void OnDisable() => Dispose();
    void OnDestroy() => Dispose();
}
