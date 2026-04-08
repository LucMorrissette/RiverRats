using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DogDays.Game.Entities;

/// <summary>
/// Static world prop for cabin/building entities. Defines its own
/// collision shape (one or more rectangles) relative to the sprite,
/// eliminating the need for hand-placed collider rectangles in the tilemap.
/// Multiple collision boxes allow non-rectangular footprints such as
/// L-shaped buildings with walkable porches.
/// </summary>
public sealed class Cabin : IWorldProp
{
    private readonly Texture2D _texture;
    private readonly Vector2 _position;
    private readonly Rectangle[] _localCollisionBoxes;

    /// <summary>
    /// Creates a cabin at a world position with one or more collision boxes
    /// defined relative to the sprite's top-left corner.
    /// </summary>
    /// <param name="position">Top-left world position in pixels.</param>
    /// <param name="texture">Cabin texture used for drawing and visual bounds.</param>
    /// <param name="localCollisionBoxes">
    /// One or more collision rectangles relative to the sprite origin (top-left).
    /// Together they form the collision shape that blocks player movement.
    /// </param>
    public Cabin(Vector2 position, Texture2D texture, Rectangle[] localCollisionBoxes, bool suppressOcclusion = false)
    {
        _position = position;
        _texture = texture;
        _localCollisionBoxes = localCollisionBoxes;
        SuppressOcclusion = suppressOcclusion;
    }

    /// <summary>Top-left world position in pixels.</summary>
    public Vector2 Position => _position;

    /// <summary>Texture used for rendering.</summary>
    public Texture2D Texture => _texture;

    /// <summary>Number of collision rectangles in this cabin's collision shape.</summary>
    public int CollisionBoxCount => _localCollisionBoxes.Length;

    /// <summary>When true, the reveal lens will not activate when a character walks behind this cabin.</summary>
    public bool SuppressOcclusion { get; }

    /// <summary>World-space bounding rectangle covering the entire sprite (used for Y-sorting and occlusion).</summary>
    public Rectangle Bounds => new(
        (int)_position.X,
        (int)_position.Y,
        _texture.Width,
        _texture.Height);

    /// <summary>
    /// Returns a world-space collision rectangle at the given index.
    /// </summary>
    /// <param name="index">Index into the collision box array.</param>
    public Rectangle GetCollisionBounds(int index)
    {
        var local = _localCollisionBoxes[index];
        return new Rectangle(
            (int)_position.X + local.X,
            (int)_position.Y + local.Y,
            local.Width,
            local.Height);
    }

    /// <summary>
    /// Draws the cabin in world space.
    /// </summary>
    /// <param name="spriteBatch">Sprite batch for the world pass.</param>
    /// <param name="layerDepth">Depth value for Y-sorting (0 = back, 1 = front).</param>
    public void Draw(SpriteBatch spriteBatch, float layerDepth = 0f)
    {
        spriteBatch.Draw(_texture, _position, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
    }
}
