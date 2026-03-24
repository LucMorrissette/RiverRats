# §11 Systems Catalog · §12 Components Catalog

## Systems Catalog

| System | Description |
|---|---|
| `ParticleManager` | Manages a pre-allocated pool of `Particle` structs with free-index stack. Zero-allocation update/draw loop. Handles lifecycle (aging, expiry, slot recycling), physics (velocity, gravity, angular velocity), and rendering (color lerp, scale, rotation). |
| `GnomeSpawner` | Manages a dynamic pool of `GnomeEnemy` instances. Handles initial batch spawn, timed trickle spawn at a configurable interval, per-frame updates (passing a target position), and distance-based culling. Spawns off-screen around camera edges with staggered hop phases. |
| `FlowField` | BFS-based flow field that computes one direction vector per 32×32 tile pointing toward a target. All enemies share a single field. Recomputed every N frames to amortize cost. 8-directional BFS with diagonal corner-cutting prevention. Pre-allocated circular queue and direction/distance arrays for zero GC in the hot path. |
| `ProjectileSystem` | Manages a pre-allocated pool of `Projectile` instances with auto-fire logic for two shooters (player and follower) on alternating cooldowns. Handles firing at the nearest gnome, movement, and projectile-vs-gnome collision (kill projectile, remove gnome). |

*(Add entries as systems are created — CollisionSystem, NpcManager, etc.)*

<!-- Example format:
| `CollisionSystem` | Collision detection and resolution between ICollidable entities. |
| `InteractionManager` | Player proximity to IInteractable, dispatches interactions. |
-->

## Components Catalog

| Component | Description |
|---|---|
| `LoopAnimator` | Animates a single-row horizontal sprite sheet in a continuous loop. Computes source rectangles without direction rows or movement gating—frames advance every frame update. Used for looping visual effects like fire, sparkles, or environmental animations. `Draw()` accepts an optional `layerDepth` for Y-sorting. |
| `ParticleEmitter` | Accumulates delta time and spawns particles at a configured rate (particles/sec) from a `ParticleProfile`. Attached to entities; supports enable/disable toggling. Delegates actual particle creation to a shared `ParticleManager`. |

| `SpriteAnimator` | Animates a multi-row sprite sheet organized as directions × walk frames. Handles idle/walk frame cycling based on movement state. `Draw()` accepts an optional `layerDepth` for Y-sorting. |

*(Add entries as components are created — Health, etc.)*

<!-- Example format:
| `SpriteAnimator` | Manages sprite animation state, frame advancement, DidLoop flag. |
| `Health` | Integer health with injected damage/death callbacks. |
-->
