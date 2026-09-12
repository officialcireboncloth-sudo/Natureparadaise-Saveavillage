using UnityEngine;

/// <summary>Audio ambience modular; clip dapat diganti dari Inspector tanpa mengubah WeatherSystem.</summary>
[DisallowMultipleComponent]
public sealed class WeatherAudioController : MonoBehaviour
{
    [SerializeField] AudioSource precipitationSource;
    [SerializeField] AudioSource windSource;
    [SerializeField] AudioClip drizzleLoop;
    [SerializeField] AudioClip rainLoop;
    [SerializeField] AudioClip heavyRainLoop;
    [SerializeField] AudioClip snowLoop;
    [SerializeField] AudioClip windLoop;
    [SerializeField, Range(0f, 1f)] float maximumVolume = 0.75f;
    [SerializeField, Min(0.1f)] float fadeSpeed = 1.8f;

    float targetPrecipitationVolume;
    float targetWindVolume;

    void Awake()
    {
        if (precipitationSource == null) precipitationSource = CreateSource("Weather Precipitation Audio");
        if (windSource == null) windSource = CreateSource("Weather Wind Audio");
    }

    void OnEnable()
    {
        WeatherImpactFlow.AudioUpdated += ApplyWeather;
        if (WeatherImpactFlow.HasCurrent) ApplyWeather(WeatherImpactFlow.Current);
    }

    void OnDisable() => WeatherImpactFlow.AudioUpdated -= ApplyWeather;

    void Update()
    {
        Fade(precipitationSource, targetPrecipitationVolume);
        Fade(windSource, targetWindVolume);
    }

    void ApplyWeather(WeatherImpactSnapshot impact)
    {
        AudioClip precipitation = impact.Weather switch
        {
            WeatherType.Drizzle => drizzleLoop,
            WeatherType.Rain => rainLoop,
            WeatherType.HeavyRain or WeatherType.WindRainStorm or WeatherType.Cyclone or WeatherType.Thunderstorm => heavyRainLoop,
            WeatherType.Snow or WeatherType.Blizzard => snowLoop,
            _ => null
        };
        SetLoop(precipitationSource, precipitation);
        SetLoop(windSource, impact.WindIntensity > 0.01f ? windLoop : null);
        targetPrecipitationVolume = impact.PrecipitationIntensity * maximumVolume;
        targetWindVolume = impact.WindIntensity * maximumVolume;
    }

    AudioSource CreateSource(string sourceName)
    {
        GameObject child = new(sourceName, typeof(AudioSource));
        child.transform.SetParent(transform, false);
        AudioSource source = child.GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    static void SetLoop(AudioSource source, AudioClip clip)
    {
        if (source == null || source.clip == clip) return;
        source.Stop();
        source.clip = clip;
        if (clip != null) source.Play();
    }

    void Fade(AudioSource source, float target)
    {
        if (source == null) return;
        source.volume = Mathf.MoveTowards(source.volume, target, fadeSpeed * Time.unscaledDeltaTime);
        if (source.clip != null && !source.isPlaying && source.volume > 0.001f) source.Play();
    }
}
