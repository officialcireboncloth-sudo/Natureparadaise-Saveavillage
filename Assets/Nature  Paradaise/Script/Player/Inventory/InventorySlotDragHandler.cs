using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Adapter event Unity UI untuk satu slot inventory. Logika perpindahan tetap
/// dimiliki InventoryUI agar komponen slot tetap ringan dan mudah diganti visualnya.
/// </summary>
[DisallowMultipleComponent]
public sealed class InventorySlotDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    InventoryUI owner;
    int slotIndex;

    public void Initialize(InventoryUI inventoryUI, int index)
    {
        owner = inventoryUI;
        slotIndex = index;
    }

    public void OnBeginDrag(PointerEventData eventData) => owner?.BeginSlotDrag(slotIndex, eventData);
    public void OnDrag(PointerEventData eventData) => owner?.UpdateSlotDrag(eventData);
    public void OnEndDrag(PointerEventData eventData) => owner?.EndSlotDrag();
    public void OnDrop(PointerEventData eventData) => owner?.DropOnSlot(slotIndex);
}
