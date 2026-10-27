# Bottom gameplay information dock

All transient gameplay feedback uses GameplayInfoPanel: one fixed dark translucent panel with white text above the visible tools, inside the screen safe area. It contains the current action/activity and up to three notification rows. The dock measures the actual tools rectangle across canvas scales before rendering, so it clears the hotbar when resolution or safe-area layout changes.

## Routed sources

- WorldInteractionPrompt keeps its nearest-target selection, exact owner/anchor checks, keyboard input and touch callback. Its text now fills the shared bottom action row; it no longer follows an object's projected position.
- InventoryHotbarUI sends the selected tool/item instructions to the dock. The old selected-slot info card is hidden. Water amount is part of the same hint; the WateringCanSystem fallback no longer draws a label above the player.
- SaveLoadFeedback and PlayerPickupNotification retain their existing public APIs and forward to the shared notification queue. They do not create separate top/center feedback canvases.
- Fishing instructions, fishing session descriptions, animal-care action text and nearby construction status use the same information area. Construction no longer creates floating project text above a work site.
- Interactive menus, health/stamina, the clock/weather HUD and activity mechanics retain their own controls. Decorative reaction particles/icons are not floating text.

## Queue and timing

Each visible result lasts 2.5 seconds, including a final 0.5 second fade. Generic messages are clamped to 2–3 seconds. Only three notifications are visible; additional messages enter a FIFO queue and start their lifetime when promoted to a visible row. There is no overwrite/drop of older messages when the visible rows are full.

Repeated item receipts from the same inventory merge into the matching visible or queued notification. A visible receipt refreshes its lifetime. With artwork, the row shows the item's icon and xN; without artwork it retains the item name and xN. Repeated identical generic messages are deduplicated.

Modal menus hide the dock and freeze visible notification age. Owner-scoped care activities may still show their own hint when that activity is the only prompt-suppression owner. After an interaction expires, the selected tool hint resumes. Feedback is temporary UI state, so no inventory, field or save schema is changed.
