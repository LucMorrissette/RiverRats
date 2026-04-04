using Microsoft.Xna.Framework;
using RiverRats.Game.Data;
using RiverRats.Game.Entities;
using RiverRats.Game.Systems;
using RiverRats.Tests.Helpers;

namespace RiverRats.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="PlayerCollapseSequence"/> timing and transform state.
/// </summary>
public class PlayerCollapseSequenceTests
{
    private static readonly Rectangle WorldBounds = new(0, 0, 512, 512);

    private static PlayerBlock CreatePlayer(Vector2 position)
    {
        return new PlayerBlock(position, new Point(32, 32), 96f, WorldBounds);
    }

    [Fact]
    public void Begin__CapturesPlayerPositionAndFacing()
    {
        var sequence = new PlayerCollapseSequence();
        var player = CreatePlayer(new Vector2(140f, 220f));
        player.SetFacing(FacingDirection.Left);

        sequence.Begin(player);

        Assert.True(sequence.IsActive);
        Assert.Equal(0f, sequence.Progress);
        Assert.Equal(0, sequence.CurrentFrameIndex);
        Assert.Equal(new Rectangle(0, 224, 32, 32), sequence.CurrentSourceRectangle);
    }

    [Fact]
    public void Update__DuringSequence__KeepsPlayerLockedAndAdvancesToKneelingFrame()
    {
        var sequence = new PlayerCollapseSequence();
        var player = CreatePlayer(new Vector2(140f, 220f));
        player.SetFacing(FacingDirection.Left);
        sequence.Begin(player);

        player.SetPosition(new Vector2(260f, 300f));

        sequence.Update(FakeGameTime.FromSeconds(PlayerCollapseSequence.DurationSeconds * 0.3f), player);

        Assert.True(sequence.IsActive);
        Assert.InRange(sequence.Progress, 0.29f, 0.31f);
        Assert.Equal(new Vector2(140f, 220f), player.Position);
        Assert.Equal(FacingDirection.Left, player.Facing);
        Assert.Equal(1, sequence.CurrentFrameIndex);
        Assert.Equal(new Rectangle(32, 224, 32, 32), sequence.CurrentSourceRectangle);
    }

    [Fact]
    public void Update__AfterFullDuration__EndsSequence()
    {
        var sequence = new PlayerCollapseSequence();
        var player = CreatePlayer(new Vector2(140f, 220f));
        player.SetFacing(FacingDirection.Right);
        sequence.Begin(player);

        sequence.Update(FakeGameTime.FromSeconds(PlayerCollapseSequence.DurationSeconds), player);

        Assert.False(sequence.IsActive);
        Assert.Equal(1f, sequence.Progress);
        Assert.Equal(3, sequence.CurrentFrameIndex);
        Assert.Equal(new Rectangle(96, 256, 32, 32), sequence.CurrentSourceRectangle);
    }
}