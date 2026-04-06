using System;

namespace RiverRats.Game.Data.Save;

/// <summary>
/// Root save data DTO. Contains all persisted state for a single save slot.
/// </summary>
internal sealed class SaveGameData
{
    /// <summary>Current save schema version. Increment when the schema changes.</summary>
    public const int CurrentVersion = 2;

    /// <summary>Save schema version used when this data was written.</summary>
    public int Version { get; set; } = CurrentVersion;

    /// <summary>UTC timestamp when this save was created.</summary>
    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Player state snapshot.</summary>
    public SavePlayerData Player { get; set; } = new();

    /// <summary>Per-quest state snapshots.</summary>
    public SaveQuestStateData[] Quests { get; set; } = [];

    /// <summary>Combat stats snapshot (forest minigame).</summary>
    public SaveCombatStatsData CombatStats { get; set; } = new();

    /// <summary>Day/night cycle snapshot.</summary>
    public SaveDayNightData DayNight { get; set; } = new();

    /// <summary>Mutable watercraft state snapshots across all visited maps.</summary>
    public SaveWatercraftData[] Watercraft { get; set; } = [];
}
