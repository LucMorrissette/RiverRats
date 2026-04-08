namespace DogDays.Game.Data.Save;

/// <summary>
/// Snapshot of day/night cycle state for save/load.
/// </summary>
internal sealed class SaveDayNightData
{
    /// <summary>Cycle progress (0–1).</summary>
    public float CycleProgress { get; set; }
}
