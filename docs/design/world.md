# §20 World & Tilemap

## World Classes

| Class | Description |
|---|---|
| `IMapCollisionData` | World collision query contract for blocked-tile checks using world-space rectangles. |
| `TiledWorldRenderer` | TMX/TSX-backed world renderer that draws ordered tile layers, routes `Water/*` layers through the water pass, aggregates tile-property collision across all layers, and exposes TMX object-layer prop placements. Also exposes `ColliderBounds`, `ZoneTriggers`, and `SpawnPoints` parsed from dedicated TMX object layers so gameplay can stay editor-driven. |
| `WorldCollisionMap` | Collision aggregator that combines terrain blockers with additional placed obstacle bounds. |

*(Add entries as world/tilemap classes are created — TileMap, TileMapRenderer, etc.)*

## Prop Collision Strategy

Collision boxes for world props are defined **in code** as part of the entity class, not hand-placed in the TMX `Colliders` layer. This eliminates per-instance manual placement and ensures every instance of a prop type gets an identical, correct collision box automatically.

### How it works

1. **Entity class** (`Tree`, `Cabin`) accepts a `localCollisionBox` `Rectangle` in its constructor. This rectangle is relative to the sprite's top-left origin.
2. **`PropFactory`** holds `static readonly Rectangle` constants for each prop type's collision box (e.g., `PineTreeCollisionBox`, `BirchTreeCollisionBox`, `CozyCabinCollisionBox`).
3. **`GameplayScreen.LoadContent()`** calls `PropFactory.CreateTrees()` / `PropFactory.CreateCabins()` with the appropriate collision box constant, then merges collision bounds via `PropFactory.GetTreeCollisionBounds()` / `PropFactory.GetCabinCollisionBounds()` into the `WorldCollisionMap`.

### TMX Colliders layer

The TMX `Colliders` object layer is reserved for **terrain and world-boundary colliders only** (e.g., ground/water borders). Do not add prop-specific collision rectangles to this layer.

## Zone Transition Authoring

Zone-to-zone travel is authored entirely in TMX object layers rather than terrain tile metadata.

### `ZoneTriggers` layer

- Use plain rectangle objects with no `gid`.
- Required property: `targetMap` — destination TMX content asset name (for example `Maps/WoodsBehindCabin`).
- Optional property: `targetSpawnId` — destination spawn point name. Defaults to `default` when omitted.

When the player overlaps a trigger rectangle, `GameplayScreen` replaces itself with a fresh `GameplayScreen` for the destination map. This keeps map loading atomic and avoids carrying stale world state across zones.

### `SpawnPoints` layer

- Use named point objects with no `gid`.
- The TMX object `name` is the spawn identifier.
- `GameplayScreen` looks up the requested spawn id and falls back to map centre if no matching spawn exists.

This pairing gives each exit its own per-instance destination data without encoding map links into shared tile definitions.

### Adding a new solid prop type

1. Define a `static readonly Rectangle` in `PropFactory` describing the collision area relative to the sprite's top-left corner.
2. Use `PropFactory.CreateTrees()` or `PropFactory.CreateCabins()` (or create a new entity type following the `Tree`/`Cabin` pattern with a `CollisionBounds` property).
3. Add a `GetXxxCollisionBounds()` helper to `PropFactory`.
4. Merge the collision bounds in `GameplayScreen.LoadContent()` via `PropFactory.MergeRectangleArrays()`.

<!-- Example format:
| `TileMap` | Core tile data, collision queries, runtime modifications. |
| `TileMapRenderer` | Tile rendering with per-layer support. |
-->
