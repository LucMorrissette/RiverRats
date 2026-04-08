#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using DogDays.Game.World;

namespace DogDays.Game.Components;

/// <summary>
/// Reusable navigation component for indoor NPCs. Picks random destination
/// nodes on an <see cref="IndoorNavGraph"/>, computes A* routes, advances
/// through the route as the owner reports movement, and repaths when stuck.
/// Does not perform movement itself — the owning entity reads the current
/// target position and steers toward it.
/// </summary>
public sealed class IndoorNavigator
{
    private const float NodeArrivalThresholdSq = 8f * 8f;
    private const float DefaultPauseDuration = 1.5f;
    private const float StuckTimeoutSeconds = 2f;
    private const int MaxDestinationAttempts = 10;

    private readonly IndoorNavGraph _navGraph;
    private readonly Random _random;
    private readonly float _pauseDuration;

    private IReadOnlyList<IndoorNavNode> _currentRoute = [];
    private int _routeNodeIndex;
    private int _destinationNodeId = -1;

    private float _pauseTimer;
    private float _stuckTimer;

    /// <summary>
    /// Creates a navigator backed by the supplied graph.
    /// </summary>
    /// <param name="navGraph">Indoor navigation graph with at least two nodes.</param>
    /// <param name="startPosition">World position to seed the first route from.</param>
    /// <param name="pauseDuration">Seconds to pause at each destination before picking a new one.</param>
    /// <param name="random">Random instance for destination selection. Uses a default if null.</param>
    public IndoorNavigator(IndoorNavGraph navGraph, Vector2 startPosition,
        float pauseDuration = DefaultPauseDuration, Random? random = null)
    {
        if (navGraph is null || navGraph.Nodes.Count < 2)
            throw new ArgumentException("Navigation graph must have at least two nodes.", nameof(navGraph));

        _navGraph = navGraph;
        _pauseDuration = pauseDuration;
        _random = random ?? new Random();

        PickNewDestination(startPosition);
    }

    /// <summary>The node id of the current destination, or -1 if none.</summary>
    public int DestinationNodeId => _destinationNodeId;

    /// <summary>The current A* route being followed (empty if no route).</summary>
    public IReadOnlyList<IndoorNavNode> CurrentRoute => _currentRoute;

    /// <summary>Index within <see cref="CurrentRoute"/> of the node currently being approached.</summary>
    public int RouteNodeIndex => _routeNodeIndex;

    /// <summary>Whether the navigator is in a pause state (arrived and waiting).</summary>
    public bool IsPaused => _pauseTimer > 0f;

    /// <summary>
    /// Returns the world-space position of the current target node,
    /// or <c>null</c> if the navigator has no active route.
    /// </summary>
    public Vector2? CurrentTargetPosition
    {
        get
        {
            if (_currentRoute.Count == 0 || _routeNodeIndex >= _currentRoute.Count)
                return null;
            return _currentRoute[_routeNodeIndex].Position;
        }
    }

    /// <summary>
    /// Advances the navigation state each frame. Call this before moving the entity.
    /// </summary>
    /// <param name="currentPosition">The entity's current world position.</param>
    /// <param name="moved">Whether the entity actually moved last frame (for stuck detection).</param>
    /// <param name="elapsed">Elapsed time in seconds this frame.</param>
    public void Update(Vector2 currentPosition, bool moved, float elapsed)
    {
        if (_pauseTimer > 0f)
        {
            _pauseTimer -= elapsed;
            return;
        }

        // No route — pick a new destination.
        if (_currentRoute.Count == 0 || _routeNodeIndex >= _currentRoute.Count)
        {
            PickNewDestination(currentPosition);
            return;
        }

        var target = _currentRoute[_routeNodeIndex].Position;
        var distanceSq = Vector2.DistanceSquared(currentPosition, target);

        if (distanceSq <= NodeArrivalThresholdSq)
        {
            _routeNodeIndex++;

            // Reached the final destination node — pause and pick a new one next frame.
            if (_routeNodeIndex >= _currentRoute.Count)
            {
                _pauseTimer = _pauseDuration;
                _stuckTimer = 0f;
                return;
            }

            // Intermediate node — keep walking.
            _stuckTimer = 0f;
            return;
        }

        // Stuck detection.
        if (!moved)
        {
            _stuckTimer += elapsed;
            if (_stuckTimer >= StuckTimeoutSeconds)
            {
                _stuckTimer = 0f;
                Repath(currentPosition);
            }
        }
        else
        {
            _stuckTimer = 0f;
        }
    }

    /// <summary>
    /// Picks a random destination node and computes a route from the nearest node.
    /// </summary>
    private void PickNewDestination(Vector2 currentPosition)
    {
        var nearest = _navGraph.FindNearestNode(currentPosition);
        if (nearest == null)
        {
            _currentRoute = [];
            return;
        }

        for (int attempt = 0; attempt < MaxDestinationAttempts; attempt++)
        {
            var candidateIndex = _random.Next(_navGraph.Nodes.Count);
            var candidate = _navGraph.Nodes[candidateIndex];

            if (candidate.Id == nearest.Id)
                continue;

            var route = _navGraph.FindRoute(nearest.Id, candidate.Id);
            if (route.Count >= 2)
            {
                _currentRoute = route;
                _routeNodeIndex = 1; // Skip start node (we're already near it).
                _destinationNodeId = candidate.Id;
                _stuckTimer = 0f;
                return;
            }
        }

        // Fallback: couldn't find a valid destination — pause and try again later.
        _currentRoute = [];
        _destinationNodeId = -1;
        _pauseTimer = _pauseDuration;
    }

    /// <summary>
    /// Repaths from the nearest reachable node to a new destination.
    /// Called when the entity is stuck and can't make progress.
    /// </summary>
    private void Repath(Vector2 currentPosition)
    {
        var nearest = _navGraph.FindNearestNode(currentPosition);
        if (nearest == null)
        {
            _currentRoute = [];
            return;
        }

        for (int attempt = 0; attempt < MaxDestinationAttempts; attempt++)
        {
            var candidateIndex = _random.Next(_navGraph.Nodes.Count);
            var candidate = _navGraph.Nodes[candidateIndex];

            if (candidate.Id == nearest.Id)
                continue;

            if (attempt < MaxDestinationAttempts / 2 && candidate.Id == _destinationNodeId)
                continue;

            var route = _navGraph.FindRoute(nearest.Id, candidate.Id);
            if (route.Count >= 2)
            {
                _currentRoute = route;
                _routeNodeIndex = 1;
                _destinationNodeId = candidate.Id;
                return;
            }
        }

        // Fallback: pause and try again next cycle.
        _currentRoute = [];
        _destinationNodeId = -1;
        _pauseTimer = _pauseDuration;
    }
}
