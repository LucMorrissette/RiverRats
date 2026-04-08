using Microsoft.Xna.Framework;
using DogDays.Game.Entities;
using DogDays.Game.Input;
using DogDays.Game.World;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Integration;

/// <summary>
/// Integration-style movement tests covering blocking world obstacles.
/// </summary>
public class PlayerBlockObstacleCollisionTests
{
    [Fact]
    public void Update__MovingIntoBoulderObstacle__StopsBeforeOverlap()
    {
        var input = new FakeInputManager();
        var player = new PlayerBlock(
            startPosition: new Vector2(0f, 0f),
            size: new Point(32, 32),
            moveSpeedPixelsPerSecond: 120f,
            worldBounds: new Rectangle(0, 0, 1024, 640));
        var obstacleBounds = new Rectangle(40, 0, 32, 32);
        var collisionMap = new WorldCollisionMap(
            new NoCollisionData(),
            new[]
            {
                obstacleBounds
            });

        input.Press(InputAction.MoveRight);

        for (var i = 0; i < 10; i++)
        {
            player.Update(FakeGameTime.OneFrame(), input, collisionMap);
            input.Update();
        }

        Assert.Equal(obstacleBounds.Left, player.FootBounds.Right);
        Assert.False(player.FootBounds.Intersects(obstacleBounds));
    }

    [Fact]
    public void Update__MovingIntoSubTileColliderBound__StopsBeforeOverlap()
    {
        var input = new FakeInputManager();
        var player = new PlayerBlock(
            startPosition: new Vector2(50f, 100f),
            size: new Point(32, 32),
            moveSpeedPixelsPerSecond: 120f,
            worldBounds: new Rectangle(0, 0, 1024, 640));
        var colliderBound = new Rectangle(100, 100, 30, 20);
        var collisionMap = new WorldCollisionMap(
            new NoCollisionData(),
            new[]
            {
                colliderBound
            });

        input.Press(InputAction.MoveRight);

        for (var i = 0; i < 10; i++)
        {
            player.Update(FakeGameTime.OneFrame(), input, collisionMap);
            input.Update();
        }

        Assert.False(player.FootBounds.Intersects(colliderBound));
    }

    private sealed class NoCollisionData : IMapCollisionData
    {
        public bool IsWorldRectangleBlocked(Rectangle worldBounds)
        {
            _ = worldBounds;
            return false;
        }
    }
}