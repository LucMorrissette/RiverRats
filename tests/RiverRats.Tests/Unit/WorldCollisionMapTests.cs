using Microsoft.Xna.Framework;
using RiverRats.Game.World;

namespace RiverRats.Tests.Unit;

/// <summary>
/// Unit tests for combined terrain and obstacle collision queries.
/// </summary>
public class WorldCollisionMapTests
{
    [Fact]
    public void IsWorldRectangleBlocked__OverlapsStaticObstacle__ReturnsTrue()
    {
        var collisionMap = new WorldCollisionMap(
            new NoCollisionData(),
            new[]
            {
                new Rectangle(100, 100, 32, 32)
            });

        var blocked = collisionMap.IsWorldRectangleBlocked(new Rectangle(120, 120, 16, 16));

        Assert.True(blocked);
    }

    [Fact]
    public void IsWorldRectangleBlocked__TerrainBlockedEvenWithoutObstacleOverlap__ReturnsTrue()
    {
        var collisionMap = new WorldCollisionMap(
            new AlwaysBlockedCollisionData(),
            new[]
            {
                new Rectangle(100, 100, 32, 32)
            });

        var blocked = collisionMap.IsWorldRectangleBlocked(new Rectangle(0, 0, 16, 16));

        Assert.True(blocked);
    }

    [Fact]
    public void IsWorldRectangleBlocked__NoTerrainOrObstacleOverlap__ReturnsFalse()
    {
        var collisionMap = new WorldCollisionMap(
            new NoCollisionData(),
            new[]
            {
                new Rectangle(100, 100, 32, 32)
            });

        var blocked = collisionMap.IsWorldRectangleBlocked(new Rectangle(0, 0, 16, 16));

        Assert.False(blocked);
    }

    private sealed class NoCollisionData : IMapCollisionData
    {
        public bool IsWorldRectangleBlocked(Rectangle worldBounds)
        {
            _ = worldBounds;
            return false;
        }
    }

    private sealed class AlwaysBlockedCollisionData : IMapCollisionData
    {
        public bool IsWorldRectangleBlocked(Rectangle worldBounds)
        {
            _ = worldBounds;
            return true;
        }
    }
}