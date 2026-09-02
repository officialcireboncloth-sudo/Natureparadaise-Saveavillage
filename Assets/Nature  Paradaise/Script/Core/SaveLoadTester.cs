using UnityEngine;

/// <summary>Shortcut development untuk menjalankan save dan load tanpa UI produksi.</summary>
public class SaveLoadTester : MonoBehaviour
{
    public KeyCode saveKey = KeyCode.F10;
    public KeyCode loadKey = KeyCode.F11;

    void Update()
    {
        // Shortcut development tidak boleh merespons saat Debug Clues dimatikan.
        if (!HUDManager.DebugCluesEnabled)
            return;

        if (Input.GetKeyDown(saveKey))
        {
            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveGame();
        }

        if (Input.GetKeyDown(loadKey))
        {
            if (SaveManager.Instance != null)
                SaveManager.Instance.LoadGame();
        }
    }
}
