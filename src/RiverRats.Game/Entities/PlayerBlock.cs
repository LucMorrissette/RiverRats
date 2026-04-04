using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Components;
using RiverRats.Game.Data;
using RiverRats.Game.Input;
using RiverRats.Game.World;

namespace RiverRats.Game.Entities;

/// <summary>
/// Player entity with sprite-animated, four-direction movement.
/// </summary>
public sealed class PlayerBlock : IWorldProp
{
    private readonly Vector2 _size;
    private readonly float _moveSpeedPixelsPerSecond;
    private readonly Rectangle _worldBounds;
    private readonly float _accelerationRate;

    private Vector2 _position;
    private FacingDirection _facing = FacingDirection.Down;
    private bool _isMoving;
    private float _currentSpeedFraction;
    private const float FootWidthRatio = 0.6f;
    private const float FootHeightRatio = 0.25f;
    private const float FootInsetFromBottom = 2f;
    private readonly Point _footSize;
    private readonly Vector2 _footOffset;

    /// <summary>
    /// Initializes a player block.
    /// </summary>
    /// <param name="startPosition">Initial top-left world position in pixels.</param>
    /// <param name="size">Block size in pixels.</param>
    /// <param name="moveSpeedPixelsPerSecond">Movement speed in pixels per second.</param>
    /// <param name="worldBounds">World bounds in pixels used for movement clamping.</param>
    /// <param name="accelerationRate">Speed ramp rate (units per second). 0 = instant full speed.</param>
    public PlayerBlock(
        Vector2 startPosition,
        Point size,
        float moveSpeedPixelsPerSecond,
        Rectangle worldBounds,
        float accelerationRate = 0f)
    {
        _position = startPosition;
        _size = new Vector2(size.X, size.Y);
        _moveSpeedPixelsPerSecond = moveSpeedPixelsPerSecond;
        _worldBounds = worldBounds;
        _accelerationRate = accelerationRate;

        _footSize = new Point(
            Math.Max(1, (int)MathF.Round(_size.X * FootWidthRatio)),
            Math.Max(1, (int)MathF.Round(_size.Y * FootHeightRatio)));

        _footOffset = new Vector2(
            (_size.X - _footSize.X) / 2f,
            _size.Y - _footSize.Y - FootInsetFromBottom);

        ClampToBounds();
    }

    /// <summary>External speed modifier applied to base movement speed (e.g., from level-ups).</summary>
    internal float SpeedMultiplier { get; set; } = 1.0f;

    /// <summary>Current top-left world position in pixels.</summary>
    public Vector2 Position => _position;

    /// <summary>Current world-space center point used for camera follow.</summary>
    public Vector2 Center => _position + (_size * 0.5f);

    /// <summary>Current facing direction.</summary>
    public FacingDirection Facing => _facing;

    /// <summary>Whether the entity moved this frame.</summary>
    public bool IsMoving => _isMoving;

    /// <summary>
    /// Directly sets the player's world position. Used by scripted sequences
    /// (e.g., couch sitting) that bypass normal movement input.
    /// </summary>
    internal void SetPosition(Vector2 position)
    {
        _position = position;
    }

    /// <summary>
    /// Sets the player's facing direction without requiring input.
    /// Used by scripted sequences.
    /// </summary>
    internal void SetFacing(FacingDirection facing)
    {
        _facing = facing;
    }

    /// <summary>Current AABB in world-space pixels.</summary>
    public Rectangle Bounds => new(
        (int)_position.X,
        (int)_position.Y,
        (int)_size.X,
        (int)_size.Y);

    /// <summary>Focused world-space foot bounds used for collision queries.</summary>
    public Rectangle FootBounds => GetFootBounds(_position);

    /// <summary>
    /// Updates player position from movement input.
    /// </summary>
    /// <param name="gameTime">Frame timing values.</param>
    /// <param name="inputManager">Input source abstraction.</param>
    public void Update(GameTime gameTime, IInputManager inputManager, IMapCollisionData mapCollisionData)
    {
        var direction = Vector2.Zero;

        if (inputManager.IsHeld(InputAction.MoveLeft))
        {
            direction.X -= 1f;
        }

        if (inputManager.IsHeld(InputAction.MoveRight))
        {
            direction.X += 1f;
        }

        if (inputManager.IsHeld(InputAction.MoveUp))
        {
            direction.Y -= 1f;
        }

        if (inputManager.IsHeld(InputAction.MoveDown))
        {
            direction.Y += 1f;
        }

        _isMoving = direction != Vector2.Zero;

        if (_isMoving)
        {
            var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_accelerationRate > 0f)
            {
                _currentSpeedFraction = MathHelper.Clamp(
                    _currentSpeedFraction + _accelerationRate * elapsedSeconds, 0f, 1f);
            }
            else
            {
                _currentSpeedFraction = 1f;
            }

            UpdateFacing(direction);
            direction.Normalize();
            var movementDelta = direction * _moveSpeedPixelsPerSecond * SpeedMultiplier * _currentSpeedFraction * elapsedSeconds;

            if (movementDelta.X != 0f)
            {
                TryMoveOnAxis(new Vector2(movementDelta.X, 0f), mapCollisionData);
            }

            if (movementDelta.Y != 0f)
            {
                TryMoveOnAxis(new Vector2(0f, movementDelta.Y), mapCollisionData);
            }
        }
        else
        {
            _currentSpeedFraction = 0f;
        }
    }

    /// <summary>
    /// Applies externally-driven movement such as a scripted dash while preserving
    /// the player's collision footprint and world-bounds clamping.
    /// </summary>
    /// <param name="movementDelta">World-space movement delta in pixels for this frame.</param>
    /// <param name="mapCollisionData">Collision source used for axis-separated resolution.</param>
    /// <param name="updateFacingFromMovement">True to update facing from the supplied delta.</param>
    internal bool ApplyExternalMovement(Vector2 movementDelta, IMapCollisionData mapCollisionData, bool updateFacingFromMovement = true)
    {
        if (movementDelta == Vector2.Zero)
        {
            _isMoving = false;
            return false;
        }

        if (updateFacingFromMovement)
        {
            UpdateFacing(movementDelta);
        }

        var previousPosition = _position;

        if (movementDelta.X != 0f)
        {
            TryMoveOnAxis(new Vector2(movementDelta.X, 0f), mapCollisionData);
        }

        if (movementDelta.Y != 0f)
        {
            TryMoveOnAxis(new Vector2(0f, movementDelta.Y), mapCollisionData);
        }

        _isMoving = _position != previousPosition;
        return _isMoving;
    }

    /// <summary>
    /// Clears transient movement state when movement is driven by a non-standard sequence.
    /// </summary>
    internal void ClearMovementState()
    {
        _isMoving = false;
        _currentSpeedFraction = 0f;
    }

    /// <summary>
    /// Draws the player via a sprite animator.
    /// </summary>
    /// <param name="spriteBatch">Sprite batch for world pass.</param>
    /// <param name="animator">Sprite animator component.</param>
    /// <param name="spriteSheet">Character sprite sheet texture.</param>
    /// <param name="layerDepth">Depth value for Y-sorting (0 = back, 1 = front).</param>
    public void Draw(SpriteBatch spriteBatch, SpriteAnimator animator, Texture2D spriteSheet, float layerDepth = 0f, Color? tint = null)
    {
        animator.Draw(spriteBatch, spriteSheet, _position, layerDepth, tint);
    }

    /// <summary>
    /// Not supported — the player must be drawn via <see cref="Draw(SpriteBatch,SpriteAnimator,Texture2D,float,Color?)"/>,
    /// which requires an animator and sprite sheet.
    /// </summary>
    void IWorldProp.Draw(SpriteBatch spriteBatch, float layerDepth) =>
        throw new NotSupportedException("PlayerBlock must be drawn via Draw(SpriteBatch, SpriteAnimator, Texture2D, float, Color?).");

    private void UpdateFacing(Vector2 direction)
    {
        // Prefer the dominant axis; if equal, prefer horizontal.
        if (MathF.Abs(direction.X) >= MathF.Abs(direction.Y))
        {
            _facing = direction.X >= 0f ? FacingDirection.Right : FacingDirection.Left;
        }
        else
        {
            _facing = direction.Y >= 0f ? FacingDirection.Down : FacingDirection.Up;
        }
    }

    private void ClampToBounds()
    {
        _position = ClampPosition(_position);
    }

    private void TryMoveOnAxis(Vector2 axisMovement, IMapCollisionData mapCollisionData)
    {
        var remainingDistance = axisMovement.X != 0f ? axisMovement.X : axisMovement.Y;
        var stepDirection = MathF.Sign(remainingDistance);

        while (remainingDistance != 0f)
        {
            var stepMagnitude = MathF.Abs(remainingDistance) >= 1f ? 1f : MathF.Abs(remainingDistance);
            var step = stepMagnitude * stepDirection;
            var delta = axisMovement.X != 0f
                ? new Vector2(step, 0f)
                : new Vector2(0f, step);

            var candidatePosition = ClampPosition(_position + delta);
            var candidateFootBounds = GetFootBounds(candidatePosition);

            if (mapCollisionData.IsWorldRectangleBlocked(candidateFootBounds))
            {
                break;
            }

            _position = candidatePosition;
            remainingDistance -= step;
        }
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

    private Rectangle GetFootBounds(Vector2 basePosition)
    {
        var left = (int)MathF.Round(basePosition.X + _footOffset.X);
        var top = (int)MathF.Round(basePosition.Y + _footOffset.Y);
        return new Rectangle(left, top, _footSize.X, _footSize.Y);
    }
}
