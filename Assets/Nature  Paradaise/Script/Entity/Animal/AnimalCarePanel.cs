using UnityEngine;

/// <summary>Menu gameplay kandang/animal: rename, trough, care, penugasan, turnout dan recall.</summary>
public sealed class AnimalCarePanel : MonoBehaviour
{
    static AnimalCarePanel instance;
    AnimalGrowthSystem animal;
    AnimalHome home;
    Inventory inventory;
    PlayerController player;
    string editedName;
    string feedback;
    Vector2 scroll;
    public static void Show(AnimalGrowthSystem selected, AnimalHome selectedHome, Inventory inv)
    {
        if (inv == null || (selected == null && selectedHome == null) || instance != null) return;
        instance = new GameObject("AnimalCarePanel_Runtime").AddComponent<AnimalCarePanel>();
        instance.animal = selected; instance.home = selectedHome; instance.inventory = inv;
        instance.player = inv.GetComponent<PlayerController>();
        instance.editedName = selected != null ? selected.AnimalName : string.Empty;
        instance.player?.AcquireMovementLock(instance);
        TimeManager.Instance?.AcquirePause(instance);
        WorldInteractionPrompt.AcquireSuppression(instance);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || (animal == null && home == null)) Destroy(gameObject);
    }
    void OnDestroy()
    {
        player?.ReleaseMovementLock(this);
        TimeManager.Instance?.ReleasePause(this);
        WorldInteractionPrompt.ReleaseSuppression(this);
        if (instance == this) instance = null;
    }
    void OnGUI()
    {
        float width = Mathf.Min(570f, Screen.width - 20f);
        GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, 20, width, Screen.height - 40f), GUI.skin.box);
        if (GUILayout.Button("Tutup (Esc)")) Destroy(gameObject);
        scroll = GUILayout.BeginScrollView(scroll);
        if (home != null)
        {
            GUILayout.Label($"{home.Label} — {home.Residents.Count}/{home.Capacity} hewan | Pakan: {home.Fodder}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Isi 1 Fodder")) feedback = home.Deposit(inventory, 1) ? "Pakan ditambahkan" : "Fodder kurang / tempat penuh";
            if (GUILayout.Button("Isi 10 Fodder")) feedback = home.Deposit(inventory, 10) ? "Pakan ditambahkan" : "Butuh 10 Fodder / tempat penuh";
            if (GUILayout.Button("Ambil sisa pakan")) feedback = home.Withdraw(inventory) ? "Pakan dikembalikan" : "Tas penuh / pakan kosong";
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Keluarkan hewan"))
            {
                int released = 0;
                foreach (AnimalRoutine routine in home.Residents) if (routine.Release()) released++;
                feedback = $"{released} hewan keluar. Hanya siang, cuaca baik, dan sudah lahir.";
            }
            if (GUILayout.Button("Panggil pulang")) foreach (AnimalRoutine routine in home.Residents) routine.Recall();
            GUILayout.EndHorizontal();
            foreach (AnimalRoutine routine in home.Residents)
                if (GUILayout.Button($"{routine.Animal.AnimalName} — {routine.Animal.Type} — {routine.Activity}")) Select(routine.Animal);
        }
        if (animal != null)
        {
            GUILayout.Space(12); GUILayout.Label(animal.InfoSummary);
            AnimalRoutine routine = animal.GetComponent<AnimalRoutine>();
            GUILayout.Label($"Kandang: {routine?.Home?.Label ?? "Belum ditugaskan"} | {routine?.Activity}");
            editedName = GUILayout.TextField(editedName ?? "", 24);
            if (GUILayout.Button("Simpan nama")) { animal.SetAnimalName(editedName); editedName = animal.AnimalName; }
            AnimalController controller = animal.GetComponent<AnimalController>();
            controller.playerInv = inventory;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Feed (1 Fodder)")) controller.FeedCabbage();
            if (GUILayout.Button("Pet")) animal.Pet();
            if (GUILayout.Button("Treat")) feedback = controller.TryGiveTreat() ? "Treat diberikan" : "Treat kurang / sudah diberikan hari ini";
            if (GUILayout.Button("Medicine")) feedback = controller.TryGiveMedicine() ? "Pemulihan dimulai: pakan + istirahat di kandang" : "Obat kurang / hewan sehat atau sedang pemulihan";
            GUILayout.EndHorizontal();
            if (animal.HasProductReady && GUILayout.Button($"Ambil produk — {AnimalCareCatalog.QualityName(animal.ProductQualityLevel)}")) controller.TakeMilk();
            if (routine != null)
            {
                if (GUILayout.Button(routine.IsHoused ? "Keluar merumput" : "Kembali ke kandang"))
                { if (routine.IsHoused) feedback = routine.Release() ? "Keluar merumput" : "Tidak bisa keluar pada kondisi ini"; else routine.Recall(); }
                GUILayout.Label("Tugaskan / pindahkan kandang:");
                foreach (AnimalHome candidate in AnimalHome.Active)
                    if (candidate != null && candidate.Id != routine.HomeId && candidate.HasRoom(animal.Type) &&
                        GUILayout.Button($"{candidate.Label} ({candidate.Residents.Count}/{candidate.Capacity})")) routine.Assign(candidate);
            }
        }
        GUILayout.Label(feedback ?? "");
        GUILayout.EndScrollView(); GUILayout.EndArea();
    }
    void Select(AnimalGrowthSystem selected) { animal = selected; editedName = selected.AnimalName; }
}
