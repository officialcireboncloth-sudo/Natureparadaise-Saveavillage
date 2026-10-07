# Stylized Meadow — editable source kit

The custom meadow kit follows the supplied rounded-leaf, yellow-green reference. Source files stay outside Unity's `Assets` folder so Blender/GIMP documents are not imported as duplicate game assets.

## Folder guide

- `Blender/StylizedMeadow.blend`: editable metre-scale source meshes, low-detail variants, packed textures, lighting and the meadow presentation scene. Collections separate source meshes, LOD meshes and preview instances.
- `GIMP/GrassGround.xcf`: original generated grass albedo plus an editable seamless layer with a live GEGL seamless filter. Original artwork remains available as a hidden layer.
- `GIMP/LeafPalette.xcf`: independent painted gradient lanes for four greens, ivory petals, yellow buds, stems and recesses. This atlas shades actual leaf geometry; it is not a cutout-card atlas.
- `Textures`: generated original and processed PNG exports. Editable SVG UV palette lanes are grouped in `Textures/PaletteSources`.
- `References/MeadowStyle.png`: the user's supplied visual reference.
- `Scripts`: reproducible Blender and GIMP build scripts. Run from the Unity project root. Rebuilding intentionally updates this kit's exports and raw files.
- `Previews`: Blender/Unity renders and import/mesh audits.
- `Backups`: previous Blender source revisions, kept separate from the current editable file.

## Unity assets

Ready assets live in `Assets/Nature  Paradaise/Art/StylizedMeadow`.

- Three grass rosettes, two rounded bushes, white flowers and yellow flowers.
- Each regular prefab has a two-level LODGroup. `_Detail` prefabs use one low-detail mesh for native Terrain GPU instancing.
- Baked Unity `Meshes` remove FBX node rotation/scale from Terrain details; roots retain identity transforms and bottom pivots.
- One shared `MeadowLeaves` material uses a double-sided, shadow-receiving toon shader with root-weighted wind. Wind is applied in forward, shadow and depth passes.
- `Terrain/MeadowGrass.terrainlayer` repeats the 1024 px seamless albedo every 18 metres. Leaf motifs are three times larger than the former 6 m repeat, keeping them readable from the gameplay camera. No normal map is used, keeping the surface soft.
- `Profiles/MeadowVegetation.asset` maps grass terrain paint to the custom variants. It uses the existing TerrainLayerVegetation culling and exclusion system. Paths and fields can remain clear using their own terrain layer and existing exclusions.
- `Scenes/StylizedMeadowPreview.unity` is an isolated 12 × 12 metre native Terrain sample. Its preview is placed away from Map geometry and uses its own camera/layer, light and post-processing mask.

Preset density is per square metre: grass `0.075`, bushes `0.0035`, white flowers `0.012`, yellow flowers `0.008`. Grass width scale is `2–2.8` and height `1.8–2.3`; bush width `1.4–1.9` and height `1.35–1.75`; flowers use `1.65–2.05`. Larger, fewer clumps stay readable from the gameplay camera. Sample draw distance is `50 m`. Density, seed, scale, variants and terrain paint threshold remain editable in the profile Inspector.

Use **Nature Paradise → World → Stylized Meadow → Build Assets and Preview** to rebuild Unity materials, prefabs, profile and the sample from the FBX/PNG exports. The source set is decorative; harvestable wild grass keeps its separate model and gameplay identity.

Use **Nature Paradise → World → Stylized Meadow → Apply To Map** to apply the kit to the existing grass paint channels in Map. The active profile stays at `Assets/Nature  Paradaise/Resources/World/Terrain Vegetation.asset` so existing references remain valid. Paths, sand, terrain heights, painted weights and farm exclusions are preserved. The live Map preset uses 80 m detail culling; the isolated sample uses 50 m. The application report is in `Previews/MapApplicationAudit.txt`.

Editor setup scripts are grouped under `Assets/Nature  Paradaise/Script/Editor/World/StylizedMeadow`. Map has been applied on all four terrain tiles, including repair of the missing grass reference in the fourth tile's existing channel. `Previews/MapInGame.png` and `MapPlayAudit.txt` record the Play Mode check: seven custom detail variants per terrain, seasonal grass layer retained, and sampled exclusion cells empty.

The ground albedo began with generated artwork; GIMP makes the seamless editable project and exports. Leaf palette artwork and Blender meshes are deterministic authored geometry/UVs. The source files are genuine `.xcf` and `.blend` documents.

Native reopen checks are recorded in `Previews/BlenderSourceAudit.json` and `Previews/GimpSourceAudit.json`. Their scripts reopen the saved source files and verify meshes, UVs, packed textures, dimensions and editable layers.
