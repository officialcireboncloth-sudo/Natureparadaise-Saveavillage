# Atlas Market Stand

Editable Blender source: `MarketStand_A.blend` (metres, Z up, front -Y). The shared colour atlas is packed into the file. Named collections separate the timber frame, counter, crates, striped canopy, produce and rope. Original produce meshes remain in a hidden source library. The review camera/lights are excluded from the FBX.

The counter is 1.02m high, the canopy is about 2.8m high, and the full stand is 3.40m wide. The export contains two combined LOD meshes: 9,311 and 4,933 triangles. Each LOD uses one material slot, referencing `Material/Shared Wood Atlas.mat` in Unity, including its existing normal map. No additional texture atlas is required.

Unity uses a compact 0.8 presentation scale: about 2.72m wide, 2.22m high, with the counter at 0.82m. The gameplay collider uses the same factor. The atlas gameplay prefab normalizes inherited world scale in `Awake`, so legacy Map instances with a 2x root scale cannot double the stand's size. Position, rotation and persistent listing data remain unchanged. The Blender source retains its full-size modelling dimensions.

The revised canopy has nine continuous lengthwise stripes, a smooth curved front slope, a rear return, deeper rounded front valances and gathered side fabric with rope ties. Each ribbon uses a continuous padded UV patch over its whole surface, preventing per-face palette resets and alternating bands. Imported smooth normals and low mesh compression retain the cloth curvature. Colour/normal atlas files are reused unchanged.

Tomato, carrot, cabbage and eggplant geometry comes from the project's Toon Farm Pack `Models/TFP_*_01A.fbx` files. Cabbage uses the vendor's lighter LOD. Cabbage/eggplant colours are adapted to the shared atlas palette; the added carrot greens are simple double-sided geometry. Palette UVs have a small area so Unity can calculate stable normal-map tangents.

`BuildMarketStand.py` regenerates the master, combined LOD export and local review render. The generated FBX is `mesh/World/MarketStand/MarketStand_A.fbx`. In Unity, **Nature Paradise → Art → Apply Atlas Market Stand** imports it and updates `Prefabs/World/MarketStand.prefab` through `Prefabs/World/MarketStand/MarketStandVisual.prefab`, preserving the existing market component and prefab GUID. This menu does not open or save the world map.

The displayed produce is decoration. Listings, prices, inventory, buyer simulation and persistent stand ID remain owned by the existing `MarketStand` component.
