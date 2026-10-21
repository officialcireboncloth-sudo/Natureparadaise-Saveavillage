#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit import settings shared by every Barn B level and LOD.</summary>
public static class BarnNormalAtlasSetup
{
    public const string Folder = "Assets/Nature  Paradaise/Textures/Buildings/BarnStyle_B";
    public const string NormalPath = Folder + "/TFP_Atlas_BarnB_Normal.png";
    public const string ShaderPath = Folder + "/BarnAtlasNormal.shader";
    public const string MaterialPath = Folder + "/TFP_Atlas_BarnB_Wood_1A.mat";

    [MenuItem("Nature Paradise/Barn/Apply Shared Normal Atlas")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before importing the barn atlas.");
        AssetDatabase.ImportAsset(NormalPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(NormalPath);
        importer.textureType = TextureImporterType.NormalMap;
        importer.convertToNormalmap = false;
        importer.sRGBTexture = false;
        importer.flipGreenChannel = false; // GIMP normal-map exports tangent-space +Y.
        importer.maxTextureSize = 1024;
        importer.mipmapEnabled = true;
        importer.isReadable = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.anisoLevel = 1;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.crunchedCompression = false;
        Platform(importer, "Standalone", TextureImporterFormat.BC5);
        // ETC2 is supported by the project's GLES3 mobile baseline, avoiding an
        // ASTC fallback that would silently inflate memory on older Android GPUs.
        Platform(importer, "Android", TextureImporterFormat.ETC2_RGBA8);
        Platform(importer, "iPhone", TextureImporterFormat.ASTC_6x6);
        importer.SaveAndReimport();
        AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceSynchronousImport);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (!shader || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("Barn normal shader did not compile.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (!material) throw new FileNotFoundException(MaterialPath);
        material.shader = shader;
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath));
        material.SetFloat("_BumpScale", 1f);
        material.SetFloat("_BarnNormalEnabled", 1);
        material.EnableKeyword("_BARN_NORMALMAP");
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        // Rebuild UV-derived tangents after merged meshes and reversed open
        // machine faces. This changes no geometry, normals, UVs or asset IDs.
        foreach (string path in AssetDatabase.FindAssets("t:Mesh", new[]{BarnStyleBSetup.Models})
                     .Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".asset")).Distinct())
            foreach (var mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>())
            {
                mesh.RecalculateTangents();
                mesh.UploadMeshData(false);
                EditorUtility.SetDirty(mesh);
            }
        AssetDatabase.SaveAssets();
        Debug.Log("Barn B shared normal atlas applied: 1024, mipmaps, BC5 / ETC2 / ASTC; one shared material.");
    }

    static void Platform(TextureImporter importer, string platform, TextureImporterFormat format)
    {
        var settings = importer.GetPlatformTextureSettings(platform);
        settings.name = platform;
        settings.overridden = true;
        settings.maxTextureSize = 1024;
        settings.format = format;
        settings.textureCompression = TextureImporterCompression.Compressed;
        settings.compressionQuality = 50;
        settings.crunchedCompression = false;
        importer.SetPlatformTextureSettings(settings);
    }
}
#endif
