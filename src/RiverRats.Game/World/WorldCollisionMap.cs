using Microsoft.Xna.Framework;

namespace RiverRats.Game.World;

/// <summary>
/// Aggregates terrain collision with additional static world-space obstacle bounds.
/// </summary>
public sealed class WorldCollisionMap : IMapCollisionData
{
    private readonly IMapCollisionData _terrainCollisionData;
    private readonly Rectangle[] _staticObstacleBounds;

    /// <summary>
    /// Creates a shared collision source for terrain and placed world obstacles.
    /// </summary>
    /// <param name="terrainCollisionData">Base terrain collision provider.</param>
    /// <param name="staticObstacleBounds">Additional world-space solid rectangles.</param>
    public WorldCollisionMap(IMapCollisionData terrainCollisionData, Rectangle[] staticObstacleBounds)
    {
        _terrainCollisionData = terrainCollisionData;
        _staticObstacleBounds = staticObstacleBounds;
    }

    /// <inheritdoc />
    public bool IsWorldRectangleBlocked(Rectangle worldBounds)
    {
        if (_terrainCollisionData.IsWorldRectangleBlocked(worldBounds))
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
}