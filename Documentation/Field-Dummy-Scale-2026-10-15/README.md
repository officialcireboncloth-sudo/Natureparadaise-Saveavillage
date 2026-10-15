# Field, ground texture and vendor dummy dimensions

The player remains at the approved 1.5 m reference height.

| Setting | Before | After |
| --- | --- | --- |
| Six shop dummy capsules | 1.5 m high, 0.525–0.75 m wide | 1.2 m high, 0.4 m wide |
| Main Map field cell | 2 m | 0.8 m |
| Main Map field footprint, 15 × 15 cells | 30 × 30 m | 12 × 12 m |
| Preview/testing field cell | 3 m | 0.8 m |
| Empty field texture repeat | 8 m | 2 m |
| Hoed soil texture repeat | 2 m | 0.8 m |
| Field edge transition | 0.4 m | 0.12 m |
| Field outer corner radius | 1 m in Map | 0.2 m |
| Watered soil edge transition | 0.13 m | 0.05 m |
| Actual Terrain TL_Dirt repeat | 16 m | 4 m |
| Actual Terrain TL_Sand repeat | 12 m | 5 m |

Only body/collider children and world-label positions change in the six vendor prefabs. Shop UI owners keep unit scale. Existing prefab-instance overrides in Map, Map_GameplayPreview and TestingScene are updated where applicable. The three shop setup commands use the new body dimensions when rebuilding shops.

FieldArea defaults and the shared Field Soil Visuals profile use the new dimensions. Field row/column counts, IDs and tile indices stay unchanged. Authored source-plane scales and box colliders match the resized grid before Play; runtime also synchronizes these. The green farming outline and tile meshes already derive their dimensions from FieldArea.CellSize. Tile feather remains 0.065 of the cell, producing a 0.052 m transition.

The terrain layer inspection used copies of all four Map TerrainData assets and resolved their actual shared layer references. TL_Dirt and TL_Sand are shared, so their finer pattern also appears wherever those layers are used. No terrain height, splat painting, vegetation density, texture pixel data, or soil palette changes are made by this revision.

## Validation

- Runtime and editor C# compilation passed. Existing unused-field warnings remain.
- `Tools/WorldProportions/verify_field_dummy.py` passed: actual Map vendor instance overrides and ancestor scales, all three scene grids, authored colliders and source planes.
- Separate Unity project: all six prefab renderer/collider bounds, feet at ground level and unit-scale UI owners passed.
- Unity Play Mode fixture using actual Map grid dimensions: all 225 cell centre/world-coordinate round trips, neighbour spacing and outside-boundary rejection passed.
- Hoe/save/restore at both grid corners retained tile indices and rebuilt the three expected soil meshes. Mesh width, field and tile shader parameters, UV repeat and actual terrain layer sizes passed.

`Unity-Validation.txt`, `Scene-Validation.txt` and `Validation/` retain the relevant evidence/fixture. This verifies serialized Map settings and isolated runtime behavior; the user's full open Map gameplay session was not restarted. Exit Play and reopen Map to load serialized scene changes, preserving any desired unsaved editor work first.

`Before/` contains the exact pre-edit bytes for assets processed by the spatial refinement script, including earlier uncommitted work. `manifest.json` lists those files. The profile/default and two terrain-layer changes are recorded by the table and normal Git diff.
