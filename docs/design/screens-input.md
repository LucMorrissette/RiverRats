# §7 Screen Management · §8 Input

## Screen Management

| Screen | Type | Description |
|---|---|---|
| `IGameScreen` | Interface | Screen lifecycle contract: `LoadContent`, `Update`, `Draw`, `UnloadContent`, `IsTransparent`. |
| `ScreenManager` | Manager | Stack-based screen host. Push/pop/replace screens. Topmost receives input; visible stack is drawn bottom-to-top. |
| `GameplayScreen` | Screen | Primary gameplay screen owning player, camera, and world renderer. |

*(Add entries as screens are created — TitleScreen, PauseScreen, etc.)*

## Input

| Decision | Value | Rationale |
|---|---|---|
| **Input abstraction** | `IInputManager` interface | All gameplay code uses the interface; never raw `Keyboard.GetState()`. |
| **State tracking** | Previous + current state each frame | Enables press, release, and hold detection. |
| **Action mapping** | `InputAction` enum | Rebindable named actions (e.g., `InputAction.Confirm`) rather than hardcoded keys. |
| **Confirm interaction** | `InputAction.Confirm` is the gameplay interaction action and is bound to `Space` and `Enter` by default | World interactions like toggling a nearby firepit stay inside the same input abstraction as movement and debug actions. |
| **Screenshot hotkey** | `InputAction.CopyScreenshotToClipboard` bound to `P` | Keeps screenshot capture inside the same action-based input layer as gameplay/debug actions. |
| **Null-object pattern** | `EmptyInputManager` | For screens or states that don't process input. |
| **Keyboard source abstraction** | `IKeyboardStateSource` | Decouples MonoGame hardware calls from input logic for deterministic unit testing. |
| **macOS fast-click detection** | Use `IsMouseLeftReleased()` (release edge) instead of `IsMouseLeftPressed()` (press edge) | On macOS/SDL2, fast clicks (press + release in ~30–80ms) complete between 60 FPS polls (~16ms/frame), missing the `Pressed` state entirely. The release edge is always captured because the button must be held ≥1 frame before releasing. See `[Obsolete]` marker on `IsMouseLeftPressed()`. |

## IInputManager API

| Member | Type | Description |
|---|---|---|
| `Update()` | `void` | Advances input state from previous frame to current frame. |
| `IsHeld(action)` | `bool` | `true` while any key bound to the action is down. |
| `IsPressed(action)` | `bool` | `true` only on the frame a bound key transitions up -> down. |
| `IsReleased(action)` | `bool` | `true` only on the frame a bound key transitions down -> up. |
| `IsMouseLeftPressed()` | `bool` | **[Obsolete]** `true` only on the frame left button transitions Released → Pressed. Unreliable for fast clicks on macOS; use `IsMouseLeftReleased()` instead. |
| `IsMouseLeftReleased()` | `bool` | `true` only on the frame left button transitions Pressed → Released. Reliable for fast clicks on macOS because the button must be held ≥1 frame. |
| `GetMousePosition()` | `Point` | Current mouse cursor position in physical window client coordinates. |
