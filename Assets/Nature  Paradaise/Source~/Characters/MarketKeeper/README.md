# MarketKeeper

Editable source for the uploaded elderly market keeper. Prepared 4 November 2026.

- `MarketKeeper.blend`: original rigged character with cleaned object/material/image names; links to the 1024×1024 runtime base color via a relative path.
- `MarketKeeper_Manifest.json`: original mesh/rig counts and bounds. Mixamo bone names stay intact.
- `prepare_character.py`: native Blender rename/export and FBX re-import check; uses the original local backup when present, otherwise the cleaned FBX. It does not add animation or change scene placement.
- `resize_texture_gimp.py`: optional reprocessing of the original 4K texture with native GIMP 3 NOHALO. Requires the local original in `ArtSource/MarketKeeper/Originals/`.

The original upload is backed up locally outside Unity imports and Git. Runtime assets are in `mesh/Character/MarketKeeper/`; no duplicate 4K map is included in the runtime or the Blender source.

Unity import uses one URP Lit material, a valid Humanoid avatar, mipmaps, and ASTC 6×6 on Android/iOS. The uploaded FBX has **no baked animation Actions**. Unity now retargets shared player Idle/Walk through the same `NPCCharacterMotion` system as Mina; `Resources/Characters/MarketKeeper.prefab` is fitted to 1.5 m and placed on the cart KeeperAnchor with a capsule collider. `MarketKeeper Actions.asset` reserves an empty `MarketAction` entry for the future custom Humanoid clip. See `Documentation/MarketKeeperAnimation.md`.

See `Documentation/MarketCartPreparation.md` and `Documentation/MarketCartProduceCatalog.json` for the verified Toonpack display inventory.
