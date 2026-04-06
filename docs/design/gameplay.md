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
| **Watercraft boarding + paddling** | Facing a nearby `Watercraft` and pressing `InputAction.Confirm` triggers a short scripted hop for the player and follower into directional canoe seats; while seated, movement input paddles the craft with the main player kept at the bow and side-view occupants layered inside the hull, and confirm/cancel only disembarks when a valid landing spot exists | Keeps canoe interaction in the same confirm/proximity model as other props while extending it into a simple, readable ride mechanic. |
| **Walkable prop overrides** | Props like docks can contribute walkable bounds that suppress blocked terrain underneath | Lets bridges/docks sit over blocked water or collision layers without carving holes in the base map. |
| **Object-layer colliders** | TMX `Colliders` object layer rectangles are loaded and merged into `WorldCollisionMap._staticObstacleBounds` | Supports sub-tile collision precision for barriers, railings, and architectural details without tile-grid constraints. |
| **Party trail following** | Companion position is sampled from the player's recent path at a fixed lag distance | Prevents corner-cutting and keeps follower placement deterministic without separate collision steering. |
| **Wave completion model** | Time-based: each wave runs for a configured duration; the wave clears when the timer expires, not when all enemies are killed | "Survive the wave" feel. Enemies trickle-spawn continuously throughout the duration up to a max-active cap. Remaining enemies after expiry persist but no new ones spawn. |
| **Pre-wave countdown** | A configurable countdown phase precedes each wave's Active phase | Gives the player a visual warning before enemies begin spawning. Displayed as a centered integer countdown. |
| **Wave intermission** | A configurable pause phase follows each cleared wave before the next countdown begins | Provides time for orb collection and repositioning between waves. Duration is a tunable constant. |
| **Enemy variant system** | `GnomeEnemy` accepts an `EnemyType` that parameterizes tint, scale, speed, HP, and on-death behavior | Enables visually and mechanically distinct enemy variants without separate entity classes. The `WaveConfig` spawn-weight mix controls which types appear each wave. |
| **Forest dodge roll** | On `Maps/WoodsBehindCabin`, `InputAction.Confirm` triggers a short dash in the player's current movement direction, grants dash-length invulnerability, and starts a 4-second cooldown | Adds an explicit reactive defense to the survival map without altering overworld interaction behavior. |
| **Forest cooldown feedback** | Dash cooldown is rendered as a shrinking world-space gauge under the player's feet | Keeps the timing cue anchored to the avatar instead of spending persistent HUD space. |
| **Death transition staging** | Player death holds on `GameplayScreen` for a short collapse sequence before pushing `DeathScreen` | Gives the defeat state a readable physical payoff while keeping failure presentation screen-owned. |

*(Add entries as movement speed, hitbox sizing, terrain modifiers, combat, and other gameplay systems are designed.)*

## Characters & Animation

| Decision | Value | Rationale |
|---|---|---|
| **Character sheet layout** | Rows 0-3 = walk directions, row 4 = sitting poses, row 5 = four-frame commando roll loop, rows 6-9 = four-frame collapse strips for Down/Left/Right/Up | Keeps contextual animations additive without changing the walk animator contract. |
| **Dash roll orientation** | Roll frames are authored once as a compact spinning bundle and rotated at draw time to match the dash vector | Supports diagonal dashes without multiplying sprite-sheet rows for each direction. |
| **Collapse animation source** | Player death collapse uses dedicated four-frame directional strips that read as buckle -> knees -> tip -> flat | Gives the death transition a readable physical sequence without relying on runtime squashing. |

*(Add entries for character sprite sizes, origins, animation system, spritesheet layout, walk cycles, directional priority, etc.)*
