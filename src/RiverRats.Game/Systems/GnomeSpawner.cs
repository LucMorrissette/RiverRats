using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Entities;

namespace RiverRats.Game.Systems;

/// <summary>
/// Spawns and manages a pool of <see cref="GnomeEnemy"/> instances around the camera edges.
/// Handles initial batch spawn, timed trickle spawning, per-frame updates, and culling
/// gnomes that wander too far from the player.
/// </summary>
internal sealed class GnomeSpawner
{
    private const float SpawnOffsetMin = 40f;
    private const float SpawnOffsetMax = 60f;
    private const float CullDistanceSquared = 500f * 500f;

    private readonly int _initialCount;
    private readonly float _spawnIntervalSeconds;
    private readonly int _maxActive;
    private readonly Random _rng;
    private readonly List<GnomeEnemy> _gnomes;

    private float _spawnTimer;
    private bool _initialSpawnDone;

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
    }

    /// <summary>Read-only access to active gnomes for external draw loops.</summary>
    internal IReadOnlyList<GnomeEnemy> Gnomes => _gnomes;

    /// <summary>
    /// Updates all gnomes, spawns new ones on schedule, and culls distant gnomes.
    /// </summary>
    /// <param name="gameTime">Current frame timing.</param>
    /// <param name="playerPosition">Player centre position used as the gnome target.</param>
    /// <param name="cameraWorldBounds">Camera visible area used to place spawns off-screen.</param>
    public void Update(GameTime gameTime, Vector2 playerPosition, Rectangle cameraWorldBounds)
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

        // Trickle spawn.
        _spawnTimer += dt;
        if (_spawnTimer >= _spawnIntervalSeconds && _gnomes.Count < _maxActive)
        {
            _spawnTimer -= _spawnIntervalSeconds;
            _gnomes.Add(CreateGnome(cameraWorldBounds));
        }

        // Update all gnomes.
        for (var i = 0; i < _gnomes.Count; i++)
        {
            _gnomes[i].Update(gameTime, playerPosition);
        }

        // Cull gnomes that are too far from the player (iterate backwards for safe removal).
        for (var i = _gnomes.Count - 1; i >= 0; i--)
        {
            var diff = _gnomes[i].Position - playerPosition;
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
