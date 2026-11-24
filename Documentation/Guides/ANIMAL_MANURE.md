# Animal manure

Born cows, goats, sheep, chickens, and ducks produce one pile at a random interval of 3–6 game hours. At the default 15-minute game day this is approximately 1.9–3.8 real minutes. Sleeping can produce one overdue pile per animal, rather than a burst of missed drops. At six uncollected piles per animal, further production pauses; existing piles never expire.

Use `Assets/Nature  Paradaise/Resources/AnimalManureSettings.asset` to adjust the interval, per-animal limit, pickup distance, and animation duration. The model is the Toon Farm manure mesh in `Resources/World/AnimalManure.prefab`. Piles do not block animal paths.

Buy **Garpu (Pitchfork)** from an equipment seller, select it in the hotbar, and approach a pile. **[E] Ambil Kotoran** plays the updated Player.fbx **Scoop Manure** animation. One manure item enters inventory immediately, without a progress bar. A full inventory leaves the pile untouched. The player faces the pile and remains locked only for the brief animation.

Livestock produces **Kotoran Hewan**; chickens and ducks produce **Kotoran Unggas**. The Fertilizer Maker keeps its existing recipes and adds two basic soil-fertilizer recipes: 3 Kotoran Hewan or 5 Kotoran Unggas → 1 Basic Fertilizer in 2 game hours. Existing crop-booster recipes also accept these same manure items.

SaveManager records pile IDs, producer IDs, world positions or barn-home/local positions, and the next production time per animal. Barn piles remain associated with the correct home through exit, revisit, and save/load, even if their producer later goes outside or is sold. Old saves initialize an empty manure state and random production timers.

Play Mode validation passed: random timer range, no overdue burst or double spawn, pitchfork requirement, full-inventory preservation, immediate inventory transfer, no duplicate pickup, Scoop Manure animation, released movement lock, outdoor and indoor JSON save/restore, barn visibility transitions with unchanged local pile positions, both recipe references, and queue/collection of the livestock-manure soil fertilizer. The temporary test harness was removed.
