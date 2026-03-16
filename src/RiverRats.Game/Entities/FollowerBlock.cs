#nullable enable

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Components;
using RiverRats.Game.Data;

namespace RiverRats.Game.Entities;

/// <summary>
/// Follower entity at a fixed offset from the leader.
/// </summary>
public sealed class FollowerBlock
{
    private readonly Vector2 _size;
    private readonly Rectangle _worldBounds;
    private readonly Vector2 _positionOffset;

    private Vector2 _position;
    private FacingDirection _facing = FacingDirection.Down;

    /// <summary>
    /// Initializes a follower block at a fixed offset from a leader.
    /// </summary>
    /// <param name="startPosition">Initial top-left world position in pixels.</param>
    /// <param name="size">Block size in pixels.</param>
    /// <param name="worldBounds">World bounds in pixels used for position clamping.</param>
    /// <param name="positionOffset">Fixed offset from leader position (e.g., (0, 32) for below).</param>
    public FollowerBlock(
        Vector2 startPosition,
        Point size,
        Rectangle worldBounds,
        Vector2 positionOffset)
    {
        _position = startPosition;
        _size = new Vector2(size.X, size.Y);
        _worldBounds = worldBounds;
        _positionOffset = positionOffset;

        ClampToBounds();
    }

    /// <summary>Current top-left world position in pixels.</summary>
    public Vector2 Position => _position;

    /// <summary>Current world-space center point used for rendering and spatial logic.</summary>
    public Vector2 Center => _position + (_size * 0.5f);

    /// <summary>Current facing direction.</summary>
    public FacingDirection Facing => _facing;

    /// <summary>Current AABB in world-space pixels.</summary>
    public Rectangle Bounds => new(
        (int)_position.X,
        (int)_position.Y,
        (int)_size.X,
        (int)_size.Y);

    /// <summary>
    /// Updates follower to remain at a fixed offset from the leader position.
    /// </summary>
    /// <param name="leaderPosition">Leader's top-left world position in pixels.</param>
    /// <param name="leaderFacing">Leader's facing direction.</param>
    public void Update(Vector2 leaderPosition, FacingDirection leaderFacing)
    {
        _position = ClampPosition(leaderPosition + _positionOffset);
        _facing = leaderFacing;
    }

    /// <summary>
    /// Draws the follower via a sprite animator.
    /// </summary>
    /// <param name="spriteBatch">Sprite batch for world pass.</param>
    /// <param name="animator">Sprite animator component.</param>
    /// <param name="spriteSheet">Character sprite sheet texture.</param>
    public void Draw(SpriteBatch spriteBatch, SpriteAnimator animator, Texture2D spriteSheet)
    {
        animator.Draw(spriteBatch, spriteSheet, _position);
    }

    private void ClampToBounds()
    {
        _position = ClampPosition(_position);
    }

    private Vector2 ClampPosition(Vector2 position)
    {
        var minX = _worldBounds.Left;
        var minY = _worldBounds.Top;
        var maxX = _worldBounds.Right - _size.X;
        var maxY = _worldBounds.Bottom - _size.Y;

        return new Vector2(
            MathHelper.Clamp(position.X, minX, maxX),
            MathHelper.Clamp(position.Y, minY, maxY));
    }
}
