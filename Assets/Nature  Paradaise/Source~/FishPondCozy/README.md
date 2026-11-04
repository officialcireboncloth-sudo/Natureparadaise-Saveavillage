# Fish Pond Cozy — Level 1

Editable Blender source for the stone fish pond in FishPond_Target.png. Front is Unity +Z. The existing prefab root scale of 0.4 is retained; the model fits its 5.6 x 4.4 metre placement reservation. Level 1 capacity remains 50 fish.

Open FishPond_Cozy_Master.blend. Collection 01 contains individually editable stone banks, underwater liner/pebbles, deck boards and railings, three stairs, inlet rock/spout, teal feed maker with an open hopper/output tray, a barrel and hanging feed sack. Collection 02 contains the three production LODs and independent water mesh. Collection 03 is the preview studio. Collection 04 contains gameplay and inlet/splash anchors.

The editable collection starts hidden to prevent doubled geometry over the combined production mesh. Hide the production meshes before showing the editable collection. Atlas images are packed into the blend.

Rebuild from the repository root with Blender 4.5:

`blender --background --python-exit-code 1 --python "<absolute path>/build_fish_pond_cozy.py" -- --no-preview`

The builder uses the committed PoultryCozy and HouseCozy geometry helpers and the shared pond atlas page 03. Rebuild Source~/SharedAtlas/Extension03/pack_pond_atlas_gimp.py before the Blender builder when changing the texture source. It writes the master blend, three solid FBXs, a water FBX and geometric manifest. The named axis empties accompany each export so Unity can normalize handedness and winding.

In Unity Edit Mode use **Nature Paradise > Art > Apply Cozy Fish Pond Level 1**. FishPondCozySetup imports the FBXs, creates normalized runtime mesh subassets, and updates the existing FishPond_Lv1 prefab. Its GUID, root file ID, root scale, pond record ID and feed machine component remain stable. It repositions the three gameplay targets to the modeled deck, machine and hanging sack. The bank collider belongs to the near mesh and remains fixed while render LODs change.

## Water template

FishPond_Cozy_Water is a flat, upward-facing mesh at local height 0.65, with independent planar UVs and its own material slot. Blender uses a simple preview material. Unity remaps it to FishPondWater_Cozy, a teal variant of Toonpack **Toon/CustomWaterSimple** retaining the original normal/noise textures. It is reused in all three LODs. It has no collider, static reflection painting or atlas UVs. Inlet and splash anchors are ready for future runtime flow effects; a frozen waterfall is not baked into the solid mesh.

## Mobile geometry

| Mesh | Triangles | Shared material slots |
| --- | ---: | ---: |
| Near | 14,142 | 2 |
| Middle | 8,184 | 1 |
| Far | 3,640 | 1 |
| Independent water | 24 | 1 |

The material revision adds one shared 1024 colour atlas with 512 normal/mask maps. Existing Cozy and Toonpack shaders are reused. Details simplify at distance; the deck, machine, sack, barrel and inlet silhouettes remain. Every triangle stays inside its own padded atlas slot even after LOD reduction. Wood grain uses metre-based crops instead of stretching a full texture across every narrow plank. Existing atlas pages, other building materials and higher pond levels remain unchanged.

Review and native verification: ../../Documentation/FishPondCozyRework.md.
