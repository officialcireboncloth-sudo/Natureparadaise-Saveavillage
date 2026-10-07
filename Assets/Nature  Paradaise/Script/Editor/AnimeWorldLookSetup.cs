using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Explicit, repeatable anime surface preset; foliage keeps its wind shader.</summary>
public static class AnimeWorldLookSetup
{
    const string Root = "Assets/Nature  Paradaise";
    [MenuItem("Nature Paradise/World/Apply Anime Surface Look")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Apply in Edit Mode.");
        var shader = Shader.Find("Nature Paradise/Anime Surface");
        if (shader == null) throw new System.InvalidOperationException("Anime Surface shader has not imported.");
        int changed = 0;
        Directory.CreateDirectory("Library/AnimeSurfaceBackups");
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { Root + "/Material" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == null) continue;
            bool player = path.EndsWith("/m_PlayerDummy.mat");
            bool eligible = material.shader.name == "Toon/CustomToon" || material.shader == shader;
            if (!eligible || material.color.a < .99f) continue;
            string backup = "Library/AnimeSurfaceBackups/" + guid + ".mat";
            if (!File.Exists(backup)) File.Copy(path, backup);
            var texture = material.GetTexture("_TextureSample");
            var scale = material.GetTextureScale("_TextureSample");
            var offset = material.GetTextureOffset("_TextureSample");
            var tint = material.color;
            material.shader = shader;
            material.shaderKeywords = System.Array.Empty<string>();
            material.SetTexture("_TextureSample", texture);
            material.SetTextureScale("_TextureSample", scale);
            material.SetTextureOffset("_TextureSample", offset);
            material.color = tint;
            material.SetColor("_ShadowColor", new Color(.64f, .72f, .88f));
            material.SetFloat("_ShadeThreshold", .52f);
            material.SetFloat("_ShadeSoftness", player ? .09f : .22f);
            material.SetFloat("_ShadeStrength", player ? .55f : .45f);
            material.SetFloat("_CastShadowStrength", .78f);
            material.SetFloat("_AmbientInfluence", .22f);
            material.SetFloat("_Brightness", player ? 1.05f : 1.1f);
            material.SetFloat("_RimStrength", player ? .06f : .025f);
            material.SetFloat("_SkinEnabled", player ? 1 : 0);
            material.SetFloat("_AutoSkinMask", 1);
            material.SetFloat("_SkinCleanliness", .2f);
            material.SetFloat("_SkinBrightness", 1.12f);
            material.SetFloat("_SkinShadowStrength", .3f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            changed++;
        }
        var style = Resources.Load<ToonWorldStyle>("World/Toon World Style");
        style.opaqueShader = shader;
        EditorUtility.SetDirty(style);
        var lighting = Resources.Load<GardenLightingProfile>("World/Garden Lighting");
        lighting.ambientSky = new Color(.76f, .86f, 1f);
        lighting.ambientEquator = new Color(.48f, .56f, .66f);
        lighting.ambientGround = new Color(.28f, .32f, .24f);
        lighting.shadowStrength = .8f;
        lighting.noonSky = new Color(.82f, .91f, 1f);
        lighting.noonEquator = new Color(.52f, .61f, .7f);
        lighting.noonGround = new Color(.32f, .36f, .26f);
        lighting.noonShadowStrength = .85f;
        lighting.morningEquator = new Color(.58f, .6f, .54f);
        lighting.morningGround = new Color(.35f, .38f, .28f);
        lighting.eveningEquator = new Color(.6f, .47f, .38f);
        lighting.eveningGround = new Color(.38f, .29f, .22f);
        EditorUtility.SetDirty(lighting);
        if (lighting.postProcessing != null && lighting.postProcessing.TryGet<ColorLookup>(out var lookup))
        { lookup.contribution.Override(.1f); EditorUtility.SetDirty(lookup); }
        if (lighting.postProcessing != null && lighting.postProcessing.TryGet<ColorAdjustments>(out var grade))
        {
            grade.postExposure.Override(.4f);
            grade.contrast.Override(6f);
            grade.saturation.Override(12f);
            EditorUtility.SetDirty(grade);
        }
        string scenePath = Root + "/Map/Scenes/World/Map.unity";
        var scene = SceneManager.GetSceneByPath(scenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try { GardenWorldSetup.ApplyLightingToScene(scene, lighting); EditorSceneManager.SaveScene(scene); }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        AssetDatabase.SaveAssets();
        File.WriteAllText("Library/AnimeSurfaceAudit.txt", "Materials configured: " + changed + "\nFoliage wind shaders preserved. Terrain paint and vegetation density preserved.\n");
    }
}
