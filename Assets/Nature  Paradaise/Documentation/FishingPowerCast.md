# Fishing: power cast and catch presentation

Use the Fishing Rod, face a marked fishing zone, hold F to charge, and release F to cast.
Press F when BITE appears; hold/release F to control the existing reel minigame.

FishingTestScene includes compact River (left), Lake (middle), and Ocean (right)
zones under 30_WORLD/FishingRegions_Editable. Walk along the bank to
switch zones. The Ocean pool includes Tuna, which requires at least 67% reach
within the targeted zone.

## Player animation clips

| Clip | Source frames at 30 fps | Playback |
| --- | --- | --- |
| Fishing Charge | 0-30 | Scrub with power; hold last pose at MAX |
| Fishing Cast Low | 0-48 | 1.6 seconds |
| Fishing Cast | 0-48 | 1.6 seconds |
| Fishing Cast High | 0-48 | 1.28 seconds (1.25x) |
| Fishing Idle | 0-90 | Loop |
| Fishing Reel | 0-60 | Loop throughout reel minigame |
| Fishing Catch | 0-75 | 2.5 seconds, then Hold Two Hands |

## Editable settings

- Player > FishingSystem: chargeDuration (1.8s), bobberFlightDuration (0.7s),
  castReleaseNormalizedTime (0.45), resultDisplayDuration (3s).
- Fishing Zone > Transform: Position (X/Z center and Y landing height).
- Fishing Zone > FishingSpot: Radius (meters), Water Type, Location Name, Fish
  Pool, Junk Pool, Base Junk Chance (12%), and Maximum Cast Distance (12m).
- FishDefinitionSO: waterTypes, minimumCastPower, size range, time, season,
  weather and existing minigame balance. CSV supports minimum_cast_power.

In Edit Mode, drag `Prefabs/World/FishingZone.prefab` into the scene or choose
**Nature Paradise → Fishing → Create Fishing Zone**. Move the new GameObject
into place and set its Position Y to the local surface
height. Duplicate it for additional spots. A single decorative water mesh can
cover several spots, with each spot keeping its own position, radius, and fish
pool. Water Type filters the fish list and needs no water object or collider.
Assign Fish Definition assets from `Resources/Fishing` to Fish Pool. An empty
list uses all definitions matching the zone's Water Type. Selecting a zone in
the Scene view shows its cyan outline and diameter over the water and terrain.

The cast targets the first reachable zone along the player's facing direction.
Charge moves the landing point from the near edge toward the far edge of that
zone at its configured height. Place zones in clear locations within casting
distance of where players can stand.
Charging shows a landing ring on the water and target distance in meters;
the cast panel keeps the actual distance after release. The orange bobber
uses an unlit material so it remains visible under changing daylight.
Near casts favor small fish; medium
casts use a uniform size roll; far casts favor large fish. Junk is rolled at
all distances and is still affected by bait.

The rod follows the hand bone, bends with power and reel tension, and supplies
the line origin. The bobber launches partway through the swing, flies through
an arc and creates a small landing splash.

After Catch, the player holds the item and the right-side fishing panel shows
the item name, size, location and inventory delivery countdown. After 3 seconds,
the item enters inventory. A full inventory produces a ground pickup. Saving
or interrupting an earned result commits it exactly once; loading discards the
abandoned presentation and restores the saved inventory.

Fish/junk ItemSO.worldPrefab is used when assigned. Items without a model use
simple temporary fish/junk shapes; replace worldPrefab to supply final art.

## Fishing UI

Active fishing uses a screen-space uGUI panel on the right: a vertical fish target
and reel marker, catch percentage/progress, yellow line tension gauge, and live
instructions. Charging, casting, waiting, bite, catching, and result use the same
panel. Instructions follow FishingSystem.ReelKey (F by default); the existing
minigame controls and catch/reward logic are retained.

The inventory hotbar and selected-tool hint hide for every non-Idle fishing state,
including the result presentation, and return when the session ends or fishing is
disabled. Hidden hotbars cannot receive pointer input. The legacy tool toolbar
also follows this visibility rule.

Use Nature Paradise/UI/Fishing/Select Image Slots to select
Resources/UI/FishingUITheme.asset. Panel artwork, fish silhouette, reel indicator,
progress ring, upward arrow, and catch placeholder are optional Sprite slots.
Missing illustrations remain empty. A caught item's existing ItemSO.icon is used
when supplied. Screenshots are layout references and are not cropped for assets.
