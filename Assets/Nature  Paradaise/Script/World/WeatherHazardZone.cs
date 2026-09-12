using UnityEngine;

public enum WeatherHazardType
{
    BlizzardAvalanche,
    ThunderLightning
}

/// <summary>Trigger modular untuk hazard lokasi, misalnya longsoran di area gunung.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class WeatherHazardZone : MonoBehaviour
{
    [SerializeField] WeatherHazardType hazardType = WeatherHazardType.BlizzardAvalanche;
    [SerializeField, Range(0, 23)] int clinicWakeHour = 12;
    int triggeredDay = -1;

    void Reset()
    {
        Collider zone = GetComponent<Collider>();
        zone.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerLifeCycle lifeCycle = other.GetComponentInParent<PlayerLifeCycle>();
        WeatherSystem weather = WeatherSystem.Instance;
        int day = TimeManager.Instance != null ? TimeManager.Instance.day : -1;
        if (lifeCycle == null || weather == null || triggeredDay == day || !IsActive(weather.CurrentWeather)) return;

        triggeredDay = day;
        string message = hazardType == WeatherHazardType.BlizzardAvalanche
            ? "Kamu terkena longsoran salju dan dibawa ke klinik."
            : "Kamu tersambar petir dan dibawa ke klinik.";
        lifeCycle.RequestWeatherFaint(clinicWakeHour, true, message);
    }

    bool IsActive(WeatherType weather)
    {
        return hazardType switch
        {
            WeatherHazardType.BlizzardAvalanche => weather == WeatherType.Blizzard,
            WeatherHazardType.ThunderLightning => weather == WeatherType.Thunderstorm,
            _ => false
        };
    }
}
