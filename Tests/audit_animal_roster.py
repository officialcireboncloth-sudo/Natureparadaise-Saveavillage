"""Check imported animal data, unknown prices, production bindings and shop prices."""
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


def value(s, key):
    return re.search(r"^\s+" + re.escape(key) + r": (.*)$", s, re.M)[1]


def main():
    data = rows(ROOT / "Data/Balance/Animals.csv")
    check(len(data) == 28 and len({r["animal_id"] for r in data}) == 28, "28 unique source rows")
    check(sum(bool(r["buy_price"]) for r in data) == 13, "13 known purchase prices")
    check(sum(bool(r["adult_sell_price"]) for r in data) == 13, "13 known adult sale prices")
    check(sum(bool(r["product_sell_price"]) for r in data) == 7, "7 known product prices")
    blocks = re.split(r"^  - id: ", text(ROOT / "Resources/Catalogs/AnimalRosterCatalog.asset"), flags=re.M)[1:]
    catalog = {b.splitlines()[0]: b for b in blocks}
    check(len(catalog) == 28, "28 serialized entries")
    items = {r["item_id"]: r for r in rows(ROOT / "Data/Balance/Items.csv")}
    for row in data:
        block = catalog[row["animal_id"]]
        for key, flag, serialized in [("buy_price", "hasBuyPrice", "buyPrice"), ("adult_sell_price", "hasAdultSellPrice", "adultSellPrice"), ("product_sell_price", "hasProductPrice", "productPrice")]:
            check(value(block, flag) == str(int(bool(row[key]))), "Known/unknown price flag " + row["animal_id"])
            check(value(block, serialized) == (row[key] or "0"), "Exact source amount " + row["animal_id"])
        if row["profile_path"]:
            profile = text(Path(row["profile_path"]))
            check(value(profile, "variantId") == row["animal_id"], "Stable profile identity")
            check(value(profile, "baseSellPrice") == (row["adult_sell_price"] or "0"), "Profile adult sale base")
            check(value(profile, "productionEnabled") == str(int(row["purpose"] == "Product")), "Role controls production")
            check(value(profile, "sellOnlyAsAdult") == "1", "Adult price gate")
        if row["product_item_id"]:
            product = items[row["product_item_id"]]
            check(product["sell_price"] == (row["product_sell_price"] or "0"), "Product CSV price")
            check(value(text(Path(product["asset_path"])), "sellPrice") == (row["product_sell_price"] or "0"), "Product asset price")
    expected = {"animal.cow_dairy": (6400, 4000), "animal.chicken_layer": (1200, 750), "animal.goat_dairy": (4000, 2500),
                "animal.sheep_wool": (4800, 3000), "animal.pig_meat": (4800, 9000), "animal.duck_layer": (1600, 1000)}
    shop = text(ROOT / "Resources/Shops/Animal Catalog.asset")
    offers = re.findall(r"    - displayName: .*?(?=    - displayName:|  - title:|  priceOverrides:)", shop, re.S)
    for animal_id, (buy, sell) in expected.items():
        row = next(r for r in data if r["animal_id"] == animal_id)
        profile = Path(row["profile_path"])
        guid = re.search(r"^guid: (\w+)", text(Path(str(profile) + ".meta")), re.M)[1]
        offer = next(b for b in offers if value(b, "offerKind") == "1" and guid in b)
        check(int(value(offer, "price")) == buy and int(row["adult_sell_price"]) == sell, "Shop and profile source price " + animal_id)
    for b in offers:
        check(int(value(b, "price")) > 0, "No unintended free animal offers")
    pig = text(ROOT / "Resources/Profiles/Animals/Pig Growth Profile.asset")
    check(value(pig, "productionEnabled") == "0" and value(pig, "productItem") == "{fileID: 0}", "Pig has no product")
    print(f"PASS {checks} animal roster/asset/shop/CSV checks")


if __name__ == "__main__":
    main()
