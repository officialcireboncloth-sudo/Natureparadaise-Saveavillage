# Complete editable player master

Open **Player_Farming.blend**. This is the complete native player and control rig, with all 47 Actions together, including the approved horse mount, dismount, mirrored takes and shared riding idle. Use the Action Editor dropdown to select `Player | Mount Horse`, `Player | Dismount Horse` or `Player | Riding Idle`. The reference horse uses the real Unity proportions, converted into the native player authoring units.

To export the same saved motion again:

1. Save Player_Farming.blend.
2. In Blender's Text Editor select **Export Player to Unity.py** and run it, or run the adjacent export_player.py through Blender.
3. The complete Player.fbx in this Unity project is updated. The exporter reloads the intact master after export cleanup.
4. In Unity choose **Nature Paradise > Animals > Apply Horse Rider Transitions** to refresh take ranges, the controller, animation set, profile and prefabs.

The exporter does not regenerate animation poses. It exports the saved Actions, keeps the production hierarchy and original hidden rig shapes, and excludes the reference horse and editor-only world-preview movement. HorseTrajectory.json is the paired world trajectory used by the Unity profile. Update that trajectory as well if the world-preview route/timing is changed.

Source~ is tracked art source, excluded from Unity imports and player builds. Runtime horse clips are part of the complete Player.fbx; a separate horse-rider animation FBX is no longer needed by the controller.
