using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Data;
using RiverRats.Game.Data;
using RiverRats.Game.Systems;
using RiverRats.Game.World;

namespace RiverRats.Game.Entities;

/// <summary>
/// A garden gnome enemy that chases the player via flow field, then winds up
/// and lunges hat-first like a missile. Bounces back on hit or slides to a stop on miss.
/// </summary>
internal sealed class GnomeEnemy
{
    private const float HopCycleDuration = 0.45f;
    private const float HopHeight = 6f;
    private const float MoveSpeed = 60f;
    private const int SpriteSize = 16;

    // Attack tuning.
    private const float AttackTriggerRange = 48f;
    private const float AttackTriggerRangeSq = AttackTriggerRange * AttackTriggerRange;
    private const float WindUpDuration = 0.4f;
    private const float LungeSpeed = 220f;
    private const float LungeDistance = 80f;
    private const float HitStunDuration = 1.2f;
    private const float MissRecoveryDuration = 0.6f;
    private const float RecoilDistance = 64f;
    private const int BounceCount = 3;
    private const float BounceHeight = 8f;

    // Damage stun (shorter than recoil stun).
    private const float DamageStunDuration = 0.6f;

    // Death pop animation.
    private const float DyingDuration = 0.2f;
    private const float PopScaleMax = 1.6f;

    private int _hp = 1;
    private float _speedMultiplier = 1.0f;

    // Per-type variant fields (set once at spawn via SetEnemyType).
    private EnemyType _enemyType = EnemyType.Standard;
    private float _drawScale = 1.0f;
    private Color _tintColor = Color.White;
    private float _baseSpeedMultiplier = 1.0f;
    private bool _explodeOnDeath;

    private Vector2 _position;
    private float _hopPhase;
    private bool _facingLeft;

    // Stuck detection — phase through obstacles after being stuck too long.
    private const float StuckTimeThreshold = 1f;
    private const float StuckDistanceThreshold = 8f;
    private const float PhaseClearDistance = 16f;
    private Vector2 _stuckCheckpoint;
    private float _stuckTimer;
    private bool _phasing;

    // State machine.
    private GnomeState _state;
    private float _stateTimer;
    private Vector2 _lungeDirection;
    private float _lungeDistanceTraveled;
    private Vector2 _recoilStart;
    private Vector2 _recoilEnd;
    private bool _hitPlayer;
    private bool _isDead;
    private bool _justHitPlayer;

    /// <summary>
    /// Creates a gnome enemy at the given world position.
    /// </summary>
    /// <param name="position">Initial world position (top-left of sprite).</param>
    /// <param name="initialHopPhase">Starting hop phase (0–1) to stagger gnome hop cycles.</param>
    public GnomeEnemy(Vector2 position, float initialHopPhase)
    {
        _position = position;
        _stuckCheckpoint = position;
        _hopPhase = initialHopPhase;
        _state = GnomeState.Chasing;
    }

    /// <summary>Current hit points. Defaults to 1 for backwards compatibility.</summary>
    public int Hp => _hp;

    /// <summary>World position of the gnome (top-left of sprite, excluding hop offset).</summary>
    public Vector2 Position => _position;

    /// <summary>Current behavioral state.</summary>
    public GnomeState State => _state;

    /// <summary>Whether the gnome has finished its death animation and should be removed.</summary>
    public bool IsDead => _isDead;

    /// <summary>True for exactly one frame after this gnome's lunge hit the player.</summary>
    public bool JustHitPlayer => _justHitPlayer;

    /// <summary>Whether the gnome is currently phasing through obstacles.</summary>
    public bool IsPhasing => _phasing;

    /// <summary>The enemy variant type of this gnome.</summary>
    public EnemyType EnemyType => _enemyType;

    /// <summary>Whether this gnome should explode on death (Bomber type).</summary>
    public bool ExplodeOnDeath => _explodeOnDeath;

    /// <summary>
    /// Returns the bounding rectangle used for Y-sorting. Based on world position (no hop offset).
    /// </summary>
    public Rectangle Bounds => new(
        (int)_position.X,
        (int)_position.Y,
        SpriteSize,
        SpriteSize);

    /// <summary>
    /// Sets the gnome's HP. Call when spawning or recycling from the pool.
    /// </summary>
    public void SetHp(int hp)
    {
        _hp = hp;
    }

    /// <summary>
    /// Sets a per-gnome speed multiplier applied on top of crowding slowdown.
    /// </summary>
    /// <param name="multiplier">Speed scaling factor (1.0 = normal).</param>
    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = multiplier;
    }

    /// <summary>
    /// Configures per-type visual and behavioral overrides. Call once right after construction.
    /// </summary>
    public void SetEnemyType(EnemyType type)
    {
        _enemyType = type;
        switch (type)
        {
            case EnemyType.Standard:
                _drawScale = 1.0f;
                _tintColor = Color.White;
                _baseSpeedMultiplier = 1.0f;
                _explodeOnDeath = false;
                break;
            case EnemyType.Rusher:
                _drawScale = 0.7f;
                _tintColor = new Color(120, 255, 120);
                _baseSpeedMultiplier = 1.8f;
                _explodeOnDeath = false;
                break;
            case EnemyType.Brute:
                _drawScale = 1.4f;
                _tintColor = new Color(180, 100, 255);
                _baseSpeedMultiplier = 0.5f;
                _explodeOnDeath = false;
                break;
            case EnemyType.Bomber:
                _drawScale = 1.0f;
                _tintColor = new Color(255, 140, 60);
                _baseSpeedMultiplier = 1.0f;
                _explodeOnDeath = true;
                break;
        }
    }

    /// <summary>
    /// Reduces HP by the given damage amount. Transitions to Dying if HP reaches zero,
    /// or to a short Stunned state if HP remains above zero.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (_state == GnomeState.Dying)
            return;

        _hp -= damage;
        if (_hp <= 0)
        {
            Die();
        }
        else
        {
            _state = GnomeState.Stunned;
            _stateTimer = 0f;
            _hitPlayer = false;
            _recoilStart = _position;
            _recoilEnd = _position; // no recoil slide on damage stun
        }
    }

    /// <summary>
    /// Transitions the gnome into the Dying state with a pop animation.
    /// After the animation completes, <see cref="IsDead"/> becomes true.
    /// </summary>
    public void Die()
    {
        if (_state == GnomeState.Dying)
            return;
        _state = GnomeState.Dying;
        _stateTimer = 0f;
    }

    /// <summary>
    /// Advances the gnome one frame. Behavior depends on current state.
    /// </summary>
    /// <param name="gameTime">Current frame timing.</param>
    /// <param name="targetPosition">World position of the target (player centre).</param>
    /// <param name="flowField">Pre-computed BFS flow field pointing toward the target.</param>
    /// <param name="collisionMap">World collision data for fine-grained obstacle queries.</param>
    /// <param name="separationForce">Pre-computed repulsion vector from nearby gnomes.</param>
    /// <param name="playerBounds">Player bounding rectangle for lunge hit detection.</param>
    /// <param name="speedMultiplier">Crowding slowdown factor (0–1).</param>
    public void Update(GameTime gameTime, Vector2 targetPosition, FlowField flowField,
        IMapCollisionData collisionMap, Vector2 separationForce, Rectangle playerBounds,
        float speedMultiplier = 1f)
    {
        _justHitPlayer = false;
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (_state)
        {
            case GnomeState.Chasing:
                UpdateChasing(dt, targetPosition, flowField, collisionMap, separationForce, speedMultiplier);
                break;
            case GnomeState.WindingUp:
                UpdateWindingUp(dt, targetPosition);
                break;
            case GnomeState.Lunging:
                UpdateLunging(dt, collisionMap, playerBounds);
                break;
            case GnomeState.Stunned:
                UpdateStunned(dt);
                break;
            case GnomeState.Dying:
                UpdateDying(dt);
                break;
        }
    }

    /// <summary>
    /// Draws the gnome with visual effects for each state: squash during wind-up,
    /// rotation during lunge, bounce during stun.
    /// </summary>
    /// <param name="spriteBatch">Active sprite batch (must be within Begin/End).</param>
    /// <param name="texture">The garden gnome texture.</param>
    /// <param name="layerDepth">Y-sort depth for FrontToBack ordering.</param>
    public void Draw(SpriteBatch spriteBatch, Texture2D texture, float layerDepth)
    {
        var effects = _facingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        var origin = new Vector2(SpriteSize * 0.5f, SpriteSize * 0.5f);
        var drawCenter = new Vector2(_position.X + SpriteSize * 0.5f, _position.Y + SpriteSize * 0.5f);

        switch (_state)
        {
            case GnomeState.Chasing:
            {
                var hopOffset = -MathF.Abs(MathF.Sin(_hopPhase * MathHelper.TwoPi)) * HopHeight;
                drawCenter.Y += hopOffset;
                spriteBatch.Draw(texture, drawCenter, null, _tintColor, 0f, origin,
                    new Vector2(_drawScale, _drawScale), effects, layerDepth);
                break;
            }

            case GnomeState.WindingUp:
            {
                // Squash: compress Y, widen X as wind-up progresses.
                var t = _stateTimer / WindUpDuration;
                var scaleX = 1f + t * 0.4f;
                var scaleY = 1f - t * 0.35f;
                spriteBatch.Draw(texture, drawCenter, null, _tintColor, 0f, origin,
                    new Vector2(scaleX * _drawScale, scaleY * _drawScale), effects, layerDepth);
                break;
            }

            case GnomeState.Lunging:
            {
                // Rotate so the hat (top of sprite) points in the lunge direction.
                // Sprite hat is at -Y (top), so add PiOver2 to align hat with movement.
                var rotation = MathF.Atan2(_lungeDirection.Y, _lungeDirection.X) + MathHelper.PiOver2;
                // Stretch along lunge axis for a missile look.
                spriteBatch.Draw(texture, drawCenter, null, _tintColor, rotation, origin,
                    new Vector2(_drawScale, 1.3f * _drawScale), SpriteEffects.None, layerDepth);
                break;
            }

            case GnomeState.Stunned:
            {
                // Bounce with diminishing height.
                var stunT = _stateTimer / (_hitPlayer ? HitStunDuration : MissRecoveryDuration);
                var bounceT = stunT * BounceCount;
                var bouncePhase = bounceT - MathF.Floor(bounceT);
                var dampen = 1f - stunT;
                var bounceOffset = -MathF.Abs(MathF.Sin(bouncePhase * MathF.PI)) * BounceHeight * dampen;
                drawCenter.Y += bounceOffset;
                // Flash white briefly to show stun.
                var tint = stunT < 0.15f ? Color.White * 0.5f : Color.White;
                spriteBatch.Draw(texture, drawCenter, null, MultiplyColor(tint, _tintColor), 0f, origin,
                    new Vector2(_drawScale, _drawScale), effects, layerDepth);
                break;
            }

            case GnomeState.Dying:
            {
                // Pop animation: rapid scale-up then shrink to zero.
                var dyingT = Math.Clamp(_stateTimer / DyingDuration, 0f, 1f);
                float popScale;
                if (dyingT < 0.3f)
                {
                    // Quick stretch to max.
                    popScale = 1f + (PopScaleMax - 1f) * (dyingT / 0.3f);
                }
                else
                {
                    // Shrink from max to zero.
                    popScale = PopScaleMax * (1f - (dyingT - 0.3f) / 0.7f);
                }

                // Flash white for the first half, then fade to transparent.
                var alpha = 1f - dyingT;
                var dyingTint = dyingT < 0.5f ? Color.White : Color.White * alpha;
                spriteBatch.Draw(texture, drawCenter, null, MultiplyColor(dyingTint, _tintColor), 0f, origin,
                    new Vector2(popScale * _drawScale, popScale * _drawScale), effects, layerDepth);
                break;
            }
        }
    }

    private void UpdateChasing(float dt, Vector2 targetPosition, FlowField flowField,
        IMapCollisionData collisionMap, Vector2 separationForce, float speedMultiplier)
    {
        // Check if within attack range.
        var center = new Vector2(_position.X + SpriteSize * 0.5f, _position.Y + SpriteSize * 0.5f);
        var toTarget = targetPosition - center;
        if (toTarget.LengthSquared() <= AttackTriggerRangeSq)
        {
            _state = GnomeState.WindingUp;
            _stateTimer = 0f;
            return;
        }

        var flowDir = flowField.GetDirection(center);

        // If flow field has no data (gnome on a padded-blocked tile), fall back to
        // a direct bee-line toward the target so the gnome doesn't freeze in place.
        if (flowDir == Vector2.Zero)
        {
            var distSq = toTarget.LengthSquared();
            if (distSq > 1f)
                flowDir = toTarget * (1f / MathF.Sqrt(distSq));
        }

        // Blend flow direction with separation force.
        var steeringDir = flowDir + separationForce;
        var steerLenSq = steeringDir.LengthSquared();
        if (steerLenSq > 0.0001f)
        {
            steeringDir *= 1f / MathF.Sqrt(steerLenSq);
            var effectiveSpeed = MoveSpeed * _baseSpeedMultiplier * _speedMultiplier * speedMultiplier;
            var movement = steeringDir * effectiveSpeed * dt;

            if (_phasing)
            {
                // Phasing — ignore collision, move freely.
                _position += movement;

                // Clear phasing once we've moved far enough from where we got stuck.
                var fromCheckpoint = _position - _stuckCheckpoint;
                if (fromCheckpoint.LengthSquared() >= PhaseClearDistance * PhaseClearDistance)
                {
                    _phasing = false;
                    _stuckCheckpoint = _position;
                    _stuckTimer = 0f;
                }
            }
            else
            {
                var originalPos = _position;

                var movedX = false;
                var movedY = false;

                if (movement.X != 0f)
                {
                    var candidateX = _position.X + movement.X;
                    var candidateBounds = new Rectangle((int)candidateX, (int)_position.Y, SpriteSize, SpriteSize);
                    if (!collisionMap.IsWorldRectangleBlocked(candidateBounds))
                    {
                        _position.X = candidateX;
                        movedX = true;
                    }
                }

                if (movement.Y != 0f)
                {
                    var candidateY = _position.Y + movement.Y;
                    var candidateBounds = new Rectangle((int)_position.X, (int)candidateY, SpriteSize, SpriteSize);
                    if (!collisionMap.IsWorldRectangleBlocked(candidateBounds))
                    {
                        _position.Y = candidateY;
                        movedY = true;
                    }
                }

                // Speed redistribution: when one axis is blocked, slide the other at full speed.
                if (movedX && !movedY)
                {
                    var fullSlideX = MathF.CopySign(effectiveSpeed * dt, steeringDir.X);
                    var candidateX = originalPos.X + fullSlideX;
                    var slideBounds = new Rectangle((int)candidateX, (int)_position.Y, SpriteSize, SpriteSize);
                    if (!collisionMap.IsWorldRectangleBlocked(slideBounds))
                        _position.X = candidateX;
                }
                else if (!movedX && movedY)
                {
                    var fullSlideY = MathF.CopySign(effectiveSpeed * dt, steeringDir.Y);
                    var candidateY = originalPos.Y + fullSlideY;
                    var slideBounds = new Rectangle((int)_position.X, (int)candidateY, SpriteSize, SpriteSize);
                    if (!collisionMap.IsWorldRectangleBlocked(slideBounds))
                        _position.Y = candidateY;
                }
                else if (!movedX && !movedY)
                {
                    // Both axes blocked — try each axis independently.
                    if (steeringDir.X != 0f)
                    {
                        var slideX = MathF.CopySign(effectiveSpeed * dt, steeringDir.X);
                        var slideBounds = new Rectangle((int)(_position.X + slideX), (int)_position.Y, SpriteSize, SpriteSize);
                        if (!collisionMap.IsWorldRectangleBlocked(slideBounds))
                            _position.X += slideX;
                    }

                    if (steeringDir.Y != 0f)
                    {
                        var slideY = MathF.CopySign(effectiveSpeed * dt, steeringDir.Y);
                        var slideBounds = new Rectangle((int)_position.X, (int)(_position.Y + slideY), SpriteSize, SpriteSize);
                        if (!collisionMap.IsWorldRectangleBlocked(slideBounds))
                            _position.Y += slideY;
                    }
                }

                // Stuck detection: if we haven't moved far from the checkpoint, accumulate time.
                var drift = _position - _stuckCheckpoint;
                if (drift.LengthSquared() <= StuckDistanceThreshold * StuckDistanceThreshold)
                {
                    _stuckTimer += dt;
                    if (_stuckTimer >= StuckTimeThreshold)
                        _phasing = true;
                }
                else
                {
                    // Meaningful movement — reset checkpoint.
                    _stuckCheckpoint = _position;
                    _stuckTimer = 0f;
                }
            }

            _facingLeft = steeringDir.X < 0f || (steeringDir.X == 0f && targetPosition.X < _position.X);
        }

        _hopPhase += dt / HopCycleDuration;
        if (_hopPhase > 1f)
            _hopPhase -= 1f;
    }

    private void UpdateWindingUp(float dt, Vector2 targetPosition)
    {
        _stateTimer += dt;

        // Face the player during wind-up.
        _facingLeft = targetPosition.X < _position.X + SpriteSize * 0.5f;

        if (_stateTimer >= WindUpDuration)
        {
            // Lock lunge direction at the end of wind-up.
            var center = new Vector2(_position.X + SpriteSize * 0.5f, _position.Y + SpriteSize * 0.5f);
            var toTarget = targetPosition - center;
            var lenSq = toTarget.LengthSquared();
            _lungeDirection = lenSq > 0.0001f
                ? toTarget * (1f / MathF.Sqrt(lenSq))
                : new Vector2(_facingLeft ? -1f : 1f, 0f);

            _state = GnomeState.Lunging;
            _stateTimer = 0f;
            _lungeDistanceTraveled = 0f;
        }
    }

    private void UpdateLunging(float dt, IMapCollisionData collisionMap, Rectangle playerBounds)
    {
        var step = LungeSpeed * dt;
        var movement = _lungeDirection * step;

        // Move and check for wall collision.
        var newPos = _position + movement;
        var newBounds = new Rectangle((int)newPos.X, (int)newPos.Y, SpriteSize, SpriteSize);
        var hitWall = collisionMap.IsWorldRectangleBlocked(newBounds);

        if (!hitWall)
        {
            _position = newPos;
            _lungeDistanceTraveled += step;
        }

        // Check for player hit.
        var gnomeBounds = new Rectangle((int)_position.X, (int)_position.Y, SpriteSize, SpriteSize);
        if (gnomeBounds.Intersects(playerBounds))
        {
            EnterStunned(hitPlayer: true);
            return;
        }

        // Hit a wall or traveled full lunge distance — miss.
        if (hitWall || _lungeDistanceTraveled >= LungeDistance)
        {
            EnterStunned(hitPlayer: false);
        }
    }

    private void UpdateStunned(float dt)
    {
        // Damage stun uses a shorter duration when the gnome was hit by a projectile
        // but still has HP remaining. We detect this by checking _recoilStart == _recoilEnd
        // (no recoil slide) combined with !_hitPlayer.
        var duration = _hitPlayer ? HitStunDuration
            : _recoilStart == _recoilEnd ? DamageStunDuration
            : MissRecoveryDuration;
        _stateTimer += dt;
        var t = Math.Clamp(_stateTimer / duration, 0f, 1f);

        // Lerp position from recoil start to recoil end.
        _position = Vector2.Lerp(_recoilStart, _recoilEnd, t);

        if (_stateTimer >= duration)
        {
            _state = GnomeState.Chasing;
            _stateTimer = 0f;
        }
    }

    private void EnterStunned(bool hitPlayer)
    {
        _hitPlayer = hitPlayer;
        if (hitPlayer)
            _justHitPlayer = true;
        _state = GnomeState.Stunned;
        _stateTimer = 0f;
        _recoilStart = _position;

        // Recoil away from lunge direction.
        var recoilDist = hitPlayer ? RecoilDistance : RecoilDistance * 0.4f;
        _recoilEnd = _position - _lungeDirection * recoilDist;
    }

    private void UpdateDying(float dt)
    {
        _stateTimer += dt;
        if (_stateTimer >= DyingDuration)
            _isDead = true;
    }

    private static Color MultiplyColor(Color a, Color b)
    {
        return new Color(
            (a.R * b.R) / 255,
            (a.G * b.G) / 255,
            (a.B * b.B) / 255,
            (a.A * b.A) / 255);
    }
}
