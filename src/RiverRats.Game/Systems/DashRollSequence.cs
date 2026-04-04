#nullable enable

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Components;
using RiverRats.Game.Data;
using RiverRats.Game.Entities;
using RiverRats.Game.World;

namespace RiverRats.Game.Systems;

/// <summary>
/// Forest-only dodge-roll controller for the main player character.
/// Applies a short burst of movement, temporarily grants invulnerability,
/// and exposes a cooldown gauge rendered in world space beneath the player.
/// </summary>
internal sealed class DashRollSequence
{
    private const float DashDurationSeconds = 0.26f;
    private const float DashSpeedPixelsPerSecond = 270f;
    private const float InvulnerabilityLeadSeconds = 0.05f;
    private const float CooldownSecondsValue = 4f;
    private const int RollFrameRow = 5;
    private const int RollFrameCount = 4;
    private const int FrameWidth = 32;
    private const int FrameHeight = 32;
    private const float RollFootAnchorOffsetPixels = 9f;
    private const int GaugeWidthPixels = 18;
    private const int GaugeHeightPixels = 3;
    private const int GaugeGapPixels = 3;

    private static readonly Color GaugeBackground = new(22, 18, 14, 180);
    private static readonly Color GaugeFill = new(255, 214, 96, 235);

    private Vector2 _dashDirection;
    private FacingDirection _dashFacing = FacingDirection.Down;
    private float _dashElapsedSeconds;
    private float _cooldownRemainingSeconds;

    /// <summary>
    /// Multiplied against the base dash cooldown (lower = faster recovery).
    /// </summary>
    internal float CooldownMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Total time, in seconds, required before the next dash can start.
    /// </summary>
    internal const float CooldownSeconds = CooldownSecondsValue;

    /// <summary>
    /// Whether a dash roll is currently active.
    /// </summary>
    internal bool IsActive { get; private set; }

    /// <summary>
    /// Whether the dash can be triggered on the current frame.
    /// </summary>
    internal bool IsReady => !IsActive && _cooldownRemainingSeconds <= 0f;

    /// <summary>
    /// Remaining cooldown in seconds.
    /// </summary>
    internal float CooldownRemainingSeconds => _cooldownRemainingSeconds;

    /// <summary>
    /// Remaining cooldown as a 0–1 fraction. 1 means just used; 0 means ready.
    /// </summary>
    internal float CooldownFraction => CooldownSecondsValue <= 0f
        ? 0f
        : MathHelper.Clamp(_cooldownRemainingSeconds / CooldownSecondsValue, 0f, 1f);

    /// <summary>
    /// Current roll animation frame.
    /// </summary>
    internal int CurrentFrameIndex
    {
        get
        {
            if (!IsActive)
            {
                return 0;
            }

            var progress = MathHelper.Clamp(_dashElapsedSeconds / DashDurationSeconds, 0f, 0.999f);
            return Math.Min(RollFrameCount - 1, (int)(progress * RollFrameCount));
        }
    }

    /// <summary>
    /// Tries to begin a dash in the supplied movement direction.
    /// </summary>
    /// <param name="movementInput">Current player input direction.</param>
    /// <param name="player">Player being controlled.</param>
    /// <param name="health">Player health component used for dash invulnerability.</param>
    internal bool TryBegin(Vector2 movementInput, PlayerBlock player, Health? health)
    {
        if (!IsReady)
        {
            return false;
        }

        if (movementInput == Vector2.Zero)
        {
            return false;
        }

        _dashDirection = Vector2.Normalize(movementInput);
        _dashFacing = ResolveFacing(_dashDirection, player.Facing);
        _dashElapsedSeconds = 0f;
        _cooldownRemainingSeconds = CooldownSecondsValue * Math.Max(0.05f, CooldownMultiplier);
        IsActive = true;

        player.SetFacing(_dashFacing);
        health?.SetInvincibleForDuration(DashDurationSeconds + InvulnerabilityLeadSeconds);
        return true;
    }

    /// <summary>
    /// Advances the dash and cooldown timers, moving the player while the roll is active.
    /// </summary>
    internal void Update(GameTime gameTime, PlayerBlock player, IMapCollisionData collisionMap, Health? health)
    {
        var elapsedSeconds = Math.Max(0f, (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (_cooldownRemainingSeconds > 0f)
        {
            _cooldownRemainingSeconds = Math.Max(0f, _cooldownRemainingSeconds - elapsedSeconds);
        }

        if (!IsActive)
        {
            return;
        }

        _dashElapsedSeconds += elapsedSeconds;
        player.SetFacing(_dashFacing);

        var remainingDashSeconds = Math.Max(0f, DashDurationSeconds - _dashElapsedSeconds);
        health?.SetInvincibleForDuration(remainingDashSeconds + InvulnerabilityLeadSeconds);

        var moved = player.ApplyExternalMovement(
            _dashDirection * DashSpeedPixelsPerSecond * elapsedSeconds,
            collisionMap,
            updateFacingFromMovement: false);

        if (!moved || _dashElapsedSeconds >= DashDurationSeconds)
        {
            IsActive = false;
            player.ClearMovementState();
        }
    }

    /// <summary>
    /// Draws the active roll animation instead of the standard walk sprite.
    /// </summary>
    internal void DrawRoll(SpriteBatch spriteBatch, Texture2D spriteSheet, PlayerBlock player, float layerDepth, Color? tint = null)
    {
        if (!IsActive)
        {
            return;
        }

        var rotation = MathF.Atan2(_dashDirection.Y, _dashDirection.X);
        var sourceRectangle = new Rectangle(
            CurrentFrameIndex * FrameWidth,
            RollFrameRow * FrameHeight,
            FrameWidth,
            FrameHeight);
        var origin = new Vector2(FrameWidth * 0.5f, FrameHeight * 0.5f);
        var footBounds = player.FootBounds;
        var drawCenter = new Vector2(
            player.Bounds.Center.X,
            footBounds.Bottom - RollFootAnchorOffsetPixels);

        spriteBatch.Draw(
            spriteSheet,
            drawCenter,
            sourceRectangle,
            tint ?? Color.White,
            rotation,
            origin,
            1f,
            SpriteEffects.None,
            layerDepth);
    }

    /// <summary>
    /// Draws the shrinking cooldown gauge below the player's feet while cooldown remains.
    /// </summary>
    internal void DrawCooldownGauge(SpriteBatch spriteBatch, Texture2D pixelTexture, PlayerBlock player, float layerDepth)
    {
        if (_cooldownRemainingSeconds <= 0f)
        {
            return;
        }

        var footBounds = player.FootBounds;
        var gaugeX = footBounds.Center.X - (GaugeWidthPixels / 2);
        var gaugeY = footBounds.Bottom + GaugeGapPixels;
        var backgroundRect = new Rectangle(gaugeX, gaugeY, GaugeWidthPixels, GaugeHeightPixels);
        var fillWidth = Math.Max(1, (int)MathF.Round(GaugeWidthPixels * CooldownFraction));
        var fillRect = new Rectangle(gaugeX, gaugeY, fillWidth, GaugeHeightPixels);

        spriteBatch.Draw(pixelTexture, backgroundRect, null, GaugeBackground, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
        spriteBatch.Draw(pixelTexture, fillRect, null, GaugeFill, 0f, Vector2.Zero, SpriteEffects.None, Math.Min(layerDepth + 0.0001f, 0.9999f));
    }

    private static FacingDirection ResolveFacing(Vector2 direction, FacingDirection fallbackFacing)
    {
        if (direction == Vector2.Zero)
        {
            return fallbackFacing;
        }

        return MathF.Abs(direction.X) >= MathF.Abs(direction.Y)
            ? direction.X >= 0f ? FacingDirection.Right : FacingDirection.Left
            : direction.Y >= 0f ? FacingDirection.Down : FacingDirection.Up;
    }
}