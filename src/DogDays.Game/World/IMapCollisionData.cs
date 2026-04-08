using Microsoft.Xna.Framework;

namespace DogDays.Game.World;

/// <summary>
/// Provides tilemap collision queries in world-space pixels.
/// </summary>
public interface IMapCollisionData
{
    /// <summary>
    /// Returns true if the provided world-space rectangle overlaps any blocked tile.
    /// </summary>
    /// <param name="worldBounds">World-space axis-aligned bounds in pixels.</param>
    bool IsWorldRectangleBlocked(Rectangle worldBounds);
}