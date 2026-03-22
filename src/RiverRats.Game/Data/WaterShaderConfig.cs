namespace RiverRats.Game.Data;

/// <summary>
/// Configuration values for the water distortion shader.
/// </summary>
public sealed class WaterShaderConfig
{
    /// <summary>How far pixels get displaced. Higher = more dramatic waves.</summary>
    public float Amplitude { get; init; } = 0.004f;

    /// <summary>Wave tightness. Higher = more ripples packed in.</summary>
    public float Frequency { get; init; } = 25f;

    /// <summary>How fast waves animate.</summary>
    public float Speed { get; init; } = 1f;

    /// <summary>Additional displacement for click ripples.</summary>
    public float RippleAmplitude { get; init; } = 0.020f;

    /// <summary>Ripple tightness. Higher = tighter circular ripples.</summary>
    public float RippleFrequency { get; init; } = 40f;

    /// <summary>How fast ripples expand and fade.</summary>
    public float RippleSpeed { get; init; } = 18f;

    /// <summary>Tint applied to surface-reach props at full depth.</summary>
    public Microsoft.Xna.Framework.Vector3 WaterTintColor { get; init; } = new(0.55f, 0.65f, 0.85f);

    /// <summary>Default configuration.</summary>
    public static WaterShaderConfig Default { get; } = new();
}
