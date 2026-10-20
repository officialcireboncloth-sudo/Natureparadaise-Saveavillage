# NPC gift UI

UI-only preparation. The panel selects a gift and emits a request; it does not remove inventory, grant relationship points, enforce a calendar, increment weekly totals or write saves. NPC interaction/relationship rules will be connected later.

## Editable assets

- `Resources/UI/NPCGiftUI.prefab`: native uGUI hierarchy, editable in Prefab Mode. Canvas is a screen-space scene root, independent of NPC scale. Portrait on the left; six gift cards, selected-item details, confirmation and cancel on the right.
- `Resources/UI/NPCGiftTheme.asset`: optional panel/card/button artwork, header/give icons, quality-star artwork and decoration. Empty slots remain invisible. Colors, typography, rounded surfaces and button behavior use the current shared gameplay UI; green is the hover/selection accent.
- Portrait: existing `DialogueSpeakerSO.portrait`, or a portrait override passed when opening. There is no generated/cropped NPC artwork.
- Item cards reuse `ItemSO.icon`; the detail reuses `inventoryIllustration`, falling back to the same `icon`. No texture copies or per-UI item image databases are created. Quality uses the optional shared star sprite or a readable numeric fallback.

Menus: **Nature Paradise > UI > NPC Gift > Open Editable Prefab**, **Select Image Slots**, and **Preview in Play Mode**. Preview uses existing NPC/item assets and only reports a selected item; it grants no gifts. **Create or Update Editable Prefab** regenerates the layout; keep optional artwork in the theme and item/NPC assets rather than editing generated child image references before a rebuild.

## Connecting the future service

End an active dialogue/modal first. Supply `NPCGiftOption` rows containing the original ItemSO, source inventory slot, available stack count, quality and caller-provided eligibility. Rows with a null item or zero count are not shown. Give each source slot its own row to retain quality/source identity.

```csharp
NPCGiftUI panel = null;
panel = NPCGiftUI.Show(speaker, choices, request =>
{
    // Future service: validate the source slot and eligibility, then apply one gift.
    // request.Recipient / Item / InventorySlot / QualityStars / Quantity (always 1)
    // panel.CompleteRequest(accepted, feedback);
}, giftsGiven, weeklyLimit, player, portraitOverride);
```

The Inspector's **On Give Requested** event is an alternative to the callback; use one submission path. The callback takes precedence. With no callback or persistent event handler, Berikan is disabled. A request remains pending until `CompleteRequest` acknowledges it, preventing duplicate submissions. Rejection keeps the panel open; success closes by default. Refresh changed inventory/eligibility with `SetOptions`, and counters with `SetWeeklyCount`. Negative counter values mean unknown and hide the counter. No default “two gifts per week” rule is implemented by the UI.

`Closed` is available for caller cleanup. If a service performs asynchronous work, it owns cancellation/validation when the panel closes. The UI snapshots supplied rows, so subsequent caller-side mutations do not silently change the pending selection.

## Controls and lifecycle

A/D or arrows choose a gift, Enter requests confirmation, Esc/Batal closes. Clicking a card and the buttons uses the same methods. More than six entries page automatically. Empty inventory, missing portraits/icons, unavailable entries, supplied weekly limits and long descriptions have readable states; descriptions scroll without shrinking to unreadable text.

Opening locks player movement, pauses the game clock/world, suppresses interaction prompts and hides the regular HUD/map launcher. Closing, disabling or destroying the panel releases its own locks and restores the previous time scale, cursor and map visibility. Existing external pause/movement tokens remain owned by their callers. Normal gameplay keys are blocked while this modal is open.

Validation evidence is recorded in `Documentation/NPCGiftUI-2026-10-16/` outside the Unity Assets folder. No Map/NPC interaction installation or gift gameplay state is added by this task.
