using Microsoft.Xna.Framework;
using DogDays.Game.Data;
using DogDays.Game.Entities;
using DogDays.Game.Input;
using DogDays.Game.Systems;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="WatercraftBoardSequence"/>.
/// </summary>
public class WatercraftBoardSequenceTests
{
    private const int FrameSize = 32;
    private const int WatercraftWidth = 21;
    private const int WatercraftHeight = 64;
    private const int HorizontalWatercraftWidth = 64;
    private const int HorizontalWatercraftHeight = 16;
    private static readonly Rectangle WorldBounds = new(0, 0, 512, 512);

    private static Watercraft CreateWatercraft(Vector2 position)
    {
        return new Watercraft(position, WatercraftWidth, WatercraftHeight, HorizontalWatercraftWidth, HorizontalWatercraftHeight);
    }

    private static bool AllowBounds(Rectangle _) => true;

    private static bool BlockBounds(Rectangle _) => false;

    private static PlayerBlock CreatePlayer(Vector2 position, FacingDirection facing)
    {
        var player = new PlayerBlock(position, new Point(FrameSize, FrameSize), 96f, WorldBounds);
        player.SetFacing(facing);
        return player;
    }

    private static FollowerBlock CreateFollower(Vector2 position)
    {
        return new FollowerBlock(position, new Point(FrameSize, FrameSize), WorldBounds);
    }

    [Fact]
    public void IsActive__BeforeBegin__ReturnsFalse()
    {
        var sequence = new WatercraftBoardSequence(FrameSize, FrameSize);

        Assert.False(sequence.IsActive);
    }

    [Fact]
    public void Begin__SetsSequenceActive()
    {
        var sequence = new WatercraftBoardSequence(FrameSize, FrameSize);
        var watercraft = CreateWatercraft(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 170f), FacingDirection.Up);
        var follower = CreateFollower(new Vector2(80f, 170f));

        sequence.Begin(watercraft, player, follower);

        Assert.True(sequence.IsActive);
        Assert.Equal(WatercraftBoardState.HoppingToSeat, sequence.State);
    }

    [Fact]
    public void Update__AfterHopDuration__TransitionsToSeated()
    {
        var sequence = new WatercraftBoardSequence(FrameSize, FrameSize);
        var watercraft = CreateWatercraft(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 170f), FacingDirection.Up);
        var follower = CreateFollower(new Vector2(80f, 170f));
        var input = new FakeInputManager();

        sequence.Begin(watercraft, player, follower);

        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);
        }

        Assert.True(sequence.IsActive);
        Assert.True(sequence.IsSeated);
        Assert.Equal(WatercraftBoardState.Seated, sequence.State);
        Assert.Equal(FacingDirection.Down, player.Facing);
        Assert.Equal(FacingDirection.Down, follower.Facing);
    }

    [Fact]
    public void Mounted__AfterHopDuration__FiresOnceWhenActorsReachSeats()
    {
        var sequence = new WatercraftBoardSequence(FrameSize, FrameSize);
        var watercraft = CreateWatercraft(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 170f), FacingDirection.Up);
        var follower = CreateFollower(new Vector2(80f, 170f));
        var input = new FakeInputManager();
        var mountedCount = 0;
        Watercraft? mountedCraft = null;

        sequence.Mounted += craft =>
        {
            mountedCount++;
            mountedCraft = craft;
        };

        sequence.Begin(watercraft, player, follower);

        Assert.Equal(0, mountedCount);

        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);
        }

        Assert.Equal(1, mountedCount);
        Assert.Same(watercraft, mountedCraft);

        for (var i = 0; i < 10; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);
        }

        Assert.Equal(1, mountedCount);
    }

    [Fact]
    public void RestoreSeated__DoesNotFireMounted()
    {
        var sequence = new WatercraftBoardSequence(FrameSize, FrameSize);
        var watercraft = CreateWatercraft(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 170f), FacingDirection.Up);
        var follower = CreateFollower(new Vector2(80f, 170f));
        var mountedCount = 0;

        sequence.Mounted += _ => mountedCount++;

        sequence.RestoreSeated(watercraft, player, follower);

        Assert.Equal(0, mountedCount);
    }

    [Fact]
    public void Update__WhenSeated__PlayerOccupiesRearSeatAndFollowerOccupiesFrontSeat()
    {
        var sequence = new WatercraftBoardSequence(FrameSize, FrameSize);
        var watercraft = CreateWatercraft(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 170f), FacingDirection.Up);
        var follower = CreateFollower(new Vector2(80f, 170f));
        var input = new FakeInputManager();

        sequence.Begin(watercraft, player, follower);

        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);
        }

        Assert.Equal(watercraft.GetRearSeatPosition(FrameSize, FrameSize), player.Position);
        Assert.Equal(watercraft.GetFrontSeatPosition(FrameSize, FrameSize), follower.Position);
    }

    [Fact]
    public void Update__WhenSeated_ConfirmPressed__TransitionsToHoppingOff()
    {
        var sequence = new WatercraftBoardSequence(FrameSize, FrameSize);
        var watercraft = CreateWatercraft(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 170f), FacingDirection.Up);
        var follower = CreateFollower(new Vector2(80f, 170f));
        var input = new FakeInputManager();

        sequence.Begin(watercraft, player, follower);

        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);
        }

        input.Press(InputAction.Confirm);
        sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);

        Assert.Equal(WatercraftBoardState.HoppingOff, sequence.State);
    }

    [Fact]
    public void Update__AfterHopOff__ReturnsToIdle()
    {
        var sequence = new WatercraftBoardSequence(FrameSize, FrameSize);
        var watercraft = CreateWatercraft(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 170f), FacingDirection.Up);
        var follower = CreateFollower(new Vector2(80f, 170f));
        var input = new FakeInputManager();

        sequence.Begin(watercraft, player, follower);

        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);
        }

        input.Press(InputAction.Confirm);
        sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);
        input.Update();

        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);
        }

        Assert.False(sequence.IsActive);
        Assert.Equal(WatercraftBoardState.Idle, sequence.State);
        Assert.Equal(FacingDirection.Down, player.Facing);
        Assert.Equal(FacingDirection.Down, follower.Facing);
    }

    [Fact]
    public void Update__WhenSeatedAndMoveRightHeld__MovesWatercraftWithPlayerInRearSeat()
    {
        var sequence = new WatercraftBoardSequence(FrameSize, FrameSize);
        var watercraft = CreateWatercraft(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 170f), FacingDirection.Up);
        var follower = CreateFollower(new Vector2(80f, 170f));
        var input = new FakeInputManager();

        sequence.Begin(watercraft, player, follower);
        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);
        }

        input.Press(InputAction.MoveRight);
        sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);

        Assert.Equal(FacingDirection.Right, watercraft.Facing);
        Assert.Equal(watercraft.GetRearSeatPosition(FrameSize, FrameSize), player.Position);
        Assert.Equal(watercraft.GetFrontSeatPosition(FrameSize, FrameSize), follower.Position);
        Assert.True(player.Position.X < follower.Position.X);
    }

    [Fact]
    public void Update__WhenTurningInPlace__KeepsRearSeatPlayerStable()
    {
        var sequence = new WatercraftBoardSequence(FrameSize, FrameSize);
        var watercraft = CreateWatercraft(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 170f), FacingDirection.Up);
        var follower = CreateFollower(new Vector2(80f, 170f));
        var input = new FakeInputManager();

        sequence.Begin(watercraft, player, follower);
        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);
        }

        var startingPlayerPosition = player.Position;
        var startingPlayerCenter = player.Center;
        input.Press(InputAction.MoveRight);

        sequence.Update(FakeGameTime.FromSeconds(0f), input, player, follower, AllowBounds, AllowBounds);

        Assert.Equal(FacingDirection.Right, watercraft.Facing);
        Assert.InRange(Vector2.Distance(startingPlayerPosition, player.Position), 0f, 0.5f);
        Assert.InRange(Vector2.Distance(startingPlayerCenter, player.Center), 0f, 0.5f);
    }

    [Fact]
    public void Update__WhenSeatedAndMoveUpRightHeld__MovesWatercraftDiagonallyWithoutSpeedBoost()
    {
        var sequence = new WatercraftBoardSequence(FrameSize, FrameSize);
        var watercraft = CreateWatercraft(new Vector2(100f, 100f));
        watercraft.SetState(watercraft.Center, FacingDirection.Up);
        var player = CreatePlayer(new Vector2(100f, 170f), FacingDirection.Up);
        var follower = CreateFollower(new Vector2(80f, 170f));
        var input = new FakeInputManager();

        sequence.Begin(watercraft, player, follower);
        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);
        }

        var startingCenter = watercraft.Center;
        input.Press(InputAction.MoveUp);
        input.Press(InputAction.MoveRight);

        sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);

        var movement = watercraft.Center - startingCenter;
        var expectedAxisDelta = (36f / 60f) / MathF.Sqrt(2f);

        Assert.Equal(FacingDirection.Up, watercraft.Facing);
        Assert.Equal(expectedAxisDelta, movement.X, 3);
        Assert.Equal(-expectedAxisDelta, movement.Y, 3);
        Assert.Equal(36f / 60f, movement.Length(), 3);
        Assert.Equal(watercraft.GetRearSeatPosition(FrameSize, FrameSize), player.Position);
        Assert.Equal(watercraft.GetFrontSeatPosition(FrameSize, FrameSize), follower.Position);
    }

    [Fact]
    public void Update__WhenDiagonalTurnIsBlocked__KeepsPreviousFacingAndPosition()
    {
        var sequence = new WatercraftBoardSequence(FrameSize, FrameSize);
        var watercraft = CreateWatercraft(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 170f), FacingDirection.Up);
        var follower = CreateFollower(new Vector2(80f, 170f));
        var input = new FakeInputManager();

        sequence.Begin(watercraft, player, follower);
        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);
        }

        var startingCenter = watercraft.Center;
        input.Press(InputAction.MoveRight);
        input.Press(InputAction.MoveDown);

        sequence.Update(FakeGameTime.OneFrame(), input, player, follower, BlockBounds, AllowBounds);

        Assert.Equal(FacingDirection.Down, watercraft.Facing);
        Assert.Equal(startingCenter, watercraft.Center);
        Assert.Equal(watercraft.GetRearSeatPosition(FrameSize, FrameSize), player.Position);
        Assert.Equal(watercraft.GetFrontSeatPosition(FrameSize, FrameSize), follower.Position);
    }

    [Fact]
    public void Update__WhenSeatedAndNoDisembarkSpace__RemainsSeated()
    {
        var sequence = new WatercraftBoardSequence(FrameSize, FrameSize);
        var watercraft = CreateWatercraft(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 170f), FacingDirection.Up);
        var follower = CreateFollower(new Vector2(80f, 170f));
        var input = new FakeInputManager();

        sequence.Begin(watercraft, player, follower);
        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);
        }

        input.Press(InputAction.Confirm);
        sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, BlockBounds);

        Assert.Equal(WatercraftBoardState.Seated, sequence.State);
    }

    [Fact]
    public void Update__WhenSeatedAndMovementBlocked__WatercraftDoesNotAdvance()
    {
        var sequence = new WatercraftBoardSequence(FrameSize, FrameSize);
        var watercraft = CreateWatercraft(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 170f), FacingDirection.Up);
        var follower = CreateFollower(new Vector2(80f, 170f));
        var input = new FakeInputManager();

        sequence.Begin(watercraft, player, follower);
        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower, AllowBounds, AllowBounds);
        }

        var startingCenter = watercraft.Center;
        input.Press(InputAction.MoveUp);

        sequence.Update(FakeGameTime.OneFrame(), input, player, follower, BlockBounds, AllowBounds);

        Assert.Equal(startingCenter, watercraft.Center);
        Assert.Equal(WatercraftBoardState.Seated, sequence.State);
    }
}