# Farming effects

All production assets live inside `Assets/Nature  Paradaise` and are included by Git.
PC and mobile use the same gameplay contact callbacks; touch input needs no separate effect path.

| Activity | Contact and effect |
| --- | --- |
| Hoe | Existing animation impact delay; soil chips and soft dust at each tile successfully hoed, including upgrade patterns. |
| Sickle | Existing swing contact; thin arc plus green leaves at successfully cut gatherables, terrain grass patches and dead crops. |
| Hammer | Existing impact delay; rock surface chips, grey dust and tiny flash. Final hit uses a larger burst. |
| Watering | Animation-normalized pour window; droplets follow `WaterSpout`, then splash after their flight. First landing updates soil wetness and consumes water/stamina once. |
| Brush | Repeating animal-care animation stroke; dust/fur at the animal surface. Completed care adds a small heart and clean sparkle. |
| Axe | Existing axe impact; wood chips at the struck tree surface. Leaves follow the falling crown; larger dust burst occurs at landing. |
| Seed | Hand contact, 0.62 seconds after action begins; seed consumption, planting and grain/dust effect happen together. The last seed works. |
| Fertilizer | Hand contact, 0.62 seconds after action begins; successful soil update consumes the item and emits small grains. |
| Harvest | Existing pickup contact; leaves and a brief sparkle after the crop is successfully added to inventory. |
| Milk | Repeating milking stroke; small milk droplets at the held container's `MilkSurface`, with a sparkle after successful collection. |
| Shear | Repeating shearing stroke; wool at the animal surface. |
| Feed | Handover contact, 0.45 seconds after an accepted feeding action; grains at the animal or trough. |
| Fishing | Splash/ripple when the bobber lands; a larger splash at the recorded water position during the catch animation's pull. |

Failed or invalid impacts do not emit debris. Disabling a tool cancels its pending action and water stream. Shared effects respect game pause. Upgraded area actions emit at every successful target while sharing a fixed global particle budget.

## Assets and tuning

- `Resources/Effects/FarmActionEffects.asset`: shared material, density, particle budget and short SFX references. Loaded automatically by the first successful action.
- `Resources/Effects/farm_particles.shader` and `.mat`: URP transparent vertex-color particles. No particle textures or post processing are required.
- The shared particle shader renders at queue 3010, after the transparent field backdrop (3000) and hoed stamps (3001). The earlier default queue of 3000 let opaque-looking soil overlays hide all hoe debris despite successful emission. Normal depth testing still hides particles behind opaque buildings and other obstacles. Check visibility on a real blended FieldArea at gameplay zoom when changing effects; a gallery over opaque ground alone does not cover this case.
- `Audio/SFX/Farming/sfx_farm_*.wav`: 20 original synthesized, mono 22.05 kHz Foley/chime clips, all under one second; about 285 KiB of source audio total. Imports use Vorbis quality 0.45 and preload the short clips.
- Held tool prefabs: `WaterSpout`, `MilkSurface`, or `ToolImpact` child marks the physical work point. Existing anchor edits are preserved when applying setup again, except the hammer anchor which is refreshed on the flat +Y striking face rather than the +Z shaft/head tip.
- Shared runtime manager: at most 12 reusable emitters, 96 particles per emitter, 384 live particles overall, 4 simultaneous pour targets and 6 reusable spatial audio voices. No new manager is added to each tile/resource.
- Adjust density or budget in the catalog for a lower device tier. Physical mobile frame rate must still be profiled on the intended devices.

### Watering timing

`Resources/Profiles/Watering Action.asset` is shared by FarmingTool and PlayerHeldTools.
It replaces the old fixed 0.68-second start and 1.35-second action timer. Emission starts
at normalized Watering Plant progress 0.12 and stops at 0.72. The can blends from carry
to its pouring orientation during preparation, then returns upright over 0.72–0.92.
The 2.8-second source clip currently plays at state speed 2, but actual Animator state
progress drives the effect, including incoming transitions and changed animator speed.

Drops keep a fixed 0.18-second lifetime/flight instead of the general debris lifetime
variation. Splash timers run after that flight, not at emission. The first valid landing
commits soil watering, one water unit and stamina in the same frame, then publishes
one Water contact event. Subsequent small splashes do not repeat resource consumption.
The stream reads the animated spout every frame. Stopping emission leaves only the
last short flight and ground splash to finish; it does not keep emitting from an upright can.

Cancel, empty can, invalid/removed target or animation interruption stop the stream
and pending uncommitted impacts. Pause freezes the watering animator together with
the shared particles; resume restores its prior speed. The old water timer fields in
serialized scenes are no longer used. A 1.4-second fallback exists only for a missing
or inactive watering animator. The shared particle/material budget is unchanged;
each of the maximum four flows holds a fixed 16-slot contact buffer.

### Hammer strike orientation

The hammer's canonical +Z shaft follows the player's forward vertical swing plane.
The flat +Y end of the head is the striking face. PlayerHeldTools removes the imported
clip's lateral shaft tilt and maintains a signed face direction through the stroke,
including overhead windup. The contact pose eases in at 0.48–0.64 seconds, holds a
5-degree shaft elevation around the existing 0.68-second impact, and eases out at
0.74–0.92 seconds. This keeps the striking face down and slightly forward instead
of letting the head turn diagonally or bury its shaft tip in the rock.

The existing two-arm solver retains both palms on the handle, with no bone scaling
or changes to the clip's body/feet. Hammer damage, stamina, auto-approach, 0.68-second
impact timing and 0.95-second work duration retain their existing behaviour. The
mesh, scale, atlas material and painted inventory icon are reused. The pose correction
runs in the shared PlayerHeldTools path for keyboard and touch input.

For deliberate recalibration, stop Play Mode and choose **Nature Paradise > Player >
Correct Hammer Strike Grip**. It updates only the hammer entry and its ToolImpact
anchor. Normal hammer rebuild and farm-effects setup also retain the corrected face
convention. Restart Play Mode to replace an old equipped instance.

Use **Nature Paradise → Effects → Apply Farming Particles and Audio** to reapply asset wiring and audio import settings. Assets are already applied; clients do not need to run this menu after cloning.

Test effect changes in a separate scene. Do not use the main Map scene as a particle test scene.
