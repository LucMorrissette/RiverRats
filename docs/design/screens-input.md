# §7 Screen Management · §8 Input

## Screen Management

| Screen | Type | Description |
|---|---|---|
| `IGameScreen` | Interface | Screen lifecycle contract: `LoadContent`, `Update`, `Draw`, `DrawOverlay`, `UnloadContent`, `IsTransparent`. |
| `ScreenManager` | Manager | Stack-based screen host. Push/pop/replace screens. Topmost receives input; visible stack is drawn bottom-to-top. |
| `GameplayScreen` | Screen | Primary gameplay screen owning player, camera, and world renderer. Holds a `ScreenManager` reference to push overlay screens, exposes its `MusicManager` as `IMusicManager` for overlays, supports loading a specific map asset plus optional spawn-point id for zone-to-zone replacement, and performs a CRT power-off/power-on transition (vertical squeeze → line → dot → black) around zone swaps. |
| `PauseScreen` | Screen (overlay) | Transparent overlay (`IsTransparent = true`) pushed on top of `GameplayScreen`. Draws a semi-transparent dark overlay + centered "PAUSED" text via `DrawOverlay`. Dims music volume on enter, restores on exit. Pops itself from the stack on unpause. Constructor takes `ScreenManager`, `IMusicManager`, `GraphicsDevice`, `int virtualWidth`, `int virtualHeight`. |

*(Add entries as screens are created — TitleScreen, etc.)*

## IGameScreen API

| Member | Signature | Description |
|---|---|---|
| `IsTransparent` | `bool` (property) | When `true`, screens below this one in the stack are still drawn. Use `true` for overlay screens (pause), `false` for opaque screens (gameplay, title). |
| `LoadContent()` | `void` | Called once when the screen is first pushed onto the stack. Load assets and initialize state here. |
| `Update(gameTime, input)` | `void` | Called every frame for the topmost screen only. |
| `Draw(gameTime, spriteBatch)` | `void` | Called every frame for all visible screens, drawn bottom-to-top. Renders into the scene render target at virtual resolution. |
| `DrawOverlay(gameTime, spriteBatch, sceneScale)` | `void` (default no-op) | Called by `Game1` after the scene render target is composited to the backbuffer. Renders UI at native window resolution. `sceneScale` is the integer scale factor from virtual resolution (e.g., 480×270) to the current window size. Screens that do not need a native-resolution overlay do not need to implement this method. |
| `UnloadContent()` | `void` | Called when the screen is removed from the stack. Dispose resources here. |

## Input

| Decision | Value | Rationale |
|---|---|---|
| **Input abstraction** | `IInputManager` interface | All gameplay code uses the interface; never raw `Keyboard.GetState()`. |
| **State tracking** | Previous + current state each frame | Enables press, release, and hold detection. |
| **Action mapping** | `InputAction` enum | Rebindable named actions (e.g., `InputAction.Confirm`) rather than hardcoded keys. |
| **Confirm interaction** | `InputAction.Confirm` is the gameplay interaction action and is bound to `Space` and `Enter` by default | World interactions like toggling a nearby firepit stay inside the same input abstraction as movement and debug actions. |
| **Screenshot hotkey** | `InputAction.CopyScreenshotToClipboard` bound to `P` | Keeps screenshot capture inside the same action-based input layer as gameplay/debug actions. |
| **Pause action** | `InputAction.Pause` — new mapped action | Separates pause semantics from cancel semantics. `Cancel` no longer shares a binding with `Pause`. |
| **CRT filter toggle** | `InputAction.ToggleCrtFilter` bound to `F9` | Toggles the CRT post-process shader (barrel distortion, scanlines, vignette) on/off at runtime. Developer/player preference toggle — no gameplay impact. |
| **Cancel binding change** | `InputAction.Cancel` no longer includes `Escape` | `Cancel` retains `Back` only. `Escape` is now exclusively bound to `InputAction.Pause`. |
| **Null-object pattern** | `EmptyInputManager` | For screens or states that don't process input. |
| **Keyboard source abstraction** | `IKeyboardStateSource` | Decouples MonoGame hardware calls from input logic for deterministic unit testing. |
| **Gamepad source abstraction** | `IGamePadStateSource` | Decouples `GamePad.GetState()` from input logic for deterministic unit testing. |
| **Gamepad support** | `InputManager` polls `GamePadState` alongside `KeyboardState` via `IGamePadStateSource`; keyboard and gamepad results are OR-merged per action | Game is fully playable on an NES-style controller (Hyperkin Cadet). D-pad → movement, A → Confirm, B → Cancel, Start → Pause. Dev/debug actions remain keyboard-only. |
| **Joystick fallback** | `InputManager` polls raw `JoystickSnapshot` via `IJoystickStateSource` for USB controllers without an SDL2 game controller mapping (MAPPING: n/a). Uses Hat0 for D-pad and raw button indices for face buttons (Hyperkin Cadet: B1=A, B0=B, B9=Start). | Unmapped retro USB controllers that SDL2 doesn't recognize as gamepads still work via the raw joystick API. |
| **macOS fast-click detection** | Use `IsMouseLeftReleased()` (release edge) instead of `IsMouseLeftPressed()` (press edge) | On macOS/SDL2, fast clicks (press + release in ~30–80ms) complete between 60 FPS polls (~16ms/frame), missing the `Pressed` state entirely. The release edge is always captured because the button must be held ≥1 frame before releasing. See `[Obsolete]` marker on `IsMouseLeftPressed()`. |

## IInputManager API

| Member | Type | Description |
|---|---|---|
| `Update()` | `void` | Advances input state from previous frame to current frame. |
| `IsHeld(action)` | `bool` | `true` while any key, gamepad button, or joystick input bound to the action is down. |
| `IsPressed(action)` | `bool` | `true` only on the frame a bound key, button, or joystick input transitions up -> down. |
| `IsReleased(action)` | `bool` | `true` only on the frame a bound key, button, or joystick input transitions down -> up. |
| `IsMouseLeftPressed()` | `bool` | **[Obsolete]** `true` only on the frame left button transitions Released → Pressed. Unreliable for fast clicks on macOS; use `IsMouseLeftReleased()` instead. |
| `IsMouseLeftReleased()` | `bool` | `true` only on the frame left button transitions Pressed → Released. Reliable for fast clicks on macOS because the button must be held ≥1 frame. |
| `GetMousePosition()` | `Point` | Current mouse cursor position in physical window client coordinates. |
