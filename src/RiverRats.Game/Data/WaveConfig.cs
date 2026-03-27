using System.Collections.Generic;

namespace RiverRats.Data;

/// <summary>
/// Readonly data describing one wave's parameters for the forest survival minigame.
/// </summary>
internal sealed class WaveConfig
{
    /// <summary>1-based wave number.</summary>
    public int WaveNumber { get; init; }

    /// <summary>Duration of the wave in seconds.</summary>
    public float DurationSeconds { get; init; }

    /// <summary>Speed multiplier applied to all standard gnomes this wave.</summary>
    public float EnemySpeedMultiplier { get; init; }

    /// <summary>Hit points for each standard gnome spawned this wave.</summary>
    public int EnemyHp { get; init; }

    /// <summary>
    /// Spawn weight for each enemy type this wave. Higher weight = more likely to be chosen.
    /// Types not present in the dictionary will not spawn.
    /// </summary>
    public Dictionary<EnemyType, float> EnemyTypeMix { get; init; } = new()
    {
        { EnemyType.Standard, 1.0f }
    };
}
