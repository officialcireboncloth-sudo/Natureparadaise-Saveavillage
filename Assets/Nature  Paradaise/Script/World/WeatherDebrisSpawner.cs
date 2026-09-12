using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Titik authoring modular untuk puing desa sehari setelah topan. Prefab dan titik spawn
/// dipasang manual agar level designer tetap mengontrol bentuk serta lokasinya.
/// </summary>
[DisallowMultipleComponent]
public sealed class WeatherDebrisSpawner : MonoBehaviour
{
    [SerializeField] List<GameObject> debrisPrefabs = new();
    [SerializeField] List<Transform> spawnPoints = new();
    [SerializeField, Min(1)] int maximumDebris = 8;

    readonly List<GameObject> spawnedDebris = new();
    int spawnedForDay = -1;

    void OnEnable()
    {
        WeatherSystem.CurrentWeatherChanged += HandleWeatherChanged;
        TimeManager.OnDay += Refresh;
    }

    void Start() => Refresh();

    void OnDisable()
    {
        WeatherSystem.CurrentWeatherChanged -= HandleWeatherChanged;
        TimeManager.OnDay -= Refresh;
    }

    void HandleWeatherChanged(WeatherType _) => Refresh();

    void Refresh()
    {
        WeatherSystem weather = WeatherSystem.Instance;
        int day = TimeManager.Instance != null ? TimeManager.Instance.day : -1;
        if (weather == null || !weather.IsCommunityCleanupDay)
        {
            ClearDebris();
            spawnedForDay = -1;
            return;
        }
        if (spawnedForDay == day) return;

        ClearDebris();
        spawnedForDay = day;
        if (debrisPrefabs.Count == 0 || spawnPoints.Count == 0) return;

        System.Random random = new(unchecked(weather.WorldWeatherSeed * 397 ^ day));
        int count = Mathf.Min(Mathf.Max(1, maximumDebris), spawnPoints.Count);
        List<int> available = new(spawnPoints.Count);
        for (int i = 0; i < spawnPoints.Count; i++) if (spawnPoints[i] != null) available.Add(i);

        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int selected = random.Next(available.Count);
            Transform point = spawnPoints[available[selected]];
            available.RemoveAt(selected);
            GameObject prefab = debrisPrefabs[random.Next(debrisPrefabs.Count)];
            if (prefab == null) continue;
            GameObject debris = Instantiate(prefab, point.position, point.rotation, transform);
            debris.name = $"StormDebris_Day{day}_{i + 1}";
            spawnedDebris.Add(debris);
        }
    }

    void ClearDebris()
    {
        foreach (GameObject debris in spawnedDebris)
            if (debris != null) Destroy(debris);
        spawnedDebris.Clear();
    }
}
