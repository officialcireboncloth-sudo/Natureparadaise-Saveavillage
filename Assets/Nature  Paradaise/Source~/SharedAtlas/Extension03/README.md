# Pond surface atlas 03

Replaces the chalky bank and stretched plank appearance on the Level 1 Cozy pond. This is a new shared atlas page; existing pages 01/02 are unchanged.

Generated_PondSurfaceMaster.png was produced with the built-in imagegen tool. Generation.json retains the complete prompt and mode. Native GIMP packs the material quadrants into NP_CozySurfaces_03_BaseColor.png at 1024 x 1024, plus a shared prop palette. Independent editable relief and surface masks are 512 x 512. Native XCFs retain the layers. Rebuild with GIMP 3's python-fu-eval interpreter and pack_pond_atlas_gimp.py.

| Atlas region | Use |
| --- | --- |
| PondRock | Dark gray, softly varied river stone |
| PondRockMoss | Gray stone with restrained olive crevice moss |
| PondWood | Horizontal warm aged oak grain, sparse small knots |
| PondPebbles | Small submerged river pebbles |
| PondIron / PondBrass / PondTeal | Machine and metal prop surfaces |
| PondDark / PondBurlap / PondEnd / PondRope / PondLeaf | Shared timber, bag, end-grain, rope and leaves |

The wood is mapped by physical dimensions: long face direction follows texture U, cross-grain crops use 1.45 metre coverage and end faces sample end grain. Each plank uses its own crop. UVs remain inside eight-pixel-padded atlas regions. The pebble bed uses six continuous quads with repeated texture sections to maintain a small stone scale. Fine underwater pebbles are texture detail rather than individual geometry.

NP_CozySurfaces_03 uses the existing Cozy Atlas shader, 0.78 environment fill, 0.08 diffuse wrap, restrained normal relief, and ASTC 6x6 mobile overrides. Near pond geometry uses this material plus shared foliage; distant LODs use only this surface material. Water remains an independent mesh using a pond-specific teal variant of the existing Toonpack shader. No water image is included in this atlas.
