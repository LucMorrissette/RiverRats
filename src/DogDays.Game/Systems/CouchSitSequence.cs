using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DogDays.Game.Data;
using DogDays.Game.Entities;
using DogDays.Game.Input;

namespace DogDays.Game.Systems;

/// <summary>
/// Manages a scripted sequence where the player and follower hop onto a couch,
/// sit down, and later hop off when the action button is pressed again.
/// </summary>
public sealed class CouchSitSequence
{
    /// <summary>Duration of the hop-on/hop-off tween in seconds.</summary>
    private const float HopDurationSeconds = 0.35f;

    /// <summary>Peak height of the hop arc in pixels.</summary>
    private const float HopArcHeightPixels = 10f;

    /// <summary>Delay between the player's hop and the follower's hop start.</summary>
    private const float FollowerHopDelaySeconds = 0.12f;

    /// <summary>
    /// Row index on the character sprite sheet for the sitting frame.
    /// Row 4 = sitting, column 0.
    /// </summary>
    private const int SittingFrameRow = 4;

    /// <summary>Standing position offset from the couch when hopping off (pixels in front of the couch).</summary>
    private const float StandOffsetPixels = 12f;

    /// <summary>
    /// Old couch sprites open toward the left side, so seated characters always
    /// use the left-facing frame regardless of approach direction.
    /// </summary>
    private const FacingDirection CouchFacingDirection = FacingDirection.Left;

    private readonly int _frameWidth;
    private readonly int _frameHeight;

    private CouchSitState _state = CouchSitState.Idle;
    private Couch _targetCouch;

    // Direction characters return to after standing up.
    private FacingDirection _exitFacing;

    // Hop animation state
    private float _playerHopTimer;
    private float _followerHopTimer;
    private Vector2 _playerStartPosition;
    private Vector2 _followerStartPosition;
    private Vector2 _playerSeatPosition;
    private Vector2 _followerSeatPosition;
    private Vector2 _playerStandPosition;
    private Vector2 _followerStandPosition;

    // Current render positions (interpolated during hop)
    private Vector2 _playerRenderPosition;
    private Vector2 _followerRenderPosition;

    /// <summary>
    /// Creates a couch sit sequence controller.
    /// </summary>
    /// <param name="frameWidth">Width of a single sprite frame in pixels.</param>
    /// <param name="frameHeight">Height of a single sprite frame in pixels.</param>
    public CouchSitSequence(int frameWidth, int frameHeight)
    {
        _frameWidth = frameWidth;
        _frameHeight = frameHeight;
    }

    /// <summary>Whether a sit sequence is currently active (characters sitting or transitioning).</summary>
    public bool IsActive => _state != CouchSitState.Idle;

    /// <summary>Whether the characters are fully seated and waiting for dismissal.</summary>
    public bool IsSeated => _state == CouchSitState.Seated;

    /// <summary>Current state of the sit sequence.</summary>
    public CouchSitState State => _state;

    /// <summary>
    /// Begins the sit sequence: player and follower hop onto the target couch.
    /// </summary>
    /// <param name="couch">The couch to sit on.</param>
    /// <param name="player">The player entity.</param>
    /// <param name="follower">The follower entity.</param>
    public void Begin(Couch couch, PlayerBlock player, FollowerBlock follower)
    {
        _targetCouch = couch;
        _state = CouchSitState.HoppingToSeat;
        _exitFacing = player.Facing;

        _playerStartPosition = player.Position;
        _followerStartPosition = follower.Position;

        // Assign seats: player gets the seat closer to their current position.
        var distToA = Vector2.DistanceSquared(player.Position, couch.SeatPositionA);
        var distToB = Vector2.DistanceSquared(player.Position, couch.SeatPositionB);

        if (distToA <= distToB)
        {
            _playerSeatPosition = couch.SeatPositionA;
            _followerSeatPosition = couch.SeatPositionB;
        }
        else
        {
            _playerSeatPosition = couch.SeatPositionB;
            _followerSeatPosition = couch.SeatPositionA;
        }

        // Hop-off positions are always on the open side of this couch, with extra
        // clearance so the characters do not clip back into the prop collision.
        _playerStandPosition = ComputeStandPosition(_playerSeatPosition, couch.Bounds);
        _followerStandPosition = ComputeStandPosition(_followerSeatPosition, couch.Bounds);

        _playerHopTimer = 0f;
        _followerHopTimer = -FollowerHopDelaySeconds; // Negative = delayed start

        _playerRenderPosition = _playerStartPosition;
        _followerRenderPosition = _followerStartPosition;
    }

    /// <summary>
    /// Updates the sit sequence state machine.
    /// </summary>
    /// <param name="gameTime">Frame timing.</param>
    /// <param name="input">Input manager for detecting the stand-up action.</param>
    /// <param name="player">Player entity to reposition during/after the sequence.</param>
    /// <param name="follower">Follower entity to reposition during/after the sequence.</param>
    public void Update(GameTime gameTime, IInputManager input, PlayerBlock player, FollowerBlock follower)
    {
        if (_state == CouchSitState.Idle)
        {
            return;
        }

        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (_state)
        {
            case CouchSitState.HoppingToSeat:
                UpdateHopToSeat(elapsed, player, follower);
                break;

            case CouchSitState.Seated:
                if (input.IsPressed(InputAction.Confirm) || input.IsPressed(InputAction.Cancel))
                {
                    BeginHopOff(player, follower);
                }
                break;

            case CouchSitState.HoppingOff:
                UpdateHopOff(elapsed, player, follower);
                break;
        }
    }

    /// <summary>
    /// Draws the player and follower at their current sequence positions using
    /// the appropriate animation frame (walking idle while hopping, sitting while seated).
    /// </summary>
    /// <param name="spriteBatch">Active sprite batch.</param>
    /// <param name="playerSheet">Player character sprite sheet texture.</param>
    /// <param name="followerSheet">Follower character sprite sheet texture.</param>
    /// <param name="mapPixelHeight">Map height in pixels for sort depth calculation.</param>
    /// <param name="mapPixelWidth">Map width in pixels for sort depth calculation.</param>
    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D playerSheet,
        Texture2D followerSheet,
        float mapPixelHeight,
        float mapPixelWidth)
    {
        if (_state == CouchSitState.Idle)
        {
            return;
        }

        var couchBounds = _targetCouch.Bounds;

        // Draw characters in front of the couch (sort depth just past the couch bottom).
        var seatDepthBase = (couchBounds.Bottom + 1f) / mapPixelHeight;
        // Ensure both characters are in the valid depth range.
        var playerSeatDepth = Math.Min(seatDepthBase, 0.999f);
        var followerSeatDepth = Math.Min(seatDepthBase + 0.0001f, 0.9999f);

        var isSitting = _state == CouchSitState.Seated;
        var playerSource = isSitting ? GetSittingSourceRect(CouchFacingDirection) : GetIdleSourceRect(_exitFacing);
        var followerSource = isSitting ? GetSittingSourceRect(CouchFacingDirection) : GetIdleSourceRect(_exitFacing);

        spriteBatch.Draw(followerSheet, _followerRenderPosition, followerSource,
            Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, followerSeatDepth);
        spriteBatch.Draw(playerSheet, _playerRenderPosition, playerSource,
            Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, playerSeatDepth);
    }

    private void UpdateHopToSeat(float elapsed, PlayerBlock player, FollowerBlock follower)
    {
        _playerHopTimer += elapsed;
        _followerHopTimer += elapsed;

        var playerT = Math.Clamp(_playerHopTimer / HopDurationSeconds, 0f, 1f);
        var followerT = Math.Clamp(Math.Max(0f, _followerHopTimer) / HopDurationSeconds, 0f, 1f);

        _playerRenderPosition = ComputeHopPosition(_playerStartPosition, _playerSeatPosition, playerT);
        _followerRenderPosition = ComputeHopPosition(_followerStartPosition, _followerSeatPosition, followerT);

        // Both characters have reached their seats.
        if (playerT >= 1f && followerT >= 1f)
        {
            _state = CouchSitState.Seated;
            _playerRenderPosition = _playerSeatPosition;
            _followerRenderPosition = _followerSeatPosition;
            player.SetPosition(_playerSeatPosition);
            player.SetFacing(CouchFacingDirection);
            follower.SetPosition(_followerSeatPosition);
            follower.SetFacing(CouchFacingDirection);
        }
    }

    private void BeginHopOff(PlayerBlock player, FollowerBlock follower)
    {
        _state = CouchSitState.HoppingOff;
        _playerStartPosition = _playerSeatPosition;
        _followerStartPosition = _followerSeatPosition;
        _playerHopTimer = 0f;
        _followerHopTimer = -FollowerHopDelaySeconds;
    }

    private void UpdateHopOff(float elapsed, PlayerBlock player, FollowerBlock follower)
    {
        _playerHopTimer += elapsed;
        _followerHopTimer += elapsed;

        var playerT = Math.Clamp(_playerHopTimer / HopDurationSeconds, 0f, 1f);
        var followerT = Math.Clamp(Math.Max(0f, _followerHopTimer) / HopDurationSeconds, 0f, 1f);

        _playerRenderPosition = ComputeHopPosition(_playerSeatPosition, _playerStandPosition, playerT);
        _followerRenderPosition = ComputeHopPosition(_followerSeatPosition, _followerStandPosition, followerT);

        if (playerT >= 1f && followerT >= 1f)
        {
            _state = CouchSitState.Idle;
            _targetCouch = null;
            player.SetPosition(_playerStandPosition);
            player.SetFacing(_exitFacing);
            follower.SetPosition(_followerStandPosition);
            follower.SetFacing(_exitFacing);
        }
    }

    /// <summary>
    /// Linearly interpolates between start and end with a parabolic Y arc.
    /// </summary>
    private static Vector2 ComputeHopPosition(Vector2 start, Vector2 end, float t)
    {
        var eased = SmoothStep(t);
        var basePosition = Vector2.Lerp(start, end, eased);

        // Parabolic arc: peaks at t=0.5, returns to 0 at t=0 and t=1.
        var arc = 4f * t * (1f - t) * HopArcHeightPixels;
        return new Vector2(basePosition.X, basePosition.Y - arc);
    }

    private static float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// Computes a stand position on the open side of the couch with enough clearance
    /// to avoid clipping into the prop after landing.
    /// </summary>
    private Vector2 ComputeStandPosition(Vector2 seatPosition, Rectangle couchBounds)
    {
        return new Vector2(
            couchBounds.Left - _frameWidth - StandOffsetPixels,
            seatPosition.Y);
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
/// States for the couch sitting sequence.
/// </summary>
public enum CouchSitState
{
    /// <summary>No sequence active.</summary>
    Idle,

    /// <summary>Characters are hopping toward their seats.</summary>
    HoppingToSeat,

    /// <summary>Characters are seated on the couch.</summary>
    Seated,

    /// <summary>Characters are hopping off the couch.</summary>
    HoppingOff,
}
