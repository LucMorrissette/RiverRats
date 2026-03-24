using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiverRats.Game.Entities;

/// <summary>
/// A garden gnome enemy that hops toward a target position.
/// Uses a sine-wave Y offset for the hopping visual — no sprite sheet animation required.
/// </summary>
internal sealed class GnomeEnemy
{
    private const float HopCycleDuration = 0.45f;
    private const float HopHeight = 6f;
    private const float MoveSpeed = 35f;

    private Vector2 _position;
    private float _hopPhase;
    private bool _facingLeft;

    /// <summary>
    /// Creates a gnome enemy at the given world position.
    /// </summary>
    /// <param name="position">Initial world position (top-left of sprite).</param>
    /// <param name="initialHopPhase">Starting hop phase (0–1) to stagger gnome hop cycles.</param>
    public GnomeEnemy(Vector2 position, float initialHopPhase)
    {
        _position = position;
        _hopPhase = initialHopPhase;
    }

    /// <summary>World position of the gnome (top-left of sprite, excluding hop offset).</summary>
    public Vector2 Position => _position;

    /// <summary>
    /// Advances the gnome one frame: moves toward the target and cycles the hop animation.
    /// </summary>
    /// <param name="gameTime">Current frame timing.</param>
    /// <param name="targetPosition">World position the gnome hops toward (typically the player centre).</param>
    public void Update(GameTime gameTime, Vector2 targetPosition)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Move toward target.
        var toTarget = targetPosition - _position;
        if (toTarget.LengthSquared() > 1f)
        {
            toTarget.Normalize();
            _position += toTarget * MoveSpeed * dt;
        }

        // Update facing based on target direction.
        _facingLeft = targetPosition.X < _position.X;

        // Advance hop cycle.
        _hopPhase += dt / HopCycleDuration;
        if (_hopPhase > 1f)
            _hopPhase -= 1f;
    }

    /// <summary>
    /// Draws the gnome with its current hop offset and facing direction.
    /// </summary>
    /// <param name="spriteBatch">Active sprite batch (must be within Begin/End).</param>
    /// <param name="texture">The garden gnome texture.</param>
    /// <param name="layerDepth">Y-sort depth for FrontToBack ordering.</param>
    public void Draw(SpriteBatch spriteBatch, Texture2D texture, float layerDepth)
    {
        var hopOffset = -MathF.Abs(MathF.Sin(_hopPhase * MathHelper.TwoPi)) * HopHeight;
        var drawPos = new Vector2(_position.X, _position.Y + hopOffset);

        var effects = _facingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        spriteBatch.Draw(
            texture,
            drawPos,
            sourceRectangle: null,
            Color.White,
            rotation: 0f,
            origin: Vector2.Zero,
            scale: 1f,
            effects: effects,
            layerDepth: layerDepth);
    }

    /// <summary>
    /// Returns the bounding rectangle used for Y-sorting. Based on world position (no hop offset).
    /// </summary>
    public Rectangle Bounds => new(
        (int)_position.X,
        (int)_position.Y,
        16,
        16);
}
