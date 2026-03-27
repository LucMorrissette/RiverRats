# §11 Systems Catalog · §12 Components Catalog

## Systems Catalog

| System | Description |
|---|---|
| `ParticleManager` | Manages a pre-allocated pool of `Particle` structs with free-index stack. Zero-allocation update/draw loop. Handles lifecycle (aging, expiry, slot recycling), physics (velocity, gravity, angular velocity, optional local ground-plane bounces), and rendering (color lerp, scale, rotation). |
| `GnomeSpawner` | Manages a dynamic pool of `GnomeEnemy` instances. Handles initial batch spawn, timed trickle spawn at a configurable interval, per-frame updates (passing a target position), and distance-based culling. Spawns off-screen around camera edges with staggered hop phases. Accepts an `EnemyType` parameter per spawn call, applying the type's visual and behavioral overrides to the created `GnomeEnemy`. The spawner does not decide which types to create — it receives type instructions from `WaveManager` based on the current wave's configured spawn-weight mix. |
| `FlowField` | BFS-based flow field that computes one direction vector per 32×32 tile pointing toward a target. All enemies share a single field. Recomputed every N frames to amortize cost. 8-directional BFS with diagonal corner-cutting prevention. Pre-allocated circular queue and direction/distance arrays for zero GC in the hot path. |
| `WaveManager` | Manages wave-based enemy spawning for the forest survival minigame. Owns the wave lifecycle state machine and drives `GnomeSpawner`. State flow: PreWave → Countdown (configurable pre-wave countdown displayed to the player) → Active (continuous trickle-spawning throughout a configurable wave duration timer; wave completes when the timer expires, not when all enemies are killed) → Cleared → Intermission (configurable pause for orb collection) → next Countdown. Fires events on wave start, wave cleared, and all-waves-complete. Selects enemy types per spawn batch using the current `WaveConfig`'s spawn-weight mix. Remaining enemies after wave timer expiry persist but no new spawns occur. |
| `ProjectileSystem` | Manages a pre-allocated pool of `Projectile` instances with auto-fire logic for two shooters (player and follower) on alternating cooldowns. Handles firing at the nearest gnome, movement, optional arrow-trail spark emission, obstacle checks, and projectile-vs-gnome collision with a fixed three-kill pierce budget per shot. |

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
