#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

[InitializeOnLoad]
static class AngryMeshImportAudit
{
    const string Pack = "Assets/ANGRY MESH";
    const string Meadow = Pack + "/Stylized Pack - Meadow Environment";
    const string Output = "tmp/AngryMeshUrpRepair/";

    static AngryMeshImportAudit()
    {
        if (File.Exists(Output + "verify.request"))
            EditorApplication.update += CheckAfterImport;
    }

    static void CheckAfterImport()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        EditorApplication.update -= CheckAfterImport;
        File.Delete(Output + "verify.request");
        try { Run(); }
        catch (Exception error)
        {
            File.WriteAllText(Output + "UnityAudit.txt", error.ToString());
            Debug.LogException(error);
        }
    }

    static void Run()
    {
        Require(GraphicsSettings.currentRenderPipeline &&
                GraphicsSettings.currentRenderPipeline.GetType().Name == "UniversalRenderPipelineAsset", "URP is active");
        var problems = new List<string>();
        var materials = Paths("*.mat").Select(AssetDatabase.LoadAssetAtPath<Material>).ToArray();
        foreach (var material in materials)
        {
            if (!material || !material.shader || !material.shader.isSupported || material.shader.name == "Hidden/InternalErrorShader")
                problems.Add("Unsupported material or shader: " + (material ? material.name : "<missing>"));
        }
        var prefabs = Paths("*.prefab");
        int prefabRenderers = 0;
        foreach (string path in prefabs)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) { problems.Add("Prefab did not import: " + path); continue; }
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                prefabRenderers++;
                if (renderer.sharedMaterials.Length == 0 || renderer.sharedMaterials.Any(mat => !mat || !materials.Contains(mat)))
                    problems.Add("Unresolved prefab material: " + path + " :: " + renderer.name);
            }
        }

        var models = Paths("*.fbx");
        int modelRenderers = 0;
        foreach (string path in models)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var mapped = importer.GetExternalObjectMap().Values.OfType<Material>().ToArray();
            if (!imported || mapped.Length == 0)
            {
                problems.Add("FBX or material remap did not import: " + path);
                continue;
            }
            var renderers = imported.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0) problems.Add("FBX has no renderers: " + path);
            foreach (var renderer in renderers)
            {
                modelRenderers++;
                if (renderer.sharedMaterials.Length == 0 || renderer.sharedMaterials.Any(mat => !mat || !mapped.Contains(mat)))
                    problems.Add("Unresolved FBX material: " + path + " :: " + renderer.name + " = " +
                                 string.Join(", ", renderer.sharedMaterials.Select(mat => mat ? mat.name : "<missing>")));
            }
        }
        File.WriteAllText(Output + "UnityAudit.txt", string.Join("\n", problems));
        bool allowAsync = ShaderUtil.allowAsyncCompilation;
        ShaderUtil.allowAsyncCompilation = false;
        try
        {
            Render("Prefabs/Trees/Trees - Summer/P_OakTree_01_Summer_Ivy.prefab", "Unity_OakPrefab.png");
            Render("Sources/Meshes/Grass/SM_Grass_01_01.fbx", "Unity_GrassFBX.png");
            Render("Prefabs/Flowers/P_Flower_05_03.prefab", "Unity_FlowerPrefab.png");
            Render("Sources/Meshes/Trees/SM_BirchTree_01.fbx", "Unity_BirchFBX.png");
        }
        finally { ShaderUtil.allowAsyncCompilation = allowAsync; }
        var shaders = materials.Where(mat => mat && mat.shader).Select(mat => mat.shader).Distinct().ToArray();
        foreach (var shader in shaders)
            foreach (var message in ShaderUtil.GetShaderMessages(shader))
                if (message.severity.ToString() == "Error") problems.Add(shader.name + ": " + message.message);

        string result = $"{(problems.Count == 0 ? "PASS" : "FAIL")}: {materials.Length} materials, {shaders.Length} shaders, " +
                        $"{prefabs.Length} prefabs ({prefabRenderers} renderers), {models.Length} FBXs ({modelRenderers} renderers).";
        File.WriteAllLines(Output + "UnityAudit.txt", new[] { result }.Concat(problems));
        Debug.Log("[ANGRY MESH URP] " + result);
    }

    static string[] Paths(string pattern)
    {
        return Directory.GetFiles(Pack, pattern, SearchOption.AllDirectories).Select(path => path.Replace('\\', '/')).ToArray();
    }

    static void Render(string path, string file)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Meadow + "/" + path);
        Require(model, "Sample imported: " + path);
        var preview = new PreviewRenderUtility();
        Texture2D image = null;
        try
        {
            var subject = Object.Instantiate(model);
            preview.AddSingleGO(subject);
            foreach (var lod in subject.GetComponentsInChildren<LODGroup>()) lod.ForceLOD(0);
            if (path.EndsWith(".fbx"))
            {
                foreach (var renderer in subject.GetComponentsInChildren<MeshRenderer>())
                    if (renderer.name.Contains("_LOD") && !renderer.name.EndsWith("_LOD0")) renderer.enabled = false;
            }
            var renderers = subject.GetComponentsInChildren<Renderer>().Where(renderer => renderer.enabled).ToArray();
            Require(renderers.Length > 0, "Sample has a visible renderer: " + path);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            float size = Mathf.Max(bounds.size.y, bounds.size.x, bounds.size.z);
            Require(size > .005f, "Sample has nonzero size: " + path);
            preview.camera.orthographic = true;
            preview.camera.orthographicSize = size * .65f;
            preview.camera.nearClipPlane = .001f;
            preview.camera.farClipPlane = size * 15f;
            preview.camera.transform.position = bounds.center + size * new Vector3(1.7f, 1.1f, 2.3f);
            preview.camera.transform.LookAt(bounds.center);
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(.36f, .40f, .44f);
            preview.ambientColor = new Color(.64f, .67f, .70f);
            preview.lights[0].intensity = 1.4f;
            preview.lights[0].transform.rotation = Quaternion.Euler(38, 22, 0);
            preview.lights[1].intensity = .85f;
            preview.lights[1].transform.rotation = Quaternion.Euler(31, 155, 0);
            preview.BeginStaticPreview(new Rect(0, 0, 460, 460));
            preview.Render(true);
            image = preview.EndStaticPreview();
            File.WriteAllBytes(Output + file, image.EncodeToPNG());
        }
        finally
        {
            if (image) Object.DestroyImmediate(image);
            preview.Cleanup();
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
#endif
