#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace DogDays.Game.World;

/// <summary>
/// A navigation graph for indoor maps, built from nodes and links.
/// Provides nearest-node lookup and A* pathfinding.
/// </summary>
public sealed class IndoorNavGraph
{
    private readonly IReadOnlyList<IndoorNavNode> _nodes;
    private readonly IReadOnlyList<IndoorNavLink> _links;
    private readonly Dictionary<int, IndoorNavNode> _nodeById;
    private readonly Dictionary<int, List<int>> _adjacency;

    /// <summary>
    /// All nodes in this graph.
    /// </summary>
    public IReadOnlyList<IndoorNavNode> Nodes => _nodes;

    /// <summary>
    /// All links in this graph.
    /// </summary>
    public IReadOnlyList<IndoorNavLink> Links => _links;

    /// <summary>
    /// Creates a navigation graph from a set of nodes and links.
    /// </summary>
    /// <param name="nodes">The navigable points in the graph.</param>
    /// <param name="links">Bidirectional connections between nodes.</param>
    public IndoorNavGraph(IReadOnlyList<IndoorNavNode> nodes, IReadOnlyList<IndoorNavLink> links)
    {
        _nodes = nodes;
        _links = links;

        _nodeById = new Dictionary<int, IndoorNavNode>(nodes.Count);
        for (int i = 0; i < nodes.Count; i++)
        {
            _nodeById[nodes[i].Id] = nodes[i];
        }

        _adjacency = new Dictionary<int, List<int>>(nodes.Count);
        for (int i = 0; i < nodes.Count; i++)
        {
            _adjacency[nodes[i].Id] = new List<int>();
        }

        for (int i = 0; i < links.Count; i++)
        {
            var link = links[i];
            if (_adjacency.TryGetValue(link.NodeIdA, out var listA))
            {
                listA.Add(link.NodeIdB);
            }
            if (_adjacency.TryGetValue(link.NodeIdB, out var listB))
            {
                listB.Add(link.NodeIdA);
            }
        }
    }

    /// <summary>
    /// Returns the node with the given id, or <c>null</c> if not found.
    /// </summary>
    /// <param name="id">The node id to look up.</param>
    public IndoorNavNode? GetNode(int id)
    {
        return _nodeById.TryGetValue(id, out var node) ? node : null;
    }

    /// <summary>
    /// Returns the node closest to the given world-space position,
    /// or <c>null</c> if the graph contains no nodes.
    /// Uses <see cref="Vector2.DistanceSquared"/> for efficiency.
    /// </summary>
    /// <param name="worldPosition">The position to search from.</param>
    public IndoorNavNode? FindNearestNode(Vector2 worldPosition)
    {
        if (_nodes.Count == 0)
            return null;

        IndoorNavNode? nearest = null;
        float bestDistSq = float.MaxValue;

        for (int i = 0; i < _nodes.Count; i++)
        {
            float distSq = Vector2.DistanceSquared(worldPosition, _nodes[i].Position);
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                nearest = _nodes[i];
            }
        }

        return nearest;
    }

    /// <summary>
    /// Removes adjacency entries for links whose straight-line path is blocked
    /// by collision. Sweeps a foot-bounds rectangle along each link at
    /// <paramref name="sweepStep"/> pixel intervals. Node positions are treated
    /// as foot center positions.
    /// </summary>
    /// <param name="collisionMap">World collision data to test against.</param>
    /// <param name="footSize">Foot collision rectangle size (width, height) in pixels.</param>
    /// <param name="sweepStep">Distance in pixels between sweep samples.</param>
    /// <returns>Number of links pruned.</returns>
    public int PruneBlockedLinks(IMapCollisionData collisionMap, Point footSize, float sweepStep = 4f)
    {
        int pruned = 0;

        for (int i = 0; i < _links.Count; i++)
        {
            var link = _links[i];
            var nodeA = GetNode(link.NodeIdA);
            var nodeB = GetNode(link.NodeIdB);
            if (nodeA == null || nodeB == null)
                continue;

            if (!IsSegmentClear(nodeA.Position, nodeB.Position, collisionMap, footSize, sweepStep))
            {
                if (_adjacency.TryGetValue(link.NodeIdA, out var listA))
                    listA.Remove(link.NodeIdB);
                if (_adjacency.TryGetValue(link.NodeIdB, out var listB))
                    listB.Remove(link.NodeIdA);

                Debug.WriteLine(
                    $"[NavGraph] Pruned blocked link: {nodeA.Name ?? nodeA.Id.ToString()} → " +
                    $"{nodeB.Name ?? nodeB.Id.ToString()} ({nodeA.Position} → {nodeB.Position})");
                pruned++;
            }
        }

        return pruned;
    }

    /// <summary>
    /// Returns <c>true</c> if a foot-bounds rectangle can sweep from
    /// <paramref name="from"/> to <paramref name="to"/> without collision.
    /// Positions are treated as foot center (foot bounds are centered on them).
    /// </summary>
    internal static bool IsSegmentClear(Vector2 from, Vector2 to,
        IMapCollisionData collisionMap, Point footSize, float sweepStep)
    {
        var delta = to - from;
        float distance = delta.Length();
        if (distance < 0.001f)
            return !IsFootBlocked(from, collisionMap, footSize);

        int steps = Math.Max(1, (int)MathF.Ceiling(distance / sweepStep));

        for (int s = 0; s <= steps; s++)
        {
            float t = (float)s / steps;
            var pos = Vector2.Lerp(from, to, t);
            if (IsFootBlocked(pos, collisionMap, footSize))
                return false;
        }

        return true;
    }

    private static bool IsFootBlocked(Vector2 footCenter, IMapCollisionData collisionMap, Point footSize)
    {
        var left = (int)MathF.Round(footCenter.X - footSize.X / 2f);
        var top = (int)MathF.Round(footCenter.Y - footSize.Y / 2f);
        return collisionMap.IsWorldRectangleBlocked(new Rectangle(left, top, footSize.X, footSize.Y));
    }

    /// <summary>
    /// Finds a route from <paramref name="startNodeId"/> to <paramref name="goalNodeId"/>
    /// using A* pathfinding with Euclidean distance as the heuristic.
    /// </summary>
    /// <param name="startNodeId">The id of the start node.</param>
    /// <param name="goalNodeId">The id of the goal node.</param>
    /// <returns>
    /// An ordered list of nodes from start to goal (inclusive).
    /// Returns an empty list if no route exists or either id is invalid.
    /// </returns>
    public IReadOnlyList<IndoorNavNode> FindRoute(int startNodeId, int goalNodeId)
    {
        if (!_nodeById.TryGetValue(startNodeId, out var startNode) ||
            !_nodeById.TryGetValue(goalNodeId, out var goalNode))
        {
            return [];
        }

        if (startNodeId == goalNodeId)
        {
            return [startNode];
        }

        // g-score: best known cost from start to each node
        var gScore = new Dictionary<int, float> { [startNodeId] = 0f };

        // Track which node each node was reached from
        var cameFrom = new Dictionary<int, int>();

        // Open set as a min-priority queue ordered by f-score (g + heuristic)
        var openSet = new PriorityQueue<int, float>();
        float hStart = Vector2.Distance(startNode.Position, goalNode.Position);
        openSet.Enqueue(startNodeId, hStart);

        // Closed set to avoid re-processing
        var closedSet = new HashSet<int>();

        while (openSet.Count > 0)
        {
            int currentId = openSet.Dequeue();

            if (currentId == goalNodeId)
            {
                return ReconstructPath(cameFrom, currentId);
            }

            if (!closedSet.Add(currentId))
                continue;

            var currentNode = _nodeById[currentId];
            float currentG = gScore[currentId];

            if (!_adjacency.TryGetValue(currentId, out var neighbors))
                continue;

            for (int i = 0; i < neighbors.Count; i++)
            {
                int neighborId = neighbors[i];
                if (closedSet.Contains(neighborId))
                    continue;

                var neighborNode = _nodeById[neighborId];
                float tentativeG = currentG + Vector2.Distance(currentNode.Position, neighborNode.Position);

                if (!gScore.TryGetValue(neighborId, out float existingG) || tentativeG < existingG)
                {
                    gScore[neighborId] = tentativeG;
                    cameFrom[neighborId] = currentId;
                    float fScore = tentativeG + Vector2.Distance(neighborNode.Position, goalNode.Position);
                    openSet.Enqueue(neighborId, fScore);
                }
            }
        }

        // No path found
        return [];
    }

    private IReadOnlyList<IndoorNavNode> ReconstructPath(Dictionary<int, int> cameFrom, int currentId)
    {
        var path = new List<IndoorNavNode> { _nodeById[currentId] };
        while (cameFrom.TryGetValue(currentId, out int previousId))
        {
            currentId = previousId;
            path.Add(_nodeById[currentId]);
        }
        path.Reverse();
        return path;
    }
}
