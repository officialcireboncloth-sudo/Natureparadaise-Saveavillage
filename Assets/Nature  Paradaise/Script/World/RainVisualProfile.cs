using UnityEngine;

/// <summary>Shared rain appearance and hard budgets; independent of weather gameplay.</summary>
[CreateAssetMenu(menuName = "Nature Paradise/Weather/Rain Visual Profile")]
public sealed class RainVisualProfile : ScriptableObject
{
    public Material dropMaterial;
    public Material splashMaterial;
    [Header("Soft water colours")]
    public Color waterTint = new(.72f, .80f, .84f, 1);
    [Range(.05f, .6f)] public float opacity = .28f;
    [Header("Drop shape in world metres")]
    [Range(.015f, .08f)] public float width = .045f;
    [Range(2f, 8f)] public float lengthScale = 5f;
    [Range(4f, 10f)] public float fallSpeed = 7f;
    [Range(1f, 4f)] public float fallHeight = 3f;
    [Range(.1f, 1f)] public float splashSize = .18f;
    [Range(.5f, 3f)] public float transitionSeconds = 1.2f;
    [Header("Small-screen readability")]
    [Tooltip("Minimum projected drop width at a 720-pixel short screen edge. Scales with screen resolution and camera zoom.")]
    [Range(1f, 4f)] public float minimumDropWidthPixels = 2.6f;
    [Header("Maximum emission per second (Normal)")]
    [Range(10, 80)] public int drizzleRate = 55;
    [Range(30, 140)] public int rainRate = 85;
    [Range(50, 180)] public int heavyRate = 115;
    [Range(60, 220)] public int stormRate = 145;
    [Header("Hard particle limits")]
    [Range(32, 400)] public int desktopDropLimit = 256;
    [Range(16, 160)] public int desktopSplashLimit = 96;
    [Range(32, 256)] public int mobileDropLimit = 160;
    [Range(16, 96)] public int mobileSplashLimit = 64;
    [Tooltip("Surface probes per second, not per drop. Hits are shared by a small cluster of drops.")]
    [Range(8, 80)] public int surfaceProbesPerSecond = 32;
    public LayerMask surfaceMask = Physics.DefaultRaycastLayers;

    /// <summary>Keeps light/mobile drizzle readable without raising the particle or raycast budgets.</summary>
    public float Density(WeatherType weather, int mode, bool mobile)
    {
        bool drizzle = weather == WeatherType.Drizzle;
        float density = mode == 1 ? (drizzle ? .65f : .55f) : 1f;
        return density * (mobile ? (drizzle ? .9f : .8f) : 1f);
    }

    public float Rate(WeatherType weather) => weather switch
    {
        WeatherType.Drizzle => drizzleRate,
        WeatherType.Rain => rainRate,
        WeatherType.HeavyRain => heavyRate,
        WeatherType.WindRainStorm or WeatherType.Cyclone or WeatherType.Thunderstorm => stormRate,
        _ => 0
    };

    public Vector3 Velocity(WeatherType weather) => (weather switch
    {
        WeatherType.WindRainStorm => new Vector3(.24f, -1, .04f),
        WeatherType.Cyclone => new Vector3(.32f, -1, .09f),
        WeatherType.Thunderstorm => new Vector3(.16f, -1, 0),
        _ => Vector3.down
    }).normalized * fallSpeed;
}
