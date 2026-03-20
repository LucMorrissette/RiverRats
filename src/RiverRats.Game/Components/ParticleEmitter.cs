using System;
using Microsoft.Xna.Framework;
using RiverRats.Game.Data;
using RiverRats.Game.Systems;

namespace RiverRats.Game.Components;

/// <summary>
/// A component that can be attached to entities to spawn particles.
/// Manages the timing and frequency of emission.
/// </summary>
public class ParticleEmitter
{
    private readonly ParticleManager _particleManager;
    private readonly ParticleProfile _profile;
    private float _accumulatedTime;

    /// <summary>Gets or sets whether the emitter is currently active.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Creates a new ParticleEmitter.</summary>
    /// <param name="manager">The manager to emit particles into.</param>
    /// <param name="profile">The configuration for the particles.</param>
    public ParticleEmitter(ParticleManager manager, ParticleProfile profile)
    {
        _particleManager = manager ?? throw new ArgumentNullException(nameof(manager));
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    /// <summary>Updates the emitter and spawns particles based on the spawn rate.</summary>
    /// <param name="gameTime">Snapshot of timing values.</param>
    /// <param name="position">The current position to spawn at.</param>
    public void Update(GameTime gameTime, Vector2 position)
    {
        if (!IsEnabled) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _accumulatedTime += dt;

        float timePerParticle = 1f / _profile.SpawnRate;

        while (_accumulatedTime >= timePerParticle)
        {
            _accumulatedTime -= timePerParticle;
            _particleManager.Emit(_profile, position, 1);
        }
    }
}
