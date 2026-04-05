#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Components;
using RiverRats.Game.Data;
using RiverRats.Game.World;

namespace RiverRats.Game.Entities;

/// <summary>
/// Grandpa NPC that patrols between navigation graph nodes outdoors.
/// Delegates route selection and progress tracking to an <see cref="IndoorNavigator"/>
/// component, and handles movement, collision, and rendering locally.
/// </summary>
public sealed class GrandpaNpc : IWorldProp, IDialogSpeaker
{
    private const float MoveSpeedPixelsPerSecond = 22f;

    private const float FootWidthRatio = 0.6f;
    private const float FootHeightRatio = 0.22f;
    private const float FootInsetFromBottom = 2f;

    /// <summary>Interaction reach in pixels beyond the sprite bounds.</summary>
    private const int InteractionPaddingPixels = 20;

    private static readonly DialogScript[] _dialogPool = new DialogScript[]
    {
        new DialogScript(new DialogLine("Grandpa", "Back in my day, we fished with our bare hands.")),
        new DialogScript(new DialogLine("Grandpa", "Watch the river — it'll teach you patience.")),
        new DialogScript(new DialogLine("Grandpa", "These old bones still have a few adventures left.")),
        new DialogScript(new DialogLine("Grandpa", "I planted that oak tree when I was your age.")),
        new DialogScript(new DialogLine("Grandpa", "If you hear an owl at dusk, it means good luck.")),
    };

    private readonly IndoorNavigator _navigator;
    private readonly Vector2 _size;
    private readonly Point _footSize;
    private readonly Vector2 _footOffset;
    private readonly Vector2 _footCenterOffset;
    private readonly Random _random;

    private Vector2 _position;
    private FacingDirection _facing = FacingDirection.Down;
    private bool _isMoving;

    /// <summary>
    /// Creates a Grandpa NPC that patrols using the supplied navigation graph.
    /// </summary>
    /// <param name="startPosition">Initial top-left world position in pixels.</param>
    /// <param name="size">Sprite size in pixels (should match the sprite frame dimensions).</param>
    /// <param name="navGraph">Navigation graph with at least two nodes.</param>
    /// <param name="pauseDuration">Seconds to pause at each destination before picking a new one.</param>
    /// <param name="random">Random instance for destination selection. Uses a default if null.</param>
    public GrandpaNpc(Vector2 startPosition, Point size, IndoorNavGraph navGraph,
        float pauseDuration = 2.5f, Random? random = null)
    {
        _position = startPosition;
        _size = new Vector2(size.X, size.Y);
        _random = random ?? new Random();

        _footSize = new Point(
            Math.Max(1, (int)MathF.Round(size.X * FootWidthRatio)),
            Math.Max(1, (int)MathF.Round(size.Y * FootHeightRatio)));
        _footOffset = new Vector2(
            (size.X - _footSize.X) / 2f,
            size.Y - _footSize.Y - FootInsetFromBottom);
        _footCenterOffset = _footOffset + new Vector2(_footSize.X / 2f, _footSize.Y / 2f);

        _navigator = new IndoorNavigator(navGraph, startPosition + _footCenterOffset, pauseDuration, _random);
    }

    /// <summary>Current top-left world position in pixels.</summary>
    public Vector2 Position => _position;

    /// <summary>Current world-space center point.</summary>
    public Vector2 Center => _position + (_size * 0.5f);

    /// <summary>Current facing direction.</summary>
    public FacingDirection Facing => _facing;

    /// <summary>Whether the NPC moved this frame.</summary>
    public bool IsMoving => _isMoving;

    /// <summary>Current AABB in world-space pixels.</summary>
    public Rectangle Bounds => new(
        (int)_position.X,
        (int)_position.Y,
        (int)_size.X,
        (int)_size.Y);

    /// <summary>Foot collision rectangle at the current position.</summary>
    public Rectangle FootBounds => GetFootBounds(_position);

    /// <summary>
    /// Advances the patrol AI: move toward the current route node, advance through
    /// the route, pause at the destination, then pick a new one.
    /// </summary>
    public void Update(GameTime gameTime, IMapCollisionData? collisionData = null)
    {
        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        var footCenter = _position + _footCenterOffset;
        _navigator.Update(footCenter, _isMoving, elapsed);

        var target = _navigator.CurrentTargetPosition;
        if (target == null || _navigator.IsPaused)
        {
            _isMoving = false;
            return;
        }

        var adjustedTarget = target.Value - _footCenterOffset;
        var toTarget = adjustedTarget - _position;
        var distanceSq = toTarget.LengthSquared();

        if (distanceSq < 0.001f)
        {
            _isMoving = false;
            return;
        }

        var distance = MathF.Sqrt(distanceSq);
        var direction = toTarget / distance;
        var movementDelta = direction * MoveSpeedPixelsPerSecond * elapsed;

        if (movementDelta.LengthSquared() > distanceSq)
            movementDelta = toTarget;

        UpdateFacing(direction);

        bool moved = false;

        if (movementDelta.X != 0f)
            moved |= TryMoveOnAxis(new Vector2(movementDelta.X, 0f), collisionData);

        if (movementDelta.Y != 0f)
            moved |= TryMoveOnAxis(new Vector2(0f, movementDelta.Y), collisionData);

        _isMoving = moved;
    }

    /// <summary>
    /// Draws the Grandpa NPC via a sprite animator.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, SpriteAnimator animator, Texture2D spriteSheet, float layerDepth = 0f)
    {
        animator.Draw(spriteBatch, spriteSheet, _position, layerDepth);
    }

    /// <inheritdoc />
    void IWorldProp.Draw(SpriteBatch spriteBatch, float layerDepth) =>
        throw new NotSupportedException(
            "GrandpaNpc must be drawn via Draw(SpriteBatch, SpriteAnimator, Texture2D, float).");

    private void UpdateFacing(Vector2 direction)
    {
        if (MathF.Abs(direction.X) >= MathF.Abs(direction.Y))
        {
            _facing = direction.X < 0f ? FacingDirection.Left : FacingDirection.Right;
        }
        else
        {
            _facing = direction.Y < 0f ? FacingDirection.Up : FacingDirection.Down;
        }
    }

    private bool TryMoveOnAxis(Vector2 axisMovement, IMapCollisionData? collisionData)
    {
        var remaining = axisMovement.X != 0f ? axisMovement.X : axisMovement.Y;
        var stepDir = MathF.Sign(remaining);
        var moved = false;

        while (remaining != 0f)
        {
            var mag = MathF.Abs(remaining) >= 1f ? 1f : MathF.Abs(remaining);
            var step = mag * stepDir;
            var delta = axisMovement.X != 0f
                ? new Vector2(step, 0f)
                : new Vector2(0f, step);

            var candidate = _position + delta;
            if (collisionData != null && collisionData.IsWorldRectangleBlocked(GetFootBounds(candidate)))
                break;

            _position = candidate;
            remaining -= step;
            moved = true;
        }

        return moved;
    }

    private Rectangle GetFootBounds(Vector2 basePosition)
    {
        var left = (int)MathF.Round(basePosition.X + _footOffset.X);
        var top = (int)MathF.Round(basePosition.Y + _footOffset.Y);
        return new Rectangle(left, top, _footSize.X, _footSize.Y);
    }

    // ── IDialogSpeaker ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public Rectangle InteractionBounds => new(
        Bounds.X - InteractionPaddingPixels,
        Bounds.Y - InteractionPaddingPixels,
        Bounds.Width + InteractionPaddingPixels * 2,
        Bounds.Height + InteractionPaddingPixels * 2);

    /// <inheritdoc />
    public void FaceToward(Vector2 targetWorldPosition)
    {
        var direction = targetWorldPosition - Center;
        if (direction.LengthSquared() <= 0f)
        {
            return;
        }

        UpdateFacing(direction);
    }

    /// <inheritdoc />
    public DialogScript GetDialog() =>
        _dialogPool[_random.Next(_dialogPool.Length)];
}
