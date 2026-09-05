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
    static readonly Rect PanelRect = new(20, 190, 500, 480);
    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);
    public static bool BlocksWorldPointer
    {
        get
        {
            Vector2 pointer = new(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (!PanelRect.Contains(pointer)) return false;
            foreach (FertilizerProcessor machine in Active)
                if (machine.inventory != null && machine.catalog != null &&
                    PlayerInteractionTarget.Contains(machine.inventory.transform, machine.transform))
                    return true;
            return false;
        }
    }
    [SerializeField] string processorId = "farm-fertilizer-processor";
    [SerializeField] FertilizerCatalog catalog;
    [SerializeField] Inventory inventory;
    [SerializeField, Min(1)] int queueCapacity = 8;
    [SerializeField, Min(1f)] float interactionDistance = 3f;
    readonly List<FertilizerJob> jobs = new();
    string feedback = "";
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

    void OnGUI()
    {
        if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
        if (inventory == null || catalog == null ||
            !PlayerInteractionTarget.Contains(inventory.transform, transform)) return;
        GUILayout.BeginArea(PanelRect, GUI.skin.box);
        GUILayout.Label("MESIN PENGOLAH PUPUK — QUALITY BOOSTER");
        for (int i = 0; i < catalog.recipes.Length; i++)
        {
            FertilizerRecipe recipe = catalog.recipes[i];
            if (recipe == null || recipe.output == null) continue;
            string ingredients = "";
            foreach (FertilizerIngredient ingredient in recipe.ingredients)
                ingredients += (ingredients.Length == 0 ? "" : " + ") +
                    ingredient.amount + " " + ingredient.item.itemName;
            if (GUILayout.Button(recipe.output.itemName + " x" + recipe.outputCount + " / " + recipe.productionHours + " jam"))
                feedback = TryQueue(i) ? "Produksi masuk antrean." : "Bahan kurang / antrean penuh / waktu belum tersedia.";
            GUILayout.Label(ingredients);
            if (HUDManager.DebugCluesEnabled && GUILayout.Button("DEBUG: beri bahan resep ini"))
                foreach (FertilizerIngredient ingredient in recipe.ingredients)
                    inventory.Add(ingredient.item, ingredient.amount);
        }
        if (jobs.Count > 0)
        {
            double remaining = Math.Max(0d, jobs[0].finishesAtHour - Now);
            if (GUILayout.Button($"Ambil hasil | Antrean {jobs.Count}/{queueCapacity} | Sisa {remaining:F1} jam"))
                feedback = TryCollect() ? "Hasil masuk tas." : "Belum selesai atau tas penuh.";
        }
        GUILayout.Label(feedback);
        GUILayout.EndArea();
    }

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
