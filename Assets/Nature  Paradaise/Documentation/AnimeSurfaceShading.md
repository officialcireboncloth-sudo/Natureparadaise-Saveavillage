# Anime surface look

Applied through **Nature Paradise > World > Apply Anime Surface Look** in Edit Mode.
The preset configures the actual player material (`m_PlayerDummy`), project-owned
opaque toon materials, and the default material used by generated world props.
Pack grass, bush, terrain and water shaders retain their existing rendering and wind.
Existing terrain paint, population and camera framing are preserved.

## Material controls

Shader: `Nature Paradise/Anime Surface` (URP Forward and Forward+).

- **Toon Threshold / Toon Edge Softness** control the light-to-shadow transition.
- **Form Shadow Strength** controls the silhouette's volume shading.
- **Received Shadow Strength / Shadow Tint** retain cast shadows with a cool color.
- **Soft Rim Strength** adds a restrained edge highlight.
- **Character Skin Treatment** enables the player skin adjustment.
- **Skin Texture Smoothing / Skin Brightness / Skin Shadow Strength** make skin
  cleaner and lighter while keeping the clothes' original texture.
- **Skin Mask** can supply an authored UV mask. Disable **Detect Warm Skin Colors
  Within Mask** when using an exact mask.

The current single-atlas player uses conservative warm-color detection as a fallback.
Warm leather or hair can also match it; an authored skin mask is preferable for final
characters. Texture smoothing blends texture color, not mesh normals. The shader does
not replace the model, UVs or texture's painted detail. Face SDF shading requires an
authored face shadow map and is not part of this preset. This is an anime-inspired
implementation, not a recreation of Genshin's proprietary renderer.

## World lighting

`Resources/World/Garden Lighting` exposes noon sky/equator/ground and noon shadow
strength, in addition to the existing morning/evening/night controls. The anime preset
keeps bright sky fill, reduces lower fill, strengthens cast shadows, and reduces the
demo LUT contribution to 0.1. Exposure +0.4, contrast +6 and saturation +12 maintain
a bright colorful baseline. Midday bloom and the soft time-of-day sun palette stay
active. Terrain and foliage receive this scene lighting through their pack shaders.

Material backups are saved once per GUID to `Library/AnimeSurfaceBackups` before
conversion. Reapplying the menu restores the preset values. It saves the Map scene;
apply after finishing any intentional scene edits.
