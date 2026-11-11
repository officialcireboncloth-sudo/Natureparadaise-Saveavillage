using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Read-only import checks for the one shared crop material.</summary>
[InitializeOnLoad]
public static class CropSharedAtlasValidation
{
    const string Atlas = "NP_Plants_Unified_01";
    const string Root = "Assets/Nature  Paradaise/";
    const string ReportPath = "ArtSource/Crops/Unified/Review/UnityImportAudit.json";
    const string RequestPath = "ArtSource/Crops/Unified/Review/UnityImportRequest.json";

    [Serializable] sealed class Result
    {
        public string status, utc, method, unityVersion;
        public int prefabs, renderers, materials, shaderErrors;
        public string[] errors;
        public string playMode = "NOT_RUN";
        public string mobileProfiling = "NOT_RUN";
    }

    static CropSharedAtlasValidation() => EditorApplication.delayCall += ValidatePending;

    static void ValidatePending()
    {
        if (!File.Exists(RequestPath)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += ValidatePending;
            return;
        }
        if (File.Exists(ReportPath) && File.GetLastWriteTimeUtc(ReportPath) >= File.GetLastWriteTimeUtc(RequestPath)) return;
        Validate();
    }

    [MenuItem("Nature Paradise/Crops/Validate Shared Atlas")]
    public static void Validate()
    {
        var errors = new List<string>();
        int prefabCount = 0, rendererCount = 0, shaderErrors = 0;
        var materials = new HashSet<Material>();
        var shared = AssetDatabase.LoadAssetAtPath<Material>(Root + "Material/Shared/Plants/" + Atlas + ".mat");
        if (shared == null) errors.Add("Shared crop material missing.");
        else
        {
            if (!shared.enableInstancing) errors.Add("GPU instancing disabled on shared material.");
            if (shared.shader == null || !shared.shader.isSupported) errors.Add("Plant shader missing or unsupported.");
            else
            {
                var variants = new ShaderVariantCollection();
                variants.Add(new ShaderVariantCollection.ShaderVariant(shared.shader, PassType.ScriptableRenderPipeline,
                    "INSTANCING_ON", "_NORMALMAP", "_METALLICSPECGLOSSMAP"));
                variants.WarmUp();
                foreach (var message in ShaderUtil.GetShaderMessages(shared.shader))
                    if (message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                    {
                        ++shaderErrors;
                        errors.Add(message.message);
                    }
                UnityEngine.Object.DestroyImmediate(variants);
            }
            foreach (string property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap" })
            {
                var texture = shared.GetTexture(property) as Texture2D;
                if (texture == null || texture.width != 4096 || texture.height != 4096)
                    errors.Add(property + " is missing or has an unexpected imported size.");
            }
        }
        foreach (string id in AssetDatabase.FindAssets("t:Prefab", new[] { Root + "Prefabs/Crops" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(id);
            if (!Path.GetFileName(path).StartsWith("NP_", StringComparison.Ordinal)) continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { errors.Add("Could not import " + path); continue; }
            ++prefabCount;
            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                ++rendererCount;
                if (renderer.sharedMaterials.Length != 1) errors.Add(path + " has multiple material slots.");
                foreach (var material in renderer.sharedMaterials)
                {
                    materials.Add(material);
                    if (material != shared) errors.Add(path + " uses a different material.");
                }
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) errors.Add(path + " has a missing mesh.");
            }
        }
        if (materials.Count != 1 || prefabCount == 0 || rendererCount == 0) errors.Add("Expected populated crop prefabs using one material.");
        var report = new Result {
            status = errors.Count == 0 ? "PASS" : "FAIL", utc = DateTime.UtcNow.ToString("O"),
            method = "Unity Editor imported assets and warmed instanced forward shader variant; no Play Mode or phone profiling",
            unityVersion = Application.unityVersion, prefabs = prefabCount, renderers = rendererCount,
            materials = materials.Count, shaderErrors = shaderErrors, errors = errors.ToArray()
        };
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        if (errors.Count == 0) Debug.Log($"[Crop Atlas] PASS: {prefabCount} prefabs, {rendererCount} renderers, one shared material.");
        else Debug.LogError("[Crop Atlas] " + string.Join("\n", errors));
    }
}
