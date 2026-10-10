using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Imports the approved single-mesh barn LODs and updates the existing exterior prefabs.</summary>
public static class BarnApprovedExteriorSetup
{
    const string Root = "Assets/Nature  Paradaise";
    const string Models = Root + "/mesh/Buildings/Barn_A";
    const string Prefabs = Root + "/Prefabs/Barn/Exterior";
    const string Definition = Root + "/Resources/Buildings/Barn Building.asset";
    const string Materials = Root + "/Pack/Toon Series/Toon Farm Pack/Models/Materials";
    const string Output = "ArtSource/Barn_A/Previews";
    static readonly float[] Scales = { .75f, .875f, 1f, 1.125f, 1.25f };
    static readonly int[] Triangles = { 3938, 1788, 368 };
    static readonly string[] ImportNames = { "Barn_A_High.fbx", "Barn_A_Medium.fbx", "Barn_A_Low.fbx" };
    static readonly float[] Thresholds = { .28f, .10f, .012f };

    [MenuItem("Nature Paradise/Barn/Apply Approved Barn A Exterior", false, 105)]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before applying barn assets.");
        Directory.CreateDirectory(Models);
        Directory.CreateDirectory(Prefabs);
        Directory.CreateDirectory(Output);
        var meshes = new Mesh[3];
        var modelRotations = new Quaternion[3];
        Material[] materials = {
            AssetDatabase.LoadAssetAtPath<Material>(Materials + "/TFP_Atlas_1A.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(Materials + "/TFP_Atlas_Lights_1A.mat")
        };
        Require(materials.All(m => m != null), "Existing atlas materials missing");
        for (int lod = 0; lod < 3; lod++)
        {
            string name = $"Barn_A_Master_LOD{lod}.fbx";
            string path = Models + "/" + ImportNames[lod];
            string oldPath = Models + "/" + name;
            if (File.Exists(oldPath) && !File.Exists(path))
                Require(string.IsNullOrEmpty(AssetDatabase.MoveAsset(oldPath, path)), "Move standalone LOD import asset");
            File.Copy("ArtSource/Barn_A/Exports/" + name, path, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.isReadable = true; // Used by the orientation and topology audit below.
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.importNormals = ModelImporterNormals.Import;
            foreach (Material material in materials)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), material.name), material);
            importer.SaveAndReimport();
            meshes[lod] = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Single();
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var filter = imported.GetComponentsInChildren<MeshFilter>(true).Single();
            Require(filter.transform.position.sqrMagnitude < .00001f && Vector3.Distance(filter.transform.lossyScale, Vector3.one) < .0001f,
                "FBX node ground pivot and metre scale");
            // Unity retains the FBX node's axis rotation even with bakeAxisConversion.
            // Keep that transform when using its shared mesh instead of discarding it.
            modelRotations[lod] = filter.transform.rotation;
            var bounds = TransformedBounds(meshes[lod], modelRotations[lod]);
            Require(meshes[lod].triangles.Length / 3 == Triangles[lod], "Triangle count LOD" + lod);
            Require(Vector3.Distance(bounds.size, new Vector3(8.9f, 6.8f, 10.9f)) < .002f,
                "Imported metre dimensions LOD" + lod + ": " + bounds.size);
            Require(Mathf.Abs(bounds.min.y) < .002f, "Ground pivot LOD" + lod);
            Require(meshes[lod].subMeshCount == 2, "Two atlas submeshes LOD" + lod);
        }
        // Placement arrows and existing sites use local +Z as the building front.
        // Normalize the imported mesh from its lantern, without changing saved site yaw.
        Vector3 glow = modelRotations[0] * GlowCentre(meshes[0]);
        Require(Mathf.Abs(glow.z) > 4.9f && Mathf.Abs(glow.x) < .1f, "Front lantern orientation");
        float yaw = glow.z < 0 ? 180f : 0f;
        var definition = AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(Definition);
        Require(definition != null, "Barn definition missing");
        var report = new StringBuilder("BARN A UNITY IMPORT: PASS\n");
        report.AppendLine("One MeshRenderer and one mesh per LOD; two existing atlas material slots.");
        report.AppendLine("LOD meshes authored in Blender; Unity switches by screen height and culls below 1.2%.");
        report.AppendLine("Model axis correction yaw: " + yaw);
        for (int level = 1; level <= 5; level++)
        {
            GameObject prefab = BuildPrefab(level, meshes, materials, modelRotations, yaw);
            Validate(prefab, level, meshes, materials);
            BuildingLevelDefinition data = definition.GetLevel(level);
            if (data != null) data.completedPrefab = prefab;
            report.AppendLine($"Lv{level}: scale {Scales[level-1]}, prefab PASS, " +
                (data != null ? "linked to existing gameplay level" : "exterior prefab ready; gameplay level not defined"));
        }
        // Reserve the complete maximum roof envelope so later upgrades do not
        // grow into adjacent buildings. Existing costs/capacities remain data-owned.
        definition.footprintWidth = 12;
        definition.footprintDepth = 14;
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        RenderPreview(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/BarnExterior_Lv3.prefab"), 0);
        RenderPreview(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/BarnExterior_Lv3.prefab"), 1);
        RenderPreview(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/BarnExterior_Lv3.prefab"), 2);
        RenderPreview(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/BarnExterior_Lv3.prefab"), -1);
        report.AppendLine("Automatic far-distance LOD culling: PASS (native Unity render contains only background).");
        report.Append(BarnApprovedExteriorValidation.Run());
        // Audit is complete; the production FBX buffers do not need CPU copies.
        for (int lod = 0; lod < 3; lod++)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(Models + "/" + ImportNames[lod]);
            importer.isReadable = false;
            importer.SaveAndReimport();
        }
        File.WriteAllText(Output + "/UnityImportAudit.txt", report.ToString());
        if (File.Exists(Output + "/UnityApplyError.txt")) File.Delete(Output + "/UnityApplyError.txt");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/BarnExterior_Lv3.prefab");
        EditorGUIUtility.PingObject(Selection.activeObject);
        Debug.Log("[Barn A] APPLY PASS: single mesh per LOD, five scale-only prefabs, existing gameplay linked.");
    }

    static GameObject BuildPrefab(int level, Mesh[] meshes, Material[] materials, Quaternion[] rotations, float yaw)
    {
        string path = Prefabs + $"/BarnExterior_Lv{level}.prefab";
        bool existing = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existing ? PrefabUtility.LoadPrefabContents(path) : new GameObject($"BarnExterior_Lv{level}");
        try
        {
            // Retain root/file GUIDs referenced by BuildingDefinition and scene instances.
            foreach (Transform child in root.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            root.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            float scale = Scales[level - 1];
            var model = Child(root.transform, "Model_Editable");
            model.localScale = Vector3.one * scale;
            model.localRotation = Quaternion.Euler(0, yaw, 0);
            var group = model.gameObject.AddComponent<LODGroup>();
            var lods = new LOD[3];
            for (int lod = 0; lod < 3; lod++)
            {
                var child = Child(model, "Barn_A_LOD" + lod);
                child.localRotation = rotations[lod];
                child.gameObject.AddComponent<MeshFilter>().sharedMesh = meshes[lod];
                var renderer = child.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                lods[lod] = new LOD(Thresholds[lod], new Renderer[] { renderer }) { fadeTransitionWidth = .15f };
            }
            group.fadeMode = LODFadeMode.CrossFade;
            group.animateCrossFading = true;
            group.SetLODs(lods);
            group.RecalculateBounds();
            var collision = Child(root.transform, "Collision_Editable").gameObject.AddComponent<BoxCollider>();
            collision.center = new Vector3(0, 2.1f, 0) * scale;
            collision.size = new Vector3(8, 4.2f, 10) * scale;
            var entrance = Child(root.transform, "Entrance_Editable");
            entrance.localPosition = new Vector3(0, 0, 5.9f) * scale;
            var authoring = root.GetComponent<BarnExteriorAuthoring>() ?? root.AddComponent<BarnExteriorAuthoring>();
            authoring.Configure(model, collision, entrance);
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (existing) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
    }

    static Transform Child(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static Vector3 GlowCentre(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] indices = mesh.GetTriangles(1).Distinct().ToArray();
        Require(indices.Length > 0, "Lantern submesh missing");
        Vector3 sum = Vector3.zero;
        foreach (int index in indices) sum += vertices[index];
        return sum / indices.Length;
    }

    static Bounds TransformedBounds(Mesh mesh, Quaternion rotation)
    {
        var bounds = new Bounds(rotation * mesh.vertices[0], Vector3.zero);
        foreach (var vertex in mesh.vertices) bounds.Encapsulate(rotation * vertex);
        return bounds;
    }

    static void Validate(GameObject prefab, int level, Mesh[] meshes, Material[] materials)
    {
        var authoring = prefab.GetComponent<BarnExteriorAuthoring>();
        Require(authoring != null, "Authoring missing");
        var group = authoring.ModelRoot.GetComponent<LODGroup>();
        Require(group != null && group.GetLODs().Length == 3, "LODGroup missing");
        Require(group.animateCrossFading, "Timed LOD transition instead of stationary dither");
        Require(prefab.GetComponentsInChildren<Collider>(true).Length == 1, "Single body collision");
        Require(prefab.GetComponentsInChildren<MeshRenderer>(true).Length == 3, "One renderer per LOD");
        Require(authoring.ModelRoot.localScale == Vector3.one * Scales[level - 1], "Uniform level scale");
        for (int lod = 0; lod < 3; lod++)
        {
            var renderer = group.GetLODs()[lod].renderers.Single();
            Require(renderer.GetComponent<MeshFilter>().sharedMesh == meshes[lod], "Shared LOD mesh");
            Require(renderer.sharedMaterials.SequenceEqual(materials), "Shared existing atlas materials");
            Require(Mathf.Abs(group.GetLODs()[lod].screenRelativeTransitionHeight - Thresholds[lod]) < .0001f, "LOD threshold");
        }
        for (int lod = 0; lod < 3; lod++)
        {
            Vector3 glow = group.GetLODs()[lod].renderers[0].transform.TransformPoint(GlowCentre(meshes[lod]));
            Require(glow.z > 4.9f * Scales[level - 1], "LOD" + lod + " front matches placement +Z");
        }
        Require(Vector3.Distance(authoring.Entrance.localPosition, new Vector3(0, 0, 5.9f) * Scales[level - 1]) < .001f,
            "Entrance follows corrected model front");
        Require(Vector3.Dot(authoring.Entrance.forward, prefab.transform.forward) > .999f, "Entrance faces outward +Z");
        Require(!authoring.BuildingCollision.bounds.Contains(authoring.Entrance.position + Vector3.up), "Entrance outside blocker");
    }

    static void RenderPreview(GameObject prefab, int lod)
    {
        var preview = new PreviewRenderUtility();
        GameObject instance = Object.Instantiate(prefab);
        preview.AddSingleGO(instance);
        var previewGroup = instance.GetComponentInChildren<LODGroup>();
        previewGroup.ForceLOD(lod);
        // Audit the steady state at a fixed camera distance. Production uses
        // a timed transition so a stationary camera cannot freeze mid-dither.
        previewGroup.animateCrossFading = false;
        var camera = preview.camera;
        camera.orthographic = true;
        camera.orthographicSize = lod < 0 ? 1000 : 8;
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 100;
        camera.transform.position = new Vector3(-15, 15, 20);
        camera.transform.LookAt(new Vector3(0, 2.8f, 0));
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.24f, .34f, .43f);
        preview.ambientColor = new Color(.65f, .69f, .72f);
        preview.lights[0].intensity = 1.3f;
        preview.lights[0].transform.rotation = Quaternion.Euler(45, -35, 0);
        preview.lights[1].intensity = .8f;
        preview.lights[1].transform.rotation = Quaternion.Euler(30, 140, 0);
        try
        {
            preview.BeginStaticPreview(new Rect(0, 0, 1280, 960));
            preview.Render(true);
            Texture2D image = preview.EndStaticPreview();
            if (lod < 0)
            {
                Color[] pixels = image.GetPixels();
                Color first = pixels[0];
                Require(pixels.All(c => Mathf.Abs(c.r-first.r) < .005f && Mathf.Abs(c.g-first.g) < .005f && Mathf.Abs(c.b-first.b) < .005f),
                    "Native automatic LOD culling beyond last threshold");
            }
            File.WriteAllBytes(Output + (lod < 0 ? "/Barn_A_Unity_Culled.png" : $"/Barn_A_Unity_LOD{lod}.png"), image.EncodeToPNG());
            Object.DestroyImmediate(image);
        }
        finally { preview.Cleanup(); }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Barn A audit failed: " + message);
    }
}
