# Nature Paradise Development Guide

## Project conventions

- `SampleScene` is the feature test scene. `Map` is reserved for final world building.
- Keep gameplay data in `ScriptableObject` assets and runtime state in focused components.
- Public APIs need a short XML summary when their intent is not obvious.
- Inspector fields use headers and tooltips for values designers are expected to tune.
- Comments explain intent, constraints, or non-obvious tradeoffs; they should not repeat the code.
- New systems must avoid one expensive `Update` per world object. Prefer a manager, registry, events, pooling, or interval-based scans.
- UI must support optional sprites. A readable fallback must remain available when art is not assigned.
- Persistent scene objects need stable IDs, and new persistent data must remain compatible with older save files.

## Inventory layout

- Backpack level 0: `4 x 4` (16 slots).
- Backpack level 1: `5 x 5` (25 slots).
- Each later level adds one row and one column, up to the configured maximum.
- Slots `0-3` are the four hotbar slots and are part of the same inventory list.
- Number keys `1-4` select the hotbar. Debug tool shortcuts use `F1-F6`.

## Git handoff checklist

- Commit `.meta` files together with Unity assets.
- Never commit `Library`, `Temp`, `Logs`, or generated build output.
- Open `SampleScene`, wait for compilation, and check the Console before pushing.
- Test save/load when changing inventory, field, weather, player status, or gatherable state.
