using UnityEngine;

[CreateAssetMenu(menuName = "Game/Field/Soil Profile")]
/// <summary>
/// Konfigurasi reusable untuk nilai awal tanah dan perubahan moisture/fertility
/// akibat waktu, penyiraman, serta pemupukan.
/// </summary>
public sealed class SoilProfileSO : ScriptableObject
{
    [Header("Initial Values")]
    [Tooltip("Kualitas permanen awal tanah; pemakaian dan panen dapat menurunkannya.")]
    [Range(0, 100)] public int initialSoilQuality = 75;
    [Tooltip("Nutrisi awal yang dikonsumsi tanaman selama pertumbuhan.")]
    [Range(0, 100)] public int initialFertility = 60;
    [Tooltip("Kelembapan awal tile baru.")]
    [Range(0, 100)] public int initialMoisture = 35;

    [Header("Soil Durability")]
    [Range(1, 80)] public int maximumDurability = 80;
    [Min(1)] public int durabilityLossPerCropDay = 1;
    [Min(1)] public int restDaysPerRecovery = 5;
    [Range(1, 80)] public int durabilityRecoveredPerRestCycle = 20;

    [Header("Time Simulation")]
    [Tooltip("Moisture yang hilang setiap pergantian jam game pada tile aktif.")]
    [Min(0)] public int hourlyEvaporation = 2;
    [Tooltip("Fertility alami yang kembali setiap pergantian hari.")]
    [Min(0)] public int dailyFertilityRecovery = 0;

    [Header("Default Actions")]
    [Tooltip("Moisture yang ditambahkan satu kali watering.")]
    [Min(1)] public int waterAmount = 35;
    [Tooltip("Fertility yang ditambahkan satu kali fertilizing.")]
    [Min(1)] public int fertilizerAmount = 25;
    [Tooltip("Soil condition yang dipulihkan satu kali fertilizing.")]
    [Min(0)] public int fertilizerSoilQualityAmount = 8;

    public byte InitialSoilQuality => ClampToByte(initialSoilQuality);
    public byte InitialFertility => ClampToByte(initialFertility);
    public byte InitialMoisture => ClampToByte(initialMoisture);
    public byte MaximumDurability => (byte)Mathf.Clamp(maximumDurability, 1, 80);

    public static byte ClampToByte(int value)
    {
        return (byte)Mathf.Clamp(value, 0, 100);
    }
}
