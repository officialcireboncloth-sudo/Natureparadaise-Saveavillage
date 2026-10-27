using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>Imports the small PNG artwork and connects existing UI data without editing scenes.</summary>
public static class UIIconArtworkSetup
{
    public const string Root = "Assets/Nature  Paradaise";
    public const string Icons = Root + "/Textures/UI/Icons";
    public const string AtlasPath = Root + "/Textures/UI/Atlases/atlas_ui_weather_tools.spriteatlasv2";
    public static readonly string[] WeatherNames = {
        "Sunny", "PartlyCloudy", "Heatwave", "Drizzle", "Rain", "HeavyRain",
        "WindRainStorm", "Cyclone", "Thunderstorm", "Snow", "Blizzard",
        "ClearNight", "PartlyCloudyNight"
    };
    public static readonly string[] ToolNames = {
        "Hoe", "Sickle", "Hammer", "WateringCan", "Axe", "FishingRod",
        "Shears", "Pitchfork", "Brush", "Milker", "Pickaxe"
    };
    public static readonly string[] CompanionNames = { "Horse", "Dog", "Follow", "Stay", "Home", "Mount" };

    [MenuItem("Nature Paradise/UI/Apply Companion PNG Icons")]
    public static void ApplyCompanionIcons()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before changing icon assets.");
        AssetDatabase.Refresh();
        foreach (string name in CompanionNames) Import("Companions", name);
        var theme = Required<CompanionCommandTheme>(Root + "/Resources/UI/CompanionCommandTheme.asset");
        theme.horseIcon = Icon("Companions", "Horse");
        theme.dogIcon = Icon("Companions", "Dog");
        theme.followIcon = Icon("Companions", "Follow");
        theme.stayIcon = Icon("Companions", "Stay");
        theme.homeIcon = Icon("Companions", "Home");
        theme.mountIcon = Icon("Companions", "Mount");
        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Nature Paradise/UI/Apply Weather and Tool PNG Icons")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before changing icon assets.");
        EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
        AssetDatabase.Refresh();
        OrganizeArtwork();
        ApplyCompanionIcons();
        foreach (string name in WeatherNames) Import("Weather", name);
        foreach (string name in ToolNames) Import("Tools", name);
        foreach (string name in new[] { "Wood", "Stone" })
        {
            Import("Materials", name);
            var item = Required<ItemSO>(Root + "/Resources/Items/Materials/" + name + ".asset");
            item.icon = Icon("Materials", name);
            EditorUtility.SetDirty(item);
        }

        var weather = Required<WeatherForecastTVTheme>(Root + "/Resources/UI/WeatherForecastTVTheme.asset");
        weather.weatherIcons = new WeatherForecastTVTheme.WeatherArtwork[11];
        for (int i = 0; i < weather.weatherIcons.Length; i++)
            weather.weatherIcons[i] = new WeatherForecastTVTheme.WeatherArtwork {
                weather = (WeatherType)i, icon = Icon("Weather", WeatherNames[i])
            };
        weather.clearNightIcon = Icon("Weather", "ClearNight");
        weather.partlyCloudyNightIcon = Icon("Weather", "PartlyCloudyNight");
        weather.rainIcon = Icon("Weather", "Rain");
        weather.forecastIcon = Icon("Weather", "PartlyCloudy");
        EditorUtility.SetDirty(weather);

        string[] assets = { "Hoe Tool", "Sickle Tool", "Hammer Tool", "Watering Can Tool",
            "Axe Tool", "Fishing Rod", "Shears Tool", "Pitchfork Tool" };
        for (int i = 0; i < assets.Length; i++)
        {
            var item = Required<ItemSO>(Root + "/Resources/Items/Tools/" + assets[i] + ".asset");
            item.icon = Icon("Tools", ToolNames[i]);
            EditorUtility.SetDirty(item);
        }
        var care = Required<AnimalInteractionTheme>(Root + "/Resources/UI/AnimalInteractionTheme.asset");
        var milker = Required<ItemSO>(Root + "/Resources/Items/Tools/Milker Tool.asset");
        milker.icon = Icon("Tools", "Milker");
        EditorUtility.SetDirty(milker);
        care.brushIcon = Icon("Tools", "Brush");
        care.shearsIcon = Icon("Tools", "Shears");
        care.milkIcon = Icon("Tools", "Milker");
        care.sunIcon = Icon("Weather", "Sunny");
        EditorUtility.SetDirty(care);
        var bell = Required<AnimalBellTheme>(Root + "/Resources/UI/AnimalBellTheme.asset");
        bell.rainIcon = Icon("Weather", "Rain");
        bell.snowIcon = Icon("Weather", "Snow");
        bell.fairWeatherIcon = Icon("Weather", "Sunny");
        EditorUtility.SetDirty(bell);
        var mining = Required<MiningHUDTheme>(Root + "/Resources/UI/MiningHUDTheme.asset");
        mining.mineIcon = Icon("Tools", "Pickaxe");
        EditorUtility.SetDirty(mining);
        var inventory = Required<InventoryTheme>(Root + "/Resources/UI/InventoryTheme.asset");
        inventory.toolsIcon = Icon("Tools", "Hoe");
        EditorUtility.SetDirty(inventory);

        var atlas = File.Exists(AtlasPath) ? SpriteAtlasAsset.Load(AtlasPath) : new SpriteAtlasAsset();
        var packables = new UnityEngine.Object[] { Required<DefaultAsset>(Icons) };
        atlas.Remove(packables);
        atlas.Add(packables);
        SpriteAtlasAsset.Save(atlas, AtlasPath);
        AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);
        var atlasImporter = (SpriteAtlasImporter)AssetImporter.GetAtPath(AtlasPath);
        atlasImporter.includeInBuild = true;
        // The custom weather Graphic uses rectangular UVs; sprites must never rotate or trim.
        var packing = atlasImporter.packingSettings;
        packing.padding = 4; packing.enableRotation = false; packing.enableTightPacking = false;
        atlasImporter.packingSettings = packing;
        var texture = atlasImporter.textureSettings;
        texture.readable = false; texture.generateMipMaps = false; texture.sRGB = true;
        texture.filterMode = FilterMode.Bilinear; texture.anisoLevel = 1;
        atlasImporter.textureSettings = texture;
        var desktop = atlasImporter.GetPlatformSettings("DefaultTexturePlatform");
        desktop.maxTextureSize = 1024;
        desktop.format = TextureImporterFormat.RGBA32;
        desktop.textureCompression = TextureImporterCompression.Uncompressed;
        atlasImporter.SetPlatformSettings(desktop);
        foreach (string platform in new[] { "Android", "iPhone" })
        {
            var mobile = atlasImporter.GetPlatformSettings(platform);
            mobile.name = platform; mobile.overridden = true; mobile.maxTextureSize = 1024;
            mobile.format = TextureImporterFormat.ASTC_6x6; mobile.compressionQuality = 50;
            atlasImporter.SetPlatformSettings(mobile);
        }
        AssetDatabase.SaveAssets();
        atlasImporter.SaveAndReimport();
        Debug.Log("[UI Artwork] 24 transparent 128px PNGs connected; shared atlas limited to 1024px, ASTC 6x6 on mobile.");
    }

    static T Required<T>(string path) where T : UnityEngine.Object =>
        AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing artwork dependency: " + path);

    /// <summary>Stable, readable filenames shared by import tools and artwork validation.</summary>
    public static string IconPath(string group, string name)
    {
        string snake = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
        string prefix = group == "Weather" ? "icon_weather_" : group == "Materials" ? "icon_material_" :
            group == "Companions" ? "icon_companion_" : "icon_tool_";
        return Icons + "/" + group + "/" + prefix + snake + ".png";
    }

    static void OrganizeArtwork()
    {
        string atlases = Root + "/Textures/UI/Atlases";
        if (!AssetDatabase.IsValidFolder(atlases)) AssetDatabase.CreateFolder(Root + "/Textures/UI", "Atlases");
        foreach (string name in WeatherNames) MoveArtwork(Icons + "/Weather/" + name + ".png", IconPath("Weather", name));
        foreach (string name in ToolNames) MoveArtwork(Icons + "/Tools/" + name + ".png", IconPath("Tools", name));
        MoveArtwork(Root + "/Textures/UI/NatureParadiseUIIcons.spriteatlasv2", AtlasPath);
    }

    static void MoveArtwork(string oldPath, string newPath)
    {
        if (!File.Exists(oldPath)) return;
        if (File.Exists(newPath)) throw new InvalidOperationException("Duplicate icon artwork: " + newPath);
        string error = AssetDatabase.MoveAsset(oldPath, newPath);
        if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
    }

    static Sprite Icon(string group, string name) => Required<Sprite>(IconPath(group, name));

    static void Import(string group, string name)
    {
        string path = IconPath(group, name);
        if (!File.Exists(path)) throw new FileNotFoundException("Missing generated PNG", path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Not a texture: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.maxTextureSize = 128; importer.mipmapEnabled = false; importer.isReadable = false;
        importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = true;
        importer.sRGBTexture = true; importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear; importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
