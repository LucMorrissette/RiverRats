using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Entities;
using RiverRats.Game.World;

namespace RiverRats.Game.Systems;

/// <summary>
/// Spawns and manages a pool of <see cref="GnomeEnemy"/> instances around the camera edges.
/// Handles initial batch spawn, timed trickle spawning, per-frame updates, culling
/// gnomes that wander too far from the player, and sweeping dead gnomes after their
/// death animation completes.
/// </summary>
internal sealed class GnomeSpawner
{
    private const float SpawnOffsetMin = 40f;
    private const float SpawnOffsetMax = 60f;
    private const float CullDistanceSquared = 500f * 500f;
    private const float SeparationRadius = 33f;
    private const float SeparationRadiusSq = SeparationRadius * SeparationRadius;
    private const float SeparationWeight = 1.2f;
    private const float CrowdingSlowdownFactor = 0.15f;

    private readonly int _initialCount;
    private readonly float _spawnIntervalSeconds;
    private readonly int _maxActive;
    private readonly Random _rng;
    private readonly List<GnomeEnemy> _gnomes;
    private Vector2[] _separationVectors;
    private int[] _neighborCounts;

    private float _spawnTimer;
    private bool _initialSpawnDone;

    /// <summary>
    /// Callback invoked when a gnome finishes its death animation and is removed.
    /// Receives the centre position of the dead gnome for spawning effects.
    /// </summary>
    internal Action<Vector2> OnGnomeDied { get; set; } = _ => { };

    /// <summary>
    /// Callback invoked when a gnome's lunge hits the player.
    /// </summary>
    internal Action OnPlayerHit { get; set; } = () => { };

    /// <summary>
    /// Creates a gnome spawner.
    /// </summary>
    /// <param name="initialCount">Number of gnomes to spawn on the first update call.</param>
    /// <param name="spawnIntervalSeconds">Seconds between trickle spawns after the initial batch.</param>
    /// <param name="maxActive">Maximum number of active gnomes at any time.</param>
    public GnomeSpawner(int initialCount, float spawnIntervalSeconds, int maxActive)
    {
        _initialCount = initialCount;
        _spawnIntervalSeconds = spawnIntervalSeconds;
        _maxActive = maxActive;
        _rng = new Random(42);
        _gnomes = new List<GnomeEnemy>(maxActive);
        _separationVectors = new Vector2[maxActive];
        _neighborCounts = new int[maxActive];
    }

    /// <summary>Read-only access to active gnomes for external draw loops.</summary>
    internal IReadOnlyList<GnomeEnemy> Gnomes => _gnomes;

    /// <summary>
    /// Updates all gnomes, spawns new ones on schedule, and culls distant gnomes.
    /// </summary>
    /// <param name="gameTime">Current frame timing.</param>
    /// <param name="playerPosition">Player centre position used as the gnome target.</param>
    /// <param name="playerBounds">Player bounding rectangle for lunge hit detection.</param>
    /// <param name="cameraWorldBounds">Camera visible area used to place spawns off-screen.</param>
    /// <param name="flowField">BFS flow field directing gnomes toward the player.</param>
    /// <param name="collisionMap">World collision data for fine-grained obstacle queries.</param>
    public void Update(GameTime gameTime, Vector2 playerPosition, Rectangle playerBounds, Rectangle cameraWorldBounds, FlowField flowField, IMapCollisionData collisionMap)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Initial batch spawn on first call.
        if (!_initialSpawnDone)
        {
            _initialSpawnDone = true;
            var count = Math.Min(_initialCount, _maxActive);
            for (var i = 0; i < count; i++)
            {
                _gnomes.Add(CreateGnome(cameraWorldBounds));
            }
        }

        // Trickle spawn (batch of up to 3 per interval).
        _spawnTimer += dt;
        if (_spawnTimer >= _spawnIntervalSeconds && _gnomes.Count < _maxActive)
        {
            _spawnTimer -= _spawnIntervalSeconds;
            var batchSize = Math.Min(3, _maxActive - _gnomes.Count);
            for (var i = 0; i < batchSize; i++)
                _gnomes.Add(CreateGnome(cameraWorldBounds));
        }

        // Compute separation vectors (O(n²) — acceptable for small enemy counts).
        ComputeSeparation();

        // Update all gnomes with their separation steering hint and crowding slowdown.
        for (var i = 0; i < _gnomes.Count; i++)
        {
            var crowdingSpeed = 1f / (1f + _neighborCounts[i] * CrowdingSlowdownFactor);
            _gnomes[i].Update(gameTime, playerPosition, flowField, collisionMap, _separationVectors[i], playerBounds, crowdingSpeed);
            if (_gnomes[i].JustHitPlayer)
                OnPlayerHit();
        }

        // Cull gnomes that are too far from the player (iterate backwards for safe removal).
        for (var i = _gnomes.Count - 1; i >= 0; i--)
        {
            var gnome = _gnomes[i];

            // Sweep dead gnomes (death animation finished).
            if (gnome.IsDead)
            {
                var b = gnome.Bounds;
                OnGnomeDied(new Vector2(b.X + b.Width * 0.5f, b.Y + b.Height * 0.5f));
                _gnomes.RemoveAt(i);
                continue;
            }

            var diff = gnome.Position - playerPosition;
            if (diff.LengthSquared() > CullDistanceSquared)
            {
                _gnomes.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Removes the gnome at the specified index. Used by combat systems on hit.
    /// </summary>
    internal void RemoveAt(int index)
    {
        _gnomes.RemoveAt(index);
    }

    private void ComputeSeparation()
    {
        var count = _gnomes.Count;

        // Grow scratch buffers if needed.
        if (_separationVectors.Length < count)
        {
            _separationVectors = new Vector2[count];
            _neighborCounts = new int[count];
        }

        Array.Clear(_separationVectors, 0, count);
        Array.Clear(_neighborCounts, 0, count);

        for (var i = 0; i < count; i++)
        {
            var posI = _gnomes[i].Position;
            for (var j = i + 1; j < count; j++)
            {
                var posJ = _gnomes[j].Position;
                var dx = posI.X - posJ.X;
                var dy = posI.Y - posJ.Y;
                var distSq = dx * dx + dy * dy;

                if (distSq >= SeparationRadiusSq || distSq < 0.0001f)
                    continue;

                _neighborCounts[i]++;
                _neighborCounts[j]++;

                // Linear falloff: full strength at overlap, zero at radius edge.
                var dist = MathF.Sqrt(distSq);
                var strength = (1f - dist / SeparationRadius) * SeparationWeight;
                var invDist = 1f / dist;
                var force = new Vector2(dx * invDist * strength, dy * invDist * strength);

                _separationVectors[i] += force;
                _separationVectors[j] -= force;
            }
        }
    }

    private GnomeEnemy CreateGnome(Rectangle cameraBounds)
    {
        var pos = PickOffscreenPosition(cameraBounds);
        var phase = (float)_rng.NextDouble();
        return new GnomeEnemy(pos, phase);
    }

    private Vector2 PickOffscreenPosition(Rectangle cameraBounds)
    {
        var offset = SpawnOffsetMin + (float)_rng.NextDouble() * (SpawnOffsetMax - SpawnOffsetMin);

        // Pick a random edge: 0=top, 1=bottom, 2=left, 3=right.
        var edge = _rng.Next(4);
        return edge switch
        {
            0 => new Vector2(
                cameraBounds.Left + (float)_rng.NextDouble() * cameraBounds.Width,
                cameraBounds.Top - offset),
            1 => new Vector2(
                cameraBounds.Left + (float)_rng.NextDouble() * cameraBounds.Width,
                cameraBounds.Bottom + offset),
            2 => new Vector2(
                cameraBounds.Left - offset,
                cameraBounds.Top + (float)_rng.NextDouble() * cameraBounds.Height),
            _ => new Vector2(
                cameraBounds.Right + offset,
                cameraBounds.Top + (float)_rng.NextDouble() * cameraBounds.Height),
        };
    }
}
