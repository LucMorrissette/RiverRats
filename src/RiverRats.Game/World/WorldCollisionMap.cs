using Microsoft.Xna.Framework;

namespace RiverRats.Game.World;

/// <summary>
/// Aggregates terrain collision with additional static world-space obstacle bounds.
/// </summary>
public sealed class WorldCollisionMap : IMapCollisionData
{
    private readonly IMapCollisionData _terrainCollisionData;
    private readonly Rectangle[] _staticObstacleBounds;
    private readonly Rectangle[] _walkableOverrideBounds;

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

        return false;
    }

    private bool IsWithinWalkableOverride(Rectangle worldBounds)
    {
        for (var i = 0; i < _walkableOverrideBounds.Length; i++)
        {
            if (_walkableOverrideBounds[i].Contains(worldBounds))
            {
                return true;
            }
        }

        return false;
    }
}