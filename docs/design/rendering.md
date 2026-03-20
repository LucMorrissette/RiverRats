# §3 Display & Rendering · §4 Art Style · §19 Particle System & Effects · §23 Graphics Classes

## Display & Rendering

| Decision | Value | Rationale |
|---|---|---|
| **Base tile size** | 32×32 px | Standard for 2D pixel art; power-of-two friendly. |
| **Virtual resolution** | 960×540 | 30×16.875 tiles visible. Clean 2× integer scale to 1080p. |
| **Scaling mode** | 2× integer scaling with letterbox | Pixel-perfect rendering; no sub-pixel blur. Fills 1080p exactly. |
| **Map format** | Tiled `.tmx` with external `.tsx` tilesets | Editor-authored maps via MonoGame.Extended content pipeline. |
| **Water post-process grouping** | Tile layers named `Water/*` render into a shared water render target before distortion | Lets water bottoms, props, and future surface overlays share one shader pass without tile-level coupling. |
| **Surface-reach distortion** | Props with `reachesSurface` render into a dedicated render target with alpha-encoded vertical gradient (0 at top, 1 at bottom); the `SurfaceReachDistortion` shader technique reads this alpha to scale distortion per-pixel | Simulates props emerging from the water surface — no distortion at the top (air), full distortion at the bottom (submerged). Uses horizontal strip drawing to encode gradient into alpha channel. |
| **Terrain variation** | Deterministic position-hash with weighted tile variants | Organic-looking grass, sand, and riverbed distribution without per-tile authoring or runtime flicker. |
| **Camera** | Camera2D producing a `Matrix` transform | All world-space drawing uses camera matrix in SpriteBatch.Begin(). |
| **UI rendering pass** | Separate SpriteBatch without camera transform | Screen-space UI stays fixed regardless of camera position. |
| **Screenshot capture source** | Final virtual scene render target | Copies the post-processed gameplay frame to the clipboard without depending on window size or letterboxing. |
| **Y-sorting** | IYSortable interface | Entities sorted by Y position for correct depth ordering. |
| **VSync** | *(TBD)* | Off for uncapped frames or on for tear-free. |
| **Fixed timestep** | *(TBD)* | Off for variable delta unless physics requires it. |

## Font Rendering

| Decision | Value | Rationale |
|---|---|---|

*(No fonts selected yet. Add entries as font rendering decisions are made.)*

## Art Style

| Decision | Value | Rationale |
|---|---|---|

*(Art style to be defined. Add entries for perspective, frame sizes, palette choices, etc.)*

## Particle System & Effects

| Decision | Value | Rationale |
|---|---|---|
| **CPU particle system** | Pre-allocated struct pool with free-index stack; each particle has position, velocity, rotation, scale, color lerp, gravity, and lifetime | Zero-allocation hot loop, proven pattern from Trashsquatch, individual particle sprites produce natural-looking effects. |

*(Add entries as particle and visual effect systems are built.)*

## Graphics Classes

| Class | Description |
|---|---|
| `Camera2D` | Produces a world-space view `Matrix` for `SpriteBatch`. Clamps position to map pixel bounds. |
| `DayNightCycle` | Looping time-of-day cycle that outputs a multiply-blend tint `Color`. Phases: Night → Dawn → Day → Dusk. Pure logic, no GPU dependency. |
| `TiledWorldRenderer` | Wraps TMX/TSX map loading, deterministic weighted tile-variant drawing, and `Water/*` layer grouping for the distortion pass. |

*(Add entries as graphics classes are created — ScreenScaler, etc.)*
