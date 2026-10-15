# World proportions â€” 15 October 2026

The player remains 1.5 m tall, with the previously approved player/tool ratio unchanged. This revision calibrates the surrounding world rather than compressing the map or moving village placements.

- Species targets: adult cow 1.26 m, chicken 0.35 m, horse 1.70 m, dog 0.55 m. Cow/chicken stages increase progressively; shared raw FBX/model prefabs remain reusable. Actor colliders and horse spawn scale match the revised bodies. Horse mount/dismount lateral clearance remains 0.65 m in world units.
- Human vendor dummies are 1.50 m. Only their body/collider and world-label children change scale; the shop UI canvas owners retain unit scale. World labels are excluded from physical measurements.
- House/barn exteriors 0.6Ã—, coop 0.75Ã—, large shop modules 0.8Ã—. Root scaling carries meshes, colliders and child entrance markers together. Exterior house door/return markers follow the same factor.
- House/coop interiors use a 0.75Ã— owner; barn uses 0.6Ã—. Furniture, triggers, spawns and cameras inherit once. The existing underground floor at y=-200 is retained for barn/coop; raw interior prefab scales are not multiplied again.
- Grass/bush/flower detail width/height ranges and grass terrain texture tile sizes use 1.5/3.6. No textures or meshes are duplicated. `TerrainLayerVegetation.RefreshPrototypeDimensions` applies sizes on enable without regenerating density/harvest cells; it also has an explicit context menu.
- Outdoor camera zoom sizes 5/6.5/8, follow distance 15, focus offset Y 0.75. Barn follow distance 18, coop 15; fixed house views inherit the house owner scale. Camera pitch/yaw and screen UI remain authored.

`manifest.json` lists migrated assets. `Before/` contains the exact pre-migration file bytes, including previously uncommitted work. `Tools/WorldProportions/apply.py` refuses repeat application. Re-running older art setup/rebuild menus may restore their old authoring dimensions; inspect proportions after intentionally rebuilding artwork.

Validation: runtime and editor C# compile; Unity renderer-bounds assertions for player, cow/chicken/horse/dog, house Lv1 and human dummy; terrain detail size update preserves painted/harvested cells and is idempotent. A separate preview project is used so the main Unity session is not closed or reloaded. Full gameplay transitions in the user's current open scene are not exercised by this fixture. Stop Play and reopen the Map scene after saving any desired scene edits to load the revised serialized camera/scene values.

The comparison image orders player, chicken, cow, dog, horse from left to right. It uses temporary built-in preview materials because the isolated validation project is not running the main URP renderer; production materials and shared textures remain unchanged.
