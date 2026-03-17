# §5 Gameplay · §6 Characters & Animation

## Gameplay

| Decision | Value | Rationale |
|---|---|---|
| **Movement type** | Free (sub-pixel) movement on tile grid | Better feel for action/exploration than tile-stepping. |
| **Collision detection** | AABB (axis-aligned bounding box) | Simple, fast, and sufficient for 2D. |
| **Collision resolution** | Axis-separated (resolve X and Y independently) | Prevents corner-catching and wall sticking. |
| **Delta time** | All movement/timing uses `gameTime.ElapsedGameTime` | Never assumes fixed frame rate. |
| **Input routing** | Movement is read via `IInputManager` actions | Keeps gameplay logic decoupled from keyboard polling. |
| **Diagonal movement** | Input vector normalization before speed scaling | Prevents diagonal movement from being faster than cardinal movement. |
| **Prototype actor bounds** | Player actor is clamped to world pixel bounds | Ensures camera follow and actor stay within authored map area. |
| **Terrain collision source** | Tile properties (`blocked=true`) in TSX | Passability is authored once per terrain tile and reused wherever that tile appears. |
| **Placed obstacle collision** | Static prop AABBs are merged through `WorldCollisionMap` | Player/follower movement uses one collision contract for both terrain and screen-placed solids. |
| **Party trail following** | Companion position is sampled from the player's recent path at a fixed lag distance | Prevents corner-cutting and keeps follower placement deterministic without separate collision steering. |

*(Add entries as movement speed, hitbox sizing, terrain modifiers, combat, and other gameplay systems are designed.)*

## Characters & Animation

| Decision | Value | Rationale |
|---|---|---|

*(Add entries for character sprite sizes, origins, animation system, spritesheet layout, walk cycles, directional priority, etc.)*
