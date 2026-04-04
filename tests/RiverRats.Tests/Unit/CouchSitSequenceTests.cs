using Microsoft.Xna.Framework;
using RiverRats.Game.Data;
using RiverRats.Game.Entities;
using RiverRats.Game.Input;
using RiverRats.Game.Systems;
using RiverRats.Tests.Helpers;

namespace RiverRats.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="CouchSitSequence"/> state machine.
/// </summary>
public class CouchSitSequenceTests
{
    private const int FrameSize = 32;
    private static readonly Rectangle WorldBounds = new(0, 0, 512, 512);

    private static Couch CreateCouch(Vector2 position)
    {
        return new Couch(position, width: 24, height: 80);
    }

    private static PlayerBlock CreatePlayer(Vector2 position)
    {
        return new PlayerBlock(position, new Point(FrameSize, FrameSize), 96f, WorldBounds);
    }

    private static FollowerBlock CreateFollower(Vector2 position)
    {
        return new FollowerBlock(position, new Point(FrameSize, FrameSize), WorldBounds);
    }

    [Fact]
    public void IsActive__BeforeBegin__ReturnsFalse()
    {
        var sequence = new CouchSitSequence(FrameSize, FrameSize);

        Assert.False(sequence.IsActive);
    }

    [Fact]
    public void Begin__SetsStateToHoppingToSeat()
    {
        var sequence = new CouchSitSequence(FrameSize, FrameSize);
        var couch = CreateCouch(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 190f));
        var follower = CreateFollower(new Vector2(80f, 190f));

        sequence.Begin(couch, player, follower);

        Assert.True(sequence.IsActive);
        Assert.Equal(CouchSitState.HoppingToSeat, sequence.State);
    }

    [Fact]
    public void Update__AfterHopDuration__TransitionsToSeated()
    {
        var sequence = new CouchSitSequence(FrameSize, FrameSize);
        var couch = CreateCouch(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 190f));
        var follower = CreateFollower(new Vector2(80f, 190f));
        var input = new FakeInputManager();

        sequence.Begin(couch, player, follower);

        // Advance enough time for both characters to finish hopping (0.35s + 0.12s delay).
        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower);
        }

        Assert.Equal(CouchSitState.Seated, sequence.State);
        Assert.True(sequence.IsSeated);
    }

    [Fact]
    public void Update__WhenSeated_ConfirmPressed__TransitionsToHoppingOff()
    {
        var sequence = new CouchSitSequence(FrameSize, FrameSize);
        var couch = CreateCouch(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 190f));
        var follower = CreateFollower(new Vector2(80f, 190f));
        var input = new FakeInputManager();

        sequence.Begin(couch, player, follower);

        // Hop to seat
        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower);
        }

        Assert.Equal(CouchSitState.Seated, sequence.State);

        // Press Confirm to stand up
        input.Press(InputAction.Confirm);
        sequence.Update(FakeGameTime.OneFrame(), input, player, follower);

        Assert.Equal(CouchSitState.HoppingOff, sequence.State);
    }

    [Fact]
    public void Update__WhenSeated_CancelPressed__TransitionsToHoppingOff()
    {
        var sequence = new CouchSitSequence(FrameSize, FrameSize);
        var couch = CreateCouch(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 190f));
        var follower = CreateFollower(new Vector2(80f, 190f));
        var input = new FakeInputManager();

        sequence.Begin(couch, player, follower);

        // Hop to seat
        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower);
        }

        // Press Cancel to stand up
        input.Press(InputAction.Cancel);
        sequence.Update(FakeGameTime.OneFrame(), input, player, follower);

        Assert.Equal(CouchSitState.HoppingOff, sequence.State);
    }

    [Fact]
    public void Update__AfterHopOff__ReturnsToIdle()
    {
        var sequence = new CouchSitSequence(FrameSize, FrameSize);
        var couch = CreateCouch(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 190f));
        var follower = CreateFollower(new Vector2(80f, 190f));
        var input = new FakeInputManager();

        sequence.Begin(couch, player, follower);

        // Hop to seat
        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower);
        }

        // Stand up
        input.Press(InputAction.Confirm);
        sequence.Update(FakeGameTime.OneFrame(), input, player, follower);
        input.Update();

        // Hop off
        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower);
        }

        Assert.Equal(CouchSitState.Idle, sequence.State);
        Assert.False(sequence.IsActive);
        Assert.Equal(FacingDirection.Down, player.Facing);
        Assert.Equal(FacingDirection.Down, follower.Facing);
        Assert.True(player.Position.X <= couch.Bounds.Left - FrameSize - 10f);
        Assert.True(follower.Position.X <= couch.Bounds.Left - FrameSize - 10f);
    }

    [Fact]
    public void Update__WhenSeated__PlayerPositionIsAtSeat()
    {
        var sequence = new CouchSitSequence(FrameSize, FrameSize);
        var couch = CreateCouch(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 190f));
        var follower = CreateFollower(new Vector2(80f, 190f));
        var input = new FakeInputManager();

        sequence.Begin(couch, player, follower);

        // Complete the hop
        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower);
        }

        // Player should be at one of the seat positions.
        var atSeatA = Vector2.Distance(player.Position, couch.SeatPositionA) < 1f;
        var atSeatB = Vector2.Distance(player.Position, couch.SeatPositionB) < 1f;
        Assert.True(atSeatA || atSeatB, "Player should be at one of the couch seat positions.");
    }

    [Fact]
    public void Update__WhenSeated__PlayerFacesLeft()
    {
        var sequence = new CouchSitSequence(FrameSize, FrameSize);
        var couch = CreateCouch(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 190f));
        var follower = CreateFollower(new Vector2(80f, 190f));
        var input = new FakeInputManager();

        // Couch seating always uses the left-facing frame for this couch orientation.
        sequence.Begin(couch, player, follower);

        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower);
        }

        Assert.Equal(FacingDirection.Left, player.Facing);
        Assert.Equal(FacingDirection.Left, follower.Facing);
    }

    [Fact]
    public void Update__PlayerAndFollower__TakeSeparateSeats()
    {
        var sequence = new CouchSitSequence(FrameSize, FrameSize);
        var couch = CreateCouch(new Vector2(100f, 100f));
        var player = CreatePlayer(new Vector2(100f, 190f));
        var follower = CreateFollower(new Vector2(80f, 190f));
        var input = new FakeInputManager();

        sequence.Begin(couch, player, follower);

        for (var i = 0; i < 60; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), input, player, follower);
        }

        // Player and follower should be at different positions.
        Assert.NotEqual(player.Position, follower.Position);
    }

    [Fact]
    public void Update__IdleState__DoesNothing()
    {
        var sequence = new CouchSitSequence(FrameSize, FrameSize);
        var player = CreatePlayer(new Vector2(100f, 190f));
        var follower = CreateFollower(new Vector2(80f, 190f));
        var input = new FakeInputManager();

        var playerPosBefore = player.Position;

        sequence.Update(FakeGameTime.OneFrame(), input, player, follower);

        Assert.Equal(playerPosBefore, player.Position);
        Assert.False(sequence.IsActive);
    }
}
