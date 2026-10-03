using UnityEngine;

/// <summary>Keputusan setelah tameable creature dikalahkan.</summary>
public sealed class CreatureFatePanel : MonoBehaviour
{
    static CreatureFatePanel instance;
    TameableCreature creature;
    PlayerController player;

    public static void Show(TameableCreature selected, PlayerController selectedPlayer)
    {
        if (selected == null || selectedPlayer == null || instance != null) return;
        instance = new GameObject("CreatureFatePanel_Runtime").AddComponent<CreatureFatePanel>();
        instance.creature = selected; instance.player = selectedPlayer;
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
        if (creature == null) { Destroy(gameObject); return; }
        float width = Mathf.Min(430f, Screen.width - 24f);
        GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, 50f, width, 220f), GUI.skin.box);
        GUILayout.Label("Creature Defeated");
        GUILayout.Label("Mercy akan menjinakkan hewan ini sebagai personal companion.");
        if (GUILayout.Button("Mercy / Tame")) { creature.ChooseMercy(); Destroy(gameObject); }
        if (GUILayout.Button("End")) { creature.ChooseEnd(); Destroy(gameObject); }
        if (GUILayout.Button("Kembali (Esc)")) Destroy(gameObject);
        GUILayout.EndArea();
    }
}
