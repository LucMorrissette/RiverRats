# §21 Core Classes · §22 Data Classes · §24 Utility Classes

## Core Classes

| Class | Location | Description |
|---|---|---|
| `IInputManager` | `Input/` | Action-based input contract (held/pressed/released) used by gameplay and screens. |
| `IKeyboardStateSource` | `Input/` | Keyboard state provider abstraction that enables deterministic tests. |
| `IGamePadStateSource` | `Input/` | Gamepad state provider abstraction that enables deterministic tests. |
| `IJoystickStateSource` | `Input/` | Raw joystick state provider abstraction for unmapped USB controllers. Returns `JoystickSnapshot`. |
| `InputManager` | `Input/` | Keyboard + gamepad + joystick implementation of `IInputManager` with default bindings and rebind support. OR-merges all three input devices. |
| `EmptyInputManager` | `Input/` | Null-object implementation for states/screens that intentionally ignore input. |
| `KeyboardStateSource` | `Input/` | Production keyboard source that wraps `Keyboard.GetState()`. |
| `GamePadStateSource` | `Input/` | Production gamepad source that wraps `GamePad.GetState(PlayerIndex.One)`. |
| `JoystickStateSource` | `Input/` | Production joystick source that wraps `Joystick.GetState(0)` and converts to `JoystickSnapshot`. |

| `IGameScreen` | `Screens/` | Screen lifecycle contract (load, update, draw, unload, transparency). |
| `ScreenManager` | `Screens/` | Stack-based screen host. Push/pop/replace semantics with deferred mutation during update. |
| `GameEventBus` | `Core/` | Type-keyed publish/subscribe hub for cross-system gameplay events used by quests and other session-scoped progression features. |
| `GameSessionServices` | `Core/` | Shared runtime service bundle that survives screen replacement and carries the session event bus, quest manager, and save game service. |
| `ISaveGameService` | `Core/` | Abstraction for persisting and loading save game data. Supports multiple slots, save/load/delete/query operations. |
| `JsonSaveGameService` | `Data/Save/` | `ISaveGameService` implementation that writes JSON files to `%APPDATA%/RiverRats/saves/`. Uses atomic writes (tmp + rename). |
| `SaveGameMapper` | `Data/Save/` | Static capture/restore mapper — single source of truth for converting live game state to/from `SaveGameData`. |
| `QuestDefinitionLoader` | `Data/` | Loads quest definitions from raw JSON and validates ids, objective structure, and required counts before runtime state is created. |

*(Add entries as core classes are created — GameEvents, Direction, IMapCollisionData, etc.)*

<!-- Example format:
| `GameEvents` | `Core/` | Static event hub for global events. |
| `Direction` | `Core/` | Enum for cardinal directions. |
-->

## Data Classes

| Class | Location | Description |
|---|---|---|
| `InputAction` | `Input/` | Enum of logical gameplay actions that key bindings map to. |\n| `JoystickSnapshot` | `Input/` | Lightweight readonly struct capturing raw joystick Hat0 and button states. Public constructor enables deterministic tests without hardware. |
| `FacingDirection` | `Data/` | Cardinal facing enum shared by movement and sprite animation. |
| `FollowerMovementConfig` | `Data/` | Breadcrumb follower tuning for trailing distance, trail sampling, and facing dead-zone behavior. |
| `LightData` | `Graphics/` | Immutable frame snapshot of a world point light: position, radius, color, and intensity. Used to pass lighting data from entities to the renderer without coupling rendering code to entity references. |
| `ParticleProfile` | `Data/` | Immutable `record` defining particle effect configuration: spawn rate, life range, speed range, scale range, start/end colors, spread angle, gravity, and optional local ground-bounce settings. Used by `ParticleEmitter` and `ParticleManager.Emit()`. |
| `Particle` | `Systems/` | Value-type `struct` representing a single particle: position, velocity, rotation, angular velocity, scale, start/end colors, gravity, optional local ground-plane bounce state, initial/remaining life, and active flag. Designed for cache-friendly iteration in `ParticleManager`. |
| `SpawnPointData` | `Data/` | Immutable record describing a named world-space spawn position parsed from a TMX `SpawnPoints` object layer. |
| `ZoneTriggerData` | `Data/` | Immutable record describing a world-space transition rectangle plus its destination map asset and spawn id, parsed from a TMX `ZoneTriggers` object layer. |
| `EnemyType` | `Data/` | Enum identifying an enemy variant kind. Each value maps to a set of visual and behavioral parameter overrides (tint, scale, speed multiplier, HP, on-death behavior) applied to a `GnomeEnemy` at spawn time. The spawner selects a type from the current wave's configured mix. |
| `GnomeState` | `Data/` | Enum describing behavioral states for `GnomeEnemy`: Chasing, WindingUp, Lunging, Stunned, Dying. Drives the per-frame state machine inside the entity. |
| `WaveConfig` | `Data/` | Readonly data object describing one wave's parameters: wave number, enemy count, speed multiplier, enemy HP, wave duration, and enemy-type spawn-weight mix. Used by `WaveManager` to configure each wave's spawning and completion rules. |
| `WaveState` | `Data/` | Enum tracking the lifecycle phase of a wave: PreWave, Countdown, Active (continuous spawning + timer), Cleared, Intermission, AllWavesComplete. Consumed by `WaveManager` for state transitions and by HUD renderers for display logic. |
| `PlayerCombatStats` | `Data/` | Mutable combat stat modifiers for the forest survival minigame: max HP, speed multiplier, cooldown multiplier, projectile tuning, level, XP, and XP-to-next-level. Supports level-up stat scaling and reset-to-defaults. |
| `FishingZoneData` | `Data/` | Readonly `record struct` parsed from the TMX `FishingZones` object layer. Holds the zone `Rectangle` and a `FishType` identifier used by `FishingScreen` to spawn species-appropriate fish silhouettes. |
| `WaterShaderConfig` | `Data/` | Plain data class holding water distortion shader parameters: wave speed, amplitude, frequency layers, and surface Y-threshold. Passed to `FishingWater.fx` and `WaterDistortion.fx` at runtime. |
| `GameEventType` | `Core/` | Enum of gameplay event kinds published through `GameEventBus` for progression systems such as quests. |
| `GameEvent` | `Core/` | Lightweight event payload with an event type, optional target id, and amount used by the shared gameplay event bus. |
| `QuestStatus` | `Data/` | Enum tracking whether a quest is not started, active, completed, or failed in the current play session. |
| `QuestEventConditionDefinition` | `Data/` | Event requirement used by quest start triggers and objective completion checks. Matches a `GameEventType`, optional target id, and required count. |
| `ObjectiveDefinition` | `Data/` | Immutable quest objective definition loaded from JSON, including player-facing text and an event-driven completion rule. |
| `QuestDefinition` | `Data/` | Immutable linear quest definition loaded from JSON, including id, title, description, optional start trigger, auto-start flag, and ordered objectives. |
| `QuestState` | `Data/` | Mutable runtime quest progress for a loaded `QuestDefinition`, tracking current objective index and per-objective counters. Supports `RestoreState()` for save/load. |
| `SaveGameData` | `Data/Save/` | Root save DTO. Contains version, timestamp, and nested DTOs for player, quests, combat stats, and day/night state. |
| `SavePlayerData` | `Data/Save/` | Player position, facing direction, and current zone map asset name. |
| `SaveQuestStateData` | `Data/Save/` | Per-quest snapshot: quest id, status, objective index, and progress counters. |
| `SaveCombatStatsData` | `Data/Save/` | Forest combat stat snapshot: max HP, level, XP, multipliers. |
| `SaveDayNightData` | `Data/Save/` | Day/night cycle progress (0–1 float). |

## Utility Classes

| Class | Description |
|---|---|
| `PerlinNoise` | Static 2D Perlin noise generator with tileable output. Provides single-sample and multi-octave fBm noise-map generation for procedural textures. |
| `PolygonBounds` | Convex or concave polygon shape used to define irregular swim areas in the fishing mini-game. Supports point containment (`Contains`), random interior sampling (`RandomPointInside`), inset erosion (`Inset`), horizontal slicing (`SliceHorizontal`), and rectangle conversion (`FromRectangle`). Parsed from TMX polygon objects by `SimpleTiledRenderer.GetObjectPolygons()`. |

*(Add entries as utility classes are created — MathUtils, TextHelper, etc.)*
