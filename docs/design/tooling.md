# §27 Technical Gotchas · §28 Dev Assets & Tooling · §29 Developer Tools

## Technical Gotchas

| Gotcha | Context | Resolution |
|---|---|---|
| `RenderTarget2D` defaults to `DiscardContents` | Switching between render targets silently discards previous contents | Use `RenderTargetUsage.PreserveContents` for any RT that gets switched away from and back to within a single frame. |

*(Add entries as gotchas are discovered.)*

## Dev Assets & Tooling

| Decision | Value | Rationale |
|---|---|---|
| `generate_cabin_wood_floor.py` | `tooling/sprites/generate_cabin_wood_floor.py` | Uses Pillow to generate both the cabin wood floor set and a worn retro checkered floor set for the cabin interior tileset. |
| `generate_storage_shed.py` | `tooling/sprites/generate_storage_shed.py` | Uses Pillow to generate a 64×64 storage shed sprite with transparent background for outdoor prop use. |
| `prepare_pine_tree.py` | `tooling/sprites/prepare_pine_tree.py` | Removes background, crops, resizes to 128 px tall (4 tiles), and reduces palette. Outputs `Content/Sprites/pine-tree.png`. |
| `prepare_birch_tree.py` | `tooling/sprites/prepare_birch_tree.py` | Removes background, crops, resizes to 128 px tall (4 tiles), and reduces palette. Outputs `Content/Sprites/birch-tree.png`. |

*(Add entries for placeholder art generation, asset scripts, content pipeline tooling, etc.)*

## Developer Tools

| Tool | Toggle Action | Key | Description |
|---|---|---|---|

*(Add entries for in-game debug tools — tile inspector, collision visualizer, etc.)*
