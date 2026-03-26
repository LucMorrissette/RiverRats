using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Data;

namespace RiverRats.Game.Systems;

/// <summary>
/// Manages a pool of particles and handles their update and draw cycles.
/// Designed for zero-allocation performance in the hot loop.
/// </summary>
public sealed class ParticleManager
{
    private readonly Particle[] _particles;
    private readonly int[] _freeIndices;
    private int _freeCount;
    private readonly int _maxParticles;
    private readonly Random _rng = new(); // Pre-allocated to avoid per-Emit allocation

    /// <summary>Gets the number of active particles in the pool.</summary>
    public int ActiveCount => _maxParticles - _freeCount;

    /// <summary>Creates a new ParticleManager with a pre-allocated pool.</summary>
    /// <param name="maxParticles">The maximum number of particles to allow.</param>
    public ParticleManager(int maxParticles = 512)
    {
        _maxParticles = maxParticles;
        _particles = new Particle[_maxParticles];
        _freeIndices = new int[_maxParticles];
        _freeCount = _maxParticles;

        for (int i = 0; i < _maxParticles; i++)
        {
            _freeIndices[i] = i;
            _particles[i].IsActive = false;
        }
    }

    /// <summary>Updates all active particles.</summary>
    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        for (int i = 0; i < _maxParticles; i++)
        {
            if (!_particles[i].IsActive) continue;

            _particles[i].LifeRemaining -= dt;

            if (_particles[i].LifeRemaining <= 0)
            {
                _particles[i].IsActive = false;
                _freeIndices[_freeCount++] = i;
                continue;
            }

            _particles[i].Velocity.Y += _particles[i].Gravity * dt;
            _particles[i].Position += _particles[i].Velocity * dt;

            if (_particles[i].RemainingBounces >= 0
                && _particles[i].Position.Y >= _particles[i].GroundY
                && _particles[i].Velocity.Y > 0f)
            {
                if (_particles[i].RemainingBounces > 0)
                {
                    _particles[i].Position.Y = _particles[i].GroundY;
                    _particles[i].Velocity.Y = -_particles[i].Velocity.Y * _particles[i].BounceDamping;
                    _particles[i].Velocity.X *= _particles[i].BounceFriction;
                    _particles[i].RemainingBounces--;
                }
                else
                {
                    _particles[i].IsActive = false;
                    _freeIndices[_freeCount++] = i;
                    continue;
                }
            }

            _particles[i].Rotation += _particles[i].AngularVelocity * dt;
        }
    }

    /// <summary>Draws all active particles.</summary>
    /// <param name="spriteBatch">The SpriteBatch to use (must be in an active Begin/End).</param>
    /// <param name="texture">The texture to use for all particles.</param>
    public void Draw(SpriteBatch spriteBatch, Texture2D texture)
    {
        if (texture == null) return;

        Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);

        for (int i = 0; i < _maxParticles; i++)
        {
            if (!_particles[i].IsActive) continue;

            float normalizedLife = _particles[i].LifeRemaining / _particles[i].InitialLife;
            Color color = Color.Lerp(_particles[i].EndColor, _particles[i].StartColor, normalizedLife);

            spriteBatch.Draw(
                texture,
                _particles[i].Position,
                null,
                color,
                _particles[i].Rotation,
                origin,
                _particles[i].Scale,
                SpriteEffects.None,
                0f);
        }
    }

    /// <summary>Emits particles into the world.</summary>
    /// <param name="profile">The configuration for the particles.</param>
    /// <param name="position">Where to spawn them.</param>
    /// <param name="count">How many to spawn.</param>
    public void Emit(ParticleProfile profile, Vector2 position, int count)
    {
        Emit(profile, position, count, 0f);
    }

    /// <summary>
    /// Emits particles with a base emission direction.
    /// The spread angle from the profile is applied around this base direction.
    /// </summary>
    /// <param name="profile">The configuration for the particles.</param>
    /// <param name="position">Where to spawn them.</param>
    /// <param name="count">How many to spawn.</param>
    /// <param name="baseAngleRadians">Base emission angle in radians. 0 = upward (-Y).</param>
    public void Emit(ParticleProfile profile, Vector2 position, int count, float baseAngleRadians)
    {
        for (int i = 0; i < count; i++)
        {
            if (_freeCount <= 0) break;

            int index = _freeIndices[--_freeCount];

            float life = MathHelper.Lerp(profile.MinLife, profile.MaxLife, (float)_rng.NextDouble());
            float speed = MathHelper.Lerp(profile.MinSpeed, profile.MaxSpeed, (float)_rng.NextDouble());
            float angle = baseAngleRadians + MathHelper.Lerp(-profile.SpreadRadians / 2f, profile.SpreadRadians / 2f, (float)_rng.NextDouble());
            float scale = MathHelper.Lerp(profile.MinScale, profile.MaxScale, (float)_rng.NextDouble());
            float groundOffset = MathHelper.Lerp(profile.MinGroundOffset, profile.MaxGroundOffset, (float)_rng.NextDouble());

            Vector2 direction = new Vector2(
                (float)Math.Sin(angle),
                -(float)Math.Cos(angle));

            _particles[index].Position = position;
            _particles[index].Velocity = direction * speed;
            _particles[index].StartColor = profile.StartColor;
            _particles[index].EndColor = profile.EndColor;
            _particles[index].Scale = scale;
            _particles[index].Rotation = (float)_rng.NextDouble() * MathHelper.TwoPi;
            _particles[index].AngularVelocity = MathHelper.Lerp(-2f, 2f, (float)_rng.NextDouble());
            _particles[index].InitialLife = life;
            _particles[index].LifeRemaining = life;
            _particles[index].Gravity = profile.Gravity;
            _particles[index].GroundY = position.Y + groundOffset;
            _particles[index].BounceDamping = profile.BounceDamping;
            _particles[index].BounceFriction = profile.BounceFriction;
            _particles[index].RemainingBounces = profile.MaxGroundBounces;
            _particles[index].IsActive = true;
        }
    }

    /// <summary>Returns a copy of the first active particle for deterministic unit tests.</summary>
    internal bool TryGetFirstActiveParticle(out Particle particle)
    {
        for (int i = 0; i < _maxParticles; i++)
        {
            if (_particles[i].IsActive)
            {
                particle = _particles[i];
                return true;
            }
        }

        particle = default;
        return false;
    }
}
