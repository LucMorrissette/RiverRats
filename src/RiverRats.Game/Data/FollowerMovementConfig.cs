namespace RiverRats.Game.Data;

/// <summary>
/// Tuning parameters for breadcrumb-style follower movement.
/// </summary>
public sealed record FollowerMovementConfig
{
    /// <summary>Distance in pixels that the follower trails behind the leader along the recorded path.</summary>
    public float FollowDistancePixels { get; init; } = 30f;

    /// <summary>Distance in pixels used when the leader is not moving, keeping the party slightly tighter at rest.</summary>
    public float IdleFollowDistancePixels { get; init; } = 20f;

    /// <summary>Maximum segment length in pixels retained between recorded trail points.</summary>
    public float TrailSampleDistancePixels { get; init; } = 8f;

    /// <summary>Distance in pixels below which facing is not updated, to avoid flicker.</summary>
    public float FacingDeadZonePixels { get; init; } = 1f;

    /// <summary>How quickly the desired follow distance eases toward its moving or idle target.</summary>
    public float DistanceEasePerSecond { get; init; } = 4f;

    /// <summary>How quickly the follower eases toward the sampled breadcrumb position.</summary>
    public float PositionEasePerSecond { get; init; } = 7f;

    /// <summary>How quickly the follower eases into a side-rest position beside the leader.</summary>
    public float RestPositionEasePerSecond { get; init; } = 3f;

    /// <summary>Distance in pixels within which eased movement snaps to the breadcrumb target and settles.</summary>
    public float PositionSnapDistancePixels { get; init; } = 1f;

    /// <summary>Minimum per-frame movement in pixels required to keep walk animation active.</summary>
    public float AnimationMoveThresholdPixels { get; init; } = 0.25f;

    /// <summary>Horizontal offset in pixels used when resting beside the leader.</summary>
    public float SideRestOffsetPixels { get; init; } = 32f;

    /// <summary>Leader movement distance in pixels required to break out of a side-rest pose and resume breadcrumb following.</summary>
    public float RestExitDistancePixels { get; init; } = 36f;

    /// <summary>Default configuration for a one-character breadcrumb trail.</summary>
    public static FollowerMovementConfig Default { get; } = new();
}
