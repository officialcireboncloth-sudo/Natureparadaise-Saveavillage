#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>Art-only replacement of the existing Lv1 interior, retaining its gameplay fixtures.</summary>
public static class HouseInteriorCozySetup
{
    const string Root = "Assets/Nature  Paradaise";
    const string Models = Root + "/mesh/Buildings/HouseInteriorCozy/";
    const string PrefabPath = Root + "/Prefabs/PlayerHouse/Interior/HouseInteriorLayout_Lv1.prefab";
    const string Output = "ArtSource/HouseInteriorCozy/Previews";
    static StringBuilder report;
    static int checks;
    static Material[] shared;
    static void Check(bool value, string label) { if (!value) throw new InvalidOperationException(label); report.AppendLine("PASS " + label); checks++; }
    static bool Fixture => Application.dataPath.Replace('\\', '/').Contains("/HandoffAudit-2026-10-23/CleanClone/");
    static Vector3 V(JToken j) => new Vector3((float)j[0], (float)j[1], (float)j[2]);
    static Bounds Expected(JToken j) { var b = new Bounds(); b.SetMinMax(V(j["min"]), V(j["max"])); return b; }
    static Bounds Measure(Mesh mesh, Matrix4x4 matrix)
    {
        var b = new Bounds(matrix.MultiplyPoint3x4(mesh.vertices[0]), Vector3.zero);
        foreach (var v in mesh.vertices) b.Encapsulate(matrix.MultiplyPoint3x4(v)); return b;
    }
    static Transform Child(Transform parent, string name)
    {
        var t = parent.Find(name); if (t == null) { t = new GameObject(name).transform; t.SetParent(parent, false); }
        return t;
    }

    [MenuItem("Nature Paradise/Art/Apply Cozy House Interior Level 1")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        Directory.CreateDirectory(Output); report = new StringBuilder("COZY HOUSE INTERIOR LV1 — NATIVE UNITY\n"); checks = 0;
        var otherLevels = Directory.GetFiles(Root + "/Prefabs/PlayerHouse/Interior", "*.prefab").Where(p => !p.EndsWith("HouseInteriorLayout_Lv1.prefab")).ToDictionary(p => p, File.ReadAllText);
        var scenePath = Root + "/Map/Scenes/Interiors/HouseInterior.unity"; string sceneBefore = File.ReadAllText(scenePath);
        shared = new[] { "NP_CozySurfaces_01", "NP_CozyFoliage_01", "NP_CozyGlow_01" }.Select(n => AssetDatabase.LoadAssetAtPath<Material>(Root + "/Material/Shared/" + n + ".mat")).ToArray();
        Check(shared.All(m => m != null), "Existing three shared atlas materials resolve");
        var spec = JObject.Parse(File.ReadAllText(Root + "/Source~/HouseInteriorCozy/HouseInterior_Cozy_Lv1_Manifest.json"));
        var filters = new Dictionary<string, MeshFilter>();
        foreach (var entry in spec["modules"])
        {
            string name = (string)entry["name"], path = Models + name + ".fbx"; AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.globalScale = 1; importer.useFileScale = true; importer.bakeAxisConversion = true; importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None; importer.importCameras = false; importer.importLights = false; importer.addCollider = false;
            importer.importNormals = ModelImporterNormals.Import; importer.importTangents = ModelImporterTangents.CalculateMikk; importer.meshCompression = ModelImporterMeshCompression.Off; importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; importer.materialLocation = ModelImporterMaterialLocation.InPrefab; importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            foreach (var mat in shared) { importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), mat.name), mat); importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name + "-" + mat.name), mat); }
            importer.SaveAndReimport();
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<MeshFilter>(true);
            Check(imported.Length == 1, name + ": one grouped runtime mesh"); filters[(string)entry["module"]] = imported.Single();
        }
        var matrices = Enumerable.Range(0, 4).SelectMany(i => new[] { 1, -1 }.Select(sign => Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 90 * i, 0), new Vector3(sign, 1, 1))));
        var axis = matrices.OrderBy(m => spec["modules"].Sum(j => {
            var f = filters[(string)j["module"]]; var actual = Measure(f.sharedMesh, m * f.transform.localToWorldMatrix); var expected = Expected(j);
            return Vector3.Distance(actual.min, expected.min) + Vector3.Distance(actual.max, expected.max);
        })).First();
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var bed = root.GetComponentInChildren<PlayerBed>(true); var book = root.GetComponentInChildren<HouseSaveBook>(true);
            var radio = root.GetComponentInChildren<WeatherForecastTV>(true); var chest = root.GetComponentInChildren<HouseStorageChest>(true);
            var calendar = root.GetComponentInChildren<CalendarUI>(true);
            Check(bed && book && radio && chest && calendar, "Existing bed, save, weather radio, storage and calendar retained");
            var anchors = new[] { bed.transform, book.transform, radio.transform, chest.transform, root.transform.Find("EntryPoint_Editable"), root.transform.Find("ExitPoint_Editable"), bed.transform.Find("SleepPose_Editable"), bed.transform.Find("WakeStandPoint_Editable") };
            var before = anchors.ToDictionary(t => t, t => (t.position, t.rotation, t.localScale));
            var camera = root.GetComponentInChildren<Camera>(true); var viewRotation = camera.transform.localRotation;
            var bedSettings = EditorJsonUtility.ToJson(bed);
            // Remove old artwork components only. All transform/component references and modal canvases survive.
            foreach (var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true).Where(t => t.GetComponentInParent<Canvas>(true) == null).ToArray()) Object.DestroyImmediate(text);
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.GetComponentInParent<Canvas>(true) == null).ToArray())
            {
                var r = filter.GetComponent<MeshRenderer>(); if (r != null) Object.DestroyImmediate(r); Object.DestroyImmediate(filter);
            }
            var oldStatic = root.transform.Find("CozyVisuals"); if (oldStatic != null) Object.DestroyImmediate(oldStatic.gameObject);
            var visuals = Child(root.transform, "CozyVisuals");
            string meshPath = Models + "HouseInterior_Cozy_Lv1_Runtime.asset";
            var saved = AssetDatabase.LoadAllAssetsAtPath(meshPath).OfType<Mesh>().ToDictionary(m => m.name); bool exists = File.Exists(meshPath);
            MeshRenderer architecture = null;
            foreach (var entry in spec["modules"])
            {
                string module = (string)entry["module"]; var input = filters[module]; var matrix = axis * input.transform.localToWorldMatrix;
                var bounds = Measure(input.sharedMesh, matrix); var expected = Expected(entry);
                Check(Vector3.Distance(bounds.min, expected.min) + Vector3.Distance(bounds.max, expected.max) < .015f, module + ": unmirrored metre coordinates match Blender");
                Check(input.sharedMesh.triangles.Length / 3 == (int)entry["triangles"], module + ": exact native triangle count");
                Check(input.sharedMesh.uv.Length == input.sharedMesh.vertexCount && input.sharedMesh.uv.All(v => v.x >= 0 && v.x <= 1 && v.y >= 0 && v.y <= 1), module + ": normalized shared atlas UVs");
                var parent = module switch { "Bed" => bed.transform, "SaveBook" => book.transform, "Radio" => radio.transform, "Chest" => chest.transform, _ => visuals };
                var frame = Child(parent, "CozyGeometry_" + module); frame.localPosition = Vector3.zero; frame.localRotation = Quaternion.identity; frame.localScale = Vector3.one;
                var local = frame.worldToLocalMatrix * root.transform.localToWorldMatrix * matrix;
                var mesh = Object.Instantiate(input.sharedMesh); mesh.name = "HouseInteriorCozy_" + module;
                mesh.vertices = mesh.vertices.Select(local.MultiplyPoint3x4).ToArray();
                mesh.normals = mesh.normals.Select(n => local.inverse.transpose.MultiplyVector(n).normalized).ToArray();
                mesh.tangents = mesh.tangents.Select(t => { var d = local.MultiplyVector(new Vector3(t.x, t.y, t.z)).normalized; return new Vector4(d.x, d.y, d.z, t.w * Mathf.Sign(local.determinant)); }).ToArray();
                if (local.determinant < 0) for (int s = 0; s < mesh.subMeshCount; s++) { var ix = mesh.GetTriangles(s); for (int k = 0; k < ix.Length; k += 3) (ix[k + 1], ix[k + 2]) = (ix[k + 2], ix[k + 1]); mesh.SetTriangles(ix, s); }
                mesh.RecalculateBounds();
                if (saved.TryGetValue(mesh.name, out var existing)) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(mesh); }
                else if (!exists) { AssetDatabase.CreateAsset(mesh, meshPath); exists = true; } else AssetDatabase.AddObjectToAsset(mesh, meshPath);
                frame.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = frame.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterials = input.GetComponent<Renderer>().sharedMaterials;
                Check(renderer.sharedMaterials.All(m => shared.Contains(m)), module + ": shared materials only");
                if (module == "Architecture") architecture = renderer;
            }
            // Keep the two chairs wholly inside the right wall, matching their revised artwork.
            foreach (var t in root.transform.Find("Collision_Editable").Cast<Transform>().Where(t => t.name.StartsWith("Dining")))
            {
                float originalX = t.name.StartsWith("DiningTable") ? 4.21f : t.name.Contains("01") ? 3.40003061f : 5.01997f;
                t.localPosition = new Vector3(originalX - .20f, t.localPosition.y, t.localPosition.z);
                if (t.name.StartsWith("DiningChair")) { var c = t.GetComponent<BoxCollider>(); c.size = new Vector3(.43f, c.size.y, c.size.z); }
            }
            // The existing calendar UI is now reached from the framed leaf artwork.
            calendar.transform.localPosition = new Vector3(4.52f, 1.82f, 4.78f); calendar.transform.localScale = Vector3.one; calendar.interactionRadius = 1.7f;
            var calendarModel = calendar.transform.Find("Calendar_ModelSlot"); calendarModel.localPosition = Vector3.zero; calendarModel.localRotation = Quaternion.identity; calendarModel.localScale = Vector3.one;
            var calendarCollider = calendarModel.GetComponent<BoxCollider>(); calendarCollider.center = Vector3.zero; calendarCollider.size = new Vector3(.40f, .46f, .06f); calendarCollider.isTrigger = true;
            var calendarReach = calendar.GetComponent<BoxCollider>(); if (calendarReach == null) calendarReach = calendar.gameObject.AddComponent<BoxCollider>();
            calendarReach.center = new Vector3(-.55f, -.80f, -.10f); calendarReach.size = new Vector3(1.4f, .30f, .30f); calendarReach.isTrigger = true;
            var lightTransform = Child(visuals, "WindowDaylight"); lightTransform.localPosition = new Vector3(2.62f, 2.13f, 4.86f); lightTransform.LookAt(root.transform.TransformPoint(new Vector3(3.18f, .025f, 1.75f)));
            var light = lightTransform.gameObject.AddComponent<Light>(); light.type = LightType.Spot; light.range = 7; light.spotAngle = 62; light.innerSpotAngle = 42; light.shadows = LightShadows.Soft; light.shadowBias = .015f; light.shadowNormalBias = .025f; light.shadowCustomResolution = 256;
            var lighting = visuals.gameObject.AddComponent<HouseInteriorCozyLighting>(); lighting.Configure(light, architecture, Array.IndexOf(architecture.sharedMaterials, shared[2])); lighting.ApplyLighting(10, WeatherType.Sunny);
            camera.transform.localPosition = new Vector3(2.6f, 8.705f, -3.375f);
            Check(anchors.All(t => t.position == before[t].position && t.rotation == before[t].rotation && t.localScale == before[t].localScale), "Bed/sleep/wake, save, radio, storage and entry/exit anchors preserved");
            Check(EditorJsonUtility.ToJson(bed) == bedSettings, "PlayerBed settings and sleep references unchanged");
            Check(camera.transform.localRotation == viewRotation && camera.fieldOfView == 40, "Existing gameplay camera lens and tilt retained; framing widened for the full room");
            Check(calendar.uiRoot != null && !calendar.uiRoot.activeSelf, "Calendar modal UI bindings retained and closed");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Check(otherLevels.All(p => File.ReadAllText(p.Key) == p.Value), "House levels 2–5 unchanged");
        Check(File.ReadAllText(scenePath) == sceneBefore, "House interior scene and its existing prefab bindings unchanged");
        if (Application.isBatchMode && Fixture) { ValidateAndRender(); ValidateSceneBinding(); }
        foreach (var entry in spec["modules"]) { var imp = (ModelImporter)AssetImporter.GetAtPath(Models + (string)entry["name"] + ".fbx"); imp.isReadable = false; imp.SaveAndReimport(); }
        File.WriteAllText(Output + "/UnityApplyAudit.txt", report + $"CHECKS {checks}; INTERIOR_COZY_APPLY_PASS\n");
        if (File.Exists(Output + "/UnityApplyError.txt")) File.Delete(Output + "/UnityApplyError.txt");
    }

    static void ValidateAndRender()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        Physics.SyncTransforms();
        foreach (var p in new[] { new Vector3(2.6f, .085f, .95f), new Vector3(1.04f, .0855f, 1.8525f), new Vector3(2.6f, .085f, 2.6f), new Vector3(2.1f, .085f, 3.4f) })
        {
            var center = root.transform.TransformPoint(p); var a = center + Vector3.up * .28f; var b = center + Vector3.up * 1.40f;
            Check(!Physics.CheckCapsule(a, b, .22f, ~0, QueryTriggerInteraction.Ignore), "Player capsule clear at entry/wake/central/bedside: " + p);
        }
        var bed = root.GetComponentInChildren<PlayerBed>();
        Check(bed.TryGetMattressSurface(root.transform.TransformPoint(new Vector3(1.04f, 0, 3.47f)), out float height) && height > .58f && height < .80f, "Readable bed mesh resolves the real mattress surface: " + height);
        var actor = new GameObject("Interaction probe"); actor.transform.position = root.transform.TransformPoint(new Vector3(2.1f, .08f, 3.47f));
        Check(bed.CanInteract(actor.transform), "Bed interaction still works from its empty side");
        var calendar = root.GetComponentInChildren<CalendarUI>(true); var radio = root.GetComponentInChildren<WeatherForecastTV>();
        actor.transform.position = root.transform.TransformPoint(new Vector3(3.3f, .08f, 4.05f));
        Check(PlayerInteractionTarget.ContainsPickup(actor.transform, calendar.transform, calendar.interactionRadius) && PlayerInteractionTarget.SurfaceDistance(actor.transform, calendar.transform) < PlayerInteractionTarget.SurfaceDistance(actor.transform, radio.transform), "Calendar can be selected from the left of the radio shelf");
        actor.transform.position = root.transform.TransformPoint(new Vector3(4.5f, .08f, 3.75f));
        Check(PlayerInteractionTarget.SurfaceDistance(actor.transform, radio.transform) < PlayerInteractionTarget.SurfaceDistance(actor.transform, calendar.transform), "Radio retains its own nearest interaction from the front"); Object.DestroyImmediate(actor);
        Check(root.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3) < 22000, "Interior mesh budget under 22k triangles");
        Check(root.GetComponentsInChildren<MeshRenderer>().Length == 6, "Six grouped mesh renderers, no duplicated old artwork");
        foreach (var mat in shared) Check(!ShaderUtil.ShaderHasError(mat.shader) && mat.GetTexture("_BaseMap") != null, "Shader and atlas resolve: " + mat.name);
        ShaderUtil.allowAsyncCompilation = false;
        RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(.72f, .80f, .87f); RenderSettings.ambientEquatorColor = new Color(.48f, .50f, .48f); RenderSettings.ambientGroundColor = new Color(.28f, .25f, .20f); RenderSettings.fog = false;
        var probe = new SphericalHarmonicsL2(); probe.AddAmbientLight(RenderSettings.ambientEquatorColor.linear * .8f); probe.AddDirectionalLight(Vector3.up, RenderSettings.ambientSkyColor.linear, .45f); RenderSettings.ambientProbe = probe;
        var sun = new GameObject("Review fill").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = .55f; sun.color = new Color(1, .96f, .87f); sun.transform.rotation = Quaternion.Euler(55, 20, 0); sun.shadows = LightShadows.Soft;
        var camera = new GameObject("Interior review camera").AddComponent<Camera>(); camera.fieldOfView = 40; camera.nearClipPlane = .05f; camera.farClipPlane = 80; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.78f, .77f, .69f);
        camera.transform.position = new Vector3(2.6f, 8.705f, -3.375f); camera.transform.rotation = Quaternion.Euler(54, 0, 0);
        Capture(camera, Output + "/HouseInteriorCozy_Unity_ReferenceView.png");
        camera.transform.position = new Vector3(-3.6f, 7.8f, -5.5f); camera.transform.LookAt(new Vector3(2.6f, .9f, 2.5f)); Capture(camera, Output + "/HouseInteriorCozy_Unity_FrontLeft.png");
        camera.orthographic = true; camera.orthographicSize = 3.5f; camera.transform.position = new Vector3(2.6f, 11, 2.3f); camera.transform.LookAt(new Vector3(2.6f, 0, 2.3f), Vector3.forward); Capture(camera, Output + "/HouseInteriorCozy_Unity_Top.png");
        var localLighting = root.GetComponentInChildren<HouseInteriorCozyLighting>(); var windowLight = localLighting.GetComponentInChildren<Light>(); localLighting.ApplyLighting(10, WeatherType.Sunny); float sunny = windowLight.intensity;
        localLighting.ApplyLighting(10, WeatherType.Rain); Check(windowLight.intensity < sunny, "Window light dims in rain");
        localLighting.ApplyLighting(22, WeatherType.Sunny); Check(windowLight.intensity < sunny && windowLight.color.b > windowLight.color.r, "Night uses subdued blue window light");
        sun.intensity = .20f; sun.color = new Color(.58f, .72f, 1); camera.orthographic = false; camera.transform.position = new Vector3(2.6f, 8.705f, -3.375f); camera.transform.rotation = Quaternion.Euler(54, 0, 0); Capture(camera, Output + "/HouseInteriorCozy_Unity_Night.png");
        Object.DestroyImmediate(root); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
    static void ValidateSceneBinding()
    {
        var scene = EditorSceneManager.OpenScene(Root + "/Map/Scenes/Interiors/HouseInterior.unity", OpenSceneMode.Single);
        var lighting = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HouseInteriorCozyLighting>(true)).Single();
        Check(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(lighting.gameObject) == PrefabPath, "Actual HouseInterior scene resolves the updated Lv1 prefab");
        var layout = PrefabUtility.GetNearestPrefabInstanceRoot(lighting.gameObject); var camera = layout.GetComponentInChildren<Camera>(true);
        Check(Vector3.Distance(camera.transform.localPosition, new Vector3(2.6f, 8.705f, -3.375f)) < .001f, "Scene instance uses the updated full-room camera framing");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
    static void Capture(Camera camera, string path)
    {
        const int width = 1200, height = 1100; camera.aspect = (float)width / height; var rt = new RenderTexture(width, height, 24); rt.Create();
        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt }; RenderPipeline.SubmitRenderRequest(camera, request); RenderPipeline.SubmitRenderRequest(camera, request);
        var old = RenderTexture.active; RenderTexture.active = rt; var image = new Texture2D(width, height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG()); RenderTexture.active = old; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
    }
    public static void RunBatch()
    {
        if (!Fixture) throw new InvalidOperationException("Isolated fixture only.");
        try { Apply(); EditorApplication.Exit(0); }
        catch (Exception e) { Directory.CreateDirectory(Output); File.WriteAllText(Output + "/UnityApplyError.txt", (report?.ToString() ?? "") + e); Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
#endif
