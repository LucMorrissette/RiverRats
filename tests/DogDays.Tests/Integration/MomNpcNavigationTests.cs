using System;
using Microsoft.Xna.Framework;
using DogDays.Game.Entities;
using DogDays.Game.World;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Integration;

/// <summary>
/// Integration tests verifying Mom NPC graph-based navigation across
/// multi-frame simulations including collision and repath behavior.
/// </summary>
public class MomNpcNavigationTests
{
    private static readonly Point FrameSize = new(32, 32);

    /// <summary>
    /// Creates a cabin-like 6-node graph routed around furniture obstacles.
    /// Layout (roughly):
    ///   1(170,100) -- 2(260,100)
    ///       |              |
    ///   6(50,100)    3(260,170)
    ///       |              |
    ///   5(50,170)  -- 4(170,170)
    /// </summary>
    private static IndoorNavGraph CabinGraph()
    {
        var nodes = new IndoorNavNode[]
        {
            new(1, new Vector2(170f, 100f), "top-right-shelf", "idle"),
            new(2, new Vector2(260f, 100f), "far-right-top", null),
            new(3, new Vector2(260f, 170f), "far-right-bottom", null),
            new(4, new Vector2(170f, 170f), "couch-bottom", "idle"),
            new(5, new Vector2(50f, 170f), "left-bottom", "idle"),
            new(6, new Vector2(50f, 100f), "left-top", null),
        };
        var links = new IndoorNavLink[]
        {
            new(1, 2),  // top edge
            new(2, 3),  // right edge
            new(3, 4),  // bottom-right
            new(4, 5),  // bottom edge
            new(5, 6),  // left edge
            new(6, 1),  // top-left to top-right
        };
        return new IndoorNavGraph(nodes, links);
    }

    [Fact]
    public void Mom__TraversesCabinGraph__WithoutEnteringFurnitureBounds()
    {
        var graph = CabinGraph();
        // Furniture collision rectangles from the cabin map.
        var couchBounds = new Rectangle(134, 121, 24, 80);
        var tvBounds = new Rectangle(5, 115, 16, 32);
        var consoleBounds = new Rectangle(31, 159, 16, 10);
        var plantBounds = new Rectangle(2, 169, 32, 34);

        var collision = new FurnitureCollisionData(couchBounds, tvBounds, consoleBounds, plantBounds);
        var mom = new MomNpc(new Vector2(170f, 100f), FrameSize, graph,
            pauseDuration: 0.1f, random: new Random(42));

        // Simulate 30 seconds of patrol (1800 frames at 60fps).
        for (var i = 0; i < 1800; i++)
        {
            mom.Update(FakeGameTime.OneFrame(), collision);

            var foot = mom.FootBounds;
            Assert.False(couchBounds.Intersects(foot),
                $"Frame {i}: Mom foot bounds {foot} intersected couch {couchBounds}");
            Assert.False(tvBounds.Intersects(foot),
                $"Frame {i}: Mom foot bounds {foot} intersected TV {tvBounds}");
        }
    }

    [Fact]
    public void Mom__RecoversWhenPlayerBlocksRoute()
    {
        var graph = CabinGraph();
        var mom = new MomNpc(new Vector2(170f, 100f), FrameSize, graph,
            pauseDuration: 0.1f, random: new Random(42));

        // Simulate a 5-second block (player standing in the way).
        var totalBlock = new DelegateCollisionData(_ => true);
        for (var i = 0; i < 300; i++)
            mom.Update(FakeGameTime.OneFrame(), totalBlock);

        // After being blocked, Mom should have repathed (not frozen forever).
        // Verify she has a valid route or destination.
        Assert.True(mom.DestinationNodeId != -1 || mom.CurrentRoute.Count > 0,
            "Mom should have repathed or picked a new destination after being blocked.");

        // Now remove the block and verify she resumes movement.
        var noBlock = new DelegateCollisionData(_ => false);
        var posBeforeResume = mom.Position;

        for (var i = 0; i < 60; i++)
            mom.Update(FakeGameTime.OneFrame(), noBlock);

        Assert.NotEqual(posBeforeResume, mom.Position);
    }

    [Fact]
    public void Mom__CompletesFullPatrolCycle__AcrossMultipleDestinations()
    {
        var graph = CabinGraph();
        var mom = new MomNpc(new Vector2(170f, 100f), FrameSize, graph,
            pauseDuration: 0.1f, random: new Random(42));

        var noBlock = new DelegateCollisionData(_ => false);
        var destinationsVisited = new System.Collections.Generic.HashSet<int>();

        // Simulate 60 seconds of patrol — should visit multiple destinations.
        for (var i = 0; i < 3600; i++)
        {
            mom.Update(FakeGameTime.OneFrame(), noBlock);

            // Track when Mom arrives at a destination (route exhausted, pausing).
            if (!mom.IsMoving && mom.CurrentRoute.Count > 0)
            {
                var lastNode = mom.CurrentRoute[mom.CurrentRoute.Count - 1];
                destinationsVisited.Add(lastNode.Id);
            }
        }

        Assert.True(destinationsVisited.Count >= 2,
            $"Mom should visit at least 2 different destinations, visited {destinationsVisited.Count}.");
    }

    [Fact]
    public void Mom__MovesAlongGraphEdges__NotThroughFurniture()
    {
        // Simple 3-node L-shaped route: go right then down (around an obstacle).
        var nodes = new IndoorNavNode[]
        {
            new(1, new Vector2(50f, 50f), null, null),
            new(2, new Vector2(200f, 50f), null, null),
            new(3, new Vector2(200f, 200f), null, null),
        };
        var links = new IndoorNavLink[] { new(1, 2), new(2, 3) };
        var graph = new IndoorNavGraph(nodes, links);

        // Wall at (100-150, 50-200) — a direct path from node 1 to 3 would go
        // through it, but the graph L-route goes around it.
        var wall = new Rectangle(100, 80, 50, 120);
        var collision = new DelegateCollisionData(r => wall.Intersects(r));

        var mom = new MomNpc(new Vector2(50f, 50f), FrameSize, graph, random: new Random(0));

        // Walk for 10 seconds.
        for (var i = 0; i < 600; i++)
        {
            mom.Update(FakeGameTime.OneFrame(), collision);
            Assert.False(wall.Intersects(mom.FootBounds),
                $"Frame {i}: Mom walked through the wall obstacle.");
        }
    }

    private sealed class FurnitureCollisionData : IMapCollisionData
    {
        private readonly Rectangle[] _obstacles;

        public FurnitureCollisionData(params Rectangle[] obstacles)
        {
            _obstacles = obstacles;
        }

        public bool IsWorldRectangleBlocked(Rectangle worldBounds)
        {
            for (var i = 0; i < _obstacles.Length; i++)
            {
                if (_obstacles[i].Intersects(worldBounds))
                    return true;
            }
            return false;
        }
    }

    private sealed class DelegateCollisionData : IMapCollisionData
    {
        private readonly Func<Rectangle, bool> _isBlocked;
        public DelegateCollisionData(Func<Rectangle, bool> isBlocked) => _isBlocked = isBlocked;
        public bool IsWorldRectangleBlocked(Rectangle worldBounds) => _isBlocked(worldBounds);
    }
}
