using Microsoft.Xna.Framework;

namespace RiverRats.Game.Data;

/// <summary>
/// Configuration data for a particle effect.
/// Defines how particles should look and behave when emitted.
/// </summary>
public sealed record ParticleProfile
{
    /// <summary>Number of particles to spawn per second.</summary>
    public float SpawnRate { get; init; } = 10f;

    /// <summary>Minimum life of a particle in seconds.</summary>
    public float MinLife { get; init; } = 1.0f;

    /// <summary>Maximum life of a particle in seconds.</summary>
    public float MaxLife { get; init; } = 2.0f;

    /// <summary>Minimum travel speed.</summary>
    public float MinSpeed { get; init; } = 20f;

    /// <summary>Maximum travel speed.</summary>
    public float MaxSpeed { get; init; } = 50f;

    /// <summary>Minimum scale.</summary>
    public float MinScale { get; init; } = 0.5f;

    /// <summary>Maximum scale.</summary>
    public float MaxScale { get; init; } = 1.5f;

    /// <summary>Color at the start of the particle's life.</summary>
    public Color StartColor { get; init; } = Color.White;

    /// <summary>Color at the end of the particle's life.</summary>
    public Color EndColor { get; init; } = Color.Transparent;

    /// <summary>The spread angle in radians (e.g., PI/4 for a tight cone).</summary>
    public float SpreadRadians { get; init; } = MathHelper.PiOver4;

    /// <summary>Downward acceleration in pixels per second². Negative values = rise.</summary>
    public float Gravity { get; init; } = 0f;

    /// <summary>Minimum distance below the spawn point where the particle can hit its local ground plane.</summary>
    public float MinGroundOffset { get; init; } = 0f;

    /// <summary>Maximum distance below the spawn point where the particle can hit its local ground plane.</summary>
    public float MaxGroundOffset { get; init; } = 0f;

    /// <summary>How many times the particle can bounce off its local ground plane before expiring.</summary>
    public int MaxGroundBounces { get; init; } = 0;

    /// <summary>Vertical velocity multiplier applied after a bounce.</summary>
    public float BounceDamping { get; init; } = 0.5f;

    /// <summary>Horizontal velocity multiplier applied after a bounce.</summary>
    public float BounceFriction { get; init; } = 0.7f;
}
