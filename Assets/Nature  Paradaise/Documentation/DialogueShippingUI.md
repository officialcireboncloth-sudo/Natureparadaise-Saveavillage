# Dialogue and Shipping Bin UI

DialogueService conversations share the bottom dialogue presenter, including authored narrative testing scenes. Fill Portrait and Role Description on each DialogueSpeakerSO. Missing portraits remain empty. Optional panel/relationship artwork: Nature Paradise > UI > Dialogue > Select Image Slots; select NPC assets through Select NPC Portrait Slots.

W/S or up/down chooses an answer; Enter confirms. Enter/E/Space continues a line without choices. Esc ends conversation. Choice conditions, commands, speaker overrides, quest events, and flags use the existing DialogueService. The service retains its configured clock pause and player movement locks. No relationship values are invented.

Shipping Bin uses the shared chest presenter with its own ShippingBinTheme image slots: Nature Paradise > UI > Shipping Bin > Select Image Slots. Ingredient/item artwork comes from existing ItemSO sprites.

Drag bag to bin to deposit a whole stack; drag back to bag to withdraw using the inventory's existing placement rules. W/A/S/D selects, Tab switches side, E deposits the selected quantity, R takes it back, Shift+E/R transfers a whole stack. Quantity buttons remain available. Unsellable items are visible and cannot be deposited. Valuable sales retain confirmation; Enter confirms and Esc cancels the confirmation before closing.

The bin still merges compatible listings, preserves quality/fish size/unit price snapshots, and pays on daily reset. Its existing unlimited capacity is shown as number of item groups rather than a made-up capacity. Viewing the bin pauses gameplay; close/disable restores its own locks. Buttons are gray with green pointer hover and cyan selected grid outlines.
