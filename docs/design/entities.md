# §10 Entity Catalog

| Entity | Interfaces | Description |
|---|---|---|
| `PlayerBlock` | — | Prototype controllable player entity with input-driven movement, map-bounds clamping, and solid-color block rendering. Y-sorted via `layerDepth`. |
| `FollowerBlock` | — | Sidekick companion that samples the leader's recent path and stays a fixed distance behind it, preserving corners without offset steering. Y-sorted via `layerDepth`. |
| `Boulder` | — | Static prop entity drawn from a sprite and used as a solid world obstacle. Y-sorted via `layerDepth`. Also used as the backing entity for cabin and pine tree props. |
| `Dock` | — | Static decorative prop entity drawn from a sprite and placed in world space by gameplay screen data. Not Y-sorted (always draws behind sorted entities). |
| `SunkenChest` | — | Static decorative prop entity drawn from a sprite and placed from TMX object-layer prop metadata, including underwater placement support. Y-sorted via `layerDepth`. |
| `Firepit` | — | Interactive prop entity drawn from a sprite and used as a solid world obstacle, placed via TMX object-layer data. Can compose a `SmallFire`, expose nearby interaction bounds, and toggle its attached fire on/off. Y-sorted via `layerDepth`; passes depth to attached `SmallFire`. |
| `SmallFire` | — | Animated visual effect entity drawn from a looping horizontal sprite sheet. Composes a `LoopAnimator` component for frame cycling, accepts externally attached smoke/spark particle emitters, and exposes a per-frame warm point-light snapshot for the lighting pass. Purely visual with no collision. Receives `layerDepth` from parent `Firepit`. |

*(Add entries as entities are created. Each row should list the entity class, the interfaces it implements, and a brief description of its purpose and key behaviors.)*

<!-- Example format:
| `Player` | `IYSortable` | Player character. Has `Health` component, handles movement and animation. |
| `Npc` | `IYSortable`, `IInteractable`, `ICollidable` | Non-player character with dialogue and behavior components. |
-->
