namespace DogDays.Data;

/// <summary>
/// Identifies enemy variant kinds for the forest survival minigame.
/// Each type parameterizes visual and behavioral traits on the shared GnomeEnemy entity.
/// </summary>
internal enum EnemyType
{
    /// <summary>Standard gnome with baseline stats.</summary>
    Standard,

    /// <summary>Fast, small, fragile gnome. Higher speed, lower HP, smaller scale.</summary>
    Rusher,

    /// <summary>Slow, large, tough gnome. Lower speed, higher HP, larger scale.</summary>
    Brute,

    /// <summary>Normal stats but explodes on death, dealing area damage.</summary>
    Bomber,
}
