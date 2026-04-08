using System;
using Microsoft.Xna.Framework;
using DogDays.Game.World;
using Xunit;

namespace DogDays.Tests.Unit;

public class IndoorNavGraphTests
{
    private static IndoorNavNode Node(int id, float x, float y, string? name = null, string? tags = null)
        => new(id, new Vector2(x, y), name, tags);

    // ── Graph construction ──────────────────────────────────────────

    [Fact]
    public void Constructor__WithNodesAndLinks__BuildsAdjacencyCorrectly()
    {
        var nodes = new[] { Node(0, 0, 0), Node(1, 50, 0), Node(2, 100, 0) };
        var links = new[] { new IndoorNavLink(0, 1), new IndoorNavLink(1, 2) };

        var graph = new IndoorNavGraph(nodes, links);

        Assert.Equal(3, graph.Nodes.Count);
        Assert.Equal(2, graph.Links.Count);
    }

    [Fact]
    public void Constructor__EmptyGraph__CreatesWithoutError()
    {
        var graph = new IndoorNavGraph(
            Array.Empty<IndoorNavNode>(),
            Array.Empty<IndoorNavLink>());

        Assert.Empty(graph.Nodes);
        Assert.Empty(graph.Links);
    }

    // ── GetNode ─────────────────────────────────────────────────────

    [Fact]
    public void GetNode__ExistingId__ReturnsNode()
    {
        var nodes = new[] { Node(0, 0, 0, "Start"), Node(1, 50, 0, "End") };
        var graph = new IndoorNavGraph(nodes, Array.Empty<IndoorNavLink>());

        var result = graph.GetNode(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("End", result.Name);
    }

    [Fact]
    public void GetNode__NonExistentId__ReturnsNull()
    {
        var nodes = new[] { Node(0, 0, 0) };
        var graph = new IndoorNavGraph(nodes, Array.Empty<IndoorNavLink>());

        var result = graph.GetNode(999);

        Assert.Null(result);
    }

    // ── FindNearestNode ─────────────────────────────────────────────

    [Fact]
    public void FindNearestNode__ExactPosition__ReturnsMatchingNode()
    {
        var nodes = new[] { Node(0, 10, 20), Node(1, 50, 60) };
        var graph = new IndoorNavGraph(nodes, Array.Empty<IndoorNavLink>());

        var result = graph.FindNearestNode(new Vector2(50, 60));

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void FindNearestNode__BetweenNodes__ReturnsCloserNode()
    {
        var nodes = new[] { Node(0, 0, 0), Node(1, 100, 0) };
        var graph = new IndoorNavGraph(nodes, Array.Empty<IndoorNavLink>());

        // Position at (30, 0) is closer to node 0 at (0, 0) than node 1 at (100, 0)
        var result = graph.FindNearestNode(new Vector2(30, 0));

        Assert.NotNull(result);
        Assert.Equal(0, result.Id);
    }

    [Fact]
    public void FindNearestNode__EmptyGraph__ReturnsNull()
    {
        var graph = new IndoorNavGraph(
            Array.Empty<IndoorNavNode>(),
            Array.Empty<IndoorNavLink>());

        var result = graph.FindNearestNode(new Vector2(10, 10));

        Assert.Null(result);
    }

    // ── FindRoute (A*) ──────────────────────────────────────────────

    [Fact]
    public void FindRoute__DirectConnection__ReturnsTwoNodes()
    {
        var nodes = new[] { Node(0, 0, 0), Node(1, 50, 0) };
        var links = new[] { new IndoorNavLink(0, 1) };
        var graph = new IndoorNavGraph(nodes, links);

        var route = graph.FindRoute(0, 1);

        Assert.Equal(2, route.Count);
        Assert.Equal(0, route[0].Id);
        Assert.Equal(1, route[1].Id);
    }

    [Fact]
    public void FindRoute__MultiHopPath__ReturnsShortestRoute()
    {
        // Diamond: A(0,0), B(50,0), C(100,0), D(50,50)
        // Links: A-B, B-C, A-D, D-C
        // A→C via A-B-C is shorter than A-D-C
        var nodes = new[]
        {
            Node(0, 0, 0, "A"),
            Node(1, 50, 0, "B"),
            Node(2, 100, 0, "C"),
            Node(3, 50, 50, "D")
        };
        var links = new[]
        {
            new IndoorNavLink(0, 1),
            new IndoorNavLink(1, 2),
            new IndoorNavLink(0, 3),
            new IndoorNavLink(3, 2)
        };
        var graph = new IndoorNavGraph(nodes, links);

        var route = graph.FindRoute(0, 2);

        Assert.Equal(3, route.Count);
        Assert.Equal(0, route[0].Id); // A
        Assert.Equal(1, route[1].Id); // B
        Assert.Equal(2, route[2].Id); // C
    }

    [Fact]
    public void FindRoute__UnreachableNode__ReturnsEmptyList()
    {
        // Node 2 is disconnected from nodes 0 and 1
        var nodes = new[] { Node(0, 0, 0), Node(1, 50, 0), Node(2, 200, 200) };
        var links = new[] { new IndoorNavLink(0, 1) };
        var graph = new IndoorNavGraph(nodes, links);

        var route = graph.FindRoute(0, 2);

        Assert.Empty(route);
    }

    [Fact]
    public void FindRoute__SameStartAndGoal__ReturnsSingleNode()
    {
        var nodes = new[] { Node(0, 0, 0), Node(1, 50, 0) };
        var links = new[] { new IndoorNavLink(0, 1) };
        var graph = new IndoorNavGraph(nodes, links);

        var route = graph.FindRoute(0, 0);

        Assert.Single(route);
        Assert.Equal(0, route[0].Id);
    }

    [Fact]
    public void FindRoute__InvalidNodeId__ReturnsEmptyList()
    {
        var nodes = new[] { Node(0, 0, 0), Node(1, 50, 0) };
        var links = new[] { new IndoorNavLink(0, 1) };
        var graph = new IndoorNavGraph(nodes, links);

        var route = graph.FindRoute(999, 0);

        Assert.Empty(route);
    }

    // ── IndoorNavNode tag parsing ───────────────────────────────────

    [Fact]
    public void IndoorNavNode__CommaSeparatedTags__ParsesCorrectly()
    {
        var node = Node(0, 0, 0, tags: "idle,lounge");

        Assert.Equal(2, node.Tags.Length);
        Assert.Equal("idle", node.Tags[0]);
        Assert.Equal("lounge", node.Tags[1]);
    }

    [Fact]
    public void IndoorNavNode__NullTags__ReturnsEmptyArray()
    {
        var node = Node(0, 0, 0, tags: null);

        Assert.Empty(node.Tags);
    }

    [Fact]
    public void IndoorNavNode__EmptyTags__ReturnsEmptyArray()
    {
        var node = Node(0, 0, 0, tags: "");

        Assert.Empty(node.Tags);
    }

    // ── IsSegmentClear ──────────────────────────────────────────────

    [Fact]
    public void IsSegmentClear__OpenPath__ReturnsTrue()
    {
        var noWalls = new DelegateCollisionData(_ => false);
        var from = new Vector2(0, 0);
        var to = new Vector2(100, 0);

        Assert.True(IndoorNavGraph.IsSegmentClear(from, to, noWalls, new Point(10, 6), 4f));
    }

    [Fact]
    public void IsSegmentClear__ObstacleMidway__ReturnsFalse()
    {
        // Block at X 48-56 (an 8-wide wall across the path)
        var wall = new DelegateCollisionData(r => r.Right > 48 && r.Left < 56);
        var from = new Vector2(0, 0);
        var to = new Vector2(100, 0);

        Assert.False(IndoorNavGraph.IsSegmentClear(from, to, wall, new Point(10, 6), 4f));
    }

    [Fact]
    public void IsSegmentClear__ObstacleAtStart__ReturnsFalse()
    {
        var blocked = new DelegateCollisionData(r => r.Left < 10);
        var from = new Vector2(0, 0);
        var to = new Vector2(100, 0);

        Assert.False(IndoorNavGraph.IsSegmentClear(from, to, blocked, new Point(10, 6), 4f));
    }

    [Fact]
    public void IsSegmentClear__ObstacleAtEnd__ReturnsFalse()
    {
        var blocked = new DelegateCollisionData(r => r.Right > 95);
        var from = new Vector2(0, 0);
        var to = new Vector2(100, 0);

        Assert.False(IndoorNavGraph.IsSegmentClear(from, to, blocked, new Point(10, 6), 4f));
    }

    [Fact]
    public void IsSegmentClear__ZeroLengthSegmentOnClearTile__ReturnsTrue()
    {
        var noWalls = new DelegateCollisionData(_ => false);

        Assert.True(IndoorNavGraph.IsSegmentClear(
            new Vector2(50, 50), new Vector2(50, 50), noWalls, new Point(10, 6), 4f));
    }

    [Fact]
    public void IsSegmentClear__ZeroLengthSegmentOnBlockedTile__ReturnsFalse()
    {
        var allBlocked = new DelegateCollisionData(_ => true);

        Assert.False(IndoorNavGraph.IsSegmentClear(
            new Vector2(50, 50), new Vector2(50, 50), allBlocked, new Point(10, 6), 4f));
    }

    // ── PruneBlockedLinks ───────────────────────────────────────────

    [Fact]
    public void PruneBlockedLinks__ClearLinks__PrunesNone()
    {
        var nodes = new[] { Node(0, 0, 0), Node(1, 50, 0), Node(2, 100, 0) };
        var links = new[] { new IndoorNavLink(0, 1), new IndoorNavLink(1, 2) };
        var graph = new IndoorNavGraph(nodes, links);
        var noWalls = new DelegateCollisionData(_ => false);

        int pruned = graph.PruneBlockedLinks(noWalls, new Point(10, 6));

        Assert.Equal(0, pruned);
        // Route should still work
        var route = graph.FindRoute(0, 2);
        Assert.Equal(3, route.Count);
    }

    [Fact]
    public void PruneBlockedLinks__BlockedLink__RemovesAdjacency()
    {
        // A(0,0) -- B(50,0) -- C(100,0)
        // Block the segment between B and C (wall at x=70..80)
        var nodes = new[] { Node(0, 0, 0, "A"), Node(1, 50, 0, "B"), Node(2, 100, 0, "C") };
        var links = new[] { new IndoorNavLink(0, 1), new IndoorNavLink(1, 2) };
        var graph = new IndoorNavGraph(nodes, links);
        var wall = new DelegateCollisionData(r => r.Right > 70 && r.Left < 80);

        int pruned = graph.PruneBlockedLinks(wall, new Point(10, 6));

        Assert.Equal(1, pruned);
        // A→B still works
        var routeAB = graph.FindRoute(0, 1);
        Assert.Equal(2, routeAB.Count);
        // A→C is now unreachable
        var routeAC = graph.FindRoute(0, 2);
        Assert.Empty(routeAC);
    }

    [Fact]
    public void PruneBlockedLinks__ReturnsCorrectCount()
    {
        // 3 links, all blocked
        var nodes = new[] { Node(0, 0, 0), Node(1, 50, 0), Node(2, 100, 0) };
        var links = new[]
        {
            new IndoorNavLink(0, 1),
            new IndoorNavLink(1, 2),
            new IndoorNavLink(0, 2)
        };
        var graph = new IndoorNavGraph(nodes, links);
        var allBlocked = new DelegateCollisionData(_ => true);

        int pruned = graph.PruneBlockedLinks(allBlocked, new Point(10, 6));

        Assert.Equal(3, pruned);
    }

    [Fact]
    public void PruneBlockedLinks__DiamondGraph__PreservesAlternateRoute()
    {
        // A(0,0), B(50,50), C(50,-50), D(100,0)
        // Links: A-B, A-C, B-D, C-D
        // Block the A-B segment, but A-C-D should survive
        var nodes = new[]
        {
            Node(0, 0, 0, "A"),
            Node(1, 50, 50, "B"),
            Node(2, 50, -50, "C"),
            Node(3, 100, 0, "D")
        };
        var links = new[]
        {
            new IndoorNavLink(0, 1),
            new IndoorNavLink(0, 2),
            new IndoorNavLink(1, 3),
            new IndoorNavLink(2, 3)
        };
        var graph = new IndoorNavGraph(nodes, links);
        // Block Y > 20 (blocks A-B link which goes through Y=25+, leaves A-C through Y=-25)
        var wall = new DelegateCollisionData(r => r.Top > 20);

        int pruned = graph.PruneBlockedLinks(wall, new Point(6, 6));

        Assert.True(pruned >= 1);
        // A→D via C should still work
        var route = graph.FindRoute(0, 3);
        Assert.True(route.Count >= 2, "Should find alternate route through C");
        Assert.Equal(0, route[0].Id);
        Assert.Equal(3, route[route.Count - 1].Id);
    }

    [Fact]
    public void PruneBlockedLinks__NodeOnObstacle__PrunesAllConnectedLinks()
    {
        // If node B is ON a collision tile, all links touching B should be pruned
        var nodes = new[] { Node(0, 0, 0, "A"), Node(1, 50, 0, "B"), Node(2, 100, 0, "C") };
        var links = new[] { new IndoorNavLink(0, 1), new IndoorNavLink(1, 2) };
        var graph = new IndoorNavGraph(nodes, links);
        // Block at B's position (X=45..55)
        var wall = new DelegateCollisionData(r => r.Right > 45 && r.Left < 55);

        int pruned = graph.PruneBlockedLinks(wall, new Point(10, 6));

        Assert.Equal(2, pruned);
        // No routes from A should exist
        var route = graph.FindRoute(0, 2);
        Assert.Empty(route);
    }

    [Fact]
    public void PruneBlockedLinks__NarrowFootBounds__PassesThroughNarrowGap()
    {
        // Wide gap (20px), narrow foot bounds (6px wide)
        // Wall at Y=0..10 but only for X < 20 and X > 40 (open gap at 20-40)
        var nodes = new[] { Node(0, 30, -20), Node(1, 30, 30) };
        var links = new[] { new IndoorNavLink(0, 1) };
        var graph = new IndoorNavGraph(nodes, links);
        // Block everything EXCEPT the column X=20..40
        var walls = new DelegateCollisionData(r => r.Left < 20 || r.Right > 40);

        int pruned = graph.PruneBlockedLinks(walls, new Point(6, 6));

        Assert.Equal(0, pruned);
    }

    // ── Helper ───────────────────────────────────────────────────────

    private sealed class DelegateCollisionData : IMapCollisionData
    {
        private readonly Func<Rectangle, bool> _isBlocked;
        public DelegateCollisionData(Func<Rectangle, bool> isBlocked) => _isBlocked = isBlocked;
        public bool IsWorldRectangleBlocked(Rectangle bounds) => _isBlocked(bounds);
    }
}
