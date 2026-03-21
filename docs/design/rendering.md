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
| **Night lighting pass** | Gameplay builds a low-resolution lightmap from ambient night color plus additive point lights, then composites it over the scene with multiply blend | Preserves the existing day/night darkening model while allowing local light sources like small fires to punch through with soft glows at low GPU cost. |
| **Terrain variation** | Deterministic position-hash with weighted tile variants | Organic-looking grass, sand, and riverbed distribution without per-tile authoring or runtime flicker. |
| **Cloud shadow pass** | Two layers of procedurally generated tileable Perlin noise scrolling at different speeds, composited via multiply blend on a half-resolution render target | Soft, drifting cloud shadows over the scene. Low-res target + bilinear upscaling produces inherently diffuse edges. Intensity fades at night. |
| **Camera** | Camera2D producing a `Matrix` transform | All world-space drawing uses camera matrix in SpriteBatch.Begin(). |
| **UI rendering pass** | Separate SpriteBatch without camera transform | Screen-space UI stays fixed regardless of camera position. |
| **Screenshot capture source** | Final virtual scene render target | Copies the post-processed gameplay frame to the clipboard without depending on window size or letterboxing. |
| **Y-sorting** | `SpriteSortMode.FrontToBack` in entity pass; `layerDepth = Bounds.Bottom / mapPixelHeight` | Entities with a lower screen-bottom Y draw behind those with a higher Y. Uses XNA's native sprite depth sorting — no custom interface required. Entities that should not participate (e.g., docks) pass `layerDepth = 0` to pin them behind all sorted entities. |
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

## Y-Sorting (Entity Depth Ordering)

Pass 4 (Entities) in `GameplayScreen.Draw()` uses `SpriteSortMode.FrontToBack` so SpriteBatch sorts sprites by their `layerDepth` parameter:

```
layerDepth = entity.Bounds.Bottom / mapPixelHeight
```

- `Bounds.Bottom` is the bottom edge of the entity sprite in world pixels — the "ground line."
- Dividing by map height normalises the value to the 0–1 range that `layerDepth` expects.
- **Lower Y** (higher on screen) → smaller depth → drawn first → appears **behind**.
- **Higher Y** (lower on screen) → larger depth → drawn last → appears **in front**.

### Opting out

Entities that should not participate in Y-sorting (e.g., `Dock`) call `Draw()` without a `layerDepth` argument, defaulting to `0f`. This pins them behind every sorted entity.

### Sort anchor tuning

If a tall sprite's visual "ground contact" does not match `Bounds.Bottom`, add a configurable sort-Y offset to shift the anchor without changing collision bounds.

## Particle System & Effects

| Decision | Value | Rationale |
|---|---|---|
| **CPU particle system** | Pre-allocated struct pool with free-index stack; each particle has position, velocity, rotation, scale, color lerp, gravity, and lifetime | Zero-allocation hot loop, proven pattern from Trashsquatch, individual particle sprites produce natural-looking effects. |
| **Fire ambience effects** | Small fires can attach smoke and spark particle emitters while separately contributing a point light to the night lighting pass | Keeps atmospheric visual effects composable: particles stay in the particle system and lighting stays in the rendering pipeline. |

*(Add entries as particle and visual effect systems are built.)*

## Graphics Classes

| Class | Description |
|---|---|
| `Camera2D` | Produces a world-space view `Matrix` for `SpriteBatch`. Clamps position to map pixel bounds. |
| `DayNightCycle` | Looping time-of-day cycle that outputs both a multiply-blend tint `Color` and a scalar `NightStrength`. Phases: Night → Dawn → Day → Dusk. Pure logic, no GPU dependency. |
| `LightingRenderer` | Owns the low-resolution lightmap render target, draws additive point lights from `LightData` snapshots, and composites the lightmap over the scene with multiply blending. |
| `CloudShadowRenderer` | Generates tileable Perlin-noise cloud textures at load time, draws two independently scrolling layers onto a half-resolution shadow map each frame, and composites with multiply blending for soft drifting cloud shadows. |
| `TiledWorldRenderer` | Wraps TMX/TSX map loading, deterministic weighted tile-variant drawing, and `Water/*` layer grouping for the distortion pass. |

*(Add entries as graphics classes are created — ScreenScaler, etc.)*
