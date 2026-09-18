using UnityEngine;

/// <summary>Menu debug kandang/animal: informasi, rename, trough, care, dan penugasan.</summary>
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
            GUILayout.Label($"{home.Label} Lv.{(home.site != null ? home.site.CurrentLevel : 1)} — Animals: {home.AnimalCount}/{home.Capacity} | Reserved: {home.ReservedSlots} | Available: {home.AvailableSlots}");
            GUILayout.Label($"Animal Feed: {home.Fodder} | Belum makan: {home.RequiredFeedToday} | Auto Feeder: {(home.HasAutoFeeder ? "ON" : "OFF")}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Isi 1 Animal Feed")) feedback = home.Deposit(inventory, 1) ? "Pakan ditambahkan" : "Animal Feed kurang / tempat penuh";
            if (GUILayout.Button("Isi 10 Animal Feed")) feedback = home.Deposit(inventory, 10) ? "Pakan ditambahkan" : "Butuh 10 Animal Feed / tempat penuh";
            if (GUILayout.Button("Ambil sisa pakan")) feedback = home.Withdraw(inventory) ? "Pakan dikembalikan" : "Tas penuh / pakan kosong";
            GUILayout.EndHorizontal();
            GUILayout.Label(home.AnimalsOutside
                ? "Status saklar: DI LUAR — gunakan bell kandang untuk memasukkan semuanya."
                : "Status saklar: DI DALAM — gunakan bell kandang untuk mengeluarkan semuanya.");
            foreach (AnimalRoutine routine in home.Residents)
                if (GUILayout.Button($"{routine.Animal.AnimalName} — {routine.Animal.Type} — {routine.Activity}")) Select(routine.Animal);
        }
        if (animal != null)
        {
            GUILayout.Space(12); GUILayout.Label(animal.InfoSummary);
            GUILayout.Label($"Hari berturut-turut tanpa makan: {animal.HungryDays}/3");
            AnimalRoutine routine = animal.GetComponent<AnimalRoutine>();
            if (animal.IsAdult && GUILayout.Button(AnimalGrowthProfileSO.IsBird(animal.Type) ? "Mulai inkubasi (1 telur, 1 slot)" : "Mulai breeding (1 slot)"))
            {
                ShopManager shop = FindFirstObjectByType<ShopManager>();
                feedback = shop != null && shop.TryStartBreeding(animal, inventory)
                    ? "Proses dimulai; slot sudah direservasi."
                    : "Butuh induk dewasa, sehat, sudah makan, slot kosong dan tidak ada proses sejenis. Inkubasi butuh telur.";
            }
            GUILayout.Label($"Kandang: {routine?.Home?.Label ?? "Belum ditugaskan"} | {routine?.Activity}");
            editedName = GUILayout.TextField(editedName ?? "", 24);
            if (GUILayout.Button("Simpan nama")) { animal.SetAnimalName(editedName); editedName = animal.AnimalName; }
            AnimalController controller = animal.GetComponent<AnimalController>();
            controller.playerInv = inventory;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Feed (1 Animal Feed)")) controller.FeedCabbage();
            if (GUILayout.Button("Pet")) animal.Pet();
            if (GUILayout.Button("Treat")) feedback = controller.TryGiveTreat() ? "Treat diberikan" : "Treat kurang / sudah diberikan hari ini";
            if (GUILayout.Button("Medicine")) feedback = controller.TryGiveBestMedicine() ? "Obat diberikan; cek status kondisi hewan" : "Obat kurang / hewan sehat atau sedang pemulihan";
            GUILayout.EndHorizontal();
            if (animal.HasProductReady && GUILayout.Button($"Ambil produk — {AnimalCareCatalog.QualityName(animal.ProductQualityLevel)}")) controller.TakeMilk();
            if (routine != null)
            {
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
