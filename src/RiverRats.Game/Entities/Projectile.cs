using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiverRats.Game.Entities;

/// <summary>
/// A small projectile that flies in a straight line at constant speed.
/// Managed by <see cref="RiverRats.Game.Systems.ProjectileSystem"/> via object pooling.
/// </summary>
internal sealed class Projectile
{
    private const float MaxLifetime = 1f;
    private const int CollisionWidth = 10;
    private const int CollisionHeight = 6;
    private const int HalfCollisionWidth = CollisionWidth / 2;
    private const int HalfCollisionHeight = CollisionHeight / 2;

    private Vector2 _position;
    private Vector2 _velocity;
    private float _lifetimeRemaining;
    private float _rotation;
    private int _remainingPierces;
    private bool _alive;

    /// <summary>Current world position of the projectile centre.</summary>
    public Vector2 Position => _position;

    /// <summary>Whether the projectile is currently active.</summary>
    public bool IsAlive => _alive;

    /// <summary>Current world-space rotation in radians.</summary>
    public float Rotation => _rotation;

    /// <summary>Remaining enemy hits before the projectile expires.</summary>
    public int RemainingPierces => _remainingPierces;

    /// <summary>Bounding rectangle for collision checks centred on the projectile.</summary>
    public Rectangle Bounds => new(
        (int)_position.X - HalfCollisionWidth,
        (int)_position.Y - HalfCollisionHeight,
        CollisionWidth,
        CollisionHeight);

    /// <summary>
    /// Activates this pooled projectile, launching it from the given origin with the given velocity.
    /// </summary>
    /// <param name="position">Starting world position (centre).</param>
    /// <param name="velocity">Velocity vector (direction × speed).</param>
    /// <param name="pierceCount">How many enemies this projectile can kill before expiring.</param>
    public void Fire(Vector2 position, Vector2 velocity, int pierceCount)
    {
        _position = position;
        _velocity = velocity;
        _lifetimeRemaining = MaxLifetime;
        _remainingPierces = pierceCount;
        _rotation = velocity.LengthSquared() > 0f ? MathF.Atan2(velocity.Y, velocity.X) : 0f;
        _alive = true;
    }

    /// <summary>
    /// Moves the projectile and ticks its lifetime. Deactivates when lifetime expires.
    /// </summary>
    /// <param name="gameTime">Current frame timing.</param>
    public void Update(GameTime gameTime)
    {
        if (!_alive) return;

        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _position += _velocity * dt;
        _lifetimeRemaining -= dt;

        if (_lifetimeRemaining <= 0f)
            _alive = false;
    }

    /// <summary>
    /// Marks this projectile as dead (e.g., on collision).
    /// </summary>
    public void Kill()
    {
        _alive = false;
    }

    /// <summary>
    /// Consumes one enemy hit and returns whether the projectile should remain alive.
    /// </summary>
    public bool RegisterHit()
    {
        if (!_alive)
            return false;

        _remainingPierces--;
        if (_remainingPierces <= 0)
            _alive = false;

        return _alive;
    }

    /// <summary>
    /// Draws the projectile using a directional arrow texture.
    /// </summary>
    /// <param name="spriteBatch">Active sprite batch (must be within Begin/End).</param>
    /// <param name="projectileTexture">Projectile texture facing right by default.</param>
    /// <param name="layerDepth">Y-sort depth for FrontToBack ordering.</param>
    public void Draw(SpriteBatch spriteBatch, Texture2D projectileTexture, float layerDepth)
    {
        spriteBatch.Draw(
            projectileTexture,
            _position,
            sourceRectangle: null,
            Color.White,
            rotation: _rotation,
            origin: new Vector2(projectileTexture.Width * 0.5f, projectileTexture.Height * 0.5f),
            scale: 1f,
            effects: SpriteEffects.None,
            layerDepth: layerDepth);
    }
}
