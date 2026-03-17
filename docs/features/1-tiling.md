# Feature 1 - Tiling

## Goal

Introduce a first-pass tilemap feature that replaces the empty startup screen with a grass-tiled world rendered at a **32×32 tile size** and a **960×540 virtual resolution**. The initial implementation should prove the rendering pipeline, the asset pipeline, and the project's preferred map architecture without overcommitting to a large world system too early.

## Why This Feature Exists

Right now the project only boots into a blank MonoGame window. Before building entities, input, collisions, or gameplay, we need a reliable world surface to render against.

This feature establishes three important foundations:

1. **Visible world-space rendering** instead of a placeholder clear color.
2. **A tile-based map architecture** instead of hardcoded draw loops in `Game1`.
3. **A path toward editor-authored maps** by bringing in `MonoGame.Extended.Tiled` now, while the project is still small.

## Player-Facing Outcome

When the game runs, the player should see a full-screen field of grass tiles using the reference asset at [reference_assets/grass2.png](../../reference_assets/grass2.png). The result should feel like the first playable layer of an EarthBound/Stardew-style world: simple, readable, and pixel-stable.

## Design Decisions Locked By This Feature

| Decision | Value | Why |
|---|---|---|
| **Tile size** | 32×32 px | Matches the current grass asset exactly and avoids premature resampling. |
| **Virtual resolution** | 960×540 | Clean 2× integer scale to 1080p with a modern widescreen presentation. |
| **Starter map format** | Tiled (`.tmx`) via `MonoGame.Extended.Tiled` | Gives us a clean content pipeline and editor-authored map path from day one. |
| **Initial world scope** | Single ground layer | Enough to validate the map pipeline before layering in collision, objects, or decoration. |
| **Initial tileset scope** | One grass tile | Smallest useful implementation. Feature proves architecture first, content variety later. |

## Requirements

1. Add the **MonoGame.Extended.Tiled** package and any required supporting MonoGame.Extended packages.
2. Set up the content pipeline so a Tiled-authored map can be loaded by the game.
3. Create a starter tileset using [reference_assets/grass2.png](../../reference_assets/grass2.png).
4. Create a simple map that fills the visible play area with repeating grass tiles.
5. Render the map through a dedicated world/tilemap path, not by hardcoding tile loops directly in `Game1`.
6. Preserve pixel stability at the chosen **960×540** virtual resolution.
7. Keep the first implementation focused on rendering only. No collision, camera follow, NPCs, or gameplay logic are required in this feature.

## Non-Goals

This feature does **not** include:

- Collision layers
- Object layers
- Tile metadata/properties
- World streaming or zone transitions
- Decorative tile variation selection
- Map interaction logic
- Save/load integration

## Implementation Shape

### Content and Asset Setup

- Bring the grass asset into the actual game content pipeline in a form usable by Tiled.
- Create a minimal tileset definition that exposes the grass tile as a 32×32 tile.
- Create a starter map sized large enough to cover the initial visible play area and a little beyond it.

### Runtime Architecture

Instead of drawing tiles directly inside `Game1`, introduce a small world rendering path that can grow later.

Recommended structure:

- `World/` contains the tilemap-facing types.
- `Game1` owns high-level orchestration only.
- A dedicated renderer or map wrapper is responsible for loading and drawing the Tiled map.

Preferred first-pass shape:

- `World/TileMapRenderer` or equivalent wrapper around the MonoGame.Extended Tiled renderer
- Optional lightweight map holder for content references and dimensions
- Integration in `Game1.LoadContent()` and `Game1.Draw()` only

## Map Sizing Guidance

At **32×32** tiles and **960×540** virtual resolution:

- Visible width: `960 / 32 = 30` tiles
- Visible height: `540 / 32 = 16.875` tiles

That means the camera/view can show partial tiles at the bottom edge, which is acceptable.

For the initial test map, target at least:

- **32×20 tiles** minimum

This gives a little breathing room beyond the initial visible area and avoids creating a map that feels tightly cropped.

## MonoGame.Extended Scope

This feature is intentionally narrow:

- We are bringing in **MonoGame.Extended.Tiled** for map loading/rendering.
- We are **not** committing to MonoGame.Extended screen management, collision, or other higher-level framework pieces yet.
- We can adopt more MonoGame.Extended modules later if they solve a real problem.

## File and System Targets

Expected areas touched when this feature is implemented:

- `src/RiverRats.Game/RiverRats.Game.csproj`
- `src/RiverRats.Game/Game1.cs`
- `src/RiverRats.Game/Content/Content.mgcb`
- `src/RiverRats.Game/Content/Maps/` *(new)*
- `src/RiverRats.Game/Content/Tilesets/` *(new)*
- `src/RiverRats.Game/World/` *(new)*
- `docs/design/rendering.md`
- `docs/design/world.md`
- `docs/design/core-data-classes.md` *(if new world-facing types are introduced)*

## Acceptance Criteria

1. Running the game shows a grass-tiled map instead of a blank screen.
2. The grass fills the entire visible world area at startup.
3. Tiles render cleanly with no visible seams caused by sub-pixel placement.
4. The implementation uses a reusable map-loading/rendering path rather than embedding map logic directly in `Game1`.
5. The project builds and runs with the new dependency in place.

## Verification

- Run the game and confirm the window opens to a full grass field.
- Resize or rerun at the default startup resolution and confirm pixels remain stable.
- Confirm the map is loaded from content rather than procedurally painted in code.
- Confirm the code structure leaves room for future map layers, collision, and editor-authored levels.

## Follow-On Features

Natural next steps after this feature:

1. Camera and world transform integration
2. Collision and blocked tiles
3. Multiple tile types and tileset growth
4. Decorative grass variation selection
5. Object layers and interactables
6. Zone or map loading architecture
