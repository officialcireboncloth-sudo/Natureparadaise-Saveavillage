using UnityEngine;

/// <summary>Shortcut development untuk menjalankan save dan load tanpa UI produksi.</summary>
public class SaveLoadTester : MonoBehaviour
{
    public KeyCode saveKey = KeyCode.F5;
    public KeyCode loadKey = KeyCode.F9;

    void Update()
    {
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
