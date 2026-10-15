# Mobile shops in Map

Applied and saved in native Unity 6000.0.81f1 on 14 October 2026. Select the top parent in Hierarchy to move or rotate a complete shop layout.

| Map root | Prefab folder | Staging position |
|---|---|---|
| `ChickenStore_Mobile_Group` | `Assets/Nature  Paradaise/Prefabs/ShopsMobile` | (259.2035, 18.4958076, 577.6776) |
| `MiniMarket_Group` | `Assets/Nature  Paradaise/Prefabs/ShopsMobile` | (295.2035, 18.4958076, 577.6776) |

One prefab instance of each root is saved in `Assets/Nature  Paradaise/Map/Scenes/World/Map.unity`. Child buildings and fence pieces remain nested modular prefabs. Existing farm and other scene roots retain their transforms. Staging positions were selected on sampled terrain near the earlier farm; final locations can be adjusted by the user. No terrain edits were made.

Chicken Store uses the optimized shop, companion coop and optional weather vane from `ArtSource/ChickenStore_A_Mobile`. A 24 × 24 m perimeter reuses the existing 1 m / 2 m rails, posts, corners, T junctions and 2 m gate prefabs. The main gate is approximately aligned with the shop door. A second gate opens the reference's front-right chicken pen; the pen shares the perimeter frontage/right edge. Shared junctions use one post, and all fence pieces retain unit scale. Source-plan X maps to Unity -X to preserve the reference's front-facing arrangement. No animals, crops, vegetation, props or paving were added.

Minimarket uses the latest low-geometry `ArtSource/MiniMarket_A` model. Its reference has no enclosing fence, so none was added to that group.

Native verification passed: all four imported FBXs have authored triangle counts, matching bounds/orientation, metre scale, independent door hinges, existing `TFP_Atlas_1A.mat` remapping and no imported cameras/lights. Groups use box colliders. Gate leaves retain their existing colliders/hinges. Building bodies are exterior solid blockers; interior traversal, gameplay registration and door/gate animations are outside this placement task. New imported meshes have CPU Read/Write copies disabled after validation.

Chicken group including fences: 9,575 triangles, 18,096 Unity imported vertices, 125 renderers. Buildings alone remain 5,783 triangles including the ornament; reused fences contribute 3,792 triangles. Minimarket: 2,910 triangles, 5,238 imported vertices, 3 renderers. Engine vertices differ from Blender's welded count due to normals/UV splits. The existing atlas material already supports instancing. No new runtime textures or material/shader changes.

Reports: `Previews/UnityImportAudit.txt`, `Previews/UnityApplyResult.json`, and the two native Unity preview PNGs. The pre-change Map backup is recorded in the result JSON under `backup` and stored in `Revisions`. Temporary preview scenes were closed after rendering.

Authoring menu: `Nature Paradise > Shops > Apply Mobile Chicken Store and MiniMarket to Map`. The installer consumes one explicit `apply.request`; it does not auto-apply on every reload. Reruns preserve existing Map group transforms and update the prefab assets. Runtime building/save definitions are unchanged.
