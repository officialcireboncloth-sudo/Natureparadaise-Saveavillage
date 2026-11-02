#if UNITY_EDITOR
using System;
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

/// <summary>Shared-atlas Barn Store exterior only; no player barn or scene edits.</summary>
public static class BarnStoreCozySetup
{
    const string Root = "Assets/Nature  Paradaise";
    const string Models = Root + "/mesh/Buildings/BarnStoreCozy/";
    const string PrefabPath = Root + "/Prefabs/FarmReference/Modules/Farm_BarnStore_B.prefab";
    const string Output = "ArtSource/BarnStoreCozy/Previews";
    static StringBuilder report;
    static int checks;
    static Material[] shared;
    static Mesh[] meshes;
    static Quaternion[] rotations;
    static Material[][] bindings;

    [MenuItem("Nature Paradise/Art/Apply Cozy Barn Store")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before applying artwork.");
        Directory.CreateDirectory(Output); report = new StringBuilder("BARN STORE COZY — NATIVE UNITY\n"); checks = 0;
        string groupPath = Root + "/Prefabs/FarmReference/FarmReference_Group.prefab";
        string groupBefore = File.ReadAllText(groupPath);
        var barnFiles = Directory.GetFiles(Root + "/Prefabs/Barn/Exterior", "*.prefab").ToDictionary(p => p, File.ReadAllText);
        shared = Materials(); Check(shared.All(m => m != null), "Three existing shared cozy materials resolve");
        var manifest = JObject.Parse(File.ReadAllText(Root + "/Source~/BarnStoreCozy/BarnStore_Cozy_Manifest.json"));
        meshes = new Mesh[3]; rotations = new Quaternion[3]; bindings = new Material[3][];
        for (int lod = 0; lod < 3; lod++) Import(lod, manifest["lods"][lod]);
        Check(meshes[0].vertexCount > meshes[1].vertexCount && meshes[1].vertexCount > meshes[2].vertexCount, "LOD geometry decreases with distance");
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var scale = root.transform.localScale; var position = root.transform.localPosition; var rotation = root.transform.localRotation;
            string collision = string.Join("|", root.GetComponentsInChildren<Collider>(true).Select(c => EditorJsonUtility.ToJson(c)));
            var old = root.transform.Find("Geometry"); Check(old != null, "Existing Barn Store Geometry target located"); Object.DestroyImmediate(old.gameObject);
            var geometry = new GameObject("Geometry").transform; geometry.SetParent(root.transform, false);
            var group = geometry.gameObject.AddComponent<LODGroup>(); var lods = new LOD[3];
            for (int lod = 0; lod < 3; lod++)
            {
                var child = new GameObject("BarnStore_Cozy_LOD" + lod).transform; child.SetParent(geometry, false); child.localRotation = rotations[lod];
                child.gameObject.AddComponent<MeshFilter>().sharedMesh = meshes[lod];
                var renderer = child.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterials = bindings[lod]; renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
                lods[lod] = new LOD(new[] { .23f, .085f, .012f }[lod], new Renderer[] { renderer }) { fadeTransitionWidth = .1f };
            }
            group.fadeMode = LODFadeMode.CrossFade; group.animateCrossFading = true; group.SetLODs(lods); group.RecalculateBounds();
            foreach (var p in new[] { new Vector3(-1.22f, 3.89f, 3.60f), new Vector3(1.22f, 3.89f, 3.60f) })
            {
                var anchor = new GameObject("CozyLanternLight").transform; anchor.SetParent(geometry, false); anchor.localPosition = new Vector3(-p.x, p.y, p.z);
                var lamp = anchor.gameObject.AddComponent<Light>(); lamp.type = LightType.Point; lamp.color = new Color(1, .76f, .44f); lamp.range = 3.0f; lamp.intensity = .65f; lamp.shadows = LightShadows.None; lamp.enabled = false;
            }
            Check(root.transform.localScale == scale && root.transform.localPosition == position && root.transform.localRotation == rotation, "Root scale and placement retained");
            Check(collision == string.Join("|", root.GetComponentsInChildren<Collider>(true).Select(c => EditorJsonUtility.ToJson(c))), "Existing body, wing, rear-wall and post colliders retained");
            var collisionRoot = root.transform.Find("Colliders");
            var wall = collisionRoot.Find("RightDisplayWall");
            if (wall == null) { wall = new GameObject("RightDisplayWall").transform; wall.SetParent(collisionRoot, false); }
            wall.localPosition = new Vector3(-4.37f, 1.27f, 0);
            var wallCollider = wall.GetComponent<BoxCollider>();
            if (wallCollider == null) wallCollider = wall.gameObject.AddComponent<BoxCollider>();
            wallCollider.center = Vector3.zero; wallCollider.size = new Vector3(.15f, 2.42f, 5.98f);
            Check(!wallCollider.isTrigger, "Closed right flank has a matching solid wall collider");
            Check(root.GetComponentsInChildren<MeshRenderer>(true).Length == 3, "One renderer per LOD; no duplicated old building");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Check(File.ReadAllText(groupPath) == groupBefore, "Farm group and fence layout unchanged");
        Check(barnFiles.All(p => File.ReadAllText(p.Key) == p.Value), "All player barn exteriors unchanged");
        if (Application.isBatchMode && IsFixture()) ValidateAndRender();
        for (int lod = 0; lod < 3; lod++) { var importer = (ModelImporter)AssetImporter.GetAtPath(Models + "BarnStore_Cozy_LOD" + lod + ".fbx"); importer.isReadable = false; importer.SaveAndReimport(); }
        report.AppendLine($"CHECKS {checks}; BARN_STORE_APPLY_PASS"); File.WriteAllText(Output + "/UnityApplyAudit.txt", report.ToString());
        if (File.Exists(Output + "/UnityApplyError.txt")) File.Delete(Output + "/UnityApplyError.txt");
        Debug.Log("[Barn Store Cozy] BARN_STORE_APPLY_PASS; existing module updated.");
    }

    static Material[] Materials() => new[] { "NP_CozySurfaces_01", "NP_CozyFoliage_01", "NP_CozyGlow_01" }.Select(n => AssetDatabase.LoadAssetAtPath<Material>(Root + "/Material/Shared/" + n + ".mat")).ToArray();
    static void Import(int lod, JToken entry)
    {
        string name = "BarnStore_Cozy_LOD" + lod, path = Models + name + ".fbx"; AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(path); importer.globalScale = 1; importer.useFileScale = true; importer.bakeAxisConversion = true;
        importer.importAnimation = false; importer.animationType = ModelImporterAnimationType.None; importer.importCameras = false; importer.importLights = false; importer.addCollider = false;
        importer.importNormals = ModelImporterNormals.Import; importer.importTangents = ModelImporterTangents.CalculateMikk; importer.meshCompression = ModelImporterMeshCompression.Off; importer.isReadable = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; importer.materialLocation = ModelImporterMaterialLocation.InPrefab; importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
        foreach (var mat in shared) { importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), mat.name), mat); importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name + "-" + mat.name), mat); }
        importer.SaveAndReimport(); var imported = AssetDatabase.LoadAssetAtPath<GameObject>(path); var filters = imported.GetComponentsInChildren<MeshFilter>(true);
        Check(filters.Length == 1, name + ": one exported mesh"); var filter = filters.Single(); var mesh = filter.sharedMesh; var lo = entry["boundsMin"]; var hi = entry["boundsMax"];
        var expected = new Bounds(); expected.SetMinMax(new Vector3(-(float)hi[0], (float)lo[2], -(float)hi[1]), new Vector3(-(float)lo[0], (float)hi[2], -(float)lo[1]));
        Quaternion chosen = Enumerable.Range(0, 4).Select(i => Quaternion.Euler(0, i * 90, 0) * filter.transform.rotation).OrderBy(q => Vector3.Distance(Measure(mesh, q).min, expected.min) + Vector3.Distance(Measure(mesh, q).max, expected.max)).First();
        var bounds = Measure(mesh, chosen); Check(Vector3.Distance(bounds.min, expected.min) < .01f && Vector3.Distance(bounds.max, expected.max) < .01f, name + ": metre scale and facade +Z");
        Check(mesh.triangles.Length / 3 == (int)entry["triangles"], name + ": native triangle count " + mesh.triangles.Length / 3);
        Check(mesh.uv.Length == mesh.vertexCount && mesh.uv.All(v => v.x >= 0 && v.x <= 1 && v.y >= 0 && v.y <= 1), name + ": all UVs stay within shared atlas");
        var materials = filter.GetComponent<Renderer>().sharedMaterials; Check(materials.All(m => shared.Contains(m)), name + ": shared material remapping"); meshes[lod] = mesh; rotations[lod] = chosen; bindings[lod] = materials;
    }
    static Bounds Measure(Mesh mesh, Quaternion rotation) { var result = new Bounds(rotation * mesh.vertices[0], Vector3.zero); foreach (var v in mesh.vertices) result.Encapsulate(rotation * v); return result; }
    static void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException(label); checks++; report.AppendLine("PASS " + label); }
    static bool IsFixture() => Application.dataPath.Replace('\\', '/').Contains("/HandoffAudit-2026-10-23/CleanClone/");

    static void ValidateAndRender()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)); var group = root.GetComponentInChildren<LODGroup>(); group.ForceLOD(0); Physics.SyncTransforms();
        Check(!Physics.Linecast(root.transform.TransformPoint(new Vector3(-3.25f, 1.2f, 4.4f)), root.transform.TransformPoint(new Vector3(-3.25f, 1.2f, 1.4f))), "Right feed display remains physically open from the front");
        Check(!Physics.CheckSphere(root.transform.TransformPoint(new Vector3(0, 1, 3.8f)), .25f), "Front door approach is clear of the building collider");
        Check(Physics.Linecast(root.transform.TransformPoint(new Vector3(-5.1f, 1.2f, 1)), root.transform.TransformPoint(new Vector3(-4.1f, 1.2f, 1))), "Right outside flank is closed to player movement");
        ShaderUtil.allowAsyncCompilation = false; foreach (var mat in shared) Check(!ShaderUtil.ShaderHasError(mat.shader) && mat.GetTexture("_BaseMap") != null, "Shader and texture resolve: " + mat.name);
        RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(.72f, .80f, .87f); RenderSettings.ambientEquatorColor = new Color(.48f, .50f, .48f); RenderSettings.ambientGroundColor = new Color(.28f, .25f, .20f); RenderSettings.fog = false;
        var probe = new SphericalHarmonicsL2(); probe.AddAmbientLight(RenderSettings.ambientEquatorColor.linear * .7f); probe.AddDirectionalLight(Vector3.up, RenderSettings.ambientSkyColor.linear, .45f); RenderSettings.ambientProbe = probe;
        var sun = new GameObject("Review sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.15f; sun.color = new Color(1, .96f, .87f); sun.transform.rotation = Quaternion.Euler(48, -35, 0); RenderSettings.sun = sun;
        var camera = new GameObject("Review camera").AddComponent<Camera>(); camera.orthographic = true; camera.farClipPlane = 80; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.78f, .77f, .69f);
        foreach (var shot in new[] { ("Front", new Vector3(0, 7, 22), 5.1f), ("FrontRight", new Vector3(-11, 9, 22), 5.9f), ("RearLeft", new Vector3(12, 9, -22), 5.9f) })
        {
            camera.orthographicSize = shot.Item3; camera.transform.position = shot.Item2; camera.transform.LookAt(new Vector3(0, 2.6f, .15f)); Capture(camera, Output + "/BarnStore_Unity_" + shot.Item1 + ".png");
        }
        group.ForceLOD(2); Capture(camera, Output + "/BarnStore_Unity_LOD2.png"); group.ForceLOD(0);
        foreach (var lamp in root.GetComponentsInChildren<Light>(true)) { Check(!lamp.enabled && lamp.shadows == LightShadows.None, "Authored night lamp uses existing scheduler without shadow-map cost"); lamp.enabled = true; }
        sun.intensity = .18f; sun.color = new Color(.57f, .75f, 1); RenderSettings.ambientEquatorColor = new Color(.22f, .29f, .42f); probe = new SphericalHarmonicsL2(); probe.AddAmbientLight(RenderSettings.ambientEquatorColor.linear * .7f); RenderSettings.ambientProbe = probe;
        camera.orthographicSize = 5.1f; camera.transform.position = new Vector3(0, 7, 22); camera.transform.LookAt(new Vector3(0, 2.6f, .15f)); Capture(camera, Output + "/BarnStore_Unity_Night.png");
        Object.DestroyImmediate(root); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
    static void Capture(Camera camera, string path)
    {
        const int width = 1300, height = 1050; camera.aspect = (float)width / height; var rt = new RenderTexture(width, height, 24); rt.Create();
        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt }; RenderPipeline.SubmitRenderRequest(camera, request); RenderPipeline.SubmitRenderRequest(camera, request);
        var old = RenderTexture.active; RenderTexture.active = rt; var image = new Texture2D(width, height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG()); RenderTexture.active = old; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
    }
    public static void RunBatch()
    {
        if (!IsFixture()) throw new InvalidOperationException("Isolated fixture only.");
        try { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); Apply(); EditorApplication.Exit(0); }
        catch (Exception e) { Directory.CreateDirectory(Output); File.WriteAllText(Output + "/UnityApplyError.txt", (report?.ToString() ?? "") + e); Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
#endif
