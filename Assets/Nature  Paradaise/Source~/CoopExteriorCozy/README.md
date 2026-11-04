# Player Coop exterior Cozy

Editable source: `CoopExterior_Cozy_Master.blend`. This is the player-built Coop, not the shop companion Coop. The reference is the right panel of `CoopExterior_Target.png`. See `../../Documentation/CoopExteriorCozyRework.md` for applied status and native Unity review.

The blend contains editable modules, three merged export meshes and a review studio. Studio cameras/lights and guides are not exported. Work in metres. The builder's input space is Unity +Y up / +Z front; `cv()` handles Blender handedness so the front review matches Unity (door image-left). Exported axis empties let the Unity setup normalize vertices, normals, winding and tangent handedness. Do not remove them.

Rebuild with Blender 4.5: `blender --background --python-exit-code 1 --python build_coop_exterior_cozy.py`. Run from the repository root, passing this script's full path. It reuses committed PoultryCozy and HouseCozy geometry helpers and both shared atlas pages. Building regenerates the blend, FBXs and geometric manifest; it does not modify Unity prefabs.

Then run Unity Edit Mode menu **Nature Paradise > Art > Apply Cozy Player Coop Exterior**. It creates/reuses runtime mesh subassets and applies the model to existing Coop exterior prefabs while preserving portal/collider/site identities. Do not export the Blender studio as a gameplay scene.

The manifest describes exported geometry. Applied status and verification are recorded in the documentation/audit. All five visual prefab levels reuse these LODs at their existing scales; gameplay level/capacity rules are not expanded.
