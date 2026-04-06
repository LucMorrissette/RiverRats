using Microsoft.Xna.Framework;
using RiverRats.Game.Data;
using RiverRats.Game.Entities;
using RiverRats.Game.Input;
using RiverRats.Game.Systems;
using RiverRats.Tests.Helpers;

namespace RiverRats.Tests.Unit;

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
    public void Update__WhenSeated__FollowerBoardsTheWatercraft()
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

        Assert.Equal(watercraft.GetRearSeatPosition(FrameSize, FrameSize), follower.Position);
        Assert.Equal(watercraft.GetFrontSeatPosition(FrameSize, FrameSize), player.Position);
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
    public void Update__WhenSeatedAndMoveRightHeld__MovesWatercraftWithPlayerAtFront()
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
        Assert.Equal(watercraft.GetFrontSeatPosition(FrameSize, FrameSize), player.Position);
        Assert.Equal(watercraft.GetRearSeatPosition(FrameSize, FrameSize), follower.Position);
        Assert.True(player.Position.X > follower.Position.X);
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
}