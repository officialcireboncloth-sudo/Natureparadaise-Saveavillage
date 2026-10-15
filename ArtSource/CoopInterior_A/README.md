# Coop Interior A — modeled 15 October 2026

Approved four-stage mobile kit, built in the user's open Blender 4.5.14 LTS session (`gui-build-status.txt`). Main file: `Blender/CoopInterior_A.blend`. Initial unsaved session was preserved as `Blender/BeforeCoop_UnsavedBackup.blend`. The scene selector contains the modular library and four layouts. Modeling/export stage completed; Unity integration was subsequently applied on 15 October 2026. See Unity/README.md and Unity/SavedAssetValidation.json for current integration status.

| Layout | Capacity markers | Floor metres | Triangles | Source mesh vertices, repeated instances included | Palungs | Auto heads | Incubator eggs |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: |
| Lv1 | 6 | 10 × 8 | 4,654 | 3,612 | 2 | 0 | 1 |
| Lv2 | 9 | 12 × 10 | 5,270 | 4,122 | 3 | 0 | 1 |
| Lv3 | 12 | 14 × 12 | 8,218 | 5,892 | 4 | 4 | 1 |
| Lv4 | 15 | 16 × 14 | 9,726 | 6,874 | 5 | 5 | 3 |

Counts exclude animals and preview camera/lights; include floor, walls, props, glass, fills and repeated modules. These are Blender source counts, not Unity split vertex counts. Targets 5k/6.5k/9k/10k triangles pass. No phone performance claim is made before integration/profiling. Source layout objects remain modular with linked meshes. Unity integration should combine static meshes by material/spatial group to reduce renderers (48/56/81/95 source mesh objects); avoid blindly making each source object its own renderer. Only one level should be active.

## Modeling and reuse

- Reused Barn shell geometry, timber language, pipe parts and chosen original Toonpack sacks/crates; reused original whole egg `TFP_Raw_Egg_01A` and recolored jar `TFP_Jar_01A` through the same atlas.
- Adapted Barn mechanical forms into low-poly grain feed makers (small and large). Hopper/mixer bowls are newly authored closed radial profiles after reviewing the Toonpack bowl candidates; no need to import their extra geometry.
- Authored low three-compartment chicken trough, independent feed fills, incubators (one and three cups), transparent cover, mayonnaise mixer, auto hopper/bell heads, chicken drinker, timber tables and perch.
- Lv3/Lv4 header, front-running branches and drops form connected geometry; each bell aligns over a trough. Header centre 2.4 m, head base 1.5 m. Main supply connects feed maker side to the header.
- Two inline 3-slot troughs in Lv1 provide six spaces. Higher levels have 3/4/5 three-slot modules. Animal and feed sockets are empties, not new animal models or gameplay code.
- Sparse floor straw is flat geometry. Grain is a simple removable fill; no individual kernels, subdivision surfaces, dense bevels or realtime machine lights.
- Source geometry is triangulated intentionally for export, welded at coincident seams where appropriate and normals recalculated. A reused window module had four >2-face junction edges; the Coop version was repaired with one closed frame ring. Original Barn file stays unchanged.

## Material

Opaque objects use existing `TFP_Atlas_1A`, GUID `522fcb2b5717c7640b460ee9ff73a78a`, verified PSD hash and packed Blender preview image. New objects deliberately sample safe palette cells using constant UVs; zero-area UV islands are expected for this palette method. No runtime texture was added.

`Coop_Hood_Glass_ExistingPalette` is a local Blender transparent approximation (alpha .19) with existing Unity `TFP_Glass_Windows_1B` as the documented candidate. It must be remapped and checked in native Unity on integration; FBX does not guarantee transparent shader settings. Hood geometry is one closed cover volume, no nested duplicate shells.

## Validation and files

`Previews/TopologyAudit.json`: independent audit of saved .blend, **PASS**. Checks: no zero-area/duplicate faces, loose edges/vertices, >2-face junction edges, inconsistent shared-edge winding, nonfinite coordinates, non-triangle faces, missing UVs or UVs outside 0–1. Capacity/trough/head/egg/socket counts, positive scales and per-level triangle budgets pass. Open boundaries in source props (e.g. container openings), Barn decorative seams and flat floor straw are reported rather than filled. The audit does not claim Boolean-unioned assemblies, comprehensive self-intersection tests or Unity transparency/FPS validation.

`Exports/CoopInterior_Lv1..Lv4.fbx`: full layouts. Other FBXs are separate reusable modules. Preview camera/lights are excluded. Metres; entrance origin; Blender +Y rear and Z up; FBX forward -Z/up Y. Coop sizes are final authored sizes, do not additionally apply the previous Barn 1.75 factor.

`Previews/review.html`: four layout renders and equipment closeups. `MeshManifest.json`: measurements and source provenance. `References/atlas-audit.json`: atlas provenance. `Scripts/build_interior.py`: reproducible builder; `run_in_open_blender.py`: GUI wrapper; `verify_interior.py`: saved source audit; `render_review.py`: preview only, never saves geometry.

Unity integration now reconciles Coop gameplay and balance CSV capacity to the approved visual capacity 6/9/12/15; see Unity/README.md. Incubator one egg Lv1–3 remains the approved sketch proposal; Lv4 explicitly has three eggs. Modeling does not implement recipes, timers, automatic feeding or incubation logic.

