# Barn Interior A — four approved stages

Modeled in Blender 4.5.14 LTS on 14 October 2026 after approval of `Documentation/Design/BarnInteriorReview/SKETCH_SPEC.md`. Same Toon Farm Pack atlas, red and cream barn palette, flat stylized geometry and Chicken Store lantern construction as the existing exterior assets. No animals included.

**Unity integration completed 14 October 2026.** See `Unity/README.md`, native renders and validation reports. The source modeling counts below remain Blender counts; Unity runtime meshes add 10/10/20/20 triangles for the reverse side of open hopper surfaces.

## Deliverables

- `Blender/BarnInterior_A.blend`: packed atlas; four editable layout scenes plus `00 MODULAR KIT`. Origin is the entrance centre, Blender +Y toward the rear, Z up; metres.
- `Exports/BarnInterior_Lv1..4.fbx`: complete visual layouts, with root group and separate components/markers.
- `Exports/N*.fbx`, `Exports/A*.fbx`: individual new/adapted modules at their local origin. Source pack assets are reused in layouts; original pack FBXs are not overwritten.
- `Previews/Lv1_Overview.png` through `Lv4_Overview.png`: actual Blender renders.
- `Previews/MeshManifest.json`, `Verification.json`, `FBXRoundTrip.json`: measured geometry and export checks.
- `References/atlas-audit.json`: original PSD hash/GUID plus UV palette rectangles. Blender packs the existing PNG composite; no new runtime texture.

| Stage | Clear floor | Triangles including repeated props/fill | Blender mesh vertices including instances | Estimated vertices after flat normals/UV splits |
| --- | --- | ---: | ---: | ---: |
| Manual | 12 × 10 m | 5,918 | 5,500 | 13,100 |
| Motor | 16 × 12 m | 7,592 | 6,978 | 16,494 |
| Auto | 20 × 14 m | 9,584 | 8,580 | 20,320 |
| Process | 24 × 16 m | 11,792 | 10,262 | 24,594 |

All layouts use one atlas material. These are geometry counts, not device performance measurements. Final Unity vertex counts may vary with importer settings. Use only the active stage at runtime; instance repetition shares mesh data in the editable .blend. Static batching/mesh grouping, collision, lighting and device profiling belong to Unity integration. Current editable layouts have 60/81/115/140 mesh objects, so draw calls still need attention at integration.

## Components and reuse

New: two-metre wall/window modules, posts and braces; 3.6 × .9 × .65 m three-compartment trough; separate fill; manual/motor/large chopper family; mixer; straight/elbow/T/coupler pipe kit, supports and dispensers; shared process tank used twice.

Reuse: original `TFP_Wood_Barrel_01A`, `TFP_Bucket_Metal_01A`, `TFP_Wooden_Crate_01A`, `TFP_Cereal_Sack_01A`, `TFP_Milk_Bottle_01A`, `TFP_Spading_Fork_01A` source meshes/UVs. Lantern geometry follows the existing Chicken Store builder. The imported bucket FBX mesh has 208 triangles; its audited Unity prefab contained 252, so counts here are measured from this layout, not copied from the prefab inventory.

Adapted/new fixtures in the same palette: storage rack, cabinet, water basin, sink, counter, tool board. Repeated hay bales use an 84-triangle substitute rather than the original 888-triangle pack bale. Straw is sparse flat geometry; no dense strands, alpha grass carpet, or realtime light per lantern.

Trough fill and visual sockets are separate objects. Pipe outlets connect above the rear trough compartment. Front is a cutaway with a two-metre entrance. Windows face into the room; no roof is supplied for this interior camera.

## Validation and integration boundary

Reopened saved .blend, checked all four scenes: zero degenerate faces and zero invalid UV coordinates. All four FBXs round-trip with matching triangle/object counts, populated materials, and matching footprint/height bounds. New/adapted modules are also exported independently. Lights and cameras are preview-only and excluded from FBX.

The subsequent approved Unity apply replaces the existing BarnInterior layout prefabs and binds their authored feed/animal sockets in `BarnInterior.unity`. Map, navmesh, storage capacity and save data are unchanged. Feed capacities remain 4/8/14/20; the 6/9/12/15 visual compartments do not change capacity. The controller uses portion markers and shared fill visibility without generating legacy trough cubes for these four layouts. Coop retains its original fallback. Process tanks have no locked product function or simulated fluid.

## Rebuild

Run the existing Blender executable with `--background --factory-startup --python ArtSource/BarnInterior_A/Scripts/build_interior.py`. To render and audit the saved file, run `Scripts/review_interior.py -- --render`; export round-trip audit is `Scripts/verify_exports.py`. Factory startup avoids unrelated installed add-ons. Source modeling and review scripts never write Unity Assets or alter existing .blend files.
