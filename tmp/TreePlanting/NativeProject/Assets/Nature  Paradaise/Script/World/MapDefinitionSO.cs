using System;
using System.Collections.Generic;
using UnityEngine;

public enum MapMarkerCategory
{
    Location,
    Shop,
    NPC,
    QuestAvailable,
    QuestTarget,
    Story,
    FastTravel
}

[Serializable]
public sealed class MapRegionDefinition
{
    public string areaId = "area.new";
    public string displayName = "New Area";
    [Tooltip("Posisi area pada layout map, memakai koordinat normal 0-1.")]
    public Rect normalizedRect = new(0.1f, 0.1f, 0.3f, 0.3f);
    public Color color = new(0.22f, 0.42f, 0.24f, 1f);
    public bool discoveredByDefault;
}

[Serializable]
public sealed class MapStaticMarkerDefinition
{
    public string markerId = "location.new";
    public string displayName = "Location";
    public MapMarkerCategory category;
    [Tooltip("Posisi marker pada layout map, memakai koordinat normal 0-1.")]
    public Vector2 normalizedPosition = new(0.5f, 0.5f);
    public Sprite icon;
    public bool unlockedByDefault = true;
    [Tooltip("Nama GameObject scene yang menjadi posisi marker sebenarnya.")]
    public string sceneObjectName;
    public bool followSceneObject = true;
    [Tooltip("Sembunyikan marker jika object tersebut memang belum dibangun/tersedia.")]
    public bool hideIfSceneObjectMissing = true;
}

[Serializable]
public sealed class MapSceneDefinition
{
    public string mapId = "interior.new";
    public string displayName = "Interior";
    [Tooltip("Nama scene Unity, misalnya HouseInterior atau BarnInterior.")]
    public string sceneName;
    public Sprite background;
    public Color backgroundColor = new(0.12f, 0.10f, 0.08f, 1f);
    public bool useTopDownWorldCapture = true;
    [Range(-180f, 180f)] public float mapRotationDegrees;
    public bool fogOfWarEnabled;
    [Tooltip("Hitung batas map dari Renderer yang aktif di scene ini.")]
    public bool autoBoundsFromScene = true;
    public Vector2 worldMin = new(-12f, -10f);
    public Vector2 worldMax = new(12f, 10f);
    public List<MapRegionDefinition> regions = new();
    public List<MapStaticMarkerDefinition> staticMarkers = new();
}

[Serializable]
public sealed class MapFastTravelPointDefinition
{
    public string pointId = "travel.new";
    public string displayName = "Fast Travel";
    [Tooltip("Map tempat marker ditampilkan. world.main untuk world map.")]
    public string mapId = "world.main";
    public Vector2 normalizedPosition = new(0.5f, 0.5f);
    [Tooltip("Nama object tujuan di scene. Posisi normalized hanya menjadi fallback.")]
    public string sceneObjectName;
    public Vector3 arrivalOffset;
    public bool unlockedByDefault;
    [Min(0.5f)] public float discoveryRadius = 8f;
}

/// <summary>
/// Seluruh layout map berada dalam satu asset. Background, batas dunia, region, marker,
/// warna, zoom, dan frekuensi update dapat diganti tanpa mengubah kode UI.
/// </summary>
[CreateAssetMenu(menuName = "Nature Paradise/Map/Map Definition", fileName = "World Map")]
public sealed class MapDefinitionSO : ScriptableObject
{
    [Header("Identity & Artwork")]
    public string mapId = "world.main";
    public string displayName = "Nature Paradise";
    public Sprite background;
    public Color backgroundColor = new(0.08f, 0.15f, 0.12f, 1f);

    [Header("Temporary Top-Down Capture")]
    [Tooltip("Memakai snapshot kamera atas sebagai artwork map. Kamera hanya render saat dibutuhkan.")]
    public bool useTopDownWorldCapture = true;
    [Range(256, 2048)] public int desktopCaptureResolution = 1024;
    [Range(256, 1024)] public int mobileCaptureResolution = 512;
    public Color captureClearColor = new(0.08f, 0.14f, 0.16f, 1f);

    [Header("World Mapping")]
    [Tooltip("Menggabungkan bounds seluruh Terrain aktif. Cocok ketika layout world masih berubah.")]
    public bool autoBoundsFromTerrain = true;
    public Vector2 worldMin = new(-288f, -822f);
    public Vector2 worldMax = new(1712f, 1178f);
    [Tooltip("Rotasi layout map. Positif memutar map berlawanan arah jarum jam.")]
    [Range(-180f, 180f)] public float mapRotationDegrees = -81f;

    [Header("Performance")]
    [Range(0.05f, 1f)] public float markerUpdateInterval = 0.12f;
    [Range(0.1f, 2f)] public float areaCheckInterval = 0.25f;
    [Range(0.5f, 2f)] public float minimumZoom = 0.8f;
    [Range(1f, 5f)] public float maximumZoom = 3f;
    public bool miniMapEnabledByDefault;

    [Header("Fog of War")]
    public bool fogOfWarEnabled = true;
    [Range(12, 48)] public int fogColumns = 30;
    [Range(8, 32)] public int fogRows = 18;
    [Range(1, 5)] public int fogRevealRadiusCells = 2;
    [Range(0.5f, 1f)] public float unexploredFogOpacity = 0.92f;

    [Header("Layout")]
    public List<MapRegionDefinition> regions = new();
    public List<MapStaticMarkerDefinition> staticMarkers = new();

    [Header("Separate Scene / Interior Maps")]
    public List<MapSceneDefinition> sceneMaps = new();

    [Header("Fast Travel")]
    public List<MapFastTravelPointDefinition> fastTravelPoints = new();

    public MapSceneDefinition FindSceneMap(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || sceneMaps == null) return null;
        return sceneMaps.Find(map => map != null &&
            string.Equals(map.sceneName, sceneName, StringComparison.OrdinalIgnoreCase));
    }

    public static MapDefinitionSO CreateRuntimeDefault()
    {
        MapDefinitionSO result = CreateInstance<MapDefinitionSO>();
        result.name = "World Map Runtime Default";
        result.useTopDownWorldCapture = true;
        result.regions = new List<MapRegionDefinition>
        {
            Region("farm", "Farm", new Rect(0.31f, 0.67f, 0.24f, 0.22f), new Color(0.31f, 0.55f, 0.25f, 1f), false),
            Region("village", "Village", new Rect(0.30f, 0.40f, 0.35f, 0.25f), new Color(0.55f, 0.43f, 0.26f, 1f), false),
            Region("forest", "Pinewood Forest", new Rect(0.06f, 0.53f, 0.23f, 0.38f), new Color(0.13f, 0.34f, 0.18f, 1f), false),
            Region("mountain", "Mountain", new Rect(0.58f, 0.60f, 0.34f, 0.31f), new Color(0.31f, 0.35f, 0.31f, 1f), false),
            Region("beach", "Beach", new Rect(0.15f, 0.10f, 0.68f, 0.24f), new Color(0.62f, 0.55f, 0.34f, 1f), false)
        };
        result.staticMarkers = new List<MapStaticMarkerDefinition>
        {
            Marker("player.house", "Player House", MapMarkerCategory.Location, new Vector2(0.43f, 0.76f), "HouseEntranceDoor_Editable"),
            Marker("farm", "Farm", MapMarkerCategory.Location, new Vector2(0.49f, 0.70f), "FieldArea_Editable"),
            Marker("general.store", "General Store", MapMarkerCategory.Shop, new Vector2(0.43f, 0.51f), "NPCSeller"),
            Marker("hospital", "Hospital", MapMarkerCategory.Location, new Vector2(0.37f, 0.46f), "VillageClinicSpawn"),
            Marker("carpenter", "Carpenter", MapMarkerCategory.Shop, new Vector2(0.60f, 0.66f), "Carpenter_Editable"),
            Marker("market", "Market", MapMarkerCategory.Location, new Vector2(0.45f, 0.55f), "Market"),
            Marker("npc.mina", "Mina", MapMarkerCategory.NPC, new Vector2(0.55f, 0.45f), "NPC_QuestTester_Mina_Editable")
        };
        result.sceneMaps = DefaultInteriorMaps();
        result.fastTravelPoints = DefaultFastTravelPoints();
        return result;
    }

    public static List<MapSceneDefinition> DefaultInteriorMaps() => new()
    {
        new MapSceneDefinition
        {
            mapId = "interior.house", displayName = "Player House", sceneName = "HouseInterior",
            worldMin = new Vector2(9984f, 9989f), worldMax = new Vector2(10016f, 10011f),
            staticMarkers = new List<MapStaticMarkerDefinition>
            {
                Marker("house.exit", "Exit", MapMarkerCategory.Location, new Vector2(0.5f, 0.08f), "HouseInteriorExitDoor_Editable"),
                Marker("house.bed", "Bed", MapMarkerCategory.Location, new Vector2(0.25f, 0.7f), "Bed_MeshSlot"),
                Marker("house.kitchen", "Kitchen", MapMarkerCategory.Location, new Vector2(0.75f, 0.62f), "Kitchen_MeshSlot"),
                Marker("house.refrigerator", "Refrigerator", MapMarkerCategory.Location, new Vector2(0.86f, 0.75f), "Refrigerator_MeshSlot"),
                Marker("house.aquarium", "Aquarium", MapMarkerCategory.Location, new Vector2(0.18f, 0.52f), "Aquarium_TestFurniture_Editable")
            }
        },
        new MapSceneDefinition
        {
            mapId = "interior.barn", displayName = "Barn Interior", sceneName = "BarnInterior",
            worldMin = new Vector2(-18f, -14f), worldMax = new Vector2(18f, 14f),
            staticMarkers = new List<MapStaticMarkerDefinition>
            {
                Marker("barn.exit", "Exit", MapMarkerCategory.Location, new Vector2(0.5f, 0.08f), "BarnExitDoor_Editable")
            }
        }
    };

    public static List<MapFastTravelPointDefinition> DefaultFastTravelPoints() => new()
    {
        Travel("travel.home", "Player House", "PlayerHouseExitSpawn", true, 8f),
        Travel("travel.general-store", "General Store", "NPCSeller", false, 10f, new Vector3(0f, 0f, -3f)),
        Travel("travel.clinic", "Hospital", "VillageClinicSpawn", false, 10f),
        Travel("travel.carpenter", "Carpenter", "Carpenter_Editable", false, 10f, new Vector3(0f, 0f, -3f))
    };

    static MapRegionDefinition Region(string id, string label, Rect rect, Color color, bool discovered) =>
        new() { areaId = id, displayName = label, normalizedRect = rect, color = color, discoveredByDefault = discovered };

    static MapStaticMarkerDefinition Marker(string id, string label, MapMarkerCategory category, Vector2 position,
        string sceneObjectName) => new()
    {
        markerId = id,
        displayName = label,
        category = category,
        normalizedPosition = position,
        unlockedByDefault = true,
        followSceneObject = true,
        hideIfSceneObjectMissing = true,
        sceneObjectName = sceneObjectName
    };

    static MapFastTravelPointDefinition Travel(string id, string label, string sceneObjectName,
        bool unlocked, float radius, Vector3 offset = default) => new()
    {
        pointId = id,
        displayName = label,
        mapId = "world.main",
        sceneObjectName = sceneObjectName,
        unlockedByDefault = unlocked,
        discoveryRadius = radius,
        arrivalOffset = offset
    };
}
