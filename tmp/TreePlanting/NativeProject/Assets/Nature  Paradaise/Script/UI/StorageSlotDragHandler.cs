using UnityEngine;
using UnityEngine.EventSystems;

public sealed class StorageSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    StorageChestUI owner;
    bool storage;
    int index;
    public void Initialize(StorageChestUI ui,bool isStorage,int slot)
    {
        owner=ui;
        storage=isStorage;
        index=slot;
    }
    public void OnBeginDrag(PointerEventData e)
    {
        if(index>=0)owner?.BeginDrag(storage,index,e);
    }
    public void OnDrag(PointerEventData e)=>owner?.UpdateDrag(e);
    public void OnEndDrag(PointerEventData e)=>owner?.EndDrag();
    public void OnDrop(PointerEventData e)=>owner?.Drop(storage,index);
}
