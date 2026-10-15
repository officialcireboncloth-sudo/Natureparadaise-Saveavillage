using UnityEngine;

/// <summary>Authored visual bindings for one barn upgrade; storage capacity remains owned by AnimalHome.</summary>
[DisallowMultipleComponent]
public sealed class BarnInteriorLayout : MonoBehaviour
{
    [SerializeField] Transform feedingRoot;
    [SerializeField] Transform[] feedingSlots;
    [Tooltip("One renderer per logical portion. Multiple portions may share a visual compartment.")]
    [SerializeField] Renderer[] feedFills;
    [SerializeField] Transform[] animalSpots;
    [SerializeField] Transform feedMakerPoint;

    public Transform FeedingRoot => feedingRoot;
    public Transform[] FeedingSlots => feedingSlots;
    public Renderer[] FeedFills => feedFills;
    public Transform[] AnimalSpots => animalSpots;
    public Transform FeedMakerPoint => feedMakerPoint;

#if UNITY_EDITOR
    public void EditorConfigure(Transform root,Transform[] slots,Renderer[] fills,Transform[] animals,Transform maker)
    {
        feedingRoot=root;feedingSlots=slots;feedFills=fills;animalSpots=animals;feedMakerPoint=maker;
    }
#endif
}
