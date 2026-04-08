# Feature 003 — Screen Manager

## Goal

Replace the direct gameplay loop in `Game1` with a stack-based screen manager so that `Game1` becomes a thin orchestrator. This unlocks title screens, pause overlays, transitions, and clean save/load wiring.

## Architecture

### Screen Lifecycle

Every screen follows this contract:

| Method | Called When |
|---|---|
| `LoadContent()` | Once, when the screen is first pushed |
| `Update(GameTime)` | Every frame the screen is the **active** (topmost) screen |
| `Draw(GameTime, SpriteBatch)` | Every frame the screen is visible |
| `UnloadContent()` | When the screen is removed from the stack |

### Stack Behavior

- **Push** — adds a screen on top. Previous screen stays in the stack (can be drawn underneath for overlays).
- **Pop** — removes the topmost screen and calls its `UnloadContent()`.
- **Replace** — pops all screens and pushes a new one (e.g., returning to title).
- Only the **topmost screen** receives `Update()` calls (input exclusivity).
- Screens below the top can still be drawn if the top screen is **transparent** (e.g., pause overlay on top of gameplay).

### Rendering Pipeline

The existing virtual-resolution pipeline (960×540 render target → upscaled to window) stays in `Game1`. The ScreenManager draws into the render target; `Game1` upscales the result.

```
Game1.Draw()
  ├─ SetRenderTarget(sceneRT)
  ├─ ScreenManager.Draw()          ← screens draw into the 960×540 RT
  ├─ SetRenderTarget(null)
  └─ Draw sceneRT → backbuffer (upscaled)
```

### Input Flow

`Game1.Update()` calls `InputManager.Update()` once, then passes it to `ScreenManager.Update()` which routes it to the topmost screen.

## New Types

| Type | Location | Purpose |
|---|---|---|
| `IGameScreen` | `Screens/IGameScreen.cs` | Contract for all game screens |
| `ScreenManager` | `Screens/ScreenManager.cs` | Stack-based screen host |
| `GameplayScreen` | `Screens/GameplayScreen.cs` | Holds all current gameplay logic (player, camera, world) |

## Changes to Existing Types

| Type | Change |
|---|---|
| `Game1` | Remove all gameplay fields/logic. Keep virtual resolution, render target, input manager. Delegate to `ScreenManager`. |

## Files Created

- `src/DogDays.Game/Screens/IGameScreen.cs`
- `src/DogDays.Game/Screens/ScreenManager.cs`
- `src/DogDays.Game/Screens/GameplayScreen.cs`
- `docs/features/003-screen-manager.md` (this file)

## Files Modified

- `src/DogDays.Game/Game1.cs`
- `docs/DESIGN.md`
- `docs/design/screens-input.md`

## Tests

- `ScreenManagerTests.cs` — Push/pop/replace behavior, update routing, draw ordering
- Existing gameplay tests remain passing (PlayerBlock, Camera2D, InputManager are unchanged)

## Acceptance Criteria

- [ ] `Game1` no longer contains any gameplay logic (player, camera, world references)
- [ ] Running the game shows the same gameplay as before (crimson block on grass map with water collision)
- [ ] Pressing Escape still exits the game
- [ ] All existing tests pass
- [ ] New ScreenManager tests pass
