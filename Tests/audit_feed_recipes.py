"""Verify the approved feed recipe assets, shop stock and CSV bindings."""
from pathlib import Path
import csv
import re

ROOT = Path(__file__).resolve().parents[1] / "Assets/Nature  Paradaise"
checks = 0


def check(ok, label):
    global checks
    checks += 1
    assert ok, label


def text(p):
    return p.read_text(encoding="utf-8-sig")


def rows(p):
    with p.open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))


def main():
    items = rows(ROOT / "Data/Balance/Items.csv")
    lookup = {row["item_id"]: row for row in items}
    check(len(items) == len(lookup), "Unique item IDs")
    guids = {}
    for item_id in ["item.grass", "item.corn", "item.feed_concentrate", "item.soybean", "item.lake_snail", "item.bread", "item.tilapia"]:
        p = Path(lookup[item_id]["asset_path"])
        data = text(p)
        check(f"  itemId: {item_id}\n" in data, "CSV and item ID agree " + item_id)
        guids[item_id] = re.search(r"^guid: (\w+)", text(Path(str(p) + ".meta")), re.M)[1]
    data = text(ROOT / "Resources/Catalogs/FeedMakerCatalog.asset")
    recipes = re.split(r"^  - id: ", data, flags=re.M)[1:]
    check(len(recipes) == 3, "Exactly three approved recipes")
    expected = [
        ("livestock-balanced", [("item.grass", 10), ("item.corn", 2), ("item.feed_concentrate", 1)], 0, 0),
        ("poultry-balanced", [("item.corn", 5), ("item.soybean", 1), ("item.lake_snail", 1)], 0, 1),
        ("fish-small-bread", [("item.tilapia", 5), ("item.bread", 1)], 1, 0),
    ]
    for data, (name, ingredients, fish, poultry) in zip(recipes, expected):
        check(data.startswith(name + "\n"), "Stable recipe ID " + name)
        check("    outputCount: 10\n" in data and "    hours: 4\n" in data, "Ten feed, existing duration " + name)
        check(f"    producesFishFeed: {fish}\n" in data and f"    producesPoultryFeed: {poultry}\n" in data, "Distinct output kind " + name)
        parts = re.findall(r"    - item:.*?(?=    - item:|    input:)", data, re.S)
        check(len(parts) == len(ingredients), "Ingredient count " + name)
        for part, (item_id, count) in zip(parts, ingredients):
            check(guids[item_id] in part and f"      amount: {count}\n" in part, "Ingredient binding/amount " + item_id)
            check(f"      anySmallFish: {int(fish and item_id == 'item.tilapia')}\n" in part, "Fish size rule " + item_id)
    shop = text(ROOT / "Resources/Shops/Minimarket Catalog.asset")
    for tab, item_id in [("Bahan", "item.feed_concentrate"), ("Makanan", "item.bread")]:
        body = re.search(r"  - title: " + tab + r"\n(.*?)(?=  - title:|  priceOverrides:)", shop, re.S)[1]
        check(guids[item_id] in body, "Supermarket " + tab + " stocks " + item_id)
    for item_id in ["item.feed_concentrate", "item.soybean", "item.lake_snail"]:
        p = Path(lookup[item_id]["asset_path"])
        check("  feedKind: 0\n" in text(p), "Raw ingredient is not ready-made feed " + item_id)
    settings = [r for r in rows(ROOT / "Data/Balance/Settings/FeedMaker.csv") if r["asset_name"] == "FeedMakerCatalog"]
    lookup_settings = {r["setting"]: r["value"] for r in settings}
    for i, (_, ingredients, _, _) in enumerate(expected):
        for j, (_, count) in enumerate(ingredients):
            check(lookup_settings[f"recipes.Array.data[{i}].ingredients.Array.data[{j}].amount"] == str(count), "CSV ingredient amount")
        check(lookup_settings[f"recipes.Array.data[{i}].outputCount"] == "10", "CSV ten output")
    for r in settings:
        check(int(re.search(r"recipes.Array.data\[(\d+)\]", r["setting"])[1]) < 3, "No removed recipe in settings CSV")
    print(f"PASS {checks} feed asset/shop/CSV checks")


if __name__ == "__main__":
    main()
