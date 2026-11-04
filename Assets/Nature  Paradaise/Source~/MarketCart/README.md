# Cozy market cart

Reference: `MarketCart_Target.png`, approved 4 November 2026. This replaces the canopy stand visual while keeping the same MarketStand gameplay prefab and save identity.

## Editing and rebuilding

The real editable master remains **`Source~/MarketStand/MarketStand_A.blend`**. Collection `01 EDITABLE CART AND BASKETS` contains separate counter boards, body cladding, shelf, wheel rims/spokes/hubs, two shafts, cabinet frames/doors/panes, four woven baskets, one potato crate and genuine Toonpack produce copies. `02 RUNTIME LODS` is hidden in the master; `03 PREVIEW ONLY` is not exported.

Run `build_market_cart.py` in Blender to regenerate the master, `mesh/World/MarketStand/MarketStand_A.fbx`, three LODs and `MarketCart_Manifest.json`. The existing `BuildMarketStand.py` entry point delegates here. Run **Nature Paradise > Art > Apply Cozy Market Cart** in Unity, or the existing Apply Atlas Market Stand menu, to rebuild the tested visual bindings. The ShippingBin rebuild remains independent.

`ToonpackProduce.json` is a native Unity export of the supplied prefabs' near meshes and UVs. It includes original prefab paths/GUIDs. It avoids depending on Blender's unsupported ASCII FBX importer for some vendor models. The original Toonpack assets are never edited. The Blender preview atlas is packed into the master; Unity references the original Toonpack PSD directly.

## Reference details

- Two wooden wheels, 10 spokes each, dark iron tyres, layered wood hubs and axle caps.
- Open lower front shelf, closed plank sides/back, thick bevelled counter and framing.
- Two raised rear shafts, turned grips, carved collars and diagonal braces.
- Two cabinet display levels, glazed top/front/back/sides, centre sliding-door stile and round pulls.
- Tall round vegetable basket at front left, rectangular tomatoes in front centre, green pears at front right. Lower shelf has a potato crate and lidded wicker basket.
- Banana, orange, pomegranate, tomato, carrot, purple eggplant, green pear, potato, cauliflower, Salad and Leek use Toonpack geometry. Pomegranate replaces unavailable dragonfruit; Salad/Leek replace the reference's unavailable pakcoy. The reference's original photoreal produce is not recreated.
- Editable art width is baked for Unity's front camera convention, so the reference's left/right layout matches in game. Runtime transforms use positive unit scale; export markers make the imported coordinate frame explicit.

## Materials and mobile

Counter/body/wheels/baskets use the existing `NP_CozySurfaces_01` atlas: wood, end grain, iron, rope and wicker. Grain is cropped at physical scale along each timber. Every cart triangle stays within a padded swatch. No new runtime texture is introduced.

Each LOD has two opaque/cutout renderers (cart and produce) and one transparent renderer (glass). Glass consists of five single panes, without nested solid glass shells; the inexpensive URP Unlit transparent material has no depth writes or shadow caster. Three LODs contain **22,897 / 13,545 / 5,441 triangles**, including 10 glass triangles. Five simple box colliders cover the body, wheels and shafts; no MeshCollider is used.

`KeeperAnchor` is between the handles; `FrontInteractionAnchor` is in front. The animated MarketKeeper prefab is now attached by `MarketKeeperCharacterSetup`; the cart rebuild preserves that attachment when the keeper prefab exists. Stock/listings remain gameplay data, separate from the decorative produce and NPC.

Native Unity preview and audit results are in `Documentation/MarketCartReview/`.

## Opening schedule and travel

The gameplay prefab now includes `MarketCartSchedule`: 06:00–20:00 on Sunny, PartlyCloudy, Cloudy and Heatwave only. It travels with the keeper from/to an obstacle-checked offscreen point, retains stock while hidden, and blocks transactions during travel/closure. Loading restores presentation under the existing black loading screen, without replaying sales using unknown historical weather. No texture or mesh rebuild is required. See `Documentation/MarketCartSchedule.md` and its 65-check native Unity audit. The repeatable isolated audit source is in `Tests/MarketScheduleAudit.cs`.
