# Fish Pond PNG silhouettes

Generated with the built-in `image_gen` tool; transparent background enabled. Exported at 128×128 RGBA for Unity, preserving generated alpha. Four PNGs are in `Textures/UI/Icons/FishPond/`. Earlier painted/colorful drafts were superseded by the user's silhouette revision and are not used in the game.

Shared prompt (prepended verbatim to each subject):

> Production PNG UI icon for a cozy farming game, readable at 32 to 48 pixels. ONE centered solid WHITE silhouette on a genuinely transparent background. Clean smooth outer contour, simple recognizable chunky proportions, no outline, gradients, shading, highlights, texture, glow, shadow, scene, tile, circle, text, letters, numbers, logo or watermark. Square composition with generous transparent margins, shape occupies 78 percent of the longest dimension. Flat single-color silhouette, not a detailed painted illustration.

Subjects:

- Fish: A friendly freshwater fish side profile facing right, rounded body, clear tail fork, small dorsal and lower fins. One small transparent circular eye cutout, otherwise fully solid white.
- Machine: A compact feed-making machine with broad open funnel silhouette above a short rectangular body and two short feet. A single small transparent round knob cutout in front. Simple squat cohesive shape, no loose pellets or extra parts.
- Feed: A small plump feed sack with a tied narrow neck, two small tied fabric corners and a broad rounded base. No leaf symbol, no inner decoration. Silhouette must read as a cloth sack, not a vase.
- Health: A plump symmetrical heart health symbol with rounded lobes and a gently pointed base. One continuous solid white shape, no internal details.

The shared UI material samples the PNG alpha with a smooth cutoff, discarding faint generated halos and ignoring nonuniform near-white RGB. All icons use the shared GameplayHUDStyle.TextColor tint on dark panels; the later color revision preserved all layout dimensions. One sample per pixel; no normal map, glow, or additional lighting. Texture imports disable mipmaps/read-write; mobile overrides use ASTC 4×4. Source files total approximately 36 KB.

Binding: `Resources/UI/FishPondTheme.asset`. Reimport/rebind with **Nature Paradise > UI > Fish Pond > Apply PNG Silhouette Icons**. The earlier procedural icons remain as a fallback only if the theme is missing its sprites.
