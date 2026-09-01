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
    }

    void OnDisable()
    {
        if (status != null)
            status.Fainted -= HandleFainted;
    }

    void Update()
    {
        if (busy)
            return;

        if (Input.GetKeyDown(sleepKey))
            SleepAndSave();
        if (Input.GetKeyDown(damageKey))
            status.TakeDamage(debugDamage);
    }

    /// <summary>Memulai rangkaian tidur jika lifecycle tidak sedang menjalankan transisi lain.</summary>
    public void SleepAndSave()
    {
        if (!busy && !status.IsFainted)
            StartCoroutine(SleepRoutine());
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

    void HandleFainted()
    {
        if (!busy)
            StartCoroutine(FaintRoutine());
    }

    IEnumerator SleepRoutine()
    {
        busy = true;
        SetGameplayEnabled(false);
        SaveLoadFeedback.Instance?.ShowMessage("Tidur...");
        yield return null;

        AdvanceToNextDay();
        TeleportTo(homeSpawnId);
        status.RestoreAfterSleep();
        SaveManager.Instance?.SaveGame();

        SetGameplayEnabled(true);
        busy = false;
        SaveLoadFeedback.Instance?.ShowMessage("Bangun - hari baru");
        Debug.Log("[PLAYER] Bangun setelah tidur. Game tersimpan.");
    }

    IEnumerator FaintRoutine()
    {
        busy = true;
        SetGameplayEnabled(false);
        Debug.Log("[PLAYER] Pingsan. Player akan dibawa ke klinik.");

        if (faintDelay > 0f)
            yield return new WaitForSecondsRealtime(faintDelay);

        if (advanceDayWhenFainted)
            AdvanceToNextDay();

        if (faintGoldPenalty > 0 && ScoreManager.Instance != null)
            ScoreManager.Instance.points = Mathf.Max(0, ScoreManager.Instance.points - faintGoldPenalty);

        TeleportTo(hospitalSpawnId);
        status.RestoreAfterFaint(faintHealthRecovery, faintStaminaRecovery);

        if (saveAfterFaint)
            SaveManager.Instance?.SaveGame();

        SetGameplayEnabled(true);
        busy = false;
        Debug.Log("[PLAYER] Bangun di klinik.");
    }

    void AdvanceToNextDay()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.AdvanceToNextDay(wakeHour);
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
