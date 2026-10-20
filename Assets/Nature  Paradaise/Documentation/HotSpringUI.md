# Hot spring HUD

Native, editable uGUI preparation for the future bathing controller. The HUD displays supplied data and offers an exit request. It does not heal the player, run a recovery timer, change stamina costs/capacity, install pool triggers, teleport the player, play bathing animations or write saves.

## Assets and preview

- `Resources/UI/HotSpringUI.prefab`: date/weather, gold, current/next stamina bar, recovery feedback, E/Keluar button and elapsed bathing time. No full-screen modal/backdrop obscures the scene.
- `Resources/UI/HotSpringTheme.asset`: optional image references for weather, coin, spring, exit, panel, stamina track and stamina fill. Empty slots are invisible. Assign a Sprite once in this theme; images share the reference without copying textures. Surface colors and typography use the existing `GameplayHUDStyle`.
- The existing inventory toolbar remains visible at 45% opacity and cannot receive input. Its normal tool action hint is hidden while bathing.

Editor menus: **Nature Paradise > UI > Hot Spring > Open Editable Prefab**, **Select Image Slots**, **Preview in Play Mode**. Preview opens a static UI snapshot (38/50 stamina, +1 per 10 seconds, 01:20 elapsed), without changing the actual player's stamina or starting recovery. E/Esc or clicking Keluar closes it.

**Create or Update Editable Prefab** rebuilds the layout. Store optional artwork in the theme; rebuilding replaces manual child layout changes. The prefab has an ordinary Unity hierarchy and can be edited in Prefab Mode without rebuilding.

## Connecting the bathing controller later

Finish other active interactions/modals before opening. The controller owns pool entry/exit, placement, animation, elapsed time and recovery rules. For example:

```csharp
var view = new HotSpringViewData
{
    dateText = dateLabel,
    timeText = timeLabel,
    gold = currentGold,           // -1 hides unknown gold
    weatherIcon = weatherSprite, // optional; falls back to the theme
    stamina = status.Stamina,
    maximumStamina = status.MaxStamina,
    recoveryPerTick = configuredAmount,
    recoveryIntervalSeconds = configuredInterval,
    elapsedSeconds = elapsed
};
HotSpringUI hud = HotSpringUI.Show(view, onExit: EndBathing, player: player);
// Show returns null when another interaction is active or the prefab is missing.
// After a real recovery tick, clock change or duration update:
view.stamina = status.Stamina;
view.elapsedSeconds = elapsed;
hud?.SetData(view);
```

`SetData` snapshots the data. Mutating the supplied object alone does not update the display. No recovery value is hard-coded in the runtime HUD. The example recovery amount in the preview is only an illustration. The existing `PlayerStatusSystem.RestoreFromRecoverySource` / `RestoreStamina` APIs are available to the future controller; the UI never calls them.

Next stamina/gain is clamped to the supplied maximum. Full stamina shows **Stamina penuh**, hiding the next-tick preview. A zero/unknown maximum shows **— / —**. Zero recovery amount or interval hides the recovery preview. Invalid/negative numeric input is sanitized; elapsed durations support hours.

For a scene-authored instance, call `Present` instead of `Show`; its canvas is hidden before opening. The HUD detaches to a unit-scale scene root so NPC/pool parent scales do not squash its layout.

## Input and cleanup

Opening acquires its own movement lock and interaction-prompt suppression, hides the standard upper HUD/map/pause launcher, and disables gameplay keys/tools. The existing toolbar stays dimmed. World time and `Time.timeScale` continue unchanged so a future bathing controller can run recovery normally.

`RequestExit` closes the HUD before calling the supplied exit callback once. If no callback is supplied, the Inspector's **On Exit Requested** UnityEvent is invoked instead. The callback takes precedence. A request with no handler still closes the HUD. `Closed` fires once for all close paths; use it to clean up controller-owned recovery if the UI is disabled/destroyed externally. Exit callbacks must be safe if `Closed` already ended the session.

`Close`, disabling and destruction release this HUD's movement/input context and restore the previous cursor/map visibility and normal HUD. Other owners' movement locks and clock pauses remain owned by their callers. Scene/world gameplay is not installed by the editor builder.

Validation captures and results: `Documentation/HotSpringUI-2026-10-16/` outside Assets.
