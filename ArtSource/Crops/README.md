Current visual revision: **G01 R05** — Rubus leaf envelopes measured from the supplied images, fine directed serrations retained in actual near meshes, and Blueberry color/normal detail restored from the pre-R04 source. [Reference and exported leaf comparison](Previews/G01_Leaf_R05_ReferenceComparison.png) · [Leaf details](Previews/G01_Leaf_R05_Details.png) · [Before/after](Previews/G01_Leaf_R05_BeforeAfter.png). Same shared atlas/material; other 153 surfaces and UI retained.

Previous visual revision: **G01 R04** — fuller connected shrub crowns, compound Rubus foliage, species-specific leaf textures and fruit distributed in depth. [Before/after](Previews/G01_Bush_R04_BeforeAfter.png) · [Game-camera and spatial views](Previews/G01_Bush_R04_SpatialReview.png). Existing atlas/material, 154 locked allocations, other 151 surfaces, UI icons and 31 crop prefab/item bindings are retained.

Previous visual revision: **G01 R03** â€” corrected leaf shape, taper-aware texture mapping, proportions and spatial directions for all three berries. [Before/after](Previews/G01_Leaf_R03_BeforeAfter.png) Â· [Spatial review](Previews/G01_Leaf_R03_SpatialReview.png). All shared atlas pixels, 154 allocations, 31 crop bindings and existing UI icons remain intact.

Current integration: **31 of 31 referenced plants are bound to gameplay and the Benih seed store.** Blueberry, Raspberry and Blackberry have five planted phases, detached harvest models and matching cream paper-bag icons. All previous crop balances and accepted art remain intact. See [G01/README.md](G01/README.md) and [Integration/G01_Raspberry_Completion.json](Integration/G01_Raspberry_Completion.json).

Current runtime: **one 4096 x 4096 atlas and one shared material, NP_Plants_Unified_01.** All 380 near/distance meshes and 190 authored prefabs use it. Stable UV allocations, source detail and existing map pixels are preserved. There are 156 gameplay phase bindings and 93 UI sprites. Source pages and editable Blender/GIMP masters stay in ArtSource.

Current art batch: **G01 three berries complete.** [Applied preview](Previews/G01_AppliedPreview.png), [Raspberry growth phases](Previews/G01_Raspberry_GrowthStages.png), and [spatial leaf review](Previews/G01_Raspberry_SpatialReview.png). File, compiler and crop logic audits passed; Unity Editor import, Play Mode and device profiling are separate and were not run.

# Plant atlas library and crop models

## Completed batch

The following pages describe the original editable source library for the 31 illustrated crops. The runtime atlas is now generated through the unified pipeline above, including trellis wood. Building/machine atlases remain separate.

| Page | Contents | Files |
| --- | --- | --- |
| NP_Plants_Core_01 | Common leaves, stems, roots, seeds, bulbs, buds | 1024 square BaseColor, Normal, SurfaceMask; three editable XCFs |
| NP_Plants_Produce_01 | Shared leaves/stems plus fruit, vegetable, grain surfaces and food flowers | Same three maps and sources |
| NP_Plants_Flowers_01 | Shared leaves/stems plus flower colours, petals, centres, lips, pollen and specialist leaves | Same three maps and sources |
| NP_Plants_Trellis_01 | G03 independent colour/detail revision; registered kiwi leaf and skin | 2048 square BaseColor, Normal, SurfaceMask; three editable XCFs |

Each page has 64 stable named slots. The base pages use 10px UV insets; Trellis doubles the resolution and gutters to 20px while retaining the same UV-safe coordinates. Common foliage/seed slots are repeated so a complete plant can use one main material. G03 deliberately revises colour and detail on its independent page. Alpha in SurfaceMask is smoothness, red is metallic (zero). BaseColor is opaque. Normals are authored in native GIMP from registered relief.

Runtime maps and slot manifests: `Assets/Nature  Paradaise/Textures/Shared/Plants/`.
Runtime materials: `Assets/Nature  Paradaise/Material/Shared/Plants/`.
Editable painting: `ArtSource/Crops/Textures/` (XCF and deterministic vector brush masters).
Coverage: `NP_Plants_Catalog.json` records a page and required slots for every illustrated plant. Daisy and Moonflower remain flagged as missing references from the original master list.

## Prepared model groups

| Group | Plants | Source |
| --- | --- | --- |
| G05 R02 | Cabbage, Cauliflower, Broccoli | `G05/Blender/G05_Brassica_Master.blend` |
| G02 | Watermelon, Melon, Zucchini | `G02/Blender/G02_Cucurbits_Master.blend` |
| G03 R02 | Cucumber, Pea, Kiwi | `G03/Blender/G03_Trellis_Master.blend` |
| G04 | Green Bean, Chili Pepper, Eggplant | `G04/Blender/G04_Bush_Master.blend` |
| G06 | Radish, Onion | `G06/Blender/` |
| G07 R02 | Spinach, Water Spinach | `G07/Blender/` |
| G08 R02 | Rice, Wheat | `G08/Blender/` |
| G09 R02 | Rose, Hibiscus, Jasmine | `G09/Blender/G09_FloweringShrubs_Master.blend` |
| G10 | Sunflower, Chrysanthemum, Dahlia, Marigold | `G10/Blender/G10_RadialFlowers_Master.blend` |
| G11 | Tulip | `G11/Blender/G11_Tulip_Master.blend` |
| G12 | Orchid | `G12/Blender/G12_Orchid_Master.blend` |
| G13 | Lavender | `G13/Blender/G13_Lavender_Master.blend` |
| G01 | Blueberry, Raspberry, Blackberry | `G01/Blender/G01_Berries_Master.blend` |

All 31 illustrated plants have prepared growth and harvest art and active crop/item/store bindings. The current gameplay binding list is Integration/Manifest.json.

## G05 production source

Open `G05/Blender/G05_Brassica_Master.blend`.

The file contains Cabbage, Cauliflower and Broccoli, each displayed as Seed, Sprout, Young, HeadForming, Ready and Harvest. Stage roots are arranged in a labelled review grid. Each phase has its own collection. A separate hidden KIT collection retains the shared components; a SOURCE collection keeps the pack audit geometry separate. Packed plant maps make the master portable.

The seed and broad two-leaf sprout share mesh data across the three species. The visible young plants share the revised rounded-leaf kit. Original pack geometry is retained in the hidden source/kit collections for reference. Later stages use wide cupped leaves, closely overlapping cabbage wrappers and unified sculpted floret surfaces. Broccoli has a thick, branching stalk. Source pack files are retained.

18 phase appearances use **14 unique base exports**: two shared early stages plus four species-specific stages per crop. Each has a second distance mesh (LOD1). Exports include FBX, explicit mesh JSON and native Unity Mesh assets. Visual-only prefabs have two LODs and one shared Produce material per renderer.

| Deliverable | Location |
| --- | --- |
| Blender master and phase manifest | `ArtSource/Crops/G05/Blender/` |
| Base/LOD1 meshes | `Assets/Nature  Paradaise/mesh/Crops/G05/` |
| Visual prefabs | `Assets/Nature  Paradaise/Prefabs/Crops/G05/` |
| Suggested phase bindings | `ArtSource/Crops/G05/Blender/UnityVisualBindings.json` |
| Labelled atlas preview | `ArtSource/Crops/Previews/AtlasReview.png` |
| Five-stage and harvest preview | `ArtSource/Crops/Previews/G05_GrowthStages.png` |
| Mature plants preview | `ArtSource/Crops/Previews/G05_Closeup.png` |

G05 is integrated through crop profiles and the existing seed store. Cabbage retains its legacy six-slot schedule and stable IDs; its last two slots share the Ready model. Native Unity import and in-game field-camera review remain separate from source-file validation.

## Rebuild

1. Run `Scripts/plant_library.py` with Python to create manifests and coverage metadata.
2. Run `Scripts/paint_plants_gimp.py` using GIMP's python-fu-eval batch interpreter, with `__file__` set to that script path. This builds the XCFs, runtime PNGs, real normals and labelled atlas review.
3. Run Blender in background mode with `--python ArtSource/Crops/Scripts/build_g05_blender.py`.
4. Run `Scripts/prepare_plant_unity.py` with Python to prepare materials, import metadata, serialized meshes and visual LOD prefabs.
5. Run `Scripts/audit_plant_art.py` and the Blender reopen check. Review the preview images and then validate the imported assets in Unity.

The importer writes only the plant library and new G05 visual assets. Preserve existing slot names/UV coordinates when adding future models; use a new page for additions that cannot fit without moving used slots.

## Validation completed

- Plant coverage, atlas pixels, mask channels, UV margins, LOD reduction, serialized mesh buffers and prefab references: 1003 checks passed. Existing 46 atlas files retain their original hashes.
- Nine GIMP XCF sources reopened successfully. BaseColor and SurfaceMask retain 64 independent 128px slot layers per page.
- Saved Blender master reopened with 18 phase roots and three current packed texture maps. Seed/sprout mesh sharing confirmed across all three plants. All 28 FBX exports reimported with matching triangle counts, scale and one UV/material channel.
- The original source checks above predate gameplay integration. Current bindings and validation status are recorded in Integration/Manifest.json and Integration/IntegrationAudit.json.

## R02 form and colour revision

User feedback: first batch shapes and colours needed improvement. The revision uses wider rounded leaves, closer cabbage wrapping, fused continuous cauliflower/broccoli heads, stronger green/yellow-green foliage, a darker broccoli crown, and cream cauliflower. Surface gloss was adjusted in the real atlas masks. UV slot names and coordinates are retained. Pre-revision source and previews are in `Revisions/G05_R02_FormColor/`.

The neutral review lighting and AgX with medium-high contrast show the revised atlas colours more directly. Colour changes are in the actual PNG/XCF/material inputs, not a tint applied to the final preview image.
