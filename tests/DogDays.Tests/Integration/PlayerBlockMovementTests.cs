using Microsoft.Xna.Framework;
using DogDays.Game.Entities;
using DogDays.Game.Input;
using DogDays.Game.World;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Integration;

/// <summary>
/// Integration-style movement tests that simulate frame updates with scripted input.
/// </summary>
public class PlayerBlockMovementTests
{
    private static readonly IMapCollisionData NoBlockedTiles = new NoCollisionData();

    [Fact]
    public void Update__HeldMoveRightAcrossThreeFrames__AccumulatesPosition()
    {
        var input = new FakeInputManager();
        var player = new PlayerBlock(
            startPosition: new Vector2(0f, 0f),
            size: new Point(32, 32),
            moveSpeedPixelsPerSecond: 120f,
            worldBounds: new Rectangle(0, 0, 1024, 640));

        input.Press(InputAction.MoveRight);

        player.Update(FakeGameTime.OneFrame(), input, NoBlockedTiles);
        input.Update();
        player.Update(FakeGameTime.OneFrame(), input, NoBlockedTiles);
        input.Update();
        player.Update(FakeGameTime.OneFrame(), input, NoBlockedTiles);

        Assert.Equal(6f, player.Position.X, 4);
        Assert.Equal(0f, player.Position.Y);
    }

    [Fact]
    public void Update__ReleaseMovementAfterOneFrame__StopsFurtherMotion()
    {
        var input = new FakeInputManager();
        var player = new PlayerBlock(
            startPosition: new Vector2(0f, 0f),
            size: new Point(32, 32),
            moveSpeedPixelsPerSecond: 120f,
            worldBounds: new Rectangle(0, 0, 1024, 640));

        input.Press(InputAction.MoveRight);
        player.Update(FakeGameTime.OneFrame(), input, NoBlockedTiles);

        input.Release(InputAction.MoveRight);
        input.Update();
        player.Update(FakeGameTime.OneFrame(), input, NoBlockedTiles);

        Assert.Equal(2f, player.Position.X, 4);
        Assert.Equal(0f, player.Position.Y);
    }

    [Fact]
    public void Update__WithAccelerationRamp__AcceleratesOverMultipleFrames()
    {
        var input = new FakeInputManager();
        var player = new PlayerBlock(
            startPosition: new Vector2(0f, 0f),
            size: new Point(32, 32),
            moveSpeedPixelsPerSecond: 120f,
            worldBounds: new Rectangle(0, 0, 1024, 640),
            accelerationRate: 10f);

        input.Press(InputAction.MoveRight);

        player.Update(FakeGameTime.OneFrame(), input, NoBlockedTiles);
        var posAfterFrame1 = player.Position.X;

        input.Update();
        player.Update(FakeGameTime.OneFrame(), input, NoBlockedTiles);
        var distFrame2 = player.Position.X - posAfterFrame1;

        // Second frame should cover more distance than first (accelerating).
        Assert.True(distFrame2 > posAfterFrame1);
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
