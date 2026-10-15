# Chicken Store A — mobile variant

Unity status: subsequently imported and saved in Map as `ChickenStore_Mobile_Group`, including existing modular perimeter and chicken-pen fences. Details: `ArtSource/ShopBuildings_Unity/README.md`.

Requested by the user to reduce the roughly 12k vertices visible in the original Blender overview. `Blender/ChickenStore_Mobile_A.blend` opens the new scene `00 Chicken Store MOBILE Overview`. Original scenes and unsaved edits present during execution are retained; `References/BeforeMobile_LiveSnapshot.blend` also preserves the pre-build session. The original `ArtSource/ChickenStore_A` asset remains available.

| Runtime asset | Mesh vertices | Triangles | Estimated vertices after flat-normal/UV splits |
|---|---:|---:|---:|
| Main shop, including separate door leaf | 3,387 | 4,249 | 7,843 |
| Companion coop | 1,029 | 1,406 | 2,808 |
| Optional weather vane | 68 | 128 | 204 |
| **Total** | **4,484** | **5,783** | **10,855** |

Blender's mesh vertex count and the engine vertex count differ because normal/UV discontinuities split vertices. This is an estimate before native Unity import; device performance needs Unity profiling. Preview floor, preserved older scenes, hidden export copies and editable component duplicates are not counted in the runtime totals. The floor adds 4 vertices/2 triangles to visible scene statistics if enabled.

The former runtime total was 16,862 triangles (shop 13,772, coop 2,962, ornament 128); the mobile variant removes about 66% of those triangles. Against the user's roughly 12k visible mesh vertices, the new 4,484 represents approximately 63% fewer.

Changes: roof shingle solids replaced by continuous roof surfaces and thin seam quads; exact analytical roof intersections and valleys retained; foundation stone solids replaced by one plinth plus surface joints; global bevel removed from frames, counter, foundation and door; sign lettering made planar at low curve resolution; coop planks widened. The seam pattern is coarser and roof relief/edge softness is reduced. Building dimensions, silhouette, canopy stripes, logo, lanterns, windows, counter, nest boxes and ramp remain. Shop max height 5.60 m, coop base height 3.10 m, optional vane top 3.443 m; runtime bounds match the original asset.

One existing `TFP_Atlas_1A` material, no new runtime texture. Atlas PSD hash is unchanged. Main door remains separate with a functional hinge. Runtime exports in `Exports`: `ChickenStore_Mobile_A.fbx`, `ChickenCoop_Mobile_A.fbx`, and the optional vane. Preview lights, cameras and floor are excluded. No Unity scene or prefab was changed for this optimization.

For editing use `01 Chicken Store MOBILE - Editable` and `02 Coop MOBILE - Editable`. Source component collections start `01 EDITABLE MOBILE`; export copies remain hidden. Original scenes keep their old names. The combined overview is a linked presentation copy; edit the source scenes for asset work.

`Previews/Verification.json` records 128 geometry/UV/material/hinge and FBX roundtrip checks. The verification script additionally enforces a combined budget below 6k triangles and unchanged original bounds. Preview images show the simplified asset from front and back.

`Scripts/build_mobile.py` derives the variant from the approved original builder through named function replacements and writes the concrete generated builder to `Scripts/generated_mobile_builder.py`. Use `LIVE_BUILD=True` once per session to preserve existing scenes and take a snapshot. Background mode creates the variant independently. `verify_mobile.py` and `render_mobile.py` reuse the earlier checks/views with the mobile asset names.
