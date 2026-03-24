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
| **UI overlay pass** | `DrawOverlay()` renders UI to the backbuffer at native window resolution, after the scene render target is integer-scaled | Font text rasterized by FontStashSharp must draw at actual display pixels to avoid PointClamp blockiness. Pixel dimensions (padding, indicator size) are multiplied by `sceneScale` to match the virtual-to-window ratio. |
| **Screenshot capture source** | Final virtual scene render target | Copies the post-processed gameplay frame to the clipboard without depending on window size or letterboxing. |
| **Y-sorting** | `SpriteSortMode.FrontToBack` in entity pass; `layerDepth = Bounds.Bottom / mapPixelHeight` | Entities with a lower screen-bottom Y draw behind those with a higher Y. Uses XNA's native sprite depth sorting — no custom interface required. Entities that should not participate (e.g., docks) pass `layerDepth = 0` to pin them behind all sorted entities. |
| **Occlusion reveal** | Entities in front of player drawn to separate RT, composited with `OcclusionReveal` shader creating a circular alpha-fade lens | Player remains visible behind tall props. Zero cost when no occlusion is detected (original single-pass used). |
| **VSync** | *(TBD)* | Off for uncapped frames or on for tear-free. |
| **Fixed timestep** | *(TBD)* | Off for variable delta unless physics requires it. |

## Font Rendering

| Decision | Value | Rationale |
|---|---|---|
| **Font library** | FontStashSharp (runtime TTF rasterization via `FontStashSharp.MonoGame` NuGet) | Renders crisp text at any resolution from a single `.ttf` file. Avoids the MGCB SpriteFont pipeline which requires pre-baked sizes and produces blurry results when scaled. |
| **Font file** | Nunito TTF loaded from `Content/Fonts/Nunito.ttf` via `File.ReadAllBytes` (not MGCB pipeline) | Clean, readable sans-serif with multiple weights; same font proven in the Trashsquatch project. |
| **Font sizes** | Rasterized on demand at pixel sizes (e.g., 12pt tiny, 16pt HUD) via `FontSystem.GetFont(size)` | FontStashSharp caches glyphs per size so there's no per-frame allocation. |

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

## Occlusion Reveal (See-Through Lens)

When the player walks behind a tall prop (tree, cabin, boulder), a circular alpha-fade lens centred on the player reveals them through the occluding object.

### How it works

1. **Detection (Update):** `CheckPlayerOcclusion()` tests whether any Y-sorted entity both sorts in front of the player AND overlaps the player's expanded bounding box.
2. **Split rendering (Draw):** When occlusion is active, the entity Y-sort pass splits into two sub-passes:
   - **Pass 4a:** Entities behind/at player depth + player + follower → drawn directly to the scene.
   - **Pass 4b:** Entities in front of player → drawn to `OcclusionRevealRenderer.OccluderTarget` render target.
   - **Pass 4c:** `OccluderTarget` composited back to scene via the `OcclusionReveal` shader with `BlendState.AlphaBlend`.
3. **Shader:** `OcclusionReveal.fx` computes distance from each pixel to the player centre (in UV space, aspect-ratio corrected). Inside the reveal radius, alpha fades from `MinAlpha` at the centre to full opacity at the edge using `smoothstep`.
4. **No-op path:** When no occlusion is detected, the original single-pass Y-sort drawing is used with zero performance cost.

| Parameter | Default | Purpose |
|---|---|---|
| `RevealRadius` | 0.12 UV | Size of the circular reveal lens |
| `EdgeSoftness` | 0.55 | Fraction of radius used as soft gradient edge |
| `MinAlpha` | 0.25 | Minimum opacity at the lens centre (0 = fully transparent) |

## Particle System & Effects

| Decision | Value | Rationale |
|---|---|---|
| **CPU particle system** | Pre-allocated struct pool with free-index stack; each particle has position, velocity, rotation, scale, color lerp, gravity, and lifetime | Zero-allocation hot loop, proven pattern from Trashsquatch, individual particle sprites produce natural-looking effects. |
| **Fire ambience effects** | Small fires can attach smoke and spark particle emitters while separately contributing a point light to the night lighting pass | Keeps atmospheric visual effects composable: particles stay in the particle system and lighting stays in the rendering pipeline. |

*(Add entries as particle and visual effect systems are built.)*

## CRT Post-Process Filter

A full-screen post-process effect applied to the final scene render target blit, simulating a curved CRT monitor. Toggled at runtime via `InputAction.ToggleCrtFilter` (F9).

### Current implementation (Standard)

`CrtEffect.fx` runs as a pixel shader during the final `SpriteBatch.Draw()` that blits `_sceneRenderTarget` to the backbuffer. Three composable layers:

| Layer | Parameter | Default | Description |
|---|---|---|---|
| **Barrel distortion** | `DistortionAmount` | 0.15 | Pushes UVs outward from centre proportional to r². Pixels outside the curved boundary render black, creating rounded screen edges. |
| **Scanlines** | `ScanlineIntensity` | 0.25 | Sine-wave pattern along pixel rows darkens every other scanline. Intensity 0 = off, 1 = fully dark. |
| **Vignette** | `VignetteStrength` | 0.3 | Darkens pixels based on squared distance from centre. Simulates CRT brightness falloff toward edges. |

### Integration point

`Game1.Draw()` conditionally passes `_crtEffect` to `SpriteBatch.Begin()` when `_crtEnabled` is set. The HUD overlay pass renders *after* the CRT pass and is unaffected — HUD text stays crisp.

### CRT Power-Off/On Zone Transition

Zone transitions use a CRT-themed power-off/on effect instead of a simple alpha fade:

| Phase | Alpha range | Visual |
|---|---|---|
| **Phase 1: Vertical squeeze** | 0.0–0.6 | Black bars close from top and bottom, squeezing the scene into a thin horizontal line at screen centre. A phosphor glow brightens the shrinking strip. |
| **Phase 2: Horizontal shrink** | 0.6–1.0 | The line contracts horizontally to a bright dot at dead centre, then fades out. |
| **Hold black** | 1.0 (held) | Zone swap occurs while screen is fully black. |
| **Power-on** | 1.0→0.0 | Reverse of power-off: dot expands to line, line stretches to full image. |

Total transition: ~0.95s (0.4s out + 0.15s hold + 0.4s in). Rendered entirely in `GameplayScreen.DrawOverlay()` using `_pixelTexture` rectangles — no additional shader needed.

### Future enhancements (Full immersion tier)

These can be layered into the same shader without changing the integration point:

| Enhancement | Description | Complexity |
|---|---|---|
| **Chromatic aberration** | Offset R/G/B channel UV lookups by small amounts, increasing toward screen edges. Simulates colour fringing from imperfect CRT electron beam convergence. | Low — three `tex2D` calls instead of one, plus a per-channel UV offset proportional to distance from centre. |
| **Phosphor dot grid** | Overlay a subtle RGB sub-pixel pattern that modulates brightness in a repeating 3-column pattern (R, G, B). Only visible at higher output resolutions. | Low — `fmod(screenPixel.x, 3)` selects which channel to slightly boost/dim. |
| **Scanline flicker** | Modulate scanline intensity with a slow `sin(Time)` oscillation to simulate CRT refresh instability. | Trivial — add `Time` parameter and multiply scanline factor by `lerp(1, sin(Time * flickerSpeed), flickerAmount)`. |
| **Screen glow / bloom** | Blur bright areas and composite additively, simulating phosphor bleed on a CRT. | Medium — requires a second downsampled render target for the blur pass. |
| **Interlace simulation** | Alternate which scanline rows are bright every other frame, simulating interlaced video. | Low — pass a frame counter; offset scanline phase by half a row on odd frames. |
| **Corner shadow rounding** | Darken the corners more aggressively near the barrel distortion edge for a thicker bezel look. | Low — additional `smoothstep` near the UV boundary. |

## Graphics Classes

| Class | Description |
|---|---|
| `Camera2D` | Produces a world-space view `Matrix` for `SpriteBatch`. Clamps position to map pixel bounds. |
| `DayNightCycle` | Looping time-of-day cycle that outputs a multiply-blend tint `Color`, a scalar `NightStrength`, and a `GameHour` float (0–24) derived from cycle progress. Phases: Night → Dawn → Day → Dusk. Pure logic, no GPU dependency. |
| `LightingRenderer` | Owns the low-resolution lightmap render target, draws additive point lights from `LightData` snapshots, and composites the lightmap over the scene with multiply blending. |
| `CloudShadowRenderer` | Generates tileable Perlin-noise cloud textures at load time, draws two independently scrolling layers onto a half-resolution shadow map each frame, and composites with multiply blending for soft drifting cloud shadows. |
| `TiledWorldRenderer` | Wraps TMX/TSX map loading, deterministic weighted tile-variant drawing, and `Water/*` layer grouping for the distortion pass. |

| `OcclusionRevealRenderer` | Manages a virtual-resolution render target for entities that sort in front of the player. Composites them back over the scene through the `OcclusionReveal` shader which applies a circular alpha-fade lens centred on the player, letting the player show through occluding props. |

*(Add entries as graphics classes are created — ScreenScaler, etc.)*
