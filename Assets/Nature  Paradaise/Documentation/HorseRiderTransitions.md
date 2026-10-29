# Horse mounting, saddle alignment and dismounting

Source~/Player/Player_Farming.blend contains the native player/control rig, 47 runtime Actions, 14 historical pose snapshots and four sparse editable BLOCK timelines (65 Actions total). The approved anatomical mount and dismount are implemented explicitly for both sides. Side follows the horse, not the camera.

The five live clips come from mesh/Dummy/PlayerDummy/Player.fbx: Mount Horse, Mount Horse Right, Dismount Horse, Dismount Horse Right and Riding Idle. Older animation-only FBX and storyboard sources are historical, not live dependencies.

Horse Companion.prefab uses SaddleSeatAnchor under the animated Def_Spine. Rider hip offset is (0, 0.150452, -0.06), derived from the approved pose/reference horse. This anatomy offset does not scale with the horse. PersonalAnimal aligns the pelvis to the marker while preserving the player's world scale.

Horse Rider Transitions.asset reads paired body movement/turn from Source~/Player/HorseTrajectory.json. Animator normalized phase controls the trajectory, including entry blends and pause. Export separates body yaw from local bones; player facing follows that yaw and finishes smoothly at standing Idle. Forward/backward residual motion is preserved. Four clips have 95 frames at 30 fps, played at speed 1.6 (about 1.96 seconds). Mount end, Riding Idle and dismount start share the exact seated pose; standing endpoints match production Idle before the separate facing rotation.

Dismount checks both sides and nearby forward/back alternatives against the capsule. Obstacles, large steps and drops are rejected. Both blocked sides keep the player mounted. Player colliders and held-tool activity are suspended during transitions and restored afterward. Disabling the horse restores control, collision and Animator speed.

For changes, edit the relevant BLOCK | Kuda ... | Frame 1-95 Action, save, run the embedded exporter or adjacent export_player.py, then Unity > Nature Paradise > Animals > Apply Horse Rider Transitions. prepare_horse_runtime.py reads the sparse editable timeline and derives runtime takes/trajectory in memory. Keep both export helpers beside the master. The optional Validate Approved Horse Animations menu checks bindings and endpoints.

Validation on Unity 6000.0.81f1: 91 full-export checks, 19 asset/endpoint checks and 126 Play Mode checks passed. Coverage includes all 47 clips, unchanged 274-node hierarchy/skin binding, prior production motion, both sides at horse scales 0.45/0.6330596/0.95, live resize, pause, blocked-side fallback, cleanup and standing hand-off without pelvis/facing jumps. Main project separately passed the 19 binding/endpoint checks. Main Map file hash remained unchanged. Tests used a separate project and persistent save folder. Physical mobile FPS was not benchmarked. Minor local clothing/horse clipping around 1-3 cm remains in some source poses.
