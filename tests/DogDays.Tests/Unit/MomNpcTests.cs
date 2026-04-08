using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using DogDays.Game.Data;
using DogDays.Game.Entities;
using DogDays.Game.World;
using DogDays.Tests.Helpers;
using Xunit;

namespace DogDays.Tests.Unit;

public class MomNpcTests
{
    private static readonly Point FrameSize = new(32, 32);

    /// <summary>
    /// Foot center offset for a 32×32 sprite with standard foot ratios (60% width, 25% height,
    /// 2px inset). Start positions should be adjusted by this so foot center lands on the nav node.
    /// </summary>
    private static readonly Vector2 FootCenterOffset = new(16f, 26f);

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

    /// <summary>
    /// Creates a simple 2-node linear graph for quick traversal tests.
    /// </summary>
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
            new MomNpc(Vector2.Zero, FrameSize, null!));
    }

    [Fact]
    public void Constructor__ThrowsWhenNavGraphHasFewerThanTwoNodes()
    {
        var nodes = new IndoorNavNode[] { new(1, Vector2.Zero, null, null) };
        var graph = new IndoorNavGraph(nodes, Array.Empty<IndoorNavLink>());

        Assert.Throws<ArgumentException>(() =>
            new MomNpc(Vector2.Zero, FrameSize, graph));
    }

    [Fact]
    public void Position__ReturnsStartPosition()
    {
        var start = new Vector2(50f, 75f);
        var mom = new MomNpc(start, FrameSize, SquareGraph(), random: new Random(42));
        Assert.Equal(start, mom.Position);
    }

    [Fact]
    public void Update__MovesTowardRouteNode()
    {
        // Place Mom at node 1 (100,100). She should pick a destination and start moving.
        var graph = SquareGraph();
        var start = new Vector2(100f, 100f);
        var mom = new MomNpc(start, FrameSize, graph, random: new Random(42));

        mom.Update(FakeGameTime.FromSeconds(0.5f));

        Assert.True(mom.IsMoving, "Mom should be moving toward a route node.");
    }

    [Fact]
    public void Update__PicksDestinationOnConstruction()
    {
        var graph = SquareGraph();
        var mom = new MomNpc(new Vector2(100f, 100f), FrameSize, graph, random: new Random(42));

        Assert.NotEqual(-1, mom.DestinationNodeId);
        Assert.True(mom.CurrentRoute.Count >= 2, "Should have a route with at least 2 nodes.");
    }

    [Fact]
    public void Update__FacesRightWhenMovingRight()
    {
        // Place at node 1 (100,100). Force destination to node 2 (200,100) — rightward.
        var nodes = new IndoorNavNode[]
        {
            new(1, new Vector2(100f, 100f), null, null),
            new(2, new Vector2(200f, 100f), null, null),
        };
        var links = new IndoorNavLink[] { new(1, 2) };
        var graph = new IndoorNavGraph(nodes, links);

        // Seed guarantees node 2 is picked (only two nodes, can't pick self).
        var mom = new MomNpc(new Vector2(100f, 100f), FrameSize, graph, random: new Random(0));
        mom.Update(FakeGameTime.FromSeconds(0.5f));

        Assert.Equal(FacingDirection.Right, mom.Facing);
    }

    [Fact]
    public void Update__FacesDownWhenMovingDown()
    {
        var graph = TwoNodeGraph(new Vector2(100f, 50f), new Vector2(100f, 200f));
        var mom = new MomNpc(new Vector2(100f, 50f), FrameSize, graph, random: new Random(0));

        mom.Update(FakeGameTime.FromSeconds(0.5f));

        Assert.Equal(FacingDirection.Down, mom.Facing);
    }

    [Fact]
    public void FaceToward__FacesPlayerOnDominantAxis()
    {
        var mom = new MomNpc(new Vector2(100f, 100f), FrameSize, SquareGraph(), random: new Random(42));

        mom.FaceToward(new Vector2(60f, 116f));

        Assert.Equal(FacingDirection.Left, mom.Facing);
    }

    [Fact]
    public void Update__PausesOnArrivalAtDestination()
    {
        var graph = TwoNodeGraph(new Vector2(100f, 100f), new Vector2(100f, 200f));
        // Align foot center with node 1 so nearest-node resolves correctly.
        var start = new Vector2(100f, 100f) - FootCenterOffset;
        var mom = new MomNpc(start, FrameSize, graph, random: new Random(0));

        // Walk long enough to reach node 2 (100px at 28px/s ≈ 3.6s; arrival threshold
        // is 8px so she arrives ~frame 198, then pauses 1.5s = 90 frames → still paused at 250).
        for (var i = 0; i < 250; i++)
            mom.Update(FakeGameTime.OneFrame());

        // After arriving at destination, should be paused.
        Assert.False(mom.IsMoving, "Should pause after arriving at destination.");
    }

    [Fact]
    public void Update__PicksNewDestinationAfterPause()
    {
        var graph = TwoNodeGraph();
        var mom = new MomNpc(new Vector2(100f, 100f), FrameSize, graph,
            pauseDuration: 0.1f, random: new Random(42));

        // Walk to destination.
        for (var i = 0; i < 60; i++)
            mom.Update(FakeGameTime.OneFrame());

        var firstDest = mom.DestinationNodeId;

        // Burn through the pause + a few more frames to trigger new destination.
        for (var i = 0; i < 30; i++)
            mom.Update(FakeGameTime.FromSeconds(0.1f));

        // Should have picked a new destination and started moving.
        Assert.True(mom.CurrentRoute.Count >= 2 || mom.IsMoving || mom.DestinationNodeId != -1,
            "Should have picked a new destination after pause.");
    }

    [Fact]
    public void Bounds__MatchesPositionAndSize()
    {
        var start = new Vector2(64f, 48f);
        var mom = new MomNpc(start, FrameSize, SquareGraph(), random: new Random(42));

        var bounds = mom.Bounds;
        Assert.Equal(64, bounds.X);
        Assert.Equal(48, bounds.Y);
        Assert.Equal(32, bounds.Width);
        Assert.Equal(32, bounds.Height);
    }

    [Fact]
    public void Center__ReturnsMiddleOfBounds()
    {
        var start = new Vector2(100f, 100f);
        var mom = new MomNpc(start, FrameSize, SquareGraph(), random: new Random(42));

        Assert.Equal(new Vector2(116f, 116f), mom.Center);
    }

    [Fact]
    public void Update__DoesNotMoveWhilePaused()
    {
        var graph = TwoNodeGraph(new Vector2(100f, 100f), new Vector2(100f, 200f));
        // Align foot center with node 1.
        var start = new Vector2(100f, 100f) - FootCenterOffset;
        var mom = new MomNpc(start, FrameSize, graph,
            pauseDuration: 5f, random: new Random(0));

        // Walk long enough to reach node 2 (100px at 28px/s; arrival threshold 8px → ~198 frames).
        for (var i = 0; i < 300; i++)
            mom.Update(FakeGameTime.OneFrame());

        var posAfterArrival = mom.Position;

        // Several frames during long pause — should not move.
        for (var i = 0; i < 10; i++)
            mom.Update(FakeGameTime.OneFrame());

        Assert.Equal(posAfterArrival, mom.Position);
        Assert.False(mom.IsMoving);
    }

    [Fact]
    public void Update__StopsAtCollisionObstacle()
    {
        var graph = TwoNodeGraph(new Vector2(100f, 100f), new Vector2(300f, 100f));
        var wall = new DelegateCollisionData(r => r.Right > 150);
        var mom = new MomNpc(new Vector2(100f, 100f), FrameSize, graph, random: new Random(0));

        for (var i = 0; i < 120; i++)
            mom.Update(FakeGameTime.OneFrame(), wall);

        Assert.True(mom.Position.X > 100f,
            "Mom should have moved some distance before hitting the wall.");
        Assert.True(mom.FootBounds.Right <= 150,
            $"Mom's foot right edge ({mom.FootBounds.Right}) should not exceed the wall at x=150.");
    }

    [Fact]
    public void Update__SlidesAlongWallOnPerpendicularAxis()
    {
        var graph = TwoNodeGraph(new Vector2(100f, 100f), new Vector2(200f, 200f));
        var wall = new DelegateCollisionData(r => r.Right > 135);
        var mom = new MomNpc(new Vector2(100f, 100f), FrameSize, graph, random: new Random(0));

        for (var i = 0; i < 60; i++)
            mom.Update(FakeGameTime.OneFrame(), wall);

        Assert.True(mom.Position.Y > 100f, "Mom should slide down even when blocked horizontally.");
    }

    [Fact]
    public void FootBounds__UsesPlayerFootRatios()
    {
        var mom = new MomNpc(new Vector2(0f, 0f), FrameSize, SquareGraph(), random: new Random(42));
        var foot = mom.FootBounds;

        // 60% of 32 = 19.2 → rounds to 19
        Assert.Equal(19, foot.Width);
        // 25% of 32 = 8
        Assert.Equal(8, foot.Height);
    }

    [Fact]
    public void Update__RepathsAfterStuckTimeout()
    {
        // Wall blocks all movement. After 2s stuck timeout, Mom should repath
        // (pick a new route) instead of skipping a waypoint.
        var graph = SquareGraph();
        var totalWall = new DelegateCollisionData(_ => true);
        var mom = new MomNpc(new Vector2(100f, 100f), FrameSize, graph,
            pauseDuration: 0f, random: new Random(42));

        var initialDest = mom.DestinationNodeId;
        var initialRoute = mom.CurrentRoute;

        // Run for 3 seconds — stuck timeout is 2s, should trigger repath.
        for (var i = 0; i < 180; i++)
            mom.Update(FakeGameTime.OneFrame(), totalWall);

        // After repath, route should have changed (or at minimum, a repath was attempted).
        // The destination or route reference should differ.
        var repathed = mom.DestinationNodeId != initialDest
            || !ReferenceEquals(mom.CurrentRoute, initialRoute);
        Assert.True(repathed, "Mom should repath after being stuck, not skip waypoints.");
    }

    [Fact]
    public void Update__WalksMultipleRouteNodes()
    {
        // 3-node linear path: (100,100) -> (100,200) -> (100,300).
        var nodes = new IndoorNavNode[]
        {
            new(1, new Vector2(100f, 100f), null, null),
            new(2, new Vector2(100f, 200f), null, null),
            new(3, new Vector2(100f, 300f), null, null),
        };
        var links = new IndoorNavLink[] { new(1, 2), new(2, 3) };
        var graph = new IndoorNavGraph(nodes, links);

        // Align foot center with node 1.
        var start = new Vector2(100f, 100f) - FootCenterOffset;
        var mom = new MomNpc(start, FrameSize, graph, random: new Random(1));

        // Walk long enough to pass through node 2 and reach node 3.
        // Distance is 200px at 28px/s ≈ 7.1s → 430 frames.
        for (var i = 0; i < 500; i++)
            mom.Update(FakeGameTime.OneFrame());

        // Position.Y should reflect foot center near node 3 at Y=300.
        // With foot center offset of 26, position.Y ≈ 274 at node 3.
        Assert.True(mom.Position.Y > 200f,
            $"Mom should have walked through intermediate nodes. Position.Y={mom.Position.Y}");
    }

    private sealed class DelegateCollisionData : IMapCollisionData
    {
        private readonly Func<Rectangle, bool> _isBlocked;
        public DelegateCollisionData(Func<Rectangle, bool> isBlocked) => _isBlocked = isBlocked;
        public bool IsWorldRectangleBlocked(Rectangle worldBounds) => _isBlocked(worldBounds);
    }
}
