using Microsoft.Xna.Framework;
using RiverRats.Game.Data;
using RiverRats.Game.Entities;
using RiverRats.Game.Systems;

namespace RiverRats.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="WatercraftCollisionRules"/>.
/// </summary>
public class WatercraftCollisionRulesTests
{
    private const int WatercraftWidth = 21;
    private const int WatercraftHeight = 64;
    private const int HorizontalWatercraftWidth = 64;
    private const int HorizontalWatercraftHeight = 16;

    private static Watercraft CreateWatercraft(Vector2 position)
    {
        return new Watercraft(position, WatercraftWidth, WatercraftHeight, HorizontalWatercraftWidth, HorizontalWatercraftHeight);
    }

    [Fact]
    public void IsBlocked__IntersectingStaticBlocker__ReturnsTrue()
    {
        var currentBounds = new Rectangle(20, 20, 16, 16);
        var candidateBounds = new Rectangle(100, 100, 64, 16);
        var staticBlockers = new[] { new Rectangle(120, 90, 32, 32) };

        var blocked = WatercraftCollisionRules.IsBlocked(candidateBounds, currentBounds, staticBlockers, [], activeWatercraft: null);

        Assert.True(blocked);
    }

    [Fact]
    public void IsBlocked__IntersectingOtherWatercraft__ReturnsTrue()
    {
        var activeWatercraft = CreateWatercraft(new Vector2(100f, 100f));
        activeWatercraft.SetState(activeWatercraft.Center, FacingDirection.Right);
        var otherWatercraft = CreateWatercraft(new Vector2(130f, 100f));
        otherWatercraft.SetState(otherWatercraft.Center, FacingDirection.Right);

        var blocked = WatercraftCollisionRules.IsBlocked(
            activeWatercraft.Bounds,
            new Rectangle(0, 0, activeWatercraft.Bounds.Width, activeWatercraft.Bounds.Height),
            [],
            [activeWatercraft, otherWatercraft],
            activeWatercraft);

        Assert.True(blocked);
    }

    [Fact]
    public void IsBlocked__IntersectingOnlyActiveWatercraft__ReturnsFalse()
    {
        var activeWatercraft = CreateWatercraft(new Vector2(100f, 100f));
        activeWatercraft.SetState(activeWatercraft.Center, FacingDirection.Right);

        var blocked = WatercraftCollisionRules.IsBlocked(
            activeWatercraft.Bounds,
            activeWatercraft.Bounds,
            [],
            [activeWatercraft],
            activeWatercraft);

        Assert.False(blocked);
    }

    [Fact]
    public void IsBlocked__ClearBounds__ReturnsFalse()
    {
        var activeWatercraft = CreateWatercraft(new Vector2(100f, 100f));
        var otherWatercraft = CreateWatercraft(new Vector2(260f, 260f));
        var candidateBounds = new Rectangle(100, 20, 64, 16);
        var staticBlockers = new[] { new Rectangle(220, 220, 24, 24) };

        var blocked = WatercraftCollisionRules.IsBlocked(
            candidateBounds,
            new Rectangle(0, 0, 16, 16),
            staticBlockers,
            [activeWatercraft, otherWatercraft],
            activeWatercraft);

        Assert.False(blocked);
    }

    [Fact]
    public void IsBlocked__CandidateReducesExistingStaticOverlap__ReturnsFalse()
    {
        var blocker = new Rectangle(100, 100, 64, 64);
        var currentBounds = new Rectangle(120, 100, 64, 16);
        var candidateBounds = new Rectangle(130, 84, 64, 16);

        var blocked = WatercraftCollisionRules.IsBlocked(candidateBounds, currentBounds, [blocker], [], activeWatercraft: null);

        Assert.False(blocked);
    }

    [Fact]
    public void IsBlocked__CandidateIncreasesExistingStaticOverlap__ReturnsTrue()
    {
        var blocker = new Rectangle(100, 100, 64, 64);
        var currentBounds = new Rectangle(130, 84, 64, 16);
        var candidateBounds = new Rectangle(120, 100, 64, 16);

        var blocked = WatercraftCollisionRules.IsBlocked(candidateBounds, currentBounds, [blocker], [], activeWatercraft: null);

        Assert.True(blocked);
    }
}