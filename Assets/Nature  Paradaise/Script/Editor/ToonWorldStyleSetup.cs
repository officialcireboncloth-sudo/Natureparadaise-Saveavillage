using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit asset migration; no material scanning or conversion during gameplay.</summary>
public static class ToonWorldStyleSetup
{
    const string Root = "Assets/Nature  Paradaise";
    const string Pack = Root + "/Pack/Toon Series/Toon Farm Pack";
    const string StylePath = Root + "/Resources/World/Toon World Style.asset";

    [MenuItem("Nature Paradise/World/Apply Toon Farm Shading")]
    public static void Apply()
    {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Pack/Toon Series/Shared/Shaders/CustomToon.shader");
        var existingStyle = AssetDatabase.LoadAssetAtPath<ToonWorldStyle>(StylePath);
        var ramp = existingStyle != null && existingStyle.lightingRamp != null ? existingStyle.lightingRamp :
            AssetDatabase.LoadAssetAtPath<Texture2D>(Pack + "/Textures/TFP_Toon_Ramp_1C.psd");
        if (shader == null || ramp == null) throw new System.InvalidOperationException("Toon Farm shader/ramp is missing.");
        var rampImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(ramp));
        if (rampImporter.mipmapEnabled || rampImporter.wrapMode != TextureWrapMode.Clamp || rampImporter.textureCompression != TextureImporterCompression.Uncompressed)
        {
            rampImporter.mipmapEnabled = false; rampImporter.wrapMode = TextureWrapMode.Clamp;
            rampImporter.filterMode = FilterMode.Bilinear; rampImporter.textureCompression = TextureImporterCompression.Uncompressed;
            rampImporter.SaveAndReimport();
        }
        Directory.CreateDirectory(Root + "/Resources/World");
        AssetDatabase.Refresh();
        var style = AssetDatabase.LoadAssetAtPath<ToonWorldStyle>(StylePath);
        if (style == null) { style = ScriptableObject.CreateInstance<ToonWorldStyle>(); AssetDatabase.CreateAsset(style, StylePath); }
        style.opaqueShader = shader; style.lightingRamp = ramp; EditorUtility.SetDirty(style);

        var report = new StringBuilder(); int converted = 0, tuned = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { Root }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // FindAssets also returns embedded FBX material subassets. Imported models
            // remain authored source files; only persistent .mat assets are migrated.
            if (!path.EndsWith(".mat", System.StringComparison.OrdinalIgnoreCase)) continue;
            // UI, weather, terrain painting, soil overlays, transparency and particle effects
            // have distinct render contracts and keep their specialized shaders.
            if (path.Contains("/Particles/") || path.Contains("/Skyboxes/") || path.Contains("/Resources/UI/") || path.EndsWith("FishPondWater.mat") ||
                path.Contains("/Material/World/m_Soil") || path.EndsWith("m_HoeMark.mat") || path.EndsWith("m_Field.mat")) continue;
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == null) continue;
            bool packToon = material.shader.name == "Toon/CustomToon" || material.shader.name == "Toon/CustomToonVegetation" || material.shader.name == "Toon/CustomToonGrass";
            if (!packToon && material.shader.name != "Universal Render Pipeline/Lit") continue;
            if (!packToon && ((material.HasProperty("_Surface") && material.GetFloat("_Surface") > .5f) ||
                (material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") > .5f))) continue;
            if (!packToon && material.HasProperty("_BaseColor") && material.GetColor("_BaseColor").a < .99f) continue;

            // Backups are review/recovery artifacts, not imported game assets.
            string backup = "Library/ToonWorldMaterialBackups/" + guid + ".mat";
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (!File.Exists(backup)) File.Copy(path, backup);
            Undo.RecordObject(material, "Apply Toon Farm Shading");
            if (!packToon)
            {
                var texture = material.GetTexture("_BaseMap");
                var scale = material.GetTextureScale("_BaseMap"); var offset = material.GetTextureOffset("_BaseMap");
                var color = material.GetColor("_BaseColor");
                material.shader = shader;
                material.shaderKeywords = System.Array.Empty<string>();
                material.SetTexture("_TextureSample", texture);
                material.SetTextureScale("_TextureSample", scale); material.SetTextureOffset("_TextureSample", offset);
                material.SetColor("_Color", color);
                material.renderQueue = -1; material.SetOverrideTag("RenderType", "Opaque");
                converted++;
            }
            else tuned++;
            material.SetTexture("_TextureRamp", ramp);
            material.SetFloat("_SpecularHighlights", 0); material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.SetFloat("_ReceiveShadows", 1); material.DisableKeyword("_RECEIVE_SHADOWS_OFF");
            EditorUtility.SetDirty(material); report.AppendLine(path + " -> " + material.shader.name);
        }
        AssetDatabase.SaveAssets();
        report.Insert(0, $"Converted={converted}, tuned={tuned}, ramp={ramp.name}\n");
        File.WriteAllText("Library/ToonWorldMaterialAudit.txt", report.ToString());
        Debug.Log($"[TOON WORLD] Applied: {converted} opaque materials, {tuned} existing Toon Farm materials.");
    }
}
