#nullable enable

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Data;
using RiverRats.Game.Entities;
using RiverRats.Game.Input;

namespace RiverRats.Game.Systems;

/// <summary>
/// Manages a scripted sequence where the player and follower hop into a watercraft,
/// paddle it while seated, and later hop back out when a valid landing spot is available.
/// </summary>
public sealed class WatercraftBoardSequence
{
    private const float HopDurationSeconds = 0.35f;
    private const float HopArcHeightPixels = 10f;
    private const float FollowerHopDelaySeconds = 0.12f;
    private const float MoveSpeedPixelsPerSecond = 36f;
    private const float GlideDecayRate = 2.1f;
    private const float GlideStopThreshold = 1f;
    private const int SittingFrameRow = 4;
    private const float StandOffsetPixels = 12f;

    private readonly int _frameWidth;
    private readonly int _frameHeight;

    private WatercraftBoardState _state = WatercraftBoardState.Idle;
    private Watercraft _targetWatercraft = null!;

    private float _hopTimer;
    private float _followerHopTimer;
    private FacingDirection _travelFacing;
    private Vector2 _playerStartPosition;
    private Vector2 _followerStartPosition;
    private Vector2 _playerSeatPosition;
    private Vector2 _followerSeatPosition;
    private Vector2 _playerStandPosition;
    private Vector2 _followerStandPosition;
    private Vector2 _playerRenderPosition;
    private Vector2 _followerRenderPosition;
    private Vector2 _glideVelocity;

    /// <summary>
    /// Creates a boarding hop controller.
    /// </summary>
    /// <param name="frameWidth">Actor sprite width in pixels.</param>
    /// <param name="frameHeight">Actor sprite height in pixels.</param>
    public WatercraftBoardSequence(int frameWidth, int frameHeight)
    {
        _frameWidth = frameWidth;
        _frameHeight = frameHeight;
    }

    /// <summary>Whether a watercraft sequence is active (hopping, seated, or hopping off).</summary>
    public bool IsActive => _state != WatercraftBoardState.Idle;

    /// <summary>Whether both characters are fully seated in the watercraft.</summary>
    public bool IsSeated => _state == WatercraftBoardState.Seated;

    /// <summary>Current watercraft sequence state.</summary>
    public WatercraftBoardState State => _state;

    /// <summary>Currently boarded watercraft when the sequence is active.</summary>
    public Watercraft? ActiveWatercraft => IsActive ? _targetWatercraft : null;

    /// <summary>
    /// Returns <c>true</c> when the party is seated and at least one disembark
    /// direction has clear ground for both actors. Read-only — does not mutate state.
    /// </summary>
    public bool CanDisembark(Func<Rectangle, bool> canDisembarkToBounds)
    {
        if (!IsSeated || _targetWatercraft is null)
            return false;

        var directionCandidates = new[]
        {
            _targetWatercraft.Facing,
            FacingDirection.Down,
            FacingDirection.Up,
            _targetWatercraft.Facing == FacingDirection.Left ? FacingDirection.Right : FacingDirection.Left,
        };

        for (var i = 0; i < directionCandidates.Length; i++)
        {
            var direction = directionCandidates[i];
            var playerStand = ComputeStandPosition(_playerSeatPosition, _targetWatercraft.Bounds, direction);
            var followerStand = ComputeStandPosition(_followerSeatPosition, _targetWatercraft.Bounds, direction);

            if (canDisembarkToBounds(ToActorBounds(playerStand))
                && canDisembarkToBounds(ToActorBounds(followerStand)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Raised once when both actors finish hopping into the craft and are fully seated.
    /// </summary>
    public event Action<Watercraft>? Mounted;

    /// <summary>Current movement speed in pixels per second (includes glide). Zero when stationary or idle.</summary>
    public float CurrentSpeed => IsActive ? _glideVelocity.Length() : 0f;

    /// <summary>
    /// Signed speed along the craft's facing axis in pixels per second.
    /// Positive values indicate forward travel; negative values indicate reverse travel.
    /// </summary>
    public float SignedForwardSpeed
    {
        get
        {
            if (!IsActive)
            {
                return 0f;
            }

            var forward = GetFacingVector(_targetWatercraft.Facing);
            return Vector2.Dot(_glideVelocity, forward);
        }
    }

    /// <summary>
    /// Begins the watercraft sequence: both characters hop into the target craft.
    /// </summary>
    public void Begin(Watercraft watercraft, PlayerBlock player, FollowerBlock follower)
    {
        _targetWatercraft = watercraft;
        _state = WatercraftBoardState.HoppingToSeat;
        _travelFacing = watercraft.Facing;

        _playerStartPosition = player.Position;
        _followerStartPosition = follower.Position;
        UpdateSeatTargets();

        _hopTimer = 0f;
        _followerHopTimer = -FollowerHopDelaySeconds;
        _glideVelocity = Vector2.Zero;
        _playerRenderPosition = _playerStartPosition;
        _followerRenderPosition = _followerStartPosition;

        player.ClearMovementState();
        player.SetFacing(_travelFacing);
        player.SetPosition(_playerStartPosition);
        follower.SetFacing(_travelFacing);
        follower.SetPosition(_followerStartPosition);
    }

    /// <summary>
    /// Restores a seated state after loading a save or rebuilding the screen.
    /// </summary>
    public void RestoreSeated(Watercraft watercraft, PlayerBlock player, FollowerBlock follower)
    {
        _targetWatercraft = watercraft;
        _state = WatercraftBoardState.Seated;
        _glideVelocity = Vector2.Zero;
        _travelFacing = watercraft.Facing;
        UpdateSeatTargets();
        _playerRenderPosition = _playerSeatPosition;
        _followerRenderPosition = _followerSeatPosition;
        UpdateSeatedActors(player, follower);
    }

    /// <summary>
    /// Updates the watercraft sequence state machine.
    /// </summary>
    public void Update(
        GameTime gameTime,
        IInputManager input,
        PlayerBlock player,
        FollowerBlock follower,
        Func<Rectangle, bool> canMoveToBounds,
        Func<Rectangle, bool> canDisembarkToBounds)
    {
        if (_state == WatercraftBoardState.Idle)
        {
            return;
        }

        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (_state)
        {
            case WatercraftBoardState.HoppingToSeat:
                UpdateHopToSeat(elapsed, player, follower);
                break;

            case WatercraftBoardState.Seated:
                TryMoveWhileSeated(elapsed, input, player, follower, canMoveToBounds);
                UpdateSeatedActors(player, follower);
                if ((input.IsPressed(InputAction.Confirm) || input.IsPressed(InputAction.Cancel))
                    && TryBeginHopOff(canDisembarkToBounds))
                {
                    return;
                }
                break;

            case WatercraftBoardState.HoppingOff:
                UpdateHopOff(elapsed, player, follower);
                break;
        }
    }

    /// <summary>
    /// Draws the watercraft plus both occupants for the active scripted sequence.
    /// </summary>
    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D playerSheet,
        Texture2D followerSheet,
        float mapPixelHeight,
        float mapPixelWidth)
    {
        if (_state == WatercraftBoardState.Idle)
        {
            return;
        }

        var watercraftBounds = _targetWatercraft.Bounds;
        var baseDepth = Math.Min((watercraftBounds.Bottom + 1f) / mapPixelHeight, 0.999f);
        var playerDepth = Math.Min(baseDepth + (_playerRenderPosition.Y <= _followerRenderPosition.Y ? 0.0001f : 0.0002f), 0.9999f);
        var followerDepth = Math.Min(baseDepth + (_followerRenderPosition.Y <= _playerRenderPosition.Y ? 0.0001f : 0.0002f), 0.9999f);
        var watercraftDepth = Math.Min(baseDepth + (_targetWatercraft.IsSideView ? 0.0003f : 0f), 0.9999f);

        var isSitting = _state == WatercraftBoardState.Seated;
        var playerSource = isSitting ? GetSittingSourceRect(_travelFacing) : GetIdleSourceRect(_travelFacing);
        var followerSource = isSitting ? GetSittingSourceRect(_travelFacing) : GetIdleSourceRect(_travelFacing);

        if (!_targetWatercraft.IsSideView)
        {
            _targetWatercraft.Draw(spriteBatch, watercraftDepth);
        }

        spriteBatch.Draw(followerSheet, _followerRenderPosition, followerSource,
            Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, followerDepth);
        spriteBatch.Draw(playerSheet, _playerRenderPosition, playerSource,
            Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, playerDepth);

        if (_targetWatercraft.IsSideView)
        {
            _targetWatercraft.Draw(spriteBatch, watercraftDepth);
        }
    }

    private void UpdateHopToSeat(float elapsed, PlayerBlock player, FollowerBlock follower)
    {
        _hopTimer += elapsed;
        _followerHopTimer += elapsed;

        var playerT = Math.Clamp(_hopTimer / HopDurationSeconds, 0f, 1f);
        var followerT = Math.Clamp(Math.Max(0f, _followerHopTimer) / HopDurationSeconds, 0f, 1f);

        _playerRenderPosition = ComputeHopPosition(_playerStartPosition, _playerSeatPosition, playerT);
        _followerRenderPosition = ComputeHopPosition(_followerStartPosition, _followerSeatPosition, followerT);

        if (playerT >= 1f && followerT >= 1f)
        {
            _state = WatercraftBoardState.Seated;
            _playerRenderPosition = _playerSeatPosition;
            _followerRenderPosition = _followerSeatPosition;
            UpdateSeatedActors(player, follower);
            Mounted?.Invoke(_targetWatercraft);
        }
    }

    private void BeginHopOff()
    {
        _state = WatercraftBoardState.HoppingOff;
        _playerStartPosition = _playerSeatPosition;
        _followerStartPosition = _followerSeatPosition;
        _hopTimer = 0f;
        _followerHopTimer = -FollowerHopDelaySeconds;
    }

    private void UpdateHopOff(float elapsed, PlayerBlock player, FollowerBlock follower)
    {
        _hopTimer += elapsed;
        _followerHopTimer += elapsed;

        var playerT = Math.Clamp(_hopTimer / HopDurationSeconds, 0f, 1f);
        var followerT = Math.Clamp(Math.Max(0f, _followerHopTimer) / HopDurationSeconds, 0f, 1f);

        _playerRenderPosition = ComputeHopPosition(_playerSeatPosition, _playerStandPosition, playerT);
        _followerRenderPosition = ComputeHopPosition(_followerSeatPosition, _followerStandPosition, followerT);

        if (playerT >= 1f && followerT >= 1f)
        {
            _state = WatercraftBoardState.Idle;
            _targetWatercraft = null!;
            player.SetPosition(_playerStandPosition);
            player.SetFacing(_travelFacing);
            player.ClearMovementState();
            follower.SetPosition(_followerStandPosition);
            follower.SetFacing(_travelFacing);
        }
    }

    private void TryMoveWhileSeated(
        float elapsed,
        IInputManager input,
        PlayerBlock player,
        FollowerBlock follower,
        Func<Rectangle, bool> canMoveToBounds)
    {
        var movementDirection = GetRawMovementInput(input);
        var currentCenter = _targetWatercraft.Center;
        var currentFacing = _targetWatercraft.Facing;

        // --- Glide when no input ---
        if (movementDirection == Vector2.Zero)
        {
            if (_glideVelocity.LengthSquared() > GlideStopThreshold * GlideStopThreshold)
            {
                ApplyGlide(elapsed, currentFacing, canMoveToBounds, player, follower);
            }
            else
            {
                _glideVelocity = Vector2.Zero;
            }

            return;
        }

        var desiredFacing = GetFacingForMovementInput(movementDirection, currentFacing);
        if (desiredFacing != currentFacing)
        {
            var pivotedCenter = _targetWatercraft.GetPivotedCenterForTurn(desiredFacing, _frameWidth, _frameHeight);
            var pivotedBounds = _targetWatercraft.GetBoundsForState(pivotedCenter, desiredFacing);
            if (canMoveToBounds(pivotedBounds))
            {
                _targetWatercraft.SetState(pivotedCenter, desiredFacing);
                currentCenter = pivotedCenter;
                currentFacing = desiredFacing;
            }
        }

        var normalizedDirection = Vector2.Normalize(movementDirection);
        var velocity = normalizedDirection * MoveSpeedPixelsPerSecond;
        var candidateCenter = currentCenter + (velocity * elapsed);
        var candidateBounds = _targetWatercraft.GetBoundsForState(candidateCenter, currentFacing);

        if (canMoveToBounds(candidateBounds))
        {
            _targetWatercraft.SetState(candidateCenter, currentFacing);
            _glideVelocity = velocity;
        }
        else
        {
            _glideVelocity = Vector2.Zero;
        }

        _travelFacing = _targetWatercraft.Facing;
        UpdateSeatTargets();
        _playerRenderPosition = _playerSeatPosition;
        _followerRenderPosition = _followerSeatPosition;
        UpdateSeatedActors(player, follower);
    }

    private void ApplyGlide(
        float elapsed,
        FacingDirection currentFacing,
        Func<Rectangle, bool> canMoveToBounds,
        PlayerBlock player,
        FollowerBlock follower)
    {
        _glideVelocity *= MathF.Exp(-GlideDecayRate * elapsed);

        var currentCenter = _targetWatercraft.Center;
        var candidateCenter = currentCenter + (_glideVelocity * elapsed);
        var candidateBounds = _targetWatercraft.GetBoundsForState(candidateCenter, currentFacing);

        if (canMoveToBounds(candidateBounds))
        {
            _targetWatercraft.SetState(candidateCenter, currentFacing);
        }
        else
        {
            _glideVelocity = Vector2.Zero;
        }

        UpdateSeatTargets();
        _playerRenderPosition = _playerSeatPosition;
        _followerRenderPosition = _followerSeatPosition;
        UpdateSeatedActors(player, follower);
    }

    private bool TryBeginHopOff(Func<Rectangle, bool> canDisembarkToBounds)
    {
        var directionCandidates = new[]
        {
            _targetWatercraft.Facing,
            FacingDirection.Down,
            FacingDirection.Up,
            _targetWatercraft.Facing == FacingDirection.Left ? FacingDirection.Right : FacingDirection.Left,
        };

        for (var i = 0; i < 4; i++)
        {
            var direction = directionCandidates[i];
            var playerStand = ComputeStandPosition(_playerSeatPosition, _targetWatercraft.Bounds, direction);
            var followerStand = ComputeStandPosition(_followerSeatPosition, _targetWatercraft.Bounds, direction);

            if (!canDisembarkToBounds(ToActorBounds(playerStand)) || !canDisembarkToBounds(ToActorBounds(followerStand)))
            {
                continue;
            }

            _travelFacing = direction;
            _playerStandPosition = playerStand;
            _followerStandPosition = followerStand;
            BeginHopOff();
            return true;
        }

        return false;
    }

    private Rectangle ToActorBounds(Vector2 position)
    {
        return new Rectangle(
            (int)MathF.Round(position.X),
            (int)MathF.Round(position.Y),
            _frameWidth,
            _frameHeight);
    }

    private void UpdateSeatTargets()
    {
        _playerSeatPosition = _targetWatercraft.GetRearSeatPosition(_frameWidth, _frameHeight);
        _followerSeatPosition = _targetWatercraft.GetFrontSeatPosition(_frameWidth, _frameHeight);
    }

    private void UpdateSeatedActors(PlayerBlock player, FollowerBlock follower)
    {
        player.SetPosition(_playerSeatPosition);
        player.SetFacing(_travelFacing);
        player.ClearMovementState();
        follower.SetPosition(_followerSeatPosition);
        follower.SetFacing(_travelFacing);
    }

    private static Vector2 GetRawMovementInput(IInputManager input)
    {
        var horizontal = 0;
        var vertical = 0;

        if (input.IsHeld(InputAction.MoveLeft))
        {
            horizontal -= 1;
        }

        if (input.IsHeld(InputAction.MoveRight))
        {
            horizontal += 1;
        }

        if (input.IsHeld(InputAction.MoveUp))
        {
            vertical -= 1;
        }

        if (input.IsHeld(InputAction.MoveDown))
        {
            vertical += 1;
        }

        return new Vector2(horizontal, vertical);
    }

    private static Vector2 ComputeHopPosition(Vector2 start, Vector2 end, float progress)
    {
        var eased = SmoothStep(progress);
        var basePosition = Vector2.Lerp(start, end, eased);
        var arc = 4f * progress * (1f - progress) * HopArcHeightPixels;
        return new Vector2(basePosition.X, basePosition.Y - arc);
    }

    private static float SmoothStep(float value)
    {
        return value * value * (3f - 2f * value);
    }

    private static Vector2 GetFacingVector(FacingDirection facing)
    {
        return facing switch
        {
            FacingDirection.Left => new Vector2(-1f, 0f),
            FacingDirection.Right => new Vector2(1f, 0f),
            FacingDirection.Up => new Vector2(0f, -1f),
            _ => new Vector2(0f, 1f),
        };
    }

    private static FacingDirection GetFacingForMovementInput(Vector2 movementDirection, FacingDirection currentFacing)
    {
        var absX = Math.Abs(movementDirection.X);
        var absY = Math.Abs(movementDirection.Y);

        if (absX > absY)
        {
            return movementDirection.X >= 0f ? FacingDirection.Right : FacingDirection.Left;
        }

        if (absY > absX)
        {
            return movementDirection.Y >= 0f ? FacingDirection.Down : FacingDirection.Up;
        }

        if (currentFacing == FacingDirection.Left || currentFacing == FacingDirection.Right)
        {
            return movementDirection.X >= 0f ? FacingDirection.Right : FacingDirection.Left;
        }

        return movementDirection.Y >= 0f ? FacingDirection.Down : FacingDirection.Up;
    }

    private Vector2 ComputeStandPosition(Vector2 seatPosition, Rectangle watercraftBounds, FacingDirection exitFacing)
    {
        return exitFacing switch
        {
            FacingDirection.Left => new Vector2(watercraftBounds.Right + StandOffsetPixels, seatPosition.Y),
            FacingDirection.Right => new Vector2(watercraftBounds.Left - _frameWidth - StandOffsetPixels, seatPosition.Y),
            FacingDirection.Up => new Vector2(seatPosition.X, watercraftBounds.Bottom + StandOffsetPixels),
            _ => new Vector2(seatPosition.X, watercraftBounds.Top - _frameHeight - StandOffsetPixels),
        };
    }

    private Rectangle GetSittingSourceRect(FacingDirection facing)
    {
        var column = (int)facing;
        return new Rectangle(
            column * _frameWidth,
            SittingFrameRow * _frameHeight,
            _frameWidth,
            _frameHeight);
    }

    private Rectangle GetIdleSourceRect(FacingDirection facing)
    {
        var row = (int)facing;
        return new Rectangle(0, row * _frameHeight, _frameWidth, _frameHeight);
    }
}

/// <summary>
/// States for the watercraft boarding sequence.
/// </summary>
public enum WatercraftBoardState
{
    /// <summary>No sequence active.</summary>
    Idle,

    /// <summary>Characters are hopping into the watercraft.</summary>
    HoppingToSeat,

    /// <summary>Characters are seated in the watercraft.</summary>
    Seated,

    /// <summary>Characters are hopping back out of the watercraft.</summary>
    HoppingOff,
}