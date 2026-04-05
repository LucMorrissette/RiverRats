using System;
using Microsoft.Xna.Framework;
using RiverRats.Game.Components;
using RiverRats.Game.World;
using Xunit;

namespace RiverRats.Tests.Unit;

public class IndoorNavigatorTests
{
    private static readonly Vector2 StartPos = new(100f, 100f);

    /// <summary>
    /// Creates a simple 4-node square graph (100,100)-(200,100)-(200,200)-(100,200)
    /// with edges forming a cycle: 1-2, 2-3, 3-4, 4-1.
    /// </summary>
    private static IndoorNavGraph SquareGraph()
    {
        var nodes = new IndoorNavNode[]
        {
            new(1, new Vector2(100f, 100f), "a", null),
            new(2, new Vector2(200f, 100f), "b", null),
            new(3, new Vector2(200f, 200f), "c", null),
            new(4, new Vector2(100f, 200f), "d", null),
        };
        var links = new IndoorNavLink[]
        {
            new(1, 2),
            new(2, 3),
            new(3, 4),
            new(4, 1),
        };
        return new IndoorNavGraph(nodes, links);
    }

    private static IndoorNavGraph TwoNodeGraph(Vector2? posA = null, Vector2? posB = null)
    {
        var a = posA ?? new Vector2(100f, 100f);
        var b = posB ?? new Vector2(100f, 120f);
        var nodes = new IndoorNavNode[]
        {
            new(1, a, "start", null),
            new(2, b, "end", null),
        };
        var links = new IndoorNavLink[] { new(1, 2) };
        return new IndoorNavGraph(nodes, links);
    }

    [Fact]
    public void Constructor__ThrowsWhenNavGraphNull()
    {
        Assert.Throws<ArgumentException>(() =>
            new IndoorNavigator(null!, Vector2.Zero));
    }

    [Fact]
    public void Constructor__ThrowsWhenNavGraphHasFewerThanTwoNodes()
    {
        var nodes = new IndoorNavNode[] { new(1, Vector2.Zero, null, null) };
        var graph = new IndoorNavGraph(nodes, Array.Empty<IndoorNavLink>());

        Assert.Throws<ArgumentException>(() =>
            new IndoorNavigator(graph, Vector2.Zero));
    }

    [Fact]
    public void Constructor__PicksInitialDestination()
    {
        var nav = new IndoorNavigator(SquareGraph(), StartPos, random: new Random(42));

        Assert.NotEqual(-1, nav.DestinationNodeId);
        Assert.True(nav.CurrentRoute.Count >= 2);
    }

    [Fact]
    public void CurrentTargetPosition__ReturnsFirstRouteNodeAfterStart()
    {
        var nav = new IndoorNavigator(TwoNodeGraph(), StartPos, random: new Random(0));

        Assert.NotNull(nav.CurrentTargetPosition);
        // Route skips start node, so target should be node 2 position.
        Assert.Equal(new Vector2(100f, 120f), nav.CurrentTargetPosition.Value);
    }

    [Fact]
    public void Update__AdvancesToNextNodeOnArrival()
    {
        // 3-node linear path.
        var nodes = new IndoorNavNode[]
        {
            new(1, new Vector2(100f, 100f), null, null),
            new(2, new Vector2(100f, 110f), null, null),
            new(3, new Vector2(100f, 120f), null, null),
        };
        var links = new IndoorNavLink[] { new(1, 2), new(2, 3) };
        var graph = new IndoorNavGraph(nodes, links);

        var nav = new IndoorNavigator(graph, new Vector2(100f, 100f), random: new Random(1));

        // Destination should be node 3 (only option besides node 1 in a
        // 3-node graph with seed 1).
        Assert.True(nav.CurrentRoute.Count >= 2);

        // Simulate arrival at first intermediate node (node 2) by passing position near it.
        nav.Update(new Vector2(100f, 110f), true, 1f / 60f);

        // Route index should have advanced.
        Assert.True(nav.RouteNodeIndex >= 2, "Should advance past intermediate node.");
    }

    [Fact]
    public void Update__PausesAtFinalDestination()
    {
        var nav = new IndoorNavigator(TwoNodeGraph(), StartPos,
            pauseDuration: 2f, random: new Random(0));

        // Simulate arrival at destination node.
        nav.Update(new Vector2(100f, 120f), true, 1f / 60f);

        Assert.True(nav.IsPaused, "Should pause after arriving at final destination.");
    }

    [Fact]
    public void Update__PicksNewDestinationAfterPauseExpires()
    {
        var nav = new IndoorNavigator(TwoNodeGraph(), StartPos,
            pauseDuration: 0.1f, random: new Random(42));

        // Arrive at destination.
        nav.Update(new Vector2(100f, 120f), true, 1f / 60f);
        Assert.True(nav.IsPaused);

        // Burn through the pause.
        nav.Update(new Vector2(100f, 120f), false, 0.2f);

        // After pause, should have picked a new destination.
        Assert.False(nav.IsPaused);
        Assert.NotEqual(-1, nav.DestinationNodeId);
    }

    [Fact]
    public void Update__RepathsAfterStuckTimeout()
    {
        var graph = SquareGraph();
        var nav = new IndoorNavigator(graph, StartPos,
            pauseDuration: 0f, random: new Random(42));

        var initialDest = nav.DestinationNodeId;
        var initialRoute = nav.CurrentRoute;

        // Report not-moved for > 2 seconds (stuck timeout).
        for (int i = 0; i < 180; i++)
            nav.Update(StartPos, false, 1f / 60f);

        var repathed = nav.DestinationNodeId != initialDest
            || !ReferenceEquals(nav.CurrentRoute, initialRoute);
        Assert.True(repathed, "Navigator should repath after stuck timeout.");
    }

    [Fact]
    public void Update__ResetsStuckTimerOnMovement()
    {
        var graph = SquareGraph();
        var nav = new IndoorNavigator(graph, StartPos,
            pauseDuration: 0f, random: new Random(42));

        var initialDest = nav.DestinationNodeId;

        // Almost stuck for 1.9 seconds...
        for (int i = 0; i < 114; i++)
            nav.Update(StartPos, false, 1f / 60f);

        // Then move — should reset stuck timer.
        nav.Update(new Vector2(101f, 100f), true, 1f / 60f);

        // Continue not moving for another 1 second — shouldn't trigger repath yet.
        for (int i = 0; i < 60; i++)
            nav.Update(new Vector2(101f, 100f), false, 1f / 60f);

        Assert.Equal(initialDest, nav.DestinationNodeId);
    }

    [Fact]
    public void CurrentTargetPosition__ReturnsNullWhenNoRoute()
    {
        // Disconnected 2-node graph — no links, so route will be empty after failed pick.
        var nodes = new IndoorNavNode[]
        {
            new(1, new Vector2(0f, 0f), null, null),
            new(2, new Vector2(500f, 500f), null, null),
        };
        var graph = new IndoorNavGraph(nodes, Array.Empty<IndoorNavLink>());

        var nav = new IndoorNavigator(graph, Vector2.Zero, random: new Random(0));

        // Constructor tried to pick a destination but route is empty (no links).
        // After pause expires, target should be null.
        nav.Update(Vector2.Zero, false, 2f);

        // Will re-attempt and fail again. Target remains null.
        Assert.Null(nav.CurrentTargetPosition);
    }

    [Fact]
    public void IsPaused__FalseInitially()
    {
        var nav = new IndoorNavigator(SquareGraph(), StartPos, random: new Random(42));
        Assert.False(nav.IsPaused);
    }

    [Fact]
    public void Update__DoesNotAdvanceDuringPause()
    {
        var nav = new IndoorNavigator(TwoNodeGraph(), StartPos,
            pauseDuration: 5f, random: new Random(0));

        // Arrive to trigger pause.
        nav.Update(new Vector2(100f, 120f), true, 1f / 60f);
        Assert.True(nav.IsPaused);

        var routeAfterPause = nav.CurrentRoute;
        var indexAfterPause = nav.RouteNodeIndex;

        // Update a few frames — should remain paused.
        for (int i = 0; i < 10; i++)
            nav.Update(new Vector2(100f, 120f), false, 1f / 60f);

        Assert.True(nav.IsPaused);
        Assert.Same(routeAfterPause, nav.CurrentRoute);
        Assert.Equal(indexAfterPause, nav.RouteNodeIndex);
    }
}
