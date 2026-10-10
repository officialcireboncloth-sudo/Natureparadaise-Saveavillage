# Mining UI / cave preset — 10 October 2026

- E with Hammer selected: Pecahkan Batu. F tool input remains compatible. The secondary prompt says “Stamina digunakan”; actual fractional stamina/weather costs remain unchanged.
- Each valid rock impact grants exactly one primary configured drop to the bag. The first nonempty drop with chance > 0 is the node's primary reward (ore nodes can assign ore instead of stone). The legacy amount/chance roll is no longer used for rocks. If no primary item exists, Stone is used. Last impact depletes the rock without awarding a second drop. Insufficient tool level/depleted rocks give nothing.
- If the bag is full, that one reward becomes a loose pickup at the rock; no item is silently lost. Discovery notification appears when actually collected, using the shared ItemSO.icon.
- All existing PlayerPickupNotification callers now use a pooled screen-space top-center toast rather than player TextMesh. Four simultaneous entries, unscaled lifetime, shared Sprite references.
- CaveInteriorController marks cave scenes; MiningHUD replaces the normal status/time HUD only in the current cave, preserving the existing hotbar and modal suppression. HUD reports actual gold/stamina/time; floor/name editable in Inspector.
- Empty artwork slots: Resources/UI/MiningHUDTheme.asset. No screenshot artwork was extracted. Mine/coin icons stay empty until assigned. Resource images use ItemSO.icon.

## Authoring

CaveInterior.unity sudah tersedia dan terdaftar di Build Settings. Nature Paradise → Mining → Create Cave Interior Preset selects this scene, or creates it additively if missing. It preserves current scenes. The preset has a debug floor, entry spawn, exit portal, empty rock and ladder/art containers. It is scaffolding, not a finished cave environment or procedural floor system.

Select an entrance object in the world, then Nature Paradise → Mining → Add Cave Entrance To Selected Object. E enters using the house's SceneTransitionManager additive load/fade and returns to the original exterior position. An optional exterior spawn ID can be assigned on the portal. Spawn position/rotation follow their Transform and the cave root; move them freely. No hard-coded world teleport coordinates.

Attach WorldGatherable to your rock prefabs, set Kind=Rock, Can Break With Hammer=true, tool level/durability and primary Drop item. Add cave meshes, lighting and floor ladder interactions later. Test the entry using Game View focus. No world entrance is placed automatically because its desired location/art has not been provided.
