using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DogDays.Game.Data;
using DogDays.Game.Entities;

namespace DogDays.Game.Systems;

/// <summary>
/// Plays a short authored collapse animation for the player before the death screen appears.
/// </summary>
internal sealed class PlayerCollapseSequence
{
    private const int FrameWidth = 32;
    private const int FrameHeight = 32;
    private const float DurationSecondsValue = 0.85f;
    private const int CollapseFrameCount = 4;
    private const int CollapseRowStart = 6;
    private const float Frame0EndProgress = 0.22f;
    private const float Frame1EndProgress = 0.47f;
    private const float Frame2EndProgress = 0.65f;

    private Vector2 _position;
    private FacingDirection _facing = FacingDirection.Down;
    private float _elapsedSeconds;

    /// <summary>
    /// Total collapse duration before the death screen transition.
    /// </summary>
    internal const float DurationSeconds = DurationSecondsValue;

    /// <summary>
    /// Whether the collapse sequence is currently active.
    /// </summary>
    internal bool IsActive { get; private set; }

    /// <summary>
    /// Current progress in the collapse animation as a 0-1 value.
    /// </summary>
    internal float Progress => DurationSecondsValue <= 0f
        ? 1f
        : MathHelper.Clamp(_elapsedSeconds / DurationSecondsValue, 0f, 1f);

    /// <summary>
    /// Current authored collapse frame index.
    /// </summary>
    internal int CurrentFrameIndex
    {
        get
        {
            var progress = Progress;
            if (progress < Frame0EndProgress)
            {
                return 0;
            }

            if (progress < Frame1EndProgress)
            {
                return 1;
            }

            if (progress < Frame2EndProgress)
            {
                return 2;
            }

            return CollapseFrameCount - 1;
        }
    }

    /// <summary>
    /// Current source rectangle for the authored collapse strip.
    /// </summary>
    internal Rectangle CurrentSourceRectangle => new(
        CurrentFrameIndex * FrameWidth,
        (CollapseRowStart + (int)_facing) * FrameHeight,
        FrameWidth,
        FrameHeight);

    /// <summary>
    /// Starts the collapse sequence from the player's current position and facing.
    /// </summary>
    internal void Begin(PlayerBlock player)
    {
        _position = player.Position;
        _facing = player.Facing;
        _elapsedSeconds = 0f;
        IsActive = true;
        player.ClearMovementState();
    }

    /// <summary>
    /// Advances the collapse timer and keeps the player locked in place.
    /// </summary>
    internal void Update(GameTime gameTime, PlayerBlock player)
    {
        if (!IsActive)
        {
            return;
        }

        _elapsedSeconds = Math.Min(
            DurationSecondsValue,
            _elapsedSeconds + Math.Max(0f, (float)gameTime.ElapsedGameTime.TotalSeconds));

        player.SetPosition(_position);
        player.SetFacing(_facing);
        player.ClearMovementState();

        if (_elapsedSeconds >= DurationSecondsValue)
        {
            IsActive = false;
        }
    }

    /// <summary>
    /// Draws the collapsing player using the authored collapse frames for the locked facing direction.
    /// </summary>
    internal void Draw(SpriteBatch spriteBatch, Texture2D spriteSheet, PlayerBlock player, float layerDepth, Color? tint = null)
    {
        if (!IsActive)
        {
            return;
        }

        spriteBatch.Draw(
            spriteSheet,
            _position,
            CurrentSourceRectangle,
            tint ?? Color.White,
            0f,
            Vector2.Zero,
            1f,
            SpriteEffects.None,
            layerDepth);
    }
}