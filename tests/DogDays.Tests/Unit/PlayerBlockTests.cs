using Microsoft.Xna.Framework;
using DogDays.Game.Data;
using DogDays.Game.Entities;
using DogDays.Game.Input;
using DogDays.Game.World;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Unit;

/// <summary>
/// Unit tests for PlayerBlock movement math and world-bound clamping.
/// </summary>
public class PlayerBlockTests
{
    private static readonly IMapCollisionData NoBlockedTiles = new DelegateCollisionData(_ => false);

    [Fact]
    public void Update__MoveRightForOneSecond__AdvancesByConfiguredSpeed()
    {
        var input = new FakeInputManager();
        var player = new PlayerBlock(
            startPosition: new Vector2(100f, 100f),
            size: new Point(32, 32),
            moveSpeedPixelsPerSecond: 120f,
            worldBounds: new Rectangle(0, 0, 1024, 640));

        input.Press(InputAction.MoveRight);

        player.Update(FakeGameTime.FromSeconds(1f), input, NoBlockedTiles);

        Assert.Equal(220f, player.Position.X);
        Assert.Equal(100f, player.Position.Y);
    }

    [Fact]
    public void Update__MoveDiagonallyForOneSecond__NormalizesSpeed()
    {
        var input = new FakeInputManager();
        var player = new PlayerBlock(
            startPosition: new Vector2(100f, 100f),
            size: new Point(32, 32),
            moveSpeedPixelsPerSecond: 120f,
            worldBounds: new Rectangle(0, 0, 1024, 640));

        input.Press(InputAction.MoveRight);
        input.Press(InputAction.MoveDown);

        player.Update(FakeGameTime.FromSeconds(1f), input, NoBlockedTiles);

        Assert.Equal(184.85281f, player.Position.X, 4);
        Assert.Equal(184.85281f, player.Position.Y, 4);
    }

    [Fact]
    public void Update__MovePastRightEdge__ClampsToWorldBounds()
    {
        var input = new FakeInputManager();
        var player = new PlayerBlock(
            startPosition: new Vector2(980f, 100f),
            size: new Point(32, 32),
            moveSpeedPixelsPerSecond: 120f,
            worldBounds: new Rectangle(0, 0, 1024, 640));

        input.Press(InputAction.MoveRight);

        player.Update(FakeGameTime.FromSeconds(1f), input, NoBlockedTiles);

        Assert.Equal(992f, player.Position.X);
        Assert.Equal(100f, player.Position.Y);
    }

    [Fact]
    public void Center__FromPositionAndSize__ReturnsMidpoint()
    {
        var player = new PlayerBlock(
            startPosition: new Vector2(100f, 120f),
            size: new Point(32, 32),
            moveSpeedPixelsPerSecond: 120f,
            worldBounds: new Rectangle(0, 0, 1024, 640));

        Assert.Equal(new Vector2(116f, 136f), player.Center);
    }

    [Fact]
    public void Update__MoveIntoBlockedTile__DoesNotEnterBlockedArea()
    {
        var input = new FakeInputManager();
        var blockedRegion = new Rectangle(64, 0, 32, 32);
        var collisionData = new DelegateCollisionData(bounds => bounds.Intersects(blockedRegion));
        var player = new PlayerBlock(
            startPosition: new Vector2(32f, 0f),
            size: new Point(32, 32),
            moveSpeedPixelsPerSecond: 64f,
            worldBounds: new Rectangle(0, 0, 256, 256));

        input.Press(InputAction.MoveRight);
        player.Update(FakeGameTime.FromSeconds(1f), input, collisionData);

        Assert.True(player.FootBounds.Right <= blockedRegion.Left);
        Assert.False(player.FootBounds.Intersects(blockedRegion));
        Assert.Equal(0f, player.Position.Y);
    }

    [Theory]
    [InlineData(InputAction.MoveRight, FacingDirection.Right)]
    [InlineData(InputAction.MoveLeft, FacingDirection.Left)]
    [InlineData(InputAction.MoveUp, FacingDirection.Up)]
    [InlineData(InputAction.MoveDown, FacingDirection.Down)]
    public void Update__SingleDirectionInput__SetsFacingDirection(InputAction action, FacingDirection expected)
    {
        var input = new FakeInputManager();
        var player = new PlayerBlock(
            startPosition: new Vector2(100f, 100f),
            size: new Point(32, 32),
            moveSpeedPixelsPerSecond: 120f,
            worldBounds: new Rectangle(0, 0, 1024, 640));

        input.Press(action);
        player.Update(FakeGameTime.OneFrame(), input, NoBlockedTiles);

        Assert.Equal(expected, player.Facing);
    }

    [Fact]
    public void Update__NoInput__IsMovingFalse()
    {
        var input = new FakeInputManager();
        var player = new PlayerBlock(
            startPosition: new Vector2(100f, 100f),
            size: new Point(32, 32),
            moveSpeedPixelsPerSecond: 120f,
            worldBounds: new Rectangle(0, 0, 1024, 640));

        player.Update(FakeGameTime.OneFrame(), input, NoBlockedTiles);

        Assert.False(player.IsMoving);
    }

    [Fact]
    public void Update__WithMovementInput__IsMovingTrue()
    {
        var input = new FakeInputManager();
        var player = new PlayerBlock(
            startPosition: new Vector2(100f, 100f),
            size: new Point(32, 32),
            moveSpeedPixelsPerSecond: 120f,
            worldBounds: new Rectangle(0, 0, 1024, 640));

        input.Press(InputAction.MoveDown);
        player.Update(FakeGameTime.OneFrame(), input, NoBlockedTiles);

        Assert.True(player.IsMoving);
    }

    [Fact]
    public void Facing__DefaultsToDown()
    {
        var player = new PlayerBlock(
            startPosition: new Vector2(100f, 100f),
            size: new Point(32, 32),
            moveSpeedPixelsPerSecond: 120f,
            worldBounds: new Rectangle(0, 0, 1024, 640));

        Assert.Equal(FacingDirection.Down, player.Facing);
    }

    [Fact]
    public void Update__WithAccelerationRate__FirstFrameSlowerThanFullSpeed()
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

        // Without ramp: 120*(1/60)=2.0. With ramp: fraction≈0.167, distance≈0.33.
        Assert.True(player.Position.X > 0f);
        Assert.True(player.Position.X < 2f);
    }

    private sealed class DelegateCollisionData : IMapCollisionData
    {
        private readonly Func<Rectangle, bool> _isBlocked;

        public DelegateCollisionData(Func<Rectangle, bool> isBlocked)
        {
            _isBlocked = isBlocked;
        }

        public bool IsWorldRectangleBlocked(Rectangle worldBounds)
        {
            return _isBlocked(worldBounds);
        }
    }
}
