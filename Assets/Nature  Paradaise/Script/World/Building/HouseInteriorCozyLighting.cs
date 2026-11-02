using UnityEngine;

/// <summary>Local window light for the cozy Lv1 room; follows the existing clock and weather.</summary>
public sealed class HouseInteriorCozyLighting : MonoBehaviour
{
    [SerializeField] Light windowLight;
    [SerializeField] Renderer windowRenderer;
    [SerializeField] int windowMaterialIndex;
    MaterialPropertyBlock pane;
    float nextUpdate;

    public void Configure(Light light, Renderer renderer, int materialIndex)
    {
        windowLight = light; windowRenderer = renderer; windowMaterialIndex = materialIndex;
    }
    void OnEnable() { nextUpdate = 0; Refresh(); }
    void Update()
    {
        if (Time.unscaledTime < nextUpdate) return;
        nextUpdate = Time.unscaledTime + .25f;
        Refresh();
    }
    void Refresh() => ApplyLighting(TimeManager.Instance != null ? TimeManager.Instance.CurrentTimeHours : 10,
        WeatherSystem.Instance != null ? WeatherSystem.Instance.CurrentWeather : WeatherType.Sunny);

    public void ApplyLighting(float hour, WeatherType weather)
    {
        float daylight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(5, 7, hour)) *
            (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(17, 19, hour)));
        float sky = weather switch
        {
            WeatherType.Sunny or WeatherType.Heatwave => 1,
            WeatherType.PartlyCloudy => .72f,
            WeatherType.Cloudy => .35f,
            WeatherType.Drizzle => .28f,
            WeatherType.Rain => .20f,
            WeatherType.HeavyRain => .13f,
            WeatherType.Snow => .48f,
            _ => .09f
        };
        if (windowLight != null)
        {
            windowLight.intensity = Mathf.Lerp(.30f, 18.0f * sky, daylight);
            windowLight.color = Color.Lerp(new Color(.48f, .64f, 1), new Color(1, .84f, .61f), daylight);
            windowLight.shadows = Application.isMobilePlatform ? LightShadows.None : LightShadows.Soft;
        }
        if (windowRenderer == null) return;
        pane ??= new MaterialPropertyBlock();
        pane.Clear();
        pane.SetColor("_EmissionColor", Color.Lerp(new Color(.09f, .14f, .25f), new Color(.65f, .56f, .35f) * sky, daylight));
        windowRenderer.SetPropertyBlock(pane, windowMaterialIndex);
    }
}
