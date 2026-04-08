using Microsoft.Xna.Framework;

namespace DogDays.Game.Systems;

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
    public float GroundY;       // Local ground plane relative to the spawn point
    public float BounceDamping; // Vertical velocity multiplier on bounce
    public float BounceFriction; // Horizontal velocity multiplier on bounce
    public float LifeRemaining; // In seconds
    public float InitialLife;   // To calculate fade % and lerp color
    public int RemainingBounces;
    public bool IsActive;
}
