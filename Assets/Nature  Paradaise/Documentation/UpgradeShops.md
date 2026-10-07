# Upgrade shops and seller capsules

Map > SHOPS_TESTING contains 01 Crops, 02 Animal, 03 Minimarket, 04 Blacksmith and 05 Lumber Store. All use the original NPCSeller capsule scale and material, with camera-facing labels above them. Objects remain independently movable.

Use **Nature Paradise > Shop > Apply Seller Scale and Upgrade Shops** to create missing upgrade shops or refresh seller styling. Existing upgrade shop layouts and positions are preserved. The previous Carpenter prompt is disabled in the Map to avoid duplicate upgrade entry points.

Select an UpgradeShopFront and use **Preview UI In Hierarchy** in edit mode or **Open For Testing** in Play mode. The authored Upgrade_UI_Editable contains backgrounds, merchant portraits, tool/house illustrations, buttons and text. Illustration slots preserve aspect ratios and stay transparent until artwork is assigned. The screen backdrop covers the entire viewport; only content observes the safe area.

Blacksmith Upgrades.asset contains the six tool offers, maximum levels, illustrations and per-level gold/material costs. Initial balancing is 1000/2000/3000 gold plus Stone and Wood; replace these costs with final balance. The catalogue does not invent an Iron item when the current item database lacks one. Transactions validate village level, inventory materials and gold through BuildingCostUtility, then update PlayerStatusSystem tool levels and save. Individual tool gameplay retains its current level behavior; numerical capacity/area/stamina comparisons are deliberately not displayed until corresponding gameplay definitions exist. The material tab opens the existing transaction UI backed by Blacksmith Materials.asset.

Lumber Store uses PlayerHouseController's actual next-level definition, material requirements, construction days and completion rules. House preview artwork is assigned to houseLevelIllustrations (index 0 = level 1). Features shown correspond to the house levels already implemented. No new construction price table duplicates the house definition.

Both screens pause the clock, lock player movement and suppress world prompts while open, and restore those states and the cursor when closed or disabled. Enter confirms only valid transactions; Escape/E closes. A/D selects tools at the blacksmith. Failed transactions preserve materials and gold.

Refresh Upgrade UI Layouts regenerates the authored UI; use it before making manual artwork/layout edits. Apply Seller Scale and Upgrade Shops preserves existing upgrade layouts and custom material stock.
