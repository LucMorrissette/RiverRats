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
    private const int Size = 4;
    private const int HalfSize = Size / 2;

    private Vector2 _position;
    private Vector2 _velocity;
    private float _lifetimeRemaining;
    private bool _alive;

    /// <summary>Current world position of the projectile centre.</summary>
    public Vector2 Position => _position;

    /// <summary>Whether the projectile is currently active.</summary>
    public bool IsAlive => _alive;

    /// <summary>Bounding rectangle for collision checks (4×4 centred on position).</summary>
    public Rectangle Bounds => new(
        (int)_position.X - HalfSize,
        (int)_position.Y - HalfSize,
        Size,
        Size);

    /// <summary>
    /// Activates this pooled projectile, launching it from the given origin with the given velocity.
    /// </summary>
    /// <param name="position">Starting world position (centre).</param>
    /// <param name="velocity">Velocity vector (direction × speed).</param>
    public void Fire(Vector2 position, Vector2 velocity)
    {
        _position = position;
        _velocity = velocity;
        _lifetimeRemaining = MaxLifetime;
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
    /// Draws the projectile as a small cyan rectangle using a 1×1 pixel texture.
    /// </summary>
    /// <param name="spriteBatch">Active sprite batch (must be within Begin/End).</param>
    /// <param name="pixelTexture">A 1×1 white pixel texture.</param>
    /// <param name="layerDepth">Y-sort depth for FrontToBack ordering.</param>
    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, float layerDepth)
    {
        spriteBatch.Draw(
            pixelTexture,
            new Rectangle((int)_position.X - HalfSize, (int)_position.Y - HalfSize, Size, Size),
            sourceRectangle: null,
            Color.Cyan,
            rotation: 0f,
            origin: Vector2.Zero,
            effects: SpriteEffects.None,
            layerDepth: layerDepth);
    }
}
