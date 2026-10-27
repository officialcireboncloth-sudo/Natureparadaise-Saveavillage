# Wide sickle sweep

The sickle now draws back on the player's right, sweeps horizontally from right to left, follows through briefly and returns to locomotion. Its existing Sickle animation supplies the base body pose; a player-only pose correction turns the hips/spine, lowers the hips and shifts weight into the sweep, keeps the feet on the animation's floor positions and aims the actual hand-held blade through the work arc.

The equipped model now comes from Toon Farm Pack `TFP_Sickle_01A`, with a compact approximately 65cm overall length beside the 1.5m player. It replaces the oversized imported model while retaining the generated mesh/prefab GUIDs. The model has 253 vertices / 161 triangles, uses the existing pack atlas material and has no colliders. Its `ToolImpact` marker sits on the actual hooked tip. During the right-to-left sweep, the inner concave sharp edge faces the direction of travel; the convex spine trails behind it. The inventory PNG is a painted illustration using the pack hook shape and the existing hoe/axe/hammer style, exported through GIMP at transparent 128x128 with the existing sprite GUID. It fills a comparable area of the UI slot rather than showing a thin model render. No extra runtime texture or material is introduced.

`Resources/Profiles/Sickle Sweep.asset` is the shared timing and area configuration:

- Forward fan: 120 degrees, centred on facing captured after approaching/alignment. A target already in the forward fan preserves the broad sweep; a prompted side target is faced before the action starts.
- Base reach: 0.75 metres in world space, shared by the logical cutting fan and physical blade path. Upgrades add 0.04 metres per level. Automatic approach closes the actual player-to-target gap to 0.75m even for upgraded tools.
- Windup 0.45 seconds, active sweep 0.50 seconds, follow-through 0.40 seconds.
- Blade height, modest torso turn/lean, weight shift, crouch, trail width and trail lifetime are adjustable in the same profile. The low stance lets the compact sickle reach the grass without lengthening the model.

The action collects candidate references once from world grass/weeds, dead crops and existing Terrain grass spatial buckets. Candidates are processed in angular order as the blade crosses the fan, rather than cutting a circle all at once. Healthy crops, other floors, rear/side targets and targets beyond the configured reach remain untouched. Each target still uses its existing drop, depletion, regrowth and save rules. Terrain detail uploads are grouped by dirty layer each action frame.

The thin trail is a camera-facing mesh ribbon sampled from the real blade tip, not a separate oversized arc particle. It reuses the shared farming particle material, has at most 32 points / 96 vertices, and fades with the same action clock. Small leaf fragments occur only on successful target cuts. The swish plays as the blade enters the cutting phase. No new textures are used.

Game-clock pause freezes the action and its owned Animator playback. Putting the tool away or disabling the gathering/action component cancels remaining contacts, restores the corrected bones, clears the ribbon and releases only the gathering movement lock. The same action is used by keyboard and mobile tool input.

The editor menu **Nature Paradise → Player → Apply Wide Sickle Fit** rebuilds only the compact sickle and carrying fit. It preserves the authored UI illustration. The general cutting-grip rebuild also delegates to this setup so it cannot restore the oversized model or replace the painted icon. The runtime pose correction is applied automatically; no Map scene changes or manual scene components are needed.

Validation uses the actual player rig and equipped model, native rendering of the ribbon, ordered forward/rear/range cases, real painted Terrain detail patches, dead/healthy crops, cancellation, pause and save/load. Main Map was not opened or saved for testing.

The close-range revision passed 53 native sickle checks, including the leading concave edge throughout the 0.75m sweep, physical tip contact, visible ribbon, dead/healthy crops, terrain patches, cancellation and save/load. The silent approach/walk revision passed another 51 checks, including measured 0.75m arrival distance and actual-speed animation cadence. The earlier icon revision also passed 171 UI checks for shared atlas packing and visible hotbar icons at PC/mobile resolutions.
