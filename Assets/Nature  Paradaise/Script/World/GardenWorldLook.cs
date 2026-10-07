using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Volume))]
public sealed class GardenWorldLook : MonoBehaviour
{
    public GardenLightingProfile profile;
    Volume volume;
    Bloom bloom;
    ColorAdjustments grade;
    float baseBloom, baseThreshold, baseExposure;
    static readonly int NoonGrassPeakId = Shader.PropertyToID("_NPNoonGrassPeak");
    void OnDisable() => Shader.SetGlobalFloat(NoonGrassPeakId, 0);
    void OnEnable()
    {
        volume = GetComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 5;
        if (profile != null) volume.sharedProfile = profile.postProcessing;
        if (Application.isPlaying && profile != null)
        {
            // Volume.profile clones the components; time-of-day adjustments never write the authored asset.
            var runtimeProfile = volume.profile;
            if (runtimeProfile.TryGet(out bloom))
            { baseBloom = bloom.intensity.value; baseThreshold = bloom.threshold.value; }
            if (runtimeProfile.TryGet(out grade)) baseExposure = grade.postExposure.value;
        }
    }
    void LateUpdate()
    {
        if (volume != null) volume.weight = WeatherSystem.IsPlayerOutdoors() ? 1 : 0;
        if (!Application.isPlaying || profile == null || !profile.useTimeOfDayPalette)
        { Shader.SetGlobalFloat(NoonGrassPeakId, 0); return; }
        float peak = TimeManager.Instance != null ? DayNightCycle.GetNoonPeakWeight(TimeManager.Instance.CurrentTimeHours) : 0;
        // Clouds soften the noon glow instead of making rain look like full summer sun.
        if (WeatherSystem.Instance != null && WeatherSystem.Instance.CurrentWeather != WeatherType.Sunny)
            peak *= WeatherSystem.Instance.CurrentWeather == WeatherType.PartlyCloudy ? .65f : .25f;
        Shader.SetGlobalFloat(NoonGrassPeakId, WeatherSystem.IsPlayerOutdoors() ? peak : 0);
        if (bloom != null)
        {
            bloom.intensity.Override(Mathf.Lerp(baseBloom, profile.noonBloomIntensity, peak));
            bloom.threshold.Override(Mathf.Lerp(baseThreshold, profile.noonBloomThreshold, peak));
        }
        if (grade != null) grade.postExposure.Override(baseExposure + profile.noonExposureBoost * peak);
    }
}
