using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Systems;
using RiverRats.Game.World;

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
    private const int SpriteSize = 16;

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
    /// Advances the gnome one frame: moves along the flow field and cycles the hop animation.
    /// The flow field provides a pre-computed direction that routes around obstacles.
    /// Axis-separated collision checks are kept as a safety net for sub-tile prop boundaries.
    /// </summary>
    /// <param name="gameTime">Current frame timing.</param>
    /// <param name="targetPosition">World position of the target (used for facing when flow is zero).</param>
    /// <param name="flowField">Pre-computed BFS flow field pointing toward the target.</param>
    /// <param name="collisionMap">World collision data for fine-grained obstacle queries.</param>
    public void Update(GameTime gameTime, Vector2 targetPosition, FlowField flowField, IMapCollisionData collisionMap)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Query flow field at gnome centre.
        var center = new Vector2(_position.X + SpriteSize * 0.5f, _position.Y + SpriteSize * 0.5f);
        var flowDir = flowField.GetDirection(center);

        if (flowDir != Vector2.Zero)
        {
            var movement = flowDir * MoveSpeed * dt;

            // Try X axis (safety net for sub-tile prop collisions).
            if (movement.X != 0f)
            {
                var candidateX = _position.X + movement.X;
                var candidateBounds = new Rectangle((int)candidateX, (int)_position.Y, SpriteSize, SpriteSize);
                if (!collisionMap.IsWorldRectangleBlocked(candidateBounds))
                    _position.X = candidateX;
            }

            // Try Y axis.
            if (movement.Y != 0f)
            {
                var candidateY = _position.Y + movement.Y;
                var candidateBounds = new Rectangle((int)_position.X, (int)candidateY, SpriteSize, SpriteSize);
                if (!collisionMap.IsWorldRectangleBlocked(candidateBounds))
                    _position.Y = candidateY;
            }
        }

        // Update facing based on flow direction, falling back to target position.
        _facingLeft = flowDir.X < 0f || (flowDir.X == 0f && targetPosition.X < _position.X);

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
        SpriteSize,
        SpriteSize);
}
