using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiverRats.Game.Entities;

/// <summary>
/// Static world obstacle rendered from a single sprite and treated as solid for movement.
/// </summary>
public sealed class Boulder : IWorldProp
{
    private readonly Texture2D _texture;
    private readonly Vector2 _position;

    /// <summary>
    /// Creates a boulder obstacle at a world position.
    /// </summary>
    /// <param name="position">Top-left world position in pixels.</param>
    /// <param name="texture">Boulder texture used for drawing and bounds.</param>
    public Boulder(Vector2 position, Texture2D texture, bool suppressOcclusion = false)
    {
        _position = position;
        _texture = texture;
        SuppressOcclusion = suppressOcclusion;
    }

    /// <summary>Top-left world position in pixels.</summary>
    public Vector2 Position => _position;

    /// <summary>Texture used for rendering and bounds calculation.</summary>
    public Texture2D Texture => _texture;

    /// <summary>When true, the reveal lens will not activate when a character walks behind this boulder.</summary>
    public bool SuppressOcclusion { get; }

    /// <summary>World-space blocking bounds for this boulder.</summary>
    public Rectangle Bounds => new(
        (int)_position.X,
        (int)_position.Y,
        _texture.Width,
        _texture.Height);

    /// <summary>
    /// Draws the boulder in world space.
    /// </summary>
    /// <param name="spriteBatch">Sprite batch for the world pass.</param>
    /// <param name="layerDepth">Depth value for Y-sorting (0 = back, 1 = front).</param>
    public void Draw(SpriteBatch spriteBatch, float layerDepth = 0f)
    {
        spriteBatch.Draw(_texture, _position, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
    }
}