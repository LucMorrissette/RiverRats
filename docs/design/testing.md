# §25 Testing

## Testing Decisions

| Decision | Value | Rationale |
|---|---|---|
| **Framework** | xUnit | Standard .NET test framework; good tooling support. |
| **Test project** | `tests/DogDays.Tests/` | Separate project referencing the game project directly. |
| **Testability strategy** | Decouple logic from GPU via interfaces | All game logic testable without a GraphicsDevice. |
| **Test tiers** | Unit + Integration | Unit for single-class isolation; Integration for multi-system frame simulation. |
| **Key testing interfaces** | `IInputManager`, `IMapCollisionData`, `IAudioManager` | Hardware dependencies sit behind interfaces for fake substitution. |

## Test Helpers Catalog

| Helper | Purpose |
|---|---|
| `FakeGameTime` | Static factory for `GameTime` instances (`OneFrame()`, `FromSeconds(n)`). No real clock needed. |
| `FakeInputManager` | Scriptable `IInputManager` fake for integration tests (press/release/hold simulation). |
| `FakeMusicManager` | Implements `IMusicManager`. Records `SetVolume` calls and other method invocations for assertions. |

| `FakeGamePadStateSource` | Scriptable `IGamePadStateSource` fake for gamepad unit tests (queued `GamePadState` frames). Defined inline in `InputManagerGamepadTests`. |
| `FakeJoystickStateSource` | Scriptable `IJoystickStateSource` fake for joystick unit tests (queued `JoystickSnapshot` frames). Defined inline in `InputManagerJoystickTests`. |
| `FakeSaveGameService` | In-memory `ISaveGameService` fake. Records save/delete call counts for assertions. |

*(Add entries as test helpers are created.)*

<!-- Example format:
| `FakeInputManager` | Implements `IInputManager`. Script held/pressed/released actions. |
| `FakeGameTime` | Static factory for `GameTime` instances (`OneFrame()`, `FromSeconds(n)`). |
| `FakeMapCollisionData` | Implements `IMapCollisionData`. In-memory tile grid — no texture needed. |
| `FakeAudioManager` | Implements `IAudioManager`. Records SFX calls for assertions. |
-->

## Test Catalog

| Test File | Tests |
|---|---|
| `Unit/Camera2DTests.cs` | Initial position, LookAt clamping (all four edges), map-smaller-than-viewport locking, GetViewMatrix translation. |
| `Unit/InputManagerTests.cs` | Action press/release/hold transitions, secondary key bindings, and rebind behavior. |
| `Unit/PlayerBlockTests.cs` | Delta-time movement distance, diagonal normalization, world-bounds clamping, and center-point math. |
| `Unit/WorldCollisionMapTests.cs` | Collision boundary aggregation, obstacle addition, and walk-over suppression rules. |
| `Integration/PlayerBlockMovementTests.cs` | Multi-frame held input movement accumulation and release-to-stop behavior with scripted input. |
| `Integration/PlayerBlockObstacleCollisionTests.cs` | Multi-axis separation and sliding behavior against placed static obstacles in the world. |
| `Unit/ScreenManagerTests.cs` | Push/pop/replace lifecycle, update routing (topmost only), draw ordering/transparency, deferred mutation during update, empty-stack safety. |
| `Unit/PauseScreenTests.cs` | `IsTransparent` is `true`, dims music volume on `LoadContent`, restores music volume on `UnloadContent`, pops itself from `ScreenManager` on `InputAction.Pause` press, no-ops when `Pause` is not pressed. |
| `Unit/FrontDoorTests.cs` | Authored initial state, auto-open only at very close range for the player, confirm follower proximity alone does not open the door, and auto-close when the player is not nearby. |
| `Unit/GardenShedTests.cs` | Authored initial state, opens only when player foot bounds are on the ramp footprint, and closes when the player steps off the ramp. |
| `Unit/GardenGnomeTests.cs` | Hides quickly when the player enters the reduced proximity radius, reappears slowly once the player leaves, and stays at home when the player remains outside that range. |
| `Unit/DashRollSequenceTests.cs` | Dash start rules, facing lock, immediate invulnerability, frame progression, cooldown timing, and blocked-start cancellation. |
| `Unit/PlayerCollapseSequenceTests.cs` | Collapse start state, player lock-in during the sequence, transform squash/rotation progression, and completion timing. |
| `Integration/DashRollSequenceIntegrationTests.cs` | Multi-frame dash movement halts before world obstacles and exits the roll cleanly after collision. |
