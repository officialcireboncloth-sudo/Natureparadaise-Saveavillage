# Complete editable player master

Player_Farming.blend contains the native player mesh/control rig, 47 runtime Actions, 14 historical single-pose assets, four editable horse timelines and two source-only milker reference animations: 67 Actions total. The reference horse uses the current Unity mesh and scale.

Editable Actions:

- BLOCK | Kuda Naik | Frame 1-95 (left): 14 main keys.
- BLOCK | Kuda Turun | Frame 1-95 (left): 11 main keys.
- BLOCK | Kuda Naik Kanan | Frame 1-95 (right): 14 main keys.
- BLOCK | Kuda Turun Kanan | Frame 1-95 (right): 11 main keys.

Left/right follows the horse's facing, not the camera. Mount uses the near foot as support and passes the far leg behind the saddle. Dismount releases both stirrups, lifts the hips while leaning, passes the far leg behind and gathers both feet beside the horse before landing. All four endpoints and Riding Idle share the same seated pose. Ankles retain their calibrated calf relationship. A few poses retain local clothing/horse clipping of about 1-3 cm.

BLOCK keys use Bezier Auto Clamped and remain sparse. Edit these timelines for further revisions; the POSE assets are older snapshots.

## Export and apply

1. Save Player_Farming.blend.
2. Run the embedded Export Player to Unity.py, or adjacent export_player.py with Blender.
3. In Unity choose Nature Paradise > Animals > Apply Horse Rider Transitions.
4. Optional: Nature Paradise > Animals > Validate Approved Horse Animations.

prepare_horse_runtime.py reads the four BLOCK timelines on each export and updates the runtime takes in memory. It separates body yaw from local bone motion and writes the paired HorseTrajectory.json. PersonalAnimal applies the trajectory and body turn, so standing/locomotion hand-offs do not snap. Runtime takes are sampled for FBX; the saved editable source is never baked per frame or destructively cleaned.

The full export keeps 47 runtime clips, the production hierarchy and hidden rig shapes. Reference horse, source-only POSE/BLOCK takes and preview object movement are excluded. The intact saved master reloads after export.

Current Unity integration uses mesh/Dummy/PlayerDummy/Player.fbx, the shared saddle anchor, explicit left/right states, and speed 1.6 (about 1.96 seconds per transition). Saddle hip offset is derived from the approved pose and current reference horse. Integration evidence is in ArtSource/Player/HorseUnityApply_2026-10-29.

Source~ is tracked in Git but excluded from Unity imports/player builds. Keep the master, both adjacent export helpers and HorseTrajectory.json together.

## Portable machine milking

Edit **Player | Milking Animal** on the original Armature. It is a 3-second action (frames 1–91), with sparse Bezier Auto Clamped keys at 1, 10, 22, 32, 40, 56, 64, 72, 82 and 91. Markers identify carrying, lowering the body, guiding/attaching the cup, holding the vacuum steady, stopping/removing the cup and returning to standing. The right hand holds the sealed canister; the left hand handles the upward-facing suction cup. It has no alternating manual squeezing strokes.

The **Milker references - DO NOT EXPORT** collection uses the actual Unity tool mesh at the player's game scale. Its two REF animations are source references and excluded from export, together with the reference objects. Horse references are hidden for the milking review; show their collection again to edit the horse actions.

After exporting, use **Nature Paradise > Tools > Apply Machine Milking Animation** in Unity. This applies only the milking clip at 1x speed and updates the existing portable Milker prefab: original material/UVs, separate canister/cup and a reusable 66-vertex hose. Cow and goat use the complete sequence. Validation and backups: ArtSource/MilkerMachineAnimation.
