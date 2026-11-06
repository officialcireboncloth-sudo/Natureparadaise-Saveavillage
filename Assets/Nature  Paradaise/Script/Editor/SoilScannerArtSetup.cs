#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Replace the scanner presentation while retaining its prefab identity and interaction.</summary>
public static class SoilScannerArtSetup
{
    const string Root = "Assets/Nature  Paradaise";
    const string Models = Root + "/mesh/Props/SoilScanner/";
    const string Textures = Root + "/Textures/Shared/Cozy/";
    const string Output = "ArtSource/SoilScanner/Review";
    public static bool IsAvailable => File.Exists(Models + "SoilScanner_Cozy_LOD0.json");
    static Vector3 V(JToken v) => new((float)v[0], (float)v[1], (float)v[2]);
    static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); Debug.Log("SCANNER CHECK: " + message); }

    static Material Extension()
    {
        foreach (string suffix in new[] { "BaseColor", "Normal", "SurfaceMask" })
        {
            string path = Textures + "NP_CozySurfaces_04_" + suffix + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = suffix == "Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = suffix == "BaseColor";
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear; importer.maxTextureSize = 512; importer.isReadable = false;
            foreach (string platform in new[] { "Android", "iPhone" })
            {
                var settings = importer.GetPlatformTextureSettings(platform);
                settings.overridden = true; settings.maxTextureSize = 512; settings.format = TextureImporterFormat.ASTC_6x6;
                importer.SetPlatformTextureSettings(settings);
            }
            importer.SaveAndReimport();
        }
        string materialPath = Root + "/Material/Shared/NP_CozySurfaces_04.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        var shared = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Material/Shared/NP_CozySurfaces_01.mat");
        if (material == null) { material = new Material(shared) { name = "NP_CozySurfaces_04" }; AssetDatabase.CreateAsset(material, materialPath); }
        else material.CopyPropertiesFromMaterial(shared);
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "NP_CozySurfaces_04_BaseColor.png"));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "NP_CozySurfaces_04_Normal.png"));
        material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "NP_CozySurfaces_04_SurfaceMask.png"));
        material.SetColor("_BaseColor", Color.white); material.SetFloat("_PresentationFade", 1); material.enableInstancing = true;
        EditorUtility.SetDirty(material); return material;
    }

    [MenuItem("Nature Paradise/Soil/Apply Blender Scanner Art")]
    public static GameObject Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before replacing scanner art.");
        Check(IsAvailable, "Blender mesh export exists");
        var extension = Extension();
        var materials = new[] { AssetDatabase.LoadAssetAtPath<Material>(Root + "/Material/Shared/NP_CozySurfaces_01.mat"), extension };
        var meshes = new Mesh[3];
        for (int lod = 0; lod < 3; lod++)
        {
            string name = "SoilScanner_Cozy_LOD" + lod;
            var spec = JObject.Parse(File.ReadAllText(Models + name + ".json"));
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.vertices = spec["vertices"].Select(V).ToArray(); mesh.normals = spec["normals"].Select(V).ToArray();
            mesh.uv = spec["uv"].Select(v => new Vector2((float)v[0], (float)v[1])).ToArray();
            mesh.subMeshCount = 2;
            for (int sub = 0; sub < 2; sub++) mesh.SetTriangles(spec["submeshes"][sub].Select(i => (int)i).ToArray(), sub);
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            Check(mesh.triangles.Length / 3 == (int)spec["triangles"], name + " triangulation matches Blender");
            Check(mesh.uv.All(u => float.IsFinite(u.x) && float.IsFinite(u.y) && u.x >= 0 && u.x <= 1 && u.y >= 0 && u.y <= 1), name + " valid atlas UVs");
            Check(Vector3.Distance(mesh.bounds.min, V(spec["min"])) < .001f && Vector3.Distance(mesh.bounds.max, V(spec["max"])) < .001f, name + " exact metre bounds");
            string path = Models + name + ".asset"; var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); meshes[lod] = mesh; }
            else { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(existing); meshes[lod] = existing; }
        }
        string prefabPath = SoilScannerSetup.PrefabPath;
        string guidBefore = AssetDatabase.AssetPathToGUID(prefabPath);
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var machine = root.GetComponent<SoilScannerMachine>();
            Check(machine != null && machine.InteractionAnchor != null, "Existing gameplay component and anchor retained");
            var anchor = machine.InteractionAnchor; var anchorPosition = anchor.localPosition;
            foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                if (child.name == "Visual_Dummy_ReplaceLater" || child.name == "Visual_SoilScanner_Cozy") Object.DestroyImmediate(child.gameObject);
            var visual = new GameObject("Visual_SoilScanner_Cozy"); visual.transform.SetParent(root.transform, false);
            var group = visual.AddComponent<LODGroup>(); var levels = new LOD[3];
            for (int lod = 0; lod < 3; lod++)
            {
                var part = new GameObject("SoilScanner_Cozy_LOD" + lod, typeof(MeshFilter), typeof(MeshRenderer));
                part.transform.SetParent(visual.transform, false); part.GetComponent<MeshFilter>().sharedMesh = meshes[lod];
                var renderer = part.GetComponent<MeshRenderer>(); renderer.sharedMaterials = materials;
                levels[lod] = new LOD(new[] { .30f, .13f, .025f }[lod], new Renderer[] { renderer });
            }
            group.SetLODs(levels); group.RecalculateBounds(); group.fadeMode = LODFadeMode.None;
            root.name = "SoilScanner";
            Check(root.GetComponentsInChildren<Collider>().Length == 1, "Original single collision body retained");
            Check(anchor == machine.InteractionAnchor && anchor.localPosition == anchorPosition, "Interaction anchor unchanged");
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        foreach (var mesh in meshes) AssetDatabase.SaveAssetIfDirty(mesh);
        AssetDatabase.SaveAssetIfDirty(extension);
        Check(AssetDatabase.AssetPathToGUID(prefabPath) == guidBefore, "Prefab GUID retained for both existing map instances");
        Check(!ShaderUtil.GetShaderMessages(extension.shader).Any(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error), "Cozy shader has no compile errors");
        Directory.CreateDirectory(Output);
        var report = new StringBuilder("NATIVE UNITY SCANNER ART PASS\n");
        for (int i = 0; i < 3; i++) report.AppendLine($"LOD{i}: {meshes[i].triangles.Length / 3} triangles, 2 shared atlas materials");
        report.AppendLine("One original collider; interaction component and anchor retained; prefab identity retained.");
        report.AppendLine("Atlas 01 reused for cream and brushed metal; additive GIMP atlas 04 at 512x512.");
        File.WriteAllText(Output + "/UnityAudit.txt", report.ToString());
        return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }

    public static void ApplyBatch()
    {
        try { Apply(); Debug.Log("SCANNER_ART_UNITY_PASS"); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
#endif
