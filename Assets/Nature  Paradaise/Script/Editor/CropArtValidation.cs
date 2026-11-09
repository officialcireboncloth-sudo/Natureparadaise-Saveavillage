#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Actual Unity import, rendering and Play Mode checks for the G04 art batch.</summary>
[InitializeOnLoad]
public static class CropArtValidation
{
    const string Root = "Assets/Nature  Paradaise";
    const string Output = "ArtSource/Crops/G04/Review/Unity";
    const string Key = "NatureParadise.G04.ArtValidation";
    const string ScenePath = Root + "/Map/Scenes/Review/G04_ArtValidation.unity";
    static readonly string[] Species = { "GreenBean", "ChiliPepper", "Eggplant" };
    static readonly string[] Stages = { "Seed", "Sprout", "Young", "Flowering", "Ready", "Harvest" };
    static Report report;
    static double started;
    static bool ticking;

    [Serializable] public sealed class AssetRecord
    {
        public string path;
        public int vertices, triangles, lod1Triangles;
    }
    [Serializable] public sealed class Report
    {
        public string status = "RUNNING", unityVersion, graphicsDevice, renderPipeline, scene;
        public int importedPrefabs, importedNativeMeshes, importedFbx, runtimeStageInstances, frameCount;
        public float playSeconds, fieldOrthoSize = 5, fieldPitch = 55, fieldYaw = 0;
        public int fieldInstances, fieldTriangles;
        public List<string> checks = new(), errors = new(), warnings = new(), captures = new();
        public List<AssetRecord> assets = new();
    }
    [Serializable] sealed class MeshSpec { public int triangles = 0; public float[] min = Array.Empty<float>(), max = Array.Empty<float>(); }
    [Serializable] sealed class SavedScene { public string path; public bool loaded, active; }
    [Serializable] sealed class SceneSetupRecord { public SavedScene[] scenes; }

    static CropArtValidation()
    {
        EditorApplication.playModeStateChanged += StateChanged;
        EditorApplication.update += Tick;
        Application.logMessageReceived += Log;
        started = EditorApplication.timeSinceStartup;
    }

    static void Check(bool ok, string label)
    {
        if (!ok) throw new InvalidOperationException(label);
        report.checks.Add(label);
    }
    static void Log(string condition, string trace, LogType type)
    {
        if (string.IsNullOrEmpty(SessionState.GetString(Key, ""))) return;
        if (report == null && File.Exists(Output + "/UnityAudit.json")) Read();
        if (report == null) return;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            report.errors.Add(condition + "\n" + trace);
        else if (type == LogType.Warning && !report.warnings.Contains(condition)) report.warnings.Add(condition);
        Write();
    }
    static void Write() => File.WriteAllText(Output + "/UnityAudit.json", JsonUtility.ToJson(report, true));
    static void Read() => report = JsonUtility.FromJson<Report>(File.ReadAllText(Output + "/UnityAudit.json"));
    static string PrefabPath(string species, string stage) =>
        species == "ChiliPepper" && stage == "Sprout"
        ? Root + "/Prefabs/Crops/G03/NP_G03_Shared_Sprout.prefab"
        : Root + "/Prefabs/Crops/G04/NP_" + species + "_" + stage + ".prefab";

    // Batch entry deliberately owns a fresh Editor session; never closes an interactive user's scenes.
    public static void RunBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use a separate batch Editor session.");
        Begin();
    }

    [MenuItem("Nature Paradise/Crops/Validate G04 (Import + Play Mode)")]
    public static void RunInEditor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for import/compilation and leave Play Mode before running validation.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save your open scenes first; validation preserves and restores the saved scene setup.");
        SessionState.SetString(Key + ".Scenes", JsonUtility.ToJson(new SceneSetupRecord
        {
            scenes = EditorSceneManager.GetSceneManagerSetup().Select(s => new SavedScene { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray()
        }));
        Begin();
    }

    static void Begin()
    {
        started = EditorApplication.timeSinceStartup;
        Directory.CreateDirectory(Output);
        report = new Report { unityVersion = Application.unityVersion, graphicsDevice = SystemInfo.graphicsDeviceName,
            renderPipeline = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "Built-in" };
        SessionState.SetString(Key, "import");
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Check(SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null, "Real graphics device available for render validation");
            ValidateImports();
            CropGameplayValidation.Run();
            BuildReviewScene();
            report.scene = ScenePath;
            Write();
            SessionState.SetString(Key, "entering");
            EditorApplication.isPlaying = true;
        }
        catch (Exception e) { Fail(e); }
    }

    static void ValidateImports()
    {
        string materialPath = Root + "/Material/Shared/Plants/NP_Plants_Trellis_01.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        Check(material != null && material.shader != null && material.shader.isSupported, "Shared Trellis material and shader load and are supported");
        Check(material.shader.name == "Universal Render Pipeline/Lit", "Actual URP Lit shader resolves");
        Check(!ShaderUtil.ShaderHasError(material.shader), "URP shader has no compilation errors");
        Check(material.enableInstancing && material.IsKeywordEnabled("_NORMALMAP"), "Instancing and tangent normals enabled");
        foreach (var pair in new[] { ("BaseColor", "_BaseMap"), ("Normal", "_BumpMap"), ("SurfaceMask", "_MetallicGlossMap") })
        {
            string path = Root + "/Textures/Shared/Plants/NP_Plants_Trellis_01_" + pair.Item1 + ".png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Check(texture != null && texture.width == 2048 && texture.height == 2048, "Imported 2048 map " + pair.Item1);
            Check(material.GetTexture(pair.Item2) == texture, "Resolved material texture " + pair.Item1);
            Check(importer != null && importer.mipmapEnabled && importer.sRGBTexture == (pair.Item1 == "BaseColor"), "Colour space and mipmaps " + pair.Item1);
            Check(importer.textureType == (pair.Item1 == "Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default), "Correct importer type " + pair.Item1);
        }
        string meshFolder = Root + "/mesh/Crops/G04";
        foreach (string path in Directory.GetFiles(meshFolder, "*.asset").Select(p => p.Replace('\\', '/')).OrderBy(p => p))
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var spec = JsonUtility.FromJson<MeshSpec>(File.ReadAllText(Path.ChangeExtension(path, ".json")));
            Check(mesh != null && mesh.vertexCount > 0 && mesh.subMeshCount == 1, "Native mesh import " + mesh.name);
            Check(mesh.GetIndexCount(0) == spec.triangles * 3 && mesh.HasVertexAttribute(VertexAttribute.Normal)
                && mesh.HasVertexAttribute(VertexAttribute.Tangent) && mesh.HasVertexAttribute(VertexAttribute.TexCoord0), "Index and vertex channels " + mesh.name);
            Vector3 expected = new Vector3(spec.max[0] - spec.min[0], spec.max[1] - spec.min[1], spec.max[2] - spec.min[2]);
            Check((mesh.bounds.size - expected).magnitude < .002f && mesh.bounds.min.y >= -.015f, "Bounds and soil pivot " + mesh.name);
            report.importedNativeMeshes++;
            string fbxPath = Path.ChangeExtension(path, ".fbx");
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            Check(fbx != null && fbx.GetComponentsInChildren<MeshFilter>(true).Any(f => f.sharedMesh != null), "FBX imports in Unity " + mesh.name);
            int fbxTriangles = fbx.GetComponentsInChildren<MeshFilter>(true).Sum(f => (int)f.sharedMesh.GetIndexCount(0) / 3);
            Check(fbxTriangles == spec.triangles, "FBX/native triangle agreement " + mesh.name);
            report.importedFbx++;
        }
        foreach (string path in Directory.GetFiles(Root + "/Prefabs/Crops/G04", "*.prefab").Select(p => p.Replace('\\', '/')).OrderBy(p => p))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(prefab != null, "Prefab import " + path);
            Check(prefab.GetComponentsInChildren<Transform>(true).All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "No missing scripts " + prefab.name);
            var group = prefab.GetComponent<LODGroup>();
            var lods = group != null ? group.GetLODs() : Array.Empty<LOD>();
            Check(lods.Length == 2 && lods.All(l => l.renderers.Length == 1 && l.renderers[0] != null), "Two valid LOD renderers " + prefab.name);
            Check(lods[0].screenRelativeTransitionHeight > lods[1].screenRelativeTransitionHeight, "Ordered LOD thresholds " + prefab.name);
            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
                Check(renderer.sharedMaterials.Length == 1 && renderer.sharedMaterial == material, "Shared material on " + renderer.name);
            var a = lods[0].renderers[0].GetComponent<MeshFilter>().sharedMesh;
            var b = lods[1].renderers[0].GetComponent<MeshFilter>().sharedMesh;
            Check(b.GetIndexCount(0) / 3 <= 2800 && b.GetIndexCount(0) < a.GetIndexCount(0), "LOD triangle budget " + prefab.name);
            report.assets.Add(new AssetRecord { path = path, vertices = a.vertexCount, triangles = (int)a.GetIndexCount(0) / 3, lod1Triangles = (int)b.GetIndexCount(0) / 3 });
            report.importedPrefabs++;
        }
        Check(report.importedNativeMeshes == 40 && report.importedFbx == 40 && report.importedPrefabs == 20, "Complete 40 meshes, 40 FBXs and 20 prefabs");
        // Shared page regression: resolve all existing crop visual prefabs after the atlas update.
        foreach (string group in new[] { "G02", "G03", "G05" })
        foreach (string path in Directory.GetFiles(Root + "/Prefabs/Crops/" + group, "*.prefab"))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\', '/'));
            Check(prefab != null && prefab.GetComponentsInChildren<MeshFilter>(true).All(f => f.sharedMesh != null)
                && prefab.GetComponentsInChildren<MeshRenderer>(true).All(r => r.sharedMaterial != null && r.sharedMaterial.shader.isSupported), "Previous crop prefab resolves " + prefab.name);
        }
    }

    static GameObject Make(string name, Transform parent = null)
    {
        var obj = new GameObject(name);
        if (parent != null) obj.transform.SetParent(parent, false);
        return obj;
    }
    static GameObject Instance(string path, Transform parent, Vector3 position)
    {
        var result = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
        result.transform.localPosition = position;
        return result;
    }
    static void BuildReviewScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.6f, .65f, .7f);
        RenderSettings.fog = false;
        var light = Make("Review Sun").AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.4f; light.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(48, -32, 0);
        var camera = Make("G04 Validation Camera").AddComponent<Camera>();
        camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 2;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.78f, .82f, .77f);
        camera.nearClipPlane = .02f; camera.farClipPlane = 100; camera.allowHDR = false; camera.allowMSAA = true;
        var data = camera.gameObject.AddComponent<UniversalAdditionalCameraData>(); data.renderPostProcessing = false;
        camera.gameObject.AddComponent<AudioListener>();
        var close = Make("Mature");
        for (int i = 0; i < 3; i++) Instance(PrefabPath(Species[i], "Ready"), close.transform, new Vector3((i - 1) * 1.8f, 0, 0));
        var phases = Make("Phases");
        for (int i = 0; i < 3; i++)
        for (int s = 0; s < 6; s++)
        {
            var obj = Instance(PrefabPath(Species[i], Stages[s]), phases.transform, new Vector3((s - 2.5f) * 1.7f, 0, (i - 1) * 2.3f));
            obj.name = Species[i] + "_" + Stages[s];
        }
        phases.SetActive(false);
        var field = Make("FieldGrid");
        for (int z = 0; z < 10; z++)
        for (int x = 0; x < 10; x++)
        {
            var obj = Instance(PrefabPath(Species[(x + z) % 3], "Ready"), field.transform, new Vector3((x - 4.5f) * .8f, 0, (z - 4.5f) * .8f));
            obj.transform.localScale = Vector3.one * .48f;
        }
        field.SetActive(false);
        // Read authored camera settings without loading the gameplay scene or invoking its scripts.
        string map = File.ReadAllText(Root + "/Map/Scenes/World/Map.unity");
        var cameraScript = Directory.GetFiles(Root + "/Script", "TopDownCameraFollow.cs", SearchOption.AllDirectories).FirstOrDefault();
        if (cameraScript != null)
        {
            string guid = AssetDatabase.AssetPathToGUID(cameraScript.Replace('\\', '/'));
            var block = map.Split(new[] { "--- !u!" }, StringSplitOptions.None).FirstOrDefault(b => b.Contains("guid: " + guid));
            if (block != null)
            {
                report.fieldPitch = YamlFloat(block, "outdoorPitch", YamlFloat(block, "pitch", 55));
                report.fieldYaw = YamlFloat(block, "yaw", 0);
            }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
    }
    static float YamlFloat(string block, string key, float fallback)
    {
        var match = System.Text.RegularExpressions.Regex.Match(block, @"(?m)^  " + key + @": ([\d.\-]+)");
        return match.Success ? float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : fallback;
    }

    static void StateChanged(PlayModeStateChange state)
    {
        if (string.IsNullOrEmpty(SessionState.GetString(Key, ""))) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Read(); started = EditorApplication.timeSinceStartup;
            SessionState.SetString(Key, "playing");
        }
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetString(Key, "") == "returning")
        {
            Read(); report.status = report.errors.Count == 0 ? "PASS" : "FAIL"; Write();
            SessionState.EraseString(Key);
            Debug.Log("[G04 UNITY VALIDATION] " + report.status + " " + report.checks.Count + " checks");
            if (Application.isBatchMode) EditorApplication.Exit(report.status == "PASS" ? 0 : 1);
            else RestoreScenes();
        }
    }
    static void Tick()
    {
        string phase = SessionState.GetString(Key, "");
        if (string.IsNullOrEmpty(phase) || ticking) return;
        if (EditorApplication.timeSinceStartup - started > 240) { Fail(new TimeoutException("Unity art validation timed out in " + phase)); return; }
        if (phase != "playing" || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup - started < 2) return;
        ticking = true;
        try { ValidateRuntime(); }
        catch (Exception e) { report.errors.Add(e.ToString()); }
        finally
        {
            report.playSeconds = (float)(EditorApplication.timeSinceStartup - started);
            report.frameCount = Time.frameCount;
            report.status = "LEAVING_PLAY_MODE"; Write();
            SessionState.SetString(Key, "returning"); started = EditorApplication.timeSinceStartup;
            EditorApplication.isPlaying = false; ticking = false;
        }
    }
    static void ValidateRuntime()
    {
        Check(Application.isPlaying && Time.frameCount > 0, "Actual Play Mode reached and rendered frames advanced");
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        var phases = roots.Single(o => o.name == "Phases");
        var mature = roots.Single(o => o.name == "Mature");
        var field = roots.Single(o => o.name == "FieldGrid");
        var camera = roots.Single(o => o.name == "G04 Validation Camera").GetComponent<Camera>();
        mature.SetActive(true); phases.SetActive(false); field.SetActive(false);
        ForceLod(mature, 0);
        Aim(camera, new Vector3(0, .55f, 0), 18, 0, 2.0f);
        Capture(camera, "G04_Unity_Closeup_LOD0", 1600, 1000);
        ForceLod(mature, 1); Capture(camera, "G04_Unity_Closeup_LOD1", 1600, 1000);
        ForceLod(mature, -1);
        mature.SetActive(false); phases.SetActive(true);
        foreach (Transform obj in phases.transform)
        {
            Check(obj.GetComponentsInChildren<MeshFilter>(true).All(f => f.sharedMesh != null), "Runtime stage instantiates " + obj.name);
            var group = obj.GetComponent<LODGroup>();
            Check(group != null && group.enabled && group.size > 0, "Runtime LOD bounds " + obj.name);
            report.runtimeStageInstances++;
        }
        Check(report.runtimeStageInstances == 18, "All 18 phase appearances instantiate during Play Mode");
        ValidateFieldView();
        Aim(camera, new Vector3(0, .2f, 0), 60, 0, 4.8f);
        Capture(camera, "G04_Unity_AllPhases", 1920, 1080);
        phases.SetActive(false); field.SetActive(true);
        ForceLod(field, -1);
        Aim(camera, Vector3.zero, report.fieldPitch, report.fieldYaw, report.fieldOrthoSize);
        Capture(camera, "G04_Unity_FieldCamera", 1920, 1080);
        report.fieldInstances = field.transform.childCount;
        foreach (var group in field.GetComponentsInChildren<LODGroup>())
        {
            float height = group.size * Mathf.Max(group.transform.lossyScale.x, group.transform.lossyScale.y, group.transform.lossyScale.z)
                * QualitySettings.lodBias / (camera.orthographicSize * 2);
            var lods = group.GetLODs();
            int selected = height >= lods[0].screenRelativeTransitionHeight ? 0 : height >= lods[1].screenRelativeTransitionHeight ? 1 : -1;
            Check(selected >= 0, "Field camera retains visible LOD " + group.name);
            if (selected >= 0) report.fieldTriangles += (int)lods[selected].renderers[0].GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0) / 3;
        }
        Check(report.fieldInstances == 100, "100-plant field-camera smoke scene");
        Check(report.errors.Count == 0, "No errors or exceptions during import and runtime validation");
    }
    static void ValidateFieldView()
    {
        // Exercise the real field visual component with temporary definitions; no production profile is edited.
        var ownerObject = new GameObject("G04 inactive field fixture");
        ownerObject.SetActive(false);
        var owner = ownerObject.AddComponent<FieldArea>();
        var temporary = new List<Object> { ownerObject };
        try
        {
            foreach (string species in Species)
            {
                var crop = ScriptableObject.CreateInstance<CropDataSO>(); temporary.Add(crop);
                crop.cropId = "validation.G04." + species; crop.daysUntilFirstHarvest = 4;
                crop.visualStages = Enumerable.Range(0, 5).Select(s => new CropVisualStage
                {
                    stageName = Stages[s], startsOnDay = s, scale = Vector3.one * .6f,
                    modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(species, Stages[s]))
                }).ToArray();
                var obj = new GameObject("G04 field view fixture " + species); temporary.Add(obj);
                obj.transform.position = Vector3.right * 100;
                var view = obj.AddComponent<FieldCropView>();
                view.Initialize(owner, 0, 0, crop, 0);
                for (int s = 0; s < 5; s++)
                {
                    Check(crop.GetStageForGrowth(s) == s, "Real growth-to-stage mapping " + species + " " + Stages[s]);
                    view.ApplyStage(s, crop, s == 4 ? CropLifecycleState.HarvestReady : CropLifecycleState.Growing);
                    var visual = obj.transform.Find("Stage_" + s + "_" + Stages[s]);
                    Check(visual != null && visual.GetComponentsInChildren<MeshFilter>(true).All(f => f.sharedMesh != null), "FieldCropView applies " + species + " " + Stages[s]);
                    Check((obj.transform.localScale - Vector3.one * (.6f * owner.CellSize)).sqrMagnitude < 1e-8f,
                        "Field tile scaling " + species + " " + Stages[s]);
                }
                var tint = new MaterialPropertyBlock();
                foreach (var renderer in obj.GetComponentsInChildren<MeshRenderer>())
                {
                    renderer.GetPropertyBlock(tint);
                    Check(tint.GetColor("_BaseColor") == Color.white, "Authored atlas keeps its colours " + species);
                }
                view.ApplyStage(4, crop, CropLifecycleState.Withered);
                foreach (var renderer in obj.GetComponentsInChildren<MeshRenderer>())
                {
                    renderer.GetPropertyBlock(tint);
                    Check(tint.GetColor("_BaseColor").r < .6f, "URP wither tint applied " + species);
                }
                view.ApplyStage(4, crop, CropLifecycleState.Growing);
            }
        }
        finally { foreach (var obj in temporary) if (obj != null) Object.DestroyImmediate(obj); }
    }
    static void ForceLod(GameObject obj, int value)
    {
        foreach (var group in obj.GetComponentsInChildren<LODGroup>(true)) group.ForceLOD(value);
    }
    static void Aim(Camera camera, Vector3 target, float pitch, float yaw, float ortho)
    {
        camera.transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        camera.transform.position = target - camera.transform.forward * 20;
        camera.orthographicSize = ortho;
    }
    static void Capture(Camera camera, string name, int width, int height)
    {
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target; camera.Render();
            RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            Color32[] pixels = image.GetPixels32();
            int magenta = pixels.Count(p => p.r > 200 && p.g < 70 && p.b > 200);
            int green = pixels.Count(p => p.g > p.r * 1.1f && p.g > p.b * 1.08f);
            Check(magenta < pixels.Length / 500, "No pink/error shader pixels " + name);
            Check(green > pixels.Length / 1500, "Visible plant pixels " + name);
            string path = Output + "/" + name + ".png";
            File.WriteAllBytes(path, image.EncodeToPNG()); report.captures.Add(path);
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = previous;
            target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
        }
    }
    static void Fail(Exception e)
    {
        if (report == null) report = new Report();
        report.errors.Add(e.ToString()); report.status = "FAIL"; Write(); SessionState.EraseString(Key);
        Debug.LogError("[G04 UNITY VALIDATION] " + e);
        if (Application.isBatchMode) EditorApplication.Exit(1);
        else if (!EditorApplication.isPlayingOrWillChangePlaymode) RestoreScenes();
    }
    static void RestoreScenes()
    {
        string saved = SessionState.GetString(Key + ".Scenes", "");
        SessionState.EraseString(Key + ".Scenes");
        if (!string.IsNullOrEmpty(saved)) EditorSceneManager.RestoreSceneManagerSetup(JsonUtility.FromJson<SceneSetupRecord>(saved).scenes
            .Select(s => new SceneSetup { path = s.path, isLoaded = s.loaded, isActive = s.active }).ToArray());
    }
}
#endif
