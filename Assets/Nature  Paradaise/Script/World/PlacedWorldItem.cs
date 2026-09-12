using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Data save satu stack item yang dijatuhkan atau diletakkan di dunia.</summary>
[Serializable]
public sealed class PlacedItemSaveData
{
    public string id;
    public string itemId;
    public string assetName;
    public string itemName;
    public int amount;
    public int qualityStars;
    public Vector3 position;
    public Vector3 eulerAngles;
    public bool physicsDrop;
    public TreeSaveData tree;
}

/// <summary>
/// Representasi persisten item inventory di dunia. Root menangani fisika,
/// sedangkan child trigger menangani pickup agar benda tetap bertabrakan dengan tanah.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlacedWorldItem : MonoBehaviour
{
    static readonly List<PlacedWorldItem> Registry = new();

    [SerializeField] string persistentId;
    [SerializeField] ItemSO item;
    [SerializeField, Min(1)] int amount = 1;
    [SerializeField, Range(0, 5)] int qualityStars;
    [SerializeField] bool physicsDrop;

    public static IReadOnlyList<PlacedWorldItem> Active => Registry;
    public ItemSO Item => item;
    public bool IsInstalledFarmItem => !physicsDrop && item != null && item.IsFarmPlacement;

    void OnEnable()
    {
        if (!Registry.Contains(this)) Registry.Add(this);
    }

    void OnDisable() => Registry.Remove(this);

    /// <summary>Membuat representasi world dari item inventory dalam mode drop atau place stabil.</summary>
    public static PlacedWorldItem Spawn(ItemSO item, int amount, Vector3 position, Quaternion rotation, bool usePhysics, int qualityStars = 0)
    {
        if (item == null || amount <= 0) return null;

        GameObject root = new($"WorldItem_{item.itemName}");
        root.transform.SetPositionAndRotation(position, rotation);
        PlacedWorldItem placed = root.AddComponent<PlacedWorldItem>();
        placed.persistentId = Guid.NewGuid().ToString("N");
        placed.item = item;
        placed.amount = amount;
        placed.qualityStars = qualityStars;
        placed.physicsDrop = usePhysics;

        CreateVisual(item, root.transform);
        if (!usePhysics && item.treeDefinition != null)
        {
            BoxCollider treeCollider = root.AddComponent<BoxCollider>();
            treeCollider.size = new Vector3(0.7f, 2f, 0.7f);
            treeCollider.center = Vector3.up;
            WorldTree tree = root.AddComponent<WorldTree>();
            tree.InitializePlanted(placed.persistentId, item.treeDefinition);
            return placed;
        }
        SphereCollider solid = root.AddComponent<SphereCollider>();
        solid.radius = 0.32f;
        solid.center = Vector3.up * 0.32f;
        if (usePhysics)
        {
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 0.35f;
            body.linearDamping = 0.2f;
            body.angularDamping = 0.8f;
        }

        GameObject trigger = new("PickupTrigger");
        trigger.transform.SetParent(root.transform, false);
        trigger.transform.localPosition = Vector3.up * 0.35f;
        SphereCollider triggerCollider = trigger.AddComponent<SphereCollider>();
        triggerCollider.radius = 0.75f;
        triggerCollider.isTrigger = true;
        WorldItemPickup pickup = trigger.AddComponent<WorldItemPickup>();
        pickup.Initialize(item, amount);
        pickup.QualityStars = qualityStars;
        pickup.SetDestroyTarget(root);
        return placed;
    }

    static void CreateVisual(ItemSO item, Transform parent)
    {
        GameObject visual = item.worldPrefab != null
            ? Instantiate(item.worldPrefab, parent)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "ItemVisual";
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = Vector3.up * 0.35f;
        visual.transform.localScale = item.worldScale == Vector3.zero ? Vector3.one * 0.4f : item.worldScale;
        if (item.treeDefinition != null) visual.transform.localPosition = Vector3.up * visual.transform.localScale.y * 0.5f;
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Rigidbody body in visual.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
    }

    /// <summary>Mengambil snapshot posisi dan stack item untuk save.</summary>
    public PlacedItemSaveData Capture() => new()
    {
        id = persistentId,
        itemId = item != null ? item.Id : string.Empty,
        assetName = item != null ? item.name : string.Empty,
        itemName = item != null ? item.itemName : string.Empty,
        amount = amount,
        qualityStars = qualityStars,
        position = transform.position,
        eulerAngles = transform.eulerAngles,
        physicsDrop = physicsDrop,
        tree = GetComponent<WorldTree>()?.Capture()
    };

    public static List<PlacedItemSaveData> CaptureAll()
    {
        List<PlacedItemSaveData> result = new(Registry.Count);
        for (int i = 0; i < Registry.Count; i++)
            if (Registry[i] != null && Registry[i].item != null) result.Add(Registry[i].Capture());
        return result;
    }

    /// <summary>Membuat kembali seluruh placed item dari save data.</summary>
    public static void RestoreAll(List<PlacedItemSaveData> data)
    {
        PlacedWorldItem[] current = Registry.ToArray();
        Registry.Clear();
        for (int i = 0; i < current.Length; i++) if (current[i] != null)
        {
            current[i].gameObject.SetActive(false);
            Destroy(current[i].gameObject);
        }
        if (data == null) return;

        for (int i = 0; i < data.Count; i++)
        {
            PlacedItemSaveData saved = data[i];
            ItemSO resolved = ItemCatalog.Resolve(saved.itemId, saved.assetName, saved.itemName);
            PlacedWorldItem restored = Spawn(resolved, saved.amount, saved.position, Quaternion.Euler(saved.eulerAngles), saved.physicsDrop, saved.qualityStars);
            if (restored != null)
            {
                restored.persistentId = saved.id;
                restored.GetComponent<WorldTree>()?.Restore(saved.tree);
            }
        }
    }

}
