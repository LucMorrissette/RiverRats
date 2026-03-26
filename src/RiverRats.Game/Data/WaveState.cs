namespace RiverRats.Data;

/// <summary>
/// Tracks the lifecycle phase of the current wave in the forest survival minigame.
/// </summary>
internal enum WaveState
{
    /// <summary>Initial state before the first wave begins.</summary>
    PreWave,

    /// <summary>Enemies are being stagger-spawned into the arena.</summary>
    Spawning,

    /// <summary>All enemies are spawned; waiting for them to be defeated.</summary>
    Active,

    /// <summary>All enemies from the wave are dead. Transitional state.</summary>
    Cleared,

    /// <summary>Brief pause between waves.</summary>
    Intermission,

    /// <summary>All waves have been completed (victory).</summary>
    AllWavesComplete,
}
