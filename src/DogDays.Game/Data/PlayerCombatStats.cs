namespace DogDays.Data;

/// <summary>
/// Mutable combat stat modifiers for the forest survival minigame.
/// Owned by the gameplay screen and passed into systems that need it.
/// </summary>
public sealed class PlayerCombatStats
{
    /// <summary>Starting max HP.</summary>
    public int MaxHp { get; set; } = 5;

    /// <summary>Multiplied against base player speed.</summary>
    public float SpeedMultiplier { get; set; } = 1.0f;

    /// <summary>Multiplied against base weapon cooldown (lower = faster).</summary>
    public float CooldownMultiplier { get; set; } = 1.0f;

    /// <summary>Multiplied against base projectile speed.</summary>
    public float ProjectileSpeedMultiplier { get; set; } = 1.0f;

    /// <summary>Multiplied against base projectile range/lifetime.</summary>
    public float ProjectileRangeMultiplier { get; set; } = 1.0f;

    /// <summary>Current level.</summary>
    public int Level { get; set; } = 1;

    /// <summary>Current XP.</summary>
    public int Xp { get; set; } = 0;

    /// <summary>XP needed for next level-up.</summary>
    public int XpToNextLevel { get; set; } = 10;

    /// <summary>
    /// Increments level and applies stat boosts. Carries over excess XP
    /// and scales the XP threshold for the next level.
    /// </summary>
    public void ApplyLevelUp()
    {
        Level += 1;
        MaxHp += 1;
        SpeedMultiplier += 0.03f;
        CooldownMultiplier *= 0.92f;
        ProjectileSpeedMultiplier += 0.05f;
        ProjectileRangeMultiplier += 0.10f;
        Xp -= XpToNextLevel;
        XpToNextLevel = (int)(XpToNextLevel * 1.5f);
    }

    /// <summary>
    /// Resets all fields to defaults for a new run.
    /// </summary>
    public void Reset()
    {
        MaxHp = 5;
        SpeedMultiplier = 1.0f;
        CooldownMultiplier = 1.0f;
        ProjectileSpeedMultiplier = 1.0f;
        ProjectileRangeMultiplier = 1.0f;
        Level = 1;
        Xp = 0;
        XpToNextLevel = 10;
    }
}
