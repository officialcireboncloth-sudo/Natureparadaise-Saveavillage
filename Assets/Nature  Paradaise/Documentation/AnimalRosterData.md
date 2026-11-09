# Animal roster import — 8 November 2026

Source: `List animal Fix.csv`, 28 numbered rows, supplied by the user. The uploaded bytes were decoded with the wrong character encoding by the text reader; reversing that decoding recovered all 1,874 original bytes, then the CSV was read with its comma separator and quoted thousands separators. Empty prices remain empty in `Data/Balance/Animals.csv`, with explicit known/unknown flags in `Resources/Catalogs/AnimalRosterCatalog.asset`.

| Data imported | Count |
| --- | --- |
| Animal/role entries | 28 |
| Known animal buy prices | 13 |
| Known adult sale prices | 13 |
| Known basic product sale prices | 7 |
| Runtime livestock species connected | 6 |
| Profiles including meat/breeding variants | 17 |

## Connected livestock

| Variant | Young purchase | Adult sale base | Basic product price per unit |
| --- | --- | --- | --- |
| Sapi Perah | 6,400 G | 4,000 G | Milk: 400 G |
| Ayam Petelur | 1,200 G | 750 G | Egg: 100 G |
| Kambing Perah | 4,000 G | 2,500 G | Goat Milk: 300 G |
| Domba Wol | 4,800 G | 3,000 G | Wool: 650 G |
| Babi Ternak | 4,800 G | 9,000 G | No product |
| Bebek Petelur | 1,600 G | 1,000 G | Duck Egg: 150 G |

The existing heart bonus for live-animal value is retained: the CSV adult price is the zero-heart base, not a replacement for that mechanic. Prices are exposed only once adult. Product Normal quality (tier 1) now sells at the exact basic price in the source; existing 20% quality steps start at tier 2. Shearing still yields the existing number of wool items; 650 G is per item, not per shearing action.

Pig is appended after the five original `AnimalType` values, preserving existing save identities. It uses the existing Piglet/Pig art and the existing Barn/livestock feed, care and growth framework. Its 56/28-day growth defaults are the existing generic non-bird fallback, **not growth values supplied by this CSV**. No new pig production is created. Existing species' growth durations and artwork are retained.

The existing animal shop entries and legacy shop-generation fallback now read the source prices/profiles. Breeding egg offer prices are not specified in the source and keep their existing prices. Existing supermarket retail overrides for affected products are raised, where necessary, using the existing three-times-base policy so an old 60 G milk override does not remain below the new 400 G sale price; these are derived retail prices, not prices supplied by the animal CSV.

## Roles and saves

Meat and breeding variants for Cow, Chicken, Goat, Sheep, Duck and Pig have separate profile IDs. Production is disabled on those profiles; male milk/egg producers cannot generate products. New young dairy/layer offers are female. Purchased breeding eggs and farm births retain the existing random-sex behavior.

Unknown-price variants are recorded but are not stocked as free animals. A live animal saves `variantId`, so a meat animal does not restore as a dairy/layer animal. Old saves without the field keep their old species/profile lookup. The selected companion economy ID is also saved; existing Horse/Dog/Cat companions expose roster prices through their existing species defaults, with explicit IDs available for Husky or other roles.

## Data recorded without claiming completed gameplay

The roster includes Goose variants, Horse riding/hauling, Alpaca fiber, Donkey hauling, Ostrich layers, guard Dog, Husky companion, and house Cat, including all known prices and benefits exactly as provided. New Goose Egg, Alpaca Fiber and Ostrich Egg item identities are registered; the latter two have source sale prices 1,000 G and 800 G. Goose Egg has no configured sale price.

These entries are not routed through the Cow/Barn purchase path just because they have a number in the buy-price column. Existing horse/companion behavior remains intact. New species production, new companion purchase/restore workflows, and cargo/hauling inventory integration still need their own implementation and visual validation. The CSV does not specify those mechanics, growth timings, capacities, production intervals, or missing prices. Model ownership colors from the original spreadsheet are not present in the CSV and are not inferred.

## Updating the data

Edit `Data/Balance/Animals.csv`, then use `Nature Paradise > Data CSV > Import Animal Roster`. This updates roster values, product prices, existing profile prices/roles, and existing shop offers after validating rows. It preserves configured runtime/model bindings; adding a new species requires configuring its implementation rather than assigning it a default Cow type.

Checks: `Tests/AnimalRosterTests.cs` exercises real catalog/profile code with minimal Unity stand-ins; `Tests/audit_animal_roster.py` checks all rows, unknown-price flags, profiles, outputs and shop bindings. Product tier-1 pricing is covered by `Tests/HarvestSystemsTests.cs`. Compilation and standalone checks do not replace an editor/Play Mode check of purchasing, growth and load.

Verification result: 24 catalog/pricing checks, 272 source/asset/shop checks and 69 harvest/pricing regression checks pass. Runtime and editor assemblies compile without errors. Native Play Mode verification is pending.
