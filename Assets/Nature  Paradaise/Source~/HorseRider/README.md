# Horse rider animation source

Open Horse_Rider_Transitions.blend in Blender 4.5 or later. The default scene is Unity Horse Dismount Review, in metres, with the actual Unity-sized horse reference, the rider at its production visual scale, a saddle marker and a keyed world-motion reference. No control-shape meshes are exported or displayed. The horse mesh is a static reference baked from the active production prefab parts, not a replacement horse rig.

The Rider Animation Sources scene contains editable baked pose keys for Mount Horse (1–76), Dismount Horse (1–91), their right-side versions, and Riding Idle (1–61), at 30 fps. Unity plays mount/dismount states at speed 2, giving 1.25 s and 1.5 s respectively. Mount end, seated idle and dismount start share exactly the same pose. The standing endpoint matches the existing Idle clip. The swing leg follows a wider rearward arc with a guided knee and levelled ankle; it does not curl the boot toward the waist.

Horse_UnityScale_Reference.fbx and .obj carry the actual Unity world dimensions, with horse prefab root scale 0.6330596 already baked into the geometry. Do not multiply these reference objects by that scale again. The review rider uses production scale 1.33043254 and model offset (-0.0174398734, 0.0974751771, 0.15) in Blender coordinates. Blender reference coordinates correspond to (-Unity X, -Unity Z, Unity Y).

The saddle reference surface is at Unity world Y=1.044548 for the default horse. The rider pelvis sits 0.05 m above the surface; this anatomy offset remains in world units. In Unity the SaddleSeatAnchor belongs to Def_Spine and scales/moves with the horse. The original player skeleton stays at its authored size.

The packaged author_transitions.py rebuilds ArtSource/HorseRider from the current production Player.fbx, preserving its original file. Run with Blender in background, or edit the baked actions directly in the blend. Animation-only exports must retain the production skeleton convention: Unity Armature scale 100, its child translations scaled accordingly, file units centimetres, Generic rig, forward -Z/up Y, no added leaf bones. Use the same existing .meta when replacing the runtime FBX, then run Nature Paradise > Animals > Apply Horse Rider Transitions. The setup does not edit world scenes.

Runtime assets live in Player/Animations/Horse, Resources/Profiles/Horse Rider Transitions.asset, Horse Companion.prefab and the player controller/animation set. This Source~ folder is intentionally excluded from Unity import and player builds, while remaining available in Git for future art changes.

