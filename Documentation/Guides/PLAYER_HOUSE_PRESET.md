# Player house — Toon Farm scene assets

The house uses `TFP_House_01A_Preset_2` from Toon Farm Pack. Its objects are saved in Edit Mode; no installation or model generation is required when entering Play Mode.

## Editing

- In Map, expand `30_WORLD / Buildings / PlayerHouse_Editable / HouseExterior_Lv1_Editable`.
- Open `Assets/Nature  Paradaise/Map/Scenes/Interiors/HouseInterior.unity` additively to inspect the interior alongside Map. Expand `HouseInterior_Editable / InteriorLayout_Lv1_Editable`.
- Select an object and press F in Scene View to frame it. Save the scene after editing.
- Levels 1–4 have separately editable scene objects. They currently use the same exterior preset and interior footprint. Only one level should be active at a time; the existing controllers select the level during gameplay.

## Interior

The supplied preset interior is adapted as a ground-floor cutaway for the existing top-down camera. Upper-storey renderers and stairs are inactive; modular perimeter walls and the bed are authored directly in the scene. Furniture interaction components are retained on the scene's bed, TV, storage, refrigerator, kitchen (levels 2–4), and aquarium. Older standalone BedDummy and TVDummy objects are inactive.

`Use Authored Scene Layout` keeps the entrance transforms and furniture as authored instead of applying the old dummy-layout positioning. House setup utilities skip authored layouts when updating the kitchen, refrigerator, and aquarium.

The four room layouts are now twice as wide and twice as long (about 24.45 × 36.46 world units). Furniture keeps its original world size under `Furniture_Editable`; edit this container's children to reposition it. Entrance and exit positions follow the enlarged footprint.

All 100 active wall sections across the four layouts now have a consistent 3.6-unit height, with their tops at Y=3.61. Door models keep their original size. Props on tables, cabinets, racks, and stacked crates preserve their original relative spacing after the footprint expansion; standalone floor props sit at floor height. Frames, mirrors, shelves, lamps, and other wall fixtures sit against the walls and below their tops. The large flower pot rests on the upper wooden crate. Checks covered 112 supported items, 176 floor items, and 56 wall fixtures, with unchanged furniture scales. These placements are saved scene transforms and require no runtime placement script.

The former stair footprint has a saved `ToonFarm_Interior_Cutaway_Editable / Floor_StairOpeningPatch_Editable` in each level. It uses the pack's closed wood floor and full BoxCollider at ground-floor height, slightly beneath adjacent floor surfaces to prevent overlapping coplanar rendering. Coverage passed 1,600 collider samples and 12 CharacterController drop checks across the four layouts. The inactive upper floor remains inactive.

Each `Furniture_Editable / Bed_MeshSlot` has `SleepPose_Editable` (player root position/facing for the bed animation) and `WakeStandPoint_Editable` (standing position beside the bed). PlayerBed passes these anchors to the sleep sequence. CharacterController is suspended during the bed pose, then restored at the standing point; the save records the standing position. The sampled Wake Up Bed pose was checked with head, hips, and feet inside the mattress footprint, and the standing point was checked against the floor collider.

Interior lighting is constant and uses the interior directional light. Outdoor precipitation, fog, cloud shadows, and wetness are suppressed inside, and thunder no longer flashes scene lighting. The portal reuses an already loaded interior, allowing additive Edit Mode previews without loading overlapping copies during gameplay.

The entrance/exit portals keep their existing scene and spawn IDs. Map, Map_GameplayPreview, and TestingScene contain the replacement exterior. Vendor prefabs are unchanged.

Door interaction measures distance to the door collider (including triggers), using the player capsule center. It no longer depends on the raised door pivot or the farming interaction tile. Geometry checks passed at scales 1, 1.75, and 3; Play Mode accepted the current edited door and completed entry/exit. DialogueService and QuestService detach from their scene parent before becoming persistent, resolving their DontDestroyOnLoad warnings. Separate TerrainCollider warnings come from Oak tree prototype MeshColliders, which need supported primitive colliders for terrain-tree collision; those tree assets were not changed here.

## Validation

Unity imported and saved the scenes. The four interior layouts were checked for missing script/material references; the entrance was checked against the floor collider. Furniture scale checks passed for 199 roots per level. Play Mode checked 77 weather/time combinations for constant indoor light and hidden weather effects, plus the single-instance interior transition and restoration of outdoor rain/sun on exit. Furniture interactions and upgrades have not been exhaustively tested. A separate existing AnimalController.OnDisable exception accesses a destroyed Inventory when stopping Play Mode; it is outside the interior changes.
