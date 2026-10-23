# Nature Paradise: Git handoff

Open the project with Unity **6000.0.81f1**, let Package Manager and asset import
finish, then open `Map/Scenes/UI/MainMenu.unity` to start gameplay.

## Required project files

Commit together:

- `Assets/Nature  Paradaise` and its `.meta`, including all asset/folder metas.
- `Packages/manifest.json` and `Packages/packages-lock.json`.
- The tracked `ProjectSettings` folder, including Graphics, Quality, Editor Build
  Settings and ProjectVersion.
- `.gitignore` and project setup documentation.

The tracked Nature Paradaise folder includes game code, scenes, prefabs, animation
controllers/clips, audio, models, textures, shaders, materials, runtime Resources
and these required dependencies:

| Dependency | Location inside Nature Paradaise |
| --- | --- |
| TextMesh Pro essential fonts/settings/shaders | `Pack/TextMesh Pro` |
| URP PC/mobile assets, renderers, volume profiles | `Settings/Graphics` |
| Input System settings and actions | `Settings/Input` |
| Adaptive Performance simulator configuration | `Settings/Adaptive Performance` |
| Scene terrain data, including paint/trees/details | `Map/TerrainData` |
| Used SoStylized models/materials/textures/subgraphs | `Pack/SoStylized` |

Unity modules and registry packages are restored from the package manifest/lock;
their local `Library/PackageCache` is not project source. Generated Library,
Temp, Logs, IDE files and root Build/Builds output remain ignored. Optional
`.unitypackage` archives and their metas are not needed to open or play the game.
Authoring backups, source art and local validation workspaces remain excluded by
the existing ArtSource/Documentation rules. Runtime FBX/PNG assets are included.

## Historical cause and preserved references

The 15 October commit `574bc123` tracked assets outside Nature Paradaise. Commit
`5497714e` on 21 October removed those files while adding the restricted Assets
ignore policy. Graphics/Quality settings and package manifests stayed unchanged,
leaving live references to excluded files. Commit `09ff2f8d` restored TMP resources
inside Nature Paradaise but did not include the remaining rendering/world files.

The handoff repair relocates 85 required external files with their original metas.
All 85 GUIDs match their exact original paths in `574bc123`; current file contents
and import parameters are retained. This preserves current gameplay and old
references while allowing the required files to be committed under one folder.

## Client verification

Audit on 23 October 2026: a fresh project copy was made from Git-tracked files
plus pending versionable changes, with no source Library/Temp/UserSettings.
Original package manifests resolved unchanged. Native Unity validation passed
148 checks: 537 runtime Resources loaded, all nine enabled build scenes opened
with zero missing scripts, PC/mobile renderers and TMP loaded, and the native
Map render contained zero magenta pixels. The final file audit found no missing
asset metas, orphan asset metas, duplicate GUIDs/resource keys, missing literal
runtime resources, absolute external shader includes or local-only package paths.
The 6689 native dependencies detected by the project audit are all versionable
inside Nature Paradaise. This verifies editor import/references/rendering, not a
full gameplay or physical mobile performance run.

Commit and push all new dependency folders and their metas with the code/settings
changes. For a reliable comparison, clone into a new folder: an old checkout can
contain ignored assets imported by hand, including duplicate TMP GUIDs. Do not
copy the developer's Library, Temp, UserSettings or player save files into it.

Check Console after import, then open MainMenu and verify the world and character
materials, terrain/trees, UI fonts, movement and house/barn interaction. A local
editor dependency audit does not replace checking gameplay on the client's PC.
