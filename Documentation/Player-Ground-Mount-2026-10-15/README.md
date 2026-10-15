# Player feet and horse seat correction

The player's body scale and the horse's size are retained.

- Player CharacterController skin width is now 0.02 m (previously 0.08 m), both at runtime and in the four authored player scenes. The smaller collider gap suits the current 1.5 m player.
- PlayerVisual's model Y offset is 0.15 m instead of 0.181461403 m. The approved model scale and X/Z offsets remain unchanged.
- Player visual setup removes the current FBX's measured 0.031461403 m standing-sole padding from its imported culling bounds at the 1.5 m reference height. Rebuilding this visual keeps the corrected offset without saving animation-pose overrides. A replacement FBX requires its own sole-padding calibration.
- PersonalAnimal resolves the animated Hips bone on both Humanoid and Generic Mixamo rigs. Mount/dismount transitions and saddle following therefore use the actual pelvis rather than the capsule-height approximation.
- An explicit MountPoint takes priority over saddle-renderer bounds. Those bounds include hanging straps/stirrups and previously pulled the target downward. The marker continues to follow the horse's animated spine.
- Rider pelvic clearance is +0.05 m in world units. Parenting continues to retain the rider's world scale, and dismount restores its original parent/colliders.

## Validation

Runtime and editor C# compilation passed, with existing unused-field warnings. A separate Unity Play Mode fixture evaluates the real Generic player and horse rigs with animation culling disabled for measurement.

| Measurement on a flat collider | Before | After |
| --- | --- | --- |
| Standing sole above floor | 0.112 m | 0.020 m |
| Sampled running sole above floor | 0.120 m | 0.028 m |
| Pelvis distance to configured seat target | 0.210 m | 0.00018 m |
| Sole after dismount | 0.111 m | 0.020 m |

Mount completion, rider world scale of one and CharacterController restoration passed. The scene/prefab values and isolated runtime behavior were tested; the user's open Map session was not restarted. Reload Play Mode to load the revised prefab/scene settings.

The evidence uses posed SkinnedMeshRenderer bounds with updateWhenOffscreen enabled, rather than transforming BakeMesh vertices through the FBX mesh-node transform. The latter is unsuitable for this export's 100x mesh-node transform and axis correction. Earlier raw BakeMesh measurements were discarded.
