# Market Stand and Shipping BIN — Cozy rework

2 November 2026. Visual target: the right-hand models in `Source~/MarketPropsCozy/MarketProps_Cozy_Target.png`.

## Applied artwork

**Market Stand**: a continuous concave green/cream cloth canopy with a rounded front hem, hanging side edges, a gathered rear corner and rope bindings. The underside rafters no longer intersect the sagging cloth. Timber posts and knee braces are thicker; the shelf is raised, crate handles and fasteners are modelled, and wood/fabric UVs use the shared Cozy atlas. Original Toon Farm tomato, carrot, cabbage and eggplant meshes are retained. Their colour UVs are remapped to the corresponding food/leaf palette slots, positioned above the crate lip, and kept inside its front/back walls.

**Shipping BIN**: a slightly broader chest, thicker dark corner posts, softened wood edges, end-grain caps, iron straps/rivets and the existing carved harvest relief. The interior and underside of the lid use the same atlas. The lid is separate geometry with a rear-edge pivot, opening to 72 degrees; the existing storage interaction still opens/closes it.

Both props use the existing `NP_CozySurfaces_01` material and its colour/normal/metal-smoothness atlas. No new gameplay textures or materials were created.

## Source and export

- Actual editable stand master: `Source~/MarketStand/MarketStand_A.blend`.
- Actual editable BIN master: `Source~/ShippingBin/ShippingBin_A.blend`.
- Existing FBX paths in `mesh/World/MarketStand` and `mesh/World/ShippingBin` are retained.
- Reproducible authoring: `Source~/MarketPropsCozy/build_market_props_cozy.py`. The original per-prop builder entry points now call this revision.
- Versioned `Source~/MarketPropsCozy/Library` files contain the original editable component library, including the vendor produce. They are authoring-only inputs, not Unity runtime assets.
- The editor normalizes the FBX coordinate frame using explicit exported right/front/up markers, retaining metre scale and avoiding mirrored artwork.

Apply menus remain **Nature Paradise > Art > Apply Atlas Market Stand** and **Apply Atlas Shipping Bin**. These update the existing visual and gameplay prefabs; the original gameplay component, identity, inventory reference and stored sale settings are preserved.

The BIN master opens with its editable lid raised. Exported geometry has a closed rest pose; the Unity pivot controls the opening. Export meshes and preview lights/camera are separated from editable component collections.

## Runtime size and budget

- Stand: approximately 2.78 m wide × 2.23 m high × 1.63 m deep after the existing 0.8 visual scale. 17,423 near triangles; the far LOD is about half that. One renderer/material draw per selected LOD.
- BIN: approximately 1.76 m wide × 0.97 m high × 1.09 m deep, closed. About 10.3k near triangles, with a smaller far LOD. Two renderers/material draws per selected LOD, for body and articulated lid.
- FBX CPU readability is disabled after normalized mesh extraction. Shared normalized meshes are stored in the respective `*_Cozy_Runtime.asset` files.
- Existing world-size normalization stays in place. Only the BIN collision width/depth is adjusted to cover its new body; the canopy and opened lid do not introduce additional blocking colliders.

Actual Android/iOS frame rate and a mobile build have not been measured during this art revision.

## Native Unity validation

Unity 6000.0.81f1 in the isolated CleanClone fixture checked import coordinate frames, native versus Blender bounds/triangle counts, UV validity, shared shader/material bindings, reduced far LOD geometry, compact world dimensions, and preservation of gameplay data/root placement. BIN tests covered the 72-degree opening, clearance above the interior, stationary body, and return to its closed rest pose.

The actual Map scene resolves both new prop prefabs. Its serialized file was not rewritten. The existing shared shader/materials and world scenes were also checked unchanged when installing the final assets into the main project.

See `MarketPropsCozyReview` for the native Unity screenshots and audit reports: 16 stand checks, 26 BIN checks, and 3 scene-binding checks. Review screenshots are not referenced by gameplay prefabs. Local pre-change backups are in `ArtSource/MarketPropsCozy/Before`.
