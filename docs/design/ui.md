# §13 UI & HUD

## UI Decisions

| Decision | Value | Rationale |
|---|---|---|
| **UI logic location** | Separate Renderer classes | UI code stays out of entities and screens. |
| **Rendering pass** | Separate SpriteBatch without camera transform | UI is screen-space, not world-space. |
| **HUD time display** | `HudRenderer` draws time as 12-hour format with 30-minute granularity plus a procedural sun/moon indicator | Communicates time-of-day at a glance without cluttering the screen. Procedural indicator avoids sprite asset dependency. |

*(Add entries as UI patterns are established — state updates, dialogue system, inventory interaction, etc.)*

## UI Classes

| Class | Description |
|---|---|
| `DayNightIndicator` | Procedural 32×32 widget that renders a sun or moon arcing across a sky background using a 1×1 pixel texture. Sky color, ground strip, stars, and celestial body positions are computed from a game-hour float (0–24). |
| `HudRenderer` | Renders the main HUD overlay in screen space. Draws a semi-transparent rounded panel containing the `DayNightIndicator` and 12-hour formatted time text. Receives game hour and font as parameters — no entity or screen dependencies. |

*(Add entries as UI classes are created — DialogueBoxRenderer, etc.)*
