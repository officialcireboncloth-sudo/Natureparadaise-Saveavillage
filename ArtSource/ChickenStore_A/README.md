# Chicken Store A

Built in the user's open Blender session after sketch approval, 14 October 2026. Exterior of the main shop plus the small red companion coop. Source: `Documentation/Design/ChickenStoreReview/reference.png` and the approved specification in `References/ApprovedSpecification.md`.

Open `Blender/ChickenStore_A.blend`. The initial user scene is retained as `Original Scene - preserved`. Scene `00 Chicken Store + Coop Overview` presents both buildings with linked mesh copies. For edits use `01 Chicken Store - Editable` and `02 Coop Companion - Editable`, each with `01 EDITABLE` component collections. `02 EXPORT` contains the merged runtime geometry, hidden in the viewport and renders to avoid duplicates. `90 PREVIEW ONLY` lights, camera and floor are excluded from exports. Overview offsets are only for presentation.

| Asset | Body W × D | Height | Full visual W × D × H | Triangles |
|---|---|---|---|---|
| ChickenStore_A | 8.00 × 6.00 m | 5.60 m | 8.96 × 7.643 × 5.60 m | 13,772 |
| ChickenCoop_Companion_A | 3.00 × 2.80 m | 3.10 m | 3.50 × 3.84 × 3.10 m | 2,962 |
| Optional weather vane | — | top 3.443 m from ground | separate ornament | 128 |

Visual bounds include roof fascia, foundation, awning and ramp. Origin is ground center; Blender X right, Y back, Z up, front faces -Y. Scale 1 unit = 1 m. No geometry below ground.

The shop includes the intersecting double front gable, staggered roof tiles, twin chimneys, cream timber walls, striped scalloped canopy, empty built-in counter, windows, lanterns, closed service door, and mesh sign `LIVE CHICKEN STORE` with chicken relief. The main door is a separate mesh under a hinge at Blender (-0.31, -3.07, 0.19); rotate the hinge around Z. The coop includes nest boxes, empty dark recesses, slatted arched vent, rear maintenance hatch, side vents and ramp. The weather vane is independently removable and exported separately. Full interiors, animation, livestock, merchandise, plants and loose props are outside this exterior build.

All runtime meshes use one material, `TFP_Atlas_1A`. UVs sample ten audited safe regions of the existing Toon Farm Pack atlas (GUID `522fcb2b5717c7640b460ee9ff73a78a`). No new runtime texture. The PNG in `References` is the existing atlas composite packed for Blender portability; the original Unity PSD and material stay unchanged. For Unity, remap imported material to the existing `TFP_Atlas_1A.mat` rather than importing that preview PNG. Glass is opaque amber atlas color.

Three FBXs are in `Exports`. The shop FBX preserves its separate door hinge and leaf. A separate `FBX_AxisReference` empty preserves Unity's synthetic file-root transform. Unity axis conversion remains on child nodes; follow the prior farm integration convention rather than assuming Blender and Unity coordinates match. Native Unity integration has not been performed for this build. Reuse the existing modular farm fence kit.

`Previews/Verification.json`: 128 passing checks covering geometry, ground contact, approved heights, shared material, UV bounds, nondegenerate triangles, door hinge behavior, original PSD hash, packed atlas, and independent FBX reimports with matching bounds and triangle counts. Five rendered views are in `Previews`.

Rebuild: run `Scripts/build_chicken_store.py` with Blender Python. `LIVE_BUILD=True` preserves existing scenes; run once in a fresh live scene to avoid duplicate builds. A background rebuild retains the original `Scene` from the previous output. `Scripts/verify_chicken_store.py` and `Scripts/render_chicken_store.py` read the saved blend without saving over it. The common mesh helpers are frozen from the previous farm build and stored in `Scripts/mesh_common.py`.

