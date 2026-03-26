namespace RiverRats.Game.Data;

/// <summary>
/// Behavioral states for <see cref="Entities.GnomeEnemy"/>.
/// </summary>
internal enum GnomeState
{
    /// <summary>Following the flow field toward the player.</summary>
    Chasing,

    /// <summary>Stopped and compressing before a lunge attack.</summary>
    WindingUp,

    /// <summary>Flying toward the player hat-first at high speed.</summary>
    Lunging,

    /// <summary>Recoiling/bouncing after a hit or miss, temporarily unable to act.</summary>
    Stunned,

    /// <summary>Playing death pop animation before removal.</summary>
    Dying,
}
