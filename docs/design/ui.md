# §13 UI & HUD

## UI Decisions

| Decision | Value | Rationale |
|---|---|---|
| **UI logic location** | Separate Renderer classes | UI code stays out of entities and screens. |
| **Rendering pass** | Separate SpriteBatch without camera transform | UI is screen-space, not world-space. |
| **HUD time display** | `HudRenderer` draws time as 12-hour format with 30-minute granularity plus a procedural sun/moon indicator | Communicates time-of-day at a glance without cluttering the screen. Procedural indicator avoids sprite asset dependency. |
| **Quest tracking HUD** | `GameplayScreen.DrawOverlay()` renders a compact tracked-quest panel | Keeps the current objective visible during play without opening the pause journal. |
| **Pause quest journal** | `PauseScreen` lists all started quests and uses Up/Down + Confirm to change tracking | Reuses the existing pause input model and avoids a separate quest-only screen. |
| **Quest discovery feedback** | Newly started quests queue into a short animated banner sequence that plays a sting, flashes warm, sweeps shimmer, and emits additive glint particles on activation | Makes quest discovery obvious at the moment it happens instead of relying on the pause journal. |
| **Quest completion feedback** | Completed quests briefly replace the tracker with a success state, play a cue, kick and flash the panel green, pop the completion badge, then sweep a short shimmer before falling back to the next active quest or a no-active placeholder | Gives completion payoff in the same HUD location the player already associates with quest state. |

*(Add entries as UI patterns are established — state updates, dialogue system, inventory interaction, etc.)*

## UI Classes

| Class | Description |
|---|---|
| `DayNightIndicator` | Procedural 32×32 widget that renders a sun or moon arcing across a sky background using a 1×1 pixel texture. Sky color, ground strip, stars, and celestial body positions are computed from a game-hour float (0–24). |
| `HudRenderer` | Renders the main HUD overlay in screen space. Draws a semi-transparent rounded panel containing the `DayNightIndicator` and 12-hour formatted time text. Receives game hour and font as parameters — no entity or screen dependencies. |
| `ForestHudRenderer` | Renders the forest survival HUD overlay in screen space. Draws health hearts, XP bar with level label, wave counter, and wave-status banners. Accepts `WaveState` to drive display logic: during the Countdown phase, renders a centered numeric countdown (integer seconds remaining); during the Active phase, renders a wave-remaining-time indicator (countdown number or timer bar); during Cleared/Intermission, renders wave-complete banners. All display data is passed in as parameters — no direct entity or system references. |
| `QuestTrackerRenderer` | Renders a compact top-left quest panel in screen space. Shows active tracked objectives, animates completed quests with a short kick, green flash, badge pop, and shimmer pass, and renders a subdued "No active quest" empty state when no active quest remains. |
| `QuestJournalRenderer` | Renders the pause-screen quest journal in screen space. Lists all started quests, marks the currently tracked quest, highlights the current selection, and shows the selected quest's description plus objective/status details. |
| `QuestDiscoveryBannerRenderer` | Renders a large top-center quest discovery banner in screen space. Used for newly started quests and designed to read clearly before the player ever opens the pause journal. Handles the warm flash, shimmer, and badge pop for the active discovery card. |
| `QuestDiscoveryGlintPass` | Owns pooled screen-space particles for quest discovery feedback. Emits short additive glint bursts around the discovery banner when a queued quest becomes the active banner. |

*(Add entries as UI classes are created — DialogueBoxRenderer, etc.)*
