#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>CSV designer-facing untuk menambah dan balancing recipe tanpa mengedit inspector satu per satu.</summary>
public static class KitchenRecipeCsvDatabase
{
    const string CsvPath = "Assets/Nature  Paradaise/Data/Balance/Recipes.csv";
    const string RecipeRoot = "Assets/Nature  Paradaise/Resources/Cooking/Recipes";
    static readonly string[] Columns = { "recipe_id", "asset_path", "display_name", "description", "ingredients",
        "result_item_id", "result_amount", "required_kitchen_level", "required_equipment", "base_cooking_minutes", "learned_by_default" };

    [MenuItem("Nature Paradise/Data CSV/Recipes/Export")]
    public static void Export()
    {
        IEnumerable<string[]> rows = LoadAssets<KitchenRecipeSO>().OrderBy(value => value.Id).Select(recipe => new[]
        {
            recipe.Id, AssetDatabase.GetAssetPath(recipe), recipe.DisplayName, recipe.description,
            string.Join("|", recipe.ingredients.Where(value => value?.item != null).Select(value => $"{value.item.Id}:{value.amount}")),
            recipe.resultItem != null ? recipe.resultItem.Id : string.Empty, recipe.resultAmount.ToString(),
            recipe.requiredKitchenLevel.ToString(), recipe.requiredEquipment.ToString().Replace(", ", "|"),
            recipe.baseCookingMinutes.ToString(), recipe.learnedByDefault ? "TRUE" : "FALSE"
        });
        WriteCsv(rows); AssetDatabase.Refresh();
        Debug.Log($"[RECIPE CSV] Export selesai: {CsvPath}");
    }

    [MenuItem("Nature Paradise/Data CSV/Recipes/Validate")]
    public static void Validate() => ReadRows(false);

    [MenuItem("Nature Paradise/Data CSV/Recipes/Import")]
    public static void Import() => ReadRows(true);

    static void ReadRows(bool apply)
    {
        if (!File.Exists(CsvPath)) { Debug.LogError($"[RECIPE CSV] File tidak ditemukan: {CsvPath}"); return; }
        List<string[]> table = Parse(File.ReadAllText(CsvPath));
        if (table.Count == 0) { Debug.LogError("[RECIPE CSV] File kosong."); return; }
        Dictionary<string, int> header = table[0].Select((name, index) => (name, index))
            .ToDictionary(value => value.name.Trim(), value => value.index, StringComparer.OrdinalIgnoreCase);
        List<string> missing = Columns.Where(name => !header.ContainsKey(name)).ToList();
        if (missing.Count > 0) { Debug.LogError($"[RECIPE CSV] Kolom hilang: {string.Join(", ", missing)}"); return; }
        Dictionary<string, ItemSO> items = LoadAssets<ItemSO>().GroupBy(value => value.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        List<string> errors = new(); int valid = 0;
        for (int rowIndex = 1; rowIndex < table.Count; rowIndex++)
        {
            string[] row = table[rowIndex]; if (row.All(string.IsNullOrWhiteSpace)) continue;
            string id = Cell(row, header, "recipe_id"); string path = Cell(row, header, "asset_path");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(path)) { errors.Add($"Baris {rowIndex + 1}: recipe_id/asset_path kosong."); continue; }
            if (!items.TryGetValue(Cell(row, header, "result_item_id"), out ItemSO result)) { errors.Add($"{id}: result item tidak ditemukan."); continue; }
            List<KitchenIngredientRequirement> ingredients = new(); bool ingredientError = false;
            foreach (string token in Cell(row, header, "ingredients").Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] pair = token.Split(':');
                if (pair.Length != 2 || !items.TryGetValue(pair[0].Trim(), out ItemSO item) || !int.TryParse(pair[1], out int amount) || amount <= 0)
                { errors.Add($"{id}: ingredient tidak valid '{token}'."); ingredientError = true; break; }
                ingredients.Add(new KitchenIngredientRequirement { item = item, amount = amount });
            }
            if (ingredientError || ingredients.Count == 0) continue;
            string equipmentText = Cell(row, header, "required_equipment").Replace('|', ',');
            if (!Enum.TryParse(equipmentText, true, out KitchenEquipment equipment)) { errors.Add($"{id}: equipment tidak valid."); continue; }
            int resultAmount = Positive(Cell(row, header, "result_amount"), 1);
            int kitchenLevel = Mathf.Clamp(Positive(Cell(row, header, "required_kitchen_level"), 1), 1, 4);
            int minutes = Mathf.Max(0, Positive(Cell(row, header, "base_cooking_minutes"), 10));
            if (apply)
            {
                KitchenRecipeSO recipe = AssetDatabase.LoadAssetAtPath<KitchenRecipeSO>(path);
                if (recipe == null) { recipe = ScriptableObject.CreateInstance<KitchenRecipeSO>(); Directory.CreateDirectory(Path.GetDirectoryName(path)); AssetDatabase.CreateAsset(recipe, path); }
                recipe.recipeId = id; recipe.displayName = Cell(row, header, "display_name"); recipe.description = Cell(row, header, "description");
                recipe.ingredients = ingredients; recipe.resultItem = result; recipe.resultAmount = resultAmount;
                recipe.requiredKitchenLevel = kitchenLevel; recipe.requiredEquipment = equipment; recipe.baseCookingMinutes = minutes;
                recipe.learnedByDefault = bool.TryParse(Cell(row, header, "learned_by_default"), out bool learned) && learned;
                EditorUtility.SetDirty(recipe);
            }
            valid++;
        }
        if (errors.Count > 0) { foreach (string error in errors) Debug.LogError($"[RECIPE CSV] {error}"); return; }
        if (apply) { AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); }
        Debug.Log($"[RECIPE CSV] {(apply ? "Import" : "Validasi")} berhasil: {valid} recipe.");
    }

    static int Positive(string value, int fallback) => int.TryParse(value, out int parsed) && parsed > 0 ? parsed : fallback;
    static string Cell(string[] row, Dictionary<string, int> header, string name) => header[name] < row.Length ? row[header[name]].Trim() : string.Empty;
    static List<T> LoadAssets<T>() where T : UnityEngine.Object => AssetDatabase.FindAssets($"t:{typeof(T).Name}")
        .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>).Where(value => value != null).ToList();

    static void WriteCsv(IEnumerable<string[]> rows)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CsvPath));
        using StreamWriter writer = new(CsvPath, false, new UTF8Encoding(false));
        writer.WriteLine(string.Join(",", Columns.Select(Escape)));
        foreach (string[] row in rows) writer.WriteLine(string.Join(",", row.Select(Escape)));
    }
    static string Escape(string value)
    {
        value ??= string.Empty; return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? value : $"\"{value.Replace("\"", "\"\"")}\"";
    }
    static List<string[]> Parse(string text)
    {
        List<string[]> result = new(); List<string> row = new(); StringBuilder cell = new(); bool quoted = false;
        for (int index = 0; index < text.Length; index++)
        {
            char value = text[index];
            if (value == '"') { if (quoted && index + 1 < text.Length && text[index + 1] == '"') { cell.Append('"'); index++; } else quoted = !quoted; }
            else if (value == ',' && !quoted) { row.Add(cell.ToString()); cell.Clear(); }
            else if ((value == '\n' || value == '\r') && !quoted)
            {
                if (value == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
                row.Add(cell.ToString()); cell.Clear(); result.Add(row.ToArray()); row.Clear();
            }
            else cell.Append(value);
        }
        if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); result.Add(row.ToArray()); }
        return result;
    }
}
#endif
