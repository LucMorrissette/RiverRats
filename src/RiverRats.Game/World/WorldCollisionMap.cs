using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace RiverRats.Game.World;

/// <summary>
/// Aggregates terrain collision with additional static world-space obstacle bounds
/// and runtime-mutable dynamic obstacles (e.g. NPCs).
/// </summary>
public sealed class WorldCollisionMap : IMapCollisionData
{
    private readonly IMapCollisionData _terrainCollisionData;
    private readonly Rectangle[] _staticObstacleBounds;
    private readonly Rectangle[] _walkableOverrideBounds;
    private readonly List<Rectangle> _dynamicObstacles = new();

    /// <summary>
    /// Creates a shared collision source for terrain and placed world obstacles.
    /// </summary>
    /// <param name="terrainCollisionData">Base terrain collision provider.</param>
    /// <param name="staticObstacleBounds">Additional world-space solid rectangles.</param>
    public WorldCollisionMap(IMapCollisionData terrainCollisionData, Rectangle[] staticObstacleBounds)
        : this(terrainCollisionData, staticObstacleBounds, [])
    {
    }

    /// <summary>
    /// Creates a shared collision source for terrain, solid obstacles, and walkable override regions.
    /// </summary>
    /// <param name="terrainCollisionData">Base terrain collision provider.</param>
    /// <param name="staticObstacleBounds">Additional world-space solid rectangles.</param>
    /// <param name="walkableOverrideBounds">World-space rectangles that remain walkable even if terrain below is blocked.</param>
    public WorldCollisionMap(IMapCollisionData terrainCollisionData, Rectangle[] staticObstacleBounds, Rectangle[] walkableOverrideBounds)
    {
        _terrainCollisionData = terrainCollisionData;
        _staticObstacleBounds = staticObstacleBounds;
        _walkableOverrideBounds = walkableOverrideBounds;
    }

    /// <inheritdoc />
    public bool IsWorldRectangleBlocked(Rectangle worldBounds)
    {
        if (_terrainCollisionData.IsWorldRectangleBlocked(worldBounds) && !IsWithinWalkableOverride(worldBounds))
        {
            return true;
        }

        for (var i = 0; i < _staticObstacleBounds.Length; i++)
        {
            if (_staticObstacleBounds[i].Intersects(worldBounds))
            {
                return true;
            }
        }

        for (var i = 0; i < _dynamicObstacles.Count; i++)
        {
            if (_dynamicObstacles[i].Intersects(worldBounds))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Adds a dynamic obstacle that can move each frame (e.g. an NPC).
    /// Returns the index used to update or remove it later.
    /// </summary>
    public int AddDynamicObstacle(Rectangle bounds)
    {
        _dynamicObstacles.Add(bounds);
        return _dynamicObstacles.Count - 1;
    }

    /// <summary>
    /// Updates the bounds of a previously-added dynamic obstacle.
    /// </summary>
    public void UpdateDynamicObstacle(int index, Rectangle bounds)
    {
        _dynamicObstacles[index] = bounds;
    }

    /// <summary>
    /// Removes all dynamic obstacles.
    /// </summary>
    public void ClearDynamicObstacles()
    {
        _dynamicObstacles.Clear();
    }

    private bool IsWithinWalkableOverride(Rectangle worldBounds)
    {
        for (var i = 0; i < _walkableOverrideBounds.Length; i++)
        {
            if (_walkableOverrideBounds[i].Intersects(worldBounds))
            {
                return true;
            }
        }

        return false;
    }
}