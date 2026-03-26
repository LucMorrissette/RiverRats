namespace RiverRats.Data;

/// <summary>
/// Readonly data describing one wave's parameters for the forest survival minigame.
/// </summary>
internal sealed class WaveConfig
{
    /// <summary>1-based wave number.</summary>
    public int WaveNumber { get; init; }

    /// <summary>Total enemies to spawn this wave.</summary>
    public int EnemyCount { get; init; }

    /// <summary>Speed multiplier applied to all gnomes this wave.</summary>
    public float EnemySpeedMultiplier { get; init; }

    /// <summary>Hit points for each gnome spawned this wave.</summary>
    public int EnemyHp { get; init; }
}
