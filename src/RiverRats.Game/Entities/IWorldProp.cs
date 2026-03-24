using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiverRats.Game.Entities;

/// <summary>
/// Common interface for all world-placed prop entities that can be
/// Y-sorted and drawn in the world pass.
/// </summary>
public interface IWorldProp
{
    /// <summary>Top-left world position in pixels.</summary>
    Vector2 Position { get; }

    /// <summary>World-space bounding rectangle for sorting and collision checks.</summary>
    Rectangle Bounds { get; }

    /// <summary>
    /// When true, the reveal lens will not activate when a character walks behind this prop.
    /// </summary>
    bool SuppressOcclusion => false;

    /// <summary>
    /// Draws the prop in world space at the given sort depth.
    /// </summary>
    /// <param name="spriteBatch">Active sprite batch for the current render pass.</param>
    /// <param name="layerDepth">Depth value for Y-sorting (0 = back, 1 = front).</param>
    void Draw(SpriteBatch spriteBatch, float layerDepth);
}
