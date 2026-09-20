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
    Vector2 feedInventoryScroll;
    int draggedFeedSlot = -1;
    string draggedFeedLabel;
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
            GUILayout.Label($"{home.Label} Lv.{(home.site != null ? home.site.CurrentLevel : 1)} — Animals: {home.AnimalCount}/{home.FeedingSlotCapacity} | Reserved: {home.ReservedSlots} | Available: {home.AvailableSlots}");
            DrawFeedStorage();
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

    void DrawFeedStorage()
    {
        GUILayout.Label($"TEMPAT PAKAN — {home.TotalFeed}/{home.FeedingSlotCapacity} terisi | " +
                        $"Animal Feed {home.Fodder} | Grass {home.Grass} | Kosong {home.FeedSpace}");
        GUILayout.Label($"Belum makan: {home.RequiredFeedToday} | Auto Feeder: {(home.HasAutoFeeder ? "ON" : "OFF")}");
        GUILayout.Label($"Dipakai hari ini: {home.FeedPortionsReserved} box | Kebutuhan maksimal: {home.DailyFeedRequirement}/hari | " +
                        $"Estimasi stok: {home.EstimatedFeedDays} hari.");
        GUILayout.Label($"Box yang dipakai berkurang pukul 00:00 — {GameTimeDebugText.UntilMidnight()} lagi. Sisa box tetap tersimpan.");
        GUILayout.Space(8f);
        GUILayout.Label("CARA MENGISI TEMPAT PAKAN",GUI.skin.box);
        GUILayout.Label("Pilih Animal Feed atau Grass pada hotbar sampai terlihat dipegang player. " +
                        "Dekati box kosong, lalu tekan F. Setiap tekanan memasukkan tepat 1 item.");
    }

    void HandleDragSource(Rect rect, int slotIndex, ItemStack stack)
    {
        Event current = Event.current;
        if (current.type != EventType.MouseDown || current.button != 0 || !rect.Contains(current.mousePosition)) return;
        draggedFeedSlot = slotIndex;
        draggedFeedLabel = $"{stack.DisplayName} x{stack.count}";
        current.Use();
    }

    void HandleDropTarget(Rect rect)
    {
        Event current = Event.current;
        if (draggedFeedSlot < 0 || current.type != EventType.MouseUp || current.button != 0) return;
        if (rect.Contains(current.mousePosition))
        {
            TryDeposit(draggedFeedSlot, 1);
            current.Use();
        }
        draggedFeedSlot = -1;
        draggedFeedLabel = null;
    }

    void DrawDraggedFeedGhost()
    {
        if (draggedFeedSlot < 0) return;
        Event current = Event.current;
        if (current.type == EventType.MouseUp)
        {
            draggedFeedSlot = -1;
            draggedFeedLabel = null;
            return;
        }
        Vector2 mouse = current.mousePosition;
        GUI.Box(new Rect(mouse.x + 12f, mouse.y + 12f, 150f, 34f), draggedFeedLabel ?? "Feed");
        if (current.type == EventType.MouseDrag) current.Use();
    }

    void TryDeposit(int slotIndex, int amount)
    {
        if (home.FeedSpace <= 0)
        {
            feedback = $"Tempat pakan penuh ({home.TotalFeed}/{home.FeedingSlotCapacity}).";
            return;
        }
        int moved = home.DepositFromSlot(inventory, slotIndex, 1);
        feedback = moved > 0
            ? $"Box {home.TotalFeed} terisi. Isi sekarang {home.TotalFeed}/{home.FeedingSlotCapacity}."
            : "Hanya Grass atau Animal Feed yang dapat dimasukkan.";
    }
    void Select(AnimalGrowthSystem selected) { animal = selected; editedName = selected.AnimalName; }
}
