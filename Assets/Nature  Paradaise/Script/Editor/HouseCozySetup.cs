#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>Applies the versioned cozy Lv1 house to its existing prefab without replacing gameplay anchors.</summary>
public static class HouseCozySetup
{
    const string Root = "Assets/Nature  Paradaise";
    const string Textures = Root + "/Textures/Shared/Cozy/";
    const string Models = Root + "/mesh/Buildings/HouseCozy/";
    const string PrefabPath = Root + "/Prefabs/PlayerHouse/Exterior/HouseExterior_Lv1.prefab";
    const string Output = "ArtSource/HouseCozy/Previews";
    static StringBuilder report;
    static int checks;

    [MenuItem("Nature Paradise/Art/Apply Cozy House Level 1")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply in Edit Mode.");
        Directory.CreateDirectory(Output);
        report = new StringBuilder("COZY HOUSE LEVEL 1 — Native Unity apply\n"); checks = 0;
        var baseColor = Texture("NP_CozySurfaces_01_BaseColor.png", false, false, 2048);
        var normal = Texture("NP_CozySurfaces_01_Normal.png", true, false, 1024);
        var surface = Texture("NP_CozySurfaces_01_SurfaceMask.png", false, false, 2048, false);
        var foliage = Texture("NP_CozyFoliage_01_BaseColor.png", false, true, 1024);
        var opaque = Material("NP_CozySurfaces_01", baseColor);
        opaque.SetTexture("_BumpMap", normal); opaque.SetFloat("_BumpScale", .32f); opaque.EnableKeyword("_NORMALMAP");
        opaque.SetTexture("_MetallicGlossMap", surface); opaque.EnableKeyword("_METALLICSPECGLOSSMAP");
        opaque.SetFloat("_Smoothness", 1f);
        var leaves = Material("NP_CozyFoliage_01", foliage);
        leaves.SetFloat("_AlphaClip", 1); leaves.SetFloat("_Cutoff", .42f); leaves.SetFloat("_Cull", 0);
        leaves.EnableKeyword("_ALPHATEST_ON"); leaves.SetOverrideTag("RenderType", "TransparentCutout"); leaves.renderQueue = 2450;
        var glow = Material("NP_CozyGlow_01", baseColor);
        glow.SetTexture("_EmissionMap", baseColor); glow.SetColor("_EmissionColor", new Color(.65f, .58f, .42f)); glow.EnableKeyword("_EMISSION");
        CozyWorldShadingSetup.ConfigureMaterials();
        foreach (var mat in new[] { opaque, leaves, glow }) EditorUtility.SetDirty(mat);
        Material[] shared = { opaque, leaves, glow };
        var meshes = new Mesh[3]; var rotations = new Quaternion[3]; var remaps = new Material[3][];
        long[] triangles = new long[3];
        for (int lod = 0; lod < 3; lod++)
        {
            string path = Models + $"HouseCozy_Lv1_LOD{lod}.fbx";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.globalScale = 1; importer.useFileScale = true; importer.bakeAxisConversion = true;
            importer.importAnimation = false; importer.animationType = ModelImporterAnimationType.None;
            importer.importCameras = false; importer.importLights = false; importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.isReadable = true; importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk; importer.meshCompression = ModelImporterMeshCompression.Off;
            foreach (Material mat in shared)
            {
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), mat.name), mat);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), Path.GetFileNameWithoutExtension(path) + "-" + mat.name), mat);
            }
            importer.SaveAndReimport();
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var filters = imported.GetComponentsInChildren<MeshFilter>(true);
            Check(filters.Length == 1, $"LOD{lod}: one runtime mesh, no studio/modules exported");
            var filter = filters[0]; meshes[lod] = filter.sharedMesh;
            remaps[lod] = filter.GetComponent<Renderer>().sharedMaterials;
            report.AppendLine("Imported slots: " + string.Join(", ", remaps[lod].Select(m => m != null ? m.name + " @ " + AssetDatabase.GetAssetPath(m) : "null")));
            Check(remaps[lod].All(m => shared.Contains(m)), $"LOD{lod}: all materials use shared cozy atlases");
            Quaternion baseRotation = filter.transform.rotation;
            rotations[lod] = Enumerable.Range(0, 4).Select(y => Quaternion.Euler(0, y * 90, 0) * baseRotation)
                .OrderBy(q => BoundsError(Measure(meshes[lod], q))).First();
            Bounds b = Measure(meshes[lod], rotations[lod]);
            Check(b.min.y > -.02f && b.max.y > 9 && b.min.z < -7.9f && b.max.z > 3.6f && b.max.z < 3.85f,
                $"LOD{lod}: grounded, facade +Z, existing steps at z=3.7; {b}");
            Check(meshes[lod].uv.Length == meshes[lod].vertexCount && meshes[lod].uv.All(uv => uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1), $"LOD{lod}: complete normalized atlas UVs");
            triangles[lod] = meshes[lod].triangles.LongLength / 3;
            report.AppendLine($"LOD{lod} triangles={triangles[lod]} materials={remaps[lod].Length}");
        }
        Check(triangles[0] < 13000 && triangles[2] < 2300 && triangles[0] > triangles[1] && triangles[1] > triangles[2], "LOD budgets and decreasing geometry");
        // FBX import/material upgrade callbacks can invalidate earlier texture handles.
        // Rebind the authored assets after imports, including the compatibility texture property.
        foreach (var mat in shared)
        {
            var image = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + (mat == leaves ? "NP_CozyFoliage_01_BaseColor.png" : "NP_CozySurfaces_01_BaseColor.png"));
            Check(image != null, "Texture resolves after model imports: " + mat.name);
            mat.SetTexture("_BaseMap", image); mat.SetTexture("_MainTex", image); EditorUtility.SetDirty(mat);
        }
        glow.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "NP_CozySurfaces_01_BaseColor.png"));
        opaque.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "NP_CozySurfaces_01_Normal.png"));
        opaque.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "NP_CozySurfaces_01_SurfaceMask.png"));
        Check(opaque.GetTexture("_BumpMap") != null && opaque.GetTexture("_MetallicGlossMap") != null, "Shared normal and surface mask remain connected");

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var portal = root.GetComponentInChildren<HouseScenePortal>(true);
            Vector3 entrance = portal.transform.localPosition;
            var spawn = root.GetComponentInChildren<PlayerSpawnPoint>(true);
            Vector3 exit = spawn.transform.localPosition, scale = root.transform.localScale;
            string collisions = string.Join("|", root.GetComponentsInChildren<BoxCollider>(true).Select(c => c.name + c.center + c.size + c.isTrigger));
            var group = root.GetComponentInChildren<LODGroup>(true); var lods = group.GetLODs();
            Check(lods.Length == 3, "Existing prefab has three LOD renderer slots");
            for (int lod = 0; lod < 3; lod++)
            {
                var renderer = lods[lod].renderers.Single(); var filter = renderer.GetComponent<MeshFilter>();
                filter.sharedMesh = meshes[lod]; renderer.sharedMaterials = remaps[lod];
                renderer.transform.localPosition = Vector3.zero; renderer.transform.localRotation = rotations[lod]; renderer.transform.localScale = Vector3.one;
                renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
                lods[lod].screenRelativeTransitionHeight = new[] { .18f, .07f, .012f }[lod];
            }
            group.fadeMode = LODFadeMode.CrossFade; group.animateCrossFading = true;
            group.SetLODs(lods); group.RecalculateBounds();
            var lampAnchor = root.transform.Find("CozyLanternLight");
            if (lampAnchor == null)
            {
                lampAnchor = new GameObject("CozyLanternLight").transform; lampAnchor.SetParent(root.transform, false);
            }
            lampAnchor.localPosition = new Vector3(-1.42f, 3.3f, .94f);
            var lamp = lampAnchor.GetComponent<Light>();
            if (lamp == null) lamp = lampAnchor.gameObject.AddComponent<Light>();
            lamp.type = LightType.Point; lamp.color = new Color(1, .76f, .44f); lamp.intensity = .8f;
            lamp.range = 4; lamp.shadows = LightShadows.None; lamp.enabled = false;
            Check(portal.transform.localPosition == entrance && spawn.transform.localPosition == exit && root.transform.localScale == scale, "Door, exit spawn and actual Unity house scale preserved");
            Check(collisions == string.Join("|", root.GetComponentsInChildren<BoxCollider>(true).Select(c => c.name + c.center + c.size + c.isTrigger)), "Body/porch/stair collision and portal volume preserved");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        if (Application.isBatchMode && Application.dataPath.Replace('\\', '/').Contains("/HandoffAudit-2026-10-23/CleanClone/")) ValidateAndRender();
        for (int lod = 0; lod < 3; lod++)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(Models + $"HouseCozy_Lv1_LOD{lod}.fbx");
            importer.isReadable = false; importer.SaveAndReimport();
        }
        report.AppendLine($"CHECKS {checks}; APPLY_PASS");
        File.WriteAllText(Output + "/UnityApplyAudit.txt", report.ToString());
        if (File.Exists(Output + "/UnityApplyError.txt")) File.Delete(Output + "/UnityApplyError.txt");
        Debug.Log("[House Cozy] APPLY_PASS; existing Lv1 prefab now uses cozy shared-atlas geometry.");
    }

    static Texture2D Texture(string filename, bool normal, bool alpha, int size, bool srgb = true)
    {
        string path = Textures + filename; AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.sRGBTexture = !normal && srgb; importer.alphaIsTransparency = alpha;
        importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Clamp; importer.maxTextureSize = size;
        importer.textureCompression = TextureImporterCompression.Compressed; importer.filterMode = FilterMode.Bilinear; importer.anisoLevel = 2;
        foreach (string platform in new[] { "Android", "iPhone" })
        {
            var settings = importer.GetPlatformTextureSettings(platform); settings.overridden = true;
            settings.maxTextureSize = 1024; settings.format = TextureImporterFormat.ASTC_6x6; importer.SetPlatformTextureSettings(settings);
        }
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Check(texture != null, "Imported texture is available: " + filename); return texture;
    }
    static Material Material(string name, Texture2D image)
    {
        string path = Root + "/Material/Shared/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
        mat.SetTexture("_BaseMap", image); mat.SetTexture("_MainTex", image); mat.SetColor("_BaseColor", Color.white); mat.SetFloat("_Smoothness", .2f); mat.enableInstancing = true;
        return mat;
    }
    static Bounds Measure(Mesh mesh, Quaternion rotation)
    {
        var b = new Bounds(rotation * mesh.vertices[0], Vector3.zero);
        foreach (Vector3 v in mesh.vertices) b.Encapsulate(rotation * v); return b;
    }
    static float BoundsError(Bounds b) => Vector3.Distance(b.center, new Vector3(0, 4.645f, -2.21f)) + Mathf.Abs(b.size.y - 9.29f);
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; report.AppendLine("PASS " + label); }

    static void ValidateAndRender()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try
        {
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var house = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            var group = house.GetComponentInChildren<LODGroup>(); group.ForceLOD(0);
            ValidateRoofCoverage(house, group);
            var portal = house.GetComponentInChildren<HouseScenePortal>();
            var actor = new GameObject("Player capsule test"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor, scene);
            var cc = actor.AddComponent<CharacterController>(); cc.height = 1.65f; cc.center = Vector3.up * .825f; cc.radius = .25f;
            actor.transform.position = portal.transform.position + Vector3.up * .3f; Physics.SyncTransforms();
            Check(portal.CanInteract(actor.transform), "Player on the actual scaled stair trigger can enter");
            actor.transform.position = house.transform.TransformPoint(new Vector3(0, 0, -8.7f)); Physics.SyncTransforms();
            Check(!portal.CanInteract(actor.transform), "Back of the house cannot trigger front door");
            Object.DestroyImmediate(actor);
            ShaderUtil.allowAsyncCompilation = false;
            foreach (var mat in house.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Distinct())
            {
                Check(mat != null && !ShaderUtil.ShaderHasError(mat.shader), "Compiled shared material " + mat.name);
                Check(mat.GetTexture("_BaseMap") != null, "Runtime material keeps its texture: " + mat.name);
            }
            var sunGo = new GameObject("Review sun"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sunGo, scene);
            var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(48, -35, 0); sun.intensity = 1.15f; sun.color = new Color(1, .96f, .87f); sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun; RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.72f, .80f, .87f); RenderSettings.ambientEquatorColor = new Color(.48f, .50f, .48f); RenderSettings.ambientGroundColor = new Color(.28f, .25f, .20f); RenderSettings.fog = false;
            var probe = new SphericalHarmonicsL2(); probe.AddAmbientLight(RenderSettings.ambientEquatorColor.linear * .7f);
            probe.AddDirectionalLight(Vector3.up, RenderSettings.ambientSkyColor.linear, .45f); probe.AddDirectionalLight(Vector3.down, RenderSettings.ambientGroundColor.linear, .35f); RenderSettings.ambientProbe = probe;
            var cameraGo = new GameObject("Review camera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo, scene);
            var camera = cameraGo.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 4.8f; camera.farClipPlane = 80; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.78f, .77f, .69f);
            foreach (var shot in new[] { ("Front", new Vector3(0, 8, 17)), ("FrontRight", new Vector3(-11, 10, 17)), ("RearLeft", new Vector3(12, 10, -16)) })
            {
                camera.transform.position = shot.Item2; camera.transform.LookAt(new Vector3(0, 2.6f, -.9f)); Capture(camera, Output + "/HouseCozy_Unity_" + shot.Item1 + ".png");
            }
            group.ForceLOD(2); Capture(camera, Output + "/HouseCozy_Unity_LOD2.png");
            group.ForceLOD(0);
            camera.orthographicSize = 1.55f;
            camera.transform.position = new Vector3(-3.5f, 2.7f, 5.8f);
            camera.transform.LookAt(new Vector3(0, .55f, .9f));
            Capture(camera, Output + "/HouseCozy_Unity_StoneUV.png");
            camera.orthographicSize = 2.4f;
            camera.transform.position = new Vector3(-3.6f, 8, 7);
            camera.transform.LookAt(new Vector3(0, 4.15f, -1));
            Capture(camera, Output + "/HouseCozy_Unity_RoofUV.png");
            camera.orthographicSize = 4.4f;
            camera.transform.position = new Vector3(0, 15, -.95f);
            camera.transform.LookAt(new Vector3(0, 0, -.95f), Vector3.forward);
            Capture(camera, Output + "/HouseCozy_Unity_RoofTop.png");
            camera.orthographicSize = 4.8f;
            var lantern = house.GetComponentsInChildren<Light>(true).Single(l => l.type == LightType.Point);
            Check(!lantern.enabled && lantern.shadows == LightShadows.None, "Modeled lantern has an authored night-light anchor without shadow-map cost");
            lantern.enabled = true; sun.intensity = .18f; sun.color = new Color(.57f, .75f, 1);
            RenderSettings.ambientSkyColor = new Color(.26f, .34f, .49f); RenderSettings.ambientEquatorColor = new Color(.22f, .29f, .42f);
            probe = new SphericalHarmonicsL2(); probe.AddAmbientLight(RenderSettings.ambientEquatorColor.linear * .7f); probe.AddDirectionalLight(Vector3.up, RenderSettings.ambientSkyColor.linear, .45f); RenderSettings.ambientProbe = probe;
            camera.transform.position = new Vector3(-11, 10, 17); camera.transform.LookAt(new Vector3(0, 2.6f, -.9f)); Capture(camera, Output + "/HouseCozy_Unity_Night.png");
            var definition = AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(Root + "/Resources/Buildings/Player House Building.asset");
            Check(definition.GetLevel(1).completedPrefab == prefab, "Existing house definition still resolves to the reworked prefab");
            Check(definition.GetLevel(2).completedPrefab != prefab, "Higher upgrade levels keep their existing models");
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
    static void ValidateRoofCoverage(GameObject house, LODGroup group)
    {
        var lods = group.GetLODs();
        for (int lod = 0; lod < lods.Length; lod++)
        {
            var renderer = lods[lod].renderers.Single();
            var collider = renderer.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            Physics.SyncTransforms();
            try
            {
                int misses = 0;
                foreach (float x in new[] { -4.99f, -2.7f, 0, 2.7f, 4.99f })
                    for (int row = 0; row < 40; row++)
                    {
                        float z = -7.94f + row * 8.68f / 39;
                        float expected = 5.28f + 2.77f * (1 - Mathf.Abs(z + 3.6f) / 4.4f);
                        var ray = new Ray(house.transform.TransformPoint(new Vector3(x, 12, z)), Vector3.down);
                        if (!collider.Raycast(ray, out var hit, 20) || house.transform.InverseTransformPoint(hit.point).y < expected - .025f) misses++;
                    }
                Check(misses == 0, $"LOD{lod}: roof skin covers 200 probes including both staggered end edges; misses={misses}");
            }
            finally { Object.DestroyImmediate(collider); }
        }
    }
    static void Capture(Camera camera, string path)
    {
        const int width = 1100, height = 1000; camera.aspect = (float)width / height;
        var rt = new RenderTexture(width, height, 24); rt.Create();
        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
        RenderPipeline.SubmitRenderRequest(camera, request); RenderPipeline.SubmitRenderRequest(camera, request);
        var old = RenderTexture.active; RenderTexture.active = rt;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG()); RenderTexture.active = old; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
    }
    public static void RunBatch()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/HandoffAudit-2026-10-23/CleanClone/")) throw new InvalidOperationException("Isolated fixture only.");
        try { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); Apply(); EditorApplication.Exit(0); }
        catch (Exception e) { Directory.CreateDirectory(Output); File.WriteAllText(Output + "/UnityApplyError.txt", (report?.ToString() ?? "") + e); Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
#endif
