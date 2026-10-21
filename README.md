# Pingpong · Hexagonal Arena

**A local two-player Pong prototype where angled walls and obstacles change the path of the rally.**

The game uses Unity's 2D physics and C# components to connect ball motion, paddle movement, arena boundaries and goal detection. A bootstrap script configures missing runtime components on the scene's named objects.

## Gameplay

| Player | Move up | Move down |
|---|---|---|
| Left paddle | `W` | `S` |
| Right paddle | `↑` | `↓` |

The ball rebounds from paddles, walls and obstacles. Passing behind a paddle awards the opposing player a point and resets the ball. Paddle movement is clamped to the sloping limits of the hexagonal arena.

## Open in Unity

This repository contains an `Assets` collection, not a complete Unity project export: `Packages` and `ProjectSettings` are not included.

1. Create a compatible Unity 2D project.
2. Import the repository's [Assets](Assets/) while preserving its `.meta` files.
3. Install/enable Unity's Input System and use a 2D renderer compatible with the included scene assets.
4. Open [Assets/game-hex.unity](Assets/game-hex.unity) and inspect the scene's object names and attached scripts.
5. Enter Play mode and use the controls above.

The scripts use APIs such as `Rigidbody2D.linearVelocity`, so choose an editor version that supports them. No editor-version lockfile or packaged executable is included.

## Code to explore

- [GameBootstrap](Assets/Scripts/GameBootstrap.cs): scene lookup and runtime physics/component setup.
- [BallController](Assets/Scripts/BallController.cs): ball motion, collision behavior and resets.
- [PaddleController](Assets/Scripts/PaddleController.cs): keyboard input and hexagonal movement limits.
- [BoundaryCollider](Assets/Scripts/BoundaryCollider.cs): arena boundaries.
- [GoalLine](Assets/Scripts/GoalLine.cs) and [ScoreManager](Assets/Scripts/ScoreManager.cs): scoring and display.

The bootstrap depends on the existing scene hierarchy and names; importing scripts alone does not recreate the whole arena. This is a local game prototype with no network multiplayer or published release build.
