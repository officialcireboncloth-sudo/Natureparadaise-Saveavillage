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
                if (!routine.IsHoused && !routine.Returning) { alreadyInState++; continue; }
                Vector3 position = selectedHome != null
                    ? selectedHome.OutdoorReleasePosition(releaseIndex++)
                    : routine.Home.OutdoorReleasePosition(releaseIndex++);
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
    [SerializeField, Min(1f)] float interactionRadius = 2.25f;
    AnimalHome home;
    Inventory player;
    PlayerAnimalBell bell;
    Transform toggleButton;
    Renderer buttonRenderer;

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
        Transform building = owner.site != null && owner.site.BuildingAnchor != null
            ? owner.site.BuildingAnchor : owner.transform;
        // Jauhkan bell dari radius portal masuk kandang. BarnInterior juga memberi
        // prioritas eksplisit kepada bell bila kedua radius masih bersentuhan.
        transform.position = owner.Entry - building.right * 5f;
        transform.rotation = Quaternion.Euler(0f, building.eulerAngles.y, 0f);
        if (toggleButton == null) BuildVisual();
    }

    void BuildVisual()
    {
        GameObject stand = CreatePart("BellStand", PrimitiveType.Cube,
            new Vector3(0f, 0.55f, 0f), new Vector3(1.15f, 1.1f, 0.55f), new Color(0.22f, 0.16f, 0.1f));
        stand.GetComponent<Collider>().enabled = false;
        GameObject button = CreatePart("AnimalBell_Toggle", PrimitiveType.Cylinder,
            new Vector3(0f, 1.18f, 0f), new Vector3(0.38f, 0.13f, 0.38f), Color.green);
        toggleButton = button.transform;
        buttonRenderer = button.GetComponent<Renderer>();
        toggleButton.localRotation = Quaternion.Euler(90f, 0f, 0f);
        AddLabel(toggleButton, "BELL");
    }

    GameObject CreatePart(string partName, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = partName;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                Material material = new(shader) { color = color };
                renderer.material = material;
            }
        }
        return part;
    }

    static void AddLabel(Transform button, string value)
    {
        GameObject labelObject = new($"Label_{value}");
        labelObject.transform.SetParent(button, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        labelObject.transform.localScale = Vector3.one * 0.18f;
        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = value;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 42;
        label.color = Color.white;
    }

    void Update()
    {
        if (home == null || !home.Available) return;
        ResolvePlayer();
        if (player == null || bell == null || toggleButton == null) return;

        home.SynchronizeBellStateFromResidents();
        bool currentlyOutside = home.AnimalsOutside;
        if (buttonRenderer != null && buttonRenderer.material != null)
            buttonRenderer.material.color = currentlyOutside
                ? new Color(0.95f, 0.48f, 0.08f)
                : new Color(0.12f, 0.8f, 0.3f);
        float distance = HorizontalDistance(player.transform.position, toggleButton.position);
        if (distance > interactionRadius) return;

        string action = currentlyOutside ? "MASUKKAN SEMUA HEWAN" : "KELUARKAN SEMUA HEWAN";
        int inside = 0;
        int outside = 0;
        foreach (AnimalRoutine resident in home.Residents)
        {
            if (resident == null || resident.Animal == null || !resident.Animal.HasBeenBorn) continue;
            if (resident.IsHoused) inside++; else outside++;
        }
        WorldInteractionPrompt.Request(this, toggleButton,
            $"E: {action}\n{home.Label} | Dalam {inside} | Luar {outside}", distance, 0.55f);
        if (!PlayerInteractionTarget.PressPickup(player.transform, toggleButton, KeyCode.E, interactionRadius)) return;
        bell.ToggleAnimals(home);
    }

    public bool IsPlayerInRange(Transform playerTransform)
    {
        return playerTransform != null && toggleButton != null &&
               HorizontalDistance(playerTransform.position, toggleButton.position) <= interactionRadius;
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
