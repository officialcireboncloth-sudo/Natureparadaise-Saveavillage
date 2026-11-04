# Shared Cozy atlas extension 02

Created 3 November 2026 using built-in image generation for the four surface sources, then native GIMP 3 for packing, editable layers, color tuning, regular hex wire alpha, hay, normal and surface mask. No external image API was used.

Source master: `Generated_SurfaceMaster.png`. Editable outputs: BaseColor, Normal and SurfaceMask XCFs in this folder. Runtime PNGs and slot coordinates: `Textures/Shared/Cozy/NP_CozySurfaces_02_*` and `NP_CozyAtlasSlots_02.json`. Existing page 01 is intentionally unchanged; do not repack its UVs.

## Source prompt / construction specification

Create one square hand-painted stylized farming-game surface sheet with four equal quadrants and strict boundaries. Top left: clean smooth red-orange terracotta clay with subtle restrained ceramic variation, no tile pattern because individual roof tiles will be geometry. Top right: clean ivory painted timber, fine vertical grain, no chipping and no painted plank edges. Bottom left: continuous warm gray river stone surface, no masonry blocks. Bottom right: the same river stone with muted sage moss covering about 35 percent. Cozy natural materials, readable at small mobile resolution, flat albedo only, neutral diffuse color. No perspective, cast shadows, gradients from scene lighting, labels, text, frames, objects or external margins. Keep the four surfaces distinct and evenly lit.

The generated master is retained verbatim. GIMP adds a warm multiply glaze to clay; creates a distant tiled patch, hex wire with genuinely transparent apertures, golden stylized hay and reusable palette swatches. Normal is restrained independent relief; mask is R metallic / A smoothness. Base color 1024², normal/mask 512², 8-pixel safe padding. No water-animation texture is claimed by this page.

Rebuild in native GIMP 3 with python-fu-eval, setting `__file__` to the absolute `pack_extension_gimp.py` path before executing its contents. The script locates the repository from that path, loads the master, saves layered XCFs and exports PNGs/slot JSON. A successful run prints `COZY_EXTENSION_GIMP_PASS 10`. Then reimport through the Unity Coop exterior setup menu to retain mobile compression settings.
