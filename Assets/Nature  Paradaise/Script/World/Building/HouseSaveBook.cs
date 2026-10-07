using UnityEngine;

/// <summary>Scene-authored save book, independent from sleeping at the bed.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class HouseSaveBook : MonoBehaviour
{
    [SerializeField, Min(.5f)] float interactionRadius = 1.5f;
    static readonly System.Collections.Generic.HashSet<HouseSaveBook> active = new();
    void OnEnable() => active.Add(this);
    void OnDisable() => active.Remove(this);
    public static bool HasCloserBook(Transform player, float distance)
    {
        foreach(var book in active)
            if(book != null && PlayerInteractionTarget.ContainsPickup(player, book.transform, book.interactionRadius) &&
                Vector2.Distance(new Vector2(player.position.x,player.position.z),new Vector2(book.transform.position.x,book.transform.position.z)) < distance) return true;
        return false;
    }
    PlayerController player;
    void Update()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (player == null || !PlayerInteractionTarget.ContainsPickup(player.transform, transform, interactionRadius)) return;
        WorldInteractionPrompt.Request(this, transform, "E: Buku Save — Simpan permainan", Vector3.Distance(player.transform.position, transform.position), .4f);
        if (!PlayerInteractionTarget.PressPickup(player.transform, transform, KeyCode.E, interactionRadius)) return;
        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
        GameplayInput.ConsumeCurrentFrame();
    }
}
