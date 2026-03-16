# §10 Entity Catalog

| Entity | Interfaces | Description |
|---|---|---|
| `PlayerBlock` | — | Prototype controllable player entity with input-driven movement, map-bounds clamping, and solid-color block rendering. |
| `FollowerBlock` | — | Sidekick companion that targets side-adjacent formation slots relative to leader facing, with collision and map-bounds clamping. |
| `Boulder` | — | Static prop entity drawn from a sprite and used as a solid world obstacle. |

*(Add entries as entities are created. Each row should list the entity class, the interfaces it implements, and a brief description of its purpose and key behaviors.)*

<!-- Example format:
| `Player` | `IYSortable` | Player character. Has `Health` component, handles movement and animation. |
| `Npc` | `IYSortable`, `IInteractable`, `ICollidable` | Non-player character with dialogue and behavior components. |
-->
