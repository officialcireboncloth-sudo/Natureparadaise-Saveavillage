using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
/// <summary>
/// Menggerakkan matahari dan mengubah ambient lighting berdasarkan waktu 24 jam.
/// Modifier cuaca diterima terpisah agar jadwal cuaca tidak bergantung pada implementasi lighting.
/// </summary>
public sealed class DayNightCycle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] TimeManager timeManager;
    [SerializeField] Light sun;

    [Header("Sun")]
    [SerializeField, Range(0f, 360f)] float sunYaw = 170f;
    [SerializeField, Min(0f)] float dayIntensity = 1.5f;
    [SerializeField, Min(0f)] float nightIntensity = 0.02f;
    [SerializeField] Color sunriseColor = new(1f, 0.48f, 0.25f, 1f);
    [SerializeField] Color noonColor = new(1f, 0.96f, 0.84f, 1f);
    [SerializeField] Color moonColor = new(0.30f, 0.38f, 0.62f, 1f);

    [Header("Environment")]
    [SerializeField] Color dayAmbient = new(0.68f, 0.75f, 0.83f, 1f);
    [SerializeField] Color sunsetAmbient = new(0.42f, 0.25f, 0.22f, 1f);
    [SerializeField] Color nightAmbient = new(0.035f, 0.055f, 0.11f, 1f);
    [SerializeField, Range(0f, 2f)] float dayReflectionIntensity = 1f;
    [SerializeField, Range(0f, 2f)] float nightReflectionIntensity = 0.2f;

    float weatherSunMultiplier = 1f;
    float weatherAmbientMultiplier = 1f;
    float weatherExposureMultiplier = 1f;
    Color weatherTint = Color.white;

    void Awake()
    {
        ResolveReferences();
        if (sun != null)
            RenderSettings.sun = sun;
    }

    void Update()
    {
        if (timeManager == null || sun == null)
        {
            ResolveReferences();
            if (timeManager == null || sun == null)
                return;
        }

        ApplyLighting(timeManager.CurrentTimeHours);
    }

    void ResolveReferences()
    {
        if (timeManager == null)
            timeManager = TimeManager.Instance != null
                ? TimeManager.Instance
                : FindFirstObjectByType<TimeManager>();
        if (sun == null)
            sun = GetComponent<Light>();
    }

    void ApplyLighting(float hour)
    {
        float targetSun = 1f;
        float targetAmbient = 1f;
        float targetExposure = 1f;
        Color targetTint = Color.white;
        if (WeatherSystem.Instance != null)
            WeatherSystem.Instance.GetLightingModifiers(out targetSun, out targetAmbient, out targetExposure, out targetTint);

        float blend = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 1.25f);
        weatherSunMultiplier = Mathf.Lerp(weatherSunMultiplier, targetSun, blend);
        weatherAmbientMultiplier = Mathf.Lerp(weatherAmbientMultiplier, targetAmbient, blend);
        weatherExposureMultiplier = Mathf.Lerp(weatherExposureMultiplier, targetExposure, blend);
        weatherTint = Color.Lerp(weatherTint, targetTint, blend);

        float dayProgress = Mathf.Repeat(hour, 24f) / 24f;
        float solarAngle = dayProgress * 360f - 90f;
        float sunHeight = Mathf.Sin((hour - 6f) / 24f * Mathf.PI * 2f);
        float daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.12f, 0.2f, sunHeight));
        float noonWeight = Mathf.Clamp01(sunHeight);

        transform.rotation = Quaternion.Euler(solarAngle, sunYaw, 0f);
        sun.intensity = Mathf.Lerp(nightIntensity, dayIntensity, daylight) * weatherSunMultiplier;

        Color horizonToNoon = Color.Lerp(sunriseColor, noonColor, noonWeight);
        sun.color = Color.Lerp(moonColor, horizonToNoon, daylight) * weatherTint;

        float twilight = Mathf.Clamp01(1f - Mathf.Abs(sunHeight) * 4f);
        Color daylightAmbient = Color.Lerp(dayAmbient, sunsetAmbient, twilight);
        Color ambient = Color.Lerp(nightAmbient, daylightAmbient, daylight) * weatherTint * weatherAmbientMultiplier;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambient;
        RenderSettings.ambientEquatorColor = Color.Lerp(ambient * 0.7f, ambient, daylight);
        RenderSettings.ambientGroundColor = ambient * 0.45f;
        RenderSettings.reflectionIntensity = Mathf.Lerp(nightReflectionIntensity, dayReflectionIntensity, daylight);

        Material skybox = RenderSettings.skybox;
        if (skybox != null && skybox.HasProperty("_Exposure"))
            skybox.SetFloat("_Exposure", Mathf.Lerp(0.18f, 1f, daylight) * weatherExposureMultiplier);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureCycleExists()
    {
        if (FindFirstObjectByType<DayNightCycle>() != null)
            return;

        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        Light directional = null;
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type != LightType.Directional)
                continue;
            if (directional == null || lights[i].intensity > directional.intensity)
                directional = lights[i];
        }

        if (directional == null)
        {
            GameObject sunObject = new("NatureParadise_Sun");
            directional = sunObject.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.shadows = LightShadows.Soft;
        }

        directional.gameObject.AddComponent<DayNightCycle>();
        Debug.Log("[TIME] Day/night cycle aktif dan matahari mengikuti jam 24 jam.");
    }
}
