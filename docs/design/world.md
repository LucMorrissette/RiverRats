# §20 World & Tilemap

## World Classes

| Class | Description |
|---|---|
| `IMapCollisionData` | World collision query contract for blocked-tile checks using world-space rectangles. |
| `TiledWorldRenderer` | TMX/TSX-backed world renderer that draws ordered tile layers, routes `Water/*` layers through the water pass, aggregates tile-property collision across all layers, and exposes TMX object-layer prop placements. |
| `WorldCollisionMap` | Collision aggregator that combines terrain blockers with additional placed obstacle bounds. |

*(Add entries as world/tilemap classes are created — TileMap, TileMapRenderer, etc.)*

<!-- Example format:
| `TileMap` | Core tile data, collision queries, runtime modifications. |
| `TileMapRenderer` | Tile rendering with per-layer support. |
-->
