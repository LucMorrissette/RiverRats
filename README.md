# Dog Days

A 2D top-down exploration game built with MonoGame — outdoor adventure set along rivers, shorelines, and natural terrain.

![Screenshot placeholder](reference_assets/River%20Rats%202026-03-17%2019.40.44.png)

![Gameplay GIF](reference_assets/heyoooooo.gif)

## Controls

| Action | Keys |
|--------|------|
| Move Up | `W` / `↑` |
| Move Down | `S` / `↓` |
| Move Left | `A` / `←` |
| Move Right | `D` / `→` |
| Confirm | `Space` / `Enter` |
| Cancel / Exit | `Escape` / `Backspace` |
| Click water for ripples | `Left mouse button` — clicking on water creates expanding ripple distortion effects |
| Toggle collision debug | `U` |
| Copy screenshot to clipboard | `P` |

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | MonoGame 3.8 (DesktopGL) |
| Runtime | .NET 10 — cross-platform (macOS, Linux, Windows) |
| Extensions | MonoGame.Extended 5.4.0 |
| Map editor | [Tiled](https://www.mapeditor.org/) (`.tmx` / `.tsx`) |
| Tests | xUnit 2.9.2 + coverlet |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or later — project uses `RollForward=Major`)
- Cross-platform: macOS, Linux, Windows (DesktopGL backend)
- For map editing: [Tiled Map Editor](https://www.mapeditor.org/)

## Build / Run / Test

```bash
# Build the solution
dotnet build DogDays.slnx

# Run the game
dotnet run --project src/DogDays.Game/DogDays.Game.csproj

# Run all tests
dotnet test
```

## Project Layout

```
src/DogDays.Game/          Main game project
    Game1.cs, Program.cs
    /Components/             Reusable behaviors (SpriteAnimator)
    /Content/                Content Pipeline assets (maps, sprites, tilesets, effects)
    /Data/                   Data structures, configs, enums
    /Entities/               Game objects (PlayerBlock, FollowerBlock, Boulder, Dock)
    /Graphics/               Camera2D, DayNightCycle, rendering helpers
    /Input/                  Input abstraction (IInputManager, InputManager, InputAction)
    /Screens/                Screen management (GameplayScreen, ScreenManager)
    /World/                  Tilemap and collision systems
tests/DogDays.Tests/       xUnit test project
    /Unit/                   Unit tests
    /Integration/            Integration tests
    /Helpers/                Test fakes (FakeInputManager, FakeGameTime)
docs/
    DESIGN.md                Master design document index
    design/                  Focused design sub-documents
    features/                Implemented feature specs
tooling/sprites/             Python asset generation scripts
```

## Current Features

- ✅ Stack-based screen/state management system
- ✅ Action-mapped input abstraction layer
- ✅ Tiled map rendering with deterministic weighted tile variants
- ✅ Terrain and prop collision system (AABB with axis-separated resolution)
- ✅ Player movement with delta-time and diagonal normalization
- ✅ Companion follower with breadcrumb trail
- ✅ Camera2D with map-bounds clamping
- ✅ Water distortion shader effect
- ✅ Day/night cycle
- ✅ Sprite animation component
- ✅ Static world props (boulders, docks) from TMX object layers
- ✅ Sub-tile collision geometry via collider objects
- ✅ Comprehensive unit and integration test suite

## Architecture Overview

The game uses a **composition-over-inheritance** entity model and a **stack-based screen manager** for game state. Key architectural decisions:

- **Input:** All input flows through an action-mapped [`IInputManager`](src/DogDays.Game/Input/IInputManager.cs) abstraction — never raw keyboard state.
- **Rendering:** 960×540 virtual resolution with 2× integer scaling. [`Camera2D`](src/DogDays.Game/Graphics/Camera2D.cs) provides matrix transforms with map-bounds clamping. Y-sorting ensures correct draw order.
- **World:** Tiled `.tmx` maps drive terrain, collisions (from tile properties), and object-layer prop placements. [`WorldCollisionMap`](src/DogDays.Game/World/WorldCollisionMap.cs) aggregates terrain + props + collider objects.
- **Entities:** [`PlayerBlock`](src/DogDays.Game/Entities/PlayerBlock.cs), [`FollowerBlock`](src/DogDays.Game/Entities/FollowerBlock.cs) (companion following via breadcrumb trail), [`Boulder`](src/DogDays.Game/Entities/Boulder.cs), [`Dock`](src/DogDays.Game/Entities/Dock.cs).

For full details, see [`docs/DESIGN.md`](docs/DESIGN.md).

## Design Documentation

The canonical design document lives at [`docs/DESIGN.md`](docs/DESIGN.md), with focused sub-documents under [`docs/design/`](docs/design/) covering architecture, rendering, entities, world systems, input, and more.

## Asset Tooling

Python scripts in [`tooling/sprites/`](tooling/sprites/) generate sprite sheets, terrain tiles, and other pixel-art assets used by the Content Pipeline.
