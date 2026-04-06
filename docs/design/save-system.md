# §18 Save & Persistence

| Decision | Value | Rationale |
|---|---|---|
| **Save abstraction** | `ISaveGameService` interface | Decoupled from file system; testable via fakes. |
| **Save format** | JSON | Human-readable, easy to debug during development. |
| **Save format versioning** | `SaveGameData.CurrentVersion` integer (v2) | Enables explicit gating and migration when schema changes. |
| **Capture/restore pattern** | `SaveGameMapper` with deterministic capture/apply | Single source of truth for serialization logic. |
| **Slot count** | 3 (slot 0 = auto-save, slots 1–2 = manual) | Simple multi-slot without full menu. |
| **Atomic writes** | Write to `.tmp`, rename to final path | Prevents corruption on crash during save. |
| **Auto-save triggers** | Zone transition, quest completed, level-up | Saves at meaningful progress milestones. |
| **Dev hotkeys** | K = quick save (slot 1), L = quick load (slot 1) | Rapid iteration during development. |
| **Save file location** | `%APPDATA%/RiverRats/saves/slot_N.json` | Standard Windows user data location. |
| **Load mechanism** | Screen replacement via `ScreenManager.Replace()` | Clean teardown — no mid-frame state mutation. |
| **Restore scope** | Exact player position + zone + combat stats + quests + day/night + per-map watercraft state | Full gameplay state round-trip. |

## Persisted State

| Data | DTO | Owner |
|---|---|---|
| Player position, facing, zone | `SavePlayerData` | `SaveGameMapper` |
| Quest status, objective index, progress | `SaveQuestStateData[]` | `SaveGameMapper` via `QuestState.RestoreState()` |
| Combat stats (level, XP, HP, multipliers) | `SaveCombatStatsData` | `SaveGameMapper` |
| Day/night cycle progress | `SaveDayNightData` | `SaveGameMapper` |
| Watercraft position, facing, occupied state by map | `SaveWatercraftData[]` | `SaveGameMapper` + `GameplayScreen` session restore |

## Explicitly Not Persisted

| Data | Reason |
|---|---|
| Particle effects, smoke, sparks | Transient visual — recreated on zone load. |
| Enemy positions, gnome spawner state | Ephemeral — enemies respawn on zone entry. |
| NPC patrol routes | Re-randomized each zone load. |
| Follower position/trail | Reconstructs naturally from player position. |
| Screen shake, flash timers | Transient feedback effects. |
| Dialog progress | NPCs repeat dialog on re-interaction. |
| Fishing minigame state | Separate screen — not yet persisted. |
