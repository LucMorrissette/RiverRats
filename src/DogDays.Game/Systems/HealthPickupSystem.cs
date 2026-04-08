using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DogDays.Components;
using DogDays.Game.Entities;
using DogDays.Game.World;

namespace DogDays.Game.Systems;

/// <summary>
/// Manages spawning, updating, pickup detection, and drawing for the health pickup pool
/// used in the forest survival minigame. Delegates per-pickup lifecycle to
/// <see cref="HealthPickup"/>. Extracted from <c>GameplayScreen</c>.
/// </summary>
public sealed class HealthPickupSystem
{
    private const float SpawnIntervalMin = 15f;
    private const float SpawnIntervalMax = 25f;
    private const float CollectionRadiusSq = 16f * 16f;
    private const int DrawSize = 10;
    private const float SpawnRadiusMin = 150f;
    private const float SpawnRadiusMax = 250f;
    private const int SpawnAttempts = 10;
    private const int CollisionSize = 16;

    private readonly HealthPickup[] _pickups;
    private readonly int _maxPickups;
    private float _spawnTimer;
    private float _nextInterval;

    /// <summary>Creates the system with a fixed pool capacity.</summary>
    public HealthPickupSystem(int maxPickups, Random rng)
    {
        _maxPickups = maxPickups;
        _pickups = new HealthPickup[maxPickups];
        for (var i = 0; i < maxPickups; i++)
            _pickups[i] = new HealthPickup();

        _nextInterval = SpawnIntervalMin
            + (float)rng.NextDouble() * (SpawnIntervalMax - SpawnIntervalMin);
    }

    /// <summary>
    /// Updates spawn timer, per-pickup aging, and player collection.
    /// </summary>
    /// <param name="dt">Delta time in seconds.</param>
    /// <param name="playerCenter">Player centre in world space.</param>
    /// <param name="playerHealth">Player health component — healed on pickup; null = no-op.</param>
    /// <param name="collisionMap">Collision map used to find valid spawn positions.</param>
    /// <param name="mapPixelWidth">Map width in pixels (used for spawn bounds check).</param>
    /// <param name="mapPixelHeight">Map height in pixels (used for spawn bounds check).</param>
    /// <param name="rng">Random source for spawn position and interval.</param>
    public void Update(
        float dt,
        Vector2 playerCenter,
        Health playerHealth,
        WorldCollisionMap collisionMap,
        int mapPixelWidth,
        int mapPixelHeight,
        Random rng)
    {
        if (playerHealth == null || !playerHealth.IsAlive)
            return;

        _spawnTimer += dt;
        if (_spawnTimer >= _nextInterval)
        {
            _spawnTimer = 0f;
            _nextInterval = SpawnIntervalMin
                + (float)rng.NextDouble() * (SpawnIntervalMax - SpawnIntervalMin);

            var freeSlot = -1;
            for (var i = 0; i < _maxPickups; i++)
            {
                if (!_pickups[i].IsActive)
                {
                    freeSlot = i;
                    break;
                }
            }

            if (freeSlot >= 0)
            {
                var spawnPos = TryFindWalkablePosition(
                    playerCenter, collisionMap, mapPixelWidth, mapPixelHeight, rng);
                if (spawnPos.HasValue)
                    _pickups[freeSlot].Spawn(spawnPos.Value);
            }
        }

        for (var i = 0; i < _maxPickups; i++)
        {
            _pickups[i].Update(dt);

            if (!_pickups[i].IsActive)
                continue;

            var toPlayer = playerCenter - _pickups[i].Position;
            if (toPlayer.LengthSquared() <= CollectionRadiusSq)
            {
                _pickups[i].Deactivate();
                playerHealth.Heal(1);
            }
        }
    }

    /// <summary>
    /// Draws all active health pickups. Assumes the sprite batch is already begun.
    /// </summary>
    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D pixelTexture,
        float mapHeight,
        float mapWidth,
        float playerDepth,
        EntityDepthFilter filter)
    {
        var halfSize = DrawSize / 2;
        for (var i = 0; i < _maxPickups; i++)
        {
            if (!_pickups[i].IsActive)
                continue;

            var pos = _pickups[i].Position;
            var sortBounds = new Rectangle(
                (int)(pos.X - halfSize), (int)(pos.Y - halfSize),
                DrawSize, DrawSize);
            var depth = SortDepth(sortBounds, mapHeight, mapWidth);
            if (!PassesDepthFilter(depth, playerDepth, filter))
                continue;

            spriteBatch.Draw(
                pixelTexture,
                sortBounds,
                null,
                Color.LimeGreen * _pickups[i].Opacity,
                0f,
                Vector2.Zero,
                SpriteEffects.None,
                depth);
        }
    }

    private Vector2? TryFindWalkablePosition(
        Vector2 center,
        WorldCollisionMap collisionMap,
        int mapPixelWidth,
        int mapPixelHeight,
        Random rng)
    {
        for (var attempt = 0; attempt < SpawnAttempts; attempt++)
        {
            var angle = (float)(rng.NextDouble() * MathHelper.TwoPi);
            var radius = SpawnRadiusMin + (float)rng.NextDouble() * (SpawnRadiusMax - SpawnRadiusMin);
            var candidate = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            var bounds = new Rectangle(
                (int)candidate.X - CollisionSize / 2,
                (int)candidate.Y - CollisionSize / 2,
                CollisionSize,
                CollisionSize);
            var worldBounds = new Rectangle(0, 0, mapPixelWidth, mapPixelHeight);
            if (worldBounds.Contains(bounds) && !collisionMap.IsWorldRectangleBlocked(bounds))
                return candidate;
        }

        return null;
    }

    private static float SortDepth(Rectangle bounds, float mapHeight, float mapWidth, float anchorOffset = 0f)
    {
        var yDepth = (bounds.Bottom - anchorOffset) / mapHeight;
        var tieBreakerRange = 1f / mapHeight;
        var yScaled = yDepth * (1f - tieBreakerRange);
        var xTie = bounds.Left / (mapWidth * mapHeight);
        return MathHelper.Clamp(yScaled + xTie, 0f, 0.9999f);
    }

    private static bool PassesDepthFilter(float depth, float playerDepth, EntityDepthFilter filter)
    {
        return filter switch
        {
            EntityDepthFilter.BehindOrAtPlayer => depth <= playerDepth,
            EntityDepthFilter.InFrontOfPlayer => depth > playerDepth,
            _ => true
        };
    }
}
