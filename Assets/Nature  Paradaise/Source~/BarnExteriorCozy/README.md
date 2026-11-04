# Player Barn exterior Cozy

`BarnExterior_Cozy_Master.blend` is the editable master for the player-owned livestock barn. `BarnExterior_Target.png` is the requested front reference. This does not replace Barn Store, the Coop, or livestock interiors.

Input coordinates use Unity metres: +Y up, +Z front; `cv()` handles Blender handedness. Four exported axis markers allow the native Unity importer to normalize vertices, normals, winding and tangent handedness. Keep these markers. Runtime mesh vertices are independent of Blender installation.

The source contains editable facade modules, real gambrel tiles, a recessed hay loft, the cow sign, props, three merged LODs and a review studio. The studio and cameras are not exported. Existing SharedAtlas pages 01/02 and committed PoultryCozy/HouseCozy geometry helpers are dependencies; there is no generated per-object texture.

Rebuild from the repository root with Blender 4.5: `blender --background --python-exit-code 1 --python <absolute path to build_barn_exterior_cozy.py>`. Append `-- --no-preview` to skip studio renders. It regenerates blend, FBXs and geometric manifest only. Then use Unity Edit Mode menu **Nature Paradise > Art > Apply Cozy Player Barn Exterior**. It preserves prefab/root/door identities and bell component references while aligning the retained body collider to the closed new shell.

Bell remains a separate, animated mesh from the existing barn, mounted at the right side. Its approach/prompt markers are repositioned independently of the central entrance. Do not join it into the static LOD mesh. The builder adds the shared-atlas side bracket only.

Geometry tagged `KeepLOD` is joined after optional LOD reduction so foundation, roof underlay and generated cow decal remain intact. Other details simplify or disappear at distance. Applied state, native checks and Unity preview images: `../../Documentation/BarnExteriorCozyRework.md`.

## Generated sign image

The cow contour geometry has been replaced by a generated PNG on a two-triangle plane. See ../SharedAtlas/Emblems01/README.md and Generation.json for the retained source image, full prompts and GIMP packing steps. The runtime page is NP_CozyEmblems_01_BaseColor.png (512 x 256). Rebuild that page before the Barn builder when changing the source image. Native Unity setup creates/remaps its shared cutout material.
