#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Uses actual Resources, store transactions and disposable inventory fixtures.</summary>
public static class CropGameplayValidation
{
    const string Output = "ArtSource/Crops/Integration/UnityGameplayAudit.json";
    static readonly string[] Kinds = { "Watermelon", "Melon", "Zucchini", "Cucumber", "Pea", "Kiwi", "GreenBean", "ChiliPepper", "Eggplant", "Cabbage", "Broccoli", "Cauliflower" };
    [Serializable] sealed class Report { public string status; public List<string> checks = new(); public string error; }
    static Report report;
    static void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException(label); report.checks.Add(label); }

    [MenuItem("Nature Paradise/Crops/Validate Applied Crops and Seed Store")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run this store fixture outside Play Mode.");
        report = new Report { status = "RUNNING" };
        Directory.CreateDirectory(Path.GetDirectoryName(Output));
        var preview = EditorSceneManager.NewPreviewScene();
        var garbage = new List<Object>();
        ScoreManager previousScore = ScoreManager.Instance;
        try
        {
            ItemCatalog.Invalidate();
            var catalog = Resources.Load<ShopCatalogSO>("Shops/Crops Catalog");
            Check(catalog != null && catalog.kind == ShopKind.Crops, "Existing seed-store catalog resolves");
            var seedTab = catalog.tabs.Single(t => t.title == "Benih");
            Check(seedTab.items.Count == 12 && seedTab.items.Distinct().Count() == 12, "Twelve distinct seeds in the existing Benih tab");
            var actor = new GameObject("Crop store disposable fixture");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor, preview);
            var bag = actor.AddComponent<Inventory>();
            var manager = actor.AddComponent<ShopManager>(); manager.catalog = catalog; manager.playerInv = bag;
            var score = actor.AddComponent<ScoreManager>(); ScoreManager.Instance = score; score.points = 10000;
            foreach (string kind in Kinds)
            {
                var crop = Resources.Load<CropDataSO>("Profiles/Farming/" + kind + " Crop");
                Check(crop != null && crop.seedItem != null && crop.produceItem != null, "Crop and item resources " + kind);
                var seed = crop.seedItem;
                Check(seed.IsSeed && seed.seedCrop == crop && catalog.Contains(seed) && seedTab.items.Contains(seed), "Store-to-seed-to-crop reverse links " + kind);
                Check(ItemCatalog.Resolve(seed.Id) == seed && ItemCatalog.Resolve(crop.produceItem.Id) == crop.produceItem, "Stable inventory/save IDs " + kind);
                Check(crop.visualStages.Length == (kind == "Cabbage" ? 6 : 5), "Reference phases and legacy Cabbage schedule " + kind);
                Check(crop.visualStages.All(s => s.modelPrefab != null && s.modelPrefab.GetComponent<LODGroup>() != null), "Every crop stage uses a real LOD prefab " + kind);
                Check(crop.GetStageForGrowth(crop.TotalGrowthDays) == crop.StageCount - 1, "Growth reaches final plant phase " + kind);
                Check(crop.visualStages.Last().modelPrefab.name.EndsWith("_Ready") && crop.produceItem.worldPrefab.name.EndsWith("_Harvest"), "Ready plant stays planted; detached produce is the inventory/drop model " + kind);
                foreach (var item in new[] { seed, crop.produceItem, crop.mutationProduceItem })
                {
                    Check(item != null && item.icon != null && item.inventoryIllustration != null, "Inventory and shop sprites " + kind + " " + item.name);
                    Check(item.icon.rect.size == new Vector2(256, 256) && item.icon.texture.width == 256 && item.icon.texture.height == 256, "Actual 256px sprite import " + item.name);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(item.icon));
                    Check(importer.alphaIsTransparency && !importer.mipmapEnabled && importer.maxTextureSize == 256, "UI sprite transparency and import size " + item.name);
                }
                var product = new ShopFrontProduct(seed, false, catalog.BuyPrice(seed), catalog.Settings(seed));
                Check(product.Icon == seed.inventoryIllustration, "Actual storefront resolves the packet artwork " + kind);
                Check(crop.mutationProduceItem.isMutatedCrop && crop.mutationProduceItem.mutationSourceItem == crop.produceItem
                    && crop.mutationProduceItem.GetMarketSellPrice(5) == crop.produceItem.GetMarketSellPrice(5) * 3, "Mutation retains its source and price rule " + kind);
                bag.RestoreContents(0, Enumerable.Repeat<ItemStack>(null, bag.CapacityAtBackpackLevel(0)).ToList());
                int goldBefore = score.points, unitPrice = catalog.BuyPrice(seed);
                Check(manager.TryBuyItem(seed, 2), "Actual shop purchases two packets " + kind);
                Check(bag.GetCount(seed) == 2 && score.points == goldBefore - unitPrice * 2, "Exact seed delivery and gold deduction " + kind);
                Check(!manager.TryBuyItem(seed, 0) && score.points == goldBefore - unitPrice * 2 && bag.GetCount(seed) == 2, "Zero-quantity purchase changes nothing " + kind);
                int savedGold = score.points; score.points = 0;
                Check(!manager.TryBuyItem(seed, 1) && score.points == 0 && bag.GetCount(seed) == 2, "Insufficient gold changes no inventory " + kind);
                score.points = savedGold;
            }
            // A full bag must return the gold spent by the transaction service.
            var filler = ScriptableObject.CreateInstance<ItemSO>(); garbage.Add(filler);
            filler.itemId = "item.validation_full_bag"; filler.maxStack = 1;
            bag.RestoreContents(0, Enumerable.Range(0, bag.CapacityAtBackpackLevel(0)).Select(_ => new ItemStack { item = filler, count = 1 }).ToList());
            int fullGold = score.points;
            Check(!manager.TryBuyItem(seedTab.items[0], 1) && score.points == fullGold, "Full-inventory failure refunds the whole price");
            Check(bag.GetCount(seedTab.items[0]) == 0, "Failed transaction creates no seed packets");
            var otherStore = ScriptableObject.CreateInstance<ShopCatalogSO>(); garbage.Add(otherStore); otherStore.kind = ShopKind.Animal;
            manager.catalog = otherStore;
            Check(!manager.TryBuyItem(seedTab.items[0], 1) && score.points == fullGold, "A different shop cannot sell these seeds implicitly");
            report.status = "PASS";
        }
        catch (Exception e) { report.status = "FAIL"; report.error = e.ToString(); throw; }
        finally
        {
            ScoreManager.Instance = previousScore;
            EditorSceneManager.ClosePreviewScene(preview);
            foreach (var obj in garbage) if (obj != null) Object.DestroyImmediate(obj);
            File.WriteAllText(Output, JsonUtility.ToJson(report, true));
            Debug.Log("[CROP STORE VALIDATION] " + report.status + " " + report.checks.Count + " checks");
        }
    }
}
#endif
