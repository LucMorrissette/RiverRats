using Microsoft.Xna.Framework;

namespace RiverRats.Game.Systems;

/// <summary>
/// Represents a single particle in the particle system.
/// Using a struct for memory efficiency and cache friendliness.
/// </summary>
public struct Particle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Color StartColor;
    public Color EndColor;
    public float Scale;
    public float Rotation;
    public float AngularVelocity;
    public float Gravity;       // Downward acceleration in pixels/sec²
    public float LifeRemaining; // In seconds
    public float InitialLife;   // To calculate fade % and lerp color
    public bool IsActive;
}
