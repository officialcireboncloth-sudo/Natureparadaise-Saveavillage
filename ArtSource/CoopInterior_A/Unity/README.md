# Coop Interior A — Unity integration

Applied 15 October 2026 to the existing `CoopInterior.unity` and four existing Coop interior prefab paths, preserving their asset GUIDs. The existing Coop portal still targets `CoopInterior` / `coop-interior-entry`; the scene remains in build settings. This separate interior scene is loaded by the portal; it is not a duplicate room placed on the terrain in Map.

| Level | Capacity / feed portions / animal markers | Triangles | Native Unity mesh vertices | Renderers before → after |
| --- | ---: | ---: | ---: | --- |
| 1 | 6 | 4,654 | 8,727 | 48 → 14 |
| 2 | 9 | 5,270 | 9,947 | 56 → 18 |
| 3 | 12 | 8,218 | 15,171 | 81 → 24 |
| 4 | 15 | 9,726 | 17,859 | 95 → 28 |

Native vertices are higher than Blender vertices due to imported face normals/UV splits. Triangles are unchanged by import/combine. Counts include complete authored room, machine props, hood, all removable grain fills; exclude animals. Only one layout is active. Source FBXs have CPU readability disabled after mesh extraction. Runtime meshes are grouped by shell, props, equipment, distribution and separately editable trough/fill groups; hood is separate transparent material.

## Saved assets

- `Assets/Nature  Paradaise/Map/Scenes/Interiors/CoopInterior.unity`
- `Assets/Nature  Paradaise/Prefabs/Coop/Interior/CoopInteriorLayout_Lv1.prefab` through Lv4
- `Assets/Nature  Paradaise/mesh/Buildings/CoopInterior_A/`: four imported FBXs, four runtime mesh asset containers and local `CoopIncubatorGlass.mat`
- `Assets/Nature  Paradaise/Resources/Buildings/Coop Building.asset` and its four matching capacity rows in `Data/Balance/Settings/Buildings.csv`: capacities 6/9/12/15, matching the user specification. Costs and construction requirements are unchanged by this integration.

Authored sizes remain 10×8 / 12×10 / 14×12 / 16×14 metres, root scale one, entrance at origin. Layouts occupy the existing isolated interior region at `(0,-200,-10)`. No extra Barn 1.75 multiplier. Entry spawn, exit marker and camera reference were retained and aligned. Old placeholder slot roots are disabled, preventing runtime primitive duplicates.

`BarnInteriorLayout` is the shared binding component already used by the shared Barn/Coop controller. Feed maker socket, feed portions, removable fill renderers and animal spots are bound per level. Existing gameplay handles stock/feeding and chooses the active level. The shared controller's no-home fallback now uses authored slot/animal counts when a binding exists; unbound legacy layouts retain their old fallback.

Opaque geometry uses existing `TFP_Atlas_1A.mat`. The hood uses a local copy of existing `TFP_Glass_Windows_1B` shader/settings, with the window-art bitmap removed, a pale tint at alpha .19, backface culling and no depth writes or shadow casting. No new runtime texture or global pack material edits. `Unity_Lv4_HoodDetail.png` confirms all three eggs remain visible through the hood in native URP rendering.

## Validation scope

`UnityApplyResult.json` and `UnityImportAudit.txt`: import orientation/dimensions, exact source triangle preservation, hood material, expected trough/head/egg counts, feed slots, empty/full/consumed fill state, collision-clear animal markers, finite vertices, UVs and normals. `SavedAssetValidation.json/.txt`: follow-up checks of saved prefabs/scene, portal spawn, level references and source import settings, plus shared-controller compatibility checks.

Native Editor preview images are `Unity_Lv1.png` … `Unity_Lv4.png` and `Unity_Lv4_HoodDetail.png`. Fills are intentionally shown for these previews; actual prefabs start with empty fills and stock drives visibility. Preview lighting is a test rig, not final Map lighting. Review opens level 4 in an isolated Prefab stage; the interior scene is closed after validation so its controller does not start alongside Map before entering the portal.

No Play Mode walkthrough, phone FPS profile or completed incubator/mayonnaise/automatic-feeding gameplay is claimed. The new machines are visual assets; existing feed maker interaction is connected through the shared controller. Save files were not edited or animal residents removed; a legacy save already containing more residents than the new lower capacities is not migrated by this visual integration.

Backups exist under `ArtSource/CoopInterior_A/Revisions/`; the earliest `20261015_070505345` directory from the first apply contains the original placeholder prefabs/scene and prior capacity data. Map was not saved by the importer. `Attempt1_CsvLineEnding_Error.txt` and `Attempt2_OldAssembly_Error.txt` record resolved setup attempts; current success is determined by the PASS JSON reports.
