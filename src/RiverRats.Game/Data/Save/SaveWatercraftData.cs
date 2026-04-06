using RiverRats.Game.Data;

namespace RiverRats.Game.Data.Save;

/// <summary>
/// Snapshot of a placed watercraft's mutable runtime state for save/load.
/// </summary>
internal sealed class SaveWatercraftData
{
    /// <summary>Map asset name that owns this watercraft instance.</summary>
    public string MapAssetName { get; set; } = string.Empty;

    /// <summary>Initial TMX placement X used to match the saved state back to the authored prop.</summary>
    public float InitialX { get; set; }

    /// <summary>Initial TMX placement Y used to match the saved state back to the authored prop.</summary>
    public float InitialY { get; set; }

    /// <summary>Current watercraft center X.</summary>
    public float CenterX { get; set; }

    /// <summary>Current watercraft center Y.</summary>
    public float CenterY { get; set; }

    /// <summary>Current travel-facing direction.</summary>
    public FacingDirection Facing { get; set; } = FacingDirection.Down;

    /// <summary>Whether the player party was seated in this craft when the snapshot was taken.</summary>
    public bool IsOccupied { get; set; }
}