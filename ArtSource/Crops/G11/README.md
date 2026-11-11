# G11 Tulip

Current shape revision: **R03**. Sprout, Young, BudForming, Ready and Harvest use physical curved leaf blades whose frames follow radial growth and world-up, with front/back placement and individual rolls. The accepted R02 flower, stalk and bulb shapes are retained. See `Review/R03_Completion.json` and `ArtSource/Crops/Previews/G11_R03_SpatialReview.png`.

Current runtime binding: **NP_Plants_Unified_01**, the single shared crop atlas/material for all groups. Consolidation changes UVs/material links while keeping accepted shapes and asset GUIDs. Per-group atlas files below remain editable authoring inputs. Always publish through `prepare_plant_unity.py --group G11`; runtime validation now uses `verify_unified_plant_atlas.py` and the gameplay binding audit. See `../Unified/README.md`. Field scale, prices, growth timing, icons, store entries, planting depths and interaction hints are retained.

The current user growth reference defines a tan pear-shaped bulb, two rolled shoots emerging from the same bulb, three young strap leaves, a green-to-pink tapered bud, a rounded pink cup with two broad opposing leaves, and one harvested cut flower with a single leaf.

`Blender/G11_Tulip_Master.blend` contains six editable phase roots: Bulb, Sprout, Young, BudForming, Ready and Harvest. The mature cup has three outer and three inner closed tepal shells, physical wall thickness, a real open upper cavity and recessed reproductive details. The stem ends at the green flower neck along the same axis. Broad leaf surfaces face the game view while retaining curved three-dimensional surfaces and thickness.

R02 authoring uses `g11_r02_geometry.py`, `paint_g11_r02_gimp.py` and `build_g11_r02.py`. The standard geometry and painting entry points also retain the R02 result on later builds. `apply_g11_r02.py` updates visual offsets and records exact authorized before/after hashes; the original gameplay baseline stays intact. `verify_g11_r02.py` checks closed source forms, rounded tips, leaf curvature/asymmetry and exact preservation of the six unaffected atlas regions and SurfaceMask.

The independent `NP_Plants_Tulip_01` atlas has eight 512px regions in a 2048 x 1024 page. BaseColor, registered Normal and SurfaceMask have editable GIMP XCF masters under `ArtSource/Crops/Textures/`. SurfaceMask red is zero metallic and alpha is per-surface smoothness. The atlas and existing Cozy shader use one shared material per renderer.

Six Unity visual prefabs under `Assets/Nature  Paradaise/Prefabs/Crops/G11/` each have two LODs. FBX, explicit data and native meshes are under `Assets/Nature  Paradaise/mesh/Crops/G11/`. Runtime mesh buffers are rendered directly for the production previews; the isolated flower detail shows the editable sculpted source.

`Review/ArtAudit.json` records physical petal volumes, exact reference leaf/bulb counts, an open-cavity ray test on the exported mesh, stem/head geometry, padded UVs, packed maps, native buffers and FBX reimports. `Review/GimpReopenAudit.json` verifies the three painting masters. Source/export tests are distinct from Unity visual Play Mode, which has not been run by the agent.

The gameplay integration, paper packet icon, seed-store entry and preservation checks are recorded in `ArtSource/Crops/Integration/G11_Completion.json`. Earlier crop models, balances, Onion/Radish burial and interaction hints are preserved.
