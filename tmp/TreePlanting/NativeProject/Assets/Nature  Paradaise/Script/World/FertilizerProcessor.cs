using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FertilizerJob
{
    public string recipeId;
    public double finishesAtHour;
}

[Serializable]
public sealed class FertilizerProcessorSaveData
{
    public string id;
    public List<FertilizerJob> jobs;
}

/// <summary>Antrean memakai jam kalender game sehingga tidur ikut menyelesaikan produksi.</summary>
public sealed class FertilizerProcessor : MonoBehaviour
{
    static readonly List<FertilizerProcessor> Active = new();
    void OnEnable() => Active.Add(this);
    void OnDisable() { ClosePanel(); Active.Remove(this); }
    public static bool BlocksWorldPointer => FertilizerMakerUI.Instance != null;
    public FertilizerCatalog Catalog => catalog != null ? catalog : catalog = FertilizerCatalog.Load();
    public bool IsOpen => FertilizerMakerUI.Instance != null && FertilizerMakerUI.Instance.Machine == this;
    public string ProcessorId => processorId;
    [SerializeField] string processorId = "farm-fertilizer-processor";
    [SerializeField] FertilizerCatalog catalog;
    [SerializeField] Inventory inventory;
    [SerializeField, Min(1)] int queueCapacity = 8;
    [SerializeField, Min(1f)] float interactionDistance = 3f;
    readonly List<FertilizerJob> jobs = new();
    public IReadOnlyList<FertilizerJob> Jobs => jobs;
    double Now => TimeManager.Instance == null ? 0 :
        (TimeManager.Instance.day - 1) * 24d + TimeManager.Instance.hour + TimeManager.Instance.minute / 60d;
    void Awake()
    {
        if (catalog == null) catalog = FertilizerCatalog.Load();
        if (GetComponentInChildren<Renderer>() == null)
        {
            GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dummy.name = "ProcessorVisual_ReplaceMe";
            dummy.transform.SetParent(transform, false);
            dummy.transform.localPosition = Vector3.up * 0.6f;
            dummy.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        }
    }

    public bool TryQueue(int recipeIndex)
    {
        if (inventory == null || catalog == null || TimeManager.Instance == null ||
            recipeIndex < 0 || recipeIndex >= catalog.recipes.Length || jobs.Count >= queueCapacity) return false;
        FertilizerRecipe recipe = catalog.recipes[recipeIndex];
        if (recipe == null || recipe.output == null || recipe.ingredients == null ||
            recipe.outputCount < 1 || recipe.productionHours < 1) return false;
        Dictionary<ItemSO, int> needed = new();
        foreach (FertilizerIngredient ingredient in recipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null || ingredient.amount < 1) return false;
            needed.TryGetValue(ingredient.item, out int count);
            needed[ingredient.item] = count + ingredient.amount;
        }
        foreach (var pair in needed)
            if (inventory.GetCount(pair.Key) < pair.Value) return false;
        foreach (var pair in needed) inventory.Remove(pair.Key, pair.Value);
        double start = jobs.Count == 0 ? Now : Math.Max(Now, jobs[jobs.Count - 1].finishesAtHour);
        jobs.Add(new FertilizerJob { recipeId = recipe.id, finishesAtHour = start + recipe.productionHours });
        return true;
    }

    public bool TryCollect()
    {
        if (catalog == null || inventory == null || jobs.Count == 0 || Now < jobs[0].finishesAtHour) return false;
        FertilizerRecipe recipe = Array.Find(catalog.recipes, r => r != null && r.id == jobs[0].recipeId);
        if (recipe == null || !inventory.Add(recipe.output, recipe.outputCount)) return false;
        jobs.RemoveAt(0);
        return true;
    }

    void Update()
    {
        if (IsOpen) return;
        if (inventory == null) inventory = FindFirstObjectByType<PlayerController>()?.GetComponent<Inventory>();
        if (!CanInteract(inventory)) return;
        WorldInteractionPrompt.Request(this, transform, "E: Pembuat Pupuk",
            PlayerInteractionTarget.SurfaceDistance(inventory.transform, transform), 1.4f, () => Open(inventory));
        if (PlayerInteractionTarget.PressPrompt(this, KeyCode.E)) Open(inventory);
    }
    public bool CanInteract(Inventory source)
    {
        var actor = source != null ? source.GetComponent<PlayerController>() : null;
        return isActiveAndEnabled && actor != null && !actor.IsMovementLocked && !actor.IsRiding &&
            !LoadingScreen.IsVisible && !WorldInteractionPrompt.IsSuppressed && Catalog != null &&
            (SceneTransitionManager.Instance == null || !SceneTransitionManager.Instance.IsTransitioning) &&
            Mathf.Abs(source.transform.position.y - transform.position.y) < 2 &&
            PlayerInteractionTarget.SurfaceDistance(source.transform, transform) <= interactionDistance;
    }
    public bool Open(Inventory source)
    {
        if (!CanInteract(source) || FertilizerMakerUI.Instance != null) return false;
        inventory = source;
        source.GetComponent<PlayerController>().FaceTowardsInteraction(transform.position);
        return FertilizerMakerUI.Show(this, source);
    }
    void ClosePanel() { if (IsOpen) FertilizerMakerUI.Instance.Close(); }

    public static List<FertilizerProcessorSaveData> CaptureAll()
    {
        List<FertilizerProcessorSaveData> result = new();
        foreach (FertilizerProcessor machine in FindObjectsByType<FertilizerProcessor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            List<FertilizerJob> copy = new();
            foreach (FertilizerJob job in machine.jobs)
                copy.Add(new FertilizerJob { recipeId = job.recipeId, finishesAtHour = job.finishesAtHour });
            result.Add(new FertilizerProcessorSaveData { id = machine.processorId, jobs = copy });
        }
        return result;
    }

    public static void RestoreAll(List<FertilizerProcessorSaveData> saved)
    {
        foreach (FertilizerProcessor machine in FindObjectsByType<FertilizerProcessor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            machine.jobs.Clear();
            FertilizerProcessorSaveData entry = saved?.Find(s => s != null && s.id == machine.processorId);
            if (entry?.jobs == null) continue;
            foreach (FertilizerJob job in entry.jobs)
                if (job != null) machine.jobs.Add(new FertilizerJob { recipeId = job.recipeId, finishesAtHour = job.finishesAtHour });
        }
    }
}
