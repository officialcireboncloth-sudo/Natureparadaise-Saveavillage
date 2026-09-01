using UnityEngine;

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

    [Header("Quality Weights")]
    [Range(0f, 1f)] public float soilWeight = 0.35f;
    [Range(0f, 1f)] public float moistureWeight = 0.25f;
    [Range(0f, 1f)] public float fertilityWeight = 0.25f;
    [Range(0f, 1f)] public float healthWeight = 0.15f;

    [Tooltip("Skala relatif (X,Y,Z) untuk tiap tahap. Elemen terakhir = matang")]
    public Vector3[] stageScales =
    {
        new Vector3(0.4f,0.4f,0.4f),
        new Vector3(0.7f,0.9f,0.7f),
        new Vector3(1.0f,1.2f,1.0f)
    };

    [Tooltip("Jumlah hari untuk naik ke tahap berikut (panjang = stageScales.Length-1)")]
    public int[] daysPerStage = { 1, 2 };

    public int StageCount => Mathf.Max(1, stageScales?.Length ?? 0);

    public float TotalGrowthDays
    {
        get
        {
            if (daysPerStage == null || daysPerStage.Length == 0)
                return 1f;

            int total = 0;
            for (int i = 0; i < daysPerStage.Length; i++)
                total += Mathf.Max(1, daysPerStage[i]);

            return Mathf.Max(1, total);
        }
    }

    /// <summary>Mengubah akumulasi hari tumbuh menjadi index stage visual.</summary>
    public int GetStageForGrowth(float growthDays)
    {
        if (StageCount <= 1)
            return 0;

        float threshold = 0f;
        int transitionCount = Mathf.Min(StageCount - 1, daysPerStage?.Length ?? 0);

        for (int i = 0; i < transitionCount; i++)
        {
            threshold += Mathf.Max(1, daysPerStage[i]);
            if (growthDays < threshold)
                return i;
        }

        return StageCount - 1;
    }

    /// <summary>Mengambil skala visual stage dengan index yang sudah diamankan.</summary>
    public Vector3 GetStageScale(int stage)
    {
        if (stageScales == null || stageScales.Length == 0)
            return Vector3.one;

        return stageScales[Mathf.Clamp(stage, 0, stageScales.Length - 1)];
    }
}
