namespace RiverRats.Game.Systems;

/// <summary>
/// Controls which entities are drawn relative to the player's sort depth.
/// Used when rendering world entities across multiple passes (normal, occlusion behind, occlusion in-front).
/// </summary>
public enum EntityDepthFilter
{
    /// <summary>Draw all entities regardless of depth relative to the player.</summary>
    All,

    /// <summary>Draw only entities at or behind the player's sort depth (lower Y = behind).</summary>
    BehindOrAtPlayer,

    /// <summary>Draw only entities strictly in front of the player's sort depth (higher Y = in front).</summary>
    InFrontOfPlayer,
}
