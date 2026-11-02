# Lumber owner collision and construction status

## Behavior

The Lumber owner now has an explicit body capsule, both at the shop and while dispatched. VisualClone continues stripping gameplay and source colliders from art clones; the capsule is added separately at the worker root. At the current 0.75 preset it is 1.575 m tall and 0.51 m wide.

The construction kit adds fitted solid boxes around the workbench, lumber stack and tool crate. The bench box extends to the floor, so a player cannot walk between the decorative legs. As with tarp walls, kit props and the worker defer activation if spawned on an actor, then become solid after the actor walks clear. No actor is teleported. Navigation ignores worker bodies when testing ground/routes, but still treats benches and supplies as obstacles.

Each active project now projects one rounded status card above its work station: building name, target level, current activity and progress. It is independent of the nearby bottom hint. Screen projection preserves readability when zooming and prevents 3D scenery from obscuring text. Its non-interactive canvas sorts below the main HUD and has no raycaster. It hides with the site when entering interiors, on completion/return, and on cancel/dispose.

Runtime-generated colliders/cards do not change save schemas, project prices, duration or worker availability. Existing slots receive them when loaded in a fresh Play session.

## Native verification

Unity 6000.0.81f1 isolated review project: ConstructionBodyStatusValidation.RunBatch, exit 0, 25 checks. Actual CharacterController movements use the game's player capsule dimensions and ordinary walk increments. Checks cover worker/bench blocking, route departure, self-ground queries, safe spawn overlap, status content, zoom, interior visibility, completion and cleanup. Two camera-composited UI review renders were inspected; production uses an overlay canvas.

The existing ConstructionSiteSafetyValidation also passed 18 checks covering open exits, wall blocking/deferred activation and work dust. Its wall-specific assertions now distinguish tarp walls from newly collidable props.

HouseConstructionRestoreValidation was rerun after these changes: exit 0, all 51 checks passed, including completed Slot 1 without screens and real upgrades resuming correctly.

Both production savegame.json and autosave.json hashes remained unchanged. No production scene or save file was written. These are native editor physics/render checks, not a full Play Mode or physical mobile-device test.

Review artifacts: ArtSource/ConstructionBodyStatus in the isolated HandoffAudit-2026-10-23/CleanClone project. The batch entry is deliberately restricted to that review project.
