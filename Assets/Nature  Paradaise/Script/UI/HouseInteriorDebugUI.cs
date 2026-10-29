using UnityEngine;

/// <summary>Compatibility bridge for authored house components; layout controls live in the unified testing menu.</summary>
[DisallowMultipleComponent]
public sealed class HouseInteriorDebugUI : MonoBehaviour
{
    HouseInteriorController house;
    bool wasInside;
    void Awake() => house = GetComponent<HouseInteriorController>();
    void Update()
    {
        if (house == null) return;
        bool inside = house.IsPlayerInside;
        if (wasInside && !inside) house.ClearDebugLayout();
        wasInside = inside;
    }
    /// <summary>Selects a session layout through the existing house validation.</summary>
    public bool SelectLevel(int level) => house != null && house.SetDebugLayout(level);
}
