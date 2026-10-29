using System.Collections.Generic;
using UnityEngine;

/// <summary>One visible owner per Lumber Store. Paid projects share his availability, including the trip home.</summary>
[DisallowMultipleComponent]
public sealed class LumberStoreOwner : MonoBehaviour
{
    readonly List<ConstructionProject> orders = new();
    ConstructionProject activeOrder;
    Transform idleVisual;
    HumanoidActionPlayer idleMotion;
    UpgradeShopFront front;
    public bool IsAway => activeOrder != null;
    public ConstructionProject ActiveOrder => activeOrder;
    public int WaitingOrders => orders.Count - (activeOrder != null ? 1 : 0);
    public Transform IdleVisual => idleVisual;
    public string StoreId => gameObject.scene.path + ":" + Hierarchy(transform);
    public Vector3 Departure => front != null && front.builderDeparturePoint != null
        ? front.builderDeparturePoint.position : transform.position + transform.forward * 2.2f;

    static string Hierarchy(Transform value) => value.parent != null ? Hierarchy(value.parent) + "/" + value.name : value.name;
    public static LumberStoreOwner Ensure(UpgradeShopFront shop)
    {
        if (shop == null || shop.kind != UpgradeShopKind.Lumber) return null;
        var owner = shop.GetComponent<LumberStoreOwner>();
        return owner != null ? owner : shop.gameObject.AddComponent<LumberStoreOwner>();
    }
    public static LumberStoreOwner Find(Vector3 departure, string savedId = null)
    {
        LumberStoreOwner nearest = null; float distance = float.MaxValue;
        foreach (var shop in Object.FindObjectsByType<UpgradeShopFront>(FindObjectsSortMode.None))
        {
            if (shop.kind != UpgradeShopKind.Lumber) continue;
            var owner = Ensure(shop);
            if (!string.IsNullOrEmpty(savedId) && owner.StoreId == savedId) return owner;
            float candidate = (owner.Departure - departure).sqrMagnitude;
            if (candidate < distance) { nearest = owner; distance = candidate; }
        }
        return nearest;
    }
    void Awake()
    {
        front = GetComponent<UpgradeShopFront>();
        var settings = ConstructionWorkerSettings.Load();
        if (settings == null || settings.workerPrefab == null) return;
        var placeholder = transform.Find("Seller_Capsule_Editable");
        if (placeholder != null) placeholder.gameObject.SetActive(false);
        idleVisual = new GameObject("Lumber_Owner_AtStore_Runtime").transform;
        idleVisual.SetParent(transform, false);
        Vector3 point = ConstructionProject.GroundDeparture(Departure);
        idleVisual.SetPositionAndRotation(point, transform.rotation);
        // Match the exact model, metre scale and Avatar used by dispatched workers, even under scaled shops.
        Vector3 parentScale = transform.lossyScale;
        idleVisual.localScale = new Vector3(settings.workerScale / Mathf.Max(.001f, Mathf.Abs(parentScale.x)),
            settings.workerScale / Mathf.Max(.001f, Mathf.Abs(parentScale.y)), settings.workerScale / Mathf.Max(.001f, Mathf.Abs(parentScale.z)));
        var model = ConstructionProjectVisuals.VisualClone(settings.workerPrefab, idleVisual);
        var animator = model.GetComponentInChildren<Animator>();
        if (animator != null && animator.avatar != null && animator.avatar.isHuman && animator.avatar.isValid && settings.humanoidActions != null)
            idleMotion = new HumanoidActionPlayer(animator, settings.humanoidActions);
    }
    public void Register(ConstructionProject project)
    {
        if (!orders.Contains(project)) orders.Add(project);
    }
    public bool TryDispatch(ConstructionProject project)
    {
        if (activeOrder == project) return true;
        if (activeOrder != null) return false;
        orders.RemoveAll(p => p == null || p.Phase == ConstructionWorkerPhase.Finished);
        orders.Sort((a, b) => { int order = a.QueueOrder.CompareTo(b.QueueOrder); return order != 0 ? order : string.CompareOrdinal(a.name, b.name); });
        if (orders.Count == 0 || orders[0] != project) return false;
        activeOrder = project;
        if (idleVisual != null) idleVisual.gameObject.SetActive(false);
        return true;
    }
    public void Release(ConstructionProject project)
    {
        orders.Remove(project);
        if (activeOrder != project) return;
        activeOrder = null;
        if (idleVisual != null) idleVisual.gameObject.SetActive(true);
    }
    void Update()
    {
        if (!IsAway) idleMotion?.Evaluate(TimeManager.Instance != null && TimeManager.Instance.IsPaused ? 0 : Time.deltaTime);
    }
    void OnDestroy() { idleMotion?.Dispose(); }
}
