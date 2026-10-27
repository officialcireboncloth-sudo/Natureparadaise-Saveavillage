# Companion interaction panel

Kuda and Anjing use the same left-side uGUI panel. Open the animal's world interaction with E or its touch prompt. The panel displays the animal name, its actual command status, affection out of 1000, a proportional affection bar, and an active-command indicator.

- **Ikuti Saya**, **Tunggu**, and **Pulang** call the existing PersonalAnimal command system and close the panel so the command can run.
- Horse home action is **Pulang ke Kandang**; dog/cat home action is **Pulang ke Rumah**. Both retain their authored home destination.
- **Naik Kuda** appears only for horses and calls the existing mounting system. A failed mount keeps the panel open with feedback. A successful mount releases the panel's pause/lock while preserving the rider transition's lock.
- Close with the X, bottom Tutup button, E, or Escape. Desktop arrow/Enter navigation and pointer/touch buttons share the same command handlers.
- The panel hides the gameplay HUD, hotbar and map launcher while open. A full-screen input shield and world prompt suppression prevent clicks or tool actions from leaking into the world. Cursor and owned movement/time locks are restored on close or target loss.
- Theme colors and optional sprite overrides live in Resources/UI/CompanionCommandTheme.asset. Six 128px transparent PNG icons (horse, dog, follow, stay, home, mount) live in Textures/UI/Icons/Companions and share the existing 1024px mobile atlas. They use pure white silhouettes without shading. MainMenuIcon remains the fallback when an optional sprite slot is empty. The artwork import menu reconnects the PNGs without editing scenes.
- Panel scaling stays within the screen safe area, including portrait viewports. Dog/cat panels omit the mount row and use a shorter height. Default species names are localized; custom names remain intact.

Existing companion IDs, heart points, commands, home positions and saved data remain unchanged. Validation uses authored Horse/Dog Companion prefabs in an isolated native Unity Play Mode fixture, with real command dispatch, mounting/dismounting, pointer event/raycast checks, keyboard navigation links, JSON restore, resource cleanup, layout and pixel-level icon rendering. Main Map is not opened or saved. Physical mobile input and FPS are not benchmarked by the desktop fixture.
