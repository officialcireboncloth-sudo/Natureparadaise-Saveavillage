# Weather and tool PNG artwork

The runtime artwork lives in `Textures/UI/Icons` under the Nature Paradaise folder.
Every icon is a physically **128 x 128 PNG with real alpha transparency**. The original
set was generated using the built-in imagegen tool and downscaled/exported with native GIMP.
The sickle illustration uses the compact Toon Pack hook shape with the same painted
wood, steel highlights and visual weight as the hoe/axe/hammer icons. It replaces the
thin model-rendered icon. Rebuilding the sickle grip preserves the illustration;
its sprite GUID stays unchanged. The current generation prompt is in `UIIconPrompts.json`.
Original large artwork stays locally in the ignored `ArtSource/UIIcons/Originals`.
The full generation prompt set is recorded in `UIIconPrompts.json` next to this guide.
The 24 runtime PNG files remain in one shared mobile atlas; the revised sickle replaces
its original image rather than adding another sprite.

## Folder and filename conventions

```text
Textures/UI/
  Icons/
    Weather/icon_weather_sunny.png
    Tools/icon_tool_hoe.png
    Currency/icon_currency_gold.png
  Atlases/atlas_ui_weather_tools.spriteatlasv2
```

PNG filenames use lowercase snake_case and a category prefix. Designer-facing
weather/tool names still come from gameplay data. The import menu also migrates the
previous filenames using Unity AssetDatabase.MoveAsset, retaining each .meta GUID.

## Inventory tools

| ItemSO | PNG | Indonesian name |
| --- | --- | --- |
| Hoe Tool | Tools/icon_tool_hoe.png | Cangkul |
| Sickle Tool | Tools/icon_tool_sickle.png | Sabit |
| Hammer Tool | Tools/icon_tool_hammer.png | Palu |
| Watering Can Tool | Tools/icon_tool_watering_can.png | Penyiram |
| Axe Tool | Tools/icon_tool_axe.png | Kapak |
| Fishing Rod | Tools/icon_tool_fishing_rod.png | Pancing |
| Shears Tool | Tools/icon_tool_shears.png | Gunting bulu |
| Pitchfork Tool | Tools/icon_tool_pitchfork.png | Garpu kandang |

All eight `ItemSO.icon` references are assigned. Inventory, hotbar, storage,
shops, drag previews and detail artwork therefore use the same source sprite.
Detail artwork keeps its existing `inventoryIllustration` override if supplied.

Additional existing model/action artwork:

- `icon_tool_brush.png`: animal grooming action.
- `icon_tool_milker.png`: milking action (cow and goat).
- `icon_tool_pickaxe.png`: mining HUD symbol. The alternate pickaxe model already exists;
  mining gameplay currently equips Hammer. No new tool item or save ID is introduced.

## Weather

| WeatherType | PNG / label |
| --- | --- |
| Sunny | icon_weather_sunny.png / Cerah |
| PartlyCloudy | icon_weather_partly_cloudy.png / Cerah Mendung |
| Heatwave | icon_weather_heatwave.png / Panas Terik |
| Drizzle | icon_weather_drizzle.png / Gerimis |
| Rain | icon_weather_rain.png / Hujan Sedang |
| HeavyRain | icon_weather_heavy_rain.png / Hujan Lebat |
| WindRainStorm | icon_weather_wind_rain_storm.png / Hujan Angin Badai |
| Cyclone | icon_weather_cyclone.png / Angin Topan |
| Thunderstorm | icon_weather_thunderstorm.png / Badai Petir |
| Snow | icon_weather_snow.png / Hujan Salju |
| Blizzard | icon_weather_blizzard.png / Badai Salju |

The main HUD and animal care clock use `WeatherForecastTVTheme`, including
`icon_weather_clear_night.png` and `icon_weather_partly_cloudy_night.png` from 18:00 until 05:59. The TV forecast
uses daytime symbols because a forecast describes a day's weather. Missing artwork
still has the lightweight vector fallback on the live weather HUD.

## Import and batching

`Textures/UI/Atlases/atlas_ui_weather_tools.spriteatlasv2` packs all 24 icons into a shared
texture, at most 1024 x 1024. Rotation and tight packing are disabled because the
custom weather Graphic uses rectangular UVs. Icons use bilinear filtering,
clamped edges, no mipmaps and no retained CPU readback copy. Android and iOS atlas
overrides use ASTC 6x6; desktop uses RGBA32. Runtime memory depends on the packed
texture, not the compressed PNG file size. Device FPS is not measured here.

Sprite packing must be enabled in `ProjectSettings/EditorSettings.asset`.
Commit that setting together with PNGs, their `.meta` files, the atlas, theme/tool
assets and UI scripts. Everything else in this icon change is inside Nature Paradaise.
Restart an already-open Unity editor after receiving the project setting change.

The isolated Unity 6000.0.81f1 validation passed **171 checks**, including actual
rendered pixels for all eight hotbar icons at 1280 x 720 and 720 x 1280, all eleven
live weather mappings, both night variants, real PNG transparency and shared texture
packing. The catalogue preview shows each icon at 128px and 48px. Device FPS is
not part of these checks.

To reconnect/import the same artwork, stop Play Mode and choose
**Nature Paradise > UI > Apply Weather and Tool PNG Icons**. This editor command
only updates the known icon slots and their import/atlas settings. It preserves
existing scene files, item identities, models and gameplay data.

## Gold artwork and top HUD

`Currency/icon_currency_gold.png` is a transparent 128x128 painted gold coin with an
embossed sprout, generated with the built-in imagegen tool on 2026-10-25 and exported
through native GIMP. Its complete prompt and runtime path are in `UIIconPrompts.json`
(entry `Gold`). The large original stays in ignored `ArtSource/UIIcons/Originals/Gold.png`.
The currency folder is included by the existing root Icons folder in the 1024px UI atlas;
the existing mobile ASTC settings apply. No duplicate gold texture is needed.

`Resources/UI/InventoryTheme.asset` supplies `coinIcon` for both inventory and HUD.
HUDManager's optional `goldIcon` can override that reference. Missing artwork retains
a simple currency fallback. The actual balance and Indonesian thousands formatting
continue to come from ScoreManager.

The runtime top bar is 690x88 reference units: weather icon/caption, day plus season,
time, and gold each have a separate column. Dividers, explicit padding and an 84%
opaque green background improve grouping and contrast. Menu stays outside the bar,
with a 14-unit gap and the same vertical centre. Its caption stays on one line; mobile
shows Menu without a keyboard shortcut. Existing safe-area and canvas scaling still
apply. Scene references do not need reassignment; restart Play Mode to rebuild old HUDs.

## Wood and Stone

`Materials/icon_material_wood.png` and `Materials/icon_material_stone.png` are transparent 128px painted resource icons. The Wood and Stone ItemSO assets reference them directly, so inventory, hotbar, pickup notifications and storage share the same artwork. They join the existing folder-based shared atlas (1024px, ASTC 6x6 on mobile). The artwork import menu also reimports and reconnects these material icons. Large originals remain in ignored ArtSource; built-in imagegen prompts are recorded in UIIconPrompts.json.

## Companion command icons

Six transparent 128 x 128 PNGs under `Companions/icon_companion_*.png` cover Horse, Dog, Follow, Stay, Home and Mount. They are flat pure white silhouettes with no shading or colored details, including the horse/dog species marks. Built-in imagegen supplies the shapes; native GIMP cleans the alpha contour, removes stray islands, normalizes margins and exports opaque pixels as #FFFFFF. Full generation/edit prompts are recorded in UIIconPrompts.json. Large source images remain in ignored ArtSource.

CompanionCommandTheme references all six sprites. The UI uses white sprite tint, while optional missing sprites retain the existing vector fallback. They join the same folder-based 1024px atlas with ASTC 6x6 mobile compression, no mipmaps and non-readable runtime textures. `Nature Paradise/UI/Apply Companion PNG Icons` reimports and reconnects this set without rebuilding scene objects.
