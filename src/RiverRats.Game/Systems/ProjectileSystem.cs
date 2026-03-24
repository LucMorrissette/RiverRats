using System.Collections.Generic;
using Microsoft.Xna.Framework;
using RiverRats.Game.Entities;

namespace RiverRats.Game.Systems;

/// <summary>
/// Manages a fixed pool of <see cref="Projectile"/> instances and auto-fire logic
/// for two shooters (player and follower). Handles firing, movement, and
/// projectile-vs-gnome collision.
/// </summary>
internal sealed class ProjectileSystem
{
    private const float ProjectileSpeed = 200f;
    private const float FiringRange = 200f;
    private const float FiringRangeSquared = FiringRange * FiringRange;

    private readonly Projectile[] _pool;
    private readonly float _fireInterval;
    private float _playerCooldown;
    private float _followerCooldown;

    /// <summary>
    /// Creates a projectile system with a pre-allocated pool.
    /// </summary>
    /// <param name="maxProjectiles">Maximum number of simultaneous projectiles.</param>
    /// <param name="fireIntervalSeconds">Seconds between auto-fire shots per shooter.</param>
    public ProjectileSystem(int maxProjectiles, float fireIntervalSeconds)
    {
        _pool = new Projectile[maxProjectiles];
        for (var i = 0; i < maxProjectiles; i++)
            _pool[i] = new Projectile();

        _fireInterval = fireIntervalSeconds;
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
    public void Update(GameTime gameTime, Vector2 playerCenter, Vector2 followerCenter, GnomeSpawner gnomeSpawner)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var gnomes = gnomeSpawner.Gnomes;

        // Tick cooldowns and fire.
        _playerCooldown -= dt;
        if (_playerCooldown <= 0f && gnomes.Count > 0)
        {
            var idx = FindNearestGnomeInRange(playerCenter, gnomes);
            if (idx >= 0)
            {
                Fire(playerCenter, GnomeCenter(gnomes[idx]));
                _playerCooldown += _fireInterval;
            }
        }

        _followerCooldown -= dt;
        if (_followerCooldown <= 0f && gnomes.Count > 0)
        {
            var idx = FindNearestGnomeInRange(followerCenter, gnomes);
            if (idx >= 0)
            {
                Fire(followerCenter, GnomeCenter(gnomes[idx]));
                _followerCooldown += _fireInterval;
            }
        }

        // Move all alive projectiles.
        for (var i = 0; i < _pool.Length; i++)
            _pool[i].Update(gameTime);

        // Collision: projectile vs gnome.
        for (var i = 0; i < _pool.Length; i++)
        {
            if (!_pool[i].IsAlive) continue;

            for (var g = gnomes.Count - 1; g >= 0; g--)
            {
                if (_pool[i].Bounds.Intersects(gnomes[g].Bounds))
                {
                    _pool[i].Kill();
                    gnomeSpawner.RemoveAt(g);
                    break;
                }
            }
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

    private void Fire(Vector2 origin, Vector2 targetCenter)
    {
        for (var i = 0; i < _pool.Length; i++)
        {
            if (!_pool[i].IsAlive)
            {
                var direction = targetCenter - origin;
                if (direction.LengthSquared() > 0f)
                    direction.Normalize();
                _pool[i].Fire(origin, direction * ProjectileSpeed);
                return;
            }
        }
    }
}
