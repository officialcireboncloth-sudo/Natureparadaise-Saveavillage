# Barn B — GIMP wood and roof atlas, 21 October 2026

Current timber revision: the two former clay roof slots now contain red-painted
wood planks (brick red base 137/63/43 and aged red 155/77/50). The roof remains
timber geometry and grain/normal relief; its color is authored in the atlas,
not a material tint. `paint_red_wood_roof_gimp.py` updates only the two roof
color slots. All 524288 roof pixels changed; zero pixels outside them changed.
Normal PNG, shared material, texture GUID/import settings are unchanged.
Native Unity preview/import: `Documentation/BarnB-RedWoodRoof-2026-10-21`.
Warm/dark timber slots are repainted; original red/cream and other atlas
pixels are retained. The exterior uses horizontal wood siding, broad timber roof
boards, a modeled cow-head sign, and a wall-mounted brass-colored bell/rope.
Color/normal dimensions remain 2048/1024 with existing GUIDs/compression/mips.
`ArtSource/BarnStyle_B/Scripts/paint_wood_roof_gimp.py` is the current GIMP
authoring entry; it paints color and float height/normal layers (GEGL scale 64).
Earlier clay scripts below are revision history and will restore the old roof.
`rebuild_wood_bell.py` exports the current geometry and bell rig. The one material
is shared by all LODs and the additional animated bell renderer. Static LODs:
4446/3812/2966 triangles, bell+rope 240. See
`Documentation/BarnB-WoodBell-2026-10-21` for 429 gameplay, 65 house interaction
and 271 normal/mobile shader checks and native Unity previews. Phone FPS unmeasured.

Earlier wood/clay authoring and normal-map history follows:

`TFP_Atlas_BarnB_Wood_1A.png` is a 2048 × 2048 variant of the Toon Farm Pack
atlas. Four 256 × 512 wood slots occupy pixels x=0..1023, y=0..511 from the
top-left: warm brown, dark brown, painted red, cream. Two 512 × 512 clay roof
slots occupy x=0..1023, y=512..1023: terracotta and a weathered variation.
The existing filename/GUID is retained for stable material references.
Other pixels match the original composite exactly. This area is unused by the Barn B geometry before
this revision; it is NOT unused by all Toonpack assets. Use this atlas only
with the revised Barn B meshes. The shared original PSD/GUID is unchanged.

`TFP_Atlas_BarnB_Wood_1A.xcf` is the editable GIMP source: an original atlas
composite layer and separate editable base, color, grain, knot, roof shading
and edge-wear layers. Grain and clay details are painted with GIMP's native
brush tools; the PNG is exported by GIMP. Export after
editing; Unity uses the PNG, not the XCF source.

The matching material uses the Barn-only `Nature Paradise/Barn Atlas Normal`
derivative of CustomToon, retaining the original ramp and lighting. The original
Toonpack shader is unchanged. Color texture import uses 2048 maximum,
bilinear filtering, mipmaps, clamp wrapping and automatic platform compression.
Surface UVs are inset 16px to reduce bleeding into neighboring slots. This is
prepared for PC/mobile but has not been profiled on physical devices.

`TFP_Atlas_BarnB_Normal.png` is one **1024 × 1024 tangent-space +Y normal atlas**.
It registers with the same wood/roof UV slots at half the color resolution;
all other swatches are flat. GIMP brush-authored height includes grain recesses,
knots, shallow convex clay and tile lips, excluding painted highlights/patina.
`TFP_Atlas_BarnB_Normal.xcf` retains the editable neutral-128 height layer and
generated GEGL normal layer. `Scripts/paint_normal_gimp.py` in ArtSource
regenerates it through GIMP, with GEGL scale 48. Unity material strength is 1.
Height painting uses floating-point precision to prevent terracing; broader
wood fibers and roof curvature remain visible at gameplay mip levels.

Normal import is linear Normal Map, clamp, bilinear, mipmapped, not CPU-readable.
Platform overrides are PC BC5, Android ETC2 RGBA8 and iOS ASTC 6×6, all max 1024.
Theoretical GPU texture payload including mips is about 1.33 MiB for BC5/ETC2
and 0.60 MiB for ASTC 6×6, shared by every barn level/LOD, excluding driver overhead.
One normal sample is added to surface lighting; no extra material, polygon,
displacement, parallax, per-object map or forward draw pass is added.
Shader target 3.5 retains a GLES3 baseline, instancing and existing LOD crossfade.
Normal perturbation is applied before both the Toon ramp and URP lighting;
DepthNormals and GBuffer also receive it. Shadow silhouettes use the real mesh.
Runtime mesh tangents are rebuilt from UVs in the Editor, retaining mesh IDs,
vertices, normals, UVs, triangle indices and existing prefab bindings.

`Nature Paradise > Barn > Apply Shared Normal Atlas` explicitly reapplies import
settings, shader/material binding and tangents. The surface relief toggle or
normal strength zero recovers the original Toon appearance (pixel-identical in
native validation). All 241 checks passed, including native PC renders and
GLES3/Vulkan Android and Metal iOS shader compilation, with relief enabled/off.
Reports/renders: `Documentation/BarnB-Normal-2026-10-21`. These are editor/compiler
checks, not physical mobile FPS measurements. Normal maps affect surface relief;
they do not replace scene cast/contact shadow or ambient-occlusion configuration.

Readability revision: initial narrow relief was too subtle at gameplay distance.
The current atlas is checked with identical 55-degree cameras, lighting and mesh
at 1280×720, normal keyword OFF/ON. Only the normal map changes. Source PNG/XCF
changed, but runtime resolution, mip count, compression and sampler count did not.

Updated Blender sources and FBX layouts are in `ArtSource/BarnStyle_B`.
`Scripts/paint_surface_gimp.py` regenerates the current wood/roof atlas using GIMP 3;
`Scripts/apply_surface_atlas.py` applies UVs to the Barn B sources and exports;
the merged exterior inherits the editable source face UVs, including the
triangulated runtime mesh, to prevent diagonal texture seams.
`Scripts/verify_surface_atlas.py` checks saved geometry/markers, UV validity,
unchanged atlas pixels and all five FBX round trips.
`Previews/SurfaceAtlasValidation.json` is the current verification report.
`Previews/Surface_Exterior.png`, `Surface_Interior_Lv1.png`, and
`Surface_Interior_Lv4.png` are actual Blender renders with the original cameras.
The pre-texture version is backed up in `Revisions/BeforeWood_20261021`;
the first wood-only pass is retained in `Revisions/BeforeSurface_20261021`.
The earlier wood-only scripts/reports/renders remain for revision history.

Unity integration completed 21 October 2026. Existing exterior Lv1–5 and interior
Lv1–4 prefab GUIDs now use Barn B meshes in `mesh/Buildings/BarnStyle_B`.
Exterior LODs are 5002/3500/2500 triangles, with one shared atlas material per
renderer and corrected front-facing roof panels. The collider follows the new
body/porch shape. Interior 1.75 scale, feed/animal/machine bindings and existing
colliders are retained. Scenes, building definitions, costs, capacity and player
saves are unchanged. Native Unity tests passed 213 Play Mode assertions.
See `Documentation/BarnB-Unity-2026-10-21` for reports and native renders.
Use `Nature Paradise > Barn > Apply Barn B Wood and Roof` to explicitly reapply.
The ArtSource folder remains excluded by the existing Git ignore rules; the
PNG, editable XCF and material in this Nature Paradaise folder are versionable.
