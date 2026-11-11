# G14 Daisy and Moonflower

Both crops have five planted phases and a detached harvest model. Daisy uses a basal rosette, scalloped leaves, and three upright heads with separate cream ray petals and a golden disk. Moonflower uses entwined stems, curved tendrils, heart-shaped leaves and a continuous five-lobed trumpet.

- Editable mesh source: `Blender/G14_DaisyMoonflower_Master.blend`. Each phase is an independent collection, with petals, leaves, stems and tendrils editable as individual meshes.
- Editable paint source: `ArtSource/Crops/Scripts/paint_g14_atlas.py`, `ArtSource/Crops/Scripts/g14_daisy_surfaces.py` and the three source PNGs in `Textures/`. The nine surfaces occupy reserved rectangles in `NP_Plants_Unified_01`. Runtime UVs are assigned to the shared atlas in the Blender master and exports.
- Unity assets: `mesh/Crops/G14` and `Prefabs/Crops/G14` inside `Assets/Nature  Paradaise`. Each prefab has one opaque renderer per LOD and uses the existing shared instanced material. `Blender/G14_Manifest.json` records near and distance triangle counts.
- Gameplay: five watered growth days, all seasons, 5 G per seed and 12 G per normal harvest. Both items use the existing nonfood flower and mutation rules. The seed packets appear in the `Benih` tab of the existing shop in `Map`.
- Phase renders: `ArtSource/Crops/Previews/G14_DaisyMoonflower_Preview.png`. Render the master with `render_g14.py` and compose with `compose_g14_preview.py`.

## Daisy texture revision

Daisy has green pigment gradients and branching veins, warm ivory ray petals with longitudinal ridges, separate florets on the golden disk, striped seeds and a dedicated stem surface. `ArtSource/Crops/Previews/G14_Daisy_R02_Detail.png` compares the original and revised textures under identical studio lighting, with an additional view at the Map camera scale.
