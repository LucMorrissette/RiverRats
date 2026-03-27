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
| **Placed obstacle collision** | TMX object-layer prop AABBs are merged through `WorldCollisionMap` | Player/follower movement uses one collision contract for both terrain and map-authored solid props. |
| **Nearby prop interaction** | `InputAction.Confirm` triggers the nearest prop whose expanded interaction bounds overlap the player's foot bounds | Keeps world interaction action-based and proximity-driven without requiring actors to overlap blocked props directly. |
| **Walkable prop overrides** | Props like docks can contribute walkable bounds that suppress blocked terrain underneath | Lets bridges/docks sit over blocked water or collision layers without carving holes in the base map. |
| **Object-layer colliders** | TMX `Colliders` object layer rectangles are loaded and merged into `WorldCollisionMap._staticObstacleBounds` | Supports sub-tile collision precision for barriers, railings, and architectural details without tile-grid constraints. |
| **Party trail following** | Companion position is sampled from the player's recent path at a fixed lag distance | Prevents corner-cutting and keeps follower placement deterministic without separate collision steering. |
| **Wave completion model** | Time-based: each wave runs for a configured duration; the wave clears when the timer expires, not when all enemies are killed | "Survive the wave" feel. Enemies trickle-spawn continuously throughout the duration up to a max-active cap. Remaining enemies after expiry persist but no new ones spawn. |
| **Pre-wave countdown** | A configurable countdown phase precedes each wave's Active phase | Gives the player a visual warning before enemies begin spawning. Displayed as a centered integer countdown. |
| **Wave intermission** | A configurable pause phase follows each cleared wave before the next countdown begins | Provides time for orb collection and repositioning between waves. Duration is a tunable constant. |
| **Enemy variant system** | `GnomeEnemy` accepts an `EnemyType` that parameterizes tint, scale, speed, HP, and on-death behavior | Enables visually and mechanically distinct enemy variants without separate entity classes. The `WaveConfig` spawn-weight mix controls which types appear each wave. |

*(Add entries as movement speed, hitbox sizing, terrain modifiers, combat, and other gameplay systems are designed.)*

## Characters & Animation

| Decision | Value | Rationale |
|---|---|---|

*(Add entries for character sprite sizes, origins, animation system, spritesheet layout, walk cycles, directional priority, etc.)*
