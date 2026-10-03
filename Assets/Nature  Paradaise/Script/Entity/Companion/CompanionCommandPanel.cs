using UnityEngine;

/// <summary>Menu sederhana Follow, Stay, Go Home, dan Mount untuk companion terdekat.</summary>
public sealed class CompanionCommandPanel : MonoBehaviour
{
    static CompanionCommandPanel instance;
    PersonalAnimal companion;
    PlayerController player;

    public static void Show(PersonalAnimal selected, PlayerController selectedPlayer)
    {
        if (selected == null || selectedPlayer == null || instance != null) return;
        instance = new GameObject("CompanionCommandPanel_Runtime").AddComponent<CompanionCommandPanel>();
        instance.companion = selected;
        instance.player = selectedPlayer;
        selectedPlayer.AcquireMovementLock(instance);
        TimeManager.Instance?.AcquirePause(instance);
        WorldInteractionPrompt.AcquireSuppression(instance);
    }

    void Update() { if (Input.GetKeyDown(KeyCode.Escape)) Destroy(gameObject); }

    void OnDestroy()
    {
        player?.ReleaseMovementLock(this);
        TimeManager.Instance?.ReleasePause(this);
        WorldInteractionPrompt.ReleaseSuppression(this);
        if (instance == this) instance = null;
    }

    void OnGUI()
    {
        if (companion == null) { Destroy(gameObject); return; }
        float width = Mathf.Min(420f, Screen.width - 24f);
        GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, 35f, width, 310f), GUI.skin.box);
        GUILayout.Label($"{companion.DisplayName} — {companion.Species}");
        GUILayout.Label($"Command: {companion.Command} | Heart: {companion.HeartPoints}/1000");
        if (GUILayout.Button("Follow Me")) { companion.SetCommand(CompanionCommand.Follow, player.transform); Destroy(gameObject); }
        if (GUILayout.Button("Stay")) { companion.SetCommand(CompanionCommand.Stay, player.transform); Destroy(gameObject); }
        if (GUILayout.Button("Go Home")) { companion.SetCommand(CompanionCommand.GoHome, player.transform); Destroy(gameObject); }
        if (companion.Species == CompanionSpecies.Horse && GUILayout.Button("Mount"))
        {
            bool mounted = companion.TryMount(player);
            if (!mounted) SaveLoadFeedback.Instance?.ShowMessage("Tidak dapat menaiki horse dari posisi ini.");
            Destroy(gameObject);
        }
        if (GUILayout.Button("Tutup (Esc)")) Destroy(gameObject);
        GUILayout.EndArea();
    }
}
