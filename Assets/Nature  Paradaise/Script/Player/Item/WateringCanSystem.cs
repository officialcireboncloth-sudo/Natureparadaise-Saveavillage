using System;
using UnityEngine;

/// <summary>Kapasitas air Watering Can milik player. Satu tile memakai satu unit air.</summary>
[DisallowMultipleComponent]
public sealed class WateringCanSystem : MonoBehaviour
{
    [SerializeField, Min(1)] int maximumWater = 100;
    [SerializeField, Min(0)] int currentWater = 100;
    [Header("World Debug Label")]
    [SerializeField, Min(0f)] float labelHeight = 2.65f;
    [SerializeField] Vector2 labelSize = new(132f, 30f);

    PlayerToolHotbar hotbar;
    GUIStyle labelStyle;

    public int CurrentWater => Mathf.Clamp(currentWater, 0, MaximumWater);
    public int MaximumWater => Mathf.Max(1, maximumWater);
    public bool IsEmpty => CurrentWater <= 0;
    public event Action Changed;

    void Awake()
    {
        currentWater = Mathf.Clamp(currentWater, 0, MaximumWater);
        hotbar = GetComponent<PlayerToolHotbar>();
    }

    void OnGUI()
    {
        if (hotbar == null) hotbar = GetComponent<PlayerToolHotbar>();
        if (hotbar == null || hotbar.SelectedTool != PlayerToolType.WateringCan || Camera.main == null) return;

        Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * labelHeight);
        if (screen.z <= 0f) return;
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = Color.white;
        }
        Rect rect = new(screen.x - labelSize.x * 0.5f, Screen.height - screen.y - labelSize.y * 0.5f,
            labelSize.x, labelSize.y);
        GUI.Box(rect, $"Water {CurrentWater}/{MaximumWater}", labelStyle);
    }

    public bool TryUse(int amount = 1)
    {
        amount = Mathf.Max(1, amount);
        if (CurrentWater < amount) return false;
        currentWater -= amount;
        Changed?.Invoke();
        return true;
    }

    public bool Refill()
    {
        if (CurrentWater >= MaximumWater) return false;
        currentWater = MaximumWater;
        Changed?.Invoke();
        return true;
    }

    public void Restore(int amount)
    {
        int restored = Mathf.Clamp(amount, 0, MaximumWater);
        if (currentWater == restored) return;
        currentWater = restored;
        Changed?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInstalled()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null && player.GetComponent<WateringCanSystem>() == null)
            player.gameObject.AddComponent<WateringCanSystem>();
    }
}
