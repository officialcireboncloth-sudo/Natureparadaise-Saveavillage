# Feed Maker UI

Open using the existing machine interaction. The right-hand panel uses FeedMaker capacity, queue, output, upgrade and save data.

Artwork: Nature Paradise > UI > Feed Maker > Select Image Slots. Assign sprites in FeedMakerTheme; null slots stay empty. Ingredient icons use ItemSO.icon. Livestock, poultry and fish produce distinct existing feed items. Dedicated building machines have no category tabs: Barn shows only Pakan Hewan, Coop only Pakan Unggas, and the fish pond only Pakan Ikan. The recipe card fills the former tab area, the title identifies the machine, and the A/D category shortcut is absent and inactive. Only a generic machine exposes category tabs.

Buttons are gray normally, green on pointer hover. The active tab has a neutral underline.

Controls: A/D category, W/S quantity, recipe arrows change recipe, Enter produce, F collect one, Esc close. All selected batches must fit input capacity and have every ingredient before the inventory is committed once. Missing ingredients or capacity reject the whole request. The UI shows available/required amounts per ingredient, scaled to the selected number of batches. Processing duration is shown per batch, with original machine level concurrency. Production resumes after closing the paused modal.

An upgrade button retains existing wood/stone/gold costs. Opening, closing or viewing does not consume ingredients.

## Approved recipes — 8 November 2026

| Output | All required ingredients | Per batch | Used by |
| --- | --- | --- | --- |
| Pakan Hewan | 10 Rumput + 2 Jagung + 1 Konsentrat | 10 | Sapi, kambing, domba, alpaka, kuda, keledai, babi |
| Pakan Unggas | 5 Jagung + 1 Kedelai + 1 Keong danau | 10 | Ayam, bebek, burung unta |
| Pakan Ikan | 5 Ikan kecil + 1 Roti | 10 | Ikan di kolam budidaya |

Ikan kecil means `FishSizeTier.Small` using the existing per-species thresholds. Different species may be combined to reach five. Medium/large/jumbo fish and fish without a recorded size are not consumed. Stack quality and fish size/weight remain intact for items that are not used.

Konsentrat is stocked in Supermarket/Minimarket > Bahan. Its provisional buy price is 100 G because no price was specified. Roti reuses `item.bread`, already stocked in Makanan with its existing shop price. Both survive regeneration through the separated-shop setup rules. New `item.soybean` and `item.lake_snail` assets are registered for the recipe; their farming/foraging acquisition sources are **not yet configured**. Their buy/sell prices stay zero pending the actual balance data. No new art is included in this recipe change.

The catalog now exposes exactly three recipes. The old single-material choices are removed from new production. Saved jobs retain their original recipe ID, input/output amounts, kind and remaining time; they finish with their original output rather than being converted into the new ten-unit batches. Batch duration remains the prior 4 game hours before level speed bonuses. Level-1 input capacity is at least one complete recipe (13 units); higher levels retain their existing 20/40/60-unit capacities.

Data lives in `Resources/Catalogs/FeedMakerCatalog.asset`, `Data/Balance/Items.csv`, and `Data/Balance/Settings/FeedMaker.csv`. In the settings CSV, edit each `ingredients.Array.data[n].amount` for required amounts; legacy `inputCount` is only a fallback for old recipe definitions.

Verification: 74 planner/catalog checks and 67 asset/shop/CSV checks pass. Runtime and editor code compile without errors. `Tests/FeedRecipeTests.cs` runs the actual recipe planner/catalog with minimal engine stand-ins. The editor menu `Nature Paradise > UI > Feed Maker > Validate Recipes and Shop` exercises the real queue, inventory, outputs, old save jobs, and shop bindings in a disposable preview scene. Running the menu in Unity and checking the UI in Play Mode are still required for native integration/visual verification.
