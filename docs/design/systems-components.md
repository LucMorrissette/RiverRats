# §11 Systems Catalog · §12 Components Catalog

## Systems Catalog

| System | Description |
|---|---|
| `ParticleManager` | Manages a pre-allocated pool of `Particle` structs with free-index stack. Zero-allocation update/draw loop. Handles lifecycle (aging, expiry, slot recycling), physics (velocity, gravity, angular velocity, optional local ground-plane bounces), and rendering (color lerp, scale, rotation). |
| `GnomeSpawner` | Manages a dynamic pool of `GnomeEnemy` instances. Handles initial batch spawn, timed trickle spawn at a configurable interval, per-frame updates (passing a target position), and distance-based culling. Spawns off-screen around camera edges with staggered hop phases. Accepts an `EnemyType` parameter per spawn call, applying the type's visual and behavioral overrides to the created `GnomeEnemy`. The spawner does not decide which types to create — it receives type instructions from `WaveManager` based on the current wave's configured spawn-weight mix. |
| `FlowField` | BFS-based flow field that computes one direction vector per 32×32 tile pointing toward a target. All enemies share a single field. Recomputed every N frames to amortize cost. 8-directional BFS with diagonal corner-cutting prevention. Pre-allocated circular queue and direction/distance arrays for zero GC in the hot path. |
| `WaveManager` | Manages wave-based enemy spawning for the forest survival minigame. Owns the wave lifecycle state machine and drives `GnomeSpawner`. State flow: PreWave → Countdown (configurable pre-wave countdown displayed to the player) → Active (continuous trickle-spawning throughout a configurable wave duration timer; wave completes when the timer expires, not when all enemies are killed) → Cleared → Intermission (configurable pause for orb collection) → next Countdown. Fires events on wave start, wave cleared, and all-waves-complete. Selects enemy types per spawn batch using the current `WaveConfig`'s spawn-weight mix. Remaining enemies after wave timer expiry persist but no new spawns occur. |
| `ProjectileSystem` | Manages a pre-allocated pool of `Projectile` instances with auto-fire logic for two shooters (player and follower) on alternating cooldowns. Handles firing at the nearest gnome, movement, optional arrow-trail spark emission, obstacle checks, and projectile-vs-gnome collision with a fixed three-kill pierce budget per shot. |
| `SlashSystem` | Manages always-active hatchet sweep attacks for two slashers (player and follower). Each slasher cycles between a 360° sweep phase and a cooldown phase. Gnomes within the wedge arc during a sweep are killed on contact (once per sweep). |
| `DashRollSequence` | Forest-only dodge-roll controller for the main player. Consumes `InputAction.Confirm` on `Maps/WoodsBehindCabin`, applies a short collision-aware dash, keeps the player invulnerable for the roll duration, swaps rendering to the commando-roll row, and renders a shrinking cooldown gauge under the player's feet. |
| `PlayerCollapseSequence` | Short screen-owned death sequencer for the main player. Locks position/facing, reuses the idle frame, and applies a bottom-anchored squash/tilt animation before `GameplayScreen` transitions to `DeathScreen`. |
| `FireflyManager` | Manages a pre-allocated pool of firefly entities that spawn during night, drift organically with organic sine-wave motion, emit a soft green glow (exposed as `LightData` snapshots), and fade out after a short life. Zero-allocation in the hot loop. Spawn rate scales with `DayNightCycle.NightStrength`. |
| `FishingRippleManager` | Manages event-driven water ripples, splash highlights, and spook rings for the fishing scene. Tracks up to 8 concentric distortion ripples, 4 expanding splash highlight rings, and 4 red spook rings. Ripples are spawned by gameplay events (lure splash, fish strike, catch, twitch, bad cast) rather than player input. Writes aged position and size data to `FishingWater.fx` shader parameters each frame. |
| `RippleSystem` | Manages click-spawned water ripples in the overworld water shader. Spawns a new ripple on mouse click, ages existing ripples, culls expired ones, and writes the active ripple set as UV-space `Vector3` shader data (xy = position, z = age/frequency) to the water distortion shader parameters. |
| `XpLevelSystem` | Tracks XP accumulation and handles level-ups by modifying `PlayerCombatStats`. Fires `OnLevelUp` (an `event Action<int>`) with the new level number after calling `PlayerCombatStats.ApplyLevelUp()`. Increases `Health.MaxHp` on level-up. |

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
| `FrameTimer` | Value-type (`struct`) reusable frame-timing helper for sprite animations. Tracks elapsed time and advances a `CurrentFrame` counter with wrapping. Used internally by `LoopAnimator` and other animation components that need frame-rate-independent frame cycling. |
| `Health` | Tracks hit points, invincibility frames, and raises `OnDamaged` (`event Action<int>`) and `OnDied` (`event Action`) events. `TakeDamage(int)` is ignored while `IsInvincible` is set. `SetInvincibleForDuration(...)` is additive so dash i-frames and post-hit i-frames do not truncate each other. `MaxHp` can be updated at runtime for level-ups. Reusable by any entity that needs health (player, enemies, etc.). |

*(Add entries as components are created.)*

<!-- Example format:
| `SpriteAnimator` | Manages sprite animation state, frame advancement, DidLoop flag. |
| `Health` | Integer health with injected damage/death callbacks. |
-->
