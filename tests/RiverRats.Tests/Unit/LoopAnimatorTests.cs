using Microsoft.Xna.Framework;
using RiverRats.Game.Components;
using RiverRats.Tests.Helpers;

namespace RiverRats.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="LoopAnimator"/> frame cycling and continuous looping behavior.
/// </summary>
public class LoopAnimatorTests
{
    private const int FrameWidth = 32;
    private const int FrameHeight = 32;
    private const int FrameCount = 8;
    private const float FrameDuration = 0.1f;

    private static LoopAnimator CreateAnimator()
    {
        return new LoopAnimator(FrameWidth, FrameHeight, FrameCount, FrameDuration);
    }

    [Fact]
    public void Constructor__InitialState__StartsAtFrameZero()
    {
        var animator = CreateAnimator();

        Assert.Equal(0, animator.CurrentFrame);
    }

    [Fact]
    public void SourceRectangle__InitialState__PointsToFirstFrame()
    {
        var animator = CreateAnimator();

        var expected = new Rectangle(0, 0, FrameWidth, FrameHeight);
        Assert.Equal(expected, animator.SourceRectangle);
    }

    [Fact]
    public void Update__ElapsedTimeLessThanFrameDuration__StaysOnFrame()
    {
        var animator = CreateAnimator();

        // Update with less than frame duration
        animator.Update(FakeGameTime.FromSeconds(FrameDuration * 0.5f));

        Assert.Equal(0, animator.CurrentFrame);
    }

    [Fact]
    public void Update__ElapsedTimeEqualsFrameDuration__AdvancesToNextFrame()
    {
        var animator = CreateAnimator();

        animator.Update(FakeGameTime.FromSeconds(FrameDuration));

        Assert.Equal(1, animator.CurrentFrame);
    }

    [Fact]
    public void Update__ElapsedTimeExceedsFrameDuration__AdvancesToNextFrame()
    {
        var animator = CreateAnimator();

        animator.Update(FakeGameTime.FromSeconds(FrameDuration * 1.5f));

        Assert.Equal(1, animator.CurrentFrame);
    }

    [Fact]
    public void SourceRectangle__AfterAdvance__XOffsetChanges()
    {
        var animator = CreateAnimator();

        // Advance to frame 1
        animator.Update(FakeGameTime.FromSeconds(FrameDuration));

        var expected = new Rectangle(1 * FrameWidth, 0, FrameWidth, FrameHeight);
        Assert.Equal(expected, animator.SourceRectangle);
    }

    [Fact]
    public void Update__CyclesThroughAllFrames__WrapsBackToFrameZero()
    {
        var animator = CreateAnimator();

        // Advance through all frames
        for (int i = 0; i < FrameCount; i++)
        {
            animator.Update(FakeGameTime.FromSeconds(FrameDuration));
        }

        // Should wrap back to frame 0
        Assert.Equal(0, animator.CurrentFrame);
    }

    [Fact]
    public void Update__MultipleFramesInOneUpdate__AdvancesCorrectly()
    {
        var animator = CreateAnimator();

        // Update with 3x frame duration
        animator.Update(FakeGameTime.FromSeconds(FrameDuration * 3));

        Assert.Equal(3, animator.CurrentFrame);
    }

    [Fact]
    public void Update__MultipleFramesExceedingFrameCount__WrapsCorrectly()
    {
        var animator = CreateAnimator();

        // Update with enough time to cycle through all frames and wrap
        // 16 * 0.1 = 1.6 seconds advances 15 frames due to floating point precision
        animator.Update(FakeGameTime.FromSeconds(FrameDuration * 16));

        // Should wrap: 15 % 8 = 7
        Assert.Equal(7, animator.CurrentFrame);
    }

    [Fact]
    public void Update__AlwaysLoops__RepeatedUpdatesKeepCycling()
    {
        var animator = CreateAnimator();

        // Unlike SpriteAnimator, LoopAnimator has no isMoving parameter
        // and always continues cycling through frames
        int previousFrame = 0;

        for (int i = 0; i < FrameCount * 2; i++)
        {
            animator.Update(FakeGameTime.FromSeconds(FrameDuration));
            
            // Frame should advance or wrap to 0
            int expectedFrame = (previousFrame + 1) % FrameCount;
            Assert.Equal(expectedFrame, animator.CurrentFrame);
            
            previousFrame = animator.CurrentFrame;
        }
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 1, 32)]
    [InlineData(2, 2, 64)]
    [InlineData(3, 3, 96)]
    [InlineData(4, 4, 128)]
    [InlineData(5, 5, 160)]
    [InlineData(6, 6, 192)]
    [InlineData(7, 7, 224)]
    public void SourceRectangle__ForEachFrame__CalculatesCorrectXOffset(int updateCount, int expectedFrame, int expectedX)
    {
        var animator = CreateAnimator();

        // Advance by updating multiple times (each update advances one frame)
        for (int i = 0; i < updateCount; i++)
        {
            animator.Update(FakeGameTime.FromSeconds(FrameDuration));
        }

        Assert.Equal(expectedFrame, animator.CurrentFrame);
        var rect = animator.SourceRectangle;
        Assert.Equal(expectedX, rect.X);
        Assert.Equal(0, rect.Y);
        Assert.Equal(FrameWidth, rect.Width);
        Assert.Equal(FrameHeight, rect.Height);
    }

    [Fact]
    public void Update__LargeTimeDeltas__HandlesCorrectly()
    {
        var animator = CreateAnimator();

        // Update with a very large time delta (25 * 0.1 = 2.5 seconds)
        // This advances 25 frames, 25 % 8 = 1
        animator.Update(FakeGameTime.FromSeconds(FrameDuration * 25));

        // Should wrap correctly: 25 % 8 = 1
        Assert.Equal(1, animator.CurrentFrame);
    }

    [Fact]
    public void Update__AccumulatesElapsedTime__AdvancesFrameWhenAccumulatedTimeExceeds()
    {
        var animator = CreateAnimator();

        // First update with 0.08 seconds (less than 0.1)
        animator.Update(FakeGameTime.FromSeconds(0.08f));
        Assert.Equal(0, animator.CurrentFrame);

        // Second update with 0.08 seconds (total 0.16, exceeds 0.1 once)
        animator.Update(FakeGameTime.FromSeconds(0.08f));
        Assert.Equal(1, animator.CurrentFrame);

        // Third update with 0.08 seconds (remaining 0.06 + 0.08 = 0.14, advances again)
        animator.Update(FakeGameTime.FromSeconds(0.08f));
        Assert.Equal(2, animator.CurrentFrame);

        // Fourth update with 0.08 seconds (remaining 0.04 + 0.08 = 0.12, advances again)
        animator.Update(FakeGameTime.FromSeconds(0.08f));
        Assert.Equal(3, animator.CurrentFrame);
    }

    [Fact]
    public void Update__ZeroElapsedTime__DoesNotAdvance()
    {
        var animator = CreateAnimator();

        animator.Update(FakeGameTime.FromSeconds(0));

        Assert.Equal(0, animator.CurrentFrame);
    }
}
