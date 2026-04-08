using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DogDays.Game.Components;
using DogDays.Game.Util;

namespace DogDays.Game.Entities;

/// <summary>
/// An animated fish silhouette that swims in the underwater area of the fishing screen.
/// Each fish has a behavioral state machine that cycles between idle drifting, steady
/// swimming, sudden darting, and stationary pausing — mimicking natural fish movement.
/// </summary>
public sealed class FishSilhouette
{
    // Atlas layout: 6 frames per row, 3 rows, each cell 34×22.
    private const int CellWidth = 34;
    private const int CellHeight = 22;
    private const int FrameCount = 6;

    /// <summary>Half the sprite size — used to offset spawn positions so the center is inside the swim bounds.</summary>
    public static readonly Vector2 SpriteHalfSize = new(CellWidth * 0.5f, CellHeight * 0.5f);

    // Frame duration varies by behavior to reflect urgency.
    private const float IdleFrameDuration = 0.22f;
    private const float SwimFrameDuration = 0.12f;
    private const float DartFrameDuration = 0.06f;
    private const float PauseFrameDuration = 0.35f;

    private readonly FishType _fishType;
    private readonly PolygonBounds _swimBounds;
    private readonly PolygonBounds _clampBounds;
    private readonly Random _rng;

    private Vector2 _position;
    private Vector2 _previousPosition;
    private Vector2 _velocity;
    private bool _facingLeft;
    private FrameTimer _frameTimer;
    private FishBehavior _behavior;
    private float _behaviorTimer;

    private AttractionState _attractionState;
    private float _disturbance;
    private float _spookCooldown;
    private float _curiosityTimer;
    private Vector2 _lurePosition;
    private float _orbitAngle;
    private float _rotation;
    private float _flipCooldown;

    /// <summary>
    /// Creates a fish silhouette.
    /// </summary>
    /// <param name="fishType">Species (determines atlas row and speed ranges).</param>
    /// <param name="startPosition">Initial position in screen space.</param>
    /// <param name="swimBounds">Polygon the fish must stay within.</param>
    /// <param name="rng">Random number generator for behavior variation.</param>
    public FishSilhouette(FishType fishType, Vector2 startPosition, PolygonBounds swimBounds, Random rng)
    {
        _fishType = fishType;
        _position = startPosition;
        _previousPosition = startPosition;
        _swimBounds = swimBounds;
        _clampBounds = swimBounds.Inset(16f);
        _rng = rng;
        _facingLeft = rng.Next(2) == 0;
        _frameTimer = new FrameTimer(FrameCount, IdleFrameDuration);
        _behavior = FishBehavior.Idle;
        _behaviorTimer = NextBehaviorDuration();
        _velocity = IdleDriftVelocity();
    }

    /// <summary>Screen-space position of the fish.</summary>
    public Vector2 Position => _position;

    /// <summary>Whether the fish is facing left.</summary>
    public bool FacingLeft => _facingLeft;

    /// <summary>Current behavioral state.</summary>
    public FishBehavior Behavior => _behavior;

    /// <summary>Current attraction state toward the lure.</summary>
    public AttractionState Attraction => _attractionState;

    /// <summary>The species of this fish.</summary>
    public FishType Species => _fishType;

    /// <summary>Whether the fish is currently hooked.</summary>
    public bool IsHooked => _attractionState == AttractionState.Hooked;

    /// <summary>Whether the fish is fleeing off-screen.</summary>
    public bool IsFleeing => _attractionState == AttractionState.Fleeing;

    /// <summary>Reel speed multiplier based on species size. Smaller fish reel faster.</summary>
    public float ReelSpeedMultiplier => _fishType switch
    {
        FishType.Perch => 1.25f,
        FishType.Bass => 0.75f,
        FishType.Catfish => 0.4f,
        _ => 1f,
    };

    /// <summary>Sets the horizontal facing direction.</summary>
    public void SetFacingLeft(bool facingLeft) => _facingLeft = facingLeft;

    /// <summary>
    /// Changes facing direction only if the flip cooldown has elapsed,
    /// preventing rapid left/right jitter.
    /// </summary>
    private void SetFacing(bool left)
    {
        if (left == _facingLeft)
            return;
        if (_flipCooldown > 0f)
            return;
        _facingLeft = left;
        _flipCooldown = 0.15f;
    }

    /// <summary>Center point of the fish sprite in screen space.</summary>
    public Vector2 Center => _position + SpriteHalfSize;

    /// <summary>Mouth position in screen space, accounting for facing direction and rotation.</summary>
    public Vector2 MouthPosition
    {
        get
        {
            var center = Center;
            var mouthDist = GetSpeciesTuning().MouthOffset;
            // Mouth is near the leading edge of the sprite (left when _facingLeft).
            var localOffset = new Vector2(_facingLeft ? -mouthDist : mouthDist, 0f);
            // Rotate the offset by the current rotation.
            if (_rotation != 0f)
            {
                var cos = MathF.Cos(_rotation);
                var sin = MathF.Sin(_rotation);
                localOffset = new Vector2(
                    localOffset.X * cos - localOffset.Y * sin,
                    localOffset.X * sin + localOffset.Y * cos);
            }
            return center + localOffset;
        }
    }

    /// <summary>
    /// Updates fish behavior, animation, and position.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Hooked — position is externally controlled, just animate.
        if (IsHooked)
        {
            _frameTimer.Advance(gameTime);
            return;
        }

        // Fleeing — dart right at high speed, ignore bounds.
        if (IsFleeing)
        {
            _position += _velocity * dt;
            _frameTimer.Advance(gameTime);
            return;
        }

        // Tick timers.
        if (_flipCooldown > 0f)
            _flipCooldown -= dt;
        TickAttractionTimers(dt);

        // Advance behavior timer.
        _behaviorTimer -= dt;
        if (_behaviorTimer <= 0f)
        {
            TransitionBehavior();
        }

        // Override velocity based on attraction state.
        ApplyAttractionVelocity(dt);

        // Apply velocity.
        _previousPosition = _position;
        _position += _velocity * dt;

        // Clamp to swim bounds and bounce off edges.
        ClampToBounds();

        // Advance animation.
        _frameTimer.Advance(gameTime);
    }

    /// <summary>
    /// Draws the fish silhouette from the atlas.
    /// </summary>
    /// <param name="spriteBatch">Active sprite batch.</param>
    /// <param name="texture">The fish-silhouettes atlas texture.</param>
    /// <param name="tint">Color tint to apply (use <see cref="Color.White"/> for default).</param>
    public void Draw(SpriteBatch spriteBatch, Texture2D texture, Color tint)
    {
        var sourceRect = new Rectangle(
            _frameTimer.CurrentFrame * CellWidth,
            (int)_fishType * CellHeight,
            CellWidth,
            CellHeight);

        var effects = _facingLeft ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

        var origin = new Vector2(CellWidth * 0.5f, CellHeight * 0.5f);
        var drawPos = _position + origin;

        spriteBatch.Draw(
            texture,
            drawPos,
            sourceRect,
            tint,
            _rotation,
            origin,
            1f,
            effects,
            0f);
    }

    /// <summary>
    /// Drives the attraction state machine. Call each frame while the lure is in the water.
    /// </summary>
    /// <param name="lurePosition">Current lure position in screen space.</param>
    /// <param name="deltaTime">Elapsed seconds this frame.</param>
    /// <param name="lureEvent">Stimulus event this frame (use <see cref="LureEvent.None"/> when idle).</param>
    public void UpdateAttraction(Vector2 lurePosition, float deltaTime, LureEvent lureEvent)
    {
        _lurePosition = lurePosition;

        if (_attractionState == AttractionState.Hooked)
            return;

        var distance = Math.Abs(Center.X - lurePosition.X);
        var factor = DistanceFactor(distance);
        var tuning = GetSpeciesTuning();

        // Update disturbance.
        switch (lureEvent)
        {
            case LureEvent.Twitch:
                _disturbance += 0.25f * factor;
                break;
            case LureEvent.ReelTick:
                _disturbance += 0.05f * deltaTime * factor;
                break;
            case LureEvent.Splash:
                _disturbance += 0.15f * factor;
                break;
            case LureEvent.BadSplash:
                _disturbance += 0.60f * factor;
                break;
        }

        _disturbance -= 0.10f * deltaTime;
        _disturbance = MathHelper.Clamp(_disturbance, 0f, tuning.SpookThreshold);

        // Disturbance threshold overrides everything except Spooked/Hooked.
        if (_disturbance >= tuning.SpookThreshold &&
            _attractionState != AttractionState.Spooked)
        {
            _attractionState = AttractionState.Spooked;
            _spookCooldown = 5f;
            SetSpookedVelocity();
            return;
        }

        switch (_attractionState)
        {
            case AttractionState.Unaware:
                if ((lureEvent == LureEvent.Twitch || lureEvent == LureEvent.Splash) &&
                    distance <= tuning.AwarenessRadius)
                {
                    _attractionState = AttractionState.Curious;
                    _curiosityTimer = 5f;
                }
                break;

            case AttractionState.Curious:
                if (lureEvent == LureEvent.Twitch)
                {
                    _curiosityTimer = 5f;
                    if (tuning.CanStrike)
                        _attractionState = AttractionState.Approaching;
                }
                else if (lureEvent == LureEvent.Splash)
                {
                    _curiosityTimer = 5f;
                }
                break;

            case AttractionState.Approaching:
                if (distance < 30f)
                {
                    if (tuning.CanStrike)
                    {
                        _attractionState = AttractionState.StrikeReady;
                        _curiosityTimer = 4f + (float)_rng.NextDouble();
                        _orbitAngle = 0f;
                    }
                    else
                    {
                        _attractionState = AttractionState.Curious;
                        _curiosityTimer = 5f;
                    }
                }
                break;

            case AttractionState.StrikeReady:
                if (lureEvent == LureEvent.Twitch || lureEvent == LureEvent.Splash)
                    _curiosityTimer = 4f + (float)_rng.NextDouble();
                break;

            case AttractionState.Spooked:
                // Cooldown handled in TickAttractionTimers.
                break;
        }
    }

    /// <summary>
    /// Marks this fish as hooked, freezing autonomous movement.
    /// </summary>
    public void SetHooked()
    {
        _attractionState = AttractionState.Hooked;
        _velocity = Vector2.Zero;
        _facingLeft = false; // Face away from shore — fish fights the pull.
    }

    /// <summary>
    /// Forces this fish into the Spooked state, scattering it away from the lure.
    /// </summary>
    public void Spook()
    {
        if (_attractionState is AttractionState.Hooked or AttractionState.Fleeing)
            return;
        _attractionState = AttractionState.Spooked;
        _spookCooldown = 3f + (float)_rng.NextDouble() * 2f;
        _disturbance = 0f;
        SetSpookedVelocity();
    }

    /// <summary>
    /// Sets a one-time flee velocity away from the lure when entering the Spooked state.
    /// The direction is computed once and not updated, preventing jitter at boundaries.
    /// </summary>
    private void SetSpookedVelocity()
    {
        var away = Center - _lurePosition;
        if (away.LengthSquared() > 1f)
            away.Normalize();
        else
            away = _facingLeft ? -Vector2.UnitX : Vector2.UnitX;

        var speed = GetSpeedRange().max * 2f;
        _velocity = away * speed;
        _facingLeft = _velocity.X < 0; // Force — spook overrides cooldown.
        _flipCooldown = 0.15f;
        _behavior = FishBehavior.Dart;
        _behaviorTimer = 0.5f + (float)_rng.NextDouble() * 0.5f;
    }

    /// <summary>
    /// Sends the fish fleeing to the right at high speed. Used when a hooked fish escapes.
    /// </summary>
    public void Flee()
    {
        _attractionState = AttractionState.Fleeing;
        _rotation = 0f;
        _facingLeft = false;
        _velocity = new Vector2(120f + (float)_rng.NextDouble() * 60f, 0f);
        _frameTimer = new FrameTimer(FrameCount, DartFrameDuration);
    }

    /// <summary>
    /// Sets the fish position directly. Used by the fishing screen during hooked animations.
    /// </summary>
    /// <param name="position">New screen-space position.</param>
    public void SetPosition(Vector2 position)
    {
        _position = position;
    }

    /// <summary>
    /// Sets the fish rotation in radians. Used during hooked animations.
    /// </summary>
    public void SetRotation(float radians)
    {
        _rotation = radians;
    }

    private void TransitionBehavior()
    {
        // Weighted random behavior selection.
        var roll = _rng.NextDouble();
        FishBehavior next;
        if (roll < 0.30)
            next = FishBehavior.Idle;
        else if (roll < 0.60)
            next = FishBehavior.Swim;
        else if (roll < 0.75)
            next = FishBehavior.Dart;
        else
            next = FishBehavior.Pause;

        // Avoid repeating pauses back-to-back (looks dead).
        if (next == FishBehavior.Pause && _behavior == FishBehavior.Pause)
            next = FishBehavior.Swim;

        // Occasionally flip direction on transition.
        if (_rng.NextDouble() < 0.3)
            SetFacing(!_facingLeft);

        _behavior = next;
        _behaviorTimer = NextBehaviorDuration();
        _velocity = BehaviorVelocity();
        _frameTimer = new FrameTimer(FrameCount, BehaviorFrameDuration());
    }

    private float NextBehaviorDuration()
    {
        return _behavior switch
        {
            FishBehavior.Idle => 1.5f + (float)_rng.NextDouble() * 2.5f,
            FishBehavior.Swim => 2.0f + (float)_rng.NextDouble() * 3.0f,
            FishBehavior.Dart => 0.3f + (float)_rng.NextDouble() * 0.5f,
            FishBehavior.Pause => 1.0f + (float)_rng.NextDouble() * 3.0f,
            _ => 2f,
        };
    }

    private float BehaviorFrameDuration()
    {
        return _behavior switch
        {
            FishBehavior.Idle => IdleFrameDuration,
            FishBehavior.Swim => SwimFrameDuration,
            FishBehavior.Dart => DartFrameDuration,
            FishBehavior.Pause => PauseFrameDuration,
            _ => IdleFrameDuration,
        };
    }

    private Vector2 BehaviorVelocity()
    {
        var speedRange = GetSpeedRange();
        var direction = _facingLeft ? -1f : 1f;

        return _behavior switch
        {
            FishBehavior.Idle => IdleDriftVelocity(),
            FishBehavior.Swim => new Vector2(
                direction * (speedRange.min + (float)_rng.NextDouble() * (speedRange.max - speedRange.min)),
                ((float)_rng.NextDouble() - 0.5f) * speedRange.max * 0.8f),
            FishBehavior.Dart => new Vector2(
                direction * speedRange.max * (1.5f + (float)_rng.NextDouble() * 0.5f),
                ((float)_rng.NextDouble() - 0.5f) * speedRange.max * 1.2f),
            FishBehavior.Pause => Vector2.Zero,
            _ => Vector2.Zero,
        };
    }

    private Vector2 IdleDriftVelocity()
    {
        var speedRange = GetSpeedRange();
        var direction = _facingLeft ? -1f : 1f;
        return new Vector2(
            direction * speedRange.min * (0.2f + (float)_rng.NextDouble() * 0.3f),
            ((float)_rng.NextDouble() - 0.5f) * speedRange.min * 0.8f);
    }

    private (float min, float max) GetSpeedRange()
    {
        return _fishType switch
        {
            FishType.Perch => (25f, 60f),
            FishType.Bass => (15f, 40f),
            FishType.Catfish => (8f, 25f),
            _ => (15f, 40f),
        };
    }

    private void TickAttractionTimers(float dt)
    {
        switch (_attractionState)
        {
            case AttractionState.Spooked:
                _spookCooldown -= dt;
                if (_spookCooldown <= 0f)
                {
                    _attractionState = AttractionState.Unaware;
                    _disturbance = 0f;
                }
                break;

            case AttractionState.Curious:
                _curiosityTimer -= dt;
                if (_curiosityTimer <= 0f)
                    _attractionState = AttractionState.Unaware;
                break;

            case AttractionState.Approaching:
                _curiosityTimer -= dt;
                if (_curiosityTimer <= 0f)
                    _attractionState = AttractionState.Unaware;
                break;

            case AttractionState.StrikeReady:
                _curiosityTimer -= dt;
                if (_curiosityTimer <= 0f)
                {
                    _attractionState = AttractionState.Spooked;
                    _spookCooldown = 5f;
                }
                break;
        }
    }

    private void ApplyAttractionVelocity(float dt)
    {
        switch (_attractionState)
        {
            case AttractionState.Curious:
            {
                // Only drift toward the lure if this species can actually strike.
                // Ambient fish (perch) stay on their normal swim patterns.
                var tuning = GetSpeciesTuning();
                if (tuning.CanStrike)
                {
                    var towardLure = _lurePosition - Center;
                    if (towardLure.LengthSquared() > 1f)
                    {
                        towardLure.Normalize();
                        // Gently blend current velocity toward the lure direction
                        // without changing speed. This steers without oscillation.
                        var currentSpeed = _velocity.Length();
                        if (currentSpeed < 1f)
                            currentSpeed = GetSpeedRange().min;
                        var desired = towardLure * currentSpeed;
                        _velocity = Vector2.Lerp(_velocity, desired, 1.5f * dt);
                        SetFacing(_velocity.X < 0);
                    }
                }
                break;
            }
            case AttractionState.Approaching:
            {
                // Target a point slightly below the lure so the fish rises near the surface.
                var approachTarget = _lurePosition + new Vector2(0f, 12f);
                var towardLure = approachTarget - Center;
                if (towardLure.LengthSquared() > 1f)
                {
                    towardLure.Normalize();
                    var speed = GetSpeedRange().max;
                    _velocity = towardLure * speed;
                    SetFacing(_velocity.X < 0);
                }
                break;
            }
            case AttractionState.StrikeReady:
            {
                const float orbitRadius = 15f;
                const float orbitSpeed = 1.5f;
                _orbitAngle += orbitSpeed * dt;
                var orbitOffset = new Vector2(
                    MathF.Cos(_orbitAngle) * orbitRadius,
                    MathF.Sin(_orbitAngle) * orbitRadius);
                var targetPos = _lurePosition + orbitOffset - SpriteHalfSize;
                _velocity = (targetPos - _position) * 3f;
                SetFacing(_lurePosition.X < Center.X);
                break;
            }
            case AttractionState.Spooked:
                // Velocity is set once on entering Spooked — no continuous override.
                break;
        }
    }

    private static float DistanceFactor(float distance)
    {
        if (distance < 30f) return 1.0f;
        if (distance < 80f) return 0.5f;
        if (distance < 150f) return 0.2f;
        return 0.0f;
    }

    private (float AwarenessRadius, float SpookThreshold, bool CanStrike, float MouthOffset) GetSpeciesTuning()
    {
        return _fishType switch
        {
            FishType.Perch => (200f, 1.5f, true, 5f),
            FishType.Bass => (150f, 1.5f, true, 10f),
            FishType.Catfish => (100f, 1.0f, true, 13f),
            _ => (100f, 1.0f, true, 10f),
        };
    }

    private void ClampToBounds()
    {
        // Fish actively pursuing or orbiting the lure are allowed to leave swim bounds.
        if (_attractionState is AttractionState.Approaching
            or AttractionState.StrikeReady)
            return;

        // Test the fish center against the polygon.
        var halfSize = new Vector2(CellWidth * 0.5f, CellHeight * 0.5f);
        var center = _position + halfSize;

        if (_clampBounds.Contains(center))
            return;

        // Check if previous position was valid — revert without hard-reversing velocity
        // so the fish slides along the edge instead of visibly bouncing.
        var prevCenter = _previousPosition + halfSize;
        if (_clampBounds.Contains(prevCenter))
        {
            _position = _previousPosition;
            // Flip facing and swim away from the edge.
            _facingLeft = !_facingLeft; // Force — bounds correction overrides cooldown.
            _flipCooldown = 0.15f;
            _velocity = IdleDriftVelocity();
            _behavior = FishBehavior.Idle;
            _behaviorTimer = NextBehaviorDuration();
        }
        else
        {
            // Both positions are outside — teleport center to polygon centroid.
            _position = _swimBounds.Centroid - halfSize;
            _velocity = IdleDriftVelocity();
            _behavior = FishBehavior.Idle;
        }

        // If we bounced during a dart, transition early to look natural.
        if (_behavior == FishBehavior.Dart)
        {
            _behaviorTimer = 0f;
        }
    }

    /// <summary>
    /// Fish species that determines atlas row, size, and speed characteristics.
    /// </summary>
    public enum FishType
    {
        /// <summary>Small, fast, darty fish (row 0).</summary>
        Perch = 0,

        /// <summary>Medium, steady swimmer (row 1).</summary>
        Bass = 1,

        /// <summary>Large, slow lurker (row 2).</summary>
        Catfish = 2,
    }

    /// <summary>
    /// Behavioral states for fish movement.
    /// </summary>
    public enum FishBehavior
    {
        /// <summary>Gentle drifting with minimal movement.</summary>
        Idle,

        /// <summary>Steady directional swimming.</summary>
        Swim,

        /// <summary>Sudden fast burst of speed.</summary>
        Dart,

        /// <summary>Completely stationary, hovering in place.</summary>
        Pause,
    }

    /// <summary>
    /// Overlay states tracking a fish's interest in a lure.
    /// </summary>
    public enum AttractionState
    {
        /// <summary>Default — normal behavior, unaware of lure.</summary>
        Unaware,

        /// <summary>Noticed the lure, biasing drift toward it.</summary>
        Curious,

        /// <summary>Actively swimming toward the lure.</summary>
        Approaching,

        /// <summary>Near the lure, waiting for a strike trigger.</summary>
        StrikeReady,

        /// <summary>Scared off, on cooldown before it can be attracted again.</summary>
        Spooked,

        /// <summary>Caught — position is controlled externally.</summary>
        Hooked,

        /// <summary>Escaped — darting away to the right, will be removed when off-screen.</summary>
        Fleeing,
    }

    /// <summary>
    /// Stimulus events that the lure can produce each frame.
    /// </summary>
    public enum LureEvent
    {
        /// <summary>No stimulus this frame.</summary>
        None,

        /// <summary>Player popped/twitched the lure.</summary>
        Twitch,

        /// <summary>Lure landed on the water (cast).</summary>
        Splash,

        /// <summary>Player is actively reeling.</summary>
        ReelTick,

        /// <summary>Lure landed badly (red-zone cast) — spooks fish.</summary>
        BadSplash,
    }
}
