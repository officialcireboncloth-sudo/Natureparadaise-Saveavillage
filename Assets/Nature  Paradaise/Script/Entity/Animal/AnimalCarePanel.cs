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
        GUILayout.Label($"Debug: seluruh sisa pakan dikosongkan pukul 00:00 — {GameTimeDebugText.UntilMidnight()} lagi.");
        GUILayout.Label("Drag Grass atau Animal Feed dari Inventory ke kotak tempat pakan. Tombol tersedia untuk mobile.");

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(260f));
        GUILayout.Label("INVENTORY — PAKAN");
        feedInventoryScroll = GUILayout.BeginScrollView(feedInventoryScroll, GUILayout.Height(150f));
        bool found = false;
        if (inventory != null)
        {
            for (int index = 0; index < inventory.slots.Count; index++)
            {
                ItemStack stack = inventory.GetSlot(index);
                if (stack?.item == null || stack.count <= 0 || !home.AcceptsFeedItem(stack.item)) continue;
                found = true;
                GUILayout.BeginHorizontal(GUI.skin.box);
                Rect dragRect = GUILayoutUtility.GetRect(new GUIContent($"{stack.DisplayName} x{stack.count}"), GUI.skin.box,
                    GUILayout.ExpandWidth(true), GUILayout.Height(40f));
                GUI.Box(dragRect, $"{stack.DisplayName} x{stack.count}\nDRAG", GUI.skin.box);
                HandleDragSource(dragRect, index, stack);
                if (GUILayout.Button("+1", GUILayout.Width(42f), GUILayout.Height(40f))) TryDeposit(index, 1);
                if (GUILayout.Button("All", GUILayout.Width(44f), GUILayout.Height(40f))) TryDeposit(index, stack.count);
                GUILayout.EndHorizontal();
            }
        }
        if (!found) GUILayout.Label("Tidak ada Grass atau Animal Feed.");
        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
        GUILayout.Label("TROUGH / TEMPAT PAKAN");
        Rect targetRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.box,
            GUILayout.ExpandWidth(true), GUILayout.Height(92f));
        Color previous = GUI.color;
        GUI.color = home.FeedSpace > 0 ? new Color(0.72f, 1f, 0.72f) : new Color(1f, 0.58f, 0.58f);
        GUI.Box(targetRect, home.FeedSpace > 0
            ? $"DROP DI SINI\n{home.TotalFeed} / {home.FeedingSlotCapacity}\nSisa {home.FeedSpace}"
            : $"PENUH\n{home.TotalFeed} / {home.FeedingSlotCapacity}");
        GUI.color = previous;
        HandleDropTarget(targetRect);
        if (GUILayout.Button($"Ambil Animal Feed x{home.Fodder}"))
            feedback = home.Withdraw(inventory) ? "Animal Feed dikembalikan ke Inventory." : "Tas penuh / Animal Feed kosong.";
        if (GUILayout.Button($"Ambil Grass x{home.Grass}"))
            feedback = home.Withdraw(inventory, true) ? "Grass dikembalikan ke Inventory." : "Tas penuh / Grass kosong.";
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        DrawDraggedFeedGhost();
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
            ItemStack stack = inventory?.GetSlot(draggedFeedSlot);
            TryDeposit(draggedFeedSlot, stack?.count ?? 0);
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
        int moved = home.DepositFromSlot(inventory, slotIndex, amount);
        feedback = moved > 0
            ? $"Pakan x{moved} dimasukkan. Isi sekarang {home.TotalFeed}/{home.FeedingSlotCapacity}."
            : "Hanya Grass atau Animal Feed yang dapat dimasukkan.";
    }
    void Select(AnimalGrowthSystem selected) { animal = selected; editedName = selected.AnimalName; }
}
