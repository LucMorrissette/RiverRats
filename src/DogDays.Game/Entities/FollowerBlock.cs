#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DogDays.Game.Components;
using DogDays.Game.Data;

namespace DogDays.Game.Entities;

/// <summary>
/// Follower entity that trails behind the leader along a recorded breadcrumb path.
/// </summary>
public sealed class FollowerBlock : IWorldProp
{
    private readonly Vector2 _size;
    private readonly Rectangle _worldBounds;
    private readonly FollowerMovementConfig _movementConfig;
    private readonly List<Vector2> _trailPoints = new();

    private Vector2 _position;
    private Vector2 _leaderPosition;
    private Vector2 _restAnchorLeaderPosition;
    private float _currentFollowDistancePixels;
    private FacingDirection _facing = FacingDirection.Down;
    private Vector2? _activeRestPosition;
    private bool _isMoving;
    private bool _isTrailInitialized;

    /// <summary>
    /// Initializes a follower block with breadcrumb trail movement.
    /// </summary>
    /// <param name="startPosition">Initial top-left world position in pixels.</param>
    /// <param name="size">Block size in pixels.</param>
    /// <param name="worldBounds">World bounds in pixels used for position clamping.</param>
    /// <param name="movementConfig">Follower trail tuning parameters.</param>
    public FollowerBlock(
        Vector2 startPosition,
        Point size,
        Rectangle worldBounds,
        FollowerMovementConfig? movementConfig = null)
    {
        _position = startPosition;
        _size = new Vector2(size.X, size.Y);
        _worldBounds = worldBounds;
        _movementConfig = movementConfig ?? FollowerMovementConfig.Default;
        _currentFollowDistancePixels = _movementConfig.FollowDistancePixels;

        ClampToBounds();
        _leaderPosition = _position;
        _trailPoints.Add(_position);
    }

    /// <summary>Current top-left world position in pixels.</summary>
    public Vector2 Position => _position;

    /// <summary>Current world-space center point used for rendering and spatial logic.</summary>
    public Vector2 Center => _position + (_size * 0.5f);

    /// <summary>Current facing direction.</summary>
    public FacingDirection Facing => _facing;

    /// <summary>Whether the follower advanced along the trail this frame.</summary>
    public bool IsMoving => _isMoving;

    /// <summary>
    /// Directly sets the follower's world position. Used by scripted sequences
    /// (e.g., couch sitting) that bypass normal trail following.
    /// </summary>
    internal void SetPosition(Vector2 position)
    {
        _position = position;
    }

    /// <summary>
    /// Sets the follower's facing direction without requiring movement.
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

    /// <summary>
    /// Updates follower to remain a fixed distance behind the leader on the leader's recent path.
    /// </summary>
    /// <param name="leaderPosition">Leader's top-left world position in pixels.</param>
    /// <param name="leaderFacing">Leader's facing direction.</param>
    /// <param name="restPosition">Optional idle rest position beside the leader.</param>
    public void Update(GameTime gameTime, Vector2 leaderPosition, FacingDirection leaderFacing, Vector2? restPosition = null)
    {
        var clampedLeaderPosition = ClampPosition(leaderPosition);
        var leaderMoved = _isTrailInitialized && Vector2.DistanceSquared(clampedLeaderPosition, _leaderPosition) > 0f;

        if (!_isTrailInitialized)
        {
            _leaderPosition = clampedLeaderPosition;
            _isTrailInitialized = true;

            if (_trailPoints.Count == 1)
            {
                _trailPoints.Add(_leaderPosition);
            }
        }
        else
        {
            RecordLeaderTrail(clampedLeaderPosition);
        }

        var elapsedSeconds = Math.Max(0f, (float)gameTime.ElapsedGameTime.TotalSeconds);
        var desiredFollowDistance = leaderMoved
            ? _movementConfig.FollowDistancePixels
            : Math.Min(_movementConfig.FollowDistancePixels, _movementConfig.IdleFollowDistancePixels);
        _currentFollowDistancePixels = EaseScalar(
            _currentFollowDistancePixels,
            desiredFollowDistance,
            _movementConfig.DistanceEasePerSecond,
            elapsedSeconds);

        TrimTrail();

        var previousPosition = _position;
        var trailingPosition = GetTrailingPosition(_currentFollowDistancePixels);
        var targetPosition = trailingPosition;
        var targetEasePerSecond = _movementConfig.PositionEasePerSecond;

        if (restPosition.HasValue && !leaderMoved)
        {
            _activeRestPosition = ClampPosition(restPosition.Value);
            _restAnchorLeaderPosition = clampedLeaderPosition;
            targetPosition = _activeRestPosition.Value;
            targetEasePerSecond = _movementConfig.RestPositionEasePerSecond;
        }
        else if (_activeRestPosition.HasValue)
        {
            var restExitDistanceSquared = _movementConfig.RestExitDistancePixels * _movementConfig.RestExitDistancePixels;
            if (Vector2.DistanceSquared(clampedLeaderPosition, _restAnchorLeaderPosition) < restExitDistanceSquared)
            {
                targetPosition = _activeRestPosition.Value;
                targetEasePerSecond = _movementConfig.RestPositionEasePerSecond;
            }
            else
            {
                _activeRestPosition = null;
            }
        }

        var easedPosition = EaseVector(_position, targetPosition, targetEasePerSecond, elapsedSeconds);

        if (Vector2.DistanceSquared(easedPosition, targetPosition)
            <= _movementConfig.PositionSnapDistancePixels * _movementConfig.PositionSnapDistancePixels)
        {
            easedPosition = targetPosition;
        }

        _position = ClampPosition(easedPosition);

        var movementDelta = _position - previousPosition;
        _isMoving = movementDelta.LengthSquared()
            > _movementConfig.AnimationMoveThresholdPixels * _movementConfig.AnimationMoveThresholdPixels;

        if (movementDelta.LengthSquared() > _movementConfig.FacingDeadZonePixels * _movementConfig.FacingDeadZonePixels)
        {
            UpdateFacing(movementDelta);
        }
        else if (!_isMoving)
        {
            _facing = leaderFacing;
        }
    }

    /// <summary>
    /// Draws the follower via a sprite animator.
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
    /// Not supported — the follower must be drawn via <see cref="Draw(SpriteBatch,SpriteAnimator,Texture2D,float,Color?)"/>,
    /// which requires an animator and sprite sheet.
    /// </summary>
    void IWorldProp.Draw(SpriteBatch spriteBatch, float layerDepth) =>
        throw new NotSupportedException("FollowerBlock must be drawn via Draw(SpriteBatch, SpriteAnimator, Texture2D, float, Color?).");

    private void ClampToBounds()
    {
        _position = ClampPosition(_position);
    }

    private void RecordLeaderTrail(Vector2 nextLeaderPosition)
    {
        var segmentStart = _leaderPosition;
        var segment = nextLeaderPosition - segmentStart;
        var distance = segment.Length();

        if (distance <= 0f)
        {
            return;
        }

        var stepDistance = Math.Max(1f, _movementConfig.TrailSampleDistancePixels);
        var direction = segment / distance;

        while (distance >= stepDistance)
        {
            segmentStart += direction * stepDistance;
            _trailPoints.Add(segmentStart);
            distance = Vector2.Distance(segmentStart, nextLeaderPosition);
        }

        _leaderPosition = nextLeaderPosition;

        if (_trailPoints.Count == 0 || _trailPoints[^1] != _leaderPosition)
        {
            _trailPoints.Add(_leaderPosition);
        }
    }

    private Vector2 GetTrailingPosition(float trailingDistancePixels)
    {
        if (_trailPoints.Count == 0)
        {
            return _position;
        }

        var remainingDistance = Math.Max(0f, trailingDistancePixels);
        var segmentEnd = _leaderPosition;

        for (var i = _trailPoints.Count - 1; i >= 0; i--)
        {
            var segmentStart = _trailPoints[i];
            var segmentLength = Vector2.Distance(segmentStart, segmentEnd);

            if (segmentLength >= remainingDistance)
            {
                if (segmentLength <= 0f)
                {
                    return segmentStart;
                }

                var interpolation = remainingDistance / segmentLength;
                return Vector2.Lerp(segmentEnd, segmentStart, interpolation);
            }

            remainingDistance -= segmentLength;
            segmentEnd = segmentStart;
        }

        return _trailPoints[0];
    }

    private void TrimTrail()
    {
        var maxTrailDistance = Math.Max(_movementConfig.FollowDistancePixels, _movementConfig.IdleFollowDistancePixels)
            + Math.Max(1f, _movementConfig.TrailSampleDistancePixels) * 3f;

        while (_trailPoints.Count > 1 && GetTrailLength() > maxTrailDistance)
        {
            _trailPoints.RemoveAt(0);
        }
    }

    private float GetTrailLength()
    {
        if (_trailPoints.Count == 0)
        {
            return 0f;
        }

        var totalDistance = 0f;
        var segmentEnd = _leaderPosition;

        for (var i = _trailPoints.Count - 1; i >= 0; i--)
        {
            totalDistance += Vector2.Distance(_trailPoints[i], segmentEnd);
            segmentEnd = _trailPoints[i];
        }

        return totalDistance;
    }

    private static Vector2 EaseVector(Vector2 current, Vector2 target, float easePerSecond, float elapsedSeconds)
    {
        if (elapsedSeconds <= 0f || easePerSecond <= 0f)
        {
            return target;
        }

        var interpolation = 1f - MathF.Exp(-easePerSecond * elapsedSeconds);
        return Vector2.Lerp(current, target, interpolation);
    }

    private static float EaseScalar(float current, float target, float easePerSecond, float elapsedSeconds)
    {
        if (elapsedSeconds <= 0f || easePerSecond <= 0f)
        {
            return target;
        }

        var interpolation = 1f - MathF.Exp(-easePerSecond * elapsedSeconds);
        return MathHelper.Lerp(current, target, interpolation);
    }

    private void UpdateFacing(Vector2 movementDelta)
    {
        if (MathF.Abs(movementDelta.X) >= MathF.Abs(movementDelta.Y))
        {
            _facing = movementDelta.X >= 0f ? FacingDirection.Right : FacingDirection.Left;
        }
        else
        {
            _facing = movementDelta.Y >= 0f ? FacingDirection.Down : FacingDirection.Up;
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
}
