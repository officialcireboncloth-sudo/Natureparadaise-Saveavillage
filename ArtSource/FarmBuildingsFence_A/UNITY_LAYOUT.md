# Unity reference layout

Status: APPLIED AND VERIFIED in Unity 6000.0.81f1 on 14 October 2026. All nine FBX models are imported and remapped. The prefab and one Map root instance exist; `Previews/UnityApplyResult.json` says PASS and `Previews/UnityImportAudit.txt` ends in UNITY_APPLY_PASS. Unity render: `Previews/Unity_FarmReference_Group.png`.

The one-shot request was consumed successfully. The installer imported/remapped the FBX files, made nine module prefabs, built `Assets/Nature  Paradaise/Prefabs/FarmReference/FarmReference_Group.prefab`, validated it, and saved one root instance named `FarmReference_Group` in `Map.unity`. Its initial position is (211.962128, 18.4958076, 477.6648), rotation identity, scale one. It preserves an existing group's transform when rerun. The menu is `Nature Paradise > Farm Reference > Apply Group to Map`.

Layout uses a 24 × 24 m fence perimeter. A single 2 m front gate aligns with the Barn Store door, with a second 2 m gate on the inner garden fence. The left garden area shares the outer front/side rails; shared endpoints use one corner/T/post instead of overlapping posts. Two buildings sit at the rear. Source-plan X maps to Unity -X while front is Unity +Z, preserving the reference's left/right arrangement from the front. Every module keeps metre scale.

Hierarchy:

- FarmReference_Group
  - Buildings / Lumbung_Krem and Barn_Store
  - Fence_Perimeter / Rails, Posts, Gate_Front_2m
  - Fence_Garden / inner rail modules and Gate_Garden_2m

Each module stays a nested prefab. Moving or rotating the top parent moves the entire composition; individual modules remain selectable. Existing Toonpack material `TFP_Atlas_1A.mat` is reused. Building collision leaves the storage nook and store canopy accessible; gate leaves have their own colliders and retain hinge transforms. No animal/plant/ground art, gameplay building registration, save migration or gate animation is added.

Native checks passed for triangle counts, original atlas remapping, asymmetrical roof ridge/front orientation, rail pitch, separate gate pivots, both layout gates, unit module scales, colliders and parent move/rotate. Complete group: 119 mesh renderers and 17,868 triangles, with no new runtime textures. The Map backup is `Revisions/Map_BeforeFarmGroup.unity`.

FBX exports now include an unparented non-rendering `FBX_AxisReference` empty to keep Unity's generated file root at identity. Native authored child transforms and gate hinges retain their conversion. Blender saved-source/FBX roundtrip audit remains PASS for all nine models; the original atlas has not changed.
