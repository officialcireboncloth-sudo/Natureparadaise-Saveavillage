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
    [SerializeField, Min(0f)] float bedYawnAnimationTime = 1.65f;
    [SerializeField, Min(0f)] float bedWakeAnimationTime = 2f;

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
    [SerializeField, Min(0f)] float knockOutAnimationTime = 2.4f;
    [SerializeField, Min(0f)] float wakeUpAnimationTime = 2.8f;

    [Header("Debug / Keyboard Testing")]
    [SerializeField] KeyCode sleepKey = KeyCode.L;
    [SerializeField] KeyCode damageKey = KeyCode.K;
    [SerializeField, Min(1f)] float debugDamage = 25f;

    bool busy;
    bool bedPoseActive;
    bool bedControllerWasEnabled;
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
        RestoreBedController();
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
            if (GameplayInput.GetKeyDown(sleepKey))
                SleepAndSave();
            if (GameplayInput.GetKeyDown(damageKey))
                status.TakeDamage(debugDamage);
        }
    }

    /// <summary>Memulai rangkaian tidur jika lifecycle tidak sedang menjalankan transisi lain.</summary>
    public void SleepAndSave()
    {
        SleepAndSave(null, null);
    }

    public void SleepAndSave(Transform sleepPose, Transform wakeStandPoint)
    {
        if (!busy && !status.IsFainted)
            StartCoroutine(SleepRoutine(false, sleepPose, wakeStandPoint));
    }

    public void SleepWithoutSave(Transform sleepPose = null, Transform wakeStandPoint = null)
    {
        if (!busy && !status.IsFainted)
            StartCoroutine(SleepRoutine(false, sleepPose, wakeStandPoint, false));
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

    IEnumerator SleepRoutine(bool forcedInPlace, Transform sleepPose = null, Transform wakeStandPoint = null, bool saveAfterSleep = true)
    {
        Vector3 standingPosition = transform.position;
        Quaternion standingRotation = transform.rotation;
        busy = true;
        status.AcquireActivity(this, PlayerMovementState.Sleeping);
        SetGameplayEnabled(false);
        if (forcedInPlace)
        {
            movement?.PlayKnockOutAnimation();
            if (knockOutAnimationTime > 0f)
                yield return new WaitForSecondsRealtime(knockOutAnimationTime);
        }
        else
        {
            movement?.PlayYawnAnimation();
            if (bedYawnAnimationTime > 0f)
                yield return new WaitForSecondsRealtime(bedYawnAnimationTime);
        }
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
        if (!forcedInPlace && sleepPose != null)
        {
            bedPoseActive = true;
            bedControllerWasEnabled = characterController != null && characterController.enabled;
            if (characterController != null) characterController.enabled = false;
            PlaceAt(sleepPose.position, sleepPose.rotation);
        }

        sleepBlackout = false;
        if (forcedInPlace)
        {
            movement?.PlayWakeUpAnimation();
            if (wakeUpAnimationTime > 0f)
                yield return new WaitForSecondsRealtime(wakeUpAnimationTime);
        }
        else
        {
            movement?.PlayWakeUpBedAnimation();
            float wakeDuration = Mathf.Max(bedWakeAnimationTime, movement != null ? movement.BedWakeAnimationDuration : 0f);
            if (wakeDuration > 0f)
                yield return new WaitForSecondsRealtime(wakeDuration + .15f);
            movement?.FinishBedWakeAnimation();
            if (wakeStandPoint != null)
            {
                var house = wakeStandPoint.GetComponentInParent<HouseInteriorController>();
                if(TryResolveStandingPoint(wakeStandPoint.position, characterController, house != null ? house.transform : null, out Vector3 safePosition) ||
                   TryResolveStandingPoint(standingPosition, characterController, house != null ? house.transform : null, out safePosition))
                    PlaceAt(safePosition, wakeStandPoint.rotation);
                else PlaceAt(standingPosition, standingRotation);
            }
            else if (bedPoseActive)
                PlaceAt(standingPosition, standingRotation);
            RestoreBedController();
        }
        // Save the standing position, never the temporary pose on the mattress.
        if (saveAfterSleep) SaveManager.Instance?.SaveGame();
        SetGameplayEnabled(true);
        status.ReleaseActivity(this);
        busy = false;
        SaveLoadFeedback.Instance?.ShowMessage(forcedInPlace
            ? "Bangun pukul 06:00 di tempat kamu tertidur"
            : "Bangun - hari baru");
        Debug.Log(saveAfterSleep ? "[PLAYER] Bangun setelah tidur. Game tersimpan." : "[PLAYER] Bangun setelah tidur tanpa save.");
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
        movement?.PlayKnockOutAnimation();
        Debug.Log("[PLAYER] Pingsan. Player akan dibawa ke klinik.");

        float knockOutWait = Mathf.Max(faintDelay, knockOutAnimationTime);
        if (knockOutWait > 0f)
            yield return new WaitForSecondsRealtime(knockOutWait);

        if (advanceDayWhenFainted)
            AdvanceToNextDay(recoveryHour);

        if (faintGoldPenalty > 0 && ScoreManager.Instance != null)
            ScoreManager.Instance.points = Mathf.Max(0, ScoreManager.Instance.points - faintGoldPenalty);

        TeleportTo(recoverAtHospital ? hospitalSpawnId : homeSpawnId);
        status.RestoreAfterFaint(faintHealthRecovery, faintStaminaRecovery);

        if (saveAfterFaint)
            SaveManager.Instance?.SaveGame();

        movement?.PlayWakeUpAnimation();
        if (wakeUpAnimationTime > 0f)
            yield return new WaitForSecondsRealtime(wakeUpAnimationTime);
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

        PlaceAt(position, rotation);
    }

    void PlaceAt(Vector3 position, Quaternion rotation)
    {
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

    void RestoreBedController()
    {
        if (!bedPoseActive) return;
        if (characterController != null) characterController.enabled = bedControllerWasEnabled;
        bedPoseActive = false;
    }

    /// <summary>Ground the capsule on the house foundation and keep it clear of beds, furniture and walls.</summary>
    public static bool TryResolveStandingPoint(Vector3 preferred, CharacterController controller, Transform houseRoot, out Vector3 result)
    {
        result = preferred;
        if(controller == null) return false;
        float scale = Mathf.Abs(controller.transform.lossyScale.y);
        float radius = controller.radius * Mathf.Max(Mathf.Abs(controller.transform.lossyScale.x), Mathf.Abs(controller.transform.lossyScale.z));
        float height = Mathf.Max(radius * 2, controller.height * scale);
        Vector3 center = Vector3.Scale(controller.center, controller.transform.lossyScale);
        for(int ring=0; ring<=3; ring++)
        for(int direction=0; direction<(ring==0?1:8); direction++)
        {
            float angle=direction*Mathf.PI*.25f;
            Vector3 candidate=preferred+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*ring*1.1f;
            foreach(var hit in Physics.RaycastAll(candidate+Vector3.up*4,Vector3.down,12,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.normal.y<.8f || hit.collider.GetComponentInParent<PlayerController>()!=null)continue;
                if(houseRoot!=null && (!hit.collider.transform.IsChildOf(houseRoot) ||
                    (hit.collider.name!="ContinuousFloorCollider" && hit.collider.name!="ContinuousFloorCollider_Editable")))continue;
                candidate.y=hit.point.y+height*.5f-center.y+controller.skinWidth+.04f;
                Vector3 capsuleCenter=candidate+center;
                bool blocked=false;
                foreach(var obstacle in Physics.OverlapCapsule(capsuleCenter+Vector3.up*(height*.5f-radius),capsuleCenter-Vector3.up*(height*.5f-radius),radius,~0,QueryTriggerInteraction.Ignore))
                    if(obstacle.GetComponentInParent<PlayerController>()==null){blocked=true;break;}
                if(blocked)continue;
                result=candidate;return true;
            }
        }
        return false;
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
