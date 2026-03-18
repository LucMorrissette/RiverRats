using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiverRats.Game.Entities;

/// <summary>
/// Static world prop rendered from a dock sprite.
/// </summary>
public sealed class Dock
{
    private readonly Texture2D _texture;
    private readonly Vector2 _position;

    /// <summary>
    /// Creates a dock prop at a world position.
    /// </summary>
    /// <param name="position">Top-left world position in pixels.</param>
    /// <param name="texture">Dock texture used for drawing.</param>
    public Dock(Vector2 position, Texture2D texture)
    {
        _position = position;
        _texture = texture;
    }

    /// <summary>Top-left world position in pixels.</summary>
    public Vector2 Position => _position;

    /// <summary>World-space walkable area covered by this dock sprite.</summary>
    public Rectangle Bounds => new(
        (int)_position.X,
        (int)_position.Y,
        _texture.Width,
        _texture.Height);

    /// <summary>
    /// Draws the dock in world space.
    /// </summary>
    /// <param name="spriteBatch">Sprite batch for the world pass.</param>
    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_texture, _position, Color.White);
    }
}