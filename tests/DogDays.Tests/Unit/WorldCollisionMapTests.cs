using Microsoft.Xna.Framework;
using DogDays.Game.World;

namespace DogDays.Tests.Unit;

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

    [Fact]
    public void IsWorldRectangleBlocked__FullyInsideWalkableOverride__IgnoresTerrainCollision()
    {
        var collisionMap = new WorldCollisionMap(
            new AlwaysBlockedCollisionData(),
            [],
            [new Rectangle(100, 100, 64, 64)]);

        var blocked = collisionMap.IsWorldRectangleBlocked(new Rectangle(112, 112, 32, 32));

        Assert.False(blocked);
    }

    [Fact]
    public void IsWorldRectangleBlocked__PartiallyOverlappingWalkableOverride__IgnoresTerrainCollision()
    {
        var collisionMap = new WorldCollisionMap(
            new AlwaysBlockedCollisionData(),
            [],
            [new Rectangle(100, 100, 64, 64)]);

        var blocked = collisionMap.IsWorldRectangleBlocked(new Rectangle(140, 112, 32, 32));

        Assert.False(blocked);
    }

    [Fact]
    public void IsWorldRectangleBlocked__CompletelyOutsideWalkableOverride__KeepsTerrainBlocked()
    {
        var collisionMap = new WorldCollisionMap(
            new AlwaysBlockedCollisionData(),
            [],
            [new Rectangle(100, 100, 64, 64)]);

        // Rectangle at (200,200) is completely outside the walkable override at (100,100)-(164,164)
        var blocked = collisionMap.IsWorldRectangleBlocked(new Rectangle(200, 200, 32, 32));

        Assert.True(blocked);
    }

    [Fact]
    public void IsWorldRectangleBlocked__InsideWalkableOverrideButOverlappingObstacle__ReturnsTrue()
    {
        var collisionMap = new WorldCollisionMap(
            new AlwaysBlockedCollisionData(),
            [new Rectangle(120, 120, 32, 32)],
            [new Rectangle(100, 100, 64, 64)]);

        var blocked = collisionMap.IsWorldRectangleBlocked(new Rectangle(120, 120, 16, 16));

        Assert.True(blocked);
    }

    [Fact]
    public void IsWorldRectangleBlocked_ReturnsTrueWhenOverlappingColliderBound()
    {
        var collisionMap = new WorldCollisionMap(
            new NoCollisionData(),
            new[]
            {
                new Rectangle(100, 100, 50, 20)
            });

        var blocked = collisionMap.IsWorldRectangleBlocked(new Rectangle(120, 110, 16, 16));

        Assert.True(blocked);
    }

    [Fact]
    public void IsWorldRectangleBlocked_ReturnsFalseWhenNotOverlappingColliderBound()
    {
        var collisionMap = new WorldCollisionMap(
            new NoCollisionData(),
            new[]
            {
                new Rectangle(100, 100, 50, 20)
            });

        var blocked = collisionMap.IsWorldRectangleBlocked(new Rectangle(200, 200, 16, 16));

        Assert.False(blocked);
    }

    [Fact]
    public void IsWorldRectangleBlocked_ReturnsTrueWhenOverlappingAnyOfMultipleColliderBounds()
    {
        var collisionMap = new WorldCollisionMap(
            new NoCollisionData(),
            new[]
            {
                new Rectangle(50, 50, 30, 30),
                new Rectangle(150, 150, 40, 40),
                new Rectangle(300, 200, 25, 25)
            });

        var blocked = collisionMap.IsWorldRectangleBlocked(new Rectangle(160, 160, 16, 16));

        Assert.True(blocked);
    }

    [Fact]
    public void AddDynamicObstacle__BlocksQueryThatOverlaps()
    {
        var collisionMap = new WorldCollisionMap(new NoCollisionData(), []);
        collisionMap.AddDynamicObstacle(new Rectangle(100, 100, 20, 20));

        Assert.True(collisionMap.IsWorldRectangleBlocked(new Rectangle(110, 110, 10, 10)));
    }

    [Fact]
    public void AddDynamicObstacle__DoesNotBlockNonOverlapping()
    {
        var collisionMap = new WorldCollisionMap(new NoCollisionData(), []);
        collisionMap.AddDynamicObstacle(new Rectangle(100, 100, 20, 20));

        Assert.False(collisionMap.IsWorldRectangleBlocked(new Rectangle(200, 200, 10, 10)));
    }

    [Fact]
    public void UpdateDynamicObstacle__MovesBlockingRegion()
    {
        var collisionMap = new WorldCollisionMap(new NoCollisionData(), []);
        var index = collisionMap.AddDynamicObstacle(new Rectangle(100, 100, 20, 20));

        // Move the obstacle away.
        collisionMap.UpdateDynamicObstacle(index, new Rectangle(300, 300, 20, 20));

        Assert.False(collisionMap.IsWorldRectangleBlocked(new Rectangle(110, 110, 10, 10)),
            "Original location should no longer block.");
        Assert.True(collisionMap.IsWorldRectangleBlocked(new Rectangle(310, 310, 10, 10)),
            "New location should block.");
    }

    [Fact]
    public void ClearDynamicObstacles__RemovesAllDynamicBlocking()
    {
        var collisionMap = new WorldCollisionMap(new NoCollisionData(), []);
        collisionMap.AddDynamicObstacle(new Rectangle(100, 100, 20, 20));
        collisionMap.AddDynamicObstacle(new Rectangle(200, 200, 20, 20));

        collisionMap.ClearDynamicObstacles();

        Assert.False(collisionMap.IsWorldRectangleBlocked(new Rectangle(110, 110, 10, 10)));
        Assert.False(collisionMap.IsWorldRectangleBlocked(new Rectangle(210, 210, 10, 10)));
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