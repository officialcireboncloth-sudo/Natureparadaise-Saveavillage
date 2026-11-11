# G10 radial flowers

Sunflower, Chrysanthemum, Dahlia and Marigold each have Seed, Sprout, Young, BudForming, Ready and Harvest visuals. The new batch follows the existing species plan and the accepted vivid, sculpted direction from G09. No new G10 growth-reference images were attached for this batch.

Open `Blender/G10_RadialFlowers_Master.blend` for all 24 editable phase roots. Closed cupped petal shells have physical thickness; leaf silhouettes are species-specific, including lobed chrysanthemum leaves, compound dahlia leaves and pinnate marigold foliage. Sunflower disks have raised spiral florets. Flower stems end at their calyx with a continuous final axis. Harvest sprigs have separate ground pivots.

The independent `NP_Plants_RadialFlowers_01` atlas contains 16 stable 512px regions in a 2048px page. BaseColor, registered tangent Normal and SurfaceMask each have an editable GIMP XCF under `ArtSource/Crops/Textures/`. SurfaceMask red is zero metallic; alpha stores per-surface smoothness. The existing Cozy shader uses one material per renderer.

Runtime FBX, explicit mesh data and native Unity meshes are under `Assets/Nature  Paradaise/mesh/Crops/G10/`. Twenty-four visual prefabs under `Assets/Nature  Paradaise/Prefabs/Crops/G10/` each contain two LODs. Near geometry is capped at approximately 26,000 triangles and distance geometry at 2,800; these budgets have not been profiled on a mobile device.

See `Blender/UnityVisualBindings.json` for stage mapping and `Review/ArtAudit.json` for physical mesh, UV, packed texture, buffer and FBX round-trip checks. `Review/GimpReopenAudit.json` records the reopened painting masters. Actual Blender renders are in `ArtSource/Crops/Previews/G10_*.png`. Ready, growth, volume and harvest reviews render the exported near mesh buffers with the production material; isolated flower closeups show the editable source geometry.

G10 is now applied to gameplay profiles, seed/produce/mutation items and the existing seed store, bringing the integrated total to 25 species. The preceding 21 profiles and balances, accepted models and interaction hints are preserved. Seed UI uses the same paper packet drawing function as earlier crops. See `ArtSource/Crops/Integration/G10_Completion.json` for current binding and preservation checks. Unity visual Play Mode validation is separate and has not been run by the agent.

Rebuild: `prepare_g10.py`, GIMP `paint_g10_gimp.py`, Blender `build_g10_blender.py`, `prepare_g10_unity_page.py`, `prepare_plant_unity.py --group G10`, Blender `render_g10.py`, GIMP `review_g10_gimp.py`, Blender `verify_g10_blender.py`.
