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
| `ForestHudRenderer` | Renders the forest survival HUD overlay in screen space. Draws health hearts, XP bar with level label, wave counter, and wave-status banners. Accepts `WaveState` to drive display logic: during the Countdown phase, renders a centered numeric countdown (integer seconds remaining); during the Active phase, renders a wave-remaining-time indicator (countdown number or timer bar); during Cleared/Intermission, renders wave-complete banners. All display data is passed in as parameters — no direct entity or system references. |

*(Add entries as UI classes are created — DialogueBoxRenderer, etc.)*
