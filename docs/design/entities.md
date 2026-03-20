# §10 Entity Catalog

| Entity | Interfaces | Description |
|---|---|---|
| `PlayerBlock` | — | Prototype controllable player entity with input-driven movement, map-bounds clamping, and solid-color block rendering. |
| `FollowerBlock` | — | Sidekick companion that samples the leader's recent path and stays a fixed distance behind it, preserving corners without offset steering. |
| `Boulder` | — | Static prop entity drawn from a sprite and used as a solid world obstacle. |
| `Dock` | — | Static decorative prop entity drawn from a sprite and placed in world space by gameplay screen data. |
| `Firepit` | — | Static prop entity drawn from a sprite and used as a solid world obstacle, placed via TMX object-layer data. |
| `SmallFire` | — | Animated visual effect entity drawn from a looping horizontal sprite sheet. Composes a `LoopAnimator` component for frame cycling and accepts an optional `ParticleEmitter` component (attached externally) for smoke particles. Purely visual with no collision. |

*(Add entries as entities are created. Each row should list the entity class, the interfaces it implements, and a brief description of its purpose and key behaviors.)*

<!-- Example format:
| `Player` | `IYSortable` | Player character. Has `Health` component, handles movement and animation. |
| `Npc` | `IYSortable`, `IInteractable`, `ICollidable` | Non-player character with dialogue and behavior components. |
-->
