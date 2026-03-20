# §11 Systems Catalog · §12 Components Catalog

## Systems Catalog

| System | Description |
|---|---|
| `ParticleManager` | Manages a pre-allocated pool of `Particle` structs with free-index stack. Zero-allocation update/draw loop. Handles lifecycle (aging, expiry, slot recycling), physics (velocity, gravity, angular velocity), and rendering (color lerp, scale, rotation). |

*(Add entries as systems are created — CollisionSystem, NpcManager, etc.)*

<!-- Example format:
| `CollisionSystem` | Collision detection and resolution between ICollidable entities. |
| `InteractionManager` | Player proximity to IInteractable, dispatches interactions. |
-->

## Components Catalog

| Component | Description |
|---|---|
| `LoopAnimator` | Animates a single-row horizontal sprite sheet in a continuous loop. Computes source rectangles without direction rows or movement gating—frames advance every frame update. Used for looping visual effects like fire, sparkles, or environmental animations. |
| `ParticleEmitter` | Accumulates delta time and spawns particles at a configured rate (particles/sec) from a `ParticleProfile`. Attached to entities; supports enable/disable toggling. Delegates actual particle creation to a shared `ParticleManager`. |

*(Add entries as components are created — SpriteAnimator, Health, ParticleEmitter, etc.)*

<!-- Example format:
| `SpriteAnimator` | Manages sprite animation state, frame advancement, DidLoop flag. |
| `Health` | Integer health with injected damage/death callbacks. |
-->
