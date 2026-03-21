# §21 Core Classes · §22 Data Classes · §24 Utility Classes

## Core Classes

| Class | Location | Description |
|---|---|---|
| `IInputManager` | `Input/` | Action-based input contract (held/pressed/released) used by gameplay and screens. |
| `IKeyboardStateSource` | `Input/` | Keyboard state provider abstraction that enables deterministic tests. |
| `InputManager` | `Input/` | Keyboard-backed implementation of `IInputManager` with default bindings and rebind support. |
| `EmptyInputManager` | `Input/` | Null-object implementation for states/screens that intentionally ignore input. |
| `KeyboardStateSource` | `Input/` | Production keyboard source that wraps `Keyboard.GetState()`. |

| `IGameScreen` | `Screens/` | Screen lifecycle contract (load, update, draw, unload, transparency). |
| `ScreenManager` | `Screens/` | Stack-based screen host. Push/pop/replace semantics with deferred mutation during update. |

*(Add entries as core classes are created — GameEvents, Direction, IMapCollisionData, etc.)*

<!-- Example format:
| `GameEvents` | `Core/` | Static event hub for global events. |
| `Direction` | `Core/` | Enum for cardinal directions. |
-->

## Data Classes

| Class | Location | Description |
|---|---|---|
| `InputAction` | `Input/` | Enum of logical gameplay actions that key bindings map to. |
| `FacingDirection` | `Data/` | Cardinal facing enum shared by movement and sprite animation. |
| `FollowerMovementConfig` | `Data/` | Breadcrumb follower tuning for trailing distance, trail sampling, and facing dead-zone behavior. |
| `LightData` | `Graphics/` | Immutable frame snapshot of a world point light: position, radius, color, and intensity. Used to pass lighting data from entities to the renderer without coupling rendering code to entity references. |
| `ParticleProfile` | `Data/` | Immutable `record` defining particle effect configuration: spawn rate, life range, speed range, scale range, start/end colors, spread angle, and gravity. Used by `ParticleEmitter` and `ParticleManager.Emit()`. |
| `Particle` | `Systems/` | Value-type `struct` representing a single particle: position, velocity, rotation, angular velocity, scale, start/end colors, gravity, initial/remaining life, and active flag. Designed for cache-friendly iteration in `ParticleManager`. |

*(Add entries as data classes are created — configs, enums, save DTOs, etc.)*

<!-- Example format:
| `SaveGameData` | `Data/Save/` | Root save game data model. |
-->

## Utility Classes

| Class | Description |
|---|---|
| `PerlinNoise` | Static 2D Perlin noise generator with tileable output. Provides single-sample and multi-octave fBm noise-map generation for procedural textures. |

*(Add entries as utility classes are created — MathUtils, TextHelper, etc.)*
