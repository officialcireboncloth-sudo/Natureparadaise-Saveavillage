# Shipping Bin — editable Blender master

Open `ShippingBin_A.blend` in Blender 4.5 or newer. The body, metal straps, rivets, carved harvest emblem, hinges and lid remain separate editable objects in named collections. The source opens with the lid raised, matching the design reference. The existing Toon Farm based Shared Wood Atlas colour image is packed into the file.

- Closed dimensions: approximately 1.62 m wide, 1.1 m deep and 0.97 m high. No extra textures or materials.
- Front: Blender -Y / Unity +Z. Units are metres. Corner feet rest on the origin plane.
- `Editable Lid Hinge - closed 0 open -78deg` controls the source lid. X rotation 0 closes it; -78 degrees opens it.
- `95 UNITY EXPORT - Body and Hinged Lid LODs` is hidden for source review. It contains the closed FBX export geometry: two body/lid mesh pairs plus the rear hinge Empty. Keep the body and lid separate for animation.
- `BuildShippingBin.py` rebuilds the master, packed atlas UVs, closed FBX, LODs and local previews. Running it overwrites this generated master; save manual variations separately before rerunning.
- To use manual modelling changes, rebuild the export meshes from the edited parts and export only `ShippingBin_Atlas`, `LidHinge`, and the four LOD meshes, with -Z forward / Y up, animations disabled. Preserve the export names and rear hinge position.
- Unity: **Nature Paradise → Art → Apply Atlas Shipping Bin** imports/remaps the FBX and updates the existing shipping prefab while preserving its identity and gameplay settings. It does not open or modify Map.
- The native Unity lid pivot uses unscaled animation time so the lid opens/closes with the storage interface even while gameplay time is paused. A simple stationary box collider covers only the body.
- The atlas prefab repairs legacy scene scale overrides at runtime to retain this player-sized chest; scene position and rotation stay intact.

Near LOD: about 4.8k triangles; far LOD: about 2.7k. One shared material, two renderers per visible LOD (body + animated lid). Physical mobile performance has not been benchmarked.
