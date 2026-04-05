# §26 Game Events Catalog

Events are listed with their declaring class, delegate type, parameters, and the condition that fires them.

> **Note on `GnomeSpawner` callbacks:** `OnGnomeDied` and `OnPlayerHit` are `Action` delegate properties rather than C# `event` fields, but they serve the same notification contract. They are included here because they cross the boundary between systems and gameplay.

## Active Events

| Event | Declaring Class | Signature | Fires When |
|---|---|---|---|
| `OnGnomeDied` | `GnomeSpawner` | `Action<Vector2, EnemyType, bool>` — position, enemy type, drop loot | A gnome finishes its death animation and is removed from the pool. The `Vector2` is the gnome's centre position, `EnemyType` is its variant, and the `bool` is `true` when loot drops are not suppressed. |
| `OnPlayerHit` | `GnomeSpawner` | `Action` — no parameters | A gnome's lunge lands on the player (`JustHitPlayer` is `true` for exactly one frame). |
| `OnWaveStarted` | `WaveManager` | `event Action<int>` — 1-based wave number | The pre-wave countdown ends and the Active spawning phase begins. |
| `OnWaveCleared` | `WaveManager` | `event Action<int>` — 1-based wave number | The wave timer expires (Active phase ends). Remaining enemies persist but no new gnomes spawn. |
| `OnAllWavesComplete` | `WaveManager` | `event Action` — no parameters | The final wave is cleared and the state machine transitions to `AllWavesComplete`. |
| `OnLevelUp` | `XpLevelSystem` | `event Action<int>` — new level number | Accumulated XP crosses the `XpToNextLevel` threshold. `PlayerCombatStats.ApplyLevelUp()` is called before the event fires. |
| `NpcTalkedTo` | `GameEventBus` (published by `GameplayScreen`) | `GameEvent` — `targetId` = NPC id, `amount` = 1 | The player starts dialog with a quest-relevant NPC (`mom`, `grandpa`). |
| `ZoneEntered` | `GameEventBus` (published by `GameplayScreen` / `FishingScreen`) | `GameEvent` — `targetId` = map asset name, `amount` = 1 | A gameplay or fishing map finishes loading for the current screen. |
| `EnemyKilled` | `GameEventBus` (published by `GameplayScreen` via `GnomeSpawner.OnGnomeDied`) | `GameEvent` — `targetId` = `EnemyType`, `amount` = 1 | A loot-dropping enemy death is swept from the active pool, which corresponds to a player/follower kill. |
| `WaveCleared` | `GameEventBus` (published by `GameplayScreen` via `WaveManager.OnWaveCleared`) | `GameEvent` — `targetId` = cleared wave number, `amount` = 1 | The forest wave timer expires and the wave transitions into intermission. |
| `LevelReached` | `GameEventBus` (published by `GameplayScreen` via `XpLevelSystem.OnLevelUp`) | `GameEvent` — `targetId` = new level number, `amount` = 1 | The player levels up in the forest combat map. |
