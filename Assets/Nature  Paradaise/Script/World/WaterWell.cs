using System.Collections;
using UnityEngine;

/// <summary>Sumur isi ulang Watering Can. Dummy otomatis ditempatkan di sisi field untuk pengujian.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class WaterWell : MonoBehaviour
{
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField, Min(0.5f)] float interactionRadius = 2.4f;
    [SerializeField, Min(0f)] float promptHeight = 1.5f;
    [Header("Animation Impact Timing")]
    [SerializeField, Min(0f)] float refillImpactDelay = 1.55f;
    [SerializeField, Min(0f)] float refillActionDuration = 2.05f;
    Inventory playerInventory;
    bool actionBusy;

    void Update()
    {
        if (playerInventory == null) playerInventory = FindFirstObjectByType<Inventory>();
        if (actionBusy || playerInventory == null ||
            !PlayerInteractionTarget.ContainsPickup(playerInventory.transform, transform, interactionRadius)) return;

        WateringCanSystem can = playerInventory.GetComponent<WateringCanSystem>();
        string status = can != null ? $"{can.CurrentWater}/{can.MaximumWater}" : "100/100";
        WorldInteractionPrompt.Request(this, transform, $"{interactKey}: Isi Watering Can ({status})",
            Vector3.Distance(playerInventory.transform.position, transform.position), promptHeight);
        if (!PlayerInteractionTarget.PressPickup(playerInventory.transform, transform, interactKey, interactionRadius)) return;

        if (can == null) can = playerInventory.gameObject.AddComponent<WateringCanSystem>();
        if (can.CurrentWater >= can.MaximumWater)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Watering Can sudah penuh: 100/100");
            return;
        }

        PlayerController movement = playerInventory.GetComponent<PlayerController>();
        movement?.PlayRefillWateringCanAnimation();
        StartCoroutine(RefillRoutine(can, movement));
    }

    IEnumerator RefillRoutine(WateringCanSystem can, PlayerController movement)
    {
        actionBusy = true;
        movement?.AcquireMovementLock(this);
        if (refillImpactDelay > 0f) yield return new WaitForSeconds(refillImpactDelay);
        bool changed = can != null && can.Refill();
        SaveLoadFeedback.Instance?.ShowMessage(changed
            ? $"Watering Can terisi penuh: {can.CurrentWater}/{can.MaximumWater}"
            : "Watering Can sudah penuh: 100/100");
        float remaining = Mathf.Max(0f, refillActionDuration - refillImpactDelay);
        if (remaining > 0f) yield return new WaitForSeconds(remaining);
        movement?.ReleaseMovementLock(this);
        actionBusy = false;
    }

    void OnDisable()
    {
        if (playerInventory != null)
            playerInventory.GetComponent<PlayerController>()?.ReleaseMovementLock(this);
        actionBusy = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureTestWell()
    {
        if (FindFirstObjectByType<WaterWell>() != null || FieldArea.ActiveAreas.Count == 0) return;
        FieldArea field = FieldArea.ActiveAreas[0];
        if (field == null) return;

        GameObject well = CreateTestWellVisual();
        well.name = "WaterWell_Test_Runtime";
        Vector3 ground = field.GridToWorld(0, 0) - field.transform.right * field.CellSize * 1.4f;
        FitVisualAndCollider(well, ground);
        well.AddComponent<WaterWell>();
    }

    static GameObject CreateTestWellVisual()
    {
        WaterWellVisualCatalog catalog = Resources.Load<WaterWellVisualCatalog>("WaterWellVisualCatalog");
        if (catalog != null && catalog.wellPrefab != null) return Instantiate(catalog.wellPrefab);
#if UNITY_EDITOR
        // Fallback editor menjaga prototype tetap jalan jika catalog belum terimport.
        const string assetPath = "Assets/Nature  Paradaise/mesh/Dummy/DummyWell.fbx";
        GameObject model = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (model != null) return Instantiate(model);
#endif
        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Renderer renderer = fallback.GetComponent<Renderer>();
        if (renderer != null) renderer.material.color = new Color(0.38f, 0.42f, 0.48f);
        return fallback;
    }

    static void FitVisualAndCollider(GameObject well, Vector3 ground)
    {
        Renderer[] renderers = well.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new(well.transform.position, Vector3.one);
        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (hasBounds)
        {
            float horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);
            if (horizontalSize > 0.01f)
            {
                float scale = 2.4f / horizontalSize;
                well.transform.localScale *= scale;
                bounds = new Bounds();
                hasBounds = false;
                foreach (Renderer renderer in renderers)
                {
                    if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
            }
            well.transform.position += ground - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }
        else well.transform.position = ground + Vector3.up * 0.45f;

        Collider collider = well.GetComponent<Collider>();
        if (collider == null)
        {
            BoxCollider box = well.AddComponent<BoxCollider>();
            if (hasBounds)
            {
                box.center = well.transform.InverseTransformPoint(bounds.center);
                Vector3 scale = well.transform.lossyScale;
                box.size = new Vector3(
                    bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                    bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                    bounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
            }
        }
    }
}
