namespace RiverRats.Data;

/// <summary>
/// Tracks the lifecycle phase of the current wave in the forest survival minigame.
/// </summary>
internal enum WaveState
{
    /// <summary>Initial state before the first wave begins.</summary>
    PreWave,

    /// <summary>Countdown phase (5-4-3-2-1) before the wave starts.</summary>
    Countdown,

    /// <summary>Wave is active: enemies continuously spawn and timer counts down.</summary>
    Active,

    /// <summary>Wave timer expired. Transitional state.</summary>
    Cleared,

    /// <summary>Extended pause between waves for orb collection.</summary>
    Intermission,

    /// <summary>All waves have been completed (victory).</summary>
    AllWavesComplete,
}
