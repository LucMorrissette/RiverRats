using Microsoft.Xna.Framework;

namespace RiverRats.Game.Graphics;

/// <summary>
/// A snapshot of a single point light's properties for a given frame.
/// Passed to <see cref="LightingRenderer"/> each frame so it can draw the lightmap
/// without holding a reference to any entity.
/// </summary>
public readonly struct LightData
{
    /// <summary>World-space position of the light center.</summary>
    public readonly Vector2 Position;

    /// <summary>Light radius in world pixels.</summary>
    public readonly float Radius;

    /// <summary>Light color (RGB).</summary>
    public readonly Color Color;

    /// <summary>Light intensity 0–1, applied as the gradient texture's alpha tint.</summary>
    public readonly float Intensity;

    /// <summary>
    /// Creates a LightData snapshot.
    /// </summary>
    public LightData(Vector2 position, float radius, Color color, float intensity)
    {
        Position = position;
        Radius = radius;
        Color = color;
        Intensity = intensity;
    }
}
