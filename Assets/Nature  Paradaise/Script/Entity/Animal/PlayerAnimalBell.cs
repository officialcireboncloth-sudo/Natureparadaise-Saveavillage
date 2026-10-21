using UnityEngine;

/// <summary>
/// Layanan bell pada Player. Gameplay memakai satu saklar fisik per AnimalHome;
/// daftar/pengaturan hewan akan dipindahkan ke pause menu pada progres berikutnya.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerAnimalBell : MonoBehaviour
{
    [SerializeField, Min(0.1f)] float cooldown = 1f;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip bellClip;

    float nextUseTime;
    static AudioClip fallbackBell;

    public bool IsReady => Time.unscaledTime >= nextUseTime;

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    public void ReleaseAnimals() => ReleaseAnimals(null);
    public void RecallAnimals() => RecallAnimals(null);
    public void ReleaseAnimals(AnimalHome home) => SetAnimalsOutside(true, home);
    public void RecallAnimals(AnimalHome home) => SetAnimalsOutside(false, home);
    public void ToggleAnimals(AnimalHome home)
    {
        if (home == null || !IsReady) return;
        SetAnimalsOutside(!home.AnimalsOutside, home);
    }

    void SetAnimalsOutside(bool outside, AnimalHome selectedHome)
    {
        if (!IsReady) return;

        foreach (AnimalRoutine routine in AnimalRoutine.Active)
            routine?.TryRepairHomeAssignment(true);

        bool IsTarget(AnimalRoutine routine) => routine != null && routine.Home != null &&
                                                (selectedHome == null || routine.Home == selectedHome);
        int bornTargetCount = 0;
        foreach (AnimalRoutine routine in AnimalRoutine.Active)
            if (IsTarget(routine) && routine.Animal != null && routine.Animal.HasBeenBorn)
                bornTargetCount++;
        if (bornTargetCount == 0)
        {
            SaveLoadFeedback.Instance?.ShowMessage(
                $"Animal Bell: belum ada hewan yang sudah lahir di {selectedHome?.Label ?? "kandang"}.");
            return;
        }
        if (outside)
        {
            foreach (AnimalRoutine routine in AnimalRoutine.Active)
            {
                if (!IsTarget(routine) || routine.Animal == null || !routine.Animal.HasBeenBorn) continue;
                if (routine.CanReleaseNow()) continue;
                string reason = routine.ReleaseBlockReason();
                SaveLoadFeedback.Instance?.ShowMessage(
                    $"{selectedHome?.Label ?? "Kandang"}: semua hewan tetap di dalam — {reason}.");
                return;
            }
        }

        nextUseTime = Time.unscaledTime + cooldown;
        selectedHome?.BellStation?.PlayRing();
        GameAudio.PlayOneShot(audioSource, bellClip != null ? bellClip : GetFallbackBell(), GameAudioBus.Main);
        int changed = 0;
        int targetCount = 0;
        int alreadyInState = 0;
        int releaseIndex = 0;
        foreach (AnimalRoutine routine in AnimalRoutine.Active)
        {
            if (!IsTarget(routine)) continue;
            targetCount++;
            if (outside)
            {
                int slotIndex = releaseIndex++;
                if (!routine.IsHoused && !routine.Returning) { alreadyInState++; continue; }
                Vector3 position = selectedHome != null
                    ? selectedHome.OutdoorReleasePosition(slotIndex)
                    : routine.Home.OutdoorReleasePosition(slotIndex);
                if (routine.ReleaseAt(position)) changed++;
            }
            else if (!routine.IsHoused)
            {
                routine.Recall();
                changed++;
            }
            else alreadyInState++;
        }

        // Status saklar baru disimpan setelah perintah berhasil. Sebelumnya status dapat
        // berubah ke "luar" walaupun Release gagal, sehingga klik berikutnya justru mencoba masuk.
        if (selectedHome != null)
        {
            bool commandAccepted = targetCount > 0 && changed + alreadyInState >= targetCount;
            if (commandAccepted) selectedHome.SetAnimalsOutsideState(outside);
            else
            {
                bool anyOutside = false;
                foreach (AnimalRoutine resident in selectedHome.Residents)
                    if (resident != null && resident.Animal != null && resident.Animal.HasBeenBorn && !resident.IsHoused)
                    { anyOutside = true; break; }
                selectedHome.SetAnimalsOutsideState(anyOutside);
            }
        }

        string homeName = selectedHome != null ? selectedHome.Label : "semua kandang";
        string message;
        if (targetCount == 0)
            message = $"Animal Bell: belum ada hewan di {homeName}.";
        else if (outside)
            message = changed > 0
                ? $"Animal Bell: {changed}/{targetCount} hewan dari {homeName} dikeluarkan" +
                  (alreadyInState > 0 ? $" ({alreadyInState} sudah di luar)." : ".")
                : alreadyInState == targetCount
                    ? $"Semua {targetCount} hewan di {homeName} sudah berada di luar."
                    : $"Hewan tidak dapat keluar dari {homeName}. Cek pesan alasan dan status penghuni.";
        else
            message = changed > 0
                ? $"Animal Bell: {changed} hewan dipanggil masuk ke {homeName}."
                : $"Semua hewan di {homeName} sudah berada di dalam.";
        SaveLoadFeedback.Instance?.ShowMessage(message);
    }

    static AudioClip GetFallbackBell()
    {
        if (fallbackBell != null) return fallbackBell;
        const int sampleRate = 44100;
        const float duration = 0.7f;
        int count = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)sampleRate;
            float decay = Mathf.Exp(-5f * t);
            samples[i] = (Mathf.Sin(2f * Mathf.PI * 880f * t) +
                          Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.45f) * decay * 0.2f;
        }
        fallbackBell = AudioClip.Create("AnimalBell_Default", count, 1, sampleRate, false);
        fallbackBell.SetData(samples, 0);
        return fallbackBell;
    }
}

/// <summary>Satu saklar world-space per Barn/Coop; state menentukan aksi berikutnya.</summary>
[DisallowMultipleComponent]
public sealed class AnimalBellStation : MonoBehaviour
{
    [SerializeField, Min(.5f)] float interactionRadius = 1.05f;
    AnimalHome home;
    Inventory player;
    PlayerAnimalBell bell;
    AnimalBellVisual visual;
    AnimalBellVisual fallbackVisual;
    Transform boundExterior;
    BarnExteriorAuthoring boundAuthoring;
    bool visualResolved;
    System.Action openMenuAction;
    public AnimalHome Home => home;
    public PlayerAnimalBell Bell => bell;
    public Inventory PlayerInventory => player;
    public AnimalBellVisual Visual => visual;
    public Transform InteractionPoint => visual!=null?visual.Approach:null;
    public float InteractionRadius
    {
        get
        {
            return boundAuthoring!=null&&boundAuthoring.Bell==visual&&visual.Approach!=null
                ?Mathf.Min(interactionRadius,HorizontalDistance(home.Entry,visual.Approach.position)*.42f):interactionRadius;
        }
    }
    /// <summary>Animates the authored bell alongside the existing audio/animal command.</summary>
    public void PlayRing(){if(visual!=null)visual.Ring();}
    public void OpenMenu()
    {
        ResolvePlayer();
        var movement=player!=null?player.GetComponent<PlayerController>():null;
        if(home!=null&&home.Available&&player!=null&&bell!=null&&
           (movement==null||!movement.IsMovementLocked)&&IsPlayerInRange(player.transform))AnimalBellUI.Show(this);
    }
    void OnDisable(){if(AnimalBellUI.Instance!=null&&AnimalBellUI.Instance.Station==this)AnimalBellUI.Instance.Dispose();}

    public static AnimalBellStation Create(AnimalHome owner)
    {
        if (owner == null) return null;
        Transform existing = owner.transform.Find("AnimalBellStation_Runtime");
        AnimalBellStation station = existing != null ? existing.GetComponent<AnimalBellStation>() : null;
        if (station == null)
        {
            GameObject root = new("AnimalBellStation_Runtime");
            root.transform.SetParent(owner.transform, true);
            station = root.AddComponent<AnimalBellStation>();
        }
        station.Configure(owner);
        return station;
    }

    void Configure(AnimalHome owner)
    {
        home = owner;
        RefreshVisual();
    }
    void RefreshVisual()
    {
        Transform exterior=home.site!=null?home.site.CompletedVisualRoot:null;
        if(visualResolved&&boundExterior==exterior&&visual!=null)return;
        boundExterior=exterior;visualResolved=true;
        var authored=exterior!=null?exterior.GetComponentInChildren<BarnExteriorAuthoring>(true):null;
        boundAuthoring=authored;
        visual=authored!=null?authored.Bell:null;
        if(visual!=null){if(fallbackVisual!=null)fallbackVisual.gameObject.SetActive(false);return;}
        // Older barns and coops use the SAME authored bell mesh on a timber post.
        // New Barn B prefabs always bind their wall-mounted model instead.
        if(fallbackVisual==null)
        {
            var prefab=Resources.Load<AnimalBellVisual>("Buildings/AnimalBell");
            if(prefab!=null)fallbackVisual=Instantiate(prefab,transform);
        }
        visual=fallbackVisual;
        if(visual==null)return;
        visual.gameObject.SetActive(true);
        Transform building=home.site!=null?home.site.BuildingAnchor:home.transform;
        transform.SetPositionAndRotation(home.Entry-building.right*5f,building.rotation);
    }

    void Update()
    {
        if (home == null || !home.Available) return;
        RefreshVisual();
        ResolvePlayer();
        if (player == null || bell == null || !IsPlayerInRange(player.transform)) return;
        var movement=player.GetComponent<PlayerController>();
        if(movement!=null&&movement.IsMovementLocked)return;
        if(SceneTransitionManager.Instance!=null&&SceneTransitionManager.Instance.IsInsideInterior)return;
        float distance=HorizontalDistance(player.transform.position,visual.Approach.position);
        if(openMenuAction==null)openMenuAction=OpenMenu;
        WorldInteractionPrompt.Request(this,visual.PromptAnchor,
            Application.isMobilePlatform?"Ketuk: Lonceng Kandang":"E: Lonceng Kandang",distance,.35f,openMenuAction);
        if (!PlayerInteractionTarget.PressPrompt(this, KeyCode.E)) return;
        OpenMenu();
    }

    public bool IsPlayerInRange(Transform playerTransform)
    {
        if(playerTransform==null||visual==null||visual.Approach==null)return false;
        Vector3 delta=playerTransform.position-visual.Approach.position;
        return Mathf.Abs(delta.y)<2f&&HorizontalDistance(playerTransform.position,visual.Approach.position)<=InteractionRadius&&
               Vector3.Dot(delta,visual.Approach.forward)>-.50f;
    }

    void ResolvePlayer()
    {
        if (player == null) player = FindFirstObjectByType<Inventory>();
        if (player == null) return;
        if (bell == null) bell = player.GetComponent<PlayerAnimalBell>();
        if (bell == null) bell = player.gameObject.AddComponent<PlayerAnimalBell>();
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
