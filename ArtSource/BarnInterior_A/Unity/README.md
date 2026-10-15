# Barn Interior A — Unity integration

Status: **PASS**, Unity 6000.0.81f1, 14 October 2026. Four approved layouts replace placeholder geometry in `Assets/Nature  Paradaise/Map/Scenes/Interiors/BarnInterior.unity`. Existing portal spawn ID `barn-interior-entry` and level prefab GUIDs are retained. Existing Map remains open and its unsaved work was not saved by this operation.

**Size revision:** barn exterior and interior were subsequently enlarged by 75% (1.75×). Current floor sizes are 21×17.5 / 28×21 / 35×24.5 / 42×28 metres. See `../Scale175/README.md`; geometry counts below are unchanged. The original entrance coordinates later in this document describe the initial integration, superseded by the scale revision.

| Level | Runtime triangles | Native mesh vertices | Mesh renderers before → after | Feed / animal capacity | Visual compartments |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 5,928 | 11,380 | 60 → 12 | 4 | 6 |
| 2 | 7,602 | 14,314 | 81 → 16 | 8 | 9 |
| 3 | 9,604 | 18,147 | 115 → 22 | 14 | 12 |
| 4 | 11,812 | 22,038 | 140 → 28 | 20 | 15 |

Each level uses the original `TFP_Atlas_1A` material. No new texture, shader or global material changes. Native meshes live under `Assets/Nature  Paradaise/mesh/Buildings/BarnInterior_A`; original source FBX CPU read/write copies are disabled after import. Optimized mesh assets merge shell, prop clusters and distribution while retaining separately movable troughs and separate feed fills. Original layout FBXs remain available for detailed authoring. Reverse faces are added only to open connected patches on machinery (10/10/20/20 triangles) so hopper interiors render correctly with Unity's culled atlas material.

Prefab paths remain `Assets/Nature  Paradaise/Prefabs/Barn/Interior/BarnInteriorLayout_Lv1..4.prefab`. Each carries `BarnInteriorLayout` bindings for portion markers, fill renderers, animal positions and the FeedMaker interaction point. Storage still belongs to `AnimalHome`; original capacity and save fields were not edited. Levels 1–2 have some spare decorative compartments; levels 3–4 share a compartment renderer between logical portions where required. The interaction text calls these logical slots **Porsi**. When a shared compartment contains one portion, its other empty portion remains selectable at the same location, and the fill remains visible until both portions are empty.

Runtime selects exactly one layout for the current barn level. The controller uses authored visuals instead of creating `TroughContainer` / `FeedFill` cubes. Legacy Coop layouts with no binding component retain their old generation path. The new FeedMaker marker is bound to the existing dedicated FeedMaker interaction; no process-tank simulation was added.

Box colliders cover the floor, three walls, front cutaway stubs, troughs and major rear fixtures. Entrance is at world `(0,-199.85,-8.3)` above the floor at Y=-200, preserving separation from the additive world. The old exit cube renderer/collider is disabled while its interaction marker is retained. Animal markers are authored per level in clear areas. No new animal meshes or NavMesh were generated; existing animal interior routines place residents at these markers.

## Validation

- `UnityApplyResult.json` and `UnityImportAudit.txt`: import axis/scale, original material, geometry counts, renderer reduction, serialized prefab/scene writes and build registration.
- `SavedAssetValidation.json` / `.txt`: reloaded prefab bindings for all four levels; one active layout; capacity 4/8/14/20; empty/full/consumed fill states; every portion selectable, including shared compartments; no duplicate primitive troughs; legacy Coop fallback; active collider clearance at all animal marker centres; entry above each floor; finite saved vertices and UV/normal arrays; disabled FBX read/write; preserved entry ID and exit marker.
- `Unity_Lv1.png` through `Unity_Lv4.png`: native Unity renders with feed fill enabled for visual review.

These checks run in the Unity Editor, including isolated preview instances. They are not a player-controlled Play Mode walkthrough or device FPS test. The Map scene and save data were not changed by the setup. To review, open a level prefab in isolation. Do not leave BarnInterior additively open alongside Map before Play: its controller would then start before the portal is entered. Final verification closes that additive scene and opens the level 1 prefab stage instead.

Original scene/prefab backups from before the first apply are in the earliest timestamped `ArtSource/BarnInterior_A/Revisions/` folder. Later revisions preserve subsequent iterations. `OrientationAttempt_Error.txt` is historical diagnostic evidence from an initial axis measurement; the current result files are PASS.

Editor menus: `Nature Paradise > Barn > Apply Approved Interior A` rebuilds from source exports; `Verify Approved Interior A and Show Prefab` validates saved assets and opens the isolated prefab. The one-shot request files are consumed once; a script reload alone does not reapply or overwrite layouts.
