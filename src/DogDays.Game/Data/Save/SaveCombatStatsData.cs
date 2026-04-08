namespace DogDays.Game.Data.Save;

/// <summary>
/// Snapshot of player combat stats for save/load (forest minigame).
/// </summary>
internal sealed class SaveCombatStatsData
{
    /// <summary>Maximum hit points.</summary>
    public int MaxHp { get; set; } = 5;

    /// <summary>Current level.</summary>
    public int Level { get; set; } = 1;

    /// <summary>Accumulated XP.</summary>
    public int Xp { get; set; }

    /// <summary>XP threshold for next level-up.</summary>
    public int XpToNextLevel { get; set; } = 10;

    /// <summary>Movement speed multiplier.</summary>
    public float SpeedMultiplier { get; set; } = 1.0f;

    /// <summary>Weapon cooldown multiplier (lower = faster).</summary>
    public float CooldownMultiplier { get; set; } = 1.0f;

    /// <summary>Projectile speed multiplier.</summary>
    public float ProjectileSpeedMultiplier { get; set; } = 1.0f;

    /// <summary>Projectile range multiplier.</summary>
    public float ProjectileRangeMultiplier { get; set; } = 1.0f;
}
