#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>Reworks only the chicken shop and its ornamental companion coop.</summary>
public static class PoultryCozySetup
{
    const string Root = "Assets/Nature  Paradaise";
    const string Models = Root + "/mesh/Buildings/PoultryCozy/";
    const string Modules = Root + "/Prefabs/ShopsMobile/Modules/";
    const string Group = Root + "/Prefabs/ShopsMobile/ChickenStore_Mobile_Group.prefab";
    const string Output = "ArtSource/PoultryCozy/Previews";
    static StringBuilder report;
    static int checks;
    static Material[] shared;
    sealed class Imported
    {
        public Mesh mesh;
        public Quaternion rotation;
        public Material[] materials;
    }
    static readonly Dictionary<string, Imported> imports = new();

    [MenuItem("Nature Paradise/Art/Apply Cozy Chicken Store and Companion Coop")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before applying artwork.");
        Directory.CreateDirectory(Output);
        report = new StringBuilder("COZY POULTRY SHOP + COMPANION — NATIVE UNITY\n"); checks = 0; imports.Clear();
        string playerCoop = Root + "/Resources/Buildings/Coop Building.asset";
        string protectedDefinition = File.ReadAllText(playerCoop);
        var protectedPrefabs = Directory.GetFiles(Root + "/Prefabs/Coop/Exterior", "*.prefab").ToDictionary(p => p, File.ReadAllText);
        shared = new[] { "NP_CozySurfaces_01", "NP_CozyFoliage_01", "NP_CozyGlow_01" }
            .Select(n => AssetDatabase.LoadAssetAtPath<Material>(Root + "/Material/Shared/" + n + ".mat")).ToArray();
        Check(shared.All(m => m != null), "Three existing shared cozy materials resolve");
        foreach (string texture in new[] { "NP_CozySurfaces_01_BaseColor.png", "NP_CozySurfaces_01_Normal.png", "NP_CozySurfaces_01_SurfaceMask.png" })
            AssetDatabase.ImportAsset(Root + "/Textures/Shared/Cozy/" + texture, ImportAssetOptions.ForceSynchronousImport);
        foreach (string building in new[] { "ChickenStore_Cozy", "ChickenCoop_Companion_Cozy" })
        {
            var manifest = JObject.Parse(File.ReadAllText(Root + "/Source~/PoultryCozy/" + building + "_Manifest.json"));
            Check((int)manifest["chickensInNests"] == 0, building + ": no static chicken occupants");
            foreach (var entry in manifest["lods"]) Import((string)entry["name"], entry);
        }
        // Remove the old optional instance before replacing its Geometry parent,
        // so the yard does not retain an orphaned added-child prefab override.
        var group = PrefabUtility.LoadPrefabContents(Group);
        try
        {
            var candidates = group.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.Contains("Optional_WeatherVane")).ToArray();
            var transforms = candidates.Where(t => !candidates.Any(p => p != t && t.IsChildOf(p))).ToArray();
            foreach (var t in transforms) if (t != null) Object.DestroyImmediate(t.gameObject);
            Check(transforms.Length <= 1, "Obsolete optional weather vane removed, no double ornament");
            PrefabUtility.SaveAsPrefabAsset(group, Group);
        }
        finally { PrefabUtility.UnloadPrefabContents(group); }
        UpdateModule("ChickenStore_Mobile_A", "ChickenStore_Cozy", true);
        UpdateModule("ChickenCoop_Mobile_A", "ChickenCoop_Companion_Cozy", false);
        AssetDatabase.SaveAssets();
        Check(File.ReadAllText(playerCoop) == protectedDefinition, "Player-build coop definition, costs and capacities unchanged");
        Check(protectedPrefabs.All(p => File.ReadAllText(p.Key) == p.Value), "All player-build coop exterior prefabs unchanged");
        if (Application.isBatchMode && IsFixture()) ValidateAndRender();
        foreach (string name in imports.Keys)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(Models + name + ".fbx");
            importer.isReadable = false; importer.SaveAndReimport();
        }
        report.AppendLine($"CHECKS {checks}; POULTRY_APPLY_PASS");
        File.WriteAllText(Output + "/UnityApplyAudit.txt", report.ToString());
        if (File.Exists(Output + "/UnityApplyError.txt")) File.Delete(Output + "/UnityApplyError.txt");
        Debug.Log("[Poultry Cozy] POULTRY_APPLY_PASS; shop and companion only.");
    }

    static void Import(string name, JToken entry)
    {
        string path = Models + name + ".fbx";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.globalScale = 1; importer.useFileScale = true; importer.bakeAxisConversion = true;
        importer.importAnimation = false; importer.animationType = ModelImporterAnimationType.None;
        importer.importCameras = false; importer.importLights = false; importer.addCollider = false;
        importer.importNormals = ModelImporterNormals.Import; importer.importTangents = ModelImporterTangents.CalculateMikk;
        importer.meshCompression = ModelImporterMeshCompression.Off; importer.isReadable = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
        foreach (var mat in shared)
        {
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), mat.name), mat);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name + "-" + mat.name), mat);
        }
        importer.SaveAndReimport();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var filters = model.GetComponentsInChildren<MeshFilter>(true);
        Check(filters.Length == 1, name + ": one mesh per exported LOD or door");
        var filter = filters.Single(); var mesh = filter.sharedMesh;
        var lo = entry["boundsMin"]; var hi = entry["boundsMax"];
        var expected = new Bounds();
        expected.SetMinMax(new Vector3(-(float)hi[0], (float)lo[2], -(float)hi[1]), new Vector3(-(float)lo[0], (float)hi[2], -(float)lo[1]));
        var rotation = Enumerable.Range(0, 4).Select(i => Quaternion.Euler(0, i * 90, 0) * filter.transform.rotation)
            .OrderBy(q => Vector3.Distance(Measure(mesh, q).min, expected.min) + Vector3.Distance(Measure(mesh, q).max, expected.max)).First();
        var bounds = Measure(mesh, rotation);
        Check(Vector3.Distance(bounds.min, expected.min) < .01f && Vector3.Distance(bounds.max, expected.max) < .01f, name + ": metre scale and facade +Z");
        Check(mesh.triangles.Length / 3 == (int)entry["triangles"], name + ": native mesh count " + mesh.triangles.Length / 3);
        Check(mesh.uv.Length == mesh.vertexCount && mesh.uv.All(v => v.x >= 0 && v.x <= 1 && v.y >= 0 && v.y <= 1), name + ": normalized shared-atlas UVs");
        var materials = filter.GetComponent<Renderer>().sharedMaterials;
        Check(materials.All(m => shared.Contains(m)), name + ": every submesh uses existing shared materials");
        imports[name] = new Imported { mesh = mesh, rotation = rotation, materials = materials };
    }

    static void UpdateModule(string prefabName, string modelName, bool shop)
    {
        string path = Modules + prefabName + ".prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var scale = root.transform.localScale; var pos = root.transform.localPosition; var rot = root.transform.localRotation;
            string collision = string.Join("|", root.GetComponentsInChildren<Collider>(true).Where(c => c.transform.parent == root.transform).Select(c => EditorJsonUtility.ToJson(c)));
            var old = root.transform.Find("Geometry");
            Check(old != null, prefabName + ": existing Geometry target located");
            Object.DestroyImmediate(old.gameObject);
            var geometry = Child(root.transform, "Geometry"); var group = geometry.gameObject.AddComponent<LODGroup>();
            Renderer doorRenderer = null;
            if (shop)
            {
                var hinge = Child(geometry, "DoorHinge_Editable"); hinge.localPosition = new Vector3(-.05f, .26f, 3.27f);
                doorRenderer = MeshChild(hinge, "DoorLeaf_Editable", imports[modelName + "_Door"]);
                var doorCollision = doorRenderer.gameObject.AddComponent<BoxCollider>();
                doorCollision.center = imports[modelName + "_Door"].mesh.bounds.center;
                doorCollision.size = imports[modelName + "_Door"].mesh.bounds.size;
            }
            var lods = new LOD[3];
            for (int lod = 0; lod < 3; lod++)
            {
                var renderer = MeshChild(geometry, "Cozy_LOD" + lod, imports[modelName + "_LOD" + lod]);
                lods[lod] = new LOD(new[] { .23f, .085f, .012f }[lod], doorRenderer ? new[] { renderer, doorRenderer } : new[] { renderer }) { fadeTransitionWidth = .1f };
            }
            group.fadeMode = LODFadeMode.CrossFade; group.animateCrossFading = true; group.SetLODs(lods); group.RecalculateBounds();
            Vector3[] lampPositions = shop ? new[] { new Vector3(-1.40f, 4.64f, 3.99f), new Vector3(1.40f, 4.64f, 3.99f), new Vector3(-.20f, 2.26f, 3.42f), new Vector3(1.69f, 2.26f, 3.42f), new Vector3(3.93f, 2.17f, 3.42f) } : new[] { new Vector3(0, 2.36f, 1.596f) };
            foreach (var p in lampPositions)
            {
                var anchor = Child(geometry, "CozyLanternLight"); anchor.localPosition = new Vector3(-p.x, p.y, p.z);
                var lamp = anchor.gameObject.AddComponent<Light>(); lamp.type = LightType.Point; lamp.color = new Color(1, .76f, .44f);
                lamp.range = shop ? 2.8f : 2; lamp.intensity = shop ? .6f : .45f; lamp.shadows = LightShadows.None; lamp.enabled = false;
            }
            Check(root.transform.localScale == scale && root.transform.localPosition == pos && root.transform.localRotation == rot, prefabName + ": original root scale and placement retained");
            Check(collision == string.Join("|", root.GetComponentsInChildren<Collider>(true).Where(c => c.transform.parent == root.transform).Select(c => EditorJsonUtility.ToJson(c))), prefabName + ": original body collider retained");
            Check(root.GetComponentsInChildren<MonoBehaviour>(true).All(c => c == null || !c.GetType().Name.StartsWith("Animal")), prefabName + ": decorative yard remains empty of animal actors");
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static Renderer MeshChild(Transform parent, string name, Imported model)
    {
        var child = Child(parent, name); child.localRotation = model.rotation;
        child.gameObject.AddComponent<MeshFilter>().sharedMesh = model.mesh;
        var renderer = child.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterials = model.materials;
        renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true; return renderer;
    }
    static Transform Child(Transform parent, string name) { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
    static Bounds Measure(Mesh mesh, Quaternion rotation)
    {
        var result = new Bounds(rotation * mesh.vertices[0], Vector3.zero);
        foreach (var v in mesh.vertices) result.Encapsulate(rotation * v); return result;
    }
    static void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException(label); checks++; report.AppendLine("PASS " + label); }
    static bool IsFixture() => Application.dataPath.Replace('\\', '/').Contains("/HandoffAudit-2026-10-23/CleanClone/");

    static void ValidateAndRender()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.72f, .80f, .87f); RenderSettings.ambientEquatorColor = new Color(.48f, .50f, .48f); RenderSettings.ambientGroundColor = new Color(.28f, .25f, .20f); RenderSettings.fog = false;
        var probe = new SphericalHarmonicsL2(); probe.AddAmbientLight(RenderSettings.ambientEquatorColor.linear * .7f); probe.AddDirectionalLight(Vector3.up, RenderSettings.ambientSkyColor.linear, .45f); RenderSettings.ambientProbe = probe;
        var sun = new GameObject("Review sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(48, -35, 0); sun.intensity = 1.15f; sun.color = new Color(1, .96f, .87f); RenderSettings.sun = sun;
        ShaderUtil.allowAsyncCompilation = false;
        foreach (var mat in shared) Check(!ShaderUtil.ShaderHasError(mat.shader) && mat.GetTexture("_BaseMap") != null, "Shader and texture render correctly: " + mat.name);
        var camera = new GameObject("Review camera").AddComponent<Camera>(); camera.orthographic = true; camera.farClipPlane = 80; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.78f, .77f, .69f);
        foreach (bool shop in new[] { true, false })
        {
            string prefabName = shop ? "ChickenStore_Mobile_A" : "ChickenCoop_Mobile_A";
            string label = shop ? "ChickenStore" : "CompanionCoop";
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Modules + prefabName + ".prefab"));
            var lod = root.GetComponentInChildren<LODGroup>(); lod.ForceLOD(0);
            var target = new Vector3(0, shop ? 2.25f : 1.7f, .1f);
            camera.orthographicSize = shop ? 4.0f : 2.45f;
            foreach (var shot in new[] { ("Front", new Vector3(0, 5.0f, 18)), ("FrontRight", new Vector3(-8, 6, 17)), ("RearLeft", new Vector3(9, 7, -16)) })
            {
                camera.transform.position = shot.Item2; camera.transform.LookAt(target); Capture(camera, Output + "/" + label + "_Unity_" + shot.Item1 + ".png");
            }
            lod.ForceLOD(2); Capture(camera, Output + "/" + label + "_Unity_LOD2.png"); lod.ForceLOD(0);
            if (shop)
            {
                var hinge = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "DoorHinge_Editable");
                var body = root.GetComponentsInChildren<MeshFilter>().First(f => f.name == "Cozy_LOD0"); var before = body.transform.position;
                hinge.localRotation = Quaternion.Euler(0, -28, 0); Check(body.transform.position == before, "Shop door hinge remains separately editable without moving body"); hinge.localRotation = Quaternion.identity;
            }
            foreach (var light in root.GetComponentsInChildren<Light>(true)) Check(!light.enabled && light.shadows == LightShadows.None, label + ": authored night lamp has no shadow map");
            Object.DestroyImmediate(root);
        }
        var yard = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Group));
        Check(yard.GetComponentsInChildren<LODGroup>().Length >= 2, "Existing yard inherits both reworked module prefabs");
        Check(!yard.GetComponentsInChildren<Transform>(true).Any(t => t.name.Contains("Optional_WeatherVane")), "Old nested weather vane leaves no orphaned override in the yard");
        Object.DestroyImmediate(yard);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
    static void Capture(Camera camera, string path)
    {
        const int width = 1200, height = 1000; camera.aspect = (float)width / height;
        var rt = new RenderTexture(width, height, 24); rt.Create();
        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
        RenderPipeline.SubmitRenderRequest(camera, request); RenderPipeline.SubmitRenderRequest(camera, request);
        var old = RenderTexture.active; RenderTexture.active = rt;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG());
        RenderTexture.active = old; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
    }
    public static void RunBatch()
    {
        if (!IsFixture()) throw new InvalidOperationException("Batch validation is isolated-fixture only.");
        try { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); Apply(); EditorApplication.Exit(0); }
        catch (Exception e) { Directory.CreateDirectory(Output); File.WriteAllText(Output + "/UnityApplyError.txt", (report?.ToString() ?? "") + e); Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static void RenderBatch()
    {
        if (!IsFixture()) throw new InvalidOperationException("Preview is isolated-fixture only.");
        try
        {
            Directory.CreateDirectory(Output); report = new StringBuilder("NATIVE POULTRY PREVIEW\n"); checks = 0;
            shared = new[] { "NP_CozySurfaces_01", "NP_CozyFoliage_01", "NP_CozyGlow_01" }
                .Select(n => AssetDatabase.LoadAssetAtPath<Material>(Root + "/Material/Shared/" + n + ".mat")).ToArray();
            ValidateAndRender(); report.AppendLine($"CHECKS {checks}; PREVIEW_PASS");
            File.WriteAllText(Output + "/UnityPreviewAudit.txt", report.ToString()); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
#endif
