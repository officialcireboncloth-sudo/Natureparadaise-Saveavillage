using UnityEngine;

/// <summary>Compatibility entry point for item collection; all feedback is rendered by the shared bottom information dock.</summary>
[DisallowMultipleComponent]
public sealed class PlayerPickupNotification : MonoBehaviour
{
    public static void Show(Transform player,string message)
    {
        if(player!=null)GameplayInfoPanel.Notify(message);
    }
    public static void ShowItem(Inventory inventory,ItemSO item,int amount)=>GameplayInfoPanel.NotifyItem(inventory,item,amount);
}
