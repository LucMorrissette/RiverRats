namespace RiverRats.Game.Data.Save;

/// <summary>
/// Snapshot of player state for save/load.
/// </summary>
internal sealed class SavePlayerData
{
    /// <summary>Player world X position.</summary>
    public float X { get; set; }

    /// <summary>Player world Y position.</summary>
    public float Y { get; set; }

    /// <summary>Current facing direction.</summary>
    public FacingDirection Facing { get; set; }

    /// <summary>Content asset name of the zone the player is in (e.g. "Maps/StarterMap").</summary>
    public string ZoneMapAssetName { get; set; } = string.Empty;
}
