#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class FishPondIconSetup
{
    const string Root="Assets/Nature  Paradaise";
    [MenuItem("Nature Paradise/UI/Fish Pond/Apply PNG Silhouette Icons")]
    public static void Apply()
    {
        var theme=AssetDatabase.LoadAssetAtPath<FishPondTheme>(Root+"/Resources/UI/FishPondTheme.asset");
        if(theme==null)throw new InvalidOperationException("FishPondTheme is missing");
        theme.overviewFish=Import("fish");theme.overviewMachine=Import("machine");theme.overviewFeed=Import("feed");theme.overviewHealth=Import("health");
        string path=Root+"/Resources/UI/FishPondSilhouette.mat";
        var shader=Shader.Find("Nature Paradise/UI/PNG Silhouette");if(shader==null)throw new InvalidOperationException("Pond icon shader is missing");
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(shader){name="FishPondSilhouette"};AssetDatabase.CreateAsset(material,path);}else material.shader=shader;
        theme.overviewIconMaterial=material;EditorUtility.SetDirty(theme);EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
    }
    static Sprite Import(string name)
    {
        string path=Root+"/Textures/UI/Icons/FishPond/icon_pond_"+name+"_silhouette.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;
        importer.textureShape=TextureImporterShape.Texture2D;
        importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=128;
        var textureSettings=new TextureImporterSettings();importer.ReadTextureSettings(textureSettings);textureSettings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(textureSettings);
        importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
        importer.isReadable=false;importer.maxTextureSize=128;importer.npotScale=TextureImporterNPOTScale.None;
        importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;
        foreach(string platform in new[]{"Android","iPhone"})
        {var settings=importer.GetPlatformTextureSettings(platform);settings.overridden=true;settings.maxTextureSize=128;settings.format=TextureImporterFormat.ASTC_4x4;importer.SetPlatformTextureSettings(settings);}
        importer.SaveAndReimport();AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        var imported=AssetDatabase.LoadAllAssetsAtPath(path);var sprite=imported.OfType<Sprite>().FirstOrDefault();
        if(sprite==null)throw new InvalidOperationException(path+" has no Sprite subasset: "+string.Join(", ",imported.Select(a=>a.GetType().Name+" "+a.name)));
        return sprite;
    }
}
#endif
