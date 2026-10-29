# Farm interior fixture revision — 28 October 2026

The editable Blender source is `Blender/FarmInterior_Revision.blend`. It contains the modern feed processor, wooden warming incubator with independent glass lid and heater, burlap feed sack, loose straw pile and wall lamp. The existing shared Toon Farm wood atlas is packed into the file. No new runtime texture is introduced.

`Exports` contains the source FBX modules. `Palette.json` records padded regions of the existing atlas. `Scripts/build_kit.py` and `Scripts/mesh_common.py` reproduce the kit in Blender; run from a Unity project checkout containing these files. Geometry is in metres, Blender Z up and +Y toward the back of the room.

The production Barn/Coop layout prefabs remain the authoritative assembled interiors. `FarmInteriorRevisionSetup` updates their existing visual meshes and adds a batched decoration group. It retains the prefab GUID, room scale, original feed slots, animal markers, entrance and machine positions. The incubator retains its existing nest marker, independent egg visual and `EggIncubator` save identity. The new lid still uses the existing opening animation.

Use **Nature Paradise > Art > Apply Farm Interior Revision** after rebuilding the legacy base layouts. This explicit operation reads the committed source files in this folder, imports the FBX modules and updates all four levels of both buildings. A client does not need to run this menu to play: the generated meshes, materials and prefab references are normal committed assets.

Coop feed troughs use the shared aluminium material. Barn troughs use the shared wood atlas. Water-container steel regions are remapped to neutral aluminium samples while the water surface remains blue. The Barn floor uses a lighter warm earth sample. Existing vendor atlas/material files are not edited.

Each layout has four straw corners, two visible feed sacks and four lamp models. Corner props and lamps use two combined meshes. Only two lamps per active interior have additional point lights; both disable dynamic shadows. Feed-maker capacity, costs, feed types, incubation duration and save schema are unchanged.

Runtime mesh sources live under `mesh/Buildings/FarmInteriorRevision`. Scene framing is kept in the existing interior scenes. Main Map is never opened or saved by this authoring tool.
