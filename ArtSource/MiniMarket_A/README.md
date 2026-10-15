# MiniMarket A — mobile exterior

Unity status: subsequently imported and saved in Map as `MiniMarket_Group`. Details: `ArtSource/ShopBuildings_Unity/README.md`.

Built in the user's open Blender session after approval of `Documentation/Design/MiniMarketReview/A-MiniMarket-sketch.png`. The user requested low vertex count for phones. `Blender/MiniMarket_A.blend` opens scene `MiniMarket A - EDITABLE`. Previous scenes and their existing edits are retained in that file; `References/BeforeMiniMarket_LiveSnapshot.blend` also preserves the session immediately before this build. The original ChickenStore file was not overwritten.

Runtime geometry: **2,910 triangles, 2,106 mesh vertices, one shared opaque atlas material, three meshes** (static building and two door leaves). Estimated vertex count after flat-normal and UV splits is recorded in `Previews/Verification.json`; this is more representative of engine vertices than Blender's welded mesh count. Actual phone frame rate still needs Unity profiling on target hardware; this stage validates the asset geometry only.

`MM 01 EDITABLE components` contains editable component meshes and independent door hinges. `MM 02 EXPORT - hidden duplicate` contains merged static geometry and the two hinged leaves. It is hidden from viewport/render to avoid double geometry. `90 PREVIEW ONLY` contains floor, studio lights and camera; those are excluded from FBX. Viewport statistics include the preview floor's two triangles when it is visible.

Body is 10 × 7 m, roof deck Z=3.95 m, crest max Z=4.85 m. Main coping Z=4.30 m with corner caps Z=4.35 m. HVAC top Z=4.65 m. Actual complete visual envelope including both side awnings, front canopy, coping and trim is **11.78 × 8.48 × 4.85 m**. The original sketch envelope omitted the small left shutter awning and final mounting offsets. Source axes: X right, Y back, Z up, front -Y; 1 unit = 1 m; origin at ground center. Both entrance hinge pivots are at X=±0.9, Y=-3.56, Z=0.15 m. Rotate hinges around Z for future door motion; no animation is included.

Mobile choices: continuous wall/parapet and roof volumes; panel and mortar lines as thin quads; no individual roof tiles or stone blocks; no subdivision/global bevel; flat low-resolution lettering; six-sided lamp shades; no runtime lights; opaque amber windows; no modeled interior. Roof HVAC, permanent lights, SUPERMARKET lettering and leaf emblem are included. Plants, planters, merchandise, crates, terrain and paving are excluded.

`TFP_Atlas_1A` is the existing Toon Farm Pack atlas material (PSD GUID `522fcb2b5717c7640b460ee9ff73a78a`). Original PSD SHA-256 is unchanged. Eleven audited swatches include olive coping/awning and leaf colors. No new runtime textures. The existing composite PNG is packed for Blender portability only. On Unity import remap the FBX material to the existing `TFP_Atlas_1A.mat`; do not import the preview PNG as a new game texture.

`Exports/MiniMarket_A.fbx` includes static body, separate door hinges/leaves and an identity file-root reference empty. Unity axis conversion remains on child nodes, consistent with previous farm exports. Native import and Map placement have now passed validation.

`Previews/Verification.json` contains geometry/material/UV/height/budget checks, hinge checks and an independent FBX reimport with matching bounds and triangle count. Front, front-right and rear-left previews are rendered separately without overwriting the blend.

Scripts: `build_minimarket.py` rebuilds from approved dimensions and atlas data; use `LIVE_BUILD=True` once in an existing Blender session to preserve open scenes and save a snapshot. Default background execution creates a standalone asset scene. `verify_minimarket.py` and `render_minimarket.py` read the saved file without saving over it. Runtime export counts exclude other preserved scenes and editable/export duplicates.
