# Fish Pond A — approved Blender masters

Approved 2026-10-13 by the user: “aku udh buka blender kita eksekusi sekarang”.
This package contains Blender meshes and FBX exports, now applied to Unity on
2026-10-13. The existing FishPond_Lv1–3 prefab GUIDs are preserved and the building
definition links the new models with capacities 50/100/250. The existing feed maker
is bound at its approved location. Water uses a pond variant of the existing Toon
Pack material, preserving its shader and normal/noise maps. Existing daily feed
behavior is retained; new pellet-emission effects and extended automatic-feeder
gameplay are not added by this model/material integration.

## Unity integration

- Model imports: `Assets/Nature  Paradaise/mesh/Buildings/FishPond_A`.
- Level prefabs: `Assets/Nature  Paradaise/Prefabs/FishPond/FishPond_Lv1–3.prefab`.
- Water: `Assets/Nature  Paradaise/Material/World/FishPond/FishPondWater.mat`, now
  `Toon/CustomWaterSimple` derived from pack `TFP_Water_1A.mat`. Original pack assets
  remain unchanged. The pond variant has readable blue colors and 0.50 base opacity.
- `Nature Paradise/Fish Pond/Apply Approved Pond A` reinstalls the approved meshes.
- `Refresh Water Review` captures native URP renders; `Validate Runtime Integration`
  checks the runtime controllers in a separate testing scene.
- Review scene: `Assets/Nature  Paradaise/Map/Scenes/Testing/FishPond_A_Review.unity`.
- Import validation: `Previews/UnityImportAudit.txt`; runtime: `UnityPlayAudit.txt`.
- Real Unity previews: `FishPond_A_Lv1/2/3_Unity.png`.

Each LOD has one opaque renderer plus the same separate water renderer. Both follow
LOD selection and distance culling. The high mesh provides a fixed static collider,
including stairs, dock, rim and machine bodies. Interact E uses the dock marker;
the barrel/feeding interaction no longer prevents the pond UI from opening there.
Fish are normalized to the new underwater bounds and spread through the approved
polygon, outside the dock. Representative visual counts are 8/12/18; logical fish
capacity is independent of those limits.

Legacy Lv4 saves migrate to Lv3 with site IDs, fish, quality, size, growth and feed
preserved. Over-capacity ponds still allow withdrawal and block new deposits without
consuming inventory. Existing construction costs/durations for Lv1–3 are preserved.
Placement uses the approved per-level sites: 14×11 / 16×13 / 20×16 m. On the Map's
2 m grid, the rotated site is rounded up to a centered odd-sized tile rectangle;
the whole rectangle must fit inside the field. Upgrades reserve the additional
cells and reject occupied expansion space without deleting crops or resources.
The definition's 10×8 footprint remains a maximum-size data fallback, rather than
forcing the Lv3 reservation on Lv1.
Default placement front +Z is rotated toward the fixed Map camera; the asymmetric
mesh and station coordinates were verified in Unity rather than guessing FBX yaw.
Runtime capacity reads the building definition, so capacity edits through the
existing balance CSV workflow stay consistent with the UI. The Buildings settings
CSV snapshot is synced to the three levels, updated footprint and placement yaw;
retired Lv4 rows are removed. Unrelated buildings and costs remain unchanged.

## Files

- `Blender/FishPond_A_Masters.blend`: three individual metric scenes and one level comparison scene.
- `Exports/`: High / Medium / Low opaque FBX and one Water FBX for each level (12 files total).
- `References/FishPond_A_Approved.json`: archived approved geometry, coordinates, atlas swatches and integration specification.
- `References/TFP_Atlas_1A_BlenderPreview.png`: PSD composite for Blender only, packed in the `.blend`; not a new runtime texture.
- `References/Pond-LvN-UserReference.png`: original user reference images.
- `Scripts/build_pond.py`: reproducible Blender modelling/export script.
- `Scripts/render_preview.py`: render the saved meshes without regenerating geometry.
- `Scripts/validate_pond.py`: saved geometry and real FBX reimport audit.
- `Previews/`: front/right, rear/left, comparison renders, `MeshManifest.json` and `Validation.json`.

## Geometry and level mapping

| Level | Logical capacity for future integration | Outer basin W×D | Inner water W×D | Opaque High / Medium / Low triangles | Water triangles |
|---|---:|---|---|---|---:|
| 1 | 50 | 8×6 m | 6.8×4.8 m | 2563 / 1633 / 574 | 14 |
| 2 | 100 | 10×7.5 m | 8.8×6.3 m | 3193 / 2101 / 782 | 14 |
| 3 | 250 | 14×10.5 m | 12.8×9.3 m | 4084 / 2621 / 864 | 14 |

Scale is 1,1,1, with three distinct basins; machine dimensions are not stretched with
the pond. All levels include a feed maker and barrel. Lv2–3 include an automatic
feeder; Lv3 adds a supply box and a second inlet. Three 0.30 m stairs reach a wooden
dock with no front railing and a 1.40 m unobstructed central route. The dock board
top is 0.95 m; the nominal station height in the approved proposal remains 0.90 m.

The shallow raised liner has its floor at approved Y=0.10 m and water at Y=0.65 m.
It covers terrain under transparent water without requiring a carved terrain hole.
Placement later requires a level site; the approved maximum ground excursion is
0.08 m. Do not place this basin on arbitrary slopes.

Production opaque geometry is combined into one mesh for each LOD. The editable
source collection keeps named component groups for future revisions. Exports have
one material each, no lights/cameras/ground/markers and no embedded textures. Water
is a separate horizontal mesh reused across LODs, so there are two static renderers
and two material slots at a time. Additional runtime fish and particles are separate.
LOD geometry is authored in Blender, not automatically generated by Unity.

## Coordinates and import

Approved coordinates: X right, Y up, front −Z; origin at the reserved site center at
ground level. Basin center is X=+1.5 m. Blender conversion is `(x,y,z)=(X,Z,Y)`:
front is Blender −Y. Face winding is oriented explicitly after that reflected axis
conversion; water and coping face upward, liner walls face the water, outer walls
face outward. FBX export uses −Z forward / Y up and metric scale.

All 12 files were imported back into Blender and checked for metre dimensions,
pivots, counts, safe UVs, materials and face winding. Unity import also verifies the
asymmetric High bounds and every water boundary point, with front normalized to +Z.
Canonical Unity coordinates are `(-X,Y,-Z)` relative to the approved drawing; station
markers use the same mapping and identity scale. No negative object scale is used.

## Atlas and water

Opaque slot: `TFP_Atlas_1A`. Remap to the existing shared Toon Farm Pack material.
All faces sample approved safe palette rectangles in the existing 2048² atlas PSD,
with a 4 px margin. This follows the prior Barn/Coop/House palette workflow. Large
machine casings and dock posts have a small one-segment bevel; stones retain their
flat eight-sided faces and bevel ring. Atlas PSD was checked unchanged by SHA-256.
There are **zero new runtime textures**.

Water slot: `TFP_Water_1A`. The Blender material is only an approximation for model
review. The final water must use existing Unity `Toon/CustomWaterSimple`, based on
the pack's `TFP_Water_1A.mat`. Starting pond-specific overrides are stored in the
approved JSON. Create a pond variant, rather than changing all pack water globally.
Its 14 triangles have upward normals and metre UVs, not palette-cell UVs. Unity import
verified those normals for the pack shader's backface culling. The PC render pipeline
already has depth/opaque textures enabled; the review camera explicitly requests
both. Mobile pipeline depth/foam settings have not been changed by this integration.

Inlet and overflow ribbons in the sketch are excluded from static exports. FX
markers identify the future water stream locations; no frozen particle sheet,
fish, or pellet stream is baked into the mesh. `Pellet emitter` on Lv2–3 points
approved +Z toward the basin beyond the rear dock rail. Its direction and stations
are recorded in marker custom properties. Configure actual effects and normal/noise
textures from the existing Toon Pack later in Unity. Depth/opaque textures and foam
behavior must be checked in the target render pipeline then.

## Blender organization

Select a scene from the top scene dropdown:

- `Fish Pond A Lv1`, `Fish Pond A Lv2`, `Fish Pond A Lv3`.
- `Fish Pond A Levels 1-3`: actual shared High and Water mesh datablocks at the same
  scale; this is a comparison arrangement, not gameplay placement.

Each individual scene has:

1. `01 EDITABLE SOURCE`: hidden grouped source components. Disable combined High
   before showing these to avoid doubled surfaces.
2. `02 COMBINED MESHES`: High and Water visible; Medium and Low hidden individually.
3. `03 PREVIEW STUDIO`: floor/camera/lights, excluded from exports.
4. `04 STATION AND FX MARKERS`: hidden non-rendering empty points for later gameplay.

## Verification

`Previews/Validation.json`: **200 checks passed** against the saved `.blend` and all
12 FBX imports, including approved High bounds, water polygon/height/normals,
1.40 m feeder clearance, station coordinates, UV palette safety, LOD budgets and
the shared comparison meshes. Front/right and rear/left renders inspect the actual
meshes. Unity import and native water renders passed. Actual Play Mode checks passed
for all three prefabs: fish positions, underwater heights, maker initialization,
dock interaction range, UI opening, fish deposit/withdrawal, fish survival across
visual replacement, and legacy over-capacity deposit/withdrawal behavior.

Placement previews clone meshes/LODs under an inactive staging parent and strip
gameplay scripts before activation. They cannot open the pond UI, spawn fish or
register persistent pond/feed-maker records. Invalid locations explain whether
the footprint leaves the field or is blocked. Empty and bare hoed tiles are valid;
crops, wild grass, trees, resources and other buildings must be cleared first.
`Validate Placement Regression` tests the actual preview/confirm/upgrade path
in a separate 15×15 grid scene without a SaveManager, then returns to the Map.
Its results are saved to `Previews/UnityPlacementAudit.txt`.
The 2026-10-13 run passed **28 checks**, including resource/boundary rejection,
preview isolation, payment-reservation rollback, hoed soil placement and Lv1–3
expansion with an occupied-ring rejection that preserves the crop and existing pond.

Future work can add the inlet/overflow particles at the authored FX markers and
pellet particles at the auto-feeder emitter. Existing Tilapia currently has no world
prefab, so the existing capsule fallback is kept until an actual fish mesh is assigned.
