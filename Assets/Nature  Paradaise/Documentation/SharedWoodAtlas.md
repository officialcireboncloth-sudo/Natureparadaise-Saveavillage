# Shared wood atlas and manure fork

The Toon Farm Pack `TFP_Spading_Fork_01A` is fitted to the player's hand as `Prefabs/Player/Tools/Held_Pitchfork.prefab`. Select Garpu (Pitchfork), approach a pile and press E or tap the action prompt. The editable Scoop Manure clip is played over one second; both hands grip the shaft and the tines reach the pile. After the full second, exactly one pile disappears and one Compost enters the inventory, with a `Compost x1` notice at the bottom right. Putting the fork away before completion cancels without reward; a full inventory leaves the pile intact. Repeated input cannot collect the same pile twice. Existing manure item IDs remain valid for old inventory saves.

Livestock (cow, sheep, goat, chicken and duck) and owned horses receive one random daily opportunity between 06:00–22:00. Each animal can produce at most once per game day and cannot create another pile while its previous pile remains. The pile stays at its saved position across days, room changes and Save/Load. Missed days never generate a catch-up batch. Daily schedules and produced-day quotas are saved; legacy interval schedules skip production for the loaded day to avoid duplication. Legacy piles are preserved and block new production until cleaned.

The new `item.compost` material also works in the fertilizer processor through `compost.soil`, `compost.booster.basic`, and `compost.booster.organic`. These preserve the corresponding legacy recipe costs/times while accepting the unified cleanup reward. Legacy manure recipes and in-progress recipe IDs remain available for old stock and saves.

## Reusable material

Use `Material/Shared Wood Atlas.mat` for wooden tools, furniture and building parts. It references the existing atlas `Textures/Buildings/BarnStyle_B/TFP_Atlas_BarnB_Wood_1A.png` and its normal map; the historical filename and GUID are retained so Barn B keeps its existing references. The original vendor Toon Pack PSD/materials remain unchanged. There is no separate wood texture for each object.

The warm, dark and pale painted wood slots were revised using native GIMP brushes and retain an editable XCF alongside the PNG. Broad flowing grain and restrained knots remain legible at gameplay distance. Red wood and red roof slots retain their previous artwork. The slot manifest is `Textures/Buildings/BarnStyle_B/SharedWoodSlots.json`.

| Wood | Safe UV minimum | Safe UV maximum | Grain direction |
| --- | --- | --- | --- |
| Warm | (0.0078125, 0.7578125) | (0.1171875, 0.9921875) | V along the shaft/plank |
| Dark | (0.1328125, 0.7578125) | (0.2421875, 0.9921875) | V along the shaft/plank |
| Painted cream | (0.3828125, 0.7578125) | (0.4921875, 0.9921875) | V along the shaft/plank |

Keep UVs inside these padded rectangles; do not repeat UVs across atlas boundaries. Rotate UVs to follow the actual plank length. Use slightly different crops for neighbouring planks. Nonwood parts can continue to sample their original palette positions on the same atlas. The shared material uses matte URP Lit shading and mild normal strength, with mipmaps and compressed 1024px Android/iOS atlas imports. Source colour stays 2048px and the shared normal map stays 1024px. Device performance still depends on the complete scene.

## House interiors

House levels 1–5 now map wooden floors, framing, beds, tables, chairs and other wooden furniture to grain regions rather than a single flat palette pixel. Glass, displays, fabric, plaster and other nonwood UVs retain their authored samples. Static furniture/architecture geometry and colliders retain their original positions, triangles and bounds; only seam vertices needed for UV separation are added. The actual house scene inherits material changes through its existing prefab references.

The explicit editor menu **Nature Paradise → Art → Apply Shared Wood and Pitchfork** recreates the fork fit and reapplies the shared atlas to house runtime meshes. Run it after rebuilding the original house meshes with HouseAtlasInteriorSetup. Already mapped house surfaces are skipped on repeat application. The generated assets are committed normally together with their `.meta` files; clients do not need the editor menu merely to play.
