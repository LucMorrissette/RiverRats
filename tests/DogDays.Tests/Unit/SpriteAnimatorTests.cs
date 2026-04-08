using Microsoft.Xna.Framework;
using DogDays.Game.Components;
using DogDays.Game.Data;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="SpriteAnimator"/> frame cycling and direction logic.
/// </summary>
public class SpriteAnimatorTests
{
    private const int FrameSize = 32;
    private const int FramesPerDirection = 4;
    private const float FrameDuration = 0.15f;

    private static SpriteAnimator CreateAnimator()
    {
        return new SpriteAnimator(FrameSize, FrameSize, FramesPerDirection, FrameDuration);
    }

    [Fact]
    public void Update__NotMoving__ResetsToFrameZero()
    {
        var animator = CreateAnimator();

        // Advance a few frames while moving so we're not on frame 0.
        animator.Update(FakeGameTime.FromSeconds(FrameDuration * 2), isMoving: true);
        Assert.NotEqual(0, animator.CurrentFrame);

        // Stop moving — should snap to idle frame.
        animator.Update(FakeGameTime.OneFrame(), isMoving: false);
        Assert.Equal(0, animator.CurrentFrame);
    }

    [Fact]
    public void Update__Moving__CyclesThroughFrames()
    {
        var animator = CreateAnimator();

        animator.Update(FakeGameTime.FromSeconds(FrameDuration), isMoving: true);
        Assert.Equal(1, animator.CurrentFrame);

        animator.Update(FakeGameTime.FromSeconds(FrameDuration), isMoving: true);
        Assert.Equal(2, animator.CurrentFrame);

        animator.Update(FakeGameTime.FromSeconds(FrameDuration), isMoving: true);
        Assert.Equal(3, animator.CurrentFrame);

        // Wraps around.
        animator.Update(FakeGameTime.FromSeconds(FrameDuration), isMoving: true);
        Assert.Equal(0, animator.CurrentFrame);
    }

    [Fact]
    public void SourceRectangle__DirectionAndFrame__ReturnsCorrectRegion()
    {
        var animator = CreateAnimator();
        animator.Direction = FacingDirection.Left;

        // Advance to frame 1.
        animator.Update(FakeGameTime.FromSeconds(FrameDuration), isMoving: true);

        var expected = new Rectangle(1 * FrameSize, 1 * FrameSize, FrameSize, FrameSize);
        Assert.Equal(expected, animator.SourceRectangle);
    }

    [Theory]
    [InlineData(FacingDirection.Down, 0)]
    [InlineData(FacingDirection.Left, 1)]
    [InlineData(FacingDirection.Right, 2)]
    [InlineData(FacingDirection.Up, 3)]
    public void SourceRectangle__EachDirection__UsesCorrectRow(FacingDirection direction, int expectedRow)
    {
        var animator = CreateAnimator();
        animator.Direction = direction;

        // At idle (frame 0).
        var rect = animator.SourceRectangle;
        Assert.Equal(0, rect.X);
        Assert.Equal(expectedRow * FrameSize, rect.Y);
    }
}
