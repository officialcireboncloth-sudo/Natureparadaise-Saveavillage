"""Check authored asset wiring against CSV without requiring a Unity process."""
from pathlib import Path
import csv
import re
import struct

ROOT = Path(__file__).resolve().parents[1] / "Assets/Nature  Paradaise"
checks = 0


def check(ok, label):
    global checks
    checks += 1
    assert ok, label


def text(path):
    return path.read_text(encoding="utf-8-sig")


def rows(path):
    with path.open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))


def guid(path):
    return re.search(r"^guid: (\w+)", text(Path(str(path) + ".meta")), re.M)[1]


def main():
    items = rows(ROOT / "Data/Balance/Items.csv")
    lookup = {r["item_id"]: r for r in items}
    check(len(lookup) == len(items), "Item IDs unique")
    for r in items:
        source = r.get("mutation_source_item_id", "")
        if not source:
            continue
        check(source in lookup and source != r["item_id"], "Valid mutation source " + r["item_id"])
        variant = Path(r["asset_path"])
        normal = Path(lookup[source]["asset_path"])
        data = text(variant)
        check("  isMutatedCrop: 1" in data and guid(normal) in data, "Mutation price reference " + r["item_id"])
        check(r["item_id"] in data, "CSV and serialized item ID match")
    for species in ("Apple", "Mango", "Orange", "Coconut"):
        data = text(ROOT / f"Resources/Trees/{species}.asset")
        check("  matureDays: 60\n" in data and "  useFruitCare: 1\n" in data, "Fruit care " + species)
        check(len(re.findall(r"^  - stage:", data, re.M)) == 5, "Five stage slots " + species)
        encoded = re.search(r"  fruitAmountsByStars: (\w+)", data)[1]
        check(struct.unpack("<5i", bytes.fromhex(encoded)) == (5, 7, 10, 15, 20), "Star yield " + species)
        variant = ROOT / f"Resources/Items/Crops/Mutations/{species} Fruit Mutation.asset"
        check(guid(variant) in data and "  fruitMutationChance: 0.01" in data, "One percent mutation binding " + species)
    crops = rows(ROOT / "Data/Balance/Crops.csv")
    for r in crops:
        check(r["mutation_produce_item_id"] in lookup and float(r["mutation_chance"]) == .01, "Crop mutation " + r["crop_id"])
        check(float(r["five_star_double_yield_chance"]) == .2, "Crop bonus " + r["crop_id"])
    tree_rows = rows(ROOT / "Data/Balance/Settings/Trees.csv")
    for r in tree_rows:
        if r["asset_name"] not in ("Apple", "Mango", "Orange", "Coconut"):
            continue
        if r["setting"] == "matureDays":
            check(r["value"] == "60", "Settings CSV maturity " + r["asset_name"])
        m = re.match(r"stages.Array.data\[(\d+)\]", r["setting"])
        if m:
            check(int(m[1]) < 5, "No deleted stage in CSV")
    print(f"PASS {checks} authored asset/CSV checks; {len(crops)} crops, 4 fruit profiles, "
          f"{len(rows(ROOT / 'Data/Balance/Fish.csv'))} fish profiles")


if __name__ == "__main__":
    main()
