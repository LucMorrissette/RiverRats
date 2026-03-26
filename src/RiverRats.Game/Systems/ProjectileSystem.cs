using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using RiverRats.Game.Data;
using RiverRats.Game.Entities;
using RiverRats.Game.World;

#nullable enable

namespace RiverRats.Game.Systems;

/// <summary>
/// Manages a fixed pool of <see cref="Projectile"/> instances and auto-fire logic
/// for two shooters (player and follower). Handles firing, movement, and
/// projectile-vs-gnome collision.
/// </summary>
internal sealed class ProjectileSystem
{
    internal const float ProjectileSpeed = 320f;
    internal const int ProjectilePierceCount = 3;
    private const int TrailParticlesPerFrame = 1;
    private const float FiringRange = 200f;
    private const float FiringRangeSquared = FiringRange * FiringRange;

    private readonly Projectile[] _pool;
    private readonly float _fireInterval;
    private readonly ParticleManager? _trailParticleManager;
    private readonly ParticleProfile? _trailParticleProfile;
    private float _playerCooldown;
    private float _followerCooldown;

    /// <summary>
    /// Creates a projectile system with a pre-allocated pool.
    /// </summary>
    /// <param name="maxProjectiles">Maximum number of simultaneous projectiles.</param>
    /// <param name="fireIntervalSeconds">Seconds between auto-fire shots per shooter.</param>
    /// <param name="trailParticleManager">Optional particle manager for projectile trail sparks.</param>
    /// <param name="trailParticleProfile">Optional particle profile for projectile trail sparks.</param>
    public ProjectileSystem(
        int maxProjectiles,
        float fireIntervalSeconds,
        ParticleManager? trailParticleManager = null,
        ParticleProfile? trailParticleProfile = null)
    {
        _pool = new Projectile[maxProjectiles];
        for (var i = 0; i < maxProjectiles; i++)
            _pool[i] = new Projectile();

        _fireInterval = fireIntervalSeconds;
        _trailParticleManager = trailParticleManager;
        _trailParticleProfile = trailParticleProfile;
        _playerCooldown = 0f;
        _followerCooldown = fireIntervalSeconds * 0.5f; // offset so shooters alternate
    }

    /// <summary>Direct access to the projectile pool for external draw loops.</summary>
    internal Projectile[] Projectiles => _pool;

    /// <summary>
    /// Ticks cooldowns, auto-fires at nearest gnome, moves projectiles, and resolves collisions.
    /// </summary>
    /// <param name="gameTime">Current frame timing.</param>
    /// <param name="playerCenter">Player centre position (fire origin).</param>
    /// <param name="followerCenter">Follower centre position (fire origin).</param>
    /// <param name="gnomeSpawner">Gnome spawner to query targets and remove hit gnomes.</param>
    /// <param name="collisionMap">World collision data — projectiles die on impact with obstacles.</param>
    public void Update(GameTime gameTime, Vector2 playerCenter, Vector2 followerCenter, GnomeSpawner gnomeSpawner, IMapCollisionData collisionMap)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var gnomes = gnomeSpawner.Gnomes;

        // Tick cooldowns and fire.
        _playerCooldown -= dt;
        if (_playerCooldown <= 0f && gnomes.Count > 0)
        {
            var idx = FindNearestGnomeInRange(playerCenter, gnomes);
            if (idx >= 0 && TryFireProjectile(playerCenter, GnomeCenter(gnomes[idx])))
            {
                _playerCooldown += _fireInterval;
            }
        }

        _followerCooldown -= dt;
        if (_followerCooldown <= 0f && gnomes.Count > 0)
        {
            var idx = FindNearestGnomeInRange(followerCenter, gnomes);
            if (idx >= 0 && TryFireProjectile(followerCenter, GnomeCenter(gnomes[idx])))
            {
                _followerCooldown += _fireInterval;
            }
        }

        // Move all alive projectiles.
        for (var i = 0; i < _pool.Length; i++)
        {
            _pool[i].Update(gameTime);

            EmitTrailParticles(_pool[i]);

            // Kill projectile on world obstacle hit.
            if (_pool[i].IsAlive && collisionMap.IsWorldRectangleBlocked(_pool[i].Bounds))
                _pool[i].Kill();
        }

        // Collision: projectile vs gnome.
        for (var i = 0; i < _pool.Length; i++)
        {
            if (!_pool[i].IsAlive) continue;

            ResolveProjectileHits(_pool[i], gnomes);
        }
    }

    internal static void ResolveProjectileHits(Projectile projectile, IReadOnlyList<GnomeEnemy> gnomes)
    {
        if (!projectile.IsAlive)
            return;

        for (var g = gnomes.Count - 1; g >= 0; g--)
        {
            if (gnomes[g].State == GnomeState.Dying)
                continue;

            if (!projectile.Bounds.Intersects(gnomes[g].Bounds))
                continue;

            gnomes[g].Die();
            if (!projectile.RegisterHit())
                break;
        }
    }

    private static Vector2 GnomeCenter(GnomeEnemy gnome)
    {
        var b = gnome.Bounds;
        return new Vector2(b.X + b.Width * 0.5f, b.Y + b.Height * 0.5f);
    }

    private static int FindNearestGnomeInRange(Vector2 origin, IReadOnlyList<GnomeEnemy> gnomes)
    {
        var bestIndex = -1;
        var bestDistSq = FiringRangeSquared;
        for (var i = 0; i < gnomes.Count; i++)
        {
            if (gnomes[i].State == GnomeState.Dying)
                continue;

            var diff = gnomes[i].Position - origin;
            var distSq = diff.LengthSquared();
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    private void EmitTrailParticles(Projectile projectile)
    {
        if (!projectile.IsAlive || _trailParticleManager is null || _trailParticleProfile is null)
            return;

        _trailParticleManager.Emit(
            _trailParticleProfile,
            projectile.Position,
            TrailParticlesPerFrame,
            projectile.Rotation + MathHelper.PiOver2);
    }

    internal bool TryFireProjectile(Vector2 origin, Vector2 targetCenter)
    {
        for (var i = 0; i < _pool.Length; i++)
        {
            if (!_pool[i].IsAlive)
            {
                var direction = targetCenter - origin;
                var lengthSquared = direction.LengthSquared();
                if (lengthSquared <= 0f)
                    return false;

                direction *= 1f / MathF.Sqrt(lengthSquared);
                _pool[i].Fire(origin, direction * ProjectileSpeed, ProjectilePierceCount);
                return true;
            }
        }

        return false;
    }
}
