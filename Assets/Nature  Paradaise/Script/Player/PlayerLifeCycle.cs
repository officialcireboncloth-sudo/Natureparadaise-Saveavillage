using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStatusSystem))]
/// <summary>
/// Mengatur transisi tidur dan faint, termasuk movement lock, pemulihan status,
/// perpindahan spawn, pergantian hari, penalti, dan penyimpanan otomatis.
/// </summary>
public sealed class PlayerLifeCycle : MonoBehaviour
{
    [SerializeField] PlayerStatusSystem status;
    [SerializeField] CharacterController characterController;
    [SerializeField] PlayerController movement;
    [SerializeField] FarmingTool farmingTool;
    [SerializeField] SeedTool seedTool;

    [Header("Spawn IDs")]
    [SerializeField] string homeSpawnId = "player-home";
    [SerializeField] string hospitalSpawnId = "village-clinic";

    [Header("Sleep")]
    [SerializeField, Range(0, 23)] int wakeHour = 6;

    [Header("Forced Sleep")]
    [Tooltip("Player yang masih terjaga pada jam ini otomatis tidur dan bangun pada wakeHour.")]
    [SerializeField, Range(0, 23)] int forcedSleepHour = 3;

    [Header("Faint")]
    [SerializeField, Min(0f)] float faintDelay = 1.25f;
    [SerializeField, Range(0f, 1f)] float faintHealthRecovery = 0.5f;
    [SerializeField, Range(0f, 1f)] float faintStaminaRecovery = 0.35f;
    [SerializeField, Min(0)] int faintGoldPenalty;
    [SerializeField] bool advanceDayWhenFainted = true;
    [SerializeField] bool saveAfterFaint = true;

    [Header("Debug / Keyboard Testing")]
    [SerializeField] KeyCode sleepKey = KeyCode.L;
    [SerializeField] KeyCode damageKey = KeyCode.K;
    [SerializeField, Min(1f)] float debugDamage = 25f;

    bool busy;
    bool sleepBlackout;
    bool hasPendingWeatherFaint;
    bool pendingWeatherFaintAtHospital = true;
    int pendingWeatherWakeHour = 12;
    Vector3 fallbackSpawnPosition;
    Quaternion fallbackSpawnRotation;

    public bool IsBusy => busy;

    void Awake()
    {
        if (status == null)
            status = GetComponent<PlayerStatusSystem>();
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        if (movement == null)
            movement = GetComponent<PlayerController>();
        if (farmingTool == null)
            farmingTool = GetComponent<FarmingTool>();
        if (seedTool == null)
            seedTool = GetComponent<SeedTool>();

        fallbackSpawnPosition = transform.position;
        fallbackSpawnRotation = transform.rotation;
    }

    void OnEnable()
    {
        if (status != null)
            status.Fainted += HandleFainted;
        TimeManager.OnHour += HandleHourChanged;
    }

    void OnDisable()
    {
        if (status != null)
            status.Fainted -= HandleFainted;
        TimeManager.OnHour -= HandleHourChanged;
        status?.ReleaseActivity(this);
    }

    void Update()
    {
        if (busy)
            return;

        // L/K adalah shortcut development dan hanya aktif ketika Debug Clues ON.
        if (HUDManager.DebugCluesEnabled)
        {
            if (Input.GetKeyDown(sleepKey))
                SleepAndSave();
            if (Input.GetKeyDown(damageKey))
                status.TakeDamage(debugDamage);
        }
    }

    /// <summary>Memulai rangkaian tidur jika lifecycle tidak sedang menjalankan transisi lain.</summary>
    public void SleepAndSave()
    {
        if (!busy && !status.IsFainted)
            StartCoroutine(SleepRoutine(false));
    }

    /// <summary>Memindahkan player ke spawn rumah.</summary>
    public void RespawnAtHome()
    {
        TeleportTo(homeSpawnId);
    }

    /// <summary>Memindahkan player ke spawn klinik.</summary>
    public void RespawnAtHospital()
    {
        TeleportTo(hospitalSpawnId);
    }

    /// <summary>Memicu faint cuaca dengan tujuan dan jam bangun khusus.</summary>
    public void RequestWeatherFaint(int recoveryHour, bool atHospital, string message)
    {
        if (busy || status == null || status.IsFainted) return;
        hasPendingWeatherFaint = true;
        pendingWeatherWakeHour = Mathf.Clamp(recoveryHour, 0, 23);
        pendingWeatherFaintAtHospital = atHospital;
        if (!string.IsNullOrWhiteSpace(message)) SaveLoadFeedback.Instance?.ShowMessage(message);
        status.ForceFaint();
    }

    void HandleFainted()
    {
        if (!busy)
            StartCoroutine(FaintRoutine());
    }

    void HandleHourChanged()
    {
        if (!busy && TimeManager.Instance != null && TimeManager.Instance.hour == forcedSleepHour)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Sudah pukul 03:00. Player tertidur di tempat karena kelelahan.");
            StartCoroutine(SleepRoutine(true));
        }
    }

    IEnumerator SleepRoutine(bool forcedInPlace)
    {
        busy = true;
        status.AcquireActivity(this, PlayerMovementState.Sleeping);
        SetGameplayEnabled(false);
        sleepBlackout = true;
        SaveLoadFeedback.Instance?.ShowMessage("Tidur...");
        yield return new WaitForSecondsRealtime(0.45f);

        // Kalender sudah berganti di 00:00. Tidur antara 00:00-05:59 hanya melompat ke
        // jam bangun agar crop/hewan tidak menerima Daily Reset dua kali.
        if (TimeManager.Instance != null && TimeManager.Instance.hour < wakeHour)
            TimeManager.Instance.SetClockSameDay(wakeHour);
        else
            AdvanceToNextDay();
        // Knock karena masih terjaga pukul 03:00 selalu bangun di posisi yang sama.
        // Tidur normal melalui kasur tetap mengikuti aturan spawn rumah/interior.
        if (!forcedInPlace && (SceneTransitionManager.Instance == null || !SceneTransitionManager.Instance.IsInsideInterior))
            TeleportTo(homeSpawnId);
        status.RestoreAfterSleep();
        SaveManager.Instance?.SaveGame();

        SetGameplayEnabled(true);
        sleepBlackout = false;
        status.ReleaseActivity(this);
        busy = false;
        SaveLoadFeedback.Instance?.ShowMessage(forcedInPlace
            ? "Bangun pukul 06:00 di tempat kamu tertidur"
            : "Bangun - hari baru");
        Debug.Log("[PLAYER] Bangun setelah tidur. Game tersimpan.");
    }

    void OnGUI()
    {
        if (!sleepBlackout) return;
        GUI.depth = -10000;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.blackTexture);
        GUIStyle style = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 28 };
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), "Tidur...", style);
    }

    IEnumerator FaintRoutine()
    {
        bool weatherFaint = hasPendingWeatherFaint;
        int recoveryHour = weatherFaint ? pendingWeatherWakeHour : wakeHour;
        bool recoverAtHospital = !weatherFaint || pendingWeatherFaintAtHospital;
        hasPendingWeatherFaint = false;
        busy = true;
        status.AcquireActivity(this, PlayerMovementState.Faint);
        SetGameplayEnabled(false);
        Debug.Log("[PLAYER] Pingsan. Player akan dibawa ke klinik.");

        if (faintDelay > 0f)
            yield return new WaitForSecondsRealtime(faintDelay);

        if (advanceDayWhenFainted)
            AdvanceToNextDay(recoveryHour);

        if (faintGoldPenalty > 0 && ScoreManager.Instance != null)
            ScoreManager.Instance.points = Mathf.Max(0, ScoreManager.Instance.points - faintGoldPenalty);

        TeleportTo(recoverAtHospital ? hospitalSpawnId : homeSpawnId);
        status.RestoreAfterFaint(faintHealthRecovery, faintStaminaRecovery);

        if (saveAfterFaint)
            SaveManager.Instance?.SaveGame();

        SetGameplayEnabled(true);
        status.ReleaseActivity(this);
        busy = false;
        Debug.Log(recoverAtHospital ? "[PLAYER] Bangun di klinik." : "[PLAYER] Bangun di rumah.");
    }

    void AdvanceToNextDay(int targetHour = -1)
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.AdvanceToNextDay(targetHour >= 0 ? targetHour : wakeHour);
    }

    void TeleportTo(string spawnId)
    {
        Vector3 position = fallbackSpawnPosition;
        Quaternion rotation = fallbackSpawnRotation;
        if (PlayerSpawnPoint.TryGet(spawnId, out PlayerSpawnPoint point))
        {
            position = point.transform.position;
            rotation = point.transform.rotation;
        }

        bool controllerWasEnabled = characterController != null && characterController.enabled;
        if (characterController != null)
            characterController.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        if (characterController != null)
            characterController.enabled = controllerWasEnabled;

        TopDownCameraFollow cameraFollow = Camera.main != null
            ? Camera.main.GetComponent<TopDownCameraFollow>()
            : FindFirstObjectByType<TopDownCameraFollow>();
        cameraFollow?.SetTarget(transform, true);
    }

    void SetGameplayEnabled(bool enabledState)
    {
        if (movement != null)
        {
            if (enabledState)
                movement.ReleaseMovementLock(this);
            else
                movement.AcquireMovementLock(this);
        }
        if (farmingTool != null)
            farmingTool.enabled = enabledState;
        if (seedTool != null)
            seedTool.enabled = enabledState;
    }
}
