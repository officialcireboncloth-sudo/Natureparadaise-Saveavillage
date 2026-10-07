using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
/// <summary>
/// Interaksi TV yang menampilkan cuaca hari ini dan ramalan besok.
/// Waktu permainan dihentikan sementara selama panel forecast terbuka.
/// </summary>
public sealed class WeatherForecastTV : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField] KeyCode forecastShortcutKey = KeyCode.F;
    [SerializeField, Min(0.5f)] float interactionRadius = 2.2f;
    [SerializeField, Min(0f)] float promptHeight = 1.35f;

    [SerializeField] bool isRadio;
    public bool IsRadio => isRadio;
    PlayerController player;
    TimeManager timeManager;
    bool isOpen;
    public KeyCode ForecastShortcutKey => forecastShortcutKey;
    void Awake()
    {
        player = FindFirstObjectByType<PlayerController>();
        timeManager = TimeManager.Instance != null ? TimeManager.Instance : FindFirstObjectByType<TimeManager>();
    }

    void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
        }

        bool inRange = isOpen || PlayerInteractionTarget.Contains(player.transform, transform);
        if (!inRange)
        {
            if (isOpen) CloseTV();
            return;
        }

        if (!isOpen)
            WorldInteractionPrompt.Request(this, transform, isRadio ? "E: Radio — Ramalan cuaca" : "E: TV — Ramalan cuaca", Vector3.Distance(player.transform.position, transform.position), promptHeight);

        if (!isOpen && PlayerInteractionTarget.Press(player.transform, transform, interactKey))
            OpenTV();
    }

    /// <summary>Membuka menu TV dan mengunci movement serta waktu.</summary>
    public void OpenTV()
    {
        if (isOpen)
            return;
        if (WeatherSystem.Instance != null && WeatherSystem.BlocksTelevision(WeatherSystem.Instance.CurrentWeather))
        {
            SaveLoadFeedback.Instance?.ShowMessage(isRadio ? "Radio tidak dapat dinyalakan saat badai petir." : "TV tidak dapat dinyalakan saat badai petir.");
            return;
        }
        if (WorldInteractionPrompt.IsSuppressed || GameplayPauseMenu.BlocksGameplayInput || WeatherForecastTVUI.Instance != null)
            return;
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        isOpen = true;
        WorldInteractionPrompt.AcquireSuppression(this);
        if (timeManager == null) timeManager = TimeManager.Instance;
        timeManager?.AcquirePause(this);
        player?.AcquireMovementLock(this);
        GameplayInput.ConsumeCurrentFrame();
        WeatherForecastTVUI.Show(this);
    }

    /// <summary>Uses the current/tomorrow forecast already supplied by WeatherSystem.</summary>
    public void ShowForecast()
    {
        if (isOpen && WeatherForecastTVUI.Instance != null)
            WeatherForecastTVUI.Instance.SelectTab(0);
    }

    public void CloseTV()
    {
        if (!isOpen) return;
        isOpen = false;
        if (WeatherForecastTVUI.Instance != null && WeatherForecastTVUI.Instance.Owner == this)
            WeatherForecastTVUI.Instance.Dispose();
        WorldInteractionPrompt.ReleaseSuppression(this);
        timeManager?.ReleasePause(this);
        player?.ReleaseMovementLock(this);
        GameplayInput.ConsumeCurrentFrame();
    }
    void OnDisable() => CloseTV();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureTestingTVExists()
    {
        // Jika interior rumah tersedia, TV aslinya memang belum aktif saat world baru dimuat.
        if (Application.CanStreamedLevelBeLoaded("HouseInterior") || FindFirstObjectByType<WeatherForecastTV>() != null)
            return;

        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;
        if (PlayerSpawnPoint.TryGet("player-home", out PlayerSpawnPoint home))
        {
            position = home.transform.position - home.transform.right * 2.5f + Vector3.up * 0.9f;
            rotation = home.transform.rotation;
        }
        else
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
            position = player.transform.position - player.transform.right * 2.5f + Vector3.up * 0.9f;
        }

        GameObject tv = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tv.name = "WeatherForecastTV_DebugRuntime";
        tv.transform.SetPositionAndRotation(position, rotation);
        tv.transform.localScale = new Vector3(1.8f, 1.2f, 0.35f);
        Renderer renderer = tv.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(0.035f, 0.055f, 0.08f, 1f);
        tv.AddComponent<WeatherForecastTV>();
        Debug.Log("[WEATHER] TV forecast testing dibuat dekat PlayerHomeSpawn.");
    }
}
