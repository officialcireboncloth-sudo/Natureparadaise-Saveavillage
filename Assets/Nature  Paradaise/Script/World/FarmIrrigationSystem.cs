using UnityEngine;

/// <summary>Penyiraman pagi sesudah semua event cuaca selesai; tidak memakai stamina.</summary>
public sealed class FarmIrrigationSystem : MonoBehaviour
{
    int wateredDay = -1;
    void LateUpdate()
    {
        int day = TimeManager.Instance != null ? TimeManager.Instance.day : 1;
        if (wateredDay == day) return;
        wateredDay = day;
        foreach (FieldArea field in FieldArea.ActiveAreas) FarmPlacement.WaterField(field);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        FarmEquipmentCatalog.Load();
        if (FindFirstObjectByType<FarmIrrigationSystem>() == null)
            new GameObject("FarmIrrigation_Runtime").AddComponent<FarmIrrigationSystem>();
    }
}
