namespace RiverRats.Game.Data;

/// <summary>
/// Tuning parameters for follower movement dynamics (ease-out, catch-up, facing dead-zone).
/// </summary>
public sealed record FollowerMovementConfig
{
    /// <summary>Distance in pixels below which facing is not updated, to avoid flicker.</summary>
    public float FacingDeadZonePixels { get; init; } = 4f;

    /// <summary>Distance in pixels within which the follower decelerates (ease-out).</summary>
    public float EaseOutRadiusPixels { get; init; } = 12f;

    /// <summary>Distance in pixels beyond which the follower begins catching up faster.</summary>
    public float CatchUpThresholdPixels { get; init; } = 64f;

    /// <summary>Distance in pixels at which catch-up reaches its maximum multiplier.</summary>
    public float CatchUpFullDistancePixels { get; init; } = 128f;

    /// <summary>Maximum speed multiplier applied when catching up to the leader.</summary>
    public float CatchUpMaxMultiplier { get; init; } = 1.5f;

    /// <summary>Default configuration with all movement dynamics active.</summary>
    public static FollowerMovementConfig Default { get; } = new();

    /// <summary>Flat configuration with no ease-out or catch-up — for testing core movement math.</summary>
    public static FollowerMovementConfig Flat { get; } = new()
    {
        FacingDeadZonePixels = 0f,
        EaseOutRadiusPixels = 0f,
        CatchUpThresholdPixels = float.MaxValue,
        CatchUpMaxMultiplier = 1f
    };
}
