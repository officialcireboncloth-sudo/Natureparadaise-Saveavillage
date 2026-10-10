using UnityEngine;

/// <summary>Stove interaction on the authored modular house fixture.</summary>
[DisallowMultipleComponent]
public sealed class KitchenSet : MonoBehaviour
{
    [SerializeField] Inventory playerInventory;
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField, Min(.5f)] float interactionRadius = 2.5f;
    [SerializeField, Min(0)] float promptHeight = 1.35f;
    public Inventory PlayerInventory => playerInventory;
    public bool IsPanelOpen => KitchenUI.Instance != null && KitchenUI.Instance.Stove == this;
    void Awake() => ResolvePlayer();
    void OnDisable(){if(IsPanelOpen)KitchenUI.Instance.Close();}
    void Update()
    {
        ResolvePlayer();if(playerInventory==null||IsPanelOpen)return;
        if(!PlayerInteractionTarget.ContainsPickup(playerInventory.transform,transform,interactionRadius))return;
        WorldInteractionPrompt.Request(this,transform,KitchenService.IsUnlocked
            ? $"{interactKey}: Memasak — Kitchen Lv.{KitchenService.Level}" : "Kompor terkunci — House Lv.3",
            Vector3.Distance(playerInventory.transform.position,transform.position),promptHeight);
        if(PlayerInteractionTarget.PressPickup(playerInventory.transform,transform,interactKey,interactionRadius))OpenPanel();
    }
    public void OpenPanel()
    {
        ResolvePlayer();
        if(!KitchenService.IsUnlocked){SaveLoadFeedback.Instance?.ShowMessage("Kitchen Set terbuka pada House Lv.3.");return;}
        KitchenUI.Show(this);
    }
    void ResolvePlayer(){if(playerInventory==null)playerInventory=FindFirstObjectByType<Inventory>();}
}
