using UnityEngine;

/// <summary>Musim tanam dalam bentuk flags agar satu crop dapat mendukung beberapa musim.</summary>
[System.Flags]
public enum CropSeason
{
    None = 0,
    Spring = 1 << 0,
    Summer = 1 << 1,
    Autumn = 1 << 2,
    Winter = 1 << 3,
    All = Spring | Summer | Autumn | Winter
}

public enum OutOfSeasonCropBehavior
{
    PauseGrowth,
    Wither,
    Die
}

[System.Serializable]
/// <summary>Satu slot visual crop yang dapat memakai prefab atau dummy bawaan dengan ukuran khusus.</summary>
public sealed class CropVisualStage
{
    [Tooltip("Nama state yang tampil di Inspector dan clue gameplay.")]
    public string stageName = "Seed";
    [Tooltip("Hari pertumbuhan saat state ini mulai aktif.")]
    [Min(0)] public int startsOnDay;
    [Tooltip("Model opsional untuk state ini. Kosong berarti memakai dummy crop bawaan.")]
    public GameObject modelPrefab;
    [Tooltip("Ukuran visual relatif terhadap ukuran satu tile.")]
    public Vector3 scale = Vector3.one * 0.1f;
    [Tooltip("Koreksi posisi lokal untuk model yang pivot-nya tidak berada di dasar.")]
    public Vector3 localOffset;
    [Tooltip("Koreksi rotasi lokal model dalam derajat.")]
    public Vector3 localEulerAngles;
}

[CreateAssetMenu(menuName = "Game/CropData")]
/// <summary>
/// Definisi reusable satu jenis tanaman: item seed/hasil, kebutuhan tanah,
/// durasi pertumbuhan, visual stage, bobot kualitas, dan yield.
/// </summary>
public class CropDataSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("ID stabil untuk membedakan jenis crop pada save. Jangan diubah setelah production save dibuat.")]
    public string cropId = "crop.cabbage";
    [Tooltip("Item seed yang dikonsumsi ketika crop ditanam.")]
    public ItemSO seedItem;
    [Tooltip("Item hasil yang dimasukkan ke inventory saat panen.")]
    public ItemSO produceItem;

    [Header("Season")]
    public CropSeason allowedSeasons = CropSeason.All;
    public OutOfSeasonCropBehavior outOfSeasonBehavior = OutOfSeasonCropBehavior.PauseGrowth;

    [Header("Growing Requirements")]
    [Tooltip("Batas bawah moisture ideal untuk pertumbuhan penuh.")]
    [Range(0, 100)] public int idealMoistureMin = 35;
    [Tooltip("Batas atas moisture ideal; terlalu basah dapat menurunkan growth factor.")]
    [Range(0, 100)] public int idealMoistureMax = 80;
    [Tooltip("Fertility minimum untuk pertumbuhan penuh.")]
    [Range(0, 100)] public int minimumFertility = 35;
    [Tooltip("Fertility yang dikonsumsi crop setiap hari.")]
    [Min(0)] public int dailyFertilityUse = 3;
    [Tooltip("Penurunan soil quality setelah crop dipanen.")]
    [Min(0)] public int soilDepletionOnHarvest = 4;
    [Tooltip("Jumlah dasar hasil sebelum bonus grade diterapkan.")]
    [Min(1)] public int baseYield = 1;

    [Header("Weather Resistance")]
    [Tooltip("Pengali risiko tercabut angin. Contoh: Carrot 0.6, crop biasa 1.0, Corn 1.5.")]
    [Range(0.1f, 3f)] public float windVulnerability = 1f;

    [Header("Water & Wither")]
    [Tooltip("Jumlah hari kering berturut-turut sebelum tanaman menjadi layu.")]
    [Min(1)] public int dryDaysBeforeWither = 3;
    [Tooltip("Jumlah hari disiram berturut-turut untuk memulihkan tanaman layu.")]
    [Min(1)] public int wateredDaysToRecover = 2;
    [Range(0, 100)] public int healthLossWhenDry = 10;

    [Header("Growth Timing")]
    [Tooltip("Jumlah Growth Days dari Seed sampai Harvest Ready. Hari tanpa air tidak dihitung.")]
    [Min(1)] public int daysUntilFirstHarvest = 5;

    [Header("Harvest Pattern")]
    [Tooltip("Aktifkan jika tanaman tetap hidup dan menghasilkan panen berikutnya.")]
    public bool regrowsAfterHarvest;
    [Tooltip("Jumlah Growth Days dari satu panen ke panen berikutnya. Hanya dipakai jika Regrows After Harvest aktif.")]
    [Min(1)] public int regrowDays = 3;
    [Tooltip("Stage visual setelah crop regrow dipanen. -1 memakai stage sebelum Harvest Ready.")]
    public int regrowStage = -1;

    [Header("Harvest Quality")]
    [Tooltip("Bobot relatif bintang 1-5 sebelum bonus tier booster; bukan persentase final.")]
    public float[] qualityWeights = { 50f, 30f, 14f, 5f, 1f };
    [Tooltip("Tier booster minimum bintang 1-5. Default Premium dapat mengakses peluang bintang 5.")]
    public int[] minimumBoosterForStars = { 0, 1, 2, 3, 3 };
    [Min(0)] public int harvestGraceDays = 0;

    [Header("Legacy Growth Booster (unused)")]
    [Tooltip("Batas pengurangan durasi pertumbuhan dari booster.")]
    [Range(0, 80)] public int maximumBoosterPercent = 50;

    [Header("Quality Weights")]
    [Range(0f, 1f)] public float soilWeight = 0.35f;
    [Range(0f, 1f)] public float moistureWeight = 0.25f;
    [Range(0f, 1f)] public float fertilityWeight = 0.25f;
    [Range(0f, 1f)] public float healthWeight = 0.15f;

    [Header("Modular Visual Stages")]
    [Tooltip("Satu slot per state. Model kosong memakai dummy bawaan dengan scale dari slot ini.")]
    public CropVisualStage[] visualStages =
    {
        new() { stageName = "Seed", startsOnDay = 0, scale = new Vector3(0.07f, 0.045f, 0.07f) },
        new() { stageName = "Sprout", startsOnDay = 1, scale = new Vector3(0.10f, 0.14f, 0.10f) },
        new() { stageName = "Small Plant", startsOnDay = 2, scale = new Vector3(0.15f, 0.22f, 0.15f) },
        new() { stageName = "Growing", startsOnDay = 3, scale = new Vector3(0.22f, 0.32f, 0.22f) },
        new() { stageName = "Mature", startsOnDay = 4, scale = new Vector3(0.29f, 0.42f, 0.29f) },
        new() { stageName = "Harvest Ready", startsOnDay = 5, scale = new Vector3(0.36f, 0.50f, 0.36f) }
    };

    public int StageCount => Mathf.Max(1, visualStages?.Length ?? 0);

    public float TotalGrowthDays => Mathf.Max(1, daysUntilFirstHarvest);

    /// <summary>Mengubah akumulasi hari tumbuh menjadi index stage visual.</summary>
    public int GetStageForGrowth(float growthDays)
    {
        if (StageCount <= 1)
            return 0;

        float visualScheduleDays = GetVisualScheduleDays();
        float normalizedGrowth = Mathf.Clamp01(growthDays / TotalGrowthDays);
        float visualGrowthDay = normalizedGrowth * visualScheduleDays;
        int result = 0;
        for (int i = 1; i < visualStages.Length; i++)
        {
            if (visualStages[i] == null || visualGrowthDay < visualStages[i].startsOnDay)
                break;
            result = i;
        }
        return result;
    }

    /// <summary>
    /// Stage memakai timeline relatif sehingga mengganti total hari panen tidak perlu
    /// mengatur ulang seluruh slot model dan ukuran visual.
    /// </summary>
    float GetVisualScheduleDays()
    {
        int lastDay = 1;
        if (visualStages == null)
            return lastDay;

        for (int i = 0; i < visualStages.Length; i++)
            if (visualStages[i] != null)
                lastDay = Mathf.Max(lastDay, visualStages[i].startsOnDay);
        return lastDay;
    }

    /// <summary>Mengambil skala visual stage dengan index yang sudah diamankan.</summary>
    public Vector3 GetStageScale(int stage)
    {
        CropVisualStage visual = GetVisualStage(stage);
        if (visual == null)
            return Vector3.one;
        return visual.scale;
    }

    public GameObject GetStagePrefab(int stage)
    {
        return GetVisualStage(stage)?.modelPrefab;
    }

    public string GetStageName(int stage)
    {
        CropVisualStage visual = GetVisualStage(stage);
        if (visual != null && !string.IsNullOrWhiteSpace(visual.stageName))
            return visual.stageName;
        return stage >= StageCount - 1 ? "Harvest Ready" : $"Stage {stage}";
    }

    public Vector3 GetStageOffset(int stage) => GetVisualStage(stage)?.localOffset ?? Vector3.zero;

    public Quaternion GetStageRotation(int stage)
    {
        CropVisualStage visual = GetVisualStage(stage);
        return visual != null ? Quaternion.Euler(visual.localEulerAngles) : Quaternion.identity;
    }

    CropVisualStage GetVisualStage(int stage)
    {
        if (visualStages == null || visualStages.Length == 0)
            return null;
        return visualStages[Mathf.Clamp(stage, 0, visualStages.Length - 1)];
    }

    void OnValidate()
    {
        daysUntilFirstHarvest = Mathf.Max(1, daysUntilFirstHarvest);
        regrowDays = Mathf.Max(1, regrowDays);
        windVulnerability = Mathf.Clamp(windVulnerability, 0.1f, 3f);

        if (visualStages == null)
            return;

        int previousDay = -1;
        for (int i = 0; i < visualStages.Length; i++)
        {
            CropVisualStage visual = visualStages[i];
            if (visual == null)
                continue;
            visual.startsOnDay = Mathf.Max(previousDay + 1, visual.startsOnDay);
            visual.scale = new Vector3(
                Mathf.Max(0.001f, visual.scale.x),
                Mathf.Max(0.001f, visual.scale.y),
                Mathf.Max(0.001f, visual.scale.z)
            );
            previousDay = visual.startsOnDay;
        }
    }

    public bool SupportsSeason(CropSeason season) => (allowedSeasons & season) != 0;

    public float GetBoosterGrowthMultiplier(int boosterPercent)
    {
        return 1f; // Booster hanya kualitas, tidak pernah mempercepat pertumbuhan.
    }

    public int GetRegrowStage()
    {
        int fallback = Mathf.Max(0, StageCount - 2);
        return Mathf.Clamp(regrowStage < 0 ? fallback : regrowStage, 0, StageCount - 1);
    }

    /// <summary>Musim diturunkan dari hari global dengan kalender 28 hari per musim.</summary>
    public static CropSeason GetSeasonForDay(int day, int daysPerSeason = 28)
    {
        int seasonIndex = Mathf.FloorToInt((Mathf.Max(1, day) - 1) / (float)Mathf.Max(1, daysPerSeason)) % 4;
        return seasonIndex switch
        {
            0 => CropSeason.Spring,
            1 => CropSeason.Summer,
            2 => CropSeason.Autumn,
            _ => CropSeason.Winter
        };
    }
}
