# Cozy atlas shading — 2 November 2026

Applied to the existing shared surface and foliage materials. The reworked house exterior/interior, Chicken Store, companion coop and Barn Store already reference these materials, so their mesh and gameplay prefabs do not need replacing.

## Visual treatment

- `Nature Paradise/Cozy Atlas`: continuous soft diffuse lighting with tangent normal mapping; no toon bands or outline. Relief strength 0.55, diffuse wrap 0.12 (foliage 0.22), texture saturation 0.97.
- Existing atlas colour, normal and metal/smoothness mask remain shared. Mask R controls metal and A controls smoothness, so roof/wood stay matte while metal receives a restrained highlight.
- Alpha-cutout foliage shades both sides. Shadow, depth and normal passes retain the cutout; desktop contact occlusion uses the renderer's existing SSAO feature.
- Glow stays on its existing emissive material, retaining the interior window's time/weather controls.

## Lighting and grading

The old preset stacked a 5000 K colour-temperature filter, warm light colour, exposure +0.4, saturation +12 and a noon sun multiplier of 1.55. These could overwhelm the authored atlas palette.

The new preset uses a near-neutral warm sun (1.15 intensity), noon multiplier 1.08, softer neutral fill, Neutral tonemapping, exposure 0, saturation 0, contrast +3 and restrained bloom. The extra colour-temperature filter is disabled both on the authored map suns and in `DayNightCycle`. Morning/evening transitions, weather and season modifiers, night palette and lamp scheduler remain in place.

## Reapply

**Nature Paradise > World > Apply Cozy Atlas Shading** updates the shared materials and lighting profile. In an open Map or Map_GameplayPreview scene it also updates the authored sun/fill and leaves the scene dirty for normal saving. It does not overwrite an unsaved scene automatically. The Cozy House reimport command reapplies the material settings after preparing the atlas.

## Validation

Native Unity 6000.0.81f1 rendered the current Map scene before/after, noon/night, and the Lv.1 house interior in the isolated CleanClone fixture. The shader compiled without reported shader messages. Actual Barn Store renderer bindings resolve to the shared materials and the expected atlas. See `CozyShadingReview/Audit.txt` and the PNGs in that folder.

Review PNGs use an sRGB render target, matching the linear project's display conversion. They are review files, not gameplay textures.

No new atlas textures, per-object materials, meshes, realtime lights or renderer features were added. The opaque shader uses the existing three atlas samples; foliage uses colour/cutout only. Instancing and the SRP material constant buffer are retained. Actual Android/iOS device performance and a mobile build have not been measured in this revision.

Only the two existing outdoor scenes' lighting fields were changed; gameplay, placement, saves, terrain paint, model UVs and interaction anchors were not changed. Local pre-change copies are in `ArtSource/CozyShadingReview/Before`.
