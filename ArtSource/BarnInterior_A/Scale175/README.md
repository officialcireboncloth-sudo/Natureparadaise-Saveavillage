# Barn size revision: +75% = 1.75×

Applied in Unity on 14 October 2026. User wording “d gedein 75% / 1/75” was interpreted and announced as 1.75 times the previous dimensions.

- All five exterior prefabs retain identity root transforms; their model scale, collider dimensions and entrance offset are scaled consistently. Existing per-level differences remain. Four gameplay levels still use their original prefab references.
- All four interior prefab roots and their scene instances use uniform 1.75 scale, expanding geometry, colliders, troughs, feed markers and animal markers together. Interior entrance remains anchored at `(0,-200,-10)`; independent spawn, exit and camera markers were repositioned relative to that anchor. Spawn `(0,-199.7375,-7.025)`, exit `(0,-198.775,-9.3875)`; camera distance 43.75.
- Clear interior floor sizes: Lv1 **21 × 17.5 m**, Lv2 **28 × 21 m**, Lv3 **35 × 24.5 m**, Lv4 **42 × 28 m**. Wall height 5.95 m. Player and animal models are not enlarged.
- Barn placement footprint is **21 × 25 grid cells**, rounded up from 12 × 14 × 1.75. Definition and Buildings.csv agree. Existing village placements are retained; no automatic relocation.
- No new mesh vertices or triangles, textures, capacity changes or save-data migration. Original Blender sources stay at their authored base scale; this is a Unity sizing revision. Exterior/interior authoring scripts preserve the new sizing when reapplied.

`Result.json` and `Validation.txt` record native Unity checks. Four-level property restoration at 0/90/180/270 degrees passed with the enlarged entrance outside its body collider. `../Unity/SavedAssetValidation.json` was rerun successfully after resizing, checking every active interior floor, marker, feed binding, capacity and legacy Coop path. No Play Mode walkthrough or phone profiling was performed.

`Before_*` contains scene, prefab and BuildingDefinition backups before scaling. The operation is idempotent: its menu writes the absolute target scales and never multiplies an already enlarged prefab by 1.75 again. Map was not saved by the operation. Level 1 prefab review opens separately after validation.
